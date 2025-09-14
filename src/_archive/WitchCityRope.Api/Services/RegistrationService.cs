using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Services
{
    public class RegistrationService : IRegistrationService
    {
        public async Task<EventRegistrationResponse> RegisterForEventAsync(Guid eventId, Guid userId, Core.DTOs.EventRegistrationRequest request)
        {
            // TODO: Implement event registration
            await Task.CompletedTask;
            return new EventRegistrationResponse
            {
                RegistrationId = Guid.NewGuid(),
                ConfirmationCode = $"CONF-{new Random().Next(100000, 999999)}",
                Status = "Confirmed",
                Message = "Registration successful"
            };
        }
    }
}