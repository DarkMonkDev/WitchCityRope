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
    /// Whether automatic triggering is enabled
    /// </summary>
    public bool TriggerEnabled { get; set; }

    /// <summary>
    /// Days offset for time-based triggers
    /// Positive: days BEFORE session start (e.g., 3 = 3 days before)
    /// Negative: days AFTER session start (e.g., -2 = 2 days after)
    /// Null: not applicable for non-TimeBased triggers
    /// </summary>
    public int? TimingOffsetDays { get; set; }

    /// <summary>
    /// Target recipient group for Events category
    /// Null for other categories
    /// </summary>
    public EventRecipientGroup? RecipientGroup { get; set; }
}
