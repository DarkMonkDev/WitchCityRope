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

    public VettingService(ApplicationDbContext context, ILogger<VettingService> logger)
    {
        _context = context;
        _logger = logger;
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
}