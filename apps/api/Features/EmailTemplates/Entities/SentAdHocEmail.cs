using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a sent ad-hoc email with full audit trail.
/// Read-only after creation (never modified or deleted).
/// </summary>
public class SentAdHocEmail
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body (stored for audit)
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Recipient group description
    /// Examples: "all-tickets", "session-1", "volunteers"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Actual email addresses sent to (for audit)
    /// </summary>
    public string[] RecipientEmails { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Total number of recipients
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Number of emails successfully accepted by SendGrid.
    /// Null for emails sent before this tracking was added.
    /// </summary>
    public int? SuccessCount { get; set; }

    /// <summary>
    /// Number of emails that failed to send via SendGrid.
    /// Null for emails sent before this tracking was added.
    /// </summary>
    public int? FailureCount { get; set; }

    /// <summary>
    /// Related event (nullable - not all ad-hoc emails are event-related)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// SendGrid message ID for delivery tracking
    /// </summary>
    [MaxLength(100)]
    public string? SendGridMessageId { get; set; }

    /// <summary>
    /// Scheduled send time for future delivery
    /// Null: send immediately (existing behavior)
    /// Set: send at specified UTC time
    /// </summary>
    public DateTime? ScheduledSendAt { get; set; }

    /// <summary>
    /// Delivery status: Pending, Sent, Delivered, Failed, Bounced
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    /// <summary>
    /// Timestamp when email was sent (UTC)
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who sent this email
    /// </summary>
    [Required]
    public Guid SentBy { get; set; }

    // Navigation properties
    public Event? Event { get; set; }
    public ApplicationUser SentByUser { get; set; } = null!;
}
