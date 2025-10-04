using WitchCityRope.Api.Features.Vetting.Models;
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
}

