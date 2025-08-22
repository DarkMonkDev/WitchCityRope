using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Fixtures
{
    /// <summary>
    /// Provides a shared PostgreSQL database container for integration tests
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _postgresContainer;
        public string ConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("witchcityrope_test")
                .WithUsername("testuser")
                .WithPassword("testpass123")
                .Build();

            await _postgresContainer.StartAsync();
            ConnectionString = _postgresContainer.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            if (_postgresContainer != null)
            {
                await _postgresContainer.DisposeAsync();
            }
        }

        public WitchCityRopeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            var context = new WitchCityRopeDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public DbContextOptions<WitchCityRopeDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
        }
    }

    /// <summary>
    /// Collection definition for tests that use the shared database
    /// </summary>
    [CollectionDefinition("Database Collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}