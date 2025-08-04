using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for RSVP service operations for social events
    /// </summary>
    public interface IRsvpService
    {
        /// <summary>
        /// Creates an RSVP for a social event
        /// </summary>
        Task<RsvpResponse> CreateRsvpAsync(Guid eventId, Guid userId, RsvpRequest request);

        /// <summary>
        /// Updates an existing RSVP
        /// </summary>
        Task<RsvpResponse> UpdateRsvpAsync(Guid rsvpId, Guid userId, RsvpUpdateRequest request);

        /// <summary>
        /// Cancels an RSVP
        /// </summary>
        Task<bool> CancelRsvpAsync(Guid rsvpId, Guid userId);

        /// <summary>
        /// Gets RSVPs for a specific event
        /// </summary>
        Task<IEnumerable<RsvpDto>> GetEventRsvpsAsync(Guid eventId);

        /// <summary>
        /// Gets RSVPs for a specific user
        /// </summary>
        Task<IEnumerable<RsvpDto>> GetUserRsvpsAsync(Guid userId);

        /// <summary>
        /// Checks if a user has RSVPed to an event
        /// </summary>
        Task<RsvpDto?> GetUserEventRsvpAsync(Guid eventId, Guid userId);
    }
}