using WitchCityRope.Api.Features.EmailTemplates.Entities;

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
    /// User-editable display title for the template.
    /// Shown at the top of template cards and in the editor header.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;

    // Trigger configuration fields (Events category only)
    /// <summary>
    /// How the template is triggered: Manual, FixedEvent, or TimeBased
    /// </summary>
    public TemplateTriggerType TriggerType { get; set; }

    /// <summary>
    /// Whether this template is enabled for sending emails (all categories).
    /// When false, emails using this template are silently suppressed.
    /// </summary>
    public bool SendingEnabled { get; set; }

    /// <summary>
    /// Days offset for time-based triggers
    /// Positive: days BEFORE session start (e.g., 3 = 3 days before)
    /// Negative: days AFTER session start (e.g., -2 = 2 days after)
    /// Null: not applicable for non-TimeBased triggers
    /// </summary>
    public int? TimingOffsetDays { get; set; }

    /// <summary>
    /// Hours offset for sub-day precision in time-based triggers
    /// Used with TimingOffsetDays: totalOffsetHours = (TimingOffsetDays ?? 0) * 24 + (TimingOffsetHours ?? 0)
    /// </summary>
    public int? TimingOffsetHours { get; set; }

    /// <summary>
    /// Target recipient group for Events category
    /// Null for other categories
    /// </summary>
    public EventRecipientGroup? RecipientGroup { get; set; }
}
