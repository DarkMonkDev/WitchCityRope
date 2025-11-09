using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for updating/creating event-specific email template
/// </summary>
public class UpdateEventTemplateRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Target sessions for multi-session events
    /// Default: ["all"] for all sessions
    /// </summary>
    public string[] TargetSessions { get; set; } = new[] { "all" };
}
