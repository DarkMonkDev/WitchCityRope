namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Response model for user's events
/// Contains upcoming and recent events for the user
/// </summary>
public class UserEventsResponse
{
    /// <summary>
    /// List of upcoming events the user is registered for
    /// </summary>
    public List<DashboardEventDto> UpcomingEvents { get; set; } = [];
}

/// <summary>
/// Event information for dashboard display
/// Simplified event data focused on user's registration
/// </summary>
public class DashboardEventDto
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
    /// Event start date and time
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date and time
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Type of event (Workshop, SkillShare, Social, Performance)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the instructor/organizer
    /// </summary>
    public string InstructorName { get; set; } = string.Empty;

    /// <summary>
    /// User's registration status for this event (Registered, Waitlisted, etc.)
    /// </summary>
    public string RegistrationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Registration/ticket ID
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Confirmation code for the registration
    /// </summary>
    public string ConfirmationCode { get; set; } = string.Empty;
}