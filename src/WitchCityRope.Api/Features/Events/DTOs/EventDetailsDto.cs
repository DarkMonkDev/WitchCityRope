using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.DTOs;

/// <summary>
/// DTO for complete event details including sessions and ticket types
/// Matches requirements from backend-integration-requirements.md
/// </summary>
public class EventDetailsDto
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
    /// Full HTML description of the event
    /// </summary>
    public string FullDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Event type (Class, SocialEvent)
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Policies and procedures for the event
    /// </summary>
    public string PoliciesProcedures { get; set; } = string.Empty;
    
    /// <summary>
    /// Venue information
    /// </summary>
    public VenueDto Venue { get; set; } = new();
    
    /// <summary>
    /// Event organizers
    /// </summary>
    public List<EventOrganizerDto> Organizers { get; set; } = new();
    
    /// <summary>
    /// Sessions within this event
    /// </summary>
    public List<EventSessionDto> Sessions { get; set; } = new();
    
    /// <summary>
    /// Ticket types available for this event
    /// </summary>
    public List<TicketTypeDto> TicketTypes { get; set; } = new();
    
    /// <summary>
    /// Whether the event is published
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

/// <summary>
/// DTO for venue information
/// </summary>
public class VenueDto
{
    /// <summary>
    /// Venue name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Venue address
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum capacity of the venue
    /// </summary>
    public int Capacity { get; set; }
}

/// <summary>
/// DTO for event organizer information
/// </summary>
public class EventOrganizerDto
{
    /// <summary>
    /// User ID of the organizer
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Display name of the organizer
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role of the organizer (Primary, Secondary)
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO for ticket type information
/// </summary>
public class TicketTypeDto
{
    /// <summary>
    /// Unique identifier for the ticket type
    /// </summary>
    public Guid TicketTypeId { get; set; }
    
    /// <summary>
    /// Name of the ticket type
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this ticket type includes
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// List of session identifiers included in this ticket (S1, S2, etc.)
    /// </summary>
    public List<string> IncludedSessions { get; set; } = new();
    
    /// <summary>
    /// Regular price for this ticket type
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Member price (if applicable)
    /// </summary>
    public decimal? MemberPrice { get; set; }
    
    /// <summary>
    /// Maximum quantity available for this ticket type
    /// </summary>
    public int MaxQuantity { get; set; }
    
    /// <summary>
    /// Currently available quantity
    /// </summary>
    public int AvailableQuantity { get; set; }
    
    /// <summary>
    /// When sales end for this ticket type
    /// </summary>
    public DateTime SalesEndDate { get; set; }
    
    /// <summary>
    /// Whether this ticket type is currently available for purchase
    /// </summary>
    public bool IsAvailable { get; set; }
    
    /// <summary>
    /// Reason why ticket type is not available (if applicable)
    /// </summary>
    public string? ConstraintReason { get; set; }
}