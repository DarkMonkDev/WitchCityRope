using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// DTO for user segment information with count
/// </summary>
public class UserSegmentDto
{
    /// <summary>
    /// The segment identifier
    /// </summary>
    public UserSegment Segment { get; set; }

    /// <summary>
    /// Number of users in this segment
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Human-readable description of the segment
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Segment name (enum as string for frontend display)
    /// </summary>
    public string SegmentName { get; set; } = string.Empty;
}
