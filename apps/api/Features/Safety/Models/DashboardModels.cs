namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Dashboard statistics and recent incidents for admin interface
/// </summary>
public class DashboardStatisticsResponse
{
    /// <summary>
    /// Number of unassigned incidents (CoordinatorId IS NULL, Status != Closed)
    /// </summary>
    public int UnassignedCount { get; set; }

    /// <summary>
    /// Whether there are old unassigned incidents (> 7 days)
    /// </summary>
    public bool HasOldUnassigned { get; set; }

    /// <summary>
    /// Recent incidents (last 5, excluding Closed)
    /// </summary>
    public List<IncidentSummaryDto> RecentIncidents { get; set; } = new();
}
