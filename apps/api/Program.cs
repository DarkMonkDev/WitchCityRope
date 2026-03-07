
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
using WitchCityRope.Api.Features.EmailTemplates.Jobs;
using WitchCityRope.Api.Features.Logging.Jobs;
using WitchCityRope.Api.Features.Logging.Middleware;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;
using SendGrid;
using Npgsql;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using NpgsqlTypes;
using WitchCityRope.Api.Features.Logging.Infrastructure;

// Enable Serilog self-diagnostics so sink errors appear in stderr
Serilog.Debugging.SelfLog.Enable(Console.Error);

// Serilog two-stage initialization
// Stage 1: Bootstrap logger captures startup/config errors before DI is available
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

// Serilog Stage 2: Full configuration from appsettings + services
var serilogConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var columnWriters = new Dictionary<string, ColumnWriterBase>
{
    { "timestamp", new TimestampColumnWriter() },
    { "level", new LevelColumnWriter(false, NpgsqlDbType.Smallint) },
    { "level_name", new LevelColumnWriter(true, NpgsqlDbType.Text) },
    { "message", new RenderedMessageColumnWriter() },
    { "message_template", new MessageTemplateColumnWriter() },
    { "exception", new ExceptionColumnWriter() },
    { "source_context", new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
    { "properties", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
    { "user_id", new GuidColumnWriter("UserId") },
    { "correlation_id", new GuidColumnWriter("CorrelationId") },
    { "request_path", new SinglePropertyColumnWriter("RequestPath", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
    { "machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) }
};

builder.Services.AddSerilog((services, lc) =>
{
    lc.ReadFrom.Configuration(builder.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext()
      .Enrich.WithSensitiveDataMasking(options =>
      {
          options.MaskProperties.AddRange(new[]
          {
              new MaskProperty { Name = "password" },
              new MaskProperty { Name = "token" },
              new MaskProperty { Name = "secret" },
              new MaskProperty { Name = "key" },
              new MaskProperty { Name = "authorization" },
              new MaskProperty { Name = "cookie" },
              new MaskProperty { Name = "nonce" },
              new MaskProperty { Name = "creditcard" }
          });
      });

    // PostgreSQL sink for non-Development environments
    if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(serilogConnectionString))
    {
        lc.WriteTo.PostgreSQL(
            connectionString: serilogConnectionString,
            tableName: "application_logs",
            columnOptions: columnWriters,
            schemaName: "logging",
            needAutoCreateTable: false,
            useCopy: false, // PgBouncer transaction pooling does not support COPY protocol
            batchSizeLimit: 50,
            period: TimeSpan.FromSeconds(5));
    }
});

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

// Configure Microsoft's native OpenAPI support (.NET 10+)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    // Fix: Strip "string" from integer/number union types in OpenAPI schema
    // Without this, openapi-typescript generates "number | string" instead of "number"
    // See: NumericSchemaTransformer.cs for full explanation and issue references
    options.AddSchemaTransformer<NumericSchemaTransformer>();
});

// Database configuration for PostgreSQL with connection pooling and retry policies.
// All environments (dev, staging, production) connect directly to PostgreSQL (port 25060).
// PgBouncer was removed — it was causing "max_client_conn" errors and is unnecessary
// for this low-traffic app. DarkMonk, ShipEngine, and Accounting all connect directly
// to the same DigitalOcean managed database cluster without issues.
// DB cluster: db-s-1vcpu-2gb, max_connections=50, ~35 in use across all apps.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured. Set via environment variable or user secrets.");

    var environment = builder.Environment;
    var commandTimeout = environment.IsDevelopment() ? 30 : 60;

    // Connection pool sizing: keep conservative since we share a 50-connection database
    // cluster with DarkMonk (~4 conn), ShipEngine (~10 conn), Accounting (~1 conn),
    // and system processes (~10 conn). EF Core and Hangfire each get their own pool.
    // Target: ~8 max connections per environment (EF Core 5 + Hangfire 3), similar to DarkMonk's ~5.
    var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
    {
        Pooling = true,
        MaxPoolSize = environment.IsDevelopment() ? 20 : 5,
        MinPoolSize = environment.IsDevelopment() ? 5 : 1,
        ConnectionLifetime = 300,        // 5 min — recycle connections to prevent staleness

        CommandTimeout = commandTimeout,
        Timeout = 15,                    // Connection open timeout

        KeepAlive = 30,                  // Detect broken connections
        Enlist = false,                  // Disabled: TransactionScope has known Npgsql connection leak (GitHub #4963).
                                         // Use explicit EF Core transactions instead (context.Database.BeginTransactionAsync).
    }.ToString();

    // Build NpgsqlDataSource with dynamic JSON support for JSONB columns (e.g., TicketPurchase.Metadata)
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    options.UseNpgsql(dataSource, npgsqlOptions =>
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

// Configure Hangfire for background job processing.
// Hangfire creates its OWN Npgsql connection pool separate from EF Core's.
// Without explicit MaxPoolSize, Npgsql defaults to 100 — which was the root cause of
// "max_client_conn" errors when using PgBouncer. Even with direct connections,
// we cap it to prevent runaway connection usage on our shared database cluster.
// Hangfire runs 4 low-frequency jobs (daily backup, daily log summary, log cleanup, hourly email scheduler)
// and needs at most ~3 connections for 1 worker + background threads (heartbeat, scheduler, expiration manager).
var hangfireBaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured for Hangfire. Set via environment variable or user secrets.");

var hangfireConnectionString = new NpgsqlConnectionStringBuilder(hangfireBaseConnectionString)
{
    MaxPoolSize = 3,            // 1 worker + background threads — 3 is plenty for 4 low-frequency jobs
    MinPoolSize = 1,            // Keep 1 connection warm for heartbeats
    ConnectionLifetime = 300,   // Match EF Core — 5 min recycle
    Timeout = 15,
    Enlist = false,
}.ToString();

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

// 1 worker is sufficient for our low-volume background jobs (4 recurring tasks).
// DarkMonk uses 2 workers with similar job count — 1 is plenty for WitchCityRope.
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
    options.ServerTimeout = TimeSpan.FromMinutes(5);
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
});

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
    // Password policy - enforced for new password creation and changes
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

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
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey must be configured. Set via environment variable or user secrets.");
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

// Logging background jobs
builder.Services.AddScoped<DailyLogSummaryJob>();
builder.Services.AddScoped<LogRetentionCleanupJob>();

// Health checks for database monitoring
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured for health checks."));

// Configure CORS policies - environment-aware
builder.Services.AddCors(options =>
{
    // Development: explicit localhost origins with credentials
    options.AddPolicy("Development",
        corsBuilder => corsBuilder
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:5174", "http://127.0.0.1:5173", "http://localhost:8080")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

    // Production/Staging: configured origins from environment variables
    var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();
    if (allowedOrigins.Length > 0)
    {
        options.AddPolicy("Production",
            corsBuilder => corsBuilder
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    }
});

// Configure Anti-Forgery (CSRF) Protection
// Microsoft standard pattern for .NET 10 with JSON APIs
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

// Rate limiting for abuse prevention (client error endpoint)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("client-errors", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));
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

Log.Information("Environment: {EnvironmentName}, Using Mock PayPal: {UseMocks}", environment, useMocks);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Map OpenAPI endpoint (Microsoft's native support)
    app.MapOpenApi();

    // Use NSwag for Swagger UI (provides UI for OpenAPI document)
    app.UseOpenApi(); // NSwag middleware
    app.UseSwaggerUi(); // NSwag UI (note: UseSwaggerUi, not UseSwaggerUI)
}

// Use environment-appropriate CORS policy
var corsPolicy = app.Environment.IsDevelopment() ? "Development" : "Production";
app.UseCors(corsPolicy);

// CRITICAL: Enable Anti-Forgery (CSRF) Protection middleware
// MUST be placed AFTER UseCors() and BEFORE UseAuthentication()
app.UseAntiforgery();

// Rate limiting middleware - placed after CORS/antiforgery, before auth
app.UseRateLimiter();

// CorrelationId middleware - adds correlation ID to all log entries
// Placed before auth so unauthenticated requests also get correlation IDs
app.UseMiddleware<CorrelationIdMiddleware>();

// CSRF token generation endpoint for React SPA
// Microsoft standard pattern for .NET 10 Minimal APIs with JSON
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

// Authentication middleware must come before authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard - MUST be after UseAuthentication/UseAuthorization
// so the auth filter can check httpContext.User.Identity.IsAuthenticated
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// UserContext middleware - enriches logs with authenticated user info
// Placed after auth so user claims are available
app.UseMiddleware<UserContextMiddleware>();

// Serilog request logging - MUST be after CorrelationId and UserContext middleware
// so that LogContext properties (CorrelationId, UserId) are in scope when the
// request completion log event is written
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        // Exclude health check endpoints from request logging
        var path = httpContext.Request.Path.Value;
        if (path == "/health-check" || path == "/healthz")
            return LogEventLevel.Verbose;

        if (ex != null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
    };

    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

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

// Schedule daily log summary aggregation at 1 AM UTC
RecurringJob.AddOrUpdate<DailyLogSummaryJob>(
    "daily-log-summary",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 1 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

// Schedule log retention cleanup at 3 AM UTC (deletes logs older than 90 days)
RecurringJob.AddOrUpdate<LogRetentionCleanupJob>(
    "log-retention-cleanup",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 3 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

// Schedule event email automation (reminders, thank-you) hourly for 2-hour precision
RecurringJob.AddOrUpdate<EmailSchedulerJob>(
    "event-email-scheduler",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 * * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }

// Hangfire Dashboard Authorization - Admin only
// Protects the /hangfire dashboard from unauthorized access.
// Requires the user to be authenticated AND have the Administrator role.
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Administrator");
    }
}
