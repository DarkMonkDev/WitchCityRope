namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test event creation
/// Contains event ID for test cleanup
/// </summary>
public class TestEventResponse
{
    /// <summary>
    /// Created event's ID (GUID)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Event title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Event start date/time
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date/time
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Event status
    /// </summary>
    public required string Status { get; set; }
}
