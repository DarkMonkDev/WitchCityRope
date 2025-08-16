# Testing Lessons Learned - React WitchCityRope
<!-- Last Updated: 2025-08-16 -->
<!-- Next Review: 2025-09-16 -->

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

## React E2E Testing with Playwright

### Test Structure for React Application

```
/tests/playwright/
├── specs/                    # Test files (.spec.ts)
│   ├── auth/                # Authentication tests
│   ├── events/              # Event management tests  
│   ├── admin/               # Admin functionality tests
│   ├── rsvp/                # RSVP tests
│   └── validation/          # Form validation tests
├── pages/                   # Page Object Models
│   ├── login.page.ts
│   ├── register.page.ts
│   ├── admin-dashboard.page.ts
│   └── event.page.ts
├── helpers/                 # Utility functions
│   └── auth.helpers.ts
└── playwright.config.ts     # Playwright configuration
```

### Running Tests

```bash
# Run all E2E tests
npm run test:e2e:playwright

# Run specific test category
npx playwright test --grep "authentication"
npx playwright test --grep "admin"
npx playwright test --grep "events"

# Run specific test file
npx playwright test auth/login-basic.spec.ts

# Run in UI mode for debugging
npx playwright test --ui

# Run with specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox

# Generate test reports
npx playwright show-report
```

## Test Data Management

**Issue**: Tests failing due to duplicate user data  
**Solution**: Always create unique test data using timestamps or GUIDs
```javascript
const uniqueEmail = `test-${Date.now()}@example.com`;
const uniqueUsername = `user-${Date.now()}`;
```
**Applies to**: All E2E tests creating users or events

## Selector Strategy for React Components

**Issue**: CSS selectors breaking when UI changes  
**Solution**: Use data-testid attributes on React components
```javascript
// ❌ WRONG
await page.click('.btn-primary');

// ✅ CORRECT for React
await page.click('[data-testid="submit-button"]');
```
**React Best Practice**: Add data-testid to all interactive components
```tsx
// In React components
<button data-testid="submit-button" onClick={handleSubmit}>
  Submit
</button>
```

## Wait Strategies for React Applications

**Issue**: Tests failing due to timing issues with React state updates  
**Solution**: Use proper wait conditions for React rendering
```javascript
// ❌ WRONG - Fixed wait
await page.waitForTimeout(2000);

// ✅ CORRECT - Wait for React component to render
await page.waitForSelector('[data-testid="events-list"]');
await expect(page.locator('[data-testid="event-card"]')).toHaveCount(5);

// ✅ React-specific - Wait for state updates
await page.waitForFunction(() => {
  return document.querySelector('[data-testid="loading"]') === null;
});
```
**Applies to**: After navigation, form submission, API calls, or React state changes

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

## React-Specific Testing Patterns

### Component Testing with React Testing Library

**Use React Testing Library for unit testing React components**:
```javascript
import { render, screen, fireEvent } from '@testing-library/react';
import { LoginForm } from '../components/LoginForm';

test('should display validation error for empty email', () => {
  render(<LoginForm />);
  
  const submitButton = screen.getByRole('button', { name: /submit/i });
  fireEvent.click(submitButton);
  
  expect(screen.getByText(/email is required/i)).toBeInTheDocument();
});
```

### Testing React Hooks

**Test custom hooks with renderHook**:
```javascript
import { renderHook, act } from '@testing-library/react';
import { useAuthState } from '../hooks/useAuthState';

test('should update auth state on login', () => {
  const { result } = renderHook(() => useAuthState());
  
  act(() => {
    result.current.login({ email: 'test@example.com', token: 'abc123' });
  });
  
  expect(result.current.isAuthenticated).toBe(true);
  expect(result.current.user.email).toBe('test@example.com');
});
```

### Testing API Integration

**Test React components with API calls**:
```javascript
import { render, screen, waitFor } from '@testing-library/react';
import { rest } from 'msw';
import { setupServer } from 'msw/node';
import { EventsList } from '../components/EventsList';

const server = setupServer(
  rest.get('/api/events', (req, res, ctx) => {
    return res(ctx.json([
      { id: '1', title: 'Test Event', date: '2025-08-16' }
    ]));
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

test('should load and display events', async () => {
  render(<EventsList />);
  
  await waitFor(() => {
    expect(screen.getByText('Test Event')).toBeInTheDocument();
  });
});
```

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
```javascript
// ❌ WRONG
const promise = api.createEvent(model);
expect(await api.getEvent(id)).toBeTruthy();

// ✅ CORRECT
await api.createEvent(model);
expect(await api.getEvent(id)).toBeTruthy();
```

### Test Naming
**Issue**: Unclear what test is testing  
**Solution**: Use Given_When_Then pattern
```javascript
// ❌ WRONG
test('Test1', () => {});

// ✅ CORRECT
test('Given_ValidEvent_When_Created_Then_ReturnsSuccessStatus', () => {});
```

## Recent Fixes (August 2025)

### Login Selector Fixes - August 13, 2025

**Problem**: E2E tests failing with "Element not found" for login form selectors
**Root Cause**: Tests using generic selectors instead of React application-specific selectors
**Solution**: Use specific selectors that match React form implementations

```javascript
// ❌ WRONG - Generic selectors
await page.fill('input[type="email"]', 'admin@witchcityrope.com');
await page.fill('input[type="password"]', 'Test123!');

// ✅ CORRECT - React application selectors
await page.fill('input[name="email"]', 'admin@witchcityrope.com');
await page.fill('input[name="password"]', 'Test123!');

// ✅ ALTERNATIVE - data-testid (preferred for React)
await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
await page.fill('[data-testid="password-input"]', 'Test123!');
```

**Best Practice**: Use the Page Object Model (`login.page.ts`) or auth helpers (`auth.helpers.ts`) which already have correct selectors.

### React Component E2E Helper Timeout Issue - FIXED

**Problem**: Global setup timing out when authenticating users in React application
**Root Cause**: Multiple timeout issues in Playwright configuration for SPA navigation
**Solution**: 
1. Fixed function call error in global-setup.ts
2. Increased timeouts for React component loading and API calls
3. Updated test config timeouts for Docker environments with React dev server

**Files Changed**:
- `/tests/playwright/helpers/global-setup.ts` - Fixed function call
- `/tests/playwright/helpers/test.config.ts` - Increased timeouts to 60s
- Playwright config updated for React dev server startup time

**Result**: Global setup now completes successfully for all test users

### Simplified React Tests - 2025-08-13
**Problem**: Complex timeout configurations when testing React application functionality  
**Root Cause**: Over-engineered waiting for React component rendering and state updates
**Solution**: Create simplified tests using basic Playwright waits without complex React-specific helpers

```javascript
// ❌ AVOID - Complex React state waiting
await page.waitForFunction(() => window.React && !window.ReactDOM.unstable_batchedUpdates);

// ✅ SIMPLE - Basic Playwright waits for React components
await page.waitForLoadState('networkidle');
await expect(page.locator('h1, h2, h3').filter({ hasText: /User Management/i })).toBeVisible({ timeout: 15000 });
```

**Test Pattern**: `/tests/playwright/admin/admin-user-management-simple.spec.ts`
- Admin login with known working selectors
- Standard Playwright navigation and waits
- Element verification without complex React state waiting
- Basic interaction testing for core functionality

**When to Use**: For core functionality tests where React-specific helpers are problematic

## Admin User Management Testing Patterns - 2025-08-13

### Comprehensive Test Coverage Pattern
**Problem**: Need to test admin functionality across all layers (unit, integration, E2E)
**Solution**: Create focused test suite covering each layer with specific scenarios

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

// E2E Tests - Browser automation with real React UI
test('should login as admin and verify real users load from API', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-button"]');
    await page.goto('/admin/users');
    await expect(page.locator('[data-testid="user-grid"]')).toBeVisible();
});
```

**Benefits**: 
- Each layer tests different concerns (logic, HTTP, UI)
- Catches issues at appropriate abstraction level
- Provides confidence in complete feature functionality

### E2E Real Data Verification Pattern  
**Problem**: E2E tests need to verify real API data loads (not mock data)
**Solution**: Test for presence of real data indicators

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

## Deprecated Testing Approaches

1. **Puppeteer** - All E2E tests migrated to Playwright
2. **In-memory database** - Use PostgreSQL TestContainers
3. **Fixed test data** - Always use unique identifiers
4. **Thread.Sleep/waitForTimeout** - Use proper wait conditions
5. **Blazor-specific testing patterns** - Replaced with React testing patterns

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

# React-specific: Run component tests
npm test

# React-specific: Run component tests in watch mode
npm test -- --watch

# React-specific: Generate coverage report
npm test -- --coverage
```

---

### Legacy Blazor Testing Patterns (Consolidated) - 2025-08-16

**Context**: Previous testing patterns from Blazor Server application now consolidated into React equivalents. Historical lessons from Blazor circuit testing and ASP.NET Core Identity integration preserved for reference.

**What We Learned**: 
- Blazor Server circuit errors required different debugging approaches than React
- ASP.NET Core Identity form selectors were specific to server-side rendering
- Blazor E2E helpers had timeout issues that don't apply to React SPAs
- PostgreSQL health checks are still critical regardless of frontend framework
- Test isolation patterns remain important across both Blazor and React

**Consolidated Lessons from Blazor Era**:
- **Health Checks**: Always run database health checks before integration tests
- **Test Data Uniqueness**: Use GUIDs and timestamps to prevent test conflicts
- **DateTime Handling**: UTC timestamps required for PostgreSQL (applies to both frontend frameworks)
- **Entity ID Initialization**: Always initialize entity IDs to prevent duplicate key violations
- **Authentication Testing**: Mock authentication consistently across test layers

**Action Items**: 
- [x] Migrated from Puppeteer to Playwright for E2E testing
- [x] Replaced Blazor-specific selectors with React data-testid patterns
- [x] Simplified timeout handling from Blazor circuit waits to standard React waits
- [x] Maintained PostgreSQL TestContainers pattern for database integration
- [x] Preserved multi-layer testing approach (unit, integration, E2E)

**Impact**: 
- Maintained testing reliability during React migration
- Preserved critical database and authentication testing patterns
- Eliminated Blazor Server-specific timeout and circuit issues
- Improved test execution speed with React's faster development cycle

**Migration Notes**: Content from `test-writers.md` consolidated here with React equivalents. Blazor-specific patterns archived but core testing principles preserved.

**References**:
- Legacy testing documentation in archive
- React Testing Library migration guide
- Playwright Blazor to React selector guide

**Tags**: #migration #blazor-legacy #consolidation #test-patterns #react-testing

---

*Remember: Good tests are deterministic, isolated, and fast. If a test is flaky, fix it immediately. With React applications, pay special attention to async state updates and component rendering timing.*