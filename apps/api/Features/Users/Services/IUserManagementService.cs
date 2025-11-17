using WitchCityRope.Api.Features.Users.Models;

namespace WitchCityRope.Api.Features.Users.Services;

/// <summary>
/// Interface for user management service operations
/// Enables testability through dependency injection and mocking
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Get user profile for current user
    /// </summary>
    /// <param name="userId">User ID from JWT claims</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile DTO or error</returns>
    Task<(bool Success, UserDto? Response, string Error)> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile for current user
    /// </summary>
    /// <param name="userId">User ID from JWT claims</param>
    /// <param name="request">Profile update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile DTO or error</returns>
    Task<(bool Success, UserDto? Response, string Error)> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of users for admin with filtering
    /// </summary>
    /// <param name="request">Search and filter request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated user list or error</returns>
    Task<(bool Success, UserListResponse? Response, string Error)> GetUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single user by ID for admin
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User DTO or error</returns>
    Task<(bool Success, UserDto? Response, string Error)> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user for admin with role and status changes
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">User update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user DTO or error</returns>
    Task<(bool Success, UserDto? Response, string Error)> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user roles for admin
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Roles update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user DTO or error</returns>
    Task<(bool Success, UserDto? Response, string Error)> UpdateUserRolesAsync(
        string userId,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get users by role for dropdown options
    /// </summary>
    /// <param name="role">Role to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user option DTOs or error</returns>
    Task<(bool Success, IEnumerable<UserOptionDto>? Response, string Error)> GetUsersByRoleAsync(
        string role,
        CancellationToken cancellationToken = default);
}
