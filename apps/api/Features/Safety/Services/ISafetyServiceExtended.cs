using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Extended safety incident service implementing comprehensive incident management
/// Inherits base functionality from ISafetyService and adds admin dashboard, coordinator workflow, and notes
/// </summary>
public interface ISafetyServiceExtended : ISafetyService
{
    #region Phase 2: Admin Dashboard

    /// <summary>
    /// Get paginated list of incidents with filtering and sorting
    /// Authorization: Admin can see all, Coordinator can only see assigned incidents
    /// </summary>
    Task<Result<PaginatedIncidentListResponse>> GetIncidentsAsync(
        AdminIncidentListRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboard statistics for admin interface
    /// Includes unassigned count, old unassigned flag, and recent incidents
    /// </summary>
    Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users for coordinator assignment dropdown
    /// Returns users with their active incident counts
    /// </summary>
    Task<Result<IEnumerable<UserCoordinatorDto>>> GetAllUsersForAssignmentAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Phase 3: Incident Detail & Management

    /// <summary>
    /// Assign or unassign coordinator to incident
    /// Only admins can assign coordinators
    /// Creates system note on assignment/unassignment
    /// </summary>
    Task<Result<IncidentSummaryDto>> AssignCoordinatorAsync(
        Guid incidentId,
        AssignCoordinatorRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update incident status
    /// Creates system note for status change
    /// Authorization: Admin or assigned coordinator
    /// </summary>
    Task<Result<StatusUpdateResponse>> UpdateStatusAsync(
        Guid incidentId,
        UpdateStatusRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update Google Drive links for incident
    /// Creates system note on update
    /// Authorization: Admin or assigned coordinator
    /// </summary>
    Task<Result<GoogleDriveUpdateResponse>> UpdateGoogleDriveLinksAsync(
        Guid incidentId,
        UpdateGoogleDriveRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update involved parties and witnesses for incident
    /// Creates system note on update
    /// Authorization: Admin or assigned coordinator
    /// </summary>
    Task<Result<UpdatePeopleResponse>> UpdatePeopleAsync(
        Guid incidentId,
        UpdatePeopleRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    #endregion

    #region Phase 4: Notes System

    /// <summary>
    /// Get all notes for an incident
    /// Authorization: Admin or assigned coordinator
    /// </summary>
    Task<Result<NotesListResponse>> GetNotesAsync(
        Guid incidentId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add manual note to incident
    /// Authorization: Admin or assigned coordinator
    /// </summary>
    Task<Result<IncidentNoteDto>> AddNoteAsync(
        Guid incidentId,
        AddNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing manual note
    /// Only the author or admin can update
    /// System notes cannot be edited
    /// </summary>
    Task<Result<IncidentNoteDto>> UpdateNoteAsync(
        Guid noteId,
        UpdateNoteRequest request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete manual note
    /// Only the author or admin can delete
    /// System notes cannot be deleted
    /// </summary>
    Task<Result<bool>> DeleteNoteAsync(
        Guid noteId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    #endregion

    #region Phase 5: My Reports

    /// <summary>
    /// Get user's own reports with pagination
    /// Limited view - excludes reference number, coordinator info, notes
    /// </summary>
    Task<Result<MyReportsPaginatedResponse>> GetMyReportsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's own report detail
    /// Limited view - excludes reference number, coordinator info, notes, Drive links
    /// </summary>
    Task<Result<MyReportDetailDto>> GetMyReportDetailAsync(
        Guid incidentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion
}
