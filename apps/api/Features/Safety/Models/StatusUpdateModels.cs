using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to update incident status
/// </summary>
public class UpdateStatusRequest
{
    /// <summary>
    /// New status
    /// </summary>
    public IncidentStatus NewStatus { get; set; }

    /// <summary>
    /// Optional reason/note (recommended for OnHold status)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional metadata for status change
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response after status update
/// </summary>
public class StatusUpdateResponse
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated status
    /// </summary>
    public IncidentStatus Status { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Whether system note was created
    /// </summary>
    public bool SystemNoteCreated { get; set; }
}
