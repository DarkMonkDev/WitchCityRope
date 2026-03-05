namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// DTO for EmailTriggerLog entries (admin visibility into automated email sends)
/// </summary>
public class EmailTriggerLogDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid? SessionId { get; set; }
    public string? SessionTitle { get; set; }
    public string TemplateType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string RecipientGroup { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
