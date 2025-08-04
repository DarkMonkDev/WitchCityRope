using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Fixtures
{
    /// <summary>
    /// Base class for integration tests that need database access
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected WitchCityRopeIdentityDbContext Context { get; private set; } = null!;
        protected DbContextOptions<WitchCityRopeIdentityDbContext> Options { get; private set; } = null!;

        /// <summary>
        /// Override to use a different database provider (default is SQLite in-memory)
        /// </summary>
        protected virtual bool UseRealDatabase => false;

        public virtual async Task InitializeAsync()
        {
            if (UseRealDatabase)
            {
                // Use TestContainers PostgreSQL for real database tests
                var fixture = new DatabaseFixture();
                await fixture.InitializeAsync();
                Context = fixture.CreateContext();
                Options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
                    .UseNpgsql(fixture.ConnectionString)
                    .Options;
            }
            else
            {
                // Use SQLite in-memory for fast tests
                Context = TestDbContextFactory.CreateInMemoryContext();
                Options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
            }

            await SeedDataAsync();
        }

        public virtual async Task DisposeAsync()
        {
            await Context.DisposeAsync();
        }

        /// <summary>
        /// Override to seed test data
        /// </summary>
        protected virtual async Task SeedDataAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates a new context instance with the same options
        /// Useful for testing scenarios that require multiple contexts
        /// </summary>
        protected WitchCityRopeIdentityDbContext CreateNewContext()
        {
            return new WitchCityRopeIdentityDbContext(Options);
        }

        /// <summary>
        /// Clears the change tracker to simulate a new context
        /// </summary>
        protected void ClearChangeTracker()
        {
            Context.ChangeTracker.Clear();
        }

        /// <summary>
        /// Helper method to execute a test with a separate context
        /// </summary>
        protected async Task ExecuteWithNewContextAsync(Func<WitchCityRopeIdentityDbContext, Task> action)
        {
            using var newContext = CreateNewContext();
            await action(newContext);
        }

        /// <summary>
        /// Helper method to execute a test and return a result with a separate context
        /// </summary>
        protected async Task<T> ExecuteWithNewContextAsync<T>(Func<WitchCityRopeIdentityDbContext, Task<T>> action)
        {
            using var newContext = CreateNewContext();
            return await action(newContext);
        }
    }
}