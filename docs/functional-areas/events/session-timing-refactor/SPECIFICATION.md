# Session-Based Timing Refactor - Complete Specification

**Created**: 2025-11-30
**Status**: IMPLEMENTED
**Implemented**: 2025-11-30
**Priority**: Critical - Foundational Architecture Change

---

## TABLE OF CONTENTS

1. [Problem Statement](#1-problem-statement)
2. [Design Principles](#2-design-principles)
3. [UTC Conversion Specification](#3-utc-conversion-specification)
4. [Ticket Timing Specification](#4-ticket-timing-specification)
5. [Volunteer Timing Specification](#5-volunteer-timing-specification)
6. [RSVP Timing (Out of Scope)](#6-rsvp-timing-out-of-scope)
7. [Backend Implementation](#7-backend-implementation)
8. [Frontend Implementation](#8-frontend-implementation)
9. [Database Changes](#9-database-changes)
10. [Testing Requirements](#10-testing-requirements)
11. [Edge Cases & Validation](#11-edge-cases--validation)
12. [Migration Plan](#12-migration-plan)
13. [Rollback Plan](#13-rollback-plan)

---

## 1. PROBLEM STATEMENT

### 1.1 The Naive UTC Bug

**Current Broken Behavior:**
```
User Action: Enter "6:00 PM" for session start time (user is in Eastern timezone)
Frontend: Creates Date with UTC constructor → 2025-12-01T18:00:00Z
Database: Stores 18:00 UTC
Backend Timing Check: Treats 18:00 UTC as actual UTC (1:00 PM Eastern)
Result: Timing windows are OFF BY 5 HOURS (EST) or 4 HOURS (EDT)
```

**Correct Behavior:**
```
User Action: Enter "6:00 PM" for session start time (Eastern timezone)
Frontend: Convert local to true UTC → 2025-12-01T23:00:00Z (6 PM EST = 11 PM UTC)
Database: Stores 23:00 UTC
Backend Timing Check: Uses 23:00 UTC correctly
Display: Convert 23:00 UTC back to 6:00 PM Eastern
Result: Timing windows are CORRECT
```

### 1.2 Event.StartDate Problem

**Current:** All timing calculations use `Event.StartDate` as the single reference point.

**Problem with Multi-Session Events:**
- Event has Session 1 (Dec 1), Session 2 (Dec 8), Session 3 (Dec 15)
- Event.StartDate = Dec 1 (earliest)
- After Dec 1 passes: StartDate auto-recalculates to Dec 8
- ALL ticket/volunteer timing shifts - confusing and wrong

**Solution:** Calculate timing per-session, not per-event.

---

## 2. DESIGN PRINCIPLES

### 2.1 Core Rules

1. **Session is the timing reference** - NOT Event.StartDate
2. **Multi-session tickets use FIRST (soonest) session** for timing calculations
3. **Volunteer positions are session-specific** - timing from their assigned session
4. **Event-wide items use earliest FUTURE session** - never a past session
5. **Expired sessions are hidden** - don't show tickets/volunteers for past sessions
6. **True UTC storage** - all times stored as actual UTC, converted on display

### 2.2 What "Available" Means

For a ticket type to be **available for purchase**:
1. At least one of its sessions is in the future
2. Current time is within the sales window for the FIRST future session
3. Tickets remain in stock

For a ticket type to be **cancellable**:
1. At least one of its sessions is in the future
2. Current time is within the cancellation window for the FIRST future session

For a volunteer position to be **available for signup**:
1. Its session (if assigned) is in the future
2. Current time is within the signup window for that session
3. Slots remain available
4. User hasn't already signed up

---

## 3. UTC CONVERSION SPECIFICATION

### 3.1 Conversion Direction

| Location | From | To | When |
|----------|------|-----|------|
| Frontend Save | Local Time | True UTC | Saving session times |
| Frontend Display | True UTC | Local Time | Displaying any time |
| Backend Storage | True UTC | True UTC | Always store UTC |
| Backend Calculations | True UTC | True UTC | All timing math |

### 3.2 Timezone Source

**Use Global Admin Setting**: `America/New_York` (from database setting `EventTimeZone`)

- This is a **GLOBAL setting** configured in Admin → Settings
- NOT per-event (all events use the same timezone)
- NOT user's browser timezone
- All events are in Salem, MA, so Eastern time applies to all
- Backend: `ISettingsService.GetSettingAsync("EventTimeZone")`
- Frontend: Fetch from `/api/settings/EventTimeZone` or use hook

### 3.3 Frontend Conversion Functions

**Location:** `/apps/web/src/utils/eventUtils.ts`

```typescript
// NEW: Convert local time string to UTC ISO string for API
export function localTimeToUtc(
  date: Date,           // The date (already in local context)
  timeString: string,   // "18:00" format
  timezone: string      // "America/New_York"
): string {
  // Parse time
  const [hours, minutes] = timeString.split(':').map(Number);

  // Create date in the specified timezone
  // Use Intl.DateTimeFormat to handle DST correctly
  const localDateTime = new Date(date);
  localDateTime.setHours(hours, minutes, 0, 0);

  // Convert to UTC
  // The trick: Create a formatter for the target timezone,
  // then calculate offset and apply
  return localDateTime.toISOString(); // Returns true UTC
}

// NEW: Convert UTC ISO string to local time for display
export function utcToLocalTime(
  isoString: string,    // "2025-12-01T23:00:00Z"
  timezone: string      // "America/New_York"
): { hours: number; minutes: number; formatted: string } {
  const date = new Date(isoString);

  // Use Intl to get local time parts
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });

  const parts = formatter.formatToParts(date);
  // Extract hours, minutes from parts

  return {
    hours: extractedHours,
    minutes: extractedMinutes,
    formatted: formatter.format(date) // "6:00 PM"
  };
}
```

### 3.4 Files Requiring UTC Conversion Updates

| File | Current Pattern | New Pattern |
|------|-----------------|-------------|
| `SessionFormModal.tsx` | `Date.UTC()` naive | `localTimeToUtc()` true |
| `eventUtils.ts` | `getUTCHours()` | `utcToLocalTime()` |
| `EventSessionsGrid.tsx` | `getUTCHours()` | `utcToLocalTime()` |
| `EventsTableView.tsx` | `getUTCHours()` | `utcToLocalTime()` |
| `EventDetailPage.tsx` | `formatEventTime()` | Updated `formatEventTime()` |
| All event display components | Naive UTC | True UTC conversion |

---

## 4. TICKET TIMING SPECIFICATION

### 4.1 Core Ticket Timing Rules

**Reference Time for Timing Calculations:**
- **Single-session ticket**: Use that session's StartTime
- **Multi-session ticket**: Use the **FIRST (earliest) FUTURE session's** StartTime
- **All sessions passed**: Ticket is NOT available (hide it)

### 4.2 Ticket Sales Window

**Opens:** `CurrentTime >= (ReferenceSession.StartTime - RegistrationOpenHours)`
**Closes:** `CurrentTime < (ReferenceSession.StartTime - RegistrationCloseHours)`

```
Example:
  Session 2 StartTime: Dec 8, 6:00 PM EST (Dec 8, 23:00 UTC)
  RegistrationOpenHours: 168 (7 days before)
  RegistrationCloseHours: 24 (1 day before)

  Sales Open: Dec 1, 6:00 PM EST
  Sales Close: Dec 7, 6:00 PM EST

  On Dec 5: hoursUntilSession = 72 → 72 < 168 (open) AND 72 > 24 (not closed) → AVAILABLE
  On Dec 8 at 5 PM: hoursUntilSession = 1 → 1 < 24 → CLOSED
```

### 4.3 Ticket Cancellation Window

**Closes:** `CurrentTime < (ReferenceSession.StartTime - CancellationCloseHours)`

```
Example:
  Session 2 StartTime: Dec 8, 6:00 PM EST
  CancellationCloseHours: 48 (2 days before)

  Cancel Available Until: Dec 6, 6:00 PM EST

  On Dec 5: hoursUntilSession = 72 → 72 > 48 → CAN CANCEL
  On Dec 7: hoursUntilSession = 24 → 24 < 48 → CANNOT CANCEL
```

### 4.4 Multi-Session Ticket Scenarios

**Scenario A: "All Access Pass" (covers Sessions 1, 2, 3)**
```
Sessions: Dec 1, Dec 8, Dec 15
Current Date: Dec 3 (Session 1 has passed)

Reference Session: Dec 8 (first FUTURE session)
Sales/Cancel timing: Based on Dec 8

Result: Ticket still purchasable, timing based on Dec 8
```

**Scenario B: "Weekend Intensive" (covers Sessions 1 & 2)**
```
Sessions: Dec 1, Dec 8
Current Date: Dec 10 (both sessions passed)

Reference Session: NONE (all passed)

Result: Ticket NOT available (hidden from UI)
```

**Scenario C: "Single Day Ticket" (Session 2 only)**
```
Sessions: Dec 8 only
Current Date: Dec 3

Reference Session: Dec 8

Result: Normal single-session behavior
```

### 4.5 Ticket Display Logic

**Public Event Page - Ticket Section:**
```
FOR EACH ticketType:
  futureSessions = ticketType.sessions.filter(s => s.startTime > now)

  IF futureSessions.length == 0:
    // Don't display this ticket type at all
    CONTINUE

  referenceSession = futureSessions.orderBy(startTime).first()

  IF not withinSalesWindow(referenceSession):
    // Show ticket but disabled with message
    DISPLAY: "Sales open on {date}" OR "Sales closed"
  ELSE:
    // Show ticket as purchasable
    DISPLAY: Normal purchase UI
    DISPLAY: "Valid for: {list of future sessions}"
```

### 4.6 TicketTypeDto Changes

```csharp
public class TicketTypeDto
{
    // Existing fields...
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> SessionIds { get; set; }

    // NEW computed fields:

    /// <summary>
    /// True if ticket can be purchased right now
    /// Based on: future sessions exist AND within sales window
    /// </summary>
    public bool CanPurchase { get; set; }

    /// <summary>
    /// List of session IDs that are still in the future
    /// UI uses this to show "Valid for: Session 2, Session 3"
    /// </summary>
    public List<string> AvailableSessionIds { get; set; }

    /// <summary>
    /// Message explaining availability status
    /// Examples: "Available", "Sales open Dec 1", "Sales closed", "Event passed"
    /// </summary>
    public string AvailabilityMessage { get; set; }

    /// <summary>
    /// The session used for timing calculations (first future session)
    /// Null if all sessions passed
    /// </summary>
    public string? ReferenceSessionId { get; set; }
}
```

---

## 5. VOLUNTEER TIMING SPECIFICATION

### 5.1 Core Volunteer Timing Rules

**Reference Time for Timing Calculations:**
- **Position with SessionId**: Use that session's StartTime
- **Position without SessionId (event-wide)**: Use **earliest FUTURE session's** StartTime
- **Position's session has passed**: Hide the position

### 5.2 Volunteer Signup Window

**Closes:** `CurrentTime < (ReferenceSession.StartTime - VolunteerRegistrationCloseHours)`

Note: Volunteer signup has NO open restriction (always open until close time).

```
Example:
  Position "Setup Crew" assigned to Session 2
  Session 2 StartTime: Dec 8, 6:00 PM EST
  VolunteerRegistrationCloseHours: 24 (1 day before)

  Signup Available Until: Dec 7, 6:00 PM EST
```

### 5.3 Volunteer Cancellation Window

**Closes:** `CurrentTime < (ReferenceSession.StartTime - VolunteerCancellationCloseHours)`

```
Example:
  VolunteerCancellationCloseHours: 48 (2 days before)

  Cancel Available Until: Dec 6, 6:00 PM EST
```

### 5.4 Volunteer Position Display Logic

**Public Event Page - Volunteer Section:**
```
FOR EACH position:
  IF position.sessionId IS NOT NULL:
    session = getSession(position.sessionId)
    IF session.startTime <= now:
      // Session passed, don't show
      CONTINUE
    referenceSession = session
  ELSE:
    // Event-wide position
    futureSessions = event.sessions.filter(s => s.startTime > now)
    IF futureSessions.length == 0:
      // All sessions passed
      CONTINUE
    referenceSession = futureSessions.orderBy(startTime).first()

  IF not withinSignupWindow(referenceSession):
    // Show position but with closed message
    DISPLAY: "Signup closed"
  ELSE IF position.slotsFilled >= position.slotsNeeded:
    DISPLAY: "Fully staffed"
  ELSE:
    DISPLAY: Normal signup UI
```

### 5.5 VolunteerPositionDto Changes

```csharp
public class VolunteerPositionDto
{
    // Existing fields...
    public string Id { get; set; }
    public string Title { get; set; }
    public string? SessionId { get; set; }
    public int SlotsNeeded { get; set; }
    public int SlotsFilled { get; set; }

    // EXISTING (keep):
    public bool CanSignUp { get; set; }  // Already exists, fix calculation

    // NEW computed fields:

    /// <summary>
    /// True if user can cancel their signup
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Session name for display (if session-specific)
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// Session start time for display
    /// </summary>
    public DateTime? SessionStartTime { get; set; }

    /// <summary>
    /// Message explaining signup status
    /// </summary>
    public string SignupStatusMessage { get; set; }
}
```

---

## 6. RSVP TIMING (OUT OF SCOPE)

**Per user direction:** RSVPs currently apply to all sessions by default. There are no multi-session RSVP-only events in production.

**Current Behavior (Keep As-Is):**
- RSVPs use Event.StartDate (earliest session)
- Single timing window for all sessions
- May revisit in future if needed

**Why Out of Scope:**
- No real-world use case requiring change
- Can be addressed separately if needed
- Reduces scope of current refactor

---

## 7. BACKEND IMPLEMENTATION

### 7.1 TimeZoneService Changes

**File:** `/apps/api/Features/Events/Services/TimeZoneService.cs`

```csharp
// NEW METHOD: Get reference session for a ticket type
public Session? GetReferenceSessionForTicketType(
    TicketType ticketType,
    IEnumerable<Session> allSessions)
{
    if (ticketType.SessionIds == null || !ticketType.SessionIds.Any())
    {
        // Ticket applies to all sessions - use earliest future
        return GetEarliestFutureSession(allSessions);
    }

    var ticketSessions = allSessions
        .Where(s => ticketType.SessionIds.Contains(s.Id))
        .ToList();

    // Get earliest FUTURE session from ticket's sessions
    var futureSession = ticketSessions
        .Where(s => s.StartTime > DateTime.UtcNow)
        .OrderBy(s => s.StartTime)
        .FirstOrDefault();

    return futureSession; // null if all passed
}

// NEW METHOD: Check if action allowed for a session
public bool IsActionAllowedForSession(
    Session? session,
    decimal? openHours,
    decimal? closeHours)
{
    if (session == null)
    {
        return false; // No valid session = not allowed
    }

    var now = DateTime.UtcNow;
    var hoursUntilSession = (session.StartTime - now).TotalHours;

    // Check open window (if configured)
    if (openHours.HasValue && hoursUntilSession > (double)openHours.Value)
    {
        return false; // Too early
    }

    // Check close window (if configured)
    const double EPSILON = 0.01;
    if (closeHours.HasValue && hoursUntilSession < (double)closeHours.Value - EPSILON)
    {
        return false; // Too late
    }

    return true;
}

// NEW METHOD: Get earliest future session
public Session? GetEarliestFutureSession(IEnumerable<Session> sessions)
{
    return sessions
        .Where(s => s.StartTime > DateTime.UtcNow)
        .OrderBy(s => s.StartTime)
        .FirstOrDefault();
}
```

### 7.2 AttendanceService Changes

**File:** `/apps/api/Features/Participation/Services/AttendanceService.cs`

**CreateTicketPurchaseAsync - Update timing check:**
```csharp
// OLD (remove):
var isAllowed = await _timeZoneService.IsActionAllowedAsync(
    eventEntity, EventActionType.GetTicket, cancellationToken);

// NEW:
var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
    ticketType, eventEntity.Sessions);

if (referenceSession == null)
{
    return Result.Failure("All sessions for this ticket have passed");
}

var isAllowed = _timeZoneService.IsActionAllowedForSession(
    referenceSession,
    eventEntity.RegistrationOpenHours,
    eventEntity.RegistrationCloseHours);

if (!isAllowed)
{
    return Result.Failure("Ticket purchase window is not currently open");
}
```

**CancelParticipationAsync - Update for tickets:**
```csharp
// For ticket cancellation, get reference session
if (attendance.AttendanceType == AttendanceType.Ticket)
{
    var ticketType = await GetTicketTypeForAttendance(attendance);
    var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
        ticketType, eventEntity.Sessions);

    if (referenceSession == null)
    {
        return Result.Failure("Cannot cancel - all sessions have passed");
    }

    var canCancel = _timeZoneService.IsActionAllowedForSession(
        referenceSession,
        null, // No open restriction for cancellation
        eventEntity.CancellationCloseHours);

    if (!canCancel)
    {
        return Result.Failure("Cancellation window has closed");
    }
}
```

### 7.3 VolunteerService Changes

**File:** `/apps/api/Features/Volunteers/Services/VolunteerService.cs`

**GetEventVolunteerPositionsAsync - Fix CanSignUp calculation:**
```csharp
foreach (var vp in volunteerPositions)
{
    Session? referenceSession;

    if (vp.SessionId.HasValue)
    {
        // Session-specific position
        referenceSession = eventEntity.Sessions
            .FirstOrDefault(s => s.Id == vp.SessionId);

        // If session passed, skip this position entirely
        if (referenceSession == null || referenceSession.StartTime <= DateTime.UtcNow)
        {
            continue; // Don't include in results
        }
    }
    else
    {
        // Event-wide position
        referenceSession = _timeZoneService.GetEarliestFutureSession(
            eventEntity.Sessions);

        if (referenceSession == null)
        {
            continue; // All sessions passed
        }
    }

    var canSignUp = _timeZoneService.IsActionAllowedForSession(
        referenceSession,
        null, // No open restriction for volunteer signup
        eventEntity.VolunteerRegistrationCloseHours);

    // Also check slots and existing signup
    if (canSignUp)
    {
        canSignUp = vp.SlotsRemaining > 0 && userSignup == null;
    }

    // Add to results with correct CanSignUp value
    results.Add(new VolunteerPositionDto
    {
        // ... existing mappings ...
        CanSignUp = canSignUp,
        SessionName = referenceSession?.Name,
        SessionStartTime = referenceSession?.StartTime
    });
}
```

### 7.4 EventService Changes

**File:** `/apps/api/Features/Events/Services/EventService.cs`

**GetEventByIdAsync - Add computed fields to TicketTypes:**
```csharp
// After loading event with includes...

foreach (var ticketType in eventDto.TicketTypes)
{
    var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
        ticketTypeEntity, eventEntity.Sessions);

    ticketType.ReferenceSessionId = referenceSession?.Id.ToString();
    ticketType.AvailableSessionIds = ticketTypeEntity.SessionIds
        .Where(sid => eventEntity.Sessions
            .Any(s => s.Id == sid && s.StartTime > DateTime.UtcNow))
        .Select(sid => sid.ToString())
        .ToList();

    ticketType.CanPurchase = referenceSession != null &&
        _timeZoneService.IsActionAllowedForSession(
            referenceSession,
            eventEntity.RegistrationOpenHours,
            eventEntity.RegistrationCloseHours);

    ticketType.AvailabilityMessage = GetAvailabilityMessage(
        referenceSession,
        ticketType.CanPurchase,
        eventEntity);
}
```

---

## 8. FRONTEND IMPLEMENTATION

### 8.1 Files Requiring Changes

| File | Type of Change |
|------|----------------|
| `/apps/web/src/utils/eventUtils.ts` | Add true UTC conversion functions |
| `/apps/web/src/components/events/SessionFormModal.tsx` | Convert to true UTC on save |
| `/apps/web/src/components/events/EventSessionsGrid.tsx` | Display with UTC conversion |
| `/apps/web/src/components/events/EventsTableView.tsx` | Display with UTC conversion |
| `/apps/web/src/pages/events/EventDetailPage.tsx` | Session-based ticket/volunteer display |
| `/apps/web/src/pages/events/EventsListPage.tsx` | Show next available session |
| `/apps/web/src/features/volunteers/components/VolunteerPositionCard.tsx` | Use new DTO fields |
| `/apps/web/src/components/events/EventTicketPurchaseModal.tsx` | Show available sessions |
| `/apps/web/src/components/homepage/EventCard.tsx` | Display with UTC conversion |
| `/apps/web/src/components/events/public/EventCard.tsx` | Display with UTC conversion |
| `/apps/web/src/pages/dashboard/MyEventsPage.tsx` | Display with UTC conversion |
| `/apps/web/src/components/dashboard/UserParticipations.tsx` | Display with UTC conversion |
| `/apps/web/src/components/dashboard/UserVolunteerShifts.tsx` | Display with UTC conversion |

### 8.2 EventDetailPage Logic

**Ticket Section:**
```tsx
// Filter to only show ticket types with future sessions
const availableTicketTypes = event.ticketTypes?.filter(
  tt => tt.availableSessionIds && tt.availableSessionIds.length > 0
) || [];

// Group by availability status
const purchasableTickets = availableTicketTypes.filter(tt => tt.canPurchase);
const upcomingTickets = availableTicketTypes.filter(tt => !tt.canPurchase);

return (
  <div>
    {purchasableTickets.length > 0 && (
      <section>
        <h3>Available Tickets</h3>
        {purchasableTickets.map(ticket => (
          <TicketCard
            key={ticket.id}
            ticket={ticket}
            availableSessions={getSessionNames(ticket.availableSessionIds)}
          />
        ))}
      </section>
    )}

    {upcomingTickets.length > 0 && (
      <section>
        <h3>Upcoming Ticket Sales</h3>
        {upcomingTickets.map(ticket => (
          <TicketCard
            key={ticket.id}
            ticket={ticket}
            disabled
            message={ticket.availabilityMessage}
          />
        ))}
      </section>
    )}

    {availableTicketTypes.length === 0 && event.ticketTypes?.length > 0 && (
      <Alert>All ticket sales have ended for this event.</Alert>
    )}
  </div>
);
```

**Volunteer Section:**
```tsx
// Volunteer positions already filtered by backend (past sessions excluded)
const availablePositions = volunteerPositions?.filter(p => !p.isFullyStaffed) || [];

return (
  <div>
    {availablePositions.map(position => (
      <VolunteerPositionCard
        key={position.id}
        position={position}
        // CanSignUp already calculated correctly by backend
      />
    ))}

    {availablePositions.length === 0 && volunteerPositions?.length > 0 && (
      <Text>All volunteer positions are filled or signup has closed.</Text>
    )}
  </div>
);
```

### 8.3 SessionFormModal UTC Conversion

```tsx
const handleSubmit = (values: FormValues) => {
  const eventTimezone = 'America/New_York'; // From event settings

  // Convert local time to true UTC
  const startDateTime = localTimeToUtc(
    values.date,
    values.startTime,
    eventTimezone
  );

  const endDateTime = localTimeToUtc(
    values.date,
    values.endTime,
    eventTimezone
  );

  onSubmit({
    ...values,
    startTime: startDateTime, // Now true UTC
    endTime: endDateTime,     // Now true UTC
  });
};
```

---

## 9. DATABASE CHANGES

### 9.1 No Schema Changes Required

The existing schema supports this refactor:
- `Session.StartTime` and `Session.EndTime` already exist (just fix storage)
- `TicketType.SessionIds` already exists for multi-session tickets
- `VolunteerPosition.SessionId` already exists for session-specific positions
- Event timing fields remain as-is (interpreted differently)

### 9.2 Event.StartDate Handling

**Option A (Recommended): Keep as computed property for filtering**
- Remove from entity storage
- Add as computed property in DTO
- Value = earliest session's StartTime
- Used for: event list sorting, "upcoming events" filters

**Option B: Keep as stored field**
- Continue auto-recalculation
- Don't use for timing calculations
- Only for display/sorting

**Decision:** Use Option A - cleaner architecture.

### 9.3 Data Migration

Since there's no live data:
- No migration needed for existing records
- Just update code to store true UTC going forward

---

## 10. TESTING REQUIREMENTS

### 10.1 Existing Tests to Update

| Test File | Changes Required |
|-----------|------------------|
| `TicketTimingTests.cs` | Use session-based timing, test multi-session scenarios |
| `VolunteerTimingTests.cs` | Use session-based timing, test session-specific positions |
| `RsvpTimingTests.cs` | OUT OF SCOPE - keep as-is |

### 10.2 New Backend Tests Required

**File:** `SessionBasedTicketTimingTests.cs`
```csharp
[Fact]
public async Task PurchaseTicket_MultiSession_UsesFirstFutureSession()
{
    // Arrange: Ticket covers Sessions 1 (passed), 2 (future), 3 (future)
    // Act: Attempt purchase
    // Assert: Timing calculated from Session 2
}

[Fact]
public async Task PurchaseTicket_AllSessionsPassed_Fails()
{
    // Arrange: Ticket covers Sessions 1, 2 (both passed)
    // Act: Attempt purchase
    // Assert: Returns error "All sessions have passed"
}

[Fact]
public async Task PurchaseTicket_SingleSession_UsesSessionTime()
{
    // Arrange: Ticket covers only Session 2
    // Act: Attempt purchase within window
    // Assert: Success
}

[Fact]
public async Task CancelTicket_MultiSession_UsesFirstFutureSession()
{
    // Similar pattern
}
```

**File:** `SessionBasedVolunteerTimingTests.cs`
```csharp
[Fact]
public async Task VolunteerSignup_SessionSpecific_UsesSessionTime()
{
    // Arrange: Position assigned to Session 2
    // Act: Check CanSignUp
    // Assert: Based on Session 2 timing
}

[Fact]
public async Task VolunteerSignup_SessionPassed_NotReturned()
{
    // Arrange: Position assigned to Session 1 (passed)
    // Act: Get volunteer positions
    // Assert: Position not in results
}

[Fact]
public async Task VolunteerSignup_EventWide_UsesEarliestFutureSession()
{
    // Arrange: Position with no SessionId
    // Act: Check CanSignUp after Session 1 passes
    // Assert: Based on Session 2 timing
}
```

**File:** `UtcConversionTests.cs`
```csharp
[Fact]
public void ConvertLocalToUtc_EasternTime_CorrectOffset()
{
    // 6:00 PM EST should become 23:00 UTC (EST = UTC-5)
}

[Fact]
public void ConvertLocalToUtc_DaylightSavingTime_CorrectOffset()
{
    // 6:00 PM EDT should become 22:00 UTC (EDT = UTC-4)
}
```

### 10.3 New E2E Tests Required

**File:** `tests/e2e/session-based-ticket-timing.spec.ts`
```typescript
test.describe('Session-Based Ticket Timing', () => {
  test('multi-session event shows only future session tickets', async () => {
    // Create event with 3 sessions, first one in past
    // Navigate to event page
    // Verify only tickets for sessions 2, 3 shown
  });

  test('ticket shows available sessions', async () => {
    // Create multi-session ticket
    // Navigate to event page
    // Verify "Valid for: Session 2, Session 3" shown
  });

  test('sales window based on first available session', async () => {
    // Create event with session tomorrow
    // Set registrationCloseHours to 48
    // Verify tickets show "Sales closed" (< 48 hours away)
  });
});
```

**File:** `tests/e2e/session-based-volunteer-timing.spec.ts`
```typescript
test.describe('Session-Based Volunteer Timing', () => {
  test('session-specific position hidden after session passes', async () => {
    // Create event with past session volunteer position
    // Navigate to event page
    // Verify position not displayed
  });

  test('event-wide position uses earliest future session', async () => {
    // Create event-wide position, first session passed
    // Verify signup still available (based on session 2)
  });
});
```

### 10.4 Manual Testing Checklist

- [ ] Create multi-session event with 3 sessions
- [ ] Add ticket type covering all sessions
- [ ] Verify ticket shows after first session passes
- [ ] Add volunteer position for session 2
- [ ] Verify position shows correct timing
- [ ] Verify times display correctly (6 PM shows as 6 PM)
- [ ] Verify timing calculations work (24 hours before = correct time)
- [ ] Test cancellation windows
- [ ] Test across DST boundary (if applicable)

---

## 11. EDGE CASES & VALIDATION

### 11.1 Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Event with no sessions | Tickets/volunteers not available (graceful handling) |
| All sessions in past | Hide all tickets, hide all volunteers, show "Event ended" |
| Session at exact current time | Treat as passed (use strict > comparison) |
| Ticket with no SessionIds | Applies to all sessions, use earliest future |
| VolunteerPosition with null SessionId | Event-wide, use earliest future session |
| Event with only past sessions + future sessions being added | Re-evaluate availability after session added |
| Timing fields are null | No restriction (current behavior preserved) |
| Negative timing hours (e.g., -24) | Allows action up to 24 hours AFTER session starts |

### 11.2 Validation Rules

**Session Creation:**
- StartTime must be in the future (warn if past)
- EndTime must be after StartTime
- Date/time conversion must handle DST correctly

**Ticket Type Creation:**
- SessionIds can be empty (applies to all)
- At least one valid session must exist for ticket to be purchasable

**Volunteer Position Creation:**
- SessionId is optional (event-wide if null)
- If SessionId provided, must be valid session for this event

---

## 12. MIGRATION PLAN

### 12.1 Implementation Order

1. **Phase 1: UTC Conversion** (Foundation)
   - Update `eventUtils.ts` with conversion functions
   - Update `SessionFormModal.tsx` to save true UTC
   - Update all display components to convert UTC to local
   - Test: Times display correctly

2. **Phase 2: TimeZoneService Updates**
   - Add session-based timing methods
   - Keep existing event-based methods (backward compat)
   - Test: New methods work correctly

3. **Phase 3: Ticket Timing**
   - Update AttendanceService for session-based tickets
   - Update TicketTypeDto with new fields
   - Update EventService to populate new fields
   - Update frontend ticket display
   - Test: Multi-session tickets work correctly

4. **Phase 4: Volunteer Timing**
   - Update VolunteerService for session-based timing
   - Update VolunteerPositionDto
   - Update frontend volunteer display
   - Test: Session-specific volunteers work correctly

5. **Phase 5: Cleanup**
   - Remove Event.StartDate auto-recalculation
   - Make StartDate computed property
   - Final integration testing

### 12.2 Feature Flag (Optional)

Consider a feature flag to toggle between:
- Old: Event.StartDate-based timing
- New: Session-based timing

Allows rollback if issues discovered in production.

---

## 13. ROLLBACK PLAN

### 13.1 If Issues Discovered

1. **Revert commits** - Standard git revert
2. **Database safe** - No schema changes, no data migration needed
3. **Feature flag** - If implemented, just toggle back

### 13.2 Monitoring Points

- Watch for timing-related errors in logs
- Monitor ticket purchase success rates
- Monitor volunteer signup success rates
- User feedback on time display accuracy

---

## APPENDIX A: File Change Summary

### Backend Files (C#)
1. `TimeZoneService.cs` - Add session-based methods
2. `AttendanceService.cs` - Use session timing for tickets
3. `VolunteerService.cs` - Use session timing for positions
4. `EventService.cs` - Populate new DTO fields
5. `TicketTypeDto.cs` - Add computed fields
6. `VolunteerPositionDto.cs` - Add computed fields

### Frontend Files (TypeScript/React)
1. `eventUtils.ts` - Add UTC conversion functions
2. `SessionFormModal.tsx` - Convert to true UTC
3. `EventSessionsGrid.tsx` - Display conversion
4. `EventsTableView.tsx` - Display conversion
5. `EventDetailPage.tsx` - Session-based display logic
6. `EventsListPage.tsx` - Display conversion
7. `VolunteerPositionCard.tsx` - Use new DTO fields
8. `EventTicketPurchaseModal.tsx` - Show available sessions
9. `EventCard.tsx` (multiple) - Display conversion

### Test Files
1. `TicketTimingTests.cs` - Update for sessions
2. `VolunteerTimingTests.cs` - Update for sessions
3. `SessionBasedTicketTimingTests.cs` - NEW
4. `SessionBasedVolunteerTimingTests.cs` - NEW
5. `UtcConversionTests.cs` - NEW
6. `session-based-ticket-timing.spec.ts` - NEW E2E
7. `session-based-volunteer-timing.spec.ts` - NEW E2E

---

## APPENDIX B: Quick Reference

### Timing Calculation Formula

```
For any timing check:
1. Get reference session (first future session from applicable sessions)
2. Calculate: hoursUntilSession = (session.StartTime - now).TotalHours
3. Check open window: hoursUntilSession <= openHours (if configured)
4. Check close window: hoursUntilSession >= closeHours (if configured)
5. Return: passes both checks
```

### UTC Conversion Formula

```
Save (Local → UTC):
  UTC = LocalTime - TimezoneOffset
  Example: 6:00 PM EST → 23:00 UTC (offset = -5 hours)

Display (UTC → Local):
  LocalTime = UTC + TimezoneOffset
  Example: 23:00 UTC → 6:00 PM EST (offset = -5 hours)
```
