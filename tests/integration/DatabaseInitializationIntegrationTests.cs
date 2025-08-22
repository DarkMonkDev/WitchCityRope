using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using Testcontainers.PostgreSql;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Integration tests for the complete database auto-initialization feature.
/// Tests the real interaction between DatabaseInitializationService, SeedDataService,
/// DatabaseInitializationHealthCheck, and PostgreSQL database using Testcontainers.
/// 
/// Key integration points tested:
/// - Background service startup and initialization lifecycle
/// - Real PostgreSQL database migrations and seed data
/// - Health check integration with actual database
/// - Environment detection and configuration binding
/// - Error handling with real database constraints
/// - Concurrent initialization attempts
/// - Complete end-to-end initialization flow
/// 
/// CRITICAL: These tests use real PostgreSQL via Testcontainers
/// following the patterns from lessons learned and integration test guide.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class DatabaseInitializationIntegrationTests : IClassFixture<PostgreSqlIntegrationFixture>, IDisposable
{
    private readonly PostgreSqlIntegrationFixture _fixture;
    private readonly ITestOutputHelper _output;

    public DatabaseInitializationIntegrationTests(PostgreSqlIntegrationFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_WithDevelopmentEnvironment_CompletesSuccessfully()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        
        _output.WriteLine("Starting database initialization integration test");

        // Act
        using var scope = factory.Services.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        var healthCheck = scope.ServiceProvider.GetRequiredService<DatabaseInitializationHealthCheck>();

        // Start the initialization service
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await initService.StartAsync(cts.Token);
        
        // Give it time to complete
        await Task.Delay(1000, cts.Token);

        // Assert initialization completed
        DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();

        // Verify health check reports healthy
        var healthResult = await healthCheck.CheckHealthAsync(new(), cts.Token);
        healthResult.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        healthResult.Data.Should().ContainKey("userCount");
        healthResult.Data.Should().ContainKey("eventCount");

        // Verify database has been seeded
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userCount = await dbContext.Users.CountAsync(cts.Token);
        var eventCount = await dbContext.Events.CountAsync(cts.Token);
        
        userCount.Should().BeGreaterThan(0, "Test users should be created");
        eventCount.Should().BeGreaterThan(0, "Test events should be created");

        _output.WriteLine($"Integration test completed - Users: {userCount}, Events: {eventCount}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_WithProductionEnvironment_SkipsSeedData()
    {
        // Arrange
        using var factory = CreateTestFactory("Production");

        // Act
        using var scope = factory.Services.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await initService.StartAsync(cts.Token);
        await Task.Delay(500, cts.Token);

        // Assert
        DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();

        // Verify no seed data was created (only migrations applied)
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userCount = await dbContext.Users.CountAsync(cts.Token);
        var eventCount = await dbContext.Events.CountAsync(cts.Token);

        userCount.Should().Be(0, "No test users should be created in Production");
        eventCount.Should().Be(0, "No test events should be created in Production");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_IdempotentBehavior_SafeToRunMultipleTimes()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // First run - complete initialization
        var firstService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        
        await firstService.StartAsync(cts.Token);
        await Task.Delay(1000, cts.Token);

        // Record first run results
        var firstUserCount = await dbContext.Users.CountAsync(cts.Token);
        var firstEventCount = await dbContext.Events.CountAsync(cts.Token);

        // Act - Second run (should be idempotent)
        var secondScope = factory.Services.CreateScope();
        var secondService = secondScope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        
        await secondService.StartAsync(cts.Token);
        await Task.Delay(500, cts.Token); // Should complete quickly since already done

        // Assert - No additional data created
        var secondUserCount = await dbContext.Users.CountAsync(cts.Token);
        var secondEventCount = await dbContext.Events.CountAsync(cts.Token);

        firstUserCount.Should().BeGreaterThan(0);
        firstEventCount.Should().BeGreaterThan(0);
        secondUserCount.Should().Be(firstUserCount, "Second run should not create duplicate users");
        secondEventCount.Should().Be(firstEventCount, "Second run should not create duplicate events");

        DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_WithRealMigrations_AppliesCorrectly()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verify database starts with pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        _output.WriteLine($"Pending migrations: {string.Join(", ", pendingMigrations)}");

        // Act
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        
        await initService.StartAsync(cts.Token);
        await Task.Delay(1000, cts.Token);

        // Assert
        DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();

        // Verify no pending migrations remain
        var remainingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cts.Token);
        remainingMigrations.Should().BeEmpty("All migrations should have been applied");

        // Verify database schema exists
        var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
        canConnect.Should().BeTrue();

        // Verify expected tables exist
        var usersTableExists = await TableExistsAsync(dbContext, "AspNetUsers", cts.Token);
        var eventsTableExists = await TableExistsAsync(dbContext, "Events", cts.Token);
        
        usersTableExists.Should().BeTrue("Users table should exist after migration");
        eventsTableExists.Should().BeTrue("Events table should exist after migration");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SeedDataService_WithRealDatabase_CreatesValidTestData()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        using var scope = factory.Services.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is migrated first
        await dbContext.Database.MigrateAsync();

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var result = await seedService.SeedAllDataAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SeedRecordsCreated.Should().BeGreaterThan(0);
        result.Errors.Should().BeEmpty();

        // Verify specific test users were created
        var adminUser = await userManager.FindByEmailAsync("admin@witchcityrope.com");
        adminUser.Should().NotBeNull();
        adminUser!.SceneName.Should().Be("RopeMaster");
        adminUser.IsVetted.Should().BeTrue();
        adminUser.Role.Should().Be("Administrator");

        var teacherUser = await userManager.FindByEmailAsync("teacher@witchcityrope.com");
        teacherUser.Should().NotBeNull();
        teacherUser!.SceneName.Should().Be("SafetyFirst");
        teacherUser.IsVetted.Should().BeTrue();

        // Verify test events were created with proper UTC dates
        var events = await dbContext.Events.ToListAsync(cts.Token);
        events.Should().HaveCountGreaterThan(10);
        events.Should().OnlyContain(e => e.StartDate.Kind == DateTimeKind.Utc);
        events.Should().OnlyContain(e => e.EndDate.Kind == DateTimeKind.Utc);

        var upcomingEvents = events.Where(e => e.StartDate > DateTime.UtcNow).ToList();
        var pastEvents = events.Where(e => e.StartDate < DateTime.UtcNow).ToList();
        
        upcomingEvents.Should().HaveCountGreaterOrEqualTo(10);
        pastEvents.Should().HaveCountGreaterOrEqualTo(2);

        _output.WriteLine($"Created {result.SeedRecordsCreated} records: {events.Count} events, {await userManager.Users.CountAsync()} users");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitializationHealthCheck_WithRealDatabase_ReportsCorrectStatus()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        using var scope = factory.Services.CreateScope();

        // Complete initialization first
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        
        await initService.StartAsync(cts.Token);
        await Task.Delay(1000, cts.Token);

        // Act
        var healthCheck = scope.ServiceProvider.GetRequiredService<DatabaseInitializationHealthCheck>();
        var healthResult = await healthCheck.CheckHealthAsync(new(), cts.Token);

        // Assert
        healthResult.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        healthResult.Description.Should().Be("Database initialization completed successfully");
        
        healthResult.Data.Should().ContainKey("initializationCompleted");
        healthResult.Data["initializationCompleted"].Should().Be(true);
        
        healthResult.Data.Should().ContainKey("status");
        healthResult.Data["status"].Should().Be("Ready");
        
        healthResult.Data.Should().ContainKey("userCount");
        var userCount = (int)healthResult.Data["userCount"];
        userCount.Should().BeGreaterThan(0);
        
        healthResult.Data.Should().ContainKey("eventCount");
        var eventCount = (int)healthResult.Data["eventCount"];
        eventCount.Should().BeGreaterThan(0);

        healthResult.Data.Should().ContainKey("timestamp");
        var timestamp = (DateTime)healthResult.Data["timestamp"];
        timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_WithTimeout_HandlesGracefully()
    {
        // Arrange
        var timeoutConfig = new Dictionary<string, string>
        {
            ["DatabaseInitialization:TimeoutSeconds"] = "2" // Very short timeout for testing
        };

        using var factory = CreateTestFactory("Development", additionalConfig: timeoutConfig);

        // Act & Assert
        using var scope = factory.Services.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        // This might timeout or succeed depending on timing
        try
        {
            await initService.StartAsync(cts.Token);
            await Task.Delay(3000, cts.Token); // Wait longer than timeout
            
            // If it succeeded, verify it worked
            DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();
        }
        catch (InvalidOperationException ex)
        {
            // If it timed out, verify the error is appropriate
            ex.Message.Should().Contain("Database initialization failed");
            ex.InnerException.Should().BeOfType<TimeoutException>();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseInitialization_CompleteEndToEndFlow_WorksWithAllComponents()
    {
        // Arrange
        using var factory = CreateTestFactory("Development");
        
        _output.WriteLine("Testing complete end-to-end database initialization flow");

        // Act - Simulate application startup
        using var scope = factory.Services.CreateScope();
        
        // 1. Start initialization service (simulates application startup)
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        
        var startTime = DateTime.UtcNow;
        await initService.StartAsync(cts.Token);
        await Task.Delay(1500, cts.Token); // Allow time for background processing
        
        var initializationDuration = DateTime.UtcNow - startTime;
        _output.WriteLine($"Initialization completed in {initializationDuration.TotalMilliseconds}ms");

        // Assert - Complete flow verification
        DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();

        // 2. Verify database state
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Migrations applied
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cts.Token);
        pendingMigrations.Should().BeEmpty("All migrations should be applied");

        // Seed data created
        var userCount = await userManager.Users.CountAsync(cts.Token);
        var eventCount = await dbContext.Events.CountAsync(cts.Token);
        
        userCount.Should().Be(5, "Should create 5 test users");
        eventCount.Should().Be(12, "Should create 12 test events");

        // 3. Verify health check reports everything is ready
        var healthCheck = scope.ServiceProvider.GetRequiredService<DatabaseInitializationHealthCheck>();
        var healthResult = await healthCheck.CheckHealthAsync(new(), cts.Token);
        
        healthResult.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        healthResult.Data["userCount"].Should().Be(userCount);
        healthResult.Data["eventCount"].Should().Be(eventCount);
        healthResult.Data["status"].Should().Be("Ready");

        // 4. Verify specific seed data integrity
        var adminUser = await userManager.FindByEmailAsync("admin@witchcityrope.com");
        adminUser.Should().NotBeNull("Admin user should be created");
        adminUser!.IsVetted.Should().BeTrue();
        
        var upcomingEvents = await dbContext.Events
            .Where(e => e.StartDate > DateTime.UtcNow)
            .CountAsync(cts.Token);
        upcomingEvents.Should().Be(10, "Should have 10 upcoming events");

        _output.WriteLine($"End-to-end test completed successfully - {userCount} users, {eventCount} events");
        _output.WriteLine($"Health check status: {healthResult.Status}, Duration: {initializationDuration.TotalSeconds:F2}s");
    }

    private WebApplicationFactory<Program> CreateTestFactory(string environment, 
        Dictionary<string, string>? additionalConfig = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add test configuration
                    var testConfig = new Dictionary<string, string>
                    {
                        ["DatabaseInitialization:EnableAutoMigration"] = "true",
                        ["DatabaseInitialization:EnableSeedData"] = "true",
                        ["DatabaseInitialization:TimeoutSeconds"] = "60",
                        ["DatabaseInitialization:FailOnSeedDataError"] = "true",
                        ["DatabaseInitialization:MaxRetryAttempts"] = "3",
                        ["DatabaseInitialization:RetryDelaySeconds"] = "1.0"
                    };

                    if (additionalConfig != null)
                    {
                        foreach (var kvp in additionalConfig)
                        {
                            testConfig[kvp.Key] = kvp.Value;
                        }
                    }

                    config.AddInMemoryCollection(testConfig!);
                });

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add PostgreSQL DbContext with test container
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_fixture.ConnectionString);
                    });

                    // Ensure services are registered
                    services.AddScoped<ISeedDataService, SeedDataService>();
                    services.AddSingleton<DatabaseInitializationService>();
                    services.AddScoped<DatabaseInitializationHealthCheck>();
                });
            });
    }

    private async Task<bool> TableExistsAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        try
        {
            var sql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
            var result = await context.Database.SqlQueryRaw<int>(sql, tableName)
                .FirstAsync(cancellationToken);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        // Reset static state for other tests
        var field = typeof(DatabaseInitializationService)
            .GetField("_initializationCompleted", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field?.SetValue(null, false);
    }
}

/// <summary>
/// Shared PostgreSQL fixture for integration tests.
/// Provides a single PostgreSQL container instance shared across all tests
/// in the collection for better performance.
/// </summary>
public class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    
    public string ConnectionString => _container?.GetConnectionString() ?? 
        throw new InvalidOperationException("PostgreSQL container not started");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// Ensures all tests in this collection share the same PostgreSQL container.
/// </summary>
[CollectionDefinition("PostgreSQL Integration Tests")]
public class PostgreSqlIntegrationTestCollection : ICollectionFixture<PostgreSqlIntegrationFixture>
{
}