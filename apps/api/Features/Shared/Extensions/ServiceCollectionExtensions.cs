using WitchCityRope.Api.Features.Health.Services;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Users.Services;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Shared.Extensions;

/// <summary>
/// Service registration extensions for clean Program.cs configuration
/// Simple pattern - NO complex DI container patterns or MediatR registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all feature services in one method for clean Program.cs
    /// Each feature registers its own services directly
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Health feature services
        services.AddScoped<HealthService>();

        // Authentication feature services
        services.AddScoped<AuthenticationService>();

        // Events feature services
        services.AddScoped<Events.Services.EventService>();

        // Users feature services
        services.AddScoped<UserManagementService>();

        // Database initialization services
        services.AddScoped<ISeedDataService, SeedDataService>();
        services.AddHostedService<DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// Register endpoint discovery for clean Program.cs
    /// Simple pattern to find and register all feature endpoints
    /// </summary>
    public static IServiceCollection AddFeatureEndpoints(this IServiceCollection services)
    {
        // No complex reflection or assembly scanning
        // Each feature's endpoints will be registered manually in Program.cs for clarity
        return services;
    }
}