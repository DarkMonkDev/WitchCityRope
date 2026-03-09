using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents an event-specific email template override.
/// Created only when event organizer customizes a template (copy-on-edit).
/// </summary>
public class EventEmailTemplate
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event this template is associated with
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to global template (for metadata, NOT foreign key constraint)
    /// </summary>
    [Required]
    public Guid GlobalTemplateId { get; set; }

    /// <summary>
    /// Template type (e.g., "Confirmation", "Reminder1Day")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Customized email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Customized HTML email body
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Customized plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Target sessions for multi-session events
    /// ["all"] = all sessions, ["S1", "S2"] = specific sessions
    /// </summary>
    public string[] TargetSessions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Recipient group (for future ad-hoc use)
    /// </summary>
    [MaxLength(100)]
    public string? RecipientGroup { get; set; }

    /// <summary>
    /// Override for sending enabled state at the event level.
    /// Null: use global template's SendingEnabled setting
    /// True/False: override global setting for this specific event
    /// </summary>
    public bool? OverrideSendingEnabled { get; set; }

    /// <summary>
    /// Override for timing offset
    /// Null: use global template setting
    /// Value: override with event-specific timing
    /// </summary>
    public int? OverrideTimingOffsetDays { get; set; }

    /// <summary>
    /// Override for timing offset hours (sub-day precision)
    /// Null: use global template setting
    /// Value: override with event-specific hours offset
    /// </summary>
    public int? OverrideTimingOffsetHours { get; set; }

    /// <summary>
    /// Override for recipient group
    /// Null: use global template setting
    /// Value: override with event-specific recipients
    /// </summary>
    public EventRecipientGroup? OverrideRecipientGroup { get; set; }

    /// <summary>
    /// Always true for event-specific templates
    /// </summary>
    public bool IsCustomized { get; set; } = true;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created/updated this template
    /// </summary>
    [Required]
    public Guid UpdatedBy { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ApplicationUser UpdatedByUser { get; set; } = null!;

    // NOTE: GlobalTemplate navigation is intentionally NOT included
    // GlobalTemplateId is reference-only, not a foreign key constraint
}
