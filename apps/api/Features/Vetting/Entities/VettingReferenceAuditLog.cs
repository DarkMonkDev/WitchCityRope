namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Audit trail for reference management actions
/// Tracks email delivery, responses, and contact attempts
/// </summary>
public class VettingReferenceAuditLog
{
    public VettingReferenceAuditLog()
    {
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Reference Context
    public Guid ReferenceId { get; set; }
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty; // "EmailSent", "ReminderSent", "ResponseReceived"
    public string ActionDescription { get; set; } = string.Empty;
    public string? EmailMessageId { get; set; } // For tracking email delivery
    public string? DeliveryStatus { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingReference Reference { get; set; } = null!;
}