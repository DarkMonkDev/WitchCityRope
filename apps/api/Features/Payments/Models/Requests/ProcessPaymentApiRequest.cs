using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Payments.Models.Requests;

/// <summary>
/// API request model for processing payments with sliding scale pricing
/// </summary>
public class ProcessPaymentApiRequest
{
    /// <summary>
    /// Event registration ID for the payment
    /// </summary>
    public Guid EventRegistrationId { get; set; }

    /// <summary>
    /// Original event fee amount (before sliding scale discount)
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Currency code (default: USD)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Sliding scale discount percentage (0-75%)
    /// Honor-based system - no verification required
    /// </summary>
    public decimal SlidingScalePercentage { get; set; }

    /// <summary>
    /// Type of payment method being used
    /// </summary>
    public PaymentMethodType PaymentMethodType { get; set; }

    /// <summary>
    /// PayPal return URL after successful payment
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// PayPal cancel URL if payment is cancelled
    /// </summary>
    public string? CancelUrl { get; set; }
}

/// <summary>
/// API request model for processing refunds
/// </summary>
public class ProcessRefundApiRequest
{
    /// <summary>
    /// Payment ID to refund
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Refund amount (can be partial)
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Currency for the refund (should match original payment)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Reason for the refund (minimum 10 characters required)
    /// </summary>
    public string RefundReason { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata for the refund
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}