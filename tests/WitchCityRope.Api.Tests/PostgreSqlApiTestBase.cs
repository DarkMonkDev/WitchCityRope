using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Api.Tests;

/// <summary>
/// Base class for API tests that require PostgreSQL database
/// </summary>
public abstract class PostgreSqlApiTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer PostgresContainer { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        // Start PostgreSQL container
        PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase($"apitest_{Guid.NewGuid():N}")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .Build();
            
        await PostgresContainer.StartAsync();
        
        ConnectionString = PostgresContainer.GetConnectionString();
        
        // Build service provider with test services
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        // Ensure database is created and migrated
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        if (PostgresContainer != null)
        {
            await PostgresContainer.DisposeAsync();
        }
    }
    
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add DbContext with PostgreSQL
        services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
            options.UseNpgsql(ConnectionString));
    }
    
    protected WitchCityRopeIdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
            
        return new WitchCityRopeIdentityDbContext(options);
    }
    
    protected async Task<WitchCityRopeIdentityDbContext> CreateAndSeedDbContextAsync()
    {
        var context = CreateDbContext();
        // Ensure the database schema is created
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}