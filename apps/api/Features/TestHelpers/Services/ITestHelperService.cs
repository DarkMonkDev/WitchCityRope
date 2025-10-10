using WitchCityRope.Api.Features.TestHelpers.Models;

namespace WitchCityRope.Api.Features.TestHelpers.Services;

/// <summary>
/// Service for programmatic test user creation
/// ONLY available in Development/Test environments
/// </summary>
public interface ITestHelperService
{
    /// <summary>
    /// Create a test user with specified properties
    /// Bypasses registration validation for testing purposes
    /// </summary>
    /// <param name="request">User creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information with ID for cleanup</returns>
    Task<(bool Success, TestUserResponse? Data, string? Error)> CreateTestUserAsync(
        CreateTestUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test user by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
