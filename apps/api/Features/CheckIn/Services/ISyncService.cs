using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Service for handling offline synchronization
/// Critical for mobile reliability at events
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Process pending offline check-ins
    /// Handles conflict resolution and data integrity
    /// </summary>
    Task<Result<SyncResponse>> ProcessOfflineSyncAsync(
        SyncRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue action for offline processing
    /// Stores actions when connectivity is lost
    /// </summary>
    Task<Result<string>> QueueOfflineActionAsync(
        Guid eventId,
        Guid userId,
        string actionType,
        object actionData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending sync count for user
    /// Used for UI indicators
    /// </summary>
    Task<Result<int>> GetPendingSyncCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}