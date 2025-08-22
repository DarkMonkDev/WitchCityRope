using WitchCityRope.Api.Features.Health.Endpoints;

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

        // Future feature endpoints will be added here:
        // app.MapAuthenticationEndpoints();
        // app.MapEventEndpoints();
        // app.MapUserEndpoints();

        return app;
    }
}