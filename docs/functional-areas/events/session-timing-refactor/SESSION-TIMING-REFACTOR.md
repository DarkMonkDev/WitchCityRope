# Session-Based Timing Refactor

**Created**: 2025-11-30
**Status**: Planning
**Priority**: Critical - Foundational Architecture Change

---

## 1. THE PROBLEM

### 1.1 Naive UTC Storage Bug

**Current Behavior (BROKEN):**
- User enters 6:00 PM Eastern for session start time
- Frontend stores as "naive UTC": `2025-12-01T18:00:00Z`
- Database contains `18:00` but this is LOCAL time, not UTC
- Timing calculations (ticket sales, volunteer signup) treat this as TRUE UTC
- Calculations are off by 5 hours (EST) or 4 hours (EDT)

**Example of the Bug:**
```
User enters: 6:00 PM Eastern (session start)
Stored in DB: 2025-12-01T18:00:00Z (looks like UTC but is actually local)
VolunteerRegistrationCloseHours: 24 (closes 24 hours before)

Correct behavior: Signup should close at 6:00 PM Eastern on Nov 30
Actual behavior: System thinks event is at 1:00 PM Eastern (18:00 UTC)
                 Signup closes at 1:00 PM Eastern on Nov 30 (5 hours early!)
```

### 1.2 Event.StartDate is Wrong Abstraction

**Current Architecture:**
- `Event.StartDate` is the SINGLE reference point for ALL timing
- Multi-session events have problems:
  - Event with Dec 1, Dec 8, Dec 15 sessions
  - StartDate = earliest session (Dec 1)
  - ALL timing windows calculated from Dec 1
  - After Dec 1 passes, StartDate auto-recalculates to Dec 8

**Problems:**
1. Ticket sales for Dec 15 session close based on Dec 1 date
2. Volunteer positions for specific sessions have wrong timing
3. No granularity - everything tied to one date
4. Auto-recalculation creates shifting windows (confusing)

---

## 2. DESIRED BEHAVIOR

### 2.1 True UTC Storage
- User enters 6:00 PM Eastern
- Convert to TRUE UTC: `2025-12-01T23:00:00Z` (EST = UTC-5)
- Database stores actual UTC time
- Display converts back to local for users
- Timing calculations work correctly

### 2.2 Session-Based Timing
- Each session has its own timing windows
- Ticket sales: Per-session (Dec 15 tickets available even after Dec 1 passes)
- Volunteer signup: Per-session (Dec 15 volunteer positions available)
- Cancellation: Per-session
- UI shows available options based on session timing, hides expired ones

### 2.3 Remove Event.StartDate Dependency
- Event.StartDate becomes computed (earliest future session OR display-only)
- Timing calculations use Session.StartTime directly
- No auto-recalculation surprises

---

## 3. RESEARCH FINDINGS

### 3.1 Central Timing Service

**File:** `/apps/api/Features/Events/Services/TimeZoneService.cs`

```csharp
// Lines 88-91 - THE critical calculation
var eventStartTimeUtc = eventEntity.StartDate;  // Currently used
var hoursUntilStart = (eventStartTimeUtc - currentTimeUtc).TotalHours;
```

This is called for ALL timing decisions:
- Ticket sales (GetTicket)
- RSVPs (GetRsvp)
- Cancellations (CancelTicket, CancelRsvp)
- Volunteer signup (GetVolunteer)
- Volunteer cancellation (CancelVolunteer)

### 3.2 Current Timing Fields (Event-Level)

**File:** `/apps/api/Models/Event.cs` (lines 102-141)

| Field | Purpose | Storage |
|-------|---------|---------|
| StartDate | Event start (SINGLE reference point) | DateTime UTC |
| EndDate | Event end | DateTime UTC |
| RegistrationOpenHours | When ticket/RSVP sales open | decimal? (hours before) |
| RegistrationCloseHours | When ticket/RSVP sales close | decimal? (hours before/after) |
| CancellationCloseHours | When cancellation closes | decimal? (hours before/after) |
| VolunteerRegistrationCloseHours | When volunteer signup closes | decimal? |
| VolunteerCancellationCloseHours | When volunteer cancel closes | decimal? |

### 3.3 Session Model

**File:** `/apps/api/Models/Session.cs`

| Field | Purpose |
|-------|---------|
| StartTime | Session start (DateTime UTC) |
| EndTime | Session end (DateTime UTC) |

Sessions are already linked to events. Volunteer positions can have optional `SessionId`.

### 3.4 Frontend DateTime Utilities

**File:** `/apps/web/src/utils/eventUtils.ts`

All formatting uses "naive UTC" pattern:
- `formatStoredTime()` - Uses `getUTCHours()`/`getUTCMinutes()` (no conversion)
- `formatEventTime()` - Same pattern
- Comments explicitly say "naive UTC - UTC value IS local time"

**File:** `/apps/web/src/components/events/SessionFormModal.tsx`

Session creation uses `Date.UTC()` to store naive UTC:
```typescript
const startDateTime = new Date(Date.UTC(year, month, day, startHour, startMinute));
```

### 3.5 Auto-Recalculation (Recent Addition)

**File:** `/apps/api/Features/Events/Services/EventService.cs` (lines 548-582)

```csharp
private void RecalculateEventStartDate(Event eventEntity)
{
    // StartDate = earliest future session OR earliest overall
    var earliestFutureSession = eventEntity.Sessions
        .Where(s => s.StartTime > now)
        .OrderBy(s => s.StartTime)
        .FirstOrDefault();

    eventEntity.StartDate = earliestFutureSession?.StartTime ??
                            eventEntity.Sessions.OrderBy(s => s.StartTime).First().StartTime;
}
```

### 3.6 Volunteer CanSignUp Calculation

**File:** `/apps/api/Features/Volunteers/Services/VolunteerService.cs` (lines 106-136)

```csharp
var canSignUp = await _timeZoneService.IsActionAllowedAsync(
    eventEntity, EventActionType.GetVolunteer, cancellationToken);

// Also checks slots and existing signup
if (canSignUp)
{
    canSignUp = vp.SlotsRemaining > 0 && userSignup == null;
}
```

### 3.7 Ticket Type Session Association

**File:** `/apps/api/Models/TicketType.cs`

Ticket types have `SessionIds` (many-to-many) for session-specific tickets.
This provides foundation for per-session timing.

---

## 4. AFFECTED FILES

### 4.1 Backend - Must Change

| File | Change Required |
|------|-----------------|
| `TimeZoneService.cs` | Accept Session instead of Event, calculate from Session.StartTime |
| `VolunteerService.cs` | Pass session to timing check if position has SessionId |
| `AttendanceService.cs` | Pass session to timing check for session-specific tickets |
| `Event.cs` | Make StartDate computed or remove timing dependency |
| `Session.cs` | Add timing override fields (optional) |
| `SessionFormModal.tsx` | Convert local → true UTC on save |
| `eventUtils.ts` | Convert UTC → local on display |

### 4.2 Frontend - Must Change

| File | Change Required |
|------|-----------------|
| `SessionFormModal.tsx` | True timezone conversion on save |
| `eventUtils.ts` | True timezone conversion on display |
| `EventSessionsGrid.tsx` | Display converted times |
| `EventsTableView.tsx` | Display converted times |
| `EventDetailPage.tsx` | Display per-session availability |
| `VolunteerPositionCard.tsx` | Show session-specific timing |
| All date/time displays | Use proper conversion |

### 4.3 Database Migration

- No schema change needed if we keep StartDate as computed
- OR migration to remove StartDate and use sessions only
- Ensure existing data (if any) is properly converted

---

## 5. PROPOSED IMPLEMENTATION

### Phase 1: Fix UTC Storage (Foundation)

1. **SessionFormModal.tsx**: Convert local time to TRUE UTC on save
   ```typescript
   // User enters 6:00 PM
   // Detect user's timezone (or use event timezone setting)
   // Convert to UTC: 6:00 PM EST → 23:00 UTC
   ```

2. **eventUtils.ts**: Convert TRUE UTC to local on display
   ```typescript
   // Database has 23:00 UTC
   // Detect display timezone
   // Convert to local: 23:00 UTC → 6:00 PM EST
   ```

3. **All display components**: Use new conversion functions

### Phase 2: Session-Based Timing

1. **TimeZoneService**: New method that accepts Session
   ```csharp
   public Task<bool> IsActionAllowedForSessionAsync(
       Session session,
       decimal? closeHours,
       CancellationToken ct)
   {
       var hoursUntilStart = (session.StartTime - DateTime.UtcNow).TotalHours;
       return hoursUntilStart >= (double)(closeHours ?? 0);
   }
   ```

2. **VolunteerService**: Use session timing when position has SessionId
   ```csharp
   if (position.SessionId.HasValue)
   {
       canSignUp = await _timeZoneService.IsActionAllowedForSessionAsync(
           position.Session,
           eventEntity.VolunteerRegistrationCloseHours, ct);
   }
   else
   {
       // Event-wide position: use earliest future session
       canSignUp = await _timeZoneService.IsActionAllowedAsync(...);
   }
   ```

3. **AttendanceService**: Use session timing for session-specific tickets
   - Ticket types already have SessionIds
   - Check timing against associated session(s)

### Phase 3: UI Updates

1. **EventDetailPage**: Show available sessions for tickets
   - Hide sessions whose sales window has closed
   - Show "Session X - Sales open until Y"

2. **VolunteerPositionCard**: Show session timing
   - "Signup closes 24 hours before this session"
   - Different message for event-wide vs session-specific

3. **Admin Forms**: Clear indication of session-based timing

### Phase 4: Event.StartDate Simplification

1. Make StartDate a computed property (earliest session)
2. OR keep it for display/filtering but remove timing dependency
3. Remove auto-recalculation (no longer needed)

---

## 6. SIMPLE APPROACH - Minimum Changes

Given no live data, the simplest approach:

### 6.1 Keep Current Fields, Change Interpretation

1. **Session.StartTime/EndTime**: Store TRUE UTC
2. **Event timing fields**: Apply to sessions individually
3. **Event.StartDate**: Computed from sessions (for filtering/display)

### 6.2 Conversion Points

**Save (Frontend → Backend):**
- User enters local time
- Frontend converts to UTC using event timezone
- Backend stores UTC

**Display (Backend → Frontend):**
- Backend sends UTC
- Frontend converts to local using event timezone

### 6.3 Timing Calculation

```csharp
// For session-specific features (volunteer positions with SessionId, session tickets):
hoursUntilSession = (session.StartTime - DateTime.UtcNow).TotalHours;

// For event-wide features:
earliestFutureSession = event.Sessions.Where(s => s.StartTime > now).Min(s => s.StartTime);
hoursUntilEvent = (earliestFutureSession - DateTime.UtcNow).TotalHours;
```

---

## 7. QUESTIONS TO RESOLVE

1. **Timezone Source**: Use system setting (America/New_York) or user preference?
2. **Event-wide positions**: Use earliest future session or require session assignment?
3. **Display**: Show all sessions or only those still "available"?
4. **Backward compatibility**: Needed? (No live data suggests no)

---

## 8. NEXT STEPS

- [ ] Review this document with user
- [ ] Decide on approach (phases or all-at-once)
- [ ] Create implementation tasks
- [ ] Start with Phase 1 (UTC fix) as foundation
- [ ] Build out session-based timing
- [ ] Update all UI components
- [ ] Test thoroughly
