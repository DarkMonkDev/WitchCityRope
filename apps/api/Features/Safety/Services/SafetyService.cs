using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Safety incident service using direct Entity Framework access
/// Example of simplified vertical slice architecture pattern
/// </summary>
public class SafetyService : ISafetyService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SafetyService> _logger;

    public SafetyService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ILogger<SafetyService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Submit new safety incident report
    /// Direct Entity Framework operations - NO MediatR complexity
    /// </summary>
    public async Task<Result<SubmissionResponse>> SubmitIncidentAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique reference number
            var referenceNumber = await GenerateReferenceNumberAsync(cancellationToken);

            // Auto-generate title from incident type and date if not provided
            var title = !string.IsNullOrEmpty(request.Title)
                ? request.Title
                : $"{request.Type} - {request.IncidentDate:yyyy-MM-dd}";

            // Create incident entity with encrypted sensitive data
            var incident = new SafetyIncident
            {
                ReferenceNumber = referenceNumber,
                Title = title,
                ReporterId = request.IsAnonymous ? null : request.ReporterId,
                IncidentDate = request.IncidentDate.ToUniversalTime(),
                Location = request.Location,
                Type = request.Type,
                WhereOccurred = request.WhereOccurred,
                EventName = request.EventName,
                HasSpokenToPerson = request.HasSpokenToPerson,
                DesiredOutcomes = request.DesiredOutcomes,
                FutureInteractionPreference = request.FutureInteractionPreference,
                AnonymousDuringInvestigation = request.AnonymousDuringInvestigation,
                AnonymousInFinalReport = request.AnonymousInFinalReport,
                EncryptedDescription = await _encryptionService.EncryptAsync(request.Description),
                EncryptedInvolvedParties = !string.IsNullOrEmpty(request.InvolvedParties)
                    ? await _encryptionService.EncryptAsync(request.InvolvedParties) : null,
                EncryptedWitnesses = !string.IsNullOrEmpty(request.Witnesses)
                    ? await _encryptionService.EncryptAsync(request.Witnesses) : null,
                EncryptedContactEmail = !string.IsNullOrEmpty(request.ContactEmail)
                    ? await _encryptionService.EncryptAsync(request.ContactEmail) : null,
                EncryptedContactName = !string.IsNullOrEmpty(request.ContactName)
                    ? await _encryptionService.EncryptAsync(request.ContactName) : null,
                IsAnonymous = request.IsAnonymous,
                RequestFollowUp = request.RequestFollowUp,
                Status = IncidentStatus.ReportSubmitted,
                CreatedBy = request.IsAnonymous ? null : request.ReporterId
            };

            // Save to database
            _context.SafetyIncidents.Add(incident);
            await _context.SaveChangesAsync(cancellationToken);

            // Log incident creation
            await _auditService.LogActionAsync(
                incident.Id,
                request.IsAnonymous ? null : request.ReporterId,
                "Created",
                "Safety incident report submitted",
                cancellationToken: cancellationToken);

            var response = new SubmissionResponse
            {
                ReferenceNumber = referenceNumber,
                TrackingUrl = $"/safety/track/{referenceNumber}",
                SubmittedAt = incident.CreatedAt
            };

            _logger.LogInformation("Safety incident submitted successfully: {ReferenceNumber}, Anonymous: {IsAnonymous}",
                referenceNumber, incident.IsAnonymous);

            return Result<SubmissionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit safety incident");
            return Result<SubmissionResponse>.Failure("Failed to submit incident report", ex.Message);
        }
    }

    /// <summary>
    /// Get incident status for anonymous tracking
    /// Public endpoint accessible without authentication
    /// </summary>
    public async Task<Result<IncidentStatusResponse>> GetIncidentStatusAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(i => i.ReferenceNumber == referenceNumber)
                .Select(i => new IncidentStatusResponse
                {
                    ReferenceNumber = i.ReferenceNumber,
                    Status = i.Status.ToString(),
                    LastUpdated = i.UpdatedAt,
                    CanProvideMoreInfo = !i.IsAnonymous && i.RequestFollowUp
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (incident == null)
            {
                return Result<IncidentStatusResponse>.Failure("Incident not found");
            }

            return Result<IncidentStatusResponse>.Success(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident status for reference: {ReferenceNumber}", referenceNumber);
            return Result<IncidentStatusResponse>.Failure("Failed to retrieve incident status");
        }
    }

    /// <summary>
    /// Get detailed incident for safety team with decrypted data
    /// Requires safety team authorization
    /// </summary>
    public async Task<Result<IncidentResponse>> GetIncidentDetailAsync(
        Guid incidentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user has safety team access
            var hasAccess = await VerifySafetyTeamAccessAsync(userId, cancellationToken);
            if (!hasAccess)
            {
                return Result<IncidentResponse>.Failure("Access denied - safety team role required");
            }

            var incident = await _context.SafetyIncidents
                .Include(i => i.Reporter)
                .Include(i => i.AssignedUser)
                .Include(i => i.AuditLogs)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<IncidentResponse>.Failure("Incident not found");
            }

            // Decrypt sensitive data for safety team
            var response = new IncidentResponse
            {
                Id = incident.Id,
                ReferenceNumber = incident.ReferenceNumber,
                Title = incident.Title,
                ReporterId = incident.ReporterId,
                ReporterName = incident.Reporter?.SceneName,
                IncidentDate = incident.IncidentDate,
                ReportedAt = incident.ReportedAt,
                Location = incident.Location,
                Type = incident.Type,
                WhereOccurred = incident.WhereOccurred,
                EventName = incident.EventName,
                HasSpokenToPerson = incident.HasSpokenToPerson,
                DesiredOutcomes = incident.DesiredOutcomes,
                FutureInteractionPreference = incident.FutureInteractionPreference,
                AnonymousDuringInvestigation = incident.AnonymousDuringInvestigation,
                AnonymousInFinalReport = incident.AnonymousInFinalReport,
                Description = await _encryptionService.DecryptAsync(incident.EncryptedDescription),
                InvolvedParties = !string.IsNullOrEmpty(incident.EncryptedInvolvedParties)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedInvolvedParties) : null,
                Witnesses = !string.IsNullOrEmpty(incident.EncryptedWitnesses)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedWitnesses) : null,
                ContactEmail = !string.IsNullOrEmpty(incident.EncryptedContactEmail)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedContactEmail) : null,
                ContactName = !string.IsNullOrEmpty(incident.EncryptedContactName)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedContactName) : null,
                IsAnonymous = incident.IsAnonymous,
                RequestFollowUp = incident.RequestFollowUp,
                Status = incident.Status,
                AssignedTo = incident.AssignedTo,
                AssignedUserName = incident.AssignedUser?.SceneName,
                AuditTrail = incident.AuditLogs.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    UserId = a.UserId,
                    UserName = a.User?.SceneName,
                    CreatedAt = a.CreatedAt
                }).OrderByDescending(a => a.CreatedAt).ToList(),
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt
            };

            // Log access to incident
            await _auditService.LogActionAsync(incidentId, userId, "Viewed",
                "Incident details accessed by safety team member", cancellationToken: cancellationToken);

            return Result<IncidentResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident detail: {IncidentId}", incidentId);
            return Result<IncidentResponse>.Failure("Failed to retrieve incident details");
        }
    }

    /// <summary>
    /// Get safety dashboard data for admin interface
    /// </summary>
    public async Task<Result<AdminDashboardResponse>> GetDashboardDataAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user has safety team access
            var hasAccess = await VerifySafetyTeamAccessAsync(userId, cancellationToken);
            if (!hasAccess)
            {
                return Result<AdminDashboardResponse>.Failure("Access denied - safety team role required");
            }

            // Get statistics
            var statistics = new SafetyStatistics();

            var statusCounts = await _context.SafetyIncidents
                .AsNoTracking() // Read-only query - 20-40% performance improvement
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var count in statusCounts)
            {
                switch (count.Status)
                {
                    case IncidentStatus.ReportSubmitted:
                        statistics.NewCount = count.Count;
                        break;
                    case IncidentStatus.InformationGathering:
                        statistics.InProgressCount = count.Count;
                        break;
                    case IncidentStatus.ReviewingFinalReport:
                        statistics.ReviewingFinalReportCount = count.Count;
                        break;
                    case IncidentStatus.Closed:
                        statistics.ResolvedCount = count.Count;
                        break;
                }
            }

            statistics.TotalCount = await _context.SafetyIncidents.CountAsync(cancellationToken);
            statistics.ThisMonth = await _context.SafetyIncidents
                .CountAsync(i => i.ReportedAt >= DateTime.UtcNow.AddDays(-30), cancellationToken);

            // Get recent incidents
            var recentIncidents = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(i => i.Status != IncidentStatus.Closed)
                .OrderByDescending(i => i.ReportedAt)
                .Take(10)
                .Select(i => new IncidentSummaryResponse
                {
                    Id = i.Id,
                    ReferenceNumber = i.ReferenceNumber,
                    Title = i.Title,
                    Status = i.Status,
                    ReportedAt = i.ReportedAt,
                    IncidentDate = i.IncidentDate,
                    Location = i.Location,
                    IsAnonymous = i.IsAnonymous,
                    AssignedTo = i.AssignedTo,
                    AssignedUserName = i.AssignedUser != null ? i.AssignedUser.SceneName : null
                })
                .ToListAsync(cancellationToken);

            var response = new AdminDashboardResponse
            {
                Statistics = statistics,
                RecentIncidents = recentIncidents,
                PendingActions = new List<ActionItem>() // TODO: Implement pending actions logic
            };

            return Result<AdminDashboardResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data for user: {UserId}", userId);
            return Result<AdminDashboardResponse>.Failure("Failed to retrieve dashboard data");
        }
    }

    /// <summary>
    /// Get user's own incident reports
    /// </summary>
    public async Task<Result<IEnumerable<IncidentSummaryResponse>>> GetUserReportsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incidents = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(i => i.ReporterId == userId)
                .OrderByDescending(i => i.ReportedAt)
                .Select(i => new IncidentSummaryResponse
                {
                    Id = i.Id,
                    ReferenceNumber = i.ReferenceNumber,
                    Title = i.Title,
                    Status = i.Status,
                    ReportedAt = i.ReportedAt,
                    IncidentDate = i.IncidentDate,
                    Location = i.Location,
                    IsAnonymous = i.IsAnonymous,
                    AssignedTo = i.AssignedTo,
                    AssignedUserName = i.AssignedUser != null ? i.AssignedUser.SceneName : null
                })
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<IncidentSummaryResponse>>.Success(incidents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user reports for user: {UserId}", userId);
            return Result<IEnumerable<IncidentSummaryResponse>>.Failure("Failed to retrieve user reports");
        }
    }

    /// <summary>
    /// Generate unique reference number using PostgreSQL function
    /// </summary>
    private async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken)
    {
        // Use EF Core 9 compatible syntax for raw SQL
        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT generate_safety_reference_number()";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString() ?? throw new InvalidOperationException("Failed to generate reference number");
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Verify user has safety team access
    /// </summary>
    private async Task<bool> VerifySafetyTeamAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Check if user has SafetyTeam or Administrator role
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return user?.Role == "SafetyTeam" || user?.Role == "Administrator";
    }
}