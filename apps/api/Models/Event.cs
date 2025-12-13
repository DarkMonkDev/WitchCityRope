using System.ComponentModel.DataAnnotations;
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
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief summary for event cards and listings (max ~200 characters recommended)
    /// </summary>
    [MaxLength(200)]
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
    /// Foreign key to Venue table
    /// All events must have an assigned venue for location information
    /// </summary>
    [Required]
    public int VenueId { get; set; }

    /// <summary>
    /// Navigation property to venue
    /// </summary>
    public Venue? Venue { get; set; }

    /// <summary>
    /// Whether the event is published/visible
    /// </summary>
    [Required]
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Whether free RSVPs are enabled for this event
    /// </summary>
    [Required]
    public bool AllowRsvps { get; set; }

    /// <summary>
    /// Whether ticket purchase is mandatory to attend
    /// </summary>
    [Required]
    public bool RequireTicketPurchase { get; set; }

    /// <summary>
    /// Whether only vetted members can attend this event
    /// </summary>
    [Required]
    public bool VettedMembersOnly { get; set; }

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

    // ====================================================================
    // GRANULAR TIMING CONTROLS (Phase 1: Database Schema)
    // ====================================================================

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration opens.
    /// Positive = before event start (e.g., 168 = 7 days before)
    /// Negative = after event start (e.g., -2 = 2 hours after start, max -24)
    /// NULL = no restriction (registration can open any time before event)
    /// </summary>
    public decimal? RegistrationOpenHours { get; set; }

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration closes.
    /// Positive = before event start (e.g., 24 = 1 day before)
    /// Negative = after event start (e.g., -24 = 24 hours after start, max -24)
    /// NULL = no restriction (registration open until event starts)
    /// </summary>
    public decimal? RegistrationCloseHours { get; set; }


    /// <summary>
    /// Hours before/after event start when RSVP/Ticket cancellation closes.
    /// Positive = before event start (e.g., 48 = 2 days before)
    /// Negative = after event start (e.g., -24 = 24 hours after start, max -24)
    /// NULL = no restriction (cancellation available until event starts)
    /// </summary>
    public decimal? CancellationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer signup closes.
    /// Positive = before event start (e.g., 72 = 3 days before)
    /// Negative = after event start (e.g., -24 = 24 hours after start, max -24)
    /// NULL = no restriction (volunteer signup open until event starts)
    /// </summary>
    public decimal? VolunteerRegistrationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer cancellation closes.
    /// Positive = before event start (e.g., 48 = 2 days before)
    /// Negative = after event start (e.g., -24 = 24 hours after start, max -24)
    /// NULL = no restriction (volunteer cancellation available until event starts)
    /// </summary>
    public decimal? VolunteerCancellationCloseHours { get; set; }

    // ====================================================================
    // NAVIGATION PROPERTIES
    // ====================================================================

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
    /// Navigation property to event attendances (RSVPs and tickets)
    /// One-to-many relationship with EventAttendance
    /// </summary>
    public ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();

    // ====================================================================
    // COMPUTED PROPERTIES
    // ====================================================================

    /// <summary>
    /// Gets the current number of confirmed attendees based on CORRECT business logic:
    /// - RSVP-enabled events: CurrentAttendees = RSVPs (everyone must RSVP to attend, tickets are optional donations)
    /// - Ticket-required events: CurrentAttendees = Tickets (only paid tickets, no RSVPs)
    ///
    /// Returns varied capacity states for frontend testing:
    /// - Some events SOLD OUT (100% capacity) - shows green progress bar
    /// - Some events nearly sold out (85-95%) - shows green progress bar
    /// - Some events moderately filled (50-80%) - shows yellow progress bar
    /// - Some events low attendance (<50%) - shows red progress bar
    /// </summary>
    public int GetCurrentAttendeeCount()
    {
        if (AllowRsvps && !RequireTicketPurchase)
        {
            // RSVP-enabled events: Attendees = RSVPs (primary attendance metric)
            return GetCurrentRSVPCount();
        }
        else
        {
            // Ticket-required events: Attendees = Tickets (only paid tickets)
            return GetCurrentTicketCount();
        }
    }

    /// <summary>
    /// Gets the current number of RSVPs for RSVP-enabled events (real implementation)
    /// RSVP-enabled events: Everyone must RSVP to attend (this is the primary attendance count)
    /// Ticket-required events: No RSVPs, returns 0
    /// Requires EventAttendances navigation property to be loaded
    /// </summary>
    public int GetCurrentRSVPCount()
    {
        // Only RSVP-enabled events have RSVPs
        if (!AllowRsvps) return 0;

        // Count active RSVP attendances if navigation property is loaded
        if (EventAttendances?.Any() == true)
        {
            return EventAttendances.Count(ea =>
                ea.AttendanceType == AttendanceType.RSVP &&
                ea.Status == AttendanceStatus.Active);
        }

        // Fallback: If navigation property not loaded, return 0 (service should handle proper loading)
        return 0;
    }

    /// <summary>
    /// Gets the current number of paid ticket registrations (real implementation)
    /// RSVP-enabled events: Optional tickets as donations/support (subset of RSVPs)
    /// Ticket-required events: Required tickets (this is the primary attendance count)
    /// Requires EventAttendances navigation property to be loaded
    /// </summary>
    public int GetCurrentTicketCount()
    {
        // Count active ticket attendances if navigation property is loaded
        if (EventAttendances?.Any() == true)
        {
            return EventAttendances.Count(ea =>
                ea.AttendanceType == AttendanceType.Ticket &&
                ea.Status == AttendanceStatus.Active);
        }

        // Fallback: If navigation property not loaded, return 0 (service should handle proper loading)
        return 0;
    }
}
