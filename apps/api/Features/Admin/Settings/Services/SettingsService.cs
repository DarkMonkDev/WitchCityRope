using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;

namespace WitchCityRope.Api.Features.Admin.Settings.Services;

/// <summary>
/// Service for managing application-wide settings stored in database
/// Provides get and update operations for configuration values that survive deployments
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.Settings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(cancellationToken);

        return setting?.Value;
    }

    public async Task<bool> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var setting = await _context.Settings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null)
        {
            _logger.LogWarning("Attempted to update non-existent setting: {Key}", key);
            return false;
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated setting {Key} to {Value}", key, value);

        return true;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.Settings
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return settings;
    }

    public async Task<(bool Success, string Error)> UpdateMultipleSettingsAsync(
        Dictionary<string, string> updates,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = updates.Keys.ToList();
            var settings = await _context.Settings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync(cancellationToken);

            if (settings.Count != updates.Count)
            {
                var missingKeys = keys.Except(settings.Select(s => s.Key)).ToList();
                return (false, $"Settings not found: {string.Join(", ", missingKeys)}");
            }

            var now = DateTime.UtcNow;
            foreach (var setting in settings)
            {
                setting.Value = updates[setting.Key];
                setting.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated {Count} settings", settings.Count);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return (false, "Failed to update settings");
        }
    }
}
