using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace WitchCityRope.IntegrationTests.HealthChecks;

/// <summary>
/// Health check for verifying database schema and required tables exist
/// </summary>
public class DatabaseSchemaHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseSchemaHealthCheck> _logger;

    // Required tables for the application to function
    private readonly string[] _requiredTables = 
    {
        "Events", "Tickets", "Rsvps", "Payments", "UserNotes", 
        "VettingApplications", "VolunteerTasks", "RefreshTokens",
        "IncidentReports", "EventEmailTemplates"
    };

    // Required Identity tables in auth schema
    private readonly string[] _requiredIdentityTables = 
    {
        "Users", "Roles", "UserRoles", "UserClaims", "RoleClaims", 
        "UserLogins", "UserTokens"
    };

    public DatabaseSchemaHealthCheck(string connectionString, ILogger<DatabaseSchemaHealthCheck> logger)
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

            var missingTables = new List<string>();

            // Check public schema tables
            var publicTables = await GetTablesInSchema(connection, "public", cancellationToken);
            foreach (var table in _requiredTables)
            {
                if (!publicTables.Contains(table))
                {
                    missingTables.Add($"public.{table}");
                }
            }

            // Check auth schema tables
            var authTables = await GetTablesInSchema(connection, "auth", cancellationToken);
            foreach (var table in _requiredIdentityTables)
            {
                if (!authTables.Contains(table))
                {
                    missingTables.Add($"auth.{table}");
                }
            }

            // Check if migrations table exists
            var migrationTableExists = await CheckMigrationTableExists(connection, cancellationToken);
            if (!migrationTableExists)
            {
                missingTables.Add("__EFMigrationsHistory");
            }

            if (missingTables.Any())
            {
                var missingTableList = string.Join(", ", missingTables);
                _logger.LogError("Missing required tables: {MissingTables}", missingTableList);
                return HealthCheckResult.Unhealthy($"Missing required tables: {missingTableList}");
            }

            var totalTables = publicTables.Count + authTables.Count;
            return HealthCheckResult.Healthy($"Database schema is complete. Found {totalTables} tables.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database schema health check failed");
            return HealthCheckResult.Unhealthy("Database schema check failed", ex);
        }
    }

    private async Task<List<string>> GetTablesInSchema(
        NpgsqlConnection connection, 
        string schema, 
        CancellationToken cancellationToken)
    {
        var tables = new List<string>();
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = @schema 
            ORDER BY table_name";
        command.Parameters.AddWithValue("schema", schema);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<bool> CheckMigrationTableExists(
        NpgsqlConnection connection, 
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_name = '__EFMigrationsHistory'";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }
}