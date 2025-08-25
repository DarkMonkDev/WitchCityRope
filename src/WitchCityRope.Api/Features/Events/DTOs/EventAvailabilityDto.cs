using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO for real-time event availability information
/// Used for availability matrix display and booking validation
/// </summary>
public class EventAvailabilityDto
{
    /// <summary>
    /// Event identifier
    /// </summary>
    public Guid EventId { get; set; }
    
    /// <summary>
    /// Event title for display
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this event has sessions configured
    /// </summary>
    public bool HasSessions { get; set; }
    
    /// <summary>
    /// Overall event availability status
    /// </summary>
    public bool IsAvailable { get; set; }
    
    /// <summary>
    /// Total available spots across all ticket types (minimum constraint)
    /// </summary>
    public int TotalAvailableSpots { get; set; }
    
    /// <summary>
    /// Session availability information
    /// </summary>
    public List<SessionAvailabilityDto> SessionAvailability { get; set; } = new();
    
    /// <summary>
    /// Ticket type availability information
    /// </summary>
    public List<TicketTypeAvailabilityDto> TicketTypeAvailability { get; set; } = new();
    
    /// <summary>
    /// Timestamp when this availability was calculated (for caching)
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Availability information for a specific session
/// </summary>
public class SessionAvailabilityDto
{
    /// <summary>
    /// Session identifier (S1, S2, S3, etc.)
    /// </summary>
    public string SessionIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Session display name
    /// </summary>
    public string SessionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Total capacity for this session
    /// </summary>
    public int Capacity { get; set; }
    
    /// <summary>
    /// Current registered count
    /// </summary>
    public int RegisteredCount { get; set; }
    
    /// <summary>
    /// Available spots
    /// </summary>
    public int AvailableSpots { get; set; }
    
    /// <summary>
    /// Whether this session has available capacity
    /// </summary>
    public bool HasAvailableCapacity { get; set; }
    
    /// <summary>
    /// Availability status for display (Available, Full, Limited)
    /// </summary>
    public string AvailabilityStatus { get; set; } = string.Empty;
}

/// <summary>
/// Availability information for a specific ticket type
/// </summary>
public class TicketTypeAvailabilityDto
{
    /// <summary>
    /// Ticket type identifier
    /// </summary>
    public Guid TicketTypeId { get; set; }
    
    /// <summary>
    /// Ticket type name for display
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Session identifiers included in this ticket type
    /// </summary>
    public List<string> IncludedSessions { get; set; } = new();
    
    /// <summary>
    /// Available spots for this ticket type (minimum across included sessions)
    /// </summary>
    public int AvailableSpots { get; set; }
    
    /// <summary>
    /// Whether this ticket type has available capacity
    /// </summary>
    public bool HasAvailableCapacity { get; set; }
    
    /// <summary>
    /// Whether this ticket type is currently on sale
    /// </summary>
    public bool IsCurrentlyOnSale { get; set; }
    
    /// <summary>
    /// Limiting session (the session with the least availability)
    /// </summary>
    public string? LimitingSession { get; set; }
    
    /// <summary>
    /// Availability status for display (Available, Full, Limited, Off Sale)
    /// </summary>
    public string AvailabilityStatus { get; set; } = string.Empty;
}