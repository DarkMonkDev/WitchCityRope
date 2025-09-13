using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request model for submitting review decisions
/// </summary>
public class ReviewDecisionRequest
{
    [Required]
    public int DecisionType { get; set; } // Approve=1, Deny=2, RequestAdditionalInfo=3, etc.

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Reasoning { get; set; } = string.Empty;

    [Range(1, 10)]
    public int? Score { get; set; }

    public bool IsFinalDecision { get; set; }

    [StringLength(1000)]
    public string? AdditionalInfoRequested { get; set; }

    public DateTime? AdditionalInfoDeadline { get; set; }

    public DateTime? ProposedInterviewTime { get; set; }

    [StringLength(1000)]
    public string? InterviewNotes { get; set; }
}

/// <summary>
/// Response after submitting review decision
/// </summary>
public class ReviewDecisionResponse
{
    public Guid DecisionId { get; set; }
    public string DecisionType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string NewApplicationStatus { get; set; } = string.Empty;
    public string ConfirmationMessage { get; set; } = string.Empty;
    public List<string> ActionsTriggered { get; set; } = new();
}

/// <summary>
/// Request for creating application notes
/// </summary>
public class CreateNoteRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int Type { get; set; } // General=1, Concern=2, etc.

    public bool IsPrivate { get; set; } = true;

    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Response after creating note
/// </summary>
public class NoteResponse
{
    public Guid NoteId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ConfirmationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Assignment response
/// </summary>
public class AssignmentResponse
{
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string ConfirmationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Placeholder for analytics models
/// </summary>
public class AnalyticsFilterRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AnalyticsDashboardResponse
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
}

public class ManualNotificationRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public DateTime SentAt { get; set; }
}

public class UpdatePriorityRequest
{
    public int Priority { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PriorityUpdateResponse
{
    public int NewPriority { get; set; }
    public DateTime UpdatedAt { get; set; }
}