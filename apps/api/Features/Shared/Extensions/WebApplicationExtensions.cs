using WitchCityRope.Api.Features.Health.Endpoints;
using WitchCityRope.Api.Features.Authentication.Endpoints;
using WitchCityRope.Api.Features.Events.Endpoints;
using WitchCityRope.Api.Features.Users.Endpoints;
using WitchCityRope.Api.Features.Dashboard.Endpoints;
using WitchCityRope.Api.Features.Safety.Endpoints;
using WitchCityRope.Api.Features.CheckIn.Endpoints;
using WitchCityRope.Api.Features.Participation.Endpoints;
using WitchCityRope.Api.Features.Vetting.Endpoints;
using WitchCityRope.Api.Features.TestHelpers.Endpoints;

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

        // User Dashboard endpoints (wireframe v4 implementation)
        app.MapUserDashboardEndpoints();

        // Safety feature endpoints
        app.MapSafetyEndpoints();

        // CheckIn feature endpoints
        app.MapCheckInEndpoints();

        // Participation feature endpoints
        app.MapParticipationEndpoints();

        // Vetting feature endpoints
        app.MapVettingEndpoints();

        // Test Helper endpoints (Development/Test only)
        app.MapTestHelperEndpoints();

        return app;
    }
}