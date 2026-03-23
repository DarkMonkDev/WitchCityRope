using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Full application details for reviewer view
/// Contains decrypted PII and complete application data
/// </summary>
public class ApplicationDetailResponse
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    // Applicant Information (Decrypted)
    public string SceneName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Pronouns { get; set; }
    public string Email { get; set; } = string.Empty;

    // Additional Applicant Information from Simplified Form
    public string? FetLifeHandle { get; set; }
    public string? OtherNames { get; set; }

    // Experience Information
    public string ExperienceDescription { get; set; } = string.Empty;

    // Community Information
    public string WhyJoinCommunity { get; set; } = string.Empty;
    public string? HowDidYouHearAboutUs { get; set; }
    public bool AgreesToGuidelines { get; set; }

    // Review Information
    public string? AssignedReviewerName { get; set; }
    public DateTime? ReviewStartedAt { get; set; }
    public int Priority { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }

    // Reminder tracking - how many interview reminders have been sent to this applicant
    public int RemindersSentCount { get; set; }
    public DateTime? LastReminderSentAt { get; set; }

    // References
    public List<ReferenceDetailDto> References { get; set; } = new();

    // Notes and Decisions
    public List<ApplicationNoteDto> Notes { get; set; } = new();
    public List<ReviewDecisionDto> Decisions { get; set; } = new();

    // Workflow History
    public List<WorkflowHistoryDto> WorkflowHistory { get; set; } = new();

    // Additional fields for simplified responses
    public List<string> Tags { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    public Guid ApplicationId { get; set; }
}

/// <summary>
/// Reference details with response information
/// </summary>
public class ReferenceDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ContactedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? FormExpiresAt { get; set; }

    // Response data (if available)
    public ReferenceResponseDto? Response { get; set; }
}

/// <summary>
/// Reference response information
/// </summary>
public class ReferenceResponseDto
{
    public string RelationshipDuration { get; set; } = string.Empty;
    public string ExperienceAssessment { get; set; } = string.Empty;
    public string? SafetyConcerns { get; set; }
    public string CommunityReadiness { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? AdditionalComments { get; set; }
    public DateTime RespondedAt { get; set; }
}

/// <summary>
/// Application note for reviewer collaboration
/// </summary>
public class ApplicationNoteDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Review decision information
/// </summary>
public class ReviewDecisionDto
{
    public Guid Id { get; set; }
    public string DecisionType { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public int? Score { get; set; }
    public bool IsFinalDecision { get; set; }
    public string? AdditionalInfoRequested { get; set; }
    public DateTime? AdditionalInfoDeadline { get; set; }
    public DateTime? ProposedInterviewTime { get; set; }
    public string? InterviewNotes { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Workflow history entry showing application status changes
/// </summary>
public class WorkflowHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for admin update of applicant information on a vetting application.
/// Updates the VettingApplication entity fields only (not the User entity).
/// Used by the inline edit feature on the admin vetting detail page.
/// </summary>
public class UpdateApplicationApplicantInfoRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Scene name must be between 3 and 100 characters")]
    public string SceneName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string? FirstName { get; set; }

    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string? LastName { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Pronouns cannot exceed 50 characters")]
    public string? Pronouns { get; set; }

    [StringLength(100, ErrorMessage = "FetLife handle cannot exceed 100 characters")]
    public string? FetLifeHandle { get; set; }

    [StringLength(500, ErrorMessage = "Other names cannot exceed 500 characters")]
    public string? OtherNames { get; set; }
}