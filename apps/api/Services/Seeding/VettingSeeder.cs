using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of vetting system data: statuses, applications, email templates, and audit logs.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating comprehensive vetting test data with realistic workflow progressions.
/// </summary>
public class VettingSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VettingSeeder> _logger;

    public VettingSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<VettingSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds vetting status configuration.
    /// Idempotent operation.
    /// Currently a placeholder implementation - will be expanded when VettingStatusConfig entities are added.
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
    /// Seeds sample vetting applications for testing the vetting system UI.
    /// Idempotent operation - skips if applications already exist.
    ///
    /// Creates 11+ applications with variety:
    /// - Different workflow statuses (UnderReview, InterviewApproved, FinalReview, Approved, OnHold, Denied)
    /// - Realistic user data (names, scene names, FetLife handles, pronouns)
    /// - Proper workflow timestamps (submitted, review started, interview scheduled, decision made)
    /// - Links to existing seeded users where appropriate
    /// - Admin notes reflecting review process
    ///
    /// Also creates audit log entries showing workflow progression for each application.
    /// Syncs User.VettingStatus for applications in terminal states (Approved, Denied, OnHold).
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
    /// Seeds default email templates for the vetting system workflow.
    /// Idempotent operation - skips if templates already exist.
    ///
    /// Creates 6 email templates:
    /// - ApplicationReceived: Confirmation email when application submitted
    /// - InterviewApproved: Notification that application approved for interview
    /// - Approved: Welcome email for approved members
    /// - OnHold: Notification that additional information is needed
    /// - Denied: Respectful rejection notification
    /// - InterviewReminder: Reminder email before scheduled interview
    ///
    /// Each template includes:
    /// - Subject line with variable placeholders ({{scene_name}}, etc.)
    /// - HTML body with formatted content
    /// - Plain text body for email clients without HTML support
    /// - Variable list for template rendering
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
    /// Creates audit log entries showing workflow progression for vetting applications.
    /// Generates realistic audit trail based on current workflow status.
    ///
    /// Audit trail patterns:
    /// - UnderReview: Initial submission only
    /// - InterviewApproved: Submission → InterviewApproved
    /// - FinalReview: Submission → InterviewApproved → FinalReview
    /// - Approved: Submission → InterviewApproved → FinalReview → Approved
    /// - OnHold: Submission → OnHold
    /// - Denied: Submission → Denied
    ///
    /// Each log entry includes:
    /// - Action description
    /// - Performer (user who made the change)
    /// - Timestamp (proper workflow timing)
    /// - Old/new status values
    /// - Contextual notes about the decision
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
