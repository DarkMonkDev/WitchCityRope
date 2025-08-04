using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using WitchCityRope.IntegrationTests.HealthChecks;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Shared fixture for PostgreSQL container that starts before any tests run
/// Includes comprehensive health checks to ensure database readiness
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer PostgresContainer { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;
    
    private readonly ILogger<PostgreSqlFixture> _logger;
    
    public PostgreSqlFixture()
    {
        using var factory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = factory.CreateLogger<PostgreSqlFixture>();
    }
    
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing PostgreSQL container for integration tests...");
        
        PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted("pg_isready", "-U", "testuser", "-d", "witchcityrope_test"))
            .Build();
            
        await PostgresContainer.StartAsync();
        ConnectionString = PostgresContainer.GetConnectionString();
        
        _logger.LogInformation("PostgreSQL container started. Connection string: {ConnectionString}", ConnectionString);
        
        // Run comprehensive health checks to ensure database is ready
        await EnsureDatabaseHealthy();
    }
    
    private async Task EnsureDatabaseHealthy()
    {
        _logger.LogInformation("Running basic connectivity check to ensure PostgreSQL container is ready...");
        
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var healthCheckLogger = loggerFactory.CreateLogger<DatabaseHealthCheck>();
        
        var healthChecker = new DatabaseHealthCheck(ConnectionString, healthCheckLogger);
        
        try
        {
            // Only run basic database connectivity check
            // Application-specific schema and seed data will be handled by TestWebApplicationFactory
            await healthChecker.CheckHealthAsync();
                
            _logger.LogInformation("PostgreSQL container connectivity check passed! Container is ready for application initialization.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL container connectivity check failed. Container may not be ready.");
            throw new InvalidOperationException(
                "PostgreSQL container is not ready for integration tests. Please check Docker containers and database setup.", ex);
        }
    }
    
    public async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing PostgreSQL container...");
        await PostgresContainer.DisposeAsync();
    }
}