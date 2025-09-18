using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Tests.Common.TestBase;

/// <summary>
/// Base class for tests that need database access with TestContainers
/// Provides PostgreSQL database integration for realistic testing
/// </summary>
[Collection("Database")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected DatabaseTestFixture DatabaseFixture { get; private set; }
    protected ApplicationDbContext DbContext { get; private set; }

    protected DatabaseTestBase(DatabaseTestFixture fixture)
    {
        DatabaseFixture = fixture;
        DbContext = null!; // Will be set in InitializeAsync
    }

    public virtual async Task InitializeAsync()
    {
        // Create fresh DbContext for this test
        DbContext = DatabaseFixture.CreateDbContext();

        // Reset database to clean state for this test
        await DatabaseFixture.ResetDatabaseAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // Dispose the DbContext after each test
        await DbContext.DisposeAsync();
    }

    /// <summary>
    /// Helper method to save changes and return the number of affected entities
    /// </summary>
    protected async Task<int> SaveChangesAsync()
    {
        return await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to create a fresh DbContext when needed
    /// </summary>
    protected ApplicationDbContext CreateNewDbContext()
    {
        return DatabaseFixture.CreateDbContext();
    }

    /// <summary>
    /// Helper method to verify data was saved to database
    /// Uses a separate DbContext to ensure we're not reading from change tracker
    /// </summary>
    protected async Task<T?> VerifyEntitySaved<T>(Guid id) where T : class
    {
        await using var verificationContext = CreateNewDbContext();
        return await verificationContext.Set<T>().FindAsync(id);
    }

    /// <summary>
    /// Helper method to count entities in database
    /// Uses a separate DbContext to ensure accurate count
    /// </summary>
    protected async Task<int> CountEntities<T>() where T : class
    {
        await using var countContext = CreateNewDbContext();
        return countContext.Set<T>().Count();
    }
}