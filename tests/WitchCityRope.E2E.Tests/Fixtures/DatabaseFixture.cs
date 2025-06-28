using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Respawn;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.E2E.Tests.Fixtures;

public class DatabaseFixture : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private WitchCityRopeDbContext _dbContext = null!;
    private Respawner _respawner = null!;
    private readonly string _connectionString;

    public WitchCityRopeDbContext DbContext => _dbContext;

    public DatabaseFixture(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("TestDatabase") 
            ?? throw new InvalidOperationException("Test database connection string not found");
    }

    public async Task InitializeAsync()
    {
        // Create DbContext
        var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseSqlServer(_connectionString);
        
        _dbContext = new WitchCityRopeDbContext(optionsBuilder.Options);

        // Ensure database is created and migrations are applied
        await _dbContext.Database.EnsureCreatedAsync();

        // Initialize Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(_connectionString, new RespawnerOptions
        {
            TablesToIgnore = new[]
            {
                "__EFMigrationsHistory",
                "AspNetRoles",
                "AspNetRoleClaims"
            },
            WithReseed = true
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connectionString);
    }

    public async Task SeedBaseDataAsync()
    {
        // Seed any base data needed for tests (e.g., roles, default settings)
        // This is data that should exist in all test scenarios
        
        await _dbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
    }
}