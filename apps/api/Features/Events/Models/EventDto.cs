namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Used in the Events feature vertical slice.
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
}