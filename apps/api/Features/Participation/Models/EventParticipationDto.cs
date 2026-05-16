using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// DTO for admin view of event participations.
/// Includes refund history so admins can see past refunds before issuing new ones.
/// Auto-generated as TypeScript interface by NSwag.
/// </summary>
public class EventParticipationDto
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's scene name
    /// </summary>
    public string UserSceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Type of attendance (RSVP or Ticket)
    /// </summary>
    public AttendanceType ParticipationType { get; set; }

    /// <summary>
    /// Current status of attendance
    /// </summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// When user registered for the event
    /// </summary>
    public DateTime ParticipationDate { get; set; }

    /// <summary>
    /// Optional notes from participant
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this participation can be cancelled
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Flexible metadata for additional information (e.g., purchase amount)
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether the attendee has checked in to the event
    /// Determined by presence of EventAttendees record with RegistrationStatus = 'checked-in' or 'confirmed'
    /// </summary>
    public bool HasCheckedIn { get; set; }

    /// <summary>
    /// When the attendee checked in (if applicable)
    /// Populated from EventAttendees.UpdatedAt when RegistrationStatus changed to 'checked-in'
    /// </summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>
    /// Ticket type name (for ticket purchases only)
    /// Null for RSVPs
    /// </summary>
    public string? TicketTypeName { get; set; }

    /// <summary>
    /// Comma-separated list of session names this ticket/RSVP applies to
    /// "All Sessions" if no specific sessions or event has no sessions
    /// </summary>
    public string SessionNames { get; set; } = "All Sessions";

    /// <summary>
    /// Amount paid for this participation (from TicketPurchase.TotalPrice)
    /// Null for free RSVPs (no associated TicketPurchase)
    /// Decimal value for tickets (includes donations on social events)
    /// </summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>
    /// TicketPurchase ID for refund processing
    /// Null for free RSVPs (no associated TicketPurchase)
    /// Required by frontend to call /api/payments/transactions/{transactionId}/refund endpoint
    /// </summary>
    public Guid? TicketId { get; set; }

    /// <summary>
    /// Payment method used for this purchase (PayPal, Venmo, Cash)
    /// Null for free RSVPs
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// List of session names the attendee has checked into
    /// Empty list if not checked into any sessions
    /// </summary>
    public List<string> CheckedInSessions { get; set; } = new List<string>();

    /// <summary>
    /// History of all refunds issued against this ticket purchase.
    /// Empty for RSVPs (no associated TicketPurchase) or tickets with no refunds.
    /// Ordered by ProcessedAt descending (most recent first).
    /// </summary>
    public List<RefundHistoryDto> RefundHistory { get; set; } = new List<RefundHistoryDto>();

    /// <summary>
    /// Total amount already refunded for this ticket purchase.
    /// Sum of all Completed refunds. 0 for RSVPs or tickets with no refunds.
    /// </summary>
    public decimal TotalRefunded { get; set; }

    /// <summary>
    /// Remaining refundable amount (AmountPaid - TotalRefunded).
    /// 0 for RSVPs, fully refunded tickets, or free tickets.
    /// </summary>
    public decimal RemainingRefundable { get; set; }

    /// <summary>
    /// True when the linked ticket purchase is in AwaitingManualRefund state — the ticket
    /// was cancelled but the credit-card refund has not yet been processed by an admin.
    /// False for RSVPs (no linked purchase) and for any other payment status.
    /// </summary>
    public bool RefundOwed { get; set; }
}