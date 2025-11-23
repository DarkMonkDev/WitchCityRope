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
}
