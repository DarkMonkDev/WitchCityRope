using System;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for event check-in service operations
    /// </summary>
    public interface ICheckInService
    {
        /// <summary>
        /// Checks in an attendee to an event
        /// </summary>
        Task<CheckInResponse> CheckInAttendeeAsync(Guid eventId, Core.DTOs.CheckInRequest request, Guid staffId);
    }
}