namespace WitchCityRope.Api.Features.Payments.Models.PayPal;

/// <summary>
/// PayPal refund response model
/// </summary>
public class PayPalRefundResponse
{
    /// <summary>
    /// PayPal refund ID
    /// </summary>
    public string RefundId { get; set; } = string.Empty;

    /// <summary>
    /// Refund status (COMPLETED, PENDING, CANCELLED)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount refunded
    /// </summary>
    public PayPalAmount Amount { get; set; } = new();

    /// <summary>
    /// Refund creation timestamp
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Refund update timestamp
    /// </summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Invoice ID associated with refund
    /// </summary>
    public string? InvoiceId { get; set; }

    /// <summary>
    /// Custom note for refund
    /// </summary>
    public string? NoteToPayer { get; set; }

    /// <summary>
    /// Capture ID associated with this refund
    /// </summary>
    public string CaptureId { get; set; } = string.Empty;

    /// <summary>
    /// Refund completion timestamp
    /// </summary>
    public DateTime RefundTime { get; set; }
}