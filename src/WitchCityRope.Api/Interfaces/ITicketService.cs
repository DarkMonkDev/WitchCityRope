using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for event ticket service operations
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        /// Purchases a ticket for an event
        /// </summary>
        Task<Core.DTOs.EventTicketResponse> PurchaseTicketAsync(Guid eventId, Guid userId, EventTicketRequest request);
        
        /// <summary>
        /// Gets all tickets for a user
        /// </summary>
        Task<List<Core.DTOs.TicketDto>> GetUserTicketsAsync(Guid userId);
    }
}