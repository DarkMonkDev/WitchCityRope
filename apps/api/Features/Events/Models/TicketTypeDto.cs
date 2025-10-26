using WitchCityRope.Models;

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
    /// Pricing type: Fixed for fixed price, SlidingScale for pay-what-you-can
    /// </summary>
    public PricingType PricingType { get; set; } = PricingType.Fixed;

    /// <summary>
    /// List of session identifiers this ticket includes access to
    /// </summary>
    public List<string> SessionIdentifiers { get; set; } = new List<string>();

    /// <summary>
    /// Fixed price (for fixed pricing type)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Minimum price for sliding scale pricing
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price for sliding scale pricing
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Default/suggested price for sliding scale pricing
    /// </summary>
    public decimal? DefaultPrice { get; set; }

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
        PricingType = ticketType.PricingType;

        // Map pricing fields based on pricing type
        if (ticketType.PricingType == PricingType.SlidingScale)
        {
            MinPrice = ticketType.MinPrice;
            MaxPrice = ticketType.MaxPrice;
            DefaultPrice = ticketType.DefaultPrice;
            Price = null;
        }
        else // Fixed
        {
            Price = ticketType.Price;
            MinPrice = null;
            MaxPrice = null;
            DefaultPrice = null;
        }

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