using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Core.Entities;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of application settings.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating system configuration settings.
/// </summary>
public class SettingsSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsSeeder> _logger;

    public SettingsSeeder(
        ApplicationDbContext context,
        ILogger<SettingsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds application settings including timezone configuration and buffer periods.
    /// Idempotent operation - skips if settings already exist.
    ///
    /// Settings created:
    /// - EventTimeZone: IANA timezone ID for event scheduling (America/New_York)
    /// - PreStartBufferMinutes: Minutes before event start to close ticket sales and RSVP registrations
    /// </summary>
    public async Task SeedSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Settings.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Settings already exist, skipping seed");
            return;
        }

        var now = DateTime.UtcNow;
        var settings = new List<Setting>
        {
            new Setting
            {
                Id = Guid.NewGuid(),
                Key = "EventTimeZone",
                Value = "America/New_York",
                Description = "IANA timezone ID for event scheduling (Eastern Time)",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Setting
            {
                Id = Guid.NewGuid(),
                Key = "PreStartBufferMinutes",
                Value = "0",
                Description = "Minutes before event start to close ticket sales and RSVP registrations",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await _context.Settings.AddRangeAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} default settings", settings.Count);
    }
}
