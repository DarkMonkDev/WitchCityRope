using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service for vetting operations
/// Simplified implementation to support basic admin vetting grid functionality
/// </summary>
public class VettingService : IVettingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VettingService> _logger;
    private readonly IVettingEmailService _emailService;

    public VettingService(
        ApplicationDbContext context,
        ILogger<VettingService> logger,
        IVettingEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Get paginated list of applications for admin/reviewer dashboard
    /// </summary>
    public async Task<Result<PagedResult<ApplicationSummaryDto>>> GetApplicationsForReviewAsync(
        ApplicationFilterRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user authorization - only administrators can access vetting applications
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<PagedResult<ApplicationSummaryDto>>.Failure(
                    "Access denied", "Only administrators can access vetting applications.");
            }

            // Build query
            var query = _context.VettingApplications
                .Include(v => v.User)
                .Include(v => v.AuditLogs)
                .AsNoTracking();

            // Apply status filters
            if (request.StatusFilters.Any())
            {
                var statusValues = request.StatusFilters
                    .Select(s => Enum.TryParse<VettingStatus>(s, true, out var status) ? status : (VettingStatus?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                if (statusValues.Any())
                {
                    query = query.Where(v => statusValues.Contains(v.Status));
                }
            }

            // Apply search query
            if (!string.IsNullOrWhiteSpace(request.SearchQuery))
            {
                var searchTerm = request.SearchQuery.Trim().ToLower();
                query = query.Where(v =>
                    v.SceneName.ToLower().Contains(searchTerm) ||
                    v.Email.ToLower().Contains(searchTerm) ||
                    v.Id.ToString().Contains(searchTerm));
            }

            // Apply date filters
            if (request.SubmittedAfter.HasValue)
            {
                query = query.Where(v => v.SubmittedAt >= request.SubmittedAfter.Value);
            }

            if (request.SubmittedBefore.HasValue)
            {
                query = query.Where(v => v.SubmittedAt <= request.SubmittedBefore.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "submittedat" => request.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(v => v.SubmittedAt)
                    : query.OrderByDescending(v => v.SubmittedAt),
                "status" => request.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(v => v.Status)
                    : query.OrderByDescending(v => v.Status),
                "scenename" => request.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(v => v.SceneName)
                    : query.OrderByDescending(v => v.SceneName),
                _ => query.OrderByDescending(v => v.SubmittedAt)
            };

            // Apply pagination
            var applications = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var applicationDtos = applications.Select(app => new ApplicationSummaryDto
            {
                Id = app.Id,
                ApplicationNumber = app.Id.ToString()[..8], // Simple application number from ID
                Status = app.Status.ToString(),
                SubmittedAt = app.SubmittedAt,
                LastActivityAt = app.UpdatedAt,
                SceneName = app.SceneName,
                ExperienceLevel = "Beginner", // Default for now
                YearsExperience = 0, // Default for now
                IsAnonymous = false, // Default for now
                AssignedReviewerName = null, // Not implemented yet
                ReviewStartedAt = app.ReviewStartedAt,
                Priority = 1, // Default priority
                DaysInCurrentStatus = (DateTime.UtcNow - app.SubmittedAt).Days,
                ReferenceStatus = new ApplicationReferenceStatus
                {
                    TotalReferences = 0,
                    ContactedReferences = 0,
                    RespondedReferences = 0,
                    AllReferencesComplete = false
                },
                HasRecentNotes = false, // Default for now
                HasPendingActions = app.Status == VettingStatus.UnderReview,
                InterviewScheduledFor = app.InterviewScheduledFor,
                SkillsTags = new List<string>()
            }).ToList();

            var pagedResult = new PagedResult<ApplicationSummaryDto>(
                applicationDtos, totalCount, request.Page, request.PageSize);

            _logger.LogInformation("Retrieved {Count} vetting applications for admin user {UserId}",
                applicationDtos.Count, userId);

            return Result<PagedResult<ApplicationSummaryDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vetting applications for user {UserId}", userId);
            return Result<PagedResult<ApplicationSummaryDto>>.Failure(
                "Failed to retrieve applications", ex.Message);
        }
    }

    /// <summary>
    /// Get detailed application information for review
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> GetApplicationDetailAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user authorization
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Access denied", "Only administrators can access application details.");
            }

            // Get application with related data
            var application = await _context.VettingApplications
                .Include(v => v.User)
                .Include(v => v.AuditLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Parse AdminNotes into ApplicationNoteDto array
            var notes = ParseAdminNotesToDto(application.AdminNotes);

            // Convert audit logs to workflow history
            var workflowHistory = application.AuditLogs?.Select(log => new WorkflowHistoryDto
            {
                Action = log.Action,
                PerformedAt = log.PerformedAt,
                PerformedBy = log.PerformedBy.ToString(),
                Notes = log.Notes
            }).OrderByDescending(h => h.PerformedAt).ToList() ?? new List<WorkflowHistoryDto>();

            // Create decisions from audit logs
            var decisions = application.AuditLogs?
                .Where(log => log.Action.Contains("Status Changed") || log.Action.Contains("Decision"))
                .Select(log => new ReviewDecisionDto
                {
                    Id = log.Id,
                    DecisionType = log.NewValue ?? "Unknown",
                    Reasoning = log.Notes ?? "",
                    IsFinalDecision = log.Action.Contains("Approved") || log.Action.Contains("Denied"),
                    ReviewerName = "Administrator", // Simplified
                    CreatedAt = log.PerformedAt
                }).OrderByDescending(d => d.CreatedAt).ToList() ?? new List<ReviewDecisionDto>();

            // Map to detailed response
            var response = new ApplicationDetailResponse
            {
                Id = application.Id,
                ApplicationNumber = application.Id.ToString()[..8],
                Status = application.Status.ToString(),
                SubmittedAt = application.SubmittedAt,
                LastActivityAt = application.UpdatedAt,
                FullName = application.RealName,
                SceneName = application.SceneName,
                Pronouns = application.Pronouns,
                Email = application.Email,
                ExperienceLevel = "Beginner", // Default for now
                YearsExperience = 0,
                ExperienceDescription = "", // Not in simplified entity
                SafetyKnowledge = "", // Not in simplified entity
                ConsentUnderstanding = "", // Not in simplified entity
                WhyJoinCommunity = application.AboutYourself, // Using available field
                SkillsInterests = new List<string>(),
                ExpectationsGoals = application.HowFoundUs, // Using available field
                AgreesToGuidelines = true, // Default
                IsAnonymous = false,
                AgreesToTerms = true,
                ConsentToContact = true,
                AssignedReviewerName = null,
                ReviewStartedAt = application.ReviewStartedAt,
                Priority = 1,
                InterviewScheduledFor = application.InterviewScheduledFor,
                References = new List<ReferenceDetailDto>(), // Not implemented yet
                Notes = notes, // Parsed from AdminNotes
                Decisions = decisions, // Parsed from audit logs
                WorkflowHistory = workflowHistory // Audit logs as workflow history
            };

            _logger.LogInformation("Retrieved application detail {ApplicationId} for user {UserId}",
                applicationId, userId);

            return Result<ApplicationDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application detail {ApplicationId} for user {UserId}",
                applicationId, userId);
            return Result<ApplicationDetailResponse>.Failure(
                "Failed to retrieve application detail", ex.Message);
        }
    }

    /// <summary>
    /// Submit a review decision for an application
    /// </summary>
    public async Task<Result<ReviewDecisionResponse>> SubmitReviewDecisionAsync(
        Guid applicationId,
        ReviewDecisionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user authorization
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<ReviewDecisionResponse>.Failure(
                    "Access denied", "Only administrators can submit review decisions.");
            }

            // Get application
            var application = await _context.VettingApplications
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<ReviewDecisionResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Update application status based on decision (handle both string and int DecisionType)
            VettingStatus newStatus;
            if (request.DecisionType is string decisionString)
            {
                newStatus = decisionString.ToLower() switch
                {
                    "approved" => VettingStatus.Approved,
                    "denied" => VettingStatus.Denied,
                    "onhold" => VettingStatus.OnHold,
                    "pendinginterview" => VettingStatus.PendingInterview,
                    _ => application.Status
                };
            }
            else if (request.DecisionType is int decisionInt)
            {
                newStatus = decisionInt switch
                {
                    1 => VettingStatus.Approved, // Approve
                    2 => VettingStatus.Denied,   // Deny
                    3 => VettingStatus.OnHold,   // Request additional info
                    4 => VettingStatus.PendingInterview, // Schedule interview
                    _ => application.Status // No change
                };
            }
            else
            {
                // Try to parse as string or int from JSON
                var decisionValue = request.DecisionType?.ToString()?.ToLower() ?? "";
                newStatus = decisionValue switch
                {
                    "approved" or "1" => VettingStatus.Approved,
                    "denied" or "2" => VettingStatus.Denied,
                    "onhold" or "3" => VettingStatus.OnHold,
                    "pendinginterview" or "4" => VettingStatus.PendingInterview,
                    _ => application.Status
                };
            }

            // Validate that reasoning is required for Denied and OnHold statuses
            if ((newStatus == VettingStatus.Denied || newStatus == VettingStatus.OnHold) &&
                string.IsNullOrWhiteSpace(request.Reasoning))
            {
                return Result<ReviewDecisionResponse>.Failure(
                    "Reasoning required",
                    $"A reason must be provided when setting status to {newStatus}");
            }

            var oldStatus = application.Status;
            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;
            application.DecisionMadeAt = request.IsFinalDecision ? DateTime.UtcNow : null;
            application.InterviewScheduledFor = request.ProposedInterviewTime;

            // Add admin notes if provided
            if (!string.IsNullOrWhiteSpace(request.Reasoning))
            {
                var existingNotes = application.AdminNotes ?? "";
                var newNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Decision: {request.Reasoning}";
                application.AdminNotes = string.IsNullOrEmpty(existingNotes)
                    ? newNote
                    : $"{existingNotes}\n\n{newNote}";
            }

            // Create audit log entry for status change
            if (oldStatus != newStatus)
            {
                var auditLog = new VettingAuditLog
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = application.Id,
                    Action = "Status Changed",
                    PerformedBy = userId,
                    PerformedAt = DateTime.UtcNow,
                    OldValue = oldStatus.ToString(),
                    NewValue = newStatus.ToString(),
                    Notes = request.Reasoning
                };
                _context.VettingAuditLogs.Add(auditLog);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = new ReviewDecisionResponse
            {
                DecisionId = Guid.NewGuid(), // Simplified - not storing separate decision records
                DecisionType = newStatus.ToString(),
                SubmittedAt = DateTime.UtcNow,
                NewApplicationStatus = newStatus.ToString(),
                ConfirmationMessage = $"Review decision submitted successfully. Application status: {newStatus}",
                ActionsTriggered = new List<string>()
            };

            _logger.LogInformation("Review decision submitted for application {ApplicationId} by user {UserId}. New status: {Status}",
                applicationId, userId, newStatus);

            return Result<ReviewDecisionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review decision for application {ApplicationId} by user {UserId}",
                applicationId, userId);
            return Result<ReviewDecisionResponse>.Failure(
                "Failed to submit review decision", ex.Message);
        }
    }

    /// <summary>
    /// Add a note to an application
    /// </summary>
    public async Task<Result<NoteResponse>> AddApplicationNoteAsync(
        Guid applicationId,
        CreateNoteRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user authorization
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<NoteResponse>.Failure(
                    "Access denied", "Only administrators can add application notes.");
            }

            // Get application
            var application = await _context.VettingApplications
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<NoteResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Add note to admin notes field (simplified implementation)
            var existingNotes = application.AdminNotes ?? "";
            var newNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Note: {request.Content}";
            application.AdminNotes = string.IsNullOrEmpty(existingNotes)
                ? newNote
                : $"{existingNotes}\n\n{newNote}";

            application.UpdatedAt = DateTime.UtcNow;

            // Create audit log entry for note addition
            var auditLog = new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Note Added",
                PerformedBy = userId,
                PerformedAt = DateTime.UtcNow,
                OldValue = null,
                NewValue = null,
                Notes = request.Content
            };
            _context.VettingAuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);

            var response = new NoteResponse
            {
                NoteId = Guid.NewGuid(), // Simplified - not storing separate note records
                CreatedAt = DateTime.UtcNow,
                ConfirmationMessage = "Note added successfully to application"
            };

            _logger.LogInformation("Note added to application {ApplicationId} by user {UserId}",
                applicationId, userId);

            return Result<NoteResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to application {ApplicationId} by user {UserId}",
                applicationId, userId);
            return Result<NoteResponse>.Failure(
                "Failed to add note", ex.Message);
        }
    }

    /// <summary>
    /// Get current user's vetting application status
    /// </summary>
    public async Task<Result<MyApplicationStatusResponse>> GetMyApplicationStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's vetting application
            var application = await _context.VettingApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

            if (application == null)
            {
                // User has no vetting application
                var response = new MyApplicationStatusResponse
                {
                    HasApplication = false,
                    Application = null
                };

                return Result<MyApplicationStatusResponse>.Success(response);
            }

            // User has an application - return details
            var statusInfo = new ApplicationStatusInfo
            {
                ApplicationId = application.Id,
                ApplicationNumber = application.Id.ToString("N")[..8],
                Status = application.Status.ToString(),
                StatusDescription = GetStatusDescription(application.Status),
                SubmittedAt = application.SubmittedAt,
                LastUpdated = application.UpdatedAt,
                NextSteps = GetNextSteps(application.Status),
                EstimatedDaysRemaining = GetEstimatedDaysRemaining(application.Status, application.SubmittedAt)
            };

            var responseWithApp = new MyApplicationStatusResponse
            {
                HasApplication = true,
                Application = statusInfo
            };

            return Result<MyApplicationStatusResponse>.Success(responseWithApp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application status for user {UserId}", userId);
            return Result<MyApplicationStatusResponse>.Failure(
                "Failed to get application status", ex.Message);
        }
    }

    /// <summary>
    /// Get current user's vetting application details
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> GetMyApplicationDetailAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's vetting application with related data
            var application = await _context.VettingApplications
                .Include(v => v.User)
                .Include(v => v.AuditLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Application not found", "No vetting application found for the current user");
            }

            // Map to response DTO (reuse existing mapping logic from GetApplicationDetailAsync)
            var response = new ApplicationDetailResponse
            {
                ApplicationId = application.Id,
                ApplicationNumber = application.Id.ToString("N")[..8],
                Status = application.Status.ToString(),
                SceneName = application.SceneName ?? "Not provided",
                Email = application.Email,
                SubmittedAt = application.SubmittedAt,
                UpdatedAt = application.UpdatedAt,
                ExperienceLevel = "Beginner", // Default for simplified entity
                WhyJoinCommunity = application.AboutYourself ?? "Not provided",
                Pronouns = application.Pronouns ?? "Not provided",
                AdminNotes = null, // Don't show admin notes to the applicant
                Tags = new List<string>(), // Simplified implementation
                Attachments = new List<string>(), // Simplified implementation
                WorkflowHistory = application.AuditLogs?.Select(log => new WorkflowHistoryDto
                {
                    Action = log.Action,
                    PerformedAt = log.PerformedAt,
                    PerformedBy = log.PerformedBy.ToString(),
                    Notes = log.Notes
                }).OrderByDescending(h => h.PerformedAt).ToList() ?? new List<WorkflowHistoryDto>()
            };

            return Result<ApplicationDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application detail for user {UserId}", userId);
            return Result<ApplicationDetailResponse>.Failure(
                "Failed to get application details", ex.Message);
        }
    }

    /// <summary>
    /// Get user-friendly status description
    /// </summary>
    private static string GetStatusDescription(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.Draft => "Application draft - not yet submitted",
            VettingStatus.UnderReview => "Application is being reviewed by our team",
            VettingStatus.InterviewApproved => "Approved for interview - someone will contact you soon",
            VettingStatus.PendingInterview => "Interview scheduled - please check your email for details",
            VettingStatus.Approved => "Application approved - welcome to the community!",
            VettingStatus.OnHold => "Application on hold - additional information may be needed",
            VettingStatus.Denied => "Application was not approved at this time",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Get next steps for the user based on current status
    /// </summary>
    private static string? GetNextSteps(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.Draft => "Complete and submit your vetting application",
            VettingStatus.UnderReview => "No action needed - we'll contact you with updates",
            VettingStatus.InterviewApproved => "Wait for interview scheduling email",
            VettingStatus.PendingInterview => "Attend your scheduled interview",
            VettingStatus.Approved => "You can now register for member events",
            VettingStatus.OnHold => "Check your email for requested information",
            VettingStatus.Denied => "You may reapply after 6 months",
            _ => null
        };
    }

    /// <summary>
    /// Get estimated days remaining in review process
    /// </summary>
    private static int? GetEstimatedDaysRemaining(VettingStatus status, DateTime submittedAt)
    {
        return status switch
        {
            VettingStatus.UnderReview => Math.Max(0, 14 - (DateTime.UtcNow - submittedAt).Days), // 2 week typical review
            VettingStatus.InterviewApproved => Math.Max(0, 7 - (DateTime.UtcNow - submittedAt).Days), // 1 week to schedule
            VettingStatus.PendingInterview => Math.Max(0, 3 - (DateTime.UtcNow - submittedAt).Days), // 3 days after interview
            _ => null
        };
    }

    /// <summary>
    /// Parse AdminNotes field into ApplicationNoteDto array for API responses
    /// AdminNotes format: "[2025-09-23 14:30] Note: Content\n\n[2025-09-23 15:45] Decision: Reasoning"
    /// </summary>
    private static List<ApplicationNoteDto> ParseAdminNotesToDto(string? adminNotes)
    {
        if (string.IsNullOrWhiteSpace(adminNotes))
            return new List<ApplicationNoteDto>();

        var notes = new List<ApplicationNoteDto>();
        var noteEntries = adminNotes.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in noteEntries)
        {
            // Parse format: "[2025-09-23 14:30] Note: Content" or "[2025-09-23 14:30] Decision: Reasoning"
            var match = System.Text.RegularExpressions.Regex.Match(entry, @"\[([^\]]+)\]\s*(Note|Decision):\s*(.+)", System.Text.RegularExpressions.RegexOptions.Singleline);

            if (match.Success)
            {
                var dateTimeStr = match.Groups[1].Value;
                var noteType = match.Groups[2].Value;
                var content = match.Groups[3].Value.Trim();

                if (DateTime.TryParse(dateTimeStr, out var createdAt))
                {
                    notes.Add(new ApplicationNoteDto
                    {
                        Id = Guid.NewGuid(), // Generate ID for display purposes
                        Content = content,
                        Type = noteType,
                        IsPrivate = true, // All admin notes are private
                        Tags = new List<string>(),
                        ReviewerName = "Administrator",
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    });
                }
            }
        }

        return notes.OrderByDescending(n => n.CreatedAt).ToList();
    }

    /// <summary>
    /// Submit a new vetting application (public endpoint)
    /// </summary>
    public async Task<Result<ApplicationSubmissionResponse>> SubmitApplicationAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting new vetting application for {SceneName} ({Email})",
                request.SceneName, request.Email);

            // Generate unique application number and status token
            var applicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var statusToken = Guid.NewGuid().ToString("N"); // No hyphens for cleaner URLs

            // Create vetting application entity
            var application = new VettingApplication
            {
                SceneName = request.SceneName,
                Email = request.Email,
                ApplicationNumber = applicationNumber,
                StatusToken = statusToken,
                Status = VettingStatus.Submitted,
                SubmittedAt = DateTime.UtcNow,

                // Personal information
                FullName = request.FullName,
                Pronouns = request.Pronouns,
                Phone = request.Phone,

                // Experience & knowledge
                ExperienceLevel = request.ExperienceLevel,
                YearsExperience = request.YearsExperience,
                ExperienceDescription = request.ExperienceDescription,
                SafetyKnowledge = request.SafetyKnowledge,
                ConsentUnderstanding = request.ConsentUnderstanding,

                // Community understanding
                WhyJoinCommunity = request.WhyJoinCommunity,
                SkillsInterests = string.Join(", ", request.SkillsInterests),
                ExpectationsGoals = request.ExpectationsGoals,
                AgreesToGuidelines = request.AgreesToGuidelines,

                // References (serialize as JSON)
                References = System.Text.Json.JsonSerializer.Serialize(request.References),

                // Terms
                AgreesToTerms = request.AgreesToTerms,
                IsAnonymous = request.IsAnonymous,
                ConsentToContact = request.ConsentToContact
            };

            _context.VettingApplications.Add(application);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vetting application {ApplicationNumber} submitted successfully with ID {ApplicationId}",
                applicationNumber, application.Id);

            // Build response
            var response = new ApplicationSubmissionResponse
            {
                ApplicationId = application.Id,
                ApplicationNumber = applicationNumber,
                StatusToken = statusToken,
                SubmittedAt = application.SubmittedAt,
                ConfirmationMessage = "Thank you for submitting your vetting application. You will receive updates via email.",
                EstimatedReviewDays = 14, // Standard review period
                NextSteps = "Your application will be reviewed by our vetting committee. References will be contacted within 3-5 business days.",
                ReferenceStatuses = request.References.Select(r => new ReferenceStatusSummary
                {
                    Name = r.Name,
                    Email = MaskEmail(r.Email),
                    Status = "NotContacted"
                }).ToList()
            };

            return Result<ApplicationSubmissionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit vetting application for {SceneName}", request.SceneName);
            return Result<ApplicationSubmissionResponse>.Failure(
                "Failed to submit application",
                "An error occurred while processing your application. Please try again later.");
        }
    }

    /// <summary>
    /// Get application status by status token (public endpoint)
    /// </summary>
    public async Task<Result<ApplicationStatusResponse>> GetApplicationStatusByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving application status for token {Token}", token);

            var application = await _context.VettingApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.StatusToken == token, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationStatusResponse>.Failure(
                    "Application not found",
                    "No application found with the provided status token.");
            }

            // Calculate progress
            var progress = CalculateApplicationProgress(application);

            // Build status response with limited information for privacy
            var response = new ApplicationStatusResponse
            {
                ApplicationNumber = application.ApplicationNumber,
                Status = application.Status.ToString(),
                SubmittedAt = application.SubmittedAt,
                StatusDescription = GetStatusDescription(application.Status),
                LastUpdateAt = application.LastReviewedAt ?? application.SubmittedAt,
                EstimatedDaysRemaining = CalculateEstimatedDaysRemaining(application),
                Progress = progress,
                RecentUpdates = GetRecentStatusUpdates(application)
            };

            return Result<ApplicationStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve application status for token {Token}", token);
            return Result<ApplicationStatusResponse>.Failure(
                "Failed to retrieve status",
                "An error occurred while retrieving your application status.");
        }
    }

    /// <summary>
    /// Calculate application progress summary
    /// </summary>
    private ApplicationProgressSummary CalculateApplicationProgress(VettingApplication application)
    {
        var progress = new ApplicationProgressSummary
        {
            ApplicationSubmitted = true,
            ReferencesContacted = application.Status >= VettingStatus.UnderReview,
            ReferencesReceived = application.Status >= VettingStatus.UnderReview,
            UnderReview = application.Status >= VettingStatus.UnderReview,
            InterviewScheduled = application.Status == VettingStatus.InterviewScheduled,
            DecisionMade = application.Status is VettingStatus.Approved or VettingStatus.Denied,
            CurrentPhase = GetCurrentPhase(application.Status)
        };

        // Calculate progress percentage
        progress.ProgressPercentage = application.Status switch
        {
            VettingStatus.Submitted => 20,
            VettingStatus.UnderReview => 40,
            VettingStatus.InterviewScheduled => 70,
            VettingStatus.Approved => 100,
            VettingStatus.Denied => 100,
            VettingStatus.OnHold => 50,
            VettingStatus.Withdrawn => 100,
            _ => 0
        };

        return progress;
    }

    /// <summary>
    /// Get current phase description
    /// </summary>
    private string GetCurrentPhase(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.Submitted => "Application Submitted",
            VettingStatus.UnderReview => "Under Review",
            VettingStatus.InterviewScheduled => "Interview Scheduled",
            VettingStatus.Approved => "Approved",
            VettingStatus.Denied => "Application Denied",
            VettingStatus.OnHold => "On Hold",
            VettingStatus.Withdrawn => "Withdrawn",
            _ => "Unknown"
        };
    }


    /// <summary>
    /// Calculate estimated days remaining for review
    /// </summary>
    private int? CalculateEstimatedDaysRemaining(VettingApplication application)
    {
        if (application.Status is VettingStatus.Approved or VettingStatus.Denied or VettingStatus.Withdrawn)
        {
            return null; // No remaining days for final statuses
        }

        var daysSinceSubmission = (DateTime.UtcNow - application.SubmittedAt).Days;
        var standardReviewDays = 14;
        var remaining = standardReviewDays - daysSinceSubmission;

        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Get recent status updates for public display
    /// </summary>
    private List<StatusUpdateSummary> GetRecentStatusUpdates(VettingApplication application)
    {
        var updates = new List<StatusUpdateSummary>
        {
            new StatusUpdateSummary
            {
                UpdatedAt = application.SubmittedAt,
                Message = "Application submitted successfully",
                Type = "StatusChange"
            }
        };

        if (application.LastReviewedAt.HasValue && application.Status >= VettingStatus.UnderReview)
        {
            updates.Add(new StatusUpdateSummary
            {
                UpdatedAt = application.LastReviewedAt.Value,
                Message = $"Application status changed to {application.Status}",
                Type = "StatusChange"
            });
        }

        return updates.OrderByDescending(u => u.UpdatedAt).Take(5).ToList();
    }

    /// <summary>
    /// Mask email for privacy (show first 2 chars and domain)
    /// </summary>
    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;

        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 2)
            return $"{localPart[0]}***@{domain}";

        return $"{localPart.Substring(0, 2)}***@{domain}";
    }

    #region Status Change Logic and Validation

    /// <summary>
    /// Update application status with validation and audit logging
    /// Validates status transitions, updates timestamps, creates audit log, and sends email notifications
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> UpdateApplicationStatusAsync(
        Guid applicationId,
        VettingStatus newStatus,
        string? adminNotes,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check user authorization - only administrators can change status
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Access denied", "Only administrators can change application status.");
            }

            // Get application
            var application = await _context.VettingApplications
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            var oldStatus = application.Status;

            // Check if application is in terminal state FIRST - before any other validation
            if (oldStatus == VettingStatus.Approved || oldStatus == VettingStatus.Denied)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Cannot modify terminal state",
                    "Approved and Denied applications cannot be modified.");
            }

            // Validate status transition
            var transitionValidation = ValidateStatusTransition(oldStatus, newStatus);
            if (!transitionValidation.IsSuccess)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Invalid status transition", transitionValidation.Error);
            }

            // Validate required admin notes for certain status changes
            if ((newStatus == VettingStatus.OnHold || newStatus == VettingStatus.Denied) &&
                string.IsNullOrWhiteSpace(adminNotes))
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Admin notes required",
                    $"Admin notes are required when changing status to {newStatus}");
            }

            // Update application status
            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;

            // Update status-specific timestamps
            switch (newStatus)
            {
                case VettingStatus.UnderReview:
                    if (!application.ReviewStartedAt.HasValue)
                        application.ReviewStartedAt = DateTime.UtcNow;
                    break;
                case VettingStatus.InterviewScheduled:
                    // InterviewScheduledFor should be set separately via ScheduleInterviewAsync
                    break;
                case VettingStatus.Approved:
                case VettingStatus.Denied:
                    application.DecisionMadeAt = DateTime.UtcNow;
                    break;
            }

            // Add admin notes if provided
            if (!string.IsNullOrWhiteSpace(adminNotes))
            {
                var existingNotes = application.AdminNotes ?? "";
                var newNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status change to {newStatus}: {adminNotes}";
                application.AdminNotes = string.IsNullOrEmpty(existingNotes)
                    ? newNote
                    : $"{existingNotes}\n\n{newNote}";
            }

            // Create audit log entry
            var auditLog = new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Status Changed",
                PerformedBy = adminUserId,
                PerformedAt = DateTime.UtcNow,
                OldValue = oldStatus.ToString(),
                NewValue = newStatus.ToString(),
                Notes = adminNotes
            };
            _context.VettingAuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);

            // Send email notification for status changes (failures should not prevent status change)
            // Only send emails for these statuses: InterviewApproved, InterviewScheduled, Approved, OnHold, Denied
            if (newStatus == VettingStatus.InterviewApproved ||
                newStatus == VettingStatus.InterviewScheduled ||
                newStatus == VettingStatus.Approved ||
                newStatus == VettingStatus.OnHold ||
                newStatus == VettingStatus.Denied)
            {
                try
                {
                    var emailResult = await _emailService.SendStatusUpdateAsync(
                        application,
                        application.Email,
                        application.SceneName,
                        newStatus,
                        cancellationToken);

                    if (!emailResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Failed to send status update email for application {ApplicationNumber}: {Error}",
                            application.ApplicationNumber, emailResult.Error);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Exception sending status update email for application {ApplicationNumber}",
                        application.ApplicationNumber);
                    // Continue - email failure should not prevent status change
                }
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Application {ApplicationId} status changed from {OldStatus} to {NewStatus} by admin {AdminUserId}",
                applicationId, oldStatus, newStatus, adminUserId);

            // Get updated application details to return
            return await GetApplicationDetailAsync(applicationId, adminUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating application status for {ApplicationId}", applicationId);
            return Result<ApplicationDetailResponse>.Failure(
                "Failed to update status", ex.Message);
        }
    }

    /// <summary>
    /// Schedule interview for an application
    /// Moves status to InterviewScheduled, sets interview date, and sends notification
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> ScheduleInterviewAsync(
        Guid applicationId,
        DateTime interviewDate,
        string interviewLocation,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check user authorization
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken);
            if (user == null || user.Role != "Administrator")
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Access denied", "Only administrators can schedule interviews.");
            }

            // Get application
            var application = await _context.VettingApplications
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(interviewLocation))
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Interview location required", "Interview location or instructions must be provided");
            }

            if (interviewDate <= DateTime.UtcNow)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Invalid interview date", "Interview date must be in the future.");
            }

            // Validate status transition to InterviewScheduled
            var oldStatus = application.Status;
            var transitionValidation = ValidateStatusTransition(oldStatus, VettingStatus.InterviewScheduled);
            if (!transitionValidation.IsSuccess)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Invalid status transition", transitionValidation.Error);
            }

            // Update application
            application.Status = VettingStatus.InterviewScheduled;
            application.InterviewScheduledFor = interviewDate;
            application.UpdatedAt = DateTime.UtcNow;

            // Add note about interview scheduling
            var noteText = $"Interview scheduled for {interviewDate:yyyy-MM-dd HH:mm} UTC. Location: {interviewLocation}";
            var existingNotes = application.AdminNotes ?? "";
            var newNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {noteText}";
            application.AdminNotes = string.IsNullOrEmpty(existingNotes)
                ? newNote
                : $"{existingNotes}\n\n{newNote}";

            // Create audit log entry
            var auditLog = new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Interview Scheduled",
                PerformedBy = adminUserId,
                PerformedAt = DateTime.UtcNow,
                OldValue = oldStatus.ToString(),
                NewValue = VettingStatus.InterviewScheduled.ToString(),
                Notes = noteText
            };
            _context.VettingAuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview scheduled for application {ApplicationId} at {InterviewDate} by admin {AdminUserId}",
                applicationId, interviewDate, adminUserId);

            // Get updated application details to return
            return await GetApplicationDetailAsync(applicationId, adminUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error scheduling interview for application {ApplicationId}", applicationId);
            return Result<ApplicationDetailResponse>.Failure(
                "Failed to schedule interview", ex.Message);
        }
    }

    /// <summary>
    /// Put application on hold with required reason and actions
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> PutOnHoldAsync(
        Guid applicationId,
        string reason,
        string requiredActions,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result<ApplicationDetailResponse>.Failure(
                "Reason required", "Reason for putting application on hold is required");
        }

        if (string.IsNullOrWhiteSpace(requiredActions))
        {
            return Result<ApplicationDetailResponse>.Failure(
                "Required actions needed", "Required actions for applicant must be specified");
        }

        // Combine reason and required actions into admin notes
        var adminNotes = $"On Hold - Reason: {reason}\nRequired Actions: {requiredActions}";

        // Use the general status update method
        return await UpdateApplicationStatusAsync(
            applicationId,
            VettingStatus.OnHold,
            adminNotes,
            adminUserId,
            cancellationToken);
    }

    /// <summary>
    /// Approve vetting application
    /// Sends approval email and updates user role to VettedMember
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> ApproveApplicationAsync(
        Guid applicationId,
        Guid adminUserId,
        string? adminNotes = null,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check user authorization
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken);
            if (admin == null || admin.Role != "Administrator")
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Access denied", "Only administrators can approve applications.");
            }

            // Get application
            var application = await _context.VettingApplications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Validate status - must be in InterviewScheduled or later
            if (application.Status < VettingStatus.InterviewScheduled)
            {
                return Result<ApplicationDetailResponse>.Failure(
                    "Invalid status for approval",
                    "Application must be in InterviewScheduled status or later before approval");
            }

            var oldStatus = application.Status;

            // Update application status
            application.Status = VettingStatus.Approved;
            application.DecisionMadeAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            // Add approval notes
            var noteText = adminNotes ?? "Application approved";
            var existingNotes = application.AdminNotes ?? "";
            var newNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Approved: {noteText}";
            application.AdminNotes = string.IsNullOrEmpty(existingNotes)
                ? newNote
                : $"{existingNotes}\n\n{newNote}";

            // Update user role if user is linked
            if (application.UserId.HasValue && application.User != null)
            {
                var user = application.User;

                // Update the Role property
                user.Role = "VettedMember";

                // Get the VettedMember role from database
                var vettedMemberRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "VettedMember", cancellationToken);

                if (vettedMemberRole != null)
                {
                    // Remove all existing role assignments for this user
                    var existingUserRoles = await _context.UserRoles
                        .Where(ur => ur.UserId == user.Id)
                        .ToListAsync(cancellationToken);

                    if (existingUserRoles.Any())
                    {
                        _context.UserRoles.RemoveRange(existingUserRoles);
                    }

                    // Add VettedMember role assignment
                    var newUserRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
                    {
                        UserId = user.Id,
                        RoleId = vettedMemberRole.Id
                    };
                    _context.UserRoles.Add(newUserRole);

                    _logger.LogInformation(
                        "Granted VettedMember role to user {UserId} for approved application {ApplicationId}",
                        application.UserId.Value, applicationId);
                }
                else
                {
                    _logger.LogError(
                        "VettedMember role not found in database - cannot grant role for application {ApplicationId}",
                        applicationId);
                }
            }

            // Create audit log entry
            var auditLog = new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Action = "Application Approved",
                PerformedBy = adminUserId,
                PerformedAt = DateTime.UtcNow,
                OldValue = oldStatus.ToString(),
                NewValue = VettingStatus.Approved.ToString(),
                Notes = noteText
            };
            _context.VettingAuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Application {ApplicationId} approved by admin {AdminUserId}",
                applicationId, adminUserId);

            // Get updated application details to return
            return await GetApplicationDetailAsync(applicationId, adminUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error approving application {ApplicationId}", applicationId);
            return Result<ApplicationDetailResponse>.Failure(
                "Failed to approve application", ex.Message);
        }
    }

    /// <summary>
    /// Deny vetting application with required reason
    /// </summary>
    public async Task<Result<ApplicationDetailResponse>> DenyApplicationAsync(
        Guid applicationId,
        string reason,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate required reason
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result<ApplicationDetailResponse>.Failure(
                "Denial reason required", "A reason must be provided when denying an application");
        }

        // Use the general status update method with denial notes
        var adminNotes = $"Application denied. Reason: {reason}";

        return await UpdateApplicationStatusAsync(
            applicationId,
            VettingStatus.Denied,
            adminNotes,
            adminUserId,
            cancellationToken);
    }

    /// <summary>
    /// Validate status transition according to workflow rules
    /// </summary>
    /// <param name="currentStatus">Current status of the application</param>
    /// <param name="newStatus">New status to transition to</param>
    /// <returns>Result indicating if transition is valid</returns>
    private Result<bool> ValidateStatusTransition(VettingStatus currentStatus, VettingStatus newStatus)
    {
        // Define valid transitions
        var validTransitions = new Dictionary<VettingStatus, List<VettingStatus>>
        {
            [VettingStatus.Draft] = new() { VettingStatus.Submitted },
            [VettingStatus.Submitted] = new() { VettingStatus.UnderReview, VettingStatus.Withdrawn },
            [VettingStatus.UnderReview] = new() { VettingStatus.InterviewApproved, VettingStatus.OnHold, VettingStatus.Denied, VettingStatus.Withdrawn },
            [VettingStatus.InterviewApproved] = new() { VettingStatus.PendingInterview, VettingStatus.OnHold, VettingStatus.Withdrawn },
            [VettingStatus.PendingInterview] = new() { VettingStatus.InterviewScheduled, VettingStatus.OnHold, VettingStatus.Withdrawn },
            [VettingStatus.InterviewScheduled] = new() { VettingStatus.Approved, VettingStatus.OnHold, VettingStatus.Denied, VettingStatus.Withdrawn },
            [VettingStatus.OnHold] = new() { VettingStatus.UnderReview, VettingStatus.InterviewApproved, VettingStatus.Denied, VettingStatus.Withdrawn },
            [VettingStatus.Approved] = new(), // Terminal state - no transitions
            [VettingStatus.Denied] = new(),   // Terminal state - no transitions
            [VettingStatus.Withdrawn] = new() // Terminal state - no transitions
        };

        // Check if current status has valid transitions defined
        if (!validTransitions.ContainsKey(currentStatus))
        {
            return Result<bool>.Failure(
                $"Unknown current status: {currentStatus}");
        }

        // Check if new status is in the list of valid transitions
        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            return Result<bool>.Failure(
                $"Invalid transition from {currentStatus} to {newStatus}. " +
                $"Valid transitions: {string.Join(", ", validTransitions[currentStatus])}");
        }

        return Result<bool>.Success(true);
    }

    #endregion
}