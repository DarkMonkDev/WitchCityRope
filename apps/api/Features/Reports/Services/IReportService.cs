using WitchCityRope.Api.Features.Reports.Models;

namespace WitchCityRope.Api.Features.Reports.Services;

/// <summary>
/// Service interface for admin report data aggregation.
/// Provides user statistics and daily transaction summaries for analytics dashboards.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Get current user counts broken down by verification and vetting status.
    /// </summary>
    Task<UserSummaryResponse> GetUserSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get daily aggregated transaction data for the specified date range.
    /// Returns zero-filled days so the frontend chart has a continuous X-axis.
    /// </summary>
    Task<DailyTransactionsResponse> GetDailyTransactionsAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken);
}
