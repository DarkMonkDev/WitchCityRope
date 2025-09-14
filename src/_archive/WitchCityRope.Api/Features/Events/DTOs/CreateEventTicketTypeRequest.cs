using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// Request model for creating a new event ticket type
/// </summary>
public class CreateEventTicketTypeRequest
{
    /// <summary>
    /// Display name of the ticket type (e.g., "Single Person", "Couple")
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Ticket type name is required and must be 1-100 characters")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this ticket type includes
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Description is required and must be 1-500 characters")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Type category (Single, Couples, etc.)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Type is required and must be 1-50 characters")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum price for sliding scale pricing
    /// Must be >= 0 and <= MaxPrice
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum price cannot be negative")]
    public decimal MinPrice { get; set; }
    
    /// <summary>
    /// Maximum price for sliding scale pricing
    /// Must be >= MinPrice
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Maximum price cannot be negative")]
    public decimal MaxPrice { get; set; }
    
    /// <summary>
    /// Session identifiers included in this ticket type (e.g., ["S1", "S2"])
    /// Must contain at least one valid session identifier
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Ticket type must include at least one session")]
    public List<string> IncludedSessions { get; set; } = new();
    
    /// <summary>
    /// Number of tickets available (null = unlimited)
    /// </summary>
    public int? QuantityAvailable { get; set; }
    
    /// <summary>
    /// Date when sales end for this ticket type (null = no end date)
    /// </summary>
    public DateTime? SalesEndDate { get; set; }
    
    /// <summary>
    /// Whether this is RSVP mode (no payment required)
    /// </summary>
    public bool IsRsvpMode { get; set; } = false;
}