global using Microsoft.AspNetCore.SignalR;
global using System.Security.Claims;
global using WitchCityRope.Api.Services;
global using WitchCityRope.Api.Models;
global using WitchCityRope.Api.Exceptions;
global using Microsoft.Extensions.Caching.Memory;

using WitchCityRope.Api.Infrastructure;
using WitchCityRope.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using SendGrid.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Endpoints;
using WitchCityRope.Api.Extensions;

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

// Add API-specific services using extension methods
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);

// Add caching services
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Add SendGrid for email
var sendGridApiKey = builder.Configuration["Email:SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = sendGridApiKey;
    });
}

// Configure minimal APIs
builder.Services.AddEndpointsApiExplorer();

// Add SignalR for real-time features
builder.Services.AddSignalR();

// Add data protection for secure cookie/token handling
builder.Services.AddDataProtection();

var app = builder.Build();

// Health checks are mapped in ConfigureApiPipeline

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRope.Infrastructure.Data.WitchCityRopeDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await WitchCityRope.Infrastructure.Data.DbInitializer.InitializeAsync(context, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline using extension method
app.ConfigureApiPipeline();

// Use response caching
app.UseResponseCaching();

// Map SignalR hubs
app.MapHub<NotificationHub>("/hubs/notifications");

// Map minimal API endpoints
app.MapGroup("/api/v1")
    .WithTags("API v1")
    .MapApiEndpoints();

// Map the /api/auth/me endpoint directly (without v1 prefix) for backward compatibility
app.MapGet("/api/auth/me", async (
    HttpContext context,
    WitchCityRope.Api.Features.Auth.Services.IUserRepository userRepository) =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }
    
    var userId = context.User.GetUserIdOrNull();
    if (!userId.HasValue)
    {
        return Results.Unauthorized();
    }
    
    var user = await userRepository.GetByIdAsync(userId.Value);
    if (user == null)
    {
        return Results.NotFound();
    }
    
    // Return UserInfo structure expected by the Web app
    return Results.Ok(new
    {
        id = user.Id.ToString(),
        email = user.Email.Value,
        firstName = user.SceneName.Value,  // Using scene name as first name
        lastName = "",  // No last name in this system
        avatarUrl = (string?)null,
        roles = new List<string> { user.Role.ToString() }
    });
})
.RequireAuthorization()
.WithName("GetCurrentUserCompat")
.WithOpenApi();

// Run the application
app.Run();

// SignalR Hub for notifications
public class NotificationHub : Hub
{
    public async Task SendNotification(string user, string message)
    {
        await Clients.User(user).SendAsync("ReceiveNotification", message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

// Extension method to map API endpoints
public static class EndpointExtensions
{
    public static RouteGroupBuilder MapApiEndpoints(this RouteGroupBuilder group)
    {
        // Auth endpoints
        group.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await authService.LoginAsync(request);
                return Results.Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                logger.LogWarning("Login failed for user {Email}: {Message}", request.Email, ex.Message);
                return Results.Problem(ex.Message, statusCode: 401);
            }
        })
        .WithName("Login")
        .WithOpenApi();

        group.MapPost("/auth/register", async (
            RegisterRequest request,
            IAuthService authService,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await authService.RegisterAsync(request);
                return Results.Created($"/api/v1/users/{result.UserId}", result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (WitchCityRope.Core.Exceptions.ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("Register")
        .WithOpenApi();

        group.MapPost("/auth/refresh", async (
            RefreshTokenRequest request,
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.RefreshTokenAsync(request.RefreshToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                return Results.Problem(ex.Message, statusCode: 401);
            }
        })
        .WithName("RefreshToken")
        .WithOpenApi();

        // Event endpoints
        group.MapGet("/events", async (
            int page,
            int pageSize,
            string? search,
            IEventService eventService,
            IMemoryCache cache) =>
        {
            var cacheKey = $"events_{page}_{pageSize}_{search}";
            
            if (cache.TryGetValue<PagedResult<EventDto>>(cacheKey, out var cachedResult))
            {
                return Results.Ok(cachedResult);
            }

            var result = await eventService.GetEventsAsync(page, pageSize, search);
            
            cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            
            return Results.Ok(result);
        })
        .WithName("ListEvents")
        .WithOpenApi();

        group.MapGet("/events/{id:guid}", async (
            Guid id,
            IEventService eventService) =>
        {
            var eventDetails = await eventService.GetEventByIdAsync(id);
            return eventDetails is null 
                ? Results.NotFound() 
                : Results.Ok(eventDetails);
        })
        .WithName("GetEventById")
        .WithOpenApi();

        group.MapPost("/events", async (
            CreateEventRequest request,
            IEventService eventService,
            HttpContext context) =>
        {
            try
            {
                var organizerId = context.User.GetUserId();
                var result = await eventService.CreateEventAsync(request, organizerId);
                return Results.Created($"/api/v1/events/{result.EventId}", result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (ForbiddenException ex)
            {
                return Results.Problem(ex.Message, statusCode: 403);
            }
        })
        .RequireAuthorization("RequireOrganizer")
        .WithName("CreateEvent")
        .WithOpenApi();

        // Event Session Matrix endpoints
        group.MapCreateEventEndpoint();
        group.MapGetEventWithSessionsEndpoint();
        group.MapGetEventAvailabilityEndpoint();
        
        // Events Management API endpoints (new Event Session Matrix APIs)
        group.MapEventsManagementEndpoints();

        group.MapPut("/events/{id:guid}", async (
            Guid id,
            WitchCityRope.Core.DTOs.UpdateEventRequest request,
            IEventService eventService,
            HttpContext context) =>
        {
            try
            {
                var userId = context.User.GetUserId();
                await eventService.UpdateEventAsync(id, request, userId);
                return Results.NoContent();
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (ForbiddenException ex)
            {
                return Results.Problem(ex.Message, statusCode: 403);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
        })
        .RequireAuthorization("RequireOrganizer")
        .WithName("UpdateEvent")
        .WithOpenApi();

        // Registration endpoints
        group.MapPost("/events/{eventId:guid}/register", async (
            Guid eventId,
            WitchCityRope.Core.DTOs.EventRegistrationRequest request,
            IRegistrationService registrationService,
            HttpContext context) =>
        {
            try
            {
                var userId = context.User.GetUserId();
                var result = await registrationService.RegisterForEventAsync(eventId, userId, request);
                return Results.Ok(result);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (WitchCityRope.Core.Exceptions.ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (PaymentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("RegisterForEvent")
        .WithOpenApi();

        // Vetting endpoints
        group.MapPost("/vetting/apply", async (
            WitchCityRope.Core.DTOs.VettingApplicationRequest request,
            IVettingService vettingService,
            HttpContext context) =>
        {
            try
            {
                var userId = context.User.GetUserId();
                var result = await vettingService.SubmitApplicationAsync(request, userId);
                return Results.Created($"/api/v1/vetting/applications/{result.ApplicationId}", result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (WitchCityRope.Core.Exceptions.ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("SubmitVettingApplication")
        .WithOpenApi();

        group.MapGet("/vetting/applications", async (
            string? status,
            int page,
            int pageSize,
            IVettingService vettingService) =>
        {
            var result = await vettingService.GetApplicationsAsync(status, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization("RequireVettingTeam")
        .WithName("GetVettingApplications")
        .WithOpenApi();

        group.MapPost("/vetting/applications/{applicationId:guid}/review", async (
            Guid applicationId,
            WitchCityRope.Core.DTOs.ReviewApplicationRequest request,
            IVettingService vettingService,
            HttpContext context) =>
        {
            try
            {
                var reviewerId = context.User.GetUserId();
                var result = await vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);
                return Results.Ok(result);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Results.Problem(ex.Message, statusCode: 403);
            }
        })
        .RequireAuthorization("RequireVettingTeam")
        .WithName("ReviewVettingApplication")
        .WithOpenApi();

        // Check-in endpoints
        group.MapPost("/events/{eventId:guid}/checkin", async (
            Guid eventId,
            WitchCityRope.Core.DTOs.CheckInRequest request,
            ICheckInService checkInService,
            HttpContext context) =>
        {
            try
            {
                var staffId = context.User.GetUserId();
                var result = await checkInService.CheckInAttendeeAsync(eventId, request, staffId);
                return Results.Ok(result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("CheckInAttendee")
        .WithOpenApi();

        // Payment endpoints
        group.MapPost("/payments/process", async (
            WitchCityRope.Core.DTOs.ProcessPaymentRequest request,
            IPaymentService paymentService,
            HttpContext context) =>
        {
            try
            {
                var userId = context.User.GetUserId();
                var result = await paymentService.ProcessPaymentAsync(request, userId);
                return Results.Ok(result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (PaymentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("ProcessPayment")
        .WithOpenApi();

        group.MapPost("/payments/webhook", async (
            HttpRequest request,
            IPaymentService paymentService,
            IConfiguration configuration) =>
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            var signature = request.Headers["Stripe-Signature"].ToString();
            var webhookSecret = configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(webhookSecret))
            {
                return Results.BadRequest("Missing required webhook configuration");
            }

            try
            {
                await paymentService.HandleWebhookAsync(json, signature, webhookSecret);
                return Results.Ok();
            }
            catch (Exception)
            {
                return Results.BadRequest();
            }
        })
        .AllowAnonymous()
        .WithName("PaymentWebhook")
        .WithOpenApi();

        // Safety endpoints
        group.MapPost("/safety/report", async (
            WitchCityRope.Core.DTOs.IncidentReportRequest request,
            ISafetyService safetyService,
            HttpContext context) =>
        {
            try
            {
                Guid? reporterId = null;
                if (!request.IsAnonymous && context.User.Identity?.IsAuthenticated == true)
                {
                    reporterId = context.User.GetUserIdOrNull();
                }

                var result = await safetyService.SubmitIncidentReportAsync(request, reporterId);
                return Results.Created($"/api/v1/safety/reports/{result.ReportId}", result);
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .AllowAnonymous()
        .WithName("SubmitIncidentReport")
        .WithOpenApi();

        group.MapGet("/safety/reports", async (
            string? status,
            int page,
            int pageSize,
            ISafetyService safetyService) =>
        {
            var result = await safetyService.GetIncidentReportsAsync(status, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization("RequireSafetyTeam")
        .WithName("GetIncidentReports")
        .WithOpenApi();

        // User endpoints
        group.MapGet("/users/profile", async (
            WitchCityRope.Api.Services.IUserService userService,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var profile = await userService.GetUserProfileAsync(userId);
            return profile is null
                ? Results.NotFound()
                : Results.Ok(profile);
        })
        .RequireAuthorization()
        .WithName("GetUserProfile")
        .WithOpenApi();
        
        // Authentication test endpoint
        group.MapGet("/auth/me", async (
            HttpContext context,
            WitchCityRope.Api.Features.Auth.Services.IUserRepository userRepository) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return Results.Unauthorized();
            }
            
            var userId = context.User.GetUserIdOrNull();
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }
            
            var user = await userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return Results.NotFound();
            }
            
            // Return UserInfo structure expected by the Web app
            return Results.Ok(new
            {
                id = user.Id.ToString(),
                email = user.Email.Value,
                firstName = user.SceneName?.Value ?? "",
                lastName = "",
                avatarUrl = (string?)null,
                roles = new List<string> { user.Role.ToString() }
            });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithOpenApi();

        group.MapPut("/users/profile", async (
            UpdateProfileRequest request,
            WitchCityRope.Api.Services.IUserService userService,
            HttpContext context) =>
        {
            try
            {
                var userId = context.User.GetUserId();
                await userService.UpdateProfileAsync(userId, request);
                return Results.NoContent();
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (WitchCityRope.Core.Exceptions.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
        })
        .RequireAuthorization()
        .WithName("UpdateUserProfile")
        .WithOpenApi();

        // Note: Health check endpoints are now provided by MapDefaultEndpoints() at /health and /alive

        return group;
    }
}

// Extension methods for HttpContext
public static class HttpContextExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User ID not found in claims");
        }
        return userId;
    }

    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("UserId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}


// Make the Program class partial for integration testing
public partial class Program { }