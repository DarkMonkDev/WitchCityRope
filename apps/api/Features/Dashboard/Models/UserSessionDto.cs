namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Lightweight session info for user dashboard display
/// </summary>
public class UserSessionDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Session name (e.g., "Morning Session", "Day 1")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Session start time (UTC)
    /// CRITICAL: Frontend must convert to local timezone for display
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time (UTC)
    /// CRITICAL: Frontend must convert to local timezone for display
    /// </summary>
    public DateTime EndTime { get; set; }
}
