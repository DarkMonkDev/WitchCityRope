using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for trigger configuration
/// Used in GlobalEmailTemplateDto for Events category templates
/// </summary>
public class TriggerConfigDto
{
    /// <summary>
    /// How the template is triggered: Manual, FixedEvent, or TimeBased
    /// </summary>
    public TemplateTriggerType TriggerType { get; set; }

    /// <summary>
    /// Whether this template is enabled for sending emails.
    /// Unified field — controls sending for all categories.
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
    /// </summary>
    public int? TimingOffsetHours { get; set; }

    /// <summary>
    /// Target recipient group for Events category
    /// Null for other categories
    /// </summary>
    public EventRecipientGroup? RecipientGroup { get; set; }
}
