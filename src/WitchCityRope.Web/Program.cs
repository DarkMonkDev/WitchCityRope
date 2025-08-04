using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
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
builder.Services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
{
    // Check for Aspire-provided connection string first, then fall back to DefaultConnection
    var connectionString = builder.Configuration.GetConnectionString("witchcityrope-db") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=your_password_here";
    options.UseNpgsql(connectionString);
});

// Add Infrastructure services (without DbContext since we already registered it above)
builder.Services.AddInfrastructureWithoutDbContext(builder.Configuration);

// Add Identity services
builder.Services.AddIdentity<WitchCityRope.Infrastructure.Identity.WitchCityRopeUser, WitchCityRope.Infrastructure.Identity.WitchCityRopeRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
    
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
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    
    // Handle authentication challenges appropriately based on request type
    options.Events.OnRedirectToLogin = context =>
    {
        // For API-style requests, return 401 instead of redirect
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // For Blazor navigation requests, return 401 to let Blazor handle it
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

// Note: .NET 9 authentication state serialization is built-in with Blazor Server
// No additional configuration needed - AddIdentity already configures authentication

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
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
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
    
    // Register real email sender when SendGrid is available
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, WitchCityRope.Infrastructure.Services.EmailSenderAdapter>();
}
else
{
    // Register a no-op email sender for development (ALWAYS register something)
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(provider =>
        new NoOpEmailSender());
}

// Add Authentication services (.NET 9 Blazor Server pattern)
// Register the authentication state provider for Blazor Server
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Register LocalStorage service for authentication (needed for any remaining legacy components)
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// Register the .NET 9 compliant auth service that uses SignInManager directly (CORRECT)
builder.Services.AddScoped<IAuthService, IdentityAuthService>();

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

// Add validation service (required by WCR validation components)
builder.Services.AddScoped<WitchCityRope.Web.Shared.Validation.Services.IValidationService, WitchCityRope.Web.Shared.Validation.Services.ValidationService>();

// Add authorization (.NET 9 pattern with multiple schemes support)
builder.Services.AddAuthorization(options =>
{
    // Don't set a default policy that requires authentication
    // This allows pages marked with [AllowAnonymous] to work properly
    
    // Define custom authorization policies
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Administrator", "Admin"));
    options.AddPolicy("RequireEventOrganizer", policy => policy.RequireRole("Administrator", "Admin", "EventOrganizer"));
    options.AddPolicy("RequireVettingTeam", policy => policy.RequireRole("Administrator", "Admin", "VettingTeam"));
    options.AddPolicy("RequireSafetyTeam", policy => policy.RequireRole("Administrator", "Admin", "SafetyTeam"));
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

// Add antiforgery services for Blazor forms
builder.Services.AddAntiforgery();

// Add health checks
builder.Services.AddHealthChecks();

// Add response caching
builder.Services.AddResponseCaching();

var app = builder.Build();

// Health checks will be mapped later with other endpoints

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

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Disable HTTPS redirection in development and testing environments
if (!app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Testing" && app.Environment.EnvironmentName != "Test")
{
    app.UseHttpsRedirection();
}

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

// Add antiforgery middleware after authentication
app.UseAntiforgery();

// Add Blazor initialization middleware
app.UseMiddleware<WitchCityRope.Web.Middleware.BlazorInitializationMiddleware>();

// Add custom Blazor authorization middleware
app.UseMiddleware<WitchCityRope.Web.Middleware.BlazorAuthorizationMiddleware>();

app.UseSession();

// Map endpoints
app.MapControllers(); // Map API controllers
app.MapRazorPages(); // Map Razor Pages (for Identity)

// Map Blazor Server components
app.MapRazorComponents<WitchCityRope.Web.App>()
    .AddInteractiveServerRenderMode();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();

// Make the Program class partial for integration testing
namespace WitchCityRope.Web
{
    public partial class Program { }
}

/// <summary>
/// No-op implementation of IEmailSender for development when SendGrid is not configured
/// </summary>
public class NoOpEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NoOpEmailSender>();
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("EMAIL NOT SENT (SendGrid not configured): To: {Email}, Subject: {Subject}", email, subject);
        return Task.CompletedTask;
    }
}

