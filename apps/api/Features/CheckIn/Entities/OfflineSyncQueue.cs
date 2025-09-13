using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.CheckIn.Entities;

/// <summary>
/// OfflineSyncQueue entity - Manages offline operations for sync when connectivity returns
/// Ensures data integrity during poor network conditions at events
/// </summary>
public class OfflineSyncQueue
{
    /// <summary>
    /// Unique identifier for the sync queue entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Staff member who performed the action
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of action that was performed offline
    /// Values: check-in, manual-entry, status-update, capacity-override
    /// </summary>
    [Required]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Complete action data as JSON for replay during sync
    /// </summary>
    [Required]
    public string ActionData { get; set; } = "{}";

    /// <summary>
    /// Local timestamp when the action was performed
    /// </summary>
    public DateTime LocalTimestamp { get; set; }

    /// <summary>
    /// Number of sync retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Current sync status
    /// Values: pending, syncing, completed, failed, conflict
    /// </summary>
    [Required]
    public string SyncStatus { get; set; } = "pending";

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the queue entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the action was successfully synced (null if not synced)
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Constructor initializes required fields
    /// </summary>
    public OfflineSyncQueue()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        LocalTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new sync queue entry
    /// </summary>
    public OfflineSyncQueue(Guid eventId, Guid userId, string actionType, string actionData) : this()
    {
        EventId = eventId;
        UserId = userId;
        ActionType = actionType;
        ActionData = actionData;
    }
}