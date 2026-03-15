using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
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
                Role = request.Role ?? "", // Empty string = no special role; "Member" is not a valid role
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
                Role = user.Role ?? "",
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
    /// Get an existing user by email, or create a new one if not found.
    /// Used for E2E tests that may run multiple times with same test data.
    /// </summary>
    public async Task<(bool Success, TestUserResponse? Data, string? Error)> GetOrCreateTestUserAsync(
        CreateTestUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GetOrCreate test user: {Email}", request.Email);

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogInformation("Found existing user: {Email} (ID: {UserId})", request.Email, existingUser.Id);

                return (true, new TestUserResponse
                {
                    Id = existingUser.Id.ToString(),
                    Email = existingUser.Email!,
                    SceneName = existingUser.SceneName,
                    Role = existingUser.Role ?? "",
                    CreatedAt = existingUser.CreatedAt
                }, null);
            }

            // User doesn't exist, create new one
            return await CreateTestUserAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetOrCreate test user: {Email}", request.Email);
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
                PaymentStatus = Enum.TryParse<TicketPurchasePaymentStatus>(request.PaymentStatus, ignoreCase: true, out var parsedStatus)
                    ? parsedStatus
                    : TicketPurchasePaymentStatus.Completed,
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

            // CRITICAL: Also create EventAttendee record for check-in display
            // EventAttendance links user to event for eligibility
            // EventAttendee is what shows in the check-in attendees list
            var eventAttendee = new EventAttendee(eventId, userId, "confirmed")
            {
                TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMddHHmmss}-{ticketPurchase.Id.ToString()[..8].ToUpper()}",
                HasCompletedWaiver = true, // Assume waiver completed for test data
                IsFirstTime = false,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            _context.Set<EventAttendee>().Add(eventAttendee);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test ticket purchase: {Id} - {Reference} with EventAttendance: {AttendanceId} and EventAttendee: {EventAttendeeId}",
                ticketPurchase.Id, paymentReference, attendance.Id, eventAttendee.Id);

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
    /// Verify a user's email address
    /// Used for E2E tests to bypass email verification flow
    /// </summary>
    public async Task<(bool Success, string? Error)> VerifyUserEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying email for test user: {Email}", email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found for email verification: {Email}", email);
                return (false, "User not found with the specified email");
            }

            // Set email as confirmed
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to verify email: {Errors}", errors);
                return (false, $"Email verification failed: {errors}");
            }

            _logger.LogInformation("✅ Successfully verified email for user: {Email} (ID: {UserId})", email, user.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception verifying email: {Email}", email);
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
            Role = "", // No special role for walk-in test users
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
            // Case-insensitive role match for test helper robustness
            .Where(u => u.Role != null && u.Role.ToLower() == "administrator")
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

    // ====================================================================
    // EVENT OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test event with specified properties
    /// </summary>
    public async Task<(bool Success, TestEventResponse? Data, string? Error)> CreateTestEventAsync(
        CreateTestEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test event: {Title}", request.Title);

            var eventEntity = new Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                ShortDescription = request.ShortDescription ?? $"Test event: {request.Title}",
                Description = request.Description ?? $"Test event description for {request.Title}",
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AllowRsvps = request.AllowRsvps,
                RequireTicketPurchase = request.RequireTicketPurchase,
                VettedMembersOnly = request.VettedMembersOnly,
                IsPublished = request.IsPublished,
                Capacity = request.Capacity,
                VenueId = request.VenueId ?? 1, // Default to test venue
                // Timing controls for session-based ticket availability
                RegistrationOpenHours = request.RegistrationOpenHours,
                RegistrationCloseHours = request.RegistrationCloseHours,
                CancellationCloseHours = request.CancellationCloseHours,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Event>().Add(eventEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test event: {EventId} - {Title}", eventEntity.Id, eventEntity.Title);

            return (true, new TestEventResponse
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Status = eventEntity.IsPublished ? "Published" : "Draft"
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test event: {Title}", request.Title);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete a test event by ID with full cascade deletion of all related entities
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test event with full cascade: {EventId}", eventId);

            var eventEntity = await _context.Set<Event>()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Test event not found for deletion: {EventId}", eventId);
                return (false, $"Event not found: {eventId}");
            }

            // 1. Delete VolunteerSignups for all volunteer positions in this event
            var volunteerPositions = await _context.Set<VolunteerPosition>()
                .Where(v => v.EventId == eventId)
                .ToListAsync(cancellationToken);

            var positionIds = volunteerPositions.Select(vp => vp.Id).ToList();
            if (positionIds.Any())
            {
                var volunteerSignups = await _context.Set<VolunteerSignup>()
                    .Where(vs => positionIds.Contains(vs.VolunteerPositionId))
                    .ToListAsync(cancellationToken);
                _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);
                _logger.LogDebug("Removed {Count} volunteer signups", volunteerSignups.Count);
            }

            // 2. Delete EventAttendances for this event
            var eventAttendances = await _context.Set<EventAttendance>()
                .Where(ea => ea.EventId == eventId)
                .ToListAsync(cancellationToken);
            _context.Set<EventAttendance>().RemoveRange(eventAttendances);
            _logger.LogDebug("Removed {Count} event attendances", eventAttendances.Count);

            // 2.5. Delete CheckInSessionTokens for this event
            var eventTokens = await _context.Set<CheckInSessionToken>()
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);
            _context.Set<CheckInSessionToken>().RemoveRange(eventTokens);
            _logger.LogDebug("Removed {Count} check-in session tokens", eventTokens.Count);

            // 3. Delete TicketPurchases for ticket types in this event
            var ticketTypes = await _context.Set<TicketType>()
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);

            var ticketTypeIds = ticketTypes.Select(tt => tt.Id).ToList();
            if (ticketTypeIds.Any())
            {
                var ticketPurchases = await _context.Set<TicketPurchase>()
                    .Where(tp => ticketTypeIds.Contains(tp.TicketTypeId))
                    .ToListAsync(cancellationToken);
                _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);
                _logger.LogDebug("Removed {Count} ticket purchases", ticketPurchases.Count);
            }

            // 4. Clear TicketType-Session many-to-many relationships
            // Load ticket types with their sessions
            var ticketTypesWithSessions = await _context.Set<TicketType>()
                .Include(tt => tt.Sessions)
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);

            foreach (var ticketType in ticketTypesWithSessions)
            {
                ticketType.Sessions.Clear();
            }

            // 5. Delete TicketTypes
            _context.Set<TicketType>().RemoveRange(ticketTypes);
            _logger.LogDebug("Removed {Count} ticket types", ticketTypes.Count);

            // 6. Delete VolunteerPositions
            _context.Set<VolunteerPosition>().RemoveRange(volunteerPositions);
            _logger.LogDebug("Removed {Count} volunteer positions", volunteerPositions.Count);

            // 7. Delete Sessions
            var sessions = await _context.Set<Session>()
                .Where(s => s.EventId == eventId)
                .ToListAsync(cancellationToken);
            _context.Set<Session>().RemoveRange(sessions);
            _logger.LogDebug("Removed {Count} sessions", sessions.Count);

            // 8. Delete the Event itself
            _context.Set<Event>().Remove(eventEntity);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test event with cascade: {EventId} (sessions: {Sessions}, tickets: {Tickets}, volunteers: {Volunteers})",
                eventId, sessions.Count, ticketTypes.Count, volunteerPositions.Count);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test event: {EventId}", eventId);
            return (false, ex.Message);
        }
    }

    // ====================================================================
    // SESSION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test session with specified properties
    /// </summary>
    public async Task<(bool Success, TestSessionResponse? Data, string? Error)> CreateTestSessionAsync(
        CreateTestSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test session: {Name} for event {EventId}", request.Name, request.EventId);

            // Verify parent event exists
            var eventExists = await _context.Set<Event>()
                .AnyAsync(e => e.Id == request.EventId, cancellationToken);

            if (!eventExists)
            {
                return (false, null, $"Parent event not found: {request.EventId}");
            }

            var sessionEntity = new Session
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                SessionCode = request.SessionCode ?? $"S{Guid.NewGuid().ToString()[..4].ToUpper()}",
                Name = request.Name,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Capacity = request.Capacity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Session>().Add(sessionEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test session: {SessionId} - {Name}", sessionEntity.Id, sessionEntity.Name);

            return (true, new TestSessionResponse
            {
                Id = sessionEntity.Id,
                EventId = sessionEntity.EventId,
                Name = sessionEntity.Name,
                StartTime = sessionEntity.StartTime,
                EndTime = sessionEntity.EndTime
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test session: {Name}", request.Name);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete a test session by ID with cascade deletion of related entities
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test session with cascade: {SessionId}", sessionId);

            var sessionEntity = await _context.Set<Session>()
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (sessionEntity == null)
            {
                _logger.LogWarning("Test session not found for deletion: {SessionId}", sessionId);
                return (false, $"Session not found: {sessionId}");
            }

            // 0. Delete CheckInSessionTokens referencing this session
            var sessionTokens = await _context.Set<CheckInSessionToken>()
                .Where(t => t.SessionId == sessionId)
                .ToListAsync(cancellationToken);
            _context.Set<CheckInSessionToken>().RemoveRange(sessionTokens);
            _logger.LogDebug("Removed {Count} check-in session tokens", sessionTokens.Count);

            // 1. Delete VolunteerSignups for positions linked to this session
            var sessionVolunteerPositions = await _context.Set<VolunteerPosition>()
                .Where(vp => vp.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            var positionIds = sessionVolunteerPositions.Select(vp => vp.Id).ToList();
            if (positionIds.Any())
            {
                var volunteerSignups = await _context.Set<VolunteerSignup>()
                    .Where(vs => positionIds.Contains(vs.VolunteerPositionId))
                    .ToListAsync(cancellationToken);
                _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);
                _logger.LogDebug("Removed {Count} volunteer signups", volunteerSignups.Count);
            }

            // 2. Delete EventAttendances referencing this session
            var sessionAttendances = await _context.Set<EventAttendance>()
                .Where(ea => ea.SessionId == sessionId)
                .ToListAsync(cancellationToken);
            _context.Set<EventAttendance>().RemoveRange(sessionAttendances);
            _logger.LogDebug("Removed {Count} event attendances", sessionAttendances.Count);

            // 3. Get TicketPurchases that reference TicketTypes linked to this session
            // First, get ticket types linked to this session via the many-to-many relationship
            var ticketTypesWithSession = await _context.Set<TicketType>()
                .Include(tt => tt.Sessions)
                .Where(tt => tt.Sessions.Any(s => s.Id == sessionId))
                .ToListAsync(cancellationToken);

            // Delete ticket purchases for these ticket types
            var ticketTypeIds = ticketTypesWithSession.Select(tt => tt.Id).ToList();
            if (ticketTypeIds.Any())
            {
                // First delete EventAttendances that reference these ticket purchases
                var ticketPurchases = await _context.Set<TicketPurchase>()
                    .Where(tp => ticketTypeIds.Contains(tp.TicketTypeId))
                    .ToListAsync(cancellationToken);

                var purchaseIds = ticketPurchases.Select(tp => tp.Id).ToList();
                if (purchaseIds.Any())
                {
                    var purchaseAttendances = await _context.Set<EventAttendance>()
                        .Where(ea => ea.TicketPurchaseId.HasValue && purchaseIds.Contains(ea.TicketPurchaseId.Value))
                        .ToListAsync(cancellationToken);
                    _context.Set<EventAttendance>().RemoveRange(purchaseAttendances);
                    _logger.LogDebug("Removed {Count} purchase attendances", purchaseAttendances.Count);
                }

                _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);
                _logger.LogDebug("Removed {Count} ticket purchases", ticketPurchases.Count);
            }

            // 4. Clear the many-to-many relationship (TicketTypeSessions)
            // This is done automatically when we clear the Sessions collection
            foreach (var ticketType in ticketTypesWithSession)
            {
                ticketType.Sessions.Remove(sessionEntity);
            }

            // 5. Delete VolunteerPositions linked to this session
            _context.Set<VolunteerPosition>().RemoveRange(sessionVolunteerPositions);
            _logger.LogDebug("Removed {Count} volunteer positions", sessionVolunteerPositions.Count);

            // 6. Delete the session itself
            _context.Set<Session>().Remove(sessionEntity);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test session with cascade: {SessionId}", sessionId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test session: {SessionId}", sessionId);
            return (false, ex.Message);
        }
    }

    // ====================================================================
    // TICKET TYPE OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test ticket type with specified properties
    /// </summary>
    public async Task<(bool Success, TestTicketTypeResponse? Data, string? Error)> CreateTestTicketTypeAsync(
        CreateTestTicketTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test ticket type: {Name} for event {EventId}", request.Name, request.EventId);

            // Verify parent event exists
            var eventEntity = await _context.Set<Event>()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return (false, null, $"Parent event not found: {request.EventId}");
            }

            var ticketTypeEntity = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Name = request.Name,
                Description = request.Description ?? $"Test ticket type: {request.Name}",
                PricingType = (WitchCityRope.Models.PricingType)request.PricingType,
                Price = request.Price,
                Available = request.Available,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add session associations if provided
            if (request.SessionIds?.Any() == true)
            {
                var sessions = await _context.Set<Session>()
                    .Where(s => request.SessionIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                foreach (var session in sessions)
                {
                    ticketTypeEntity.Sessions.Add(session);
                }
            }

            _context.Set<TicketType>().Add(ticketTypeEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test ticket type: {TicketTypeId} - {Name}", ticketTypeEntity.Id, ticketTypeEntity.Name);

            return (true, new TestTicketTypeResponse
            {
                Id = ticketTypeEntity.Id,
                EventId = ticketTypeEntity.EventId,
                Name = ticketTypeEntity.Name,
                Price = ticketTypeEntity.Price ?? 0,
                Available = ticketTypeEntity.Available
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test ticket type: {Name}", request.Name);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete a test ticket type by ID with cascade deletion of related entities
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestTicketTypeAsync(
        Guid ticketTypeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test ticket type with cascade: {TicketTypeId}", ticketTypeId);

            var ticketTypeEntity = await _context.Set<TicketType>()
                .Include(tt => tt.Sessions)
                .FirstOrDefaultAsync(t => t.Id == ticketTypeId, cancellationToken);

            if (ticketTypeEntity == null)
            {
                _logger.LogWarning("Test ticket type not found for deletion: {TicketTypeId}", ticketTypeId);
                return (false, $"Ticket type not found: {ticketTypeId}");
            }

            // 1. Delete TicketPurchases for this ticket type
            var ticketPurchases = await _context.Set<TicketPurchase>()
                .Where(tp => tp.TicketTypeId == ticketTypeId)
                .ToListAsync(cancellationToken);

            // 1a. First delete EventAttendances that reference these purchases
            var purchaseIds = ticketPurchases.Select(tp => tp.Id).ToList();
            if (purchaseIds.Any())
            {
                var purchaseAttendances = await _context.Set<EventAttendance>()
                    .Where(ea => ea.TicketPurchaseId.HasValue && purchaseIds.Contains(ea.TicketPurchaseId.Value))
                    .ToListAsync(cancellationToken);
                _context.Set<EventAttendance>().RemoveRange(purchaseAttendances);
                _logger.LogDebug("Removed {Count} purchase attendances", purchaseAttendances.Count);
            }

            _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);
            _logger.LogDebug("Removed {Count} ticket purchases", ticketPurchases.Count);

            // 2. Clear the many-to-many relationship (TicketTypeSessions)
            ticketTypeEntity.Sessions.Clear();

            // 3. Delete the TicketType itself
            _context.Set<TicketType>().Remove(ticketTypeEntity);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test ticket type with cascade: {TicketTypeId}", ticketTypeId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test ticket type: {TicketTypeId}", ticketTypeId);
            return (false, ex.Message);
        }
    }

    // ====================================================================
    // VOLUNTEER POSITION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test volunteer position with specified properties
    /// </summary>
    public async Task<(bool Success, TestVolunteerPositionResponse? Data, string? Error)> CreateTestVolunteerPositionAsync(
        CreateTestVolunteerPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test volunteer position: {Title} for event {EventId}", request.Title, request.EventId);

            // Verify parent event exists
            var eventExists = await _context.Set<Event>()
                .AnyAsync(e => e.Id == request.EventId, cancellationToken);

            if (!eventExists)
            {
                return (false, null, $"Parent event not found: {request.EventId}");
            }

            // Verify session exists (required — all positions are session-specific)
            var sessionExists = await _context.Set<Session>()
                .AnyAsync(s => s.Id == request.SessionId, cancellationToken);

            if (!sessionExists)
            {
                return (false, null, $"Session not found: {request.SessionId}");
            }

            var positionEntity = new VolunteerPosition
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                SessionId = request.SessionId,
                Title = request.Title,
                Description = request.Description ?? $"Test volunteer position: {request.Title}",
                SlotsNeeded = request.SlotsNeeded,
                SlotsFilled = request.SlotsFilled,
                IsPublicFacing = request.IsPublicFacing,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<VolunteerPosition>().Add(positionEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test volunteer position: {PositionId} - {Title}", positionEntity.Id, positionEntity.Title);

            return (true, new TestVolunteerPositionResponse
            {
                Id = positionEntity.Id,
                EventId = positionEntity.EventId,
                Title = positionEntity.Title,
                SlotsNeeded = positionEntity.SlotsNeeded,
                SlotsFilled = positionEntity.SlotsFilled
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test volunteer position: {Title}", request.Title);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete a test volunteer position by ID with cascade deletion of related entities
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestVolunteerPositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test volunteer position with cascade: {PositionId}", positionId);

            var positionEntity = await _context.Set<VolunteerPosition>()
                .FirstOrDefaultAsync(v => v.Id == positionId, cancellationToken);

            if (positionEntity == null)
            {
                _logger.LogWarning("Test volunteer position not found for deletion: {PositionId}", positionId);
                return (false, $"Volunteer position not found: {positionId}");
            }

            // 1. Delete VolunteerSignups for this position
            var volunteerSignups = await _context.Set<VolunteerSignup>()
                .Where(vs => vs.VolunteerPositionId == positionId)
                .ToListAsync(cancellationToken);
            _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);
            _logger.LogDebug("Removed {Count} volunteer signups", volunteerSignups.Count);

            // 2. Delete the VolunteerPosition itself
            _context.Set<VolunteerPosition>().Remove(positionEntity);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test volunteer position with cascade: {PositionId}", positionId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test volunteer position: {PositionId}", positionId);
            return (false, ex.Message);
        }
    }

    // ====================================================================
    // VETTING APPLICATION OPERATIONS
    // ====================================================================

    /// <summary>
    /// Create a test vetting application with specified properties
    /// </summary>
    public async Task<(bool Success, TestVettingApplicationResponse? Data, string? Error)> CreateTestVettingApplicationAsync(
        CreateTestVettingApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating test vetting application for user: {UserId}", request.UserId);

            // Verify user exists
            if (!Guid.TryParse(request.UserId, out var userGuid))
            {
                return (false, null, "Invalid user ID format");
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return (false, null, $"User not found: {request.UserId}");
            }

            var applicationEntity = new Features.Vetting.Entities.VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = userGuid,
                SceneName = user.SceneName ?? "Test Applicant",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Pronouns = user.Pronouns,
                WorkflowStatus = (Features.Vetting.Entities.VettingStatus)request.WorkflowStatus,
                ExperienceDescription = request.ExperienceDescription ?? "Test experience description",
                WhyJoinCommunity = request.WhyJoinCommunity ?? "Test reason to join",
                HowDidYouHearAboutUs = request.HowDidYouHearAboutUs ?? "Test referral",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                StatusToken = Guid.NewGuid().ToString(),
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Features.Vetting.Entities.VettingApplication>().Add(applicationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully created test vetting application: {ApplicationId} for user {UserId}",
                applicationEntity.Id, request.UserId);

            return (true, new TestVettingApplicationResponse
            {
                Id = applicationEntity.Id,
                UserId = request.UserId,
                Status = applicationEntity.WorkflowStatus.ToString(),
                SubmittedAt = applicationEntity.SubmittedAt
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test vetting application for user: {UserId}", request.UserId);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete a test vetting application by ID
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteTestVettingApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting test vetting application: {ApplicationId}", applicationId);

            var applicationEntity = await _context.Set<Features.Vetting.Entities.VettingApplication>()
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (applicationEntity == null)
            {
                _logger.LogWarning("Test vetting application not found for deletion: {ApplicationId}", applicationId);
                return (false, $"Vetting application not found: {applicationId}");
            }

            _context.Set<Features.Vetting.Entities.VettingApplication>().Remove(applicationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🗑️ Successfully deleted test vetting application: {ApplicationId}", applicationId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test vetting application: {ApplicationId}", applicationId);
            return (false, ex.Message);
        }
    }
}
