using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Events.Interfaces;

namespace WitchCityRope.Api.Features.Events.Services;

/// <summary>
/// Service for timezone and registration cutoff time management
/// Uses database settings for configuration (survives Docker rebuilds)
/// Caches timezone info to minimize database queries
/// </summary>
public class TimeZoneService : ITimeZoneService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TimeZoneService> _logger;

    // Cache timezone to avoid repeated database lookups
    private TimeZoneInfo? _cachedTimeZone;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private const int CACHE_MINUTES = 60;

    public TimeZoneService(ISettingsService settingsService, ILogger<TimeZoneService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<TimeZoneInfo> GetEventTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cachedTimeZone != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedTimeZone;
        }

        var timeZoneId = await _settingsService.GetSettingAsync("EventTimeZone", cancellationToken);

        if (string.IsNullOrEmpty(timeZoneId))
        {
            _logger.LogWarning("EventTimeZone setting not found, defaulting to America/New_York");
            timeZoneId = "America/New_York";
        }

        try
        {
            _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CACHE_MINUTES);
            return _cachedTimeZone;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogError(ex, "Invalid timezone ID: {TimeZoneId}, falling back to America/New_York", timeZoneId);
            _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CACHE_MINUTES);
            return _cachedTimeZone;
        }
    }

    public async Task<int> GetPreStartBufferMinutesAsync(CancellationToken cancellationToken = default)
    {
        var bufferValue = await _settingsService.GetSettingAsync("PreStartBufferMinutes", cancellationToken);

        if (string.IsNullOrEmpty(bufferValue) || !int.TryParse(bufferValue, out int bufferMinutes))
        {
            _logger.LogWarning("PreStartBufferMinutes setting invalid or missing, defaulting to 0");
            return 0;
        }

        return bufferMinutes;
    }

    public async Task<bool> IsRegistrationOpenAsync(
        DateTime eventStartDateUtc,
        CancellationToken cancellationToken = default)
    {
        var bufferMinutes = await GetPreStartBufferMinutesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var cutoffTime = eventStartDateUtc.AddMinutes(-bufferMinutes);

        var isOpen = now < cutoffTime;

        if (!isOpen)
        {
            _logger.LogInformation(
                "Registration closed: Current time {Now} is past cutoff {Cutoff} (buffer: {Buffer} min)",
                now, cutoffTime, bufferMinutes);
        }

        return isOpen;
    }

    public async Task<DateTimeOffset> ConvertToEventTimeAsync(
        DateTime utcDateTime,
        CancellationToken cancellationToken = default)
    {
        var timeZone = await GetEventTimeZoneAsync(cancellationToken);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }

    /// <summary>
    /// Determines if a specific action is allowed based on event timing configuration
    /// NULL timing fields = no restriction (backward compatible)
    /// Positive hours = before event start (e.g., 168 = 7 days before)
    /// Negative hours = after event start (e.g., -24 = 24 hours after, max -24)
    /// </summary>
    public async Task<bool> IsActionAllowedAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        EventActionType actionType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate event entity is not null
            if (eventEntity == null)
            {
                _logger.LogError("Event entity is null in timing validation for action {ActionType}", actionType);
                return false; // Deny access if event is null (defensive)
            }

            // Get current time and event start time in UTC
            var currentTimeUtc = DateTime.UtcNow;
            var eventStartTimeUtc = eventEntity.StartDate;

            // Calculate hours until event start (negative = event already started)
            var hoursUntilStart = (eventStartTimeUtc - currentTimeUtc).TotalHours;

            // Determine which timing fields to check based on action type
            decimal? openHours = null;
            decimal? closeHours = null;

            switch (actionType)
            {
                case EventActionType.GetRsvp:
                case EventActionType.GetTicket:
                    openHours = eventEntity.RegistrationOpenHours;
                    closeHours = eventEntity.RegistrationCloseHours;
                    break;

                case EventActionType.CancelRsvp:
                case EventActionType.CancelTicket:
                    openHours = eventEntity.CancellationOpenHours;
                    closeHours = eventEntity.CancellationCloseHours;
                    break;

                case EventActionType.GetVolunteer:
                    // Volunteer signup only has close hours (always open until close)
                    closeHours = eventEntity.VolunteerRegistrationCloseHours;
                    break;

                case EventActionType.CancelVolunteer:
                    // Volunteer cancel only has close hours (always open until close)
                    closeHours = eventEntity.VolunteerCancellationCloseHours;
                    break;

                default:
                    _logger.LogError("Unknown action type {ActionType} in timing validation", actionType);
                    throw new ArgumentException($"Unknown action type: {actionType}", nameof(actionType));
            }

            // Check if action is within allowed timing window
            // NULL means no restriction (backward compatible with existing events)

            // CRITICAL: For volunteer actions, only closeHours applies (no openHours)
            // For RSVP/Ticket actions, both openHours and closeHours may apply
            // NULL timing fields mean "no restriction" for that specific check

            // For volunteer actions: If closeHours is NULL, allow (no restriction)
            // For RSVP/ticket actions: If BOTH openHours and closeHours are NULL, allow (no restrictions)
            bool hasAnyRestriction = openHours.HasValue || closeHours.HasValue;

            if (!hasAnyRestriction)
            {
                _logger.LogDebug(
                    "Action {ActionType} allowed - no timing restrictions configured (openHours={OpenHours}, closeHours={CloseHours})",
                    actionType, openHours, closeHours);
                return true; // No restrictions = always allowed
            }

            // Check open hours (if configured)
            // NULL openHours means "always open" (no early restriction)
            if (openHours.HasValue)
            {
                // Positive hours = before event, negative = after event
                // If hoursUntilStart > openHours, event hasn't opened yet
                if (hoursUntilStart > (double)openHours.Value)
                {
                    _logger.LogInformation(
                        "Action {ActionType} not allowed - too early. Hours until start: {HoursUntilStart}, Opens at: {OpenHours}",
                        actionType, hoursUntilStart, openHours.Value);
                    return false; // Too early
                }
            }

            // Check close hours (if configured)
            // NULL closeHours means "never closes" (no late restriction)
            if (closeHours.HasValue)
            {
                // Positive hours = before event, negative = after event
                // If hoursUntilStart < closeHours, window has closed
                // Use small epsilon (0.01 hours = 36 seconds) for floating-point comparison tolerance
                const double EPSILON = 0.01;
                if (hoursUntilStart < (double)closeHours.Value - EPSILON)
                {
                    _logger.LogInformation(
                        "Action {ActionType} not allowed - too late. Hours until start: {HoursUntilStart}, Closes at: {CloseHours}",
                        actionType, hoursUntilStart, closeHours.Value);
                    return false; // Too late
                }
            }

            // If we made it here, action is allowed
            _logger.LogDebug(
                "Action {ActionType} allowed. Hours until start: {HoursUntilStart}, Open: {OpenHours}, Close: {CloseHours}",
                actionType, hoursUntilStart, openHours, closeHours);

            return true;
        }
        catch (Exception ex)
        {
            // CRITICAL: Catch ANY exception and log it
            // Return false (deny access) instead of throwing (which causes HTTP 500)
            _logger.LogError(ex,
                "Exception in timing validation for action {ActionType}. Event ID: {EventId}, Start Date: {StartDate}",
                actionType, eventEntity?.Id, eventEntity?.StartDate);
            return false; // Deny access on error (fail-safe)
        }
    }
}
