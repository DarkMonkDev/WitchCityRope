using System;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// Request model for creating a new event session
/// </summary>
public class CreateEventSessionRequest
{
    /// <summary>
    /// Session identifier (S1, S2, S3, etc.)
    /// Must be unique within the event
    /// </summary>
    [Required]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Session identifier must be 2-10 characters")]
    public string SessionIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the session (e.g., "Friday Workshop")
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Session name is required and must be 1-100 characters")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when this session occurs
    /// </summary>
    [Required]
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Start time of the session
    /// </summary>
    [Required]
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// End time of the session
    /// </summary>
    [Required]
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// Maximum number of attendees for this session
    /// Must be greater than 0
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0")]
    public int Capacity { get; set; }
    
    /// <summary>
    /// Whether this session is required for the event
    /// </summary>
    public bool IsRequired { get; set; } = false;
}