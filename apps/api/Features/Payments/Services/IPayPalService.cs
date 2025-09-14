using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// PayPal payment processing service interface
/// Handles all interactions with PayPal APIs including Venmo support
/// </summary>
public interface IPayPalService
{
    /// <summary>
    /// Create a PayPal order for processing payment
    /// </summary>
    Task<Result<PayPalOrderResponse>> CreateOrderAsync(
        ValueObjects.Money amount,
        Guid customerId,
        int slidingScalePercentage,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture a PayPal order after approval
    /// </summary>
    Task<Result<PayPalCaptureResponse>> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get PayPal order details by ID
    /// </summary>
    Task<Result<PayPalOrderResponse>> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create refund for a captured payment
    /// </summary>
    Task<Result<PayPalRefundResponse>> RefundCaptureAsync(
        string captureId,
        ValueObjects.Money refundAmount,
        string reason,
        string? noteToPayer = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refund details by ID
    /// </summary>
    Task<Result<PayPalRefundResponse>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate PayPal webhook signature
    /// </summary>
    Result<Dictionary<string, object>> ValidateWebhookSignature(
        string payload,
        string signature,
        string webhookId);

    /// <summary>
    /// Process PayPal webhook event
    /// </summary>
    Task<Result> ProcessWebhookEventAsync(
        Dictionary<string, object> webhookEvent,
        CancellationToken cancellationToken = default);
}