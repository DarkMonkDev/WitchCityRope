using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Core.Entities;
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

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.Settings
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return settings;
    }

    public async Task<(bool Success, string Error)> UpsertMultipleSettingsAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = settings.Keys.ToList();
            var existingSettings = await _context.Settings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            // Update existing settings
            foreach (var existing in existingSettings)
            {
                existing.Value = settings[existing.Key];
                existing.UpdatedAt = now;
            }

            // Create new settings for keys that don't exist yet
            var existingKeys = existingSettings.Select(s => s.Key).ToHashSet();
            var newSettings = settings
                .Where(kvp => !existingKeys.Contains(kvp.Key))
                .Select(kvp => new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = kvp.Key.StartsWith("EmailTestData:")
                        ? $"Email template test data: {kvp.Key.Replace("EmailTestData:", "")}"
                        : $"Application setting: {kvp.Key}",
                    CreatedAt = now,
                    UpdatedAt = now
                })
                .ToList();

            if (newSettings.Count > 0)
            {
                _context.Settings.AddRange(newSettings);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Upserted {Count} settings ({New} new, {Updated} updated)",
                settings.Count, newSettings.Count, existingSettings.Count);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting settings");
            return (false, "Failed to upsert settings");
        }
    }
}
