using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests
{
    /// <summary>
    /// Integration Test Base Class
    /// Phase 2: Test Suite Integration - Enhanced Containerized Testing Infrastructure
    /// 
    /// Features:
    /// - Inherits from IClassFixture<DatabaseTestFixture> for container sharing
    /// - Provides connection string and service provider access
    /// - Supports test isolation through database reset
    /// - Transaction rollback patterns for cleanup
    /// - Service configuration for integration scenarios
    /// </summary>
    [Collection("Database")]
    public abstract class IntegrationTestBase : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
    {
        protected readonly DatabaseTestFixture DatabaseFixture;
        protected readonly string ConnectionString;
        protected readonly IServiceProvider Services;
        protected readonly ILogger<IntegrationTestBase> Logger;

        protected IntegrationTestBase(DatabaseTestFixture fixture)
        {
            DatabaseFixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            ConnectionString = fixture.ConnectionString;
            Services = BuildServiceProvider();
            
            // Create logger for test operations
            using var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            Logger = loggerFactory.CreateLogger<IntegrationTestBase>();
        }

        /// <summary>
        /// Builds a service provider configured for integration testing
        /// with the containerized PostgreSQL database
        /// </summary>
        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Configure Entity Framework with test database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
                
                // Enable detailed logging for debugging
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                
                // Disable connection pooling for integration tests
                options.EnableServiceProviderCaching(false);
                
                // Configure warnings for test environment
                options.ConfigureWarnings(warnings =>
                {
                    // Ignore pending model changes warning in test environment
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                });
            });

            // Add logging services
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Register common test services here
            // Add any additional services needed for integration testing
            
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a fresh database context for test operations
        /// </summary>
        protected ApplicationDbContext CreateDbContext()
        {
            return DatabaseFixture.CreateDbContext();
        }

        /// <summary>
        /// Gets a scoped service from the test service provider
        /// </summary>
        protected T GetService<T>() where T : notnull
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Executes code within a database transaction scope
        /// Automatically rolls back on completion for test isolation
        /// </summary>
        protected async Task<T> ExecuteInTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
        {
            await using var context = CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                var result = await operation(context);
                
                // Intentionally do not commit - let transaction roll back for isolation
                return result;
            }
            finally
            {
                // Transaction will automatically roll back when disposed
            }
        }

        /// <summary>
        /// Executes code within a database transaction scope (void return)
        /// Automatically rolls back on completion for test isolation
        /// </summary>
        protected async Task ExecuteInTransactionAsync(Func<ApplicationDbContext, Task> operation)
        {
            await using var context = CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                await operation(context);
                
                // Intentionally do not commit - let transaction roll back for isolation
            }
            finally
            {
                // Transaction will automatically roll back when disposed
            }
        }

        /// <summary>
        /// Initializes the test - called before each test method
        /// Resets the database to a clean state for test isolation
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                Logger.LogInformation("Initializing integration test - resetting database state");
                
                // Reset database to clean state for each test
                await DatabaseFixture.ResetDatabaseAsync();
                
                Logger.LogInformation("Integration test initialization completed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize integration test");
                throw;
            }
        }

        /// <summary>
        /// Cleans up after test completion - called after each test method
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            try
            {
                Logger.LogInformation("Disposing integration test resources");
                
                // Additional cleanup if needed
                // The database reset happens in InitializeAsync for the next test
                
                Logger.LogInformation("Integration test disposal completed");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error during integration test disposal");
                // Don't rethrow disposal exceptions to prevent masking test failures
            }
        }
    }
}