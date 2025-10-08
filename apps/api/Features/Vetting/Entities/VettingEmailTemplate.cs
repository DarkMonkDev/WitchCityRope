using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email templates for vetting notifications
/// Allows administrators to customize notification content with variable substitution
/// </summary>
public class VettingEmailTemplate
{
    public VettingEmailTemplate()
    {
        IsActive = true;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
        Variables = "{}";
    }

    // Primary Key
    public Guid Id { get; set; }

    // Template Information
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;

    // Email Bodies - HTML and Plain Text for compatibility
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    // Legacy property for backward compatibility
    [Obsolete("Use HtmlBody instead. Maintained for backward compatibility.")]
    public string Body
    {
        get => HtmlBody;
        set => HtmlBody = value;
    }

    // Variable substitution support (JSON list of available variables)
    // Example: ["ApplicantName", "SubmissionDate", "ApplicationNumber", "StatusToken"]
    public string Variables { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    // Versioning and Audit
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UpdatedBy { get; set; }

    // Navigation Properties
    public ApplicationUser UpdatedByUser { get; set; } = null!;
    public ICollection<VettingNotification> Notifications { get; set; } = new List<VettingNotification>();
}

/// <summary>
/// Email template types for vetting notifications
/// Updated for Calendly external interview scheduling workflow
/// </summary>
public enum EmailTemplateType
{
    ApplicationReceived = 0,
    InterviewApproved = 1,
    Approved = 2,
    OnHold = 3,
    Denied = 4,
    InterviewReminder = 5,
    InterviewCompleted = 6  // Renamed from InterviewScheduled - tracks interview completion, not scheduling
}
