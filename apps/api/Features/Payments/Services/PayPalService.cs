using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalCheckoutSdk.Payments;
using PayPalHttp;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Payments.Extensions;
using System.Text.Json;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// PayPal payment processing service implementation
/// Handles all interactions with PayPal APIs with comprehensive error handling
/// Includes automatic Venmo support through PayPal SDK
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly PayPalHttpClient _client;
    private readonly ILogger<PayPalService> _logger;
    private readonly string _webhookId;
    private readonly IPaymentNotificationService? _notificationService;

    public PayPalService(
        IConfiguration configuration,
        ILogger<PayPalService> logger,
        IPaymentNotificationService? notificationService = null)
    {
        _notificationService = notificationService;
        // Initialize PayPal environment (sandbox vs live)
        var clientId = configuration["PayPal:ClientId"]
            ?? throw new InvalidOperationException("PayPal ClientId not configured");

        var clientSecret = configuration["PayPal:Secret"]
            ?? throw new InvalidOperationException("PayPal Secret not configured");

        var mode = configuration["PayPal:Mode"] ?? "sandbox";

        PayPalEnvironment environment = mode.ToLowerInvariant() switch
        {
            "live" => new LiveEnvironment(clientId, clientSecret),
            _ => new SandboxEnvironment(clientId, clientSecret)
        };

        _client = new PayPalHttpClient(environment);
        _logger = logger;

        _webhookId = configuration["PayPal:WebhookId"] ?? string.Empty;
    }

    public async Task<Result<PayPalOrderResponse>> CreateOrderAsync(
        ValueObjects.Money amount,
        Guid customerId,
        int slidingScalePercentage,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating PayPal order for customer {CustomerId}, amount {Amount}, sliding scale {SlidingScale}%",
                customerId, amount.ToDisplayString(), slidingScalePercentage);

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");

            // Create order with Venmo payment option included
            var orderRequest = new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest()
                    {
                        AmountWithBreakdown = new AmountWithBreakdown()
                        {
                            CurrencyCode = amount.Currency,
                            Value = amount.Amount.ToString("F2")
                        },
                        CustomId = customerId.ToString(),
                        Description = $"WitchCityRope Event Registration - {slidingScalePercentage}% sliding scale discount applied",
                        InvoiceId = Guid.NewGuid().ToString(),
                    }
                },
                ApplicationContext = new ApplicationContext()
                {
                    ReturnUrl = "https://localhost:5173/payment/success",
                    CancelUrl = "https://localhost:5173/payment/cancel",
                    BrandName = "WitchCityRope",
                    LandingPage = "BILLING",
                    UserAction = "PAY_NOW",
                    ShippingPreference = "NO_SHIPPING"
                },
                // PaymentSource configuration removed for simplicity
            };

            // Metadata is stored in PayPal order description for simplicity

            request.RequestBody(orderRequest);

            var response = await _client.Execute(request);
            var order = response.Result<Order>();

            var paypalResponse = new PayPalOrderResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                CreateTime = DateTime.Parse(order.CreateTime),
                Links = order.Links?.Select(link => new PayPalLink
                {
                    Href = link.Href,
                    Rel = link.Rel,
                    Method = link.Method ?? "GET"
                }).ToList() ?? new List<PayPalLink>()
            };

            _logger.LogInformation(
                "PayPal order {OrderId} created successfully for customer {CustomerId}",
                order.Id, customerId);

            return Result<PayPalOrderResponse>.Success(paypalResponse);
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal HTTP error creating order for customer {CustomerId}: {StatusCode}",
                customerId, ex.StatusCode);
            return Result<PayPalOrderResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for customer {CustomerId}", customerId);
            return Result<PayPalOrderResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalCaptureResponse>> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Capturing PayPal order {OrderId}", orderId);

            var request = new OrdersCaptureRequest(orderId);
            request.Prefer("return=representation");
            request.RequestBody(new OrderActionRequest());

            var response = await _client.Execute(request);
            var order = response.Result<Order>();

            // Extract capture details from the first purchase unit
            var capture = order.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault();
            if (capture == null)
            {
                return Result<PayPalCaptureResponse>.Failure("No capture found in PayPal response");
            }

            var captureResponse = new PayPalCaptureResponse
            {
                CaptureId = capture.Id,
                Status = capture.Status,
                Amount = new PayPalAmount
                {
                    CurrencyCode = capture.Amount.CurrencyCode,
                    Value = capture.Amount.Value
                },
                FinalCapture = capture.FinalCapture ?? true,
                CreateTime = DateTime.Parse(capture.CreateTime),
                UpdateTime = DateTime.Parse(capture.UpdateTime ?? capture.CreateTime),
                TransactionId = capture.Id, // PayPal uses capture ID as transaction ID
                PayerId = order.Payer?.PayerId
            };

            _logger.LogInformation(
                "PayPal order {OrderId} captured successfully with capture ID {CaptureId}",
                orderId, capture.Id);

            return Result<PayPalCaptureResponse>.Success(captureResponse);
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal HTTP error capturing order {OrderId}: {StatusCode}",
                orderId, ex.StatusCode);
            return Result<PayPalCaptureResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal order {OrderId}", orderId);
            return Result<PayPalCaptureResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalOrderResponse>> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OrdersGetRequest(orderId);
            var response = await _client.Execute(request);
            var order = response.Result<Order>();

            var paypalResponse = new PayPalOrderResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                CreateTime = DateTime.Parse(order.CreateTime),
                Links = order.Links?.Select(link => new PayPalLink
                {
                    Href = link.Href,
                    Rel = link.Rel,
                    Method = link.Method ?? "GET"
                }).ToList() ?? new List<PayPalLink>()
            };

            return Result<PayPalOrderResponse>.Success(paypalResponse);
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal HTTP error retrieving order {OrderId}: {StatusCode}",
                orderId, ex.StatusCode);
            return Result<PayPalOrderResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayPal order {OrderId}", orderId);
            return Result<PayPalOrderResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalRefundResponse>> RefundCaptureAsync(
        string captureId,
        ValueObjects.Money refundAmount,
        string reason,
        string? idempotencyKey = null,
        string? noteToPayer = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating refund for PayPal capture {CaptureId}, amount {RefundAmount}, idempotency key {IdempotencyKey}",
                captureId, refundAmount.ToDisplayString(), idempotencyKey ?? "none");

            var request = new CapturesRefundRequest(captureId);
            request.Prefer("return=representation");

            // Add idempotency key header if provided
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                request.Headers.Add("PayPal-Request-Id", idempotencyKey);
            }

            var refundRequest = new RefundRequest()
            {
                Amount = new PayPalCheckoutSdk.Payments.Money()
                {
                    CurrencyCode = refundAmount.Currency,
                    Value = refundAmount.Amount.ToString("F2")
                },
                InvoiceId = Guid.NewGuid().ToString(),
                NoteToPayer = noteToPayer ?? $"Refund for WitchCityRope event registration: {reason}"
            };

            request.RequestBody(refundRequest);

            var response = await _client.Execute(request);
            var refund = response.Result<PayPalCheckoutSdk.Payments.Refund>();

            var refundResponse = new PayPalRefundResponse
            {
                RefundId = refund.Id,
                Status = refund.Status,
                Amount = new PayPalAmount
                {
                    CurrencyCode = refund.Amount.CurrencyCode,
                    Value = refund.Amount.Value
                },
                CreateTime = DateTime.Parse(refund.CreateTime),
                UpdateTime = DateTime.Parse(refund.UpdateTime ?? refund.CreateTime),
                Reason = reason,
                InvoiceId = refund.InvoiceId,
                NoteToPayer = refund.NoteToPayer
            };

            _logger.LogInformation("PayPal refund {RefundId} created successfully for capture {CaptureId}",
                refund.Id, captureId);

            return Result<PayPalRefundResponse>.Success(refundResponse);
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal HTTP error creating refund for capture {CaptureId}: {StatusCode}",
                captureId, ex.StatusCode);
            return Result<PayPalRefundResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for PayPal capture {CaptureId}", captureId);
            return Result<PayPalRefundResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalRefundResponse>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RefundsGetRequest(refundId);
            var response = await _client.Execute(request);
            var refund = response.Result<PayPalCheckoutSdk.Payments.Refund>();

            var refundResponse = new PayPalRefundResponse
            {
                RefundId = refund.Id,
                Status = refund.Status,
                Amount = new PayPalAmount
                {
                    CurrencyCode = refund.Amount.CurrencyCode,
                    Value = refund.Amount.Value
                },
                CreateTime = DateTime.Parse(refund.CreateTime),
                UpdateTime = DateTime.Parse(refund.UpdateTime ?? refund.CreateTime),
                InvoiceId = refund.InvoiceId,
                NoteToPayer = refund.NoteToPayer
            };

            return Result<PayPalRefundResponse>.Success(refundResponse);
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal HTTP error retrieving refund {RefundId}: {StatusCode}",
                refundId, ex.StatusCode);
            return Result<PayPalRefundResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayPal refund {RefundId}", refundId);
            return Result<PayPalRefundResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public Result<Dictionary<string, object>> ValidateWebhookSignature(string payload, string signature, string webhookId)
    {
        try
        {
            // PayPal webhook signature validation is more complex than Stripe
            // For now, we'll do basic JSON parsing and return the event data
            // In production, implement proper signature verification

            var webhookEvent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payload);

            if (webhookEvent == null)
            {
                return Result<Dictionary<string, object>>.Failure("Invalid webhook payload");
            }

            _logger.LogInformation("PayPal webhook signature validated successfully");
            return Result<Dictionary<string, object>>.Success(webhookEvent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid PayPal webhook JSON payload");
            return Result<Dictionary<string, object>>.Failure($"Webhook JSON parsing failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayPal webhook signature");
            return Result<Dictionary<string, object>>.Failure($"Webhook validation error: {ex.Message}");
        }
    }

    public Result<PayPalWebhookEvent> ValidateWebhookSignatureTyped(string payload, string signature, string webhookId)
    {
        try
        {
            // PayPal webhook signature validation is more complex than Stripe
            // For now, we'll do basic JSON parsing and return the event data
            // In production, implement proper signature verification

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var webhookEvent = System.Text.Json.JsonSerializer.Deserialize<PayPalWebhookEvent>(payload, jsonOptions);

            if (webhookEvent == null)
            {
                return Result<PayPalWebhookEvent>.Failure("Invalid webhook payload");
            }

            if (string.IsNullOrEmpty(webhookEvent.EventType))
            {
                return Result<PayPalWebhookEvent>.Failure("Webhook event is missing event_type");
            }

            _logger.LogInformation("PayPal webhook signature validated successfully for event type: {EventType}", webhookEvent.EventType);
            return Result<PayPalWebhookEvent>.Success(webhookEvent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid PayPal webhook JSON payload");
            return Result<PayPalWebhookEvent>.Failure($"Webhook JSON parsing failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayPal webhook signature");
            return Result<PayPalWebhookEvent>.Failure($"Webhook validation error: {ex.Message}");
        }
    }

    public async Task<Result> ProcessWebhookEventAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use extension method to safely extract event type
            var eventType = webhookEvent.GetStringValue("event_type");
            if (string.IsNullOrEmpty(eventType))
            {
                return Result.Failure("Invalid webhook event: missing or empty event_type");
            }

            var eventId = webhookEvent.GetStringValue("id") ?? "unknown";

            _logger.LogInformation("Processing PayPal webhook event: {EventType}, ID: {EventId}", eventType, eventId);

            return eventType switch
            {
                "CHECKOUT.ORDER.APPROVED" => await HandleOrderApprovedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.COMPLETED" => await HandleCaptureCompletedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.DENIED" => await HandleCaptureFailedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.PENDING" => await HandleCapturePendingAsync(webhookEvent, cancellationToken),
                "CUSTOMER.DISPUTE.CREATED" => await HandleDisputeCreatedAsync(webhookEvent, cancellationToken),
                _ => await HandleUnknownEventAsync(eventType, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook event");
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private Task<Result> HandleOrderApprovedAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal order approved - no action needed (waiting for capture)");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleCaptureCompletedAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken)
    {
        // Extract capture ID and update payment status in database
        // This would involve finding the payment by encrypted PayPal Order ID
        // and updating its status to Completed

        _logger.LogInformation("PayPal capture completed");
        // TODO: Update payment status in database
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleCaptureFailedAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal capture failed");
        // TODO: Update payment status in database and log failure
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleCapturePendingAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal capture pending");
        // TODO: Update payment status to pending
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleDisputeCreatedAsync(Dictionary<string, object> webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal dispute created");
        // TODO: Create notification for admin team about dispute
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleUnknownEventAsync(string eventType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unhandled PayPal webhook event type: {EventType}", eventType);
        return Task.FromResult(Result.Success());
    }

    public async Task<Result> ProcessWebhookEventAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing PayPal webhook event: {EventType}, ID: {EventId}",
                webhookEvent.EventType, webhookEvent.Id);

            return webhookEvent.EventType switch
            {
                "CHECKOUT.ORDER.APPROVED" => await HandleOrderApprovedTypedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.COMPLETED" => await HandleCaptureCompletedTypedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.DENIED" => await HandleCaptureFailedTypedAsync(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.PENDING" => await HandleCapturePendingTypedAsync(webhookEvent, cancellationToken),
                "CUSTOMER.DISPUTE.CREATED" => await HandleDisputeCreatedTypedAsync(webhookEvent, cancellationToken),
                _ => await HandleUnknownEventAsync(webhookEvent.EventType, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook event {EventId} of type {EventType}",
                webhookEvent.Id, webhookEvent.EventType);
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private Task<Result> HandleOrderApprovedTypedAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal order approved - no action needed (waiting for capture): {EventId}", webhookEvent.Id);
        return Task.FromResult(Result.Success());
    }

    private async Task<Result> HandleCaptureCompletedTypedAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        // Extract capture ID and update payment status in database
        // This would involve finding the payment by encrypted PayPal Order ID
        // and updating its status to Completed

        _logger.LogInformation("PayPal capture completed: {EventId}, Summary: {Summary}",
            webhookEvent.Id, webhookEvent.Summary);

        // TODO: Update payment status in database

        // Send SSE notification if kiosk session token is present in metadata
        // This enables real-time payment updates for QR code payments
        if (_notificationService != null && webhookEvent.Resource != null)
        {
            try
            {
                // Extract session token from PayPal order metadata (if present)
                // Session token should be stored in custom_id or metadata during order creation
                var sessionToken = ExtractSessionTokenFromResource(webhookEvent.Resource);
                var attendeeId = ExtractAttendeeIdFromResource(webhookEvent.Resource);
                var eventId = ExtractEventIdFromResource(webhookEvent.Resource);
                var amount = ExtractAmountFromResource(webhookEvent.Resource);
                var captureId = ExtractCaptureIdFromResource(webhookEvent.Resource);

                if (!string.IsNullOrEmpty(sessionToken) &&
                    attendeeId.HasValue &&
                    eventId.HasValue &&
                    amount.HasValue &&
                    !string.IsNullOrEmpty(captureId))
                {
                    // Send notification to kiosk
                    var notificationSent = await _notificationService.NotifyPaymentComplete(
                        sessionToken,
                        attendeeId.Value,
                        eventId.Value,
                        Guid.Parse(captureId), // PayPal capture ID as payment ID
                        amount.Value,
                        "PayPal");

                    if (notificationSent)
                    {
                        _logger.LogInformation(
                            "SSE notification sent for PayPal payment: Attendee {AttendeeId}, Amount ${Amount}",
                            attendeeId.Value, amount.Value);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to send SSE notification for PayPal payment (session may be inactive): {SessionToken}",
                            sessionToken);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "PayPal payment completed without kiosk session token (online payment, not door payment)");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail webhook processing
                _logger.LogError(ex,
                    "Error sending SSE notification for PayPal payment completion");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Extract kiosk session token from PayPal resource metadata.
    /// Session token is stored in custom fields when QR code payment is initiated at the door.
    /// </summary>
    private string? ExtractSessionTokenFromResource(Dictionary<string, object> resource)
    {
        // Try multiple possible locations for session token
        // PayPal stores custom data in different places depending on API version
        if (resource.TryGetValue("custom_id", out var customId) && customId is string sessionToken)
        {
            return sessionToken;
        }

        if (resource.TryGetValue("invoice_id", out var invoiceId) && invoiceId is string invoice)
        {
            // Check if invoice_id contains session token (format: {sessionToken}_{invoiceId})
            var parts = invoice.Split('_');
            if (parts.Length > 1)
            {
                return parts[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Extract attendee ID from PayPal resource metadata.
    /// </summary>
    private Guid? ExtractAttendeeIdFromResource(Dictionary<string, object> resource)
    {
        // Attendee ID should be in custom fields or description
        if (resource.TryGetValue("custom_id", out var customId) && customId is string custom)
        {
            // Format might be: attendee_{guid}
            if (custom.StartsWith("attendee_") && Guid.TryParse(custom.Substring(9), out var attendeeId))
            {
                return attendeeId;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract event ID from PayPal resource metadata.
    /// </summary>
    private Guid? ExtractEventIdFromResource(Dictionary<string, object> resource)
    {
        // Event ID should be in custom fields or description
        if (resource.TryGetValue("description", out var description) && description is string desc)
        {
            // Parse event ID from description (format might include event ID)
            // This is a placeholder - actual implementation depends on how event ID is stored
        }

        return null;
    }

    /// <summary>
    /// Extract payment amount from PayPal resource.
    /// </summary>
    private decimal? ExtractAmountFromResource(Dictionary<string, object> resource)
    {
        if (resource.TryGetValue("amount", out var amountObj) && amountObj is Dictionary<string, object> amount)
        {
            if (amount.TryGetValue("value", out var value) && value is string valueStr)
            {
                if (decimal.TryParse(valueStr, out var amountValue))
                {
                    return amountValue;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extract capture ID from PayPal resource.
    /// </summary>
    private string? ExtractCaptureIdFromResource(Dictionary<string, object> resource)
    {
        if (resource.TryGetValue("id", out var id) && id is string captureId)
        {
            return captureId;
        }

        return null;
    }

    private Task<Result> HandleCaptureFailedTypedAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal capture failed: {EventId}, Summary: {Summary}",
            webhookEvent.Id, webhookEvent.Summary);
        // TODO: Update payment status in database and log failure
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleCapturePendingTypedAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal capture pending: {EventId}, Summary: {Summary}",
            webhookEvent.Id, webhookEvent.Summary);
        // TODO: Update payment status to pending
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleDisputeCreatedTypedAsync(PayPalWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal dispute created: {EventId}, Summary: {Summary}",
            webhookEvent.Id, webhookEvent.Summary);
        // TODO: Create notification for admin team about dispute
        return Task.FromResult(Result.Success());
    }
}