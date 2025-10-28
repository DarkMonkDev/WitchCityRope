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
    /// Gets the pre-start buffer time in minutes from settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Minutes before event start to close registration</returns>
    Task<int> GetPreStartBufferMinutesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if registration is still open for an event based on start time and buffer
    /// </summary>
    /// <param name="eventStartDateUtc">Event start time in UTC</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registration is open, false if past cutoff</returns>
    Task<bool> IsRegistrationOpenAsync(DateTime eventStartDateUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts UTC datetime to event timezone
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DateTimeOffset in event timezone</returns>
    Task<DateTimeOffset> ConvertToEventTimeAsync(DateTime utcDateTime, CancellationToken cancellationToken = default);
}
