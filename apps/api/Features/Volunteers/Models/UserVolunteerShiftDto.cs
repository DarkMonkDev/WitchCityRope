namespace WitchCityRope.Api.Features.Volunteers.Models;

/// <summary>
/// User's volunteer shift information for dashboard display
/// </summary>
public class UserVolunteerShiftDto
{
    /// <summary>
    /// Volunteer signup ID
    /// </summary>
    public Guid SignupId { get; set; }

    /// <summary>
    /// Event title
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event location
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Event date
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Volunteer position title
    /// </summary>
    public string PositionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Session name (if event has multiple sessions)
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// Shift start time (session start time if available)
    /// </summary>
    public DateTime? ShiftStartTime { get; set; }

    /// <summary>
    /// Shift end time (session end time if available)
    /// </summary>
    public DateTime? ShiftEndTime { get; set; }
}
