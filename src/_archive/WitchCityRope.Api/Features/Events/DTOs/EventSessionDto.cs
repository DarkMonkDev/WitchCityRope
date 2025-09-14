using System;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO representing a session within an event (S1, S2, S3, etc.)
/// Used for event session matrix display and availability calculation
/// </summary>
public class EventSessionDto
{
    /// <summary>
    /// Unique identifier for this session
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Session identifier (S1, S2, S3, etc.) - used for business logic and UI display
    /// </summary>
    public string SessionIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the session (e.g., "Friday Workshop", "Saturday Intensive")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when this session occurs
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Start time of the session (combined with Date for full datetime)
    /// </summary>
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// End time of the session (combined with Date for full datetime)
    /// </summary>
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// Maximum number of attendees for this session
    /// </summary>
    public int Capacity { get; set; }
    
    /// <summary>
    /// Current number of registered attendees
    /// </summary>
    public int RegisteredCount { get; set; }
    
    /// <summary>
    /// Number of available spots (Capacity - RegisteredCount)
    /// </summary>
    public int AvailableSpots { get; set; }
    
    /// <summary>
    /// Whether this session is required for the event
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Whether this session has available capacity for registration
    /// </summary>
    public bool HasAvailableCapacity { get; set; }
    
    /// <summary>
    /// Full start datetime for the session (Date + StartTime)
    /// </summary>
    public DateTime StartDateTime => Date.Date.Add(StartTime);
    
    /// <summary>
    /// Full end datetime for the session (Date + EndTime)
    /// </summary>
    public DateTime EndDateTime => Date.Date.Add(EndTime);
}