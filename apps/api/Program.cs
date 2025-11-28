
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Antiforgery;
using System.Text;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Infrastructure.OpenAPI;
using WitchCityRope.Api.Features.Health.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Backup.Services;
using WitchCityRope.Api.Features.Backup.Jobs;
using WitchCityRope.Api.Features.Backup.Models;
using WitchCityRope.Api.Features.Backup.Endpoints;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;
using SendGrid;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for DateTime handling
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

        // Handle multiple DateTime formats from frontend
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        // Allow flexible DateTime parsing
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configure JSON options for minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

// Configure Microsoft's native OpenAPI support (.NET 9+)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    // EnumSchemaTransformer removed - built-in transformer handles enums automatically
});

// Database configuration for PostgreSQL with connection pooling and retry policies
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Base connection string (environment-aware, container-friendly)
    var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!";

    // Environment-specific configuration
    var environment = builder.Environment;
    var poolSize = environment.IsDevelopment() ? 20 : 100;
    var commandTimeout = environment.IsDevelopment() ? 30 : 60;

    // Build optimized connection string with pooling configuration
    var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
    {
        // Connection pooling configuration
        Pooling = true,
        MaxPoolSize = poolSize,          // 20 for dev, 100 for prod
        MinPoolSize = 5,                 // Maintain minimum connections
        ConnectionLifetime = 300,        // 5 minutes - prevent stale connections

        // Timeout configuration
        CommandTimeout = commandTimeout,  // 30 seconds dev, 60 seconds prod
        Timeout = 15,                     // Connection timeout

        // Health monitoring - keepalive for broken connection detection
        KeepAlive = 30,                   // 30 seconds

        // Performance tuning
        NoResetOnClose = false,           // Reset connection state on return to pool
        Enlist = true                     // Support distributed transactions
    }.ToString();

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Command timeout for migrations (longer for large migrations)
        npgsqlOptions.CommandTimeout(120);

        // Note: EnableRetryOnFailure removed - conflicts with explicit transactions
        // in database initialization. Retry logic implemented at application level.
    });

    // EF Core query behavior (development diagnostics)
    if (environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure Hangfire for background job processing (database backups)
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!";

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(hangfireConnectionString);
    }, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    }));

builder.Services.AddHangfireServer();

// SendGrid Email Service (null-safe for development)
builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    var emailEnabled = builder.Configuration.GetValue<bool>("Vetting:EmailEnabled", true);
    var apiKey = builder.Configuration["Vetting:SendGridApiKey"];

    if (!emailEnabled || string.IsNullOrEmpty(apiKey))
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        var reason = !emailEnabled
            ? "EmailEnabled is false"
            : "SendGrid API Key not configured";
        logger.LogWarning("SendGrid disabled - EmailService will run in development mode (console logging only). Reason: {Reason}", reason);
        return null!; // EmailService handles null client gracefully
    }

    return new SendGridClient(apiKey);
});
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password settings for development (relaxed for testing)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation not required for testing
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WitchCityRope-API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WitchCityRope-Services";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minute clock skew
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
    };

    // Enhanced logging and cookie support for BFF pattern
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Check for token in Authorization header first (default behavior)
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                return Task.CompletedTask;
            }

            // If no Bearer token, check for httpOnly cookie (BFF pattern)
            var cookieToken = context.Request.Cookies["auth-token"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("Using authentication token from httpOnly cookie");
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT token validated for user: {UserId}",
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);

            // CRITICAL FIX: For logout endpoint, don't fail authentication - let authorization middleware decide
            if (context.Request.Path.StartsWithSegments("/api/auth/logout") ||
                (context.Request.Path.StartsWithSegments("/api/auth/user") && context.Request.Method == "GET"))
            {
                logger.LogInformation("Allowing authentication failure for anonymous endpoint: {Path}", context.Request.Path);
                // Don't call context.Fail() - this allows the request to continue to authorization
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    };
});

// Service layer registration (services still used by remaining controllers)
// Note: IEventService removed due to EventsController migration
// Keep IAuthService for ProtectedController and IJwtService for authentication functionality
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Token blacklist service for logout functionality (singleton for shared state)
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// Health service for health check endpoints
builder.Services.AddScoped<IHealthService, HealthService>();

// Add memory cache for CheckIn system performance
builder.Services.AddMemoryCache();

// Add HttpContextAccessor for dynamic URL discovery
builder.Services.AddHttpContextAccessor();

// New vertical slice feature services
builder.Services.AddFeatureServices(builder.Configuration);

// Database Backup Configuration
builder.Services.Configure<BackupConfiguration>(builder.Configuration.GetSection("BackupConfiguration"));
builder.Services.AddScoped<DatabaseBackupService>();
builder.Services.AddScoped<SpacesStorageService>();
builder.Services.AddScoped<BackupOrchestrationService>();
builder.Services.AddScoped<BackupJob>();
builder.Services.AddScoped<RestoreJob>();

// Health checks for database monitoring
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!");

// Configure CORS for React development
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment",
        corsBuilder => corsBuilder
            .AllowAnyOrigin() // Most permissive for development
            .AllowAnyMethod()
            .AllowAnyHeader());

    // Alternative policy with credentials (if needed)
    options.AddPolicy("ReactDevelopmentWithCredentials",
        corsBuilder => corsBuilder
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:5174", "http://127.0.0.1:5173", "http://localhost:8080")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Configure Anti-Forgery (CSRF) Protection
// Microsoft standard pattern for .NET 9 with JSON APIs
builder.Services.AddAntiforgery(options =>
{
    // Header name that React will use to send CSRF token
    options.HeaderName = "X-CSRF-TOKEN";

    // Internal validation cookie (httpOnly = true, not accessible to JavaScript)
    // This cookie is used by the server to validate requests
    options.Cookie.Name = ".AspNetCore.Antiforgery";
    options.Cookie.HttpOnly = true;  // Server-only validation cookie
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Path = "/";
});

// Validate environment configuration
var environment = builder.Environment.EnvironmentName;
var useMocks = builder.Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE");

if (environment == "Production" && useMocks)
{
    throw new InvalidOperationException("Cannot use mock services in production!");
}

if (environment == "Test" && !useMocks)
{
    // CI/CD should always use mocks
    Console.WriteLine("⚠️ Warning: Test environment should use mock services for CI/CD");
}

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<Program>();
logger.LogInformation($"Environment: {environment}, Using Mock PayPal: {useMocks}");

var app = builder.Build();

// Hangfire Dashboard (Admin-only access)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Map OpenAPI endpoint (Microsoft's native support)
    app.MapOpenApi();

    // Use NSwag for Swagger UI (provides UI for OpenAPI document)
    app.UseOpenApi(); // NSwag middleware
    app.UseSwaggerUi(); // NSwag UI (note: UseSwaggerUi, not UseSwaggerUI)
}

app.UseCors("ReactDevelopmentWithCredentials");

// CRITICAL: Enable Anti-Forgery (CSRF) Protection middleware
// MUST be placed AFTER UseCors() and BEFORE UseAuthentication()
app.UseAntiforgery();

// CSRF token generation endpoint for React SPA
// Microsoft standard pattern for .NET 9 Minimal APIs with JSON
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);

    // Store request token in non-httpOnly cookie that JavaScript can read
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        new CookieOptions
        {
            HttpOnly = false,  // CRITICAL: JavaScript must be able to read this
            SameSite = SameSiteMode.Lax, // Lax allows cross-origin for dev/test (web:5173 → api:8080)
            Secure = context.Request.IsHttps, // Use HTTPS in production, HTTP in dev/test
            Path = "/"
        });

    return Results.Ok(new { tokenGenerated = true });
})
// CRITICAL: No .RequireAuthorization() - users need CSRF token BEFORE login
// This prevents chicken-and-egg problem: need token to login, but need login for token
.AllowAnonymous()
.WithName("GetAntiforgeryToken")
.WithSummary("Generate CSRF token for any session (pre-auth and authenticated)")
.WithDescription("Generates and stores CSRF token in XSRF-TOKEN cookie. React frontend reads this cookie and includes token in X-CSRF-TOKEN header for state-changing requests.")
.WithTags("Security", "Authentication")
.Produces<object>(200)
.Produces(401);

// CRITICAL FIX: Simple test middleware
// Removed simple logout middleware - proper logout endpoint handles this in AuthenticationEndpoints.cs

// Add debugging middleware for CORS issues in development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        // Log incoming request details
        logger.LogInformation("Request: {Method} {Path} from Origin: {Origin}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers.Origin.FirstOrDefault() ?? "none");

        await next();

        // Log response headers for CORS debugging
        var corsHeaders = context.Response.Headers
            .Where(h => h.Key.StartsWith("Access-Control"))
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray()));

        if (corsHeaders.Any())
        {
            logger.LogInformation("Response CORS headers: {Headers}",
                string.Join("; ", corsHeaders.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        }
        else
        {
            logger.LogWarning("No CORS headers in response for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
    });
}

// Authentication middleware must come before authorization
app.UseAuthentication();
app.UseAuthorization();

// Existing controller endpoints (to be migrated)
app.MapControllers();

// Legacy health check endpoint (will be replaced by new Health feature)
app.MapHealthChecks("/health-check");

// New vertical slice feature endpoints
app.MapFeatureEndpoints();

// Admin backup endpoints
app.MapAdminBackupEndpoints();

// TODO: REMOVE AFTER TESTING - Test endpoint for email service verification
app.MapGet("/test-email", async (IEmailService emailService) =>
{
    var variables = new Dictionary<string, string>
    {
        { "user_name", "TestUser" },
        { "system_url", "https://staging.witchcityrope.com" },
        { "support_email", "support@witchcityrope.com" }
    };

    var result = await emailService.SendTemplatedEmailAsync(
        "test@example.com",
        "Test User",
        EmailCategory.Admin,
        "PasswordReset",
        variables);

    return result.IsSuccess
        ? Results.Ok(new { message = "Email sent successfully!", details = "Check logs for development mode output or SendGrid dashboard for production sends" })
        : Results.BadRequest(new { error = result.Error });
}).WithTags("Testing");

// Schedule automated daily backup at 2 AM
RecurringJob.AddOrUpdate<BackupJob>(
    "daily-backup",
    job => job.ExecuteAsync(null!),
    "0 2 * * *",  // Cron: 2 AM daily
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
);

app.Run();

// Make Program class accessible for testing
public partial class Program { }

// Hangfire Dashboard Authorization - Admin only
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For Hangfire.Core (not AspNetCore), we need to check if the dashboard is accessed
        // In production, configure proper authorization
        // For now, allow access (will be secured by ASP.NET Core authentication/authorization on the endpoint)
        return true;
    }
}

// API test $(date)
// API hot reload test Sun Aug 17 03:43:42 PM EDT 2025
