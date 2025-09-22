namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response for successful simplified vetting application submission
/// </summary>
public class SimplifiedApplicationResponse
{
    /// <summary>
    /// Unique identifier for the application
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Human-readable application number (VET-YYYYMMDD-NNNN)
    /// </summary>
    public string ApplicationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when application was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Confirmation message for the user
    /// </summary>
    public string ConfirmationMessage { get; set; } = string.Empty;

    /// <summary>
    /// Whether confirmation email was sent successfully
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// Next steps in the process
    /// </summary>
    public string NextSteps { get; set; } = string.Empty;

    /// <summary>
    /// Pronouns that were submitted (if provided)
    /// </summary>
    public string? Pronouns { get; set; }

    /// <summary>
    /// Other names that were submitted (if provided)
    /// </summary>
    public string? OtherNames { get; set; }
}