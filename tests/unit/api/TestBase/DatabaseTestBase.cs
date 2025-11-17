using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Services.Seeding;
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
///
/// Test Data Creation Guidelines:
/// - Use *Directly() helper methods when you need test data but are NOT testing creation logic
///   Example: Testing event registration logic, need users and events set up first
///
/// - Call production services when you ARE testing creation, validation, or business logic
///   Example: Testing user creation validation rules, call UserService.CreateUserAsync()
///
/// Why the distinction matters:
/// - *Directly() methods bypass validation, business rules, computed values, and events
/// - This makes tests faster and more isolated for non-creation scenarios
/// - But this means they cannot validate that creation logic works correctly
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
    /// Creates a test user DIRECTLY in database, bypassing all services.
    ///
    /// WARNING: This method bypasses:
    /// - User validation rules (email format, password strength, etc.)
    /// - Business logic (duplicate email checks, age verification, etc.)
    /// - Computed values from services (role assignments, initial status, etc.)
    /// - Event subscriptions/notifications (welcome emails, audit logs, etc.)
    /// - Password hashing via Identity framework
    ///
    /// Use this ONLY when you need test data and are NOT testing user creation logic.
    ///
    /// Examples of appropriate use:
    /// - Testing event registration (need users as test data)
    /// - Testing check-in logic (need users to check in)
    /// - Testing volunteer assignments (need users as volunteers)
    ///
    /// To test user creation, call the production UserService.CreateUserAsync() or
    /// UserManager.CreateAsync() instead.
    /// </summary>
    /// <param name="email">Email address for test user</param>
    /// <param name="sceneName">Scene name for test user</param>
    /// <returns>ApplicationUser entity ready to be added to DbContext</returns>
    protected ApplicationUser CreateTestUserDirectly(string email = "test@example.com", string sceneName = "TestUser")
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
    /// Creates a test event DIRECTLY in database, bypassing all services.
    ///
    /// WARNING: This method bypasses:
    /// - Event validation rules (title length, date logic, capacity limits, etc.)
    /// - Business logic (organizer permissions, scheduling conflicts, etc.)
    /// - Computed values from services (published status transitions, registration stats, etc.)
    /// - Event subscriptions/notifications (organizer notifications, attendee emails, etc.)
    /// - Session creation and management
    ///
    /// Use this ONLY when you need test data and are NOT testing event creation logic.
    ///
    /// Examples of appropriate use:
    /// - Testing event registration (need events as test data)
    /// - Testing check-in logic (need events to check in to)
    /// - Testing volunteer assignments (need events to volunteer for)
    ///
    /// To test event creation, call the production EventService.CreateEventAsync() instead.
    ///
    /// NOTE: You may need to add venue/location separately if required by your test.
    /// </summary>
    /// <param name="title">Title for test event</param>
    /// <returns>Event entity ready to be added to DbContext</returns>
    protected Event CreateTestEventDirectly(string title = "Test Event")
    {
        var startDate = DateTime.UtcNow.AddDays(7);
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test event description",
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            Capacity = 20,
            VenueId = 1, // FK constraint satisfaction - matches CreateTestVenueAsync() ID
            EventType = EventType.Class,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test user by calling production user creation services.
    ///
    /// This ensures:
    /// - All validation rules are executed (email format, password strength, etc.)
    /// - Business logic is tested (duplicate email checks, age verification, etc.)
    /// - Computed values are generated by services (role assignments, initial status, etc.)
    /// - Event subscriptions/notifications fire (welcome emails, audit logs, etc.)
    /// - Password hashing via Identity framework
    ///
    /// Use this when testing features that depend on user creation logic.
    /// For simple test data setup, use CreateTestUserDirectly() instead.
    ///
    /// Implementation Note:
    /// This method must be overridden in test classes that need it.
    /// Test classes should inject UserManager or UserService and call the appropriate
    /// creation method (e.g., UserManager.CreateAsync()).
    ///
    /// Example implementation in your test class:
    /// <code>
    /// protected override async Task&lt;ApplicationUser&gt; CreateTestUserViaService(
    ///     string email = "test@example.com",
    ///     string sceneName = "TestUser",
    ///     string password = "Test123!")
    /// {
    ///     var user = new ApplicationUser
    ///     {
    ///         Email = email,
    ///         SceneName = sceneName,
    ///         UserName = email
    ///     };
    ///     var result = await UserManager.CreateAsync(user, password);
    ///     if (!result.Succeeded)
    ///         throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    ///     return user;
    /// }
    /// </code>
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="sceneName">User scene name</param>
    /// <param name="password">User password</param>
    /// <returns>Created user entity</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when test class has not overridden this method to provide production service implementation.
    /// </exception>
    protected virtual async Task<ApplicationUser> CreateTestUserViaService(
        string email = "test@example.com",
        string sceneName = "TestUser",
        string password = "Test123!")
    {
        throw new NotImplementedException(
            "Test class must implement user creation via production service. " +
            "Override this method and call UserManager.CreateAsync() or UserService to create users. " +
            "See XML documentation above for example implementation.");
    }

    /// <summary>
    /// Creates a test event by calling production EventService.
    ///
    /// This ensures:
    /// - All validation rules are executed (date validation, capacity checks, etc.)
    /// - Business logic is tested (organizer verification, scheduling conflicts, etc.)
    /// - Computed values are generated (derived fields, status calculations, etc.)
    /// - Related entities created properly (sessions, roles, etc.)
    /// - Event subscriptions/notifications fire (emails, calendar updates, etc.)
    ///
    /// Use this when testing features that depend on event creation logic.
    /// For simple test data setup, use CreateTestEventDirectly() instead.
    ///
    /// Implementation Note:
    /// This method must be overridden in test classes that need it.
    /// Test classes should inject EventService and call CreateEventAsync().
    ///
    /// Example implementation in your test class:
    /// <code>
    /// protected override async Task&lt;Event&gt; CreateTestEventViaService(
    ///     string title = "Test Event",
    ///     Guid? organizerId = null)
    /// {
    ///     var createDto = new CreateEventDto
    ///     {
    ///         Title = title,
    ///         Description = "Test event description",
    ///         StartDate = DateTime.UtcNow.AddDays(7),
    ///         EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
    ///         Location = "Test Location",
    ///         Capacity = 20,
    ///         EventType = EventType.Class,
    ///         OrganizerId = organizerId ?? _defaultOrganizerId
    ///     };
    ///     return await EventService.CreateEventAsync(createDto);
    /// }
    /// </code>
    /// </summary>
    /// <param name="title">Event title</param>
    /// <param name="organizerId">Event organizer ID (optional)</param>
    /// <returns>Created event entity</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when test class has not overridden this method to provide production service implementation.
    /// </exception>
    protected virtual async Task<Event> CreateTestEventViaService(
        string title = "Test Event",
        Guid? organizerId = null)
    {
        throw new NotImplementedException(
            "Test class must implement event creation via EventService. " +
            "Override this method and call EventService.CreateEventAsync() to create events. " +
            "See XML documentation above for example implementation.");
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