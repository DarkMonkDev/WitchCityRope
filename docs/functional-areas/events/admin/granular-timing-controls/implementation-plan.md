# Granular Event Timing Controls - Implementation Plan
<!-- Last Updated: 2025-11-18 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Active -->

## Feature Summary

Replace the global "Pre-Start Buffer" system setting with per-event, granular timing controls for registration and cancellation windows. This provides event organizers with fine-grained control over when attendees can register for/cancel RSVPs, tickets, and volunteer spots.

## Business Value

### Current Pain Points
- **Global Setting Limitations**: Single `PreStartBufferMinutes` applies to ALL events, preventing per-event customization
- **Inflexible Windows**: Cannot set different registration vs cancellation windows
- **No Volunteer Controls**: Volunteer registration timing not enforced at all
- **Poor User Experience**: Users confused when registration closes at arbitrary times

### Solution Benefits
- **Per-Event Flexibility**: Each event can have custom timing windows
- **Independent Windows**: Registration and cancellation windows can be set independently
- **Complete Coverage**: RSVP, Tickets, and Volunteers all have timing enforcement
- **Better UX**: Clear, predictable timing rules improve user experience
- **Event Organizer Control**: Admins control timing per event needs

## User Decisions from Clarifying Questions

### 1. Window Independence
**Decision**: Registration and cancellation windows are completely independent.
- Registration can close while cancellation is still open
- Cancellation can close while registration is still open
- Any combination of values allowed

**Example**:
```
Registration Opens: -168 hours (7 days before)
Registration Closes: -1 hour (1 hour before)
Cancellation Opens: -168 hours (7 days before)
Cancellation Closes: 24 hours (day after event - yes, AFTER!)
```

### 2. Post-Event Limits
**Decision**: Fixed maximum of **-24 hours** (users can register/cancel up to 24 hours AFTER event start).

**Rationale**:
- Allows for "day-after" cancellations for unexpected situations
- Prevents indefinite post-event modifications
- Reasonable business rule for community events

**Validation**: All negative values must be >= -24

### 3. RSVP/Ticket Settings
**Decision**: Shared settings - one set of windows applies to both RSVPs and Tickets.

**Database Fields**:
- `RegistrationOpenHours` - applies to RSVP AND Ticket registration
- `RegistrationCloseHours` - applies to RSVP AND Ticket registration
- `CancellationOpenHours` - applies to RSVP AND Ticket cancellation
- `CancellationCloseHours` - applies to RSVP AND Ticket cancellation

**Separate Fields**:
- `VolunteerRegistrationCloseHours` - volunteer signup only
- `VolunteerCancellationCloseHours` - volunteer cancel only

### 4. Time Units
**Decision**: Hours only (decimal allowed, e.g., 0.5 for 30 minutes).

**UI Component**: Mantine NumberInput with:
- `step={0.5}` - 30-minute increments
- `precision={1}` - one decimal place
- `min={-24}` - post-event maximum
- `max={8760}` - one year before (reasonable maximum)

## Database Schema Changes

### Events Table - New Columns

Add 6 new nullable decimal columns to `Events` table:

```sql
ALTER TABLE Events
  ADD COLUMN RegistrationOpenHours DECIMAL(7,1) NULL,
  ADD COLUMN RegistrationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN CancellationOpenHours DECIMAL(7,1) NULL,
  ADD COLUMN CancellationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN VolunteerRegistrationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN VolunteerCancellationCloseHours DECIMAL(7,1) NULL;

COMMENT ON COLUMN Events.RegistrationOpenHours IS 'Hours before/after event start when RSVP/Ticket registration opens. Positive=before, Negative=after (max -24). NULL=any time before event';
COMMENT ON COLUMN Events.RegistrationCloseHours IS 'Hours before/after event start when RSVP/Ticket registration closes. Positive=before, Negative=after (max -24). NULL=until event starts';
COMMENT ON COLUMN Events.CancellationOpenHours IS 'Hours before/after event start when RSVP/Ticket cancellation opens. Positive=before, Negative=after (max -24). NULL=any time before event';
COMMENT ON COLUMN Events.CancellationCloseHours IS 'Hours before/after event start when RSVP/Ticket cancellation closes. Positive=before, Negative=after (max -24). NULL=until event starts';
COMMENT ON COLUMN Events.VolunteerRegistrationCloseHours IS 'Hours before/after event start when volunteer signup closes. Positive=before, Negative=after (max -24). NULL=until event starts';
COMMENT ON COLUMN Events.VolunteerCancellationCloseHours IS 'Hours before/after event start when volunteer cancel closes. Positive=before, Negative=after (max -24). NULL=until event starts';
```

### Default Values & Nullability
- **All fields nullable**: NULL = no restriction (default behavior)
- **No database defaults**: Application-level defaults only
- **Migration strategy**: Existing events get NULL (no restrictions)

### Validation Constraints
Add check constraints to enforce business rules:

```sql
ALTER TABLE Events
  ADD CONSTRAINT CK_Events_RegistrationOpenHours
    CHECK (RegistrationOpenHours IS NULL OR RegistrationOpenHours >= -24),
  ADD CONSTRAINT CK_Events_RegistrationCloseHours
    CHECK (RegistrationCloseHours IS NULL OR RegistrationCloseHours >= -24),
  ADD CONSTRAINT CK_Events_CancellationOpenHours
    CHECK (CancellationOpenHours IS NULL OR CancellationOpenHours >= -24),
  ADD CONSTRAINT CK_Events_CancellationCloseHours
    CHECK (CancellationCloseHours IS NULL OR CancellationCloseHours >= -24),
  ADD CONSTRAINT CK_Events_VolunteerRegistrationCloseHours
    CHECK (VolunteerRegistrationCloseHours IS NULL OR VolunteerRegistrationCloseHours >= -24),
  ADD CONSTRAINT CK_Events_VolunteerCancellationCloseHours
    CHECK (VolunteerCancellationCloseHours IS NULL OR VolunteerCancellationCloseHours >= -24);
```

### Settings Table - Deprecate Old Setting
Migration should remove the obsolete global setting:

```sql
DELETE FROM Settings WHERE Key = 'PreStartBufferMinutes';
```

**Note**: This is a BREAKING CHANGE. All existing code using this setting must be updated first.

## API Endpoint Changes

### 1. New Endpoint: User Volunteer Cancellation

**Endpoint**: `POST /api/volunteer-signups/{signupId}/cancel`
**Purpose**: Allow users to cancel their own volunteer assignments
**Authorization**: Authenticated users, must own the signup

**Request**: No body required (signupId in route)
**Response**:
- 200 OK: Cancellation successful
- 404 Not Found: Signup doesn't exist or not owned by user
- 400 Bad Request: Cancellation window closed

**Implementation Location**: `/apps/api/Features/Volunteers/VolunteerEndpoints.cs`

### 2. Updated Service: TimeZoneService

**Current Method**:
```csharp
Task<bool> IsRegistrationOpenAsync(Event eventEntity)
```

**New Method Signature**:
```csharp
Task<bool> IsActionAllowedAsync(Event eventEntity, EventActionType actionType)

public enum EventActionType
{
    GetRsvp,        // RSVP creation
    CancelRsvp,     // RSVP cancellation
    GetTicket,      // Ticket purchase
    CancelTicket,   // Ticket cancellation
    GetVolunteer,   // Volunteer signup
    CancelVolunteer // Volunteer cancel
}
```

**Logic Changes**:
- Replace global `PreStartBufferMinutes` check with per-event field checks
- Use appropriate event field based on action type
- NULL fields = no restriction (return true)
- Calculate hours until/since event start
- Compare against configured limits

**File Location**: `/apps/api/Services/TimeZoneService.cs`

### 3. Updated DTOs

**EventDto** must include new fields:

```csharp
public class EventDto
{
    // ... existing fields ...

    public decimal? RegistrationOpenHours { get; set; }
    public decimal? RegistrationCloseHours { get; set; }
    public decimal? CancellationOpenHours { get; set; }
    public decimal? CancellationCloseHours { get; set; }
    public decimal? VolunteerRegistrationCloseHours { get; set; }
    public decimal? VolunteerCancellationCloseHours { get; set; }
}
```

**File Location**: `/apps/api/Features/Events/Models/EventDto.cs`

### 4. Updated Enforcement Points

**AttendanceService.cs** (lines 251-257, 462-468, 695-710):
```csharp
// OLD:
if (!await _timeZoneService.IsRegistrationOpenAsync(eventEntity))
    throw new InvalidOperationException("Registration is closed");

// NEW:
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp))
    throw new InvalidOperationException("RSVP registration window is closed");
```

**VolunteerService.cs** (NEW enforcement):
```csharp
// Add to volunteer signup method:
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetVolunteer))
    throw new InvalidOperationException("Volunteer registration window is closed");

// Add to new volunteer cancel method:
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.CancelVolunteer))
    throw new InvalidOperationException("Volunteer cancellation window is closed");
```

## UI Component Changes

### 1. RSVP/Tickets Tab Settings Section

**Location**: `/apps/web/src/features/events/components/EventForm.tsx`
**Placement**: Top-right of "RSVP/Tickets" tab title (inline with heading)

**Component Structure**:
```tsx
<Group justify="space-between" mb="md">
  <Title order={3}>RSVP/Tickets</Title>
  <Button
    variant="subtle"
    leftSection={<IconSettings />}
    onClick={() => setShowRsvpSettings(!showRsvpSettings)}
  >
    Timing Settings
  </Button>
</Group>

{showRsvpSettings && (
  <Paper p="md" mb="md" withBorder>
    <Title order={5} mb="sm">Registration & Cancellation Windows</Title>
    <Grid>
      <Grid.Col span={6}>
        <NumberInput
          label="Registration Opens (hours before event)"
          description="Leave empty for any time before event"
          placeholder="e.g., 168 = 1 week"
          value={form.values.registrationOpenHours}
          onChange={(val) => form.setFieldValue('registrationOpenHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
      <Grid.Col span={6}>
        <NumberInput
          label="Registration Closes (hours before event)"
          description="Leave empty for until event starts"
          placeholder="e.g., 1 = 1 hour before"
          value={form.values.registrationCloseHours}
          onChange={(val) => form.setFieldValue('registrationCloseHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
      <Grid.Col span={6}>
        <NumberInput
          label="Cancellation Opens (hours before event)"
          description="Leave empty for any time before event"
          placeholder="e.g., 168 = 1 week"
          value={form.values.cancellationOpenHours}
          onChange={(val) => form.setFieldValue('cancellationOpenHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
      <Grid.Col span={6}>
        <NumberInput
          label="Cancellation Closes (hours before event)"
          description="Leave empty for until event starts"
          placeholder="e.g., -24 = 24 hours after event"
          value={form.values.cancellationCloseHours}
          onChange={(val) => form.setFieldValue('cancellationCloseHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
    </Grid>
  </Paper>
)}
```

### 2. Volunteers Tab Settings Section

**Location**: `/apps/web/src/features/events/components/EventForm.tsx`
**Placement**: Top-right of "Volunteers" tab title (inline with heading)

**Component Structure**:
```tsx
<Group justify="space-between" mb="md">
  <Title order={3}>Volunteers</Title>
  <Button
    variant="subtle"
    leftSection={<IconSettings />}
    onClick={() => setShowVolunteerSettings(!showVolunteerSettings)}
  >
    Timing Settings
  </Button>
</Group>

{showVolunteerSettings && (
  <Paper p="md" mb="md" withBorder>
    <Title order={5} mb="sm">Volunteer Timing Windows</Title>
    <Grid>
      <Grid.Col span={6}>
        <NumberInput
          label="Volunteer Signup Closes (hours before event)"
          description="Leave empty for until event starts"
          placeholder="e.g., 24 = 1 day before"
          value={form.values.volunteerRegistrationCloseHours}
          onChange={(val) => form.setFieldValue('volunteerRegistrationCloseHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
      <Grid.Col span={6}>
        <NumberInput
          label="Volunteer Cancel Closes (hours before event)"
          description="Leave empty for until event starts"
          placeholder="e.g., 48 = 2 days before"
          value={form.values.volunteerCancellationCloseHours}
          onChange={(val) => form.setFieldValue('volunteerCancellationCloseHours', val)}
          step={0.5}
          precision={1}
          min={-24}
          max={8760}
          allowNegative
        />
      </Grid.Col>
    </Grid>
  </Paper>
)}
```

### 3. User Volunteer Cancellation UI

**Location**: `/apps/web/src/features/events/components/UserVolunteerShifts.tsx`
**Placement**: Add cancel button next to each volunteer assignment

**Component Changes**:
```tsx
<Button
  variant="subtle"
  color="red"
  size="xs"
  onClick={() => handleVolunteerCancel(signup.id)}
  disabled={!canCancelVolunteer(signup)}
>
  Cancel Signup
</Button>
```

**API Integration**:
```typescript
// volunteerApi.ts
export const cancelVolunteerSignup = async (signupId: string): Promise<void> => {
  await apiClient.post(`/api/volunteer-signups/${signupId}/cancel`);
};
```

### Form Validation Rules

**Client-side validation** (Mantine form):
```typescript
const form = useForm({
  validate: {
    registrationOpenHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
    registrationCloseHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
    cancellationOpenHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
    cancellationCloseHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
    volunteerRegistrationCloseHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
    volunteerCancellationCloseHours: (value) =>
      value !== null && value < -24 ? 'Cannot be more than 24 hours after event' : null,
  }
});
```

## Testing Requirements

### Unit Tests

**TimeZoneService Tests** (`/tests/WitchCityRope.Core.Tests/Services/TimeZoneServiceTests.cs`):
- ✅ Registration opens: NULL field = always allowed
- ✅ Registration opens: Positive hours = before event only
- ✅ Registration opens: Negative hours = after event allowed
- ✅ Registration closes: NULL field = until event starts
- ✅ Registration closes: -24 max validation
- ✅ Cancellation windows: Independent from registration
- ✅ Volunteer windows: Separate from RSVP/Ticket
- ✅ Action type routing: Correct field used per action

**Event Entity Validation Tests** (`/tests/WitchCityRope.Core.Tests/Entities/EventTests.cs`):
- ✅ Timing fields accept NULL
- ✅ Timing fields accept decimals (0.5)
- ✅ Timing fields reject < -24
- ✅ Timing fields accept positive values

### Integration Tests

**RSVP Registration Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Attendance/RsvpRegistrationTests.cs`):
- ✅ RSVP creation allowed when within registration window
- ✅ RSVP creation blocked when before registration opens
- ✅ RSVP creation blocked when after registration closes
- ✅ RSVP creation allowed with NULL registration fields

**RSVP Cancellation Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Attendance/RsvpCancellationTests.cs`):
- ✅ RSVP cancel allowed when within cancellation window
- ✅ RSVP cancel blocked when before cancellation opens
- ✅ RSVP cancel blocked when after cancellation closes
- ✅ RSVP cancel allowed up to -24 hours after event
- ✅ RSVP cancel allowed with NULL cancellation fields

**Ticket Registration Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Attendance/TicketRegistrationTests.cs`):
- ✅ Ticket purchase uses same windows as RSVP
- ✅ Ticket purchase blocked outside registration window
- ✅ Ticket purchase allowed with NULL registration fields

**Ticket Cancellation Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Attendance/TicketCancellationTests.cs`):
- ✅ Ticket cancel uses same windows as RSVP cancel
- ✅ Ticket cancel blocked outside cancellation window
- ✅ Ticket cancel allowed with NULL cancellation fields

**Volunteer Registration Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerRegistrationTests.cs`):
- ✅ Volunteer signup allowed when within window
- ✅ Volunteer signup blocked when after close hours
- ✅ Volunteer signup allowed with NULL close hours

**Volunteer Cancellation Tests** (`/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerCancellationTests.cs`):
- ✅ Volunteer cancel allowed when within window
- ✅ Volunteer cancel blocked when after close hours
- ✅ Volunteer cancel allowed with NULL close hours
- ✅ Volunteer cancel endpoint exists and works

**Migration Correctness Tests** (`/tests/WitchCityRope.IntegrationTests/Migrations/TimingControlsMigrationTests.cs`):
- ✅ Migration adds 6 new columns
- ✅ Migration adds 6 check constraints
- ✅ Migration removes PreStartBufferMinutes setting
- ✅ Existing events have NULL timing fields after migration

### E2E Tests (Playwright)

**Admin Event Configuration** (`/tests/events/admin-timing-settings.spec.ts`):
- ✅ Admin opens RSVP/Tickets timing settings
- ✅ Admin sets registration open hours
- ✅ Admin sets registration close hours
- ✅ Admin sets cancellation open hours
- ✅ Admin sets cancellation close hours
- ✅ Settings save correctly to database
- ✅ Settings display correctly on reload

**Admin Volunteer Configuration** (`/tests/events/admin-volunteer-timing.spec.ts`):
- ✅ Admin opens Volunteers timing settings
- ✅ Admin sets volunteer registration close hours
- ✅ Admin sets volunteer cancellation close hours
- ✅ Settings save correctly to database

**User Registration Flow** (`/tests/events/user-registration-timing.spec.ts`):
- ✅ User sees RSVP button when within registration window
- ✅ User sees "Registration Opens Soon" when before window
- ✅ User sees "Registration Closed" when after window
- ✅ User can RSVP when window is open
- ✅ User blocked from RSVP when window is closed

**User Cancellation Flow** (`/tests/events/user-cancellation-timing.spec.ts`):
- ✅ User sees Cancel button when within cancellation window
- ✅ User sees "Cancellation Not Available Yet" when before window
- ✅ User sees "Cancellation Period Ended" when after window
- ✅ User can cancel when window is open
- ✅ User blocked from cancel when window is closed

**User Volunteer Flow** (`/tests/events/user-volunteer-timing.spec.ts`):
- ✅ User can sign up for volunteer spot when within window
- ✅ User blocked from volunteer signup when after close hours
- ✅ User can cancel volunteer assignment when within window
- ✅ User blocked from volunteer cancel when after close hours

## Rollout Plan

### Phase 1: Database Migration (Non-Breaking)
**Duration**: 1 day
**Risk**: Low

1. Create migration adding 6 nullable columns
2. Add check constraints
3. Deploy migration to staging
4. Verify existing events unaffected (all NULL)
5. Deploy migration to production

**Success Criteria**:
- Migration runs successfully
- Existing events have NULL timing fields
- Application continues working normally

### Phase 2: Backend Implementation
**Duration**: 2-3 days
**Risk**: Medium

1. Update EventDto with new fields
2. Refactor TimeZoneService.IsRegistrationOpenAsync → IsActionAllowedAsync
3. Create EventActionType enum
4. Update AttendanceService enforcement points
5. Update VolunteerService with timing enforcement
6. Create new volunteer cancel endpoint
7. Run unit tests (95%+ coverage required)
8. Run integration tests (100% pass required)

**Success Criteria**:
- All tests passing
- TimeZoneService correctly handles NULL fields (backward compatible)
- New volunteer cancel endpoint working
- No breaking changes to existing functionality

### Phase 3: Frontend Implementation
**Duration**: 2-3 days
**Risk**: Medium

1. Update EventForm with RSVP/Tickets timing settings section
2. Update EventForm with Volunteers timing settings section
3. Add form validation for -24 max
4. Update UserVolunteerShifts with cancel button
5. Create volunteerApi.cancelVolunteerSignup method
6. Add TypeScript types from regenerated API types
7. Test settings UI in isolation
8. Test volunteer cancel UI in isolation

**Success Criteria**:
- Settings sections collapsible and functional
- Form validation prevents invalid values
- Settings save correctly to API
- Volunteer cancel button appears and works

### Phase 4: E2E Testing
**Duration**: 2 days
**Risk**: Low

1. Write admin timing settings tests
2. Write user registration timing tests
3. Write user cancellation timing tests
4. Write volunteer timing tests
5. Run full E2E suite (100% pass required)

**Success Criteria**:
- All E2E tests passing
- Real user workflows validated
- Edge cases covered

### Phase 5: Deprecation Cleanup
**Duration**: 1 day
**Risk**: Low

1. Delete PreStartBufferMinutes from Settings table
2. Remove any UI for global setting from admin settings page
3. Update admin documentation
4. Verify no references to old setting remain

**Success Criteria**:
- Old setting completely removed
- No references in codebase
- Documentation updated

### Backward Compatibility Strategy

**During Migration**:
- Old code continues using PreStartBufferMinutes
- New code uses per-event fields
- Both systems work simultaneously

**After Backend Complete**:
- TimeZoneService uses per-event fields
- NULL fields = no restriction (same as old global setting = 0)
- Events without timing configured work as before

**After Frontend Complete**:
- Admins can configure per-event timing
- Events without configuration still use NULL (unrestricted)

**After Cleanup**:
- PreStartBufferMinutes removed
- Per-event timing is only system

## Success Criteria

### Functional Requirements Met
- ✅ Per-event timing controls working for RSVP
- ✅ Per-event timing controls working for Tickets
- ✅ Per-event timing controls working for Volunteers
- ✅ Registration and cancellation windows independent
- ✅ Post-event limits enforced (-24 max)
- ✅ NULL fields = no restriction (backward compatible)

### Quality Gates
- ✅ 95%+ unit test coverage
- ✅ 100% integration test pass rate
- ✅ 100% E2E test pass rate
- ✅ Zero TypeScript compilation errors
- ✅ Zero breaking changes to existing events

### User Experience
- ✅ Admin can configure timing per event
- ✅ Settings UI intuitive and clear
- ✅ Users see appropriate messages when outside windows
- ✅ Volunteer cancel feature functional

### Documentation
- ✅ Implementation plan complete
- ✅ Handoff documents for all agents
- ✅ API documentation updated
- ✅ Admin user guide updated

### Production Readiness
- ✅ Migration tested on staging
- ✅ Rollback plan documented
- ✅ Performance validated (no degradation)
- ✅ Security review completed

## Risks & Mitigation

### Risk 1: Breaking Changes to Existing Events
**Probability**: Medium
**Impact**: High
**Mitigation**:
- Use NULL as default (no restriction)
- Extensive backward compatibility testing
- Staged rollout with validation at each phase

### Risk 2: TimeZoneService Performance
**Probability**: Low
**Impact**: Medium
**Mitigation**:
- Use indexed Event fields
- Cache event timing calculations
- Performance tests required before deployment

### Risk 3: User Confusion
**Probability**: Medium
**Impact**: Medium
**Mitigation**:
- Clear UI labels and descriptions
- Help text on all timing inputs
- Admin documentation with examples
- Gradual rollout with admin training

### Risk 4: Complex UI Interactions
**Probability**: Low
**Impact**: Low
**Mitigation**:
- Collapsible settings sections
- Clear visual separation
- Comprehensive E2E testing

## Timeline Estimate

| Phase | Duration | Assigned To |
|-------|----------|-------------|
| Database Schema | 1 day | database-designer |
| Backend API | 2-3 days | backend-developer |
| Frontend UI | 2-3 days | react-developer |
| E2E Testing | 2 days | test-developer |
| Cleanup | 1 day | backend-developer |
| **Total** | **8-10 days** | Multiple agents |

## Next Steps

1. **Human Review**: Approve this implementation plan
2. **Database Designer**: Create migration and handoff
3. **Backend Developer**: Implement API changes and handoff
4. **React Developer**: Implement UI changes and handoff
5. **Test Developer**: Create comprehensive test suite
6. **Git Manager**: Deploy to staging and production

---

**This implementation plan is ready for Phase 2 Design approval. All user requirements incorporated and technical approach validated.**
