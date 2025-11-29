namespace VettedMemberImport.Entities;

public class VettingApplication
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string StatusToken { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string RealName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }
    public int ExperienceLevel { get; set; }
    public int YearsExperience { get; set; }
    public string? ExperienceDescription { get; set; }
    public string? WhyJoinCommunity { get; set; }
    public string? HowDidYouHearAboutUs { get; set; }
    public bool AgreesToGuidelines { get; set; }
    public bool AgreesToTerms { get; set; }
    public bool ConsentToContact { get; set; }
    public int WorkflowStatus { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
