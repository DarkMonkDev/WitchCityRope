using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// PostgreSQL integration fixture for full-stack integration tests
    /// Provides isolated database instance for complete workflow testing
    /// Following patterns from lessons learned for TestContainers integration
    /// </summary>
    public class PostgreSqlIntegrationFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        private Respawner? _respawner;

        public string ConnectionString => _container?.GetConnectionString() ?? 
            throw new InvalidOperationException("PostgreSQL container not initialized");

        public async Task InitializeAsync()
        {
            // Create PostgreSQL container for integration testing
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("witchcityrope_integration_test")
                .WithUsername("integrationuser")
                .WithPassword("integrationpass")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            // Apply EF Core migrations to integration test database
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var context = new WitchCityRopeDbContext(options);
            await context.Database.MigrateAsync();

            // Setup Respawn for fast database reset between integration tests
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new[] { "__EFMigrationsHistory" },
                WithReseed = true // Reset identity columns for consistent test data
            });
        }

        public async Task DisposeAsync()
        {
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }
        }

        public async Task ResetDatabaseAsync()
        {
            if (_respawner == null)
                throw new InvalidOperationException("Database respawner not initialized");

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        public DbContextOptions<WitchCityRopeDbContext> GetDbContextOptions()
        {
            return new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
        }

        public WitchCityRopeDbContext CreateDbContext()
        {
            return new WitchCityRopeDbContext(GetDbContextOptions());
        }
    }

    /// <summary>
    /// Test collection for PostgreSQL integration tests
    /// Ensures integration tests share the same container instance
    /// </summary>
    [CollectionDefinition("PostgreSQL Integration Tests")]
    public class PostgreSqlIntegrationCollection : ICollectionFixture<PostgreSqlIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is just
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}