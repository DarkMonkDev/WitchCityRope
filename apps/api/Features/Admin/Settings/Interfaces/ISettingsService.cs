namespace WitchCityRope.Api.Features.Admin.Settings.Interfaces;

/// <summary>
/// Service for managing application-wide settings stored in database
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a single setting value by key
    /// </summary>
    /// <param name="key">Setting key (e.g., "EventTimeZone")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setting value if found, null otherwise</returns>
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings as a dictionary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of setting key-value pairs</returns>
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates multiple settings in a single operation.
    /// If a setting key doesn't exist, it is created. If it already exists, its value is updated.
    /// </summary>
    /// <param name="settings">Dictionary of setting keys and values to create or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if failed</returns>
    Task<(bool Success, string Error)> UpsertMultipleSettingsAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}
