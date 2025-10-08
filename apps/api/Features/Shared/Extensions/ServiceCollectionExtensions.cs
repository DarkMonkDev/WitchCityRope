using FluentValidation;
using WitchCityRope.Api.Features.Health.Services;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Users.Services;
using WitchCityRope.Api.Features.Dashboard.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Safety.Validation;
using WitchCityRope.Api.Features.CheckIn.Extensions;
using WitchCityRope.Api.Features.Vetting.Services;
// using WitchCityRope.Api.Features.Vetting.Validators;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Validators;
using WitchCityRope.Api.Features.Participation.Services;
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
    public static IServiceCollection AddFeatureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Health feature services
        services.AddScoped<HealthService>();

        // Authentication feature services
        services.AddScoped<AuthenticationService>();

        // Events feature services
        services.AddScoped<Events.Services.EventService>();

        // Users feature services
        services.AddScoped<UserManagementService>();

        // Dashboard feature services
        services.AddScoped<IUserDashboardService, UserDashboardService>();

        // Safety feature services  
        services.AddScoped<ISafetyService, SafetyService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();

        // FluentValidation for Safety feature
        services.AddValidatorsFromAssemblyContaining<CreateIncidentValidator>();

        // CheckIn feature services
        services.AddCheckInServices();

        // Vetting feature services
        services.AddScoped<IVettingService, VettingService>();
        services.AddScoped<IVettingAccessControlService, VettingAccessControlService>();
        services.AddScoped<IVettingEmailService, VettingEmailService>(); // SendGrid integration with mock mode support

        // FluentValidation for Vetting feature - TEMPORARILY DISABLED FOR MIGRATION
        // services.AddValidatorsFromAssemblyContaining<CreateApplicationValidator>();

        // Payment feature services
        services.AddScoped<IPaymentService, PaymentService>();

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

        // FluentValidation for Payment feature
        services.AddValidatorsFromAssemblyContaining<ProcessPaymentApiRequestValidator>();

        // Participation feature services
        services.AddScoped<IParticipationService, ParticipationService>();

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

/// <summary>
/// Application builder extensions for endpoint mapping
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Map all feature endpoints - currently a placeholder since controllers are mapped via MapControllers()
    /// </summary>
    public static IApplicationBuilder MapFeatureEndpoints(this IApplicationBuilder app)
    {
        // Controllers (including VettingController) are already mapped via app.MapControllers()
        // This method is a placeholder for future minimal API endpoints if needed
        return app;
    }
}