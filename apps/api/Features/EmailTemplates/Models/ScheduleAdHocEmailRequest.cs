using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for scheduling an ad-hoc email for future delivery
/// </summary>
public class ScheduleAdHocEmailRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Optional recipient group (mutually exclusive with RecipientEmails)
    /// </summary>
    public UserSegment? Segment { get; set; }

    /// <summary>
    /// Optional manual recipient list (mutually exclusive with Segment)
    /// </summary>
    public List<string>? RecipientEmails { get; set; }

    /// <summary>
    /// Display name for recipient group (e.g., "All Vetted Members")
    /// </summary>
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Optional event association
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// When to send the email (UTC)
    /// Must be in the future
    /// </summary>
    [Required(ErrorMessage = "Scheduled send time is required")]
    public DateTime ScheduledSendAt { get; set; }
}
