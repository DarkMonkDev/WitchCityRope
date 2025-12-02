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
    /// True if ticket can be purchased right now.
    /// Based on: at least one future session exists AND within sales window for first future session.
    /// </summary>
    public bool CanPurchase { get; set; }

    /// <summary>
    /// The session ID used for timing calculations (first future session).
    /// Null if all sessions have passed.
    /// </summary>
    public string? ReferenceSessionId { get; set; }

    /// <summary>
    /// Session name for the reference session (for display).
    /// </summary>
    public string? ReferenceSessionName { get; set; }

    /// <summary>
    /// Message explaining availability status.
    /// Examples: "Available", "Sales open Dec 1", "Sales closed", "All sessions passed"
    /// </summary>
    public string AvailabilityMessage { get; set; } = "Available";

    /// <summary>
    /// True if ticket can be cancelled right now (for users who purchased).
    /// Based on cancellation window for reference session.
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Constructor to map from TicketType entity
    /// </summary>
    /// <param name="ticketType">The ticket type to map from</param>
    /// <param name="eventAttendances">Optional event attendances for status checking</param>
    public TicketTypeDto(
        WitchCityRope.Api.Models.TicketType ticketType,
        IEnumerable<WitchCityRope.Api.Features.Participation.Entities.EventAttendance>? eventAttendances = null)
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
        // Business Rule: QuantitySold = count of unique registered attendees with active attendances
        // NOT total quantity across all purchases (one user buying multiple tickets counts as 1 sold)
        // CRITICAL: Exclude cancelled/refunded tickets by checking EventAttendance.Status
        // Only count Active attendances (status = 1), exclude Cancelled (2), Refunded (3), Waitlisted (4)
        if (eventAttendances != null)
        {
            // When event attendances are provided, count unique users with active attendances
            // who have completed at least one payment for this ticket type
            var attendanceLookup = eventAttendances
                .Where(ea => ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active)
                .Select(ea => ea.UserId)
                .ToHashSet();

            // Count unique users with completed purchases, not total quantity
            // This represents actual registered attendees, not total tickets bought
            QuantitySold = ticketType.Purchases
                .Where(p =>
                    p.IsPaymentCompleted &&
                    attendanceLookup.Contains(p.UserId))
                .Select(p => p.UserId)
                .Distinct()
                .Count();
        }
        else
        {
            // Fallback: If no attendances provided, count unique users with completed payments
            // Changed from Sum(p.Quantity) to match business logic of counting unique attendees
            QuantitySold = ticketType.Purchases
                .Where(p => p.IsPaymentCompleted)
                .Select(p => p.UserId)
                .Distinct()
                .Count();
        }

        // Determine session identifiers based on the session relationship (many-to-many)
        if (ticketType.Sessions != null && ticketType.Sessions.Any())
        {
            // Ticket includes specific session(s)
            SessionIdentifiers = ticketType.Sessions
                .OrderBy(s => s.StartTime)
                .Select(s => s.SessionCode)
                .ToList();
        }
        else if (ticketType.Event?.Sessions != null && ticketType.Event.Sessions.Any())
        {
            // Ticket has no specific sessions - includes all sessions from the event
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