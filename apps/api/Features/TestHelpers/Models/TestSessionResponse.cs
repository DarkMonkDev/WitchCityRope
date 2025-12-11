namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test session creation
/// Contains session ID for test cleanup
/// </summary>
public class TestSessionResponse
{
    /// <summary>
    /// Created session's ID (GUID)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Session name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Session start time
    /// </summary>
    public required DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time
    /// </summary>
    public required DateTime EndTime { get; set; }
}
