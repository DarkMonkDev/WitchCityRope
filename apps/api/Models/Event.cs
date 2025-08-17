using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Event entity for PostgreSQL database
/// Matches existing database table structure
/// </summary>
public class Event
{
    /// <summary>
    /// Unique identifier using UUID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Event title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time in UTC
    /// CRITICAL: Must be UTC for PostgreSQL TIMESTAMPTZ compatibility
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date/time in UTC
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Maximum number of attendees
    /// </summary>
    [Required]
    public int Capacity { get; set; }

    /// <summary>
    /// Type of event (workshop, social, performance, etc.)
    /// </summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event location
    /// </summary>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event is published/visible
    /// </summary>
    [Required]
    public bool IsPublished { get; set; } = true;

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
    /// JSON string containing pricing information
    /// </summary>
    [Required]
    public string PricingTiers { get; set; } = "{}";
}