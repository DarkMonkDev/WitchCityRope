using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// User's vetting status for alert box display on dashboard
/// </summary>
public class VettingStatusDto
{
    /// <summary>
    /// Vetting status: "Pending", "Approved", "OnHold", "Denied", "Vetted"
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the vetting status was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Message to display in the alert box
    /// Status-specific messages for dashboard display
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL for interview scheduling (for Approved status)
    /// </summary>
    public string? InterviewScheduleUrl { get; set; }

    /// <summary>
    /// Optional URL for reapply information (for Denied status)
    /// </summary>
    public string? ReapplyInfoUrl { get; set; }
}
