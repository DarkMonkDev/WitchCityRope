namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test ticket types programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestTicketTypeRequest
{
    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Ticket type name (e.g., "Early Bird", "Regular")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of what this ticket includes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fixed price for this ticket type
    /// </summary>
    public required decimal Price { get; set; }

    /// <summary>
    /// Pricing type (Fixed = 0, SlidingScale = 1)
    /// Default: Fixed
    /// </summary>
    public int PricingType { get; set; } = 0;

    /// <summary>
    /// Number of tickets available
    /// Default: 100
    /// </summary>
    public int Available { get; set; } = 100;

    /// <summary>
    /// Session IDs this ticket covers (optional)
    /// Can be empty for event-wide tickets
    /// </summary>
    public List<Guid>? SessionIds { get; set; }
}
