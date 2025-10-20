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
        Title = string.Empty;
        Location = string.Empty;
        EncryptedDescription = string.Empty;
        ReportedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = IncidentStatus.ReportSubmitted;
        IsAnonymous = false;
        RequestFollowUp = false;
        Type = IncidentType.SafetyConcern;
        WhereOccurred = WhereOccurred.AtEvent;
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique reference number for tracking (SAF-YYYYMMDD-NNNN)
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string ReferenceNumber { get; set; }

    /// <summary>
    /// Short descriptive title/name for the incident (e.g., "Workshop Discomfort", "Equipment Failure")
    /// Displayed in table grids and incident lists for quick identification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Reporter user ID - NULL for anonymous reports
    /// </summary>
    public Guid? ReporterId { get; set; }

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
    /// Encrypted contact name if provided
    /// </summary>
    public string? EncryptedContactName { get; set; }

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
    /// Type of incident (Safety Concern, Boundary Violation, Harassment, Other)
    /// </summary>
    [Required]
    public IncidentType Type { get; set; }

    /// <summary>
    /// Where the incident occurred (At Event, Online, Private Play, Other)
    /// </summary>
    [Required]
    public WhereOccurred WhereOccurred { get; set; }

    /// <summary>
    /// Event name (if applicable)
    /// </summary>
    [MaxLength(200)]
    public string? EventName { get; set; }

    /// <summary>
    /// Whether reporter has spoken to the person involved
    /// </summary>
    public SpokenToPersonStatus? HasSpokenToPerson { get; set; }

    /// <summary>
    /// Reporter's desired outcomes (free-text)
    /// </summary>
    [MaxLength(2000)]
    public string? DesiredOutcomes { get; set; }

    /// <summary>
    /// Reporter's preference for future interactions with involved person
    /// </summary>
    [MaxLength(2000)]
    public string? FutureInteractionPreference { get; set; }

    /// <summary>
    /// Whether reporter wants to remain anonymous during the investigation
    /// </summary>
    public bool? AnonymousDuringInvestigation { get; set; }

    /// <summary>
    /// Whether reporter wants to remain anonymous in the final report
    /// </summary>
    public bool? AnonymousInFinalReport { get; set; }

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
    /// Assigned incident coordinator - NULL for unassigned incidents
    /// Can be ANY user (not limited to admin role)
    /// </summary>
    public Guid? CoordinatorId { get; set; }

    /// <summary>
    /// Google Drive folder link (manual entry for Phase 1)
    /// </summary>
    [MaxLength(500)]
    public string? GoogleDriveFolderUrl { get; set; }

    /// <summary>
    /// Google Drive final report document link
    /// </summary>
    [MaxLength(500)]
    public string? GoogleDriveFinalReportUrl { get; set; }

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
    public ApplicationUser? Coordinator { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public ICollection<IncidentAuditLog> AuditLogs { get; set; } = new List<IncidentAuditLog>();
    public ICollection<IncidentNotification> Notifications { get; set; } = new List<IncidentNotification>();
    public ICollection<IncidentNote> Notes { get; set; } = new List<IncidentNote>();
}

/// <summary>
/// Type of safety incident being reported
/// </summary>
public enum IncidentType
{
    /// <summary>
    /// General safety concern
    /// </summary>
    SafetyConcern = 1,

    /// <summary>
    /// Boundary violation
    /// </summary>
    BoundaryViolation = 2,

    /// <summary>
    /// Harassment
    /// </summary>
    Harassment = 3,

    /// <summary>
    /// Other concern not covered by above categories
    /// </summary>
    OtherConcern = 4
}

/// <summary>
/// Location type where incident occurred
/// </summary>
public enum WhereOccurred
{
    /// <summary>
    /// At a Witch City Rope event
    /// </summary>
    AtEvent = 1,

    /// <summary>
    /// Online (Discord, social media, etc.)
    /// </summary>
    Online = 2,

    /// <summary>
    /// Private play/interaction
    /// </summary>
    PrivatePlay = 3,

    /// <summary>
    /// Other community space
    /// </summary>
    OtherSpace = 4
}

/// <summary>
/// Whether reporter has spoken to involved person
/// </summary>
public enum SpokenToPersonStatus
{
    /// <summary>
    /// Yes, has spoken to person
    /// </summary>
    Yes = 1,

    /// <summary>
    /// No, has not spoken to person
    /// </summary>
    No = 2,

    /// <summary>
    /// Not applicable
    /// </summary>
    NotApplicable = 3
}

/// <summary>
/// Incident status workflow (5-stage process)
/// Migration from 4-stage: New→ReportSubmitted, InProgress→InformationGathering, Resolved/Archived→Closed
/// </summary>
public enum IncidentStatus
{
    /// <summary>
    /// Initial state - incident submitted, awaiting assignment
    /// OLD: New (1)
    /// </summary>
    ReportSubmitted = 1,

    /// <summary>
    /// Coordinator assigned, gathering additional information
    /// OLD: InProgress (2)
    /// </summary>
    InformationGathering = 2,

    /// <summary>
    /// Investigation complete, preparing final documentation
    /// NEW: No direct mapping from old enum
    /// </summary>
    ReviewingFinalReport = 3,

    /// <summary>
    /// Paused pending external information or processes
    /// NEW: No direct mapping from old enum
    /// </summary>
    OnHold = 4,

    /// <summary>
    /// Investigation complete, archived
    /// OLD: Resolved (3) OR Archived (4)
    /// </summary>
    Closed = 5
}