# Lessons Learned - Test Writers
<!-- Last Updated: 2025-08-04 -->
<!-- Next Review: 2025-09-04 -->

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

### PostgreSQL with TestContainers
**Issue**: "relation already exists" errors  
**Solution**: Use shared fixture pattern
```csharp
public class MyTests : IClassFixture<PostgreSqlFixture>
{
    // Tests share the same container instance
}
```
**Applies to**: All integration tests using database

### DateTime Handling
**Issue**: PostgreSQL requires UTC timestamps  
**Solution**: Always use UTC for dates
```csharp
// ❌ WRONG
var event = new Event { StartTime = DateTime.Now };

// ✅ CORRECT
var event = new Event { StartTime = DateTime.UtcNow };
```
**Applies to**: Any entity with DateTime properties

### Test Isolation
**Issue**: Tests affecting each other's data  
**Solution**: Use unique identifiers for all test data
```csharp
var sceneName = $"Test Scene {Guid.NewGuid()}";
var email = $"test-{Guid.NewGuid()}@example.com";
```
**Applies to**: All test data creation

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