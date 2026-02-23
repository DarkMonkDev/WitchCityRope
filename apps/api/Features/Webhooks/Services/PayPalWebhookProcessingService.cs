using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Webhooks.Services;

public interface IPayPalWebhookProcessingService
{
    Task<Result> ProcessEventAsync(PayPalWebhookEvent webhookEvent, string transmissionId, CancellationToken ct);
}

/// <summary>
/// Handles database updates triggered by PayPal webhook events.
/// Separated from PayPalService to maintain single responsibility.
/// Includes idempotency via IMemoryCache with 24-hour TTL.
/// </summary>
public class PayPalWebhookProcessingService : IPayPalWebhookProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IPaymentNotificationService _notificationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PayPalWebhookProcessingService> _logger;

    public PayPalWebhookProcessingService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IPaymentNotificationService notificationService,
        IMemoryCache cache,
        ILogger<PayPalWebhookProcessingService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _notificationService = notificationService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> ProcessEventAsync(
        PayPalWebhookEvent webhookEvent,
        string transmissionId,
        CancellationToken ct)
    {
        // Idempotency check
        var cacheKey = $"webhook-processed:{transmissionId}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            _logger.LogInformation("Duplicate webhook {TransmissionId} - skipping", transmissionId);
            return Result.Success();
        }

        _logger.LogInformation("Processing PayPal webhook event: {EventType}, ID: {EventId}",
            webhookEvent.EventType, webhookEvent.Id);

        var result = webhookEvent.EventType switch
        {
            "CHECKOUT.ORDER.APPROVED" => HandleOrderApproved(webhookEvent),
            "PAYMENT.CAPTURE.COMPLETED" => await HandleCaptureCompletedAsync(webhookEvent, ct),
            "PAYMENT.CAPTURE.DENIED" => await HandleCaptureDeniedAsync(webhookEvent, ct),
            "PAYMENT.CAPTURE.PENDING" => await HandleCapturePendingAsync(webhookEvent, ct),
            "CUSTOMER.DISPUTE.CREATED" => HandleDisputeCreated(webhookEvent),
            _ => HandleUnknownEvent(webhookEvent.EventType)
        };

        // Mark as processed
        _cache.Set(cacheKey, true, TimeSpan.FromHours(24));

        return result;
    }

    private Result HandleOrderApproved(PayPalWebhookEvent webhookEvent)
    {
        _logger.LogInformation("PayPal order approved - no action needed (waiting for capture). EventId: {EventId}",
            webhookEvent.Id);
        return Result.Success();
    }

    private async Task<Result> HandleCaptureCompletedAsync(PayPalWebhookEvent webhookEvent, CancellationToken ct)
    {
        _logger.LogInformation("PayPal capture completed: {EventId}, Summary: {Summary}",
            webhookEvent.Id, webhookEvent.Summary);

        try
        {
            var captureId = ExtractStringFromResource(webhookEvent.Resource, "id");
            var orderId = ExtractOrderIdFromResource(webhookEvent.Resource);

            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("PAYMENT.CAPTURE.COMPLETED webhook missing order ID. CaptureId: {CaptureId}",
                    captureId);
                return Result.Success(); // Don't fail - endpoint capture already persists
            }

            var orderIdHash = ComputeSha256Hash(orderId);
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.PayPalOrderIdHash == orderIdHash, ct);

            if (ticketPurchase == null)
            {
                _logger.LogWarning(
                    "No TicketPurchase found for PayPal OrderId hash from webhook. OrderIdHash: {Hash}",
                    orderIdHash);
                return Result.Success(); // Don't fail - endpoint may have already handled this
            }

            // Verify by decrypting stored OrderId
            if (!string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalOrderId))
            {
                var storedOrderId = await _encryptionService.DecryptAsync(ticketPurchase.EncryptedPayPalOrderId);
                if (storedOrderId != orderId)
                {
                    _logger.LogWarning(
                        "PayPal OrderId hash collision detected. Webhook OrderId doesn't match stored OrderId.");
                    return Result.Failure("OrderId verification failed");
                }
            }

            // Update TicketPurchase
            if (!string.IsNullOrEmpty(captureId) && string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalCaptureId))
            {
                ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(captureId);
            }

            ticketPurchase.PaymentStatus = "Completed";
            ticketPurchase.ProcessedAt ??= DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Webhook updated TicketPurchase {TicketPurchaseId} to Completed",
                ticketPurchase.Id);

            // Send SSE notification if kiosk session
            await TrySendSseNotificationAsync(webhookEvent, ticketPurchase);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PAYMENT.CAPTURE.COMPLETED webhook");
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private async Task<Result> HandleCaptureDeniedAsync(PayPalWebhookEvent webhookEvent, CancellationToken ct)
    {
        _logger.LogWarning("PayPal capture denied: {EventId}", webhookEvent.Id);

        try
        {
            var orderId = ExtractOrderIdFromResource(webhookEvent.Resource);
            if (string.IsNullOrEmpty(orderId))
                return Result.Success();

            var orderIdHash = ComputeSha256Hash(orderId);
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.PayPalOrderIdHash == orderIdHash, ct);

            if (ticketPurchase != null)
            {
                ticketPurchase.PaymentStatus = "Failed";
                await _context.SaveChangesAsync(ct);

                _logger.LogWarning(
                    "Webhook updated TicketPurchase {TicketPurchaseId} to Failed (capture denied)",
                    ticketPurchase.Id);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PAYMENT.CAPTURE.DENIED webhook");
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private async Task<Result> HandleCapturePendingAsync(PayPalWebhookEvent webhookEvent, CancellationToken ct)
    {
        _logger.LogInformation("PayPal capture pending: {EventId}", webhookEvent.Id);

        try
        {
            var orderId = ExtractOrderIdFromResource(webhookEvent.Resource);
            if (string.IsNullOrEmpty(orderId))
                return Result.Success();

            var orderIdHash = ComputeSha256Hash(orderId);
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.PayPalOrderIdHash == orderIdHash, ct);

            if (ticketPurchase != null)
            {
                // Keep as Pending (already the default)
                var pendingReason = ExtractStringFromResource(webhookEvent.Resource, "status_details");

                _logger.LogInformation(
                    "TicketPurchase {TicketPurchaseId} payment pending. Reason: {Reason}",
                    ticketPurchase.Id, pendingReason ?? "unknown");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PAYMENT.CAPTURE.PENDING webhook");
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private Result HandleDisputeCreated(PayPalWebhookEvent webhookEvent)
    {
        _logger.LogWarning(
            "PayPal dispute created: EventId={EventId}, Summary={Summary}, Resource={Resource}",
            webhookEvent.Id,
            webhookEvent.Summary,
            JsonSerializer.Serialize(webhookEvent.Resource));

        // Email notification deferred — no admin email system yet
        return Result.Success();
    }

    private Result HandleUnknownEvent(string eventType)
    {
        _logger.LogInformation("Unhandled PayPal webhook event type: {EventType}", eventType);
        return Result.Success();
    }

    #region Helper Methods

    private async Task TrySendSseNotificationAsync(PayPalWebhookEvent webhookEvent, TicketPurchase ticketPurchase)
    {
        try
        {
            var sessionToken = ExtractStringFromResource(webhookEvent.Resource, "custom_id");
            var captureId = ExtractStringFromResource(webhookEvent.Resource, "id");
            var amount = ExtractAmountFromResource(webhookEvent.Resource);

            if (!string.IsNullOrEmpty(sessionToken) && amount.HasValue && !string.IsNullOrEmpty(captureId))
            {
                // Try to find attendee from EventAttendance linked to this TicketPurchase
                var attendance = await _context.EventAttendances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ea => ea.TicketPurchaseId == ticketPurchase.Id);

                if (attendance != null)
                {
                    var notificationSent = await _notificationService.NotifyPaymentComplete(
                        sessionToken,
                        attendance.Id,
                        attendance.EventId,
                        Guid.Parse(captureId),
                        amount.Value,
                        "PayPal");

                    if (notificationSent)
                    {
                        _logger.LogInformation(
                            "SSE notification sent for PayPal payment: Amount ${Amount}",
                            amount.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SSE notification for PayPal payment completion");
        }
    }

    private static string? ExtractStringFromResource(Dictionary<string, object> resource, string key)
    {
        if (resource.TryGetValue(key, out var value))
        {
            if (value is string s)
                return s;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                return je.GetString();
        }
        return null;
    }

    private static string? ExtractOrderIdFromResource(Dictionary<string, object> resource)
    {
        // Try supplementary_data.related_ids.order_id first
        if (resource.TryGetValue("supplementary_data", out var suppData))
        {
            if (suppData is JsonElement suppElement && suppElement.ValueKind == JsonValueKind.Object)
            {
                if (suppElement.TryGetProperty("related_ids", out var relatedIds) &&
                    relatedIds.TryGetProperty("order_id", out var orderId))
                {
                    return orderId.GetString();
                }
            }
        }

        // Fall back to custom_id (used when we store orderId in custom_id during creation)
        return ExtractStringFromResource(resource, "custom_id");
    }

    private static decimal? ExtractAmountFromResource(Dictionary<string, object> resource)
    {
        if (resource.TryGetValue("amount", out var amountObj))
        {
            if (amountObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                if (je.TryGetProperty("value", out var valueEl) &&
                    decimal.TryParse(valueEl.GetString(), out var amount))
                {
                    return amount;
                }
            }
        }
        return null;
    }

    private static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }

    #endregion
}
