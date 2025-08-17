using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Service interface for event operations
/// Follows vertical slice architecture for POC
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Retrieve published events for display
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of events for display</returns>
    Task<IEnumerable<EventDto>> GetPublishedEventsAsync(CancellationToken cancellationToken = default);
}