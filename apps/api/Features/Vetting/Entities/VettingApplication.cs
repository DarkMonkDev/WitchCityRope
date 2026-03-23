using System.ComponentModel.DataAnnotations;
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
        RemindersSentCount = 0;
    }

    /// <summary>
    /// Soft-deletes this application. The application and its audit logs remain in the database
    /// but are excluded from all queries. A deleted application's email can be reused for resubmission.
    /// Also resets the associated user's VettingStatus so they can reapply.
    /// </summary>
    public void SoftDelete(Guid adminUserId)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Application is already deleted");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = adminUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    // Legacy property for backwards compatibility
    public Guid ApplicantId => UserId ?? Guid.Empty;

    // Primary Key
    public Guid Id { get; set; }

    // Public application tracking (for anonymous submissions)
    [MaxLength(25)]
    public string ApplicationNumber { get; set; } = string.Empty; // VET-YYYYMMDD-XXXXXXXX (21 chars)

    [MaxLength(50)]
    public string StatusToken { get; set; } = string.Empty; // For public status lookup

    // Applicant Information (from wireframe design)
    public Guid? UserId { get; set; }  // FK to User (nullable for anonymous submissions)
    public string SceneName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }

    // Experience & Knowledge (from CreateApplicationRequest)
    public string? ExperienceDescription { get; set; }

    // Community Understanding (from CreateApplicationRequest)
    public string? WhyJoinCommunity { get; set; }
    public string? HowDidYouHearAboutUs { get; set; }
    public bool AgreesToGuidelines { get; set; }

    // Terms & Consent (from CreateApplicationRequest)
    public bool AgreesToTerms { get; set; }
    public bool ConsentToContact { get; set; }

    // Application Status and Management
    /// <summary>
    /// Workflow status tracking the application review process.
    /// This is automatically synced to User.VettingStatus on ALL status changes.
    /// User.VettingStatus is the source of truth for permissions and UI display.
    /// </summary>
    public VettingStatus WorkflowStatus { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Additional tracking fields
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? LastReviewedAt { get; set; } // Last status change
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }

    // Reminder tracking - counts how many interview reminder emails have been sent
    public int RemindersSentCount { get; set; }
    public DateTime? LastReminderSentAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft delete fields - applications are soft-deleted rather than hard-deleted
    // to preserve audit trail and data integrity. Soft-deleted applications are
    // excluded from all queries (admin list, detail views, public status checks).
    // Admins can delete applications of any status. Deleted apps allow email reuse for resubmission.
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation Properties
    public ApplicationUser? User { get; set; } // Nullable for anonymous applications
    public ApplicationUser? DeletedByUser { get; set; }
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
