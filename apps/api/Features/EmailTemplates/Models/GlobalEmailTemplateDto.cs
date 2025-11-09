namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for global email template
/// Auto-generates TypeScript interface via NSwag
/// </summary>
public class GlobalEmailTemplateDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Email category: Vetting, Events, Admin, Incident, AdHoc
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Template type within category (e.g., "Confirmation")
    /// </summary>
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Display-friendly name for template type
    /// </summary>
    public string TemplateTypeName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Available variables for this template
    /// Example: ["{{attendee_name}}", "{{event_title}}"]
    /// </summary>
    public string[] Variables { get; set; } = Array.Empty<string>();

    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
