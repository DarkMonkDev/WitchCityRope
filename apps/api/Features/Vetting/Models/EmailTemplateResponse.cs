namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response DTO for email template data
/// Used for GET requests to return email template information
/// </summary>
public class EmailTemplateResponse
{
    public Guid Id { get; set; }
    public int TemplateType { get; set; }
    public string TemplateTypeName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string Variables { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
