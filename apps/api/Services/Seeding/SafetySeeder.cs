using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of safety incident test data.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating sample safety incidents with varied statuses and coordinators.
/// </summary>
public class SafetySeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SafetySeeder> _logger;

    public SafetySeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEncryptionService encryptionService,
        ILogger<SafetySeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds safety incident test data with varied statuses, coordinators, and encrypted PII.
    /// Idempotent operation - skips if incidents already exist.
    ///
    /// Creates 8 sample incidents covering different:
    /// - Incident types (rope practice issues, inappropriate behavior, equipment failure, conflicts)
    /// - Workflow statuses (ReportSubmitted, InformationGathering, ReviewingFinalReport, OnHold, Closed)
    /// - Coordinator assignments (coordinator1, coordinator2, teacher, admin, unassigned)
    /// - Anonymous vs identified reporters
    /// - Follow-up requests
    ///
    /// All PII fields are encrypted using IEncryptionService:
    /// - Description, InvolvedParties, Witnesses, ContactEmail, ContactName
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
        var incidents = new List<SafetyIncident>();

        // Incident 1: New report submitted today (unassigned)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.ReportSubmitted,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1),
            CreatedBy = memberUser?.Id
        });

        // Incident 2: Information gathering phase (assigned to coordinator1)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.InformationGathering,
            AssignedTo = coordinator1?.Id,
            CoordinatorId = coordinator1?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_001",
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-1),
            CreatedBy = guestUser?.Id,
            UpdatedBy = adminUser.Id
        });

        // Incident 3: Reviewing final report (assigned to coordinator2)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.ReviewingFinalReport,
            AssignedTo = coordinator2?.Id,
            CoordinatorId = coordinator2?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_002",
            GoogleDriveFinalReportUrl = "https://docs.google.com/document/d/EXAMPLE_DOC_ID_002",
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-1),
            UpdatedBy = teacherUser.Id
        });

        // Incident 4: On hold (waiting for external information)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.OnHold,
            AssignedTo = coordinator1?.Id,
            CoordinatorId = coordinator1?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_003",
            CreatedAt = now.AddDays(-14),
            UpdatedAt = now.AddDays(-5),
            CreatedBy = memberUser?.Id,
            UpdatedBy = adminUser.Id
        });

        // Incident 5: Closed case (resolved - equipment issue addressed)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.Closed,
            AssignedTo = coordinator2?.Id,
            CoordinatorId = coordinator2?.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_004",
            GoogleDriveFinalReportUrl = "https://docs.google.com/document/d/EXAMPLE_DOC_ID_004",
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-7),
            UpdatedBy = coordinator2?.Id
        });

        // Incident 6: Recent report needing assignment
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.ReportSubmitted,
            CreatedAt = now.AddHours(-12),
            UpdatedAt = now.AddHours(-12)
        });

        // Incident 7: Information gathering (different location)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.InformationGathering,
            AssignedTo = teacherUser.Id,
            CoordinatorId = teacherUser.Id,
            GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/EXAMPLE_FOLDER_ID_005",
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2),
            CreatedBy = guestUser?.Id,
            UpdatedBy = teacherUser.Id
        });

        // Incident 8: Closed case (community conflict resolved)
        incidents.Add(new SafetyIncident
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
            Status = IncidentStatus.Closed,
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
