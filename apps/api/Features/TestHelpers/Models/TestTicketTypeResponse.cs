namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test ticket type creation
/// Contains ticket type ID for test cleanup
/// </summary>
public class TestTicketTypeResponse
{
    /// <summary>
    /// Created ticket type's ID (GUID)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Ticket type name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Ticket price
    /// </summary>
    public required decimal Price { get; set; }

    /// <summary>
    /// Number of tickets available
    /// </summary>
    public required int Available { get; set; }
}
