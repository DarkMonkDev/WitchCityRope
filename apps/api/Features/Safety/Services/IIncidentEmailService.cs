using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Email notifications for incident lifecycle events.
/// Handles sending emails to reporters and coordinators at key incident milestones.
/// Respects anonymity: anonymous reporters only receive emails if they provided
/// a contact email and requested follow-up.
/// </summary>
public interface IIncidentEmailService
{
    /// <summary>
    /// Send confirmation to reporter that their incident report was received.
    /// Skips if anonymous without follow-up contact info.
    /// </summary>
    Task SendReportReceivedAsync(SafetyIncident incident, CancellationToken ct = default);

    /// <summary>
    /// Notify coordinator they've been assigned to an incident.
    /// </summary>
    Task SendAssignmentNotificationAsync(SafetyIncident incident, Guid coordinatorId, CancellationToken ct = default);

    /// <summary>
    /// Notify reporter of a status change on their incident.
    /// Skips if anonymous without follow-up contact info.
    /// </summary>
    Task SendStatusUpdateAsync(SafetyIncident incident, string newStatus, CancellationToken ct = default);

    /// <summary>
    /// Notify reporter that their incident has been resolved/closed.
    /// Skips if anonymous without follow-up contact info.
    /// </summary>
    Task SendResolvedAsync(SafetyIncident incident, CancellationToken ct = default);
}
