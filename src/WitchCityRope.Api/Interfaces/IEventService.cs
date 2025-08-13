using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Models;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for event management service operations
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Retrieves a paginated list of events
        /// </summary>
        Task<Core.Models.PagedResult<Core.DTOs.EventDto>> GetEventsAsync(int page, int pageSize, string? search);

        /// <summary>
        /// Retrieves an event by its ID
        /// </summary>
        Task<Core.DTOs.EventDto?> GetEventByIdAsync(Guid id);

        /// <summary>
        /// Creates a new event
        /// </summary>
        Task<Core.DTOs.CreateEventResponse> CreateEventAsync(Core.DTOs.CreateEventRequest request, Guid organizerId);

        /// <summary>
        /// Updates an existing event
        /// </summary>
        Task UpdateEventAsync(Guid id, Core.DTOs.UpdateEventRequest request, Guid userId);

        /// <summary>
        /// Lists events with filtering and pagination
        /// </summary>
        Task<Features.Events.Models.ListEventsResponse> ListEventsAsync(Features.Events.Models.ListEventsRequest request);

        /// <summary>
        /// Gets featured events for homepage
        /// </summary>
        Task<Features.Events.Models.GetFeaturedEventsResponse> GetFeaturedEventsAsync(Features.Events.Models.GetFeaturedEventsRequest request);

        /// <summary>
        /// Registers a user for an event
        /// </summary>
        Task<Features.Events.Models.RegisterForEventResponse> RegisterForEventAsync(Features.Events.Models.RegisterForEventRequest request);
    }
}