using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of default venues for development and testing.
/// Creates 3 standard venues based on business requirements:
/// - Main Studio
/// - Community Space
/// - Outdoor Space
/// </summary>
public class VenueSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VenueSeeder> _logger;

    public VenueSeeder(
        ApplicationDbContext context,
        ILogger<VenueSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds 3 default venues for the application.
    /// Idempotent operation - skips if venues already exist.
    ///
    /// Creates:
    /// - Main Studio: Primary workshop and class space
    /// - Community Space: Large capacity social event space
    /// - Outdoor Space: Weather-dependent events with backup plan
    ///
    /// Each venue includes directions, notes, and is active by default.
    /// </summary>
    public async Task SeedVenuesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting venue seeding");

        // Check if venues already exist (idempotent operation)
        var existingVenueCount = await _context.Venues.CountAsync(cancellationToken);
        if (existingVenueCount > 0)
        {
            _logger.LogInformation("Venues already exist ({Count}), skipping venue seeding", existingVenueCount);
            return;
        }

        // Create 3 default venues per functional specification
        var defaultVenues = new[]
        {
            new Venue
            {
                Name = "Main Studio",
                Location = "Salem, MA",
                Directions = "Enter through main entrance, studio is on the second floor. Elevator available. Street parking on Washington St.",
                VenueInformation = "Maximum capacity: 30 people. Please remove shoes before entering.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Venue
            {
                Name = "Community Space",
                Location = "Newton, MA",
                Directions = "Located at 123 Salem Street, Salem MA. Use side entrance after 6pm. Free parking in rear lot.",
                VenueInformation = "Large open space suitable for social events and large classes. Capacity: 50 people.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Venue
            {
                Name = "Outdoor Space",
                Location = "Providence, RI",
                Directions = "Weather-dependent location will be announced via email 24 hours before event.",
                VenueInformation = "Backup indoor location: Main Studio. Bring sun protection and bug spray in summer months.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Venues.AddRangeAsync(defaultVenues, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Venue seeding completed. Created: {VenueCount} venues", defaultVenues.Length);
    }
}
