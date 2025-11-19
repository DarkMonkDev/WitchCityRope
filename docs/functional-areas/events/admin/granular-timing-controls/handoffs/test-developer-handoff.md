# Test Developer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: React Developer + Backend Developer -->
<!-- To: Test Developer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 CRITICAL TESTING REQUIREMENTS (MUST IMPLEMENT)

### 1. Comprehensive Unit Test Coverage (95%+ Required)
**Rule**: All timing calculation logic must have complete unit test coverage.
- ✅ Correct: Test NULL handling, positive hours, negative hours, edge cases (-24 boundary)
- ❌ Wrong: Only test happy path scenarios
- ❌ Wrong: Skip NULL handling tests

### 2. Integration Tests Must Cover All Enforcement Points
**Rule**: Verify timing enforcement at ALL API endpoints (RSVP, Ticket, Volunteer).
- ✅ Correct: Test all 6 action types (GetRsvp, CancelRsvp, GetTicket, CancelTicket, GetVolunteer, CancelVolunteer)
- ❌ Wrong: Only test RSVP enforcement
- ❌ Wrong: Skip volunteer timing tests

### 3. E2E Tests Must Validate User Workflows
**Rule**: Test complete user flows from admin configuration to user registration/cancellation.
- ✅ Correct: Admin sets timing → User attempts action → Verify correct behavior
- ❌ Wrong: Only test admin configuration
- ❌ Wrong: Only test API responses without UI validation

### 4. Migration Tests Required
**Rule**: Verify database migration correctness and backward compatibility.
- ✅ Correct: Test migration on fresh DB and DB with existing events
- ❌ Wrong: Skip migration testing
- ❌ Wrong: Don't verify NULL defaults for existing events

### 5. Timing Calculations Must Be Precise
**Rule**: Test timezone-aware timing calculations with sub-hour precision.
- ✅ Correct: Test 0.5 hour increments, timezone conversions, daylight saving
- ❌ Wrong: Only test whole hour values
- ❌ Wrong: Ignore timezone differences

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Implementation Plan | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md` | Testing Requirements section |
| Backend Developer Handoff | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/backend-developer-handoff.md` | TimeZoneService specification |
| React Developer Handoff | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/react-developer-handoff.md` | Component test requirements |
| TimeZoneService | `/apps/api/Services/TimeZoneService.cs` | IsActionAllowedAsync implementation |
| AttendanceService | `/apps/api/Features/Attendance/AttendanceService.cs` | Enforcement points |
| VolunteerService | `/apps/api/Features/Volunteers/VolunteerService.cs` | Volunteer enforcement |

## 🚨 KNOWN PITFALLS

### Pitfall 1: Not Testing NULL Handling
**Why it happens**: Assume all events have timing configured
**How to avoid**: NULL = no restriction is critical for backward compatibility - test extensively

### Pitfall 2: Forgetting Timezone Conversions
**Why it happens**: Working in UTC during testing
**How to avoid**: Events are timezone-aware - test with multiple timezones

### Pitfall 3: Not Testing Decimal Hour Values
**Why it happens**: Thinking hours are whole numbers
**How to avoid**: 0.5 = 30 minutes is supported - test precision

### Pitfall 4: Skipping Volunteer Tests
**Why it happens**: Focusing on RSVP/Tickets
**How to avoid**: Volunteers have separate timing fields - test independently

### Pitfall 5: Not Testing -24 Boundary
**Why it happens**: Edge case at maximum post-event timing
**How to avoid**: -24 is allowed, -24.1 is not - test boundary precisely

## ✅ VALIDATION CHECKLIST

Before proceeding to finalization, verify:

### Unit Tests
- [ ] TimeZoneService.IsActionAllowedAsync:
  - [ ] NULL registrationOpenHours returns true
  - [ ] NULL registrationCloseHours returns true
  - [ ] NULL cancellationOpenHours returns true
  - [ ] NULL cancellationCloseHours returns true
  - [ ] NULL volunteerRegistrationCloseHours returns true
  - [ ] NULL volunteerCancellationCloseHours returns true
  - [ ] Positive hours calculated correctly (before event)
  - [ ] Negative hours calculated correctly (after event)
  - [ ] -24 exact value allowed
  - [ ] < -24 values rejected
  - [ ] 0.5 hour increments work correctly
  - [ ] Action type routes to correct fields
- [ ] Event entity validation:
  - [ ] Timing fields accept NULL
  - [ ] Timing fields accept decimals
  - [ ] Timing fields reject < -24
- [ ] 95%+ code coverage achieved

### Integration Tests
- [ ] RSVP registration timing:
  - [ ] Allowed when within window
  - [ ] Blocked when before registrationOpenHours
  - [ ] Blocked when after registrationCloseHours
  - [ ] Allowed with NULL fields
  - [ ] Error message clear when blocked
- [ ] RSVP cancellation timing:
  - [ ] Allowed when within window
  - [ ] Blocked when before cancellationOpenHours
  - [ ] Blocked when after cancellationCloseHours
  - [ ] Allowed up to -24 hours after event
  - [ ] Allowed with NULL fields
- [ ] Ticket registration timing (uses RSVP fields)
- [ ] Ticket cancellation timing (uses RSVP cancel fields)
- [ ] Volunteer registration timing:
  - [ ] Uses volunteerRegistrationCloseHours
  - [ ] Independent from RSVP timing
  - [ ] Allowed with NULL fields
- [ ] Volunteer cancellation timing:
  - [ ] Uses volunteerCancellationCloseHours
  - [ ] Independent from RSVP timing
  - [ ] Allowed with NULL fields
  - [ ] Validates ownership
- [ ] Volunteer cancel endpoint:
  - [ ] Returns 200 on success
  - [ ] Returns 404 when not found
  - [ ] Returns 401 when not owner
  - [ ] Returns 400 when window closed
- [ ] Migration correctness:
  - [ ] 6 columns added
  - [ ] 6 constraints added
  - [ ] Existing events have NULL values
  - [ ] PreStartBufferMinutes not removed yet
- [ ] 100% integration test pass rate

### E2E Tests
- [ ] Admin timing configuration:
  - [ ] Can open RSVP/Tickets settings
  - [ ] Can set all 4 RSVP/Ticket timing fields
  - [ ] Can set all 2 Volunteer timing fields
  - [ ] Settings save correctly
  - [ ] Settings display correctly on reload
  - [ ] Validation prevents < -24 values
  - [ ] Empty fields allowed
  - [ ] Decimal values accepted (0.5)
- [ ] User RSVP timing:
  - [ ] RSVP button visible when in window
  - [ ] RSVP button disabled when outside window
  - [ ] Message shown when registration not open yet
  - [ ] Message shown when registration closed
  - [ ] RSVP works when timing NULL
- [ ] User RSVP cancellation timing:
  - [ ] Cancel button visible when in window
  - [ ] Cancel button disabled when outside window
  - [ ] Message shown when cancellation not open yet
  - [ ] Message shown when cancellation closed
  - [ ] Can cancel up to -24 hours after event
  - [ ] Cancel works when timing NULL
- [ ] User ticket timing (same as RSVP)
- [ ] User ticket cancellation timing (same as RSVP cancel)
- [ ] User volunteer timing:
  - [ ] Can signup when in window
  - [ ] Blocked from signup when after close
  - [ ] Signup works when timing NULL
  - [ ] Can cancel when in window
  - [ ] Blocked from cancel when after close
  - [ ] Cancel works when timing NULL
  - [ ] Cancel button appears
  - [ ] Cancel confirmation works
- [ ] 100% E2E test pass rate

## 🔄 DISCOVERED CONSTRAINTS

### Existing Test Infrastructure
**TestContainers**: PostgreSQL tests use real database containers
**Impact**: Migration tests can run against actual PostgreSQL
**Required**: Verify migration compatibility with PostgreSQL version

### Existing TimeZoneService Tests
**Location**: Likely needs to be created new
**Current State**: May not exist comprehensive tests
**Impact**: Create full test suite from scratch
**Required**: Follow existing service test patterns

### Playwright Test Structure
**Location**: `/tests/playwright/`
**Pattern**: Feature-based test organization
**Impact**: Create new files for timing tests
**Required**: Follow existing naming conventions

## 📊 TEST SPECIFICATION

### Unit Tests - TimeZoneService

**File**: `/tests/WitchCityRope.Core.Tests/Services/TimeZoneServiceTests.cs`

```csharp
public class TimeZoneServiceTests
{
    private readonly TimeZoneService _service;
    private readonly Mock<IConfiguration> _configMock;

    public TimeZoneServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _service = new TimeZoneService(_configMock.Object);
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullRegistrationOpenHours_ReturnsTrue()
    {
        // Arrange: Event with NULL registration open hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(7),
            registrationOpenHours: null,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.True(result, "NULL registrationOpenHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_BeforeRegistrationOpens_ReturnsFalse()
    {
        // Arrange: Registration opens 7 days before, event is 10 days away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168, // 7 days
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.False(result, "Registration should not be open yet (10 days > 7 days)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_AfterRegistrationCloses_ReturnsFalse()
    {
        // Arrange: Registration closes 1 hour before, event is 30 minutes away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(30),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.False(result, "Registration should be closed (30 min < 1 hour)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithinRegistrationWindow_ReturnsTrue()
    {
        // Arrange: Window from 7 days to 1 hour before, event is 3 days away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.True(result, "Registration should be open (3 days = 72 hours, within 168-1)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_PostEventCancellation_ReturnsTrue()
    {
        // Arrange: Cancel allowed up to 24 hours after, event was 12 hours ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(-12),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // 24 hours AFTER event
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        Assert.True(result, "Cancellation should be allowed (12 hours ago < 24 hours)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_BeyondPostEventLimit_ReturnsFalse()
    {
        // Arrange: Cancel allowed up to 24 hours after, event was 25 hours ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(-25),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        Assert.False(result, "Cancellation should be closed (25 hours > 24 hours limit)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_HalfHourIncrement_CalculatesCorrectly()
    {
        // Arrange: Registration closes 0.5 hours (30 minutes) before, event is 45 minutes away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(45),
            registrationOpenHours: 168,
            registrationCloseHours: 0.5m // 30 minutes
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.True(result, "Registration should be open (45 min > 30 min)");
    }

    [Theory]
    [InlineData(EventActionType.GetRsvp)]
    [InlineData(EventActionType.GetTicket)]
    public void IsActionAllowedAsync_RsvpAndTicket_UseSameFields(EventActionType actionType)
    {
        // Arrange: RSVP and Ticket should use same timing fields
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = _service.IsActionAllowedAsync(eventEntity, actionType).Result;

        // Assert
        Assert.True(result, $"{actionType} should use shared registration fields");
    }

    [Fact]
    public async Task IsActionAllowedAsync_VolunteerAction_UsesVolunteerFields()
    {
        // Arrange: Volunteer has different timing than RSVP
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(2),
            registrationCloseHours: 1, // RSVP closes 1 hour before
            volunteerRegistrationCloseHours: 24 // Volunteer closes 24 hours before
        );

        // Act
        var volunteerResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.GetVolunteer);
        var rsvpResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.GetRsvp);

        // Assert
        Assert.True(volunteerResult, "Volunteer should be open (2 days = 48 hours > 24)");
        Assert.True(rsvpResult, "RSVP should be open (2 days = 48 hours > 1)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithTimezone_CalculatesCorrectly()
    {
        // Arrange: Event in Pacific timezone, current time Eastern
        var pacificTime = DateTime.UtcNow.AddHours(-8); // PST
        var eventEntity = CreateTestEvent(
            startDateTime: pacificTime.AddDays(3),
            timeZoneId: "America/Los_Angeles",
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert - Should correctly convert timezones
        Assert.True(result, "Timezone conversion should be correct");
    }

    private Event CreateTestEvent(
        DateTime startDateTime,
        decimal? registrationOpenHours = null,
        decimal? registrationCloseHours = null,
        decimal? cancellationOpenHours = null,
        decimal? cancellationCloseHours = null,
        decimal? volunteerRegistrationCloseHours = null,
        decimal? volunteerCancellationCloseHours = null,
        string timeZoneId = "America/New_York")
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            StartDateTime = startDateTime,
            TimeZoneId = timeZoneId,
            RegistrationOpenHours = registrationOpenHours,
            RegistrationCloseHours = registrationCloseHours,
            CancellationOpenHours = cancellationOpenHours,
            CancellationCloseHours = cancellationCloseHours,
            VolunteerRegistrationCloseHours = volunteerRegistrationCloseHours,
            VolunteerCancellationCloseHours = volunteerCancellationCloseHours
        };
    }
}
```

### Integration Tests - Attendance Timing

**File**: `/tests/WitchCityRope.IntegrationTests/Features/Attendance/AttendanceTimingTests.cs`

```csharp
public class AttendanceTimingTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateRsvp_WithinRegistrationWindow_Succeeds()
    {
        // Arrange
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );
        var user = await CreateTestUserAsync();

        // Act
        var response = await PostAsync($"/api/events/{eventEntity.Id}/rsvp", new { });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateRsvp_BeforeRegistrationOpens_Fails()
    {
        // Arrange: Event 10 days away, registration opens 7 days before
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );
        var user = await CreateTestUserAsync();

        // Act
        var response = await PostAsync($"/api/events/{eventEntity.Id}/rsvp", new { });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("RSVP registration window is not currently open", error);
    }

    [Fact]
    public async Task CancelRsvp_PostEvent_WithinLimit_Succeeds()
    {
        // Arrange: Event was 12 hours ago, cancel allowed up to 24 hours after
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(-12),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24
        );
        var user = await CreateTestUserAsync();
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, user.Id);

        // Act
        var response = await DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ... more integration tests ...
}
```

### E2E Tests - Admin Configuration

**File**: `/tests/playwright/events/admin-timing-settings.spec.ts`

```typescript
import { test, expect } from '@playwright/test';
import { login, createTestEvent } from './helpers';

test.describe('Admin Event Timing Settings', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, 'admin@witchcityrope.com', 'Test123!');
  });

  test('should show RSVP/Tickets timing settings when clicked', async ({ page }) => {
    // Navigate to event creation
    await page.goto('/admin/events/create');

    // Click RSVP/Tickets tab
    await page.click('text=RSVP/Tickets');

    // Settings should not be visible initially
    await expect(page.locator('text=Registration Opens')).not.toBeVisible();

    // Click show settings button
    await page.click('text=Show Timing Settings');

    // Settings should now be visible
    await expect(page.locator('text=Registration Opens')).toBeVisible();
    await expect(page.locator('text=Registration Closes')).toBeVisible();
    await expect(page.locator('text=Cancellation Opens')).toBeVisible();
    await expect(page.locator('text=Cancellation Closes')).toBeVisible();
  });

  test('should save timing settings correctly', async ({ page }) => {
    // Create event with timing settings
    await page.goto('/admin/events/create');
    await page.fill('[name="title"]', 'Test Event with Timing');
    await page.fill('[name="startDateTime"]', '2025-12-01T19:00');

    // Open timing settings
    await page.click('text=RSVP/Tickets');
    await page.click('text=Show Timing Settings');

    // Set timing values
    await page.fill('input[name="registrationOpenHours"]', '168');
    await page.fill('input[name="registrationCloseHours"]', '1');
    await page.fill('input[name="cancellationOpenHours"]', '168');
    await page.fill('input[name="cancellationCloseHours"]', '-24');

    // Save event
    await page.click('button:has-text("Create Event")');

    // Verify redirect to event list
    await expect(page).toHaveURL(/\/admin\/events/);

    // Edit event and verify settings persisted
    await page.click('text=Test Event with Timing');
    await page.click('text=Edit');
    await page.click('text=RSVP/Tickets');
    await page.click('text=Show Timing Settings');

    // Verify values
    await expect(page.locator('input[name="registrationOpenHours"]')).toHaveValue('168');
    await expect(page.locator('input[name="registrationCloseHours"]')).toHaveValue('1');
    await expect(page.locator('input[name="cancellationOpenHours"]')).toHaveValue('168');
    await expect(page.locator('input[name="cancellationCloseHours"]')).toHaveValue('-24');
  });

  test('should reject values less than -24', async ({ page }) => {
    await page.goto('/admin/events/create');
    await page.click('text=RSVP/Tickets');
    await page.click('text=Show Timing Settings');

    // Try to set invalid value
    await page.fill('input[name="registrationOpenHours"]', '-25');
    await page.blur('input[name="registrationOpenHours"]');

    // Should show validation error
    await expect(page.locator('text=Cannot be more than 24 hours after')).toBeVisible();
  });

  test('should accept decimal values', async ({ page }) => {
    await page.goto('/admin/events/create');
    await page.click('text=RSVP/Tickets');
    await page.click('text=Show Timing Settings');

    // Set decimal value (0.5 = 30 minutes)
    await page.fill('input[name="registrationCloseHours"]', '0.5');
    await page.blur('input[name="registrationCloseHours"]');

    // Should accept value without error
    await expect(page.locator('input[name="registrationCloseHours"]')).toHaveValue('0.5');
  });
});
```

### E2E Tests - User Timing Flows

**File**: `/tests/playwright/events/user-timing-flows.spec.ts`

```typescript
import { test, expect } from '@playwright/test';
import { login, createEventWithTiming, advanceTime } from './helpers';

test.describe('User Event Timing Flows', () => {
  test('should block RSVP when before registration opens', async ({ page }) => {
    // Create event 10 days away, registration opens 7 days before
    const event = await createEventWithTiming({
      startDateTime: addDays(new Date(), 10),
      registrationOpenHours: 168, // 7 days
      registrationCloseHours: 1
    });

    await login(page, 'member@witchcityrope.com', 'Test123!');
    await page.goto(`/events/${event.id}`);

    // RSVP button should be disabled or show message
    await expect(page.locator('button:has-text("RSVP")')).toBeDisabled();
    await expect(page.locator('text=Registration Opens Soon')).toBeVisible();
  });

  test('should allow RSVP when within registration window', async ({ page }) => {
    // Create event 3 days away, registration 7 days to 1 hour before
    const event = await createEventWithTiming({
      startDateTime: addDays(new Date(), 3),
      registrationOpenHours: 168,
      registrationCloseHours: 1
    });

    await login(page, 'member@witchcityrope.com', 'Test123!');
    await page.goto(`/events/${event.id}`);

    // RSVP button should be enabled
    const rsvpButton = page.locator('button:has-text("RSVP")');
    await expect(rsvpButton).toBeEnabled();

    // Click RSVP
    await rsvpButton.click();

    // Should see success message
    await expect(page.locator('text=RSVP confirmed')).toBeVisible();
  });

  test('should allow cancellation up to 24 hours after event', async ({ page }) => {
    // Create event that happened 12 hours ago, cancel up to 24 hours after
    const event = await createEventWithTiming({
      startDateTime: addHours(new Date(), -12),
      cancellationOpenHours: 168,
      cancellationCloseHours: -24
    });

    await login(page, 'member@witchcityrope.com', 'Test123!');
    // User already has RSVP
    await page.goto(`/dashboard/my-events`);

    // Cancel button should be visible
    const cancelButton = page.locator(`[data-event-id="${event.id}"] button:has-text("Cancel")`);
    await expect(cancelButton).toBeEnabled();

    // Click cancel
    await cancelButton.click();

    // Confirm
    await page.click('button:has-text("Yes, Cancel")');

    // Should see success message
    await expect(page.locator('text=RSVP cancelled')).toBeVisible();
  });
});
```

## 🎯 SUCCESS CRITERIA

### Unit Test Results
**Target**: 95%+ code coverage
**Files**:
- TimeZoneService: 100% coverage
- Event entity: 100% coverage
- AttendanceService timing logic: 100% coverage
- VolunteerService timing logic: 100% coverage

**Pass Criteria**: All unit tests passing, coverage meets target

### Integration Test Results
**Target**: 100% pass rate
**Coverage**:
- RSVP registration timing (5 tests)
- RSVP cancellation timing (5 tests)
- Ticket registration timing (5 tests)
- Ticket cancellation timing (5 tests)
- Volunteer registration timing (5 tests)
- Volunteer cancellation timing (5 tests)
- Migration correctness (4 tests)

**Pass Criteria**: All integration tests passing, all scenarios covered

### E2E Test Results
**Target**: 100% pass rate
**Coverage**:
- Admin timing configuration (6 tests)
- User RSVP timing flows (6 tests)
- User cancellation timing flows (6 tests)
- User volunteer timing flows (6 tests)

**Pass Criteria**: All E2E tests passing, real workflows validated

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT skip NULL handling tests (critical for backward compatibility)
- ❌ DO NOT only test RSVP (must test Tickets and Volunteers separately)
- ❌ DO NOT skip migration tests (database correctness critical)
- ❌ DO NOT ignore timezone tests (events are timezone-aware)
- ❌ DO NOT skip decimal value tests (0.5 hour support required)
- ❌ DO NOT forget -24 boundary tests (exact edge case)

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Timing Window | Period when action is allowed | Registration 7 days before to 1 hour before |
| NULL Restriction | No timing restriction configured | NULL = any time before event |
| Post-Event Timing | Negative hours indicating after event start | -24 = 24 hours after event |
| Action Type | Category of user action needing timing check | GetRsvp, CancelRsvp, GetVolunteer, etc. |
| Integration Test | Test against real database with full stack | Verify API endpoints enforce timing |
| E2E Test | Browser-based test of complete user workflow | Admin sets timing → User attempts action |

## 🔗 NEXT AGENT INSTRUCTIONS

### Git Manager Agent
**FIRST**: Read this handoff document completely
**SECOND**: Verify all tests passing:
```bash
# Unit tests
dotnet test tests/WitchCityRope.Core.Tests/ --filter Category=TimingControls

# Integration tests
dotnet test tests/WitchCityRope.IntegrationTests/ --filter Category=TimingControls

# E2E tests
npm run test:e2e -- timing
```
**THIRD**: Review test coverage report
**FOURTH**: Read git manager handoff for finalization
**THEN**: Begin Phase 5 finalization process

## 🤝 HANDOFF CONFIRMATION

**Previous Agents**: React Developer + Backend Developer
**Previous Phase Completed**: 2025-11-18 (Implementation Complete)
**Key Finding**: Backend API and Frontend UI fully implemented with timing controls operational - ready for comprehensive test validation

**Next Agent Should Be**: Git Manager Agent (after testing complete)
**Next Phase**: Finalization (Phase 5)
**Estimated Effort**: 2 days for comprehensive test suite creation and validation

---

## Exact File Paths for Test Implementation

**Unit Test Files** (create):
- `/tests/WitchCityRope.Core.Tests/Services/TimeZoneServiceTests.cs`
- `/tests/WitchCityRope.Core.Tests/Entities/EventTimingValidationTests.cs`

**Integration Test Files** (create):
- `/tests/WitchCityRope.IntegrationTests/Features/Attendance/RsvpTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Attendance/TicketTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerCancelEndpointTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Migrations/EventTimingControlsMigrationTests.cs`

**E2E Test Files** (create):
- `/tests/playwright/events/admin-timing-settings.spec.ts`
- `/tests/playwright/events/user-rsvp-timing.spec.ts`
- `/tests/playwright/events/user-cancellation-timing.spec.ts`
- `/tests/playwright/events/user-volunteer-timing.spec.ts`

---

**This handoff document contains all information needed for comprehensive test implementation. Proceed with confidence!**
