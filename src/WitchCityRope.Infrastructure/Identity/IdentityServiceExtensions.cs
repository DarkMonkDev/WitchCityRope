using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Extension methods for configuring Identity services
    /// </summary>
    public static class IdentityServiceExtensions
    {
        /// <summary>
        /// Adds WitchCityRope Identity services to the service collection
        /// </summary>
        public static IServiceCollection AddWitchCityRopeIdentity(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Add the Identity DbContext
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Configure Identity
            services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = 
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";

                // Sign in settings
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
            .AddDefaultTokenProviders()
            .AddUserStore<WitchCityRopeUserStore>()
            .AddSignInManager<WitchCityRopeSignInManager>();

            // Add custom services
            services.AddScoped<WitchCityRopeUserStore>();
            services.AddScoped<IdentityMigrationHelper>();

            // Configure cookie authentication
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "WitchCityRope.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/auth/access-denied";
            });

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                // Age verification policy
                options.AddPolicy("AgeVerified", policy =>
                    policy.RequireAssertion(context =>
                    {
                        var user = context.User;
                        if (user.Identity?.IsAuthenticated != true)
                            return false;

                        // The age is already validated during registration
                        // This is just an additional check
                        return true;
                    }));

                // Vetted user policy
                options.AddPolicy("VettedUser", policy =>
                    policy.RequireClaim("IsVetted", "True"));

                // Role-based policies
                options.AddPolicy("AttendeeOnly", policy =>
                    policy.RequireRole("Attendee", "Member", "Organizer", "Moderator", "Administrator"));

                options.AddPolicy("MemberOnly", policy =>
                    policy.RequireRole("Member", "Organizer", "Moderator", "Administrator"));

                options.AddPolicy("OrganizerOnly", policy =>
                    policy.RequireRole("Organizer", "Moderator", "Administrator"));

                options.AddPolicy("ModeratorOnly", policy =>
                    policy.RequireRole("Moderator", "Administrator"));

                options.AddPolicy("AdministratorOnly", policy =>
                    policy.RequireRole("Administrator"));

                // Event management policy
                options.AddPolicy("CanManageEvents", policy =>
                    policy.RequireRole("Organizer", "Moderator", "Administrator"));

                // Vetting review policy
                options.AddPolicy("CanReviewVetting", policy =>
                    policy.RequireRole("Moderator", "Administrator"));

                // Incident management policy
                options.AddPolicy("CanManageIncidents", policy =>
                    policy.RequireRole("Moderator", "Administrator"));
            });

            return services;
        }

        /// <summary>
        /// Configures Identity for API usage (JWT instead of cookies)
        /// </summary>
        public static IServiceCollection AddWitchCityRopeIdentityForApi(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure Identity WITHOUT DbContext (already registered by Infrastructure)
            services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = 
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";

                // Sign in settings
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
            .AddDefaultTokenProviders()
            .AddUserStore<WitchCityRopeUserStore>()
            .AddSignInManager<WitchCityRopeSignInManager>();

            // Add custom services
            services.AddScoped<WitchCityRopeUserStore>();
            services.AddScoped<IdentityMigrationHelper>();

            // API services don't use cookies - JWT Bearer authentication is configured separately
            // Remove cookie authentication to avoid conflicts with JWT Bearer middleware

            // Note: Authorization policies will be added by AddApiAuthentication
            // to avoid conflicts with service registration

            return services;
        }
    }
}