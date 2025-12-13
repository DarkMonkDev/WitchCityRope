using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of default venues for development and testing.
/// Creates 3 standard venues based on business requirements:
/// - Witch City Rope (Salem, MA)
/// - DOINK (Northampton, MA)
/// - Boston Shibari Soiree (Cambridge, MA)
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
    /// - Witch City Rope: Primary workshop and class space in Salem
    /// - DOINK: Partner organization space in Northampton
    /// - Boston Shibari Soiree: Partner organization space in Cambridge
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
                Name = "Witch City Rope",
                Location = "Salem, MA",
                Directions = "Enter through main entrance on Essex Street, studio is on the second floor. Elevator available. Street parking on Washington St or use the MBTA Commuter Rail to Salem Station.",
                VenueInformation = "Maximum capacity: 30 people. Please remove shoes before entering the practice space.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Venue
            {
                Name = "DOINK",
                Location = "Northampton, MA",
                Directions = "Located on Main Street in downtown Northampton. Take I-91 to Exit 20, follow Route 5 north into downtown. Street parking available on Main St and in the parking garage on Hampton Ave. Enter through the green door next to the coffee shop.",
                VenueInformation = "Partner organization space in the Pioneer Valley. Capacity: 40 people. Accessible entrance available.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Venue
            {
                Name = "Boston Shibari Soiree",
                Location = "Cambridge, MA",
                Directions = "Located near Central Square. Take the Red Line to Central Square station, walk 5 minutes down Massachusetts Ave. Metered street parking available or use the Green St parking garage.",
                VenueInformation = "Partner organization space in Cambridge. Capacity: 35 people. Close to restaurants and bars for post-event socializing.",
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
