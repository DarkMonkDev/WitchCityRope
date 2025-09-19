using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Session entity representing individual sessions within an event
/// Supports both single-session and multi-session events
/// </summary>
public class Session
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
    /// Session identifier code (e.g., "S1", "S2", "Day1", "Day2")
    /// </summary>
    [Required]
    public string SessionCode { get; set; } = string.Empty;

    /// <summary>
    /// Name of the session (e.g., "Morning Session", "Day 1", "Afternoon Workshop")
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Session start time in UTC
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time in UTC  
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum capacity for this specific session
    /// </summary>
    [Required]
    public int Capacity { get; set; }

    /// <summary>
    /// Current number of confirmed attendees
    /// </summary>
    [Required]
    public int CurrentAttendees { get; set; } = 0;

    /// <summary>
    /// Navigation property to parent event
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Navigation property to ticket types that include this session
    /// </summary>
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

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
}