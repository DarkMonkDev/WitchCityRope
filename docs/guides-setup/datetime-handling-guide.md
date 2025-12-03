# DateTime Handling Guide - WitchCityRope

**Created**: 2025-12-02
**Status**: Active
**Purpose**: Comprehensive guide for storing, converting, and displaying dates and times

---

## TABLE OF CONTENTS

1. [Overview](#1-overview)
2. [Architecture Principles](#2-architecture-principles)
3. [Data Flow Diagram](#3-data-flow-diagram)
4. [Backend Implementation](#4-backend-implementation)
5. [Frontend Implementation](#5-frontend-implementation)
6. [Common Patterns](#6-common-patterns)
7. [Business Logic Considerations](#7-business-logic-considerations)
8. [Troubleshooting](#8-troubleshooting)
9. [Related Documentation](#9-related-documentation)

---

## 1. OVERVIEW

### 1.1 The Problem We Solved

Datetime handling in web applications is notoriously complex. Key challenges:

1. **User enters local time** - "6:00 PM" in their timezone
2. **Database stores UTC** - Must be consistent, timezone-agnostic
3. **Display shows local time** - Back to what users expect to see
4. **Business logic uses UTC** - Comparisons must be consistent

### 1.2 Our Approach

**Simple rule**: All events are in Salem, MA. We use a **global timezone** (`America/New_York`).

| Location | Format | Timezone |
|----------|--------|----------|
| User Interface | Local display | America/New_York (Eastern) |
| API Transport | ISO 8601 UTC | UTC (Z suffix) |
| Database Storage | DateTime | UTC |
| Business Logic | DateTime | UTC |

---

## 2. ARCHITECTURE PRINCIPLES

### 2.1 Core Rules

1. **Database stores TRUE UTC** - Not "naive UTC" where local time is stored as if it were UTC
2. **Conversion happens at boundaries** - DTO layer for backend, utility functions for frontend
3. **Global timezone setting** - `America/New_York` for all events (configurable in admin settings)
4. **Business logic uses UTC** - All timing calculations compare `DateTime.UtcNow` to stored UTC values

### 2.2 What "True UTC" Means

**WRONG (Naive UTC):**
```
User enters: 6:00 PM Eastern
Stored as: 2025-12-01T18:00:00Z  ← WRONG! This is actually 1:00 PM Eastern
```

**CORRECT (True UTC):**
```
User enters: 6:00 PM Eastern (EST = UTC-5)
Stored as: 2025-12-01T23:00:00Z  ← CORRECT! 6 PM EST = 11 PM UTC
```

### 2.3 Timezone Configuration

**Source**: Admin Settings → `EventTimeZone` setting (defaults to `America/New_York`)

All WitchCityRope events are physical events in Salem, MA. The timezone is global, not per-event.

---

## 3. DATA FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              DATA FLOW                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ USER INTERFACE (React)                                             │ │
│  │ User sees: "Dec 4, 2025 at 10:25 PM" (Eastern Time)                │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                    ↓ SAVE                              ↑ LOAD           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ FRONTEND CONVERSION (eventUtils.ts)                                │ │
│  │                                                                    │ │
│  │ SAVE: localTimeStringToUtc(date, "22:25", "America/New_York")     │ │
│  │       → "2025-12-05T03:25:00.000Z"                                 │ │
│  │                                                                    │ │
│  │ LOAD: utcToLocal("2025-12-05T03:25:00.000Z", "America/New_York")  │ │
│  │       → { hours: 22, minutes: 25, formatted: "10:25 PM" }          │ │
│  │                                                                    │ │
│  │ DATE EXTRACTION: Parse ISO string, extract YYYY-MM-DD portion      │ │
│  │       "2025-12-05T03:25:00.000Z" → date portion handled by backend │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                    ↓                                   ↑                │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ API TRANSPORT (HTTP)                                               │ │
│  │ JSON body: { "startTime": "2025-12-05T03:25:00.000Z" }             │ │
│  │ All times as ISO 8601 UTC strings with Z suffix                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                    ↓                                   ↑                │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ BACKEND DTO (SessionDto.cs)                                        │ │
│  │                                                                    │ │
│  │ SAVE: sessionDto.StartTime.ToUniversalTime() → Session.StartTime   │ │
│  │                                                                    │ │
│  │ LOAD: Convert UTC to local, THEN extract date:                     │ │
│  │       var localTime = ConvertFromUtc(session.StartTime, eastern);  │ │
│  │       StartDate = localTime.Date; // Dec 4 (local), not Dec 5 (UTC)│ │
│  │       StartTime = session.StartTime; // Full UTC datetime          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                    ↓                                   ↑                │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ DATABASE (PostgreSQL)                                              │ │
│  │ Session.StartTime = "2025-12-05 03:25:00" (timestamptz, UTC)       │ │
│  │ Session.EndTime = "2025-12-05 08:00:00" (timestamptz, UTC)         │ │
│  │                                                                    │ │
│  │ NOTE: No separate Date column - date derived from StartTime        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ BUSINESS LOGIC (Services)                                          │ │
│  │ All comparisons use UTC:                                           │ │
│  │   var hoursUntil = (session.StartTime - DateTime.UtcNow).TotalHours│ │
│  │   var isOpen = hoursUntil > registrationCloseHours;                │ │
│  │                                                                    │ │
│  │ NO timezone conversion needed for timing calculations!             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. BACKEND IMPLEMENTATION

### 4.1 Entity Model (Session.cs)

```csharp
/// <summary>
/// Session start time in UTC.
///
/// DATETIME HANDLING:
/// - Stored as TRUE UTC in database (timestamptz)
/// - Example: User enters 10:25 PM EST → stored as 3:25 AM UTC next day
/// - Business logic compares directly with DateTime.UtcNow
///
/// EXTRACTING LOCAL DATE:
/// Do NOT use: session.StartTime.Date (gives UTC date)
/// DO use: TimeZoneInfo.ConvertTimeFromUtc(StartTime, easternZone).Date
///
/// See: /docs/guides-setup/datetime-handling-guide.md
/// </summary>
[Required]
public DateTime StartTime { get; set; }

/// <summary>
/// Session end time in UTC.
/// Same rules as StartTime.
/// </summary>
[Required]
public DateTime EndTime { get; set; }
```

### 4.2 DTO Layer (SessionDto.cs)

```csharp
/// <summary>
/// Converts UTC time to local date for display.
/// This is the ONLY place where UTC-to-local conversion happens for dates.
/// </summary>
public SessionDto(Session session)
{
    // Get the event timezone (global setting)
    var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    // Ensure Kind is UTC for proper conversion
    var utcStartTime = DateTime.SpecifyKind(session.StartTime, DateTimeKind.Utc);
    var utcEndTime = DateTime.SpecifyKind(session.EndTime, DateTimeKind.Utc);

    // Convert to local timezone BEFORE extracting date
    var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(utcStartTime, easternZone);
    var localEndTime = TimeZoneInfo.ConvertTimeFromUtc(utcEndTime, easternZone);

    // Now extract dates from LOCAL times (not UTC!)
    StartDate = localStartTime.Date;  // Correct: Dec 4 (local)
    EndDate = localEndTime.Date;      // Not: Dec 5 (UTC)

    // Full UTC datetimes for time display and calculations
    StartTime = session.StartTime;
    EndTime = session.EndTime;
}
```

### 4.3 Service Layer - Business Logic

```csharp
// CORRECT: Business logic uses UTC-to-UTC comparison
var now = DateTime.UtcNow;
var hoursUntilSession = (session.StartTime - now).TotalHours;
var canPurchase = hoursUntilSession >= eventEntity.RegistrationCloseHours;

// CORRECT: Getting "today" for queries
var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
var todayLocal = localNow.Date;

// WRONG: Don't use DateTime.UtcNow.Date for "today" queries
// var today = DateTime.UtcNow.Date;  // At 11 PM EST, this is tomorrow in UTC!
```

### 4.4 TimeZoneService

The `TimeZoneService` provides centralized timezone handling:

```csharp
public interface ITimeZoneService
{
    TimeZoneInfo GetEventTimeZone();
    DateTime ConvertToLocal(DateTime utcDateTime);
    DateTime ConvertToUtc(DateTime localDateTime);
    DateTime GetLocalToday();
}
```

---

## 5. FRONTEND IMPLEMENTATION

### 5.1 Core Utility Functions (eventUtils.ts)

**Location**: `/apps/web/src/utils/eventUtils.ts`

```typescript
// Global timezone for all events
export const DEFAULT_EVENT_TIMEZONE = 'America/New_York';

/**
 * Convert local time to TRUE UTC for API/database storage
 *
 * @example
 * // User enters 6:00 PM on Dec 1, 2025 (Eastern time)
 * localToUtc(new Date(2025, 11, 1), 18, 0)
 * // Returns "2025-12-01T23:00:00.000Z" (EST = UTC-5)
 */
export function localToUtc(
  date: Date,
  hours: number,
  minutes: number,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string;

/**
 * Convert UTC ISO string to local time for display
 *
 * @example
 * utcToLocal("2025-12-01T23:00:00.000Z")
 * // Returns { hours: 18, minutes: 0, formatted: "6:00 PM" }
 */
export function utcToLocal(
  isoString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): { hours: number; minutes: number; formatted: string; time24: string };

/**
 * Format UTC datetime to local time string for display
 *
 * @example
 * formatUtcToLocalTime("2025-12-01T23:00:00.000Z")
 * // Returns "6:00 PM"
 */
export function formatUtcToLocalTime(
  isoString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string;
```

### 5.2 Form Handling (SessionFormModal.tsx)

```typescript
const handleSubmit = (values) => {
  // Convert local times to TRUE UTC using the event timezone
  const startDateTime = localTimeStringToUtc(
    calendarDate,
    values.startTime,
    DEFAULT_EVENT_TIMEZONE
  );
  const endDateTime = localTimeStringToUtc(
    endCalendarDate,
    values.endTime,
    DEFAULT_EVENT_TIMEZONE
  );

  const sessionData = {
    startTime: startDateTime,  // True UTC ISO string
    endTime: endDateTime,      // True UTC ISO string
  };

  onSubmit(sessionData);
};
```

### 5.3 Display Handling

```typescript
// Displaying time from UTC
const displayTime = formatUtcToLocalTime(session.startTime);
// "10:25 PM"

// Displaying date - parse the startDate from DTO (already local date)
const displayDate = new Date(session.startDate).toLocaleDateString('en-US', {
  weekday: 'long',
  month: 'short',
  day: 'numeric',
  year: 'numeric'
});
// "Thursday, Dec 4, 2025"
```

---

## 6. COMMON PATTERNS

### 6.1 Pattern: Save DateTime to Database

**Frontend:**
```typescript
const utcDateTime = localTimeStringToUtc(date, timeString, DEFAULT_EVENT_TIMEZONE);
// Send to API
```

**Backend:**
```csharp
session.StartTime = sessionDto.StartTime.ToUniversalTime();
await _context.SaveChangesAsync();
```

### 6.2 Pattern: Load DateTime for Display

**Backend (DTO):**
```csharp
var localTime = TimeZoneInfo.ConvertTimeFromUtc(session.StartTime, easternZone);
StartDate = localTime.Date;
StartTime = session.StartTime;  // Keep full UTC for frontend time conversion
```

**Frontend:**
```typescript
// Time display
const time = formatUtcToLocalTime(session.startTime);

// Date display (startDate is already local from DTO)
const date = formatDate(session.startDate);
```

### 6.3 Pattern: Query "Today's" Data

```csharp
// CORRECT: Get local "today"
var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
var todayStart = localNow.Date;
var todayEnd = todayStart.AddDays(1);

// Convert back to UTC for database query
var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStart, easternZone);
var todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(todayEnd, easternZone);

// Query
var todaysEvents = await _context.Sessions
    .Where(s => s.StartTime >= todayStartUtc && s.StartTime < todayEndUtc)
    .ToListAsync();
```

### 6.4 Pattern: Business Logic Timing Checks

```csharp
// All timing calculations use UTC-to-UTC comparison
// NO timezone conversion needed!
var now = DateTime.UtcNow;
var hoursUntilSession = (session.StartTime - now).TotalHours;

// Check if within registration window
var canRegister = hoursUntilSession >= (double)eventEntity.RegistrationCloseHours;
```

---

## 7. BUSINESS LOGIC CONSIDERATIONS

### 7.1 Session-Based Timing

All timing calculations are based on **session start times**, not event dates:

- **Single-session ticket**: Uses that session's StartTime
- **Multi-session ticket**: Uses the FIRST (earliest) FUTURE session's StartTime
- **Volunteer position**: Uses assigned session's StartTime (or earliest future if event-wide)

### 7.2 Timing Window Calculations

```csharp
// Reference: Session.StartTime (TRUE UTC)
// RegistrationOpenHours: 168 (7 days before)
// RegistrationCloseHours: 24 (1 day before)

var hoursUntilSession = (session.StartTime - DateTime.UtcNow).TotalHours;

// Sales window check
var salesOpen = hoursUntilSession <= eventEntity.RegistrationOpenHours;
var salesClosed = hoursUntilSession < eventEntity.RegistrationCloseHours;
var canPurchase = salesOpen && !salesClosed;
```

### 7.3 DST (Daylight Saving Time) Handling

The `America/New_York` timezone automatically handles DST transitions:

- **EST** (Eastern Standard Time): UTC-5 (Nov - Mar)
- **EDT** (Eastern Daylight Time): UTC-4 (Mar - Nov)

Both `TimeZoneInfo` (C#) and `Intl.DateTimeFormat` (JavaScript) handle this automatically when you use IANA timezone IDs.

---

## 8. TROUBLESHOOTING

### 8.1 Date Shows Wrong Day

**Symptom**: User enters Dec 4 but UI shows Dec 5

**Cause**: Date extracted from UTC datetime without timezone conversion

**Fix**: Convert UTC to local BEFORE extracting `.Date`

```csharp
// WRONG
Date = session.StartTime.Date;  // UTC date

// CORRECT
var localTime = TimeZoneInfo.ConvertTimeFromUtc(session.StartTime, easternZone);
StartDate = localTime.Date;  // Local date
```

### 8.2 Time Shows Wrong Hour

**Symptom**: User enters 6:00 PM but UI shows 11:00 PM (or 1:00 PM)

**Cause**: Missing or double timezone conversion

**Check**:
1. Is frontend using `localTimeStringToUtc()` when saving?
2. Is frontend using `utcToLocal()` when displaying?
3. Is the timezone correct (`America/New_York`)?

### 8.3 "Today" Query Returns Wrong Results

**Symptom**: Events appearing/missing around midnight

**Cause**: Using `DateTime.UtcNow.Date` instead of local "today"

**Fix**: Convert to local timezone first, then get date

```csharp
// WRONG
var today = DateTime.UtcNow.Date;

// CORRECT
var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
var today = localNow.Date;
```

### 8.4 Timing Windows Off By Hours

**Symptom**: Registration closes at wrong time

**Cause**: Business logic is correct (UTC-to-UTC). Display might be wrong.

**Check**: Business logic should NOT convert timezones. Only display code converts.

---

## 9. RELATED DOCUMENTATION

### 9.1 Implementation Specifications

- **Session Timing Refactor**: `/docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md`
- **Timezone Handling Proposal**: `/docs/functional-areas/events/event-timezone-handling-proposal.md`

### 9.2 Code Locations

| Component | File | Purpose |
|-----------|------|---------|
| Frontend Utilities | `/apps/web/src/utils/eventUtils.ts` | UTC conversion functions |
| Session DTO | `/apps/api/Features/Events/Models/SessionDto.cs` | Date extraction |
| Session Entity | `/apps/api/Models/Session.cs` | Database model |
| Session Form | `/apps/web/src/components/events/SessionFormModal.tsx` | User input handling |
| TimeZone Service | `/apps/api/Features/Events/Services/TimeZoneService.cs` | Centralized timezone logic |

### 9.3 Testing

When testing datetime features:

1. **Test late evening times** (10 PM - midnight) - crosses UTC day boundary
2. **Test around DST transitions** (March, November)
3. **Test multi-day sessions** (Friday night to Saturday morning)

---

## QUICK REFERENCE CARD

```
┌─────────────────────────────────────────────────────────────────┐
│                    DATETIME QUICK REFERENCE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  FRONTEND (TypeScript)                                           │
│  ─────────────────────                                           │
│  Save: localTimeStringToUtc(date, "22:25", "America/New_York")   │
│  Load: utcToLocal(isoString, "America/New_York")                 │
│  Display: formatUtcToLocalTime(isoString)                        │
│                                                                  │
│  BACKEND (C#)                                                    │
│  ────────────                                                    │
│  Get timezone: TimeZoneInfo.FindSystemTimeZoneById("America/New_York") │
│  To local: TimeZoneInfo.ConvertTimeFromUtc(utcTime, zone)        │
│  To UTC: TimeZoneInfo.ConvertTimeToUtc(localTime, zone)          │
│  Ensure UTC kind: DateTime.SpecifyKind(dt, DateTimeKind.Utc)     │
│                                                                  │
│  RULES                                                           │
│  ─────                                                           │
│  ✅ Database stores TRUE UTC                                     │
│  ✅ Business logic uses UTC-to-UTC comparisons                   │
│  ✅ Convert at DTO boundary (backend) and display (frontend)     │
│  ✅ Extract date from LOCAL time, not UTC time                   │
│  ❌ Don't use DateTime.UtcNow.Date for "today" queries           │
│  ❌ Don't convert timezones in business logic                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```
