namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Defines how email templates are triggered
/// </summary>
public enum TemplateTriggerType
{
    /// <summary>
    /// No automatic trigger - manual send only
    /// Used for Ad Hoc category templates
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Triggered by specific business events (existing behavior)
    /// Examples: ticket purchase, password reset, vetting status change
    /// Used for Vetting, Admin, Incident categories
    /// </summary>
    FixedEvent = 1,

    /// <summary>
    /// Triggered by time offset from session start
    /// Only used for Events category
    /// TimingOffsetDays: positive = before session, negative = after session
    /// </summary>
    TimeBased = 2
}
