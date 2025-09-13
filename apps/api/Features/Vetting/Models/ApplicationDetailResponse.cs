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
    public string FullName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string? Pronouns { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    
    // Experience Information
    public string ExperienceLevel { get; set; } = string.Empty;
    public int YearsExperience { get; set; }
    public string ExperienceDescription { get; set; } = string.Empty;
    public string SafetyKnowledge { get; set; } = string.Empty;
    public string ConsentUnderstanding { get; set; } = string.Empty;
    
    // Community Information
    public string WhyJoinCommunity { get; set; } = string.Empty;
    public List<string> SkillsInterests { get; set; } = new();
    public string ExpectationsGoals { get; set; } = string.Empty;
    public bool AgreesToGuidelines { get; set; }
    
    // Privacy Settings
    public bool IsAnonymous { get; set; }
    public bool AgreesToTerms { get; set; }
    public bool ConsentToContact { get; set; }
    
    // Review Information
    public string? AssignedReviewerName { get; set; }
    public DateTime? ReviewStartedAt { get; set; }
    public int Priority { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }
    
    // References
    public List<ReferenceDetailDto> References { get; set; } = new();
    
    // Notes and Decisions
    public List<ApplicationNoteDto> Notes { get; set; } = new();
    public List<ReviewDecisionDto> Decisions { get; set; } = new();
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