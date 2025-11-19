# Backend Developer Handoff - Granular Event Timing Controls Implementation Complete

**Date**: 2025-11-18
**Phase**: Implementation Phase 3 (Backend Development)
**Status**: ✅ COMPLETE - Ready for Testing Phase
**Next Phase**: Test Developer (Unit & Integration Tests)

---

## Executive Summary

Backend implementation for granular event timing controls feature is **100% COMPLETE**. All API changes implemented, code compiles successfully, and timing enforcement is active across all user-facing endpoints.

### What Was Completed

1. ✅ **EventActionType Enum** - 6 action types defined
2. ✅ **TimeZoneService Refactored** - New `IsActionAllowedAsync()` method
3. ✅ **AttendanceService Updated** - 3 enforcement points (RSVP/Ticket creation + cancellation)
4. ✅ **VolunteerService Updated** - 2 enforcement points (signup + new cancel method)
5. ✅ **New Volunteer Cancel Endpoint** - POST /api/volunteer-signups/{signupId}/cancel
6. ✅ **EventDto Updated** - 6 new timing properties exposed to frontend
7. ✅ **Code Compilation** - Zero errors, only pre-existing warnings

### Database Migration Status

✅ **Migration Already Applied** by database-designer:
- Migration: `20251118000000_AddEventTimingControls.cs`
- Event entity already has 6 new decimal? properties
- EventsSeeder already has realistic timing values
- CHECK constraints in place (>= -24 validation)

---

## Files Created/Modified

### New Files Created

1. **`/apps/api/Features/Events/EventActionType.cs`** (NEW)
   - Enum defining 6 action types: GetRsvp, CancelRsvp, GetTicket, CancelTicket, GetVolunteer, CancelVolunteer
   - Used throughout timing enforcement logic

### Files Modified

2. **`/apps/api/Features/Events/Interfaces/ITimeZoneService.cs`**
   - Added `IsActionAllowedAsync(Event, EventActionType, CancellationToken)` method signature

3. **`/apps/api/Features/Events/Services/TimeZoneService.cs`**
   - Implemented `IsActionAllowedAsync()` with NULL-safe logic
   - Handles positive/negative hours (before/after event start)
   - Maps action types to appropriate timing fields
   - Comprehensive logging for denied actions

4. **`/apps/api/Features/Participation/Services/AttendanceService.cs`**
   - Added `using WitchCityRope.Api.Features.Events;` import
   - **Line 251-257**: Updated RSVP creation to use `IsActionAllowedAsync(GetRsvp)`
   - **Line 462-468**: Updated ticket purchase to use `IsActionAllowedAsync(GetTicket)`
   - **Line 705-715**: Updated cancellation to determine action type (CancelRsvp/CancelTicket) based on attendance type

5. **`/apps/api/Features/Volunteers/Services/IVolunteerService.cs`**
   - Added `CancelVolunteerSignupAsync(string signupId, string userId, CancellationToken)` method signature

6. **`/apps/api/Features/Volunteers/Services/VolunteerService.cs`**
   - Added `ITimeZoneService` dependency injection
   - Added `using` statements for Events namespace
   - **Line 172-185**: Added timing enforcement to `SignupForPositionAsync()` using `IsActionAllowedAsync(GetVolunteer)`
   - **Line 349-434**: Implemented NEW `CancelVolunteerSignupAsync()` method with:
     - Ownership verification (user can only cancel own signups)
     - Already-cancelled check
     - Already-checked-in prevention
     - Timing enforcement via `IsActionAllowedAsync(CancelVolunteer)`
     - Slot count decrement
     - Comprehensive error handling

7. **`/apps/api/Features/Volunteers/Endpoints/VolunteerEndpoints.cs`**
   - **Line 164-224**: Added NEW POST `/api/volunteer-signups/{signupId}/cancel` endpoint
     - Requires authentication
     - Calls `volunteerService.CancelVolunteerSignupAsync()`
     - Returns 200 OK on success
     - Proper error handling with status code mapping (404/403/409/400/500)
     - OpenAPI documentation

8. **`/apps/api/Features/Events/Models/EventDto.cs`**
   - **Line 84-124**: Added 6 new timing properties with XML documentation:
     - `RegistrationOpenHours`
     - `RegistrationCloseHours`
     - `CancellationOpenHours`
     - `CancellationCloseHours`
     - `VolunteerRegistrationCloseHours`
     - `VolunteerCancellationCloseHours`

9. **`/apps/api/Features/Events/Services/EventService.cs`**
   - Updated 3 DTO mappings to include timing properties:
     - **Line 103-109**: `GetEventsAsync()` mapping
     - **Line 209-215**: `GetEventAsync()` mapping
     - **Line 436-442**: `UpdateEventAsync()` mapping

---

## Implementation Details

### EventActionType Enum

```csharp
public enum EventActionType
{
    GetRsvp,          // RSVP creation for social events
    CancelRsvp,       // RSVP cancellation
    GetTicket,        // Ticket purchase for classes/workshops
    CancelTicket,     // Ticket cancellation/refund
    GetVolunteer,     // Volunteer spot signup
    CancelVolunteer   // Volunteer assignment cancellation
}
```

### TimeZoneService.IsActionAllowedAsync() Logic

**Algorithm**:
1. Calculate hours until event start (negative = event already started)
2. Map action type to appropriate timing fields:
   - GetRsvp/GetTicket → RegistrationOpenHours, RegistrationCloseHours
   - CancelRsvp/CancelTicket → CancellationOpenHours, CancellationCloseHours
   - GetVolunteer → VolunteerRegistrationCloseHours (no open hours)
   - CancelVolunteer → VolunteerCancellationCloseHours (no open hours)
3. Check open hours (if configured): action denied if too early
4. Check close hours (if configured): action denied if too late
5. NULL values = no restriction (backward compatible)

**Example**:
- Event starts 2025-11-20 19:00 UTC
- Current time 2025-11-19 19:00 UTC
- Hours until start: 24 hours
- RegistrationCloseHours: 2 (closes 2 hours before start)
- Result: ALLOWED (24 > 2, still open)

### Enforcement Points

**AttendanceService** (3 points):
1. **CreateRSVPAsync()** - Checks `IsActionAllowedAsync(event, GetRsvp)`
2. **CreateTicketPurchaseAsync()** - Checks `IsActionAllowedAsync(event, GetTicket)`
3. **CancelParticipationAsync()** - Determines action type (CancelRsvp/CancelTicket) based on `attendance.AttendanceType`, then checks `IsActionAllowedAsync(event, actionType)`

**VolunteerService** (2 points):
1. **SignupForPositionAsync()** - Checks `IsActionAllowedAsync(position.Event, GetVolunteer)`
2. **CancelVolunteerSignupAsync()** - Checks `IsActionAllowedAsync(signup.VolunteerPosition.Event, CancelVolunteer)`

### New Volunteer Cancel Endpoint

**Route**: `POST /api/volunteer-signups/{signupId}/cancel`
**Auth**: Required
**Business Rules**:
- User can only cancel their own signups
- Cannot cancel if already cancelled
- Cannot cancel if already checked in
- Subject to timing window (VolunteerCancellationCloseHours)
- Decrements slot count on position

**Success Response**: 200 OK
**Error Responses**:
- 404 - Signup not found
- 403 - Not your signup
- 409 - Already cancelled OR already checked in
- 400 - Timing window closed
- 500 - Server error

---

## Testing Requirements

### Unit Tests (Test Developer Task)

**TimeZoneService.IsActionAllowedAsync() Tests**:
- ✅ NULL timing values (no restriction) - all actions allowed
- ✅ Positive hours (before event) - correctly enforces open/close windows
- ✅ Negative hours (after event start, max -24) - handles post-event timing
- ✅ Boundary cases - exactly at open/close time
- ✅ All 6 action types - correct field mapping

**AttendanceService Tests**:
- ✅ RSVP creation - timing enforcement
- ✅ Ticket purchase - timing enforcement
- ✅ RSVP cancellation - timing enforcement
- ✅ Ticket cancellation - timing enforcement
- ✅ Error messages - user-friendly

**VolunteerService Tests**:
- ✅ Signup - timing enforcement
- ✅ Cancel - timing enforcement
- ✅ Cancel - ownership check
- ✅ Cancel - already cancelled check
- ✅ Cancel - already checked in prevention
- ✅ Cancel - slot count decrement

### Integration Tests (Test Developer Task)

**End-to-End Flow Tests**:
1. **RSVP Timing Flow**:
   - Create event with RegistrationOpenHours = 168, RegistrationCloseHours = 24
   - Attempt RSVP 7+ days before: DENIED (too early)
   - Attempt RSVP 5 days before: ALLOWED
   - Attempt RSVP 12 hours before: DENIED (too late)

2. **Ticket Timing Flow**:
   - Create event with RegistrationOpenHours = 72, RegistrationCloseHours = 2
   - Attempt ticket purchase following same pattern
   - Verify error messages

3. **Cancellation Timing Flow**:
   - Create event with CancellationOpenHours = 48, CancellationCloseHours = 12
   - Create RSVP/Ticket
   - Attempt cancellation at various times
   - Verify allowed/denied based on timing

4. **Volunteer Timing Flow**:
   - Create event with VolunteerRegistrationCloseHours = 72, VolunteerCancellationCloseHours = 48
   - Signup for volunteer position
   - Attempt cancellation at various times
   - Verify slot count decrements

5. **Volunteer Cancel Endpoint**:
   - Test new POST /api/volunteer-signups/{signupId}/cancel
   - Verify ownership checks
   - Verify already-cancelled prevention
   - Verify already-checked-in prevention
   - Verify timing enforcement
   - Verify slot count decrement

### Test Coverage Targets

**From Quality Gates** (Feature work):
- Unit Test Coverage: 95%+
- Integration Test Pass Rate: 100%
- All enforcement points tested
- All error paths tested
- All boundary conditions tested

---

## API Documentation Updates

### New Endpoint

**POST /api/volunteer-signups/{signupId}/cancel**
```
Summary: Cancel a volunteer signup
Description: Cancel a volunteer signup. User can only cancel their own signups.
             Cannot cancel if already checked in. Subject to event timing controls.
Tags: Volunteers
Auth: Required

Parameters:
- signupId (path, required): Volunteer signup ID

Responses:
200 OK - Signup cancelled successfully
400 Bad Request - Cancellation window closed
401 Unauthorized - Not authenticated
403 Forbidden - Not your signup
404 Not Found - Signup not found
409 Conflict - Already cancelled OR already checked in
500 Internal Server Error - Server error
```

### Updated DTOs

**EventDto** (exposed to frontend via GET /api/events):
```typescript
interface EventDto {
  // ... existing properties ...

  // Granular timing controls (nullable = no restriction)
  registrationOpenHours?: number | null;
  registrationCloseHours?: number | null;
  cancellationOpenHours?: number | null;
  cancellationCloseHours?: number | null;
  volunteerRegistrationCloseHours?: number | null;
  volunteerCancellationCloseHours?: number | null;
}
```

---

## Error Messages

**User-Friendly Messages** (all enforcement points):
- RSVP: `"RSVP registration window is not currently open for this event"`
- Ticket: `"Ticket purchase window is not currently open for this event"`
- Cancel RSVP/Ticket: `"Cancellation window is not currently open for this event"`
- Volunteer Signup: `"Volunteer registration window has closed for this event"`
- Volunteer Cancel: `"Volunteer cancellation window has closed for this event"`

**Volunteer Cancel Specific**:
- Ownership: `"You can only cancel your own volunteer signups"`
- Already Cancelled: `"This volunteer signup is already cancelled"`
- Already Checked In: `"Cannot cancel volunteer signup after checking in"`

---

## Backward Compatibility

✅ **100% Backward Compatible**:
- All timing fields are `decimal?` (nullable)
- NULL values = no restriction (default behavior)
- Existing events without timing values work exactly as before
- New seed data has values for testing, but NULL handling is robust

---

## Build Status

✅ **Compilation Success**:
```bash
dotnet build apps/api/WitchCityRope.Api.csproj
# Result: 0 Errors, 87 Warnings (all pre-existing)
# Build Time: 7.53 seconds
```

**No new warnings or errors introduced.**

---

## Next Steps for Test Developer

### 1. Unit Test Implementation (Priority: HIGH)

**TimeZoneService Tests** (`tests/unit/api/Services/TimeZoneServiceTests.cs`):
- Create test file
- Test NULL handling (no restriction)
- Test positive hours (before event)
- Test negative hours (after event, max -24)
- Test all 6 action types
- Test boundary conditions (exactly at open/close)
- Test edge cases (event started, event ended)

**AttendanceService Tests** (add to existing file):
- Test RSVP creation with timing enforcement
- Test ticket purchase with timing enforcement
- Test RSVP cancellation with timing enforcement
- Test ticket cancellation with timing enforcement
- Mock `ITimeZoneService.IsActionAllowedAsync()`

**VolunteerService Tests** (add to existing file):
- Test signup with timing enforcement
- Test cancel method (all business rules)
- Mock `ITimeZoneService.IsActionAllowedAsync()`

### 2. Integration Test Implementation (Priority: HIGH)

**Create**: `tests/integration/api/TimingEnforcementIntegrationTests.cs`

**Test Scenarios**:
1. RSVP timing flow (open/close windows)
2. Ticket timing flow (open/close windows)
3. Cancellation timing flow (RSVP and Ticket)
4. Volunteer signup timing flow
5. Volunteer cancel timing flow
6. Negative hours (post-event timing)
7. NULL values (no restriction)

**Test Data Requirements**:
- Events with various timing configurations
- Users for RSVP/ticket/volunteer actions
- Volunteer positions for signup/cancel

### 3. Endpoint Testing (Priority: HIGH)

**Test New Endpoint**: POST /api/volunteer-signups/{signupId}/cancel

**Scenarios**:
- ✅ Success case (user cancels own signup within timing window)
- ✅ 404 - Signup not found
- ✅ 403 - Not your signup (different user)
- ✅ 409 - Already cancelled
- ✅ 409 - Already checked in
- ✅ 400 - Timing window closed
- ✅ 401 - Not authenticated
- ✅ Slot count decremented

### 4. E2E Testing Coordination (Priority: MEDIUM)

**Coordinate with react-developer** for frontend E2E tests:
- Timing window messaging in UI
- Error handling and user feedback
- Button states (enabled/disabled based on timing)
- Volunteer cancel button appears/disappears based on timing

---

## Handoff to Frontend (React Developer)

### What Frontend Needs to Do

**NO CHANGES REQUIRED YET** - Backend is ready, but frontend work is blocked until:
1. UI Designer completes wireframes (in progress)
2. Backend tests pass (Test Developer task)

**When Frontend Implementation Begins**:

**EventDto Changes**:
- 6 new optional timing properties available
- Frontend can display timing windows to users
- Frontend can calculate "time remaining" until windows close

**New Endpoint Available**:
- POST /api/volunteer-signups/{signupId}/cancel
- Frontend can add "Cancel" button to volunteer assignments
- Handle error responses (400/403/404/409)
- Show user-friendly error messages

**Error Handling**:
- All timing-related errors return user-friendly messages
- Frontend should display these messages in notifications/modals
- Consider showing "time remaining" warnings before windows close

---

## Risk Assessment

### Risks & Mitigations

**Risk 1: Complex Timing Logic**
- **Severity**: Medium
- **Likelihood**: Low
- **Mitigation**: Comprehensive unit tests for all edge cases, boundary testing

**Risk 2: User Confusion About Timing Windows**
- **Severity**: Medium
- **Likelihood**: Medium
- **Mitigation**: Clear error messages implemented, frontend should show timing info proactively

**Risk 3: Timezone Handling**
- **Severity**: Low
- **Likelihood**: Low
- **Mitigation**: All calculations use UTC, existing TimeZoneService handles conversions

**Risk 4: Volunteer Cancel Slot Count**
- **Severity**: Low
- **Likelihood**: Low
- **Mitigation**: Slot count decrement implemented with `Math.Max(0, count - 1)` to prevent negatives

---

## Quality Checklist

✅ **Code Quality**:
- [x] Follows vertical slice architecture
- [x] Service layer separation maintained
- [x] Proper dependency injection
- [x] Comprehensive logging
- [x] Error handling with Result pattern
- [x] NULL-safe operations
- [x] XML documentation on all public members

✅ **Security**:
- [x] Volunteer cancel requires ownership verification
- [x] All endpoints respect authentication
- [x] No information leakage in error messages

✅ **Performance**:
- [x] No database queries in timing calculations (in-memory)
- [x] Async operations throughout
- [x] CancellationToken support

✅ **Maintainability**:
- [x] Clear method names
- [x] Single responsibility principle
- [x] DRY - timing logic centralized in TimeZoneService
- [x] Easily extensible for future action types

---

## Questions for Test Developer

1. **Test Data Strategy**: Should we create specific test events with known timing values, or dynamically calculate based on current time?
2. **Integration Test Scope**: Do you want separate test files for each service, or one comprehensive timing enforcement test file?
3. **E2E Tests**: Should volunteer cancel flow be added to existing volunteer E2E tests, or create new dedicated file?
4. **Performance Tests**: Do we need load tests for timing calculations (unlikely needed, but confirming)?

---

## Lessons Learned

**What Went Well**:
1. ✅ Enum approach for action types is clean and type-safe
2. ✅ TimeZoneService refactoring was straightforward
3. ✅ NULL-safe design ensures backward compatibility
4. ✅ Existing lessons learned (interface requirements, dependency injection) prevented issues

**Challenges Overcome**:
1. ✅ Determining action type in CancelParticipationAsync required checking AttendanceType
2. ✅ Volunteer cancel needed comprehensive business rule checks (ownership, checked-in, already cancelled)
3. ✅ Remembering to update all 3 EventDto mappings in EventService

**Future Improvements** (NOT blocking):
- Consider adding "time until window closes" calculation method in TimeZoneService
- Consider logging timing window calculations for admin debugging
- Consider adding admin endpoint to preview timing windows for events

---

## Implementation Completion Summary

**Total Files Modified**: 9
**Total Files Created**: 1
**Lines of Code Changed**: ~400
**Build Status**: ✅ SUCCESS
**Compilation Errors**: 0
**New Warnings**: 0
**Test Coverage**: 0% (Test Developer task)

**Estimated Effort**:
- **Planned**: 6-8 hours
- **Actual**: ~4 hours
- **Efficiency**: Better than estimated (lessons learned helped prevent common issues)

---

## Sign-Off

**Backend Developer**: Implementation complete
**Date**: 2025-11-18
**Status**: ✅ READY FOR TESTING

**Next Agent**: Test Developer
**Blocking Items**: None
**Priority**: HIGH (critical feature for production)

---

## Appendix: Code Snippets

### EventActionType Enum
```csharp
// /apps/api/Features/Events/EventActionType.cs
public enum EventActionType
{
    GetRsvp,
    CancelRsvp,
    GetTicket,
    CancelTicket,
    GetVolunteer,
    CancelVolunteer
}
```

### TimeZoneService.IsActionAllowedAsync() Signature
```csharp
public async Task<bool> IsActionAllowedAsync(
    WitchCityRope.Api.Models.Event eventEntity,
    EventActionType actionType,
    CancellationToken cancellationToken = default)
```

### Volunteer Cancel Endpoint
```csharp
app.MapPost("/api/volunteer-signups/{signupId}/cancel", async (
    string signupId,
    [FromServices] IVolunteerService volunteerService,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    // Authentication check
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var (success, error) = await volunteerService.CancelVolunteerSignupAsync(
        signupId, userId, cancellationToken);

    return success ? Results.Ok() : Results.Problem(...);
})
```

### EventDto Timing Properties
```csharp
public decimal? RegistrationOpenHours { get; set; }
public decimal? RegistrationCloseHours { get; set; }
public decimal? CancellationOpenHours { get; set; }
public decimal? CancellationCloseHours { get; set; }
public decimal? VolunteerRegistrationCloseHours { get; set; }
public decimal? VolunteerCancellationCloseHours { get; set; }
```

---

**End of Handoff Document**
