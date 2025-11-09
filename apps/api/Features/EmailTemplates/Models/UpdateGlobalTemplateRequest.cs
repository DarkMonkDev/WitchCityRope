using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for updating global email template
/// </summary>
public class UpdateGlobalTemplateRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;
}
