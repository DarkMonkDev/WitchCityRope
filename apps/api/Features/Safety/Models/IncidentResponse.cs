using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Response after successful incident submission
/// </summary>
public class SubmissionResponse
{
    /// <summary>
    /// Generated reference number for tracking
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// URL for tracking incident status
    /// </summary>
    public string TrackingUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the incident was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Response for anonymous incident status tracking
/// </summary>
public class IncidentStatusResponse
{
    /// <summary>
    /// Incident reference number
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Whether reporter can provide additional information
    /// </summary>
    public bool CanProvideMoreInfo { get; set; }
}

/// <summary>
/// Complete incident details for safety team
/// </summary>
public class IncidentResponse
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Decrypted for safety team
    public string? InvolvedParties { get; set; } // Decrypted for safety team
    public string? Witnesses { get; set; } // Decrypted for safety team
    public string? ContactEmail { get; set; } // Decrypted for safety team
    public string? ContactPhone { get; set; } // Decrypted for safety team
    public bool IsAnonymous { get; set; }
    public bool RequestFollowUp { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public List<AuditLogDto> AuditTrail { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary for incident listings
/// </summary>
public class IncidentSummaryResponse
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
}

/// <summary>
/// Safety dashboard data for admin interface
/// </summary>
public class AdminDashboardResponse
{
    public SafetyStatistics Statistics { get; set; } = new();
    public List<IncidentSummaryResponse> RecentIncidents { get; set; } = new();
    public List<ActionItem> PendingActions { get; set; } = new();
}

public class SafetyStatistics
{
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ThisMonth { get; set; }
}

public class ActionItem
{
    public Guid IncidentId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ActionNeeded { get; set; } = string.Empty;
    public IncidentSeverity Priority { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Audit trail entry
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}