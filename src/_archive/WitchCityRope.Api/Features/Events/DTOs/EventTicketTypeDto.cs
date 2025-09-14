using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO representing a ticket type with session inclusion and pricing information
/// Used for event session matrix ticket configuration and purchasing
/// </summary>
public class EventTicketTypeDto
{
    /// <summary>
    /// Unique identifier for this ticket type
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Display name of the ticket type (e.g., "Single Person", "Couple", "Weekend Pass")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this ticket type includes
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of ticket (Single, Couples, etc.) - for UI categorization
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum price for sliding scale pricing
    /// </summary>
    public decimal MinPrice { get; set; }
    
    /// <summary>
    /// Maximum price for sliding scale pricing
    /// </summary>
    public decimal MaxPrice { get; set; }
    
    /// <summary>
    /// Formatted price range for display (e.g., "$75 - $150")
    /// </summary>
    public string PriceRange => $"${MinPrice:F0} - ${MaxPrice:F0}";
    
    /// <summary>
    /// Session identifiers included in this ticket type (e.g., ["S1", "S2"])
    /// </summary>
    public List<string> IncludedSessions { get; set; } = new();
    
    /// <summary>
    /// Number of tickets available (null = unlimited)
    /// </summary>
    public int? QuantityAvailable { get; set; }
    
    /// <summary>
    /// Number of tickets sold for this type
    /// </summary>
    public int TicketsSold { get; set; }
    
    /// <summary>
    /// Remaining tickets available for purchase (null if unlimited)
    /// </summary>
    public int? RemainingTickets => QuantityAvailable.HasValue 
        ? Math.Max(0, QuantityAvailable.Value - TicketsSold) 
        : null;
    
    /// <summary>
    /// Date when sales end for this ticket type (null = no end date)
    /// </summary>
    public DateTime? SalesEndDate { get; set; }
    
    /// <summary>
    /// Whether this is RSVP mode (no payment required)
    /// </summary>
    public bool IsRsvpMode { get; set; }
    
    /// <summary>
    /// Whether this ticket type is currently active and available for sale
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Whether this ticket type is currently on sale (active + within sales dates + has quantity)
    /// </summary>
    public bool IsCurrentlyOnSale { get; set; }
    
    /// <summary>
    /// Available spots for this ticket type based on session capacity constraints
    /// (minimum available spots across all included sessions)
    /// </summary>
    public int AvailableSpots { get; set; }
    
    /// <summary>
    /// Whether this ticket type has available capacity for purchase
    /// </summary>
    public bool HasAvailableCapacity { get; set; }
}