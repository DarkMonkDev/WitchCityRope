namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response for current user's application status check
/// </summary>
public class MyApplicationStatusResponse
{
    /// <summary>
    /// Whether the user has an existing application
    /// </summary>
    public bool HasApplication { get; set; }

    /// <summary>
    /// Application details if one exists
    /// </summary>
    public ApplicationStatusInfo? Application { get; set; }
}

/// <summary>
/// Basic application status information for the user's own application
/// </summary>
public class ApplicationStatusInfo
{
    /// <summary>
    /// Application unique identifier
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Human-readable application number
    /// </summary>
    public string ApplicationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the application
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// When the application was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// When the status was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// What the user should do next, if anything
    /// </summary>
    public string? NextSteps { get; set; }

    /// <summary>
    /// Estimated days remaining in review process
    /// </summary>
    public int? EstimatedDaysRemaining { get; set; }
}