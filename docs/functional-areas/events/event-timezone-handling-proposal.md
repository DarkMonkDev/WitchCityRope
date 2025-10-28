# Event Timezone Handling - Implementation Proposal

**Date**: 2025-10-28
**Feature**: Event Registration Cutoff Based on Start Time
**Status**: Research Complete - Awaiting Decision

---

## Business Requirement

**Primary Goal**: Prevent ticket purchases, ticket cancellations, RSVP creation, and RSVP cancellations after an event's start time.

**Cutoff Rule**: 30 minutes before the event's scheduled start time (to align with the actual local time where the event occurs).

---

## Current Implementation Analysis

### What We Have Now ✓

1. **Database Storage**: PostgreSQL `timestamptz` (timezone-aware timestamps)
2. **Backend**: All dates stored as UTC `DateTime` objects
3. **Comparisons**: Uses `DateTime.UtcNow` for business logic
4. **Frontend Display**: Browser's `toLocaleTimeString()` automatically converts UTC to user's local time

**Files**:
- Backend: `/apps/api/Models/Event.cs`, `/apps/api/Data/ApplicationDbContext.cs`
- Frontend: `/apps/web/src/components/events/public/EventCard.tsx`

### Critical Gaps Identified ✗

1. **No Event Timezone Stored**: We don't know what timezone the event is actually happening in
2. **Frontend Input Ambiguity**: When admin creates event at "2:00 PM", is that UTC? Local? Event location time?
3. **No Timezone Context**: API doesn't receive timezone information from frontend
4. **Display Inconsistency Risk**: Events created in one timezone might display incorrectly to users in other timezones

### Example Problem Scenario

**Current Flow**:
1. Admin in Boston (EST, UTC-5) creates event for "2:00 PM on Nov 5, 2025"
2. Browser creates date in local time: `new Date('2025-11-05T14:00:00')`
3. Converts to ISO: `'2025-11-05T19:00:00Z'` (7 PM UTC)
4. Stored in database as 7 PM UTC
5. User in Boston views event: Browser converts back to 2 PM EST ✓ Works!
6. User in Los Angeles (PST, UTC-8) views same event: Shows 11 AM PST ✗ **Wrong expectation!**

**The real issue**: If this is a **physical event in Boston**, it happens at 2 PM Boston time regardless of where viewers are located. We need to store "2 PM in America/New_York timezone".

---

## Research Findings: Industry Best Practices

### 1. Storage Pattern for Physical Events

**Recommendation from multiple sources**: For physical events occurring at a specific location, **store the event in its local timezone**, not UTC.

**Why?**
- Events are tied to physical locations with fixed local times
- "The workshop starts at 2 PM" is meaningful in the event's local timezone
- Daylight Saving Time adjustments happen automatically when you store timezone ID
- Prevents confusion when event timezone has DST changes between creation and event date

**Pattern**:
```
Event {
  StartDate: DateTime (e.g., 2025-11-05 14:00:00)
  TimeZone: string (e.g., "America/New_York" - IANA timezone ID)
}
```

### 2. DateTime vs DateTimeOffset in .NET

**Microsoft's Recommendation**: Use `DateTimeOffset` for most scenarios instead of `DateTime`.

**Why DateTimeOffset?**
- Includes offset from UTC (e.g., `-05:00`)
- Unambiguously identifies a single point in time
- Better for distributed systems
- Avoids `DateTime.Kind` ambiguity issues

**When to use DateTime?**
- Store opening hours that apply across timezones (e.g., "9 AM - 5 PM" for all locations)

### 3. PostgreSQL Timestamp Handling

**Key Facts**:
- `timestamptz` does NOT store timezone - only stores UTC timestamp
- To store actual timezone, need separate text column for IANA timezone ID
- Npgsql 6.0+ requires `DateTime.Kind = UTC` for `timestamptz` columns
- Can use `timestamp without time zone` for local times (requires explicit column type config)

### 4. NodaTime Library

**Recommendation**: Use NodaTime for robust timezone handling in .NET.

**Benefits**:
- Built-in IANA Time Zone Database (TZDB)
- Cross-platform compatible (Windows/Linux timezone ID differences)
- More accurate DST handling
- Better-designed types than BCL DateTime
- Regularly updated timezone rules

**Integration**: NuGet package `NodaTime` + optional `NodaTime.Serialization.SystemTextJson`

---

## Proposed Solutions

## Option 1: Site-Wide Timezone (RECOMMENDED - SIMPLEST)

**Concept**: All events are assumed to occur in the site's configured timezone (e.g., "America/New_York" for Salem, MA).

### Pros ✓
- **Simplest implementation** - single configuration point
- **No per-event complexity** - all events treated consistently
- **Perfect for single-location organizations** - WitchCityRope events are all in Salem
- **Easy admin experience** - no timezone selection needed per event
- **Minimal database changes** - just add application setting

### Cons ✗
- Not suitable if organization expands to other locations
- All events must be in same timezone

### Implementation Details

#### Backend Changes

**1. Add Application Setting**

**File**: `/apps/api/appsettings.json`
```json
{
  "Application": {
    "EventTimeZone": "America/New_York",
    "EventTimeZoneName": "Eastern Time"
  }
}
```

**2. Create Timezone Service**

**File**: `/apps/api/Services/TimeZoneService.cs`
```csharp
public interface ITimeZoneService
{
    TimeZoneInfo GetEventTimeZone();
    DateTimeOffset ConvertToEventTime(DateTime utcDateTime);
    DateTime ConvertToUtc(DateTime localDateTime);
    bool IsEventStarted(DateTime eventStartDateUtc, int bufferMinutes = 30);
}

public class TimeZoneService : ITimeZoneService
{
    private readonly TimeZoneInfo _eventTimeZone;
    private readonly string _timeZoneId;

    public TimeZoneService(IConfiguration configuration)
    {
        _timeZoneId = configuration["Application:EventTimeZone"] ?? "America/New_York";
        _eventTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
    }

    public TimeZoneInfo GetEventTimeZone() => _eventTimeZone;

    public DateTimeOffset ConvertToEventTime(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTime(utcDateTime, _eventTimeZone);
    }

    public DateTime ConvertToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
            _eventTimeZone
        );
    }

    public bool IsEventStarted(DateTime eventStartDateUtc, int bufferMinutes = 30)
    {
        var now = DateTime.UtcNow;
        var cutoffTime = eventStartDateUtc.AddMinutes(-bufferMinutes);
        return now >= cutoffTime;
    }
}
```

**3. Update Business Logic**

**File**: `/apps/api/Features/Events/Services/EventService.cs`
```csharp
// Inject TimeZoneService
public async Task<(bool Success, string Error)> PurchaseTicketAsync(
    string eventId,
    string sessionId,
    string userId)
{
    var session = await _context.Sessions
        .Include(s => s.Event)
        .FirstOrDefaultAsync(s => s.Id == sessionId);

    if (session == null)
        return (false, "Session not found");

    // Check if registration is still allowed
    if (_timeZoneService.IsEventStarted(session.StartTime, bufferMinutes: 30))
    {
        return (false, "Registration has closed for this session");
    }

    // ... rest of purchase logic
}
```

**4. Add Admin Settings Endpoint (Future)**

**File**: `/apps/api/Features/Admin/Endpoints/SettingsEndpoints.cs`
```csharp
app.MapGet("/api/admin/settings/timezone", (IConfiguration config) =>
{
    return Results.Ok(new
    {
        TimeZoneId = config["Application:EventTimeZone"],
        TimeZoneName = config["Application:EventTimeZoneName"]
    });
});

app.MapPut("/api/admin/settings/timezone", async (
    UpdateTimeZoneRequest request,
    IConfiguration config) =>
{
    // Update configuration (requires custom config provider)
    // Or store in database Settings table
});
```

#### Frontend Changes

**1. Display Event Timezone**

**File**: `/apps/web/src/components/events/public/EventCard.tsx`
```typescript
// Add timezone indicator to display
{(() => {
  if (!event.startDate) return 'TBD'
  const start = new Date(event.startDate)
  return (
    <>
      {start.toLocaleDateString('en-US', {
        weekday: 'long',
        month: 'short',
        day: 'numeric'
      })}
      <Text size="xs" c="dimmed">Eastern Time (Salem, MA)</Text>
    </>
  )
})()}
```

**2. Fetch Timezone Config**

**File**: `/apps/web/src/api/settings.ts`
```typescript
export async function getEventTimeZone(): Promise<{
  timeZoneId: string
  timeZoneName: string
}> {
  const response = await api.get('/api/admin/settings/timezone')
  return response.data
}
```

**3. Show Registration Cutoff Warning**

**File**: `/apps/web/src/components/events/public/EventRegistration.tsx`
```typescript
const getTimeUntilCutoff = (startDate: string): string => {
  const start = new Date(startDate)
  const cutoff = new Date(start.getTime() - 30 * 60 * 1000) // 30 min before
  const now = new Date()

  if (now >= cutoff) {
    return "Registration closed"
  }

  const msUntilCutoff = cutoff.getTime() - now.getTime()
  const hoursUntilCutoff = Math.floor(msUntilCutoff / (1000 * 60 * 60))

  if (hoursUntilCutoff < 1) {
    const minutesUntilCutoff = Math.floor(msUntilCutoff / (1000 * 60))
    return `Registration closes in ${minutesUntilCutoff} minutes`
  }

  return `Registration closes ${cutoff.toLocaleString()}`
}
```

#### Database Changes

**None required** - continues using existing `timestamptz` columns with UTC storage.

---

## Option 2: Per-Event Timezone (MORE FLEXIBLE)

**Concept**: Each event stores its own timezone, allowing events in different locations.

### Pros ✓
- **Scalable** - supports multi-location events
- **Accurate** - each event has explicit timezone
- **Future-proof** - can expand to other regions
- **Handles virtual events** - can mark as "Virtual" with no timezone

### Cons ✗
- **More complex** - requires timezone selection per event
- **Database migration** - need to add timezone column
- **Admin UX burden** - organizers must select timezone
- **Potential errors** - wrong timezone selection could confuse users

### Implementation Details

#### Database Changes

**Migration**: Add `TimeZone` column to Events table

```csharp
public class Event
{
    // ... existing fields

    /// <summary>
    /// IANA timezone ID where event occurs (e.g., "America/New_York")
    /// Null for virtual events or when using site default
    /// </summary>
    public string? TimeZone { get; set; }
}
```

**SQL Migration**:
```sql
ALTER TABLE "Events"
ADD COLUMN "TimeZone" TEXT NULL;

-- Set default for existing events
UPDATE "Events"
SET "TimeZone" = 'America/New_York'
WHERE "TimeZone" IS NULL;
```

#### Backend Changes

**EventDto** - Add timezone field:
```csharp
public string? TimeZone { get; set; }
```

**EventService** - Timezone-aware logic:
```csharp
public bool IsRegistrationOpen(Event evt, int bufferMinutes = 30)
{
    var timeZone = string.IsNullOrEmpty(evt.TimeZone)
        ? TimeZoneInfo.FindSystemTimeZoneById("America/New_York")  // Site default
        : TimeZoneInfo.FindSystemTimeZoneById(evt.TimeZone);

    var eventLocalTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartDate, timeZone);
    var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

    var cutoffTime = eventLocalTime.AddMinutes(-bufferMinutes);
    return nowLocal < cutoffTime;
}
```

#### Frontend Changes

**Create Event Form** - Add timezone selector:
```typescript
<Select
  label="Event Timezone"
  data={[
    { value: 'America/New_York', label: 'Eastern Time' },
    { value: 'America/Chicago', label: 'Central Time' },
    { value: 'America/Denver', label: 'Mountain Time' },
    { value: 'America/Los_Angeles', label: 'Pacific Time' },
  ]}
  defaultValue="America/New_York"
  {...form.getInputProps('timeZone')}
/>
```

**Event Display** - Show timezone:
```typescript
<Text size="sm" c="dimmed">
  {formatEventDateTime(event.startDate)} {event.timeZone === 'America/New_York' ? 'ET' : getTimezoneAbbr(event.timeZone)}
</Text>
```

---

## Option 3: NodaTime Integration (MOST ROBUST)

**Concept**: Use NodaTime library for comprehensive timezone handling with IANA database.

### Pros ✓
- **Industry standard** - used by Jon Skeet (DateTime expert)
- **Cross-platform** - works on Windows/Linux/Docker
- **Regularly updated** - IANA database updates included
- **Type-safe** - avoids DateTime.Kind issues
- **Better DST handling** - automatically handles timezone rule changes

### Cons ✗
- **Learning curve** - new library and types
- **Migration effort** - need to convert existing DateTime usage
- **NuGet dependency** - adds external package
- **Serialization complexity** - requires custom JSON converters

### Implementation Details

**NuGet Packages**:
```bash
dotnet add package NodaTime
dotnet add package NodaTime.Serialization.SystemTextJson
```

**Event Entity**:
```csharp
using NodaTime;

public class Event
{
    // Store as UTC Instant (point in time)
    public Instant StartDateInstant { get; set; }

    // Store event timezone
    public string TimeZone { get; set; }  // e.g., "America/New_York"

    // Helper property for backward compatibility
    public DateTime StartDate
    {
        get => StartDateInstant.ToDateTimeUtc();
        set => StartDateInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
```

**TimeZoneService with NodaTime**:
```csharp
using NodaTime;

public class NodaTimeZoneService
{
    private readonly DateTimeZone _eventTimeZone;

    public NodaTimeZoneService(IConfiguration configuration)
    {
        var tzId = configuration["Application:EventTimeZone"] ?? "America/New_York";
        _eventTimeZone = DateTimeZoneProviders.Tzdb[tzId];
    }

    public bool IsEventStarted(Instant eventStart, int bufferMinutes = 30)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var cutoff = eventStart - Duration.FromMinutes(bufferMinutes);
        return now >= cutoff;
    }

    public LocalDateTime GetLocalDateTime(Instant instant)
    {
        return instant.InZone(_eventTimeZone).LocalDateTime;
    }
}
```

**JSON Configuration**:
```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    });
```

---

## Comparison Matrix

| Feature | Option 1: Site-Wide | Option 2: Per-Event | Option 3: NodaTime |
|---------|---------------------|---------------------|---------------------|
| **Simplicity** | ⭐⭐⭐⭐⭐ Simplest | ⭐⭐⭐ Moderate | ⭐⭐ Complex |
| **Implementation Time** | 4-6 hours | 8-12 hours | 12-16 hours |
| **Database Migration** | ❌ None needed | ✅ Add timezone column | ✅ Add timezone column + Instant fields |
| **Multi-Location Support** | ❌ No | ✅ Yes | ✅ Yes |
| **DST Handling** | ✅ .NET built-in | ✅ .NET built-in | ⭐ NodaTime superior |
| **Cross-Platform** | ⚠️ Potential Windows/Linux issues | ⚠️ Potential Windows/Linux issues | ✅ Fully cross-platform |
| **Admin UX** | ⭐⭐⭐⭐⭐ No config needed | ⭐⭐⭐ Timezone dropdown | ⭐⭐⭐ Timezone dropdown |
| **Type Safety** | ⭐⭐⭐ DateTime | ⭐⭐⭐ DateTime | ⭐⭐⭐⭐⭐ Instant/LocalDateTime |
| **Maintenance** | ✅ Low | ✅ Low | ⚠️ Requires NuGet updates |
| **Suitable For** | Single-location orgs | Multi-location orgs | Enterprise apps |

---

## Recommendation

### For WitchCityRope: **Option 1 (Site-Wide Timezone)** ⭐ RECOMMENDED

**Rationale**:
1. **All events are in Salem, MA** - single location organization
2. **Simplest to implement** - minimal code changes, no database migration
3. **Lowest maintenance burden** - one configuration point
4. **User requested simple solution** - "Simple is better usually"
5. **Easy to upgrade later** - can add per-event timezone if needed in future

**Implementation Steps**:
1. Add `Application:EventTimeZone` to appsettings (5 min)
2. Create `TimeZoneService` (30 min)
3. Update business logic in `EventService` (1-2 hours)
4. Add frontend timezone display (1 hour)
5. Add registration cutoff warnings (1 hour)
6. Test with different scenarios (2 hours)

**Total Estimated Time**: 4-6 hours

**Future Migration Path**: If organization expands to other locations, can add per-event timezone column later without breaking existing functionality.

---

## Testing Strategy

### Test Scenarios

1. **Event Registration Cutoff**
   - Create event starting in 1 hour
   - Attempt registration at T-35 minutes: ✅ Should succeed
   - Attempt registration at T-25 minutes: ❌ Should fail
   - Attempt registration after event starts: ❌ Should fail

2. **Ticket Cancellation Cutoff**
   - Purchase ticket for event starting in 2 hours
   - Cancel at T-35 minutes: ✅ Should succeed
   - Cancel at T-25 minutes: ❌ Should fail

3. **RSVP Cutoff**
   - Same logic as ticket purchase/cancellation

4. **Daylight Saving Time**
   - Create event during DST transition period
   - Verify cutoff time adjusts correctly for DST

5. **Display Consistency**
   - View event from different browser timezones
   - Verify displayed time matches event's local time (Eastern)
   - Verify timezone indicator shown (e.g., "2:00 PM ET")

### Test Data

```csharp
// Seed test event starting 45 minutes from now
var futureEvent = new Event
{
    Title = "Test Event - Registration Open",
    StartDate = DateTime.UtcNow.AddMinutes(45),
    EndDate = DateTime.UtcNow.AddMinutes(105),
    Location = "Salem Community Center",
};

// Seed test event starting 20 minutes from now (should be closed)
var closingSoonEvent = new Event
{
    Title = "Test Event - Registration Closing",
    StartDate = DateTime.UtcNow.AddMinutes(20),
    EndDate = DateTime.UtcNow.AddMinutes(80),
};
```

---

## Open Questions for Decision

1. **Timezone Configuration**: Should site timezone be:
   - ✅ Application setting (appsettings.json) - Recommended for simplicity
   - ⚠️ Database setting (Settings table) - Allows runtime changes via admin panel
   - ⚠️ Environment variable - Cloud deployment flexibility

2. **Cutoff Buffer**: Confirm 30 minutes before start time is correct?
   - Could make this configurable per event type (workshops vs performances)

3. **Display Format**: How should timezone be shown to users?
   - Option A: "2:00 PM ET" (timezone abbreviation)
   - Option B: "2:00 PM Eastern Time" (full timezone name)
   - Option C: "2:00 PM (Salem, MA)" (location-based)

4. **Error Messages**: What message should users see when registration closed?
   - "Registration has closed for this event"
   - "Registration closed 30 minutes before event start"
   - "This event has already started"

5. **Admin Settings Page**: Should timezone be configurable via UI?
   - Now: No admin settings page exists yet
   - Future: Add to admin settings when that page is built

---

## Next Steps

**Awaiting Your Decision On**:
1. Which option to implement (1, 2, or 3)?
2. Timezone configuration method (appsettings, database, or environment)?
3. Cutoff buffer confirmation (30 minutes)?
4. Display format preference

**Once approved, implementation order**:
1. Backend timezone service
2. Business logic updates
3. Frontend display changes
4. Test suite
5. Documentation

---

## References

- [Microsoft: DateTime vs DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)
- [Npgsql: Date and Time Handling](https://www.npgsql.org/doc/types/datetime.html)
- [NodaTime Documentation](https://nodatime.org/)
- [IANA Time Zone Database](https://www.iana.org/time-zones)
- Stack Overflow: [How should I store data for events in different timezones?](https://stackoverflow.com/questions/22061999)
