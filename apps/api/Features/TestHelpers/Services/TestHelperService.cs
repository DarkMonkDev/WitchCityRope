using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Safety.Services;
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
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TestHelperService> _logger;

    public TestHelperService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEncryptionService encryptionService,
        ILogger<TestHelperService> logger)
    {
        _context = context;
        _userManager = userManager;
        _encryptionService = encryptionService;
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

    /// <summary>
    /// Create a test ticket purchase with specified properties
    /// Bypasses normal payment flow for E2E test isolation
    /// </summary>
    public async Task<(bool Success, TestTicketPurchaseResponse? Data, string? Error)> CreateTestTicketPurchaseAsync(
        CreateTestTicketPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test ticket purchase: Amount={Amount}, Method={Method}",
                request.TotalPrice, request.PaymentMethod);

            // Create unique test user for each purchase to avoid unique constraint violations
            // (One active attendance per user per event per type)
            var userId = request.UserId ?? await CreateUniqueTestUserAsync(cancellationToken);
            if (userId == Guid.Empty)
            {
                return (false, null, "Could not create test user for ticket purchase");
            }

            // Get ticket type with event info (use first available if not specified)
            var ticketType = request.TicketTypeId.HasValue
                ? await _context.Set<TicketType>()
                    .Include(tt => tt.Event)
                    .FirstOrDefaultAsync(tt => tt.Id == request.TicketTypeId.Value, cancellationToken)
                : await GetFirstTicketTypeWithEventAsync(cancellationToken);

            if (ticketType == null)
            {
                return (false, null, "No ticket types available in database");
            }

            var ticketTypeId = ticketType.Id;
            var eventName = ticketType.Event?.Title ?? "Unknown Event";

            // Generate unique payment reference if not provided
            var paymentReference = request.PaymentReference
                ?? $"E2E-TEST-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // Create encrypted PayPal Capture ID if needed
            string? encryptedCaptureId = null;
            var includeCapture = request.IncludePayPalCaptureId
                ?? request.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase);

            if (includeCapture)
            {
                var mockCaptureId = $"WH-TEST-{DateTime.Now.Ticks}-CAPTURE-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                encryptedCaptureId = await _encryptionService.EncryptAsync(mockCaptureId);
                _logger.LogDebug("Generated encrypted PayPal Capture ID for test payment");
            }

            // Create TicketPurchase record
            var ticketPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = ticketTypeId,
                UserId = userId,
                Quantity = request.Quantity,
                TotalPrice = request.TotalPrice,
                PaymentStatus = request.PaymentStatus,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = paymentReference,
                Notes = request.Notes ?? $"E2E Test Payment - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                EncryptedPayPalCaptureId = encryptedCaptureId,
                ProcessedAt = request.PaymentStatus == "Completed" ? DateTime.UtcNow : null,
                PurchaseDate = DateTime.UtcNow,
                EventWaiverAccepted = true,
                EventWaiverAcceptedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<TicketPurchase>().Add(ticketPurchase);
            await _context.SaveChangesAsync(cancellationToken);

            // CRITICAL: Also create EventAttendance record to link the purchase to the event
            // This is required for the deletion check to detect sales
            var eventId = ticketType.EventId;
            var attendance = new EventAttendance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = eventId,
                AttendanceType = AttendanceType.Ticket,
                Status = AttendanceStatus.Active,
                TicketPurchaseId = ticketPurchase.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<EventAttendance>().Add(attendance);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test ticket purchase: {Id} - {Reference} with EventAttendance: {AttendanceId}",
                ticketPurchase.Id, paymentReference, attendance.Id);

            // Return response for test assertions
            var response = new TestTicketPurchaseResponse
            {
                Id = ticketPurchase.Id,
                PaymentReference = paymentReference,
                TotalPrice = request.TotalPrice,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentStatus,
                UserId = userId,
                TicketTypeId = ticketTypeId,
                Quantity = request.Quantity,
                HasPayPalCaptureId = encryptedCaptureId != null,
                EventName = eventName
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating test ticket purchase");
            return (false, null, $"Internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a test ticket purchase by ID
    /// Used for test cleanup
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestTicketPurchaseAsync(
        Guid ticketPurchaseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test ticket purchase: {Id}", ticketPurchaseId);

            var ticketPurchase = await _context.Set<TicketPurchase>()
                .FirstOrDefaultAsync(tp => tp.Id == ticketPurchaseId, cancellationToken);

            if (ticketPurchase == null)
            {
                _logger.LogWarning("Test ticket purchase not found for deletion: {Id}", ticketPurchaseId);
                return (false, "Ticket purchase not found");
            }

            _context.Set<TicketPurchase>().Remove(ticketPurchase);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test ticket purchase: {Id}", ticketPurchaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting test ticket purchase: {Id}", ticketPurchaseId);
            return (false, $"Internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a unique test user for each ticket purchase
    /// This avoids unique constraint violations (one active attendance per user per event per type)
    /// </summary>
    private async Task<Guid> CreateUniqueTestUserAsync(CancellationToken cancellationToken)
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"testpurchase-{uniqueId}@e2etest.local",
            UserName = $"testpurchase-{uniqueId}@e2etest.local",
            EmailConfirmed = true,
            IsActive = true,
            SceneName = $"E2E Test User {uniqueId}",
            Role = "Member",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(testUser, "TestPass123!");
        if (result.Succeeded)
        {
            _logger.LogDebug("Created test user: {Email} for ticket purchase", testUser.Email);
            return testUser.Id;
        }

        _logger.LogWarning("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        return Guid.Empty;
    }

    /// <summary>
    /// Helper to get admin user ID
    /// </summary>
    private async Task<Guid> GetAdminUserIdAsync(CancellationToken cancellationToken)
    {
        var adminUser = await _userManager.Users
            .Where(u => u.Role == "Administrator")
            .FirstOrDefaultAsync(cancellationToken);

        return adminUser?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Helper to get first available ticket type with event info
    /// </summary>
    private async Task<TicketType?> GetFirstTicketTypeWithEventAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<TicketType>()
            .Include(tt => tt.Event)
            .OrderBy(tt => tt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
