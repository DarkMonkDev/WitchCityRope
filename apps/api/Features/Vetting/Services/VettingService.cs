using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Main vetting service implementation
/// Direct Entity Framework usage following vertical slice pattern
/// Handles application lifecycle, encryption, and business logic
/// </summary>
public class VettingService : IVettingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<VettingService> _logger;
    private readonly IConfiguration _configuration;

    public VettingService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<VettingService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Submit new vetting application with full encryption and validation
    /// </summary>
    public async Task<Result<ApplicationSubmissionResponse>> SubmitApplicationAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing application submission for {Email}", request.Email);

            // Check for existing applications from same email
            var existingApplicationQuery = _context.VettingApplications
                .Where(a => a.DeletedAt == null);

            var existingApplications = await existingApplicationQuery
                .ToListAsync(cancellationToken);

            // Decrypt and check for duplicate email
            foreach (var existing in existingApplications)
            {
                var decryptedEmail = await _encryptionService.DecryptAsync(existing.EncryptedEmail);
                if (decryptedEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if it's a recent application (within 30 days)
                    if (existing.CreatedAt > DateTime.UtcNow.AddDays(-30))
                    {
                        return Result<ApplicationSubmissionResponse>.Failure(
                            "Recent application exists",
                            "An application from this email address was submitted recently. Please wait 30 days before submitting another application.");
                    }
                }
            }

            // Check for scene name uniqueness (if not anonymous)
            if (!request.IsAnonymous)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.SceneName == request.SceneName, cancellationToken);

                if (existingUser != null)
                {
                    return Result<ApplicationSubmissionResponse>.Failure(
                        "Scene name taken",
                        "This scene name is already in use. Please choose a different scene name.");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Generate application number: VET-YYYYMMDD-NNNN
                var applicationNumber = await GenerateApplicationNumberAsync(cancellationToken);

                // Create application with encrypted PII
                var application = new VettingApplication
                {
                    ApplicationNumber = applicationNumber,
                    Status = ApplicationStatus.Submitted,
                    
                    // Encrypt all PII fields
                    EncryptedFullName = await _encryptionService.EncryptAsync(request.FullName),
                    EncryptedSceneName = await _encryptionService.EncryptAsync(request.SceneName),
                    EncryptedPronouns = !string.IsNullOrEmpty(request.Pronouns) 
                        ? await _encryptionService.EncryptAsync(request.Pronouns) 
                        : null,
                    EncryptedEmail = await _encryptionService.EncryptAsync(request.Email),
                    EncryptedPhone = !string.IsNullOrEmpty(request.Phone) 
                        ? await _encryptionService.EncryptAsync(request.Phone) 
                        : null,

                    // Experience information
                    ExperienceLevel = (ExperienceLevel)request.ExperienceLevel,
                    YearsExperience = request.YearsExperience,
                    EncryptedExperienceDescription = await _encryptionService.EncryptAsync(request.ExperienceDescription),
                    EncryptedSafetyKnowledge = await _encryptionService.EncryptAsync(request.SafetyKnowledge),
                    EncryptedConsentUnderstanding = await _encryptionService.EncryptAsync(request.ConsentUnderstanding),

                    // Community information
                    EncryptedWhyJoinCommunity = await _encryptionService.EncryptAsync(request.WhyJoinCommunity),
                    SkillsInterests = JsonSerializer.Serialize(request.SkillsInterests),
                    EncryptedExpectationsGoals = await _encryptionService.EncryptAsync(request.ExpectationsGoals),
                    AgreesToGuidelines = request.AgreesToGuidelines,

                    // Privacy settings
                    IsAnonymous = request.IsAnonymous,
                    AgreesToTerms = request.AgreesToTerms,
                    ConsentToContact = request.ConsentToContact,

                    // Workflow settings
                    Priority = ApplicationPriority.Standard,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VettingApplications.Add(application);
                await _context.SaveChangesAsync(cancellationToken);

                // Create references
                var references = new List<VettingReference>();
                foreach (var refRequest in request.References)
                {
                    var reference = new VettingReference
                    {
                        ApplicationId = application.Id,
                        ReferenceOrder = refRequest.Order,
                        EncryptedName = await _encryptionService.EncryptAsync(refRequest.Name),
                        EncryptedEmail = await _encryptionService.EncryptAsync(refRequest.Email),
                        EncryptedRelationship = await _encryptionService.EncryptAsync(refRequest.Relationship),
                        Status = ReferenceStatus.NotContacted,
                        FormExpiresAt = DateTime.UtcNow.AddDays(14), // 2 weeks to respond
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    references.Add(reference);
                    _context.VettingReferences.Add(reference);
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Create audit log entry
                var auditLog = new VettingApplicationAuditLog
                {
                    ApplicationId = application.Id,
                    ActionType = "ApplicationSubmitted",
                    ActionDescription = "New vetting application submitted",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApplicationNumber = application.ApplicationNumber,
                        Status = application.Status.ToString(),
                        ExperienceLevel = application.ExperienceLevel.ToString(),
                        YearsExperience = application.YearsExperience,
                        IsAnonymous = application.IsAnonymous,
                        ReferenceCount = references.Count
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                _context.VettingApplicationAuditLog.Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Application {ApplicationNumber} submitted successfully for {Email}", 
                    application.ApplicationNumber, request.Email);

                // Build response with reference status
                var referenceStatuses = references.Select((r, index) => new ReferenceStatusSummary
                {
                    Name = MaskName(request.References[index].Name),
                    Email = MaskEmail(request.References[index].Email),
                    Status = "NotContacted",
                    ContactedAt = null,
                    RespondedAt = null
                }).ToList();

                var response = new ApplicationSubmissionResponse
                {
                    ApplicationId = application.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    StatusToken = application.StatusToken,
                    SubmittedAt = application.CreatedAt,
                    ConfirmationMessage = "Your application has been submitted successfully. You will receive a confirmation email shortly.",
                    EstimatedReviewDays = GetEstimatedReviewDays(),
                    NextSteps = "Our vetting team will contact your references within 2-3 business days and begin the review process.",
                    ReferenceStatuses = referenceStatuses
                };

                // Queue confirmation email notification
                // TODO: Implement notification queuing

                return Result<ApplicationSubmissionResponse>.Success(response);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process application submission for {Email}", request.Email);
            return Result<ApplicationSubmissionResponse>.Failure(
                "Submission failed",
                "An error occurred while processing your application. Please try again or contact support.");
        }
    }

    /// <summary>
    /// Get application status using secure token
    /// </summary>
    public async Task<Result<ApplicationStatusResponse>> GetApplicationStatusAsync(
        string statusToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _context.VettingApplications
                .Include(a => a.References)
                .FirstOrDefaultAsync(a => a.StatusToken == statusToken && a.DeletedAt == null, cancellationToken);

            if (application == null)
            {
                return Result<ApplicationStatusResponse>.Failure(
                    "Application not found",
                    "No application found with the provided status token.");
            }

            // Calculate progress
            var progress = CalculateApplicationProgress(application);

            // Get recent status updates (limited)
            var recentUpdates = await GetRecentStatusUpdatesAsync(application.Id, cancellationToken);

            var response = new ApplicationStatusResponse
            {
                ApplicationNumber = application.ApplicationNumber,
                Status = application.Status.ToString(),
                SubmittedAt = application.CreatedAt,
                StatusDescription = GetStatusDescription(application.Status),
                LastUpdateAt = application.UpdatedAt,
                EstimatedDaysRemaining = CalculateEstimatedDaysRemaining(application),
                Progress = progress,
                RecentUpdates = recentUpdates
            };

            return Result<ApplicationStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get application status for token {StatusToken}", statusToken);
            return Result<ApplicationStatusResponse>.Failure(
                "Status lookup failed",
                "An error occurred while looking up application status.");
        }
    }

    /// <summary>
    /// Get applications for reviewer dashboard with filtering
    /// </summary>
    public async Task<Result<PagedResult<ApplicationSummaryDto>>> GetApplicationsForReviewAsync(
        Guid reviewerId,
        ApplicationFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify reviewer exists and is active
            var reviewer = await _context.VettingReviewers
                .FirstOrDefaultAsync(r => r.Id == reviewerId && r.IsActive, cancellationToken);

            if (reviewer == null)
            {
                return Result<PagedResult<ApplicationSummaryDto>>.Failure(
                    "Reviewer not found",
                    "Reviewer not found or inactive.");
            }

            var query = _context.VettingApplications
                .Include(a => a.AssignedReviewer)
                .ThenInclude(r => r!.User)
                .Include(a => a.References)
                .Where(a => a.DeletedAt == null);

            // Apply filters
            query = ApplyFilters(query, filter, reviewerId);

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var skip = (filter.Page - 1) * filter.PageSize;
            var applications = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var summaries = new List<ApplicationSummaryDto>();
            foreach (var app in applications)
            {
                // Decrypt scene name for display
                var sceneName = await _encryptionService.DecryptAsync(app.EncryptedSceneName);
                
                var summary = new ApplicationSummaryDto
                {
                    Id = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    Status = app.Status.ToString(),
                    SubmittedAt = app.CreatedAt,
                    LastActivityAt = app.UpdatedAt,
                    SceneName = app.IsAnonymous ? "Anonymous" : sceneName,
                    ExperienceLevel = app.ExperienceLevel.ToString(),
                    YearsExperience = app.YearsExperience,
                    IsAnonymous = app.IsAnonymous,
                    AssignedReviewerName = app.AssignedReviewer?.User?.SceneName,
                    ReviewStartedAt = app.ReviewStartedAt,
                    Priority = (int)app.Priority,
                    DaysInCurrentStatus = (DateTime.UtcNow - app.UpdatedAt).Days,
                    InterviewScheduledFor = app.InterviewScheduledFor,
                    SkillsTags = JsonSerializer.Deserialize<List<string>>(app.SkillsInterests) ?? new()
                };

                // Calculate reference status
                summary.ReferenceStatus = new ApplicationReferenceStatus
                {
                    TotalReferences = app.References.Count,
                    ContactedReferences = app.References.Count(r => r.Status != ReferenceStatus.NotContacted),
                    RespondedReferences = app.References.Count(r => r.Status == ReferenceStatus.Responded),
                    AllReferencesComplete = app.References.All(r => r.Status == ReferenceStatus.Responded),
                    OldestPendingReferenceDate = app.References
                        .Where(r => r.Status != ReferenceStatus.Responded)
                        .Min(r => (DateTime?)r.CreatedAt)
                };

                summaries.Add(summary);
            }

            var pagedResult = new PagedResult<ApplicationSummaryDto>(summaries, totalCount, filter.Page, filter.PageSize);

            return Result<PagedResult<ApplicationSummaryDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applications for reviewer {ReviewerId}", reviewerId);
            return Result<PagedResult<ApplicationSummaryDto>>.Failure(
                "Query failed",
                "An error occurred while retrieving applications.");
        }
    }

    // Placeholder implementations for other interface methods
    public Task<Result<ApplicationDetailResponse>> GetApplicationDetailAsync(Guid applicationId, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<ReviewDecisionResponse>> SubmitReviewDecisionAsync(Guid applicationId, ReviewDecisionRequest request, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<AssignmentResponse>> AssignApplicationAsync(Guid applicationId, Guid reviewerId, Guid assignedByUserId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<NoteResponse>> AddApplicationNoteAsync(Guid applicationId, CreateNoteRequest request, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<AnalyticsDashboardResponse>> GetAnalyticsDashboardAsync(AnalyticsFilterRequest filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<NotificationResponse>> SendManualNotificationAsync(Guid applicationId, ManualNotificationRequest request, Guid sentByUserId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<PriorityUpdateResponse>> UpdateApplicationPriorityAsync(Guid applicationId, UpdatePriorityRequest request, Guid updatedByUserId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    // Helper methods
    private async Task<string> GenerateApplicationNumberAsync(CancellationToken cancellationToken)
    {
        var datePrefix = $"VET-{DateTime.UtcNow:yyyyMMdd}";
        
        var existingCount = await _context.VettingApplications
            .CountAsync(a => a.ApplicationNumber.StartsWith(datePrefix), cancellationToken);

        var sequence = (existingCount + 1).ToString("D4");
        return $"{datePrefix}-{sequence}";
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= 2)
            return name;
        
        return $"{name[0]}***{name[^1]}";
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;
        
        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];
        
        if (localPart.Length <= 2)
            return email;
        
        var maskedLocal = $"{localPart[0]}***{localPart[^1]}";
        return $"{maskedLocal}@{domain}";
    }

    private int GetEstimatedReviewDays()
    {
        return _configuration.GetValue<int>("Vetting:EstimatedReviewDays", 14);
    }

    private static ApplicationProgressSummary CalculateApplicationProgress(VettingApplication application)
    {
        var progress = new ApplicationProgressSummary
        {
            ApplicationSubmitted = true,
            ReferencesContacted = application.References.Any(r => r.Status != ReferenceStatus.NotContacted),
            ReferencesReceived = application.References.All(r => r.Status == ReferenceStatus.Responded),
            UnderReview = application.Status == ApplicationStatus.UnderReview,
            InterviewScheduled = application.InterviewScheduledFor.HasValue,
            DecisionMade = application.Status == ApplicationStatus.Approved || application.Status == ApplicationStatus.Denied
        };

        // Calculate percentage
        var completedSteps = 0;
        if (progress.ApplicationSubmitted) completedSteps++;
        if (progress.ReferencesContacted) completedSteps++;
        if (progress.ReferencesReceived) completedSteps++;
        if (progress.UnderReview) completedSteps++;
        if (progress.DecisionMade) completedSteps++;

        progress.ProgressPercentage = (completedSteps * 100) / 5;
        progress.CurrentPhase = GetCurrentPhase(application.Status);

        return progress;
    }

    private static string GetCurrentPhase(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.Submitted => "Application Submitted",
            ApplicationStatus.PendingReferences => "Contacting References",
            ApplicationStatus.UnderReview => "Under Review",
            ApplicationStatus.PendingInterview => "Interview Scheduled",
            ApplicationStatus.PendingAdditionalInfo => "Additional Information Requested",
            ApplicationStatus.Approved => "Approved",
            ApplicationStatus.Denied => "Decision Made",
            _ => "Processing"
        };
    }

    private static string GetStatusDescription(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.Submitted => "Your application has been received and is being processed.",
            ApplicationStatus.PendingReferences => "We are contacting your references for feedback.",
            ApplicationStatus.UnderReview => "Your application is currently under review by our vetting team.",
            ApplicationStatus.PendingInterview => "An interview has been scheduled. Check your email for details.",
            ApplicationStatus.PendingAdditionalInfo => "We need additional information from you. Check your email for details.",
            ApplicationStatus.Approved => "Congratulations! Your application has been approved.",
            ApplicationStatus.Denied => "Your application was not approved at this time.",
            _ => "Your application is being processed."
        };
    }

    private int? CalculateEstimatedDaysRemaining(VettingApplication application)
    {
        var estimatedTotalDays = GetEstimatedReviewDays();
        var daysElapsed = (DateTime.UtcNow - application.CreatedAt).Days;
        var remaining = estimatedTotalDays - daysElapsed;
        
        return remaining > 0 ? remaining : null;
    }

    private async Task<List<StatusUpdateSummary>> GetRecentStatusUpdatesAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        // Get recent audit log entries for public status updates
        var auditLogs = await _context.VettingApplicationAuditLog
            .Where(a => a.ApplicationId == applicationId)
            .Where(a => a.ActionType == "StatusChange" || a.ActionType == "ReferenceUpdate")
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        return auditLogs.Select(a => new StatusUpdateSummary
        {
            UpdatedAt = a.CreatedAt,
            Message = GetPublicUpdateMessage(a.ActionType, a.ActionDescription),
            Type = a.ActionType
        }).ToList();
    }

    private static string GetPublicUpdateMessage(string actionType, string actionDescription)
    {
        return actionType switch
        {
            "StatusChange" => "Application status updated",
            "ReferenceUpdate" => "Reference status updated",
            _ => "Application updated"
        };
    }

    private static IQueryable<VettingApplication> ApplyFilters(
        IQueryable<VettingApplication> query, 
        ApplicationFilterRequest filter, 
        Guid reviewerId)
    {
        // Status filters
        if (filter.StatusFilters.Any())
        {
            var statusEnums = filter.StatusFilters
                .Select(s => Enum.Parse<ApplicationStatus>(s))
                .ToList();
            query = query.Where(a => statusEnums.Contains(a.Status));
        }

        // Assignment filters
        if (filter.OnlyMyAssignments == true)
        {
            query = query.Where(a => a.AssignedReviewerId == reviewerId);
        }

        if (filter.OnlyUnassigned == true)
        {
            query = query.Where(a => a.AssignedReviewerId == null);
        }

        if (filter.AssignedReviewerId.HasValue)
        {
            query = query.Where(a => a.AssignedReviewerId == filter.AssignedReviewerId);
        }

        // Priority filters
        if (filter.PriorityFilters.Any())
        {
            var priorities = filter.PriorityFilters.Cast<ApplicationPriority>().ToList();
            query = query.Where(a => priorities.Contains(a.Priority));
        }

        // Experience filters
        if (filter.ExperienceLevelFilters.Any())
        {
            var levels = filter.ExperienceLevelFilters.Cast<ExperienceLevel>().ToList();
            query = query.Where(a => levels.Contains(a.ExperienceLevel));
        }

        if (filter.MinYearsExperience.HasValue)
        {
            query = query.Where(a => a.YearsExperience >= filter.MinYearsExperience);
        }

        if (filter.MaxYearsExperience.HasValue)
        {
            query = query.Where(a => a.YearsExperience <= filter.MaxYearsExperience);
        }

        // Date filters
        if (filter.SubmittedAfter.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= filter.SubmittedAfter);
        }

        if (filter.SubmittedBefore.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= filter.SubmittedBefore);
        }

        if (filter.LastActivityAfter.HasValue)
        {
            query = query.Where(a => a.UpdatedAt >= filter.LastActivityAfter);
        }

        if (filter.LastActivityBefore.HasValue)
        {
            query = query.Where(a => a.UpdatedAt <= filter.LastActivityBefore);
        }

        // Search query
        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.Where(a => a.ApplicationNumber.Contains(filter.SearchQuery));
            // Note: Scene name search would require decryption, so we limit to application number
        }

        return query;
    }

    private static IQueryable<VettingApplication> ApplySorting(
        IQueryable<VettingApplication> query, 
        string sortBy, 
        string sortDirection)
    {
        var isDescending = sortDirection.Equals("Desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "SubmittedAt" => isDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            "LastActivity" => isDescending ? query.OrderByDescending(a => a.UpdatedAt) : query.OrderBy(a => a.UpdatedAt),
            "Priority" => isDescending ? query.OrderByDescending(a => a.Priority) : query.OrderBy(a => a.Priority),
            "Status" => isDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => query.OrderByDescending(a => a.CreatedAt) // Default sort
        };
    }
}