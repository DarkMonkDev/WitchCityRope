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

    /// <summary>
    /// Navigation property to sessions
    /// </summary>
    public ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>
    /// Navigation property to ticket types
    /// </summary>
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    /// <summary>
    /// Gets the current number of confirmed attendees based on CORRECT business logic:
    /// - Social Events: CurrentAttendees = RSVPs (everyone must RSVP to attend, tickets are optional donations)
    /// - Class Events: CurrentAttendees = Tickets (only paid tickets, no RSVPs)
    /// 
    /// Returns varied capacity states for frontend testing:
    /// - Some events SOLD OUT (100% capacity) - shows green progress bar
    /// - Some events nearly sold out (85-95%) - shows green progress bar  
    /// - Some events moderately filled (50-80%) - shows yellow progress bar
    /// - Some events low attendance (<50%) - shows red progress bar
    /// </summary>
    public int GetCurrentAttendeeCount()
    {
        if (EventType == "Social")
        {
            // Social events: Attendees = RSVPs (primary attendance metric)
            return GetCurrentRSVPCount();
        }
        else // Class
        {
            // Class events: Attendees = Tickets (only paid tickets)
            return GetCurrentTicketCount();
        }
    }

    /// <summary>
    /// Gets the current number of RSVPs for Social events (mock implementation)
    /// Social Events: Everyone must RSVP to attend (this is the primary attendance count)
    /// Class Events: No RSVPs, returns 0
    /// TODO: This should query the actual RSVP table
    /// </summary>
    public int GetCurrentRSVPCount()
    {
        // Only Social events have RSVPs
        if (EventType != "Social") return 0;
        
        // Generate deterministic mock RSVP count for Social events
        // This represents the PRIMARY attendance metric for Social events
        var seed = Id.GetHashCode() % 10; // Get a number 0-9 based on ID
        
        return seed switch
        {
            // 20% of events - SOLD OUT (100% capacity)
            0 or 1 => Capacity,
            
            // 30% of events - Nearly sold out (85-95%)
            2 or 3 or 4 => (int)(Capacity * (0.85 + (seed * 0.02))), // 85%, 87%, 89%
            
            // 30% of events - Moderately filled (50-80%)
            5 or 6 or 7 => (int)(Capacity * (0.50 + (seed * 0.05))), // 50%, 55%, 60%
            
            // 20% of events - Low attendance (20-45%)
            8 or 9 => (int)(Capacity * (0.20 + (seed * 0.025))), // 20%, 22.5%
            
            _ => (int)(Capacity * 0.6) // Fallback to 60%
        };
    }

    /// <summary>
    /// Gets the current number of paid ticket registrations (mock implementation)
    /// Social Events: Optional tickets as donations/support (subset of RSVPs)
    /// Class Events: Required tickets (this is the primary attendance count)
    /// TODO: This should query the actual Registration table
    /// </summary>
    public int GetCurrentTicketCount()
    {
        if (EventType == "Class")
        {
            // Classes require paid tickets - this is the PRIMARY attendance metric
            var seed = Id.GetHashCode() % 10; // Get a number 0-9 based on ID
            
            return seed switch
            {
                // 20% of events - SOLD OUT (100% capacity)
                0 or 1 => Capacity,
                
                // 30% of events - Nearly sold out (85-95%)
                2 or 3 or 4 => (int)(Capacity * (0.85 + (seed * 0.02))), // 85%, 87%, 89%
                
                // 30% of events - Moderately filled (50-80%)
                5 or 6 or 7 => (int)(Capacity * (0.50 + (seed * 0.05))), // 50%, 55%, 60%
                
                // 20% of events - Low attendance (20-45%)
                8 or 9 => (int)(Capacity * (0.20 + (seed * 0.025))), // 20%, 22.5%
                
                _ => (int)(Capacity * 0.6) // Fallback to 60%
            };
        }
        else // Social events
        {
            // Social events: Some RSVPed people also buy tickets as optional support
            // About 30% of RSVPs also buy tickets as donations
            var rsvpCount = GetCurrentRSVPCount();
            return (int)(rsvpCount * 0.3);
        }
    }
}