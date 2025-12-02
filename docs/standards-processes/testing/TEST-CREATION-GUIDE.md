# Test Creation Guide
---
title: Test Creation Guide
scope: How to WRITE tests (unit, integration, E2E)
audience: test-developer agent, developers writing tests
related:
  - Test Execution Guide (how to RUN tests)
  - Playwright Guide (E2E testing patterns)
  - Integration Test Patterns (database testing)
last_updated: 2025-12-01
version: 1.0
---

## What This Guide Covers

This guide teaches you HOW TO WRITE tests for WitchCityRope:

✅ **Test writing patterns** - Unit, integration, E2E test structures
✅ **Test data management** - TestHelperService, builders, unique data
✅ **Selector patterns** - data-testid, Mantine v7, React Strict Mode
✅ **Authentication patterns** - Login helpers, session management
✅ **Wait strategies** - Proper async handling, avoiding flakiness
✅ **Container-compatible patterns** - URLs, database connections
✅ **CSRF token handling** - Frontend token management
✅ **TDD workflow** - Red-green-refactor cycle

## What This Guide Does NOT Cover

❌ **How to RUN tests** → See [Test Execution Guide](TEST-EXECUTION-GUIDE.md)
❌ **Docker setup** → See [Test Execution Guide](TEST-EXECUTION-GUIDE.md)
❌ **CI/CD configuration** → See [Test Execution Guide](TEST-EXECUTION-GUIDE.md)
❌ **Test infrastructure troubleshooting** → See [Test Execution Guide](TEST-EXECUTION-GUIDE.md)

## Prerequisites

Before writing tests:
- [ ] Read [Test Execution Guide](TEST-EXECUTION-GUIDE.md) to understand test environment
- [ ] Verify Docker containers running (use `container-restart` skill)
- [ ] Review [Test Catalog](TEST_CATALOG.md) to avoid duplicates
- [ ] Check [Playwright Guide](browser-automation/playwright-guide.md) for E2E patterns

---

## Test Organization

### Directory Structure (MANDATORY)

**ALL tests MUST be in `/tests/` directory:**

```
tests/
├── e2e/                              # E2E Tests (Playwright)
│   ├── admin/                       # Admin functionality
│   ├── auth/                        # Authentication flows
│   ├── events/                      # Event management
│   ├── vetting/                     # Vetting system
│   └── test-utils/                  # E2E test utilities
│
├── unit/
│   ├── api/                         # .NET API Unit Tests
│   │   └── Features/               # Organized by domain
│   └── web/                         # React Unit Tests
│       ├── components/             # Component tests
│       ├── hooks/                  # Hook tests
│       └── pages/                  # Page tests
│
├── integration/                      # Full-Stack Integration Tests
│   ├── api/                        # API integration tests
│   └── Features/                   # Feature integration tests
│
└── shared/                          # Shared Test Infrastructure
    ├── builders/                   # Test data builders
    ├── fixtures/                   # Test fixtures
    └── helpers/                    # Test helper functions
```

**CRITICAL RULE**: NO tests co-located with source code. ALL tests go in `/tests/`.

### Test Naming Convention

**Format**: `MethodName_WhenCondition_ShouldExpectedBehavior`

**Examples**:
```csharp
RegisterForEventAsync_WhenUserEligibleAndEventHasCapacity_ShouldCreateRegistration
ProcessPaymentAsync_WhenPaymentFails_ShouldReturnFailureResult
GetEventsAsync_WhenNoEventsExist_ShouldReturnEmptyList
```

**TypeScript/E2E**:
```typescript
test('should login with valid credentials and redirect to dashboard')
test('should display validation error for invalid email format')
test('should prevent registration when event at capacity')
```

---

## Unit Test Creation

### Basic Structure (Arrange-Act-Assert)

```csharp
[Fact]
public async Task CreateEvent_WithValidData_Succeeds()
{
    // Arrange - Set up test data and dependencies
    var title = "Test Event";
    var startTime = DateTime.UtcNow.AddDays(7);
    var mockRepository = new Mock<IEventRepository>();

    // Act - Execute the operation
    var result = Event.Create(title, startTime);

    // Assert - Verify the expected outcome
    result.Should().BeSuccessful();
    result.Value.Title.Should().Be(title);
}
```

### Theory Tests (Multiple Scenarios)

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public void CreateEvent_WithInvalidTitle_Fails(string invalidTitle)
{
    // Arrange
    var startTime = DateTime.UtcNow.AddDays(7);

    // Act
    var result = Event.Create(invalidTitle, startTime);

    // Assert
    result.Should().BeFailure();
    result.Error.Should().Contain("Title");
}
```

### Mocking Dependencies

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

### Advanced Mocking: UserManager with Token Providers

**Problem**: `Substitute.ForPartsOf<UserManager>` calls base implementation which requires token providers.

**Solution**: Use `When().DoNotCallBase()`:

```csharp
public class MyServiceTests : IAsyncLifetime
{
    private UserManager<ApplicationUser> _userManager = null!;

    public async Task InitializeAsync()
    {
        _userManager = Substitute.ForPartsOf<UserManager<ApplicationUser>>(
            userStore, null, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, userLogger);

        // Prevent calling real UserManager methods
        _userManager.When(x => x.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>()))
            .DoNotCallBase();
    }

    [Fact]
    public async Task PasswordReset_WithValidUser_GeneratesToken()
    {
        // Configure mock return in individual tests
        _userManager.GeneratePasswordResetTokenAsync(user)
            .Returns(Task.FromResult("reset-token-123"));

        // Act & Assert
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        token.Should().Be("reset-token-123");
    }
}
```

**When to use**: Token generation methods (password reset, email confirmation), methods requiring external dependencies.

### Testing Minimal API Endpoints with Anonymous Types

**Problem**: `Results.Ok(new { ... })` creates anonymous types that cannot be cast.

**Solution**: Use reflection to access properties:

```csharp
[Fact]
public async Task Endpoint_WithSuccess_Returns200OkWithAnonymousType()
{
    // Arrange
    var mockService = Substitute.For<IMyService>();
    mockService.ProcessAsync(Arg.Any<string>()).Returns((true, string.Empty));

    // Act
    var result = await EndpointHelper(mockService);

    // Assert - Use reflection for anonymous types
    result.Should().BeAssignableTo<IResult>();

    var statusCodeProperty = result.GetType().GetProperty("StatusCode");
    var statusCode = (int?)statusCodeProperty!.GetValue(result);
    statusCode.Should().Be(200);

    var valueProperty = result.GetType().GetProperty("Value");
    dynamic responseValue = valueProperty!.GetValue(result)!;

    bool success = responseValue.Success;
    string message = responseValue.Message;

    success.Should().BeTrue();
    message.Should().Be("Expected message");
}
```

---

## Integration Test Creation

### Database Testing with PostgreSQL

```csharp
[Collection("PostgreSQL Integration Tests")]
public class EventRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task SaveEvent_PersistsToDatabase()
    {
        // Arrange
        await using var context = CreateDbContext();
        var repository = new EventRepository(context);
        var event = new EventBuilder().Build();

        // Act
        await repository.AddAsync(event);
        await context.SaveChangesAsync();

        // Assert
        var saved = await repository.GetByIdAsync(event.Id);
        saved.Should().NotBeNull();
        saved.Title.Should().Be(event.Title);
    }
}
```

### PostgreSQL Requirements

**CRITICAL RULES**:
- ✅ All DateTime values MUST be UTC
- ✅ Use unique test data (GUIDs for names/emails)
- ✅ Use `[Collection("PostgreSQL Integration Tests")]` attribute
- ✅ Clean up test data after each test

```csharp
// Correct: UTC datetime
var startTime = DateTime.UtcNow.AddDays(7);

// Correct: Unique identifiers
var email = $"test-{Guid.NewGuid():N}@example.com";

// Correct: Collection attribute for shared container
[Collection("PostgreSQL Integration Tests")]
public class MyIntegrationTests { }
```

See [Integration Test Patterns](integration-test-patterns.md) for complete PostgreSQL testing guide.

---

## E2E Test Creation (Playwright)

> 🚨 **MANDATORY: READ PLAYWRIGHT GUIDE FOR E2E TESTS** 🚨
>
> This section provides an **overview** of E2E patterns. For complete patterns including:
> - TestHelperService API endpoints with code examples
> - Container-compatible URL patterns (CRITICAL for Docker)
> - Wait strategies (why networkidle fails)
> - Mantine v7 checkbox interactions
> - React Strict Mode `.last()` patterns
> - Database persistence verification
>
> **You MUST also read**: [Playwright Guide](browser-automation/playwright-guide.md)

### Basic E2E Test Structure

```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';

test.describe('Authentication', () => {
    test('should login with valid credentials', async ({ page }) => {
        // Arrange
        const loginPage = new LoginPage(page);
        await loginPage.goto();

        // Act
        await loginPage.login('member@witchcityrope.com', 'Test123!');

        // Assert
        await expect(page).toHaveURL('/dashboard');
        await expect(page.locator('[data-testid="welcome-message"]'))
            .toContainText('Welcome');
    });
});
```

### Container-Compatible URL Patterns

**CRITICAL**: Use relative URLs for container compatibility:

```typescript
// ✅ CORRECT - Container-compatible
await page.goto('/events');
await page.goto('/login');

// ❌ WRONG - Breaks in containers
await page.goto('http://localhost:5173/events');
await page.goto('http://test-web:5173/events'); // Hardcoded container name
```

**Why**: Playwright's `baseURL` configuration handles the full URL. Tests work in both dev and test containers.

### Selector Patterns (Mantine v7 + React Strict Mode)

**Use `data-testid` for ALL selectors** - Mantine v7 classes change dynamically:

```typescript
// ✅ CORRECT - Stable data-testid selectors
await page.click('[data-testid="create-event-btn"]');
await page.fill('[data-testid="event-title"]', title);
await page.locator('[data-testid="submit-btn"]').click();

// ❌ WRONG - Mantine classes change
await page.click('.mantine-Button-root'); // Class names are generated
await page.fill('input[class*="Input"]', title); // Too fragile
```

**React Strict Mode Double-Mount Handling**:
```typescript
// Wait for component to stabilize (React Strict Mode double-mount)
await expect(page.locator('[data-testid="event-form"]')).toBeVisible();
await page.waitForTimeout(100); // Brief stabilization wait

// Then interact
await page.fill('[data-testid="event-title"]', 'My Event');
```

**Component-Specific Selectors**:
```typescript
// Mantine Select
await page.click('[data-testid="event-type-select"]');
await page.click('[data-value="Class"]'); // Mantine option

// Mantine DateInput
await page.fill('[data-testid="event-date"]', '2025-12-25');

// Mantine RichTextEditor (TipTap)
await page.fill('[data-testid="event-description"] .tiptap', 'Description');
```

See [Playwright Guide](browser-automation/playwright-guide.md) for complete selector patterns.

### Wait Strategies (CRITICAL)

**NEVER use fixed timeouts** - Use condition-based waits:

```typescript
// ❌ WRONG - Fixed wait (flaky)
await page.waitForTimeout(2000);

// ✅ CORRECT - Wait for condition
await page.waitForSelector('[data-testid="loaded"]');
await expect(page.locator('.spinner')).not.toBeVisible();
await page.waitForLoadState('networkidle');
```

**Common wait patterns**:
```typescript
// Wait for element to be visible
await expect(page.locator('[data-testid="dashboard"]')).toBeVisible();

// Wait for element to be hidden
await expect(page.locator('[data-testid="loading"]')).not.toBeVisible();

// Wait for network to be idle
await page.waitForLoadState('networkidle');

// Wait for specific response
await page.waitForResponse(resp =>
    resp.url().includes('/api/events') && resp.status() === 200
);
```

### Authentication Patterns (MANDATORY)

**NEVER write custom login code** - Use the login helper:

```typescript
// ✅ CORRECT - Use shared login helper
import { loginAsAdmin, loginAsMember } from '../test-utils/auth-helpers';

test('admin can access event dashboard', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/admin/events');
    await expect(page.locator('[data-testid="events-dashboard"]')).toBeVisible();
});

// ❌ WRONG - Custom login code (duplicates, breaks, unmaintained)
test('admin can access event dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@witchcityrope.com');
    // ... duplicated login code
});
```

**Available auth helpers**:
- `loginAsAdmin(page)` - Admin user
- `loginAsMember(page)` - Regular member
- `loginAsVetted(page)` - Vetted member
- `loginAsTeacher(page)` - Teacher role
- `logout(page)` - Logout current user

**Session persistence**:
```typescript
// Login persists across page navigations
await loginAsAdmin(page);
await page.goto('/admin/events'); // Still logged in
await page.goto('/admin/users'); // Still logged in
```

### CSRF Token Handling

**CSRF tokens are handled automatically** in frontend:

```typescript
// ✅ NO manual token handling needed in E2E tests
// Frontend Axios interceptor handles tokens automatically

// Just test the functionality
await page.click('[data-testid="create-event-btn"]');
await page.fill('[data-testid="event-title"]', 'Test Event');
await page.click('[data-testid="submit-btn"]');

// Assert success
await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
```

**How it works**:
1. Frontend calls `/auth/csrf-token` on app load
2. Token stored in React state
3. Axios interceptor adds token to all requests
4. E2E tests don't need to handle tokens explicitly

See [CSRF Protection Guide](/docs/functional-areas/security/csrf-protection/developer-guide.md) for implementation details.

### Page Object Model Pattern

```typescript
// pages/event.page.ts
export class EventPage {
    constructor(private page: Page) {}

    async goto() {
        await this.page.goto('/events');
    }

    async createEvent(title: string, date: string) {
        await this.page.click('[data-testid="create-event-btn"]');
        await this.page.fill('[data-testid="event-title"]', title);
        await this.page.fill('[data-testid="event-date"]', date);
        await this.page.click('[data-testid="submit-btn"]');
    }

    async getEventByTitle(title: string) {
        return this.page.locator(`[data-testid="event-card"]:has-text("${title}")`);
    }
}

// Test file
import { EventPage } from '../pages/event.page';

test('should create new event', async ({ page }) => {
    const eventPage = new EventPage(page);
    await eventPage.goto();
    await eventPage.createEvent('Test Event', '2025-12-25');

    const event = await eventPage.getEventByTitle('Test Event');
    await expect(event).toBeVisible();
});
```

---

## Test Data Management

### TestHelperService Pattern (RECOMMENDED)

**Use TestHelperService for creating test data** - It handles uniqueness automatically:

```csharp
// In test setup
private readonly TestHelperService _testHelper = new();

[Fact]
public async Task RegisterForEvent_WithNewUser_Succeeds()
{
    // Arrange - TestHelperService creates unique user
    var user = await _testHelper.CreateUserAsync(
        email: "test@example.com",
        sceneName: "TestUser",
        isVetted: true
    );

    var event = await _testHelper.CreateEventAsync(
        title: "Test Event",
        capacity: 20
    );

    // Act
    var result = await _service.RegisterForEventAsync(user.Id, event.Id);

    // Assert
    result.Should().BeSuccessful();
}
```

**TestHelperService benefits**:
- ✅ Automatic unique identifiers (no conflicts)
- ✅ Proper entity relationships
- ✅ Realistic default values
- ✅ Database persistence included
- ✅ Cleanup handled automatically

### Test Data Builders (Alternative)

```csharp
public class EventBuilder
{
    private string _title = "Test Event";
    private DateTime _startTime = DateTime.UtcNow.AddDays(7);
    private int _capacity = 20;

    public EventBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public EventBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    public Event Build()
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = _title,
            StartTime = _startTime,
            Capacity = _capacity,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Usage
var event = new EventBuilder()
    .WithTitle("Monthly Rope Jam")
    .WithCapacity(50)
    .Build();
```

### Unique Test Data (CRITICAL)

**ALWAYS use unique identifiers** to prevent test interference:

```csharp
// ✅ CORRECT - Unique data
var email = $"test-{Guid.NewGuid():N}@example.com";
var sceneName = $"TestUser-{DateTime.UtcNow.Ticks}";

// ❌ WRONG - Hardcoded data (causes conflicts)
var email = "test@example.com"; // Multiple tests will conflict
var sceneName = "TestUser"; // Database unique constraint violation
```

### Seeded Test Users (Available)

Pre-seeded users in development/test databases:

```
admin@witchcityrope.com / Test123!     - Administrator, Vetted
teacher@witchcityrope.com / Test123!   - Teacher, Vetted
vetted@witchcityrope.com / Test123!    - Member, Vetted
member@witchcityrope.com / Test123!    - Member, Not Vetted
guest@witchcityrope.com / Test123!     - Attendee role
```

**When to use seeded users**:
- ✅ E2E authentication tests
- ✅ Quick manual testing
- ❌ Integration tests (use TestHelperService for isolation)

**Password handling (CRITICAL)**:
```typescript
// ✅ CORRECT - No escaping needed
const password = 'Test123!';

// ❌ WRONG - Escaped exclamation breaks authentication
const password = 'Test123\!'; // This will fail
```

---

## TDD Workflow (Test-Driven Development)

### Red-Green-Refactor Cycle

```
1. RED - Write failing test first
2. GREEN - Write minimal code to make test pass
3. REFACTOR - Improve code quality while tests stay green
```

**Example TDD workflow**:

```csharp
// 1. RED - Write failing test
[Fact]
public void Event_ShouldCalculateAvailableSpots()
{
    // Arrange
    var event = new EventBuilder()
        .WithCapacity(20)
        .WithRegisteredCount(15)
        .Build();

    // Act
    var available = event.AvailableSpots; // Property doesn't exist yet

    // Assert
    available.Should().Be(5);
}

// Test fails - property doesn't exist

// 2. GREEN - Add minimal implementation
public class Event
{
    public int Capacity { get; set; }
    public int RegisteredCount { get; set; }

    public int AvailableSpots => Capacity - RegisteredCount; // Minimal code
}

// Test passes

// 3. REFACTOR - Add validation/guards
public class Event
{
    public int Capacity { get; set; }
    public int RegisteredCount { get; set; }

    public int AvailableSpots => Math.Max(0, Capacity - RegisteredCount); // Prevent negative
}

// Test still passes, code quality improved
```

### TDD Best Practices

✅ **Write test before implementation**
✅ **Start with simplest test case**
✅ **Add edge cases incrementally**
✅ **Refactor with confidence** (tests verify correctness)
✅ **Keep tests independent** (each test can run alone)

❌ **Don't skip the RED step** (verify test can fail)
❌ **Don't write all tests at once** (incremental progress)
❌ **Don't refactor without green tests** (know when you break things)

---

## Anti-Patterns to Avoid

### ❌ Don't Test Framework Code

```csharp
// ❌ WRONG - Testing auto-property
[Fact]
public void Event_TitleProperty_ShouldStoreValue()
{
    var event = new Event { Title = "Test" };
    event.Title.Should().Be("Test"); // Pointless - C# does this
}

// ✅ CORRECT - Test business logic
[Fact]
public void Event_WithEmptyTitle_ShouldFail()
{
    var result = Event.Create(title: "");
    result.Should().BeFailure(); // Tests validation logic
}
```

### ❌ Don't Use Fixed Timeouts

```typescript
// ❌ WRONG - Flaky timing
await page.waitForTimeout(2000);

// ✅ CORRECT - Wait for condition
await expect(page.locator('[data-testid="loaded"]')).toBeVisible();
```

### ❌ Don't Hardcode URLs in E2E Tests

```typescript
// ❌ WRONG - Breaks in containers
await page.goto('http://localhost:5173/events');

// ✅ CORRECT - Use relative URLs
await page.goto('/events');
```

### ❌ Don't Duplicate Login Logic

```typescript
// ❌ WRONG - Duplicated login code
test('admin dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');
    // ... test code
});

// ✅ CORRECT - Use auth helper
test('admin dashboard', async ({ page }) => {
    await loginAsAdmin(page);
    // ... test code
});
```

### ❌ Don't Use Mantine CSS Classes as Selectors

```typescript
// ❌ WRONG - Classes change with Mantine updates
await page.click('.mantine-Button-root');

// ✅ CORRECT - Use data-testid
await page.click('[data-testid="submit-btn"]');
```

### ❌ Don't Skip Database Cleanup

```csharp
// ❌ WRONG - Leaves test data in database
[Fact]
public async Task CreateUser_Succeeds()
{
    var user = new User { Email = "test@example.com" };
    await _repository.AddAsync(user);
    // No cleanup - affects other tests
}

// ✅ CORRECT - Use TestHelperService (auto cleanup) or transaction
[Fact]
public async Task CreateUser_Succeeds()
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    var user = new User { Email = "test@example.com" };
    await _repository.AddAsync(user);

    await transaction.RollbackAsync(); // Cleanup
}
```

---

## Test Quality Standards

### Required Coverage

- **Public methods**: All public methods must have tests
- **Business logic**: Critical business logic requires comprehensive scenarios
- **Edge cases**: Test boundary conditions and error cases
- **Integration points**: Test service integration with mocks

### Test Characteristics

- **Independent**: Tests should not depend on each other
- **Repeatable**: Tests should produce same results every run
- **Fast**: Unit tests < 1ms, Integration tests < 100ms, E2E tests < 5s
- **Clear**: Test intent should be obvious from name and structure
- **Maintainable**: Use helpers and Page Objects for reusability

### Test Categories

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

---

## Additional Resources

### Documentation

- **[Test Execution Guide](TEST-EXECUTION-GUIDE.md)** - How to RUN tests
- **[Playwright Guide](browser-automation/playwright-guide.md)** - Complete E2E patterns
- **[Integration Test Patterns](integration-test-patterns.md)** - PostgreSQL testing
- **[Test Catalog](TEST_CATALOG.md)** - All existing tests

### Lessons Learned

- **[Test Developer Lessons](/docs/lessons-learned/test-developer-lessons-learned.md)** - Common mistakes
- **[Test Executor Lessons](/docs/lessons-learned/test-executor-lessons-learned.md)** - Execution issues
- **[Mantine E2E Patterns](/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md)** - Mantine-specific patterns

### Skills

- **test-catalog-updater** - Update test catalog after creating tests
- **phase-4-validator** - Validate testing phase completion
- **container-restart** - Restart Docker containers for testing

---

**Remember**: Write tests that clearly describe WHAT they test and WHY. Future you (and other developers) will thank you.
