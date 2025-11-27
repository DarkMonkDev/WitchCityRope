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
    /// Get all events with optional filters for admin access and past events
    /// </summary>
    /// <param name="includeUnpublished">Include draft/unpublished events (admin only)</param>
    /// <param name="includePastEvents">Include past events (last 90 days for public access)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, list of events, and error message</returns>
    Task<(bool Success, List<EventDto> Response, string Error)> GetEventsAsync(
        bool includeUnpublished = false,
        bool includePastEvents = false,
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

    /// <summary>
    /// Creates a copy of an existing event with a new date and title.
    /// Deep copies all related entities (sessions, ticket types, volunteer positions, organizers, email templates).
    /// Excludes attendance and transaction data (RSVPs, ticket purchases).
    /// New event is created as draft (IsPublished = false).
    /// </summary>
    /// <param name="eventId">ID of the event to copy (string representation of Guid)</param>
    /// <param name="request">Copy parameters including new date and title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, copied event DTO if successful, and error message</returns>
    Task<(bool Success, EventDto? Response, string? Error)> CopyEventAsync(
        string eventId,
        CopyEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new event with optional sessions, ticket types, volunteer positions, and organizers
    /// </summary>
    /// <param name="request">Create request with event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple with success flag, created event DTO if successful, and error message</returns>
    Task<(bool Success, EventDto? Response, string Error)> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default);
}
