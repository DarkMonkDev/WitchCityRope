# Lessons Learned - Test Writers
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

## Deprecated Testing Approaches

1. **Puppeteer** - All E2E tests migrated to Playwright
2. **In-memory database** - Use PostgreSQL TestContainers
3. **Fixed test data** - Always use unique identifiers
4. **Thread.Sleep/waitForTimeout** - Use proper wait conditions

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