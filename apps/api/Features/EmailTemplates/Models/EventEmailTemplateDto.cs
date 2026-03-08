namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Response DTO for event-specific email template
/// </summary>
public class EventEmailTemplateDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid GlobalTemplateId { get; set; }

    public string TemplateType { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public string[] TargetSessions { get; set; } = Array.Empty<string>();
    public bool IsCustomized { get; set; }

    /// <summary>
    /// Resolved trigger configuration (override value if set, otherwise global value)
    /// </summary>
    public string? TriggerType { get; set; }
    public bool TriggerEnabled { get; set; }
    public int? TimingOffsetDays { get; set; }
    public int? TimingOffsetHours { get; set; }
    public string? RecipientGroup { get; set; }

    /// <summary>
    /// Whether any trigger settings are overridden at the event level
    /// </summary>
    public bool HasTriggerOverrides { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
