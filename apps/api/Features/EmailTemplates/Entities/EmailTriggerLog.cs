using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Audit log for automated email triggers
/// Provides idempotency for time-based triggers (prevent duplicate sends)
/// Tracks all automatic email operations
/// </summary>
public class EmailTriggerLog
{
    /// <summary>
    /// Primary key (EF Core manages - NO initializer)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template that triggered (GlobalEmailTemplate or EventEmailTemplate)
    /// </summary>
    [Required]
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Event associated with trigger (nullable for non-event triggers)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Session that triggered time-based send
    /// Required for time-based triggers, null for fixed event triggers
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Template type (e.g., "Confirmation", "Reminder1Day")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Trigger type: Manual, FixedEvent, TimeBased
    /// Stored as string for audit clarity
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Recipient group used (EventRecipientGroup or UserSegment)
    /// Stored as string for audit clarity
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Number of recipients email was sent to
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// When trigger condition was met (UTC)
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// When email was actually sent (UTC)
    /// Null if send failed
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Send status: Sent, Failed, Skipped
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Sent";

    /// <summary>
    /// Error message if Status = Failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties (nullable to prevent cascade issues)
    public Event? Event { get; set; }
    public Session? Session { get; set; }
}
