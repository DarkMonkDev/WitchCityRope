using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for sending ad-hoc email
/// Supports both segment-based and manual recipient list sending
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

    /// <summary>
    /// Optional user segment for targeted email campaigns
    /// If provided, email will be sent to all users in the segment
    /// Mutually exclusive with RecipientEmails
    /// </summary>
    public UserSegment? Segment { get; set; }

    /// <summary>
    /// Optional manual list of recipient email addresses
    /// If provided, email will be sent to these specific addresses
    /// Mutually exclusive with Segment
    /// </summary>
    public List<string>? RecipientEmails { get; set; }

    /// <summary>
    /// Description of recipient group (for audit trail)
    /// Auto-populated if Segment is used, or manually specified for RecipientEmails
    /// </summary>
    [Required(ErrorMessage = "Recipient group description is required")]
    [MaxLength(100)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Optional event ID if email is event-related
    /// </summary>
    public Guid? EventId { get; set; }
}
