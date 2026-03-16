namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for sent ad-hoc email
/// </summary>
public class SentAdHocEmailDto
{
    public Guid Id { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public string RecipientGroup { get; set; } = string.Empty;
    public int RecipientCount { get; set; }

    /// <summary>
    /// Number of emails successfully accepted by SendGrid.
    /// Null for emails sent before tracking was added.
    /// </summary>
    public int? SuccessCount { get; set; }

    /// <summary>
    /// Number of emails that failed to send.
    /// Null for emails sent before tracking was added.
    /// </summary>
    public int? FailureCount { get; set; }

    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }

    public string? SendGridMessageId { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
    public Guid SentBy { get; set; }
    public string SentByEmail { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled send time for future delivery
    /// Null for immediate sends
    /// </summary>
    public DateTime? ScheduledSendAt { get; set; }
}
