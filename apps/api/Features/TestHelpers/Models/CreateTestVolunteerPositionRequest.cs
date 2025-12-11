namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test volunteer positions programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestVolunteerPositionRequest
{
    /// <summary>
    /// Parent event ID
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Position title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Description of the volunteer role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of volunteer slots needed
    /// Default: 3
    /// </summary>
    public int SlotsNeeded { get; set; } = 3;

    /// <summary>
    /// Number of volunteer slots already filled
    /// Default: 0
    /// </summary>
    public int SlotsFilled { get; set; } = 0;

    /// <summary>
    /// Whether position is visible on public event page
    /// Default: true
    /// </summary>
    public bool IsPublicFacing { get; set; } = true;

    /// <summary>
    /// Specific session ID (optional, null for event-wide positions)
    /// </summary>
    public Guid? SessionId { get; set; }
}
