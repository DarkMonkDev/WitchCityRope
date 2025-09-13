using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Safety.Entities;

/// <summary>
/// Safety incident entity for PostgreSQL database
/// Supports anonymous reporting with encrypted sensitive data
/// </summary>
public class SafetyIncident
{
    public SafetyIncident()
    {
        Id = Guid.NewGuid();
        ReferenceNumber = string.Empty;
        Location = string.Empty;
        EncryptedDescription = string.Empty;
        ReportedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = IncidentStatus.New;
        Severity = IncidentSeverity.Medium;
        IsAnonymous = false;
        RequestFollowUp = false;
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique reference number for tracking (SAF-YYYYMMDD-NNNN)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ReferenceNumber { get; set; }

    /// <summary>
    /// Reporter user ID - NULL for anonymous reports
    /// </summary>
    public Guid? ReporterId { get; set; }

    /// <summary>
    /// Incident severity level
    /// </summary>
    [Required]
    public IncidentSeverity Severity { get; set; }

    /// <summary>
    /// When the incident occurred (UTC)
    /// </summary>
    [Required]
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// When the incident was reported (UTC)
    /// </summary>
    [Required]
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Location where incident occurred
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Location { get; set; }

    /// <summary>
    /// Encrypted incident description
    /// </summary>
    [Required]
    public string EncryptedDescription { get; set; }

    /// <summary>
    /// Encrypted involved parties information
    /// </summary>
    public string? EncryptedInvolvedParties { get; set; }

    /// <summary>
    /// Encrypted witness information
    /// </summary>
    public string? EncryptedWitnesses { get; set; }

    /// <summary>
    /// Encrypted contact email if provided
    /// </summary>
    public string? EncryptedContactEmail { get; set; }

    /// <summary>
    /// Encrypted contact phone if provided
    /// </summary>
    public string? EncryptedContactPhone { get; set; }

    /// <summary>
    /// Whether this is an anonymous report
    /// </summary>
    [Required]
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// Whether reporter requested follow-up
    /// </summary>
    [Required]
    public bool RequestFollowUp { get; set; }

    /// <summary>
    /// Current status of the incident
    /// </summary>
    [Required]
    public IncidentStatus Status { get; set; }

    /// <summary>
    /// Assigned safety team member
    /// </summary>
    public Guid? AssignedTo { get; set; }

    /// <summary>
    /// When record was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When record was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who created the record (service account for anonymous)
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Who last updated the record
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public ApplicationUser? Reporter { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public ICollection<IncidentAuditLog> AuditLogs { get; set; } = new List<IncidentAuditLog>();
    public ICollection<IncidentNotification> Notifications { get; set; } = new List<IncidentNotification>();
}

/// <summary>
/// Incident severity levels
/// </summary>
public enum IncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Incident status workflow
/// </summary>
public enum IncidentStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3,
    Archived = 4
}