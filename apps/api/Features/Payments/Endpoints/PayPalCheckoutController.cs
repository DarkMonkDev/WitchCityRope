using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Unified PayPal checkout endpoints that mirror the credit card checkout flow.
/// Stage 1 (create-order): validate -> create pending ticket(s) -> create PayPal order -> link them.
/// Stage 2 (capture-order): capture PayPal payment -> finalize ticket(s) -> activate attendance -> send emails.
/// Stage 3 (cancel-order): roll back pending ticket(s) if user cancels in PayPal popup.
///
/// This replaces the old PayPalCheckoutEndpoints which created PayPal orders without creating tickets,
/// causing a flow mismatch where tickets were never linked to payments.
/// </summary>
/// CSRF Note: This controller uses [IgnoreAntiforgeryToken] because:
/// 1. All endpoints require [Authorize] (JWT cookie auth)
/// 2. Auth cookies use SameSite=Strict which prevents cross-site request forgery
/// 3. PayPal JS SDK makes these calls from an iframe/popup context where
///    antiforgery tokens can't be reliably passed
/// 4. The antiforgery middleware was causing "request field is required" errors
///    because the token couldn't be decrypted after container restarts
///    (DataProtection keys are ephemeral in-container)
[ApiController]
[Route("api/checkout/paypal")]
[Authorize]
[IgnoreAntiforgeryToken]
public class PayPalCheckoutController : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly IAttendanceService _attendanceService;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IEventEmailService _eventEmailService;
    private readonly ILogger<PayPalCheckoutController> _logger;

    public PayPalCheckoutController(
        IPayPalService payPalService,
        IAttendanceService attendanceService,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IEventEmailService eventEmailService,
        ILogger<PayPalCheckoutController> logger)
    {
        _payPalService = payPalService;
        _attendanceService = attendanceService;
        _context = context;
        _encryptionService = encryptionService;
        _eventEmailService = eventEmailService;
        _logger = logger;
    }

    /// <summary>
    /// Stage 1: Validate, create pending ticket purchase(s), then create a PayPal order linked to them.
    /// Called by the frontend PayPalButton's createOrder callback.
    /// Returns the PayPal order ID for the JS SDK popup.
    /// </summary>
    [HttpPost("create-order")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PayPalCheckoutCreateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] PayPalCheckoutCreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        using (_logger.BeginScope(new Dictionary<string, object> { ["PayPalCheckoutCorrelationId"] = correlationId }))
        {
            // ================================================================
            // STAGE 1: VALIDATE
            // ================================================================
            _logger.LogInformation(
                "[PayPalCheckout:{CorrelationId}] STAGE 1/3: Validating. Event={EventId}, Amount={Amount}, IdempotencyKey={IdempotencyKey}",
                correlationId, request.EventId, request.Amount, request.IdempotencyKey);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            if (request.Amount <= 0)
            {
                return PayPalCheckoutProblem(correlationId, "validation",
                    "Payment amount must be greater than zero.", paymentCharged: false);
            }

            // Round sliding scale to integer — frontend sliders can produce floats like 66.666...
            var slidingScalePercentage = (int)Math.Round(request.SlidingScalePercentage);
            if (slidingScalePercentage < 0 || slidingScalePercentage > 75)
            {
                return PayPalCheckoutProblem(correlationId, "validation",
                    "Sliding scale percentage must be between 0 and 75.", paymentCharged: false);
            }

            if (request.TicketTypeIds == null || request.TicketTypeIds.Count == 0)
            {
                return PayPalCheckoutProblem(correlationId, "validation",
                    "No tickets selected.", paymentCharged: false);
            }

            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                return PayPalCheckoutProblem(correlationId, "validation",
                    "Missing checkout session identifier. Please refresh and try again.", paymentCharged: false);
            }

            // Idempotency check: has this checkout already been processed?
            var existingPurchase = await _context.TicketPurchases
                .AsNoTracking()
                .Where(tp => tp.IdempotencyKey == request.IdempotencyKey && tp.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPurchase != null)
            {
                if (existingPurchase.PaymentStatus is TicketPurchasePaymentStatus.Completed or TicketPurchasePaymentStatus.Confirmed)
                {
                    _logger.LogInformation(
                        "[PayPalCheckout:{CorrelationId}] Idempotent duplicate detected. Already completed for TicketPurchase {TicketPurchaseId}",
                        correlationId, existingPurchase.Id);

                    return PayPalCheckoutProblem(correlationId, "validation",
                        "This purchase has already been completed.", paymentCharged: false);
                }

                // If there's a pending purchase with same key that has a PayPal order already,
                // return the existing order ID so the popup can resume
                if (existingPurchase.PayPalOrderIdHash != null)
                {
                    var existingOrderId = existingPurchase.EncryptedPayPalOrderId != null
                        ? await _encryptionService.DecryptAsync(existingPurchase.EncryptedPayPalOrderId)
                        : null;

                    if (!string.IsNullOrEmpty(existingOrderId))
                    {
                        _logger.LogInformation(
                            "[PayPalCheckout:{CorrelationId}] Returning existing PayPal order for idempotent retry",
                            correlationId);

                        return Ok(new PayPalCheckoutCreateOrderResponse { OrderId = existingOrderId });
                    }
                }

                if (existingPurchase.PaymentStatus == TicketPurchasePaymentStatus.Failed)
                {
                    _logger.LogInformation(
                        "[PayPalCheckout:{CorrelationId}] Previous attempt with same key failed. Allowing retry.",
                        correlationId);
                }
            }

            _logger.LogInformation("[PayPalCheckout:{CorrelationId}] STAGE 1 COMPLETE: Validation passed", correlationId);

            // ================================================================
            // STAGE 2: CREATE PENDING TICKET PURCHASE
            // ================================================================
            _logger.LogInformation(
                "[PayPalCheckout:{CorrelationId}] STAGE 2/3: Creating pending ticket purchase. TicketTypes=[{TicketTypeIds}]",
                correlationId, string.Join(", ", request.TicketTypeIds));

            var ticketRequest = new CreateTicketPurchaseRequest
            {
                EventId = request.EventId,
                TicketTypeIds = request.TicketTypeIds,
                EventWaiverAccepted = request.EventWaiverAccepted,
                PaymentMethodId = "paypal-pending",
                Notes = $"PayPal Checkout {correlationId}",
                Amount = request.Amount,
                SlidingScalePercentage = slidingScalePercentage
            };

            var ticketResult = await _attendanceService.CreateTicketPurchaseAsync(
                ticketRequest, userId, cancellationToken);

            if (!ticketResult.IsSuccess)
            {
                _logger.LogWarning(
                    "[PayPalCheckout:{CorrelationId}] STAGE 2 FAILED: {Error}",
                    correlationId, ticketResult.Error);

                return PayPalCheckoutProblem(correlationId, "ticket_creation",
                    ticketResult.Error ?? "Unable to create ticket purchase.", paymentCharged: false);
            }

            // Find the ticket purchases we just created
            var pendingPurchases = await _context.TicketPurchases
                .Where(tp => tp.UserId == userId
                    && tp.PaymentMethod == "paypal-pending"
                    && tp.Notes != null && tp.Notes.Contains($"PayPal Checkout {correlationId}"))
                .ToListAsync(cancellationToken);

            if (pendingPurchases.Count == 0)
            {
                _logger.LogError(
                    "[PayPalCheckout:{CorrelationId}] STAGE 2 ANOMALY: CreateTicketPurchaseAsync succeeded but no pending purchases found",
                    correlationId);

                return PayPalCheckoutProblem(correlationId, "ticket_creation",
                    "Ticket records were not created correctly. Please try again.", paymentCharged: false);
            }

            // Set idempotency key on all created purchases
            foreach (var tp in pendingPurchases)
            {
                tp.IdempotencyKey = request.IdempotencyKey;
            }
            await _context.SaveChangesAsync(cancellationToken);

            var ticketPurchaseIds = pendingPurchases.Select(tp => tp.Id).ToList();

            _logger.LogInformation(
                "[PayPalCheckout:{CorrelationId}] STAGE 2 COMPLETE: Created {Count} pending ticket purchase(s). IDs=[{Ids}]",
                correlationId, pendingPurchases.Count, string.Join(", ", ticketPurchaseIds));

            // ================================================================
            // STAGE 3: CREATE PAYPAL ORDER
            // Wrapped in try/catch to ensure pending tickets are rolled back
            // if any step fails (e.g., encryption, DB save) after Stage 2
            // committed them to the database.
            // ================================================================
            try
            {
                _logger.LogInformation(
                    "[PayPalCheckout:{CorrelationId}] STAGE 3/3: Creating PayPal order. Amount={Amount}",
                    correlationId, request.Amount);

                var money = Money.Create(request.Amount, request.Currency ?? "USD");

                var metadata = new Dictionary<string, string>
                {
                    ["correlationId"] = correlationId,
                    ["ticketPurchaseIds"] = string.Join(",", ticketPurchaseIds)
                };
                if (!string.IsNullOrEmpty(request.EventTitle))
                    metadata["eventTitle"] = request.EventTitle;

                var orderResult = await _payPalService.CreateOrderAsync(
                    money, userId, slidingScalePercentage, metadata, cancellationToken);

                if (!orderResult.IsSuccess)
                {
                    _logger.LogError(
                        "[PayPalCheckout:{CorrelationId}] STAGE 3 FAILED: PayPal order creation failed. {Error}. Rolling back pending tickets.",
                        correlationId, orderResult.ErrorMessage);

                    // Roll back pending tickets since PayPal order failed
                    await RollbackPendingPurchasesAsync(ticketPurchaseIds, correlationId, cancellationToken);

                    return PayPalCheckoutProblem(correlationId, "payment",
                        "Unable to create PayPal payment. Please try again.", paymentCharged: false);
                }

                var orderId = orderResult.Value!.OrderId;

                // Link PayPal order to all pending ticket purchases
                foreach (var tp in pendingPurchases)
                {
                    tp.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(orderId);
                    tp.PayPalOrderIdHash = ComputeSha256Hash(orderId);
                    tp.PaymentMethod = "PayPal";
                }
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[PayPalCheckout:{CorrelationId}] STAGE 3 COMPLETE: PayPal order {OrderId} created and linked to {Count} ticket purchase(s)",
                    correlationId, orderId, pendingPurchases.Count);

                return Ok(new PayPalCheckoutCreateOrderResponse { OrderId = orderId });
            }
            catch (Exception ex)
            {
                // Any unhandled exception after Stage 2 (pending tickets created) must
                // roll back those tickets to prevent orphaned PendingPayment records
                // that block future purchases for the same sessions.
                _logger.LogError(ex,
                    "[PayPalCheckout:{CorrelationId}] STAGE 3 FAILED with exception: {Message}. Rolling back pending tickets.",
                    correlationId, ex.Message);

                await RollbackPendingPurchasesAsync(ticketPurchaseIds, correlationId, cancellationToken);

                return PayPalCheckoutProblem(correlationId, "payment",
                    "An unexpected error occurred during payment setup. Please try again.", paymentCharged: false);
            }
        }
    }

    /// <summary>
    /// Stage 2: Capture the PayPal order after user approval, then finalize ticket purchases.
    /// Called by the frontend PayPalButton's onApprove callback.
    /// </summary>
    [HttpPost("capture-order")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PayPalCheckoutCaptureOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CaptureOrder(
        [FromBody] PayPalCheckoutCaptureOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.OrderId))
        {
            return Problem(title: "Bad Request", detail: "OrderId is required", statusCode: 400);
        }

        _logger.LogInformation(
            "Capturing PayPal order {OrderId} for user {UserId}", request.OrderId, userId);

        // Look up pending ticket purchases by PayPal order ID hash
        var orderIdHash = ComputeSha256Hash(request.OrderId);
        var pendingPurchases = await _context.TicketPurchases
            .Where(tp => tp.PayPalOrderIdHash == orderIdHash && tp.UserId == userId)
            .ToListAsync(cancellationToken);

        if (pendingPurchases.Count == 0)
        {
            _logger.LogWarning(
                "No pending ticket purchases found for PayPal order {OrderId}", request.OrderId);
            return Problem(title: "Not Found",
                detail: "No ticket purchase found for this PayPal order.", statusCode: 404);
        }

        var ticketPurchaseIds = pendingPurchases.Select(tp => tp.Id).ToList();

        // Capture the PayPal order
        var captureResult = await _payPalService.CaptureOrderAsync(request.OrderId, cancellationToken);

        if (!captureResult.IsSuccess)
        {
            _logger.LogError(
                "PayPal capture failed for order {OrderId}: {Error}. Rolling back pending tickets.",
                request.OrderId, captureResult.ErrorMessage);

            await RollbackPendingPurchasesAsync(ticketPurchaseIds, "capture-fail", cancellationToken);

            return Problem(title: "Payment Error",
                detail: "PayPal payment capture failed. Your tickets have been released. Please try again.",
                statusCode: 500);
        }

        var capture = captureResult.Value!;

        _logger.LogInformation(
            "PayPal order {OrderId} captured successfully. CaptureId={CaptureId}. Finalizing {Count} ticket purchase(s).",
            request.OrderId, capture.CaptureId, pendingPurchases.Count);

        // Finalize all ticket purchases
        try
        {
            // Finalize each ticket purchase with PayPal capture details.
            // Also set TotalPrice from the captured amount as a safety net —
            // it should already be set during pending creation, but if it was $0
            // (e.g., sliding scale ticket where ticketType.Price was null), this
            // ensures we record what PayPal actually charged.
            var capturedAmount = capture.Amount.GetDecimalValue();

            foreach (var tp in pendingPurchases)
            {
                tp.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(capture.CaptureId);
                tp.EncryptedPayPalPayerId = capture.PayerId != null
                    ? await _encryptionService.EncryptAsync(capture.PayerId)
                    : null;
                tp.PaymentStatus = TicketPurchasePaymentStatus.Completed;
                tp.PaymentMethod = "PayPal";
                tp.ProcessedAt = DateTime.UtcNow;
                tp.UpdatedAt = DateTime.UtcNow;

                // Safety net: update TotalPrice from PayPal's captured amount if it's still $0
                if (tp.TotalPrice == 0m && capturedAmount > 0m)
                {
                    _logger.LogWarning(
                        "TicketPurchase {Id} had TotalPrice=$0, updating from PayPal captured amount ${Amount}",
                        tp.Id, capturedAmount);
                    tp.TotalPrice = capturedAmount;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayPal capture persisted to {Count} TicketPurchase(s). IDs=[{Ids}]",
                pendingPurchases.Count, string.Join(", ", ticketPurchaseIds));

            // Activate attendance records now that payment is confirmed
            var activateResult = await _attendanceService.ActivateAttendanceForPurchasesAsync(
                ticketPurchaseIds, cancellationToken);
            if (!activateResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to activate attendance for purchases [{Ids}]: {Error}",
                    string.Join(", ", ticketPurchaseIds), activateResult.Error);
            }

            // Post-purchase emails (non-fatal side effect)
            try
            {
                await _eventEmailService.SendPostPurchaseEmailsAsync(
                    userId, ticketPurchaseIds, cancellationToken);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "Post-purchase email failed (non-fatal). Purchases=[{Ids}]",
                    string.Join(", ", ticketPurchaseIds));
            }
        }
        catch (Exception ex)
        {
            // CRITICAL: Payment was captured but we failed to finalize ticket records.
            // Unlike credit cards, we can't easily void a PayPal capture.
            // Log critically and notify admin.
            _logger.LogCritical(ex,
                "CRITICAL: PayPal payment captured (CaptureId={CaptureId}, OrderId={OrderId}) " +
                "but failed to finalize ticket purchases [{TicketPurchaseIds}]. MANUAL INTERVENTION REQUIRED.",
                capture.CaptureId, request.OrderId, string.Join(", ", ticketPurchaseIds));

            return Problem(
                title: "Payment Processing Error",
                detail: "Your PayPal payment was processed but we encountered an error completing your registration. " +
                        "Please contact info@witchcityrope.com for assistance.",
                statusCode: 500);
        }

        var confirmationNumber = pendingPurchases.First().PaymentReference;

        return Ok(new PayPalCheckoutCaptureOrderResponse
        {
            CaptureId = capture.CaptureId,
            Status = capture.Status,
            Amount = capture.Amount.Value,
            Currency = capture.Amount.CurrencyCode,
            PayerId = capture.PayerId,
            ConfirmationNumber = confirmationNumber,
            TicketPurchaseIds = ticketPurchaseIds
        });
    }

    /// <summary>
    /// Cancel a pending PayPal checkout. Rolls back the pending ticket purchases
    /// that were created during create-order.
    /// Called by the frontend PayPalButton's onCancel callback.
    /// </summary>
    [HttpPost("cancel-order")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelOrder(
        [FromBody] PayPalCheckoutCancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.OrderId))
        {
            return Ok(); // Nothing to cancel
        }

        _logger.LogInformation(
            "Cancelling PayPal checkout for order {OrderId}, user {UserId}", request.OrderId, userId);

        var orderIdHash = ComputeSha256Hash(request.OrderId);
        var pendingPurchases = await _context.TicketPurchases
            .Where(tp => tp.PayPalOrderIdHash == orderIdHash
                && tp.UserId == userId
                && tp.PaymentStatus == TicketPurchasePaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingPurchases.Count > 0)
        {
            var ticketPurchaseIds = pendingPurchases.Select(tp => tp.Id).ToList();
            await RollbackPendingPurchasesAsync(ticketPurchaseIds, "user-cancelled", cancellationToken);

            _logger.LogInformation(
                "Rolled back {Count} pending ticket purchase(s) for cancelled PayPal order {OrderId}",
                pendingPurchases.Count, request.OrderId);
        }

        return Ok();
    }

    /// <summary>
    /// Roll back pending ticket purchases and their attendance records when payment fails or is cancelled.
    /// </summary>
    private async Task RollbackPendingPurchasesAsync(
        List<Guid> ticketPurchaseIds, string reason, CancellationToken cancellationToken)
    {
        try
        {
            var purchases = await _context.TicketPurchases
                .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                .ToListAsync(cancellationToken);

            foreach (var tp in purchases)
            {
                tp.PaymentStatus = TicketPurchasePaymentStatus.Failed;
                tp.UpdatedAt = DateTime.UtcNow;
            }

            // Cancel associated attendance records
            var attendances = await _context.EventAttendances
                .Where(ea => ea.TicketPurchaseId.HasValue
                    && ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value))
                .ToListAsync(cancellationToken);

            foreach (var att in attendances)
            {
                att.Status = AttendanceStatus.Cancelled;
                att.CancelledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Rolled back {PurchaseCount} ticket purchase(s) and {AttendanceCount} attendance record(s). Reason: {Reason}",
                purchases.Count, attendances.Count, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during rollback of ticket purchases [{TicketPurchaseIds}]",
                string.Join(", ", ticketPurchaseIds));
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Creates a ProblemDetails response with PayPal checkout-specific extensions.
    /// </summary>
    private IActionResult PayPalCheckoutProblem(
        string correlationId, string failureStage, string detail, bool paymentCharged)
    {
        return Problem(
            title: failureStage switch
            {
                "validation" => "Checkout Validation Failed",
                "ticket_creation" => "Unable to Process Order",
                "payment" => "PayPal Payment Failed",
                _ => "Checkout Failed"
            },
            detail: detail,
            statusCode: paymentCharged ? 500 : 400,
            extensions: new Dictionary<string, object?>
            {
                ["checkoutCorrelationId"] = correlationId,
                ["failureStage"] = failureStage,
                ["paymentCharged"] = paymentCharged
            });
    }
}

// --- Request/Response DTOs ---

public class PayPalCheckoutCreateOrderRequest
{
    public Guid EventId { get; set; }
    public List<Guid> TicketTypeIds { get; set; } = new();
    public decimal Amount { get; set; }
    /// <summary>
    /// Sliding scale discount percentage (0-75). Accepts decimal from frontend
    /// (JavaScript sliders produce floats like 66.666...) and is rounded to int
    /// before being passed to downstream services.
    /// </summary>
    public decimal SlidingScalePercentage { get; set; }
    public string? Currency { get; set; }
    public string? EventTitle { get; set; }
    public bool EventWaiverAccepted { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class PayPalCheckoutCreateOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
}

public class PayPalCheckoutCaptureOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
}

public class PayPalCheckoutCaptureOrderResponse
{
    public string CaptureId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Amount { get; set; } = "0.00";
    public string Currency { get; set; } = "USD";
    public string? PayerId { get; set; }
    public string? ConfirmationNumber { get; set; }
    public List<Guid> TicketPurchaseIds { get; set; } = new();
}

public class PayPalCheckoutCancelOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
}
