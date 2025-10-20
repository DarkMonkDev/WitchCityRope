using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Summary of user's own incident reports (limited fields for privacy)
/// </summary>
public class MyReportSummaryDto
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// When incident occurred
    /// </summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// Location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public IncidentStatus Status { get; set; }

    /// <summary>
    /// When report was submitted
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    // NOTE: NO reference number, NO coordinator info (privacy restriction)
}

/// <summary>
/// Detail view of user's own incident report (limited fields for privacy)
/// </summary>
public class MyReportDetailDto
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public IncidentStatus Status { get; set; }

    /// <summary>
    /// When incident occurred
    /// </summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// When report was submitted
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Description (what user submitted, decrypted)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Involved parties (what user submitted, decrypted)
    /// </summary>
    public string? InvolvedParties { get; set; }

    /// <summary>
    /// Witnesses (what user submitted, decrypted)
    /// </summary>
    public string? Witnesses { get; set; }

    /// <summary>
    /// Whether this is anonymous (should always be false for this endpoint)
    /// </summary>
    public bool IsAnonymous { get; set; }

    // NOTE: Excludes referenceNumber, coordinatorId, notes, Drive links
}

/// <summary>
/// Paginated response for user's reports
/// </summary>
public class MyReportsPaginatedResponse
{
    /// <summary>
    /// Report summaries on current page
    /// </summary>
    public List<MyReportSummaryDto> Reports { get; set; } = new();

    /// <summary>
    /// Total count of user's reports
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}
