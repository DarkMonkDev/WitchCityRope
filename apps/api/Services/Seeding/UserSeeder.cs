using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of roles and users.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating ASP.NET Identity roles and test user accounts.
/// </summary>
public class UserSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<UserSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates all required ASP.NET Core Identity roles for the application.
    /// Ensures roles exist before user creation and assignment.
    ///
    /// Roles created:
    /// - Administrator: Full system access
    /// - Teacher: Can create and manage events
    /// - SafetyTeam: Can manage safety incidents
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
                FirstName = "Alex",
                LastName = "Morgan",
                DiscordName = "ropemaster_admin",
                FetLifeName = "RopeMasterSalem",
                PhoneNumber = "978-555-0101",
                Bio = "Experienced rope artist and community organizer with over 10 years in the scene. Passionate about creating safe spaces for rope bondage education and practice. Administrator of WitchCityRope since 2020.",
                Role = "Administrator",
                PronouncedName = "Rope Master",
                Pronouns = "they/them",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "teacher@witchcityrope.com",
                SceneName = "SafetyFirst",
                FirstName = "Sarah",
                LastName = "Chen",
                DiscordName = "safety_teacher",
                FetLifeName = "SafetyFirstSalem",
                PhoneNumber = "978-555-0102",
                Bio = "Certified safety instructor with backgrounds in emergency medicine and risk management. Teaching rope safety and suspension techniques for 8+ years. Dedicated to making rope bondage accessible and safe for everyone.",
                Role = "Teacher",
                PronouncedName = "Safety First",
                Pronouns = "she/her",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "vetted@witchcityrope.com",
                SceneName = "RopeEnthusiast",
                FirstName = "Marcus",
                LastName = "Rivera",
                DiscordName = "rope_enthusiast",
                FetLifeName = "EnthusiastRope",
                PhoneNumber = "978-555-0103",
                Bio = "Active community member for 3 years. Love attending workshops and rope jams. Always eager to learn new ties and techniques. Specialty in decorative rope work and photography.",
                Role = "",  // No special permissions
                PronouncedName = "Rope Enthusiast",
                Pronouns = "he/him",
                VettingStatus = 3,  // Approved (vetted)
                IsActive = true
            },
            new {
                Email = "member@witchcityrope.com",
                SceneName = "Learning",
                FirstName = "Jordan",
                LastName = "Blake",
                DiscordName = "learning_rope",
                FetLifeName = "LearningTheRopes",
                PhoneNumber = "978-555-0104",
                Bio = "New to the rope community and excited to learn! Attended Introduction to Rope Safety class and looking forward to practicing basic ties. Interested in both technical and artistic aspects of rope bondage.",
                Role = "",  // No special permissions
                PronouncedName = "Learning",
                Pronouns = "they/them",
                VettingStatus = 0,  // UnderReview (not vetted)
                IsActive = true
            },
            new {
                Email = "guest@witchcityrope.com",
                SceneName = "Newcomer",
                FirstName = "Taylor",
                LastName = "Anderson",
                DiscordName = "new_to_rope",
                FetLifeName = "NewcomerToScene",
                PhoneNumber = "978-555-0105",
                Bio = "Just discovered the rope community and curious to learn more. Attended my first rope jam last month and was amazed by the creativity and skill. Looking to take classes and build confidence.",
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
                FirstName = "Riley",
                LastName = "Thompson",
                DiscordName = "safety_coord1",
                FetLifeName = "SafetyCoordRiley",
                PhoneNumber = "978-555-0106",
                Bio = "Safety team coordinator with EMT certification. 6 years experience in rope bondage with focus on risk-aware practices. Available for incident response and safety consultations. Committed to community wellbeing.",
                Role = "SafetyTeam",
                PronouncedName = "Safety Coordinator",
                Pronouns = "they/them",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            new {
                Email = "coordinator2@witchcityrope.com",
                SceneName = "IncidentHandler",
                FirstName = "Jessica",
                LastName = "Martinez",
                DiscordName = "incident_handler",
                FetLifeName = "IncidentHandlerJess",
                PhoneNumber = "978-555-0107",
                Bio = "Safety team member specializing in incident documentation and follow-up. Background in crisis intervention and conflict resolution. Dedicated to ensuring our community remains safe and accountable.",
                Role = "SafetyTeam",
                PronouncedName = "Incident Handler",
                Pronouns = "she/her",
                VettingStatus = 3,  // Approved
                IsActive = true
            },
            // Additional users for vetting application testing (users 5-16)
            new { Email = "applicant1@example.com", SceneName = "RopeNovice", FirstName = "Emma", LastName = "Wilson", DiscordName = "rope_novice", FetLifeName = "NoviceRoper", PhoneNumber = "978-555-0201", Bio = "Complete beginner interested in learning rope bondage. Seeking mentorship and classes to build foundational skills safely.", Role = "", PronouncedName = "Rope Novice", Pronouns = "she/her", VettingStatus = 0, IsActive = false },
            new { Email = "applicant2@example.com", SceneName = "KnotLearner", FirstName = "Casey", LastName = "Park", DiscordName = "knot_learner", FetLifeName = "KnotEnthusiast", PhoneNumber = "978-555-0202", Bio = "Fascinated by the technical aspects of knot work. Practicing basic ties at home and eager to learn more complex patterns from experienced riggers.", Role = "", PronouncedName = "Knot Learner", Pronouns = "they/them", VettingStatus = 0, IsActive = true },
            new { Email = "applicant3@example.com", SceneName = "TrustBuilder", FirstName = "Michael", LastName = "Lee", DiscordName = "trust_builder", FetLifeName = "TrustAndRope", PhoneNumber = "978-555-0203", Bio = "Teacher and safety team member focusing on communication and consent in rope bondage. Believe that trust and connection are the foundation of all rope work. Teaching for 5 years.", Role = "Teacher,SafetyTeam", PronouncedName = "Trust Builder", Pronouns = "he/him", VettingStatus = 3, IsActive = true },
            new { Email = "applicant4@example.com", SceneName = "SilkAndSteel", FirstName = "Olivia", LastName = "Hayes", DiscordName = "silk_and_steel", FetLifeName = "SilkSteelRope", PhoneNumber = "978-555-0204", Bio = "Rope model and photographer exploring the artistic side of rope bondage. Love the contrast between soft materials and strong techniques. Active in the community for 4 years.", Role = "", PronouncedName = "Silk And Steel", Pronouns = "she/her", VettingStatus = 3, IsActive = true },
            new { Email = "applicant5@example.com", SceneName = "EagerLearner", FirstName = "Sam", LastName = "Rodriguez", DiscordName = "eager_learner", FetLifeName = "EagerToLearn", PhoneNumber = "978-555-0205", Bio = "Enthusiastic newcomer attending every workshop and rope jam I can find! Want to absorb as much knowledge as possible and practice regularly. Open to feedback and mentorship.", Role = "", PronouncedName = "Eager Learner", Pronouns = "she/they", VettingStatus = 0, IsActive = true },
            new { Email = "applicant6@example.com", SceneName = "QuickLearner", FirstName = "David", LastName = "Kim", DiscordName = "quick_learner", FetLifeName = "QuickRopeLearner", PhoneNumber = "978-555-0206", Bio = "Pick up new techniques quickly but know I still have a lot to learn about safety and communication. Grateful for this welcoming community and patient teachers.", Role = "", PronouncedName = "Quick Learner", Pronouns = "he/him", VettingStatus = 0, IsActive = true },
            new { Email = "applicant7@example.com", SceneName = "ThoughtfulRigger", FirstName = "Morgan", LastName = "Foster", DiscordName = "thoughtful_rigger", FetLifeName = "ThoughtfulRopes", PhoneNumber = "978-555-0207", Bio = "Teacher specializing in mindful rope work and body awareness. Every tie is an opportunity for connection and presence. Teaching workshops on floor work and transitional ties for 6 years.", Role = "Teacher", PronouncedName = "Thoughtful Rigger", Pronouns = "they/them", VettingStatus = 3, IsActive = true },
            new { Email = "applicant8@example.com", SceneName = "CommunityBuilder", FirstName = "Rachel", LastName = "Cooper", DiscordName = "community_builder", FetLifeName = "BuildingCommunity", PhoneNumber = "978-555-0208", Bio = "Passionate about growing our community and making it inclusive. Help organize social events and welcome newcomers. Believe rope bondage can bring people together in meaningful ways.", Role = "", PronouncedName = "Community Builder", Pronouns = "she/her", VettingStatus = 0, IsActive = true },
            new { Email = "applicant9@example.com", SceneName = "NervousNewbie", FirstName = "Chris", LastName = "Bennett", DiscordName = "nervous_newbie", FetLifeName = "NewbieChris", PhoneNumber = "978-555-0209", Bio = "A bit nervous but excited to explore rope bondage. Appreciate the emphasis on safety and consent in this community. Taking it slow and building confidence step by step.", Role = "", PronouncedName = "Nervous Newbie", Pronouns = "he/him", VettingStatus = 0, IsActive = true },
            new { Email = "applicant10@example.com", SceneName = "RopeBunny", FirstName = "Mia", LastName = "Sullivan", DiscordName = "rope_bunny", FetLifeName = "BunnyInRopes", PhoneNumber = "978-555-0210", Bio = "Love the feeling of rope and enjoy being tied by skilled riggers. Interested in suspension work and learning more about the bottom/model perspective. Safety-conscious and communicative.", Role = "", PronouncedName = "Rope Bunny", Pronouns = "she/her", VettingStatus = 0, IsActive = true },
            new { Email = "applicant11@example.com", SceneName = "SafetyConscious", FirstName = "Alex", LastName = "Turner", DiscordName = "safety_conscious", FetLifeName = "SafetyAlways", PhoneNumber = "978-555-0211", Bio = "Teacher with unwavering focus on safety protocols and emergency preparedness. Every class includes thorough safety briefings and risk discussions. Teaching since 2018 with zero incidents.", Role = "Teacher", PronouncedName = "Safety Conscious", Pronouns = "they/them", VettingStatus = 3, IsActive = true },
            new { Email = "applicant12@example.com", SceneName = "PatientPractitioner", FirstName = "Daniel", LastName = "Wright", DiscordName = "patient_practitioner", FetLifeName = "PatientRigger", PhoneNumber = "978-555-0212", Bio = "Teacher known for patient, detailed instruction and creating supportive learning environments. Specialize in working with complete beginners and building solid fundamentals. Teaching for 7 years.", Role = "Teacher", PronouncedName = "Patient Practitioner", Pronouns = "he/him", VettingStatus = 3, IsActive = true }
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

            // Try to get optional profile fields using reflection
            var firstNameProperty = accountType.GetProperty("FirstName");
            var firstName = firstNameProperty?.GetValue(account) as string;

            var lastNameProperty = accountType.GetProperty("LastName");
            var lastName = lastNameProperty?.GetValue(account) as string;

            var fetLifeNameProperty = accountType.GetProperty("FetLifeName");
            var fetLifeName = fetLifeNameProperty?.GetValue(account) as string;

            var phoneNumberProperty = accountType.GetProperty("PhoneNumber");
            var phoneNumber = phoneNumberProperty?.GetValue(account) as string;

            var bioProperty = accountType.GetProperty("Bio");
            var bio = bioProperty?.GetValue(account) as string;

            var user = new ApplicationUser
            {
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true,
                SceneName = account.SceneName,
                FirstName = firstName,
                LastName = lastName,
                DiscordName = account.DiscordName,
                FetLifeName = fetLifeName,
                PhoneNumber = phoneNumber,
                Bio = bio,
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
}
