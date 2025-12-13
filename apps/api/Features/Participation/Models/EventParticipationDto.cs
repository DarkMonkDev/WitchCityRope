using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// DTO for admin view of event participations
/// Auto-generated as TypeScript interface by NSwag
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
    /// Required by frontend to call /api/admin/refunds/{ticketId} endpoint
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
}