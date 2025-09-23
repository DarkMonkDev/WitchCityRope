using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;

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
/// - Vetting applications (up to 11 total) with comprehensive status examples
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
            await SeedVettingApplicationsAsync(cancellationToken);
            await SeedVettingEmailTemplatesAsync(cancellationToken);

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
    /// expanded when vetting status entities are added to schema.
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
    /// Creates sample vetting applications for testing the vetting system UI.
    /// Includes variety of application statuses, realistic user data, and proper audit trails.
    ///
    /// Applications created:
    /// - Up to 11 sample applications with different statuses (UnderReview, InterviewApproved, InterviewScheduled, Approved, OnHold, Denied)
    /// - Realistic names, scene names, FetLife handles, and application text
    /// - Proper pronoun representation (she/her, he/him, they/them, etc.)
    /// - Links to existing seeded users where appropriate
    /// - Proper UTC DateTime handling following ApplicationDbContext patterns
    /// - All applications include required email addresses in proper format
    /// - VettingAuditLog entries showing workflow progression for each application
    /// </summary>
    public async Task SeedVettingApplicationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vetting applications creation");

        // Check if vetting applications already exist (idempotent operation)
        var existingApplicationCount = await _context.VettingApplications.CountAsync(cancellationToken);
        if (existingApplicationCount > 0)
        {
            _logger.LogInformation("Vetting applications already exist ({Count}), skipping application seeding", existingApplicationCount);
            return;
        }

        // Get existing users to link applications to real users
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");

        // Ensure we have enough users for the applications we want to create
        if (users.Count < 5 || adminUser == null)
        {
            _logger.LogWarning("Not enough users ({UserCount}) or missing admin user to create diverse vetting applications. Need at least 5 users and admin.", users.Count);
            return;
        }

        // Create diverse set of vetting applications with different statuses
        // Each user can only have one application due to unique constraint
        var sampleApplications = new List<VettingApplication>
        {
            // Application 1: Under Review - recent submission
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[0].Id, // Use first existing user
                SceneName = "RopeNovice",
                RealName = "Alexandra Martinez",
                Email = "alexandra.martinez@email.com",
                FetLifeHandle = "RopeNovice2024",
                Pronouns = "she/her",
                OtherNames = "Alex",
                AboutYourself = @"I'm new to rope bondage but deeply interested in learning about the art and community. I've been reading extensively about rope safety and attending introductory workshops in other cities. I'm drawn to both the aesthetic beauty and the trust-building aspects of rope. I have experience in other BDSM practices and understand the importance of consent, communication, and safety. I'm looking for a supportive community where I can learn from experienced practitioners and contribute positively to the group dynamic.",
                HowFoundUs = "Found your group through FetLife recommendations and online research about rope communities in the area.",
                Status = VettingStatus.UnderReview,
                SubmittedAt = DateTime.UtcNow.AddDays(-3),
                AdminNotes = null
            },

            // Application 2: Interview Approved - ready for scheduling
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[1].Id, // Use second existing user
                SceneName = "KnotLearner",
                RealName = "Jordan Kim",
                Email = "jordan.kim@email.com",
                FetLifeHandle = "KnotLearner_JK",
                Pronouns = "they/them",
                OtherNames = null,
                AboutYourself = @"I've been interested in rope bondage for over a year and have practiced with a partner at home. We've been focusing on safety and basic ties, but I want to expand my knowledge and learn from experienced riggers. I have a background in dance and appreciate the artistic and movement aspects of rope. I'm committed to ongoing education about consent, negotiation, and safety practices. I'm hoping to join a community where I can learn advanced techniques while building meaningful connections with like-minded people.",
                HowFoundUs = "Recommended by a friend who's a member of your community.",
                Status = VettingStatus.InterviewApproved,
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-7),
                AdminNotes = "Good references and thoughtful application. Ready for interview to assess practical knowledge."
            },

            // Application 3: Pending Interview - interview scheduled
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[2].Id, // Use third existing user
                SceneName = "TrustBuilder",
                RealName = "Marcus Johnson",
                Email = "marcus.johnson@email.com",
                FetLifeHandle = "TrustBuilder_MJ",
                Pronouns = "he/him",
                OtherNames = "Marc, MJ",
                AboutYourself = @"I'm a 28-year-old professional who discovered rope bondage through a partner. I've been practicing for six months and am passionate about the psychological and emotional aspects of rope. I have experience as a rigger and understand the responsibility that comes with restraining another person. I've completed online safety courses and practiced extensively with enthusiastic partners. I'm seeking a community where I can continue learning and share experiences with others who value both technical skill and emotional intelligence in rope work.",
                HowFoundUs = "Found your group through local BDSM community recommendations.",
                Status = VettingStatus.PendingInterview,
                SubmittedAt = DateTime.UtcNow.AddDays(-14),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-10),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(3), // Interview scheduled for 3 days from now
                AdminNotes = "Interview scheduled for Saturday 2 PM. Applicant shows good understanding of consent and safety principles."
            },

            // Application 4: Approved - recently approved member
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[3].Id, // Use fourth existing user
                SceneName = "SilkAndSteel",
                RealName = "Sarah Chen",
                Email = "sarah.chen@email.com",
                FetLifeHandle = "SilkAndSteel_SC",
                Pronouns = "she/her",
                OtherNames = "Sarah C.",
                AboutYourself = @"I'm an experienced practitioner with 3+ years of rope bondage experience, both as a rigger and rope bunny. I've taught workshops in my previous city and am looking to join the community here after relocating for work. I have extensive knowledge of safety protocols, medical considerations, and emergency procedures. I'm interested in both the technical aspects of rope work and the community building aspects. I'd love to contribute my knowledge while continuing to learn from other experienced practitioners.",
                HowFoundUs = "Referred by a mutual friend in the rope community.",
                Status = VettingStatus.Approved,
                SubmittedAt = DateTime.UtcNow.AddDays(-21),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-18),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-2),
                AdminNotes = "Excellent references from previous community. Strong technical knowledge and teaching experience. Approved for full membership."
            },

            // Application 5: On Hold - additional information needed
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[4].Id, // Use fifth existing user
                SceneName = "EagerLearner",
                RealName = "Taylor Rodriguez",
                Email = "taylor.rodriguez@email.com",
                FetLifeHandle = "EagerLearner99",
                Pronouns = "she/they",
                OtherNames = "Tay",
                AboutYourself = @"I'm completely new to rope bondage but very eager to learn! I've watched videos online and read some books about safety. I don't have any hands-on experience yet but I'm excited to start learning with experienced people. I understand this is about more than just tying knots - it's about trust, communication, and building relationships. I'm committed to learning and following all safety guidelines.",
                HowFoundUs = "Found your group through Google search for rope bondage communities.",
                Status = VettingStatus.OnHold,
                SubmittedAt = DateTime.UtcNow.AddDays(-12),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-8),
                AdminNotes = "Very enthusiastic but lacks practical experience. Recommended to attend beginner classes and gain basic experience before reapplying. On hold pending completion of safety course."
            }
        };

        // Add additional applications if we have more users to prevent constraint violations
        // Only add more applications if we have users beyond the first 5
        if (users.Count >= 6)
        {
            var additionalApplications = new List<VettingApplication>();

            // Application 6: Denied - doesn't meet community standards
            if (users.Count >= 6)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[5].Id,
                    SceneName = "QuickLearner",
                    RealName = "Jamie Taylor",
                    Email = "jamie.taylor@email.com",
                    FetLifeHandle = "QuickLearner_JT",
                    Pronouns = "he/him",
                    OtherNames = "Jamie T",
                    AboutYourself = @"I've been interested in rope for a few months. I think I learn fast and want to get into the community quickly. I've watched some YouTube videos and practiced on myself. I'm ready to start tying people up and want to find partners to practice with.",
                    HowFoundUs = "Found through social media posts about rope events.",
                    Status = VettingStatus.Denied,
                    SubmittedAt = DateTime.UtcNow.AddDays(-20),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-17),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-3),
                    AdminNotes = "Application denied. Applicant shows poor understanding of consent and safety protocols. Focused on 'tying people up' rather than community and education. Recommended to attend formal education courses before reapplying."
                });
            }

            // Application 7: Recent submission - just submitted
            if (users.Count >= 7)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[6].Id,
                    SceneName = "ThoughtfulRigger",
                    RealName = "Alex Rivera",
                    Email = "alex.rivera@email.com",
                    FetLifeHandle = "ThoughtfulRigger_AR",
                    Pronouns = "they/them",
                    OtherNames = "Riv, AR",
                    AboutYourself = @"I've been practicing rope for about 8 months with a consistent partner. We've focused heavily on safety, communication, and building trust. I've completed several online courses about rope safety and anatomy. I'm particularly interested in the artistic and meditative aspects of rope bondage. I understand that joining a community is about contributing positively and learning from experienced practitioners, not just gaining access to events.",
                    HowFoundUs = "Recommended by a trusted mentor in the rope community who suggested I apply when I felt ready.",
                    Status = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    AdminNotes = null
                });
            }

            // Application 8: Long-term member of another community
            if (users.Count >= 8)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[7].Id,
                    SceneName = "CommunityBuilder",
                    RealName = "Morgan Kim",
                    Email = "morgan.kim@email.com",
                    FetLifeHandle = "CommunityBuilder_MK",
                    Pronouns = "she/her",
                    OtherNames = "Mo",
                    AboutYourself = @"I'm relocating from another city where I was an active member of a rope community for 2+ years. I have experience both as a rigger and rope bunny, and I've helped organize events and workshops. I'm looking for a new community home where I can continue learning and contributing. I understand the importance of vetting processes and community standards, having helped with similar processes in my previous group.",
                    HowFoundUs = "Researched rope communities in the area and was impressed by your group's focus on safety and education.",
                    Status = VettingStatus.InterviewApproved,
                    SubmittedAt = DateTime.UtcNow.AddDays(-8),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-5),
                    AdminNotes = "Strong references from previous community leaders. Ready for interview to discuss integration into local group."
                });
            }

            // Application 9: Nervous but genuine applicant
            if (users.Count >= 9)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[8].Id,
                    SceneName = "NervousNewbie",
                    RealName = "Jordan Martinez",
                    Email = "jordan.martinez@email.com",
                    FetLifeHandle = "NervousNewbie_JM",
                    Pronouns = "he/him",
                    OtherNames = "Jordy",
                    AboutYourself = @"I'm quite nervous about applying, but I've been interested in rope bondage for over a year. I've been reading extensively and watching educational content, but I haven't had practical experience yet. I'm drawn to the trust and communication aspects of rope, and I want to learn in a safe, supportive environment. I understand this is a serious community with high standards, and I'm committed to being a respectful and contributing member.",
                    HowFoundUs = "Found through careful research of rope communities with good safety reputations.",
                    Status = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    AdminNotes = null
                });
            }

            // Application 10: RopeBunny - Someone new to rope looking to learn
            if (users.Count >= 10)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[9].Id,
                    SceneName = "RopeBunny",
                    RealName = "Riley Chen",
                    Email = "ropebunny@example.com",
                    FetLifeHandle = "RopeBunny_RC",
                    Pronouns = "she/her",
                    OtherNames = "Riley",
                    AboutYourself = @"I'm completely new to rope bondage but fascinated by the art form and the trust-building aspects. I've done extensive research online and read about safety practices, but I have no hands-on experience yet. I'm particularly drawn to the rope bunny role and want to learn from experienced riggers in a safe, educational environment. I understand the importance of consent, communication, and gradual skill building. I'm looking for a supportive community where I can learn the fundamentals and build meaningful connections with experienced practitioners.",
                    HowFoundUs = "Discovered through FetLife rope education groups and local BDSM community recommendations.",
                    Status = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    AdminNotes = null
                });
            }

            // Application 11: SafetyFirst - An experienced rigger from another city
            if (users.Count >= 11)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[10].Id,
                    SceneName = "SafetyFirst",
                    RealName = "Sam Rodriguez",
                    Email = "safetyfirst@example.com",
                    FetLifeHandle = "SafetyFirst_SR",
                    Pronouns = "they/them",
                    OtherNames = "Sam",
                    AboutYourself = @"I'm an experienced rigger relocating from Portland, where I was an active member of the rope community for 4+ years. I have extensive experience with both floor work and suspension, and I've completed multiple safety courses including basic first aid and rope-specific emergency procedures. In my previous community, I helped mentor newcomers and occasionally assisted with safety monitoring at events. I prioritize safety above all else in rope work and am committed to ongoing education. I'm seeking a new community home where I can contribute my knowledge while continuing to learn from other experienced practitioners.",
                    HowFoundUs = "Researched rope communities in the Salem area through rope safety forums and FetLife groups.",
                    Status = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    AdminNotes = null
                });
            }

            if (additionalApplications.Any())
            {
                sampleApplications.AddRange(additionalApplications);
            }
        }

        await _context.VettingApplications.AddRangeAsync(sampleApplications, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Create audit log entries that show the workflow progression for each application
        await CreateVettingAuditLogsAsync(sampleApplications, adminUser.Id, cancellationToken);

        _logger.LogInformation("Vetting applications creation completed. Created: {ApplicationCount} applications with audit logs", sampleApplications.Count);
    }

    /// <summary>
    /// Creates default email templates for the vetting system.
    /// Populates the VettingEmailTemplates table with standard notification templates.
    /// </summary>
    public async Task SeedVettingEmailTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vetting email templates seeding");

        // Check if email templates already exist
        var existingTemplateCount = await _context.VettingEmailTemplates.CountAsync(cancellationToken);
        if (existingTemplateCount > 0)
        {
            _logger.LogInformation("Email templates already exist, skipping creation. Found: {Count} templates", existingTemplateCount);
            return;
        }

        // Get admin user for UpdatedBy field
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, cannot seed email templates");
            return;
        }

        var templates = new List<VettingEmailTemplate>
        {
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.ApplicationReceived,
                Subject = "Application Received - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.

Application Number: {{application_number}}
Submission Date: {{submission_date}}

Our vetting team will review your application and contact you within the next 7-10 business days with updates on your status.

If you have any questions, please don't hesitate to contact us.

Best regards,
The WitchCityRope Vetting Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.InterviewApproved,
                Subject = "Interview Approved - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

Congratulations! Your vetting application has been approved for the interview stage.

Application Number: {{application_number}}
Next Steps: Please schedule your interview using the link below
Interview Scheduling: {{interview_link}}

During your interview, we will discuss your experience, interests, and answer any questions you may have about our community.

Please schedule your interview within the next 14 days.

Best regards,
The WitchCityRope Vetting Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.ApplicationApproved,
                Subject = "Welcome to WitchCityRope - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

Congratulations! Your application has been approved and you are now a vetted member of WitchCityRope.

Application Number: {{application_number}}
Approval Date: {{approval_date}}

Welcome to our community! You now have access to:
- All member events and workshops
- Our private community forums
- Advanced classes and demonstrations
- Volunteer opportunities

Your member profile has been activated and you can now register for upcoming events.

Welcome aboard!

Best regards,
The WitchCityRope Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.ApplicationOnHold,
                Subject = "Application On Hold - Additional Information Needed - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

Your vetting application is currently on hold as we need some additional information to proceed.

Application Number: {{application_number}}
Reason: {{hold_reason}}

Required Actions:
{{required_actions}}

Please provide the requested information within 30 days to avoid application expiration.

If you have any questions about what's needed, please contact us.

Best regards,
The WitchCityRope Vetting Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.ApplicationDenied,
                Subject = "Application Status Update - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

Thank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.

Application Number: {{application_number}}
Review Date: {{review_date}}

This decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.

We appreciate your interest in our community.

Best regards,
The WitchCityRope Vetting Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.InterviewReminder,
                Subject = "Interview Reminder - {{applicant_name}}",
                Body = @"Dear {{applicant_name}},

This is a friendly reminder about your upcoming vetting interview.

Application Number: {{application_number}}
Interview Date: {{interview_date}}
Interview Time: {{interview_time}}
Location: {{interview_location}}

If you need to reschedule, please contact us at least 24 hours in advance.

We look forward to meeting with you!

Best regards,
The WitchCityRope Vetting Team",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }
        };

        await _context.VettingEmailTemplates.AddRangeAsync(templates, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} default email templates for vetting system", templates.Count);
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

        // First, create sessions without ticket types
        foreach (var eventItem in events)
        {
            if (eventItem.Title.Contains("Suspension Intensive") || eventItem.Title.Contains("Conference"))
            {
                // Multi-day events (2-3 days)
                var numberOfDays = eventItem.Title.Contains("Conference") ? 3 : 2;
                AddMultiDayEventSessions(eventItem, numberOfDays, sessionsToAdd);
            }
            else
            {
                // Single-day events (most events)
                AddSingleDayEventSession(eventItem, sessionsToAdd);
            }
        }

        // Save sessions first to get their IDs
        await _context.Sessions.AddRangeAsync(sessionsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Now create ticket types with valid session IDs
        var ticketTypesToAdd = new List<TicketType>();

        // Group sessions by event to handle multi-day events
        var sessionsByEvent = sessionsToAdd.GroupBy(s => s.EventId).ToList();

        foreach (var eventGroup in sessionsByEvent)
        {
            var eventItem = events.First(e => e.Id == eventGroup.Key);
            var eventSessions = eventGroup.ToList();

            if (eventSessions.Count > 1)
            {
                // Multi-day event - create both individual day tickets and full event tickets
                CreateMultiDayTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else
            {
                // Single-day event
                CreateTicketTypesForSession(eventItem, eventSessions.First(), ticketTypesToAdd);
            }
        }

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
    /// Considers database populated if it contains users, events, and vetting applications,
    /// allowing for safe re-execution of seeding operations.
    /// </summary>
    public async Task<bool> IsSeedDataRequiredAsync(CancellationToken cancellationToken = default)
    {
        var userCount = await _userManager.Users.CountAsync(cancellationToken);
        var eventCount = await _context.Events.CountAsync(cancellationToken);
        var vettingApplicationCount = await _context.VettingApplications.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0 || vettingApplicationCount == 0;

        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, VettingApplications={VettingApplicationCount}, Required={IsRequired}",
            userCount, eventCount, vettingApplicationCount, isRequired);

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

    /// <summary>
    /// Helper method to add session for single-day events
    /// Creates one session (ticket types added separately)
    /// </summary>
    private void AddSingleDayEventSession(Event eventItem, List<Session> sessionsToAdd)
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
    }

    /// <summary>
    /// Helper method to add sessions for multi-day events
    /// Creates multiple day sessions (ticket types added separately)
    /// </summary>
    private void AddMultiDayEventSessions(Event eventItem, int numberOfDays, List<Session> sessionsToAdd)
    {
        for (int day = 1; day <= numberOfDays; day++)
        {
            var daySession = new Session
            {
                EventId = eventItem.Id,
                SessionCode = $"D{day}",
                Name = $"Day {day}",
                StartTime = eventItem.StartDate.AddDays(day - 1),
                EndTime = eventItem.StartDate.AddDays(day - 1).AddHours(8), // 8 hour sessions
                Capacity = (int)Math.Ceiling(eventItem.Capacity / (double)numberOfDays),
                CurrentAttendees = eventItem.GetCurrentAttendeeCount() / numberOfDays
            };

            sessionsToAdd.Add(daySession);
        }
    }

    /// <summary>
    /// Helper method to create ticket types for a session
    /// Must be called after session has been saved and has a valid ID
    /// </summary>
    private void CreateTicketTypesForSession(Event eventItem, Session session, List<TicketType> ticketTypesToAdd)
    {
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
                Available = session.Capacity,
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
                Available = session.Capacity,
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
                Available = session.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
                IsRsvpMode = false
            };

            ticketTypesToAdd.Add(regularTicket);
        }
    }

    /// <summary>
    /// Helper method to create ticket types for multi-day events
    /// Creates individual day tickets and full event ticket with discount
    /// </summary>
    private void CreateMultiDayTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        var basePrice = ParsePrice(eventItem.PricingTiers);
        var dailyPrice = Math.Round(basePrice * 0.6m, 2); // Individual day is 60% of full price

        // Create individual day tickets
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var dayTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = $"Day {i + 1} Only",
                Description = $"Access to Day {i + 1} activities only",
                Price = dailyPrice,
                Available = session.Capacity,
                Sold = (int)(session.Capacity * 0.3), // 30% sold on average
                IsRsvpMode = false
            };

            ticketTypesToAdd.Add(dayTicket);
        }

        // Create full event ticket with discount
        var fullEventTicket = new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null, // Multi-session ticket
            Name = $"All {sessions.Count} Days",
            Description = $"Full access to all {sessions.Count} days - SAVE ${(dailyPrice * sessions.Count - basePrice):F0}!",
            Price = basePrice,
            Available = eventItem.Capacity,
            Sold = eventItem.GetCurrentAttendeeCount(),
            IsRsvpMode = false
        };

        ticketTypesToAdd.Add(fullEventTicket);
    }

    /// <summary>
    /// Creates audit log entries for vetting applications to show status progression history.
    /// Generates realistic workflow progression for each application based on their current status.
    /// </summary>
    private async Task CreateVettingAuditLogsAsync(List<VettingApplication> applications, Guid adminUserId, CancellationToken cancellationToken)
    {
        var auditLogs = new List<VettingAuditLog>();

        foreach (var application in applications)
        {
            // All applications start with initial submission
            auditLogs.Add(new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Application Submitted",
                PerformedBy = application.UserId,
                PerformedAt = application.SubmittedAt,
                OldValue = null,
                NewValue = "Draft",
                Notes = $"Initial application submitted by {application.SceneName}"
            });

            // Create status progression based on current status
            switch (application.Status)
            {
                case VettingStatus.UnderReview:
                    // Just submitted, only has initial submission
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });
                    break;

                case VettingStatus.InterviewApproved:
                    // Draft → UnderReview → InterviewApproved
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(3),
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = "Application approved for interview stage - good references and thoughtful responses"
                    });
                    break;

                case VettingStatus.PendingInterview:
                    // Draft → UnderReview → InterviewApproved → PendingInterview
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(3),
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = "Application approved for interview stage"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Interview Scheduled",
                        PerformedBy = adminUserId,
                        PerformedAt = application.InterviewScheduledFor?.AddDays(-1) ?? DateTime.UtcNow.AddDays(-1),
                        OldValue = "InterviewApproved",
                        NewValue = "PendingInterview",
                        Notes = $"Interview scheduled for {application.InterviewScheduledFor?.ToString("yyyy-MM-dd HH:mm") ?? "TBD"}"
                    });
                    break;

                case VettingStatus.Approved:
                    // Draft → UnderReview → InterviewApproved → PendingInterview → Approved
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(2),
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = "Application approved for interview stage"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Interview Completed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.DecisionMadeAt?.AddDays(-5) ?? application.SubmittedAt.AddDays(15),
                        OldValue = "InterviewApproved",
                        NewValue = "PendingInterview",
                        Notes = "Interview conducted - excellent candidate with strong community values"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Application Approved",
                        PerformedBy = adminUserId,
                        PerformedAt = application.DecisionMadeAt ?? application.SubmittedAt.AddDays(18),
                        OldValue = "PendingInterview",
                        NewValue = "Approved",
                        Notes = "Application approved for full membership - welcome to the community!"
                    });
                    break;

                case VettingStatus.OnHold:
                    // Draft → UnderReview → OnHold
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(4),
                        OldValue = "UnderReview",
                        NewValue = "OnHold",
                        Notes = "Additional information needed - applicant should complete safety course before proceeding"
                    });
                    break;

                case VettingStatus.Denied:
                    // Draft → UnderReview → Denied
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddHours(1),
                        OldValue = "Draft",
                        NewValue = "UnderReview",
                        Notes = "Application moved to review queue"
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Application Denied",
                        PerformedBy = adminUserId,
                        PerformedAt = application.DecisionMadeAt ?? application.SubmittedAt.AddDays(7),
                        OldValue = "UnderReview",
                        NewValue = "Denied",
                        Notes = "Application denied - insufficient understanding of consent and safety protocols"
                    });
                    break;
            }
        }

        await _context.VettingAuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} vetting audit log entries", auditLogs.Count);
    }
}

