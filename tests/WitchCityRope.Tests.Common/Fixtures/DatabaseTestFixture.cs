using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WitchCityRope.Infrastructure.Data;
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

                // Setup Respawn for fast database cleanup between tests
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = new[] { "public" },
                    TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
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

        public WitchCityRopeDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
                .ConfigureWarnings(warnings =>
                {
                    // Ignore pending model changes warning in test environment
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                })
                .Options;
            
            return new WitchCityRopeDbContext(options);
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