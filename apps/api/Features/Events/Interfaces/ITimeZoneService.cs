namespace WitchCityRope.Api.Features.Events.Interfaces;

/// <summary>
/// Service for timezone and registration cutoff time management
/// Handles event timezone conversions and calculates registration/cancellation deadlines
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// Gets the configured event timezone from settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TimeZoneInfo for the configured event timezone</returns>
    Task<TimeZoneInfo> GetEventTimeZoneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts UTC datetime to event timezone
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DateTimeOffset in event timezone</returns>
    Task<DateTimeOffset> ConvertToEventTimeAsync(DateTime utcDateTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a specific action is allowed based on event timing configuration
    /// </summary>
    /// <param name="eventEntity">Event to check timing for</param>
    /// <param name="actionType">Type of action user is attempting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if action is allowed, false if outside timing window</returns>
    Task<bool> IsActionAllowedAsync(WitchCityRope.Api.Models.Event eventEntity, EventActionType actionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the reference session for a ticket type (first future session from ticket's sessions)
    /// Used for session-based timing calculations for multi-session tickets
    /// </summary>
    /// <param name="ticketType">The ticket type to get reference session for</param>
    /// <param name="allSessions">All sessions from the event</param>
    /// <returns>The first future session from ticket's sessions, or null if all passed</returns>
    WitchCityRope.Api.Models.Session? GetReferenceSessionForTicketType(
        WitchCityRope.Api.Models.TicketType ticketType,
        IEnumerable<WitchCityRope.Api.Models.Session> allSessions);

    /// <summary>
    /// Get the earliest future session from a list of sessions
    /// </summary>
    /// <param name="sessions">Sessions to check</param>
    /// <returns>The earliest session where StartTime > UtcNow, or null if none</returns>
    WitchCityRope.Api.Models.Session? GetEarliestFutureSession(
        IEnumerable<WitchCityRope.Api.Models.Session> sessions);

    /// <summary>
    /// Get the earliest session from a list of sessions (regardless of past/future)
    /// Used for multi-session ticket timing - timing is based on the EARLIEST session
    /// </summary>
    /// <param name="sessions">Sessions to check</param>
    /// <returns>The earliest session by StartTime, or null if none</returns>
    WitchCityRope.Api.Models.Session? GetEarliestSession(
        IEnumerable<WitchCityRope.Api.Models.Session> sessions);

    /// <summary>
    /// Check if an action is allowed for a specific session based on timing windows
    /// Session-based timing calculation (replaces event-based timing)
    /// </summary>
    /// <param name="session">The session to check timing against (null = not allowed)</param>
    /// <param name="openHours">Hours before session when action opens (null = no restriction)</param>
    /// <param name="closeHours">Hours before session when action closes (null = no restriction)</param>
    /// <returns>True if action is allowed, false otherwise</returns>
    bool IsActionAllowedForSession(
        WitchCityRope.Api.Models.Session? session,
        decimal? openHours,
        decimal? closeHours);
}
