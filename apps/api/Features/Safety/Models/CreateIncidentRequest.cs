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
    /// Short descriptive title for the incident
    /// </summary>
    public string Title { get; set; } = string.Empty;

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
    /// Contact name for identified reports
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Type of incident being reported
    /// </summary>
    public IncidentType Type { get; set; }

    /// <summary>
    /// Where the incident occurred
    /// </summary>
    public WhereOccurred WhereOccurred { get; set; }

    /// <summary>
    /// Event name if incident occurred at an event (optional)
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Whether reporter has spoken to the person involved
    /// </summary>
    public SpokenToPersonStatus? HasSpokenToPerson { get; set; }

    /// <summary>
    /// Reporter's desired outcomes (free-text)
    /// </summary>
    public string? DesiredOutcomes { get; set; }

    /// <summary>
    /// Reporter's preference for future interactions with involved person
    /// </summary>
    public string? FutureInteractionPreference { get; set; }

    /// <summary>
    /// Whether reporter wants to remain anonymous during the investigation
    /// </summary>
    public bool? AnonymousDuringInvestigation { get; set; }

    /// <summary>
    /// Whether reporter wants to remain anonymous in the final report
    /// </summary>
    public bool? AnonymousInFinalReport { get; set; }
}