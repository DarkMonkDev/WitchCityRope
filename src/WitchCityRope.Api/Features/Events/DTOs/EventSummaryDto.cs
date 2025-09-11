using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO for event summary information in list views
/// Matches requirements from backend-integration-requirements.md
/// </summary>
public class EventSummaryDto
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid EventId { get; set; }
    
    /// <summary>
    /// Event title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Short description of the event
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Event type (Class, SocialEvent)
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Event start date and time
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Event end date and time
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Event venue/location
    /// </summary>
    public string Venue { get; set; } = string.Empty;
    
    /// <summary>
    /// Total capacity across all sessions
    /// </summary>
    public int TotalCapacity { get; set; }
    
    /// <summary>
    /// Number of available spots
    /// </summary>
    public int AvailableSpots { get; set; }
    
    /// <summary>
    /// Whether this event has multiple sessions
    /// </summary>
    public bool HasMultipleSessions { get; set; }
    
    /// <summary>
    /// Number of sessions in this event
    /// </summary>
    public int SessionCount { get; set; }
    
    /// <summary>
    /// Lowest price across all ticket types
    /// </summary>
    public decimal LowestPrice { get; set; }
    
    /// <summary>
    /// Highest price across all ticket types
    /// </summary>
    public decimal HighestPrice { get; set; }
    
    /// <summary>
    /// Whether the event is currently published
    /// </summary>
    public bool IsPublished { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}