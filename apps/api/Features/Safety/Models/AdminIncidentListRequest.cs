using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request parameters for admin incident list with filtering and pagination
/// </summary>
public class AdminIncidentListRequest
{
    /// <summary>
    /// Search text (searches reference number, location, description)
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status (multiple values supported via comma-separated string)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Start date for date range filter (ISO 8601)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for date range filter (ISO 8601)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by assigned coordinator ID
    /// </summary>
    public Guid? AssignedTo { get; set; }

    /// <summary>
    /// Show only unassigned incidents (coordinatorId IS NULL)
    /// </summary>
    public bool? Unassigned { get; set; }

    /// <summary>
    /// Filter by incident type (multiple values supported via comma-separated string)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field (default: reportedAt)
    /// </summary>
    public string SortBy { get; set; } = "reportedAt";

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}

/// <summary>
/// Paginated response for incident list
/// </summary>
public class PaginatedIncidentListResponse
{
    /// <summary>
    /// Incident items on current page
    /// </summary>
    public List<IncidentSummaryDto> Items { get; set; } = new();

    /// <summary>
    /// Total count of items matching filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Incident summary for list views
/// </summary>
public class IncidentSummaryDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public IncidentType Type { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Truncated/decrypted
    public bool IsAnonymous { get; set; }
    public Guid? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public Guid? CoordinatorId { get; set; }
    public string? CoordinatorName { get; set; }
    public string? InvolvedParties { get; set; }
    public string? Witnesses { get; set; }
    public string? GoogleDriveFolderUrl { get; set; }
    public string? GoogleDriveFinalReportUrl { get; set; }
    public int NoteCount { get; set; }
}
