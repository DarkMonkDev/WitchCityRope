namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Used in the Events feature vertical slice.
///
/// CORRECT Business Logic:
/// - Social Events: RegistrationCount = CurrentRSVPs (everyone must RSVP, tickets are optional support)
/// - Class Events: RegistrationCount = CurrentTickets (no RSVPs, only paid tickets)
/// - Tickets are additional support/donations for Social events, not additional attendees
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
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
    /// Total confirmed registrations/attendees
    /// - Social Events: equals CurrentRSVPs (primary attendance metric)
    /// - Class Events: equals CurrentTickets (only paid attendance)
    /// Frontend expects this field for displaying event capacity/availability
    /// </summary>
    public int RegistrationCount { get; set; }

    /// <summary>
    /// Number of free RSVPs (only for Social events, 0 for Class events)
    /// </summary>
    public int CurrentRSVPs { get; set; }

    /// <summary>
    /// Number of paid ticket registrations
    /// - For Class events: equals RegistrationCount (all paid)
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
    /// List of volunteer positions for this event
    /// Includes both event-wide and session-specific volunteer opportunities
    /// </summary>
    public List<VolunteerPositionDto> VolunteerPositions { get; set; } = new List<VolunteerPositionDto>();

    /// <summary>
    /// List of teacher/organizer user IDs
    /// References to ApplicationUser entities who are teaching/organizing this event
    /// </summary>
    public List<string> TeacherIds { get; set; } = new List<string>();
}