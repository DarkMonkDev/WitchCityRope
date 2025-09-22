using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;

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
/// - 8 sample events (6 upcoming, 2 past) with 3 classes and 3 social events
/// - Sessions for each event with varied scenarios
/// - Ticket types with varied pricing models (sliding scale, early bird, full event packages)
/// - Sample ticket purchases and RSVPs with realistic payment data
/// - EventParticipation records for proper RSVP/ticket testing
/// - Volunteer positions demonstrating event management functionality
/// - Vetting status configuration for development workflows
/// 
/// Implementation follows existing service layer patterns and coding standards.
/// </summary>
public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
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
            await SeedRolesAsync(cancellationToken);
            await SeedUsersAsync(cancellationToken);
            await SeedEventsAsync(cancellationToken);
            await SeedSessionsAndTicketsAsync(cancellationToken);
            await SeedTicketPurchasesAsync(cancellationToken);
            await SeedEventParticipationsAsync(cancellationToken);
            await SeedVolunteerPositionsAsync(cancellationToken);
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
    /// Creates all required ASP.NET Core Identity roles for the application.
    /// Ensures roles exist before user creation and assignment.
    ///
    /// Roles created:
    /// - Administrator: Full system access
    /// - Teacher: Can create and manage events
    /// - Member: Standard member access
    /// - Attendee: Basic event attendance access
    /// </summary>
    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting role creation");

        var roles = new[] { "Administrator", "Teacher", "Member", "Attendee" };
        var createdCount = 0;

        foreach (var roleName in roles)
        {
            // Check if role already exists (idempotent operation)
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                _logger.LogDebug("Role already exists: {RoleName}", roleName);
                continue;
            }

            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                createdCount++;
                _logger.LogInformation("Created role: {RoleName}", roleName);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create role {RoleName}: {Errors}", roleName, errors);
                throw new InvalidOperationException($"Failed to create role {roleName}: {errors}");
            }
        }

        _logger.LogInformation("Role creation completed. Created: {CreatedCount}, Total Expected: {ExpectedCount}",
            createdCount, roles.Length);
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
                // Assign user to role
                var roleResult = await _userManager.AddToRoleAsync(user, account.Role);
                if (roleResult.Succeeded)
                {
                    createdCount++;
                    _logger.LogInformation("Created test account: {Email} ({Role}, Vetted: {IsVetted})",
                        account.Email, account.Role, account.IsVetted);
                }
                else
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to assign role {Role} to user {Email}: {Errors}",
                        account.Role, account.Email, roleErrors);
                }
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
    /// - 3 upcoming classes (Introduction to Rope Safety, Suspension Basics, Advanced Floor Work)
    /// - 3 upcoming social events (Community Rope Jam, Rope Social & Discussion, New Members Meetup)
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
            // Upcoming Events (3 classes and 3 social events)
            CreateSeedEvent("Introduction to Rope Safety", 7, 18, 20, EventType.Class, 25.00m,
                "Learn the fundamentals of safe rope bondage practices in this comprehensive beginner workshop."),

            CreateSeedEvent("Suspension Basics", 14, 18, 12, EventType.Class, 65.00m,
                "Introduction to suspension techniques with emphasis on safety and proper rigging."),

            CreateSeedEvent("Advanced Floor Work", 21, 18, 10, EventType.Class, 55.00m,
                "Explore complex floor-based rope bondage techniques for experienced practitioners."),

            CreateSeedEvent("Community Rope Jam", 28, 19, 40, EventType.Social, 15.00m,
                "Casual practice session for all skill levels. Bring your rope and practice with the community."),

            CreateSeedEvent("Rope Social & Discussion", 35, 19, 30, EventType.Social, 10.00m,
                "Monthly social gathering for community connection and discussion of rope topics."),

            CreateSeedEvent("New Members Meetup", 42, 18, 25, EventType.Social, 5.00m,
                "Welcome gathering for new community members to meet established practitioners and learn about upcoming events."),

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
    /// Creates sessions and ticket types for each event
    /// Includes variety of scenarios: single-session events, multi-day events, different pricing models
    /// </summary>
    public async Task SeedSessionsAndTicketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sessions and ticket types creation");

        // Check if sessions already exist (idempotent operation)
        var existingSessionCount = await _context.Sessions.CountAsync(cancellationToken);
        if (existingSessionCount > 0)
        {
            _logger.LogInformation("Sessions already exist ({Count}), skipping session and ticket seeding", existingSessionCount);
            return;
        }

        var events = await _context.Events.ToListAsync(cancellationToken);
        var sessionsToAdd = new List<Session>();
        var ticketTypesToAdd = new List<TicketType>();

        foreach (var eventItem in events)
        {
            if (eventItem.Title.Contains("Suspension Intensive") || eventItem.Title.Contains("Conference"))
            {
                // Multi-day events (2-3 days)
                var numberOfDays = eventItem.Title.Contains("Conference") ? 3 : 2;
                AddMultiDayEvent(eventItem, numberOfDays, sessionsToAdd, ticketTypesToAdd);
            }
            else
            {
                // Single-day events (most events)
                AddSingleDayEvent(eventItem, sessionsToAdd, ticketTypesToAdd);
            }
        }

        await _context.Sessions.AddRangeAsync(sessionsToAdd, cancellationToken);
        await _context.TicketTypes.AddRangeAsync(ticketTypesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sessions and ticket types creation completed. Created: {SessionCount} sessions, {TicketCount} ticket types", 
            sessionsToAdd.Count, ticketTypesToAdd.Count);
    }

    /// <summary>
    /// Creates sample ticket purchases for realistic data testing
    /// Includes mix of completed purchases, pending payments, and free RSVPs
    /// </summary>
    public async Task SeedTicketPurchasesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ticket purchases creation");

        // Check if purchases already exist (idempotent operation)
        var existingPurchaseCount = await _context.TicketPurchases.CountAsync(cancellationToken);
        if (existingPurchaseCount > 0)
        {
            _logger.LogInformation("Ticket purchases already exist ({Count}), skipping purchase seeding", existingPurchaseCount);
            return;
        }

        var ticketTypes = await _context.TicketTypes
            .Include(t => t.Event)
            .ToListAsync(cancellationToken);
        
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var purchasesToAdd = new List<TicketPurchase>();

        // Create realistic purchase data
        foreach (var ticketType in ticketTypes)
        {
            var purchaseCount = Math.Min(ticketType.Sold, users.Count);
            
            for (int i = 0; i < purchaseCount; i++)
            {
                var user = users[i % users.Count];
                var isRSVP = ticketType.IsRsvpMode;
                
                var purchase = new TicketPurchase
                {
                    TicketTypeId = ticketType.Id,
                    UserId = user.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Quantity = 1,
                    TotalPrice = isRSVP ? 0 : ticketType.Price * (0.5m + (decimal)Random.Shared.NextDouble() * 0.5m), // Sliding scale pricing
                    PaymentStatus = isRSVP ? "Completed" : GetRandomPaymentStatus(),
                    PaymentMethod = isRSVP ? "RSVP" : GetRandomPaymentMethod(),
                    PaymentReference = Guid.NewGuid().ToString("N")[..8],
                    Notes = GetRandomPurchaseNotes()
                };

                purchasesToAdd.Add(purchase);
            }
        }

        await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket purchases creation completed. Created: {PurchaseCount} purchases", purchasesToAdd.Count);
    }

    /// <summary>
    /// Creates EventParticipation records for RSVPs and ticket purchases
    /// This provides proper data for testing RSVP/ticket functionality
    /// </summary>
    public async Task SeedEventParticipationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event participations creation");

        // Check if participations already exist (idempotent operation)
        var existingParticipationCount = await _context.EventParticipations.CountAsync(cancellationToken);
        if (existingParticipationCount > 0)
        {
            _logger.LogInformation("Event participations already exist ({Count}), skipping participation seeding", existingParticipationCount);
            return;
        }

        var events = await _context.Events.ToListAsync(cancellationToken);
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var participationsToAdd = new List<EventParticipation>();

        foreach (var eventItem in events)
        {
            if (eventItem.EventType == "Social")
            {
                // Social events: Create RSVPs for multiple users
                var rsvpCount = eventItem.Title.Contains("Community Rope Jam") ? 5 :
                               eventItem.Title.Contains("New Members Meetup") ? 8 :
                               eventItem.Title.Contains("Rope Social & Discussion") ? 6 : 3;

                for (int i = 0; i < Math.Min(rsvpCount, users.Count); i++)
                {
                    var user = users[i];
                    var participation = new EventParticipation(eventItem.Id, user.Id, ParticipationType.RSVP)
                    {
                        Id = Guid.NewGuid(),
                        Status = ParticipationStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
                        Notes = i == 0 ? "Looking forward to this event!" : null
                    };
                    participationsToAdd.Add(participation);
                }
            }
            else // Class events
            {
                // Class events: Create ticket purchases for multiple users
                var ticketCount = eventItem.Title.Contains("Introduction to Rope Safety") ? 5 :
                                 eventItem.Title.Contains("Suspension Basics") ? 4 :
                                 eventItem.Title.Contains("Advanced Floor Work") ? 3 : 2;

                for (int i = 0; i < Math.Min(ticketCount, users.Count); i++)
                {
                    var user = users[i];
                    var participation = new EventParticipation(eventItem.Id, user.Id, ParticipationType.Ticket)
                    {
                        Id = Guid.NewGuid(),
                        Status = ParticipationStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        Metadata = $"{{\"purchaseAmount\": {(decimal)Random.Shared.Next(15, 65)}, \"paymentMethod\": \"PayPal\"}}"
                    };
                    participationsToAdd.Add(participation);
                }
            }
        }

        await _context.EventParticipations.AddRangeAsync(participationsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Event participations creation completed. Created: {ParticipationCount} participations", participationsToAdd.Count);
    }

    /// <summary>
    /// Creates volunteer positions for events to demonstrate volunteer management functionality
    /// </summary>
    public async Task SeedVolunteerPositionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting volunteer positions creation");

        // Check if volunteer positions already exist (idempotent operation)
        var existingVolunteerCount = await _context.VolunteerPositions.CountAsync(cancellationToken);
        if (existingVolunteerCount > 0)
        {
            _logger.LogInformation("Volunteer positions already exist ({Count}), skipping volunteer seeding", existingVolunteerCount);
            return;
        }

        var events = await _context.Events
            .Include(e => e.Sessions)
            .ToListAsync(cancellationToken);
        
        var volunteerPositionsToAdd = new List<VolunteerPosition>();

        foreach (var eventItem in events)
        {
            // Add event-wide volunteer positions
            var eventPositions = CreateEventVolunteerPositions(eventItem);
            volunteerPositionsToAdd.AddRange(eventPositions);

            // Add session-specific volunteer positions for some events
            if (eventItem.Sessions.Any() && eventItem.EventType == "Class")
            {
                foreach (var session in eventItem.Sessions)
                {
                    var sessionPositions = CreateSessionVolunteerPositions(eventItem, session);
                    volunteerPositionsToAdd.AddRange(sessionPositions);
                }
            }
        }

        await _context.VolunteerPositions.AddRangeAsync(volunteerPositionsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Volunteer positions creation completed. Created: {VolunteerCount} positions", volunteerPositionsToAdd.Count);
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

    /// <summary>
    /// Helper method to add sessions and tickets for single-day events
    /// Most events are single-day with one session
    /// </summary>
    private void AddSingleDayEvent(Event eventItem, List<Session> sessionsToAdd, List<TicketType> ticketTypesToAdd)
    {
        var session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.EndDate,
            Capacity = eventItem.Capacity,
            CurrentAttendees = eventItem.GetCurrentAttendeeCount()
        };

        sessionsToAdd.Add(session);

        // Add ticket types based on event type
        if (eventItem.EventType == "Social")
        {
            // Social events: Free RSVP + optional donation ticket
            var rsvpTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Free RSVP",
                Description = "Free attendance - RSVP required",
                Price = 0,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentRSVPCount(),
                IsRsvpMode = true
            };

            var donationTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Support Donation",
                Description = "Optional donation to support the community",
                Price = ParsePrice(eventItem.PricingTiers),
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentTicketCount(),
                IsRsvpMode = false
            };

            ticketTypesToAdd.Add(rsvpTicket);
            ticketTypesToAdd.Add(donationTicket);
        }
        else // Class
        {
            // Class events: Regular ticket only
            var regularTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Regular",
                Description = "Full access to the workshop",
                Price = ParsePrice(eventItem.PricingTiers),
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
                IsRsvpMode = false
            };

            ticketTypesToAdd.Add(regularTicket);
        }
    }

    /// <summary>
    /// Helper method to add sessions and tickets for multi-day events
    /// Creates individual day sessions plus discounted full-event tickets
    /// </summary>
    private void AddMultiDayEvent(Event eventItem, int numberOfDays, List<Session> sessionsToAdd, List<TicketType> ticketTypesToAdd)
    {
        var basePrice = ParsePrice(eventItem.PricingTiers);
        var dailyPrice = Math.Round(basePrice * 0.6m, 2); // Individual day is 60% of full price
        var capacityPerDay = (int)Math.Ceiling(eventItem.Capacity / (double)numberOfDays);

        // Create sessions for each day
        var sessions = new List<Session>();
        for (int day = 0; day < numberOfDays; day++)
        {
            var dayStart = eventItem.StartDate.AddDays(day);
            var dayEnd = dayStart.AddHours(eventItem.EndDate.Hour - eventItem.StartDate.Hour);

            var session = new Session
            {
                EventId = eventItem.Id,
                SessionCode = $"S{day + 1}",
                Name = $"Day {day + 1}",
                StartTime = dayStart,
                EndTime = dayEnd,
                Capacity = capacityPerDay,
                CurrentAttendees = (int)(capacityPerDay * 0.7) // 70% filled on average
            };

            sessions.Add(session);
            sessionsToAdd.Add(session);
        }

        // Create ticket types - Individual day tickets
        for (int day = 0; day < numberOfDays; day++)
        {
            var dayTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = sessions[day].Id,
                Name = $"Day {day + 1} Only",
                Description = $"Access to Day {day + 1} activities only",
                Price = dailyPrice,
                Available = capacityPerDay,
                Sold = (int)(capacityPerDay * 0.5), // 50% sold for individual days
                IsRsvpMode = false
            };

            ticketTypesToAdd.Add(dayTicket);
        }

        // Full event ticket with discount
        var fullEventTicket = new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null, // Multi-session ticket
            Name = $"All {numberOfDays} Days",
            Description = $"Full access to all {numberOfDays} days - SAVE ${(dailyPrice * numberOfDays - basePrice):F0}!",
            Price = basePrice,
            Available = eventItem.Capacity,
            Sold = eventItem.GetCurrentAttendeeCount(),
            IsRsvpMode = false
        };

        ticketTypesToAdd.Add(fullEventTicket);
    }

    /// <summary>
    /// Creates volunteer positions for an event
    /// </summary>
    private List<VolunteerPosition> CreateEventVolunteerPositions(Event eventItem)
    {
        var positions = new List<VolunteerPosition>();

        // Common volunteer positions for all events
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            Title = "Door Monitor",
            Description = "Check attendees in, verify tickets/RSVPs, and welcome newcomers",
            SlotsNeeded = 2,
            SlotsFilled = Random.Shared.Next(0, 3),
            RequiresExperience = false,
            Requirements = "Friendly demeanor, punctuality"
        });

        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            Title = "Setup/Cleanup Crew",
            Description = "Help set up equipment before the event and clean up afterwards",
            SlotsNeeded = 3,
            SlotsFilled = Random.Shared.Next(1, 4),
            RequiresExperience = false,
            Requirements = "Physical ability to lift equipment"
        });

        // Additional positions for classes
        if (eventItem.EventType == "Class")
        {
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                Title = "Teaching Assistant",
                Description = "Help instructor with demonstrations and assist students",
                SlotsNeeded = 1,
                SlotsFilled = Random.Shared.Next(0, 2),
                RequiresExperience = true,
                Requirements = "Intermediate+ rope skills, teaching experience preferred"
            });
        }

        return positions;
    }

    /// <summary>
    /// Creates session-specific volunteer positions
    /// </summary>
    private List<VolunteerPosition> CreateSessionVolunteerPositions(Event eventItem, Session session)
    {
        var positions = new List<VolunteerPosition>();

        // Session-specific positions only for multi-day events
        if (session.SessionCode != "S1" || session.Name.Contains("Day"))
        {
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = $"Session Monitor - {session.Name}",
                Description = $"Monitor safety and assist during {session.Name}",
                SlotsNeeded = 1,
                SlotsFilled = Random.Shared.Next(0, 2),
                RequiresExperience = true,
                Requirements = "Safety knowledge, first aid certified preferred"
            });
        }

        return positions;
    }

    /// <summary>
    /// Helper method to extract price from pricing tiers JSON
    /// </summary>
    private decimal ParsePrice(string pricingTiers)
    {
        // Simple parsing for seed data - extract numeric values from pricing string
        var price = pricingTiers.Replace("$", "").Replace("-", " ").Replace("(", " ").Replace(")", " ");
        var parts = price.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            if (decimal.TryParse(part, out var result) && result > 0)
            {
                return result;
            }
        }

        return 25.00m; // Default price
    }

    /// <summary>
    /// Helper method to get random payment status for realistic purchase data
    /// </summary>
    private string GetRandomPaymentStatus()
    {
        var statuses = new[] { "Completed", "Completed", "Completed", "Pending", "Failed" };
        return statuses[Random.Shared.Next(statuses.Length)];
    }

    /// <summary>
    /// Helper method to get random payment method for realistic purchase data
    /// </summary>
    private string GetRandomPaymentMethod()
    {
        var methods = new[] { "PayPal", "Stripe", "Venmo", "Cash", "Zelle" };
        return methods[Random.Shared.Next(methods.Length)];
    }

    /// <summary>
    /// Helper method to get random purchase notes for realistic data
    /// </summary>
    private string GetRandomPurchaseNotes()
    {
        var notes = new[] 
        { 
            "", "", "", // Most purchases have no notes
            "First time attending!",
            "Vegetarian meal preference",
            "Mobility assistance needed",
            "Paid sliding scale minimum",
            "Group purchase for partners"
        };
        return notes[Random.Shared.Next(notes.Length)];
    }
}

