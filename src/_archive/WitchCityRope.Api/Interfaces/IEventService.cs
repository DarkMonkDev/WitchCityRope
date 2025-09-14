using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;
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
        Task<Core.Models.PagedResult<WitchCityRope.Api.Models.EventDto>> GetEventsAsync(int page, int pageSize, string? search);

        /// <summary>
        /// Retrieves an event by its ID
        /// </summary>
        Task<WitchCityRope.Api.Models.EventDto?> GetEventByIdAsync(Guid id);

        /// <summary>
        /// Creates a new event
        /// </summary>
        Task<Core.DTOs.CreateEventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId);

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
        
        // Event Session Management
        
        /// <summary>
        /// Creates a new event session
        /// </summary>
        Task<(bool Success, string Message, Features.Events.Models.EventSessionDto? Session)> CreateEventSessionAsync(Guid eventId, Features.Events.Models.CreateEventSessionRequest request);
        
        /// <summary>
        /// Updates an existing event session
        /// </summary>
        Task<(bool Success, string Message, Features.Events.Models.EventSessionDto? Session)> UpdateEventSessionAsync(Guid sessionId, Features.Events.Models.UpdateEventSessionRequest request);
        
        /// <summary>
        /// Deletes an event session
        /// </summary>
        Task<(bool Success, string Message)> DeleteEventSessionAsync(Guid sessionId);
        
        /// <summary>
        /// Gets all sessions for an event
        /// </summary>
        Task<ICollection<Features.Events.Models.EventSessionDto>> GetEventSessionsAsync(Guid eventId);
        
        // Event Ticket Type Management
        
        /// <summary>
        /// Creates a new event ticket type
        /// </summary>
        Task<(bool Success, string Message, Features.Events.Models.EventTicketTypeDto? TicketType)> CreateEventTicketTypeAsync(Guid eventId, Features.Events.Models.CreateEventTicketTypeRequest request);
        
        /// <summary>
        /// Updates an existing event ticket type
        /// </summary>
        Task<(bool Success, string Message, Features.Events.Models.EventTicketTypeDto? TicketType)> UpdateEventTicketTypeAsync(Guid ticketTypeId, Features.Events.Models.UpdateEventTicketTypeRequest request);
        
        /// <summary>
        /// Deletes an event ticket type
        /// </summary>
        Task<(bool Success, string Message)> DeleteEventTicketTypeAsync(Guid ticketTypeId);
        
        /// <summary>
        /// Gets all ticket types for an event
        /// </summary>
        Task<ICollection<Features.Events.Models.EventTicketTypeDto>> GetEventTicketTypesAsync(Guid eventId);
    }
}