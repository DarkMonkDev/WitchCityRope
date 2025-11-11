# Archived Participation Service Tests

**Archived**: November 11, 2025
**Reason**: ParticipationService → AttendanceService refactoring with API changes

## Context

On November 9, 2025 (commit `f47bcffe`), the following refactoring occurred:
- `ParticipationService` → `AttendanceService`
- `EventParticipation` entity → `EventAttendance` entity
- `ParticipationType` → `AttendanceType`
- `ParticipationStatus` → `AttendanceStatus`

## Why Archived (Not Fixed)

These tests were intentionally placed in a `.disabled` folder during the refactoring, indicating they were known to be broken. After investigation, archiving was chosen over fixing because:

### 1. API Signature Changes
The new `AttendanceService` has different:
- Constructor dependencies (added `VolunteerAssignmentService`, `ITimeZoneService`)
- Method signatures
- Return types (e.g., returns `EnhancedParticipationStatusDto` with flags, not null)

### 2. Entity Model Changes
- Tests reference `EventParticipation`, `ParticipationType`, `ParticipationStatus`
- New code uses `EventAttendance`, `AttendanceType`, `AttendanceStatus`
- Legacy entities still exist but may have different behavior

### 3. Business Logic Changes
The refactoring wasn't just a rename - it included business logic changes around:
- Capacity calculations
- RSVP/Ticket handling
- Cutoff time enforcement
- Volunteer integration

### 4. Test Architecture Issues
- Tests were already disabled by original developer
- Would require complete rewrite, not just find/replace
- Better to write fresh tests against new API

## Files Archived

1. **ParticipationServiceTests.cs** (22 tests)
   - Basic RSVP creation tests
   - Capacity validation tests
   - Vetting status checks
   - Cancellation tests

2. **ParticipationServiceTests_Extended.cs** (20+ tests)
   - Ticket purchase workflows
   - Event participation management
   - Edge cases and error scenarios
   - Database constraint tests

3. **ParticipationServiceDiagnosticTest.cs** (1 diagnostic test)
   - Debugging test for RSVP failures

## Future Testing Strategy

If attendance/participation testing is needed:
1. **Write new tests** against `AttendanceService` API
2. **Use integration tests** with real database (TestContainers)
3. **Reference these archived tests** for test scenarios/edge cases
4. **Consider E2E tests** for critical user flows (already exist in `/tests/e2e/`)

## Test Scenarios to Consider (from archived tests)

- ✅ RSVP creation for vetted users
- ✅ Non-vetted users cannot RSVP
- ✅ Capacity enforcement (full events)
- ✅ RSVP only for social events (not classes)
- ✅ Ticket purchase workflows
- ✅ Ticket purchase for class events only
- ✅ Cancellation and refund handling
- ✅ Multiple participations per user (different events)
- ✅ Database unique constraints (one active participation per event)
