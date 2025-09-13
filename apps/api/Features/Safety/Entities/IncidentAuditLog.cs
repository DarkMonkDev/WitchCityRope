using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Safety.Entities;

/// <summary>
/// Audit log for all incident-related actions
/// </summary>
public class IncidentAuditLog
{
    public IncidentAuditLog()
    {
        Id = Guid.NewGuid();
        ActionType = string.Empty;
        ActionDescription = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Related incident ID
    /// </summary>
    [Required]
    public Guid IncidentId { get; set; }

    /// <summary>
    /// User who performed the action (NULL for system actions)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Type of action performed
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    [Required]
    public string ActionDescription { get; set; }

    /// <summary>
    /// Previous values for update operations (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values for update operations (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action was performed (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public SafetyIncident Incident { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}