
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Features.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration for PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Use environment-aware connection string (container-friendly)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!";
    options.UseNpgsql(connectionString);
});

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
            return Task.CompletedTask;
        }
    };
});

// Service layer registration (services still used by remaining controllers)
// Note: IEventService removed due to EventsController migration
// Keep IAuthService for ProtectedController and IJwtService for authentication functionality
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Add memory cache for CheckIn system performance
builder.Services.AddMemoryCache();

// New vertical slice feature services
builder.Services.AddFeatureServices(builder.Configuration);

// Health checks for database monitoring
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!");

// Configure CORS for React development
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment",
        builder => builder
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:5174", "http://127.0.0.1:5173", "http://localhost:8080")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true)); // Allow any origin in development
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactDevelopment");

// Authentication middleware must come before authorization
app.UseAuthentication();
app.UseAuthorization();

// Existing controller endpoints (to be migrated)
app.MapControllers();

// Legacy health check endpoint (will be replaced by new Health feature)
app.MapHealthChecks("/health-check");

// New vertical slice feature endpoints
app.MapFeatureEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }

// API test $(date)
// API hot reload test Sun Aug 17 03:43:42 PM EDT 2025
