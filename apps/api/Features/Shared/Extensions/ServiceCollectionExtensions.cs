using FluentValidation;
using WitchCityRope.Api.Features.Health.Services;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Users.Services;
using WitchCityRope.Api.Features.Dashboard.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Safety.Validation;
using WitchCityRope.Api.Features.CheckIn.Extensions;
using WitchCityRope.Api.Features.Vetting.Services;
// using WitchCityRope.Api.Features.Vetting.Validators;
using WitchCityRope.Api.Features.VettingHold.Services;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;
using WitchCityRope.Api.Features.Reports.Services;
using WitchCityRope.Api.Features.Webhooks.Services;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.ProxyRsvp.Services;
using WitchCityRope.Api.Features.TicketAssignment.Services;
using WitchCityRope.Api.Features.TestHelpers.Services;
using WitchCityRope.Api.Features.Cms;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Venues.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Admin.Settings.Services;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Services.Seeding;

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
    public static IServiceCollection AddFeatureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Health feature services
        services.AddScoped<IHealthService, HealthService>();

        // Authentication feature services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IReturnUrlValidator, ReturnUrlValidator>();

        // Events feature services
        services.AddScoped<IEventService, Events.Services.EventService>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();

        // Admin feature services
        services.AddScoped<ISettingsService, SettingsService>();

        // Users feature services
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IMemberDetailsService, MemberDetailsService>();

        // Dashboard feature services (wireframe v4)
        services.AddScoped<IUserDashboardProfileService, UserDashboardProfileService>();

        // Safety feature services
        services.AddScoped<IIncidentEmailService, IncidentEmailService>();
        services.AddScoped<ISafetyService, SafetyService>();
        services.AddScoped<ISafetyServiceExtended, SafetyServiceExtended>();

        // ALWAYS use real encryption (even in development)
        // Encryption MUST be real for security - mocking PayPal doesn't mean we should mock encryption
        // Bug fix: USE_MOCK_PAYMENT_SERVICE was causing unencrypted Capture IDs in database
        services.AddScoped<IEncryptionService, EncryptionService>();

        services.AddScoped<IAuditService, AuditService>();

        // FluentValidation for Safety feature
        services.AddValidatorsFromAssemblyContaining<CreateIncidentValidator>();
        services.AddValidatorsFromAssemblyContaining<WitchCityRope.Api.Features.Safety.Validators.CreateIncidentRequestValidator>();

        // CheckIn feature services
        services.AddCheckInServices();

        // Vetting feature services
        services.AddScoped<IVettingService, VettingService>();
        services.AddScoped<IVettingAccessControlService, VettingAccessControlService>();
        services.AddScoped<IVettingEmailService, VettingEmailService>(); // TEMPORARY STUB - TODO: Migrate to GlobalEmailTemplates

        // VettingHold feature services (membership hold/reinstatement)
        services.AddScoped<IVettingHoldService, VettingHoldService>();

        // FluentValidation for Vetting feature - TEMPORARILY DISABLED FOR MIGRATION
        // services.AddValidatorsFromAssemblyContaining<CreateApplicationValidator>();

        // Reports feature services
        services.AddScoped<IReportService, ReportService>();

        // Payment feature services
        services.AddScoped<IPaymentListService, PaymentListService>();
        services.AddSingleton<IPaymentNotificationService, PaymentNotificationService>(); // Singleton for in-memory SSE channel management

        // Conditionally register PayPal service based on configuration
        var useMockPayPal = configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE");
        if (useMockPayPal)
        {
            services.AddSingleton<IPayPalService, MockPayPalService>();

            // Log warning in development/test environments
            services.AddSingleton<ILogger<MockPayPalService>>(provider =>
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<MockPayPalService>();
                logger.LogWarning("🚨 MOCK PayPal Service is enabled - Not for production use!");
                return logger;
            });
        }
        else
        {
            services.AddScoped<IPayPalService, PayPalService>();
        }

        services.AddScoped<IRefundService, RefundService>();

        // Authorize.NET credit card processing (register if configured)
        var authNetSection = configuration.GetSection("CreditCardProcessor:AuthorizeNet");
        if (authNetSection.Exists() && !string.IsNullOrEmpty(authNetSection["ApiLoginId"]))
        {
            services.Configure<AuthorizeNetOptions>(authNetSection);
            services.AddScoped<IAuthorizeNetService, AuthorizeNetService>();
        }

        // HttpClientFactory (required by PayPal webhook verification)
        services.AddHttpClient();

        // PayPal webhook services
        services.AddScoped<IPayPalWebhookVerificationService, PayPalWebhookVerificationService>();
        services.AddScoped<IPayPalWebhookProcessingService, PayPalWebhookProcessingService>();

        // Authorized Contacts feature services (ticket assignment & proxy RSVP delegation)
        services.AddScoped<IAuthorizedContactService, AuthorizedContactService>();

        // Proxy RSVP feature services (delegate RSVP creation on behalf of principals)
        services.AddScoped<IProxyRsvpService, ProxyRsvpService>();

        // Ticket Assignment feature services (assign, accept, decline, reassign tickets)
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();

        // Attendance feature services (renamed from Participation)
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceCountService, AttendanceCountService>();

        // Volunteer feature services
        services.AddScoped<IVolunteerService, VolunteerService>();
        services.AddScoped<IVolunteerAssignmentService, VolunteerAssignmentService>();

        // Venue feature services
        services.AddScoped<IVenueService, VenueService>();

        // Email Templates feature services
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEventRecipientService, EventRecipientService>();
        services.AddScoped<IEventEmailService, EventEmailService>();

        // CMS feature services
        services.AddCmsServices();

        // Seed data service - Refactored into specialized seeders (2025-10-27)
        // Previously was a single 3,800-line SeedDataService.cs file
        // Now orchestrated by SeedCoordinator with 11 focused seeder components
        services.AddScoped<UserSeeder>();
        services.AddScoped<SettingsSeeder>();
        services.AddScoped<CmsSeeder>();
        services.AddScoped<VenueSeeder>();
        services.AddScoped<SafetySeeder>();
        services.AddScoped<AttendanceSeeder>();
        services.AddScoped<SessionTicketSeeder>();
        services.AddScoped<VolunteerSeeder>();
        services.AddScoped<TicketPurchaseSeeder>();
        services.AddScoped<VettingSeeder>();
        services.AddScoped<EventSeeder>();
        services.AddScoped<EmailTemplateSeeder>();

        // Register the seed coordinator as the main ISeedDataService implementation
        services.AddScoped<WitchCityRope.Api.Services.Seeding.ISeedDataService, SeedCoordinator>();

        // Database initialization services
        services.AddHostedService<DatabaseInitializationService>();

        // Test Helper services (Development/Test only)
        services.AddScoped<ITestHelperService, TestHelperService>();

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

/// <summary>
/// Application builder extensions for endpoint mapping
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Map all feature endpoints using minimal API pattern
    /// DEPRECATED: This method is not used. Endpoints are registered via WebApplicationExtensions.
    /// Keeping for backward compatibility but should be removed in future cleanup.
    /// </summary>
    [Obsolete("Use WebApplicationExtensions.MapFeatureEndpoints instead")]
    public static IApplicationBuilder MapFeatureEndpoints(this IApplicationBuilder app)
    {
        // This method is no longer used - WebApplicationExtensions.MapFeatureEndpoints is called instead
        return app;
    }
}
