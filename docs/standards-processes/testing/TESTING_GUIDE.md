# WitchCityRope Testing Guide
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Testing Team -->
<!-- Status: Active -->

## Overview
This is the comprehensive testing guide for the WitchCityRope project. It covers all test types, how to run them, write them, and troubleshoot common issues.

## Quick Start

### Prerequisites
```bash
# Docker must be running for integration tests
sudo systemctl start docker

# Navigate to project root
cd /home/chad/repos/witchcityrope/WitchCityRope

# Build the solution
dotnet build
```

### Running All Tests
```bash
# Run all .NET tests (unit + integration)
dotnet test

# Run E2E tests separately (requires app running)
./dev.sh  # Start application in one terminal

# In another terminal:
cd tests/playwright && npm test
```

### Running Specific Test Types
```bash
# Unit tests only (fast, no dependencies)
dotnet test --filter "Category=Unit"

# Integration tests only (requires Docker)
dotnet test --filter "Category=Integration"

# Specific feature tests
dotnet test --filter "FullyQualifiedName~Authentication"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Organization

### Project Structure
```
tests/
├── Unit Tests (No External Dependencies)
│   ├── WitchCityRope.Core.Tests/        # Domain logic, entities, value objects
│   ├── WitchCityRope.Api.Tests/         # API services, controllers
│   └── WitchCityRope.Tests.Common/      # Shared test utilities, builders
│
├── Integration Tests (Database/Services Required)
│   ├── WitchCityRope.IntegrationTests/  # Full stack integration
│   └── WitchCityRope.Infrastructure.Tests/ # Data access, repositories
│
├── Component Tests (UI Testing)
│   └── WitchCityRope.Web.Tests/         # Blazor component tests
│
└── E2E Tests (Full Browser Testing)
    └── playwright/                       # Playwright browser tests
        ├── tests/
        │   ├── auth/                    # Authentication flows
        │   ├── events/                  # Event management
        │   ├── admin/                   # Admin functionality
        │   └── user/                    # User features
        ├── pages/                       # Page Object Models
        └── helpers/                     # Test utilities
```

## Writing Tests

### Unit Test Guidelines

#### Basic Structure
```csharp
public class EventTests
{
    [Fact]
    public void CreateEvent_WithValidData_Succeeds()
    {
        // Arrange
        var title = "Test Event";
        var startTime = DateTime.UtcNow.AddDays(7);
        
        // Act
        var result = Event.Create(title, startTime);
        
        // Assert
        result.Should().BeSuccessful();
        result.Value.Title.Should().Be(title);
    }
    
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
}
```

#### Using Test Builders
```csharp
// Use builders for complex objects
var user = new UserBuilder()
    .WithEmail("test@example.com")
    .WithSceneName("TestUser")
    .AsVetted()
    .Build();

var event = new EventBuilder()
    .WithTitle("Monthly Rope Jam")
    .WithCapacity(50)
    .StartsInDays(7)
    .WithOrganizer(user)
    .Build();
```

### Integration Test Guidelines

#### Database Testing with PostgreSQL
```csharp
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

#### Important: PostgreSQL Requirements
- All DateTime values must be UTC
- Use unique test data (GUIDs for names/emails)
- **ALWAYS run health check first** (see [integration-test-patterns.md](integration-test-patterns.md) for complete setup):

```bash
# 1. FIRST: Run health checks
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. ONLY if health checks pass: Run integration tests  
dotnet test tests/WitchCityRope.IntegrationTests/
```

### E2E Test Guidelines (Playwright)

#### Basic E2E Test
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

#### Page Object Model
```typescript
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
}
```

### Component Test Guidelines (Blazor)

```csharp
[Fact]
public void EventCard_DisplaysEventDetails()
{
    // Arrange
    using var ctx = new TestContext();
    var event = new EventBuilder().Build();
    
    // Act
    var component = ctx.RenderComponent<EventCard>(parameters => parameters
        .Add(p => p.Event, event));
    
    // Assert
    component.Find(".event-title").TextContent.Should().Be(event.Title);
    component.Find(".event-date").TextContent.Should().Contain(event.StartTime);
}
```

## Test Execution

### Development Workflow
```bash
# Before committing - quick unit tests
dotnet test tests/WitchCityRope.Core.Tests/ --no-build

# Before PR - full test suite
./scripts/run-all-tests.sh

# Specific test debugging
dotnet test --filter "MethodName" --logger "console;verbosity=detailed"
```

### E2E Test Commands
```bash
cd tests/playwright

# Run all tests
npm test

# Run specific file
npm test auth/login.spec.ts

# Debug mode with browser
npm test -- --debug

# UI mode (recommended for debugging)
npm test -- --ui

# Update screenshots
npm test -- --update-snapshots

# Run specific browser
npm test -- --project=chromium
```

### Test Coverage
```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage"

# Open coverage/index.html in browser
```

## Common Issues and Solutions

### Issue: "Headers are read-only" in Blazor Tests
**Solution**: Don't use SignInManager directly in Blazor components. Use API endpoints for authentication.

### Issue: "Database already exists" Errors
**Solution**: Use unique database names
```csharp
var dbName = $"Test_{Guid.NewGuid():N}";
```

### Issue: Test Timeout in Integration Tests
**Solution**: Increase timeout for container startup
```csharp
[Fact(Timeout = 30000)] // 30 seconds
public async Task SlowIntegrationTest() { }
```

### Issue: Flaky E2E Tests
**Solution**: Use proper wait strategies
```typescript
// Bad - fixed wait
await page.waitForTimeout(2000);

// Good - wait for condition
await page.waitForSelector('[data-testid="loaded"]');
await expect(page.locator('.spinner')).not.toBeVisible();
```

### Issue: Concurrent Test Failures
**Solution**: Use test collections
```csharp
[Collection("DatabaseTests")] // Sequential execution
public class ProblematicTests { }
```

## Test Data Management

### Seeded Test Users
```
admin@witchcityrope.com / Test123!     - Administrator, Vetted
member@witchcityrope.com / Test123!    - Member, Vetted  
user@witchcityrope.com / Test123!      - Member, Not Vetted
teacher@witchcityrope.com / Test123!   - Teacher, Vetted
```

### Creating Unique Test Data
```csharp
// Always use unique identifiers
var email = $"test-{Guid.NewGuid():N}@example.com";
var sceneName = $"TestUser-{DateTime.UtcNow.Ticks}";
```

## CI/CD Integration

### GitHub Actions Configuration
```yaml
- name: Run Unit Tests
  run: dotnet test --filter "Category=Unit" --logger "trx"

- name: Run Integration Tests  
  run: dotnet test --filter "Category=Integration" --logger "trx"
  
- name: Run E2E Tests
  run: |
    cd tests/playwright
    npm ci
    npx playwright install
    npm test
```

## Best Practices

### 1. Test Naming Convention
```csharp
// Pattern: Method_Scenario_ExpectedResult
CreateEvent_WithPastDate_ReturnsValidationError()
Login_WithValidCredentials_RedirectsToDashboard()
```

### 2. AAA Pattern
```csharp
// Arrange - Set up test data
// Act - Execute the operation
// Assert - Verify the result
```

### 3. Test Independence
- Each test must be able to run in isolation
- No dependencies on test execution order
- Clean up test data in TearDown

### 4. Use Meaningful Assertions
```csharp
// Bad
Assert.True(result.IsSuccess);

// Good
result.Should().BeSuccessful()
    .And.HaveValue()
    .Which.Title.Should().Be("Expected Title");
```

### 5. Mock External Dependencies
```csharp
var emailService = new Mock<IEmailService>();
emailService.Setup(x => x.SendAsync(It.IsAny<Email>()))
    .ReturnsAsync(true);
```

## Performance Guidelines

### Target Metrics
- Unit Tests: < 1ms per test
- Integration Tests: < 100ms per test  
- E2E Tests: < 5s per test
- Full Suite: < 5 minutes

### Optimization Tips
1. Run tests in parallel where possible
2. Use in-memory database for unit tests
3. Share expensive resources (containers)
4. Minimize file I/O in tests

## Troubleshooting

### Enable Detailed Logging
```bash
# .NET tests
export ASPNETCORE_ENVIRONMENT=Development
dotnet test --logger "console;verbosity=detailed"

# Playwright tests
DEBUG=pw:api npm test
```

### Common Error Messages

**"No service for type 'X' has been registered"**
- Add missing service registration in test setup

**"The instance of entity type 'X' cannot be tracked"**
- Use separate DbContext instances or detach entities

**"Connection refused" in integration tests**
- Ensure Docker is running
- Check if containers are healthy

## Additional Resources

- [Test Catalog](TEST_CATALOG.md) - Complete inventory of all tests
- [Current Test Status](CURRENT_TEST_STATUS.md) - Latest test health metrics
- [Playwright Migration Guide](playwright-migration/README.md) - Puppeteer to Playwright
- [Lessons Learned - Test Writers](/docs/lessons-learned/test-writers.md) - Common pitfalls and solutions

---

*Remember: Tests are living documentation. Keep them clean, clear, and current.*