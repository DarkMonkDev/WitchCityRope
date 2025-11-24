using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Base class for database tests
    /// Provides common setup and teardown for real PostgreSQL database testing
    /// Following patterns from lessons learned for TestContainers integration
    /// </summary>
    public abstract class DatabaseTestBase : IAsyncLifetime
    {
        protected readonly DatabaseTestFixture DatabaseFixture;
        protected WitchCityRopeDbContext DbContext = null!;
        protected Mock<ILogger> MockLogger = null!;

        protected DatabaseTestBase(DatabaseTestFixture databaseFixture)
        {
            DatabaseFixture = databaseFixture;
        }

        public virtual async Task InitializeAsync()
        {
            // Create fresh DbContext for this test
            DbContext = DatabaseFixture.CreateDbContext();
            
            // Setup mock logger for services that need it
            MockLogger = new Mock<ILogger>();
            
            // Reset database to clean state for this test
            await DatabaseFixture.ResetDatabaseAsync();
        }

        public virtual async Task DisposeAsync()
        {
            DbContext?.Dispose();
        }

        /// <summary>
        /// Creates a scoped service provider for dependency injection testing
        /// </summary>
        protected IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Add DbContext
            services.AddSingleton(DbContext);
            
            // Add mock logger
            services.AddSingleton(MockLogger.Object);
            
            // Add other services as needed for tests
            ConfigureServices(services);
            
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Override in derived classes to add additional services
        /// </summary>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Default implementation - override in derived tests
        }

        /// <summary>
        /// Helper method to save changes and refresh entity tracking
        /// </summary>
        protected async Task SaveAndRefreshAsync()
        {
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
        }
    }
}