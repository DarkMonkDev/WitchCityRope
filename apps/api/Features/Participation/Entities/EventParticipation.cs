using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// EventParticipation entity - Central tracking for both RSVPs and ticket purchases
/// Supports both social event RSVPs and class event ticket purchases
/// </summary>
public class EventParticipation
{
    /// <summary>
    /// Unique identifier for the participation record
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to the participating user
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of participation: RSVP or Ticket
    /// </summary>
    [Required]
    public ParticipationType ParticipationType { get; set; }

    /// <summary>
    /// Current status of the participation
    /// </summary>
    [Required]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Active;

    /// <summary>
    /// When the participation was created (UTC)
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the participation was cancelled (UTC)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Optional notes from the participant
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Flexible metadata for additional information
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    [Required]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// User who created this participation
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated this participation
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// When the participation was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public Event Event { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public ICollection<ParticipationHistory> History { get; set; } = new List<ParticipationHistory>();

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public EventParticipation()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new participation
    /// </summary>
    public EventParticipation(Guid eventId, Guid userId, ParticipationType type) : this()
    {
        EventId = eventId;
        UserId = userId;
        ParticipationType = type;
    }

    /// <summary>
    /// Determines if this participation can be cancelled
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status == ParticipationStatus.Active;
    }

    /// <summary>
    /// Cancels the participation with reason
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException("Participation cannot be cancelled in current status");

        Status = ParticipationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}