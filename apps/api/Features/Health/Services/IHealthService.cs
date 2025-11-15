using WitchCityRope.Api.Features.Health.Models;

namespace WitchCityRope.Api.Features.Health.Services;

/// <summary>
/// Interface for health check service operations
/// Enables unit testing with mocking and follows service layer pattern
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Get basic health check information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing success status, health response, and error message</returns>
    Task<(bool Success, HealthResponse? Response, string Error)> GetHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed health check information including database version and active user counts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing success status, detailed health response, and error message</returns>
    Task<(bool Success, DetailedHealthResponse? Response, string Error)> GetDetailedHealthAsync(
        CancellationToken cancellationToken = default);
}
