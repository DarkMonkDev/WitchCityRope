using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid.Extensions.DependencyInjection;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Email;
using WitchCityRope.Infrastructure.PayPal;
using WitchCityRope.Infrastructure.Security;
using WitchCityRope.Infrastructure.Mapping;

namespace WitchCityRope.Infrastructure
{
    /// <summary>
    /// Infrastructure layer dependency injection configuration
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds infrastructure services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Entity Framework Core with PostgreSQL
            services.AddDbContext<WitchCityRopeDbContext>(options =>
            {
                // Check for Aspire-provided connection string first, then fall back to DefaultConnection
                var connectionString = configuration.GetConnectionString("witchcityrope-db") 
                    ?? configuration.GetConnectionString("DefaultConnection") 
                    ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!";
                options.UseNpgsql(connectionString);
                
                // Enable sensitive data logging in development
                if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging", false))
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            // Add SendGrid for email
            var sendGridApiKey = configuration["Email:SendGrid:ApiKey"];
            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                services.AddSendGrid(options =>
                {
                    options.ApiKey = sendGridApiKey;
                });
            }

            // Register services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPaymentService, PayPalService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<JwtTokenService>();

            // Add AutoMapper with all profiles
            services.AddAutoMapper(typeof(EventProfile).Assembly);

            // Add health checks
            services.AddHealthChecks()
                .AddDbContextCheck<WitchCityRopeDbContext>("database");

            return services;
        }

        /// <summary>
        /// Ensures the database is created and migrations are applied
        /// </summary>
        public static void EnsureDatabaseCreated(this IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
                context.Database.EnsureCreated();
                
                // In production, use migrations instead:
                // context.Database.Migrate();
            }
        }

        /// <summary>
        /// Seeds the database with initial data if needed
        /// </summary>
        public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
                var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

                // Check if we already have data
                if (await context.Users.AnyAsync())
                {
                    return;
                }

                // Seed initial data here if needed
                // Example: Create default admin user
                // var adminUser = new User(
                //     await encryptionService.EncryptAsync("Admin User"),
                //     new SceneName("Admin"),
                //     new EmailAddress("admin@witchcityrope.com"),
                //     DateTime.UtcNow.AddYears(-30),
                //     UserRole.Admin
                // );
                // context.Users.Add(adminUser);
                // await context.SaveChangesAsync();
            }
        }
    }
}