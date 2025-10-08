namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Public application status response for status token lookup
/// Provides limited information for applicant privacy
/// </summary>
public class ApplicationStatusResponse
{
    public string ApplicationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string StatusDescription { get; set; } = string.Empty;

    // Timeline information
    public DateTime? LastUpdateAt { get; set; }
    public int? EstimatedDaysRemaining { get; set; }

    // Progress indicators
    public ApplicationProgressSummary Progress { get; set; } = new();

    // Communication history (limited)
    public List<StatusUpdateSummary> RecentUpdates { get; set; } = new();
}

/// <summary>
/// Application progress breakdown
/// </summary>
public class ApplicationProgressSummary
{
    public bool ApplicationSubmitted { get; set; }
    public bool ReferencesContacted { get; set; }
    public bool ReferencesReceived { get; set; }
    public bool UnderReview { get; set; }
    public bool InterviewScheduled { get; set; }
    public bool DecisionMade { get; set; }

    // Progress percentage (0-100)
    public int ProgressPercentage { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
}

/// <summary>
/// Status update summary for applicant view
/// </summary>
public class StatusUpdateSummary
{
    public DateTime UpdatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "StatusChange", "ReferenceUpdate", "Communication"
}