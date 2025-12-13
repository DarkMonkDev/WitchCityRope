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
    /// Get an existing user by email, or create a new one if not found.
    /// Used for E2E tests that may run multiple times with same test data.
    /// </summary>
    /// <param name="request">User creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing or newly created user information with ID for cleanup</returns>
    Task<(bool Success, TestUserResponse? Data, string? Error)> GetOrCreateTestUserAsync(
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

    // ====================================================================
    // EVENT OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test event with specified properties
    /// Bypasses event creation validation for testing purposes
    /// </summary>
    /// <param name="request">Event creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event information with ID for cleanup</returns>
    Task<(bool Success, TestEventResponse? Data, string? Error)> CreateTestEventAsync(
        CreateTestEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test event by ID
    /// Used for test cleanup - also deletes related sessions and ticket types
    /// </summary>
    /// <param name="eventId">Event ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    // ====================================================================
    // SESSION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test session with specified properties
    /// </summary>
    /// <param name="request">Session creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session information with ID for cleanup</returns>
    Task<(bool Success, TestSessionResponse? Data, string? Error)> CreateTestSessionAsync(
        CreateTestSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test session by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="sessionId">Session ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    // ====================================================================
    // TICKET TYPE OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test ticket type with specified properties
    /// </summary>
    /// <param name="request">Ticket type creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created ticket type information with ID for cleanup</returns>
    Task<(bool Success, TestTicketTypeResponse? Data, string? Error)> CreateTestTicketTypeAsync(
        CreateTestTicketTypeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test ticket type by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="ticketTypeId">Ticket type ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestTicketTypeAsync(
        Guid ticketTypeId,
        CancellationToken cancellationToken = default);

    // ====================================================================
    // VOLUNTEER POSITION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test volunteer position with specified properties
    /// </summary>
    /// <param name="request">Volunteer position creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created position information with ID for cleanup</returns>
    Task<(bool Success, TestVolunteerPositionResponse? Data, string? Error)> CreateTestVolunteerPositionAsync(
        CreateTestVolunteerPositionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test volunteer position by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="positionId">Position ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestVolunteerPositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);

    // ====================================================================
    // VETTING APPLICATION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test vetting application with specified properties
    /// </summary>
    /// <param name="request">Vetting application creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created application information with ID for cleanup</returns>
    Task<(bool Success, TestVettingApplicationResponse? Data, string? Error)> CreateTestVettingApplicationAsync(
        CreateTestVettingApplicationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a test vetting application by ID
    /// Used for test cleanup
    /// </summary>
    /// <param name="applicationId">Application ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string? Error)> DeleteTestVettingApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
