using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.IntegrationTests.HealthChecks;

/// <summary>
/// Comprehensive health checker for integration tests
/// Verifies database connectivity, schema, and seed data before running tests
/// </summary>
public class IntegrationTestHealthChecker
{
    private readonly string _connectionString;
    private readonly ILogger<IntegrationTestHealthChecker> _logger;

    public IntegrationTestHealthChecker(string connectionString, ILogger<IntegrationTestHealthChecker> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs all health checks and throws exception if any critical checks fail
    /// </summary>
    public async Task EnsureHealthyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting integration test health checks...");

        // Create specific loggers for each health check
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var dbLogger = loggerFactory.CreateLogger<DatabaseHealthCheck>();
        var schemaLogger = loggerFactory.CreateLogger<DatabaseSchemaHealthCheck>();
        var seedLogger = loggerFactory.CreateLogger<SeedDataHealthCheck>();

        var healthChecks = new List<(string Name, IHealthCheck HealthCheck)>
        {
            ("Database Connection", new DatabaseHealthCheck(_connectionString, dbLogger)),
            ("Database Schema", new DatabaseSchemaHealthCheck(_connectionString, schemaLogger)),
            ("Seed Data", new SeedDataHealthCheck(_connectionString, seedLogger))
        };

        var results = new List<(string Name, HealthCheckResult Result)>();

        foreach (var (name, healthCheck) in healthChecks)
        {
            _logger.LogInformation("Running health check: {HealthCheckName}", name);
            
            try
            {
                var result = await healthCheck.CheckHealthAsync(
                    new HealthCheckContext(), 
                    cancellationToken);
                
                results.Add((name, result));
                
                LogHealthCheckResult(name, result);
            }
            catch (Exception ex)
            {
                var failedResult = HealthCheckResult.Unhealthy($"Health check {name} threw exception", ex);
                results.Add((name, failedResult));
                _logger.LogError(ex, "Health check {HealthCheckName} failed with exception", name);
            }
        }

        // Check if any critical health checks failed
        var failedChecks = results.Where(r => r.Result.Status == HealthStatus.Unhealthy).ToList();
        var degradedChecks = results.Where(r => r.Result.Status == HealthStatus.Degraded).ToList();

        if (failedChecks.Any())
        {
            var failedCheckNames = string.Join(", ", failedChecks.Select(c => c.Name));
            var errorMessage = $"Critical health checks failed: {failedCheckNames}. Integration tests cannot proceed.";
            
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        if (degradedChecks.Any())
        {
            var degradedCheckNames = string.Join(", ", degradedChecks.Select(c => c.Name));
            _logger.LogWarning("Some health checks are degraded: {DegradedChecks}. Tests may experience issues.", degradedCheckNames);
        }

        _logger.LogInformation("All integration test health checks passed successfully!");
    }

    /// <summary>
    /// Runs health checks with retry logic for transient failures
    /// </summary>
    public async Task EnsureHealthyWithRetryAsync(
        int maxRetries = 3, 
        TimeSpan delay = default,
        CancellationToken cancellationToken = default)
    {
        if (delay == default)
            delay = TimeSpan.FromSeconds(2);

        var attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                await EnsureHealthyAsync(cancellationToken);
                return; // Success!
            }
            catch (InvalidOperationException) when (attempt < maxRetries - 1)
            {
                attempt++;
                _logger.LogWarning("Health check attempt {Attempt} failed. Retrying in {Delay}s...", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // If we get here, all retries failed
        _logger.LogError("All health check attempts failed after {MaxRetries} retries", maxRetries);
        throw new InvalidOperationException($"Health checks failed after {maxRetries} attempts. Database may not be ready for integration tests.");
    }

    private void LogHealthCheckResult(string name, HealthCheckResult result)
    {
        switch (result.Status)
        {
            case HealthStatus.Healthy:
                _logger.LogInformation("✅ {HealthCheckName}: {Description}", name, result.Description);
                break;
            case HealthStatus.Degraded:
                _logger.LogWarning("⚠️  {HealthCheckName}: {Description}", name, result.Description);
                break;
            case HealthStatus.Unhealthy:
                _logger.LogError("❌ {HealthCheckName}: {Description}", name, result.Description);
                if (result.Exception != null)
                {
                    _logger.LogError(result.Exception, "Health check {HealthCheckName} exception details", name);
                }
                break;
        }
    }
}