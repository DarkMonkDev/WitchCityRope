using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Event entity for PostgreSQL database
/// Matches existing database table structure
/// </summary>
public class Event
{
    /// <summary>
    /// Unique identifier using UUID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Event title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief summary for event cards and listings (max ~200 characters recommended)
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Full detailed event description with complete information
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Event policies, safety guidelines, and rules (optional)
    /// Contains important information about event policies, safety requirements, etc.
    /// </summary>
    public string? Policies { get; set; }

    /// <summary>
    /// Event start date/time in UTC
    /// CRITICAL: Must be UTC for PostgreSQL TIMESTAMPTZ compatibility
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date/time in UTC
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Maximum number of attendees
    /// </summary>
    [Required]
    public int Capacity { get; set; }

    /// <summary>
    /// Type of event (workshop, social, performance, etc.)
    /// </summary>
    [Required]
    public EventType EventType { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event is published/visible
    /// </summary>
    [Required]
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to sessions
    /// </summary>
    public ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>
    /// Navigation property to ticket types
    /// </summary>
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    /// <summary>
    /// Navigation property to volunteer positions
    /// </summary>
    public ICollection<VolunteerPosition> VolunteerPositions { get; set; } = new List<VolunteerPosition>();

    /// <summary>
    /// Navigation property to organizers/teachers
    /// Many-to-many relationship with ApplicationUser
    /// </summary>
    public ICollection<ApplicationUser> Organizers { get; set; } = new List<ApplicationUser>();

    /// <summary>
    /// Navigation property to event participations (RSVPs and tickets)
    /// One-to-many relationship with EventParticipation
    /// </summary>
    public ICollection<EventParticipation> EventParticipations { get; set; } = new List<EventParticipation>();

    /// <summary>
    /// Gets the current number of confirmed attendees based on CORRECT business logic:
    /// - Social Events: CurrentAttendees = RSVPs (everyone must RSVP to attend, tickets are optional donations)
    /// - Class Events: CurrentAttendees = Tickets (only paid tickets, no RSVPs)
    /// 
    /// Returns varied capacity states for frontend testing:
    /// - Some events SOLD OUT (100% capacity) - shows green progress bar
    /// - Some events nearly sold out (85-95%) - shows green progress bar  
    /// - Some events moderately filled (50-80%) - shows yellow progress bar
    /// - Some events low attendance (<50%) - shows red progress bar
    /// </summary>
    public int GetCurrentAttendeeCount()
    {
        if (EventType == Enums.EventType.Social)
        {
            // Social events: Attendees = RSVPs (primary attendance metric)
            return GetCurrentRSVPCount();
        }
        else // Class
        {
            // Class events: Attendees = Tickets (only paid tickets)
            return GetCurrentTicketCount();
        }
    }

    /// <summary>
    /// Gets the current number of RSVPs for Social events (real implementation)
    /// Social Events: Everyone must RSVP to attend (this is the primary attendance count)
    /// Class Events: No RSVPs, returns 0
    /// Requires EventParticipations navigation property to be loaded
    /// </summary>
    public int GetCurrentRSVPCount()
    {
        // Only Social events have RSVPs
        if (EventType != Enums.EventType.Social) return 0;

        // Count active RSVP participations if navigation property is loaded
        if (EventParticipations?.Any() == true)
        {
            return EventParticipations.Count(ep =>
                ep.ParticipationType == ParticipationType.RSVP &&
                ep.Status == ParticipationStatus.Active);
        }

        // Fallback: If navigation property not loaded, return 0 (service should handle proper loading)
        return 0;
    }

    /// <summary>
    /// Gets the current number of paid ticket registrations (real implementation)
    /// Social Events: Optional tickets as donations/support (subset of RSVPs)
    /// Class Events: Required tickets (this is the primary attendance count)
    /// Requires EventParticipations navigation property to be loaded
    /// </summary>
    public int GetCurrentTicketCount()
    {
        // Count active ticket participations if navigation property is loaded
        if (EventParticipations?.Any() == true)
        {
            return EventParticipations.Count(ep =>
                ep.ParticipationType == ParticipationType.Ticket &&
                ep.Status == ParticipationStatus.Active);
        }

        // Fallback: If navigation property not loaded, return 0 (service should handle proper loading)
        return 0;
    }
}