using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;

namespace WitchCityRope.Api.Features.Events;

public static class EventsServiceExtensions
{
    public static IServiceCollection AddEventsFeature(this IServiceCollection services)
    {
        // Register the EventService
        services.AddScoped<Interfaces.IEventService, EventService>();

        return services;
    }
}