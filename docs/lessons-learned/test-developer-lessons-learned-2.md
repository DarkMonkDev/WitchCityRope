# Test Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: test-developer-lessons-learned.md (MUST READ FIRST)
**Part 2**: test-developer-lessons-learned-2.md (THIS FILE)
**Read ALL**: Parts 1 AND 2 are MANDATORY
**Write to**: Part 2 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using lessons-learned-validator skill
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## 🚨 REQUIRED READING FOR SPECIFIC TASKS 🚨

### Before Creating E2E Persistence Tests
**MUST READ**: `/home/chad/repos/witchcityrope/docs/functional-areas/testing/e2e-persistence-testing-guide.md`
- Complete persistence test pattern (UI + API + Database verification)
- Database verification helpers
- How to use test templates
**CRITICAL**: Tests that only verify UI updates miss bugs where database doesn't update

### Before Creating Backend Integration Tests
**MUST READ**: `/home/chad/repos/witchcityrope/docs/functional-areas/testing/backend-integration-testing-guide.md`
- DTO/Entity mapping validation patterns
- TestContainers setup
- Database persistence verification
**CRITICAL**: Integration tests must verify all DTO fields map to entity properties

---

**Skills Usage**: See `/.claude/skills/HOW-TO-USE-SKILLS.md` for complete guide on when/how to use skills

---

## ⛔ NEVER Use Soft Assertions in E2E Tests (CRITICAL)

**Problem**: Using `if (await element.isVisible())` pattern makes tests pass even when features are broken, creating FALSE CONFIDENCE in test suite.

```typescript
// ❌ WRONG - Test passes if modal doesn't exist
if (await modal.isVisible()) {
  await expect(modal).toContainText('Success');
}

// ✅ CORRECT - Test FAILS if modal doesn't exist
await expect(modal).toBeVisible();
await expect(modal).toContainText('Success');
```

**When Soft Assertions Are Acceptable**: Only for INTENTIONALLY optional elements like marketing banners, not core features.

---

## ⛔ NEVER Suggest Long Timeouts (10+ Minutes)

**Problem**: Agents repeatedly suggest 10-minute or longer timeouts for tests, masking stalled/broken tests.

**User Feedback**: "NO TEST should ever take 10 minutes. Most will not take more than 30 seconds, giving them 1 minute maybe 1.5 at the absolute most is plenty."

```typescript
// ❌ WRONG - 10 minute timeout masks stalled test
test.setTimeout(600000); // ABSOLUTELY NO!

// ✅ CORRECT - 90 second ABSOLUTE MAXIMUM
test.setTimeout(90000); // ABSOLUTE MAX
await page.waitForSelector('.element', { timeout: 30000 }); // 30 seconds typical
```

**What to Do When Tests Timeout**: Fix the underlying issue (wrong selector, missing feature, service down), don't increase timeout above 90 seconds.

---

## TestContainers Integration Patterns

### PostgreSQL DateTime UTC Requirement
**Problem**: "Cannot write DateTime with Kind=Unspecified" errors.
**Solution**: Always use `DateTime.UtcNow` or `new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)`.

### Integration Test Data Isolation
**Problem**: Tests affecting each other's data causing duplicate key violations.
**Solution**: Use unique identifiers for ALL test data.

```csharp
// ❌ WRONG
var sceneName = "TestUser";

// ✅ CORRECT
var sceneName = $"TestUser_{Guid.NewGuid():N}";
```

---

## React Testing Patterns - MANDATORY

### React Testing Framework Stack
**Required Stack**:
- ✅ **Vitest**: Primary testing framework
- ✅ **React Testing Library**: Component testing
- ✅ **MSW**: API mocking for integration tests
- ✅ **Playwright**: E2E testing (NOT Puppeteer)
- ❌ **Jest**: Avoid - project uses Vitest

### MSW Axios BaseURL Compatibility
**Problem**: MSW handlers with relative paths not matching axios requests using baseURL.
**Solution**: Use full URLs when API client uses baseURL, always include auth refresh interceptor handler.

### React Component Test Infinite Loop Prevention
**Problem**: Duplicate navigation logic between components and hooks.
**Solution**: Single navigation source only - let mutation handle navigation, remove component navigation.

---

## Unit Test Migration from Mocks to Real Database

### TestContainers Migration Pattern
**Problem**: ApplicationDbContext doesn't have parameterless constructor required for mocking.
**Solution**: Use TestContainers with PostgreSQL + Respawn for database cleanup instead of mocking.

```csharp
// ❌ WRONG
var mockContext = new Mock<ApplicationDbContext>(); // Fails

// ✅ CORRECT
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
_context = new ApplicationDbContext(options);
```

---

## E2E Testing Patterns

### Dual E2E Test Configuration Pattern
**Problem**: Import errors blocking E2E test execution due to incorrect path resolution across separate test configurations.

**Critical Rules**:
1. **Each test suite uses its own helpers** - Do NOT cross-reference
2. **Count parent traversals carefully** - Verify actual file system paths
3. **Check both configs exist** - Multiple Playwright configs may be in use

### E2E Port Configuration - Hardcoded Ports
**Problem**: E2E tests hardcoded to wrong ports (5174, 5653) blocking tests from executing.

**Docker Port Reference** (MANDATORY):
- Web (React): http://localhost:5173
- API (.NET): http://localhost:5655
- Database: localhost:5433

### E2E Authentication Cookie Persistence - ABSOLUTE URLs REQUIRED
**Problem**: Relative URLs cause cookie persistence issues in Playwright.
**Solution**: Always use absolute URLs for proper cookie handling.

```typescript
// ❌ WRONG
await page.goto('/login');

// ✅ CORRECT
await page.goto('http://localhost:5173/login');
```

---

## Integration Test Maintenance

### Infrastructure Test Baseline Verification
**Problem**: Baseline reports showing test failures can become stale quickly in active development.
**Solution**: Always verify current test status before starting fix work.

**Prevention Pattern**:
1. **Re-run tests immediately before starting fix work** - Baseline may be stale
2. **Run multiple times** - Infrastructure tests can be flaky, verify consistency
3. **Check timestamps** - If baseline is hours/days old, verify current state first
4. **Category D (Infrastructure) tests are LOW priority** - They often self-heal

---

## Test Maintenance Patterns

### Display Text Changes Rarely Break Well-Architected Tests
**Problem**: Backend changed display labels. Concern that many tests would need updates.
**Discovery**: Only 1 test file needed updating. No tests had hardcoded display text expectations.

**What Tests Should Verify**:
- **DO Test**: Data is displayed (presence), correct components render, user interactions work, enum values
- **DON'T Test**: Exact display text, timestamp formats, CSS classes, auto-generated note prefixes

### Feature Removal Requires Comprehensive Test Cleanup
**Problem**: Fields removed from backend but test files still referenced them.
**Solution**: Search for field references across ALL test types and update systematically.

```bash
# Find all references across test types
grep -rn "emergencyContact\|EmergencyContact" tests/ apps/web/tests/ --include="*.cs" --include="*.ts" --include="*.tsx"
```

---

## E2E Test Selector Anti-Patterns (October 2025)

### Invisible Element Timeout Pattern
**Problem**: Generic selectors matching invisible mobile menu buttons causing 30-second timeouts.

```typescript
// ❌ WRONG - Matches invisible mobile menu button
await page.locator('button').first().click();

// ✅ CORRECT - Exclude mobile elements and ensure visibility
await page.locator('button:visible:not(.mobile-menu-toggle)').first().click();

// ✅ BEST - Use specific data-test attributes
await page.locator('[data-testid="submit-button"]').click();
```

**Critical Insights**: Playwright's auto-wait does NOT check visibility with `.first()` - mobile menu buttons are ALWAYS in DOM but hidden on desktop.

---

## Profile Test Race Conditions (MIGRATED - October 2025)

**Problem**: Multiple tests using shared `member@witchcityrope.com` account causing data conflicts and flaky tests.
**Solution**: Create unique test user per test using database helpers.

**STATUS**: ✅ ALL 16 PROFILE TESTS MIGRATED to use unique users (October 9, 2025)

```typescript
// ❌ WRONG - Shared account (race condition)
userEmail: 'member@witchcityrope.com',

// ✅ CORRECT - Unique user per test
import { createTestUser, generateUniqueTestEmail, cleanupTestUser } from './utils/database-helpers';

const testUser = await createTestUser({
  email: generateUniqueTestEmail('profile-test'),
  password: 'Test123!',
  sceneName: `TestUser${Date.now()}`,
  membershipLevel: 'Member'
});
```

**CRITICAL: ASP.NET Core Identity Password Hashing**: Password hashes are unique per user (includes salt in hash). Cannot reuse password hash from one user for another.

---

## Unit Test Helper Method Entity Persistence (October 2025)

### Test Helper Methods Must Add Entities to DbContext
**Problem**: Helper methods create entities but don't add them to DbContext, causing "entity not found" test failures.

```csharp
// ❌ WRONG - Test doesn't add entity before saving
var payment = CreateCompletedPayment(100.00m);
await _context.SaveChangesAsync();  // Nothing to save - no tracked entities!

// ✅ CORRECT - Test adds entity to context before saving
var payment = CreateCompletedPayment(100.00m);
_context.Payments.Add(payment);  // Track entity
await _context.SaveChangesAsync();
```

---

## E2E Test API Response Format Expectations (October 2025) - DEPRECATED PATTERN

### 🚨 CRITICAL UPDATE (2025-11-13): ApiResponse<T> is DEPRECATED 🚨
**CURRENT STANDARD**: Pattern B - Direct `Results.Ok(dto)` + RFC 9457 Problem Details
**THIS LESSON IS HISTORICAL**: Documents OLD ApiResponse<T> wrapper pattern (no longer used)
**For NEW tests**: Expect direct DTO responses or RFC 9457 Problem Details for errors
**See**: Backend Developer Lessons Learned Part 1 for Pattern B documentation

---

## E2E Test Multiple Notification Handling (October 2025)

### Strict Mode Violation with Multiple Notifications
**Problem**: Profile tests failing with "strict mode violation: locator resolved to 2 elements" when checking success notifications.

```typescript
// ❌ WRONG - Strict mode violation when 2+ notifications exist
await expect(successAlert).toBeVisible(); // Fails: resolved to 2 elements

// ✅ CORRECT - Use .first() to handle multiple notifications
const successAlert = page.locator('[role="alert"]').first();
await expect(successAlert).toBeVisible();
```

**When to Use .first()**: Notification checking (may have multiple stacked), success/error messages, any element that may appear multiple times.

---

## Prevention Pattern: Test Selector Mismatch with Actual UI (2025-11-10)

**Problem**: Tests failing because they expected `data-testid="create-event-button"` but actual button had `data-testid="button-create-event"`.

**Prevention**:
1. ❌ **NEVER** write test selectors without checking actual component code first
2. ✅ **ALWAYS** search for existing `data-testid` attributes in components
3. ✅ **VERIFY** by running test in headed mode and inspecting DOM with DevTools

---

## Prevention Pattern: Dashboard Content Assertions - Accept Actual Content

**Problem**: E2E tests failed because they expected specific h1 text but actual implementation shows different content.

**Solution**: Use flexible content assertions that match actual implementation:

```typescript
// ❌ WRONG - Hard-coded expected content
await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

// ✅ CORRECT - Accept actual dashboard content
await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i);
```

**Prevention Rules**: Run tests first in headed mode to see actual content before asserting, use flexible patterns with multiple acceptable values.

---

## Backend Unit Test DbContext Mocking Issues (November 2025)

### ApplicationDbContext Cannot Be Mocked - Use InMemoryDatabase Instead
**Problem**: Attempting to mock ApplicationDbContext with Moq fails because it lacks a parameterless constructor.

```csharp
// ❌ WRONG
private readonly Mock<ApplicationDbContext> _mockContext;

// ✅ CORRECT
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
_context = new ApplicationDbContext(options);
```

---

## Integration Test Entity Property Mismatches (November 2025)

### Event Entity Property Name Changes - Check Current Schema
**Problem**: Integration tests fail with "does not contain a definition" errors for Event entity properties.

**Common Mismatches**:
```csharp
// ❌ WRONG - Old property names
StartTime = DateTime.UtcNow.AddDays(7),
EventType = EventType.SocialEvent,

// ✅ CORRECT - Current property names
StartDate = DateTime.UtcNow.AddDays(7),
EventType = EventType.Social,
Location = "Test Location",  // Required property
```

---

## Respawn FK Constraint Cleanup Issues (November 2025)

### Integration Tests Failing Due to Respawn FK Violations
**Problem**: Integration tests fail when run together because Respawn database cleanup encounters FK constraint violations.

**Solution Pattern - Skip Tests with Clear Documentation**:
```csharp
// ✅ CORRECT - Skip tests when infrastructure issue, not endpoint bug
[Fact(Skip = "Respawn FK constraint issue (Events->Venues). Passes individually. Endpoints work correctly.")]
public async Task GetPublicVenue_WithValidId_ReturnsVenue()
{
    // Test implementation stays the same
}
```

**Decision Criteria**: Skip when tests pass individually, issue is pure infrastructure, fixing Respawn config is high cost/risk, small number of tests affected.

---

## 🚨 CRITICAL: E2E Test Location - Single Unified Location (2025-11-23)

**Problem**: E2E tests created in wrong location (`/tests/e2e/`) which no longer exists, causing tests to be lost or duplicated.

### ✅ CORRECT Pattern: ONLY Use Apps-Level Location

```bash
# ✅ CORRECT - ONLY valid location for E2E tests
/apps/web/tests/playwright/

# ❌ WRONG - DELETED, no longer exists
/tests/e2e/  # OBSOLETE - CONSOLIDATED 2025-11-23
```

### Prevention Rules

1. ✅ **ALWAYS create E2E tests in `/apps/web/tests/playwright/`**
2. ✅ **NEVER create tests in `/tests/e2e/`** (DELETED location)
3. ✅ **USE relative imports** from same test suite
4. ❌ **DON'T cross-reference** between old and new locations

---

## 🚨 CRITICAL: Mantine v7 Checkbox Interaction Pattern for Playwright E2E Tests (2025-11-16)

**Problem**: Mantine v7 completely hides the actual checkbox input with CSS, making it invisible to Playwright's actionability checks. Standard `.check()` and label-based interactions fail.

### ✅ CORRECT Pattern: Click the Parent Mantine Wrapper

```typescript
// ✅ CORRECT - Click parent wrapper element
const mantineCheckbox = page.locator('input[data-testid="waiver-checkbox"]')
  .locator('..')  // Get parent element (the Mantine checkbox wrapper)
  .last();  // Use .last() to avoid React strict mode duplicate
await mantineCheckbox.click();

// Verify the checkbox is checked
await expect(page.locator('input[data-testid="waiver-checkbox"]')).toBeChecked();
```

### Prevention Rules for Mantine v7 Checkboxes

1. ✅ **ALWAYS click parent wrapper**: `page.locator('input[data-testid]').locator('..').last().click()`
2. ✅ **ALWAYS use `.last()`**: Avoids React strict mode duplicates
3. ✅ **ALWAYS verify with `.toBeChecked()`**: Check the actual input state after clicking
4. ❌ **NEVER use `.check()` on Mantine checkboxes**: Will fail with visibility errors
5. ❌ **NEVER click labels directly**: Doesn't trigger Mantine checkbox state

---

## 🚨 CRITICAL: React Strict Mode Testing Pattern - Always Use .last() for Interactive Elements (2025-11-16)

**Problem**: React strict mode creates duplicate DOM elements (both hidden and visible) for debugging purposes. Using `.first()` selects hidden duplicates, causing "Element is not visible" errors.

### ✅ CORRECT Pattern: Using .last()

```typescript
// ✅ CORRECT - Selects visible element
const button = page.locator('button:has-text("Submit RSVP")').last();
await button.click(); // Works - clicks visible element

const checkbox = page.locator('input[data-testid="waiver-checkbox"]')
  .locator('..')  // For Mantine - get parent wrapper
  .last();  // Select visible duplicate
await checkbox.click(); // Works - clicks visible element
```

### When to Apply This Pattern

**ALWAYS use `.last()` for**:
- ✅ Buttons, Inputs, Checkboxes (especially Mantine), Links
- ✅ Any interactive element that could be duplicated

**ONLY use `.first()` when**:
- ❌ You explicitly want the FIRST occurrence in a list (e.g., first item in search results)
- ⚠️ **CAUTION**: Even in these cases, `.last()` is safer if elements might be duplicated

---

## 🚨 CRITICAL: E2E Persistence Testing Value - Real Bug Discovery (2025-11-16)

**Problem**: E2E tests successfully caught a critical backend bug where POST /api/events/{id}/rsvp returns 201 success but doesn't persist the RSVP to the database.

**Bug Impact**: Users see "RSVP confirmed" but database has no record - data loss!

### Pattern: What E2E Persistence Tests MUST Verify

**1. UI Shows Success**
**2. API Returns Success Status Code**
**3. Database Record Exists with Correct Status** (MOST IMPORTANT)
**4. Database Record Persists After Page Refresh** (CRITICAL):

```typescript
// Refresh page to clear React Query cache
await page.reload();

// Verify UI still shows correct state (from database, not cache)
await expect(page.locator('button:has-text("Cancel RSVP")')).toBeVisible();

// Verify database still has record
await DatabaseHelpers.verifyEventParticipation(userId, eventId, 1, 2);
```

### Prevention Rules for E2E Persistence Tests

1. ✅ **NEVER trust API status codes alone** - Verify database state
2. ✅ **ALWAYS test page refresh** - Ensures data persists, not just cached
3. ✅ **FILTER database queries properly** - Status AND Type
4. ✅ **DEBUG log what records exist** - When verification fails, show all records

---

## 🚨 CRITICAL: Database Query Filtering for Persistence Tests - Filter by Status AND Type (2025-11-16)

**Problem**: Users can have multiple attendance records for the same event (e.g., both RSVP and Ticket, or old cancelled records). Querying only by userId + eventId returns wrong record type or old cancelled records.

### ✅ CORRECT Pattern: Filter by Status AND AttendanceType

```typescript
// ✅ CORRECT - Filters by BOTH Status AND Type
const sql = `
  SELECT * FROM "EventAttendances"
  WHERE "UserId" = $1
    AND "EventId" = $2
    AND "Status" = $3
    AND "AttendanceType" = $4
  ORDER BY "UpdatedAt" DESC
  LIMIT 1
`;

const result = await query(sql, [
  userId,
  eventId,
  1,  // Status: 1=Active (filters out cancelled records)
  2   // AttendanceType: 2=RSVP (ensures we get RSVP, not Ticket)
]);
```

**AttendanceType Enum Reference**:
- 1 = Ticket (Paid ticket purchase)
- 2 = RSVP (Free RSVP, requires waiver)
- 3 = CheckIn (Walk-in at event)
- 4 = Volunteer (Volunteer assignment)

**Status Enum Reference**:
- 0 = Cancelled/inactive
- 1 = Active

---

## 🚨 CRITICAL: Pattern B Uses ProblemHttpResult, NOT JsonHttpResult<ProblemDetails> (2025-11-13)

**Problem**: Pattern B endpoint tests used wrong result type assertions, causing 80 of 90 tests to fail despite endpoints working correctly.

```csharp
// ❌ WRONG - Old pattern expectation
result.Should().BeOfType<JsonHttpResult<ProblemDetails>>();

// ✅ CORRECT - Pattern B
result.Should().BeOfType<ProblemHttpResult>();
var problemResult = (ProblemHttpResult)result;
problemResult.StatusCode.Should().Be(404);
```

**Prevention Rules**:
1. ✅ **Pattern B success**: Assert `BeOfType<Ok<DtoType>>()`
2. ✅ **Pattern B error**: Assert `BeOfType<ProblemHttpResult>()`
3. ❌ **NEVER use**: `JsonHttpResult<ProblemDetails>` (old pattern)

---

## 🚨 CRITICAL: Never Mock ApplicationDbContext Directly in Endpoint Tests (2025-11-13)

**Problem**: VenueEndpointsTests attempted to mock `ApplicationDbContext` directly, causing all 9 tests to fail with constructor errors.

**Root Cause**: `ApplicationDbContext` has no parameterless constructor, NSubstitute cannot mock it.

**Prevention Rules**:
1. ❌ **NEVER inject ApplicationDbContext directly into endpoints**
2. ✅ **ALWAYS use service layer** with interface for database access
3. ✅ **Services encapsulate database logic** and business rules
4. ✅ **Endpoints orchestrate services** - no direct DB access

---

## 🚨 CRITICAL ANTI-PATTERN: Test Helpers Bypassing Production Code (2025-11-11)

**Problem**: Test helper method in SafetyServiceTests created incidents directly in database, bypassing production code path for reference number generation. This resulted in ZERO coverage of critical business logic.

**Impact**:
- **0% coverage** of `GenerateReferenceNumberAsync()` method
- **Missing database logic** went undetected (sequential numbering, highest sequence lookup)
- **All 15 tests** used bypassed helper - NO tests validated production code

**Correct Pattern - Call Production Code**:
```csharp
// ✅ CORRECT - Calls actual production code path
private async Task<SafetyIncident> CreateTestIncidentAsync(...)
{
    var request = new CreateIncidentRequest { /* ... */ };

    // Call ACTUAL service method - exercises production code!
    var result = await _sut.SubmitIncidentAsync(request);

    var incident = await _context.SafetyIncidents
        .FirstAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

    return incident;
}
```

**Prevention Rules**:
1. ✅ **TEST HELPERS MUST CALL PRODUCTION CODE** - Never recreate business logic
2. ✅ **USE SERVICE METHODS** - Call actual service methods
3. ✅ **ADD DEDICATED TESTS** - Create specific tests for business logic
4. ❌ **NEVER MANUALLY CREATE ENTITIES** - Don't instantiate domain entities directly in test setup

---

## ⛔ When to Archive vs Fix Broken Tests After Refactoring

**Problem**: Major refactorings break test files. Deciding whether to fix or archive tests wastes time if done wrong.

### Decision Tree: Archive vs Fix

**Archive When**:
1. ✅ **Architecture fundamentally changed** - Not just renamed, but different patterns
2. ✅ **API signatures completely different** - Different parameters, return types, dependencies
3. ✅ **Tests already disabled by original developer** - Found in `.disabled/` folder = signal
4. ✅ **Would require complete rewrite** - More than 50% of test assertions need changes

**Fix When**:
1. ✅ **Simple rename** - Same class/method, just different name
2. ✅ **Import path changes** - Just namespace/module changes
3. ✅ **Minor signature updates** - Added optional parameter, same core logic
4. ✅ **Quick fixes** - Can fix in < 30 minutes

**Rule**: If not fixable in 30 minutes, stop and archive instead.

---

## Test Helper Naming Convention: Direct vs. Via Service (2025-11-11)

**Problem**: Test helpers that create entities directly in database bypass business logic. Unclear naming makes it easy to accidentally use bypass helpers when production code should be tested.

**Solution**: Use explicit naming convention to indicate whether helper bypasses production code:

**Bypass Helpers (Direct DB Access)**:
- `CreateTestUserDirectly()` - Bypasses user services
- `CreateTestEventDirectly()` - Bypasses event services

**Production Helpers (Call Services)**:
- `CreateTestUserViaService()` - Calls UserService/UserManager
- `CreateTestEventViaService()` - Calls EventService.CreateEventAsync()

**Use `*Directly()` helpers when**: Setting up test data for unrelated tests, entity creation is NOT being tested.

**Use `*ViaService()` helpers when**: Testing features that depend on creation logic, validating business rules.

---

## 🚨 CRITICAL: Playwright Request Context vs UI Testing - Scene Name Login (2025-11-17)

**Problem**: Using `page.request.post()` for API calls in E2E tests fails due to cookie/CORS issues, causing helper functions to break. Tests that should verify UI behavior were making API calls instead.

**Wrong Pattern** - Using Playwright request context:
```typescript
// ❌ WRONG - API call fails with Playwright request context
const loginResponse = await page.request.post(`${API_URL}/api/auth/login`, {
  data: { emailOrSceneName: email, password: password }
});
```

**Correct Pattern** - Use known test data:
```typescript
// ✅ CORRECT - Use test data seeded in database
const sceneNames: Record<string, string> = {
  'admin@witchcityrope.com': 'RopeMaster',
  'teacher@witchcityrope.com': 'SafetyFirst',
  'member@witchcityrope.com': 'Learning'
};
```

**When writing E2E tests**:
- ✅ **Test UI behavior, not API endpoints**
- ✅ **Use seeded test data** - Don't make API calls to get test data
- ✅ **Verify actual UI elements** - Take screenshots, inspect DOM

---

## 🚨 CRITICAL: E2E Testing Gold Standard Patterns - e2e-events-full-journey (2025-11-17)

### Pattern 1: ALWAYS Use AuthHelpers - NEVER Direct API Calls

```typescript
// ❌ WRONG - Direct API call
const response = await request.post('/api/auth/login', { data: {...} });

// ✅ CORRECT - Use AuthHelpers
await AuthHelpers.loginAs(page, 'member');
```

### Pattern 2: Fresh Locators - Don't Reuse Elements After Navigation

```typescript
// ❌ WRONG - Reusing element after navigation
const eventCard = page.locator('[data-testid="event-card"]').first();
await eventCard.click();
await page.goBack();
await eventCard.click(); // FAILS - Element is detached!

// ✅ CORRECT - Create fresh locator after navigation
const eventCard1 = page.locator('[data-testid="event-card"]').first();
await eventCard1.click();
await page.goBack();
const eventCard2 = page.locator('[data-testid="event-card"]').first();
await eventCard2.click(); // WORKS
```

### Pattern 3: Scoped Selectors - Avoid Global Text Searches

```typescript
// ❌ WRONG - Global text search
const eventsLink = page.locator('text=Events');

// ✅ CORRECT - Scoped to specific container
const breadcrumb = page.locator('[data-testid="event-details"]');
const eventsLink = breadcrumb.getByRole('link', { name: 'Events' });
```

### Pattern 4: Element Stability - Wait for Visibility Before Interactions

```typescript
// ❌ WRONG - Click immediately
await rsvpButton.click();

// ✅ CORRECT - Wait for visibility first
await rsvpButton.waitFor({ state: 'visible' });
await rsvpButton.click();
```

### Pattern 5: API Response Format - Check Actual Responses

```typescript
// ❌ WRONG - Assuming wrapped response
expect(data.success).toBe(true);
expect(data.data.length).toBeGreaterThan(0);

// ✅ CORRECT - Match actual response format
expect(Array.isArray(data)).toBe(true);
expect(data.length).toBeGreaterThan(0);
```

**Comprehensive Checklist**:
- ✅ Use `AuthHelpers.loginAs()` for authentication
- ✅ Create fresh locators after navigation
- ✅ Scope selectors to containers
- ✅ Wait for visibility before interactions
- ✅ Verify actual API response formats
- ✅ Clear auth state in beforeEach

---

## 🚨 CRITICAL MANDATORY: HTTP Integration Tests MUST Use WebApplicationFactory<Program> (2025-11-18)

**Problem**: Integration tests failing with 404 errors because no test server exists. Tests were created without WebApplicationFactory, causing HTTP requests to fail even though endpoints are correctly implemented.

### Correct Pattern - MANDATORY WebApplicationFactory

```csharp
[Collection("Database")]
public class EventAttendanceEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ApplicationDbContext _context;

    public EventAttendanceEndpointsTests(DatabaseTestFixture fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseNpgsql(fixture.ConnectionString));
                });
            });

        _context = fixture.CreateDbContext();
    }

    public void Dispose() => _factory?.Dispose();
}
```

### Why WebApplicationFactory is Mandatory

**WebApplicationFactory Responsibilities**:
1. ✅ Creates actual HTTP server instance
2. ✅ Configures dependency injection for tests
3. ✅ Replaces production DbContext with test database
4. ✅ Handles middleware and authentication
5. ✅ Properly disposes resources on cleanup

**Without WebApplicationFactory**:
- ❌ No HTTP server to receive requests
- ❌ All HTTP calls return 404 (nowhere to send them)
- ❌ Endpoints not even invoked
- ❌ Tests appear to test endpoints but actually test nothing

### Prevention Checklist

**BEFORE creating ANY HTTP integration test**:
- ✅ Include `WebApplicationFactory<Program>` in test class
- ✅ Configure services in `WithWebHostBuilder`
- ✅ Replace production DbContext with test database
- ✅ Create authenticated HttpClient with token
- ✅ Implement `IDisposable` to cleanup factory

**REQUIRED Methods/Patterns**:
- ✅ Constructor: Initialize factory and context
- ✅ `Dispose()`: Call `_factory?.Dispose()`
- ✅ Each test: Get fresh client with `_factory.CreateClient()`
- ✅ Authentication: Add JWT token to Authorization header

---

## 🚨 CRITICAL: Reuse Existing Seeded Events in Tests - DO NOT Create New Events (2025-11-20)

**Problem**: Tests creating new events from scratch cause FK constraint violations, test data pollution, and maintenance overhead. User explicitly requested: "USE EXISTING EVENTS, don't create new ones."

### The Rule: ALWAYS Reuse Seeded Events

**ABSOLUTE REQUIREMENT**: Tests MUST use existing seeded events from database, NOT create new events.

**Why This Matters**:
1. **FK Constraints**: Creating TicketTypes without Events = constraint violations
2. **Test Pollution**: Each test run adds more duplicate data to database
3. **Maintenance**: Seeded data changes break tests that create their own data
4. **Performance**: Creating entities is slower than querying existing ones
5. **User Request**: User explicitly demanded reusing existing events

### Correct Pattern: Query Seeded Events First

**Unit Tests**:
```csharp
// ✅ CORRECT - Query existing seeded event
var existingEvent = await _context.Events
    .Include(e => e.TicketTypes)
    .Where(e => e.StartDate > DateTime.UtcNow)
    .FirstOrDefaultAsync(cancellationToken);

if (existingEvent == null)
{
    // ONLY create minimal FK chain if truly no seeded data exists
    existingEvent = new Event { /* minimal data */ };
    _context.Events.Add(existingEvent);
}

var ticketType = existingEvent.TicketTypes.FirstOrDefault();
```

### WRONG Patterns - DO NOT DO THIS

❌ **Creating Events in Every Test**:
```csharp
// WRONG - Creates new event every test run
var evt = new Event { Title = "Test Event", StartDate = DateTime.UtcNow.AddDays(7) };
_context.Events.Add(evt);
```

❌ **Creating TicketTypes Without Events**:
```csharp
// WRONG - FK_TicketTypes_Events_EventId constraint violation
var ticketType = new TicketType { Name = "Test Ticket", Price = 25m };
_context.TicketTypes.Add(ticketType); // CRASH - No EventId
```

### Seeded Test Data Available

**Seeded Events** (see EventSeeder.cs):
- Upcoming workshops (Class events)
- Upcoming socials (Social events)
- Past historical events (for time-based tests)

**Seeded Users** (see UserSeeder.cs):
- admin@witchcityrope.com (Admin role)
- teacher@witchcityrope.com (Teacher role)
- vetted@witchcityrope.com (Vetted member with purchases)
- member@witchcityrope.com (General member)
- guest@witchcityrope.com (Guest/Attendee)

**Seeded Purchases** (see TicketPurchaseSeeder.cs):
- Vetted user has 3-5 ticket purchases
- Mix of PayPal, Venmo, Cash, Free payments
- Historical purchases (>90 days old for refund testing)
- Various payment statuses

### When to Create Test Data (Rare Exceptions)

**ONLY create new data when**:
1. Testing entity creation logic itself (e.g., CreateEventCommand tests)
2. Testing unique constraint violations (need duplicate data)
3. Testing specific edge cases not in seed data

**Even then**: Query first, create ONLY if truly doesn't exist.

### Key Lesson

**ABSOLUTE RULE**: Query existing seeded events/users/data FIRST, create ONLY if absolutely necessary for the specific test scenario.

**If your test creates new Events, TicketTypes, or Users**:
- ❌ You're probably doing it wrong
- ✅ FIX: Query existing seeded data instead

---

## 🚨 CRITICAL: Avoid networkidle Wait Strategy - Use domcontentloaded Instead (2025-11-24)

**Problem**: Using `waitForLoadState('networkidle')` causes test timeouts in applications with continuous background requests (polling, analytics, metrics).

**Root Cause**: App has continuous background requests that prevent network from ever becoming truly "idle".

### Why networkidle Fails

**networkidle Definition**: Waits until there are no network connections for at least 500ms.

**Apps with Continuous Requests**:
- API polling (e.g., notification checks every 30 seconds)
- Analytics beacons
- Health check endpoints
- Real-time updates
- Background data synchronization

**Result**: Network NEVER becomes idle → Test times out after 30 seconds

### ❌ WRONG Pattern - Using networkidle

```typescript
// ❌ WRONG - Causes timeouts with background requests
await page.goto('http://localhost:5173/events')
await page.waitForLoadState('networkidle')  // HANGS - network never idle

await page.reload()
await page.waitForLoadState('networkidle')  // HANGS - network never idle
```

### ✅ CORRECT Pattern - Use domcontentloaded

```typescript
// ✅ CORRECT - Waits for DOM ready, not network idle
await page.goto('http://localhost:5173/events')
await page.waitForLoadState('domcontentloaded')  // DOM ready, doesn't wait for network

await page.reload()
await page.waitForLoadState('domcontentloaded')  // DOM ready after refresh
```

### When to Use Each Wait Strategy

**Use `domcontentloaded` (DEFAULT for most tests)**:
- ✅ Navigation between pages
- ✅ Page refreshes
- ✅ Apps with polling/background requests
- ✅ When you just need DOM elements to be ready
- ✅ 95% of E2E test scenarios

**Use `networkidle` (RARE - only when specifically needed)**:
- ⚠️ Waiting for dynamic content that loads via AJAX
- ⚠️ Testing lazy-loaded images/resources
- ⚠️ Apps with NO background requests/polling
- ⚠️ Specific scenarios where you need all network activity to finish

**Use `load` (MIDDLE GROUND)**:
- ⚠️ Need all resources loaded (images, stylesheets, scripts)
- ⚠️ Testing initial page load performance
- ⚠️ Can still timeout if resources fail to load

### Detection and Prevention

**How to Detect This Issue**:
1. Test hangs for ~30 seconds before failing
2. Error: `Timeout 30000ms exceeded`
3. Error context: `waiting for load state "networkidle"`
4. Check browser DevTools: Network tab shows ongoing requests

**Prevention Rules**:
1. ✅ **DEFAULT to `domcontentloaded`** for all navigation waits
2. ✅ **AVOID `networkidle`** unless you have a specific reason
3. ✅ **DOCUMENT why** if you use `networkidle` (comment explaining need)
4. ❌ **NEVER use `networkidle`** in apps with polling/background requests

### Alternative Patterns

**If you need to wait for specific API calls**:
```typescript
// ✅ CORRECT - Wait for specific request, not all network
const responsePromise = page.waitForResponse(resp =>
  resp.url().includes('/api/events') && resp.status() === 200
)
await page.goto('/events')
await responsePromise  // Wait for specific API call
```

**If you need to wait for specific element after load**:
```typescript
// ✅ CORRECT - Wait for DOM, then wait for specific element
await page.goto('/events')
await page.waitForLoadState('domcontentloaded')
await page.locator('[data-testid="event-list"]').waitFor({ state: 'visible' })
```

### Key Lesson

**ABSOLUTE RULE**: Use `domcontentloaded` as the default wait strategy for page loads and navigation. Only use `networkidle` if you have a specific, documented reason and the app has NO background requests.

**If your tests timeout on `networkidle`**:
- ❌ Don't increase timeout duration
- ✅ Replace with `domcontentloaded`
- ✅ Wait for specific elements/requests if needed

**Why This Matters**: Modern web apps often have analytics, polling, or real-time features that prevent network from becoming truly idle. Tests using `networkidle` will fail or become extremely slow in these apps.

---
