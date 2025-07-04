using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Models;
using Syncfusion.Blazor;
using SendGrid.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;

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
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Add controllers support for API endpoints
builder.Services.AddServerSideBlazor(options =>
{
    // Configure Blazor Server circuit options for optimal performance
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

// Add Syncfusion Blazor service
builder.Services.AddSyncfusionBlazor();

// Add Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<WitchCityRopeDbContext>(options =>
{
    // Check for Aspire-provided connection string first, then fall back to DefaultConnection
    var connectionString = builder.Configuration.GetConnectionString("witchcityrope-db") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=your_password_here";
    options.UseNpgsql(connectionString);
});

// Add authentication services for Blazor Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    
    // Handle authentication challenges appropriately based on request type
    options.Events.OnRedirectToLogin = context =>
    {
        // For API-style requests or when AllowAutoRedirect is false, return 401
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // For Blazor navigation requests to protected pages, ensure proper redirect
        if (context.Request.Path.StartsWithSegments("/_blazor"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // For regular browser requests, redirect to login
        context.Response.StatusCode = 302;
        context.Response.Headers["Location"] = context.RedirectUri;
        return Task.CompletedTask;
    };
});

// Add HttpContextAccessor for authentication service
builder.Services.AddHttpContextAccessor();

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // For session state

// Configure HttpClient for API calls
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Use configuration to get the API URL
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure HttpClient for AuthenticationService
builder.Services.AddHttpClient<AuthenticationService>(client =>
{
    // Use configuration to get the API URL
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Configure HttpClient for DashboardService
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure SendGrid for email
var sendGridApiKey = builder.Configuration["Email:SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = sendGridApiKey;
    });
}

// Add Authentication services
// AuthenticationService is already registered by AddHttpClient<AuthenticationService> above
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add direct service implementations
builder.Services.AddScoped<WitchCityRope.Web.Services.IEventService, WitchCityRope.Web.Services.EventService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IUserService, WitchCityRope.Web.Services.UserService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IRegistrationService, WitchCityRope.Web.Services.RegistrationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IVettingService, WitchCityRope.Web.Services.VettingService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IPaymentService, WitchCityRope.Web.Services.PaymentService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.ISafetyService, WitchCityRope.Web.Services.SafetyService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.INotificationService, WitchCityRope.Web.Services.NotificationService>();
// DashboardService is registered with HttpClient below

// Add utility services
builder.Services.AddSingleton<WitchCityRope.Web.Services.IToastService, WitchCityRope.Web.Services.ToastService>(); // Singleton for state management
builder.Services.AddScoped<WitchCityRope.Web.Services.INavigationService, WitchCityRope.Web.Services.NavigationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IFileUploadService, WitchCityRope.Web.Services.FileUploadService>();

// Add authorization
builder.Services.AddAuthorizationCore(options =>
{
    // Define authorization policies
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireEventOrganizer", policy => policy.RequireRole("Admin", "EventOrganizer"));
    options.AddPolicy("RequireVettingTeam", policy => policy.RequireRole("Admin", "VettingTeam"));
    options.AddPolicy("RequireSafetyTeam", policy => policy.RequireRole("Admin", "SafetyTeam"));
    options.AddPolicy("RequireVettedMember", policy => 
        policy.RequireRole("Member").RequireClaim("IsVetted", "true"));
});

// Configure logging
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add session support for server-side Blazor
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Add data protection for secure storage
builder.Services.AddDataProtection();

// Configure circuit handler for monitoring and logging
builder.Services.AddScoped<CircuitHandler, WitchCityRope.Web.Services.CircuitHandlerService>();

// Configure server-side Blazor performance monitoring
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB for file uploads
});

// Configure SignalR for real-time updates with performance optimizations
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    
    // Performance optimizations
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    
    // Configure message buffer
    options.StreamBufferCapacity = 10;
})
.AddJsonProtocol(options =>
{
    // Use more compact JSON serialization
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Add response compression with Brotli and Gzip providers
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Configure Brotli compression
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Configure Gzip compression
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Add health checks
builder.Services.AddHealthChecks();

// Add response caching
builder.Services.AddResponseCaching();

var app = builder.Build();

// Map health check endpoints manually
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

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

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use response compression early in the pipeline
app.UseResponseCompression();

// Use response caching
app.UseResponseCaching();

// Configure static files with caching headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache control header for static assets
        // Cache for 1 year (365 days)
        const int durationInSeconds = 365 * 24 * 60 * 60;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] = 
            $"public,max-age={durationInSeconds}";
    }
});

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add custom Blazor authorization middleware
app.UseMiddleware<WitchCityRope.Web.Middleware.BlazorAuthorizationMiddleware>();

app.UseSession();

// Map endpoints
app.MapControllers(); // Map API controllers
app.MapBlazorHub(options =>
{
    // Configure hub-specific options for performance
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    
    // Configure buffer sizes
    options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
    options.TransportMaxBufferSize = 64 * 1024; // 64KB
    
    // Set WebSocket options
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);
    
    // Long polling options as fallback
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});
app.MapFallbackToPage("/_Host");
app.MapRazorPages();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();

// Make the Program class partial for integration testing
namespace WitchCityRope.Web
{
    public partial class Program { }
}

