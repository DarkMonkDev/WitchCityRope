using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// VolunteerPosition entity representing volunteer opportunities for events.
/// Every position is tied to a specific session — event-wide positions are not supported.
/// </summary>
public class VolunteerPosition
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to the session this position belongs to (required — all positions are session-specific)
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Title of the volunteer position
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the volunteer role and responsibilities
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Number of volunteer slots needed
    /// </summary>
    [Required]
    public int SlotsNeeded { get; set; }

    /// <summary>
    /// Number of volunteer slots filled
    /// </summary>
    [Required]
    public int SlotsFilled { get; set; } = 0;

    /// <summary>
    /// Whether this position is visible on the public event page
    /// Public positions allow attendees to sign up, private positions are admin-only
    /// </summary>
    public bool IsPublicFacing { get; set; } = true;

    /// <summary>
    /// Start time for this volunteer shift (stored as time-only string in HH:mm format)
    /// </summary>
    [MaxLength(5)]
    public string? StartTime { get; set; }

    /// <summary>
    /// End time for this volunteer shift (stored as time-only string in HH:mm format)
    /// </summary>
    [MaxLength(5)]
    public string? EndTime { get; set; }

    /// <summary>
    /// Navigation property to parent event
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Navigation property to the session this position belongs to
    /// </summary>
    public Session? Session { get; set; }

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets remaining volunteer slots needed
    /// </summary>
    public int SlotsRemaining => SlotsNeeded - SlotsFilled;

    /// <summary>
    /// Gets whether all volunteer slots are filled
    /// </summary>
    public bool IsFullyStaffed => SlotsFilled >= SlotsNeeded;
}
