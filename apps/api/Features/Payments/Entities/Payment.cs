using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Entities;

/// <summary>
/// Main payment entity supporting sliding scale pricing and comprehensive audit trails
/// </summary>
public class Payment
{
    /// <summary>
    /// Payment unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the event registration being paid for
    /// </summary>
    public Guid EventRegistrationId { get; set; }
    
    /// <summary>
    /// User who made the payment
    /// </summary>
    public Guid UserId { get; set; }
    
    #region Money Value Object Storage (PostgreSQL optimized)
    
    /// <summary>
    /// Payment amount value (stored as decimal for precision)
    /// </summary>
    public decimal AmountValue { get; set; }
    
    /// <summary>
    /// Payment currency (3-letter ISO code)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    #endregion
    
    #region Sliding Scale Pricing
    
    /// <summary>
    /// Sliding scale discount percentage applied (0-75%)
    /// </summary>
    public decimal SlidingScalePercentage { get; set; }
    
    #endregion
    
    #region Payment Processing
    
    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    /// <summary>
    /// Type of payment method used
    /// </summary>
    public PaymentMethodType PaymentMethodType { get; set; }
    
    /// <summary>
    /// Encrypted PayPal Order ID (for PCI compliance)
    /// </summary>
    public string? EncryptedPayPalOrderId { get; set; }
    
    /// <summary>
    /// Encrypted PayPal Payer ID (for PCI compliance)
    /// </summary>
    public string? EncryptedPayPalPayerId { get; set; }
    
    /// <summary>
    /// Venmo username if payment was made via Venmo (nullable)
    /// </summary>
    public string? VenmoUsername { get; set; }
    
    #endregion
    
    #region Timestamps
    
    /// <summary>
    /// When the payment was successfully processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// When the payment record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the payment record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    #endregion
    
    #region Refund Information
    
    /// <summary>
    /// Refund amount value (if refunded)
    /// </summary>
    public decimal? RefundAmountValue { get; set; }
    
    /// <summary>
    /// Refund currency (should match original currency)
    /// </summary>
    public string? RefundCurrency { get; set; }
    
    /// <summary>
    /// When the refund was processed
    /// </summary>
    public DateTime? RefundedAt { get; set; }
    
    /// <summary>
    /// Encrypted PayPal Refund ID (for PCI compliance)
    /// </summary>
    public string? EncryptedPayPalRefundId { get; set; }
    
    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? RefundReason { get; set; }
    
    /// <summary>
    /// User who processed the refund
    /// </summary>
    public Guid? RefundedByUserId { get; set; }
    
    #endregion
    
    #region Flexible Metadata
    
    /// <summary>
    /// Additional payment metadata (stored as JSONB in PostgreSQL)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    #endregion
    
    #region Navigation Properties
    
    /// <summary>
    /// Navigation property to user who made payment
    /// </summary>
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Navigation property to user who processed refund
    /// </summary>
    public ApplicationUser? RefundedByUser { get; set; }
    
    /// <summary>
    /// Navigation property to audit logs
    /// </summary>
    public List<PaymentAuditLog> AuditLogs { get; set; } = new();
    
    /// <summary>
    /// Navigation property to refund records
    /// </summary>
    public List<PaymentRefund> Refunds { get; set; } = new();
    
    /// <summary>
    /// Navigation property to failure records
    /// </summary>
    public List<PaymentFailure> Failures { get; set; } = new();
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Get the Money value object for the payment amount
    /// </summary>
    public ValueObjects.Money GetAmount()
    {
        return ValueObjects.Money.Create(AmountValue, Currency);
    }
    
    /// <summary>
    /// Set the payment amount from a Money value object
    /// </summary>
    public void SetAmount(ValueObjects.Money money)
    {
        AmountValue = money.Amount;
        Currency = money.Currency;
    }
    
    /// <summary>
    /// Get the refund Money value object (if refunded)
    /// </summary>
    public ValueObjects.Money? GetRefundAmount()
    {
        if (RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency))
        {
            return ValueObjects.Money.Create(RefundAmountValue.Value, RefundCurrency);
        }
        return null;
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
    /// Check if payment is eligible for refund
    /// </summary>
    public bool IsRefundEligible()
    {
        return Status == PaymentStatus.Completed && ProcessedAt.HasValue;
    }
    
    /// <summary>
    /// Check if payment has been refunded (fully or partially)
    /// </summary>
    public bool IsRefunded()
    {
        return Status == PaymentStatus.Refunded || Status == PaymentStatus.PartiallyRefunded;
    }
    
    /// <summary>
    /// Update timestamps when entity is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    #endregion
}