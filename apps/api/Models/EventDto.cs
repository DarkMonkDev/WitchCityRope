using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Simple model for Step 1 of vertical slice proof-of-concept.
/// 
/// CORRECT Business Logic:
/// - Social Events: CurrentAttendees = CurrentRSVPs (everyone must RSVP, tickets are optional support)
/// - Class Events: CurrentAttendees = CurrentTickets (no RSVPs, only paid tickets)
/// - Tickets are additional support/donations for Social events, not additional attendees
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Capacity { get; set; }

    /// <summary>
    /// Whether the event is published and visible to the public
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Total confirmed attendees
    /// - Social Events: equals CurrentRSVPs (primary attendance metric)
    /// - Class Events: equals CurrentTickets (only paid attendance)
    /// </summary>
    public int CurrentAttendees { get; set; }
    
    /// <summary>
    /// Number of free RSVPs (only for Social events, 0 for Class events)
    /// </summary>
    public int CurrentRSVPs { get; set; }
    
    /// <summary>
    /// Number of paid ticket registrations
    /// - For Class events: equals CurrentAttendees (all paid)
    /// - For Social events: optional paid tickets in addition to free RSVPs
    /// </summary>
    public int CurrentTickets { get; set; }

    /// <summary>
    /// List of sessions within this event
    /// Empty for single-session events, populated for multi-session events
    /// </summary>
    public List<SessionDto> Sessions { get; set; } = new List<SessionDto>();

    /// <summary>
    /// List of ticket types available for this event
    /// Includes pricing, availability, and session associations
    /// </summary>
    public List<TicketTypeDto> TicketTypes { get; set; } = new List<TicketTypeDto>();

    /// <summary>
    /// List of teacher/organizer user IDs
    /// References to ApplicationUser entities who are teaching/organizing this event
    /// </summary>
    public List<string> TeacherIds { get; set; } = new List<string>();
}