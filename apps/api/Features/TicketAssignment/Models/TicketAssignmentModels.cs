using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.TicketAssignment.Models;

/// <summary>
/// Request to assign a ticket to an authorized contact (EP-08, EP-11).
/// Used for both initial assignment and reassignment operations.
/// Business rules: BR-020 (authorization required), BR-022 (PendingAcceptance).
/// </summary>
public class AssignTicketRequest
{
    /// <summary>
    /// The user ID of the person to assign the ticket to.
    /// Must be an authorized contact of the caller (BR-020).
    /// </summary>
    [Required]
    public Guid AssignToUserId { get; set; }
}

/// <summary>
/// Request to accept an assigned ticket or proxy RSVP (EP-09, EP-13).
/// The assignee must personally accept the event waiver (AD-003).
/// Business rules: BR-031 (ToS), BR-032 (waiver activates ticket).
/// </summary>
public class AcceptAssignmentRequest
{
    /// <summary>
    /// Whether the user accepts the event waiver. Must be true to accept.
    /// AD-003: Waiver must be personally accepted by the actual attendee.
    /// </summary>
    [Required]
    public bool EventWaiverAccepted { get; set; }

    /// <summary>
    /// Whether the user accepts the platform Terms of Service.
    /// Only required if the user has not previously accepted platform-wide ToS (BR-031).
    /// </summary>
    public bool TermsOfServiceAccepted { get; set; }
}

/// <summary>
/// Optional request body for declining an assigned ticket (EP-10).
/// The reason is optional free-text for the purchaser's information.
/// </summary>
public class DeclineAssignmentRequest
{
    /// <summary>
    /// Optional reason for declining the assignment.
    /// Visible to the original purchaser.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }
}

/// <summary>
/// Response DTO for ticket assignment operations (assign, accept, decline, reassign).
/// Returns the current state of the attendance record after the operation.
/// </summary>
public class TicketAssignmentDto
{
    /// <summary>
    /// The EventAttendance record ID
    /// </summary>
    public Guid AttendanceId { get; set; }

    /// <summary>
    /// The event this ticket is for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Title of the event
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Name of the ticket type (e.g., "General Admission", "VIP")
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the attendance (e.g., "Active", "PendingAcceptance")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Scene name of the person the ticket is assigned to
    /// </summary>
    public string? AssignedToSceneName { get; set; }

    /// <summary>
    /// User ID of the person the ticket is assigned to
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Scene name of the person who performed the assignment
    /// </summary>
    public string? AssignedBySceneName { get; set; }

    /// <summary>
    /// User ID of the person who performed the assignment
    /// </summary>
    public Guid? AssignedByUserId { get; set; }

    /// <summary>
    /// When the ticket was assigned (UTC)
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// When the attendance record was originally created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for the user dashboard showing pending assignments that need acceptance (EP-16).
/// Shows tickets and RSVPs assigned to the current user that are waiting for waiver acceptance.
/// </summary>
public class PendingAssignmentDto
{
    /// <summary>
    /// The EventAttendance record ID (used for accept/decline actions)
    /// </summary>
    public Guid AttendanceId { get; set; }

    /// <summary>
    /// The event this assignment is for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Title of the event
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the event (UTC)
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Scene name of the person who assigned this ticket/RSVP
    /// </summary>
    public string AssignedBySceneName { get; set; } = string.Empty;

    /// <summary>
    /// Type of attendance: "Ticket" or "RSVP"
    /// </summary>
    public string AttendanceType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the ticket type (empty for RSVPs)
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Names of sessions this ticket covers (empty for single-session events)
    /// </summary>
    public List<string> SessionNames { get; set; } = new();

    /// <summary>
    /// When the ticket/RSVP was assigned (UTC)
    /// </summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// DTO for the purchaser's dashboard showing tickets they purchased and assigned to others (EP-17).
/// Shows current assignment status and whether reassignment is possible.
/// </summary>
public class AssignedTicketStatusDto
{
    /// <summary>
    /// The EventAttendance record ID
    /// </summary>
    public Guid AttendanceId { get; set; }

    /// <summary>
    /// The TicketPurchase record ID (links to the financial transaction)
    /// </summary>
    public Guid TicketPurchaseId { get; set; }

    /// <summary>
    /// The event this ticket is for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Title of the event
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the event (UTC)
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Name of the ticket type
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the attendance (e.g., "Active", "PendingAcceptance")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the person the ticket is assigned to (null if unassigned)
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Scene name of the person the ticket is assigned to (null if unassigned)
    /// </summary>
    public string? AssignedToSceneName { get; set; }

    /// <summary>
    /// Whether the purchaser can reassign this ticket (AD-015: Active + DeclinedAt not null)
    /// </summary>
    public bool CanReassign { get; set; }

    /// <summary>
    /// Whether this ticket is currently unassigned (owned by purchaser, not yet assigned to anyone)
    /// </summary>
    public bool IsUnassigned { get; set; }
}
