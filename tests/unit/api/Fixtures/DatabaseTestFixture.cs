using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Api.Tests.Fixtures;

/// <summary>
/// Database test fixture that provides real PostgreSQL containers for unit testing.
/// Uses TestContainers to manage PostgreSQL lifecycle and Respawn for database cleanup.
/// 
/// Key Features:
/// - Real PostgreSQL database instead of mocking ApplicationDbContext
/// - Automatic container lifecycle management (startup/cleanup)
/// - Respawn for fast database reset between tests
/// - Shared container across test collection for performance
/// - Proper UTC DateTime handling for PostgreSQL compatibility
/// </summary>
public class DatabaseTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    private readonly ServiceProvider _serviceProvider;

    public string ConnectionString => _container?.GetConnectionString() ?? 
        throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");

    public DatabaseTestFixture()
    {
        // Setup service provider for dependency injection in tests
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Initialize database schema
        await CreateDatabaseSchemaAsync();

        // Initialize Respawn for fast database cleanup
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
        });
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Creates a real ApplicationDbContext instance connected to the test PostgreSQL container.
    /// Use this instead of mocking ApplicationDbContext to test with real database behavior.
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableSensitiveDataLogging() // For better test debugging
            .EnableDetailedErrors()
            .UseLoggerFactory(_serviceProvider.GetRequiredService<ILoggerFactory>())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Resets the database to a clean state for test isolation.
    /// Much faster than recreating containers between tests.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null) 
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Creates database schema by running EF Core migrations.
    /// Called once during container initialization.
    /// </summary>
    private async Task CreateDatabaseSchemaAsync()
    {
        using var context = CreateDbContext();
        
        // Apply migrations to create schema
        await context.Database.MigrateAsync();
        
        // Verify schema was created correctly
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException("Failed to create database schema");
        }
    }
}

/// <summary>
/// Collection definition for shared database fixture.
/// All test classes that use [Collection("Database")] will share the same PostgreSQL container
/// for better performance while maintaining test isolation through Respawn.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}