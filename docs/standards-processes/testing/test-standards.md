# Testing Standards

**Purpose**: Standards for test organization, naming, data management, and quality.
**When to Read**: When writing unit tests, integration tests, or organizing test code.
**Related**: [Testing Guide](/docs/standards-processes/testing/TESTING_GUIDE.md), [E2E Testing Patterns](/docs/standards-processes/testing/E2E_TESTING_PATTERNS.md)

## Test Organization and Naming

```csharp
/// <summary>
/// Tests for EventService focusing on event registration business logic.
/// Tests are organized by method being tested, then by scenario.
/// Each test follows the Arrange-Act-Assert pattern with clear naming
/// that describes the scenario and expected outcome.
/// </summary>
public class EventServiceTests
{
    /// <summary>
    /// Tests successful event registration when all conditions are met:
    /// - User is eligible (vetted, not already registered)
    /// - Event has available capacity
    /// - Payment processing succeeds
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenUserEligibleAndEventHasCapacity_ShouldCreateRegistration()
    {
        // Arrange - Set up test data and dependencies
        var userId = 123;
        var eventId = 456;
        var mockEvent = CreateMockEvent(eventId, capacity: 20, registeredCount: 15);
        var mockUser = CreateMockUser(userId, isVetted: true);

        var mockRepository = new Mock<IEventRepository>();
        mockRepository.Setup(r => r.GetEventAsync(eventId))
                     .ReturnsAsync(mockEvent);

        var service = new EventService(mockRepository.Object, _logger, _cache);

        // Act - Execute the method being tested
        var result = await service.RegisterForEventAsync(userId, eventId);

        // Assert - Verify the expected outcome
        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.Confirmed, result.Registration.Status);
        mockRepository.Verify(r => r.CreateRegistrationAsync(
            It.Is<Registration>(reg => reg.UserId == userId && reg.EventId == eventId)),
            Times.Once);
    }

    /// <summary>
    /// Tests that registration fails appropriately when event is at capacity,
    /// and user is added to waitlist instead of being given confirmed registration.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenEventAtCapacity_ShouldAddUserToWaitlist()
    {
        // Test implementation with clear documentation of the scenario
    }
}
```

## Test Naming Convention

Format: `MethodName_WhenCondition_ShouldExpectedBehavior`

Examples:
- `RegisterForEventAsync_WhenUserEligibleAndEventHasCapacity_ShouldCreateRegistration`
- `RegisterForEventAsync_WhenEventAtCapacity_ShouldAddUserToWaitlist`
- `ProcessPaymentAsync_WhenPaymentFails_ShouldReturnFailureResult`
- `GetEventsAsync_WhenNoEventsExist_ShouldReturnEmptyList`

## Test Data Management

```csharp
/// <summary>
/// Provides consistent test data creation methods to ensure tests are
/// reliable and maintainable. Uses builder pattern for complex objects
/// and provides reasonable defaults while allowing customization.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a mock event with sensible defaults for testing.
    /// Allows customization of key properties while providing
    /// realistic default values for all required fields.
    /// </summary>
    public static Event CreateEvent(
        int id = 1,
        string name = "Test Event",
        DateTime? startTime = null,
        int capacity = 20,
        int registeredCount = 0,
        EventType type = EventType.Class,
        decimal price = 50.00m)
    {
        return new Event
        {
            Id = id,
            Name = name,
            StartTime = startTime ?? DateTime.UtcNow.AddDays(7), // Default to one week from now
            Capacity = capacity,
            RegisteredCount = registeredCount,
            Type = type,
            Price = price,
            Description = $"Test description for {name}",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a mock user with sensible defaults for testing.
    /// </summary>
    public static ApplicationUser CreateUser(
        Guid? id = null,
        string email = "test@witchcityrope.com",
        string sceneName = "Test Scene Name",
        bool isVetted = false)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = sceneName,
            VettingStatus = isVetted ? VettingStatus.Approved : VettingStatus.NotStarted,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
    }
}
```

## Test Structure

### Arrange-Act-Assert Pattern
All tests should follow the AAA pattern:

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var input = "test input";
    var expectedOutput = "expected output";
    var mockService = new Mock<IService>();
    mockService.Setup(s => s.ProcessAsync(input))
              .ReturnsAsync(expectedOutput);

    // Act - Execute the method being tested
    var result = await systemUnderTest.PerformOperationAsync(input);

    // Assert - Verify the expected outcome
    Assert.Equal(expectedOutput, result);
    mockService.Verify(s => s.ProcessAsync(input), Times.Once);
}
```

## Test Quality Standards

### Required Test Coverage
- **Public methods**: All public methods must have tests
- **Business logic**: Critical business logic requires comprehensive scenarios
- **Edge cases**: Test boundary conditions and error cases
- **Integration points**: Test service integration with mocks

### Test Characteristics
- **Independent**: Tests should not depend on each other
- **Repeatable**: Tests should produce same results every run
- **Fast**: Unit tests should complete in milliseconds
- **Clear**: Test intent should be obvious from name and structure
- **Maintainable**: Test data builders and helpers for reusability

### What NOT to Test
- **Third-party libraries**: Don't test framework code
- **Simple properties**: Don't test auto-properties
- **Constructors**: Only test if they have complex logic
- **Private methods**: Test through public interface

## Mocking Guidelines

```csharp
// Use Moq for dependency mocking
var mockRepository = new Mock<IEventRepository>();
mockRepository.Setup(r => r.GetEventAsync(eventId))
             .ReturnsAsync(testEvent);

// Verify method calls
mockRepository.Verify(r => r.CreateRegistrationAsync(
    It.IsAny<Registration>()), Times.Once);

// Verify specific argument values
mockRepository.Verify(r => r.UpdateEventAsync(
    It.Is<Event>(e => e.Id == eventId && e.Capacity == 30)), Times.Once);
```

## Test Categories

Use categories to organize test runs:

```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task UnitTestExample() { }

[Fact]
[Trait("Category", "Integration")]
public async Task IntegrationTestExample() { }

[Fact]
[Trait("Category", "E2E")]
public async Task EndToEndTestExample() { }
```

Run specific categories:
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```
