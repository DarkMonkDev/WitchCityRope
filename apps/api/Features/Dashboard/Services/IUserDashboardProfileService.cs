using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Dashboard.Services;

/// <summary>
/// Service for user dashboard profile and event management
/// Provides user-specific event data and profile management for dashboard
/// </summary>
public interface IUserDashboardProfileService
{
    /// <summary>
    /// Get user's registered events
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="includePast">Include past events (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's registered events</returns>
    Task<Result<List<UserEventDto>>> GetUserEventsAsync(
        Guid userId,
        bool includePast = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's vetting status for alert box
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vetting status with display message</returns>
    Task<Result<VettingStatusDto>> GetVettingStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profile for settings page
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile data</returns>
    Task<Result<UserProfileDto>> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="request">Profile update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    Task<Result<UserProfileDto>> UpdateUserProfileAsync(
        Guid userId,
        UpdateProfileDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="request">Password change request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);
}
