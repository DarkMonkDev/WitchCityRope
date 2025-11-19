# Backend Developer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: Database Designer -->
<!-- To: Backend Developer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 CRITICAL BACKEND REQUIREMENTS (MUST IMPLEMENT)

### 1. Refactor TimeZoneService Method Signature
**Rule**: Replace single `IsRegistrationOpenAsync()` with action-type-aware `IsActionAllowedAsync()`.
- ✅ Correct: `Task<bool> IsActionAllowedAsync(Event eventEntity, EventActionType actionType)`
- ❌ Wrong: Keep `IsRegistrationOpenAsync()` - doesn't support granular timing
- ❌ Wrong: Create multiple methods - violates DRY principle

### 2. NULL Means No Restriction (Backward Compatibility)
**Rule**: NULL timing fields must return `true` (action allowed).
- ✅ Correct: `if (eventEntity.RegistrationCloseHours == null) return true;`
- ❌ Wrong: Treat NULL as 0 - breaks existing events
- ❌ Wrong: Throw exception on NULL - breaks existing events

### 3. Action Type Determines Which Fields to Check
**Rule**: Each action type must check the correct timing fields.
- ✅ Correct: GetRsvp checks RegistrationOpenHours and RegistrationCloseHours
- ❌ Wrong: All actions check same fields - violates requirements

### 4. Create New Volunteer Cancel Endpoint
**Rule**: Users must be able to cancel their own volunteer assignments via API.
- ✅ Correct: `POST /api/volunteer-signups/{signupId}/cancel` with authentication
- ❌ Wrong: Admin-only cancel - users cannot self-cancel
- ❌ Wrong: DELETE endpoint - cancel is a state change, not deletion

### 5. Update All Existing Enforcement Points
**Rule**: Replace all `IsRegistrationOpenAsync()` calls with `IsActionAllowedAsync()`.
- ✅ Correct: Update AttendanceService.cs (3 locations) and VolunteerService.cs (2 new locations)
- ❌ Wrong: Leave old method calls - timing controls won't work

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Implementation Plan | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md` | API Endpoint Changes section |
| Database Designer Handoff | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/database-designer-handoff.md` | Data model specification |
| TimeZoneService Current | `/apps/api/Services/TimeZoneService.cs` | Existing `IsRegistrationOpenAsync()` method |
| AttendanceService Current | `/apps/api/Features/Attendance/AttendanceService.cs` | Lines 251-257, 462-468, 695-710 |
| VolunteerService Current | `/apps/api/Features/Volunteers/VolunteerService.cs` | Current volunteer signup logic |

## 🚨 KNOWN PITFALLS

### Pitfall 1: Treating NULL as Zero
**Why it happens**: Nullable decimals can be confusing
**How to avoid**: Explicitly check `== null` first, return `true` for NULL (no restriction)

### Pitfall 2: Checking Wrong Fields for Action Types
**Why it happens**: Six different fields, easy to mix up
**How to avoid**: Use switch statement on EventActionType, each case checks correct fields

### Pitfall 3: Forgetting to Add Timing Enforcement to Volunteers
**Why it happens**: Current code doesn't enforce volunteer timing
**How to avoid**: Add `IsActionAllowedAsync()` calls to BOTH volunteer signup AND new cancel endpoint

### Pitfall 4: Breaking Existing Events Without Timing Config
**Why it happens**: Assume all events have timing configured
**How to avoid**: Extensive testing with NULL timing fields (existing events)

### Pitfall 5: Incorrect Timezone Calculations
**Why it happens**: Events are timezone-aware, must convert correctly
**How to avoid**: Use existing `TimeZoneService.ConvertToEventTimeZone()` methods

## ✅ VALIDATION CHECKLIST

Before proceeding to frontend implementation, verify:

- [ ] EventActionType enum created with 6 values
- [ ] IsActionAllowedAsync() method implemented
- [ ] Method correctly handles NULL timing fields (returns true)
- [ ] Method checks correct fields per action type
- [ ] Method calculates hours until/since event correctly
- [ ] AttendanceService updated (3 locations)
- [ ] VolunteerService updated (2 new locations)
- [ ] New volunteer cancel endpoint created
- [ ] Volunteer cancel endpoint enforces timing
- [ ] Volunteer cancel endpoint validates ownership
- [ ] EventDto includes 6 new timing properties
- [ ] All unit tests passing (95%+ coverage)
- [ ] All integration tests passing (100%)
- [ ] Backward compatibility verified (NULL fields work)

## 🔄 DISCOVERED CONSTRAINTS

### Existing TimeZoneService Pattern
**Location**: `/apps/api/Services/TimeZoneService.cs`
**Current Logic**: Reads PreStartBufferMinutes setting, converts to hours, checks if within buffer
**Impact**: Must maintain similar logic but use per-event fields instead
**Required Changes**: Replace global setting read with event field checks

### Existing Enforcement Locations
**AttendanceService.cs**:
- Line 251-257: RSVP creation enforcement
- Line 462-468: Ticket purchase enforcement
- Line 695-710: RSVP/Ticket cancellation enforcement

**VolunteerService.cs**:
- No current timing enforcement (NEW requirement)
- Must add to volunteer signup method
- Must add to new volunteer cancel method

### Event Entity Already Updated
**Constraint**: Database designer already added 6 timing fields to Event entity
**Impact**: Properties available for use immediately
**Required Changes**: Update EventDto to expose these fields to frontend

## 📊 SERVICE LAYER SPECIFICATION

### EventActionType Enum

```csharp
namespace WitchCityRope.Api.Features.Events;

/// <summary>
/// Types of actions users can perform on events that require timing validation
/// </summary>
public enum EventActionType
{
    /// <summary>RSVP creation for social events</summary>
    GetRsvp,

    /// <summary>RSVP cancellation</summary>
    CancelRsvp,

    /// <summary>Ticket purchase for classes/workshops</summary>
    GetTicket,

    /// <summary>Ticket cancellation/refund</summary>
    CancelTicket,

    /// <summary>Volunteer spot signup</summary>
    GetVolunteer,

    /// <summary>Volunteer assignment cancellation</summary>
    CancelVolunteer
}
```

### TimeZoneService.IsActionAllowedAsync() Method

```csharp
/// <summary>
/// Determines if a specific action is allowed based on event timing configuration
/// </summary>
/// <param name="eventEntity">Event to check timing for</param>
/// <param name="actionType">Type of action user is attempting</param>
/// <returns>True if action is allowed, false if outside timing window</returns>
public async Task<bool> IsActionAllowedAsync(Event eventEntity, EventActionType actionType)
{
    // Get event start time in event's timezone
    var eventStartTime = await ConvertToEventTimeZone(eventEntity.StartDateTime, eventEntity.TimeZoneId);
    var currentTime = await GetCurrentTimeInEventTimeZone(eventEntity.TimeZoneId);

    // Calculate hours until event start (negative = event already started)
    var hoursUntilStart = (eventStartTime - currentTime).TotalHours;

    // Determine which timing fields to check based on action type
    decimal? openHours = null;
    decimal? closeHours = null;

    switch (actionType)
    {
        case EventActionType.GetRsvp:
        case EventActionType.GetTicket:
            openHours = eventEntity.RegistrationOpenHours;
            closeHours = eventEntity.RegistrationCloseHours;
            break;

        case EventActionType.CancelRsvp:
        case EventActionType.CancelTicket:
            openHours = eventEntity.CancellationOpenHours;
            closeHours = eventEntity.CancellationCloseHours;
            break;

        case EventActionType.GetVolunteer:
            // Volunteer signup only has close hours (always open until close)
            closeHours = eventEntity.VolunteerRegistrationCloseHours;
            break;

        case EventActionType.CancelVolunteer:
            // Volunteer cancel only has close hours (always open until close)
            closeHours = eventEntity.VolunteerCancellationCloseHours;
            break;

        default:
            throw new ArgumentException($"Unknown action type: {actionType}", nameof(actionType));
    }

    // Check if action is within allowed timing window
    // NULL means no restriction

    // Check open hours (if configured)
    if (openHours.HasValue)
    {
        // Positive hours = before event, negative = after event
        // If hoursUntilStart > openHours, event hasn't opened yet
        if (hoursUntilStart > openHours.Value)
        {
            return false; // Too early
        }
    }

    // Check close hours (if configured)
    if (closeHours.HasValue)
    {
        // Positive hours = before event, negative = after event
        // If hoursUntilStart < closeHours, window has closed
        if (hoursUntilStart < closeHours.Value)
        {
            return false; // Too late
        }
    }

    // If we made it here, action is allowed
    return true;
}
```

### AttendanceService Updates

**RSVP Creation** (line ~251):
```csharp
// OLD:
if (!await _timeZoneService.IsRegistrationOpenAsync(eventEntity))
{
    throw new InvalidOperationException("Registration is not currently open for this event");
}

// NEW:
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp))
{
    throw new InvalidOperationException("RSVP registration window is not currently open for this event");
}
```

**Ticket Purchase** (line ~462):
```csharp
// OLD:
if (!await _timeZoneService.IsRegistrationOpenAsync(eventEntity))
{
    throw new InvalidOperationException("Registration is not currently open for this event");
}

// NEW:
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetTicket))
{
    throw new InvalidOperationException("Ticket purchase window is not currently open for this event");
}
```

**RSVP/Ticket Cancellation** (line ~695):
```csharp
// OLD:
if (!await _timeZoneService.IsRegistrationOpenAsync(eventEntity))
{
    throw new InvalidOperationException("Cancellations are not allowed at this time");
}

// NEW:
var actionType = attendance.IsPaid ? EventActionType.CancelTicket : EventActionType.CancelRsvp;
if (!await _timeZoneService.IsActionAllowedAsync(eventEntity, actionType))
{
    throw new InvalidOperationException("Cancellation window is not currently open for this event");
}
```

### VolunteerService Updates

**Volunteer Signup** (add to existing signup method):
```csharp
public async Task<VolunteerSignup> SignupForVolunteerSpot(Guid spotId, Guid userId)
{
    var spot = await _context.VolunteerSpots
        .Include(s => s.Event)
        .FirstOrDefaultAsync(s => s.Id == spotId);

    if (spot == null)
        throw new NotFoundException("Volunteer spot not found");

    // NEW: Check timing window
    if (!await _timeZoneService.IsActionAllowedAsync(spot.Event, EventActionType.GetVolunteer))
    {
        throw new InvalidOperationException("Volunteer registration window has closed for this event");
    }

    // ... rest of existing signup logic ...
}
```

**Volunteer Cancel** (NEW method):
```csharp
public async Task CancelVolunteerSignup(Guid signupId, Guid userId)
{
    var signup = await _context.VolunteerSignups
        .Include(s => s.VolunteerSpot)
            .ThenInclude(s => s.Event)
        .FirstOrDefaultAsync(s => s.Id == signupId);

    if (signup == null)
        throw new NotFoundException("Volunteer signup not found");

    // Verify ownership
    if (signup.UserId != userId)
        throw new UnauthorizedException("You can only cancel your own volunteer signups");

    // Check timing window
    if (!await _timeZoneService.IsActionAllowedAsync(signup.VolunteerSpot.Event, EventActionType.CancelVolunteer))
    {
        throw new InvalidOperationException("Volunteer cancellation window has closed for this event");
    }

    // Remove signup
    _context.VolunteerSignups.Remove(signup);
    await _context.SaveChangesAsync();
}
```

### VolunteerEndpoints - New Cancel Endpoint

```csharp
// File: /apps/api/Features/Volunteers/VolunteerEndpoints.cs

public static class VolunteerEndpoints
{
    public static void MapVolunteerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/volunteer-signups")
            .RequireAuthorization();

        // ... existing endpoints ...

        // NEW: User cancel volunteer signup
        group.MapPost("/{signupId:guid}/cancel", CancelVolunteerSignup)
            .WithName("CancelVolunteerSignup")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CancelVolunteerSignup(
        Guid signupId,
        VolunteerService volunteerService,
        HttpContext httpContext)
    {
        try
        {
            var userId = httpContext.User.GetUserId();
            await volunteerService.CancelVolunteerSignup(signupId, userId);
            return Results.Ok();
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Results.Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
```

### EventDto Updates

```csharp
// File: /apps/api/Features/Events/Models/EventDto.cs

public class EventDto
{
    // ... existing properties ...

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration opens.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can register any time before event).
    /// </summary>
    public decimal? RegistrationOpenHours { get; set; }

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket registration closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can register until event starts).
    /// </summary>
    public decimal? RegistrationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket cancellation opens.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can cancel any time before event).
    /// </summary>
    public decimal? CancellationOpenHours { get; set; }

    /// <summary>
    /// Hours before/after event start when RSVP/Ticket cancellation closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can cancel until event starts).
    /// </summary>
    public decimal? CancellationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer signup closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can signup until event starts).
    /// </summary>
    public decimal? VolunteerRegistrationCloseHours { get; set; }

    /// <summary>
    /// Hours before/after event start when volunteer cancellation closes.
    /// Positive = before event, Negative = after event (max -24).
    /// NULL = no restriction (can cancel until event starts).
    /// </summary>
    public decimal? VolunteerCancellationCloseHours { get; set; }
}
```

## 🎯 SUCCESS CRITERIA

### Unit Test Coverage
**TimeZoneServiceTests.cs** - Create comprehensive test suite:

```csharp
[Fact]
public async Task IsActionAllowedAsync_WithNullRegistrationOpenHours_ReturnsTrue()
{
    // Arrange: Event with NULL registration open hours
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddDays(7),
        RegistrationOpenHours = null,
        RegistrationCloseHours = 1
    };

    // Act
    var result = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

    // Assert
    Assert.True(result); // NULL = no restriction
}

[Fact]
public async Task IsActionAllowedAsync_BeforeRegistrationOpens_ReturnsFalse()
{
    // Arrange: Registration opens 168 hours before (7 days), event in 10 days
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddDays(10),
        RegistrationOpenHours = 168, // Opens 7 days before
        RegistrationCloseHours = 1
    };

    // Act
    var result = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

    // Assert
    Assert.False(result); // Too early (10 days away, registration not open yet)
}

[Fact]
public async Task IsActionAllowedAsync_AfterRegistrationCloses_ReturnsFalse()
{
    // Arrange: Registration closes 1 hour before, event in 30 minutes
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddMinutes(30),
        RegistrationOpenHours = 168,
        RegistrationCloseHours = 1 // Closes 1 hour before
    };

    // Act
    var result = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

    // Assert
    Assert.False(result); // Too late (registration closed 1 hour before, only 30 min left)
}

[Fact]
public async Task IsActionAllowedAsync_WithinRegistrationWindow_ReturnsTrue()
{
    // Arrange: Registration 7 days before to 1 hour before, event in 3 days
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddDays(3),
        RegistrationOpenHours = 168,
        RegistrationCloseHours = 1
    };

    // Act
    var result = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

    // Assert
    Assert.True(result); // Within window (3 days = 72 hours, between 168 and 1)
}

[Fact]
public async Task IsActionAllowedAsync_PostEventCancellation_ReturnsTrue()
{
    // Arrange: Cancellation allowed up to 24 hours AFTER event, event was 12 hours ago
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddHours(-12), // Event was 12 hours ago
        CancellationOpenHours = 168,
        CancellationCloseHours = -24 // Can cancel up to 24 hours after
    };

    // Act
    var result = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

    // Assert
    Assert.True(result); // Event was 12 hours ago, can cancel up to 24 hours after
}

[Fact]
public async Task IsActionAllowedAsync_VolunteerAction_UsesCorrectFields()
{
    // Arrange: Volunteer has separate timing fields
    var eventEntity = new Event
    {
        StartDateTime = DateTime.UtcNow.AddDays(2),
        RegistrationCloseHours = 1, // RSVP/Ticket closes 1 hour before
        VolunteerRegistrationCloseHours = 24 // Volunteer closes 24 hours before
    };

    // Act - Volunteer action should use volunteer field, not registration field
    var volunteerResult = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetVolunteer);
    var rsvpResult = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

    // Assert
    Assert.True(volunteerResult); // 2 days = 48 hours, volunteer closes at 24 hours
    Assert.True(rsvpResult); // 2 days = 48 hours, RSVP closes at 1 hour
}
```

### Integration Test Coverage

**AttendanceServiceTests.cs** - Verify enforcement:
- RSVP creation blocked when outside registration window
- RSVP creation allowed when inside registration window
- RSVP cancellation blocked when outside cancellation window
- RSVP cancellation allowed when inside cancellation window
- Ticket purchase uses same windows as RSVP
- Ticket cancellation uses same windows as RSVP cancel

**VolunteerServiceTests.cs** - Verify new enforcement:
- Volunteer signup blocked when outside window
- Volunteer signup allowed when inside window
- Volunteer cancel blocked when outside window
- Volunteer cancel allowed when inside window
- Volunteer cancel validates ownership

**VolunteerEndpointsTests.cs** - Verify new endpoint:
- POST /api/volunteer-signups/{id}/cancel returns 200 on success
- POST /api/volunteer-signups/{id}/cancel returns 404 when signup not found
- POST /api/volunteer-signups/{id}/cancel returns 401 when not owner
- POST /api/volunteer-signups/{id}/cancel returns 400 when window closed

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT delete old `IsRegistrationOpenAsync()` method yet (frontend may still use it)
- ❌ DO NOT treat NULL timing fields as zero or throw exceptions
- ❌ DO NOT allow admin-only volunteer cancellation (users must self-cancel)
- ❌ DO NOT use DELETE for volunteer cancel endpoint (use POST)
- ❌ DO NOT forget timing enforcement on new volunteer cancel endpoint
- ❌ DO NOT skip backward compatibility testing with NULL fields

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| EventActionType | Enum defining types of actions users perform on events | GetRsvp, CancelRsvp, GetVolunteer, etc. |
| Timing Window | Period when specific action is allowed based on event timing config | Registration window = 7 days before to 1 hour before |
| Hours Until Start | Calculated time difference between now and event start | Positive = event in future, Negative = event in past |
| Null Restriction | NULL timing field means no restriction on that timing control | NULL RegistrationOpenHours = can register any time before event |
| Post-Event Timing | Negative hour values indicating time AFTER event starts | -24 = 24 hours after event started |

## 🔗 NEXT AGENT INSTRUCTIONS

### React Developer Agent
**FIRST**: Read this handoff document completely
**SECOND**: Verify backend API changes deployed to staging:
```bash
# Test new volunteer cancel endpoint
curl -X POST http://localhost:5655/api/volunteer-signups/{signupId}/cancel \
  -H "Authorization: Bearer {token}"

# Verify EventDto includes timing fields
curl http://localhost:5655/api/events/{eventId} | jq .registrationOpenHours
```
**THIRD**: Regenerate TypeScript types from updated API:
```bash
cd packages/shared-types
npm run generate
```
**FOURTH**: Read react developer handoff for UI implementation
**THEN**: Begin EventForm timing settings components

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Database Designer Agent
**Previous Phase Completed**: 2025-11-18 (Database Migration)
**Key Finding**: Event entity now has 6 nullable decimal timing control fields with check constraints enforcing -24 minimum

**Next Agent Should Be**: React Developer Agent
**Next Phase**: Frontend UI Implementation (Phase 3)
**Estimated Effort**: 2-3 days for service refactoring, endpoint creation, DTO updates, and comprehensive testing

---

## Exact File Paths for Implementation

**Service Files** (update):
- `/apps/api/Services/TimeZoneService.cs` - Add IsActionAllowedAsync method
- `/apps/api/Features/Attendance/AttendanceService.cs` - Update 3 enforcement points
- `/apps/api/Features/Volunteers/VolunteerService.cs` - Add 2 enforcement points + cancel method

**Endpoint Files** (update):
- `/apps/api/Features/Volunteers/VolunteerEndpoints.cs` - Add cancel endpoint

**Model Files** (update):
- `/apps/api/Features/Events/Models/EventDto.cs` - Add 6 timing properties
- `/apps/api/Features/Events/EventActionType.cs` - Create new enum

**Test Files** (create):
- `/tests/WitchCityRope.Core.Tests/Services/TimeZoneServiceTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Attendance/RsvpTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Attendance/TicketTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerTimingTests.cs`
- `/tests/WitchCityRope.IntegrationTests/Features/Volunteers/VolunteerCancelEndpointTests.cs`

---

**This handoff document contains all information needed for backend API implementation. Proceed with confidence!**
