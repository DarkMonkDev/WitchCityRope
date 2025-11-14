# Test Developer Lessons Learned - Part 3

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 3 of 3**
**Part 1**: test-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Part 2**: test-developer-lessons-learned-2.md (MUST ALSO READ)
**Part 3**: test-developer-lessons-learned-3.md (THIS FILE)
**Read ALL**: Parts 1, 2, AND 3 are MANDATORY
**Write to**: Part 3 ONLY
**Max size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using lessons-learned-validator skill
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## 🚨 CRITICAL ANTI-PATTERN: Test Helpers Bypassing Production Code (2025-11-11)

**Problem**: Test helper method in SafetyServiceTests created incidents directly in database, bypassing production code path for reference number generation. This resulted in ZERO coverage of critical business logic.

**Root Cause**: Test helper generated reference numbers manually instead of calling the actual service method that implements the production algorithm.

**Impact**:
- **0% coverage** of `GenerateReferenceNumberAsync()` method
- **Missing database logic** went undetected (sequential numbering, highest sequence lookup)
- **All 15 tests** used bypassed helper - NO tests validated production code
- **Critical bug risk** - reference number collisions would only be found in production

**Anti-Pattern Example**:
```csharp
// ❌ WRONG - Test helper bypasses production code
private async Task<SafetyIncident> CreateTestIncidentAsync(...)
{
    var incident = new SafetyIncident
    {
        // Manually creates reference number - BYPASSES PRODUCTION!
        ReferenceNumber = $"SAF-{DateTime.UtcNow:yyyyMMdd}-{uniqueId}",
        Title = "Test Incident",
        // ... other properties
    };

    _context.SafetyIncidents.Add(incident);
    await _context.SaveChangesAsync();
    return incident;
}
```

**Correct Pattern - Call Production Code**:
```csharp
// ✅ CORRECT - Calls actual production code path
private async Task<SafetyIncident> CreateTestIncidentAsync(...)
{
    // Create request DTO (what real callers would use)
    var request = new CreateIncidentRequest
    {
        ReporterId = reporterId,
        IsAnonymous = isAnonymous,
        Title = $"Test Incident {uniqueId}",
        Type = type,
        WhereOccurred = WhereOccurred.AtEvent,
        IncidentDate = DateTime.UtcNow.AddDays(-1),
        Location = "Test Location",
        Description = description ?? "Test incident description..."
    };

    // Call ACTUAL service method - exercises production code!
    var result = await _sut.SubmitIncidentAsync(request);

    if (!result.IsSuccess)
    {
        throw new InvalidOperationException($"Test setup failed: {result.Error}");
    }

    // Retrieve incident from database (production code created it)
    var incident = await _context.SafetyIncidents
        .FirstAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

    // Update status if needed (for tests requiring specific status)
    if (status != IncidentStatus.ReportSubmitted)
    {
        incident.Status = status;
        await _context.SaveChangesAsync();
    }

    return incident;
}
```

**Add Dedicated Tests for Business Logic**:
```csharp
// Test 1: Validate reference number format
[Fact]
public async Task SubmitIncidentAsync_GeneratesValidReferenceNumber()
{
    var request = new CreateIncidentRequest { /* ... */ };

    var result = await _sut.SubmitIncidentAsync(request);

    // Verify format: SAF-YYYYMMDD-NNNN
    result.Value!.ReferenceNumber.Should().MatchRegex(@"^SAF-\d{8}-\d{4}$");

    // Verify contains today's date
    var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
    result.Value.ReferenceNumber.Should().Contain(todayStr);

    // Verify stored in database
    var incident = await _context.SafetyIncidents
        .FirstOrDefaultAsync(i => i.ReferenceNumber == result.Value.ReferenceNumber);
    incident.Should().NotBeNull();
}

// Test 2: Validate sequential numbering
[Fact]
public async Task SubmitIncidentAsync_GeneratesSequentialReferenceNumbers()
{
    var result1 = await _sut.SubmitIncidentAsync(CreateValidRequest());
    var result2 = await _sut.SubmitIncidentAsync(CreateValidRequest());
    var result3 = await _sut.SubmitIncidentAsync(CreateValidRequest());

    // Verify sequential: -0001, -0002, -0003
    result1.Value!.ReferenceNumber.Should().EndWith("-0001");
    result2.Value!.ReferenceNumber.Should().EndWith("-0002");
    result3.Value!.ReferenceNumber.Should().EndWith("-0003");

    // Verify all have same date prefix
    var todayPrefix = $"SAF-{DateTime.UtcNow:yyyyMMdd}-";
    result1.Value.ReferenceNumber.Should().StartWith(todayPrefix);
    result2.Value.ReferenceNumber.Should().StartWith(todayPrefix);
    result3.Value.ReferenceNumber.Should().StartWith(todayPrefix);
}
```

**Prevention Rules**:
1. ✅ **TEST HELPERS MUST CALL PRODUCTION CODE** - Never recreate business logic in test helpers
2. ✅ **USE SERVICE METHODS** - Call actual service methods (SubmitAsync, CreateAsync, etc.)
3. ✅ **ADD DEDICATED TESTS** - Create specific tests for business logic (reference numbers, calculations, etc.)
4. ✅ **VERIFY DATABASE LOGIC** - Test helpers should exercise database queries, not bypass them
5. ❌ **NEVER MANUALLY CREATE ENTITIES** - Don't instantiate domain entities directly in test setup
6. ❌ **DON'T RECREATE ALGORITHMS** - If production code has an algorithm, don't duplicate it in tests

**What Production Code Was Missed**:
```csharp
// This method had ZERO coverage because test helper bypassed it
private async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken)
{
    var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
    var prefix = $"SAF-{dateStr}-";

    // Query database for highest sequence - NEVER EXECUTED BY TESTS!
    var lastRefToday = await _context.SafetyIncidents
        .Where(i => i.ReferenceNumber.StartsWith(prefix))
        .OrderByDescending(i => i.ReferenceNumber)
        .Select(i => i.ReferenceNumber)
        .FirstOrDefaultAsync(cancellationToken);

    int nextSequence = 1;
    if (lastRefToday != null)
    {
        // Extract and increment sequence - NEVER EXECUTED BY TESTS!
        var lastSeqStr = lastRefToday.Substring(lastRefToday.Length - 4);
        if (int.TryParse(lastSeqStr, out int lastSeq))
        {
            nextSequence = lastSeq + 1;
        }
    }

    return $"{prefix}{nextSequence:D4}"; // NEVER EXECUTED BY TESTS!
}
```

**Detection Strategy**:
- **Code coverage reports** - Look for 0% coverage on critical business logic methods
- **Code reviews** - Flag test helpers that create entities directly
- **Ask**: "Is this helper calling production code or recreating its logic?"

**Files Fixed**:
- `/home/chad/repos/witchcityrope/tests/unit/api/Features/Safety/SafetyServiceTests.cs`
  - Updated `CreateTestIncidentAsync()` helper (lines 521-567)
  - Added 2 new tests for reference number validation
  - All 15 existing tests now exercise production code path

**Before Fix**:
- 15 tests, 0% coverage of reference number generation
- Database query logic never executed
- Sequential numbering logic never tested

**After Fix**:
- 17 tests (added 2 dedicated tests)
- 100% coverage of reference number generation
- All tests exercise full production code path

**Key Lesson**: Test helpers exist for CONVENIENCE, not CODE DUPLICATION. They must call production code paths, not recreate business logic. If a helper bypasses production code, you're testing a fake system, not the real one.

**Summary Document**: `/home/chad/repos/witchcityrope/test-results/safety-service-test-anti-pattern-fix-2025-11-11.md`

---

## ⛔ When to Archive vs Fix Broken Tests After Refactoring

**Problem**: Major refactorings (service renames, architecture changes) break test files. Deciding whether to fix or archive tests wastes time if done wrong.

**Date Discovered**: November 11, 2025
**Context**: Fixed 8 compilation errors from two major refactorings (ParticipationService → AttendanceService, SeedDataService → Specialized Seeders)

### Decision Tree: Archive vs Fix

**Archive When (Don't Fight It)**:
1. ✅ **Architecture fundamentally changed** - Not just renamed, but different patterns
2. ✅ **API signatures completely different** - Different parameters, return types, dependencies
3. ✅ **Tests already disabled by original developer** - Found in `.disabled/` folder = signal
4. ✅ **Would require complete rewrite** - More than 50% of test assertions need changes
5. ✅ **Business logic changed** - Tests assume old business rules that no longer apply
6. ✅ **Better to write fresh tests** - New API clearer to test from scratch

**Fix When (Worth the Effort)**:
1. ✅ **Simple rename** - Same class/method, just different name
2. ✅ **Import path changes** - Just namespace/module changes
3. ✅ **Minor signature updates** - Added optional parameter, same core logic
4. ✅ **Tests still valid** - Test scenarios still apply to new implementation
5. ✅ **Quick fixes** - Can fix in < 30 minutes

### Real Examples (November 11, 2025)

**Example 1: SeedDataService → SeedCoordinator (ARCHIVED)**
```csharp
// OLD (3,800 lines monolithic)
var service = new SeedDataService(context, userManager, roleManager, logger, encryption);
await service.SeedUsersAsync();      // Method in one big class
await service.SeedEventsAsync();     // Another method in same class

// NEW (12 specialized seeders)
var coordinator = new SeedCoordinator(
    context, userManager,
    userSeeder,      // Now separate class
    eventSeeder,     // Now separate class
    // ... 10 more seeders
);
await coordinator.SeedAllDataAsync(); // Orchestrates all seeders
```

**Why Archived**:
- ✅ Monolithic service split into 12 specialized classes
- ✅ Tests were testing specific methods (`SeedUsersAsync()`) now distributed across different classes
- ✅ Fixing would require mocking 12+ dependencies for `SeedCoordinator`
- ✅ Better to write tests for individual seeders (e.g., `UserSeederTests`, `EventSeederTests`)

**Example 2: ParticipationService → AttendanceService (ARCHIVED)**
```csharp
// OLD API
var result = await participationService.GetParticipationStatusAsync(eventId, userId);
// Returns: null if no participation
// Uses: EventParticipation entity, ParticipationType enum

// NEW API
var result = await attendanceService.GetParticipationStatusAsync(eventId, userId);
// Returns: EnhancedParticipationStatusDto with HasRSVP/HasTicket flags (never null)
// Uses: EventAttendance entity, AttendanceType enum
// Requires: VolunteerAssignmentService, ITimeZoneService (new dependencies)
```

**Why Archived**:
- ✅ Return type completely different (null vs DTO with flags)
- ✅ Entity model changed (EventParticipation → EventAttendance)
- ✅ Enums changed (ParticipationType → AttendanceType)
- ✅ New dependencies required (volunteer service, timezone service)
- ✅ Tests were already in `.disabled/` folder (original developer signal)
- ✅ Business logic changed (cutoff times, capacity calculations)
- ✅ All test assertions would need rewrite (not just find/replace)

### Archive Best Practices

**1. Rename to exclude from build**:
```bash
# Preserves code for reference but excludes from compilation
mv Test.cs Test.cs.txt
```

**2. Create comprehensive README**:
```markdown
# Archived Tests
**Archived**: YYYY-MM-DD
**Reason**: [Architecture changed / API changed / etc]

## What Changed
[Detailed explanation of refactoring]

## Why Archived (Not Fixed)
1. [Specific reason 1]
2. [Specific reason 2]

## Future Testing Strategy
[How to test this area going forward]

## Test Scenarios to Consider
[List of scenarios from archived tests]
```

**3. Document location in file registry**:
```markdown
| 2025-11-11 | /tests/.../archive/Test.cs.txt | ARCHIVED | Tests for old API | Refactoring | ARCHIVED | Reference only |
```

### Signals That Tests Should Be Archived

**Strong Signals**:
- 🚩 Tests already in `.disabled/` folder
- 🚩 Multiple classes renamed, not just one
- 🚩 Constructor requires new dependencies not in old version
- 🚩 Method signatures completely different
- 🚩 Return types fundamentally changed
- 🚩 Entity models/DTOs renamed throughout
- 🚩 Trying to fix for > 30 minutes with no progress

**Weak Signals (Still Worth Fixing)**:
- ✅ Just namespace/import changes
- ✅ Simple parameter additions (optional parameters)
- ✅ Return type same, just wrapped differently
- ✅ Can fix in < 15 minutes

### Time Investment Guidelines

**Archive Decision**:
- Investigation: 10-15 minutes
- Archive + documentation: 15-20 minutes
- Total: ~30 minutes

**Fix Decision (if wrong)**:
- Investigation: 10-15 minutes
- Attempted fix: 30-60 minutes (often incomplete)
- Realize should archive: 5 minutes
- Archive + documentation: 15-20 minutes
- Total: 60-95 minutes (wasted)

**Rule**: If not fixable in 30 minutes, stop and archive instead.

### Files Archived (November 11, 2025)

**SeedDataService Tests**:
- `/tests/unit/api/Services/_archive/SeedDataServiceTests-obsolete-2025-11-11.cs.txt`
- **Errors fixed**: 2 compilation errors

**ParticipationService Tests**:
- `/tests/unit/api/Features/Participation/_archive/ParticipationServiceTests.cs.txt` (22 tests)
- `/tests/unit/api/Features/Participation/_archive/ParticipationServiceTests_Extended.cs.txt` (20+ tests)
- `/tests/unit/api/Features/Participation/_archive/ParticipationServiceDiagnosticTest.cs.txt` (1 test)
- **Errors fixed**: 6 compilation errors

**Total Errors Fixed**: 8 compilation errors resolved
**Time Investment**: ~65 minutes (including documentation)

**Summary Document**: `/home/chad/repos/witchcityrope/session-work/2025-11-11/refactoring-test-fixes-summary.md`

**Key Lesson**: When refactorings break tests, check if tests were intentionally disabled first. If architecture fundamentally changed (not just renamed), archive tests with good documentation rather than fighting against new API assumptions. Archived tests preserve test scenarios as reference for future test development.

## Test Helper Naming Convention: Direct vs. Via Service (2025-11-11)

**Problem**: Test helpers that create entities directly in database are convenient but bypass business logic. Unclear naming makes it easy to accidentally use bypass helpers when production code should be tested.

**Solution**: Use explicit naming convention to indicate whether helper bypasses production code:

### Naming Patterns

**Bypass Helpers (Direct DB Access)**:
- `CreateTestUserDirectly()` - Bypasses user services
- `CreateTestEventDirectly()` - Bypasses event services
- `CreateTestIncidentDirectly()` - Bypasses incident services

**Production Helpers (Call Services)**:
- `CreateTestUserViaService()` - Calls UserService/UserManager
- `CreateTestEventViaService()` - Calls EventService.CreateEventAsync()
- `CreateTestIncident()` - Calls SafetyService.SubmitIncidentAsync()

### When to Use Each Pattern

**Use `*Directly()` helpers when**:
- Setting up test data for unrelated tests
- Entity creation is NOT being tested
- You need fast test setup
- Business rules don't matter for test scenario

**Use `*ViaService()` helpers when**:
- Testing features that depend on creation logic
- Validating business rules
- Testing computed values (e.g., reference numbers)
- Integration testing workflows

### Example

```csharp
// ❌ WRONG - Tests user creation but bypasses services
[Fact]
public async Task CreateUser_GeneratesUniqueId()
{
    var user = CreateTestUserDirectly(); // BYPASSES ID GENERATION
    user.Id.Should().NotBe(Guid.Empty); // FALSE CONFIDENCE
}

// ✅ CORRECT - Tests user creation via production services
[Fact]
public async Task CreateUser_GeneratesUniqueId()
{
    var user = await CreateTestUserViaService(); // CALLS PRODUCTION CODE
    user.Id.Should().NotBe(Guid.Empty); // REAL TEST
}

// ✅ ALSO CORRECT - Using bypass for unrelated test setup
[Fact]
public async Task GetUserEvents_ReturnsOnlyUserEvents()
{
    var user = CreateTestUserDirectly(); // OK - not testing user creation
    var event1 = CreateTestEventDirectly();
    // Test focuses on GetUserEvents(), not entity creation
}
```

### Migration Guide

When finding existing bypass helpers without "Directly" suffix:

1. **Rename** helper to include "Directly": `CreateTestUser()` → `CreateTestUserDirectly()`
2. **Add XML documentation** warning about bypass
3. **Update all call sites** to use new name
4. **Add production alternative**: Create `CreateTestUserViaService()` method
5. **Update tests** that need production code to use `*ViaService()` variant

### Files Updated

- DatabaseTestBase.cs: Renamed helpers, added documentation, added service alternatives
- 13 test files: Updated to use `CreateTestUserDirectly()`
- 3 test files: Updated to use `CreateTestEventDirectly()`

**Date Applied**: November 11, 2025
**Tests Affected**: 50+ tests across 16 files
**Pattern Source**: Anti-pattern analysis document, lines 107-112
**Related Lesson**: See "Test Helpers Bypassing Production Code" (lines 1665-1843)
