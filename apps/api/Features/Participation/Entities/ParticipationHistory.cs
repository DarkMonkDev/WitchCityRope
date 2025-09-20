using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// ParticipationHistory entity - Comprehensive audit trail for participation changes
/// Maintains complete change tracking for compliance and debugging
/// </summary>
public class ParticipationHistory
{
    /// <summary>
    /// Unique identifier for the history record
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the participation record
    /// </summary>
    [Required]
    public Guid ParticipationId { get; set; }

    /// <summary>
    /// Type of action performed
    /// </summary>
    [Required]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Previous values before change (JSONB)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values after change (JSONB)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// User who made the change
    /// </summary>
    public Guid? ChangedBy { get; set; }

    /// <summary>
    /// Reason for the change
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// IP address of the user making the change
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for browser identification
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the change was made (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public EventParticipation Participation { get; set; } = null!;
    public ApplicationUser? ChangedByUser { get; set; }

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public ParticipationHistory()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new history record
    /// </summary>
    public ParticipationHistory(Guid participationId, string actionType) : this()
    {
        ParticipationId = participationId;
        ActionType = actionType;
    }
}