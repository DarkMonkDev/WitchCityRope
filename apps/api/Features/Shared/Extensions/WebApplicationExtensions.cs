using WitchCityRope.Api.Features.Health.Endpoints;
using WitchCityRope.Api.Features.Authentication.Endpoints;
using WitchCityRope.Api.Features.Events.Endpoints;
using WitchCityRope.Api.Features.Users.Endpoints;

namespace WitchCityRope.Api.Features.Shared.Extensions;

/// <summary>
/// Web application extensions for endpoint registration
/// Simple pattern for organizing endpoint registration by feature
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Register all feature endpoints in a clean, organized way
    /// Each feature handles its own endpoint registration
    /// </summary>
    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        // Health feature endpoints
        app.MapHealthEndpoints();

        // Authentication feature endpoints
        app.MapAuthenticationEndpoints();

        // Events feature endpoints
        app.MapEventEndpoints();

        // Users feature endpoints
        app.MapUserEndpoints();

        return app;
    }
}