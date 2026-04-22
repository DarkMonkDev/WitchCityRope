namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Application summary for reviewer dashboard listings
/// Contains essential information without full PII details
/// </summary>
public class ApplicationSummaryDto
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    // Applicant information (masked for privacy)
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FetLifeHandle { get; set; }

    // Review information
    public string? AssignedReviewerName { get; set; }
    public DateTime? ReviewStartedAt { get; set; }
    public int Priority { get; set; }
    public int DaysInCurrentStatus { get; set; }

    // Reference status
    public ApplicationReferenceStatus ReferenceStatus { get; set; } = new();

    // Recent activity indicators
    public bool HasRecentNotes { get; set; }
    public bool HasPendingActions { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }

    // Number of interview reminder emails sent for this application.
    // Surfaced in the admin grid so reviewers can see at a glance who has
    // already been pinged. The detail DTO exposes the same field plus the
    // LastReminderSentAt timestamp; the grid only needs the count.
    public int RemindersSentCount { get; set; }
}

/// <summary>
/// Reference status summary for application listing
/// </summary>
public class ApplicationReferenceStatus
{
    public int TotalReferences { get; set; }
    public int ContactedReferences { get; set; }
    public int RespondedReferences { get; set; }
    public bool AllReferencesComplete { get; set; }
    public DateTime? OldestPendingReferenceDate { get; set; }
}