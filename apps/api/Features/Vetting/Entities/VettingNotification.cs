namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email notification tracking and delivery management
/// Handles email queuing, delivery tracking, and retry logic
/// </summary>
public class VettingNotification
{
    public VettingNotification()
    {
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }

    // Notification Details
    public NotificationType NotificationType { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }

    // Delivery Tracking
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; } // Email provider message ID
    public string? DeliveryStatus { get; set; } // Delivered, Bounced, etc.

    // Template Information
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateData { get; set; } // JSON data for template merge

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
}

/// <summary>
/// Types of notifications sent during the vetting process
/// </summary>
public enum NotificationType
{
    ApplicationConfirmation = 1,
    ReferenceRequest = 2,
    ReferenceReminder = 3,
    StatusUpdate = 4,
    AdditionalInfoRequest = 5,
    InterviewScheduled = 6,
    DecisionNotification = 7,
    WelcomePackage = 8
}

/// <summary>
/// Email delivery status tracking
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Bounced = 5,
    Unsubscribed = 6
}