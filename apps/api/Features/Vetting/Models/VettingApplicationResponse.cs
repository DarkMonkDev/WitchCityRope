namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response after submitting a vetting application
/// </summary>
public class VettingApplicationResponse
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string ConfirmationMessage { get; set; } = string.Empty;
    public string NextSteps { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
}