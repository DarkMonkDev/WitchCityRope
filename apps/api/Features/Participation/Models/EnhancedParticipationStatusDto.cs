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
