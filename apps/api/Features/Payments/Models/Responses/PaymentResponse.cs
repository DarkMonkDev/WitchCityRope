using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Payments.Models.Responses;

/// <summary>
/// API response model for payment information
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Payment unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event registration ID this payment is for
    /// </summary>
    public Guid EventRegistrationId { get; set; }

    /// <summary>
    /// User who made the payment
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Final payment amount (after sliding scale discount)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Formatted amount for display (e.g., "$25.00")
    /// </summary>
    public string DisplayAmount { get; set; } = string.Empty;

    /// <summary>
    /// Original amount before sliding scale discount
    /// </summary>
    public decimal? OriginalAmount { get; set; }

    /// <summary>
    /// Sliding scale discount percentage applied
    /// </summary>
    public decimal SlidingScalePercentage { get; set; }

    /// <summary>
    /// Discount amount saved through sliding scale
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Human-readable status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Type of payment method used
    /// </summary>
    public PaymentMethodType PaymentMethodType { get; set; }

    /// <summary>
    /// When the payment was successfully processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// When the payment record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Refund information (if payment has been refunded)
    /// </summary>
    public RefundInfoResponse? RefundInfo { get; set; }

    /// <summary>
    /// Client secret for Stripe PaymentIntent (for frontend processing)
    /// Only returned for pending payments
    /// </summary>
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Refund information for payment response
/// </summary>
public class RefundInfoResponse
{
    /// <summary>
    /// Total amount refunded
    /// </summary>
    public decimal RefundedAmount { get; set; }

    /// <summary>
    /// Currency of the refund
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Formatted refund amount for display
    /// </summary>
    public string DisplayAmount { get; set; } = string.Empty;

    /// <summary>
    /// When the refund was processed
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Number of refund transactions
    /// </summary>
    public int RefundCount { get; set; }

    /// <summary>
    /// Whether this is a partial or full refund
    /// </summary>
    public bool IsPartialRefund { get; set; }
}

/// <summary>
/// API response model for refund information
/// </summary>
public class RefundResponse
{
    /// <summary>
    /// Refund unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original payment ID that was refunded
    /// </summary>
    public Guid OriginalPaymentId { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Formatted refund amount for display
    /// </summary>
    public string DisplayAmount { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string RefundReason { get; set; } = string.Empty;

    /// <summary>
    /// Current refund processing status
    /// </summary>
    public Models.RefundStatus RefundStatus { get; set; }

    /// <summary>
    /// Human-readable status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// User who processed the refund
    /// </summary>
    public Guid ProcessedByUserId { get; set; }

    /// <summary>
    /// Name/scene name of user who processed refund
    /// </summary>
    public string ProcessedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// When the refund was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// When the refund record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payment status response for quick status checks
/// </summary>
public class PaymentStatusResponse
{
    /// <summary>
    /// Payment ID
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Event registration ID
    /// </summary>
    public Guid EventRegistrationId { get; set; }

    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Human-readable status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Whether payment is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Whether payment has been refunded
    /// </summary>
    public bool IsRefunded { get; set; }

    /// <summary>
    /// Final payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// When payment was completed (if applicable)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}