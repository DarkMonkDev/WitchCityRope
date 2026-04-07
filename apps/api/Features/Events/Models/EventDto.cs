namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Used in the Events feature vertical slice.
///
/// CORRECT Business Logic:
/// - RSVP-enabled events: RegistrationCount = CurrentRSVPs (everyone must RSVP, tickets are optional support)
/// - Ticket-required events: RegistrationCount = CurrentTickets (no RSVPs, only paid tickets)
/// - Tickets are additional support/donations for RSVP-enabled events, not additional attendees
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Policies { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Venue ID reference - references the Venues table
    /// All events must have an assigned venue for location information
    /// </summary>
    public int VenueId { get; set; }

    /// <summary>
    /// Public venue location (city, state) for privacy-aware display
    /// Shown to all users before RSVP/ticket purchase
    /// Example: "Salem, MA"
    /// </summary>
    public string? VenueLocation { get; set; }

    public int Capacity { get; set; }

    /// <summary>
    /// Whether the event is published and visible to the public
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Whether free RSVPs are enabled for this event
    /// </summary>
    public bool AllowRsvps { get; set; }

    /// <summary>
    /// Whether ticket purchase is mandatory to attend
    /// </summary>
    public bool RequireTicketPurchase { get; set; }

    /// <summary>
    /// Whether only vetted members can attend this event
    /// </summary>
    public bool VettedMembersOnly { get; set; }

    /// <summary>
    /// Total confirmed registrations/attendees
    /// - RSVP-enabled events: equals CurrentRSVPs (primary attendance metric)
    /// - Ticket-required events: equals CurrentTickets (only paid attendance)
    /// Frontend expects this field for displaying event capacity/availability
    /// </summary>
    public int RegistrationCount { get; set; }

    /// <summary>
    /// Number of free RSVPs (only for RSVP-enabled events, 0 for ticket-required events)
    /// </summary>
    public int CurrentRSVPs { get; set; }

    /// <summary>
    /// Number of paid ticket registrations
    /// - For ticket-required events: equals RegistrationCount (all paid)
    /// - For RSVP-enabled events: optional paid tickets in addition to free RSVPs
    /// </summary>
    public int CurrentTickets { get; set; }

    /// <summary>
    /// Number of available spots to display on event cards and public pages.
    ///
    /// For multi-session events: the highest remaining capacity across all
    /// future sessions (past sessions excluded). This reflects how many new
    /// attendees the event's most available session can still accept.
    ///
    /// For single-session or no-session events: event Capacity minus current
    /// attendee count (same as the historical computation).
    ///
    /// Computed by Event.GetAvailableSpotsDisplay() — single source of truth.
    /// Frontend should use this value instead of computing capacity - registrationCount.
    /// </summary>
    public int AvailableSpotsDisplay { get; set; }

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
    public List<EventVolunteerPositionDto> VolunteerPositions { get; set; } = new List<EventVolunteerPositionDto>();

    /// <summary>
    /// List of teacher/organizer user IDs
    /// References to ApplicationUser entities who are teaching/organizing this event
    /// </summary>
    public List<string> TeacherIds { get; set; } = new List<string>();

    // ====================================================================
    // GRANULAR TIMING CONTROLS
    // ====================================================================

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration opens.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can register any time before event).
    /// </summary>
    public decimal? RegistrationOpenHours { get; set; }

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can register until event starts).
    /// </summary>
    public decimal? RegistrationCloseHours { get; set; }


    /// <summary>
    /// Hours before/after event start when RSVP/Ticket cancellation closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can cancel until event starts).
    /// </summary>
    public decimal? CancellationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer signup closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can signup until event starts).
    /// </summary>
    public decimal? VolunteerRegistrationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer cancellation closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can cancel until event starts).
    /// </summary>
    public decimal? VolunteerCancellationCloseHours { get; set; }

    /// <summary>
    /// Default maximum number of tickets or RSVPs a single person can have for this event.
    /// Applies as the cumulative per-person cap across all transactions and proxy RSVPs.
    /// NULL = no per-person limit (only event capacity constrains).
    /// </summary>
    public int? DefaultMaxTicketOrRsvpPerPerson { get; set; }
}