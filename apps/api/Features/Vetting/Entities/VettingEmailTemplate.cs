using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email templates for vetting notifications
/// Allows administrators to customize notification content
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
    }

    // Primary Key
    public Guid Id { get; set; }

    // Template Information
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
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
/// </summary>
public enum EmailTemplateType
{
    ApplicationReceived = 1,
    InterviewApproved = 2,
    ApplicationApproved = 3,
    ApplicationOnHold = 4,
    ApplicationDenied = 5,
    InterviewReminder = 6
}