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

    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }

    public string? SendGridMessageId { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
    public Guid SentBy { get; set; }
    public string SentByEmail { get; set; } = string.Empty;
}
