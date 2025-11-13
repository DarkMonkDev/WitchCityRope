using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Represents a user's attendance record for an event.
///
/// BUSINESS PURPOSE:
/// Tracks WHO is attending WHAT event and in what capacity (RSVP or Ticket).
/// Used for capacity calculations and attendance roster.
///
/// TWO TYPES OF ATTENDANCE:
/// 1. RSVP (AttendanceType.RSVP): Free attendance for social events
/// 2. Ticket (AttendanceType.Ticket): Paid attendance for class events or donation tickets
///
/// CAPACITY RULES:
/// - Social events: Capacity based on RSVP count (free attendees)
/// - Class events: Capacity based on Ticket count (paid attendees)
/// - Auto-RSVP: When user buys ticket for social event, RSVP is created automatically
///
/// BUSINESS RULE: One Active attendance per user per event
/// - Users can have multiple Cancelled/Refunded for history
/// - Database constraint enforces this rule
///
/// RELATIONSHIP TO PAYMENT:
/// - TicketPurchase = Financial transaction record (for refunds)
/// - EventAttendance = Attendance record (for capacity)
/// - Linked via TicketPurchaseId foreign key
///
/// FUTURE: Multi-ticket purchases will create multiple EventAttendance records
/// from one TicketPurchase (when Quantity > 1).
/// </summary>
public class EventAttendance
{
    /// <summary>
    /// Unique identifier for the attendance record
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event the user is attending
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// User who is attending
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of attendance: RSVP (free) or Ticket (paid)
    /// </summary>
    [Required]
    public AttendanceType AttendanceType { get; set; }

    /// <summary>
    /// Current status: Active, Cancelled, or Refunded
    /// </summary>
    [Required]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Active;

    /// <summary>
    /// Link to payment transaction (NULL for RSVP, NOT NULL for Ticket).
    /// Used for refund processing and to identify which ticket type was purchased.
    ///
    /// FUTURE: One TicketPurchase can create multiple EventAttendance records
    /// when quantity > 1 (multi-ticket purchases for guests).
    /// </summary>
    public Guid? TicketPurchaseId { get; set; }

    /// <summary>
    /// When the attendance was created (UTC)
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the attendance was cancelled (UTC)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Optional notes from the participant
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Flexible metadata for additional information
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    [Required]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// User who created this attendance
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated this attendance
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// When the attendance was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the user has accepted the Event Waiver
    /// Required for legal compliance when attending events
    /// </summary>
    public bool EventWaiverAccepted { get; set; } = false;

    /// <summary>
    /// When the user accepted the Event Waiver (UTC)
    /// NULL if not yet accepted
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    public DateTime? EventWaiverAcceptedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Navigation property to the event
    /// </summary>
    public Event Event { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user attending
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who created this record
    /// </summary>
    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property to the user who last updated this record
    /// </summary>
    public ApplicationUser? UpdatedByUser { get; set; }

    /// <summary>
    /// Navigation property to payment transaction
    /// </summary>
    public TicketPurchase? TicketPurchase { get; set; }

    /// <summary>
    /// Navigation property to attendance history records
    /// </summary>
    public ICollection<AttendanceHistory> History { get; set; } = new List<AttendanceHistory>();

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public EventAttendance()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new attendance
    /// </summary>
    public EventAttendance(Guid eventId, Guid userId, AttendanceType type) : this()
    {
        EventId = eventId;
        UserId = userId;
        AttendanceType = type;
    }

    /// <summary>
    /// Determines if this attendance can be cancelled
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status == AttendanceStatus.Active;
    }

    /// <summary>
    /// Cancels the attendance with reason
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException("Attendance cannot be cancelled in current status");

        Status = AttendanceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
