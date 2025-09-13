using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Safety.Entities;

/// <summary>
/// Email notification tracking for incident alerts
/// </summary>
public class IncidentNotification
{
    public IncidentNotification()
    {
        Id = Guid.NewGuid();
        NotificationType = "Email";
        RecipientType = string.Empty;
        RecipientEmail = string.Empty;
        Subject = string.Empty;
        MessageBody = string.Empty;
        Status = "Pending";
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Related incident ID
    /// </summary>
    [Required]
    public Guid IncidentId { get; set; }

    /// <summary>
    /// Type of notification (Email, Alert)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NotificationType { get; set; }

    /// <summary>
    /// Recipient category (SafetyTeam, OnCall, Admin)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string RecipientType { get; set; }

    /// <summary>
    /// Email address of recipient
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string RecipientEmail { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; }

    /// <summary>
    /// Email message body
    /// </summary>
    [Required]
    public string MessageBody { get; set; }

    /// <summary>
    /// Delivery status
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; }

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of delivery attempts
    /// </summary>
    [Required]
    public int RetryCount { get; set; }

    /// <summary>
    /// When notification was sent (UTC)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When record was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When record was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public SafetyIncident Incident { get; set; } = null!;
}