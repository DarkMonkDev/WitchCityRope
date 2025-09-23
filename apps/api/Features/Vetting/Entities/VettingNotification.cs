using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email notifications for vetting applications
/// Tracks email delivery status and content for audit trail
/// </summary>
public class VettingNotification
{
    public VettingNotification()
    {
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    // Primary Key
    public Guid Id { get; set; }

    // References
    public Guid ApplicationId { get; set; }
    public Guid? TemplateId { get; set; }

    // Email Information
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }

    // Delivery Tracking
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingEmailTemplate? Template { get; set; }
}

/// <summary>
/// Status of email notifications
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}