using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO for event with complete session and ticket type information
/// Used for event session matrix display
/// </summary>
public class EventWithSessionsDto : WitchCityRope.Core.DTOs.EventDto
{
    /// <summary>
    /// Sessions available for this event (S1, S2, S3, etc.)
    /// Includes capacity and availability information
    /// </summary>
    public List<EventSessionDto> Sessions { get; set; } = new();
    
    /// <summary>
    /// Ticket types available for this event
    /// Includes pricing, session inclusion, and availability
    /// </summary>
    public List<EventTicketTypeDto> TicketTypes { get; set; } = new();
    
    /// <summary>
    /// Whether this event supports session-based ticketing
    /// </summary>
    public bool HasSessions { get; set; }
    
    /// <summary>
    /// Whether this event has ticket types configured
    /// </summary>
    public bool HasTicketTypes { get; set; }
    
    /// <summary>
    /// Total capacity across all sessions (for reporting purposes)
    /// </summary>
    public int TotalSessionCapacity { get; set; }
    
    /// <summary>
    /// Event pricing summary for display
    /// Shows price range across all ticket types
    /// </summary>
    public PricingSummaryDto PricingSummary { get; set; } = new();
}

/// <summary>
/// Summary of pricing information across all ticket types
/// </summary>
public class PricingSummaryDto
{
    /// <summary>
    /// Minimum price across all ticket types
    /// </summary>
    public decimal MinPrice { get; set; }
    
    /// <summary>
    /// Maximum price across all ticket types
    /// </summary>
    public decimal MaxPrice { get; set; }
    
    /// <summary>
    /// Whether any ticket types are in RSVP mode (free)
    /// </summary>
    public bool HasFreeOptions { get; set; }
    
    /// <summary>
    /// Formatted price range for display
    /// </summary>
    public string PriceRangeDisplay => HasFreeOptions && MinPrice == 0 
        ? $"Free - ${MaxPrice:F0}" 
        : $"${MinPrice:F0} - ${MaxPrice:F0}";
}