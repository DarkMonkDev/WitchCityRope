using WitchCityRope.Api.Features.Health.Endpoints;
using WitchCityRope.Api.Features.Authentication.Endpoints;
using WitchCityRope.Api.Features.Events.Endpoints;
using WitchCityRope.Api.Features.Users.Endpoints;
using WitchCityRope.Api.Features.Dashboard.Endpoints;
using WitchCityRope.Api.Features.Safety.Endpoints;
using WitchCityRope.Api.Features.CheckIn.Endpoints;
using WitchCityRope.Api.Features.AuthorizedContacts.Endpoints;
using WitchCityRope.Api.Features.Participation.Endpoints;
using WitchCityRope.Api.Features.ProxyRsvp.Endpoints;
using WitchCityRope.Api.Features.TicketAssignment.Endpoints;
using WitchCityRope.Api.Features.Vetting.Endpoints;
using WitchCityRope.Api.Features.VettingHold.Endpoints;
using WitchCityRope.Api.Features.TestHelpers.Endpoints;
using WitchCityRope.Api.Features.Volunteers.Endpoints;
using WitchCityRope.Api.Features.Cms;
using WitchCityRope.Api.Features.Metadata.Endpoints;
using WitchCityRope.Api.Features.Admin.Settings.Endpoints;
using WitchCityRope.Api.Features.EmailTemplates.Endpoints;
using WitchCityRope.Api.Features.Payments.Endpoints;
using WitchCityRope.Api.Features.Reports.Endpoints;
using WitchCityRope.Api.Features.Logging.Endpoints;
using WitchCityRope.Api.Endpoints.Admin;
using WitchCityRope.Api.Endpoints;

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

        // Metadata feature endpoints
        app.MapMetadataEndpoints();

        // Authentication feature endpoints
        app.MapAuthenticationEndpoints();

        // Events feature endpoints
        app.MapEventEndpoints();

        // Users feature endpoints
        app.MapUserEndpoints();

        // User Dashboard endpoints (wireframe v4 implementation)
        app.MapUserDashboardEndpoints();

        // Admin feature endpoints
        app.MapSettingsEndpoints();
        app.MapVenueEndpoints(); // Admin venue endpoints
        app.MapAdminPaymentEndpoints(); // Admin payment transaction endpoints
        app.MapAdminReportEndpoints(); // Admin report/analytics endpoints

        // Public venue endpoints (authenticated users)
        app.MapPublicVenueEndpoints();

        // Safety feature endpoints
        app.MapSafetyEndpoints();

        // CheckIn feature endpoints
        app.MapCheckInEndpoints();

        // Authorized Contacts feature endpoints (ticket assignment & proxy RSVP delegation)
        app.MapAuthorizedContactEndpoints();

        // Participation feature endpoints
        app.MapParticipationEndpoints();

        // Proxy RSVP feature endpoints (delegate RSVP on behalf of principals)
        app.MapProxyRsvpEndpoints();

        // Ticket Assignment feature endpoints (assign, accept, decline, reassign)
        app.MapTicketAssignmentEndpoints();

        // Vetting feature endpoints
        app.MapVettingEndpoints();

        // VettingHold feature endpoints (membership hold/reinstatement)
        app.MapVettingHoldEndpoints();

        // Volunteer feature endpoints
        app.MapVolunteerEndpoints();
        app.MapVolunteerAssignmentEndpoints();

        // Email Templates feature endpoints
        app.MapEmailTemplateEndpoints();

        // Payment refund endpoints
        app.MapRefundEndpoints();

        // CMS feature endpoints
        app.MapCmsEndpoints();

        // Logging feature endpoints (client error ingestion)
        app.MapClientErrorEndpoints();

        // Logging feature endpoints (admin log queries)
        app.MapAdminLogEndpoints();

        // Test Helper endpoints (Development/Test only)
        app.MapTestHelperEndpoints();

        return app;
    }
}