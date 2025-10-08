using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Simplified vetting application entity aligned with design specifications
/// Represents a member application for vetting with basic audit trail support
///
/// VETTING WORKFLOW (Updated for Calendly External Scheduling):
/// 1. Application submitted → UnderReview (0) - Initial submission under review
/// 2. Team reviews application → InterviewApproved (1) - Approved for interview (Calendly link sent)
/// 3. Interview completed → InterviewCompleted (2) - Interview has been completed (marked manually)
/// 4. Interview completed automatically triggers → FinalReview (3) - Post-interview review before decision
/// 5. Final decision → Approved (4), Denied (5), or OnHold (6)
/// 6. User can withdraw anytime → Withdrawn (7)
///
/// NOTE: Interviews are scheduled externally via Calendly. The system tracks completion, not scheduling.
/// Terminal states: Approved, Denied, Withdrawn (no further transitions allowed)
/// Hold state: OnHold can return to UnderReview or InterviewApproved
/// </summary>
public class VettingApplication
{
    public VettingApplication()
    {
        WorkflowStatus = VettingStatus.UnderReview;
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
    /// <summary>
    /// Workflow status tracking the application review process.
    /// When this reaches a terminal state (Approved/Denied), it syncs to User.VettingStatus.
    /// User.VettingStatus is the source of truth for permissions.
    /// </summary>
    public VettingStatus WorkflowStatus { get; set; }
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
/// Updated for Calendly external interview scheduling workflow
/// Interview completion automatically moves to FinalReview (no intermediate InterviewCompleted state)
/// </summary>
public enum VettingStatus
{
    UnderReview = 0,        // Application submitted and under initial review
    InterviewApproved = 1,  // Approved for interview - Calendly link sent to applicant
    FinalReview = 2,        // Post-interview final review before decision (after interview completed)
    Approved = 3,           // Final decision: Approved
    Denied = 4,             // Final decision: Denied
    OnHold = 5,             // Final decision: On hold
    Withdrawn = 6           // Applicant withdrew their application
}