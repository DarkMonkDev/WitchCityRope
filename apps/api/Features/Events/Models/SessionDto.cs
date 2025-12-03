namespace WitchCityRope.Api.Features.Events.Models;

// =============================================================================
// DATETIME HANDLING DOCUMENTATION
// =============================================================================
//
// This DTO converts UTC datetimes from the database to display-friendly values.
//
// STORAGE: Session.StartTime and Session.EndTime are stored as TRUE UTC
// TIMEZONE: Application uses America/New_York (Eastern) for all events
//
// CRITICAL: StartDate and EndDate must be derived from LOCAL time, not UTC!
//
// Example of the bug this prevents:
//   User enters: Dec 4, 2025 at 10:25 PM EST
//   Stored as: Dec 5, 2025 at 3:25 AM UTC (crosses midnight)
//   WRONG: StartTime.Date → Dec 5 (UTC date)
//   RIGHT: ConvertToLocal(StartTime).Date → Dec 4 (local date)
//
// See: /docs/guides-setup/datetime-handling-guide.md
// =============================================================================

/// <summary>
/// Data Transfer Object for Session information within events.
/// Used to represent individual sessions that can be part of multi-session events.
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Global timezone for all events (Salem, MA)
    /// </summary>
    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

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
    /// Session start date (LOCAL date for display purposes).
    ///
    /// IMPORTANT: This is derived from StartTime converted to local timezone,
    /// NOT from StartTime.Date (which would give UTC date).
    ///
    /// Example: StartTime of 3:25 AM UTC (Dec 5) = 10:25 PM EST (Dec 4)
    /// This property returns Dec 4, not Dec 5.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Session end date (LOCAL date for display purposes).
    /// Used when session spans multiple days (e.g., Friday night to Saturday morning).
    ///
    /// IMPORTANT: This is derived from EndTime converted to local timezone,
    /// NOT from EndTime.Date (which would give UTC date).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Session start time in UTC.
    /// Frontend converts this to local time for display using utcToLocal().
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time in UTC.
    /// Frontend converts this to local time for display using utcToLocal().
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum capacity for this specific session
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Current number of confirmed registrations for this session.
    /// Frontend expects this field name for consistency with EventDto.RegistrationCount.
    /// </summary>
    public int RegistrationCount { get; set; }

    /// <summary>
    /// Constructor to map from Session entity.
    ///
    /// DATETIME CONVERSION:
    /// - StartTime/EndTime are passed through as-is (UTC)
    /// - StartDate/EndDate are derived by converting UTC to local FIRST, then extracting date
    /// </summary>
    public SessionDto(WitchCityRope.Api.Models.Session session)
    {
        Id = session.Id.ToString();
        SessionIdentifier = session.SessionCode;
        Name = session.Name;

        // Store full UTC datetimes for time display (frontend converts to local)
        StartTime = session.StartTime;
        EndTime = session.EndTime;

        // CRITICAL: Convert UTC to local timezone BEFORE extracting date
        // This prevents the "day shift" bug where late evening times (e.g., 10 PM EST)
        // show as the next day (because 10 PM EST = 3 AM UTC next day)
        var utcStartTime = DateTime.SpecifyKind(session.StartTime, DateTimeKind.Utc);
        var utcEndTime = DateTime.SpecifyKind(session.EndTime, DateTimeKind.Utc);

        var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(utcStartTime, EasternTimeZone);
        var localEndTime = TimeZoneInfo.ConvertTimeFromUtc(utcEndTime, EasternTimeZone);

        // Extract dates from LOCAL times (not UTC!)
        StartDate = localStartTime.Date;
        EndDate = localEndTime.Date;

        Capacity = session.Capacity;
        RegistrationCount = session.CurrentAttendees;
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public SessionDto() { }
}
