using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Tests.Fixtures
{
    /// <summary>
    /// Factory for creating test database contexts
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates an in-memory SQLite database context for fast unit tests
        /// </summary>
        public static WitchCityRopeDbContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new WitchCityRopeDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Creates an in-memory database context using Entity Framework's InMemory provider
        /// Note: This doesn't enforce referential integrity like a real database
        /// </summary>
        public static WitchCityRopeDbContext CreateInMemoryEfContext()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new WitchCityRopeDbContext(options);
            return context;
        }

        /// <summary>
        /// Creates a context with a specific database provider options
        /// </summary>
        public static WitchCityRopeDbContext CreateContext(DbContextOptions<WitchCityRopeDbContext> options)
        {
            return new WitchCityRopeDbContext(options);
        }

        /// <summary>
        /// Creates a context and seeds it with test data
        /// </summary>
        public static async Task<WitchCityRopeDbContext> CreateSeededInMemoryContextAsync()
        {
            var context = CreateInMemoryContext();
            await SeedTestDataAsync(context);
            return context;
        }

        private static async Task SeedTestDataAsync(WitchCityRopeDbContext context)
        {
            // Add seed data if needed
            await context.SaveChangesAsync();
        }
    }
}