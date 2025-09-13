using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request model for submitting new safety incident
/// </summary>
public class CreateIncidentRequest
{
    /// <summary>
    /// Reporter user ID (null for anonymous reports)
    /// </summary>
    public Guid? ReporterId { get; set; }

    /// <summary>
    /// Incident severity level
    /// </summary>
    public IncidentSeverity Severity { get; set; }

    /// <summary>
    /// When the incident occurred
    /// </summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// Location where incident occurred
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the incident
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Information about involved parties (optional)
    /// </summary>
    public string? InvolvedParties { get; set; }

    /// <summary>
    /// Witness information (optional)
    /// </summary>
    public string? Witnesses { get; set; }

    /// <summary>
    /// Whether this is an anonymous report
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// Whether reporter requests follow-up contact
    /// </summary>
    public bool RequestFollowUp { get; set; }

    /// <summary>
    /// Contact email for identified reports
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone for identified reports (optional)
    /// </summary>
    public string? ContactPhone { get; set; }
}