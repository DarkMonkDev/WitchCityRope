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
}
