namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Tracks all review decisions and their rationale
/// Maintains complete audit trail of decision-making process
/// </summary>
public class VettingDecision
{
    public VettingDecision()
    {
        CreatedAt = DateTime.UtcNow;
        
        AuditLogs = new List<VettingDecisionAuditLog>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }

    // Decision Details
    public DecisionType DecisionType { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public int? Score { get; set; } // 1-10 scale if using scoring rubric
    public bool IsFinalDecision { get; set; }

    // Additional Info Request Details
    public string? AdditionalInfoRequested { get; set; }
    public DateTime? AdditionalInfoDeadline { get; set; }

    // Interview Details
    public DateTime? ProposedInterviewTime { get; set; }
    public string? InterviewNotes { get; set; }

    // Decision Metadata
    public DateTime CreatedAt { get; set; }
    public string? DecisionIpAddress { get; set; }
    public string? DecisionUserAgent { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReviewer Reviewer { get; set; } = null!;
    public ICollection<VettingDecisionAuditLog> AuditLogs { get; set; }
}

/// <summary>
/// Types of decisions that can be made during the review process
/// </summary>
public enum DecisionType
{
    Approve = 1,
    Deny = 2,
    RequestAdditionalInfo = 3,
    ScheduleInterview = 4,
    Reassign = 5,
    Escalate = 6
}