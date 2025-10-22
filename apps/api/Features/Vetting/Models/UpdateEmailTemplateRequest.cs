using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request DTO for updating email template content
/// Used for PUT requests to update template subject and body
/// </summary>
public class UpdateEmailTemplateRequest
{
    /// <summary>
    /// Email subject line with variable placeholders
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body with variable placeholders
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body with variable placeholders (fallback for non-HTML clients)
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;
}
