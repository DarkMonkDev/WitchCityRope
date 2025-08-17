# Test Developer Lessons Learned
<!-- Last Updated: 2025-08-04 -->
<!-- Next Review: 2025-09-04 -->

## 🚨 CRITICAL: E2E Testing Uses Playwright Only

### All E2E Tests Migrated to Playwright

**UPDATED: January 2025** - All 180 Puppeteer tests migrated to Playwright

**NEVER CREATE PUPPETEER TESTS AGAIN**:
- ❌ NO `const puppeteer = require('puppeteer')` anywhere
- ❌ NO new Puppeteer test files in any directory
- ❌ DO NOT debug or modify existing Puppeteer tests in `/tests/e2e/` or `/ToBeDeleted/`
- ✅ ALL E2E tests are in `/tests/playwright/` directory
- ✅ USE Playwright TypeScript tests only: `import { test, expect } from '@playwright/test'`
- ✅ USE existing Page Object Models in `/tests/playwright/pages/`
- ✅ RUN tests with: `npm run test:e2e:playwright`

**Why This Matters**:
- 180 Puppeteer tests successfully migrated in January 2025
- Playwright tests are 40% faster and 86% less flaky
- All documentation and patterns exist for Playwright
- Puppeteer tests are deprecated and will be deleted

**Test Locations**:
- ✅ **ACTIVE**: `/tests/playwright/` (20 test files, 8 Page Objects, 6 helpers)
- ❌ **DEPRECATED**: `/tests/e2e/` (old Puppeteer tests - DO NOT USE)
- ❌ **DEPRECATED**: `/ToBeDeleted/` (old Puppeteer tests - DO NOT USE)

## E2E Testing (Playwright)

### Test Data Management
**Issue**: Tests failing due to duplicate user data  
**Solution**: Always create unique test data using timestamps or GUIDs
```javascript
const uniqueEmail = `test-${Date.now()}@example.com`;
const uniqueUsername = `user-${Date.now()}`;
```
**Applies to**: All E2E tests creating users or events

### Selector Strategy
**Issue**: CSS selectors breaking when UI changes  
**Solution**: Use data-testid attributes
```javascript
// ❌ WRONG
await page.click('.btn-primary');

// ✅ CORRECT
await page.click('[data-testid="submit-button"]');
```
**Applies to**: All UI element selections

### Wait Strategies
**Issue**: Tests failing due to timing issues  
**Solution**: Use proper wait conditions
```javascript
// ❌ WRONG - Fixed wait
await page.waitForTimeout(2000);

// ✅ CORRECT - Wait for specific condition
await page.waitForSelector('[data-testid="events-list"]');
await expect(page.locator('[data-testid="event-card"]')).toHaveCount(5);
```
**Applies to**: After navigation, form submission, or data loading

## Integration Testing

### 🚨 CRITICAL: Real PostgreSQL Required

**Critical**: Integration tests now use real PostgreSQL with comprehensive health checks. The in-memory database was hiding bugs.

### Health Check System (NEW - July 2025)

**ALWAYS run health checks before integration tests**:
```bash
# 1. FIRST: Run health checks to verify containers are ready
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. ONLY IF health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

**Health Check validates**:
- ✅ Database connection (PostgreSQL container is running)
- ✅ Database schema (all required tables and migrations applied)
- ✅ Seed data (test users and roles exist)

### PostgreSQL with TestContainers
**Issue**: "relation already exists" errors  
**Solution**: Use shared fixture pattern
```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyTests : IClassFixture<PostgreSqlFixture>
{
    // Tests share the same container instance
}
```
**Applies to**: All integration tests using database

### DateTime Handling - CRITICAL
**Issue**: PostgreSQL requires UTC timestamps - "Cannot write DateTime with Kind=Unspecified"
**Solution**: Always use UTC for dates
```csharp
// ❌ WRONG
var event = new Event { StartTime = DateTime.Now };
new DateTime(1990, 1, 1) // Kind is Unspecified

// ✅ CORRECT
var event = new Event { StartTime = DateTime.UtcNow };
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```
**Best Practice**: DbContext auto-converts all DateTime to UTC in UpdateAuditFields()

### Test Isolation - CRITICAL
**Issue**: Tests affecting each other's data - duplicate key violations
**Solution**: Use unique identifiers for ALL test data
```csharp
// ❌ WRONG - Will cause conflicts
var sceneName = "TestUser";
var email = "test@example.com";

// ✅ CORRECT - Always unique
var sceneName = $"TestUser_{Guid.NewGuid():N}";
var email = $"test-{Guid.NewGuid():N}@example.com";
```
**Applies to**: SceneNames, Emails, any unique fields

### Entity ID Initialization

**Problem**: Default Guid.Empty causes duplicate key violations

**Solution**: Always initialize IDs in constructors
```csharp
public Rsvp(Guid userId, Event @event)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this!
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

## Unit Testing

### Mocking Authentication
**Issue**: Complex authentication setup in tests  
**Solution**: Use simplified test authentication
```csharp
var authService = new Mock<IAuthService>();
authService.Setup(x => x.GetCurrentUserAsync())
    .ReturnsAsync(new UserDto { Id = testUserId });
```
**Applies to**: Tests requiring authenticated context

### Testing Validation
**Issue**: Not testing both valid and invalid cases  
**Solution**: Always test edge cases
```csharp
[Theory]
[InlineData("")]           // Empty
[InlineData(null)]         // Null  
[InlineData("a")]          // Too short
[InlineData("valid@email")] // Valid
public async Task Email_Validation_Works(string email) { }
```
**Applies to**: All validation testing

## Common Pitfalls

### Docker Environment
**Issue**: Tests pass locally but fail in CI  
**Solution**: Always test with Docker environment
```bash
docker-compose -f docker-compose.yml -f docker-compose.ci.yml up -d
npm run test:e2e
```

### Async/Await
**Issue**: Tests completing before async operations  
**Solution**: Always await async operations
```csharp
// ❌ WRONG
var task = service.CreateEventAsync(model);
Assert.NotNull(await repository.GetEventAsync(id));

// ✅ CORRECT
await service.CreateEventAsync(model);
Assert.NotNull(await repository.GetEventAsync(id));
```

### Test Naming
**Issue**: Unclear what test is testing  
**Solution**: Use Given_When_Then pattern
```csharp
// ❌ WRONG
public void Test1() { }

// ✅ CORRECT
public void Given_ValidEvent_When_Created_Then_ReturnsSuccessStatus() { }
```

## Documentation Consolidation (August 2025)

**Problem**: Testing documentation scattered across multiple files with duplicates
**Solution**: Consolidated into single source of truth pattern
- ✅ E2E_TESTING_PATTERNS.md → redirects to playwright-guide.md
- ✅ Agent definitions reference standards instead of duplicating code
- ✅ Health check commands centralized with cross-references
**Result**: Eliminated 400+ lines of duplicate Puppeteer patterns and outdated examples

## Deprecated Testing Approaches

1. **Puppeteer** - All E2E tests migrated to Playwright
2. **In-memory database** - Use PostgreSQL TestContainers
3. **Fixed test data** - Always use unique identifiers
4. **Thread.Sleep/waitForTimeout** - Use proper wait conditions
5. **Generic selectors** - Use data-testid attributes for reliable element selection

## Recent Fixes (August 2025)

### Login Selector Best Practices - August 13, 2025

**Problem**: E2E tests failing with "Element not found" for login form selectors
**Root Cause**: Tests using generic selectors instead of specific form field identifiers
**Solution**: Use specific form selectors or data-testid attributes

```javascript
// ❌ WRONG - Generic selectors may not match specific forms
await page.fill('input[type="email"]', 'admin@witchcityrope.com');
await page.fill('input[type="password"]', 'Test123!');

// ✅ CORRECT - Specific form field selectors
await page.fill('input[name="email"]', 'admin@witchcityrope.com');
await page.fill('input[name="password"]', 'Test123!');

// ✅ BEST - Data-testid for reliable testing
await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
await page.fill('[data-testid="password-input"]', 'Test123!');
```

**Best Practices**:
- Use data-testid attributes for reliable element selection
- Use Page Object Model pattern for reusable selectors
- Test form field names match frontend implementation
- Avoid generic selectors that may match multiple elements




## Admin User Management Testing Patterns - 2025-08-13

### Comprehensive Test Coverage Pattern
**Problem**: Need to test admin functionality across all layers (unit, integration, E2E)
**Solution**: Create focused test suite covering each layer with specific scenarios
**Implementation**:

```csharp
// Unit Tests - Mock HTTP responses for ApiClient methods
[Fact]
[Trait("Category", "Unit")]
public async Task GetUsersAsync_WithSearchParameters_ReturnsPagedResult()
{
    // Mock HttpMessageHandler for controlled responses
    _httpMessageHandlerMock.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync", ...)
        .ReturnsAsync(new HttpResponseMessage { ... });
    
    // Test specific parameters are passed correctly
    var result = await _apiClient.GetUsersAsync(searchRequest);
    
    // Verify both result and HTTP request details
    result.Should().NotBeNull();
    _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ...);
}

// Integration Tests - Real HTTP calls with authentication
[Fact]
[Trait("Category", "Integration")]
public async Task GetUsers_WithAdminAuth_ReturnsPagedUserList()
{
    await _client.AuthenticateAsAdminAsync(_factory);
    var response = await _client.GetAsync("api/admin/users?page=1&pageSize=10");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

// E2E Tests - Browser automation with real UI
test('should login as admin and verify real users load from API', async ({ page }) => {
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.goto('/admin/users');
    await expect(page.locator('[data-testid="user-grid"]')).toBeVisible();
});
```

**Benefits**: 
- Each layer tests different concerns (logic, HTTP, UI)
- Catches issues at appropriate abstraction level
- Provides confidence in complete feature functionality

### Integration Test Authentication Pattern
**Problem**: Admin endpoints require authentication, need consistent auth setup
**Solution**: Use extension methods for admin authentication in integration tests
**Pattern**:

```csharp
public static async Task AuthenticateAsAdminAsync(this HttpClient client, WebApplicationFactory factory)
{
    using var scope = factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
    
    var adminUser = await userManager.FindByEmailAsync("testadmin@witchcityrope.com");
    if (adminUser == null)
    {
        adminUser = new WitchCityRopeUser(...) { Role = UserRole.Administrator };
        await userManager.CreateAsync(adminUser, "Test123!");
    }
    
    // Set authentication cookie/header for subsequent requests
}
```

**Benefits**: Reusable auth setup, consistent test admin user, proper cleanup

### E2E Real Data Verification Pattern  
**Problem**: E2E tests need to verify real API data loads (not mock data)
**Solution**: Test for presence of real data indicators
**Pattern**:

```typescript
// Verify real API data loads by checking for realistic content
const userRows = page.locator('[data-testid="user-row"]').filter({ hasText: /@/ }); // Email addresses
await expect(userRows.first()).toBeVisible({ timeout: 15000 });

// Verify stats show real numbers (at least admin user exists)
const statsNumber = await page.locator('.stats-card .number').textContent();
const number = parseInt(statsNumber?.match(/[0-9]+/)?.[0] || '0');
expect(number).toBeGreaterThanOrEqual(1);
```

**Benefits**: Confirms integration with real API, not just UI mockups

### Cross-Layer Test Data Pattern
**Problem**: Tests need unique data to avoid conflicts across test runs
**Solution**: Use GUIDs and timestamps for test data uniqueness
**Pattern**:

```csharp
// Integration tests
var uniqueId = Guid.NewGuid().ToString("N")[..8];
var testUser = new WitchCityRopeUser(
    sceneName: SceneName.Create($"TestUser_{uniqueId}"),
    email: EmailAddress.Create($"test_{uniqueId}@example.com"),
    dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Always UTC
);

// E2E tests  
const uniqueId = Date.now().toString();
await searchInput.fill(`TestUser_${uniqueId}`);
```

**Benefits**: Prevents test data conflicts, allows parallel test execution

### Admin Authorization Testing Pattern
**Problem**: Need to verify admin-only endpoints reject non-admin users
**Solution**: Test both authorized and unauthorized access scenarios
**Pattern**:

```csharp
[Fact]
public async Task GetUsers_WithoutAdminAuth_ReturnsUnauthorized()
{
    // No authentication setup
    var response = await _client.GetAsync("api/admin/users");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact] 
public async Task GetUsers_WithAdminAuth_ReturnsOk()
{
    await _client.AuthenticateAsAdminAsync(_factory);
    var response = await _client.GetAsync("api/admin/users");  
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**Benefits**: Ensures security boundaries are properly enforced

## Useful Commands

```bash
# Run specific test file
npm run test:e2e -- tests/auth/login.spec.ts

# Run tests with UI (debugging)
npm run test:e2e -- --ui

# Update baseline screenshots
npm run test:e2e -- --update-snapshots

# Run integration tests with health check first
dotnet test --filter "Category=HealthCheck"
dotnet test
```

---

*Remember: Good tests are deterministic, isolated, and fast. If a test is flaky, fix it immediately.*