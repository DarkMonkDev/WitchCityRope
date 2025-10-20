using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// VolunteerPosition entity representing volunteer opportunities for events
/// Supports both event-level and session-specific volunteer positions
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
    /// Reference to specific session (null for event-wide positions)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Title of the volunteer position
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the volunteer role and responsibilities
    /// </summary>
    [Required]
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
    /// Whether this position requires specific skills or experience
    /// </summary>
    public bool RequiresExperience { get; set; } = false;

    /// <summary>
    /// Special requirements or qualifications needed
    /// </summary>
    public string Requirements { get; set; } = string.Empty;

    /// <summary>
    /// Whether this position is visible on the public event page
    /// Public positions allow attendees to sign up, private positions are admin-only
    /// </summary>
    public bool IsPublicFacing { get; set; } = true;

    /// <summary>
    /// Navigation property to parent event
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Navigation property to specific session (if session-specific)
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