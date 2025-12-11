namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test sessions programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestSessionRequest
{
    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Session identifier code (e.g., "S1", "Day1")
    /// Default: auto-generated
    /// </summary>
    public string? SessionCode { get; set; }

    /// <summary>
    /// Session name (e.g., "Morning Session")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Session start time in UTC
    /// </summary>
    public required DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time in UTC
    /// </summary>
    public required DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum capacity for this session
    /// Default: 20
    /// </summary>
    public int Capacity { get; set; } = 20;
}
