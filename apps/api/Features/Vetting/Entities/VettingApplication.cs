using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Primary entity for member vetting applications with comprehensive audit trail support
/// All PII fields are encrypted using AES-256-GCM encryption
/// </summary>
public class VettingApplication
{
    public VettingApplication()
    {
        Id = Guid.NewGuid();
        Status = ApplicationStatus.Draft;
        IsAnonymous = false;
        Priority = ApplicationPriority.Standard;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        StatusToken = GenerateSecureToken();
        
        References = new List<VettingReference>();
        Notes = new List<VettingApplicationNote>();
        Decisions = new List<VettingDecision>();
        AuditLogs = new List<VettingApplicationAuditLog>();
        Notifications = new List<VettingNotification>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Identification
    public string ApplicationNumber { get; set; } = string.Empty; // VET-YYYYMMDD-NNNN
    public ApplicationStatus Status { get; set; }
    public string StatusToken { get; set; } = string.Empty; // Secure token for public status checks

    // Applicant Information (Encrypted)
    public string EncryptedFullName { get; set; } = string.Empty;
    public string EncryptedSceneName { get; set; } = string.Empty;
    public string? EncryptedPronouns { get; set; }
    public string EncryptedEmail { get; set; } = string.Empty;
    public string? EncryptedPhone { get; set; }

    // Experience Information (Encrypted)
    public ExperienceLevel ExperienceLevel { get; set; }
    public int YearsExperience { get; set; }
    public string EncryptedExperienceDescription { get; set; } = string.Empty;
    public string EncryptedSafetyKnowledge { get; set; } = string.Empty;
    public string EncryptedConsentUnderstanding { get; set; } = string.Empty;

    // Community Information
    public string EncryptedWhyJoinCommunity { get; set; } = string.Empty;
    public string SkillsInterests { get; set; } = string.Empty; // JSON array of tags
    public string EncryptedExpectationsGoals { get; set; } = string.Empty;
    public bool AgreesToGuidelines { get; set; }

    // Privacy Settings
    public bool IsAnonymous { get; set; }
    public bool AgreesToTerms { get; set; }
    public bool ConsentToContact { get; set; }

    // Workflow Management
    public Guid? AssignedReviewerId { get; set; }
    public ApplicationPriority Priority { get; set; } = ApplicationPriority.Standard;
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }

    // Data Retention
    public DateTime? DeletedAt { get; set; } // Soft delete
    public DateTime? ExpiresAt { get; set; } // For draft cleanup

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Foreign Keys
    public Guid? ApplicantId { get; set; } // Links to ApplicationUser if registered

    // Navigation Properties
    public ApplicationUser? Applicant { get; set; }
    public VettingReviewer? AssignedReviewer { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    
    public ICollection<VettingReference> References { get; set; }
    public ICollection<VettingApplicationNote> Notes { get; set; }
    public ICollection<VettingDecision> Decisions { get; set; }
    public ICollection<VettingApplicationAuditLog> AuditLogs { get; set; }
    public ICollection<VettingNotification> Notifications { get; set; }

    // Helper Methods
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256-bit token
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

/// <summary>
/// Application workflow status
/// </summary>
public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    PendingReferences = 4,
    PendingInterview = 5,
    PendingAdditionalInfo = 6,
    Approved = 7,
    Denied = 8,
    Withdrawn = 9,
    Expired = 10
}

/// <summary>
/// Applicant experience level
/// </summary>
public enum ExperienceLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

/// <summary>
/// Application priority for review queue management
/// </summary>
public enum ApplicationPriority
{
    Standard = 1,
    High = 2,
    Urgent = 3
}