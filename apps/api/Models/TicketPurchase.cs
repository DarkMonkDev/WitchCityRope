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
    public string PaymentStatus { get; set; } = "Pending";

    /// <summary>
    /// Payment method used (e.g., "PayPal", "Stripe", "Cash")
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// External payment reference/transaction ID
    /// </summary>
    public string PaymentReference { get; set; } = string.Empty;

    /// <summary>
    /// Special notes about the purchase (e.g., accessibility needs, dietary restrictions)
    /// For door cash payments, may include cash tracking information.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Staff member who recorded a door purchase (cash or QR code).
    /// NULL for online purchases.
    /// Used for audit trail and accountability.
    /// </summary>
    public Guid? RecordedByStaffId { get; set; }

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
    /// </summary>
    public bool IsPaymentCompleted => PaymentStatus == "Completed" || PaymentStatus == "Confirmed";

    /// <summary>
    /// Gets whether this purchase represents an RSVP (free ticket)
    /// </summary>
    public bool IsRSVP => TotalPrice == 0;

    /// <summary>
    /// Gets whether this was a door purchase (processed at event)
    /// </summary>
    public bool IsDoorPurchase => RecordedByStaffId.HasValue;
}