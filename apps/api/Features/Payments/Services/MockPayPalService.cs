using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Mock PayPal service for testing and CI/CD environments
/// Returns predictable test data without making external API calls
/// </summary>
public class MockPayPalService : IPayPalService
{
    private readonly ILogger<MockPayPalService> _logger;
    private readonly Dictionary<string, PayPalOrderResponse> _orders = new();
    private readonly Dictionary<string, PayPalCaptureResponse> _captures = new();
    private readonly Dictionary<string, PayPalRefundResponse> _refunds = new();

    public MockPayPalService(ILogger<MockPayPalService> logger)
    {
        _logger = logger;
        _logger.LogWarning("🚨 MOCK PayPal Service Active - Not for production use!");
    }

    public Task<Result<PayPalOrderResponse>> CreateOrderAsync(
        Money amount,
        Guid customerId,
        int slidingScalePercentage,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var orderId = $"MOCK-ORDER-{Guid.NewGuid():N}".ToUpper();
        
        var order = new PayPalOrderResponse
        {
            OrderId = orderId,
            Status = "CREATED",
            CreateTime = DateTime.UtcNow,
            Links = new List<PayPalLink>
            {
                new() { Href = $"/mock/paypal/approve?order={orderId}", Rel = "approve", Method = "GET" },
                new() { Href = $"/api/payments/orders/{orderId}", Rel = "self", Method = "GET" },
                new() { Href = $"/api/payments/orders/{orderId}/capture", Rel = "capture", Method = "POST" }
            }
        };

        _orders[orderId] = order;
        
        _logger.LogInformation("Mock PayPal order created: {OrderId} for amount {Amount}", 
            orderId, amount.Amount);

        return Task.FromResult(Result<PayPalOrderResponse>.Success(order));
    }

    public Task<Result<PayPalCaptureResponse>> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (!_orders.ContainsKey(orderId))
        {
            return Task.FromResult(Result<PayPalCaptureResponse>.Failure(
                "Order not found in mock service"));
        }

        var captureId = $"MOCK-CAPTURE-{Guid.NewGuid():N}".ToUpper();
        
        var capture = new PayPalCaptureResponse
        {
            CaptureId = captureId,
            OrderId = orderId,
            Status = "COMPLETED",
            Amount = new PayPalAmount 
            { 
                Value = "50.00", 
                CurrencyCode = "USD" 
            },
            CaptureTime = DateTime.UtcNow,
            PayerEmail = "test@example.com",
            PayerName = "Test User"
        };

        _captures[captureId] = capture;
        _orders[orderId].Status = "COMPLETED";
        
        _logger.LogInformation("Mock PayPal order captured: {OrderId} -> {CaptureId}", 
            orderId, captureId);

        return Task.FromResult(Result<PayPalCaptureResponse>.Success(capture));
    }

    public Task<Result<PayPalOrderResponse>> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult(Result<PayPalOrderResponse>.Success(order));
        }

        return Task.FromResult(Result<PayPalOrderResponse>.Failure(
            $"Mock order {orderId} not found"));
    }

    public Task<Result<PayPalRefundResponse>> RefundCaptureAsync(
        string captureId,
        Money refundAmount,
        string reason,
        string? noteToPayer = null,
        CancellationToken cancellationToken = default)
    {
        if (!_captures.ContainsKey(captureId))
        {
            return Task.FromResult(Result<PayPalRefundResponse>.Failure(
                "Capture not found in mock service"));
        }

        var refundId = $"MOCK-REFUND-{Guid.NewGuid():N}".ToUpper();
        
        var refund = new PayPalRefundResponse
        {
            RefundId = refundId,
            CaptureId = captureId,
            Status = "COMPLETED",
            Amount = new PayPalAmount 
            { 
                Value = refundAmount.ToPayPalAmount(), 
                CurrencyCode = refundAmount.Currency 
            },
            RefundTime = DateTime.UtcNow,
            Reason = reason,
            NoteToPayer = noteToPayer
        };

        _refunds[refundId] = refund;
        
        _logger.LogInformation("Mock PayPal refund created: {RefundId} for capture {CaptureId}", 
            refundId, captureId);

        return Task.FromResult(Result<PayPalRefundResponse>.Success(refund));
    }

    public Task<Result<PayPalRefundResponse>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default)
    {
        if (_refunds.TryGetValue(refundId, out var refund))
        {
            return Task.FromResult(Result<PayPalRefundResponse>.Success(refund));
        }

        return Task.FromResult(Result<PayPalRefundResponse>.Failure(
            $"Mock refund {refundId} not found"));
    }

    public Result<Dictionary<string, object>> ValidateWebhookSignature(
        string payload,
        string signature,
        string webhookId)
    {
        // Always return valid in mock mode
        _logger.LogInformation("Mock webhook signature validation - always returns valid");
        
        var webhookData = new Dictionary<string, object>
        {
            ["verified"] = true,
            ["event_type"] = "PAYMENT.CAPTURE.COMPLETED",
            ["resource"] = new Dictionary<string, object>
            {
                ["id"] = $"MOCK-CAPTURE-{Guid.NewGuid():N}".ToUpper(),
                ["status"] = "COMPLETED",
                ["amount"] = new { value = "50.00", currency_code = "USD" }
            }
        };

        return Result<Dictionary<string, object>>.Success(webhookData);
    }

    public Task<Result> ProcessWebhookEventAsync(
        Dictionary<string, object> webhookEvent,
        CancellationToken cancellationToken = default)
    {
        var eventType = webhookEvent.GetValueOrDefault("event_type")?.ToString() ?? "UNKNOWN";
        
        _logger.LogInformation("Mock webhook event processed: {EventType}", eventType);
        
        // Always return success in mock mode
        return Task.FromResult(Result.Success());
    }
}