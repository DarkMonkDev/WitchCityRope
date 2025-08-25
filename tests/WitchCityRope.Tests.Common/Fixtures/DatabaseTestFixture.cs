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
    /// Database test fixture for PostgreSQL TestContainers
    /// Provides shared database instance for test collection
    /// Following patterns from lessons learned for real database testing
    /// </summary>
    public class DatabaseTestFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        private Respawner? _respawner;

        public string ConnectionString => _container?.GetConnectionString() ?? 
            throw new InvalidOperationException("Database container not initialized");

        public async Task InitializeAsync()
        {
            // Create PostgreSQL container following TestContainers best practices
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("witchcityrope_test")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            // Apply migrations to test database
            await using var context = CreateDbContext();
            await context.Database.MigrateAsync();

            // Setup Respawn for fast database cleanup between tests
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
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

        public WitchCityRopeDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
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