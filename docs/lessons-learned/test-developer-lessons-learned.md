# Test Developer Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
2. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
3. **Architecture Discovery Process**: `/docs/standards-processes/architecture-discovery-process.md`
4. **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Check if solution already exists
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)

### Test Developer Specific Rules:
- **Use generated types from @witchcityrope/shared-types for all test data**
- **Test NSwag-generated interfaces match API responses exactly**
- **Validate null handling for all generated DTO properties**
- **Create contract tests that verify DTO alignment automatically**

---

## 🚨 CRITICAL: DTO Test Data Alignment (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Test Data Integrity
**Severity**: Critical

### Context
Test data must match actual API responses exactly to prevent false positives and ensure integration tests reflect production behavior.

### What We Learned
- **Use Real API Responses**: Test data must match actual API DTOs, not idealized mock data
- **Validate DTO/Interface Alignment**: Integration tests should verify TypeScript interfaces match API responses
- **Test Null Handling**: Verify frontend gracefully handles null/undefined values from API
- **Contract Testing**: Implement consumer-driven contract tests to catch DTO changes
- **Cross-Browser Data Validation**: Ensure data handling works across different browsers

### Action Items
- [ ] READ: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` before creating test data
- [ ] USE: Actual API responses for mock data, not idealized structures
- [ ] IMPLEMENT: Contract tests that validate DTO/interface alignment
- [ ] TEST: Null/undefined handling in all scenarios
- [ ] VERIFY: Test data matches production API responses exactly

### Critical Test Patterns
```typescript
// ✅ CORRECT - Real API response structure
const mockUser: User = {
  sceneName: "TestUser123",
  createdAt: "2023-08-19T10:30:00Z",
  lastLoginAt: "2023-08-19T08:15:00Z",
  roles: ["general"],
  isVetted: false,
  membershipLevel: "general"
};

// ❌ WRONG - Idealized mock data
const mockUser = {
  firstName: "John",     // API doesn't return this
  lastName: "Doe",      // API doesn't return this
  email: "john@test.com" // API doesn't return this
};
```

### Tags
#critical #test-data #dto-alignment #contract-testing #integration-tests

---

# Test Developer Lessons Learned
<!-- Last Updated: 2025-08-19 -->
<!-- Next Review: 2025-09-19 -->

**Purpose**: Actionable testing lessons specific to test development role only.
**Scope**: Process documentation belongs in `/docs/standards-processes/testing/` guides.
**Format**: All lessons follow the template format for consistency.

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

### E2E Test Data Uniqueness
**Date**: 2025-08-15
**Category**: E2E
**Severity**: Critical

### Context
Tests were failing due to duplicate user data between test runs.

### What We Learned
Always create unique test data using timestamps or GUIDs.

### Action Items
```javascript
const uniqueEmail = `test-${Date.now()}@example.com`;
const uniqueUsername = `user-${Date.now()}`;
```

### Impact
Eliminated data conflicts in parallel test execution.

### Tags
`e2e` `test-data` `uniqueness` `playwright`

### Playwright Selector Strategy
**Date**: 2025-08-10
**Category**: E2E
**Severity**: High

### Context
CSS selectors were breaking when UI changed during development.

### What We Learned
Use data-testid attributes for stable element selection.

### Action Items
```javascript
// ❌ WRONG
await page.click('.btn-primary');

// ✅ CORRECT
await page.click('[data-testid="submit-button"]');
```

### Impact
Reduced E2E test maintenance by 60%.

### Tags
`playwright` `selectors` `data-testid` `maintenance`

### Playwright Wait Strategies
**Date**: 2025-08-05
**Category**: E2E
**Severity**: High

### Context
Tests were failing due to timing issues with dynamic content.

### What We Learned
Use proper wait conditions instead of fixed timeouts.

### Action Items
```javascript
// ❌ WRONG - Fixed wait
await page.waitForTimeout(2000);

// ✅ CORRECT - Wait for specific condition
await page.waitForSelector('[data-testid="events-list"]');
await expect(page.locator('[data-testid="event-card"]')).toHaveCount(5);
```

### Impact
Eliminated timing-related flaky tests.

### Tags
`playwright` `timing` `wait-strategies` `reliability`

## Integration Testing

### 🚨 CRITICAL: Real PostgreSQL Required

**Critical**: Integration tests now use real PostgreSQL with comprehensive health checks. The in-memory database was hiding bugs.

### Health Check Process
**Date**: 2025-07-15
**Category**: Integration
**Severity**: Critical

### Context
Integration tests were failing inconsistently due to database container startup timing.

### What We Learned
Must always run health checks before integration tests to ensure containers are ready.

### Action Items
- Run health checks first: `dotnet test --filter "Category=HealthCheck"`
- See `/docs/standards-processes/testing/integration-test-patterns.md` for complete process

### Impact
Reduced integration test failures by 90%.

### Tags
`postgresql` `testcontainers` `integration-testing`

### PostgreSQL TestContainers Pattern
**Date**: 2025-07-20
**Category**: Integration
**Severity**: High

### Context
"Relation already exists" errors were occurring in integration tests.

### What We Learned
Use shared fixture pattern to prevent database conflicts.

### Action Items
```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyTests : IClassFixture<PostgreSqlFixture>
{
    // Tests share the same container instance
}
```

### Impact
Eliminated database creation conflicts in integration tests.

### Tags
`postgresql` `testcontainers` `fixtures` `integration`

### PostgreSQL DateTime UTC Requirement
**Date**: 2025-07-25
**Category**: Integration
**Severity**: Critical

### Context
PostgreSQL integration tests failing with "Cannot write DateTime with Kind=Unspecified" errors.

### What We Learned
PostgreSQL requires UTC timestamps. Always use DateTimeKind.Utc.

### Action Items
```csharp
// ❌ WRONG
var event = new Event { StartTime = DateTime.Now };
new DateTime(1990, 1, 1) // Kind is Unspecified

// ✅ CORRECT
var event = new Event { StartTime = DateTime.UtcNow };
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### Impact
Eliminated all DateTime-related database errors.

### Tags
`postgresql` `datetime` `utc` `integration-testing`

### Integration Test Data Isolation
**Date**: 2025-07-30
**Category**: Integration
**Severity**: Critical

### Context
Tests were affecting each other's data causing duplicate key violations.

### What We Learned
Use unique identifiers for ALL test data to ensure isolation.

### Action Items
```csharp
// ❌ WRONG - Will cause conflicts
var sceneName = "TestUser";
var email = "test@example.com";

// ✅ CORRECT - Always unique
var sceneName = $"TestUser_{Guid.NewGuid():N}";
var email = $"test-{Guid.NewGuid():N}@example.com";
```

### Impact
Eliminated test conflicts and enabled parallel test execution.

### Tags
`test-isolation` `unique-data` `parallel-testing` `guid`

### Entity ID Initialization Requirement
**Date**: 2025-08-01
**Category**: Integration
**Severity**: Critical

### Context
Default Guid.Empty values were causing duplicate key violations in tests.

### What We Learned
Always initialize IDs in entity constructors.

### Action Items
```csharp
public Rsvp(Guid userId, Event @event)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this!
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

### Impact
Eliminated entity ID conflicts in integration tests.

### Tags
`entity-framework` `guid` `constructor` `primary-key`

## React Testing Patterns - MANDATORY

### React Testing Framework Stack
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Critical

### Context
Need consistent testing framework across React codebase.

### What We Learned
Use Vitest (not Jest) for React testing consistency.

### Action Items
- ✅ **Vitest**: Primary testing framework for React components and hooks
- ✅ **React Testing Library**: Component testing with user-centric approach
- ✅ **MSW (Mock Service Worker)**: API mocking for integration tests
- ✅ **Playwright**: E2E testing (NOT Puppeteer)
- ❌ **Jest**: Avoid - project uses Vitest for consistency

### Impact
Consistent testing experience across React components.

### Tags
`vitest` `react-testing-library` `msw` `testing-stack`

### TanStack Query Hook Testing
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Medium

### Context
Need pattern for testing TanStack Query hooks with proper provider setup.

### What We Learned
Use renderHook with QueryClientProvider wrapper for isolated hook testing.

### Action Items
```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}
```

### Impact
Enables isolated testing of TanStack Query hooks.

### Tags
`tanstack-query` `renderhook` `testing-pattern`

### Zustand Store Testing
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Medium

### Context
Need pattern for testing Zustand stores directly without React components.

### What We Learned
Test Zustand stores directly without complex setup.

### Action Items
```typescript
import { authStore } from '../authStore'

describe('authStore', () => {
  beforeEach(() => {
    authStore.getState().logout() // Reset state
  })
  
  it('should update state on login', () => {
    const user = { id: '1', email: 'test@example.com' }
    authStore.getState().login(user)
    
    expect(authStore.getState().user).toEqual(user)
    expect(authStore.getState().isAuthenticated).toBe(true)
  })
})
```

### Impact
Simplified store testing without React component overhead.

### Tags
`zustand` `store-testing` `direct-testing`

### React Router v7 Testing
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Medium

### Context
Need pattern for testing React Router v7 navigation in tests.

### What We Learned
Use createMemoryRouter for testing navigation flows.

### Action Items
```typescript
import { createMemoryRouter, RouterProvider } from 'react-router-dom'

const renderWithRouter = (routes, initialEntries = ['/']) => {
  const router = createMemoryRouter(routes, { initialEntries })
  return {
    ...render(<RouterProvider router={router} />),
    router
  }
}
```

### Impact
Enables testing of navigation flows in React Router v7.

### Tags
`react-router` `navigation-testing` `memory-router`

### Mantine Component Testing
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Medium

### Context
Need pattern for testing Mantine UI components with proper provider setup.

### What We Learned
Use userEvent for realistic interactions and MantineProvider for component testing.

### Action Items
```typescript
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MantineProvider } from '@mantine/core'

const renderWithMantine = (component) => {
  return render(
    <MantineProvider>{component}</MantineProvider>
  )
}
```

### Impact
Enables proper testing of Mantine UI components with user interactions.

### Tags
`mantine` `user-events` `ui-testing`

### MSW Axios BaseURL Compatibility
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Critical

### Context
MSW handlers with relative paths weren't matching axios requests using baseURL.

### What We Learned
MSW handlers must use full URLs when API client uses baseURL. Also need handlers for all potential ports and response interceptor endpoints.

### Action Items
```typescript
// ❌ WRONG - Relative paths don't work with axios baseURL
http.post('/api/auth/login', handler)

// ✅ CORRECT - Use full URLs matching axios baseURL + path
http.post('http://localhost:5651/api/auth/login', handler) // Main API port

// ✅ ALWAYS INCLUDE - Auth refresh interceptor handler
http.post('http://localhost:5651/auth/refresh', () => {
  return new HttpResponse('Unauthorized', { status: 401 })
})
```

### Impact
Fixed API mocking for React integration tests.

### Tags
`msw` `axios` `baseurl` `api-mocking`

### MSW Response Structure Alignment
**Date**: 2025-08-19
**Category**: Testing  
**Severity**: Critical

### Context
Tests were failing because MSW handlers returned different response structures than expected by mutations.

### What We Learned
MSW response structure must exactly match the actual API response structure.

### Action Items
```typescript
// ✅ CORRECT - Match exact API response structure
http.post('http://localhost:5651/api/auth/login', async ({ request }) => {
  const body = await request.json()
  if (body.email === 'admin@witchcityrope.com') {
    return HttpResponse.json({
      success: true,
      data: {  // User object directly, not nested
        id: '1',
        email: body.email,
        sceneName: 'TestAdmin',
        createdAt: '2025-08-19T00:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      },
      message: 'Login successful'
    })
  }
  return new HttpResponse(null, { status: 401 })
})
```

### Impact
Aligned test responses with actual API structure, enabling proper integration testing.

### Tags
`msw` `api-structure` `response-format`

### React Component Test Infinite Loop Prevention
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Critical

### Context
LoginPage was causing infinite loops in tests due to redundant navigation logic.

### What We Learned
Avoid duplicate navigation logic between components and hooks in React tests.

### Action Items
```typescript
// ❌ WRONG - Double navigation causes infinite loops
useEffect(() => {
  if (isAuthenticated) {
    window.location.href = returnTo  // In component
  }
}, [isAuthenticated])

// AND navigation in mutation onSuccess  // In hook

// ✅ CORRECT - Single navigation source
// Let mutation handle all navigation, remove component navigation
```

### Impact
Eliminated infinite re-rendering in React component tests.

### Tags
`react-testing` `infinite-loop` `navigation` `useeffect`

### TanStack Query Mutation Timing
**Date**: 2025-08-19
**Category**: Testing
**Severity**: High

### Context
Checking `isPending` immediately after `mutate()` was returning false unexpectedly.

### What We Learned
Mutation state updates are asynchronous, even with `act()`.

### Action Items
```typescript
// ❌ WRONG - Immediate check fails
act(() => { result.current.mutate(data) })
expect(result.current.isPending).toBe(true) // May be false

// ✅ CORRECT - Wait for either pending or completion
await waitFor(() => {
  expect(result.current.isPending || result.current.isSuccess).toBe(true)
})
```

### Impact
Eliminated false negatives in TanStack Query hook tests.

### Tags
`tanstack-query` `async-testing` `timing` `waitfor`

### Testing Documentation Reference
**Date**: 2025-08-19
**Category**: Testing
**Severity**: Medium

### Context
Need comprehensive testing patterns documented for consistency.

### What We Learned
Centralize testing patterns in dedicated documentation.

### Action Items
Follow patterns in `/docs/functional-areas/authentication/testing/comprehensive-testing-plan.md`:
- Testing pyramid strategy (Unit → Integration → E2E)
- Mock data management with MSW
- CI/CD integration patterns
- Coverage requirements

### Impact
Consistent testing approach across the project.

### Tags
`documentation` `testing-patterns` `standards`

## Legacy .NET Testing Reference

**Note**: Essential .NET API testing patterns preserved for reference.
**Location**: Complete patterns in `/docs/standards-processes/testing/TESTING_GUIDE.md`

### .NET Authentication Mocking
```csharp
// Simplified test authentication for .NET API tests
var authService = new Mock<IAuthService>();
authService.Setup(x => x.GetCurrentUserAsync())
    .ReturnsAsync(new UserDto { Id = testUserId });
```

### .NET Validation Testing
```csharp
// Always test edge cases with Theory tests
[Theory]
[InlineData("")]           // Empty
[InlineData(null)]         // Null  
[InlineData("a")]          // Too short
[InlineData("valid@email")] // Valid
public async Task Email_Validation_Works(string email) { }
```

## Common Pitfalls


### Docker Environment Testing
**Date**: 2025-08-15
**Category**: Integration
**Severity**: High

### Context
Tests were passing locally but failing in CI environments.

### What We Learned
Always test with Docker environment to match CI conditions.

### Action Items
See `/docs/guides-setup/docker-operations-guide.md` for complete Docker testing procedures.

### Impact
Eliminated CI/local environment discrepancies.

### Tags
`docker` `ci-cd` `environment-parity`



## Deprecated Testing Approaches

### Removed from React Testing
1. **bUnit** - Blazor component testing framework (NO LONGER APPLICABLE)
2. **xUnit** - Use Vitest for React tests (xUnit still valid for .NET API tests)
3. **Blazor components testing** - All components now React with RTL
4. **SignalR testing** - Not needed unless WebSocket features added
5. **Blazor validation testing** - Use React form validation patterns

### Still Deprecated (All Testing)
1. **Puppeteer** - All E2E tests migrated to Playwright
2. **Jest** - Use Vitest for consistency across React testing
3. **In-memory database** - Use PostgreSQL TestContainers for integration tests
4. **Fixed test data** - Always use unique identifiers
5. **Thread.Sleep/waitForTimeout** - Use proper wait conditions
6. **Generic selectors** - Use data-testid attributes for reliable element selection

### React Integration Testing Pattern
**Date**: 2025-08-19
**Category**: Integration
**Severity**: Critical

### Context
Need to test multiple React components working together (form → API → store → navigation).

### What We Learned
Use renderHook for testing hook integration without complex component rendering.

### Action Items
```typescript
// Test multiple hooks working together using renderHook
const { result: loginResult } = renderHook(() => useLogin(), { wrapper: createWrapper() })

// Trigger login and verify complete flow
act(() => {
  loginResult.current.mutate({ email: 'admin@witchcityrope.com', password: 'Test123!' })
})

// Verify TanStack Query → Zustand Store → React Router integration
await waitFor(() => {
  expect(loginResult.current.isSuccess).toBe(true)
  expect(useAuthStore.getState().isAuthenticated).toBe(true)
  expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
})
```

### Impact
Validates complete React architecture without complex component rendering issues.

### Tags
`react-integration` `renderhook` `tanstack-query` `zustand` `router`





## Quick Reference

**Testing Commands**: See `/docs/standards-processes/testing/TESTING_GUIDE.md` for complete command reference.

**Essential Commands**:
- `npm run test:e2e:playwright` - Run E2E tests
- `dotnet test --filter "Category=HealthCheck"` - Health checks first
- `npm test` - Run React unit tests

---

*Remember: Good tests are deterministic, isolated, and fast. If a test is flaky, fix it immediately.*