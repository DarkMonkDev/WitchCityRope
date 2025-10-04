using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email delivery log for vetting system
/// Tracks SendGrid email delivery status and history
/// </summary>
public class VettingEmailLog
{
    public VettingEmailLog()
    {
        DeliveryStatus = EmailDeliveryStatus.Pending;
        RetryCount = 0;
        SentAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // References
    public Guid ApplicationId { get; set; }
    public EmailTemplateType TemplateType { get; set; }

    // Email Information
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    // SendGrid Tracking
    public DateTime SentAt { get; set; }
    public EmailDeliveryStatus DeliveryStatus { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    // Retry Management
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
}

/// <summary>
/// Email delivery status tracking for SendGrid integration
/// </summary>
public enum EmailDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Bounced = 4
}
