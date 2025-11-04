using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Request model for recording a door cash payment for an event ticket.
/// Creates a TicketPurchase record with door payment tracking.
/// </summary>
public class CashPaymentRequest
{
    /// <summary>
    /// The attendee's user ID who is making the payment
    /// </summary>
    [Required]
    public Guid AttendeeId { get; set; }

    /// <summary>
    /// The ticket type being purchased
    /// </summary>
    [Required]
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Amount paid (can be 0.00 for sliding scale)
    /// </summary>
    [Required]
    [Range(0, 10000, ErrorMessage = "Amount must be between $0.00 and $10,000.00")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional notes about the cash payment
    /// (e.g., "Paid $20 cash", "Sliding scale - minimum price")
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Staff member who recorded the payment (from session token)
    /// </summary>
    [Required]
    public Guid RecordedByStaffId { get; set; }
}
