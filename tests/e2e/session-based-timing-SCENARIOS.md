# Session-Based Timing Test Scenarios

**Created**: 2025-11-30
**Last Updated**: 2025-12-01
**Specification**: `/docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md`

---

## Overview

This document describes all test scenarios for the session-based timing refactor. The core change is that timing calculations now use **Session.StartTime** instead of **Event.StartDate**, ensuring multi-session events work correctly.

---

## TICKET TIMING SCENARIOS

### Scenario T1: Multi-Session Ticket - First Session Passed

**Setup**:
- Event with 3 sessions: Dec 1, Dec 8, Dec 15
- Current date: Dec 3 (Session 1 has passed)
- Ticket type covers all sessions
- RegistrationCloseHours: 24

**Expected Behavior**:
- Reference session = Dec 8 (first FUTURE session)
- Ticket IS available (Dec 8 is > 24 hours away)
- UI shows: "Valid for: Session 2, Session 3"

**Test Files**:
- Integration: `SessionBasedTicketTimingTests.PurchaseTicket_MultiSession_UsesFirstFutureSession`
- E2E: `session-based-ticket-timing.spec.ts` → "multi-session event shows tickets for future sessions only"

---

### Scenario T2: Multi-Session Ticket - All Sessions Passed

**Setup**:
- Event with 2 sessions: Dec 1, Dec 8
- Current date: Dec 10 (both sessions passed)
- Ticket type covers both sessions

**Expected Behavior**:
- Reference session = null (no future sessions)
- Ticket NOT available (hidden from UI)
- UI shows: "All ticket sales have ended"

**Test Files**:
- Integration: `SessionBasedTicketTimingTests.PurchaseTicket_AllSessionsPassed_Fails`
- E2E: `session-based-ticket-timing.spec.ts` → "event with all sessions passed shows no tickets"

---

### Scenario T3: Single-Session Ticket

**Setup**:
- Event with 3 sessions
- Ticket type covers only Session 2
- Current date before Session 2
- RegistrationCloseHours: 24

**Expected Behavior**:
- Reference session = Session 2
- Availability based solely on Session 2 timing
- If Session 1 passes, ticket still available (based on Session 2)

**Test Files**:
- Integration: `SessionBasedTicketTimingTests.PurchaseTicket_SingleSession_UsesSessionTime`
- E2E: `session-based-ticket-timing.spec.ts` → "ticket shows reference session name"

---

### Scenario T4: Sales Window Not Yet Open

**Setup**:
- Session starts Dec 15
- RegistrationOpenHours: 168 (7 days before)
- Current date: Dec 5 (10 days before)

**Expected Behavior**:
- hoursUntilSession = 240 hours
- 240 > 168 = sales NOT open yet
- UI shows: "Sales open on Dec 8"

**Test Files**:
- E2E: `session-based-ticket-timing.spec.ts` → "unavailable ticket shows availability message"

---

### Scenario T5: Sales Window Closed

**Setup**:
- Session starts Dec 10, 6 PM
- RegistrationCloseHours: 24
- Current date: Dec 9, 7 PM (23 hours before)

**Expected Behavior**:
- hoursUntilSession = 23 hours
- 23 < 24 = sales CLOSED
- UI shows: "Sales closed"

**Test Files**:
- E2E: `session-based-ticket-timing.spec.ts` → "ticket sales window based on registration hours settings"

---

### Scenario T6: Ticket Cancellation Timing

**Setup**:
- Multi-session ticket covering Dec 1, Dec 8
- Current date: Dec 3 (Session 1 passed)
- CancellationCloseHours: 48

**Expected Behavior**:
- Reference session = Dec 8 (first future)
- hoursUntilSession = ~120 hours
- 120 > 48 = CAN cancel
- Cancel button enabled

**Test Files**:
- Integration: `SessionBasedTicketTimingTests.CancelTicket_MultiSession_UsesFirstFutureSession`
- E2E: `session-based-ticket-timing.spec.ts` → "ticket cancellation uses session-based timing"

---

## VOLUNTEER TIMING SCENARIOS

### Scenario V1: Session-Specific Position - Future Session

**Setup**:
- Position assigned to Session 2 (Dec 8)
- Current date: Dec 3
- VolunteerRegistrationCloseHours: 24

**Expected Behavior**:
- Reference session = Session 2
- hoursUntilSession = ~120 hours
- 120 > 24 = CAN sign up
- Position shows signup button

**Test Files**:
- Integration: `SessionBasedVolunteerTimingTests.VolunteerSignup_SessionSpecific_UsesSessionTiming`
- E2E: `session-based-volunteer-timing.spec.ts` → "session-specific volunteer position shows for future session"

---

### Scenario V2: Session-Specific Position - Past Session

**Setup**:
- Position assigned to Session 1 (Dec 1)
- Current date: Dec 3 (Session 1 passed)

**Expected Behavior**:
- Position NOT returned by API
- Position NOT visible in UI
- Other session positions still visible

**Test Files**:
- Integration: `SessionBasedVolunteerTimingTests.VolunteerSignup_PastSession_NotReturned`
- E2E: `session-based-volunteer-timing.spec.ts` → "past session volunteer position is hidden"

---

### Scenario V3: Event-Wide Position - Mixed Sessions

**Setup**:
- Position with SessionId = null (event-wide)
- Session 1: Dec 1 (passed)
- Session 2: Dec 8 (future)
- Session 3: Dec 15 (future)
- VolunteerRegistrationCloseHours: 24

**Expected Behavior**:
- Reference session = Dec 8 (earliest FUTURE)
- NOT Dec 1 (past), NOT Dec 15 (not earliest)
- Timing calculated from Dec 8
- Position IS available

**Test Files**:
- Integration: `SessionBasedVolunteerTimingTests.VolunteerSignup_EventWide_UsesEarliestFutureSession`
- E2E: `session-based-volunteer-timing.spec.ts` → "event-wide position available after first session passes"

---

### Scenario V4: Event-Wide Position - All Sessions Passed

**Setup**:
- Position with SessionId = null
- All sessions in the past

**Expected Behavior**:
- No future sessions = no reference session
- Position NOT returned by API
- Position NOT visible in UI

**Test Files**:
- Integration: `SessionBasedVolunteerTimingTests` (implicit in other tests)
- E2E: Covered by seed data scenarios

---

### Scenario V5: Volunteer Cancellation Timing

**Setup**:
- User has volunteer signup for Session 2 (Dec 8)
- Current date: Dec 3
- VolunteerCancellationCloseHours: 48

**Expected Behavior**:
- Reference session = Session 2
- hoursUntilSession = ~120 hours
- 120 > 48 = CAN cancel
- Cancel button enabled

**Test Files**:
- Integration: `SessionBasedVolunteerTimingTests.VolunteerCancel_UsesSessionTiming`
- E2E: `session-based-volunteer-timing.spec.ts` → "volunteer cancellation respects session timing"

---

### Scenario V6: Session Display on Position

**Setup**:
- Multi-session event
- Position assigned to Session 2

**Expected Behavior**:
- Position card shows session name
- Session badge visible (e.g., "Session 2")
- User knows which session they're volunteering for

**Test Files**:
- E2E: `session-based-volunteer-timing.spec.ts` → "volunteer signup shows session name"

---

## EDGE CASE SCENARIOS

### Scenario E1: Event With No Sessions

**Setup**:
- Event created without any sessions
- Ticket types exist

**Expected Behavior**:
- Tickets show as unavailable
- Graceful error handling
- UI shows informative message

**Test Files**:
- Integration: TimeZoneService edge case tests

---

### Scenario E2: Session at Exact Current Time

**Setup**:
- Session.StartTime = now

**Expected Behavior**:
- Treat as passed (strict > comparison)
- Session NOT used as reference
- Falls back to next session if available

**Test Files**:
- Integration: TimeZoneService boundary tests

---

### Scenario E3: Null Timing Fields

**Setup**:
- Event with RegistrationOpenHours = null
- Event with RegistrationCloseHours = null

**Expected Behavior**:
- No timing restriction applied
- Tickets always available (if sessions future)
- Current behavior preserved

**Test Files**:
- Integration: TimeZoneService null handling tests

---

### Scenario E4: Negative Timing Hours

**Setup**:
- CancellationCloseHours = -24

**Expected Behavior**:
- Allows cancellation up to 24 hours AFTER session starts
- Unusual but valid configuration
- Backend handles gracefully

**Test Files**:
- Integration: TimeZoneService edge case tests

---

### Scenario E5: Timezone Conversion - DST Boundary

**Setup**:
- Session during DST transition
- 6 PM local time

**Expected Behavior**:
- Correct UTC conversion
- EST (UTC-5) vs EDT (UTC-4) handled
- Display shows correct local time

**Test Files**:
- Unit: TimeZoneService DST tests

---

## ADMIN SCENARIOS

### Scenario A1: View Timing Settings

**Setup**:
- Admin navigates to event edit page

**Expected Behavior**:
- RegistrationOpenHours field visible
- RegistrationCloseHours field visible
- VolunteerRegistrationCloseHours field visible
- VolunteerCancellationCloseHours field visible

**Test Files**:
- E2E: `session-based-timing.spec.ts` → "admin can view session-based timing settings"

---

### Scenario A2: Modify Session Times

**Setup**:
- Admin edits session start/end time

**Expected Behavior**:
- Time input accepts local format (6:00 PM)
- Saves as true UTC
- Displays back as local time

**Test Files**:
- E2E: Session form tests

---

## UI VERIFICATION SCENARIOS

### Scenario U1: Ticket Shows Available Sessions

**Setup**:
- Multi-session ticket (Sessions 1, 2, 3)
- Session 1 passed

**Expected Behavior**:
- UI shows: "Valid for: Session 2, Session 3"
- Session 1 NOT listed (past)

**Test Files**:
- E2E: `session-based-ticket-timing.spec.ts` → "multi-session ticket shows all future sessions"

---

### Scenario U2: Availability Message Display

**Setup**:
- Ticket not yet purchasable
- OR ticket sales closed

**Expected Behavior**:
- Clear message explaining status
- "Sales open on [date]" or "Sales closed"
- Purchase button disabled/hidden

**Test Files**:
- E2E: `session-based-ticket-timing.spec.ts` → "unavailable ticket shows availability message"

---

### Scenario U3: Volunteer Position Status

**Setup**:
- Position within signup window

**Expected Behavior**:
- Signup button enabled
- Shows "X slots remaining"
- Session name displayed

**Test Files**:
- E2E: `session-based-volunteer-timing.spec.ts` → "session-specific volunteer position shows for future session"

---

## Test Data Requirements

### Seed Data Events for E2E Testing

1. **Rope Fundamentals Intensive** (3 sessions)
   - Used for: Multi-session ticket timing tests

2. **Suspension Basics** (2 sessions)
   - Used for: Session-specific volunteer tests

3. **Advanced Suspension Techniques** (2 sessions)
   - Used for: Backup multi-session event

### Test Data Created by Integration Tests

Integration tests create their own isolated test data:
- Events with specific session configurations
- Volunteer positions with specific SessionIds
- Controlled timing field values

This ensures test isolation and reproducibility.

---

## Verification Matrix

| Scenario | Backend | E2E | Manual QA |
|----------|---------|-----|-----------|
| T1: Multi-session, first passed | ✅ | ✅ | |
| T2: All sessions passed | ✅ | ✅ | |
| T3: Single-session ticket | ✅ | ✅ | |
| T4: Sales not open | | ✅ | |
| T5: Sales closed | | ✅ | |
| T6: Cancellation timing | ✅ | ✅ | |
| V1: Session-specific future | ✅ | ✅ | |
| V2: Past session hidden | ✅ | ✅ | |
| V3: Event-wide mixed | ✅ | ✅ | |
| V4: Event-wide all past | ✅ | | |
| V5: Volunteer cancel | ✅ | ✅ | |
| V6: Session display | | ✅ | |
| E1: No sessions | ✅ | | |
| E2: Exact current time | ✅ | | |
| E3: Null timing fields | ✅ | | |
| E4: Negative timing | ✅ | | |
| E5: DST boundary | ✅ | | |
| A1: Admin settings | | ✅ | |
| A2: Session time edit | | ✅ | |
