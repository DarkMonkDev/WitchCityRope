using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Cms;
using WitchCityRope.Models;

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
    private readonly WitchCityRope.Api.Features.Safety.Services.IEncryptionService _encryptionService;

    public SeedDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<SeedDataService> logger,
        WitchCityRope.Api.Features.Safety.Services.IEncryptionService encryptionService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _encryptionService = encryptionService;
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
            await SeedSafetyIncidentsAsync(cancellationToken);
            await CmsSeedData.SeedInitialPagesAsync(_context);

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

        var roles = new[] { "Administrator", "Teacher", "SafetyTeam" };
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

        // CRITICAL: Explicitly save changes to ensure roles are committed to database
        // This fixes transaction timing issues where roles are created in one context
        // but not visible to other contexts querying the database (e.g., approval logic).
        // Without this, TestContainers integration tests fail because roles aren't
        // visible when test assertions run.
        await _context.SaveChangesAsync(cancellationToken);

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
        // Expanded to support all vetting application test scenarios
        var testAccounts = new[]
        {
            // Core test accounts
            new {
                Email = "admin@witchcityrope.com",
                SceneName = "RopeMaster",
                DiscordName = "ropemaster_admin",
                Role = "Administrator",
                PronouncedName = "Rope Master",
                Pronouns = "they/them",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "teacher@witchcityrope.com",
                SceneName = "SafetyFirst",
                DiscordName = "safety_teacher",
                Role = "Teacher",
                PronouncedName = "Safety First",
                Pronouns = "she/her",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "vetted@witchcityrope.com",
                SceneName = "RopeEnthusiast",
                DiscordName = "rope_enthusiast",
                Role = "",  // No special permissions
                PronouncedName = "Rope Enthusiast",
                Pronouns = "he/him",
                VettingStatus = 3,  // Approved (vetted)
                IsActive = true
            },
            new {
                Email = "member@witchcityrope.com",
                SceneName = "Learning",
                DiscordName = "learning_rope",
                Role = "",  // No special permissions
                PronouncedName = "Learning",
                Pronouns = "they/them",
                VettingStatus = 0,  // UnderReview (not vetted)
                IsActive = true
            },
            new {
                Email = "guest@witchcityrope.com",
                SceneName = "Newcomer",
                DiscordName = "new_to_rope",
                Role = "",  // No special permissions
                PronouncedName = "Newcomer",
                Pronouns = "she/they",
                VettingStatus = 0,  // UnderReview (not vetted)
                IsActive = true
            },
            // Safety/Incident coordinators for testing incident assignment
            new {
                Email = "coordinator1@witchcityrope.com",
                SceneName = "SafetyCoordinator",
                DiscordName = "safety_coord1",
                Role = "SafetyTeam",
                PronouncedName = "Safety Coordinator",
                Pronouns = "they/them",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "coordinator2@witchcityrope.com",
                SceneName = "IncidentHandler",
                DiscordName = "incident_handler",
                Role = "SafetyTeam",
                PronouncedName = "Incident Handler",
                Pronouns = "she/her",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            // Additional users for vetting application testing (users 5-16)
            new { Email = "applicant1@example.com", SceneName = "RopeNovice", DiscordName = "rope_novice", Role = "", PronouncedName = "Rope Novice", Pronouns = "she/her", VettingStatus = 0, IsActive = false },
            new { Email = "applicant2@example.com", SceneName = "KnotLearner", DiscordName = "knot_learner", Role = "", PronouncedName = "Knot Learner", Pronouns = "they/them", VettingStatus = 0, IsActive = true },
            new { Email = "applicant3@example.com", SceneName = "TrustBuilder", DiscordName = "trust_builder", Role = "Teacher,SafetyTeam", PronouncedName = "Trust Builder", Pronouns = "he/him", VettingStatus = 3, IsActive = true },  // Multiple roles, vetted
            new { Email = "applicant4@example.com", SceneName = "SilkAndSteel", DiscordName = "silk_and_steel", Role = "", PronouncedName = "Silk And Steel", Pronouns = "she/her", VettingStatus = 3, IsActive = true },  // Approved (will be synced from application)
            new { Email = "applicant5@example.com", SceneName = "EagerLearner", DiscordName = "eager_learner", Role = "", PronouncedName = "Eager Learner", Pronouns = "she/they", VettingStatus = 0, IsActive = true },
            new { Email = "applicant6@example.com", SceneName = "QuickLearner", DiscordName = "quick_learner", Role = "", PronouncedName = "Quick Learner", Pronouns = "he/him", VettingStatus = 0, IsActive = true },
            new { Email = "applicant7@example.com", SceneName = "ThoughtfulRigger", DiscordName = "thoughtful_rigger", Role = "Teacher", PronouncedName = "Thoughtful Rigger", Pronouns = "they/them", VettingStatus = 3, IsActive = true },  // Teacher, vetted
            new { Email = "applicant8@example.com", SceneName = "CommunityBuilder", DiscordName = "community_builder", Role = "", PronouncedName = "Community Builder", Pronouns = "she/her", VettingStatus = 0, IsActive = true },
            new { Email = "applicant9@example.com", SceneName = "NervousNewbie", DiscordName = "nervous_newbie", Role = "", PronouncedName = "Nervous Newbie", Pronouns = "he/him", VettingStatus = 0, IsActive = true },
            new { Email = "applicant10@example.com", SceneName = "RopeBunny", DiscordName = "rope_bunny", Role = "", PronouncedName = "Rope Bunny", Pronouns = "she/her", VettingStatus = 0, IsActive = true },
            new { Email = "applicant11@example.com", SceneName = "SafetyConscious", DiscordName = "safety_conscious", Role = "Teacher", PronouncedName = "Safety Conscious", Pronouns = "they/them", VettingStatus = 3, IsActive = true },  // Teacher, vetted
            new { Email = "applicant12@example.com", SceneName = "PatientPractitioner", DiscordName = "patient_practitioner", Role = "Teacher", PronouncedName = "Patient Practitioner", Pronouns = "he/him", VettingStatus = 3, IsActive = true }  // Teacher, vetted
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

            // Try to get IsActive from account, default to true if not specified
            var isActive = true;
            var accountType = account.GetType();
            var isActiveProperty = accountType.GetProperty("IsActive");
            if (isActiveProperty != null)
            {
                isActive = (bool)(isActiveProperty.GetValue(account) ?? true);
            }

            var user = new ApplicationUser
            {
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true,
                SceneName = account.SceneName,
                DiscordName = account.DiscordName,
                Role = account.Role,
                PronouncedName = account.PronouncedName,
                Pronouns = account.Pronouns,
                IsActive = isActive,
                VettingStatus = account.VettingStatus, // 3 = Approved (vetted), 0 = UnderReview (not vetted)

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
                // Assign user to role(s) - handle comma-separated roles
                if (!string.IsNullOrWhiteSpace(account.Role))
                {
                    var roles = account.Role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var roleAssignments = new List<string>();

                    foreach (var role in roles)
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, role);
                        if (roleResult.Succeeded)
                        {
                            roleAssignments.Add(role);
                        }
                        else
                        {
                            var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to assign role {Role} to user {Email}: {Errors}",
                                role, account.Email, roleErrors);
                        }
                    }

                    createdCount++;
                    _logger.LogInformation("Created test account: {Email} (Roles: {Roles}, VettingStatus: {VettingStatus})",
                        account.Email, string.Join(", ", roleAssignments), account.VettingStatus);
                }
                else
                {
                    // User created without a role
                    createdCount++;
                    _logger.LogInformation("Created test account: {Email} (No role, VettingStatus: {VettingStatus})",
                        account.Email, account.VettingStatus);
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

        // CRITICAL: Explicitly save changes to ensure users are committed to database
        // This ensures users created via UserManager are visible to other database contexts
        // immediately after creation, preventing timing issues in tests and dependent operations.
        await _context.SaveChangesAsync(cancellationToken);

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
            CreateSeedEvent(
                title: "Introduction to Rope Safety",
                daysFromNow: 7,
                startHour: 18,
                capacity: 20,
                eventType: EventType.Class,
                price: 25.00m,
                shortDescription: "Learn the fundamentals of safe rope bondage practices in this comprehensive beginner workshop.",
                longDescription: @"<p>This comprehensive introduction to rope safety is designed for absolute beginners and those looking to refresh their fundamental knowledge.</p>

<h3>In this 3-hour workshop, you'll learn:</h3>
<ul>
<li>Essential safety principles and risk awareness for rope bondage</li>
<li>Communication techniques including consent negotiation and safe words</li>
<li>Basic rope handling skills and material selection (jute, hemp, synthetic)</li>
<li>Recognition of nerve compression and circulation issues</li>
<li>Emergency release procedures and safety protocols</li>
<li>How to create a safe practice environment</li>
</ul>

<h3>Prerequisites:</h3>
<p>None - this is a beginner-friendly class designed for people with no prior rope experience.</p>

<h3>Materials:</h3>
<ul>
<li>Two 30-foot lengths of 6mm rope will be provided (jute or hemp)</li>
<li>You may bring your own rope if preferred</li>
<li>Comfortable clothing that allows movement is required</li>
</ul>

<h3>Class Structure:</h3>
<ul>
<li>45 minutes: Safety theory and communication fundamentals</li>
<li>90 minutes: Hands-on practice with instructor guidance</li>
<li>30 minutes: Q&A and resource sharing</li>
<li>15 minutes: Community discussion and next steps</li>
</ul>

<p>This class emphasizes building a strong foundation in safety practices that will serve you throughout your rope journey. We focus on understanding risks, developing good habits, and creating positive experiences for all participants.</p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required (no walk-ins)</li>
<li>Arrive 15 minutes early for check-in and orientation</li>
</ul>

<h3>Consent and Communication:</h3>
<ul>
<li>Enthusiastic consent is required for all activities and interactions</li>
<li>Respect all safe words, boundaries, and limits without question</li>
<li>""No"" means no - immediately stop any activity when requested</li>
<li>Ask before touching others or their belongings</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants</li>
<li>Cell phones must be on silent and stored during class</li>
<li>Violations will result in immediate removal without refund</li>
</ul>

<h3>Appropriate Conduct and Attire:</h3>
<ul>
<li>Professional and respectful behavior expected at all times</li>
<li>Wear comfortable, non-restrictive clothing suitable for movement</li>
<li>Remove jewelry that may interfere with rope work</li>
<li>Closed-toe shoes recommended for safety</li>
</ul>

<h3>Health and Safety:</h3>
<ul>
<li>Inform instructor of any medical conditions, injuries, or mobility limitations</li>
<li>Practice good hygiene - shower before attending</li>
<li>Do not attend if you are ill or contagious</li>
<li>Report any injuries or safety concerns immediately to staff</li>
</ul>

<h3>Scent-Free Environment:</h3>
<ul>
<li>Please avoid strong perfumes, colognes, or scented products</li>
<li>Some community members have chemical sensitivities</li>
</ul>

<h3>Zero Tolerance Policies:</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior of any kind</li>
<li>No drugs or alcohol on premises</li>
<li>Violation of policies may result in permanent ban from all community events</li>
</ul>

<p><strong>By attending, you agree to abide by all policies and accept responsibility for your own safety and well-being.</strong></p>"
            ),

            CreateSeedEvent(
                title: "Suspension Basics",
                daysFromNow: 14,
                startHour: 18,
                capacity: 12,
                eventType: EventType.Class,
                price: 65.00m,
                shortDescription: "Introduction to suspension techniques with emphasis on safety and proper rigging.",
                longDescription: @"<p>Take your rope skills to the next level with this comprehensive introduction to suspension bondage. This intermediate-level workshop covers the essential techniques, safety considerations, and rigging fundamentals needed to begin exploring suspension safely.</p>

<h3>What You'll Learn:</h3>
<ul>
<li>Suspension safety principles and risk mitigation strategies</li>
<li>Proper rigging setup and equipment requirements (hard points, rigging plates, carabiners)</li>
<li>Load-bearing tie techniques and weight distribution</li>
<li>Hip harness construction with emphasis on safety and comfort</li>
<li>Partial suspension techniques for beginners</li>
<li>How to assess and monitor your partner during suspension</li>
<li>Safe descent and emergency procedures</li>
<li>Common mistakes and how to avoid them</li>
</ul>

<h3>Prerequisites:</h3>
<ul>
<li><strong>REQUIRED:</strong> Strong foundation in floor bondage techniques</li>
<li><strong>REQUIRED:</strong> Completion of 'Introduction to Rope Safety' or equivalent</li>
<li><strong>Recommended:</strong> 6+ months of regular rope practice</li>
<li>You must be comfortable tying basic harnesses and understand rope safety fundamentals</li>
</ul>

<h3>Equipment Provided:</h3>
<ul>
<li>Suspension rigging equipment (hard points, carabiners, etc.)</li>
<li>Practice rope if needed</li>
</ul>

<h3>What to Bring:</h3>
<ul>
<li>4-6 lengths of rope (30 feet each, 6mm diameter)</li>
<li>Comfortable, form-fitting clothing (leggings, fitted shirt)</li>
<li>Water bottle and snacks</li>
<li>Notebook for taking notes (optional)</li>
</ul>

<h3>Class Format:</h3>
<ul>
<li>30 minutes: Suspension theory, safety review, and equipment overview</li>
<li>2 hours: Hands-on practice with instructor supervision</li>
<li>30 minutes: Q&A, troubleshooting, and safety discussion</li>
</ul>

<p><strong>Safety Note:</strong> Suspension carries inherent risks including nerve damage, circulation issues, and injury from falls. This class emphasizes conservative, safety-first approaches suitable for beginners. You will learn to recognize warning signs and respond appropriately.</p>

<p><em>Limited to 12 participants to ensure personalized instruction and adequate safety supervision.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required (no walk-ins)</li>
<li>Prerequisite: Completion of rope safety fundamentals course or instructor approval</li>
<li>Arrive 15 minutes early for safety briefing and equipment setup</li>
</ul>

<h3>Medical and Physical Requirements:</h3>
<ul>
<li>You must disclose any medical conditions, injuries, or physical limitations to instructor before class</li>
<li>Certain conditions may prevent safe participation in suspension activities</li>
<li>You are responsible for knowing your own health status and limitations</li>
<li>Sign medical waiver acknowledging suspension risks before participating</li>
</ul>

<h3>Consent and Communication:</h3>
<ul>
<li>Enthusiastic consent required for all activities and physical contact</li>
<li>Active communication expected throughout suspension scenes</li>
<li>Immediate stop on any safe word or request to stop</li>
<li>Check-ins required at regular intervals during suspension</li>
</ul>

<h3>Equipment and Safety:</h3>
<ul>
<li>Do not touch or adjust rigging equipment without instructor permission</li>
<li>Inspect all equipment before use and report any damage or concerns</li>
<li>Follow all instructor directions regarding equipment usage</li>
<li>Spotter required for all suspension activities</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants and instructor</li>
<li>Cell phones must be on silent and stored during class</li>
</ul>

<h3>Appropriate Conduct:</h3>
<ul>
<li>Professional behavior required at all times</li>
<li>Wear appropriate athletic/practice clothing (no loose garments)</li>
<li>Remove all jewelry before participating in suspension</li>
<li>Notify instructor immediately of any discomfort, numbness, tingling, or other concerns</li>
</ul>

<h3>Zero Tolerance:</h3>
<ul>
<li>No harassment, discrimination, or boundary violations</li>
<li>No drugs or alcohol - immediate removal and ban</li>
<li>Suspension is an advanced skill requiring focus and sobriety</li>
</ul>

<h3>Risk Acknowledgment:</h3>
<ul>
<li>Suspension bondage carries inherent risks including nerve damage, circulation issues, rope burns, bruising, and injury from falls</li>
<li>By attending, you acknowledge these risks and accept responsibility for your participation</li>
<li>You agree to follow all safety protocols and instructor guidance</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Advanced Floor Work",
                daysFromNow: 21,
                startHour: 18,
                capacity: 10,
                eventType: EventType.Class,
                price: 55.00m,
                shortDescription: "Explore complex floor-based rope bondage techniques for experienced practitioners.",
                longDescription: @"<p>This advanced workshop is designed for experienced rope practitioners looking to expand their floor bondage repertoire with complex ties, creative positions, and artistic expression. We'll explore sophisticated techniques that challenge both rigger and model while maintaining safety and comfort.</p>

<h3>Workshop Topics:</h3>
<ul>
<li>Complex multi-point ties and connection techniques</li>
<li>Asymmetrical and artistic floor positions</li>
<li>Creative use of space and body positioning</li>
<li>Transitions between positions while maintaining tie integrity</li>
<li>Strappado, hogtie, and other challenging positions</li>
<li>Decorative elements and aesthetic considerations</li>
<li>Managing longer scenes and endurance considerations</li>
<li>Troubleshooting common issues with complex ties</li>
</ul>

<h3>Prerequisites:</h3>
<ul>
<li><strong>REQUIRED:</strong> Extensive floor bondage experience (1+ years regular practice)</li>
<li><strong>REQUIRED:</strong> Strong foundation in safety, communication, and basic ties</li>
<li>Must be proficient in single column, double column, chest harnesses, and hip harnesses</li>
<li>Experience with multi-limb ties and transitions</li>
<li><strong>This is NOT a beginner class</strong></li>
</ul>

<h3>What to Bring:</h3>
<ul>
<li>6-8 lengths of rope (30 feet each, 6mm diameter) - quality rope recommended</li>
<li>Comfortable form-fitting practice clothing</li>
<li>Knee pads optional but recommended</li>
<li>Water and snacks</li>
<li>Notebook for notes and sketches</li>
</ul>

<h3>Class Structure:</h3>
<ul>
<li>20 minutes: Demonstration of key concepts and techniques</li>
<li>2 hours: Hands-on practice with instructor feedback</li>
<li>20 minutes: Sharing, feedback, and Q&A</li>
</ul>

<h3>Teaching Philosophy:</h3>
<p>This class emphasizes creative exploration within safe parameters. We'll focus on developing your personal style while respecting the fundamentals that keep everyone safe. Expect to be challenged and to learn from both successes and mistakes.</p>

<p><em>Small class size (limited to 10 participants) ensures individualized attention and the ability to work at your own pace.</em></p>

<p><strong>Note:</strong> Partners/models welcome but not required. Solo practitioners can focus on learning tie techniques and theory.</p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required</li>
<li>Prerequisite: Extensive rope experience and instructor approval</li>
<li>Late arrival may result in denied entry for safety reasons</li>
</ul>

<h3>Experience Verification:</h3>
<ul>
<li>This is an advanced class - beginners will not be admitted</li>
<li>Instructor reserves right to assess skill level and decline participation</li>
<li>If your experience level is uncertain, contact instructor before registering</li>
</ul>

<h3>Safety and Communication:</h3>
<ul>
<li>Expert-level communication and consent skills expected</li>
<li>Proactive monitoring of partner required throughout scenes</li>
<li>Immediate stop on any safe word, numbness, tingling, or discomfort</li>
<li>Report all safety concerns to instructor immediately</li>
</ul>

<h3>Equipment Requirements:</h3>
<ul>
<li>Bring your own quality rope in good condition</li>
<li>Inspect rope before use and remove any damaged rope from rotation</li>
<li>Recommended: natural fiber rope (jute/hemp) for better grip and control</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants</li>
<li>This is a closed, private workshop</li>
<li>Cell phones must be stored during practice sessions</li>
</ul>

<h3>Professional Conduct:</h3>
<ul>
<li>Respectful, professional behavior required at all times</li>
<li>Appropriate practice attire (form-fitting, allows movement)</li>
<li>Practice good hygiene</li>
<li>No unsolicited advice or critique of other participants</li>
</ul>

<h3>Health and Disclosure:</h3>
<ul>
<li>Disclose relevant medical conditions, injuries, or limitations</li>
<li>You are responsible for your own physical and mental well-being</li>
<li>Take breaks as needed</li>
<li>Stay hydrated</li>
</ul>

<h3>Zero Tolerance Policies:</h3>
<ul>
<li>No harassment, boundary violations, or predatory behavior</li>
<li>No drugs or alcohol</li>
<li>Violations result in immediate removal and permanent ban</li>
</ul>

<h3>Assumption of Risk:</h3>
<ul>
<li>Advanced rope work carries increased risk of injury</li>
<li>By participating, you acknowledge these risks and accept responsibility</li>
<li>You agree to practice safely and follow all instructor guidance</li>
<li>Nerve damage, bruising, rope burns, and other injuries are possible</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Community Rope Jam",
                daysFromNow: 28,
                startHour: 19,
                capacity: 40,
                eventType: EventType.Social,
                price: 15.00m,
                shortDescription: "Casual practice session for all skill levels. Bring your rope and practice with the community.",
                longDescription: @"<p>Join us for our monthly Community Rope Jam - a relaxed, social practice session where rope enthusiasts of all skill levels can come together to practice, learn, and connect with fellow community members.</p>

<h3>What to Expect:</h3>
<ul>
<li>Open practice space with supportive atmosphere</li>
<li>Practice your existing skills or try new techniques</li>
<li>Ask questions and learn from experienced practitioners</li>
<li>Make new friends and connections in the community</li>
<li>Casual, judgment-free environment</li>
</ul>

<h3>Who Should Attend:</h3>
<ul>
<li><strong>ALL SKILL LEVELS</strong> welcome from absolute beginners to advanced practitioners</li>
<li>Solo practitioners welcome - you don't need to bring a partner</li>
<li>People interested in learning more about rope bondage</li>
<li>Experienced rope artists looking to practice and share knowledge</li>
<li>Anyone seeking community connection</li>
</ul>

<h3>Facilitators Present:</h3>
<ul>
<li>Experienced community members available to answer questions</li>
<li>Safety monitors on site</li>
<li>Not a formal class, but informal guidance available</li>
</ul>

<h3>What to Bring:</h3>
<ul>
<li>Your own rope (if you have it) - we'll have some available to borrow</li>
<li>Comfortable clothing suitable for movement</li>
<li>Water bottle</li>
<li>Positive attitude and respect for all participants</li>
</ul>

<h3>Activities:</h3>
<ul>
<li>Self-directed practice at your own pace</li>
<li>Informal peer learning and knowledge sharing</li>
<li>Socializing and community building</li>
<li>Optional: Share what you're working on with supportive feedback</li>
</ul>

<h3>Perfect For:</h3>
<ul>
<li>Practicing ties you've learned in classes</li>
<li>Experimenting with new ideas in a safe environment</li>
<li>Meeting practice partners</li>
<li>Building confidence in your rope skills</li>
<li>Connecting with the local rope community</li>
</ul>

<p>No pressure, no judgment - just a welcoming space to explore rope at your own comfort level. Whether you tie for 5 minutes or the whole session, you're welcome here.</p>

<p><em>Optional donation-based entry - pay what you can to help cover venue costs.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP required for space planning</li>
<li>Sliding scale pricing ($5-15) - pay what you can</li>
<li>All skill levels welcome</li>
</ul>

<h3>Safety First:</h3>
<ul>
<li>Practice safely within your skill level</li>
<li>Do not attempt techniques beyond your current abilities</li>
<li>Ask for help if you're unsure about safety</li>
<li>Safety monitors present to assist with concerns</li>
</ul>

<h3>Consent and Boundaries:</h3>
<ul>
<li>Enthusiastic consent required for all interactions</li>
<li>Ask before approaching others or touching their belongings</li>
<li>Respect everyone's boundaries and personal space</li>
<li>""No"" means no - respect it immediately</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from ALL people who might appear in the shot</li>
<li>Ask permission EACH time before taking photos</li>
<li>Respect others' privacy and anonymity</li>
</ul>

<h3>Respectful Environment:</h3>
<ul>
<li>Treat all participants with respect regardless of experience level</li>
<li>No unsolicited critique or advice</li>
<li>Keep coaching to your own partner unless specifically invited to help</li>
<li>Welcoming, judgment-free atmosphere</li>
</ul>

<h3>Appropriate Conduct:</h3>
<ul>
<li>Professional behavior expected</li>
<li>Comfortable, appropriate clothing for rope practice</li>
<li>Practice good hygiene</li>
<li>No drugs or alcohol on premises</li>
</ul>

<h3>Space Sharing:</h3>
<ul>
<li>Be mindful of shared space - keep practice areas compact</li>
<li>Clean up your area when finished</li>
<li>Return borrowed equipment to proper location</li>
<li>Help maintain welcoming environment for all</li>
</ul>

<h3>Bring Your Own Equipment:</h3>
<ul>
<li>Bring your own rope if possible (limited rope available to borrow)</li>
<li>Bring water and any personal items needed</li>
<li>Label your belongings</li>
</ul>

<h3>Zero Tolerance:</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior</li>
<li>Violations result in immediate removal and permanent ban</li>
<li>Community safety is our top priority</li>
</ul>

<h3>Practice at Your Own Risk:</h3>
<ul>
<li>This is not a supervised class</li>
<li>You are responsible for your own safety</li>
<li>Practice within your abilities and knowledge</li>
<li>Ask for help when needed</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Rope Social & Discussion",
                daysFromNow: 35,
                startHour: 19,
                capacity: 30,
                eventType: EventType.Social,
                price: 10.00m,
                shortDescription: "Monthly social gathering for community connection and discussion of rope topics.",
                longDescription: @"<p>Join us for our monthly Rope Social & Discussion - a casual gathering focused on community building, conversation, and connection. This is a chance to meet fellow rope enthusiasts, discuss topics of interest, and strengthen our community bonds.</p>

<h3>Event Format:</h3>
<ul>
<li>Social hour: Mingle and meet community members</li>
<li>Discussion circle: Structured conversation on a monthly topic</li>
<li>Q&A and open discussion</li>
<li>Resource sharing and announcements</li>
</ul>

<h3>Monthly Discussion Topics (rotating):</h3>
<ul>
<li>Rope philosophy and personal practice</li>
<li>Navigating consent and communication</li>
<li>Building a sustainable rope practice</li>
<li>Rope community culture and values</li>
<li>Safety deep dives on specific topics</li>
<li>Artistic expression through rope</li>
<li>And more based on community interest</li>
</ul>

<h3>Who Should Attend:</h3>
<ul>
<li>Anyone interested in rope bondage at any skill level</li>
<li>People curious about the rope community</li>
<li>Experienced practitioners looking to connect</li>
<li>Those seeking a thoughtful, discussion-oriented space</li>
<li>Community members interested in building connections</li>
</ul>

<h3>What Makes This Different:</h3>
<ul>
<li><strong>TALKING</strong> focused, not practice focused</li>
<li>Emphasis on community and connection over techniques</li>
<li>Safe space to ask questions and share experiences</li>
<li>Opportunity to learn from others' perspectives</li>
<li>Building friendships and support networks</li>
</ul>

<h3>Topics Covered (examples from past months):</h3>
<ul>
<li>How do you maintain work-life-rope balance?</li>
<li>What does ethical rope practice mean to you?</li>
<li>How do you handle rope injuries and recovery?</li>
<li>Building diverse and inclusive community spaces</li>
<li>Your questions and topics welcome!</li>
</ul>

<h3>Atmosphere:</h3>
<ul>
<li>Relaxed and welcoming</li>
<li>Judgment-free zone for all questions</li>
<li>Respectful discussion and active listening</li>
<li>Building understanding across experience levels</li>
</ul>

<p><strong>No rope practice at this event</strong> - this is purely social and discussion-based. Bring your curiosity, questions, and openness to connect with others.</p>

<p><em>Light refreshments provided. Optional sliding scale donation to support community programming.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP requested for space planning and refreshment prep</li>
<li>Sliding scale pricing ($5-10) - pay what you can</li>
<li>All backgrounds and experience levels welcome</li>
</ul>

<h3>Respectful Discussion:</h3>
<ul>
<li>This is a conversation-based event, not a practice session</li>
<li>Respect all perspectives and experiences shared</li>
<li>No interrupting or talking over others</li>
<li>Active listening encouraged</li>
<li>Disagree respectfully if you have different views</li>
</ul>

<h3>Confidentiality:</h3>
<ul>
<li>What's shared in the discussion circle stays in the discussion circle</li>
<li>Don't repeat personal stories or identifying information shared by others</li>
<li>Respect privacy and anonymity of all participants</li>
</ul>

<h3>Safe Space Guidelines:</h3>
<ul>
<li>No judgment for questions asked or experiences shared</li>
<li>Create welcoming environment for all experience levels</li>
<li>Step up/step back - make room for quieter voices</li>
<li>Assume good intent while allowing for mistakes and learning</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit permission from ALL participants</li>
<li>This is a private gathering</li>
<li>Respect attendees' privacy and anonymity</li>
</ul>

<h3>Appropriate Conduct:</h3>
<ul>
<li>Professional, respectful behavior required</li>
<li>No recruitment for personal relationships or services</li>
<li>No sales pitches or self-promotion</li>
<li>Focus on community building and authentic connection</li>
</ul>

<h3>Accessibility:</h3>
<ul>
<li>We strive to create accessible, welcoming space</li>
<li>Let organizers know if you have accessibility needs</li>
<li>Gender-neutral restrooms available</li>
<li>Scent-free preferred (some members have sensitivities)</li>
</ul>

<h3>Food and Beverages:</h3>
<ul>
<li>Light refreshments provided</li>
<li>Let us know of any allergies or dietary restrictions</li>
<li>Clean up after yourself</li>
<li>No alcohol served</li>
</ul>

<h3>Zero Tolerance:</h3>
<ul>
<li>No harassment, discrimination, or boundary violations</li>
<li>No drugs or alcohol</li>
<li>Violations result in removal and ban from community events</li>
</ul>

<h3>Community Care:</h3>
<ul>
<li>Look out for each other</li>
<li>Let organizers know if someone seems in distress</li>
<li>This is a community space - we all contribute to making it safe and welcoming</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "New Members Meetup",
                daysFromNow: 42,
                startHour: 18,
                capacity: 25,
                eventType: EventType.Social,
                price: 5.00m,
                shortDescription: "Welcome gathering for new community members to meet established practitioners and learn about upcoming events.",
                longDescription: @"<p>Welcome to the WitchCityRope community! This special meetup is designed specifically for new members to get oriented, meet friendly faces, and learn about all the opportunities our community offers.</p>

<h3>Event Overview:</h3>
<p>This casual, welcoming gathering helps new members feel comfortable and connected from day one. Whether you're brand new to rope or experienced but new to our community, this event is your gateway to getting involved.</p>

<h3>What We'll Cover:</h3>
<ul>
<li>Welcome and community overview</li>
<li>Introduction to our values, culture, and code of conduct</li>
<li>Overview of upcoming classes, events, and programming</li>
<li>How to get the most out of your membership</li>
<li>Q&A about the community, practices, and events</li>
<li>Meet established community members who can offer guidance</li>
<li>Connect with other new members</li>
</ul>

<h3>Who Should Attend:</h3>
<ul>
<li>New community members (joined within last 3 months)</li>
<li>People considering membership and want to learn more</li>
<li>Anyone who feels new and wants to build connections</li>
<li>Established members welcome to attend as greeters/mentors</li>
</ul>

<h3>What to Expect:</h3>
<ul>
<li>Friendly, welcoming atmosphere designed to reduce ""new person"" anxiety</li>
<li>Structured introductions to break the ice</li>
<li>Small group discussions to meet others</li>
<li>Resource sharing (calendars, communication channels, etc.)</li>
<li>Opportunity to ask all your questions in a supportive environment</li>
</ul>

<h3>You'll Learn About:</h3>
<ul>
<li>Different types of events (classes, practice jams, socials, performances)</li>
<li>How to register for events and manage your membership</li>
<li>Community communication channels and staying connected</li>
<li>Finding practice partners and building connections</li>
<li>Volunteer opportunities and community involvement</li>
<li>Vetting process and member-only events</li>
<li>Resources for continued learning</li>
</ul>

<h3>Meet Our Community:</h3>
<ul>
<li>Greeters assigned to help new members feel welcome</li>
<li>Experienced members available to answer questions</li>
<li>Other new members to connect with</li>
<li>Leadership and event organizers</li>
</ul>

<h3>No Rope Required:</h3>
<p>This is a social and informational event - no rope practice or experience necessary. Just bring yourself and your curiosity!</p>

<p><em>Light refreshments provided. Suggested $5 donation (free for those experiencing financial hardship).</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements:</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP requested for name tags and refreshment planning</li>
<li>Sliding scale pricing ($0-5) - pay what you can</li>
<li>Both prospective and current members welcome</li>
</ul>

<h3>Welcoming Environment:</h3>
<ul>
<li>This is explicitly a beginner-friendly, judgment-free space</li>
<li>All questions are welcome and encouraged</li>
<li>No experience with rope bondage required</li>
<li>Focus on building community connections</li>
</ul>

<h3>Confidentiality and Privacy:</h3>
<ul>
<li>Respect privacy of all attendees</li>
<li>Don't share identifying information about who attended</li>
<li>What's discussed stays within the community</li>
<li>Use scene names if preferred for anonymity</li>
</ul>

<h3>Respectful Interaction:</h3>
<ul>
<li>Treat all attendees with respect and kindness</li>
<li>No recruitment for personal relationships</li>
<li>Professional boundaries maintained</li>
<li>This is about community building, not dating</li>
</ul>

<h3>Getting to Know You:</h3>
<ul>
<li>We'll do brief introductions (optional to share or pass)</li>
<li>Share whatever you're comfortable with</li>
<li>Use scene name or first name only if preferred</li>
<li>No pressure to share personal details</li>
</ul>

<h3>Photography and Recording:</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit permission from ALL participants</li>
<li>Name tags available but optional</li>
<li>Respect wishes of those who prefer anonymity</li>
</ul>

<h3>Accessibility:</h3>
<ul>
<li>Please let organizers know of any accessibility needs</li>
<li>We strive to create welcoming space for all</li>
<li>Gender-neutral restrooms available</li>
<li>Scent-free environment preferred</li>
</ul>

<h3>Food and Beverages:</h3>
<ul>
<li>Light refreshments provided</li>
<li>Let us know of allergies or dietary restrictions</li>
<li>Clean up after yourself</li>
<li>No alcohol served</li>
</ul>

<h3>Information Sharing:</h3>
<ul>
<li>Feel free to ask questions about community</li>
<li>Event organizers available to discuss programming</li>
<li>Take informational materials and resources</li>
<li>Connect via official community channels only</li>
</ul>

<h3>Zero Tolerance:</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior</li>
<li>No boundary violations or inappropriate advances</li>
<li>Violations result in removal and ban from all events</li>
<li>Community safety is our highest priority</li>
</ul>

<h3>Community Values:</h3>
<ul>
<li>Consent, respect, and safety above all</li>
<li>Inclusive and welcoming to all backgrounds</li>
<li>Support for learning and growth</li>
<li>Building authentic connections and friendships</li>
</ul>"
            ),

            // Past Events (2 events) for testing historical data
            CreateSeedEvent(
                title: "Beginner Rope Circle",
                daysFromNow: -7,
                startHour: 18,
                capacity: 20,
                eventType: EventType.Social,
                price: 10.00m,
                shortDescription: "Past event: Introductory session for newcomers to rope bondage.",
                longDescription: @"<p>This past event was an introductory practice session designed for absolute beginners and those new to the rope community.</p>

<h3>Past attendees learned:</h3>
<ul>
<li>Basic rope handling techniques</li>
<li>Simple single column ties</li>
<li>Introduction to safety principles</li>
<li>Communication fundamentals</li>
<li>How to get started with rope practice</li>
</ul>

<p>This was a supportive, beginner-friendly environment where participants could ask questions, meet fellow newcomers, and start their rope journey in a welcoming space.</p>",
                policies: @"<h2>Standard Event Policies Applied</h2>
<ul>
<li>18+ with ID required</li>
<li>Consent and communication required</li>
<li>No photography without permission</li>
<li>Respectful conduct expected</li>
<li>Zero tolerance for harassment</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Rope Fundamentals Series",
                daysFromNow: -14,
                startHour: 17,
                capacity: 15,
                eventType: EventType.Class,
                price: 40.00m,
                shortDescription: "Past event: Multi-session fundamentals course for serious students.",
                longDescription: @"<p>This past 4-week course provided comprehensive instruction in rope bondage fundamentals for dedicated students.</p>

<h3>Course Covered:</h3>
<ul>
<li><strong>Week 1:</strong> Safety, consent, and communication foundations</li>
<li><strong>Week 2:</strong> Single column ties and basic cuffs</li>
<li><strong>Week 3:</strong> Double column ties and connecting techniques</li>
<li><strong>Week 4:</strong> Basic chest harnesses and body ties</li>
</ul>

<h3>Prerequisites:</h3>
<p>Commitment to attend all 4 sessions</p>

<p>Past participants built a strong foundation in rope fundamentals with structured curriculum and progressive skill development.</p>",
                policies: @"<h2>Standard Class Policies Applied</h2>
<ul>
<li>18+ with ID required</li>
<li>All 4 sessions attendance required</li>
<li>Safety assessment and medical waiver</li>
<li>Consent protocols mandatory</li>
<li>No photography without permission</li>
<li>Professional conduct required</li>
</ul>"
            )
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
    /// - Up to 11 sample applications with different statuses (UnderReview, InterviewApproved, FinalReview, Approved, OnHold, Denied)
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
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, cannot create vetting applications");
            return;
        }

        // Get the vetted test user for their specific application
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");

        // Get all users EXCEPT admin (to avoid duplicate applications for admin)
        // Order by email for consistent indexing across multiple seed runs
        var users = await _userManager.Users
            .Where(u => u.Id != adminUser.Id)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        // Ensure we have enough users for the applications we want to create
        if (users.Count < 4) // Need at least 4 non-admin users (plus admin = 5 total)
        {
            _logger.LogWarning("Not enough users ({UserCount}) to create diverse vetting applications. Need at least 4 non-admin users.", users.Count);
            return;
        }

        // Create diverse set of vetting applications with different statuses
        // Each user can only have one application due to unique constraint
        var sampleApplications = new List<VettingApplication>
        {
            // Application 1: Admin User - Approved (vetted member)
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id, // Admin user should be vetted
                SceneName = adminUser.SceneName ?? "RopeMaster",
                RealName = "Admin User",
                Email = adminUser.Email!,
                FetLifeHandle = "RopeMaster_Admin",
                Pronouns = "they/them",
                OtherNames = null,
                AboutYourself = @"As an administrator and experienced practitioner, I've been involved in the rope bondage community for many years. I help manage the community and ensure a safe, welcoming environment for all members.",
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-365), // Applied a year ago
                ReviewStartedAt = DateTime.UtcNow.AddDays(-364),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-362),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-360),
                AdminNotes = "Community administrator - founding member with extensive experience. Approved."
            }
        };

        // Add vetted test user application if user exists (for dashboard E2E testing)
        if (vettedUser != null)
        {
            sampleApplications.Add(new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = vettedUser.Id,
                SceneName = vettedUser.SceneName ?? "RopeEnthusiast",
                RealName = "Vetted Test User",
                Email = vettedUser.Email!,
                FetLifeHandle = "RopeEnthusiast_Test",
                Pronouns = "he/him",
                OtherNames = null,
                AboutYourself = @"I'm a passionate rope enthusiast with 2+ years of experience in rope bondage. I've completed several safety courses and have been an active participant in rope communities in other cities. I'm knowledgeable about consent practices, risk management, and safety protocols. I'm excited to join this community and continue learning from experienced practitioners while sharing my own knowledge.",
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-180), // Applied 6 months ago
                ReviewStartedAt = DateTime.UtcNow.AddDays(-178),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-175),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-170),
                AdminNotes = "Experienced practitioner with excellent references. Strong understanding of safety and consent. Approved for full membership."
            });
        }

        // Add remaining applications using existing users
        var remainingApplications = new List<VettingApplication>
        {
            // Application 2: Under Review - recent submission
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[0].Id, // First non-admin user
                SceneName = "RopeNovice",
                RealName = "Alexandra Martinez",
                Email = "alexandra.martinez@email.com",
                FetLifeHandle = "RopeNovice2024",
                Pronouns = "she/her",
                OtherNames = "Alex",
                AboutYourself = @"I'm new to rope bondage but deeply interested in learning about the art and community. I've been reading extensively about rope safety and attending introductory workshops in other cities. I'm drawn to both the aesthetic beauty and the trust-building aspects of rope. I have experience in other BDSM practices and understand the importance of consent, communication, and safety. I'm looking for a supportive community where I can learn from experienced practitioners and contribute positively to the group dynamic.",
                WorkflowStatus = VettingStatus.UnderReview, // UnderReview (0)
                SubmittedAt = DateTime.UtcNow.AddDays(-3),
                AdminNotes = null
            },

            // Application 3: Interview Approved - ready for scheduling
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[1].Id, // Second non-admin user
                SceneName = "KnotLearner",
                RealName = "Jordan Kim",
                Email = "jordan.kim@email.com",
                FetLifeHandle = "KnotLearner_JK",
                Pronouns = "they/them",
                OtherNames = null,
                AboutYourself = @"I've been interested in rope bondage for over a year and have practiced with a partner at home. We've been focusing on safety and basic ties, but I want to expand my knowledge and learn from experienced riggers. I have a background in dance and appreciate the artistic and movement aspects of rope. I'm committed to ongoing education about consent, negotiation, and safety practices. I'm hoping to join a community where I can learn advanced techniques while building meaningful connections with like-minded people.",
                WorkflowStatus = VettingStatus.InterviewApproved, // InterviewApproved (1)
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-7),
                AdminNotes = "Good references and thoughtful application. Ready for interview to assess practical knowledge."
            },

            // Application 4: Approved - approved member with multiple roles
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[2].Id, // Third non-admin user
                SceneName = "TrustBuilder",
                RealName = "Marcus Johnson",
                Email = "marcus.johnson@email.com",
                FetLifeHandle = "TrustBuilder_MJ",
                Pronouns = "he/him",
                OtherNames = "Marc, MJ",
                AboutYourself = @"I'm a 28-year-old professional who discovered rope bondage through a partner. I've been practicing for six months and am passionate about the psychological and emotional aspects of rope. I have experience as a rigger and understand the responsibility that comes with restraining another person. I've completed online safety courses and practiced extensively with enthusiastic partners. I'm seeking a community where I can continue learning and share experiences with others who value both technical skill and emotional intelligence in rope work.",
                WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                SubmittedAt = DateTime.UtcNow.AddDays(-14),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-10),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-8),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-5),
                LastReviewedAt = DateTime.UtcNow.AddDays(-5),
                AdminNotes = "Interview completed. Applicant showed excellent understanding of consent and safety principles. Approved for membership with Teacher and SafetyTeam roles."
            },

            // Application 5: Approved - recently approved member
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[3].Id, // Fourth non-admin user
                SceneName = "SilkAndSteel",
                RealName = "Sarah Chen",
                Email = "sarah.chen@email.com",
                FetLifeHandle = "SilkAndSteel_SC",
                Pronouns = "she/her",
                OtherNames = "Sarah C.",
                AboutYourself = @"I'm an experienced practitioner with 3+ years of rope bondage experience, both as a rigger and rope bunny. I've taught workshops in my previous city and am looking to join the community here after relocating for work. I have extensive knowledge of safety protocols, medical considerations, and emergency procedures. I'm interested in both the technical aspects of rope work and the community building aspects. I'd love to contribute my knowledge while continuing to learn from other experienced practitioners.",
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-21),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-18),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-2),
                AdminNotes = "Excellent references from previous community. Strong technical knowledge and teaching experience. Approved for full membership."
            }
        };

        // Add remaining applications to main list
        sampleApplications.AddRange(remainingApplications);

        // Add additional applications if we have more users to prevent constraint violations
        // Only add more applications if we have users beyond the first 4 (admin is excluded from users list)
        if (users.Count >= 5)
        {
            var additionalApplications = new List<VettingApplication>();

            // Application 6: On Hold - additional information needed
            additionalApplications.Add(new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[4].Id, // Fifth non-admin user
                SceneName = "EagerLearner",
                RealName = "Taylor Rodriguez",
                Email = "taylor.rodriguez@email.com",
                FetLifeHandle = "EagerLearner99",
                Pronouns = "she/they",
                OtherNames = "Tay",
                AboutYourself = @"I'm completely new to rope bondage but very eager to learn! I've watched videos online and read some books about safety. I don't have any hands-on experience yet but I'm excited to start learning with experienced people. I understand this is about more than just tying knots - it's about trust, communication, and building relationships. I'm committed to learning and following all safety guidelines.",
                WorkflowStatus = VettingStatus.OnHold, // OnHold (6)
                SubmittedAt = DateTime.UtcNow.AddDays(-12),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-8),
                AdminNotes = "Very enthusiastic but lacks practical experience. Recommended to attend beginner classes and gain basic experience before reapplying. On hold pending completion of safety course."
            });

            // Application 7: Denied - doesn't meet community standards
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
                    WorkflowStatus = VettingStatus.Denied, // Denied (5)
                    SubmittedAt = DateTime.UtcNow.AddDays(-20),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-17),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-3),
                    AdminNotes = "Application denied. Applicant shows poor understanding of consent and safety protocols. Focused on 'tying people up' rather than community and education. Recommended to attend formal education courses before reapplying."
                });
            }

            // Application 8: Approved - experienced Teacher
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
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-30),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-25),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-20),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-15),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-15),
                    AdminNotes = "Experienced practitioner with strong safety focus. Approved for Teacher role."
                });
            }

            // Application 9: Long-term member of another community
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
                    WorkflowStatus = VettingStatus.InterviewApproved,
                    SubmittedAt = DateTime.UtcNow.AddDays(-8),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-5),
                    AdminNotes = "Strong references from previous community leaders. Ready for interview to discuss integration into local group."
                });
            }

            // Application 10: Nervous but genuine applicant
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
                    WorkflowStatus = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    AdminNotes = null
                });
            }

            // Application 11: RopeBunny - Someone new to rope looking to learn
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
                    WorkflowStatus = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    AdminNotes = null
                });
            }

            // Application 12: SafetyConscious - Approved Teacher from another city
            if (users.Count >= 11)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[10].Id,
                    SceneName = "SafetyConscious", // Fixed: was "SafetyFirst", should match user's SceneName
                    RealName = "Sam Rodriguez",
                    Email = "safety.conscious@email.com",
                    FetLifeHandle = "SafetyConscious_SR",
                    Pronouns = "they/them",
                    OtherNames = "Sam",
                    AboutYourself = @"I'm an experienced rigger relocating from Portland, where I was an active member of the rope community for 4+ years. I have extensive experience with both floor work and suspension, and I've completed multiple safety courses including basic first aid and rope-specific emergency procedures. In my previous community, I helped mentor newcomers and occasionally assisted with safety monitoring at events. I prioritize safety above all else in rope work and am committed to ongoing education. I'm seeking a new community home where I can contribute my knowledge while continuing to learn from other experienced practitioners.",
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-40),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-35),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-30),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-25),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-25),
                    AdminNotes = "Highly experienced with excellent references from Portland community. Strong safety background. Approved for Teacher role."
                });
            }

            // Application 13: PatientPractitioner - Approved Teacher with teaching experience
            if (users.Count >= 12)
            {
                additionalApplications.Add(new VettingApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = users[11].Id,
                    SceneName = "PatientPractitioner",
                    RealName = "James Chen",
                    Email = "patient.practitioner@email.com",
                    FetLifeHandle = "PatientPractitioner_JC",
                    Pronouns = "he/him",
                    OtherNames = "Jim",
                    AboutYourself = @"I'm a dedicated rope practitioner with 5+ years of experience. I've been teaching beginner workshops in my previous community for the past 2 years and have a particular passion for patient, methodical instruction. I believe in building strong fundamentals and emphasize safety, consent, and communication in all my teaching. I've completed advanced safety courses and have experience with both floor work and suspension. I'm seeking a new community where I can continue teaching and learning from other experienced practitioners.",
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-50),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-45),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-40),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-35),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-35),
                    AdminNotes = "Excellent teaching credentials with strong references from previous community. Patient and methodical approach to instruction. Approved for Teacher role."
                });
            }

            if (additionalApplications.Any())
            {
                sampleApplications.AddRange(additionalApplications);
            }
        }

        await _context.VettingApplications.AddRangeAsync(sampleApplications, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Sync User.VettingStatus for applications in terminal states (Approved, Denied, OnHold)
        // User.VettingStatus is the source of truth for permissions/access control
        var applicationsToSync = sampleApplications.Where(app =>
            app.UserId.HasValue &&
            app.WorkflowStatus is VettingStatus.Approved or VettingStatus.Denied or VettingStatus.OnHold)
            .ToList();

        foreach (var application in applicationsToSync)
        {
            var user = await _context.Users.FindAsync(new object[] { application.UserId!.Value }, cancellationToken);
            if (user != null)
            {
                user.VettingStatus = (int)application.WorkflowStatus;
                _logger.LogInformation(
                    "Synced User.VettingStatus to {VettingStatus} for user {UserId} (application {ApplicationId})",
                    application.WorkflowStatus, user.Id, application.Id);
            }
        }
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
                Subject = "Application Received - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.

Application Number: {{application_number}}
Submission Date: {{submission_date}}

Our vetting team will review your application and contact you within the next 7-10 business days with updates on your status.

If you have any questions, please don't hesitate to contact us.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{scene_name}},

Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.

Application Number: {{application_number}}
Submission Date: {{submission_date}}

Our vetting team will review your application and contact you within the next 7-10 business days with updates on your status.

If you have any questions, please don't hesitate to contact us.

Best regards,
The WitchCityRope Vetting Team",
                Variables = "[\"scene_name\", \"application_number\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
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
                Subject = "Interview Approved - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

Congratulations! Your vetting application has been approved for the interview stage.

Application Number: {{application_number}}
Next Steps: Please schedule your interview using the link below
Interview Scheduling: {{interview_link}}

During your interview, we will discuss your experience, interests, and answer any questions you may have about our community.

Please schedule your interview within the next 14 days.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{scene_name}},

Congratulations! Your vetting application has been approved for the interview stage.

Application Number: {{application_number}}
Next Steps: Please schedule your interview using the link below
Interview Scheduling: {{interview_link}}

During your interview, we will discuss your experience, interests, and answer any questions you may have about our community.

Please schedule your interview within the next 14 days.

Best regards,
The WitchCityRope Vetting Team",
                Variables = "[\"scene_name\", \"application_number\", \"interview_link\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.Approved,
                Subject = "Welcome to WitchCityRope - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

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
                PlainTextBody = @"Dear {{scene_name}},

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
                Variables = "[\"scene_name\", \"application_number\", \"approval_date\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.OnHold,
                Subject = "Application On Hold - Additional Information Needed - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

Your vetting application is currently on hold as we need some additional information to proceed.

Application Number: {{application_number}}
Reason: {{hold_reason}}

Required Actions:
{{required_actions}}

Please provide the requested information within 30 days to avoid application expiration.

If you have any questions about what's needed, please contact us.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{scene_name}},

Your vetting application is currently on hold as we need some additional information to proceed.

Application Number: {{application_number}}
Reason: {{hold_reason}}

Required Actions:
{{required_actions}}

Please provide the requested information within 30 days to avoid application expiration.

If you have any questions about what's needed, please contact us.

Best regards,
The WitchCityRope Vetting Team",
                Variables = "[\"scene_name\", \"application_number\", \"hold_reason\", \"required_actions\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
                IsActive = true,
                UpdatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new VettingEmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = EmailTemplateType.Denied,
                Subject = "Application Status Update - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

Thank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.

Application Number: {{application_number}}
Review Date: {{review_date}}

This decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.

We appreciate your interest in our community.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{scene_name}},

Thank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.

Application Number: {{application_number}}
Review Date: {{review_date}}

This decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.

We appreciate your interest in our community.

Best regards,
The WitchCityRope Vetting Team",
                Variables = "[\"scene_name\", \"application_number\", \"review_date\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
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
                Subject = "Interview Reminder - {{scene_name}}",
                HtmlBody = @"Dear {{scene_name}},

This is a friendly reminder about your upcoming vetting interview.

Application Number: {{application_number}}

If you need to reschedule, please contact us at least 24 hours in advance.

We look forward to meeting with you!

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{scene_name}},

This is a friendly reminder about your upcoming vetting interview.

Application Number: {{application_number}}

If you need to reschedule, please contact us at least 24 hours in advance.

We look forward to meeting with you!

Best regards,
The WitchCityRope Vetting Team",
                Variables = "[\"scene_name\", \"application_number\", \"submission_date\", \"application_date\", \"status_change_date\", \"contact_email\", \"current_status\"]",
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
            else if (eventItem.Title.Contains("Suspension Basics"))
            {
                // Multi-session event (for testing session-specific volunteer positions)
                AddSuspensionBasicsSessions(eventItem, sessionsToAdd);
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
                var basePrice = eventItem.EventType == EventType.Social ? 0m : 40m; // Multi-day events default to $40
                CreateMultiDayTicketTypes(eventItem, basePrice, eventSessions, ticketTypesToAdd);
            }
            else
            {
                // Single-day event
                var price = eventItem.EventType == EventType.Social ? 10m : 25m; // Default pricing for seed data
                CreateTicketTypesForSession(eventItem, price, eventSessions.First(), ticketTypesToAdd);
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
    ///
    /// CRITICAL: Creates specific test purchases for vetted@witchcityrope.com user
    /// to enable E2E testing of user dashboard event display functionality
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
                var isRSVP = ticketType.Price == 0;

                // Calculate purchase price based on pricing type
                decimal totalPrice;
                if (isRSVP)
                {
                    totalPrice = 0;
                }
                else if (ticketType.PricingType == PricingType.SlidingScale)
                {
                    // Random price within sliding scale range
                    var minPrice = ticketType.MinPrice ?? 10m;
                    var maxPrice = ticketType.MaxPrice ?? 40m;
                    totalPrice = minPrice + (decimal)Random.Shared.NextDouble() * (maxPrice - minPrice);
                }
                else
                {
                    // Fixed price with slight variation for realism
                    totalPrice = (ticketType.Price ?? 0) * (0.5m + (decimal)Random.Shared.NextDouble() * 0.5m);
                }

                var purchase = new TicketPurchase
                {
                    TicketTypeId = ticketType.Id,
                    UserId = user.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = isRSVP ? "Completed" : GetRandomPaymentStatus(),
                    PaymentMethod = isRSVP ? "RSVP" : GetRandomPaymentMethod(),
                    PaymentReference = Guid.NewGuid().ToString("N")[..8],
                    Notes = GetRandomPurchaseNotes()
                };

                purchasesToAdd.Add(purchase);
            }
        }

        // Create specific ticket purchases for vetted test user for E2E dashboard testing
        await CreateVettedUserTicketPurchasesAsync(purchasesToAdd, cancellationToken);

        await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket purchases creation completed. Created: {PurchaseCount} purchases", purchasesToAdd.Count);
    }

    /// <summary>
    /// Creates specific ticket purchases for the vetted test user (vetted@witchcityrope.com)
    /// to provide realistic data for E2E testing of user dashboard functionality.
    ///
    /// Creates 3-5 ticket purchases covering different scenarios:
    /// - Upcoming paid event (workshop)
    /// - Upcoming free event (social RSVP)
    /// - Past attended event
    /// - Optional social event with ticket
    /// - Optional additional event for search/filter testing
    /// </summary>
    private async Task CreateVettedUserTicketPurchasesAsync(
        List<TicketPurchase> purchasesToAdd,
        CancellationToken cancellationToken)
    {
        // Get the vetted test user
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");
        if (vettedUser == null)
        {
            _logger.LogWarning("Vetted test user not found, skipping dashboard test data creation");
            return;
        }

        // Check if vetted user already has ticket purchases
        var existingPurchases = await _context.TicketPurchases
            .Where(tp => tp.UserId == vettedUser.Id)
            .CountAsync(cancellationToken);

        if (existingPurchases > 0)
        {
            _logger.LogInformation("Vetted test user already has {Count} ticket purchases, skipping seed", existingPurchases);
            return;
        }

        _logger.LogInformation("Creating ticket purchases for vetted test user dashboard E2E testing");

        // Find events to register for
        var upcomingWorkshop = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EventType == EventType.Class &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var upcomingSocial = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EventType == EventType.Social &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var pastEvent = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EndDate < DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderByDescending(e => e.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Additional upcoming events for comprehensive testing
        var additionalUpcomingEvents = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any() &&
                       e.Id != (upcomingWorkshop != null ? upcomingWorkshop.Id : Guid.Empty) &&
                       e.Id != (upcomingSocial != null ? upcomingSocial.Id : Guid.Empty))
            .OrderBy(e => e.StartDate)
            .Take(2)
            .ToListAsync(cancellationToken);

        // Scenario 1: Upcoming Paid Event (Workshop/Class)
        if (upcomingWorkshop != null)
        {
            var paidTicket = upcomingWorkshop.TicketTypes
                .Where(tt => tt.Price > 0)
                .OrderBy(tt => tt.Price)
                .FirstOrDefault();

            if (paidTicket != null)
            {
                // Calculate price based on ticket type
                decimal purchasePrice;
                if (paidTicket.PricingType == PricingType.SlidingScale)
                {
                    purchasePrice = paidTicket.DefaultPrice ?? 20m; // Use default/suggested price
                }
                else
                {
                    purchasePrice = (paidTicket.Price ?? 0) * 0.75m; // Sliding scale discount for fixed price
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = paidTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-5),
                    Quantity = 1,
                    TotalPrice = purchasePrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = "PayPal",
                    PaymentReference = $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Sliding scale pricing applied"
                });

                _logger.LogInformation("Created paid workshop ticket purchase for event: {EventTitle}", upcomingWorkshop.Title);
            }
        }

        // Scenario 2: Upcoming Free Event (Social RSVP)
        if (upcomingSocial != null)
        {
            var freeTicket = upcomingSocial.TicketTypes
                .Where(tt => tt.Price == 0)
                .FirstOrDefault();

            if (freeTicket != null)
            {
                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = freeTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-3),
                    Quantity = 1,
                    TotalPrice = 0,
                    PaymentStatus = "Completed",
                    PaymentMethod = "RSVP",
                    PaymentReference = $"RSVP_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Free RSVP - looking forward to this!"
                });

                _logger.LogInformation("Created free social RSVP for event: {EventTitle}", upcomingSocial.Title);
            }
        }

        // Scenario 3: Past Attended Event
        if (pastEvent != null)
        {
            var pastTicketType = pastEvent.TicketTypes.FirstOrDefault();

            if (pastTicketType != null)
            {
                var purchaseDate = pastEvent.StartDate.AddDays(-7);

                // Calculate price based on ticket type
                decimal totalPrice;
                bool isPaid;
                if (pastTicketType.PricingType == PricingType.SlidingScale)
                {
                    totalPrice = (pastTicketType.DefaultPrice ?? 20m) * 0.5m;
                    isPaid = true;
                }
                else
                {
                    var price = pastTicketType.Price ?? 0;
                    totalPrice = price > 0 ? price * 0.5m : 0;
                    isPaid = price > 0;
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = pastTicketType.Id,
                    PurchaseDate = purchaseDate,
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = isPaid ? "Stripe" : "RSVP",
                    PaymentReference = isPaid ? $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}" : $"RSVP_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Attended - great event!"
                });

                _logger.LogInformation("Created past event attendance for event: {EventTitle}", pastEvent.Title);
            }
        }

        // Scenario 4 & 5: Additional upcoming events for comprehensive testing (search, filters, etc.)
        foreach (var additionalEvent in additionalUpcomingEvents.Take(2))
        {
            var ticketType = additionalEvent.TicketTypes.FirstOrDefault();

            if (ticketType != null)
            {
                var isSocialEvent = additionalEvent.EventType == EventType.Social;

                // Calculate price based on ticket type
                decimal price;
                if (isSocialEvent)
                {
                    price = 0; // Free RSVP or donation
                }
                else if (ticketType.PricingType == PricingType.SlidingScale)
                {
                    // Use default price for sliding scale
                    price = ticketType.DefaultPrice ?? 20m;
                }
                else
                {
                    // Fixed price with slight discount
                    price = (ticketType.Price ?? 0) * 0.6m;
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = ticketType.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(2, 10)),
                    Quantity = 1,
                    TotalPrice = price,
                    PaymentStatus = "Completed",
                    PaymentMethod = isSocialEvent ? "RSVP" : "Venmo",
                    PaymentReference = isSocialEvent ? $"RSVP_{Guid.NewGuid().ToString()[..8]}" : $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}",
                    Notes = isSocialEvent ? null! : "Can't wait for this class!"
                });

                _logger.LogInformation("Created additional event registration for event: {EventTitle}", additionalEvent.Title);
            }
        }

        var createdCount = purchasesToAdd.Count(p => p.UserId == vettedUser.Id);
        _logger.LogInformation("Created {Count} ticket purchases for vetted test user", createdCount);
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
            if (eventItem.EventType == EventType.Social)
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
            // Special handling for Suspension Basics: Create session-specific volunteer positions
            if (eventItem.Title.Contains("Suspension Basics"))
            {
                foreach (var session in eventItem.Sessions)
                {
                    var sessionPositions = CreateSuspensionBasicsVolunteerPositions(eventItem, session);
                    volunteerPositionsToAdd.AddRange(sessionPositions);
                }
            }
            else
            {
                // Add event-wide volunteer positions for other events
                var eventPositions = CreateEventVolunteerPositions(eventItem);
                volunteerPositionsToAdd.AddRange(eventPositions);

                // Add session-specific volunteer positions for multi-day events
                if (eventItem.Sessions.Any() && eventItem.EventType == EventType.Class)
                {
                    foreach (var session in eventItem.Sessions)
                    {
                        var sessionPositions = CreateSessionVolunteerPositions(eventItem, session);
                        volunteerPositionsToAdd.AddRange(sessionPositions);
                    }
                }
            }
        }

        await _context.VolunteerPositions.AddRangeAsync(volunteerPositionsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Volunteer positions creation completed. Created: {VolunteerCount} positions", volunteerPositionsToAdd.Count);

        // Create volunteer signups for testing
        await SeedVolunteerSignupsAsync(cancellationToken);
    }

    /// <summary>
    /// Creates volunteer signups for testing volunteer management functionality
    /// </summary>
    private async Task SeedVolunteerSignupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting volunteer signups creation");

        // Check if volunteer signups already exist (idempotent operation)
        var existingSignupCount = await _context.VolunteerSignups.CountAsync(cancellationToken);
        if (existingSignupCount > 0)
        {
            _logger.LogInformation("Volunteer signups already exist ({Count}), skipping signup seeding", existingSignupCount);
            return;
        }

        // Get users for signup testing
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        var teacherUser = await _userManager.FindByEmailAsync("teacher@witchcityrope.com");
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");
        var memberUser = await _userManager.FindByEmailAsync("member@witchcityrope.com");

        if (adminUser == null || teacherUser == null || vettedUser == null || memberUser == null)
        {
            _logger.LogWarning("Could not find all test users for volunteer signup seeding");
            return;
        }

        // Get volunteer positions
        var volunteerPositions = await _context.VolunteerPositions
            .Include(vp => vp.Event)
            .ToListAsync(cancellationToken);

        if (!volunteerPositions.Any())
        {
            _logger.LogWarning("No volunteer positions found for signup seeding");
            return;
        }

        var signupsToAdd = new List<VolunteerSignup>();
        var now = DateTime.UtcNow;

        // RopeMaster (admin) volunteers for multiple positions
        // Sign up for Door Monitor positions (public-facing)
        var doorMonitorPositions = volunteerPositions
            .Where(vp => vp.Title == "Door Monitor" && vp.IsPublicFacing)
            .Take(3)
            .ToList();

        foreach (var position in doorMonitorPositions)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = position.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-7),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7)
            });

            // Update position slot count
            position.SlotsFilled++;
        }

        // RopeMaster volunteers for Setup/Cleanup
        var setupPosition = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing)
            .FirstOrDefault();

        if (setupPosition != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-5),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            });

            setupPosition.SlotsFilled++;
        }

        // Teacher volunteers for Setup/Cleanup
        var setupPosition2 = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing && vp.Id != setupPosition?.Id)
            .FirstOrDefault();

        if (setupPosition2 != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition2.Id,
                UserId = teacherUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-6),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-6)
            });

            setupPosition2.SlotsFilled++;
        }

        // Vetted member volunteers for Door Monitor
        var doorPosition = volunteerPositions
            .Where(vp => vp.Title == "Door Monitor" && vp.IsPublicFacing && !doorMonitorPositions.Contains(vp))
            .FirstOrDefault();

        if (doorPosition != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = doorPosition.Id,
                UserId = vettedUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-4),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-4)
            });

            doorPosition.SlotsFilled++;
        }

        // Regular member volunteers for Setup/Cleanup
        var setupPosition3 = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing &&
                         vp.Id != setupPosition?.Id && vp.Id != setupPosition2?.Id)
            .FirstOrDefault();

        if (setupPosition3 != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition3.Id,
                UserId = memberUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-3),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3)
            });

            setupPosition3.SlotsFilled++;
        }

        // Add one completed volunteer signup for RopeMaster (past event)
        var pastPosition = volunteerPositions
            .Where(vp => vp.Event != null && vp.Event.StartDate < now && vp.IsPublicFacing)
            .FirstOrDefault();

        if (pastPosition != null)
        {
            var completedSignup = new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = pastPosition.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Completed,
                SignedUpAt = now.AddDays(-14),
                HasCheckedIn = true,
                CheckedInAt = pastPosition.Event!.StartDate.AddMinutes(-15),
                HasCompleted = true,
                CompletedAt = pastPosition.Event!.StartDate.AddHours(2),
                CreatedAt = now.AddDays(-14),
                UpdatedAt = pastPosition.Event!.StartDate.AddHours(2)
            };

            signupsToAdd.Add(completedSignup);
            pastPosition.SlotsFilled++;
        }

        await _context.VolunteerSignups.AddRangeAsync(signupsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Volunteer signups creation completed. Created: {SignupCount} signups", signupsToAdd.Count);
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
        var ticketPurchaseCount = await _context.TicketPurchases.CountAsync(cancellationToken);
        var safetyIncidentCount = await _context.SafetyIncidents.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0 || vettingApplicationCount == 0 || ticketPurchaseCount == 0 || safetyIncidentCount == 0;

        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, VettingApplications={VettingApplicationCount}, TicketPurchases={TicketPurchaseCount}, SafetyIncidents={SafetyIncidentCount}, Required={IsRequired}",
            userCount, eventCount, vettingApplicationCount, ticketPurchaseCount, safetyIncidentCount, isRequired);

        return isRequired;
    }

    /// <summary>
    /// Helper method to create sample events with proper UTC DateTime handling.
    /// Follows ApplicationDbContext patterns for UTC date storage and audit fields.
    ///
    /// Creates realistic event data with proper scheduling, capacity, pricing information,
    /// and complete descriptive fields (short description, long description, policies).
    /// </summary>
    private Event CreateSeedEvent(
        string title,
        int daysFromNow,
        int startHour,
        int capacity,
        EventType eventType,
        decimal price,
        string shortDescription,
        string longDescription,
        string policies)
    {
        // Calculate UTC dates following ApplicationDbContext patterns
        var startDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(startHour);
        var endDate = startDate.AddHours(eventType == EventType.Social ? 2 : 3); // Social events 2hrs, classes 3hrs

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortDescription = shortDescription,      // Brief summary for cards/listings
            Description = longDescription,             // Full detailed description
            Policies = policies,                       // Event policies and safety guidelines
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Capacity = capacity,
            EventType = eventType,
            Location = eventType == EventType.Social ? "Community Space" : "Main Workshop Room",
            IsPublished = true,
            // CreatedAt/UpdatedAt will be set by ApplicationDbContext.UpdateAuditFields()
        };
    }

    /// <summary>
    /// Helper method to add sessions and tickets for single-day events
    /// Most events are single-day with one session
    /// </summary>
    private void AddSingleDayEvent(Event eventItem, decimal price, List<Session> sessionsToAdd, List<TicketType> ticketTypesToAdd)
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
        if (eventItem.EventType == EventType.Social)
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
                PricingType = PricingType.Fixed
            };

            var donationTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Support Donation",
                Description = "Optional donation to support the community",
                Price = price,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentTicketCount(),
                PricingType = PricingType.Fixed
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
                Price = price,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
                PricingType = PricingType.Fixed
            };

            ticketTypesToAdd.Add(regularTicket);
        }
    }

    /// <summary>
    /// Helper method to add sessions and tickets for multi-day events
    /// Creates individual day sessions plus discounted full-event tickets
    /// </summary>
    private void AddMultiDayEvent(Event eventItem, decimal basePrice, int numberOfDays, List<Session> sessionsToAdd, List<TicketType> ticketTypesToAdd)
    {
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
                PricingType = PricingType.Fixed
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
            PricingType = PricingType.Fixed
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
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true // Public can sign up
        });

        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            Title = "Setup/Cleanup Crew",
            Description = "Help set up equipment before the event and clean up afterwards",
            SlotsNeeded = 3,
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true // Public can sign up
        });

        // Additional positions for classes
        if (eventItem.EventType == EventType.Class)
        {
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                Title = "Teaching Assistant",
                Description = "Help instructor with demonstrations and assist students",
                SlotsNeeded = 1,
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false // Admin-only assignment
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
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false // Admin-only assignment (requires safety expertise)
            });
        }

        return positions;
    }

    /// <summary>
    /// Creates session-specific volunteer positions for Suspension Basics event
    /// This demonstrates volunteer positions tied to specific sessions for testing
    /// </summary>
    private List<VolunteerPosition> CreateSuspensionBasicsVolunteerPositions(Event eventItem, Session session)
    {
        var positions = new List<VolunteerPosition>();

        if (session.SessionCode == "DAY1")
        {
            // Day 1 positions (6:00 PM - 9:00 PM)
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Setup Crew",
                Description = "Help set up equipment and prepare the space before Day 1 begins",
                SlotsNeeded = 3,
                SlotsFilled = 0,
                IsPublicFacing = true
            });

            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Door Monitor",
                Description = "Check attendees in and welcome participants for Day 1",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });
        }
        else if (session.SessionCode == "DAY2")
        {
            // Day 2 positions (8:00 PM - 10:00 PM)
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Teaching Assistant",
                Description = "Assist instructor with advanced suspension demonstrations and provide individual feedback for Day 2",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });

            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Safety Monitor",
                Description = "Monitor rigging and ensure safety protocols are followed during Day 2",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });
        }

        return positions;
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
    /// Helper method to add sessions for Suspension Basics event (multi-session example)
    /// Creates two sessions: Day 1 and Day 2 for testing session-specific volunteers
    /// </summary>
    private void AddSuspensionBasicsSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // Day 1: 6:00 PM - 9:00 PM
        var day1Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "DAY1",
            Name = "Day 1",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.StartDate.AddHours(3),
            Capacity = eventItem.Capacity / 2,
            CurrentAttendees = 0
        };

        // Day 2: 8:00 PM - 10:00 PM
        var day2Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "DAY2",
            Name = "Day 2",
            StartTime = eventItem.StartDate.AddHours(2),
            EndTime = eventItem.StartDate.AddHours(4),
            Capacity = eventItem.Capacity / 2,
            CurrentAttendees = 0
        };

        sessionsToAdd.Add(day1Session);
        sessionsToAdd.Add(day2Session);
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
    private void CreateTicketTypesForSession(Event eventItem, decimal price, Session session, List<TicketType> ticketTypesToAdd)
    {
        if (eventItem.EventType == EventType.Social)
        {
            // Social events: Free RSVP + optional sliding scale donation ticket
            var rsvpTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Free RSVP",
                Description = "Free attendance - RSVP required",
                Price = 0,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentRSVPCount(),
                PricingType = PricingType.Fixed
            };

            var donationTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Support Donation",
                Description = "Optional sliding scale donation to support the community - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentTicketCount(),
                PricingType = PricingType.SlidingScale
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
                Price = price,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
                PricingType = PricingType.Fixed
            };

            ticketTypesToAdd.Add(regularTicket);
        }
    }

    /// <summary>
    /// Helper method to create ticket types for multi-day events
    /// Creates individual day tickets and full event ticket with discount
    /// For social events, uses sliding scale pricing
    /// </summary>
    private void CreateMultiDayTicketTypes(Event eventItem, decimal basePrice, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        if (eventItem.EventType == EventType.Social)
        {
            // Social events: Free RSVP for each day + sliding scale donation for full event
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var rsvpTicket = new TicketType
                {
                    EventId = eventItem.Id,
                    SessionId = session.Id,
                    Name = $"Day {i + 1} RSVP",
                    Description = $"Free RSVP for Day {i + 1} only",
                    Price = 0,
                    Available = session.Capacity,
                    Sold = (int)(session.Capacity * 0.3), // 30% sold on average
                    PricingType = PricingType.Fixed
                };

                ticketTypesToAdd.Add(rsvpTicket);
            }

            // Full event sliding scale donation
            var fullEventTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = null, // Multi-session ticket
                Name = $"All {sessions.Count} Days Support",
                Description = $"Optional sliding scale donation for all {sessions.Count} days - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
                PricingType = PricingType.SlidingScale
            };

            ticketTypesToAdd.Add(fullEventTicket);
        }
        else // Class
        {
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
                    PricingType = PricingType.Fixed
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
                PricingType = PricingType.Fixed
            };

            ticketTypesToAdd.Add(fullEventTicket);
        }
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
            // All applications start with initial submission - goes directly to UnderReview
            auditLogs.Add(new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Application Submitted",
                PerformedBy = application.UserId ?? Guid.Empty, // Use Empty for anonymous submissions
                PerformedAt = application.SubmittedAt,
                OldValue = null,
                NewValue = "UnderReview",
                Notes = $"Initial application submitted by {application.SceneName} - automatically moved to review queue"
            });

            // Create status progression based on current workflow status
            switch (application.WorkflowStatus)
            {
                case VettingStatus.UnderReview:
                    // Just submitted, only has initial submission (already logged above)
                    break;

                case VettingStatus.InterviewApproved:
                    // UnderReview → InterviewApproved
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

                case VettingStatus.FinalReview:
                    // UnderReview → InterviewApproved → FinalReview (interview completion moves directly to final review)
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
                        Action = "Interview Completed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.InterviewScheduledFor?.AddDays(-1) ?? DateTime.UtcNow.AddDays(-1),
                        OldValue = "InterviewApproved",
                        NewValue = "FinalReview",
                        Notes = $"Interview completed on {application.InterviewScheduledFor?.ToString("yyyy-MM-dd HH:mm") ?? "TBD"}, moved to final review"
                    });
                    break;

                case VettingStatus.Approved:
                    // UnderReview → InterviewApproved → FinalReview → Approved
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
                        NewValue = "FinalReview",
                        Notes = "Interview completed with applicant - excellent candidate with strong community values. Moving to final review."
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Application Approved",
                        PerformedBy = adminUserId,
                        PerformedAt = application.DecisionMadeAt ?? application.SubmittedAt.AddDays(18),
                        OldValue = "FinalReview",
                        NewValue = "Approved",
                        Notes = "Application approved for full membership - welcome to the community!"
                    });
                    break;

                case VettingStatus.OnHold:
                    // UnderReview → OnHold
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
                    // UnderReview → Denied
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

    /// <summary>
    /// Seed safety incident test data
    /// Creates realistic incidents with various statuses, assignments, and scenarios
    /// </summary>
    public async Task SeedSafetyIncidentsAsync(CancellationToken cancellationToken = default)
    {
        // Check if safety incidents already exist
        if (await _context.SafetyIncidents.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Safety incidents already exist, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding safety incident test data");

        // Get users for assignments and reporters
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        var teacherUser = await _userManager.FindByEmailAsync("teacher@witchcityrope.com");
        var memberUser = await _userManager.FindByEmailAsync("member@witchcityrope.com");
        var guestUser = await _userManager.FindByEmailAsync("guest@witchcityrope.com");
        var coordinator1 = await _userManager.FindByEmailAsync("coordinator1@witchcityrope.com");
        var coordinator2 = await _userManager.FindByEmailAsync("coordinator2@witchcityrope.com");

        if (adminUser == null || teacherUser == null)
        {
            _logger.LogWarning("Required users not found, cannot seed safety incidents");
            return;
        }

        var now = DateTime.UtcNow;
        var incidents = new List<WitchCityRope.Api.Features.Safety.Entities.SafetyIncident>();

        // Incident 1: New report submitted today (unassigned)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now:yyyyMMdd}-0001",
            Title = "Rope Practice Numbness",
            ReporterId = memberUser?.Id,
            IncidentDate = now.AddHours(-3),
            ReportedAt = now.AddHours(-1),
            Location = "Main Practice Space - Studio A",
            EncryptedDescription = await _encryptionService.EncryptAsync("During rope practice, participant experienced numbness in left hand that persisted for 5 minutes after release. No visible injury but concerning timeline."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Rigger: Alex R., Bottom: Casey M."),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Jamie T. (safety monitor)"),
            EncryptedContactEmail = await _encryptionService.EncryptAsync("member@witchcityrope.com"),
            IsAnonymous = false,
            RequestFollowUp = true,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.ReportSubmitted,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1),
            CreatedBy = memberUser?.Id
        });

        // Incident 2: Information gathering phase (assigned to coordinator1)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-2):yyyyMMdd}-0001",
            Title = "Inappropriate Touching",
            ReporterId = guestUser?.Id,
            IncidentDate = now.AddDays(-3),
            ReportedAt = now.AddDays(-2),
            Location = "Social Event - The Witch House",
            EncryptedDescription = await _encryptionService.EncryptAsync("Attendee reported inappropriate touching during social mingling. Made it clear this was unwanted but contact persisted after first boundary statement."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Reporter: Guest attendee, Involved party: Regular member (name withheld pending investigation)"),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Bar staff member witnessed interaction"),
            EncryptedContactEmail = await _encryptionService.EncryptAsync("guest@witchcityrope.com"),
            EncryptedContactName = await _encryptionService.EncryptAsync("Jane Guest"),
            IsAnonymous = false,
            RequestFollowUp = true,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.InformationGathering,
            AssignedTo = coordinator1?.Id,
            CoordinatorId = coordinator1?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_001",
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-1),
            CreatedBy = guestUser?.Id,
            UpdatedBy = adminUser.Id
        });

        // Incident 3: Reviewing final report (assigned to coordinator2)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-7):yyyyMMdd}-0001",
            Title = "Equipment Failure",
            ReporterId = null, // Anonymous
            IncidentDate = now.AddDays(-8),
            ReportedAt = now.AddDays(-7),
            Location = "Workshop - Intermediate Suspension",
            EncryptedDescription = await _encryptionService.EncryptAsync("During suspension workshop, equipment failure (carabiner gate opened unexpectedly). Bottom dropped approximately 6 inches before secondary safety line caught. No injury but equipment needs inspection."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Workshop instructor and participant (both request anonymity)"),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Workshop attendees (12 people present)"),
            IsAnonymous = true,
            RequestFollowUp = false,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.ReviewingFinalReport,
            AssignedTo = coordinator2?.Id,
            CoordinatorId = coordinator2?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_002",
            GoogleDriveFinalReportUrl = "https://docs.google.com/document/d/EXAMPLE_DOC_ID_002",
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-1),
            UpdatedBy = teacherUser.Id
        });

        // Incident 4: On hold (waiting for external information)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-14):yyyyMMdd}-0001",
            Title = "Performance Dizziness",
            ReporterId = memberUser?.Id,
            IncidentDate = now.AddDays(-15),
            ReportedAt = now.AddDays(-14),
            Location = "Performance Event - Salem Arts Theater",
            EncryptedDescription = await _encryptionService.EncryptAsync("Performer experienced dizziness during rope performance. Possible medical cause - waiting for performer's doctor clearance before continuing investigation."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Performer: Morgan K., Rigger: Pat S."),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Stage manager and audience members"),
            EncryptedContactEmail = await _encryptionService.EncryptAsync("member@witchcityrope.com"),
            IsAnonymous = false,
            RequestFollowUp = true,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.OnHold,
            AssignedTo = coordinator1?.Id,
            CoordinatorId = coordinator1?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_003",
            CreatedAt = now.AddDays(-14),
            UpdatedAt = now.AddDays(-5),
            CreatedBy = memberUser?.Id,
            UpdatedBy = adminUser.Id
        });

        // Incident 5: Closed case (resolved - equipment issue addressed)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-30):yyyyMMdd}-0001",
            Title = "Mat Issue Resolved",
            ReporterId = null, // Anonymous
            IncidentDate = now.AddDays(-31),
            ReportedAt = now.AddDays(-30),
            Location = "Community Practice Night - Studio B",
            EncryptedDescription = await _encryptionService.EncryptAsync("Practice mat became dislodged during transition, participant stumbled but no injury. Mats have been replaced with non-slip versions."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Two community members (anonymous)"),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Practice night coordinator"),
            IsAnonymous = true,
            RequestFollowUp = false,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.Closed,
            AssignedTo = coordinator2?.Id,
            CoordinatorId = coordinator2?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_004",
            GoogleDriveFinalReportUrl = "https://docs.google.com/document/d/EXAMPLE_DOC_ID_004",
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-7),
            UpdatedBy = coordinator2?.Id
        });

        // Incident 6: Recent report needing assignment
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddHours(-12):yyyyMMdd}-0002",
            Title = "Workshop Discomfort",
            ReporterId = null, // Anonymous
            IncidentDate = now.AddHours(-18),
            ReportedAt = now.AddHours(-12),
            Location = "Beginners Workshop - Community Center",
            EncryptedDescription = await _encryptionService.EncryptAsync("New participant felt uncomfortable with pace of instruction and physical proximity during hands-on guidance. No inappropriate contact but requesting review of teaching methods."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Workshop instructor and new participant (anonymous)"),
            IsAnonymous = true,
            RequestFollowUp = true,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.ReportSubmitted,
            CreatedAt = now.AddHours(-12),
            UpdatedAt = now.AddHours(-12)
        });

        // Incident 7: Information gathering (different location)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-5):yyyyMMdd}-0001",
            Title = "Inappropriate Comments",
            ReporterId = guestUser?.Id,
            IncidentDate = now.AddDays(-6),
            ReportedAt = now.AddDays(-5),
            Location = "Monthly Munch - Salem Coffee Roasters",
            EncryptedDescription = await _encryptionService.EncryptAsync("Community member made multiple comments about others' bodies that made several attendees uncomfortable. Comments were not overtly sexual but persistently focused on physical appearance."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Multiple munch attendees reported similar concerns"),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Munch host and 3 other attendees"),
            EncryptedContactEmail = await _encryptionService.EncryptAsync("guest@witchcityrope.com"),
            IsAnonymous = false,
            RequestFollowUp = true,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.InformationGathering,
            AssignedTo = teacherUser.Id,
            CoordinatorId = teacherUser.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_005",
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2),
            CreatedBy = guestUser?.Id,
            UpdatedBy = teacherUser.Id
        });

        // Incident 8: Closed case (community conflict resolved)
        incidents.Add(new WitchCityRope.Api.Features.Safety.Entities.SafetyIncident
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"SAF-{now.AddDays(-45):yyyyMMdd}-0001",
            Title = "Conflict Resolution",
            ReporterId = memberUser?.Id,
            IncidentDate = now.AddDays(-46),
            ReportedAt = now.AddDays(-45),
            Location = "Private Event - Member's Home",
            EncryptedDescription = await _encryptionService.EncryptAsync("Disagreement between two community members escalated to yelling. No physical contact. Both parties agreed to mediation which successfully resolved the conflict."),
            EncryptedInvolvedParties = await _encryptionService.EncryptAsync("Two long-time community members"),
            EncryptedWitnesses = await _encryptionService.EncryptAsync("Event host and several other attendees"),
            EncryptedContactEmail = await _encryptionService.EncryptAsync("member@witchcityrope.com"),
            IsAnonymous = false,
            RequestFollowUp = false,
            Status = WitchCityRope.Api.Features.Safety.Entities.IncidentStatus.Closed,
            AssignedTo = adminUser.Id,
            CoordinatorId = adminUser.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_006",
            GoogleDriveFinalReportUrl = "https://docs.google.com/document/d/EXAMPLE_DOC_ID_006",
            CreatedAt = now.AddDays(-45),
            UpdatedAt = now.AddDays(-30),
            CreatedBy = memberUser?.Id,
            UpdatedBy = adminUser.Id
        });

        await _context.SafetyIncidents.AddRangeAsync(incidents, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} safety incident test records", incidents.Count);
    }
}

