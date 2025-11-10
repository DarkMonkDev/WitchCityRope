# Event Cancellation Buffer Enforcement - Requirements Document

**Date**: 2025-11-09
**Status**: Implementation Required
**Priority**: Medium

## Problem Statement

Currently, users can cancel tickets and RSVPs for past events through the event detail page UI. The `PreStartBufferMinutes` setting exists in the admin settings and is enforced in the backend API (`TimeZoneService.IsRegistrationOpenAsync()`), but the frontend UI does not respect this setting and continues to show cancellation buttons for events that are in the past or within the buffer window.

## Current Implementation Status

### ✅ Backend (Already Implemented)
The backend has **complete and correct** enforcement of the pre-start buffer:

1. **Database Setting**: `/home/chad/repos/witchcityrope/apps/api/Core/Entities/Setting.cs:19`
   - `PreStartBufferMinutes`: Configurable setting stored in database
   - Default value: `0` (allow cancellations until event starts)
   - Editable via Admin Settings page

2. **Admin UI**: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminSettingsPage.tsx:258-314`
   - Pre-Start Buffer (Minutes) field exists
   - Description: "Minutes before event start when registration and cancellations close"
   - Currently functional and saves to database

3. **Backend Service**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/TimeZoneService.cs:71-89`
   - `IsRegistrationOpenAsync()` method correctly calculates cutoff time
   - Formula: `cutoffTime = eventStartDateUtc - bufferMinutes`
   - Returns `false` if current time >= cutoff time

4. **Backend Enforcement**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs:585-599`
   - `CancelParticipationAsync()` calls `IsRegistrationOpenAsync()`
   - Returns error: "Cancellation period has ended for this event"
   - **API correctly blocks cancellations outside the allowed window**

### ❌ Frontend (NOT Implemented)
The frontend UI completely ignores event timing and the buffer setting:

1. **ParticipationCard Component**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/ParticipationCard.tsx:455-470`
   - Always shows "Cancel RSVP" button if user has RSVP
   - Always shows "Cancel Ticket" button if user has ticket
   - No checks for event start time or buffer period

2. **EventDetailPage Component**: `/home/chad/repos/witchcityrope/apps/web/src/pages/events/EventDetailPage.tsx:433-447`
   - Passes event data to ParticipationCard
   - Has `eventStartDateTime` prop available
   - Does not perform any timing validation before rendering ParticipationCard

## Business Rules (Definitive)

### Rule 1: Cancellation Window Enforcement
**When**: The cancellation window closes at: `event_start_time - PreStartBufferMinutes`

**Applies to**:
- RSVP cancellations
- Ticket cancellations (user-initiated)
- New RSVPs
- New ticket purchases

**Does NOT apply to**:
- Admin-initiated cancellations/refunds (admins can always cancel)
- Volunteer shift cancellations (separate business rules)

### Rule 2: Past Events
**When**: The event start time has already passed (`event_start_time < current_time`)

**Behavior**: All participation actions must be disabled:
- Cannot create new RSVP
- Cannot purchase ticket
- Cannot cancel RSVP
- Cannot cancel ticket

**Exception**: Admin operations remain available

### Rule 3: UI Display Requirements
**When cancellation not allowed**:
- ✅ **DO**: Hide cancel buttons entirely
- ✅ **DO**: Show informational message: "This event has already started" OR "Cancellation window has closed"
- ❌ **DON'T**: Show disabled button (creates confusion about why it's disabled)
- ❌ **DON'T**: Show button that displays error after clicking

**When RSVP/ticket purchase not allowed**:
- ✅ **DO**: Hide action buttons
- ✅ **DO**: Show event status badge: "Event Started" or "Registration Closed"

### Rule 4: Buffer Setting Values
- **0 minutes** (default): Allow cancellations until event starts
- **30 minutes**: Close cancellations 30 minutes before event start
- **60 minutes**: Close cancellations 1 hour before event start
- **Any positive integer**: Close cancellations N minutes before event start

## Implementation Requirements

### Frontend Changes Required

#### 1. Fetch Buffer Setting
**Location**: Frontend API client or settings context

**Requirements**:
- Create API endpoint to fetch `PreStartBufferMinutes` setting
- Cache the setting value (refresh every 5 minutes or on settings update)
- Provide setting to components that need timing validation

**Files to modify**:
- Create: `/home/chad/repos/witchcityrope/apps/web/src/services/settings.api.ts`
- Or extend: `/home/chad/repos/witchcityrope/apps/web/src/api/client.ts`

#### 2. Calculate Event Timing Status
**Location**: Utility function or hook

**Requirements**:
- Create `useEventTimingStatus()` hook or `calculateEventStatus()` utility
- Inputs:
  - `eventStartDateTime: string` (ISO 8601 UTC)
  - `preStartBufferMinutes: number`
- Output:
  ```typescript
  {
    isPastEvent: boolean;           // event has already started
    isWithinBuffer: boolean;        // current time is within buffer window
    canRegister: boolean;           // can create RSVP or buy ticket
    canCancel: boolean;             // can cancel RSVP or ticket
    statusMessage: string;          // user-friendly message
    cutoffTime: Date;              // exact cancellation deadline
  }
  ```

**Logic**:
```typescript
const now = new Date();
const eventStart = new Date(eventStartDateTime);
const cutoffTime = new Date(eventStart.getTime() - (preStartBufferMinutes * 60 * 1000));

const isPastEvent = now >= eventStart;
const isWithinBuffer = now >= cutoffTime && now < eventStart;
const canRegister = now < cutoffTime;
const canCancel = now < cutoffTime;
```

**Files to create**:
- `/home/chad/repos/witchcityrope/apps/web/src/hooks/useEventTimingStatus.ts`
- Or: `/home/chad/repos/witchcityrope/apps/web/src/utils/eventTiming.ts`

#### 3. Update ParticipationCard Component
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/ParticipationCard.tsx`

**Requirements**:
- Add `eventStartDateTime` prop validation (already passed, just need to use it)
- Call `useEventTimingStatus()` hook
- Conditionally render cancel buttons based on `canCancel` flag
- Show status message when actions are disabled

**Changes**:
```typescript
// Line 84 - Add timing status
const timingStatus = useEventTimingStatus(eventStartDateTime, preStartBufferMinutes);

// Line 455-470 - Conditionally show cancel buttons
{timingStatus.canCancel && participation?.hasRSVP && (
  <Button onClick={() => handleCancelClick('rsvp')}>
    Cancel RSVP
  </Button>
)}

// Add status alert when cannot cancel
{!timingStatus.canCancel && (participation?.hasRSVP || participation?.hasTicket) && (
  <Alert variant="light" color="gray">
    <Text size="sm">{timingStatus.statusMessage}</Text>
  </Alert>
)}
```

#### 4. Update EventDetailPage Component
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/pages/events/EventDetailPage.tsx`

**Requirements**:
- Ensure `eventStartDateTime` is correctly passed to ParticipationCard (already done)
- Pass buffer setting to ParticipationCard
- Consider displaying event status badge in hero section

**Changes**:
- Fetch buffer setting at page level
- Pass to ParticipationCard as prop
- Optionally add status badge to event hero section

### Backend Changes Required

#### 5. Create Settings API Endpoint
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Admin/Settings/Endpoints/SettingsEndpoints.cs`

**Requirements**:
- Add public endpoint (no authentication required): `GET /api/settings/public`
- Return only non-sensitive settings:
  - `EventTimeZone`
  - `PreStartBufferMinutes`
- Cache response with 5-minute expiry

**New endpoint**:
```csharp
app.MapGet("/api/settings/public", async (ISettingsService settingsService) =>
{
    var settings = new
    {
        EventTimeZone = await settingsService.GetSettingAsync("EventTimeZone"),
        PreStartBufferMinutes = await settingsService.GetSettingAsync("PreStartBufferMinutes")
    };
    return Results.Ok(settings);
})
.WithName("GetPublicSettings")
.WithTags("Settings")
.Produces<object>(200);
```

**Files to modify**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Admin/Settings/Endpoints/SettingsEndpoints.cs`

### Testing Requirements

#### Unit Tests
1. **Timing utility tests**: `/home/chad/repos/witchcityrope/apps/web/src/utils/__tests__/eventTiming.test.ts`
   - Test past events are correctly identified
   - Test buffer window calculation
   - Test edge cases (event starting in 1 minute with 30 minute buffer)
   - Test zero buffer (allow until start)

2. **ParticipationCard tests**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/__tests__/ParticipationCard.test.tsx`
   - Test cancel buttons hidden for past events
   - Test cancel buttons hidden within buffer window
   - Test cancel buttons visible outside buffer window
   - Test status messages displayed correctly

#### E2E Tests
1. **Event detail page tests**: `/home/chad/repos/witchcityrope/tests/e2e/event-cancellation-buffer.spec.ts`
   - Create event starting in 2 hours with 30-minute buffer
   - Verify cancel buttons visible
   - Manipulate system time to within buffer window (if possible)
   - Verify cancel buttons hidden
   - Create past event
   - Verify all participation actions hidden

## Success Criteria

### Minimum Viable Implementation
- [ ] Frontend fetches `PreStartBufferMinutes` setting
- [ ] ParticipationCard hides cancel buttons for past events
- [ ] ParticipationCard hides cancel buttons within buffer window
- [ ] User sees informative message when cancellation not allowed
- [ ] E2E tests validate buffer enforcement

### Complete Implementation
- [ ] All minimum criteria met
- [ ] Event status badge shown on EventDetailPage
- [ ] RSVP/purchase buttons also respect buffer setting
- [ ] Admin settings page shows current buffer value
- [ ] Unit tests achieve 90%+ coverage
- [ ] E2E tests cover all timing scenarios

## Technical Considerations

### Time Zone Handling
- All event times stored in UTC in database
- Frontend must convert to local time for display
- Buffer calculation must use UTC for accuracy
- `TimeZoneService` already handles timezone conversions

### Cache Invalidation
- Settings cache should invalidate when admin updates buffer setting
- Consider WebSocket or polling for real-time updates
- Or: Accept 5-minute delay for setting changes to take effect

### Error Handling
- If buffer setting fetch fails, default to `0` (allow until start)
- Log errors but don't block UI rendering
- Show generic error message if timing calculation fails

### Backward Compatibility
- Backend already enforces buffer (no breaking changes)
- Frontend changes are purely UI (no API changes required)
- Existing cancellation endpoints remain unchanged

## Implementation Sequence

### Phase 1: Backend Endpoint (30 minutes)
1. Create public settings endpoint
2. Test endpoint returns correct values
3. Add to OpenAPI documentation

### Phase 2: Frontend Utilities (45 minutes)
1. Create settings API client function
2. Create event timing utility/hook
3. Write unit tests for timing logic

### Phase 3: UI Updates (1 hour)
1. Update ParticipationCard component
2. Update EventDetailPage component
3. Add status messages and alerts

### Phase 4: Testing (1 hour)
1. Write ParticipationCard component tests
2. Write E2E tests for buffer enforcement
3. Manual testing with different buffer values

**Total Estimated Time**: 3-4 hours

## Files to Modify/Create

### Backend
- **Modify**: `/home/chad/repos/witchcityrope/apps/api/Features/Admin/Settings/Endpoints/SettingsEndpoints.cs`

### Frontend
- **Create**: `/home/chad/repos/witchcityrope/apps/web/src/services/settings.api.ts`
- **Create**: `/home/chad/repos/witchcityrope/apps/web/src/hooks/useEventTimingStatus.ts`
- **Modify**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/ParticipationCard.tsx`
- **Modify**: `/home/chad/repos/witchcityrope/apps/web/src/pages/events/EventDetailPage.tsx`

### Tests
- **Create**: `/home/chad/repos/witchcityrope/apps/web/src/hooks/__tests__/useEventTimingStatus.test.ts`
- **Modify**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/__tests__/ParticipationCard.test.tsx`
- **Create**: `/home/chad/repos/witchcityrope/tests/e2e/event-cancellation-buffer.spec.ts`

## Dependencies

### Backend Dependencies
- None (all required services already exist)

### Frontend Dependencies
- `@tanstack/react-query` (already in use)
- `@mantine/core` (already in use)

### Testing Dependencies
- `vitest` (already in use)
- `@testing-library/react` (already in use)
- `playwright` (already in use)

## Risks and Mitigations

### Risk 1: Time Zone Confusion
**Mitigation**: Use UTC exclusively for calculations, only convert to local for display

### Risk 2: Clock Skew (Client/Server)
**Mitigation**: Use server time as source of truth, backend enforces regardless of frontend

### Risk 3: Cached Settings Not Updating
**Mitigation**: Short cache expiry (5 minutes), manual refresh option for admins

### Risk 4: User Confusion About Buffer
**Mitigation**: Clear messaging: "Cancellations close 30 minutes before event starts"

## Future Enhancements

1. **Countdown Timer**: Show "Cancellations close in 45 minutes"
2. **Email Notifications**: Warn users 24 hours before cancellation window closes
3. **Per-Event Buffer**: Allow event-specific buffer overrides
4. **Grace Period**: Admin setting for post-event cancellation grace period

## Related Documentation

- Admin Settings: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminSettingsPage.tsx`
- Backend Time Service: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/TimeZoneService.cs`
- Participation Service: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs`
