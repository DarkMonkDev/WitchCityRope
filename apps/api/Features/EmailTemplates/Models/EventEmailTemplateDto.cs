namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for event-specific email template
/// </summary>
public class EventEmailTemplateDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid GlobalTemplateId { get; set; }

    public string TemplateType { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public string[] TargetSessions { get; set; } = Array.Empty<string>();
    public bool IsCustomized { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
