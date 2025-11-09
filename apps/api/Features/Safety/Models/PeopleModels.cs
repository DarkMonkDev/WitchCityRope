namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to update involved parties and witnesses for an incident
/// </summary>
public class UpdatePeopleRequest
{
    /// <summary>
    /// Involved parties (newline-separated names, NULL to clear)
    /// </summary>
    public string? InvolvedParties { get; set; }

    /// <summary>
    /// Witnesses (newline-separated names, NULL to clear)
    /// </summary>
    public string? Witnesses { get; set; }
}

/// <summary>
/// Response after updating people involved in incident
/// </summary>
public class UpdatePeopleResponse
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated involved parties
    /// </summary>
    public string? InvolvedParties { get; set; }

    /// <summary>
    /// Updated witnesses
    /// </summary>
    public string? Witnesses { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Whether system note was created
    /// </summary>
    public bool SystemNoteCreated { get; set; }
}
