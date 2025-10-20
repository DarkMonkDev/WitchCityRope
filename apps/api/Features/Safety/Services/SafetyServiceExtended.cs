using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Extended safety incident service implementing all 12 endpoints for comprehensive incident management
/// Inherits base functionality from SafetyService and adds admin dashboard, coordinator workflow, and notes
/// </summary>
public class SafetyServiceExtended : SafetyService, ISafetyServiceExtended
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SafetyServiceExtended> _logger;

    public SafetyServiceExtended(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ILogger<SafetyServiceExtended> logger)
        : base(context, encryptionService, auditService, logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    #region Phase 2: Admin Dashboard

    /// <summary>
    /// Get paginated list of incidents with filtering and sorting
    /// Authorization: Admin can see all, Coordinator can only see assigned incidents
    /// </summary>
    public async Task<Result<PaginatedIncidentListResponse>> GetIncidentsAsync(
        AdminIncidentListRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SafetyIncidents
                .Include(i => i.Reporter)
                .Include(i => i.Coordinator)
                .Include(i => i.Notes)
                .AsNoTracking()
                .AsQueryable();

            // Authorization: Coordinators can only see assigned incidents
            if (!isAdmin)
            {
                query = query.Where(i => i.CoordinatorId == userId);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(i =>
                    i.ReferenceNumber.ToLower().Contains(searchLower) ||
                    i.Title.ToLower().Contains(searchLower) ||
                    i.Location.ToLower().Contains(searchLower));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statuses = request.Status.Split(',')
                    .Select(s => Enum.TryParse<IncidentStatus>(s.Trim(), true, out var status) ? status : (IncidentStatus?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                if (statuses.Any())
                {
                    query = query.Where(i => statuses.Contains(i.Status));
                }
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(i => i.IncidentDate >= request.StartDate.Value.ToUniversalTime());
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(i => i.IncidentDate <= request.EndDate.Value.ToUniversalTime());
            }

            // Apply coordinator filter
            if (request.AssignedTo.HasValue)
            {
                query = query.Where(i => i.CoordinatorId == request.AssignedTo.Value);
            }

            // Apply unassigned filter
            if (request.Unassigned == true)
            {
                query = query.Where(i => i.CoordinatorId == null && i.Status != IncidentStatus.Closed);
            }

            // Apply type filter
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var types = request.Type.Split(',')
                    .Select(t => Enum.TryParse<IncidentType>(t.Trim(), true, out var type) ? type : (IncidentType?)null)
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value)
                    .ToList();

                if (types.Any())
                {
                    query = query.Where(i => types.Contains(i.Type));
                }
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "status" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(i => i.Status)
                    : query.OrderByDescending(i => i.Status),
                "type" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(i => i.Type)
                    : query.OrderByDescending(i => i.Type),
                "incidentdate" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(i => i.IncidentDate)
                    : query.OrderByDescending(i => i.IncidentDate),
                "location" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(i => i.Location)
                    : query.OrderByDescending(i => i.Location),
                _ => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(i => i.ReportedAt)
                    : query.OrderByDescending(i => i.ReportedAt)
            };

            // Apply pagination
            var incidents = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Decrypt and map to DTOs
            var items = new List<IncidentSummaryDto>();
            foreach (var incident in incidents)
            {
                var description = await _encryptionService.DecryptAsync(incident.EncryptedDescription);
                var involvedParties = !string.IsNullOrEmpty(incident.EncryptedInvolvedParties)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedInvolvedParties) : null;
                var witnesses = !string.IsNullOrEmpty(incident.EncryptedWitnesses)
                    ? await _encryptionService.DecryptAsync(incident.EncryptedWitnesses) : null;

                items.Add(new IncidentSummaryDto
                {
                    Id = incident.Id,
                    ReferenceNumber = incident.ReferenceNumber,
                    Title = incident.Title,
                    Status = incident.Status,
                    Type = incident.Type,
                    IncidentDate = incident.IncidentDate,
                    ReportedAt = incident.ReportedAt,
                    LastUpdatedAt = incident.UpdatedAt,
                    Location = incident.Location,
                    Description = description.Length > 200 ? description.Substring(0, 200) + "..." : description,
                    IsAnonymous = incident.IsAnonymous,
                    ReporterId = incident.ReporterId,
                    ReporterName = incident.Reporter?.SceneName,
                    CoordinatorId = incident.CoordinatorId,
                    CoordinatorName = incident.Coordinator?.SceneName,
                    InvolvedParties = involvedParties,
                    Witnesses = witnesses,
                    GoogleDriveFolderUrl = incident.GoogleDriveFolderUrl,
                    GoogleDriveFinalReportUrl = incident.GoogleDriveFinalReportUrl,
                    NoteCount = incident.Notes.Count
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var response = new PaginatedIncidentListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };

            return Result<PaginatedIncidentListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incidents list for user {UserId}", userId);
            return Result<PaginatedIncidentListResponse>.Failure("Failed to retrieve incidents");
        }
    }

    /// <summary>
    /// Get dashboard statistics for admin interface
    /// </summary>
    public async Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            // Get unassigned count
            var unassignedCount = await _context.SafetyIncidents
                .Where(i => i.CoordinatorId == null && i.Status != IncidentStatus.Closed)
                .CountAsync(cancellationToken);

            // Check for old unassigned incidents
            var hasOldUnassigned = await _context.SafetyIncidents
                .AnyAsync(i => i.CoordinatorId == null &&
                              i.Status != IncidentStatus.Closed &&
                              i.ReportedAt < sevenDaysAgo,
                    cancellationToken);

            // Get recent incidents (last 5, excluding Closed)
            var query = _context.SafetyIncidents
                .Include(i => i.Reporter)
                .Include(i => i.Coordinator)
                .Where(i => i.Status != IncidentStatus.Closed)
                .AsNoTracking();

            // Authorization: Coordinators see only their assigned incidents
            if (!isAdmin)
            {
                query = query.Where(i => i.CoordinatorId == userId);
            }

            var recentIncidents = await query
                .OrderByDescending(i => i.ReportedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            // Decrypt and map to DTOs
            var recentDtos = new List<IncidentSummaryDto>();
            foreach (var incident in recentIncidents)
            {
                var description = await _encryptionService.DecryptAsync(incident.EncryptedDescription);

                recentDtos.Add(new IncidentSummaryDto
                {
                    Id = incident.Id,
                    ReferenceNumber = incident.ReferenceNumber,
                    Status = incident.Status,
                    IncidentDate = incident.IncidentDate,
                    ReportedAt = incident.ReportedAt,
                    LastUpdatedAt = incident.UpdatedAt,
                    Location = incident.Location,
                    Description = description.Length > 100 ? description.Substring(0, 100) + "..." : description,
                    IsAnonymous = incident.IsAnonymous,
                    ReporterId = incident.ReporterId,
                    ReporterName = incident.Reporter?.SceneName,
                    CoordinatorId = incident.CoordinatorId,
                    CoordinatorName = incident.Coordinator?.SceneName,
                    GoogleDriveFolderUrl = incident.GoogleDriveFolderUrl,
                    GoogleDriveFinalReportUrl = incident.GoogleDriveFinalReportUrl,
                    NoteCount = 0 // Not loaded for performance
                });
            }

            var response = new DashboardStatisticsResponse
            {
                UnassignedCount = unassignedCount,
                HasOldUnassigned = hasOldUnassigned,
                RecentIncidents = recentDtos
            };

            return Result<DashboardStatisticsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard statistics for user {UserId}", userId);
            return Result<DashboardStatisticsResponse>.Failure("Failed to retrieve dashboard statistics");
        }
    }

    /// <summary>
    /// Get all users for coordinator assignment dropdown
    /// Returns users with their active incident counts
    /// </summary>
    public async Task<Result<IEnumerable<UserCoordinatorDto>>> GetAllUsersForAssignmentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new UserCoordinatorDto
                {
                    Id = u.Id,
                    SceneName = u.SceneName ?? u.Email ?? "Unknown",
                    RealName = (u.FirstName != null && u.LastName != null)
                        ? $"{u.FirstName} {u.LastName}"
                        : (u.FirstName ?? u.LastName ?? ""),
                    Role = u.Role ?? "Member",
                    ActiveIncidentCount = _context.SafetyIncidents
                        .Count(i => i.CoordinatorId == u.Id && i.Status != IncidentStatus.Closed)
                })
                .OrderBy(u => u.SceneName)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<UserCoordinatorDto>>.Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users for assignment");
            return Result<IEnumerable<UserCoordinatorDto>>.Failure("Failed to retrieve user list");
        }
    }

    #endregion

    #region Phase 3: Incident Detail & Management

    /// <summary>
    /// Assign or unassign coordinator to incident
    /// Only admins can assign coordinators
    /// Creates system note on assignment/unassignment
    /// </summary>
    public async Task<Result<IncidentSummaryDto>> AssignCoordinatorAsync(
        Guid incidentId,
        AssignCoordinatorRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .Include(i => i.Coordinator)
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<IncidentSummaryDto>.Failure("Incident not found");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            var oldCoordinator = incident.Coordinator?.SceneName;
            var oldCoordinatorId = incident.CoordinatorId;

            // Update coordinator
            incident.CoordinatorId = request.CoordinatorId;
            incident.UpdatedAt = DateTime.UtcNow;
            incident.UpdatedBy = userId;

            // Create system note
            string noteContent;
            if (request.CoordinatorId.HasValue)
            {
                var newCoordinator = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.CoordinatorId.Value, cancellationToken);

                if (oldCoordinatorId.HasValue)
                {
                    noteContent = $"Coordinator changed from {oldCoordinator} to {newCoordinator?.SceneName} by {user?.SceneName}";
                }
                else
                {
                    noteContent = $"Assigned to {newCoordinator?.SceneName} by {user?.SceneName}";
                }
            }
            else
            {
                noteContent = $"Unassigned from {oldCoordinator} by {user?.SceneName}";
            }

            await CreateSystemNoteAsync(incidentId, noteContent, userId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Log assignment
            await _auditService.LogActionAsync(
                incidentId,
                userId,
                "Assigned",
                noteContent,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Incident {IncidentId} assignment updated by user {UserId}: {Note}",
                incidentId, userId, noteContent);

            // Reload coordinator navigation property
            await _context.Entry(incident).Reference(i => i.Coordinator).LoadAsync(cancellationToken);

            // Return summary
            var description = await _encryptionService.DecryptAsync(incident.EncryptedDescription);
            var summary = new IncidentSummaryDto
            {
                Id = incident.Id,
                ReferenceNumber = incident.ReferenceNumber,
                Status = incident.Status,
                IncidentDate = incident.IncidentDate,
                ReportedAt = incident.ReportedAt,
                LastUpdatedAt = incident.UpdatedAt,
                Location = incident.Location,
                Description = description.Length > 200 ? description.Substring(0, 200) + "..." : description,
                IsAnonymous = incident.IsAnonymous,
                CoordinatorId = incident.CoordinatorId,
                CoordinatorName = incident.Coordinator?.SceneName
            };

            return Result<IncidentSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign coordinator to incident {IncidentId}", incidentId);
            return Result<IncidentSummaryDto>.Failure("Failed to assign coordinator");
        }
    }

    /// <summary>
    /// Update incident status
    /// Creates system note for status change
    /// </summary>
    public async Task<Result<StatusUpdateResponse>> UpdateStatusAsync(
        Guid incidentId,
        UpdateStatusRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<StatusUpdateResponse>.Failure("Incident not found");
            }

            // Check authorization
            var canAccess = await CanUserAccessIncidentAsync(userId, incident, cancellationToken);
            if (!canAccess)
            {
                return Result<StatusUpdateResponse>.Failure("Access denied - you can only update incidents assigned to you");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            var oldStatus = incident.Status;
            incident.Status = request.NewStatus;
            incident.UpdatedAt = DateTime.UtcNow;
            incident.UpdatedBy = userId;

            // Create system note
            var noteContent = string.IsNullOrWhiteSpace(request.Reason)
                ? $"Status changed from {oldStatus} to {request.NewStatus} by {user?.SceneName}"
                : $"Status changed from {oldStatus} to {request.NewStatus} by {user?.SceneName}. Reason: {request.Reason}";

            await CreateSystemNoteAsync(incidentId, noteContent, userId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Log status change
            await _auditService.LogActionAsync(
                incidentId,
                userId,
                "StatusChanged",
                noteContent,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Incident {IncidentId} status updated by user {UserId}: {OldStatus} -> {NewStatus}",
                incidentId, userId, oldStatus, request.NewStatus);

            var response = new StatusUpdateResponse
            {
                Id = incident.Id,
                Status = incident.Status,
                LastUpdatedAt = incident.UpdatedAt,
                SystemNoteCreated = true
            };

            return Result<StatusUpdateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for incident {IncidentId}", incidentId);
            return Result<StatusUpdateResponse>.Failure("Failed to update incident status");
        }
    }

    /// <summary>
    /// Update Google Drive links for incident
    /// Creates system note on update
    /// </summary>
    public async Task<Result<GoogleDriveUpdateResponse>> UpdateGoogleDriveLinksAsync(
        Guid incidentId,
        UpdateGoogleDriveRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<GoogleDriveUpdateResponse>.Failure("Incident not found");
            }

            // Check authorization
            var canAccess = await CanUserAccessIncidentAsync(userId, incident, cancellationToken);
            if (!canAccess)
            {
                return Result<GoogleDriveUpdateResponse>.Failure("Access denied - you can only update incidents assigned to you");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            // Update Drive links
            incident.GoogleDriveFolderUrl = request.GoogleDriveFolderUrl;
            incident.GoogleDriveFinalReportUrl = request.GoogleDriveFinalReportUrl;
            incident.UpdatedAt = DateTime.UtcNow;
            incident.UpdatedBy = userId;

            // Create system note
            var noteContent = $"Google Drive links updated by {user?.SceneName}";
            await CreateSystemNoteAsync(incidentId, noteContent, userId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Log update
            await _auditService.LogActionAsync(
                incidentId,
                userId,
                "GoogleDriveUpdated",
                noteContent,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Incident {IncidentId} Google Drive links updated by user {UserId}",
                incidentId, userId);

            var response = new GoogleDriveUpdateResponse
            {
                Id = incident.Id,
                GoogleDriveFolderUrl = incident.GoogleDriveFolderUrl,
                GoogleDriveFinalReportUrl = incident.GoogleDriveFinalReportUrl,
                LastUpdatedAt = incident.UpdatedAt,
                SystemNoteCreated = true
            };

            return Result<GoogleDriveUpdateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Google Drive links for incident {IncidentId}", incidentId);
            return Result<GoogleDriveUpdateResponse>.Failure("Failed to update Google Drive links");
        }
    }

    #endregion

    #region Phase 4: Notes System

    /// <summary>
    /// Get all notes for an incident
    /// Filters private notes based on authorization
    /// </summary>
    public async Task<Result<NotesListResponse>> GetNotesAsync(
        Guid incidentId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<NotesListResponse>.Failure("Incident not found");
            }

            // Check authorization
            var canAccess = await CanUserAccessIncidentAsync(userId, incident, cancellationToken);
            if (!canAccess)
            {
                return Result<NotesListResponse>.Failure("Access denied");
            }

            var query = _context.IncidentNotes
                .Include(n => n.Author)
                .Where(n => n.IncidentId == incidentId)
                .AsNoTracking();

            // Non-admins and non-coordinators cannot see private notes
            if (!isAdmin && incident.CoordinatorId != userId)
            {
                query = query.Where(n => !n.IsPrivate);
            }

            var notes = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            // Decrypt notes (they are stored encrypted)
            var noteDtos = new List<IncidentNoteDto>();
            foreach (var note in notes)
            {
                var content = await _encryptionService.DecryptAsync(note.Content);
                noteDtos.Add(new IncidentNoteDto
                {
                    Id = note.Id,
                    IncidentId = note.IncidentId,
                    Content = content,
                    Type = note.Type,
                    IsPrivate = note.IsPrivate,
                    AuthorId = note.AuthorId,
                    AuthorName = note.Author?.SceneName,
                    Tags = note.Tags,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt
                });
            }

            var response = new NotesListResponse
            {
                Notes = noteDtos
            };

            return Result<NotesListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notes for incident {IncidentId}", incidentId);
            return Result<NotesListResponse>.Failure("Failed to retrieve notes");
        }
    }

    /// <summary>
    /// Add manual note to incident
    /// </summary>
    public async Task<Result<IncidentNoteDto>> AddNoteAsync(
        Guid incidentId,
        AddNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<IncidentNoteDto>.Failure("Incident not found");
            }

            // Check authorization
            var canAccess = await CanUserAccessIncidentAsync(userId, incident, cancellationToken);
            if (!canAccess)
            {
                return Result<IncidentNoteDto>.Failure("Access denied");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            // Encrypt note content
            var encryptedContent = await _encryptionService.EncryptAsync(request.Content);

            var note = new IncidentNote
            {
                IncidentId = incidentId,
                Content = encryptedContent,
                Type = IncidentNoteType.Manual,
                IsPrivate = request.IsPrivate,
                AuthorId = userId,
                Tags = request.Tags,
                CreatedAt = DateTime.UtcNow
            };

            _context.IncidentNotes.Add(note);
            await _context.SaveChangesAsync(cancellationToken);

            // Log note creation
            await _auditService.LogActionAsync(
                incidentId,
                userId,
                "NoteAdded",
                $"Manual note added by {user?.SceneName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Manual note added to incident {IncidentId} by user {UserId}",
                incidentId, userId);

            var noteDto = new IncidentNoteDto
            {
                Id = note.Id,
                IncidentId = note.IncidentId,
                Content = request.Content, // Return decrypted
                Type = note.Type,
                IsPrivate = note.IsPrivate,
                AuthorId = note.AuthorId,
                AuthorName = user?.SceneName,
                Tags = note.Tags,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };

            return Result<IncidentNoteDto>.Success(noteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add note to incident {IncidentId}", incidentId);
            return Result<IncidentNoteDto>.Failure("Failed to add note");
        }
    }

    /// <summary>
    /// Update existing manual note
    /// Only the author or admin can update
    /// </summary>
    public async Task<Result<IncidentNoteDto>> UpdateNoteAsync(
        Guid noteId,
        UpdateNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var note = await _context.IncidentNotes
                .Include(n => n.Author)
                .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

            if (note == null)
            {
                return Result<IncidentNoteDto>.Failure("Note not found");
            }

            // Check if user can edit this note
            if (!isAdmin && note.AuthorId != userId)
            {
                return Result<IncidentNoteDto>.Failure("You can only edit your own notes");
            }

            if (note.Type == IncidentNoteType.System)
            {
                return Result<IncidentNoteDto>.Failure("System notes cannot be edited");
            }

            // Encrypt updated content
            var encryptedContent = await _encryptionService.EncryptAsync(request.Content);

            note.Content = encryptedContent;
            note.IsPrivate = request.IsPrivate;
            note.Tags = request.Tags;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Note {NoteId} updated by user {UserId}", noteId, userId);

            var noteDto = new IncidentNoteDto
            {
                Id = note.Id,
                IncidentId = note.IncidentId,
                Content = request.Content, // Return decrypted
                Type = note.Type,
                IsPrivate = note.IsPrivate,
                AuthorId = note.AuthorId,
                AuthorName = note.Author?.SceneName,
                Tags = note.Tags,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };

            return Result<IncidentNoteDto>.Success(noteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update note {NoteId}", noteId);
            return Result<IncidentNoteDto>.Failure("Failed to update note");
        }
    }

    /// <summary>
    /// Delete manual note
    /// Only the author or admin can delete
    /// </summary>
    public async Task<Result<bool>> DeleteNoteAsync(
        Guid noteId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var note = await _context.IncidentNotes
                .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

            if (note == null)
            {
                return Result<bool>.Failure("Note not found");
            }

            // Check if user can delete this note
            if (!isAdmin && note.AuthorId != userId)
            {
                return Result<bool>.Failure("You can only delete your own notes");
            }

            if (note.Type == IncidentNoteType.System)
            {
                return Result<bool>.Failure("System notes cannot be deleted");
            }

            _context.IncidentNotes.Remove(note);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Note {NoteId} deleted by user {UserId}", noteId, userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete note {NoteId}", noteId);
            return Result<bool>.Failure("Failed to delete note");
        }
    }

    #endregion

    #region Phase 5: My Reports

    /// <summary>
    /// Get user's own reports with pagination
    /// Limited view - excludes reference number, coordinator info, notes
    /// </summary>
    public async Task<Result<MyReportsPaginatedResponse>> GetMyReportsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SafetyIncidents
                .Where(i => i.ReporterId == userId)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var incidents = await query
                .OrderByDescending(i => i.ReportedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var reports = incidents.Select(i => new MyReportSummaryDto
            {
                Id = i.Id,
                IncidentDate = i.IncidentDate,
                Location = i.Location,
                Status = i.Status,
                ReportedAt = i.ReportedAt,
                LastUpdatedAt = i.UpdatedAt
            }).ToList();

            var response = new MyReportsPaginatedResponse
            {
                Reports = reports,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            return Result<MyReportsPaginatedResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reports for user {UserId}", userId);
            return Result<MyReportsPaginatedResponse>.Failure("Failed to retrieve your reports");
        }
    }

    /// <summary>
    /// Get user's own report detail
    /// Limited view - excludes reference number, coordinator info, notes, Drive links
    /// </summary>
    public async Task<Result<MyReportDetailDto>> GetMyReportDetailAsync(
        Guid incidentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<MyReportDetailDto>.Failure("Report not found");
            }

            // Verify ownership
            if (incident.ReporterId != userId)
            {
                return Result<MyReportDetailDto>.Failure("You can only view your own reports");
            }

            // Decrypt sensitive fields
            var description = await _encryptionService.DecryptAsync(incident.EncryptedDescription);
            var involvedParties = !string.IsNullOrEmpty(incident.EncryptedInvolvedParties)
                ? await _encryptionService.DecryptAsync(incident.EncryptedInvolvedParties) : null;
            var witnesses = !string.IsNullOrEmpty(incident.EncryptedWitnesses)
                ? await _encryptionService.DecryptAsync(incident.EncryptedWitnesses) : null;

            var detail = new MyReportDetailDto
            {
                Id = incident.Id,
                Status = incident.Status,
                IncidentDate = incident.IncidentDate,
                ReportedAt = incident.ReportedAt,
                LastUpdatedAt = incident.UpdatedAt,
                Location = incident.Location,
                Description = description,
                InvolvedParties = involvedParties,
                Witnesses = witnesses,
                IsAnonymous = incident.IsAnonymous
            };

            return Result<MyReportDetailDto>.Success(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get report detail {IncidentId} for user {UserId}", incidentId, userId);
            return Result<MyReportDetailDto>.Failure("Failed to retrieve report details");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create system-generated note for incident
    /// Used for tracking assignment changes, status updates, etc.
    /// </summary>
    private async Task CreateSystemNoteAsync(
        Guid incidentId,
        string content,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        // Encrypt note content
        var encryptedContent = await _encryptionService.EncryptAsync(content);

        var note = new IncidentNote
        {
            IncidentId = incidentId,
            Content = encryptedContent,
            Type = IncidentNoteType.System,
            IsPrivate = false, // System notes are visible to all authorized users
            AuthorId = null, // System notes have no author
            CreatedAt = DateTime.UtcNow
        };

        _context.IncidentNotes.Add(note);
    }

    /// <summary>
    /// Check if user can access incident
    /// Admin: Can access all incidents
    /// Coordinator: Can access only assigned incidents
    /// User: Can access only their own identified reports
    /// </summary>
    private async Task<bool> CanUserAccessIncidentAsync(
        Guid userId,
        SafetyIncident incident,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return false;
        }

        // Admin can access all incidents
        if (user.Role == "Administrator")
        {
            return true;
        }

        // Coordinator can access assigned incidents
        if (incident.CoordinatorId == userId)
        {
            return true;
        }

        // User can access their own identified reports
        if (incident.ReporterId == userId && !incident.IsAnonymous)
        {
            return true;
        }

        return false;
    }

    #endregion
}
