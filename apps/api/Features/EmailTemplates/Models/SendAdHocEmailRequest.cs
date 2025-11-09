using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for sending ad-hoc email
/// </summary>
public class SendAdHocEmailRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Recipient group is required")]
    [MaxLength(100)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Optional event ID if email is event-related
    /// </summary>
    public Guid? EventId { get; set; }
}
