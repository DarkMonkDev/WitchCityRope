using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace WitchCityRope.IntegrationTests.HealthChecks;

/// <summary>
/// Health check for verifying essential seed data exists for testing
/// </summary>
public class SeedDataHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<SeedDataHealthCheck> _logger;

    public SeedDataHealthCheck(string connectionString, ILogger<SeedDataHealthCheck> logger)
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

            // Check if users exist in the auth schema
            var userCount = await GetUserCount(connection, cancellationToken);
            if (userCount == 0)
            {
                return HealthCheckResult.Unhealthy("No users found in database. Seed data may not be loaded.");
            }

            // Check if roles exist in the auth schema
            var roleCount = await GetRoleCount(connection, cancellationToken);
            if (roleCount == 0)
            {
                return HealthCheckResult.Unhealthy("No roles found in database. Seed data may not be loaded.");
            }

            // Check if test users exist (admin, teacher, member, etc.)
            var testUserCount = await GetTestUserCount(connection, cancellationToken);
            if (testUserCount == 0)
            {
                _logger.LogWarning("No test users found. Integration tests may fail.");
                return HealthCheckResult.Degraded("Test users not found in database. Some integration tests may fail.");
            }

            return HealthCheckResult.Healthy($"Seed data is present. Users: {userCount}, Roles: {roleCount}, Test users: {testUserCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seed data health check failed");
            return HealthCheckResult.Unhealthy("Seed data check failed", ex);
        }
    }

    private async Task<int> GetUserCount(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COUNT(*) FROM auth.""Users""";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private async Task<int> GetRoleCount(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COUNT(*) FROM auth.""Roles""";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private async Task<int> GetTestUserCount(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM auth.""Users"" 
            WHERE ""Email"" IN (
                'admin@witchcityrope.com',
                'teacher@witchcityrope.com',
                'member@witchcityrope.com',
                'vetted@witchcityrope.com'
            )";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}