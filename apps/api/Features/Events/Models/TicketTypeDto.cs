namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for TicketType information within events.
/// Used to represent different ticket options including single-session and multi-session packages.
/// </summary>
public class TicketTypeDto
{
    /// <summary>
    /// Unique ticket type identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Ticket type name (e.g., "Early Bird", "Regular", "Day 1", "Full Event")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of ticket (e.g., "single-session", "multi-session", "rsvp")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// List of session identifiers this ticket includes access to
    /// </summary>
    public List<string> SessionIdentifiers { get; set; } = new List<string>();

    /// <summary>
    /// Minimum price for sliding scale pricing
    /// </summary>
    public decimal MinPrice { get; set; }

    /// <summary>
    /// Maximum price (suggested donation amount)
    /// </summary>
    public decimal MaxPrice { get; set; }

    /// <summary>
    /// Total quantity available for this ticket type
    /// </summary>
    public int QuantityAvailable { get; set; }

    /// <summary>
    /// When sales for this ticket type end
    /// </summary>
    public DateTime? SalesEndDate { get; set; }

    /// <summary>
    /// Constructor to map from TicketType entity
    /// </summary>
    public TicketTypeDto(WitchCityRope.Api.Models.TicketType ticketType)
    {
        Id = ticketType.Id.ToString();
        Name = ticketType.Name;
        Type = ticketType.IsRsvpMode ? "rsvp" : "paid";
        MinPrice = ticketType.Price;
        MaxPrice = ticketType.Price; // For now, same as min price
        QuantityAvailable = ticketType.Available;
        SalesEndDate = null; // Not currently tracked in the entity

        // Determine session identifiers based on the session relationship
        if (ticketType.Session != null)
        {
            SessionIdentifiers = new List<string> { ticketType.Session.SessionCode };
        }
        else
        {
            // Multi-session ticket - would need to be determined based on business logic
            SessionIdentifiers = new List<string>(); // Empty for now
        }
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public TicketTypeDto() { }
}