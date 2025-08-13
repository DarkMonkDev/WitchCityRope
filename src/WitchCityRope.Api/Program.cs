global using Microsoft.AspNetCore.SignalR;
global using System.Security.Claims;
global using WitchCityRope.Api.Services;
global using WitchCityRope.Api.Models;
global using WitchCityRope.Api.Exceptions;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.AspNetCore.DataProtection;

using WitchCityRope.Api.Infrastructure;
using WitchCityRope.Infrastructure;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.AspNetCore.HttpLogging;
using Syncfusion.Licensing;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Extensions;
using Microsoft.AspNetCore.Identity;

// Register Syncfusion license early
var syncfusionLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
    ?? new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .Build()["Syncfusion:LicenseKey"];

if (!string.IsNullOrEmpty(syncfusionLicense))
{
    syncfusionLicense = syncfusionLicense.Trim();
    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
    Console.WriteLine($"[API] Syncfusion license registered: {syncfusionLicense.Substring(0, 10)}...");
}

var builder = WebApplication.CreateBuilder(args);

// Configure services

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add HTTP logging
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("X-Request-ID");
    logging.ResponseHeaders.Add("X-Response-ID");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

// Add Infrastructure services (includes DbContext, email, payment, encryption, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Identity services for API (includes UserManager, SignInManager, etc.)
builder.Services.AddWitchCityRopeIdentityForApi(builder.Configuration);

// Add API-specific services using extension methods
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);

// Add caching services
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Add SendGrid for email (with fallback for development)
var sendGridApiKey = builder.Configuration["Email:SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = sendGridApiKey;
    });
}
else
{
    // Add a null SendGrid client registration for development when API key is not configured
    builder.Services.AddScoped<ISendGridClient>(provider => null!);
    Console.WriteLine("[API] SendGrid API key not configured - using mock email service for development");
}

// Configure minimal APIs
builder.Services.AddEndpointsApiExplorer();

// Add SignalR for real-time features
builder.Services.AddSignalR();

// Data protection not needed for API service - Web service handles cookies, API only uses JWT tokens

var app = builder.Build();

// Health checks are mapped in ConfigureApiPipeline

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRope.Infrastructure.Data.WitchCityRopeIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRope.Infrastructure.Identity.WitchCityRopeUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRope.Infrastructure.Identity.WitchCityRopeRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await WitchCityRope.Infrastructure.Data.DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline using extension method
app.ConfigureApiPipeline();

app.Run();

// Make Program class internal but visible to tests via InternalsVisibleTo
internal partial class Program { }