using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Main safety incident management service
/// Follows simplified vertical slice pattern with direct Entity Framework access
/// </summary>
public interface ISafetyService
{
    /// <summary>
    /// Submit new safety incident report (anonymous or identified)
    /// </summary>
    Task<Result<SubmissionResponse>> SubmitIncidentAsync(
        CreateIncidentRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get incident status for anonymous tracking
    /// </summary>
    Task<Result<IncidentStatusResponse>> GetIncidentStatusAsync(
        string referenceNumber, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed incident information for safety team
    /// </summary>
    Task<Result<IncidentResponse>> GetIncidentDetailAsync(
        Guid incidentId, 
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get safety dashboard data for admin interface
    /// </summary>
    Task<Result<AdminDashboardResponse>> GetDashboardDataAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's own incident reports
    /// </summary>
    Task<Result<IEnumerable<IncidentSummaryResponse>>> GetUserReportsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}