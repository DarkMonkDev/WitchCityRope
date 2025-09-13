namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for Session information within events.
/// Used to represent individual sessions that can be part of multi-session events.
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Session identifier code (e.g., "S1", "S2", "Day1", "Day2")
    /// </summary>
    public string SessionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Name of the session (e.g., "Morning Session", "Day 1", "Afternoon Workshop")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Session date (date portion only for display purposes)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Session start time in UTC
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time in UTC
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum capacity for this specific session
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Current number of confirmed attendees for this session
    /// </summary>
    public int RegisteredCount { get; set; }

    /// <summary>
    /// Constructor to map from Session entity
    /// </summary>
    public SessionDto(WitchCityRope.Api.Models.Session session)
    {
        Id = session.Id.ToString();
        SessionIdentifier = session.SessionCode;
        Name = session.Name;
        Date = session.StartTime.Date;
        StartTime = session.StartTime;
        EndTime = session.EndTime;
        Capacity = session.Capacity;
        RegisteredCount = session.CurrentAttendees;
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public SessionDto() { }
}