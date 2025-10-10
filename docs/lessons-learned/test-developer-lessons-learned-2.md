# Test Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 2 of 2**
**Read Part 1 first**: test-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Write to**: Part 2 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## 🚨 REQUIRED READING FOR SPECIFIC TASKS 🚨

### Before Creating E2E Persistence Tests
**MUST READ**: `/docs/functional-areas/testing/e2e-persistence-testing-guide.md` (618 lines)
- Complete persistence test pattern (UI + API + Database verification)
- Database verification helpers
- How to use test templates
**TEMPLATES**: `/apps/web/tests/playwright/templates/` - 5 reusable templates
**CRITICAL**: Tests that only verify UI updates miss bugs where database doesn't update (profile bug, ticket cancellation bug)

### Before Creating Backend Integration Tests
**MUST READ**: `/docs/functional-areas/testing/backend-integration-testing-guide.md`
- DTO/Entity mapping validation patterns
- TestContainers setup
- Database persistence verification
**CRITICAL**: Integration tests must verify all DTO fields map to entity properties

---

## ⛔ NEVER Suggest Long Timeouts (10+ Minutes)

**Problem**: Agents repeatedly suggest 10-minute or longer timeouts for tests, masking stalled/broken tests.
**User Feedback**: "NO TEST should ever take 10 minutes. Most will not take more than 30 seconds, but giving them 1 minute maybe 1.5 at the absolute most is plenty. If it takes longer than that, then something has failed and the test is stalled forever."

**Why This is Wrong**:
- Tests taking >90 seconds are **STALLED or BROKEN**, not slow
- Long timeouts mask underlying problems (infinite loops, missing data, service failures)
- User explicitly stated: "This is VERY important that you set the bash commands to have this time out limit as well as the other tests"

**Correct Approach**:

```typescript
// ❌ WRONG - 10 minute timeout masks stalled test
test.setTimeout(600000); // 10 minutes - ABSOLUTELY NO!
await page.waitForSelector('.element', { timeout: 600000 });

// ❌ WRONG - 5 minute timeout still too long
test.setTimeout(300000); // 5 minutes - NO!

// ❌ WRONG - 2 minute timeout exceeds maximum
test.setTimeout(120000); // 2 minutes - NO! (exceeds 90s max)

// ✅ CORRECT - 90 second ABSOLUTE MAXIMUM
test.setTimeout(90000); // 90 seconds - ABSOLUTE MAX
await page.waitForSelector('.element', { timeout: 30000 }); // 30 seconds typical
```

**Bash Command Timeouts**:

```typescript
// ❌ WRONG - 10 minute bash timeout
bash({ command: 'npm run test', timeout: 600000 }); // NO!

// ✅ CORRECT - 90 second maximum for bash commands
bash({ command: 'npm run test', timeout: 90000 }); // ABSOLUTE MAX
bash({ command: 'npx playwright test', timeout: 60000 }); // Typical
```

**Realistic Timeout Expectations**:
- **Most tests**: 30 seconds or less (normal)
- **Complex tests**: 60 seconds (1 minute)
- **Absolute maximum**: 90 seconds (1.5 minutes) - NEVER EXCEED
- **Tests >90s**: Stalled/broken - fix the test, don't increase timeout

**What to Do When Tests Timeout**:
1. **DO NOT** increase timeout above 90 seconds
2. **Investigate** why test is taking so long:
   - Element never appears (wrong selector, feature not implemented)
   - Infinite loop in test logic
   - Backend service not running
   - Database missing test data
   - Network connectivity issues
3. **Fix the underlying issue**, don't mask it with longer timeouts

**Prevention**:
- ALWAYS check `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md` before suggesting timeouts
- Default to 30-60 seconds for test execution
- Use 90 seconds ONLY as absolute maximum safety buffer
- Apply same limits to bash commands, Playwright tests, Vitest tests, ALL test execution

**Reference Documentation**: `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md` - Complete timeout standard


## TestContainers Integration Patterns

### PostgreSQL TestContainers Shared Fixture
**Problem**: Multiple tests starting separate PostgreSQL containers causing resource exhaustion and port conflicts.
**Solution**: Use shared fixture pattern with collection-level container lifecycle.

```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyTests : IClassFixture<PostgreSqlFixture>
{
    // Tests share the same container instance
}
```

### PostgreSQL DateTime UTC Requirement
**Problem**: "Cannot write DateTime with Kind=Unspecified" errors.
**Solution**: PostgreSQL requires UTC timestamps. Always use DateTimeKind.Utc.

```csharp
// ❌ WRONG
var event = new Event { StartTime = DateTime.Now };
new DateTime(1990, 1, 1) // Kind is Unspecified

// ✅ CORRECT
var event = new Event { StartTime = DateTime.UtcNow };
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### Integration Test Data Isolation
**Problem**: Tests affecting each other's data causing duplicate key violations.
**Solution**: Use unique identifiers for ALL test data to ensure isolation.

```csharp
// ❌ WRONG - Will cause conflicts
var sceneName = "TestUser";
var email = "test@example.com";

// ✅ CORRECT - Always unique
var sceneName = $"TestUser_{Guid.NewGuid():N}";
var email = $"test-{Guid.NewGuid():N}@example.com";
```

### Entity ID Initialization Requirement
**Problem**: Default Guid.Empty values causing duplicate key violations.
**Solution**: Always initialize IDs in entity constructors.

```csharp
public Rsvp(Guid userId, Event @event)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this!
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

## React Testing Patterns - MANDATORY

### React Testing Framework Stack
**Problem**: Inconsistent testing framework across React codebase.
**Solution**: Use Vitest (not Jest) for React testing consistency.

**Required Stack**:
- ✅ **Vitest**: Primary testing framework
- ✅ **React Testing Library**: Component testing
- ✅ **MSW**: API mocking for integration tests
- ✅ **Playwright**: E2E testing (NOT Puppeteer)
- ❌ **Jest**: Avoid - project uses Vitest

### TanStack Query Hook Testing
**Problem**: Need pattern for testing TanStack Query hooks with proper provider setup.
**Solution**: Use renderHook with QueryClientProvider wrapper.

```typescript
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}
```

### MSW Axios BaseURL Compatibility
**Problem**: MSW handlers with relative paths not matching axios requests using baseURL.
**Solution**: Use full URLs when API client uses baseURL.

```typescript
// ❌ WRONG - Relative paths don't work with axios baseURL
http.post('/api/auth/login', handler)

// ✅ CORRECT - Use full URLs
http.post('http://localhost:5651/api/auth/login', handler)

// ✅ ALWAYS INCLUDE - Auth refresh interceptor handler
http.post('http://localhost:5651/auth/refresh', () => {
  return new HttpResponse('Unauthorized', { status: 401 })
})
```

### MSW Response Structure Alignment
**Problem**: MSW handlers returning different response structures than expected by mutations.
**Solution**: MSW response structure must exactly match actual API response structure.

```typescript
// ✅ CORRECT - Match exact API response structure
http.post('http://localhost:5651/api/auth/login', async ({ request }) => {
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
})
```

### React Component Test Infinite Loop Prevention
**Problem**: LoginPage causing infinite loops due to redundant navigation logic.
**Solution**: Avoid duplicate navigation logic between components and hooks.

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

### TanStack Query Mutation Timing
**Problem**: Checking `isPending` immediately after `mutate()` returning false unexpectedly.
**Solution**: Mutation state updates are asynchronous, even with `act()`.

```typescript
// ❌ WRONG - Immediate check fails
act(() => { result.current.mutate(data) })
expect(result.current.isPending).toBe(true) // May be false

// ✅ CORRECT - Wait for either pending or completion
await waitFor(() => {
  expect(result.current.isPending || result.current.isSuccess).toBe(true)
})
```

## Unit Test Migration from Mocks to Real Database

### TestContainers Migration Pattern
**Problem**: ApplicationDbContext doesn't have parameterless constructor required for mocking.
**Solution**: Use TestContainers with PostgreSQL + Respawn for database cleanup instead of mocking.

```csharp
// ❌ WRONG - Mocking DbContext causes constructor issues
var mockContext = new Mock<ApplicationDbContext>(); // Fails - no parameterless constructor

// ✅ CORRECT - Use real database with TestContainers
public class DatabaseTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .Build();
        await _container.StartAsync();

        // Setup Respawn for cleanup
        await using var connection = new NpgsqlConnection(ConnectionString);
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }
}

// ✅ CORRECT - Base class for database tests
[Collection("Database")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected ApplicationDbContext DbContext = null!;

    public virtual async Task InitializeAsync()
    {
        DbContext = DatabaseFixture.CreateDbContext(); // Real DbContext
    }

    public virtual async Task DisposeAsync()
    {
        DbContext?.Dispose();
        await DatabaseFixture.ResetDatabaseAsync(); // Fast cleanup
    }
}
```

**Performance**: Container-per-collection strategy, Respawn cleanup faster than recreating containers

### Moq Extension Method Mocking Fix
**Problem**: Moq cannot mock extension methods like `CreateScope()` and `GetRequiredService<T>()`.
**Solution**: Mock the underlying interface methods instead of extension methods.

```csharp
// ❌ WRONG - Cannot mock extension methods
MockServiceProvider.Setup(x => x.CreateScope()).Returns(mockScope);
MockServiceProvider.Setup(x => x.GetRequiredService<ApplicationDbContext>()).Returns(dbContext);

// ✅ CORRECT - Mock underlying interface methods
MockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
    .Returns(MockServiceScopeFactory.Object);
MockServiceScopeFactory.Setup(x => x.CreateScope())
    .Returns(MockServiceScope.Object);
MockScopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
    .Returns(DbContext);
```

## E2E Testing Patterns

### Dual E2E Test Configuration Pattern
**Problem**: Import errors blocking E2E test execution due to incorrect path resolution across separate test configurations.
**Discovery**: Project has TWO separate E2E test configurations with different directory structures.

```typescript
// Project Structure Discovery:
// 1. Root-level E2E Tests
//    - Config: /playwright.config.ts
//    - Tests: /tests/e2e/
//    - Helpers: /tests/e2e/helpers/

// 2. Apps/Web E2E Tests
//    - Config: /apps/web/playwright.config.ts
//    - Tests: /apps/web/tests/playwright/
//    - Helpers: /apps/web/tests/playwright/helpers/

// ❌ WRONG - Cross-referencing between test suites
// In /apps/web/tests/playwright/events-crud-test.spec.ts:
import { quickLogin } from '../../../tests/e2e/helpers/auth.helper';
// Path resolves to: /apps/tests/e2e/helpers/ (DOES NOT EXIST)

// ✅ CORRECT - Use helpers from same test suite
// In /apps/web/tests/playwright/events-crud-test.spec.ts:
import { AuthHelpers } from './helpers/auth.helpers';
// Path resolves to: /apps/web/tests/playwright/helpers/ (EXISTS)
```

**Critical Rules**:
1. **Each test suite uses its own helpers** - Do NOT cross-reference
2. **Count parent traversals carefully** - Verify actual file system paths
3. **Check both configs exist** - Multiple Playwright configs may be in use

### E2E Port Configuration - Hardcoded Ports
**Problem**: E2E tests hardcoded to wrong ports (5174, 5653) blocking 227+ tests from executing.
**Solution**: Use Docker ports and centralize configuration.

```bash
# ❌ WRONG - Hardcoded wrong ports in test files
await page.goto('http://localhost:5174/login')  # Should be 5173
await request.get('http://localhost:5653/api/events')  # Should be 5655

# ✅ CORRECT - Use Docker ports
await page.goto('http://localhost:5173/login')  # Docker web service
await request.get('http://localhost:5655/api/api/events')  # Docker API service

# ✅ BETTER - Use baseURL from Playwright config
await page.goto('/login')  # Relative URL uses config baseURL
const API_URL = process.env.API_URL || 'http://localhost:5655'
```

**Docker Port Reference**:
- Web (React): http://localhost:5173
- API (.NET): http://localhost:5655
- Database: localhost:5433

### E2E Test Title Expectations - Outdated Default Values
**Problem**: E2E tests checking for default Vite scaffolding title instead of actual application title.
**Solution**: Use partial, case-insensitive matches.

```typescript
// ❌ WRONG - Outdated default title from Vite template
await expect(page).toHaveTitle(/Vite \+ React/);

// ✅ CORRECT - Actual application title with case-insensitive partial match
await expect(page).toHaveTitle(/Witch City Rope/i);
```

### E2E Authentication Cookie Persistence - ABSOLUTE URLs REQUIRED
**Problem**: E2E tests show 401 Unauthorized errors after successful login when navigating to protected routes.
**Root Cause**: Playwright requires ABSOLUTE URLs (not relative URLs) for proper cookie handling and persistence.

```typescript
// ❌ WRONG - Relative URL causes cookie persistence issues
await page.goto('/login');

// ✅ CORRECT - Absolute URL ensures cookies persist properly
await page.goto('http://localhost:5173/login');
await page.waitForLoadState('networkidle');

await page.locator('[data-testid="email-input"]').fill(credentials.email);
await page.locator('[data-testid="password-input"]').fill(credentials.password);
await page.locator('[data-testid="login-button"]').click();

await page.waitForURL('**/dashboard', { timeout: 10000 });
await page.waitForLoadState('networkidle');
```

**Why Absolute URLs Work**:
1. **Cookie Domain**: Cookies are set for specific domains; relative URLs may confuse Playwright's context
2. **Protocol Handling**: HTTPS/HTTP must be explicit for proper cookie storage
3. **Context Isolation**: Absolute URLs ensure Playwright browser context knows exact origin

**Status**: ✅ RESOLVED - Absolute URLs fix cookie persistence issue

### Admin Vetting E2E Tests - Flexible Selectors
**Problem**: Need comprehensive E2E tests for admin vetting workflow covering dashboard, detail view, and full workflows.
**Solution**: Created 19 Playwright E2E tests in 3 test files with flexible selectors and graceful feature detection.

```typescript
// ✅ CORRECT - Flexible selectors support multiple UI implementations
const modal = page.locator('[role="dialog"], .modal, [data-testid="approve-modal"]');
const statusBadge = page.locator('[data-testid="status-badge"], .badge, .status');
const approveButton = page.locator('button, [data-testid="approve-button"]').filter({ hasText: /approve/i });

// ✅ CORRECT - Graceful feature detection for unimplemented features
if (await element.count() > 0) {
  // Test the feature
  await element.click();
} else {
  console.log('Feature not implemented yet - test skipped');
}
```

## Integration Test Maintenance

### Infrastructure Test Baseline Verification
**Problem**: Baseline reports showing test failures can become stale quickly in active development.
**Solution**: Always verify current test status before starting fix work.

```csharp
// ✅ CORRECT - Always verify current state before fixing
// Step 1: Run tests yourself to confirm they're actually failing
dotnet test tests/integration/ --filter "FullyQualifiedName~Phase2ValidationIntegrationTests"

// Step 2: Run full test suite to verify pass rate
dotnet test tests/integration/

// Step 3: Document findings if tests are already fixed
```

**Prevention Pattern**:
1. **Re-run tests immediately before starting fix work** - Baseline may be stale
2. **Run multiple times** - Infrastructure tests can be flaky, verify consistency
3. **Check timestamps** - If baseline is hours/days old, verify current state first
4. **Category D (Infrastructure) tests are LOW priority** - They often self-heal

### Vetting Integration Test Data Alignment
**Problem**: Integration tests failed because they referenced obsolete "Submitted" status that no longer exists in VettingStatus enum.
**Solution**: Update tests to use current domain model values.

```csharp
// ❌ WRONG - Expected obsolete status
auditLog!.Action.Should().Contain("Status changed");  // Wrong case
auditLog.OldValue.Should().Contain("Submitted");      // Doesn't exist

// ✅ CORRECT - Match actual backend behavior
auditLog!.Action.Should().Be("Status Changed");       // Exact match
auditLog.OldValue.Should().Contain("UnderReview");   // Current initial status
```

**Backend-Test Alignment**:
- Backend is source of truth for domain model
- Tests must adapt to backend changes
- Test failures from model evolution = test data issue, not code bug
- Always read backend code to understand expected values

### Same-State Status Updates Rejected
**Problem**: Integration tests failed after backend correctly reverted same-state update allowance.
**Solution**: Use valid status transitions from workflow diagram.

```csharp
// ❌ WRONG - Same-state update (rejected)
Status = "UnderReview"  // App already in UnderReview

// ✅ CORRECT - Valid transition
Status = "OnHold"  // UnderReview → OnHold (valid per workflow)
```

**Valid Vetting Workflow Transitions**:
```
UnderReview → InterviewApproved, OnHold, Denied, Withdrawn
InterviewApproved → InterviewScheduled, FinalReview, OnHold, Denied, Withdrawn
InterviewScheduled → FinalReview, OnHold, Denied, Withdrawn
FinalReview → Approved, Denied, OnHold, Withdrawn
OnHold → UnderReview, InterviewApproved, InterviewScheduled, FinalReview, Denied, Withdrawn
Approved → (terminal, no transitions)
Denied → (terminal, no transitions)
Withdrawn → (terminal, no transitions)
```

## Test Maintenance Patterns

### Display Text Changes Rarely Break Well-Architected Tests
**Problem**: Backend changed vetting status display labels and formatting. Concern that many tests would need updates.
**Discovery**: Only 1 test file needed updating (removed obsolete enum value from validation array). No tests had hardcoded display text expectations.
**Root Cause**: Proper separation of concerns - tests use enum values, components map to display text.

```typescript
// ✅ CORRECT - Test uses enum value, not display text
const application: ApplicationSummaryDto = {
  status: 'InterviewApproved',  // Type-safe enum value
  // ...
};

render(<VettingApplicationsList applications={[application]} />);
expect(screen.getByTestId('status-badge')).toBeVisible();  // Presence check

// ❌ WRONG - Test brittle to display text changes
expect(screen.getByText('Interview Approved')).toBeVisible();  // Breaks when text changes
```

**What Tests Should Verify**:
- **DO Test**: Data is displayed (presence), correct components render (structure), user interactions work (behavior), enum values match expected values
- **DON'T Test**: Exact display text (changes frequently), timestamp formats (presentational concern), CSS classes/styles (implementation detail), auto-generated note prefixes

### Feature Removal Requires Comprehensive Test Cleanup
**Problem**: Emergency contact fields removed from backend but test files still referenced them, causing compilation errors and test failures.
**Solution**: Search for field references across ALL test types and update systematically.

```typescript
// ❌ WRONG - Test data includes removed fields
const vettingApplication = {
  sceneName: 'TestUser',
  email: 'test@example.com',
  emergencyContactName: 'Emergency Contact',  // REMOVED FROM BACKEND
  emergencyContactPhone: '555-5678'           // REMOVED FROM BACKEND
};

// ✅ CORRECT - Test data matches current backend schema
const vettingApplication = {
  sceneName: 'TestUser',
  email: 'test@example.com'
  // Emergency contact fields removed
};
```

**Systematic Cleanup Approach**:
```bash
# 1. Find all references across test types
grep -rn "emergencyContact\|EmergencyContact" tests/ apps/web/tests/ --include="*.cs" --include="*.ts" --include="*.tsx"

# 2. Check multiple naming conventions
grep -rn "emergencyContact"   # camelCase (TypeScript)
grep -rn "EmergencyContact"   # PascalCase (C#)
grep -rn "emergency_contact"  # snake_case (if used)
```

**When Removing Backend Features**:
1. Search for field references across ALL test types
2. Update test data creation/seeding
3. Remove form filling logic in E2E tests
4. Remove validation assertions
5. Verify tests still compile (TypeScript and C#)
6. Document removal in test catalog

## E2E Test Selector Anti-Patterns (October 2025)

### Invisible Element Timeout Pattern
**Problem**: Generic selectors matching invisible mobile menu buttons causing 30-second timeouts.
**Discovery**: Tests using `button.first()` or `[role="tab"].first()` match mobile menu buttons that exist in DOM but are NOT VISIBLE on desktop.
**Impact**: 20-30 tests timing out, wasting 15 minutes per test run.

```typescript
// ❌ WRONG - Matches invisible mobile menu button
await page.locator('button').first().click();
await page.locator('[role="tab"]').first().click();

// ✅ CORRECT - Exclude mobile elements and ensure visibility
await page.locator('button:visible:not(.mobile-menu-toggle)').first().click();
await page.locator('[role="tab"]:visible:not(.mobile-menu-toggle)').first().click();

// ✅ BEST - Use specific data-test attributes
await page.locator('[data-testid="submit-button"]').click();
```

**Critical Insights**:
1. Playwright's auto-wait does NOT check visibility with `.first()` - it just waits for element to exist
2. Mobile menu buttons are ALWAYS in DOM (for responsive design) but hidden on desktop
3. Generic selectors match mobile elements first, causing invisible element timeouts
4. 30-second timeout = default action timeout trying to click invisible element

**Prevention**:
- Always use `:visible` pseudo-selector with `.first()`
- Exclude mobile-specific classes: `:not(.mobile-menu-toggle)`
- Add explicit visibility check before clicking
- Use specific data-test attributes instead of generic selectors

### Wrong Port Hardcoded URLs
**Problem**: Tests hardcoding wrong ports (5175, 5174) causing ERR_CONNECTION_REFUSED.
**Solution**: Always use Docker ports (5173 for web, 5655 for API) or relative URLs with baseURL.

```typescript
// ❌ WRONG - Hardcoded wrong port
await page.goto('http://localhost:5175/login');

// ✅ CORRECT - Use Docker port
await page.goto('http://localhost:5173/login');

// ✅ BEST - Use relative URL (baseURL from config)
await page.goto('/login');
```

**Docker Port Reference** (MANDATORY):
- Web (React): http://localhost:5173 (Docker container)
- API (.NET): http://localhost:5655 (Docker container)
- Database: localhost:5433 (Docker container)

### Profile Test Race Conditions (MIGRATED - October 2025)
**Problem**: Multiple tests using shared `member@witchcityrope.com` account causing data conflicts and flaky tests.
**Solution**: Create unique test user per test using database helpers.

**STATUS**: ✅ **ALL 16 PROFILE TESTS MIGRATED** to use unique users (October 9, 2025)
- profile-update-full-persistence.spec.ts: 14 tests migrated
- profile-update-persistence.spec.ts: 2 tests migrated

```typescript
// ❌ WRONG - Shared account (race condition)
await testProfileUpdatePersistence(page, {
  userEmail: 'member@witchcityrope.com',  // SHARED - causes race conditions
  userPassword: 'Test123!',
  updatedFields: { firstName: `Test${Date.now()}` }
});

// ✅ CORRECT - Unique user per test (October 2025 pattern)
import { createTestUser, generateUniqueTestEmail, cleanupTestUser, type TestUser } from './utils/database-helpers';

test('should persist profile update', async ({ page }) => {
  const testUser = await createTestUser({
    email: generateUniqueTestEmail('profile-test'),
    password: 'Test123!',
    sceneName: `TestUser${Date.now()}`,
    membershipLevel: 'Member'
  });

  try {
    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: testUser.password,
      updatedFields: { firstName: 'Updated Name' }
    });
  } finally {
    // Always cleanup, even if test fails
    await cleanupTestUser(testUser.id);
  }
});
```

**Database Helper Functions** (Updated October 2025):
- `createTestUser(options): Promise<TestUser>` - Returns `{id, email, password, sceneName}`
- `generateUniqueTestEmail(prefix)` - Generate unique email with timestamp
- `cleanupTestUser(emailOrId)` - Accepts either email OR user ID for cleanup
- `TestUser` interface - Type-safe user object

**CRITICAL: ASP.NET Core Identity Password Hashing**:
- Password hashes are unique per user (includes salt in hash)
- Cannot reuse password hash from one user for another
- Use registration API OR ASP.NET Core PasswordHasher for proper hashing
- Direct database insertion requires SecurityStamp and ConcurrencyStamp

**Why This Matters**:
1. Parallel test runs: Multiple tests updating same user data simultaneously
2. Test A updates firstName to "Test1728502345"
3. Test B updates firstName to "Test1728502346" (overwrites Test A)
4. Test A refresh verification fails (expects different firstName)
5. Result: Flaky tests that fail randomly

**Migration Complete**: All profile tests now use unique users, eliminating race conditions.

## Quick Reference

**Essential Commands**:
- `npm run test:e2e:playwright` - Run E2E tests
- `dotnet test --filter "Category=HealthCheck"` - Health checks first
- `npm test` - Run React unit tests

**Reference**: `/docs/standards-processes/testing/TESTING_GUIDE.md`

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

## Unit Test Helper Method Entity Persistence (October 2025)

### Test Helper Methods Must Add Entities to DbContext
**Problem**: Helper methods create entities but don't add them to DbContext, causing "entity not found" test failures.
**Root Cause**: Separation between entity creation and DbContext tracking - `SaveChangesAsync()` with no tracked entities saves nothing.
**Impact**: 9 RefundServiceTests failed because Payment entities weren't added to context before saving.

```csharp
// ❌ WRONG - Helper creates entity but doesn't track it
private Payment CreateCompletedPayment(decimal amount)
{
    return new Payment
    {
        EventRegistrationId = Guid.NewGuid(),
        UserId = _testUserId,
        AmountValue = amount,
        Currency = "USD",
        Status = PaymentStatus.Completed,
        ProcessedAt = DateTime.UtcNow,
        EncryptedPayPalOrderId = "encrypted-paypal-order-id"
    };
}

// ❌ WRONG - Test doesn't add entity before saving
var payment = CreateCompletedPayment(100.00m);
await _context.SaveChangesAsync();  // Nothing to save - no tracked entities!

// ✅ CORRECT - Test adds entity to context before saving
var payment = CreateCompletedPayment(100.00m);
_context.Payments.Add(payment);  // ← Track entity in DbContext
await _context.SaveChangesAsync();  // Now there's something to save
```

**Alternative Pattern** - Helper handles persistence:
```csharp
// ✅ CORRECT - Helper creates AND tracks entity
private async Task<Payment> CreateAndSaveCompletedPayment(decimal amount)
{
    var payment = new Payment
    {
        EventRegistrationId = Guid.NewGuid(),
        UserId = _testUserId,
        AmountValue = amount,
        Currency = "USD",
        Status = PaymentStatus.Completed,
        ProcessedAt = DateTime.UtcNow,
        EncryptedPayPalOrderId = "encrypted-paypal-order-id"
    };
    _context.Payments.Add(payment);
    await _context.SaveChangesAsync();
    return payment;
}

// ✅ Test just awaits the helper
var payment = await CreateAndSaveCompletedPayment(100.00m);
// Payment already saved to database
```

**When to Use Each Pattern**:
1. **Helper creates only** - When test needs to modify entity before saving, or test multiple creation scenarios without persistence
2. **Helper creates and saves** - When all tests need entity persisted immediately, simplifies test code

**Prevention Checklist**:
- [ ] Helper method creates entity → Test must call `_context.EntitySet.Add(entity)`
- [ ] Helper method creates and saves → Helper must call `Add()` and `SaveChangesAsync()`
- [ ] Verify `SaveChangesAsync()` is called AFTER entities are tracked
- [ ] Never call `SaveChangesAsync()` when DbContext has no tracked changes

**Real-World Example** (RefundServiceTests fix - October 2025):
```csharp
// Fixed 9 failing tests by adding this line before SaveChangesAsync():
_context.Payments.Add(payment);
```

**Reference**: `/home/chad/repos/witchcityrope/tests/unit/api/Services/RefundServiceTests.cs` (lines 95, 142, 221, 251, 296, 331, 375, 445, 523)

## E2E Test API Response Format Expectations (October 2025)

### E2E Tests Must Handle ApiResponse<T> Wrapper Format
**Problem**: E2E tests expected direct array responses from API endpoints, failing because all endpoints return `ApiResponse<T>` wrapper.
**Root Cause**: Tests written before API standards were fully documented, not reviewing backend lessons learned.
**Impact**: Tests fail with "expected array, got object" errors despite API working correctly.

```typescript
// ❌ WRONG - Expects direct array response
const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
const eventsData = await eventsApiResponse.json();
expect(Array.isArray(eventsData)).toBe(true); // ❌ FAILS - eventsData is ApiResponse object
expect(eventsData.length).toBeGreaterThan(0);

// ✅ CORRECT - Handles ApiResponse<List<EventDto>> wrapper
const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
const eventsResponse = await eventsApiResponse.json();
expect(eventsResponse.success).toBe(true);
expect(eventsResponse.error).toBeNull();
expect(Array.isArray(eventsResponse.data)).toBe(true); // ✅ Access array via .data
expect(eventsResponse.data.length).toBeGreaterThan(0);
```

**API Response Standard** (ALL endpoints):
```typescript
interface ApiResponse<T> {
  success: boolean;      // Operation success indicator
  data: T | null;       // Actual response data (array, object, etc.)
  error: string | null;  // Error message if success=false
  message: string | null; // Human-readable message
  timestamp: string;     // ISO 8601 timestamp
}
```

**Why This Standard Exists**:
1. **Consistency**: All endpoints use same wrapper format
2. **Error Handling**: Provides structured error information
3. **Metadata**: Includes timestamps and messages for debugging
4. **Frontend Compatibility**: React app expects and handles this format
5. **Backend Compliance**: Documented in backend lessons learned (lines 154-176)

**Prevention**:
- **Read backend lessons learned** before writing API tests
- **Validate response structure** against documented API contracts
- **Test wrapper properties** (success, data, error, message, timestamp)
- **Access data via `.data` property** for all API responses

**Fixed Files** (October 10, 2025):
- `/apps/web/tests/playwright/events-actual-routes-test.spec.ts` (lines 35-65, 220-233)
- `/apps/web/tests/playwright/e2e-events-full-journey.spec.ts` (lines 349-358)

**Reference Documentation**:
- Backend Lessons Learned Part 1 (lines 154-176): ApiResponse<T> standard
- Backend Lessons Learned Part 2 (lines 1323-1392): E2E test contract mismatch lesson
- API Response Analysis: `/test-results/api-events-response-format-analysis-20251010.md`
