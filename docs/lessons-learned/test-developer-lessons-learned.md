# Test Developer Lessons Learned
<!-- Last Updated: 2025-08-19 -->
<!-- Next Review: 2025-09-19 -->

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
**Solution**: Always use UTC for dates (Applies to .NET API tests only)
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

### Entity ID Initialization (.NET API only)

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

## React Testing Patterns - MANDATORY

### Testing Framework Stack (CRITICAL)
**NEVER use Jest for React tests - Use Vitest only**
- ✅ **Vitest**: Primary testing framework for React components and hooks
- ✅ **React Testing Library**: Component testing with user-centric approach
- ✅ **MSW (Mock Service Worker)**: API mocking for integration tests
- ✅ **Playwright**: E2E testing (NOT Puppeteer)
- ❌ **Jest**: Avoid - project uses Vitest for consistency

### TanStack Query Hook Testing
**Pattern**: Use renderHook with QueryClientProvider wrapper
```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useLogin } from '../mutations'

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('useLogin', () => {
  it('should login successfully', async () => {
    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper()
    })
    
    act(() => {
      result.current.mutate({
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      })
    })
    
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
  })
})
```

### Zustand Store Testing
**Pattern**: Direct store testing without complex setup
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

### React Router v7 Testing
**Pattern**: Use createMemoryRouter for testing navigation
```typescript
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { render, screen } from '@testing-library/react'

const renderWithRouter = (routes, initialEntries = ['/']) => {
  const router = createMemoryRouter(routes, { initialEntries })
  return {
    ...render(<RouterProvider router={router} />),
    router
  }
}

describe('Auth Flow', () => {
  it('should redirect to login when unauthenticated', () => {
    const { router } = renderWithRouter(routes, ['/dashboard'])
    
    expect(router.state.location.pathname).toBe('/login')
  })
})
```

### Mantine Component Testing
**Pattern**: Use userEvent for realistic interactions
```typescript
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MantineProvider } from '@mantine/core'

const renderWithMantine = (component) => {
  return render(
    <MantineProvider>{component}</MantineProvider>
  )
}

describe('LoginForm', () => {
  it('should handle form submission', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    
    renderWithMantine(<LoginForm onSubmit={onSubmit} />)
    
    await user.type(screen.getByLabelText(/email/i), 'test@example.com')
    await user.type(screen.getByLabelText(/password/i), 'password123')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    
    expect(onSubmit).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123'
    })
  })
})
```

### MSW API Mocking Issues (CRITICAL - August 2025)
**Problem**: MSW handlers with relative paths don't match axios requests with baseURL
**Root Cause**: API client uses `baseURL: 'http://localhost:5653'` but MSW handlers use relative paths like `/api/auth/login`
**Solution**: Use full URLs in MSW handlers
```typescript
// ❌ WRONG - Relative paths don't work with axios baseURL
http.post('/api/auth/login', handler)

// ✅ CORRECT - Use full URLs matching axios baseURL + path
http.post('http://localhost:5653/api/auth/login', handler)
```

**Additional Requirements**:
- Mock global fetch for auth store calls: `global.fetch = vi.fn()`
- Add refresh endpoint handler: `http.post('http://localhost:5653/auth/refresh', ...)`
- Match User interface structure exactly (roles array, not role string)

### TanStack Query Hook Testing Timing
**Problem**: Checking `isPending` immediately after `mutate()` returns false
**Root Cause**: Mutation state updates are asynchronous, even with `act()`
**Solution**: Use waitFor with flexible expectations
```typescript
// ❌ WRONG - Immediate check fails
act(() => { result.current.mutate(data) })
expect(result.current.isPending).toBe(true) // May be false

// ✅ CORRECT - Wait for either pending or completion
await waitFor(() => {
  expect(result.current.isPending || result.current.isSuccess).toBe(true)
})
```

### Reference Documentation
**MANDATORY**: Follow patterns in `/docs/functional-areas/authentication/testing/comprehensive-testing-plan.md`
- Testing pyramid strategy (Unit → Integration → E2E)
- Mock data management with MSW
- CI/CD integration patterns
- Coverage requirements

## .NET API Unit Testing (Legacy Patterns)

### Mocking Authentication (.NET API only)
**Issue**: Complex authentication setup in API tests  
**Solution**: Use simplified test authentication
```csharp
var authService = new Mock<IAuthService>();
authService.Setup(x => x.GetCurrentUserAsync())
    .ReturnsAsync(new UserDto { Id = testUserId });
```
**Applies to**: .NET API tests requiring authenticated context

### Testing Validation (.NET API only)
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
**Applies to**: All .NET API validation testing

## Common Pitfalls

### React Testing Common Mistakes
**Issue**: Testing implementation details instead of user behavior
**Solution**: Test what users see and do
```typescript
// ❌ WRONG - Testing implementation
expect(component.state.isLoading).toBe(true)

// ✅ CORRECT - Testing user experience
expect(screen.getByText('Loading...')).toBeInTheDocument()
```

### Async Testing in React
**Issue**: Tests completing before async operations
**Solution**: Always wait for async updates
```typescript
// ❌ WRONG - No waiting
user.click(submitButton)
expect(onSubmit).toHaveBeenCalled()

// ✅ CORRECT - Wait for async operation
await user.click(submitButton)
await waitFor(() => {
  expect(onSubmit).toHaveBeenCalled()
})
```

### Docker Environment (.NET API tests)
**Issue**: Tests pass locally but fail in CI  
**Solution**: Always test with Docker environment
```bash
docker-compose -f docker-compose.yml -f docker-compose.ci.yml up -d
npm run test:e2e
```

### Async/Await (.NET API tests)
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
**Solution**: Use descriptive names for React tests, Given_When_Then for .NET
```typescript
// ✅ CORRECT - React test naming
describe('LoginForm', () => {
  it('should display error when email is invalid', () => { })
  it('should call onSubmit when form is valid', () => { })
})
```

```csharp
// ✅ CORRECT - .NET test naming
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

## Recent Additions (August 2025)

### React Integration Testing Patterns - 2025-08-19
**Problem**: Need to test multiple React components working together (form → API → store → navigation)
**Solution**: Created integration test pattern for complete auth flow testing

**Integration Test File**: `/apps/web/src/test/integration/auth-flow-simplified.test.tsx`

**Key Integration Testing Patterns**:
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

**Critical Setup Requirements**:
```typescript
// 1. Mock React Router hooks to avoid context issues
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
  useLocation: () => ({ search: '', pathname: '/login' })
}))

// 2. Use renderHook instead of render for hook-focused integration tests
import { renderHook } from '@testing-library/react'

// 3. Test complete flow with waitFor for async operations
await waitFor(() => {
  // Verify all integration points completed
})
```

**Integration Points Successfully Tested**:
- ✅ TanStack Query mutations triggering Zustand store updates
- ✅ Zustand store permission calculations from user roles
- ✅ React Router navigation triggered by successful auth
- ✅ Query cache invalidation and clearing on auth state changes
- ✅ Error handling across the complete flow (API → Store → UI)
- ✅ Session state persistence and cleanup patterns

**Testing Strategy Lessons**:
- **Use renderHook for hook integration tests** - avoids React Router context issues
- **Mock router hooks individually** - more reliable than mocking entire router provider
- **Test behavior, not implementation** - verify end-to-end flow outcomes
- **Separate concerns** - integration tests focus on component interactions, not UI rendering

**Benefits**: Validates complete React architecture without complex component rendering issues

### Form Design Showcase Content Verification Test - 2025-08-18
**New Test**: `/tests/e2e/form-designs-check.spec.ts`
**Purpose**: Verify form design showcase pages actually display content (not just return HTTP 200)
**Problem**: User reported form design pages at /form-designs/* return 200 but don't load content
**Solution**: Created comprehensive verification test with detailed content inspection

**Key Findings from Test**:
- ✅ All pages return HTTP 200 OK (routes exist)
- ❌ Body text is completely empty on all pages
- ❌ No form elements, buttons, or interactive content rendered
- ❌ React root exists but no content is being rendered
- ✅ No console errors or network failures
- **Root Cause**: Form design components likely not implemented or not being loaded by React router

**Test Features**:
- Comprehensive content inspection with screenshots
- Console and network error monitoring
- Cross-browser and mobile testing
- Detailed HTML structure analysis
- Form element counting and verification

**Diagnostic Value**: 
- Proves the issue is React component rendering, not HTTP routing
- Identifies that this requires React developer intervention to implement actual components
- Provides visual evidence through screenshots for developers

### Form Components E2E Test - 2025-08-18
**New Test**: `/tests/e2e/form-components.spec.ts`
**Purpose**: Comprehensive testing of Mantine v7 form components test page
**Problem**: User reported form-test page not loading properly, needed verification test
**Solution**: Created comprehensive Playwright test with 12 test cases covering all functionality

**Key Patterns Applied**:
- ✅ Used data-testid attributes for stable element selection
- ✅ Comprehensive error capture (console errors, network failures)
- ✅ Screenshots at each test step for debugging
- ✅ Extended timeouts for page load (30s) and network idle waits
- ✅ Cross-browser and mobile viewport testing
- ✅ Proper async validation testing patterns

**Testing Methodology**:
```typescript
// Error capture pattern
page.on('console', msg => {
  if (msg.type() === 'error') {
    consoleErrors.push(msg.text());
  }
});

// Network monitoring pattern
page.on('requestfailed', request => {
  networkErrors.push(`${request.method()} ${request.url()} - ${request.failure()?.errorText}`);
});

// Screenshot for debugging
await page.screenshot({ path: 'test-results/debug-screenshot.png', fullPage: true });
```

**Benefits**: Provides comprehensive verification of form functionality and debugging information for troubleshooting

### Form Design Showcase CRITICAL Diagnosis - 2025-08-18
**Follow-up Investigation**: Created debug test `/tests/e2e/debug-form-design.spec.ts` to diagnose rendering issues
**CRITICAL DISCOVERY**: Issue is NOT missing components - **React application-wide rendering failure**

**Debug Test Findings**:
- ✅ All HTTP responses return 200 OK (routing works)
- ✅ All React modules load successfully (no network errors)
- ✅ React framework is detected and available
- ❌ **React root element is completely empty on ALL pages**
- ❌ **Homepage also has no content (not just form design pages)**
- ❌ **NO content renders despite successful component loading**

**Root Cause**: **FUNDAMENTAL React mounting/rendering failure affecting entire application**
- Components exist and are imported correctly
- React Router configuration loads
- BUT React is not rendering ANY content in the DOM

**Immediate Action Required**: React developer must investigate:
1. **main.tsx** - Check React root mounting
2. **App.tsx** - Check main App component rendering
3. **React Router** - Check route configuration and component mounting
4. **Browser console** - Manual check for client-side errors
5. **Minimal component test** - Isolate rendering issue

**Testing Value**: Successfully identified that this is a critical application-wide issue, not a feature-specific problem

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
# React Testing Commands
npm test                    # Run Vitest unit tests
npm run test:unit          # Run unit tests only
npm run test:integration   # Run integration tests
npm run test:coverage      # Generate coverage report

# E2E Testing Commands
npm run test:e2e           # Run Playwright E2E tests
npm run test:e2e -- --ui   # Run with UI (debugging)
npm run test:e2e -- tests/auth/login.spec.ts  # Specific test file
npm run test:e2e -- --update-snapshots        # Update screenshots

# .NET API Testing Commands
dotnet test --filter "Category=HealthCheck"  # Health checks first
dotnet test tests/WitchCityRope.Core.Tests/  # API unit tests
dotnet test tests/WitchCityRope.IntegrationTests/  # API integration tests
```

---

*Remember: Good tests are deterministic, isolated, and fast. If a test is flaky, fix it immediately.*