namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Summary view of vetting application for admin grid
/// </summary>
public class VettingApplicationSummary
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? AdminNotes { get; set; }
}