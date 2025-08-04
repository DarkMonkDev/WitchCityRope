using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Enhanced base class for integration tests that provides database cleanup between tests
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public abstract class IntegrationTestBaseWithCleanup : IAsyncLifetime
{
    protected readonly PostgreSqlFixture PostgresFixture;
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    
    private readonly DatabaseCleaner _databaseCleaner;
    private readonly ILogger<IntegrationTestBaseWithCleanup> _logger;

    protected IntegrationTestBaseWithCleanup(PostgreSqlFixture postgresFixture)
    {
        PostgresFixture = postgresFixture;
        
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<IntegrationTestBaseWithCleanup>();
        _databaseCleaner = new DatabaseCleaner(loggerFactory.CreateLogger<DatabaseCleaner>());
    }

    public virtual async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing test: {TestName}", GetType().Name);
        
        // Create a fresh factory for each test
        Factory = new TestWebApplicationFactory(PostgresFixture.PostgresContainer);
        
        // Create HTTP client after factory is created
        Client = Factory.CreateClient();
        
        // Optionally reset database to known state
        if (ShouldResetDatabaseBeforeTest)
        {
            await ResetDatabaseAsync();
        }
    }

    public virtual async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing test: {TestName}", GetType().Name);
        
        Client?.Dispose();
        
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }
    
    /// <summary>
    /// Override to control whether database should be reset before each test
    /// Default is true for safety
    /// </summary>
    protected virtual bool ShouldResetDatabaseBeforeTest => true;

    /// <summary>
    /// Resets the database to a clean state using Respawn
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        _logger.LogInformation("Resetting database for test");
        
        try
        {
            await _databaseCleaner.ResetAsync(PostgresFixture.ConnectionString);
            
            // Re-seed base data after reset
            await WithServiceScope(async sp =>
            {
                var context = sp.GetRequiredService<WitchCityRopeIdentityDbContext>();
                await SeedBaseTestDataAsync(context);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database");
            throw;
        }
    }
    
    /// <summary>
    /// Seeds base test data that should exist for all tests
    /// Override to add custom seed data
    /// </summary>
    protected virtual async Task SeedBaseTestDataAsync(WitchCityRopeIdentityDbContext context)
    {
        // Base implementation can seed roles, etc.
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets a service from the test server's service provider
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Executes an action within a service scope
    /// </summary>
    protected async Task WithServiceScope(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Executes an action within a service scope and returns a result
    /// </summary>
    protected async Task<T> WithServiceScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }
    
    /// <summary>
    /// Helper to directly access the database context for test assertions
    /// </summary>
    protected async Task<T> AssertInDatabaseAsync<T>(Func<WitchCityRopeIdentityDbContext, Task<T>> assertion)
    {
        return await WithServiceScope(async sp =>
        {
            var context = sp.GetRequiredService<WitchCityRopeIdentityDbContext>();
            return await assertion(context);
        });
    }
}