# Session-Based Timing Test Summary

**Created**: 2025-11-30
**Last Updated**: 2025-12-01
**Specification**: `/docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md`

---

## Test Coverage Overview

### Backend Tests (Integration)

| Test File | Tests | Status | Description |
|-----------|-------|--------|-------------|
| `SessionBasedVolunteerTimingTests.cs` | 4 | PASSING | Session-based volunteer timing enforcement |
| `TimeZoneServiceTests.cs` (session methods) | 18 | PASSING | UTC conversion and session timing calculations |
| `SessionDeletionTests.cs` | 9 | PASSING | Session deletion with dependency checks |
| `TicketTypeDeletionTests.cs` | 9 | PASSING | Ticket type deletion with dependency checks |

### E2E Tests (Playwright)

| Test File | Tests | Status | Description |
|-----------|-------|--------|-------------|
| `session-based-timing.spec.ts` | 5 | ACTIVE | Edge cases for session-based timing |
| `session-based-ticket-timing.spec.ts` | 7 | ACTIVE | Ticket timing from user perspective |
| `session-based-volunteer-timing.spec.ts` | 7 | ACTIVE | Volunteer timing from user perspective |

---

## Key Scenarios Tested

### 1. Ticket Timing (Session-Based)

**Core Rule**: Multi-session tickets use FIRST FUTURE session for timing calculations.

| Scenario | Test Location | Expected Behavior |
|----------|---------------|-------------------|
| Multi-session ticket, Session 1 passed | Integration | Timing uses Session 2 |
| All sessions passed | Integration | Ticket unavailable |
| Single-session ticket | Integration | Uses that session's timing |
| Sales window open check | E2E | Purchase button enabled |
| Sales window closed | E2E | Shows "sales closed" message |
| Cancellation timing | E2E + Integration | Based on first future session |

### 2. Volunteer Timing (Session-Based)

**Core Rule**: Session-specific positions use their session; event-wide positions use earliest future session.

| Scenario | Test Location | Expected Behavior |
|----------|---------------|-------------------|
| Session-specific position | Integration + E2E | Uses assigned session's timing |
| Past session position | Integration | NOT returned in API results |
| Event-wide position (no SessionId) | Integration | Uses earliest future session |
| Signup window check | E2E | Button enabled/disabled correctly |
| Cancellation timing | Integration | Based on reference session |

### 3. UTC Conversion

**Core Rule**: All times stored as true UTC, converted on display using global timezone setting.

| Scenario | Test Location | Expected Behavior |
|----------|---------------|-------------------|
| Local to UTC conversion | Unit | 6 PM EST → 23:00 UTC |
| UTC to Local display | Unit | 23:00 UTC → 6 PM EST |
| DST handling | Unit | Correct offset during EDT/EST |
| Session time save | E2E | Form saves correct UTC |

---

## Test File Details

### `session-based-timing.spec.ts` (Edge Cases)

1. **multi-session event - tickets available for future sessions**
   - Finds seed data multi-session events
   - Verifies ticket options section visible
   - Confirms tickets can be purchased when sessions are future

2. **event with registration window settings**
   - Admin views event with timing settings
   - Verifies RegistrationOpenHours/CloseHours fields accessible

3. **volunteer positions respect session timing**
   - Member views event with volunteer opportunities
   - Confirms positions displayed based on session timing

4. **ticket purchase uses session-based timing**
   - Member browses events with ticket options
   - Verifies timing affects purchase availability

5. **admin can view session-based timing settings**
   - Admin accesses event form
   - Confirms timing configuration visible

### `session-based-ticket-timing.spec.ts` (Ticket Timing)

1. **multi-session event shows tickets for future sessions only**
2. **event with all sessions passed shows no tickets**
3. **ticket shows reference session name**
4. **unavailable ticket shows availability message**
5. **ticket sales window based on registration hours settings**
6. **multi-session ticket shows all future sessions**
7. **ticket cancellation uses session-based timing**

### `session-based-volunteer-timing.spec.ts` (Volunteer Timing)

1. **session-specific volunteer position shows for future session**
2. **past session volunteer position is hidden**
3. **volunteer signup shows session name**
4. **event-wide position available after first session passes**
5. **volunteer cancellation respects session timing**
6. **session-specific position timing is independent from other sessions**
7. **volunteer timing respects VolunteerRegistrationCloseHours setting**

### `SessionBasedVolunteerTimingTests.cs` (Integration)

1. **VolunteerSignup_SessionSpecific_UsesSessionTiming**
   - Position assigned to future session
   - Verifies CanSignUp = true when beyond VolunteerRegistrationCloseHours

2. **VolunteerSignup_PastSession_NotReturned**
   - Position assigned to past session
   - Verifies position NOT in API results

3. **VolunteerSignup_EventWide_UsesEarliestFutureSession**
   - Position with SessionId = null
   - Session 1 past, Session 2 future
   - Verifies timing uses Session 2

4. **VolunteerCancel_UsesSessionTiming**
   - Volunteer signup cancellation
   - Verifies based on session timing not Event.StartDate

---

## Running Tests

### Backend Integration Tests
```bash
# All session timing tests
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "FullyQualifiedName~SessionBased"

# Volunteer timing only
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "FullyQualifiedName~VolunteerTiming"
```

### E2E Tests

Use the `test-catalog-updater` skill or run Playwright tests for files matching `session-based*.spec.ts`.

---

## Known Limitations

1. **Seed Data Dependency**: E2E tests rely on seed data having multi-session events
   - Events: "Rope Fundamentals Intensive", "Suspension Basics", "Advanced Suspension"
   - Tests skip gracefully if data not found

2. **Past Session Testing**: Difficult to test "all sessions passed" without time manipulation
   - Backend tests handle this by creating events with past dates
   - E2E tests look for existing past events (rare in seed data)

3. **UI Implementation Gaps**: Some tests check for UI elements that may not be fully implemented
   - Session badges on volunteer positions
   - "Valid for: Session X" on multi-session tickets
   - Tests log warnings rather than fail for missing UI

---

## Related Documentation

- **Specification**: `/docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md`
- **Test Scenarios**: `/tests/e2e/session-based-timing-SCENARIOS.md`
- **Implementation Guide**: `/docs/functional-areas/events/session-timing-refactor/`
