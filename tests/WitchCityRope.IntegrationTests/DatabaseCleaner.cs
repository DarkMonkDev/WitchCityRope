using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Handles database cleanup between integration tests using Respawn
/// </summary>
public class DatabaseCleaner
{
    private static Respawner? _respawner;
    private static readonly object _lock = new();
    private readonly ILogger<DatabaseCleaner> _logger;
    
    public DatabaseCleaner(ILogger<DatabaseCleaner> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Initialize the respawner for the given connection string
    /// </summary>
    public async Task InitializeAsync(string connectionString)
    {
        if (_respawner != null)
            return;
            
        lock (_lock)
        {
            if (_respawner != null)
                return;
        }
        
        _logger.LogInformation("Initializing database cleaner with Respawn...");
        
        try
        {
            _respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
            {
                TablesToIgnore = new Respawn.Graph.Table[]
                {
                    "__EFMigrationsHistory",
                    "AspNetRoles",  // Keep roles between tests
                },
                WithReseed = true,
                DbAdapter = DbAdapter.Postgres
            });
            
            _logger.LogInformation("Database cleaner initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database cleaner");
            throw;
        }
    }
    
    /// <summary>
    /// Reset the database to a clean state
    /// </summary>
    public async Task ResetAsync(string connectionString)
    {
        if (_respawner == null)
        {
            await InitializeAsync(connectionString);
        }
        
        _logger.LogInformation("Resetting database to clean state...");
        
        try
        {
            await _respawner!.ResetAsync(connectionString);
            _logger.LogInformation("Database reset completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database");
            throw;
        }
    }
    
    /// <summary>
    /// Ensures database exists and creates it if needed
    /// </summary>
    public static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        
        // Connect to postgres database to check/create the test database
        builder.Database = "postgres";
        
        using var connection = new NpgsqlConnection(builder.ToString());
        await connection.OpenAsync();
        
        // Check if database exists
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
        var exists = await checkCmd.ExecuteScalarAsync() != null;
        
        if (!exists)
        {
            // Create database
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await createCmd.ExecuteNonQueryAsync();
        }
    }
}