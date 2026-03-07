using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Payments.Models.Requests;

/// <summary>
/// Request DTO for variable amount refund and/or ticket cancellation.
/// Supports: partial refund (keep access), full refund (keep access),
/// cancel ticket (with any refund amount including $0), and RSVP removal.
/// Auto-generated as TypeScript interface by NSwag.
/// </summary>
public class VariableRefundRequest
{
    /// <summary>
    /// Refund amount to process (>= 0 and <= remaining refundable amount).
    /// Can be 0 when CancelTicket is true (cancel without refund).
    /// Must be > 0 when CancelTicket is false (refund-only requires an amount).
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "RefundAmount cannot be negative")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Reason for the refund/cancellation (required for audit trail, minimum 10 characters)
    /// </summary>
    [Required]
    [MinLength(10, ErrorMessage = "RefundReason must be at least 10 characters")]
    public string RefundReason { get; set; } = string.Empty;

    /// <summary>
    /// Whether to cancel the ticket (revoke event access by setting EventAttendance to Cancelled).
    /// When true, all Ticket-type EventAttendance records for this TicketPurchase are cancelled.
    /// When false, this is a financial-only refund and the member retains event access.
    /// </summary>
    public bool CancelTicket { get; set; } = false;

    /// <summary>
    /// Whether to also remove the RSVP when cancelling a ticket.
    /// Only meaningful when CancelTicket is true AND the event uses RSVPs (AllowRsvps=true).
    /// When true, the auto-created RSVP from ticket purchase (and any independent RSVP) is cancelled.
    /// When false, the member keeps their free RSVP access even though the ticket is cancelled.
    /// </summary>
    public bool AlsoRemoveRsvp { get; set; } = false;
}
