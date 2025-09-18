# Test Migration Strategy - Vertical Slice Architecture
<!-- Created: 2025-09-18 -->
<!-- Agent: test-developer -->
<!-- Task: Provide step-by-step test migration strategy -->

## Migration Strategy Overview

This document provides the comprehensive strategy for migrating tests from the archived Domain-Driven Design architecture to the new Vertical Slice Architecture. The migration must preserve critical business logic while adapting to the new architectural patterns.

## Phase-Based Migration Approach

### Phase 1: Infrastructure Foundation (Week 1)
**Goal**: Restore basic test compilation and infrastructure
**Success Criteria**: Tests compile and basic health tests pass

#### Step 1.1: Fix Test Common Infrastructure
**Priority**: CRITICAL - Everything depends on this

```csharp
// 1. Update WitchCityRope.Tests.Common project references
// Remove: WitchCityRope.Core, WitchCityRope.Infrastructure
// Add: WitchCityRope.Api

// 2. Replace broken builders with new patterns
// OLD: EventBuilder using rich domain entities
public class EventBuilder
{
    public Event Build() => new Event(...); // BROKEN
}

// NEW: Event DTO Builder using simple DTOs
public class EventDtoBuilder
{
    public CreateEventRequest BuildRequest() => new CreateEventRequest
    {
        Title = _title ?? "Test Event",
        Description = _description ?? "Test Description",
        StartDate = _startDate ?? DateTime.UtcNow.AddDays(7),
        // Simple DTO construction, no business logic
    };
}
```

#### Step 1.2: Create New Test Base Classes
**Location**: `/tests/WitchCityRope.Tests.Common/TestBase/`

```csharp
// VerticalSliceTestBase.cs - Base for feature testing
public abstract class VerticalSliceTestBase : IDisposable
{
    protected WebApplicationFactory<Program> App { get; private set; }
    protected HttpClient Client { get; private set; }

    protected virtual void ConfigureApp(IServiceCollection services)
    {
        // Override for custom service configuration
    }
}

// FeatureTestBase.cs - Base for individual feature testing
public abstract class FeatureTestBase<TService> : VerticalSliceTestBase
{
    protected TService Service { get; private set; }
    protected Mock<ILogger<TService>> MockLogger { get; private set; }

    // Standard service setup with mocked dependencies
}
```

#### Step 1.3: Restore TestContainers Infrastructure
**Location**: `/tests/WitchCityRope.Tests.Common/Database/`

```csharp
// DatabaseTestFixture.cs - Updated for new DbContext
public class DatabaseTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .Build();
        await _container.StartAsync();

        // Apply migrations from new API project
        using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
```

#### Step 1.4: Create Feature Test Templates
**Location**: `/tests/WitchCityRope.Tests.Common/Templates/`

```csharp
// FeatureServiceTestTemplate.cs
public abstract class FeatureServiceTest<TService> : FeatureTestBase<TService>
    where TService : class
{
    // Standard patterns for testing feature services

    protected async Task<TResponse> TestServiceMethod<TRequest, TResponse>(
        Func<TService, TRequest, Task<TResponse>> serviceMethod,
        TRequest request)
    {
        // Standard arrange-act-assert pattern for service testing
    }
}

// FeatureEndpointTestTemplate.cs
public abstract class FeatureEndpointTest : VerticalSliceTestBase
{
    // Standard patterns for testing feature endpoints

    protected async Task<HttpResponseMessage> TestPostEndpoint<TRequest>(
        string endpoint,
        TRequest request)
    {
        // Standard HTTP endpoint testing pattern
    }
}
```

### Phase 2: Health Feature Migration (Week 1-2)
**Goal**: Prove migration patterns with simplest feature
**Success Criteria**: Complete Health feature test coverage

#### Step 2.1: Health Service Unit Tests
**Location**: `/tests/unit/api/Services/Health/HealthServiceTests.cs`

```csharp
// Migrate from domain service testing to feature service testing
public class HealthServiceTests : FeatureServiceTest<HealthService>
{
    [Fact]
    public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsHealthy()
    {
        // Arrange
        using var context = DatabaseFixture.CreateDbContext();
        var service = new HealthService(context, MockLogger.Object);

        // Act
        var (success, response, error) = await service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHealthAsync_WhenDatabaseUnavailable_ReturnsUnhealthy()
    {
        // Test error scenarios with feature service patterns
    }
}
```

#### Step 2.2: Health Endpoint Integration Tests
**Location**: `/tests/integration/api/Endpoints/Health/HealthEndpointTests.cs`

```csharp
public class HealthEndpointTests : FeatureEndpointTest
{
    [Fact]
    public async Task GetHealth_WithRunningDatabase_Returns200()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var health = JsonSerializer.Deserialize<HealthResponse>(content);
        health.Status.Should().Be("Healthy");
        health.DatabaseConnected.Should().BeTrue();
    }
}
```

### Phase 3: Authentication Feature Migration (Week 2)
**Goal**: Restore critical security testing
**Success Criteria**: Login/logout workflows fully tested

#### Step 3.1: Authentication Service Tests
**Location**: `/tests/unit/api/Services/Authentication/AuthenticationServiceTests.cs`

```csharp
public class AuthenticationServiceTests : FeatureServiceTest<AuthenticationService>
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var request = new LoginRequestDtoBuilder()
            .WithEmail("admin@witchcityrope.com")
            .WithPassword("Test123!")
            .Build();

        // Act
        var (success, response, error) = await Service.LoginAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Token.Should().NotBeEmpty();
        response.User.Email.Should().Be(request.Email);
    }

    [Theory]
    [InlineData("invalid@email.com", "Wrong password")]
    [InlineData("admin@witchcityrope.com", "wrongpassword")]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure(string email, string password)
    {
        // Test all failure scenarios
    }
}
```

#### Step 3.2: Authentication Endpoint Tests
**Location**: `/tests/integration/api/Endpoints/Authentication/AuthenticationEndpointTests.cs`

```csharp
public class AuthenticationEndpointTests : FeatureEndpointTest
{
    [Fact]
    public async Task PostLogin_WithValidCredentials_Returns200AndToken()
    {
        // Arrange
        var loginRequest = new { email = "admin@witchcityrope.com", password = "Test123!" };

        // Act
        var response = await TestPostEndpoint("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content);
        loginResponse.Success.Should().BeTrue();
        loginResponse.Token.Should().NotBeEmpty();
    }
}
```

### Phase 4: Events Feature Migration (Week 3)
**Goal**: Restore event management testing
**Success Criteria**: CRUD operations and business logic tested

#### Step 4.1: Migrate Event Entity Tests to Event Service Tests

```csharp
// OLD: Rich domain entity testing (BROKEN)
public class EventTests
{
    [Fact]
    public void Constructor_ValidData_CreatesEvent()
    {
        var @event = new Event(title, description, startDate, endDate, capacity, eventType, location, organizer, pricingTiers);
        // Test rich domain behavior
    }
}

// NEW: Feature service testing
public class EventServiceTests : FeatureServiceTest<EventService>
{
    [Fact]
    public async Task CreateEventAsync_WithValidData_CreatesEvent()
    {
        // Arrange
        var request = new CreateEventRequestBuilder()
            .WithTitle("Test Event")
            .WithStartDate(DateTime.UtcNow.AddDays(7))
            .WithCapacity(50)
            .Build();

        // Act
        var (success, response, error) = await Service.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.Title.Should().Be(request.Title);

        // Verify database persistence
        using var context = DatabaseFixture.CreateDbContext();
        var savedEvent = await context.Events.FindAsync(response.Id);
        savedEvent.Should().NotBeNull();
    }
}
```

#### Step 4.2: Business Logic Preservation Strategy

```csharp
// Preserve critical business rules from domain entities
public class EventBusinessRulesTests : FeatureServiceTest<EventService>
{
    [Fact]
    public async Task CreateEventAsync_WithPastStartDate_ReturnsValidationError()
    {
        // Preserve: "Start date cannot be in the past" business rule
        var request = new CreateEventRequestBuilder()
            .WithStartDate(DateTime.UtcNow.AddDays(-1)) // Past date
            .Build();

        var (success, response, error) = await Service.CreateEventAsync(request);

        success.Should().BeFalse();
        error.Should().Contain("Start date cannot be in the past");
    }

    [Fact]
    public async Task CreateEventAsync_WithZeroCapacity_ReturnsValidationError()
    {
        // Preserve: "Capacity must be greater than zero" business rule
        var request = new CreateEventRequestBuilder()
            .WithCapacity(0)
            .Build();

        var (success, response, error) = await Service.CreateEventAsync(request);

        success.Should().BeFalse();
        error.Should().Contain("Capacity must be greater than zero");
    }
}
```

### Phase 5: Pending Feature Tests (Week 4)
**Goal**: Create placeholder tests for unimplemented features
**Success Criteria**: All business logic has test placeholders

#### Step 5.1: Mark Unimplemented Tests as Pending

```csharp
public class PendingEventFeatureTests : FeatureServiceTest<EventService>
{
    [Fact(Skip = "Event publishing workflow not yet implemented in vertical slice architecture")]
    public async Task PublishEventAsync_WithUnpublishedEvent_PublishesSuccessfully()
    {
        // TODO: Implement when event publishing feature is added
        // This test preserves the business logic requirement:
        // - Only unpublished events can be published
        // - Publishing updates the event timestamp
        // - Published events appear in public listings

        throw new NotImplementedException("Awaiting event publishing feature implementation");
    }

    [Fact(Skip = "Event unpublishing with registrations not yet implemented")]
    public async Task UnpublishEventAsync_WithExistingRegistrations_ReturnsBusinessRuleError()
    {
        // TODO: Implement when event unpublishing feature is added
        // This test preserves the business logic requirement:
        // - Events with confirmed registrations cannot be unpublished
        // - Must handle existing user registrations appropriately

        throw new NotImplementedException("Awaiting event unpublishing feature implementation");
    }
}
```

#### Step 5.2: Create Feature Implementation Tracking

```csharp
// FeatureImplementationTracker.cs
public static class FeatureImplementationStatus
{
    public static readonly Dictionary<string, bool> Features = new()
    {
        ["EventPublishing"] = false,          // Tests marked as pending
        ["EventUnpublishing"] = false,        // Tests marked as pending
        ["RegistrationWorkflow"] = false,     // Tests marked as pending
        ["PaymentProcessing"] = false,        // Tests marked as pending
        ["UserRoleManagement"] = false,       // Tests marked as pending
        ["EventCapacityManagement"] = false,  // Tests marked as pending
    };

    public static void RequireFeature(string featureName)
    {
        if (!Features[featureName])
        {
            throw new NotImplementedException($"Feature '{featureName}' not yet implemented in vertical slice architecture");
        }
    }
}
```

## Test Pattern Templates

### 1. Feature Service Test Template

```csharp
public class {FeatureName}ServiceTests : FeatureServiceTest<{FeatureName}Service>
{
    // ARRANGE: Use DTO builders, not domain entities
    // ACT: Test service methods directly
    // ASSERT: Verify service results AND database state

    [Fact]
    public async Task {MethodName}_With{ValidScenario}_Returns{ExpectedResult}()
    {
        // Arrange
        var request = new {RequestType}Builder()
            .With{Property}({ValidValue})
            .Build();

        // Act
        var (success, response, error) = await Service.{MethodName}(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        // Verify database state change
        using var context = DatabaseFixture.CreateDbContext();
        var entity = await context.{Entities}.FindAsync(response.Id);
        entity.Should().NotBeNull();
    }

    [Theory]
    [InlineData({InvalidValue1}, "{ExpectedError1}")]
    [InlineData({InvalidValue2}, "{ExpectedError2}")]
    public async Task {MethodName}_With{InvalidScenario}_Returns{ErrorType}(
        {ParamType} invalidValue,
        string expectedError)
    {
        // Test validation and business rule enforcement
    }
}
```

### 2. Feature Endpoint Test Template

```csharp
public class {FeatureName}EndpointTests : FeatureEndpointTest
{
    [Fact]
    public async Task Post{FeatureName}_WithValidRequest_Returns{StatusCode}()
    {
        // Arrange
        var request = new {RequestType}Builder().Build();

        // Act
        var response = await TestPostEndpoint("/api/{endpoint}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.{ExpectedStatus});

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<{ResponseType}>(content);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Get{FeatureName}_WithValidId_Returns{FeatureName}()
    {
        // Test GET endpoints with proper serialization
    }
}
```

### 3. Business Logic Preservation Template

```csharp
public class {FeatureName}BusinessRulesTests : FeatureServiceTest<{FeatureName}Service>
{
    // Preserve ALL business rules from original domain entities

    [Fact]
    public async Task {ServiceMethod}_With{BusinessRuleViolation}_ReturnsBusinessRuleError()
    {
        // Each business rule from the old domain entity becomes a test
        // Use descriptive test names that capture the business intent

        // Arrange: Set up scenario that violates business rule
        // Act: Call service method
        // Assert: Verify business rule is enforced
    }
}
```

## Migration Execution Plan

### Week 1: Foundation
- [ ] Fix WitchCityRope.Tests.Common compilation
- [ ] Create new test base classes
- [ ] Restore TestContainers infrastructure
- [ ] Create feature test templates
- [ ] Migrate Health feature tests

### Week 2: Critical Features
- [ ] Migrate Authentication feature tests
- [ ] Migrate basic Events feature tests
- [ ] Create DTO builders for all features
- [ ] Restore integration test infrastructure

### Week 3: Business Logic
- [ ] Preserve all Event domain business rules
- [ ] Preserve all User domain business rules
- [ ] Create comprehensive validation tests
- [ ] Add error handling tests

### Week 4: Pending Features
- [ ] Mark unimplemented features as pending
- [ ] Create feature implementation tracker
- [ ] Document business logic preservation
- [ ] Create migration handoff documentation

## Success Metrics

### Compilation Metrics
- [ ] 100% test compilation success
- [ ] 0 broken references to archived code
- [ ] All test projects build successfully

### Coverage Metrics
- [ ] Health feature: 100% method coverage
- [ ] Authentication feature: 100% endpoint coverage
- [ ] Events feature: 80% service coverage
- [ ] Business rules: 100% preservation rate

### Quality Metrics
- [ ] All critical business logic has tests
- [ ] All validation rules have tests
- [ ] All error scenarios have tests
- [ ] Performance baselines established

## Risk Mitigation

### Business Logic Loss Prevention
1. **Systematic Review**: Compare old domain tests to new service tests
2. **Business Rule Mapping**: Document all business rules being preserved
3. **Stakeholder Review**: Have business stakeholders validate test scenarios
4. **Incremental Migration**: Migrate one feature at a time with validation

### Test Infrastructure Reliability
1. **TestContainers Stability**: Use container pooling and cleanup
2. **Database State Management**: Ensure test isolation with unique data
3. **Performance Monitoring**: Track test execution times
4. **CI/CD Integration**: Ensure tests run reliably in pipeline

### Team Coordination
1. **Clear Ownership**: Test-developer owns ALL test migration work
2. **Communication Protocol**: Daily updates on migration progress
3. **Blocking Issues**: Escalate architectural questions immediately
4. **Knowledge Transfer**: Document patterns for future test development

---

**EXECUTION NOTE**: This migration strategy prioritizes preserving critical business logic while adapting to new architectural patterns. Each phase builds on the previous, ensuring working test infrastructure at every step.