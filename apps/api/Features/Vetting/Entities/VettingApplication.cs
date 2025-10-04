using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Simplified vetting application entity aligned with design specifications
/// Represents a member application for vetting with basic audit trail support
/// </summary>
public class VettingApplication
{
    public VettingApplication()
    {
        Status = VettingStatus.Draft;
        SubmittedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AuditLogs = new List<VettingAuditLog>();
    }

    // Legacy property for backwards compatibility
    public Guid ApplicantId => UserId ?? Guid.Empty;

    // Primary Key
    public Guid Id { get; set; }

    // Public application tracking (for anonymous submissions)
    public string ApplicationNumber { get; set; } = string.Empty; // VET-YYYYMMDD-XXXXX
    public string StatusToken { get; set; } = string.Empty; // For public status lookup

    // Applicant Information (from wireframe design)
    public Guid? UserId { get; set; }  // FK to User (nullable for anonymous submissions)
    public string SceneName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? FullName { get; set; } // Full legal name (may differ from RealName)
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }
    public string AboutYourself { get; set; } = string.Empty;
    public string HowFoundUs { get; set; } = string.Empty;

    // Experience & Knowledge (from CreateApplicationRequest)
    public int ExperienceLevel { get; set; } // 1=Beginner, 2=Intermediate, 3=Advanced, 4=Expert
    public int YearsExperience { get; set; }
    public string? ExperienceDescription { get; set; }
    public string? SafetyKnowledge { get; set; }
    public string? ConsentUnderstanding { get; set; }

    // Community Understanding (from CreateApplicationRequest)
    public string? WhyJoinCommunity { get; set; }
    public string? SkillsInterests { get; set; } // Comma-separated list
    public string? ExpectationsGoals { get; set; }
    public bool AgreesToGuidelines { get; set; }

    // References (JSON serialized)
    public string? References { get; set; }

    // Terms & Consent (from CreateApplicationRequest)
    public bool AgreesToTerms { get; set; }
    public bool IsAnonymous { get; set; }
    public bool ConsentToContact { get; set; }

    // Application Status and Management
    public VettingStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? AdminNotes { get; set; }  // Simple text field for admin notes

    // Additional tracking fields
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? LastReviewedAt { get; set; } // Last status change
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ApplicationUser? User { get; set; } // Nullable for anonymous applications
    public ICollection<VettingAuditLog> AuditLogs { get; set; }
}

/// <summary>
/// Simplified vetting status enum aligned with wireframe requirements
/// </summary>
public enum VettingStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    InterviewApproved = 3,
    PendingInterview = 4,
    InterviewScheduled = 5,
    OnHold = 6,
    Approved = 7,
    Denied = 8,
    Withdrawn = 9
}