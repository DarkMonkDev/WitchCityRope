using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service interface for vetting operations
/// </summary>
public interface IVettingService
{
    /// <summary>
    /// Get paginated list of applications for admin/reviewer dashboard
    /// </summary>
    Task<Result<PagedResult<ApplicationSummaryDto>>> GetApplicationsForReviewAsync(
        ApplicationFilterRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed application information for review
    /// </summary>
    Task<Result<ApplicationDetailResponse>> GetApplicationDetailAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a review decision for an application
    /// </summary>
    Task<Result<ReviewDecisionResponse>> SubmitReviewDecisionAsync(
        Guid applicationId,
        ReviewDecisionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a note to an application
    /// </summary>
    Task<Result<NoteResponse>> AddApplicationNoteAsync(
        Guid applicationId,
        CreateNoteRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user's vetting application status
    /// </summary>
    Task<Result<MyApplicationStatusResponse>> GetMyApplicationStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user's vetting application details
    /// </summary>
    Task<Result<ApplicationDetailResponse>> GetMyApplicationDetailAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a new vetting application (public endpoint)
    /// </summary>
    Task<Result<ApplicationSubmissionResponse>> SubmitApplicationAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get application status by status token (public endpoint)
    /// </summary>
    Task<Result<ApplicationStatusResponse>> GetApplicationStatusByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update application status with validation and audit logging
    /// Only administrators can change status
    /// Validates status transitions according to workflow rules
    /// Creates audit log entry and sends email notification if appropriate
    /// </summary>
    /// <param name="applicationId">ID of the application to update</param>
    /// <param name="newStatus">New status to set</param>
    /// <param name="adminNotes">Notes explaining the status change (required for OnHold and Denied)</param>
    /// <param name="adminUserId">ID of the administrator making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing updated application DTO or error details</returns>
    Task<Result<ApplicationDetailResponse>> UpdateApplicationStatusAsync(
        Guid applicationId,
        VettingStatus newStatus,
        string? adminNotes,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule interview for an application
    /// Moves status to InterviewScheduled and sets interview date
    /// Sends interview scheduled email notification
    /// </summary>
    /// <param name="applicationId">ID of the application</param>
    /// <param name="interviewDate">Date and time of the interview (must be UTC)</param>
    /// <param name="interviewLocation">Location or instructions for the interview</param>
    /// <param name="adminUserId">ID of the administrator scheduling the interview</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<ApplicationDetailResponse>> ScheduleInterviewAsync(
        Guid applicationId,
        DateTime interviewDate,
        string interviewLocation,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Put application on hold with required reason and actions
    /// Sends OnHold email notification with details
    /// </summary>
    /// <param name="applicationId">ID of the application</param>
    /// <param name="reason">Reason for putting on hold (required)</param>
    /// <param name="requiredActions">Actions applicant needs to take (required)</param>
    /// <param name="adminUserId">ID of the administrator</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<ApplicationDetailResponse>> PutOnHoldAsync(
        Guid applicationId,
        string reason,
        string requiredActions,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve vetting application
    /// Must be in InterviewScheduled status
    /// Sends approval email and grants vetted member access
    /// </summary>
    /// <param name="applicationId">ID of the application</param>
    /// <param name="adminUserId">ID of the administrator approving</param>
    /// <param name="adminNotes">Optional notes about the approval decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<ApplicationDetailResponse>> ApproveApplicationAsync(
        Guid applicationId,
        Guid adminUserId,
        string? adminNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deny vetting application
    /// Requires denial reason
    /// Sends denial email notification
    /// </summary>
    /// <param name="applicationId">ID of the application</param>
    /// <param name="reason">Reason for denial (required)</param>
    /// <param name="adminUserId">ID of the administrator denying</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<ApplicationDetailResponse>> DenyApplicationAsync(
        Guid applicationId,
        string reason,
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}

