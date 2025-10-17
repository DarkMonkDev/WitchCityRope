using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Cms;

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

        var roles = new[] { "Administrator", "Teacher", "VettedMember", "Member", "Attendee" };
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
            },
            // Additional users for vetting application testing (users 5-16)
            new { Email = "applicant1@example.com", SceneName = "RopeNovice", Role = "Attendee", PronouncedName = "Rope Novice", Pronouns = "she/her", IsVetted = false },
            new { Email = "applicant2@example.com", SceneName = "KnotLearner", Role = "Attendee", PronouncedName = "Knot Learner", Pronouns = "they/them", IsVetted = false },
            new { Email = "applicant3@example.com", SceneName = "TrustBuilder", Role = "Attendee", PronouncedName = "Trust Builder", Pronouns = "he/him", IsVetted = false },
            new { Email = "applicant4@example.com", SceneName = "SilkAndSteel", Role = "Attendee", PronouncedName = "Silk And Steel", Pronouns = "she/her", IsVetted = false },
            new { Email = "applicant5@example.com", SceneName = "EagerLearner", Role = "Attendee", PronouncedName = "Eager Learner", Pronouns = "she/they", IsVetted = false },
            new { Email = "applicant6@example.com", SceneName = "QuickLearner", Role = "Attendee", PronouncedName = "Quick Learner", Pronouns = "he/him", IsVetted = false },
            new { Email = "applicant7@example.com", SceneName = "ThoughtfulRigger", Role = "Attendee", PronouncedName = "Thoughtful Rigger", Pronouns = "they/them", IsVetted = false },
            new { Email = "applicant8@example.com", SceneName = "CommunityBuilder", Role = "Attendee", PronouncedName = "Community Builder", Pronouns = "she/her", IsVetted = false },
            new { Email = "applicant9@example.com", SceneName = "NervousNewbie", Role = "Attendee", PronouncedName = "Nervous Newbie", Pronouns = "he/him", IsVetted = false },
            new { Email = "applicant10@example.com", SceneName = "RopeBunny", Role = "Attendee", PronouncedName = "Rope Bunny", Pronouns = "she/her", IsVetted = false },
            new { Email = "applicant11@example.com", SceneName = "SafetyConscious", Role = "Attendee", PronouncedName = "Safety Conscious", Pronouns = "they/them", IsVetted = false },
            new { Email = "applicant12@example.com", SceneName = "PatientPractitioner", Role = "Attendee", PronouncedName = "Patient Practitioner", Pronouns = "he/him", IsVetted = false }
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
                VettingStatus = account.IsVetted ? 4 : 0, // 4 = Approved for vetted users, 0 = UnderReview for non-vetted

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
                longDescription: @"This comprehensive introduction to rope safety is designed for absolute beginners and those looking to refresh their fundamental knowledge.

In this 3-hour workshop, you'll learn:
- Essential safety principles and risk awareness for rope bondage
- Communication techniques including consent negotiation and safe words
- Basic rope handling skills and material selection (jute, hemp, synthetic)
- Recognition of nerve compression and circulation issues
- Emergency release procedures and safety protocols
- How to create a safe practice environment

Prerequisites: None - this is a beginner-friendly class designed for people with no prior rope experience.

Materials:
- Two 30-foot lengths of 6mm rope will be provided (jute or hemp)
- You may bring your own rope if preferred
- Comfortable clothing that allows movement is required

Class Structure:
- 45 minutes: Safety theory and communication fundamentals
- 90 minutes: Hands-on practice with instructor guidance
- 30 minutes: Q&A and resource sharing
- 15 minutes: Community discussion and next steps

This class emphasizes building a strong foundation in safety practices that will serve you throughout your rope journey. We focus on understanding risks, developing good habits, and creating positive experiences for all participants.",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- Advance registration and payment required (no walk-ins)
- Arrive 15 minutes early for check-in and orientation

Consent and Communication:
- Enthusiastic consent is required for all activities and interactions
- Respect all safe words, boundaries, and limits without question
- ""No"" means no - immediately stop any activity when requested
- Ask before touching others or their belongings

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit written permission from all participants
- Cell phones must be on silent and stored during class
- Violations will result in immediate removal without refund

Appropriate Conduct and Attire:
- Professional and respectful behavior expected at all times
- Wear comfortable, non-restrictive clothing suitable for movement
- Remove jewelry that may interfere with rope work
- Closed-toe shoes recommended for safety

Health and Safety:
- Inform instructor of any medical conditions, injuries, or mobility limitations
- Practice good hygiene - shower before attending
- Do not attend if you are ill or contagious
- Report any injuries or safety concerns immediately to staff

Scent-Free Environment:
- Please avoid strong perfumes, colognes, or scented products
- Some community members have chemical sensitivities

Zero Tolerance Policies:
- No harassment, discrimination, or predatory behavior of any kind
- No drugs or alcohol on premises
- Violation of policies may result in permanent ban from all community events

By attending, you agree to abide by all policies and accept responsibility for your own safety and well-being."
            ),

            CreateSeedEvent(
                title: "Suspension Basics",
                daysFromNow: 14,
                startHour: 18,
                capacity: 12,
                eventType: EventType.Class,
                price: 65.00m,
                shortDescription: "Introduction to suspension techniques with emphasis on safety and proper rigging.",
                longDescription: @"Take your rope skills to the next level with this comprehensive introduction to suspension bondage. This intermediate-level workshop covers the essential techniques, safety considerations, and rigging fundamentals needed to begin exploring suspension safely.

What You'll Learn:
- Suspension safety principles and risk mitigation strategies
- Proper rigging setup and equipment requirements (hard points, rigging plates, carabiners)
- Load-bearing tie techniques and weight distribution
- Hip harness construction with emphasis on safety and comfort
- Partial suspension techniques for beginners
- How to assess and monitor your partner during suspension
- Safe descent and emergency procedures
- Common mistakes and how to avoid them

Prerequisites:
- REQUIRED: Strong foundation in floor bondage techniques
- REQUIRED: Completion of 'Introduction to Rope Safety' or equivalent
- Recommended: 6+ months of regular rope practice
- You must be comfortable tying basic harnesses and understand rope safety fundamentals

Equipment Provided:
- Suspension rigging equipment (hard points, carabiners, etc.)
- Practice rope if needed

What to Bring:
- 4-6 lengths of rope (30 feet each, 6mm diameter)
- Comfortable, form-fitting clothing (leggings, fitted shirt)
- Water bottle and snacks
- Notebook for taking notes (optional)

Class Format:
- 30 minutes: Suspension theory, safety review, and equipment overview
- 2 hours: Hands-on practice with instructor supervision
- 30 minutes: Q&A, troubleshooting, and safety discussion

Safety Note: Suspension carries inherent risks including nerve damage, circulation issues, and injury from falls. This class emphasizes conservative, safety-first approaches suitable for beginners. You will learn to recognize warning signs and respond appropriately.

Limited to 12 participants to ensure personalized instruction and adequate safety supervision.",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- Advance registration and payment required (no walk-ins)
- Prerequisite: Completion of rope safety fundamentals course or instructor approval
- Arrive 15 minutes early for safety briefing and equipment setup

Medical and Physical Requirements:
- You must disclose any medical conditions, injuries, or physical limitations to instructor before class
- Certain conditions may prevent safe participation in suspension activities
- You are responsible for knowing your own health status and limitations
- Sign medical waiver acknowledging suspension risks before participating

Consent and Communication:
- Enthusiastic consent required for all activities and physical contact
- Active communication expected throughout suspension scenes
- Immediate stop on any safe word or request to stop
- Check-ins required at regular intervals during suspension

Equipment and Safety:
- Do not touch or adjust rigging equipment without instructor permission
- Inspect all equipment before use and report any damage or concerns
- Follow all instructor directions regarding equipment usage
- Spotter required for all suspension activities

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit written permission from all participants and instructor
- Cell phones must be on silent and stored during class

Appropriate Conduct:
- Professional behavior required at all times
- Wear appropriate athletic/practice clothing (no loose garments)
- Remove all jewelry before participating in suspension
- Notify instructor immediately of any discomfort, numbness, tingling, or other concerns

Zero Tolerance:
- No harassment, discrimination, or boundary violations
- No drugs or alcohol - immediate removal and ban
- Suspension is an advanced skill requiring focus and sobriety

Risk Acknowledgment:
- Suspension bondage carries inherent risks including nerve damage, circulation issues, rope burns, bruising, and injury from falls
- By attending, you acknowledge these risks and accept responsibility for your participation
- You agree to follow all safety protocols and instructor guidance"
            ),

            CreateSeedEvent(
                title: "Advanced Floor Work",
                daysFromNow: 21,
                startHour: 18,
                capacity: 10,
                eventType: EventType.Class,
                price: 55.00m,
                shortDescription: "Explore complex floor-based rope bondage techniques for experienced practitioners.",
                longDescription: @"This advanced workshop is designed for experienced rope practitioners looking to expand their floor bondage repertoire with complex ties, creative positions, and artistic expression. We'll explore sophisticated techniques that challenge both rigger and model while maintaining safety and comfort.

Workshop Topics:
- Complex multi-point ties and connection techniques
- Asymmetrical and artistic floor positions
- Creative use of space and body positioning
- Transitions between positions while maintaining tie integrity
- Strappado, hogtie, and other challenging positions
- Decorative elements and aesthetic considerations
- Managing longer scenes and endurance considerations
- Troubleshooting common issues with complex ties

Prerequisites:
- REQUIRED: Extensive floor bondage experience (1+ years regular practice)
- REQUIRED: Strong foundation in safety, communication, and basic ties
- Must be proficient in single column, double column, chest harnesses, and hip harnesses
- Experience with multi-limb ties and transitions
- This is NOT a beginner class

What to Bring:
- 6-8 lengths of rope (30 feet each, 6mm diameter) - quality rope recommended
- Comfortable form-fitting practice clothing
- Knee pads optional but recommended
- Water and snacks
- Notebook for notes and sketches

Class Structure:
- 20 minutes: Demonstration of key concepts and techniques
- 2 hours: Hands-on practice with instructor feedback
- 20 minutes: Sharing, feedback, and Q&A

Teaching Philosophy:
This class emphasizes creative exploration within safe parameters. We'll focus on developing your personal style while respecting the fundamentals that keep everyone safe. Expect to be challenged and to learn from both successes and mistakes.

Small class size (limited to 10 participants) ensures individualized attention and the ability to work at your own pace.

Note: Partners/models welcome but not required. Solo practitioners can focus on learning tie techniques and theory.",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- Advance registration and payment required
- Prerequisite: Extensive rope experience and instructor approval
- Late arrival may result in denied entry for safety reasons

Experience Verification:
- This is an advanced class - beginners will not be admitted
- Instructor reserves right to assess skill level and decline participation
- If your experience level is uncertain, contact instructor before registering

Safety and Communication:
- Expert-level communication and consent skills expected
- Proactive monitoring of partner required throughout scenes
- Immediate stop on any safe word, numbness, tingling, or discomfort
- Report all safety concerns to instructor immediately

Equipment Requirements:
- Bring your own quality rope in good condition
- Inspect rope before use and remove any damaged rope from rotation
- Recommended: natural fiber rope (jute/hemp) for better grip and control

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit written permission from all participants
- This is a closed, private workshop
- Cell phones must be stored during practice sessions

Professional Conduct:
- Respectful, professional behavior required at all times
- Appropriate practice attire (form-fitting, allows movement)
- Practice good hygiene
- No unsolicited advice or critique of other participants

Health and Disclosure:
- Disclose relevant medical conditions, injuries, or limitations
- You are responsible for your own physical and mental well-being
- Take breaks as needed
- Stay hydrated

Zero Tolerance Policies:
- No harassment, boundary violations, or predatory behavior
- No drugs or alcohol
- Violations result in immediate removal and permanent ban

Assumption of Risk:
- Advanced rope work carries increased risk of injury
- By participating, you acknowledge these risks and accept responsibility
- You agree to practice safely and follow all instructor guidance
- Nerve damage, bruising, rope burns, and other injuries are possible"
            ),

            CreateSeedEvent(
                title: "Community Rope Jam",
                daysFromNow: 28,
                startHour: 19,
                capacity: 40,
                eventType: EventType.Social,
                price: 15.00m,
                shortDescription: "Casual practice session for all skill levels. Bring your rope and practice with the community.",
                longDescription: @"Join us for our monthly Community Rope Jam - a relaxed, social practice session where rope enthusiasts of all skill levels can come together to practice, learn, and connect with fellow community members.

What to Expect:
- Open practice space with supportive atmosphere
- Practice your existing skills or try new techniques
- Ask questions and learn from experienced practitioners
- Make new friends and connections in the community
- Casual, judgment-free environment

Who Should Attend:
- ALL SKILL LEVELS welcome from absolute beginners to advanced practitioners
- Solo practitioners welcome - you don't need to bring a partner
- People interested in learning more about rope bondage
- Experienced rope artists looking to practice and share knowledge
- Anyone seeking community connection

Facilitators Present:
- Experienced community members available to answer questions
- Safety monitors on site
- Not a formal class, but informal guidance available

What to Bring:
- Your own rope (if you have it) - we'll have some available to borrow
- Comfortable clothing suitable for movement
- Water bottle
- Positive attitude and respect for all participants

Activities:
- Self-directed practice at your own pace
- Informal peer learning and knowledge sharing
- Socializing and community building
- Optional: Share what you're working on with supportive feedback

Perfect For:
- Practicing ties you've learned in classes
- Experimenting with new ideas in a safe environment
- Meeting practice partners
- Building confidence in your rope skills
- Connecting with the local rope community

No pressure, no judgment - just a welcoming space to explore rope at your own comfort level. Whether you tie for 5 minutes or the whole session, you're welcome here.

Optional donation-based entry - pay what you can to help cover venue costs.",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- RSVP required for space planning
- Sliding scale pricing ($5-15) - pay what you can
- All skill levels welcome

Safety First:
- Practice safely within your skill level
- Do not attempt techniques beyond your current abilities
- Ask for help if you're unsure about safety
- Safety monitors present to assist with concerns

Consent and Boundaries:
- Enthusiastic consent required for all interactions
- Ask before approaching others or touching their belongings
- Respect everyone's boundaries and personal space
- ""No"" means no - respect it immediately

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit written permission from ALL people who might appear in the shot
- Ask permission EACH time before taking photos
- Respect others' privacy and anonymity

Respectful Environment:
- Treat all participants with respect regardless of experience level
- No unsolicited critique or advice
- Keep coaching to your own partner unless specifically invited to help
- Welcoming, judgment-free atmosphere

Appropriate Conduct:
- Professional behavior expected
- Comfortable, appropriate clothing for rope practice
- Practice good hygiene
- No drugs or alcohol on premises

Space Sharing:
- Be mindful of shared space - keep practice areas compact
- Clean up your area when finished
- Return borrowed equipment to proper location
- Help maintain welcoming environment for all

Bring Your Own Equipment:
- Bring your own rope if possible (limited rope available to borrow)
- Bring water and any personal items needed
- Label your belongings

Zero Tolerance:
- No harassment, discrimination, or predatory behavior
- Violations result in immediate removal and permanent ban
- Community safety is our top priority

Practice at Your Own Risk:
- This is not a supervised class
- You are responsible for your own safety
- Practice within your abilities and knowledge
- Ask for help when needed"
            ),

            CreateSeedEvent(
                title: "Rope Social & Discussion",
                daysFromNow: 35,
                startHour: 19,
                capacity: 30,
                eventType: EventType.Social,
                price: 10.00m,
                shortDescription: "Monthly social gathering for community connection and discussion of rope topics.",
                longDescription: @"Join us for our monthly Rope Social & Discussion - a casual gathering focused on community building, conversation, and connection. This is a chance to meet fellow rope enthusiasts, discuss topics of interest, and strengthen our community bonds.

Event Format:
- Social hour: Mingle and meet community members
- Discussion circle: Structured conversation on a monthly topic
- Q&A and open discussion
- Resource sharing and announcements

Monthly Discussion Topics (rotating):
- Rope philosophy and personal practice
- Navigating consent and communication
- Building a sustainable rope practice
- Rope community culture and values
- Safety deep dives on specific topics
- Artistic expression through rope
- And more based on community interest

Who Should Attend:
- Anyone interested in rope bondage at any skill level
- People curious about the rope community
- Experienced practitioners looking to connect
- Those seeking a thoughtful, discussion-oriented space
- Community members interested in building connections

What Makes This Different:
- TALKING focused, not practice focused
- Emphasis on community and connection over techniques
- Safe space to ask questions and share experiences
- Opportunity to learn from others' perspectives
- Building friendships and support networks

Topics Covered (examples from past months):
- How do you maintain work-life-rope balance?
- What does ethical rope practice mean to you?
- How do you handle rope injuries and recovery?
- Building diverse and inclusive community spaces
- Your questions and topics welcome!

Atmosphere:
- Relaxed and welcoming
- Judgment-free zone for all questions
- Respectful discussion and active listening
- Building understanding across experience levels

No rope practice at this event - this is purely social and discussion-based. Bring your curiosity, questions, and openness to connect with others.

Light refreshments provided. Optional sliding scale donation to support community programming.",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- RSVP requested for space planning and refreshment prep
- Sliding scale pricing ($5-10) - pay what you can
- All backgrounds and experience levels welcome

Respectful Discussion:
- This is a conversation-based event, not a practice session
- Respect all perspectives and experiences shared
- No interrupting or talking over others
- Active listening encouraged
- Disagree respectfully if you have different views

Confidentiality:
- What's shared in the discussion circle stays in the discussion circle
- Don't repeat personal stories or identifying information shared by others
- Respect privacy and anonymity of all participants

Safe Space Guidelines:
- No judgment for questions asked or experiences shared
- Create welcoming environment for all experience levels
- Step up/step back - make room for quieter voices
- Assume good intent while allowing for mistakes and learning

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit permission from ALL participants
- This is a private gathering
- Respect attendees' privacy and anonymity

Appropriate Conduct:
- Professional, respectful behavior required
- No recruitment for personal relationships or services
- No sales pitches or self-promotion
- Focus on community building and authentic connection

Accessibility:
- We strive to create accessible, welcoming space
- Let organizers know if you have accessibility needs
- Gender-neutral restrooms available
- Scent-free preferred (some members have sensitivities)

Food and Beverages:
- Light refreshments provided
- Let us know of any allergies or dietary restrictions
- Clean up after yourself
- No alcohol served

Zero Tolerance:
- No harassment, discrimination, or boundary violations
- No drugs or alcohol
- Violations result in removal and ban from community events

Community Care:
- Look out for each other
- Let organizers know if someone seems in distress
- This is a community space - we all contribute to making it safe and welcoming"
            ),

            CreateSeedEvent(
                title: "New Members Meetup",
                daysFromNow: 42,
                startHour: 18,
                capacity: 25,
                eventType: EventType.Social,
                price: 5.00m,
                shortDescription: "Welcome gathering for new community members to meet established practitioners and learn about upcoming events.",
                longDescription: @"Welcome to the WitchCityRope community! This special meetup is designed specifically for new members to get oriented, meet friendly faces, and learn about all the opportunities our community offers.

Event Overview:
This casual, welcoming gathering helps new members feel comfortable and connected from day one. Whether you're brand new to rope or experienced but new to our community, this event is your gateway to getting involved.

What We'll Cover:
- Welcome and community overview
- Introduction to our values, culture, and code of conduct
- Overview of upcoming classes, events, and programming
- How to get the most out of your membership
- Q&A about the community, practices, and events
- Meet established community members who can offer guidance
- Connect with other new members

Who Should Attend:
- New community members (joined within last 3 months)
- People considering membership and want to learn more
- Anyone who feels new and wants to build connections
- Established members welcome to attend as greeters/mentors

What to Expect:
- Friendly, welcoming atmosphere designed to reduce ""new person"" anxiety
- Structured introductions to break the ice
- Small group discussions to meet others
- Resource sharing (calendars, communication channels, etc.)
- Opportunity to ask all your questions in a supportive environment

You'll Learn About:
- Different types of events (classes, practice jams, socials, performances)
- How to register for events and manage your membership
- Community communication channels and staying connected
- Finding practice partners and building connections
- Volunteer opportunities and community involvement
- Vetting process and member-only events
- Resources for continued learning

Meet Our Community:
- Greeters assigned to help new members feel welcome
- Experienced members available to answer questions
- Other new members to connect with
- Leadership and event organizers

No Rope Required:
This is a social and informational event - no rope practice or experience necessary. Just bring yourself and your curiosity!

Light refreshments provided. Suggested $5 donation (free for those experiencing financial hardship).",
                policies: @"Event Policies and Safety Guidelines:

Attendance Requirements:
- All participants must be 18+ years old with valid government-issued photo ID
- RSVP requested for name tags and refreshment planning
- Sliding scale pricing ($0-5) - pay what you can
- Both prospective and current members welcome

Welcoming Environment:
- This is explicitly a beginner-friendly, judgment-free space
- All questions are welcome and encouraged
- No experience with rope bondage required
- Focus on building community connections

Confidentiality and Privacy:
- Respect privacy of all attendees
- Don't share identifying information about who attended
- What's discussed stays within the community
- Use scene names if preferred for anonymity

Respectful Interaction:
- Treat all attendees with respect and kindness
- No recruitment for personal relationships
- Professional boundaries maintained
- This is about community building, not dating

Getting to Know You:
- We'll do brief introductions (optional to share or pass)
- Share whatever you're comfortable with
- Use scene name or first name only if preferred
- No pressure to share personal details

Photography and Recording:
- Absolutely no photography, video, or audio recording without explicit permission from ALL participants
- Name tags available but optional
- Respect wishes of those who prefer anonymity

Accessibility:
- Please let organizers know of any accessibility needs
- We strive to create welcoming space for all
- Gender-neutral restrooms available
- Scent-free environment preferred

Food and Beverages:
- Light refreshments provided
- Let us know of allergies or dietary restrictions
- Clean up after yourself
- No alcohol served

Information Sharing:
- Feel free to ask questions about community
- Event organizers available to discuss programming
- Take informational materials and resources
- Connect via official community channels only

Zero Tolerance:
- No harassment, discrimination, or predatory behavior
- No boundary violations or inappropriate advances
- Violations result in removal and ban from all events
- Community safety is our highest priority

Community Values:
- Consent, respect, and safety above all
- Inclusive and welcoming to all backgrounds
- Support for learning and growth
- Building authentic connections and friendships"
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
                longDescription: @"This past event was an introductory practice session designed for absolute beginners and those new to the rope community.

Past attendees learned:
- Basic rope handling techniques
- Simple single column ties
- Introduction to safety principles
- Communication fundamentals
- How to get started with rope practice

This was a supportive, beginner-friendly environment where participants could ask questions, meet fellow newcomers, and start their rope journey in a welcoming space.",
                policies: @"Standard event policies applied:
- 18+ with ID required
- Consent and communication required
- No photography without permission
- Respectful conduct expected
- Zero tolerance for harassment"
            ),

            CreateSeedEvent(
                title: "Rope Fundamentals Series",
                daysFromNow: -14,
                startHour: 17,
                capacity: 15,
                eventType: EventType.Class,
                price: 40.00m,
                shortDescription: "Past event: Multi-session fundamentals course for serious students.",
                longDescription: @"This past 4-week course provided comprehensive instruction in rope bondage fundamentals for dedicated students.

Course Covered:
Week 1: Safety, consent, and communication foundations
Week 2: Single column ties and basic cuffs
Week 3: Double column ties and connecting techniques
Week 4: Basic chest harnesses and body ties

Prerequisites: Commitment to attend all 4 sessions

Past participants built a strong foundation in rope fundamentals with structured curriculum and progressive skill development.",
                policies: @"Standard class policies applied:
- 18+ with ID required
- All 4 sessions attendance required
- Safety assessment and medical waiver
- Consent protocols mandatory
- No photography without permission
- Professional conduct required"
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

            // Application 4: Interview Scheduled - interview scheduled
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
                WorkflowStatus = VettingStatus.FinalReview, // FinalReview (2) - Interview complete, awaiting decision
                SubmittedAt = DateTime.UtcNow.AddDays(-14),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-10),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-3), // Interview completed 3 days ago
                AdminNotes = "Interview completed on Saturday. Applicant showed good understanding of consent and safety principles."
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

            // Application 8: Recent submission - just submitted
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
                    WorkflowStatus = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    AdminNotes = null
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

            // Application 12: SafetyFirst - An experienced rigger from another city
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
                    WorkflowStatus = VettingStatus.UnderReview,
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
                if (application.WorkflowStatus == VettingStatus.Approved)
                {
                    user.IsVetted = true;
                }
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
                Subject = "Application Received - {{applicant_name}}",
                HtmlBody = @"Dear {{applicant_name}},

Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.

Application Number: {{application_number}}
Submission Date: {{submission_date}}

Our vetting team will review your application and contact you within the next 7-10 business days with updates on your status.

If you have any questions, please don't hesitate to contact us.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{applicant_name}},

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
                HtmlBody = @"Dear {{applicant_name}},

Congratulations! Your vetting application has been approved for the interview stage.

Application Number: {{application_number}}
Next Steps: Please schedule your interview using the link below
Interview Scheduling: {{interview_link}}

During your interview, we will discuss your experience, interests, and answer any questions you may have about our community.

Please schedule your interview within the next 14 days.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{applicant_name}},

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
                TemplateType = EmailTemplateType.Approved,
                Subject = "Welcome to WitchCityRope - {{applicant_name}}",
                HtmlBody = @"Dear {{applicant_name}},

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
                PlainTextBody = @"Dear {{applicant_name}},

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
                TemplateType = EmailTemplateType.OnHold,
                Subject = "Application On Hold - Additional Information Needed - {{applicant_name}}",
                HtmlBody = @"Dear {{applicant_name}},

Your vetting application is currently on hold as we need some additional information to proceed.

Application Number: {{application_number}}
Reason: {{hold_reason}}

Required Actions:
{{required_actions}}

Please provide the requested information within 30 days to avoid application expiration.

If you have any questions about what's needed, please contact us.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{applicant_name}},

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
                TemplateType = EmailTemplateType.Denied,
                Subject = "Application Status Update - {{applicant_name}}",
                HtmlBody = @"Dear {{applicant_name}},

Thank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.

Application Number: {{application_number}}
Review Date: {{review_date}}

This decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.

We appreciate your interest in our community.

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{applicant_name}},

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
                HtmlBody = @"Dear {{applicant_name}},

This is a friendly reminder about your upcoming vetting interview.

Application Number: {{application_number}}
Interview Date: {{interview_date}}
Interview Time: {{interview_time}}
Location: {{interview_location}}

If you need to reschedule, please contact us at least 24 hours in advance.

We look forward to meeting with you!

Best regards,
The WitchCityRope Vetting Team",
                PlainTextBody = @"Dear {{applicant_name}},

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
            .Where(e => (e.EventType == "Class" || e.EventType.Contains("Workshop")) &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var upcomingSocial = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EventType.Contains("Social") &&
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
                .Where(tt => tt.Price > 0 && !tt.IsRsvpMode)
                .OrderBy(tt => tt.Price)
                .FirstOrDefault();

            if (paidTicket != null)
            {
                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = paidTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-5),
                    Quantity = 1,
                    TotalPrice = paidTicket.Price * 0.75m, // Sliding scale discount
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
                .Where(tt => tt.IsRsvpMode || tt.Price == 0)
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
                var totalPrice = pastTicketType.Price > 0 ? pastTicketType.Price * 0.5m : 0; // Sliding scale if paid

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = pastTicketType.Id,
                    PurchaseDate = purchaseDate,
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = pastTicketType.Price > 0 ? "Stripe" : "RSVP",
                    PaymentReference = pastTicketType.Price > 0 ? $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}" : $"RSVP_{Guid.NewGuid().ToString()[..8]}",
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
                var isSocialEvent = additionalEvent.EventType.Contains("Social");
                var price = isSocialEvent ? 0 : ticketType.Price * 0.6m; // Sliding scale

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
        var ticketPurchaseCount = await _context.TicketPurchases.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0 || vettingApplicationCount == 0 || ticketPurchaseCount == 0;

        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, VettingApplications={VettingApplicationCount}, TicketPurchases={TicketPurchaseCount}, Required={IsRequired}",
            userCount, eventCount, vettingApplicationCount, ticketPurchaseCount, isRequired);

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
}

