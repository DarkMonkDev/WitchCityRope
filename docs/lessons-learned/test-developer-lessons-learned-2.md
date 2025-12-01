# Test Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY: Read Playwright Guide Before Any E2E Test Development

**SINGLE SOURCE OF TRUTH**: All E2E testing patterns are documented in:
`/docs/standards-processes/testing/browser-automation/playwright-guide.md`

**REQUIREMENTS**:
- [ ] READ the Playwright guide before writing any E2E tests
- [ ] UPDATE the guide when discovering new patterns (not this file)
- [ ] REFERENCE the guide when encountering issues

This lessons learned file documents PROBLEM DISCOVERY CONTEXT only.
For actual patterns and solutions, see the Playwright guide.

---

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## Testing - MANDATORY READING

**Single Source of Truth**: [TESTING_GUIDE.md](/docs/standards-processes/testing/TESTING_GUIDE.md)

### Critical Rules
- ALL tests go in `/tests/` directory
- React tests: `/tests/unit/web/[feature]/`
- E2E tests: `/tests/e2e/[feature]/`
- .NET unit tests: `/tests/unit/api/[domain]/`
- DO NOT create tests co-located with source code

**See TESTING_GUIDE.md for complete testing standards.**

---

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

## 🚨 E2E Testing Patterns - NOW IN PLAYWRIGHT GUIDE (2025-12-01)

**Pattern consolidation completed 2025-12-01**: All E2E testing patterns were moved from lessons learned to the Playwright guide to establish a single source of truth.

### Patterns Now in Playwright Guide

**When you encounter these issues**, consult the Playwright guide sections:

1. **Container-Compatible URLs** → See Playwright guide section 2
   - **Problem Discovered**: November 2025 - Tests with hardcoded localhost URLs failed in containers
   - **Solution**: Playwright guide section 2 (Container-Compatible URL Patterns)

2. **Wait Strategy Timeouts** → See Playwright guide section 3
   - **Problem Discovered**: November 2024 - Tests using networkidle hung for 30+ seconds
   - **Solution**: Playwright guide section 3 (Wait Strategies)

3. **CSRF Token Handling** → See Playwright guide section 4
   - **Problem Discovered**: Throughout 2024-2025 - State-changing operations failed with 403
   - **Solution**: Playwright guide section 4 (CSRF Token Handling)

4. **Mantine Checkbox Interactions** → See Playwright guide section 5
   - **Problem Discovered**: November 2025 - Standard .check() failed on Mantine checkboxes
   - **Solution**: Playwright guide section 5 (Mantine v7 Component Patterns)

5. **React Strict Mode Duplicates** → See Playwright guide section 6
   - **Problem Discovered**: November 2025 - .first() selected hidden elements
   - **Solution**: Playwright guide section 6 (React Strict Mode Testing Pattern)

6. **TDD Defensive Skips** → See Playwright guide section 7
   - **Problem Discovered**: November 2025 - Tests failed on unimplemented features
   - **Solution**: Playwright guide section 7 (TDD Test Patterns)

7. **Authentication Helpers** → See Playwright guide section 8
   - **Problem Discovered**: Throughout 2024-2025 - Manual login implementations broke
   - **Solution**: Playwright guide section 8 (Authentication Patterns)

8. **Database Persistence Verification** → See Playwright guide section 9
   - **Problem Discovered**: November 2025 - E2E tests caught critical persistence bug
   - **Solution**: Playwright guide section 9 (Database Persistence Verification)

9. **Test Data Management** → See Playwright guide section 1
   - **Problem Discovered**: November 2025 - Tests relying on seed data were fragile
   - **Solution**: Playwright guide section 1 (Test Data Management)

10. **API URL Hardcoding** → See Playwright guide section 2
    - **Problem Discovered**: December 2025 - Node.js fetch() broke in containers
    - **Solution**: Playwright guide section 2 (NEVER Hardcode API URLs)

### How to Use This Information

**When writing E2E tests**:
1. Read the Playwright guide FIRST
2. Reference specific sections for patterns
3. If you discover a NEW pattern, ADD IT TO THE GUIDE
4. Document problem discovery context here (what, when, symptoms)
5. Reference the guide section for the solution

**When updating patterns**:
1. Update the Playwright guide (the source)
2. Add discovery context here (not the solution)
3. Reference the guide section

---

## ⛔ NEVER Use Soft Assertions in E2E Tests (CRITICAL)

**Problem Discovered**: October 2025 - Tests passed even when features were broken
**Solution**: See Playwright guide section 11 (Common Anti-Patterns to Avoid)

---

## ⛔ NEVER Suggest Long Timeouts (10+ Minutes)

**Problem Discovered**: Throughout 2024-2025 - Agents suggested 10+ minute timeouts
**Solution**: See Playwright guide section 11 (Common Anti-Patterns to Avoid)

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
**Problem Discovered**: Throughout 2024-2025
**Solution**: See Playwright guide section 2 (Container-Compatible URL Patterns) - Pattern has evolved to use relative URLs

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
**Problem Discovered**: October 2025 - Generic selectors matched invisible mobile menu buttons
**Solution**: See Playwright guide section 5 (Mantine v7 Component Patterns)

---

## Profile Test Race Conditions (MIGRATED - October 2025)

**Problem**: Multiple tests using shared `member@witchcityrope.com` account causing data conflicts and flaky tests.
**Solution**: Create unique test user per test using database helpers.

**STATUS**: ✅ ALL 16 PROFILE TESTS MIGRATED to use unique users (October 9, 2025)

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
**Problem Discovered**: October 2025
**Solution**: See Playwright guide section 6 (React Strict Mode Testing Pattern)

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

**Problem**: E2E tests created in wrong location or nested structure causing confusion and tests being missed.

### ✅ CORRECT Pattern: Simplified Flat Structure (2025-11-24)

```bash
# ✅ CORRECT - NEW simplified structure (2025-11-24)
/apps/web/tests/              # All E2E tests (flat, organized by feature)
/apps/web/test-utils/         # Shared utilities (pages, fixtures, helpers)

# ❌ WRONG - OLD nested structure
/apps/web/tests/  # REMOVED 2025-11-24 - caused directory confusion
/tests/e2e/                  # OBSOLETE - CONSOLIDATED 2025-11-23
```

### New Structure Benefits:
- **Simple**: No nested `/tests/` confusion
- **Clear**: `/tests/` for tests, `/test-utils/` for utilities
- **Standard**: Follows Playwright industry best practices
- **Reliable**: Prevents running from wrong directory (caused 847 tests to be skipped)

### Prevention Rules

1. ✅ **ALWAYS create E2E tests in `/apps/web/tests/[feature]/`**
2. ✅ **ALWAYS put utilities in `/apps/web/test-utils/`**
3. ✅ **ALWAYS run Playwright from `/apps/web` root**
4. ❌ **NEVER nest tests under `/tests/`** (removed structure)
5. ❌ **NEVER run from inside `/tests/` directory** (will miss tests)

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

**Problem Discovered**: November 2025
**Solution**: See Playwright guide section 8 (Authentication Patterns)

---

## 🚨 CRITICAL: E2E Testing Gold Standard Patterns - e2e-events-full-journey (2025-11-17)

**Problem Discovered**: November 2025
**Solution**: See Playwright guide section 11 (Common Anti-Patterns to Avoid)

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

## Prevention Pattern: Ambiguous Label Selectors in Playwright Tests (2025-11-29)

**Problem Discovered**: November 2025
**Solution**: See Playwright guide section 11 (Common Anti-Patterns to Avoid)

---

## How to Update Patterns

**When you discover a new E2E testing pattern or issue**:

1. **Add the pattern to the Playwright guide** (`/docs/standards-processes/testing/browser-automation/playwright-guide.md`)
   - Include problem description
   - Show wrong pattern with ❌
   - Show correct pattern with ✅
   - Explain why it matters
   - Add code examples

2. **In lessons learned (this file), only document**:
   - When the problem was discovered (date)
   - What symptoms appeared
   - Reference to Playwright guide section for solution
   - Example: "**Solution**: See Playwright guide section X"

3. **NEVER duplicate the actual pattern/solution in lessons learned** - that creates maintenance burden

---

**Last Pattern Consolidation**: 2025-12-01 - E2E patterns moved to Playwright guide
