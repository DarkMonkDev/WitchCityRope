using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Entities;

/// <summary>
/// Detailed refund tracking entity for comprehensive refund management
/// </summary>
public class PaymentRefund
{
    /// <summary>
    /// Refund unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the original payment being refunded
    /// </summary>
    public Guid OriginalPaymentId { get; set; }

    #region Refund Amount (Money Value Object Storage)

    /// <summary>
    /// Refund amount value (can be partial refund)
    /// </summary>
    public decimal RefundAmountValue { get; set; }

    /// <summary>
    /// Refund currency (should match original payment)
    /// </summary>
    public string RefundCurrency { get; set; } = "USD";

    #endregion

    #region Refund Details

    /// <summary>
    /// Reason for the refund (required for audit trail)
    /// </summary>
    public string RefundReason { get; set; } = string.Empty;

    /// <summary>
    /// Current refund processing status
    /// </summary>
    public RefundStatus RefundStatus { get; set; } = RefundStatus.Processing;

    /// <summary>
    /// Encrypted PayPal Refund ID (for PCI compliance)
    /// </summary>
    public string? EncryptedPayPalRefundId { get; set; }

    #endregion

    #region Administrative Tracking

    /// <summary>
    /// User who processed the refund (admin or teacher)
    /// </summary>
    public Guid ProcessedByUserId { get; set; }

    /// <summary>
    /// When the refund was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the refund record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Flexible Metadata

    /// <summary>
    /// Additional refund metadata (stored as JSONB in PostgreSQL)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the original payment
    /// </summary>
    public Payment? OriginalPayment { get; set; }

    /// <summary>
    /// Navigation property to the user who processed the refund
    /// </summary>
    public ApplicationUser? ProcessedByUser { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the Money value object for the refund amount
    /// </summary>
    public ValueObjects.Money GetRefundAmount()
    {
        return ValueObjects.Money.Create(RefundAmountValue, RefundCurrency);
    }

    /// <summary>
    /// Set the refund amount from a Money value object
    /// </summary>
    public void SetRefundAmount(ValueObjects.Money money)
    {
        RefundAmountValue = money.Amount;
        RefundCurrency = money.Currency;
    }

    /// <summary>
    /// Check if refund is completed successfully
    /// </summary>
    public bool IsCompleted()
    {
        return RefundStatus == RefundStatus.Completed;
    }

    /// <summary>
    /// Check if refund is still processing
    /// </summary>
    public bool IsProcessing()
    {
        return RefundStatus == RefundStatus.Processing;
    }

    /// <summary>
    /// Check if refund has failed
    /// </summary>
    public bool HasFailed()
    {
        return RefundStatus == RefundStatus.Failed;
    }

    /// <summary>
    /// Mark refund as completed
    /// </summary>
    public void MarkCompleted()
    {
        RefundStatus = RefundStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark refund as failed with reason
    /// </summary>
    public void MarkFailed(string failureReason)
    {
        RefundStatus = RefundStatus.Failed;
        if (Metadata.ContainsKey("failure_reason"))
        {
            Metadata["failure_reason"] = failureReason;
        }
        else
        {
            Metadata.Add("failure_reason", failureReason);
        }
        ProcessedAt = DateTime.UtcNow;
    }

    #endregion
}