using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace WitchCityRope.IntegrationTests.HealthChecks;

/// <summary>
/// Health check for database connectivity and basic operations
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(string connectionString, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Test basic database connectivity
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result?.ToString() == "1")
            {
                return HealthCheckResult.Healthy("Database connection is responsive");
            }
            
            return HealthCheckResult.Unhealthy("Database query returned unexpected result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }

    /// <summary>
    /// Simplified health check method for PostgreSQL container connectivity
    /// </summary>
    public async Task CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Test basic database connectivity
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result?.ToString() != "1")
            {
                throw new InvalidOperationException("Database query returned unexpected result");
            }

            _logger.LogInformation("Database connectivity check passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connectivity check failed");
            throw;
        }
    }
}