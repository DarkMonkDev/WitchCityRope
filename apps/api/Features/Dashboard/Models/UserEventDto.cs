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

    // NO pricing fields - this is user dashboard, not sales page
    // NO capacity fields - user doesn't need to see event capacity
}
