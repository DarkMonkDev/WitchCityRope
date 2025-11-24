using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Venue entity for PostgreSQL database.
/// Represents physical locations where WitchCityRope events are held.
/// Uses soft delete pattern (IsActive flag) to preserve event history.
/// </summary>
public class Venue
{
    /// <summary>
    /// Unique identifier using auto-increment integer
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Venue name (required, unique case-insensitive)
    /// Max length: 100 characters
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directions to venue (optional)
    /// Max length: 500 characters
    /// </summary>
    [MaxLength(500)]
    public string? Directions { get; set; }

    /// <summary>
    /// Additional venue notes (capacity, parking, etc.) (optional)
    /// Max length: 1000 characters
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// General location information (city, state) for public display
    /// Safe to display to all users (no PII)
    /// Max length: 100 characters
    /// Example: "Salem, MA"
    /// </summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// Soft delete flag - venues are never hard deleted
    /// Defaults to true (active)
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When record was created (UTC)
    /// CRITICAL: Must be UTC for PostgreSQL TIMESTAMPTZ compatibility
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to events using this venue
    /// One-to-many relationship with Event
    /// </summary>
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
