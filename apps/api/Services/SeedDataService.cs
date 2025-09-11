using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Service responsible for populating database seed data for development and testing.
/// Implements comprehensive test data creation using EF Core patterns and transaction management.
/// 
/// Key features:
/// - Idempotent operations (safe to run multiple times)
/// - Transaction-based consistency with rollback capability
/// - Proper UTC DateTime handling following ApplicationDbContext patterns
/// - ASP.NET Core Identity integration for test accounts
/// - Structured logging for operational visibility
/// - Result pattern for error handling and reporting
/// 
/// Seed data includes:
/// - 5 test accounts covering all role scenarios (Admin, Teacher, Member, Guest, Organizer)
/// - 12 sample events (10 upcoming, 2 past) with realistic data
/// - Vetting status configuration for development workflows
/// 
/// Implementation follows existing service layer patterns and coding standards.
/// </summary>
public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Coordinates all seed data operations in a single transaction.
    /// Provides comprehensive error handling and rollback capability.
    /// 
    /// Uses EF Core transaction management to ensure data consistency
    /// and follows result pattern for error reporting.
    /// </summary>
    public async Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new InitializationResult
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        _logger.LogInformation("Starting comprehensive seed data population");

        // Check if seeding is required to avoid unnecessary work
        if (!await IsSeedDataRequiredAsync(cancellationToken))
        {
            _logger.LogInformation("Seed data already exists, skipping population");
            result.Success = true;
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var initialUserCount = await _userManager.Users.CountAsync(cancellationToken);
            var initialEventCount = await _context.Events.CountAsync(cancellationToken);

            // Seed data operations in logical order
            await SeedUsersAsync(cancellationToken);
            await SeedEventsAsync(cancellationToken);
            await SeedVettingStatusesAsync(cancellationToken);

            // Calculate records created
            var finalUserCount = await _userManager.Users.CountAsync(cancellationToken);
            var finalEventCount = await _context.Events.CountAsync(cancellationToken);
            
            result.SeedRecordsCreated = (finalUserCount - initialUserCount) + (finalEventCount - initialEventCount);

            await transaction.CommitAsync(cancellationToken);
            
            result.Success = true;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Seed data population completed successfully in {Duration}ms. " +
                "Records created: {RecordCount}",
                result.Duration.TotalMilliseconds, result.SeedRecordsCreated);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            result.Success = false;
            result.Errors.Add(ex.Message);
            result.Duration = stopwatch.Elapsed;
            
            _logger.LogError(ex, "Seed data population failed after {Duration}ms",
                stopwatch.Elapsed.TotalMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Creates comprehensive test user accounts covering all role scenarios.
    /// Uses ASP.NET Core Identity for proper authentication setup.
    /// 
    /// Test accounts created:
    /// - admin@witchcityrope.com (Administrator role)
    /// - teacher@witchcityrope.com (Teacher role, vetted)
    /// - member@witchcityrope.com (Member role, vetted)
    /// - guest@witchcityrope.com (Guest/Attendee role)
    /// - organizer@witchcityrope.com (Organizer role)
    /// 
    /// All accounts use secure password "Test123!" and follow existing
    /// ApplicationUser patterns with proper UTC datetime handling.
    /// </summary>
    public async Task SeedUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting test user account creation");

        // Define comprehensive test accounts per functional specification
        var testAccounts = new[]
        {
            new { 
                Email = "admin@witchcityrope.com", 
                SceneName = "RopeMaster", 
                Role = "Administrator",
                PronouncedName = "Rope Master",
                Pronouns = "they/them",
                IsVetted = true
            },
            new { 
                Email = "teacher@witchcityrope.com", 
                SceneName = "SafetyFirst", 
                Role = "Teacher",
                PronouncedName = "Safety First",
                Pronouns = "she/her",
                IsVetted = true
            },
            new { 
                Email = "vetted@witchcityrope.com", 
                SceneName = "RopeEnthusiast", 
                Role = "Member",
                PronouncedName = "Rope Enthusiast",
                Pronouns = "he/him",
                IsVetted = true
            },
            new { 
                Email = "member@witchcityrope.com", 
                SceneName = "Learning", 
                Role = "Member",
                PronouncedName = "Learning",
                Pronouns = "they/them",
                IsVetted = false
            },
            new { 
                Email = "guest@witchcityrope.com", 
                SceneName = "Newcomer", 
                Role = "Attendee",
                PronouncedName = "Newcomer",
                Pronouns = "she/they",
                IsVetted = false
            }
        };

        var createdCount = 0;
        foreach (var account in testAccounts)
        {
            // Check if user already exists (idempotent operation)
            var existingUser = await _userManager.FindByEmailAsync(account.Email);
            if (existingUser != null)
            {
                _logger.LogDebug("Test account already exists: {Email}", account.Email);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true,
                SceneName = account.SceneName,
                Role = account.Role,
                PronouncedName = account.PronouncedName,
                Pronouns = account.Pronouns,
                IsActive = true,
                IsVetted = account.IsVetted,
                
                // Set required fields with placeholder data for development
                EncryptedLegalName = $"TestUser_{account.SceneName}",
                DateOfBirth = DateTime.UtcNow.AddYears(-25).Date, // Default age 25
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerificationTokenCreatedAt = DateTime.UtcNow,
                
                // CreatedAt/UpdatedAt will be set by ApplicationDbContext.UpdateAuditFields()
            };

            var createResult = await _userManager.CreateAsync(user, "Test123!");
            if (createResult.Succeeded)
            {
                createdCount++;
                _logger.LogInformation("Created test account: {Email} ({Role}, Vetted: {IsVetted})",
                    account.Email, account.Role, account.IsVetted);
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create test account {Email}: {Errors}",
                    account.Email, errors);
                throw new InvalidOperationException($"Failed to create user {account.Email}: {errors}");
            }
        }

        _logger.LogInformation("Test user creation completed. Created: {CreatedCount}, Total Expected: {ExpectedCount}",
            createdCount, testAccounts.Length);
    }

    /// <summary>
    /// Creates sample events for testing event management functionality.
    /// Includes variety of event types, dates, capacities, and pricing scenarios.
    /// 
    /// Events created:
    /// - 10 upcoming events (workshops, classes, meetups) with realistic scheduling
    /// - 2 past events for testing historical data scenarios
    /// - Proper UTC DateTime handling following ApplicationDbContext patterns
    /// - Realistic capacity, pricing, and location information
    /// 
    /// All events follow existing Event entity structure and database schema.
    /// </summary>
    public async Task SeedEventsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sample event creation");

        // Check if events already exist (idempotent operation)
        var existingEventCount = await _context.Events.CountAsync(cancellationToken);
        if (existingEventCount > 0)
        {
            _logger.LogInformation("Events already exist ({Count}), skipping event seeding", existingEventCount);
            return;
        }

        // Create diverse set of events per functional specification
        var sampleEvents = new[]
        {
            // Upcoming Events (10 events) - Only Class and Social types
            CreateSeedEvent("Introduction to Rope Safety", 7, 18, 20, EventType.Class, 25.00m,
                "Learn the fundamentals of safe rope bondage practices in this comprehensive beginner workshop."),
            
            CreateSeedEvent("Single Column Tie Techniques", 14, 19, 15, EventType.Class, 45.00m,
                "Master the art of single column ties with hands-on practice and personalized instruction."),
            
            CreateSeedEvent("Suspension Basics", 21, 18, 12, EventType.Class, 65.00m,
                "Introduction to suspension techniques with emphasis on safety and proper rigging."),
            
            CreateSeedEvent("Rope Maintenance & Care", 28, 17, 25, EventType.Class, 20.00m,
                "Learn how to properly maintain, clean, and store your rope for longevity and safety."),
            
            CreateSeedEvent("Community Rope Jam", 35, 19, 30, EventType.Social, 15.00m,
                "Casual practice session for all skill levels. Bring your rope and practice with the community."),
            
            CreateSeedEvent("Advanced Floor Work", 42, 18, 10, EventType.Class, 55.00m,
                "Explore complex floor-based rope bondage techniques for experienced practitioners."),
            
            CreateSeedEvent("Rope and Sensation Play", 49, 20, 8, EventType.Class, 50.00m,
                "Combine rope techniques with sensation play for enhanced experiences."),
            
            CreateSeedEvent("Predicament Bondage Workshop", 56, 18, 12, EventType.Class, 60.00m,
                "Learn to create challenging and engaging predicament scenarios with rope."),
            
            CreateSeedEvent("Photography and Rope", 63, 16, 6, EventType.Class, 35.00m,
                "Explore the artistic intersection of rope bondage and photography."),
            
            CreateSeedEvent("Rope Social & Discussion", 70, 19, 40, EventType.Social, 10.00m,
                "Monthly social gathering for community connection and discussion of rope topics."),
            
            // Past Events (2 events) for testing historical data
            CreateSeedEvent("Beginner Rope Circle", -7, 18, 20, EventType.Social, 10.00m,
                "Past event: Introductory session for newcomers to rope bondage."),
            
            CreateSeedEvent("Rope Fundamentals Series", -14, 17, 15, EventType.Class, 40.00m,
                "Past event: Multi-session fundamentals course for serious students.")
        };

        await _context.Events.AddRangeAsync(sampleEvents, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sample event creation completed. Created: {EventCount} events", sampleEvents.Length);
    }

    /// <summary>
    /// Populates vetting status configuration for development workflows.
    /// Creates baseline vetting status data needed for user management testing.
    /// 
    /// This method is currently a placeholder as the vetting status structure
    /// is not fully defined in the current schema. Implementation will be
    /// expanded when vetting status entities are added to the data model.
    /// </summary>
    public async Task SeedVettingStatusesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vetting status configuration");
        
        // Placeholder implementation - vetting status configuration
        // will be implemented when vetting status entities are added to schema
        _logger.LogInformation("Vetting status seeding completed (placeholder implementation)");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if seed data population is required by examining existing data.
    /// Implements idempotent behavior to avoid unnecessary seeding operations.
    /// 
    /// Considers database populated if it contains both users and events,
    /// allowing for safe re-execution of seeding operations.
    /// </summary>
    public async Task<bool> IsSeedDataRequiredAsync(CancellationToken cancellationToken = default)
    {
        var userCount = await _userManager.Users.CountAsync(cancellationToken);
        var eventCount = await _context.Events.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0;
        
        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, Required={IsRequired}",
            userCount, eventCount, isRequired);
        
        return isRequired;
    }

    /// <summary>
    /// Helper method to create sample events with proper UTC DateTime handling.
    /// Follows ApplicationDbContext patterns for UTC date storage and audit fields.
    /// 
    /// Creates realistic event data with proper scheduling, capacity, and pricing information.
    /// </summary>
    private Event CreateSeedEvent(string title, int daysFromNow, int startHour, int capacity, 
        EventType eventType, decimal price, string description)
    {
        // Calculate UTC dates following ApplicationDbContext patterns
        var startDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(startHour);
        var endDate = startDate.AddHours(eventType == EventType.Social ? 2 : 3); // Social events 2hrs, classes 3hrs

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Capacity = capacity,
            EventType = eventType.ToString(),
            Location = eventType == EventType.Social ? "Community Space" : "Main Workshop Room",
            IsPublished = true,
            PricingTiers = FormatPricingTiers(price, eventType),
            // CreatedAt/UpdatedAt will be set by ApplicationDbContext.UpdateAuditFields()
        };
    }

    /// <summary>
    /// Formats pricing information based on event type and sliding scale policies.
    /// Reflects the organization's progressive pricing model for accessibility.
    /// </summary>
    private string FormatPricingTiers(decimal basePrice, EventType eventType)
    {
        if (basePrice == 0)
        {
            return "Free";
        }

        var slidingMin = Math.Round(basePrice * 0.25m, 2); // 75% discount maximum
        var slidingMax = basePrice;

        return eventType == EventType.Social 
            ? $"${slidingMin:F0}-${slidingMax:F0} (pay what you can)"
            : $"${slidingMin:F0}-${slidingMax:F0} (sliding scale)";
    }
}

