using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using WitchCityRope.IntegrationTests.HealthChecks;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Tests that validate database health before running integration tests
/// These tests should be run first to ensure the database is ready
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class DatabaseHealthTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ILogger<DatabaseHealthTests> _logger;

    public DatabaseHealthTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<DatabaseHealthTests>();
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public async Task DatabaseConnection_ShouldBeHealthy()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var dbLogger = loggerFactory.CreateLogger<DatabaseHealthCheck>();
        var healthCheck = new DatabaseHealthCheck(_fixture.ConnectionString, dbLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
        Assert.Contains("Database connection is responsive", result.Description);
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public async Task DatabaseSchema_ShouldBeComplete()
    {
        // Arrange - Initialize database with proper schema via TestWebApplicationFactory
        await using var factory = new TestWebApplicationFactory(_fixture.PostgresContainer);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
        
        // Ensure database is created (this will be done by TestWebApplicationFactory)
        _logger.LogInformation("Database schema initialized via TestWebApplicationFactory");
        
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var schemaLogger = loggerFactory.CreateLogger<DatabaseSchemaHealthCheck>();
        var healthCheck = new DatabaseSchemaHealthCheck(_fixture.ConnectionString, schemaLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        Assert.True(result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ||
                    result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    $"Database schema check failed: {result.Description}");
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public async Task SeedData_ShouldBePresent()
    {
        // Arrange - Initialize database with seed data via TestWebApplicationFactory
        await using var factory = new TestWebApplicationFactory(_fixture.PostgresContainer);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
        
        // Database will be created with seed data by TestWebApplicationFactory
        _logger.LogInformation("Database seed data initialized via TestWebApplicationFactory");
        
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var seedLogger = loggerFactory.CreateLogger<SeedDataHealthCheck>();
        var healthCheck = new SeedDataHealthCheck(_fixture.ConnectionString, seedLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        Assert.True(result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ||
                    result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    $"Seed data check failed: {result.Description}");
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public async Task ComprehensiveHealthCheck_ShouldPass()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var healthCheckLogger = loggerFactory.CreateLogger<IntegrationTestHealthChecker>();
        var healthChecker = new IntegrationTestHealthChecker(_fixture.ConnectionString, healthCheckLogger);

        // Arrange - Initialize database fully via TestWebApplicationFactory
        await using var factory = new TestWebApplicationFactory(_fixture.PostgresContainer);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
        
        // Database will be fully initialized by TestWebApplicationFactory
        _logger.LogInformation("Database fully initialized via TestWebApplicationFactory");

        // Act & Assert - Should not throw any exception
        var exception = await Record.ExceptionAsync(async () =>
        {
            await healthChecker.EnsureHealthyAsync();
        });

        Assert.Null(exception);
    }
}