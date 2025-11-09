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
    /// Number of tickets sold for this ticket type
    /// </summary>
    public int QuantitySold { get; set; }

    /// <summary>
    /// When sales for this ticket type end
    /// </summary>
    public DateTime? SalesEndDate { get; set; }

    /// <summary>
    /// Constructor to map from TicketType entity
    /// </summary>
    /// <param name="ticketType">The ticket type to map from</param>
    /// <param name="eventParticipations">Optional event participations for status checking</param>
    public TicketTypeDto(
        WitchCityRope.Api.Models.TicketType ticketType,
        IEnumerable<WitchCityRope.Api.Features.Participation.Entities.EventParticipation>? eventParticipations = null)
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

        // Calculate QuantitySold dynamically from actual ticket purchases (not stored Sold column)
        // Business Rule: QuantitySold = count of unique registered attendees with active participations
        // NOT total quantity across all purchases (one user buying multiple tickets counts as 1 sold)
        // CRITICAL: Exclude cancelled/refunded tickets by checking EventParticipation.Status
        // Only count Active participations (status = 1), exclude Cancelled (2), Refunded (3), Waitlisted (4)
        if (eventParticipations != null)
        {
            // When event participations are provided, count unique users with active participations
            // who have completed at least one payment for this ticket type
            var participationLookup = eventParticipations
                .Where(ep => ep.Status == WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active)
                .Select(ep => ep.UserId)
                .ToHashSet();

            // Count unique users with completed purchases, not total quantity
            // This represents actual registered attendees, not total tickets bought
            QuantitySold = ticketType.Purchases
                .Where(p =>
                    p.IsPaymentCompleted &&
                    participationLookup.Contains(p.UserId))
                .Select(p => p.UserId)
                .Distinct()
                .Count();
        }
        else
        {
            // Fallback: If no participations provided, count unique users with completed payments
            // Changed from Sum(p.Quantity) to match business logic of counting unique attendees
            QuantitySold = ticketType.Purchases
                .Where(p => p.IsPaymentCompleted)
                .Select(p => p.UserId)
                .Distinct()
                .Count();
        }

        SalesEndDate = null; // Not currently tracked in the entity

        // Determine session identifiers based on the session relationship
        if (ticketType.Session != null)
        {
            // Single-session ticket
            SessionIdentifiers = new List<string> { ticketType.Session.SessionCode };
        }
        else if (ticketType.Event?.Sessions != null && ticketType.Event.Sessions.Any())
        {
            // Multi-session ticket - includes all sessions from the event
            SessionIdentifiers = ticketType.Event.Sessions
                .OrderBy(s => s.StartTime)
                .Select(s => s.SessionCode)
                .ToList();
        }
        else
        {
            // Fallback: no sessions available
            SessionIdentifiers = new List<string>();
        }
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public TicketTypeDto() { }
}