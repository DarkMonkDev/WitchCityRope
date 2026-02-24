using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Models;

/// <summary>
/// TicketPurchase entity representing a user's purchase of tickets
/// Supports multiple quantities and tracks payment status
/// </summary>
public class TicketPurchase
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the ticket type purchased
    /// </summary>
    [Required]
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Reference to the user who made the purchase
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// When the purchase was made
    /// </summary>
    [Required]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of tickets purchased
    /// </summary>
    [Required]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Total price paid (may be different from ticket price due to sliding scale)
    /// </summary>
    [Required]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Payment processing status
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    /// <summary>
    /// Payment method used (e.g., "PayPal", "Stripe", "Cash")
    /// </summary>
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// External payment reference/transaction ID
    /// </summary>
    [MaxLength(50)]
    public string PaymentReference { get; set; } = string.Empty;

    /// <summary>
    /// Special notes about the purchase (e.g., accessibility needs, dietary restrictions)
    /// For door cash payments, may include cash tracking information.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    // ========================================
    // PayPal Integration Fields
    // ========================================

    /// <summary>
    /// Encrypted PayPal Order ID (from order creation)
    /// NULL for non-PayPal payments (cash, Venmo, RSVP)
    /// CRITICAL: Encrypted for PCI compliance
    /// </summary>
    public string? EncryptedPayPalOrderId { get; set; }

    /// <summary>
    /// Encrypted PayPal Payer ID (payer information)
    /// NULL for non-PayPal payments
    /// CRITICAL: Encrypted for PCI compliance
    /// </summary>
    public string? EncryptedPayPalPayerId { get; set; }

    /// <summary>
    /// Encrypted PayPal Capture ID from purchase_units[0].payments.captures[0].id
    /// REQUIRED for processing PayPal refunds (different from Order ID!)
    /// NULL for non-PayPal payments
    /// CRITICAL: Encrypted for PCI compliance
    /// </summary>
    public string? EncryptedPayPalCaptureId { get; set; }

    // ========================================
    // Credit Card (Authorize.net) Integration Fields
    // ========================================

    /// <summary>
    /// Last four digits of the credit card used for payment
    /// NULL for non-credit-card payments
    /// </summary>
    public string? CreditCardLastFour { get; set; }

    /// <summary>
    /// Credit card type/brand (e.g., "Visa", "Mastercard", "Amex")
    /// NULL for non-credit-card payments
    /// </summary>
    public string? CreditCardType { get; set; }

    /// <summary>
    /// Encrypted Authorize.net transaction ID
    /// NULL for non-credit-card payments
    /// CRITICAL: Encrypted for PCI compliance
    /// </summary>
    public string? EncryptedAuthNetTransactionId { get; set; }

    // ========================================
    // PayPal Webhook Optimization Fields
    // ========================================

    /// <summary>
    /// SHA256 hash of the PayPal Order ID for indexed webhook lookups
    /// Allows finding TicketPurchase by OrderId without decrypting every row
    /// NULL for non-PayPal payments
    /// </summary>
    public string? PayPalOrderIdHash { get; set; }

    /// <summary>
    /// Sliding scale discount percentage applied (0-75%)
    /// 0 = full price, 25 = 25% discount, 75 = 75% discount (max)
    /// Defaults to 0 (no discount) for non-sliding scale tickets
    /// </summary>
    public decimal SlidingScalePercentage { get; set; } = 0.00m;

    /// <summary>
    /// Idempotency key for this transaction
    /// Prevents duplicate payment processing if request is retried
    /// Format: "WCR-{guid}" for consistency
    /// NULL for older purchases created before idempotency support
    /// </summary>
    [MaxLength(50)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// When the payment was successfully processed (UTC)
    /// NULL if payment is still pending or failed
    /// For completed payments, this is when PayPal confirmed the capture
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Staff member who recorded a door purchase (cash or QR code).
    /// NULL for online purchases.
    /// Used for audit trail and accountability.
    /// </summary>
    public Guid? RecordedByStaffId { get; set; }

    /// <summary>
    /// Indicates whether the user has accepted the Event Waiver
    /// Required for legal compliance when purchasing tickets
    /// </summary>
    public bool EventWaiverAccepted { get; set; } = false;

    /// <summary>
    /// When the user accepted the Event Waiver (UTC)
    /// NULL if not yet accepted
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    public DateTime? EventWaiverAcceptedAt { get; set; }

    /// <summary>
    /// Navigation property to ticket type
    /// </summary>
    public TicketType? TicketType { get; set; }

    /// <summary>
    /// Navigation property to user (purchaser)
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Navigation property to staff member who recorded door purchase
    /// NULL for online purchases
    /// </summary>
    public ApplicationUser? RecordedByStaff { get; set; }

    /// <summary>
    /// Navigation property to event attendances created from this purchase
    /// FUTURE: Will support one-to-many when Quantity > 1
    /// </summary>
    public ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets whether the payment has been completed
    /// Includes "PartiallyRefunded" because partially refunded payments are still valid completed payments
    /// that can receive additional refunds (there's still money to refund!)
    /// </summary>
    // Case-insensitive comparison prevents silent failures if payment gateway returns different casing
    public bool IsPaymentCompleted =>
        string.Equals(PaymentStatus, "Completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(PaymentStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(PaymentStatus, "PartiallyRefunded", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this purchase represents an RSVP (free ticket)
    /// </summary>
    public bool IsRSVP => TotalPrice == 0;

    /// <summary>
    /// Gets whether this was a door purchase (processed at event)
    /// </summary>
    public bool IsDoorPurchase => RecordedByStaffId.HasValue;
}
