using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Primary vetting service interface for application management
/// Follows established patterns from SafetyService and CheckInService
/// </summary>
public interface IVettingService
{
    /// <summary>
    /// Submit a new vetting application with PII encryption and validation
    /// Creates application with references and sends confirmation email
    /// </summary>
    Task<Result<ApplicationSubmissionResponse>> SubmitApplicationAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get application status using secure token for public access
    /// No authentication required, returns limited status information
    /// </summary>
    Task<Result<ApplicationStatusResponse>> GetApplicationStatusAsync(
        string statusToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get applications for reviewer dashboard with filtering and pagination
    /// Requires VettingReviewer role authorization
    /// </summary>
    Task<Result<PagedResult<ApplicationSummaryDto>>> GetApplicationsForReviewAsync(
        Guid reviewerId,
        ApplicationFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full application details for reviewer with decrypted PII
    /// Requires VettingReviewer role and appropriate assignment
    /// </summary>
    Task<Result<ApplicationDetailResponse>> GetApplicationDetailAsync(
        Guid applicationId,
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit review decision (approve, deny, request info, etc.)
    /// Updates application status and triggers notifications
    /// </summary>
    Task<Result<ReviewDecisionResponse>> SubmitReviewDecisionAsync(
        Guid applicationId,
        ReviewDecisionRequest request,
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign application to reviewer (auto or manual assignment)
    /// Updates workload tracking and audit logs
    /// </summary>
    Task<Result<AssignmentResponse>> AssignApplicationAsync(
        Guid applicationId,
        Guid reviewerId,
        Guid assignedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add note to application for reviewer collaboration
    /// Supports private reviewer notes and public applicant communications
    /// </summary>
    Task<Result<NoteResponse>> AddApplicationNoteAsync(
        Guid applicationId,
        CreateNoteRequest request,
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get analytics dashboard data for administrators
    /// Requires VettingAdmin role authorization
    /// </summary>
    Task<Result<AnalyticsDashboardResponse>> GetAnalyticsDashboardAsync(
        AnalyticsFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send manual notification to applicant or reference
    /// Requires VettingAdmin role for custom communications
    /// </summary>
    Task<Result<NotificationResponse>> SendManualNotificationAsync(
        Guid applicationId,
        ManualNotificationRequest request,
        Guid sentByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update application priority for urgent cases
    /// Requires VettingAdmin role authorization
    /// </summary>
    Task<Result<PriorityUpdateResponse>> UpdateApplicationPriorityAsync(
        Guid applicationId,
        UpdatePriorityRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);
}