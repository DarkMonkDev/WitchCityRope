using WitchCityRope.Api.Features.TestHelpers.Models;

namespace WitchCityRope.Api.Features.TestHelpers.Services;

/// <summary>
/// Service for programmatic test data creation
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

    /// <summary>
    /// Create a test ticket purchase with specified properties
    /// Bypasses payment flow for testing purposes
    /// </summary>
    /// <param name="request">Ticket purchase creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created ticket purchase information with ID for cleanup</returns>
    Task<(bool Success, TestTicketPurchaseResponse? Data, string? Error)> CreateTestTicketPurchaseAsync(
        CreateTestTicketPurchaseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test ticket purchase by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="ticketPurchaseId">Ticket purchase ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestTicketPurchaseAsync(
        Guid ticketPurchaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a user's email address
    /// Used for E2E tests to bypass email verification
    /// </summary>
    /// <param name="email">Email address to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> VerifyUserEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
