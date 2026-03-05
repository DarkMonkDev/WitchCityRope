using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for updating trigger configuration on global templates
/// Only applies to Events category templates
/// </summary>
public class UpdateTriggerConfigRequest
{
    [Required(ErrorMessage = "Trigger type is required")]
    public TemplateTriggerType TriggerType { get; set; }

    [Required(ErrorMessage = "Trigger enabled flag is required")]
    public bool TriggerEnabled { get; set; }

    /// <summary>
    /// Days offset for time-based triggers
    /// Must be between -365 and 365
    /// Required when TriggerType is TimeBased
    /// </summary>
    [Range(-365, 365, ErrorMessage = "Timing offset must be between -365 and 365 days")]
    public int? TimingOffsetDays { get; set; }

    /// <summary>
    /// Hours offset for sub-day precision in time-based triggers
    /// Must be between -23 and 23
    /// </summary>
    [Range(-23, 23, ErrorMessage = "Timing offset hours must be between -23 and 23")]
    public int? TimingOffsetHours { get; set; }

    /// <summary>
    /// Target recipient group for Events category
    /// Required when TriggerType is not Manual
    /// </summary>
    public EventRecipientGroup? RecipientGroup { get; set; }
}
