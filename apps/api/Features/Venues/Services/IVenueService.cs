using WitchCityRope.Api.DTOs;

namespace WitchCityRope.Api.Features.Venues.Services;

/// <summary>
/// Interface for managing venue operations
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Get a single active venue by ID
    /// </summary>
    /// <param name="id">Venue ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VenueDto if found and active, null otherwise</returns>
    Task<VenueDto?> GetPublicVenueAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active venues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active venues</returns>
    Task<List<VenueDto>> GetPublicVenuesAsync(CancellationToken cancellationToken = default);
}
