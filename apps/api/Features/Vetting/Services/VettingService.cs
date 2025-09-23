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
                Notes = new List<ApplicationNoteDto>(), // Not implemented yet
                Decisions = new List<ReviewDecisionDto>() // Not implemented yet
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

            // Update application status based on decision
            var newStatus = request.DecisionType switch
            {
                1 => VettingStatus.Approved, // Approve
                2 => VettingStatus.Denied,   // Deny
                3 => VettingStatus.OnHold,   // Request additional info
                4 => VettingStatus.PendingInterview, // Schedule interview
                _ => application.Status // No change
            };

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
}