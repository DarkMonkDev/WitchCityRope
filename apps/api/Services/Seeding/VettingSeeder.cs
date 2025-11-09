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
                OtherNames = "Master, RM", // Alternate names for admin
                ExperienceLevel = 4, // Expert
                YearsExperience = 10,
                ExperienceDescription = "Professional rigger and educator with 10+ years experience in rope bondage. Have taught at multiple regional conferences and run regular workshops. Specialize in suspension and advanced floor work with strong emphasis on safety protocols.",
                WhyJoinCommunity = "As a founding member, I want to help build a safe, consent-focused community in Salem where experienced practitioners and newcomers alike can learn and grow together. I'm passionate about education and creating a welcoming space for all.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-365), // Applied a year ago
                ReviewStartedAt = DateTime.UtcNow.AddDays(-364),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-362),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-360)
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
                OtherNames = "Enthusiast, RE",
                ExperienceLevel = 3, // Advanced
                YearsExperience = 5,
                ExperienceDescription = "Experienced rigger with 5 years of practice, skilled in suspensions and complex floor work. Have assisted with teaching at local workshops and prioritize safety and communication in all rope sessions.",
                WhyJoinCommunity = "I've recently moved to Salem and want to continue my rope journey with a supportive community. Looking forward to both learning from experienced practitioners and sharing my knowledge with newer members.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-180), // Applied 6 months ago
                ReviewStartedAt = DateTime.UtcNow.AddDays(-178),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-175),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-170)
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
                ExperienceLevel = 1, // Beginner
                YearsExperience = 0,
                ExperienceDescription = "I'm completely new to rope bondage but have been researching for several months. I've read safety guides and watched educational videos. Very eager to learn from experienced practitioners in a safe environment.",
                WhyJoinCommunity = "I want to learn rope bondage in a safe, consent-focused environment from experienced teachers. I'm committed to understanding the safety fundamentals and building a strong foundation.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.UnderReview, // UnderReview (0)
                SubmittedAt = DateTime.UtcNow.AddDays(-3)
            },

            // Application 3: On Hold - needs more experience (CORRECTED STATUS)
            new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = users[1].Id, // Second non-admin user
                SceneName = "KnotLearner",
                RealName = "Jordan Kim",
                Email = "jordan.kim@email.com",
                FetLifeHandle = "KnotLearner_JK",
                Pronouns = "they/them",
                OtherNames = "JK",
                ExperienceLevel = 1, // Beginner
                YearsExperience = 1,
                ExperienceDescription = "I've attended a few workshops over the past year and practiced basic ties with a partner. Still learning fundamentals but very enthusiastic about rope and community.",
                WhyJoinCommunity = "I want to deepen my rope knowledge and learn from more experienced practitioners. I'm particularly interested in understanding the deeper aspects of consent and communication in rope.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.OnHold, // OnHold (5) - CORRECTED from InterviewApproved
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-7)
            },

            // Application 4: Denied - poor consent understanding (CORRECTED STATUS)
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
                ExperienceLevel = 2, // Intermediate
                YearsExperience = 2,
                ExperienceDescription = "I've been practicing rope bondage for 2 years and know most basic ties. I focus on speed and efficiency in tying and like to push boundaries to help my partners grow.",
                WhyJoinCommunity = "I want access to more advanced techniques and to find new practice partners who are serious about rope. Looking to take my skills to the next level.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.Denied, // Denied (4) - CORRECTED from Approved
                SubmittedAt = DateTime.UtcNow.AddDays(-14),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-10),
                InterviewScheduledFor = DateTime.UtcNow.AddDays(-8),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-5),
                LastReviewedAt = DateTime.UtcNow.AddDays(-5)
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
                ExperienceLevel = 3, // Advanced
                YearsExperience = 6,
                ExperienceDescription = "I've been practicing rope bondage for 6 years with strong foundation in both technical skills and safety. Experience includes floor work, partial suspension, and have assisted teaching beginner workshops. I prioritize clear communication and safety protocols in all my rope work.",
                WhyJoinCommunity = "I recently relocated to Salem for work and am looking to connect with the local rope community. I want to continue learning from other experienced practitioners while also sharing my knowledge and helping newcomers develop good safety habits.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.Approved, // Approved (4)
                SubmittedAt = DateTime.UtcNow.AddDays(-21),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-18),
                DecisionMadeAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        // Add remaining applications to main list
        sampleApplications.AddRange(remainingApplications);

        // Add additional applications if we have more users to prevent constraint violations
        // Only add more applications if we have users beyond the first 4 (admin is excluded from users list)
        if (users.Count >= 5)
        {
            var additionalApplications = new List<VettingApplication>();

            // Application 6: Interview Approved - ready for interview (CORRECTED STATUS)
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
                ExperienceLevel = 2, // Intermediate
                YearsExperience = 2,
                ExperienceDescription = "I've been practicing rope bondage for about 2 years, comfortable with single-column ties and basic suspension prep. I've taken several workshops and practice regularly with a trusted partner, always prioritizing safety and consent.",
                WhyJoinCommunity = "I want to connect with like-minded individuals and expand my rope knowledge in a structured, safe environment. I'm particularly interested in learning more advanced techniques and understanding the deeper emotional aspects of rope work.",
                AgreesToGuidelines = true,
                AgreesToTerms = true,
                ConsentToContact = true,
                WorkflowStatus = VettingStatus.InterviewApproved, // InterviewApproved (1) - CORRECTED from OnHold
                SubmittedAt = DateTime.UtcNow.AddDays(-12),
                ReviewStartedAt = DateTime.UtcNow.AddDays(-8)
            });

            // Application 7: Under Review - recent submission (CORRECTED STATUS)
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
                    ExperienceLevel = 1, // Beginner
                    YearsExperience = 1,
                    ExperienceDescription = "I've attended a couple of beginner workshops and have been practicing basic ties for about a year. Still learning the fundamentals but dedicated to improving my skills and understanding of safety.",
                    WhyJoinCommunity = "I'm looking for a supportive community where I can learn proper rope techniques and safety practices. I want to connect with experienced practitioners who can mentor me as I develop my skills.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.UnderReview, // UnderReview (0) - CORRECTED from Denied
                    SubmittedAt = DateTime.UtcNow.AddDays(-20),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-17),
                    DecisionMadeAt = null
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
                    ExperienceLevel = 4, // Expert
                    YearsExperience = 8,
                    ExperienceDescription = "Professional rigger with 8 years of experience specializing in both performance and educational rope work. Have taught workshops at regional events and maintain a strong focus on safety, communication, and consent. Experienced with suspensions and complex floor work.",
                    WhyJoinCommunity = "I'm interested in teaching and giving back to the local rope community. I have extensive experience and want to help create a safe, educational environment where practitioners of all levels can learn and grow.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-30),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-25),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-20),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-15),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-15)
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
                    ExperienceLevel = 3, // Advanced
                    YearsExperience = 5,
                    ExperienceDescription = "I've been an active member of the Boston rope community for 5 years, with experience in both performing and teaching. I'm skilled in floor work and partial suspensions, and have helped organize community events and workshops.",
                    WhyJoinCommunity = "I'm relocating to Salem and want to continue my involvement in the rope community. I hope to bring my experience in community building and event organization to help support this group while continuing to learn and grow.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.InterviewApproved,
                    SubmittedAt = DateTime.UtcNow.AddDays(-8),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-5)
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
                    ExperienceLevel = 1, // Beginner
                    YearsExperience = 0,
                    ExperienceDescription = "I'm very new to rope bondage and honestly quite nervous but also excited to learn. I've been reading about safety and consent extensively and attended one beginner workshop. I want to make sure I learn the right way from the start.",
                    WhyJoinCommunity = "I'm looking for a welcoming, educational environment where I can learn proper rope techniques and safety practices. I'm committed to being a respectful member and learning from more experienced practitioners.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-5)
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
                    OtherNames = "Riley C",
                    ExperienceLevel = 2, // Intermediate
                    YearsExperience = 3,
                    ExperienceDescription = "I've been exploring rope bondage as a bottom for about 3 years. I've worked with several experienced riggers and have a solid understanding of my body, my limits, and clear communication. I'm interested in deepening my practice and understanding the technical aspects better.",
                    WhyJoinCommunity = "I want to connect with the local rope community and continue learning about rope from the bottom's perspective. I'm also interested in understanding more about the technical side and potentially learning to tie as well.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.UnderReview,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2)
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
                    ExperienceLevel = 4, // Expert
                    YearsExperience = 9,
                    ExperienceDescription = "Professional rigger with 9 years of experience, specializing in safety education and risk awareness. Have taught workshops at national conferences and served as a safety monitor at major events. Extensive experience with suspensions, complex floor work, and emergency response protocols.",
                    WhyJoinCommunity = "I'm relocating from Portland and am passionate about teaching safety-focused rope practices. I want to contribute my expertise to the Salem community and help build a strong safety culture while continuing to learn from other experienced practitioners.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-40),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-35),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-30),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-25),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-25)
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
                    ExperienceLevel = 4, // Expert
                    YearsExperience = 12,
                    ExperienceDescription = "Highly experienced rigger with 12 years in the rope community. I specialize in teaching beginners and have a patient, methodical approach to instruction. Have taught at regional conferences and maintain a strong emphasis on building solid fundamentals and understanding the 'why' behind techniques.",
                    WhyJoinCommunity = "I'm passionate about education and mentoring new practitioners. After years of teaching, I understand the value of a strong community foundation. I want to contribute my teaching experience to help Salem develop skilled, safety-conscious practitioners.",
                    AgreesToGuidelines = true,
                    AgreesToTerms = true,
                    ConsentToContact = true,
                    WorkflowStatus = VettingStatus.Approved, // Approved (3) - matches User.VettingStatus
                    SubmittedAt = DateTime.UtcNow.AddDays(-50),
                    ReviewStartedAt = DateTime.UtcNow.AddDays(-45),
                    InterviewScheduledFor = DateTime.UtcNow.AddDays(-40),
                    DecisionMadeAt = DateTime.UtcNow.AddDays(-35),
                    LastReviewedAt = DateTime.UtcNow.AddDays(-35)
                });
            }

            if (additionalApplications.Any())
            {
                sampleApplications.AddRange(additionalApplications);
            }
        }

        await _context.VettingApplications.AddRangeAsync(sampleApplications, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Create UserNotes for applications (replacing legacy AdminNotes field)
        var userNotes = new List<WitchCityRope.Api.Data.Entities.UserNote>();

        // Map of application indices to their historical admin notes
        var notesMap = new Dictionary<int, string>
        {
            { 0, "Community administrator - founding member with extensive experience. Approved." },
            { 1, "Experienced practitioner with excellent references. Strong understanding of safety and consent. Approved for full membership." },
            { 3, "Good references and thoughtful application. Recommending more foundational experience - suggested attending beginner workshops for 3-6 months before reapplying." },
            { 4, "Application denied. Interview revealed concerning attitudes toward consent and pushing boundaries without proper communication. Focused on technical skills over partner safety and communication. Recommended to attend formal consent and communication workshops before reapplying." },
            { 5, "Excellent references from previous community. Strong technical knowledge and teaching experience. Approved for full membership." }
        };

        // Add notes from main sample applications list
        for (int i = 0; i < Math.Min(sampleApplications.Count, 6); i++)
        {
            var app = sampleApplications[i];
            if (notesMap.ContainsKey(i) && app.UserId.HasValue)
            {
                userNotes.Add(new WitchCityRope.Api.Data.Entities.UserNote
                {
                    Id = Guid.NewGuid(),
                    UserId = app.UserId.Value,
                    Content = notesMap[i],
                    NoteType = "Vetting",
                    AuthorId = null, // System-generated note
                    CreatedAt = app.DecisionMadeAt ?? app.SubmittedAt,
                    IsArchived = false
                });
            }
        }

        // Add notes for additional applications (indices 6-12 in full list)
        if (sampleApplications.Count > 6)
        {
            var additionalNotesMap = new Dictionary<int, string>
            {
                { 6, "Good references and thoughtful application. Ready for interview to assess practical knowledge and community fit." },
                { 8, "Experienced practitioner with strong safety focus. Approved for Teacher role." },
                { 9, "Strong references from previous community leaders. Ready for interview to discuss integration into local group." },
                { 12, "Highly experienced with excellent references from Portland community. Strong safety background. Approved for Teacher role." },
                { 13, "Excellent teaching credentials with strong references from previous community. Patient and methodical approach to instruction. Approved for Teacher role." }
            };

            for (int i = 6; i < sampleApplications.Count; i++)
            {
                var app = sampleApplications[i];
                if (additionalNotesMap.ContainsKey(i) && app.UserId.HasValue)
                {
                    userNotes.Add(new WitchCityRope.Api.Data.Entities.UserNote
                    {
                        Id = Guid.NewGuid(),
                        UserId = app.UserId.Value,
                        Content = additionalNotesMap[i],
                        NoteType = "Vetting",
                        AuthorId = null, // System-generated note
                        CreatedAt = app.DecisionMadeAt ?? app.ReviewStartedAt ?? app.SubmittedAt,
                        IsArchived = false
                    });
                }
            }
        }

        if (userNotes.Any())
        {
            await _context.UserNotes.AddRangeAsync(userNotes, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created {Count} UserNotes for vetting applications", userNotes.Count);
        }

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
    /// Audit trail patterns (matching actual system behavior):
    /// - UnderReview: Application Submitted (UnderReview)
    /// - InterviewApproved: Application Submitted → Status Changed (InterviewApproved)
    /// - FinalReview: Application Submitted → Status Changed (InterviewApproved) → Status Changed (FinalReview)
    /// - Approved: Application Submitted → Status Changed (InterviewApproved) → Status Changed (FinalReview) → Status Changed (Approved)
    /// - OnHold: Application Submitted → Status Changed (OnHold)
    /// - Denied: Application Submitted → Status Changed (Denied)
    ///
    /// Each log entry includes:
    /// - Action: "Application Submitted" or "Status Changed"
    /// - Performer (user who made the change)
    /// - Timestamp (proper workflow timing)
    /// - OldValue/NewValue for status transitions
    /// - Notes: Only populated for initial submission, null for all status changes
    ///
    /// NOTE: All workflow transitions use "Status Changed" action with empty notes.
    /// This matches the actual system behavior where only the initial submission has notes.
    /// </summary>
    private async Task CreateVettingAuditLogsAsync(List<VettingApplication> applications, Guid adminUserId, CancellationToken cancellationToken)
    {
        var auditLogs = new List<VettingAuditLog>();

        // Arrays of realistic reviewer notes for different stages
        var initialReviewNotes = new[]
        {
            "Initial review: Application is well-written and shows good understanding of rope safety principles",
            "Application reviewed - responses demonstrate clear commitment to consent and communication",
            "Preliminary review complete - applicant has thoughtful answers about community involvement",
            "First pass review: Strong written communication skills and appropriate safety awareness",
            "Initial assessment: Application shows genuine interest in learning and community participation"
        };

        var referenceCheckNotes = new[]
        {
            "Called reference Sarah Johnson - extremely positive feedback about safety practices and community involvement",
            "Reference check with John Smith completed - very positive feedback on applicant's reliability and consent practices",
            "Spoke with reference Maria Garcia - glowing recommendation, particularly noted excellent communication skills",
            "Reference check: Contact with previous community organizer was very positive, emphasized strong boundaries",
            "Completed reference verification with Alex Kim - enthusiastic endorsement of applicant's character and safety mindset"
        };

        var interviewPrepNotes = new[]
        {
            "Prepared interview questions focused on consent understanding and conflict resolution scenarios",
            "Interview preparation complete - reviewing applicant's background in rope work and safety training",
            "Prepared interview guide covering safety protocols, boundary negotiation, and community expectations",
            "Interview questions drafted focusing on practical experience and understanding of risk awareness",
            "Completed interview prep - will focus on assessing communication style and community fit"
        };

        var postInterviewNotes = new[]
        {
            "Interview went well - applicant demonstrated clear communication and strong understanding of consent principles",
            "Positive interview - excellent responses to scenario questions, particularly around boundary setting",
            "Interview complete - impressed by applicant's maturity and thoughtful approach to rope practice",
            "Strong interview performance - applicant showed good self-awareness and understanding of community values",
            "Interview notes: Articulate communicator with realistic expectations and solid grasp of safety fundamentals"
        };

        var decisionReasoningNotes = new[]
        {
            "Recommending approval based on strong references and interview performance - good fit for community",
            "All criteria met for approval - references excellent, interview strong, clear understanding of community values",
            "Final review: Application demonstrates all required qualities for membership - recommending approval",
            "Consensus among review team - applicant shows maturity, safety awareness, and good community alignment",
            "Ready for approval - comprehensive review shows applicant meets all vetting requirements"
        };

        var onHoldNotes = new[]
        {
            "Applicant needs more foundational experience - suggested attending beginner workshops first",
            "Additional training recommended before proceeding - safety course completion required",
            "On hold pending completion of basic rope safety certification - applicant agrees this is appropriate",
            "Needs more practical experience before interview - suggested 6 months of beginner classes"
        };

        var denialReasoningNotes = new[]
        {
            "Multiple red flags during review process - concerning attitude toward consent negotiation",
            "Application shows fundamental misunderstanding of community values and safety priorities",
            "Review committee consensus: Applicant not ready for membership at this time",
            "Insufficient understanding of consent practices and safety protocols - denial recommended"
        };

        var random = new Random(42); // Fixed seed for consistent test data across runs

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
                    // UnderReview → Notes → InterviewApproved

                    // Add 2-3 reviewer notes before interview approval
                    var noteCount = random.Next(2, 4);
                    var dayOffset = 1;

                    // Initial review note
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(dayOffset++),
                        OldValue = null,
                        NewValue = null,
                        Notes = initialReviewNotes[random.Next(initialReviewNotes.Length)]
                    });

                    // Reference check note (if noteCount >= 3)
                    if (noteCount >= 3)
                    {
                        auditLogs.Add(new VettingAuditLog
                        {
                            Id = Guid.NewGuid(),
                            ApplicationId = application.Id,
                            Action = "Note Added",
                            PerformedBy = adminUserId,
                            PerformedAt = application.SubmittedAt.AddDays(dayOffset++),
                            OldValue = null,
                            NewValue = null,
                            Notes = referenceCheckNotes[random.Next(referenceCheckNotes.Length)]
                        });
                    }

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(3),
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = null  // Empty - status changes use OldValue/NewValue only
                    });
                    break;

                case VettingStatus.FinalReview:
                    // UnderReview → InterviewApproved → FinalReview

                    var interviewApprovedDate = application.ReviewStartedAt ?? application.SubmittedAt.AddDays(3);

                    // UnderReview → InterviewApproved
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = interviewApprovedDate,
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = null
                    });

                    // InterviewApproved → FinalReview
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.InterviewScheduledFor ?? interviewApprovedDate.AddDays(7),
                        OldValue = "InterviewApproved",
                        NewValue = "FinalReview",
                        Notes = null
                    });
                    break;

                case VettingStatus.Approved:
                    // Full workflow: UnderReview → InterviewApproved → FinalReview → Approved
                    // Includes both automatic status changes AND reviewer notes

                    // Initial review note (day 1-2 after submission)
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(random.Next(1, 3)),
                        OldValue = null,
                        NewValue = null,
                        Notes = initialReviewNotes[random.Next(initialReviewNotes.Length)]
                    });

                    // Status change to InterviewApproved (EXACTLY 2 days after submission)
                    var interviewApprovedAt = application.SubmittedAt.AddDays(2);
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = interviewApprovedAt,
                        OldValue = "UnderReview",
                        NewValue = "InterviewApproved",
                        Notes = null
                    });

                    // Reference check note (1-3 days after interview approval)
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = adminUserId,
                        PerformedAt = interviewApprovedAt.AddDays(random.Next(1, 4)),
                        OldValue = null,
                        NewValue = null,
                        Notes = referenceCheckNotes[random.Next(referenceCheckNotes.Length)]
                    });

                    // Interview prep note (1 day before interview - day 3)
                    var finalReviewAt = application.SubmittedAt.AddDays(4);
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = finalReviewAt.AddDays(-1),
                        OldValue = null,
                        NewValue = null,
                        Notes = interviewPrepNotes[random.Next(interviewPrepNotes.Length)]
                    });

                    // Interview completed → FinalReview (EXACTLY 4 days after submission)
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = finalReviewAt,
                        OldValue = "InterviewApproved",
                        NewValue = "FinalReview",
                        Notes = null
                    });

                    // Post-interview note (1 day after interview)
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = adminUserId,
                        PerformedAt = finalReviewAt.AddDays(1),
                        OldValue = null,
                        NewValue = null,
                        Notes = postInterviewNotes[random.Next(postInterviewNotes.Length)]
                    });

                    // Decision reasoning note (same day as approval - before the status change)
                    var approvalDate = application.SubmittedAt.AddDays(5);
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = approvalDate.AddMinutes(-30), // 30 minutes before approval
                        OldValue = null,
                        NewValue = null,
                        Notes = decisionReasoningNotes[random.Next(decisionReasoningNotes.Length)]
                    });

                    // Final approval (EXACTLY 5 days after submission)
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = approvalDate,
                        OldValue = "FinalReview",
                        NewValue = "Approved",
                        Notes = null
                    });
                    break;

                case VettingStatus.OnHold:
                    // UnderReview → Notes → OnHold

                    // Initial review note
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(1),
                        OldValue = null,
                        NewValue = null,
                        Notes = initialReviewNotes[random.Next(initialReviewNotes.Length)]
                    });

                    // On hold reasoning note
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(3),
                        OldValue = null,
                        NewValue = null,
                        Notes = onHoldNotes[random.Next(onHoldNotes.Length)]
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
                        Notes = null  // Empty - status changes use OldValue/NewValue only
                    });
                    break;

                case VettingStatus.Denied:
                    // UnderReview → Notes → Denied

                    // Initial review note
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Reviewer Comment",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(2),
                        OldValue = null,
                        NewValue = null,
                        Notes = "Application review in progress - some concerns noted about consent understanding"
                    });

                    // Denial reasoning note
                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = adminUserId,
                        PerformedAt = application.SubmittedAt.AddDays(5),
                        OldValue = null,
                        NewValue = null,
                        Notes = denialReasoningNotes[random.Next(denialReasoningNotes.Length)]
                    });

                    auditLogs.Add(new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = adminUserId,
                        PerformedAt = application.DecisionMadeAt ?? application.SubmittedAt.AddDays(7),
                        OldValue = "UnderReview",
                        NewValue = "Denied",
                        Notes = null  // Empty - status changes use OldValue/NewValue only
                    });
                    break;
            }
        }

        await _context.VettingAuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} vetting audit log entries", auditLogs.Count);
    }
}
