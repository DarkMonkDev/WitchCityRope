using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Base class for integration tests that handles factory lifecycle
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlFixture PostgresFixture;
    protected readonly TestWebApplicationFactory Factory;
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(PostgreSqlFixture postgresFixture)
    {
        PostgresFixture = postgresFixture;
        Factory = new TestWebApplicationFactory(postgresFixture.PostgresContainer);
    }

    public virtual Task InitializeAsync()
    {
        // Create HTTP client after factory is created
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
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
}