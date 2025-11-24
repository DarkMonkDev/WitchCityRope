using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.DTOs;

namespace WitchCityRope.Api.Features.Venues.Services;

/// <summary>
/// Service for managing venue operations
/// Implements business logic for venue retrieval with proper data access layer separation
/// </summary>
public class VenueService : IVenueService
{
    private readonly ApplicationDbContext _dbContext;

    public VenueService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get a single active venue by ID
    /// </summary>
    /// <param name="id">Venue ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VenueDto if found and active, null otherwise</returns>
    public async Task<VenueDto?> GetPublicVenueAsync(int id, CancellationToken cancellationToken = default)
    {
        // Only return active venues to public
        // Notes field excluded for public endpoints
        var venue = await _dbContext.Venues
            .Where(v => v.Id == id && v.IsActive)
            .Select(v => new VenueDto
            {
                Id = v.Id,
                Name = v.Name,
                Directions = v.Directions,
                Location = v.Location,
                Notes = null, // Don't expose internal notes to public
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return venue;
    }

    /// <summary>
    /// Get all active venues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active venues ordered by name</returns>
    public async Task<List<VenueDto>> GetPublicVenuesAsync(CancellationToken cancellationToken = default)
    {
        // Only return active venues to public
        // Notes field excluded for public endpoints
        var venues = await _dbContext.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new VenueDto
            {
                Id = v.Id,
                Name = v.Name,
                Directions = v.Directions,
                Location = v.Location,
                Notes = null, // Don't expose internal notes to public
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return venues;
    }
}
