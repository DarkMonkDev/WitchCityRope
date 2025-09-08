using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Api.Services;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring API services and middleware
/// </summary>
public static class ApiConfiguration
{
    /// <summary>
    /// Adds core API services to the service collection
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add controllers
        services.AddControllers();
        
        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        // Add OpenAPI/Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WitchCity Rope API",
                Version = "v1",
                Description = "API for the WitchCity Rope community platform",
                Contact = new OpenApiContact
                {
                    Name = "WitchCity Rope",
                    Email = "support@witchcityrope.com"
                }
            });

            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Health checks are already added in Infrastructure layer

        // Add response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        // Add HTTP client factory
        services.AddHttpClient();

        // Add memory cache
        services.AddMemoryCache();

        // Add distributed cache (Redis in production, in-memory for development)
        if (configuration.GetValue<bool>("UseRedis"))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization services
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");

        services.AddAuthentication(options =>
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
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Allow JWT tokens to be passed as query parameters for SignalR
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Define authorization policies
            options.AddPolicy("RequireVettedMember", policy =>
                policy.RequireRole("Member").RequireClaim("IsVetted", "true"));
            
            options.AddPolicy("RequireOrganizer", policy =>
                policy.RequireRole("Organizer", "Admin"));
            
            options.AddPolicy("RequireVettingTeam", policy =>
                policy.RequireRole("VettingTeam", "Admin"));
            
            options.AddPolicy("RequireSafetyTeam", policy =>
                policy.RequireRole("SafetyTeam", "Admin"));
            
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }

    /// <summary>
    /// Adds database context and related services
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=your_password_here";
        
        services.AddDbContext<WitchCityRopeDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Add repositories
        // TODO: Implement repository pattern
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IEventRepository, EventRepository>();
        // services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        // services.AddScoped<IApplicationRepository, ApplicationRepository>();
        // services.AddScoped<IIncidentRepository, IncidentRepository>();
        // services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }

    /// <summary>
    /// Adds application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add core services
        services.AddScoped<Services.IPasswordHasher, Services.PasswordHasher>();
        // Add the IPasswordHasher for AuthService (different interface)
        services.AddScoped<Features.Auth.Services.IPasswordHasher, Features.Auth.Services.PasswordHasherAdapter>();
        // Note: JwtTokenService doesn't implement an interface - registered directly in Infrastructure
        // Note: EmailService is registered in Infrastructure DependencyInjection
        // TODO: Implement these services
        // services.AddScoped<INotificationService, NotificationService>();
        // Note: EncryptionService is registered in Infrastructure DependencyInjection
        
        // Add repository implementations
        services.AddScoped<WitchCityRope.Api.Services.IUserRepository, Services.UserRepository>();
        // Add the IUserRepository for AuthService (different interface)
        services.AddScoped<WitchCityRope.Api.Features.Auth.Services.IUserRepository, Features.Auth.Services.AuthUserRepository>();
        
        // Add JWT service adapter
        services.AddScoped<WitchCityRope.Api.Interfaces.IJwtService, Services.JwtServiceAdapter>();
        // Add the IJwtService for AuthService (different interface)
        services.AddScoped<Features.Auth.Services.IJwtService, Features.Auth.Services.JwtServiceAdapter>();
        
        // Add the IEmailService for AuthService (different interface)
        services.AddScoped<Features.Auth.Services.IEmailService, Features.Auth.Services.EmailServiceAdapter>();
        
        // Add the IEncryptionService for AuthService (different interface)
        services.AddScoped<Features.Auth.Services.IEncryptionService, Features.Auth.Services.EncryptionServiceAdapter>();
        
        // Add direct service implementations
        services.AddScoped<IAuthService, Features.Auth.Services.AuthService>();
        services.AddScoped<Services.IUserService, Services.UserService>();
        services.AddScoped<IEventService, Features.Events.Services.EventService>();
        services.AddScoped<IRegistrationService, Services.RegistrationService>();
        services.AddScoped<IPaymentService, Services.PaymentService>();
        services.AddScoped<ISafetyService, Services.SafetyService>();
        
        // Add feature services
        // TODO: Implement these services
        // services.AddScoped<ITwoFactorService, TwoFactorService>();
        // services.AddScoped<IStripeService, StripeService>();
        // services.AddScoped<IPaymentValidationService, PaymentValidationService>();
        services.AddScoped<IVettingService, VettingService>();
        // services.AddScoped<ISafetyTeamService, SafetyTeamService>();
        services.AddScoped<ICheckInService, CheckInService>();
        // services.AddScoped<IWaiverService, WaiverService>();
        
        // Add utility services
        services.AddScoped<ISlugGenerator, SlugGenerator>();
        // TODO: Implement UserContext
        // services.AddScoped<IUserContext, UserContext>();
        
        // Add hosted services
        // TODO: Implement hosted services
        // services.AddHostedService<DatabaseMigrationService>();
        // services.AddHostedService<ExpiredTokenCleanupService>();

        return services;
    }

    /// <summary>
    /// Configures CORS policies
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("X-Total-Count", "X-Page-Number", "X-Page-Size");
            });

            // Development policy - allows specific origins with credentials
            options.AddPolicy("DevelopmentPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins.Length > 0 ? allowedOrigins : new[] { 
                        "http://localhost:5173", 
                        "http://localhost:5174",
                        "http://localhost:5651"
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("X-Total-Count", "X-Page-Number", "X-Page-Size");
            });
        });

        return services;
    }

    /// <summary>
    /// Configures application middleware pipeline
    /// </summary>
    public static WebApplication ConfigureApiPipeline(this WebApplication app)
    {
        // Exception handling
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WitchCity Rope API V1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at root
            });
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        // Core middleware
        app.UseHttpsRedirection();
        app.UseResponseCompression();
        
        // CORS
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevelopmentPolicy");
        }
        else
        {
            app.UseCors("DefaultPolicy");
        }

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Request logging
        app.UseHttpLogging();

        // Health checks
        // Note: Disabled in test environments to avoid duplicate registration
        if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            app.MapHealthChecks("/health");
        }

        // Controllers
        app.MapControllers();

        return app;
    }
}


