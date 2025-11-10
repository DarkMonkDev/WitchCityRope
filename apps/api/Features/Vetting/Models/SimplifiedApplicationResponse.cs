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

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Preferred scene name
    /// </summary>
    public string PreferredSceneName { get; set; } = string.Empty;

    /// <summary>
    /// FetLife handle (if provided)
    /// </summary>
    public string? FetLifeHandle { get; set; }

    /// <summary>
    /// Why user wants to join
    /// </summary>
    public string WhyJoin { get; set; } = string.Empty;

    /// <summary>
    /// Experience with rope bondage
    /// </summary>
    public string ExperienceWithRope { get; set; } = string.Empty;

    /// <summary>
    /// Whether user agreed to community standards
    /// </summary>
    public bool AgreeToCommunityStandards { get; set; }

    /// <summary>
    /// Application status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}