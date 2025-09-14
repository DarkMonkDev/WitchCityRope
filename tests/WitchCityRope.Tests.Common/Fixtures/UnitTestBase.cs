using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Base class for pure unit tests with no infrastructure dependencies
    /// Provides in-memory database and common mocking patterns
    /// Use this for testing business logic without TestContainers
    /// </summary>
    public abstract class UnitTestBase : IDisposable
    {
        protected WitchCityRopeDbContext DbContext { get; private set; }
        protected Mock<ILogger> MockLogger { get; private set; }
        
        private bool _disposed = false;

        protected UnitTestBase()
        {
            // Create in-memory database for unit tests
            DbContext = CreateInMemoryDbContext();
            MockLogger = new Mock<ILogger>();
        }

        /// <summary>
        /// Creates an in-memory database context for unit testing
        /// Each test gets a unique database to ensure isolation
        /// </summary>
        protected virtual WitchCityRopeDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                {
                    // Ignore warnings about in-memory database limitations
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
                })
                .Options;

            return new WitchCityRopeDbContext(options);
        }

        /// <summary>
        /// Creates a mock DbContext for scenarios where you need full control over mocking
        /// Use this when you need to mock specific DbSet behavior
        /// </summary>
        protected virtual Mock<WitchCityRopeDbContext> CreateMockDbContext()
        {
            var mockContext = new Mock<WitchCityRopeDbContext>();
            
            // Setup common properties that most tests need
            mockContext.Setup(x => x.ChangeTracker).Returns(DbContext.ChangeTracker);
            
            return mockContext;
        }

        /// <summary>
        /// Creates a mock DbSet with the provided data
        /// Useful for testing services that query specific entities
        /// </summary>
        protected virtual Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            
            return mockSet;
        }

        /// <summary>
        /// Helper method to quickly create mock logger for a specific type
        /// </summary>
        protected virtual Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Seeds the in-memory database with test data
        /// Override in derived classes to provide specific test data
        /// </summary>
        protected virtual void SeedTestData()
        {
            // Default implementation - override in derived tests
        }

        /// <summary>
        /// Resets the in-memory database to a clean state
        /// Useful when you need fresh data for a test
        /// </summary>
        protected virtual void ResetDatabase()
        {
            DbContext.Dispose();
            DbContext = CreateInMemoryDbContext();
            SeedTestData();
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                DbContext?.Dispose();
                _disposed = true;
            }
        }
    }
}