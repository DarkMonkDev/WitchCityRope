using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.TestBase;

/// <summary>
/// Base class for database-backed unit tests.
/// Provides real ApplicationDbContext instances and common test setup.
/// 
/// Key Features:
/// - Real PostgreSQL database via TestContainers (no mocking of DbContext)
/// - Automatic database cleanup between tests
/// - Mock service setup for non-database dependencies
/// - UTC DateTime handling for PostgreSQL compatibility
/// - Service provider hierarchy for dependency injection testing
/// </summary>
[Collection("Database")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected readonly DatabaseTestFixture DatabaseFixture;
    protected ApplicationDbContext DbContext = null!;
    
    // Mock services for non-database dependencies
    protected readonly Mock<ILogger<DatabaseInitializationService>> MockLogger;
    protected readonly Mock<IServiceProvider> MockServiceProvider;
    protected readonly Mock<IServiceScopeFactory> MockServiceScopeFactory;
    protected readonly Mock<IServiceScope> MockServiceScope;
    protected readonly Mock<IServiceProvider> MockScopeServiceProvider;
    protected readonly Mock<IHostEnvironment> MockHostEnvironment;
    protected readonly Mock<UserManager<ApplicationUser>> MockUserManager;
    protected readonly Mock<ISeedDataService> MockSeedService;
    
    protected readonly IConfiguration Configuration;
    protected readonly CancellationTokenSource CancellationTokenSource;

    protected DatabaseTestBase(DatabaseTestFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
        CancellationTokenSource = new CancellationTokenSource();

        // Setup mock services (everything except DatabaseContext which is real)
        MockLogger = new Mock<ILogger<DatabaseInitializationService>>();
        MockServiceProvider = new Mock<IServiceProvider>();
        MockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        MockServiceScope = new Mock<IServiceScope>();
        MockScopeServiceProvider = new Mock<IServiceProvider>();
        MockHostEnvironment = new Mock<IHostEnvironment>();
        MockSeedService = new Mock<ISeedDataService>();

        // Setup UserManager mock (complex mock for Identity)
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        MockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup service provider hierarchy - mock IServiceScopeFactory instead of extension method
        MockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(MockServiceScopeFactory.Object);
        MockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(MockServiceScope.Object);
        MockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(MockScopeServiceProvider.Object);

        // Setup default configuration for database initialization
        var configData = new Dictionary<string, string>
        {
            ["DatabaseInitialization:EnableAutoMigration"] = "true",
            ["DatabaseInitialization:EnableSeedData"] = "true",
            ["DatabaseInitialization:TimeoutSeconds"] = "30",
            ["DatabaseInitialization:FailOnSeedDataError"] = "true",
            ["DatabaseInitialization:ExcludedEnvironments:0"] = "Production",
            ["DatabaseInitialization:MaxRetryAttempts"] = "3",
            ["DatabaseInitialization:RetryDelaySeconds"] = "2.0"
        };

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    public virtual async Task InitializeAsync()
    {
        // Create fresh DbContext for each test
        DbContext = DatabaseFixture.CreateDbContext();
        
        // Setup scoped services to return real DbContext
        MockScopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
            .Returns(DbContext);
        
        // Setup other scoped services
        MockScopeServiceProvider.Setup(x => x.GetService(typeof(IHostEnvironment)))
            .Returns(MockHostEnvironment.Object);
        MockScopeServiceProvider.Setup(x => x.GetService(typeof(ISeedDataService)))
            .Returns(MockSeedService.Object);
    }

    public virtual async Task DisposeAsync()
    {
        // Dispose DbContext
        DbContext?.Dispose();
        
        // Clean database for next test
        await DatabaseFixture.ResetDatabaseAsync();
        
        // Dispose cancellation token
        CancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// Sets up mock host environment for testing different environment behaviors.
    /// </summary>
    protected void SetupHostEnvironment(string environmentName)
    {
        MockHostEnvironment.Setup(x => x.EnvironmentName)
            .Returns(environmentName);
    }

    /// <summary>
    /// Sets up development environment (enables seed data by default).
    /// </summary>
    protected void SetupDevelopmentEnvironment()
    {
        SetupHostEnvironment("Development");
    }

    /// <summary>
    /// Sets up production environment (disables seed data by default).
    /// </summary>
    protected void SetupProductionEnvironment()
    {
        SetupHostEnvironment("Production");
    }

    /// <summary>
    /// Creates a test user with UTC dates for PostgreSQL compatibility.
    /// </summary>
    protected ApplicationUser CreateTestUser(string email = "test@example.com", string sceneName = "TestUser")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            SceneName = sceneName,
            UserName = email,
            EmailConfirmed = true,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test event with UTC dates for PostgreSQL compatibility.
    /// </summary>
    protected Event CreateTestEvent(string title = "Test Event")
    {
        var startDate = DateTime.UtcNow.AddDays(7);
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test event description",
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            Location = "Test Location",
            Capacity = 20,
            EventType = "Workshop",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Verifies that a mock logger was called with a specific log level and message.
    /// </summary>
    protected void VerifyLogContains(LogLevel level, string message)
    {
        MockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}