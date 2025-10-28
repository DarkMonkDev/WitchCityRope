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
    /// Updates a single setting value
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="value">New value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if key not found</returns>
    Task<bool> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings as a dictionary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of setting key-value pairs</returns>
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple settings in a single operation
    /// </summary>
    /// <param name="updates">Dictionary of setting keys and new values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if failed</returns>
    Task<(bool Success, string Error)> UpdateMultipleSettingsAsync(
        Dictionary<string, string> updates,
        CancellationToken cancellationToken = default);
}
