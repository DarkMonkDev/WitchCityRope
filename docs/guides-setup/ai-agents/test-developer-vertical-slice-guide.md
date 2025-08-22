# Test Developer Implementation Guide - Simple Vertical Slice Testing
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

This guide provides comprehensive testing patterns for the **Simple Vertical Slice Architecture** used in WitchCityRope API development. The architecture eliminates MediatR/CQRS complexity, requiring direct Entity Framework service testing patterns with real PostgreSQL databases using TestContainers.

### Key Testing Principles
- **Test Entity Framework Services Directly**: No handler testing, no MediatR complexity
- **Real Database Testing**: Use TestContainers with PostgreSQL, eliminate mocking issues
- **Feature-Based Test Organization**: Mirror code organization in test structure
- **Simple Test Patterns**: Focus on business logic, not framework complexity

---

## Working Test Examples Reference

**ALWAYS reference the working Health feature tests**:
- Location: `/home/chad/repos/witchcityrope-react/tests/unit/api/Services/`
- Files: `HealthServiceTests.cs`, `DatabaseInitializationServiceTests.cs`
- TestContainers Setup: `/tests/unit/api/Fixtures/DatabaseTestFixture.cs`

### Test Structure (COPY THIS PATTERN)
```
tests/unit/api/
├── Fixtures/
│   └── DatabaseTestFixture.cs      # TestContainers setup
├── TestBase/
│   └── DatabaseTestBase.cs         # Base class for database tests
├── Services/
│   ├── HealthServiceTests.cs       # Service unit tests
│   └── [Feature]ServiceTests.cs    # Feature service tests
└── Endpoints/
    └── [Feature]EndpointTests.cs   # Integration tests
```

---

## 🚨 CRITICAL: Anti-Patterns (NEVER TEST THESE)

### ❌ FORBIDDEN TESTING PATTERNS
```csharp
// ❌ NEVER test MediatR handlers (don't exist in our architecture)
[Test]
public async Task Handle_GetHealthQuery_ReturnsHealth()
{
    var handler = new GetHealthHandler();
    var result = await handler.Handle(query, cancellationToken);
}

// ❌ NEVER test command/query objects (don't exist)
[Test] 
public void GetHealthQuery_ShouldBeValid()

// ❌ NEVER test pipeline behaviors (don't exist)
[Test]
public async Task ValidationPipeline_ShouldValidateRequest()

// ❌ NEVER mock ApplicationDbContext (use real database)
var mockContext = new Mock<ApplicationDbContext>();
mockContext.Setup(x => x.Users).Returns(mockDbSet);
```

### ✅ REQUIRED TESTING PATTERNS
```csharp
// ✅ Test Entity Framework services directly
[Test]
public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsHealthyStatus()

// ✅ Use real PostgreSQL database with TestContainers
using var context = new ApplicationDbContext(DatabaseTestFixture.GetDbContextOptions());

// ✅ Test minimal API endpoints with TestClient
var response = await _client.GetAsync("/api/health");

// ✅ Feature-based test organization
namespace WitchCityRope.Tests.Services.Health
```

---

## TestContainers Setup (CRITICAL FOUNDATION)

### DatabaseTestFixture.cs (MANDATORY BASE)
```csharp
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Tests.Fixtures;

/// <summary>
/// TestContainers setup for real PostgreSQL testing
/// Eliminates ApplicationDbContext mocking issues
/// </summary>
public class DatabaseTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("witchcityrope_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        // Apply migrations to test database
        using var context = new ApplicationDbContext(GetDbContextOptions());
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public DbContextOptions<ApplicationDbContext> GetDbContextOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableSensitiveDataLogging() // OK for tests
            .Options;
    }
}
```

### DatabaseTestBase.cs (COMMON BASE CLASS)
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Fixtures;

namespace WitchCityRope.Tests.TestBase;

/// <summary>
/// Base class for database integration tests
/// Provides common setup and teardown patterns
/// </summary>
public abstract class DatabaseTestBase : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    protected readonly DatabaseTestFixture _fixture;
    protected ApplicationDbContext _context = null!;
    protected Mock<ILogger> _mockLogger = null!;

    protected DatabaseTestBase(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        _context = new ApplicationDbContext(_fixture.GetDbContextOptions());
        _mockLogger = new Mock<ILogger>();
        
        // Clean database for each test
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

---

## Service Testing Patterns

### 1. Simple Service Test Template
```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.[FeatureName].Services;
using WitchCityRope.Tests.Fixtures;
using WitchCityRope.Tests.TestBase;

namespace WitchCityRope.Tests.Services.[FeatureName];

/// <summary>
/// Tests for [FeatureName]Service using real PostgreSQL database
/// Example of direct Entity Framework service testing
/// </summary>
[TestFixture]
public class [FeatureName]ServiceTests : DatabaseTestBase
{
    private [FeatureName]Service _service = null!;
    private Mock<ILogger<[FeatureName]Service>> _logger = null!;

    public [FeatureName]ServiceTests(DatabaseTestFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _logger = new Mock<ILogger<[FeatureName]Service>>();
        _service = new [FeatureName]Service(_context, _logger.Object);
    }

    [Test]
    public async Task [Method]Async_When[Condition]_[ExpectedResult]()
    {
        // Arrange
        var entity = new [Entity] { /* test data */ };
        _context.[Entities].Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.[Method]Async();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        error.Should().BeEmpty();
        
        // Verify business logic specific assertions
        response.[Property].Should().Be(expectedValue);
    }

    [Test]
    public async Task [Method]Async_WhenDatabaseError_ReturnsFailure()
    {
        // Arrange - Create scenario that causes database error
        _context.Dispose(); // Force database error

        // Act
        var (success, response, error) = await _service.[Method]Async();

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().NotBeEmpty();
        
        // Verify logging
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

### 2. Real Example from HealthServiceTests
```csharp
[TestFixture]
public class HealthServiceTests : DatabaseTestBase
{
    private HealthService _service = null!;
    private Mock<ILogger<HealthService>> _logger = null!;

    public HealthServiceTests(DatabaseTestFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _logger = new Mock<ILogger<HealthService>>();
        _service = new HealthService(_context, _logger.Object);
    }

    [Test]
    public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsHealthyStatus()
    {
        // Arrange
        var user = new User 
        { 
            Email = "test@witchcityrope.com",
            UserName = "test@witchcityrope.com",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        response.UserCount.Should().Be(1);
        response.Version.Should().Be("1.0.0");
        error.Should().BeEmpty();
        
        _logger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Health check completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetDetailedHealthAsync_WhenUsersExist_ReturnsCorrectCounts()
    {
        // Arrange
        var activeUser = new User 
        { 
            Email = "active@test.com",
            UserName = "active@test.com",
            FirstName = "Active",
            LastName = "User",
            LastLoginAt = DateTime.UtcNow.AddDays(-5) // Active within 30 days
        };
        
        var inactiveUser = new User 
        { 
            Email = "inactive@test.com", 
            UserName = "inactive@test.com",
            FirstName = "Inactive",
            LastName = "User",
            LastLoginAt = DateTime.UtcNow.AddDays(-35) // Inactive
        };

        _context.Users.AddRange(activeUser, inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetDetailedHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.UserCount.Should().Be(2);
        response.ActiveUserCount.Should().Be(1);
        response.DatabaseVersion.Should().Be("PostgreSQL (Connected)");
        response.Environment.Should().NotBeNullOrEmpty();
    }
}
```

---

## Integration Testing Patterns (Minimal API Endpoints)

### WebApplicationFactory Setup
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Fixtures;

namespace WitchCityRope.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// Uses TestContainers PostgreSQL instead of in-memory database
/// </summary>
public class WitchCityRopeWebApplicationFactory : WebApplicationFactory<Program>, IClassFixture<DatabaseTestFixture>
{
    private readonly DatabaseTestFixture _fixture;

    public WitchCityRopeWebApplicationFactory(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add TestContainers database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_fixture.ConnectionString));
        });
    }
}
```

### Endpoint Integration Test Template
```csharp
using System.Net;
using System.Text.Json;
using FluentAssertions;
using WitchCityRope.Api.Features.[FeatureName].Models;
using WitchCityRope.Tests.Fixtures;
using WitchCityRope.Tests.Integration;

namespace WitchCityRope.Tests.Endpoints.[FeatureName];

/// <summary>
/// Integration tests for [FeatureName] endpoints
/// Tests minimal API endpoints with real database
/// </summary>
[TestFixture]
public class [FeatureName]EndpointTests : IClassFixture<DatabaseTestFixture>
{
    private readonly WitchCityRopeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public [FeatureName]EndpointTests(DatabaseTestFixture fixture)
    {
        _factory = new WitchCityRopeWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Get[FeatureName]_WhenValidRequest_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/[feature]");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<[Response]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        // Add specific response assertions
    }

    [Test]
    public async Task Post[FeatureName]_WhenValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new [Request]
        {
            // Set request properties
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/[feature]", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify response content
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<[Response]>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
```

### Real Example from Health Endpoint Tests
```csharp
[TestFixture]
public class HealthEndpointTests : IClassFixture<DatabaseTestFixture>
{
    private readonly WitchCityRopeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointTests(DatabaseTestFixture fixture)
    {
        _factory = new WitchCityRopeWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task GetHealth_WhenCalled_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        healthResponse.Should().NotBeNull();
        healthResponse.Status.Should().Be("Healthy");
        healthResponse.DatabaseConnected.Should().BeTrue();
        healthResponse.UserCount.Should().BeGreaterOrEqualTo(0);
        healthResponse.Version.Should().Be("1.0.0");
    }

    [Test]
    public async Task GetDetailedHealth_WhenCalled_ReturnsDetailedStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var detailedResponse = JsonSerializer.Deserialize<DetailedHealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        detailedResponse.Should().NotBeNull();
        detailedResponse.Status.Should().Be("Healthy");
        detailedResponse.DatabaseVersion.Should().Be("PostgreSQL (Connected)");
        detailedResponse.ActiveUserCount.Should().BeGreaterOrEqualTo(0);
        detailedResponse.Environment.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GetLegacyHealth_WhenCalled_ReturnsSimpleStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"status\":\"Healthy\"");
    }
}
```

---

## Validation Testing Patterns

### FluentValidation Testing
```csharp
using FluentAssertions;
using WitchCityRope.Api.Features.[FeatureName].Models;
using WitchCityRope.Api.Features.[FeatureName].Validation;

namespace WitchCityRope.Tests.Validation.[FeatureName];

/// <summary>
/// Tests for [Request]Validator
/// Validates FluentValidation rules work correctly
/// </summary>
[TestFixture]
public class [Request]ValidatorTests
{
    private [Request]Validator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new [Request]Validator();
    }

    [Test]
    public void Validate_WhenValidRequest_ShouldPass()
    {
        // Arrange
        var request = new [Request]
        {
            Name = "Valid Name",
            // Set other valid properties
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase(null)]
    public void Validate_WhenNameInvalid_ShouldFail(string invalidName)
    {
        // Arrange
        var request = new [Request]
        {
            Name = invalidName
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof([Request].Name));
    }

    [Test]
    public void Validate_WhenNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new [Request]
        {
            Name = new string('x', 101) // Exceeds 100 character limit
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("100 characters");
    }
}
```

---

## Test Data Patterns

### Entity Factory Pattern
```csharp
using WitchCityRope.Api.Entities;

namespace WitchCityRope.Tests.TestData;

/// <summary>
/// Factory for creating test entities
/// Provides consistent test data across tests
/// </summary>
public static class TestEntityFactory
{
    public static User CreateUser(
        string email = "test@witchcityrope.com",
        string firstName = "Test",
        string lastName = "User",
        DateTime? lastLoginAt = null)
    {
        return new User
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            LastLoginAt = lastLoginAt ?? DateTime.UtcNow.AddDays(-1),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    public static Event CreateEvent(
        string title = "Test Event",
        DateTime? date = null,
        string? instructorId = null)
    {
        return new Event
        {
            Title = title,
            Description = "Test event description",
            Date = date ?? DateTime.UtcNow.AddDays(7),
            Duration = TimeSpan.FromHours(2),
            MaxParticipants = 10,
            InstructorId = instructorId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
```

### Using Test Data Factories
```csharp
[Test]
public async Task GetEventsAsync_WhenEventsExist_ReturnsEvents()
{
    // Arrange
    var user = TestEntityFactory.CreateUser("instructor@test.com");
    var event1 = TestEntityFactory.CreateEvent("Event 1", DateTime.UtcNow.AddDays(1), user.Id);
    var event2 = TestEntityFactory.CreateEvent("Event 2", DateTime.UtcNow.AddDays(2), user.Id);
    
    _context.Users.Add(user);
    _context.Events.AddRange(event1, event2);
    await _context.SaveChangesAsync();

    // Act
    var (success, response, error) = await _service.GetEventsAsync();

    // Assert
    success.Should().BeTrue();
    response.Should().HaveCount(2);
    response.Should().Contain(e => e.Title == "Event 1");
    response.Should().Contain(e => e.Title == "Event 2");
}
```

---

## Performance Testing Patterns

### Simple Performance Assertions
```csharp
[Test]
public async Task GetHealthAsync_PerformanceTest_CompletesQuickly()
{
    // Arrange
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Act
    var (success, response, error) = await _service.GetHealthAsync();
    stopwatch.Stop();

    // Assert
    success.Should().BeTrue();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
        "Health check should complete in under 100ms");
}
```

### Load Testing (Optional)
```csharp
[Test]
public async Task GetHealthAsync_LoadTest_HandlesMultipleConcurrentRequests()
{
    // Arrange
    const int concurrentRequests = 10;
    var tasks = new List<Task<(bool, HealthResponse?, string)>>();

    // Act
    for (int i = 0; i < concurrentRequests; i++)
    {
        tasks.Add(_service.GetHealthAsync());
    }

    var results = await Task.WhenAll(tasks);

    // Assert
    results.Should().AllSatisfy(result => 
    {
        result.Item1.Should().BeTrue(); // Success
        result.Item2.Should().NotBeNull(); // Response
        result.Item3.Should().BeEmpty(); // No error
    });
}
```

---

## Test Organization by Feature

### Feature-Based Test Structure
```
tests/unit/api/
├── Services/
│   ├── Authentication/
│   │   ├── AuthenticationServiceTests.cs
│   │   └── UserRegistrationServiceTests.cs
│   ├── Events/
│   │   ├── EventManagementServiceTests.cs
│   │   └── EventRegistrationServiceTests.cs
│   └── Health/
│       └── HealthServiceTests.cs
├── Endpoints/
│   ├── Authentication/
│   │   └── AuthenticationEndpointTests.cs
│   ├── Events/
│   │   └── EventEndpointTests.cs
│   └── Health/
│       └── HealthEndpointTests.cs
└── Validation/
    ├── Authentication/
    │   └── RegisterRequestValidatorTests.cs
    └── Events/
        └── CreateEventRequestValidatorTests.cs
```

---

## Common Testing Scenarios

### 1. CRUD Operations Testing
```csharp
[TestFixture]
public class UserProfileServiceTests : DatabaseTestBase
{
    // Test Create
    [Test]
    public async Task CreateUserProfileAsync_WhenValidData_CreatesProfile()
    
    // Test Read
    [Test]
    public async Task GetUserProfileAsync_WhenProfileExists_ReturnsProfile()
    
    // Test Update
    [Test]
    public async Task UpdateUserProfileAsync_WhenValidChanges_UpdatesProfile()
    
    // Test Delete
    [Test]
    public async Task DeleteUserProfileAsync_WhenProfileExists_DeletesProfile()
}
```

### 2. Business Logic Testing
```csharp
[TestFixture] 
public class EventRegistrationServiceTests : DatabaseTestBase
{
    [Test]
    public async Task RegisterForEventAsync_WhenEventFull_ReturnsError()
    
    [Test]
    public async Task RegisterForEventAsync_WhenAlreadyRegistered_ReturnsError()
    
    [Test]
    public async Task CancelRegistrationAsync_WhenWithinCancelWindow_AllowsCancellation()
    
    [Test]
    public async Task CancelRegistrationAsync_WhenPastCancelWindow_ReturnsError()
}
```

### 3. Error Handling Testing
```csharp
[TestFixture]
public class DatabaseErrorHandlingTests : DatabaseTestBase
{
    [Test]
    public async Task Service_WhenDatabaseUnavailable_ReturnsGracefulError()
    
    [Test]  
    public async Task Service_WhenConstraintViolation_ReturnsSpecificError()
    
    [Test]
    public async Task Service_WhenConcurrencyConflict_HandlesGracefully()
}
```

---

## Test Execution and Reporting

### NUnit Configuration
```xml
<!-- NUnit.runsettings -->
<RunSettings>
  <NUnit>
    <TestOutputXml>TestResults</TestOutputXml>
  </NUnit>
  <RunConfiguration>
    <MaxCpuCount>1</MaxCpuCount> <!-- TestContainers works better with single thread -->
  </RunConfiguration>
</RunSettings>
```

### Test Categories for Organization
```csharp
[TestFixture]
[Category("Integration")]
public class HealthEndpointTests

[TestFixture]
[Category("Unit")]  
public class HealthServiceTests

[TestFixture]
[Category("Performance")]
public class PerformanceTests
```

### Running Specific Test Categories
```bash
# Unit tests only (fast)
dotnet test --filter "Category=Unit"

# Integration tests only (slower, requires TestContainers)
dotnet test --filter "Category=Integration"  

# All tests
dotnet test
```

---

## Testing Checklist

### ✅ Before Writing Tests
- [ ] Study existing Health service tests as reference
- [ ] Verify TestContainers setup is working
- [ ] Understand the service being tested (no handlers!)
- [ ] Check for similar feature test patterns

### ✅ Service Testing
- [ ] Test happy path scenarios
- [ ] Test error conditions (database errors, validation failures)
- [ ] Test business logic edge cases
- [ ] Verify proper logging
- [ ] Use real database via TestContainers

### ✅ Integration Testing  
- [ ] Test minimal API endpoints
- [ ] Test request/response serialization
- [ ] Test authentication/authorization if applicable
- [ ] Test error responses and status codes
- [ ] Use WebApplicationFactory with TestContainers

### ✅ Validation Testing
- [ ] Test FluentValidation rules
- [ ] Test all validation scenarios (valid, invalid, edge cases)
- [ ] Verify error messages are user-friendly

### ✅ Test Quality
- [ ] Arrange-Act-Assert pattern
- [ ] Descriptive test names (Method_Condition_ExpectedResult)
- [ ] Independent tests (no test order dependencies)
- [ ] Clean up test data appropriately
- [ ] Fast execution (TestContainers setup cached)

---

## Success Metrics

### Test Coverage Goals
- **Service Tests**: 90%+ code coverage on business logic
- **Integration Tests**: All endpoints have happy path tests
- **Validation Tests**: 100% coverage on validation rules
- **Error Handling**: All error paths tested

### Performance Targets
- **Unit Tests**: <10ms average execution time
- **Integration Tests**: <200ms average execution time  
- **TestContainers Startup**: <30 seconds (cached)
- **Full Test Suite**: <5 minutes total execution

### Quality Indicators
- **Zero Flaky Tests**: All tests pass consistently
- **Clear Test Names**: Purpose obvious from test method name
- **Minimal Mocking**: Real database eliminates most mocking needs
- **Fast Feedback**: Developers get test results quickly

---

Remember: **TEST THE SIMPLE PATTERNS**. With direct Entity Framework services and real databases via TestContainers, testing becomes straightforward. Focus on business logic, not framework complexity. The Health service tests show exactly what patterns to follow.