using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VettedMemberImport.Data;
using VettedMemberImport.Entities;
using VettedMemberImport.Models;
using BCrypt.Net;

namespace VettedMemberImport.Services;

/// <summary>
/// Service to import vetted members from CSV into database
/// </summary>
public class UserImporter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserImporter> _logger;
    private readonly DateParser _dateParser;

    public UserImporter(ApplicationDbContext context, ILogger<UserImporter> logger, DateParser dateParser)
    {
        _context = context;
        _logger = logger;
        _dateParser = dateParser;
    }

    /// <summary>
    /// Import users from CSV rows with specified vetting status
    /// </summary>
    /// <param name="rows">CSV rows to import</param>
    /// <param name="isDryRun">If true, validate without writing to database</param>
    /// <param name="vettingStatus">Vetting status to assign (1=InterviewApproved, 3=Approved)</param>
    /// <param name="workflowStatus">Workflow status to assign (1=InterviewApproved, 3=Approved)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<ImportSummary> ImportUsersAsync(
        List<CsvRow> rows,
        bool isDryRun,
        int vettingStatus = 3,
        int workflowStatus = 3,
        CancellationToken cancellationToken = default)
    {
        var summary = new ImportSummary
        {
            TotalRecords = rows.Count
        };

        _logger.LogInformation("Starting import of {Count} records (Dry Run: {IsDryRun}, VettingStatus: {VettingStatus}, WorkflowStatus: {WorkflowStatus})",
            rows.Count, isDryRun, vettingStatus, workflowStatus);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2; // Account for header row and 0-based index

            try
            {
                await ProcessRow(row, rowNum, summary, isDryRun, vettingStatus, workflowStatus, cancellationToken);
            }
            catch (Exception ex)
            {
                summary.ErrorCount++;
                var errorMsg = $"Row {rowNum}: Unexpected error - {ex.Message}";
                summary.Errors.Add(errorMsg);
                _logger.LogError(ex, errorMsg);
            }
        }

        return summary;
    }

    private async Task ProcessRow(
        CsvRow row,
        int rowNum,
        ImportSummary summary,
        bool isDryRun,
        int vettingStatus,
        int workflowStatus,
        CancellationToken cancellationToken)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(row.Email))
        {
            summary.ErrorCount++;
            summary.Errors.Add($"Row {rowNum}: Missing email address");
            return;
        }

        if (string.IsNullOrWhiteSpace(row.SceneName))
        {
            summary.ErrorCount++;
            summary.Errors.Add($"Row {rowNum}: Missing scene name/nickname");
            return;
        }

        // Normalize email
        var normalizedEmail = row.Email.Trim().ToUpperInvariant();
        var email = row.Email.Trim().ToLowerInvariant();

        // Check for duplicate email only (scene names can be duplicated)
        var existingByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingByEmail != null)
        {
            summary.SkippedCount++;
            var skipMsg = $"Row {rowNum}: Duplicate email - {email} (Skipped)";
            summary.Warnings.Add(skipMsg);
            _logger.LogWarning(skipMsg);
            return;
        }

        // Parse submission date
        var parsedDate = _dateParser.ParseDate(row.ApplicationDate);
        if (!parsedDate.HasValue)
        {
            _logger.LogWarning("Row {RowNum}: Could not parse date '{DateStr}', using current date", rowNum, row.ApplicationDate);
        }
        var submittedAt = parsedDate ?? DateTime.UtcNow;
        var decisionDate = _dateParser.InferDecisionDate(row.Notes, submittedAt);

        if (isDryRun)
        {
            summary.SuccessCount++;
            _logger.LogInformation("[DRY RUN] Would create user: {Email}, SceneName: {SceneName}, Submitted: {SubmittedAt}",
                email, row.SceneName, submittedAt);
            return;
        }

        // Create user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            SceneName = row.SceneName.Trim(),
            Pronouns = row.Pronouns?.Trim() ?? "",
            FetLifeName = row.FetLifeHandle?.Trim(),

            // Security - Generate random secure password hash (user must reset via email)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateRandomPassword()),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),

            // Email verification - Must verify email before first login
            EmailConfirmed = false, // ASP.NET Identity field
            EmailVerificationToken = GenerateVerificationToken(),
            EmailVerificationTokenCreatedAt = DateTime.UtcNow,

            // Vetting status - Set based on import parameter (1=InterviewApproved, 3=Approved)
            VettingStatus = vettingStatus,
            HasVettingApplication = true,

            // Timestamps
            CreatedAt = submittedAt,
            UpdatedAt = DateTime.UtcNow,

            // Other required fields
            IsActive = true,
            TermsOfServiceAccepted = false, // Will accept on first login
            Role = vettingStatus == 3 ? "VettedMember" : "Member",  // VettedMember for approved, Member for interview-approved

            // Required but unused fields
            EncryptedLegalName = "",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            PronouncedName = ""
        };

        _context.Users.Add(user);

        // Combine Instagram and Other handles into OtherNames
        // Filter out "N/A" values - treat as empty
        var otherNames = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.InstagramHandle) &&
            !row.InstagramHandle.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            otherNames.Add($"IG: {row.InstagramHandle.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(row.OtherHandles) &&
            !row.OtherHandles.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            otherNames.Add(row.OtherHandles.Trim());
        }
        var combinedOtherNames = otherNames.Count > 0 ? string.Join("\n", otherNames) : null;

        // Create vetting application
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ApplicationNumber = GenerateApplicationNumber(submittedAt),
            StatusToken = Guid.NewGuid().ToString("N"),
            SceneName = row.SceneName.Trim(),
            Email = email,
            Pronouns = row.Pronouns?.Trim(),
            FetLifeHandle = row.FetLifeHandle?.Trim(),
            OtherNames = combinedOtherNames,

            // Experience/motivation (using correct columns)
            // "Description of the applicant and motivation to join" → WhyJoinCommunity
            // "Relationship with Sponsor or how did they learn about Dark Alchemy" → HowDidYouHearAboutUs
            // "Fit for Dark Alchemy" → ExperienceDescription (reviewer notes about applicant fit)
            WhyJoinCommunity = row.MotivationDescription?.Trim(),
            HowDidYouHearAboutUs = row.RelationshipWithSponsor?.Trim(),
            ExperienceDescription = row.FitForDarkAlchemy?.Trim(),

            // Workflow status - Set based on import parameter (1=InterviewApproved, 3=Approved)
            WorkflowStatus = workflowStatus,
            SubmittedAt = submittedAt,
            DecisionMadeAt = decisionDate,

            // Agreements (assumed true since approved)
            AgreesToGuidelines = true,
            AgreesToTerms = true,
            ConsentToContact = true,

            // Timestamps
            CreatedAt = submittedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _context.VettingApplications.Add(application);

        // Combine VettingStatus and Notes columns for audit log creation
        // VettingStatus column typically contains the vetting history/notes
        var combinedNotes = CombineNotes(row.VettingStatus, row.Notes);

        // Create audit logs from notes and system events
        var auditLogs = ParseNotesIntoAuditLogs(combinedNotes, application.Id, submittedAt, row.Vettor, vettingStatus, workflowStatus);
        _context.VettingAuditLogs.AddRange(auditLogs);

        // Save to database
        await _context.SaveChangesAsync(cancellationToken);

        summary.SuccessCount++;
        _logger.LogInformation("Row {RowNum}: Created user {Email} (SceneName: {SceneName})",
            rowNum, email, row.SceneName);
    }

    private string GenerateRandomPassword()
    {
        // Generate a random 32-character password
        // User will reset via email verification link
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    private string GenerateVerificationToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string GenerateApplicationNumber(DateTime submittedAt)
    {
        return $"VET-{submittedAt:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}";
    }

    private string? CombineNotes(string? vettingStatus, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(vettingStatus))
            parts.Add(vettingStatus.Trim());
        if (!string.IsNullOrWhiteSpace(notes))
            parts.Add(notes.Trim());
        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }

    private List<VettingAuditLog> ParseNotesIntoAuditLogs(string? notes, Guid applicationId, DateTime submittedAt, string? vettorName, int vettingStatus, int workflowStatus)
    {
        var auditLogs = new List<VettingAuditLog>();

        // Get admin user lookup for attribution
        var adminLookup = GetAdminLookup();
        var defaultAdminId = adminLookup.Values.FirstOrDefault();
        if (defaultAdminId == Guid.Empty)
        {
            defaultAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        // System audit log: Application Submitted (Status Changed from null to UnderReview)
        auditLogs.Add(new VettingAuditLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Action = "Status Changed",
            PerformedBy = defaultAdminId,
            PerformedAt = submittedAt,
            OldValue = null,
            NewValue = "UnderReview",
            Notes = "Application submitted (imported from legacy system)"
        });

        // Add vettor assignment note if vettor is assigned
        if (!string.IsNullOrWhiteSpace(vettorName))
        {
            var vettorAssignmentDate = submittedAt.AddDays(1);
            var vettorId = adminLookup.GetValueOrDefault(vettorName.Trim(), defaultAdminId);

            auditLogs.Add(new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Action = "Vettor Assigned",
                PerformedBy = vettorId,
                PerformedAt = vettorAssignmentDate,
                Notes = $"{vettorName} assigned to vetting process"
            });
        }

        // Parse notes from CSV (if any)
        if (!string.IsNullOrWhiteSpace(notes))
        {
            // Split notes by newlines first (each line is a separate note entry)
            var noteLines = notes.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var noteLine in noteLines)
            {
                var trimmedLine = noteLine.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                // Try to extract abbreviated date and action from note line
                var (action, performedAt, performedBy) = ParseNoteSegment(trimmedLine, submittedAt, adminLookup, defaultAdminId);

                auditLogs.Add(new VettingAuditLog
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    Action = action,
                    PerformedBy = performedBy,
                    PerformedAt = performedAt,
                    Notes = trimmedLine
                });
            }
        }

        // System audit log: Status change based on import status
        // vettingStatus: 1=InterviewApproved, 3=Approved
        // Map status integers to enum names
        var newStatusName = vettingStatus switch
        {
            1 => "InterviewApproved",
            3 => "Approved",
            _ => "UnderReview"
        };
        var statusNote = vettingStatus == 1
            ? "Approved for interview (imported from legacy system)"
            : "Application approved (imported from legacy system)";

        // Use the last date from notes if available, otherwise use submitted date + 30 days
        var statusChangeDate = auditLogs
            .Where(a => a.Action == "Note Added")
            .OrderByDescending(a => a.PerformedAt)
            .FirstOrDefault()?.PerformedAt ?? submittedAt.AddDays(30);

        auditLogs.Add(new VettingAuditLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Action = "Status Changed",
            PerformedBy = defaultAdminId,
            PerformedAt = statusChangeDate,
            OldValue = "UnderReview",
            NewValue = newStatusName,
            Notes = statusNote
        });

        return auditLogs;
    }

    private Dictionary<string, Guid> GetAdminLookup()
    {
        var adminLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var admins = _context.Users
            .Where(u => u.Role.Contains("Admin") || u.Role.Contains("SafetyTeam"))
            .ToList();

        foreach (var admin in admins)
        {
            if (!string.IsNullOrWhiteSpace(admin.SceneName))
            {
                adminLookup[admin.SceneName] = admin.Id;
            }
        }

        return adminLookup;
    }

    private (string action, DateTime performedAt, Guid performedBy) ParseNoteSegment(
        string segment,
        DateTime submittedAt,
        Dictionary<string, Guid> adminLookup,
        Guid defaultAdminId)
    {
        var action = "Note Added";
        var performedAt = submittedAt;
        var performedBy = defaultAdminId;

        // Extract abbreviated date (e.g., "11/3", "07/13/22")
        var datePattern = @"(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?";
        var dateMatch = System.Text.RegularExpressions.Regex.Match(segment, datePattern);

        if (dateMatch.Success)
        {
            var month = int.Parse(dateMatch.Groups[1].Value);
            var day = int.Parse(dateMatch.Groups[2].Value);
            var yearStr = dateMatch.Groups[3].Success ? dateMatch.Groups[3].Value : null;

            // Infer year from application submission date
            int year;
            if (!string.IsNullOrEmpty(yearStr))
            {
                // Year was provided in the date
                year = yearStr.Length == 2
                    ? 2000 + int.Parse(yearStr)
                    : int.Parse(yearStr);
            }
            else
            {
                // No year provided - infer from submission date
                // If note month/day is after submission date, use same year
                // Otherwise, assume next year
                year = submittedAt.Year;
                var noteDate = new DateTime(year, month, day);

                if (noteDate < submittedAt.Date)
                {
                    // Note date is before submission, must be next year
                    year++;
                }
            }

            try
            {
                performedAt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            }
            catch
            {
                // Invalid date, use submission date
                performedAt = submittedAt;
            }
        }

        // Look for admin names in the segment
        foreach (var kvp in adminLookup)
        {
            if (segment.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                performedBy = kvp.Value;
                break;
            }
        }

        // Determine action from keywords
        var lowerSegment = segment.ToLower();
        if (lowerSegment.Contains("submitted"))
        {
            action = "Application Submitted";
        }
        else if (lowerSegment.Contains("initiated"))
        {
            action = "Application Initiated";
        }
        else if (lowerSegment.Contains("assigned"))
        {
            action = "Vettor Assigned";
        }
        // Check for approval first (more specific - contains "interview" AND "accepted")
        else if (lowerSegment.Contains("interview") && lowerSegment.Contains("accepted"))
        {
            action = "Interview Completed and Approved";
        }
        // Then check for completion (less specific - contains "interview" AND "held" OR "completed")
        else if (lowerSegment.Contains("interview") && (lowerSegment.Contains("held") || lowerSegment.Contains("completed")))
        {
            action = "Interview Completed";
        }
        else if (lowerSegment.Contains("interview") && lowerSegment.Contains("scheduled"))
        {
            action = "Interview Scheduled";
        }
        else if (lowerSegment.Contains("interview") && lowerSegment.Contains("email sent"))
        {
            action = "Interview Request Email Sent";
        }
        else if (lowerSegment.Contains("accepted") || lowerSegment.Contains("approved"))
        {
            action = "Application Approved";
        }
        else if (lowerSegment.Contains("acceptance") && lowerSegment.Contains("mail sent"))
        {
            action = "Acceptance Email Sent";
        }

        return (action, performedAt, performedBy);
    }
}
