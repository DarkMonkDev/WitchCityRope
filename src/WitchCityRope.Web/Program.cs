using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.DataProtection;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Components.Account;
using Syncfusion.Blazor;
using SendGrid.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure;
using System.Security.Claims;

// Register Syncfusion license early
var syncfusionLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
    ?? new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .Build()["Syncfusion:LicenseKey"];

if (!string.IsNullOrEmpty(syncfusionLicense))
{
    syncfusionLicense = syncfusionLicense.Trim();
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
    Console.WriteLine($"Syncfusion license registered: {syncfusionLicense.Substring(0, 10)}...");
}

var builder = WebApplication.CreateBuilder(args);

// Configure services

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Razor Pages support for Identity pages
builder.Services.AddRazorPages();

builder.Services.AddControllers(); // Add controllers support for API endpoints

// Add Syncfusion Blazor service
builder.Services.AddSyncfusionBlazor();

// Add Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly("WitchCityRope.Infrastructure");
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Add Identity services using the custom user and role types
builder.Services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>(options =>
{
    // Configure password requirements (make them simpler for development)
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    
    // Configure user settings
    options.User.RequireUniqueEmail = true;
    
    // Configure lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
.AddDefaultTokenProviders();

// Configure Identity cookies for .NET 9 Blazor Server
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    
    // Configure cookie settings for Blazor Server
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.Name = ".WitchCityRope.Identity";
    
    // For Blazor Server, handle authentication challenges differently
    options.Events.OnRedirectToLogin = context =>
    {
        var request = context.Request;
        
        // For API calls (AJAX/Fetch), return 401 instead of redirect
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
            request.Headers["Accept"].ToString().Contains("application/json") ||
            request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // For regular page requests, do the redirect
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        var request = context.Request;
        
        // For API calls (AJAX/Fetch), return 403 instead of redirect
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
            request.Headers["Accept"].ToString().Contains("application/json") ||
            request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        
        // For regular page requests, do the redirect
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    // Add JWT token management events
    options.Events.OnSigningIn = async context =>
    {
        try
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Cookie SigningIn event triggered - acquiring JWT token for user");
            
            var apiAuthService = context.HttpContext.RequestServices.GetRequiredService<IApiAuthenticationService>();
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<WitchCityRopeUser>>();
            
            // Get the user from the signed-in principal
            var user = await userManager.GetUserAsync(context.Principal);
            if (user != null)
            {
                logger.LogInformation("Found user for JWT token acquisition: {UserId} {Email}", user.Id, user.Email);
                
                // Get JWT token for API calls
                var jwtToken = await apiAuthService.GetJwtTokenForUserAsync(user);
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    logger.LogInformation("Successfully obtained JWT token during cookie sign-in for user: {Email}", user.Email);
                }
                else
                {
                    logger.LogWarning("Failed to obtain JWT token during cookie sign-in for user: {Email}", user.Email);
                }
            }
            else
            {
                logger.LogWarning("Could not find user for JWT token acquisition during cookie sign-in");
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Error during JWT token acquisition in cookie sign-in event");
        }
    };
    
    options.Events.OnSigningOut = async context =>
    {
        try
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Cookie SigningOut event triggered - invalidating JWT token");
            
            var apiAuthService = context.HttpContext.RequestServices.GetRequiredService<IApiAuthenticationService>();
            await apiAuthService.InvalidateTokenAsync();
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Error during JWT token invalidation in cookie sign-out event");
        }
    };
});

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // For session state

// Register JWT token services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
builder.Services.AddScoped<AuthenticationEventHandler>();

// Register AuthenticationDelegatingHandler
builder.Services.AddTransient<AuthenticationDelegatingHandler>();

// Configure HttpClient for API calls with authentication
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Use configuration to get the API URL
    // Default to external API port for local development
    var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5653";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => 
    {
        // Accept all certificates in development
        return builder.Environment.IsDevelopment();
    }
});

// Configure HttpClient for local authentication API calls
// This is used by IdentityAuthService to call auth endpoints
builder.Services.AddHttpClient("LocalApi", (serviceProvider, client) =>
{
    // In container environments, always use localhost:8080
    // The application listens on 8080 inside the container
    client.BaseAddress = new Uri("http://localhost:8080");
});

// Configure HttpClient for API service authentication (JWT tokens)  
// This client is used by ApiAuthenticationService and doesn't need the auth handler
// since it's used to GET tokens, not to send them
builder.Services.AddHttpClient("ApiClient", (serviceProvider, client) =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5653";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => 
    {
        // Accept all certificates in development
        return builder.Environment.IsDevelopment();
    }
});

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Register DashboardService with DbContext instead of HttpClient
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Configure SendGrid for email
var sendGridApiKey = builder.Configuration["Email:SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = sendGridApiKey;
    });

    // SendGrid client is registered by AddSendGrid above
}
else
{
    // Add a null SendGrid client registration for development when API key is not configured
    builder.Services.AddScoped<SendGrid.ISendGridClient>(provider => null!);
    Console.WriteLine("[Web] SendGrid API key not configured - using mock email service for development");
}

// Add cascading auth state provider (using default server-side auth state provider)
builder.Services.AddCascadingAuthenticationState();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add application services
builder.Services.AddScoped<WitchCityRope.Web.Services.IAuthService, WitchCityRope.Web.Services.IdentityAuthService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IUserService, WitchCityRope.Web.Services.UserService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IEventService, WitchCityRope.Web.Services.EventService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IRegistrationService, WitchCityRope.Web.Services.RegistrationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IVettingService, WitchCityRope.Web.Services.VettingService>();
// TODO: Implement these services
// builder.Services.AddScoped<WitchCityRope.Web.Services.ITicketService, WitchCityRope.Web.Services.TicketService>();
// builder.Services.AddScoped<WitchCityRope.Web.Services.IProfileService, WitchCityRope.Web.Services.ProfileService>();
// builder.Services.AddScoped<WitchCityRope.Web.Services.IPrivacyService, WitchCityRope.Web.Services.PrivacyService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.ISafetyService, WitchCityRope.Web.Services.SafetyService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.INotificationService, WitchCityRope.Web.Services.NotificationService>();
// DashboardService is registered with HttpClient below

// Add utility services
builder.Services.AddSingleton<WitchCityRope.Web.Services.IToastService, WitchCityRope.Web.Services.ToastService>(); // Singleton for state management
builder.Services.AddScoped<WitchCityRope.Web.Services.INavigationService, WitchCityRope.Web.Services.NavigationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IFileUploadService, WitchCityRope.Web.Services.FileUploadService>();

// Add validation service (required by WCR validation components)
builder.Services.AddScoped<WitchCityRope.Web.Shared.Validation.Services.IValidationService, WitchCityRope.Web.Shared.Validation.Services.ValidationService>();

// Add authorization (.NET 9 pattern with multiple schemes support)
builder.Services.AddAuthorization(options =>
{
    // Define policies using roles from the UserRole enum
    options.AddPolicy("RequireEventAttendee", policy => policy.RequireRole("Attendee", "Member", "Organizer", "Moderator", "Administrator"));
    options.AddPolicy("RequireCommunityMember", policy => policy.RequireRole("Member", "Organizer", "Moderator", "Administrator"));
    options.AddPolicy("RequireOrganizer", policy => policy.RequireRole("Organizer", "Moderator", "Administrator"));
    options.AddPolicy("RequireModerator", policy => policy.RequireRole("Moderator", "Administrator"));
    options.AddPolicy("RequireAdministrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireVetting", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsVetted", "True");
    });
});

// Add data protection for secure cookie handling
if (builder.Environment.EnvironmentName == "Testing")
{
    // Use ephemeral data protection for tests to avoid file system permission issues
    builder.Services.AddDataProtection()
        .SetApplicationName("WitchCityRope")
        .UseEphemeralDataProtectionProvider();
}
else
{
    builder.Services.AddDataProtection()
        .SetApplicationName("WitchCityRope")
        .PersistKeysToFileSystem(new DirectoryInfo(
            builder.Environment.IsDevelopment() 
                ? Path.Combine(Directory.GetCurrentDirectory(), "temp", "keys")
                : "/app/shared/keys")); // Use temp directory in dev, shared volume in production
}

// Configure session (for temporary data and state management)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Set session timeout
    options.Cookie.HttpOnly = true; // Make session cookie HTTP only
    options.Cookie.IsEssential = true; // Make session cookie essential
    options.Cookie.Name = ".WitchCityRope.Session";
});

// Add response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure authentication and authorization
// Circuit Handler for managing SignalR connections in Blazor Server
builder.Services.AddScoped<CircuitHandler, WitchCityRope.Web.Services.CircuitHandlerService>();

// Add user manager and role manager for Identity access within Blazor components
// These are already added by AddIdentity, but we want to ensure they're available as scoped services

// Add services for the circuit and preloading
builder.Services.AddScoped<WitchCityRope.Web.Services.AdminDataPreloadService>();
// TODO: Implement UserContextService
// builder.Services.AddScoped<WitchCityRope.Web.Services.UserContextService>(); // User context for Blazor components

// Configure the app
var app = builder.Build();

// Configure request pipeline

// Security headers middleware (must be early in pipeline)
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Enable response compression
app.UseResponseCompression();

// Static files should be served before authentication
app.UseStaticFiles();

// Enable session middleware
app.UseSession();

// Essential middleware for authentication/authorization
app.UseRouting();

// Authentication and authorization middleware (order is important)
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

// Enable anti-forgery protection
app.UseAntiforgery();

// Map endpoints
app.MapRazorComponents<WitchCityRope.Web.App>()
    .AddInteractiveServerRenderMode();

// Map Razor Pages for Identity
app.MapRazorPages();

// Map controllers (for any API endpoints in the web app)
app.MapControllers();

// Add a health check endpoint
app.MapGet("/health", () => "Healthy");

// Add a debug endpoint to check authentication
if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/auth", async (HttpContext context) =>
    {
        var isAuth = context.User?.Identity?.IsAuthenticated ?? false;
        var userName = context.User?.Identity?.Name ?? "Anonymous";
        var roles = context.User?.Claims?.Where(c => c.Type == ClaimTypes.Role)?.Select(c => c.Value)?.ToList() ?? new List<string>();
        
        return new 
        { 
            IsAuthenticated = isAuth,
            UserName = userName,
            Roles = roles,
            Claims = context.User?.Claims?.Select(c => new { c.Type, c.Value })?.ToArray() ?? Array.Empty<object>()
        };
    });
}

// Run the application
app.Run();

// Make Program class public for integration tests
public partial class Program { }