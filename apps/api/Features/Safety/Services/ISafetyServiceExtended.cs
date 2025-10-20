using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Extended safety incident management service for comprehensive incident reporting system
/// Includes all 12 endpoints for public submission, admin dashboard, coordinator workflow, notes, and user reports
/// </summary>
public interface ISafetyServiceExtended : ISafetyService
{
    // PHASE 2: Admin Dashboard

    /// <summary>
    /// Get paginated list of incidents with filtering and sorting
    /// </summary>
    Task<Result<PaginatedIncidentListResponse>> GetIncidentsAsync(
        AdminIncidentListRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboard statistics (unassigned count, old unassigned flag, recent incidents)
    /// </summary>
    Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users for coordinator assignment dropdown
    /// </summary>
    Task<Result<IEnumerable<UserCoordinatorDto>>> GetAllUsersForAssignmentAsync(
        CancellationToken cancellationToken = default);

    // PHASE 3: Incident Detail & Management

    /// <summary>
    /// Assign or unassign coordinator to incident
    /// </summary>
    Task<Result<IncidentSummaryDto>> AssignCoordinatorAsync(
        Guid incidentId,
        AssignCoordinatorRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update incident status with optional reason/note
    /// </summary>
    Task<Result<StatusUpdateResponse>> UpdateStatusAsync(
        Guid incidentId,
        UpdateStatusRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update Google Drive links
    /// </summary>
    Task<Result<GoogleDriveUpdateResponse>> UpdateGoogleDriveLinksAsync(
        Guid incidentId,
        UpdateGoogleDriveRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    // PHASE 4: Notes System

    /// <summary>
    /// Get all notes for an incident
    /// </summary>
    Task<Result<NotesListResponse>> GetNotesAsync(
        Guid incidentId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add manual note to incident
    /// </summary>
    Task<Result<IncidentNoteDto>> AddNoteAsync(
        Guid incidentId,
        AddNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing manual note
    /// </summary>
    Task<Result<IncidentNoteDto>> UpdateNoteAsync(
        Guid noteId,
        UpdateNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete manual note
    /// </summary>
    Task<Result<bool>> DeleteNoteAsync(
        Guid noteId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    // PHASE 5: My Reports

    /// <summary>
    /// Get user's own reports with pagination (limited view)
    /// </summary>
    Task<Result<MyReportsPaginatedResponse>> GetMyReportsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's own report detail (limited view)
    /// </summary>
    Task<Result<MyReportDetailDto>> GetMyReportDetailAsync(
        Guid incidentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
