using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Cleanup;
using Xunit;

namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Enhanced Database test fixture for PostgreSQL TestContainers
    /// Phase 1: Enhanced Containerized Testing Infrastructure
    /// 
    /// Features:
    /// - Production parity with PostgreSQL 16 Alpine
    /// - Dynamic port allocation for parallel execution
    /// - Container labeling for identification and cleanup
    /// - Automatic cleanup guarantees with Ryuk
    /// - Performance monitoring and optimization
    /// - Comprehensive error handling and logging
    /// </summary>
    public class DatabaseTestFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        private Respawner? _respawner;
        private readonly Stopwatch _performanceTimer = new();
        private readonly ILogger<DatabaseTestFixture> _logger;

        public string ConnectionString => _container?.GetConnectionString() ?? 
            throw new InvalidOperationException("Database container not initialized");

        public string ContainerId => _container?.Id ?? 
            throw new InvalidOperationException("Database container not initialized");

        public DatabaseTestFixture()
        {
            // Create logger for container operations monitoring
            using var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            _logger = loggerFactory.CreateLogger<DatabaseTestFixture>();
        }

        public async Task InitializeAsync()
        {
            _performanceTimer.Start();
            
            try
            {
                _logger.LogInformation("Starting PostgreSQL container initialization...");

                // Create PostgreSQL container with enhanced configuration for Phase 1
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")  // Production parity
                    .WithDatabase("witchcityrope_test")
                    .WithUsername("test_user")
                    .WithPassword("Test123!")
                    .WithPortBinding(0, 5432)  // Dynamic port allocation (port 0)
                    .WithCleanUp(true)  // Ensure Ryuk cleanup
                    // Container labeling for identification and cleanup
                    .WithLabel("project", "witchcityrope")
                    .WithLabel("purpose", "testing")
                    .WithLabel("phase", "phase1-enhancement")
                    .WithLabel("cleanup", "automatic")
                    .WithLabel("created", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    .Build();

                await _container.StartAsync();

                // Register container with cleanup service (after container is started and has an ID)
                ContainerCleanupService.RegisterContainer(_container.Id, "DatabaseTestFixture");

                var startupTime = _performanceTimer.Elapsed;
                _logger.LogInformation("Container started in {StartupTime:F2} seconds. Target: <5 seconds", 
                    startupTime.TotalSeconds);

                if (startupTime.TotalSeconds > 5)
                {
                    _logger.LogWarning("Container startup exceeded 5-second target: {ActualTime:F2}s", 
                        startupTime.TotalSeconds);
                }

                _logger.LogInformation("Container ID: {ContainerId}, Port: {Port}", 
                    _container.Id, _container.GetMappedPublicPort(5432));

                // Apply migrations to test database
                await using var context = CreateDbContext();
                await context.Database.MigrateAsync();

                _logger.LogInformation("EF Core migrations applied successfully");

                // Seed roles and users (ONE TIME - Respawn preserves these)
                await SeedTestDataAsync();

                _logger.LogInformation("Test seed data populated successfully");

                // Setup Respawn for fast database cleanup between tests
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = new[] { "public" },
                    TablesToIgnore = new Respawn.Graph.Table[]
                    {
                        "__EFMigrationsHistory",
                        "Roles",      // Preserve roles created during seed data
                        "Users",      // Preserve test users created during seed data
                        "UserRoles"   // Preserve role assignments for test users
                    }
                });

                _logger.LogInformation("Respawn database cleanup configured");

                _performanceTimer.Stop();
                _logger.LogInformation("Database fixture initialization completed in {TotalTime:F2} seconds", 
                    _performanceTimer.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                _performanceTimer.Stop();
                _logger.LogError(ex, "Failed to initialize database container after {ElapsedTime:F2} seconds", 
                    _performanceTimer.Elapsed.TotalSeconds);
                
                // Ensure cleanup on failure
                await DisposeAsync();
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            var cleanupTimer = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Starting database container cleanup...");

                if (_container != null)
                {
                    // Unregister from cleanup service
                    ContainerCleanupService.UnregisterContainer(_container.Id);

                    await _container.StopAsync();
                    await _container.DisposeAsync();
                    
                    _logger.LogInformation("Container {ContainerId} stopped and disposed successfully", 
                        _container.Id);
                }

                cleanupTimer.Stop();
                _logger.LogInformation("Container cleanup completed in {CleanupTime:F2} seconds. Target: <30 seconds", 
                    cleanupTimer.Elapsed.TotalSeconds);

                if (cleanupTimer.Elapsed.TotalSeconds > 30)
                {
                    _logger.LogWarning("Container cleanup exceeded 30-second target: {ActualTime:F2}s", 
                        cleanupTimer.Elapsed.TotalSeconds);
                }
            }
            catch (Exception ex)
            {
                cleanupTimer.Stop();
                _logger.LogError(ex, "Error during container cleanup after {ElapsedTime:F2} seconds", 
                    cleanupTimer.Elapsed.TotalSeconds);
                
                // Don't rethrow disposal exceptions to prevent masking original test failures
            }
        }

        public ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .ConfigureWarnings(warnings =>
                {
                    // Ignore pending model changes warning in test environment
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                })
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Seeds essential test data (roles and users) that Respawn will preserve.
        /// Called ONCE during fixture initialization, not before each test.
        /// </summary>
        private async Task SeedTestDataAsync()
        {
            _logger.LogInformation("Seeding roles and test users for integration tests");

            // Create temporary service provider with Identity services
            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            // Add Identity services (required for UserManager and RoleManager)
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddLogging(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            await using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed roles
            var roles = new[] { "Administrator", "Teacher", "VettedMember", "Member", "Attendee" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole<Guid>(roleName);
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Seed test users (matching existing test accounts)
            var testUsers = new[]
            {
                new { Email = "admin@witchcityrope.com", Role = "Administrator", SceneName = "Test Admin" },
                new { Email = "teacher@witchcityrope.com", Role = "Teacher", SceneName = "Test Teacher" },
                new { Email = "vetted@witchcityrope.com", Role = "VettedMember", SceneName = "Test Vetted" },
                new { Email = "member@witchcityrope.com", Role = "Member", SceneName = "Test Member" },
                new { Email = "guest@witchcityrope.com", Role = "Attendee", SceneName = "Test Guest" }
            };

            foreach (var userData in testUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(userData.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        Email = userData.Email,
                        UserName = userData.Email,
                        EmailConfirmed = true,
                        SceneName = userData.SceneName,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(user, "Test123!");
                    if (createResult.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(user, userData.Role);
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Created test user {Email} with role {Role}", userData.Email, userData.Role);
                        }
                        else
                        {
                            _logger.LogError("Failed to assign role {Role} to user {Email}: {Errors}",
                                userData.Role, userData.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to create user {Email}: {Errors}",
                            userData.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            _logger.LogInformation("Test data seeding completed");
        }

        public async Task ResetDatabaseAsync()
        {
            if (_respawner == null)
                throw new InvalidOperationException("Database not initialized");

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }
    }

    /// <summary>
    /// Test collection for PostgreSQL integration tests
    /// Ensures tests share the same container instance for performance
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<DatabaseTestFixture>
    {
        // This class has no code, and is never created. Its purpose is just
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}