using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for event registration service operations
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// Registers a user for an event
        /// </summary>
        Task<EventRegistrationResponse> RegisterForEventAsync(Guid eventId, Guid userId, Core.DTOs.EventRegistrationRequest request);
    }
}