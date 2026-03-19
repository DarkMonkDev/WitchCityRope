using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Enhanced DTO for user's participation status in an event
/// Provides detailed status with boolean flags and nested objects for RSVP and tickets
/// Matches frontend expectations for ParticipationCard component
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class EnhancedParticipationStatusDto
{
    /// <summary>
    /// Whether user has an active RSVP for this event
    /// </summary>
    public bool HasRSVP { get; set; }

    /// <summary>
    /// Whether user has an active ticket for this event
    /// </summary>
    public bool HasTicket { get; set; }

    /// <summary>
    /// Whether user can create a new RSVP for this event
    /// False if already has RSVP or event is at capacity
    /// </summary>
    public bool CanRSVP { get; set; }

    /// <summary>
    /// Whether user can purchase a ticket for this event
    /// False if already has ticket or event is at capacity
    /// </summary>
    public bool CanPurchaseTicket { get; set; }

    /// <summary>
    /// Whether user can cancel their RSVP based on event timing rules
    /// Determined by event's CancellationCloseHours
    /// </summary>
    public bool CanCancelRSVP { get; set; }

    /// <summary>
    /// Whether user can cancel their ticket based on event timing rules
    /// Determined by event's CancellationCloseHours
    /// </summary>
    public bool CanCancelTicket { get; set; }

    /// <summary>
    /// Message explaining when ticket purchase is available (e.g., "Sales open Dec 15", "Sales closed")
    /// Only populated when CanPurchaseTicket is false due to timing restrictions
    /// Null when CanPurchaseTicket is true or when user already has a ticket
    /// </summary>
    public string? TicketPurchaseMessage { get; set; }

    /// <summary>
    /// RSVP details if user has an active RSVP, null otherwise
    /// </summary>
    public RsvpDetailsDto? Rsvp { get; set; }

    /// <summary>
    /// Ticket details if user has an active ticket, null otherwise
    /// </summary>
    public TicketDetailsDto? Ticket { get; set; }

    /// <summary>
    /// Event capacity information
    /// </summary>
    public CapacityInfoDto? Capacity { get; set; }

    /// <summary>
    /// Session IDs the user already has tickets for
    /// Used to prevent duplicate session purchases and show partial ownership
    /// </summary>
    public List<Guid> OwnedSessionIds { get; set; } = new();

    /// <summary>
    /// Maps TicketPurchaseId to its associated SessionIds
    /// Used for selective ticket cancellation - allows frontend to group sessions by ticket purchase
    /// Key: TicketPurchase.Id, Value: List of SessionIds owned by that purchase
    /// </summary>
    public Dictionary<Guid, List<Guid>> TicketPurchaseSessionMap { get; set; } = new();

    /// <summary>
    /// Detailed ticket purchase information including ticket type name
    /// Used for displaying ticket names in cancel mode
    /// Key: TicketPurchase.Id, Value: TicketPurchaseInfoDto with name and session IDs
    /// </summary>
    public Dictionary<Guid, TicketPurchaseInfoDto> TicketPurchases { get; set; } = new();

    /// <summary>
    /// Whether user can purchase tickets for additional sessions
    /// (has available sessions they don't own, within timing window)
    /// </summary>
    public bool CanPurchaseAdditionalSessions { get; set; }

    /// <summary>
    /// Whether user can buy tickets for other people (authorized contacts).
    /// True when: user already has a ticket AND there are purchasable ticket types
    /// with available capacity within timing windows. Distinct from CanPurchaseTicket
    /// (which checks if user can buy for themselves).
    /// </summary>
    public bool CanBuyForOthers { get; set; }

    /// <summary>
    /// Per-session availability information (for multi-session events)
    /// </summary>
    public List<SessionAvailabilityDto> SessionAvailability { get; set; } = new();
}

/// <summary>
/// Details about a user's RSVP
/// </summary>
public class RsvpDetailsDto
{
    /// <summary>
    /// RSVP ID (EventParticipation ID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Status of the RSVP (Active, Cancelled, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the RSVP was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the RSVP was cancelled (if cancelled)
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (if cancelled)
    /// </summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// Optional notes from participant
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Details about a user's ticket purchase
/// </summary>
public class TicketDetailsDto
{
    /// <summary>
    /// Ticket ID (EventParticipation ID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Status of the ticket (Active, Cancelled, Refunded, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid for the ticket
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment status (Completed, Pending, Refunded)
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// When the ticket was purchased
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the ticket was cancelled/refunded (if applicable)
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// Reason for cancellation/refund (if applicable)
    /// </summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// Optional notes from purchaser
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Information about a ticket purchase including ticket type name and cancellation eligibility
/// Used for displaying proper ticket names in cancel mode and per-purchase cancellation control
/// </summary>
public class TicketPurchaseInfoDto
{
    /// <summary>
    /// Name of the ticket type (e.g., "Day 1 Only", "Full Weekend Pass")
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Session IDs included in this ticket purchase
    /// </summary>
    public List<Guid> SessionIds { get; set; } = new();

    /// <summary>
    /// Total price paid for this ticket purchase
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Whether this specific ticket purchase can be cancelled based on its sessions' timing
    /// True if the reference session (earliest session in this ticket) is within the cancellation window
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Message explaining why cancellation is not available (e.g., "Cancellation window closed")
    /// Null when CanCancel is true
    /// </summary>
    public string? CancellationMessage { get; set; }

    /// <summary>
    /// Scene name of the person who assigned this ticket to the current user.
    /// Populated when the current user received this ticket from someone else
    /// (i.e., the EventAttendance has AssignedByUserId set and the current user
    /// is the attendee, not the purchaser). Null for self-purchased tickets.
    /// </summary>
    public string? AssignedBySceneName { get; set; }

    /// <summary>
    /// Whether this ticket was purchased for someone else (not the current user).
    /// When true, the ticket may be assigned to an authorized contact or unassigned ("assign later").
    /// The purchaser can cancel this ticket if it hasn't been accepted yet.
    /// </summary>
    public bool IsForOther { get; set; }

    /// <summary>
    /// Scene name of the person this ticket is assigned to (null if unassigned or own ticket).
    /// Only populated when IsForOther=true and the ticket has been assigned.
    /// </summary>
    public string? AssigneeSceneName { get; set; }

    /// <summary>
    /// Status of the assignment: "Unassigned", "PendingAcceptance", "Active" (accepted).
    /// Only populated when IsForOther=true.
    /// - Unassigned: No EventAttendance, ticket can be cancelled
    /// - PendingAcceptance: Assigned but not accepted, ticket can be cancelled
    /// - Active: Accepted by assignee, ticket CANNOT be cancelled by purchaser
    /// </summary>
    public string? AssigneeStatus { get; set; }
}

/// <summary>
/// Event capacity information
/// </summary>
public class CapacityInfoDto
{
    /// <summary>
    /// Current number of active participants (RSVPs + tickets)
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// Maximum capacity of the event
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Number of available spots (Total - Current)
    /// </summary>
    public int Available { get; set; }
}

/// <summary>
/// Per-session availability information
/// </summary>
public class SessionAvailabilityDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Session identifier (for matching with ticket types)
    /// </summary>
    public string SessionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Session name/description
    /// </summary>
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Session start time (UTC)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time (UTC)
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Number of tickets sold for this session
    /// </summary>
    public int SoldCount { get; set; }

    /// <summary>
    /// Number of spots available for this session
    /// </summary>
    public int AvailableCount { get; set; }

    /// <summary>
    /// Maximum capacity for this session
    /// </summary>
    public int Capacity { get; set; }
}
