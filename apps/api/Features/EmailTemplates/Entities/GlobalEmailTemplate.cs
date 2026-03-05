using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a global email template for a specific category and type.
/// Serves as default template unless overridden by event-specific template.
/// </summary>
public class GlobalEmailTemplate
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email category (Vetting, Events, Admin, Incident, AdHoc)
    /// </summary>
    [Required]
    public EmailCategory Category { get; set; }

    /// <summary>
    /// Template type within category (stored as enum string)
    /// Examples: "Confirmation", "Reminder1Day", "ApplicationReceived"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line (max 200 characters)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body with {{variable}} placeholders
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body for clients that don't support HTML
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// JSONB field containing available variables for this template
    /// Example: ["{{attendee_name}}", "{{event_title}}"]
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string Variables { get; set; } = "[]";

    /// <summary>
    /// Trigger type for this template
    /// Default: FixedEvent (existing behavior)
    /// Events category can use TimeBased
    /// </summary>
    public TemplateTriggerType TriggerType { get; set; } = TemplateTriggerType.FixedEvent;

    /// <summary>
    /// Whether automatic triggering is enabled
    /// Default: true (maintains existing behavior)
    /// </summary>
    public bool TriggerEnabled { get; set; } = true;

    /// <summary>
    /// Days offset for time-based triggers (Events category only)
    /// Positive: days BEFORE session start (e.g., 3 = 3 days before)
    /// Negative: days AFTER session start (e.g., -2 = 2 days after)
    /// Null: not applicable (FixedEvent or Manual trigger types)
    /// </summary>
    public int? TimingOffsetDays { get; set; }

    /// <summary>
    /// Hours offset for sub-day precision in time-based triggers (Events category only)
    /// Used together with TimingOffsetDays: totalOffsetHours = (TimingOffsetDays ?? 0) * 24 + (TimingOffsetHours ?? 0)
    /// Range: -23 to 23
    /// </summary>
    public int? TimingOffsetHours { get; set; }

    /// <summary>
    /// Target recipient group for Events category templates
    /// Null for other categories (recipients are hardcoded in service code)
    /// </summary>
    public EventRecipientGroup? RecipientGroup { get; set; }

    /// <summary>
    /// Soft delete flag (false = hidden, never hard delete)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number (increments on each update for audit trail)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who last updated this template
    /// </summary>
    [Required]
    public Guid UpdatedBy { get; set; }

    // Navigation properties
    public ApplicationUser UpdatedByUser { get; set; } = null!;
}

/// <summary>
/// Email category enumeration
/// </summary>
public enum EmailCategory
{
    Vetting = 0,
    Events = 1,
    Admin = 2,
    Incident = 3,
    AdHoc = 4
}
