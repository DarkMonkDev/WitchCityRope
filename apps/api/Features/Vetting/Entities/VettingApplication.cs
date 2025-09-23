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
    public Guid ApplicantId => UserId;

    // Primary Key
    public Guid Id { get; set; }

    // Applicant Information (from wireframe design)
    public Guid UserId { get; set; }  // FK to User
    public string SceneName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }
    public string AboutYourself { get; set; } = string.Empty;
    public string HowFoundUs { get; set; } = string.Empty;

    // Application Status and Management
    public VettingStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? AdminNotes { get; set; }  // Simple text field for admin notes

    // Additional tracking fields
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<VettingAuditLog> AuditLogs { get; set; }
}

/// <summary>
/// Simplified vetting status enum aligned with wireframe requirements
/// </summary>
public enum VettingStatus
{
    Draft = 1,
    UnderReview = 2,
    InterviewApproved = 3,
    PendingInterview = 4,
    OnHold = 5,
    Approved = 6,
    Denied = 7
}