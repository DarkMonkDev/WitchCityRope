using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;

namespace TestDatabaseSeeding;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add DbContext
        services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!";
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging();
        });

        // Add Identity
        services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>()
            .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
            .AddDefaultTokenProviders();

        // Register custom user store
        services.AddScoped<WitchCityRopeUserStore>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Test database connection and seeding
        using (var scope = serviceProvider.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRopeRole>>();
                
                logger.LogInformation("=== Testing Database Seeding ===");
                logger.LogInformation("Testing database connection...");
                
                // Test connection
                if (await context.Database.CanConnectAsync())
                {
                    logger.LogInformation("✓ Database connection successful!");
                    
                    // Check if migrations are pending
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        logger.LogWarning($"⚠ There are {pendingMigrations.Count()} pending migrations:");
                        foreach (var migration in pendingMigrations)
                        {
                            logger.LogWarning($"  - {migration}");
                        }
                        
                        Console.WriteLine("\nWould you like to apply migrations? (y/n)");
                        var response = Console.ReadLine();
                        if (response?.ToLower() == "y")
                        {
                            logger.LogInformation("Applying migrations...");
                            await context.Database.MigrateAsync();
                            logger.LogInformation("✓ Migrations applied successfully!");
                        }
                    }
                    else
                    {
                        logger.LogInformation("✓ No pending migrations");
                    }
                    
                    // Check existing data
                    var userCount = await context.Users.CountAsync();
                    var roleCount = await context.Roles.CountAsync();
                    var eventCount = await context.Events.CountAsync();
                    
                    logger.LogInformation($"\nCurrent database state:");
                    logger.LogInformation($"  - Users: {userCount}");
                    logger.LogInformation($"  - Roles: {roleCount}");
                    logger.LogInformation($"  - Events: {eventCount}");
                    
                    if (userCount == 0 && roleCount == 0 && eventCount == 0)
                    {
                        Console.WriteLine("\nDatabase is empty. Would you like to seed it? (y/n)");
                        var response = Console.ReadLine();
                        if (response?.ToLower() == "y")
                        {
                            // Run seeding
                            logger.LogInformation("\nStarting database seeding...");
                            await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
                            logger.LogInformation("✓ Database seeding completed!");
                            
                            // Check data after seeding
                            userCount = await context.Users.CountAsync();
                            roleCount = await context.Roles.CountAsync();
                            eventCount = await context.Events.CountAsync();
                            
                            logger.LogInformation($"\nDatabase state after seeding:");
                            logger.LogInformation($"  - Users: {userCount}");
                            logger.LogInformation($"  - Roles: {roleCount}");
                            logger.LogInformation($"  - Events: {eventCount}");
                            
                            // List some sample data
                            var users = await context.Users.Take(5).ToListAsync();
                            logger.LogInformation("\nSample users created:");
                            foreach (var user in users)
                            {
                                logger.LogInformation($"  - {user.Email} ({user.SceneName.Value})");
                            }
                            
                            var events = await context.Events.Take(5).ToListAsync();
                            logger.LogInformation("\nSample events created:");
                            foreach (var evt in events)
                            {
                                logger.LogInformation($"  - {evt.Title} on {evt.StartDate:yyyy-MM-dd}");
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation("\nDatabase already contains data. Skipping seeding.");
                    }
                }
                else
                {
                    logger.LogError("✗ Failed to connect to database!");
                    logger.LogError("Please check your connection string and ensure PostgreSQL is running.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "✗ Error during database test");
            }
        }

        Console.WriteLine("\nTest completed. Press any key to exit...");
        Console.ReadKey();
    }
}