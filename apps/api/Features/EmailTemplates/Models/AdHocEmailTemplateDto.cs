namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for saved ad-hoc email templates
/// These are user-created templates that can be reused
/// </summary>
public class AdHocEmailTemplateDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Template name (from subject or custom)
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
}
