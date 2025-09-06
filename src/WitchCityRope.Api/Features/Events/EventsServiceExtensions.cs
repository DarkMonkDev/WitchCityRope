using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;

namespace WitchCityRope.Api.Features.Events;

public static class EventsServiceExtensions
{
    public static IServiceCollection AddEventsFeature(this IServiceCollection services)
    {
        // Register the legacy EventService (for backward compatibility)
        services.AddScoped<Interfaces.IEventService, EventService>();
        
        // Register the new EventsManagementService (for Event Session Matrix APIs)
        services.AddScoped<EventsManagementService>();

        return services;
    }
}