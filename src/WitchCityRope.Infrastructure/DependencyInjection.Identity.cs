using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Services;
using WitchCityRope.Infrastructure.PayPal;
using SendGrid.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.Services;
using WitchCityRope.Core.Repositories;
using WitchCityRope.Infrastructure.Email;
using WitchCityRope.Infrastructure.Security;
using WitchCityRope.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Infrastructure services with Identity support
    /// </summary>
    public static class DependencyInjectionIdentity
    {
        /// <summary>
        /// Adds all Infrastructure services including Identity DbContext
        /// </summary>
        public static IServiceCollection AddInfrastructureWithIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext with Identity support
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection") 
                    ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=your_password_here";
                
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(WitchCityRopeIdentityDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
            });

            // Add common infrastructure services
            services.AddInfrastructureServicesCommon(configuration);

            return services;
        }

        /// <summary>
        /// Adds Infrastructure services without DbContext (for use when DbContext is registered elsewhere)
        /// </summary>
        public static IServiceCollection AddInfrastructureWithoutDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddInfrastructureServicesCommon(configuration);
        }

        /// <summary>
        /// Adds common Infrastructure services (everything except DbContext)
        /// </summary>
        private static IServiceCollection AddInfrastructureServicesCommon(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Email Service
            var sendGridApiKey = configuration["Email:SendGrid:ApiKey"];
            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                services.AddSendGrid(options =>
                {
                    options.ApiKey = sendGridApiKey;
                });
                services.AddScoped<IEmailService, EmailService>();
            }
            else
            {
                // Use mock email service for development
                services.AddScoped<IEmailService, MockEmailService>();
            }

            // Add Payment Service
            var stripeSecretKey = configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(stripeSecretKey))
            {
                services.AddScoped<IPaymentService, PayPalService>();
            }
            else
            {
                // Use mock payment processor for development
                services.AddScoped<IPaymentService, MockPaymentService>();
            }

            // Add Encryption Service
            services.AddScoped<IEncryptionService, EncryptionService>();

            // Add JWT Service
            services.AddScoped<JwtTokenService>();

            // Add Slug Generator
            services.AddScoped<ISlugGenerator, SlugGenerator>();

            // Add Content Page Service
            services.AddScoped<IContentPageService, ContentPageService>();

            // Add Volunteer Service
            services.AddScoped<IVolunteerService, VolunteerService>();

            // Add Event Email Service
            services.AddScoped<IEventEmailService, EventEmailService>();

            // Add Repositories
            services.AddScoped<IUserNoteRepository, UserNoteRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();

            // Add Member Management Service
            services.AddScoped<IMemberManagementService, MemberManagementService>();

            // Add AutoMapper with all profiles
            services.AddAutoMapper(typeof(Mapping.EventProfile).Assembly);

            // Add Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<WitchCityRopeIdentityDbContext>("database");

            return services;
        }

        /// <summary>
        /// Ensures database is created and migrations are applied
        /// </summary>
        public static async Task<IServiceProvider> EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRopeRole>>();
            
            // Apply migrations
            await context.Database.MigrateAsync();
            
            // Ensure roles exist
            var roles = new[]
            {
                ("Attendee", "Standard event attendee", 0),
                ("Member", "Verified community member with additional privileges", 1),
                ("Organizer", "Event organizer who can create and manage events", 2),
                ("Moderator", "Community moderator who can review incidents and vetting", 3),
                ("Administrator", "System administrator with full access", 4)
            };

            foreach (var (name, description, priority) in roles)
            {
                if (!await roleManager.RoleExistsAsync(name))
                {
                    var role = new WitchCityRopeRole(name)
                    {
                        Description = description,
                        Priority = priority
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            // Create default admin user if it doesn't exist
            var adminEmail = "admin@witchcityrope.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new WitchCityRopeUser(
                    encryptedLegalName: "ENCRYPTED_ADMIN_NAME", // Should be properly encrypted
                    sceneName: Core.ValueObjects.SceneName.Create("Admin"),
                    email: Core.ValueObjects.EmailAddress.Create(adminEmail),
                    dateOfBirth: new DateTime(1990, 1, 1),
                    role: Core.Enums.UserRole.Administrator
                );
                
                adminUser.UserName = adminEmail;
                adminUser.EmailConfirmed = true;
                
                var result = await userManager.CreateAsync(adminUser, "Admin123!"); // Change this password!
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            return serviceProvider;
        }
    }
}