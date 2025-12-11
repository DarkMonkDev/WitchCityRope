namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test volunteer position creation
/// Contains position ID for test cleanup
/// </summary>
public class TestVolunteerPositionResponse
{
    /// <summary>
    /// Created position's ID (GUID)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Position title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Number of slots needed
    /// </summary>
    public required int SlotsNeeded { get; set; }

    /// <summary>
    /// Number of slots filled
    /// </summary>
    public required int SlotsFilled { get; set; }
}
