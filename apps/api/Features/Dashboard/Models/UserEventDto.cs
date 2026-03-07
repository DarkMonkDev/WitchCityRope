namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// User's registered event information for dashboard display
/// CRITICAL: This is NOT PublicEventDto - different fields for dashboard context
/// This is for the user's own events dashboard, NOT public sales page
/// </summary>
public class UserEventDto
{
    /// <summary>
    /// Event ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the event
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Registration status: "RSVP Confirmed", "Ticket Purchased", "Attended"
    /// </summary>
    public string RegistrationStatus { get; set; } = string.Empty;

    /// <summary>
    /// True if this is a social event (affects registration type)
    /// </summary>
    public bool IsSocialEvent { get; set; }

    /// <summary>
    /// True if user has purchased a ticket for this event
    /// </summary>
    public bool HasTicket { get; set; }

    /// <summary>
    /// True if the event date is in the past
    /// </summary>
    public bool IsPastEvent => EndDate < DateTime.UtcNow;

    /// <summary>
    /// Sessions the user is registered for (via their ticket purchase)
    /// Empty list for events with no sessions or if user hasn't purchased a ticket
    /// </summary>
    public List<UserSessionDto> RegisteredSessions { get; set; } = new();

    /// <summary>
    /// ALL sessions for this event, regardless of user's ticket purchases.
    /// Used by the frontend to display session dates/times even for RSVP-only users.
    /// RegisteredSessions only contains sessions with tickets, so RSVP users would see
    /// "Date and Time coming soon" without this field.
    /// </summary>
    public List<UserSessionDto> EventSessions { get; set; } = new();

    /// <summary>
    /// Count of additional sessions available that user hasn't purchased
    /// 0 for single-session events or if user has purchased all sessions
    /// </summary>
    public int AdditionalSessionsAvailable { get; set; }

    /// <summary>
    /// True if the event has any ticket types available (paid or donation/free).
    /// Used to show "Purchase Ticket" button for RSVP users who haven't bought a ticket yet.
    /// Avoids the frontend needing to fetch full event details just to check this.
    /// </summary>
    public bool HasAvailableTickets { get; set; }

    /// <summary>
    /// Tickets the user has purchased for this event.
    /// Includes ticket type name and associated session name (for multi-session events).
    /// Used to display a "Tickets" summary box on the dashboard event card.
    /// </summary>
    public List<UserTicketDto> Tickets { get; set; } = new();

    // NO pricing fields - this is user dashboard, not sales page
    // NO capacity fields - user doesn't need to see event capacity
}
