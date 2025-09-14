using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Dashboard.Services;

/// <summary>
/// Service for user dashboard functionality
/// Provides user-specific dashboard data including profile, events, and statistics
/// </summary>
public interface IUserDashboardService
{
    /// <summary>
    /// Get basic dashboard data for a user
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User dashboard data including profile and vetting status</returns>
    Task<Result<UserDashboardResponse>> GetUserDashboardAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's upcoming events (next 3 by default)
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="count">Number of upcoming events to retrieve (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's upcoming events</returns>
    Task<Result<UserEventsResponse>> GetUserEventsAsync(
        Guid userId, 
        int count = 3, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's membership statistics
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's attendance and membership statistics</returns>
    Task<Result<UserStatisticsResponse>> GetUserStatisticsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}