using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.Api.Features.Events.Services;

/// <summary>
/// Interface for managing event operations
/// Follows service layer pattern for testability (Pattern B compliance)
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Get all published events (public access)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, list of events, and error message</returns>
    Task<(bool Success, List<EventDto> Response, string Error)> GetPublishedEventsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events with optional filter for admin access
    /// </summary>
    /// <param name="includeUnpublished">Include draft/unpublished events (admin only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, list of events, and error message</returns>
    Task<(bool Success, List<EventDto> Response, string Error)> GetEventsAsync(
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single event by ID
    /// </summary>
    /// <param name="id">Event ID (string representation of Guid)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, event DTO if found, and error message</returns>
    Task<(bool Success, EventDto? Response, string Error)> GetEventAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <param name="id">Event ID (string representation of Guid)</param>
    /// <param name="request">Update request with modified event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, updated event DTO if successful, and error message</returns>
    Task<(bool Success, EventDto? Response, string Error)> UpdateEventAsync(
        string id,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default);
}
