using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.TestHelpers.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.TestHelpers.Services;

/// <summary>
/// Implementation of test helper service for E2E testing
/// CRITICAL: Only available in Development/Test environments
/// </summary>
public class TestHelperService : ITestHelperService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TestHelperService> _logger;

    public TestHelperService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TestHelperService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a test user with specified properties
    /// Uses ASP.NET Core Identity for proper password hashing
    /// </summary>
    public async Task<(bool Success, TestUserResponse? Data, string? Error)> CreateTestUserAsync(
        CreateTestUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test user: {Email}", request.Email);

            // Validate email not already in use
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Test user creation failed - email already exists: {Email}", request.Email);
                return (false, null, $"User with email {request.Email} already exists");
            }

            // Create user with Identity
            // CRITICAL: PostgreSQL requires UTC DateTimes for timestamp with time zone columns
            var dateOfBirth = request.DateOfBirth ?? new DateTime(1990, 1, 1);
            if (dateOfBirth.Kind == DateTimeKind.Unspecified)
            {
                dateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                SceneName = request.SceneName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role ?? "Member",
                VettingStatus = request.VettingStatus, // 0-6 enum value, 3 = Approved (vetted)
                Bio = request.Bio,
                Pronouns = request.Pronouns ?? string.Empty, // CRITICAL: Database has NOT NULL constraint
                DateOfBirth = dateOfBirth,
                EmailConfirmed = true, // Skip email confirmation for testing
                LockoutEnabled = false, // Prevent lockout during testing
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Create user with password hashing via UserManager
            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create test user: {Errors}", errors);
                return (false, null, $"User creation failed: {errors}");
            }

            _logger.LogInformation("✅ Successfully created test user: {Email} (ID: {UserId})", user.Email, user.Id);

            // Return user information for test cleanup
            var response = new TestUserResponse
            {
                Id = user.Id.ToString(),
                Email = user.Email!,
                SceneName = user.SceneName,
                Role = user.Role ?? "Member",
                CreatedAt = DateTime.UtcNow
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating test user: {Email}", request.Email);
            return (false, null, $"Internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a test user by ID
    /// Used for test cleanup in afterEach/afterAll hooks
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test user: {UserId}", userId);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return (false, "Invalid user ID format");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Test user not found for deletion: {UserId}", userId);
                return (false, "User not found");
            }

            // Delete user via UserManager (handles related data cleanup)
            var deleteResult = await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
            {
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete test user: {Errors}", errors);
                return (false, $"User deletion failed: {errors}");
            }

            _logger.LogInformation("🗑️ Successfully deleted test user: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting test user: {UserId}", userId);
            return (false, $"Internal error: {ex.Message}");
        }
    }
}
