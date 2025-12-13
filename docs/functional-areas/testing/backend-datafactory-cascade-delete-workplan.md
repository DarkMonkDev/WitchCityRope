# Backend Developer Work Plan: DataFactory Cascade Delete Fixes

**Date**: December 13, 2025
**Priority**: HIGH
**Context**: E2E tests are failing due to DataFactory cleanup failures. The test helper endpoints don't properly handle cascade deletion of related entities.

---

## Problem Statement

The DataFactory cleanup in E2E tests fails with 400 errors when trying to delete:
1. **Sessions** - Can't delete when TicketTypes are linked to the session
2. **Events** - Can't delete when sessions, ticket purchases, or other entities exist
3. **Users** - Can't create when email already exists (need "upsert" or "get or create" behavior)

**Error Examples**:
```
DELETE /api/test-helpers/sessions/{id} - Status 400
DELETE /api/test-helpers/events/{id} - Status 400
POST /api/test-helpers/users - Status 400 (email exists)
```

---

## Entity Relationship Map

Understanding the relationships is CRITICAL for proper cascade deletion:

```
Event (root entity)
├── Session (EventId FK)
│   ├── TicketType.Sessions (many-to-many via TicketTypeSessions join table)
│   ├── EventAttendance.SessionId (nullable FK)
│   └── VolunteerPosition.SessionId (nullable FK)
├── TicketType (EventId FK)
│   ├── TicketPurchase.TicketTypeId (FK)
│   │   └── EventAttendance.TicketPurchaseId (nullable FK)
│   └── TicketType.Sessions (many-to-many)
├── VolunteerPosition (EventId FK)
│   └── VolunteerSignup.VolunteerPositionId (FK)
└── EventAttendance (EventId FK)
```

---

## Required Deletion Order (CRITICAL)

To avoid foreign key constraint violations, delete in this order:

1. **VolunteerSignups** - References VolunteerPosition
2. **EventAttendances** - References Session, Event, TicketPurchase
3. **TicketPurchases** - References TicketType
4. **TicketTypeSessions** (join table) - Links TicketType to Session
5. **TicketTypes** - References Event and Sessions
6. **VolunteerPositions** - References Session, Event
7. **Sessions** - References Event
8. **Events** - Root entity

---

## Task 1: Update DeleteTestSessionAsync

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/TestHelpers/Services/TestHelperService.cs`

**Current Implementation** (lines 571-599):
```csharp
public async Task<(bool Success, string? Error)> DeleteTestSessionAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default)
{
    // Currently only removes the session directly - FAILS if dependencies exist
    _context.Set<Session>().Remove(sessionEntity);
}
```

**New Implementation Required**:
```csharp
public async Task<(bool Success, string? Error)> DeleteTestSessionAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Deleting test session with cascade: {SessionId}", sessionId);

        var sessionEntity = await _context.Set<Session>()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (sessionEntity == null)
        {
            _logger.LogWarning("Test session not found for deletion: {SessionId}", sessionId);
            return (false, $"Session not found: {sessionId}");
        }

        // 1. Delete VolunteerSignups for positions linked to this session
        var sessionVolunteerPositions = await _context.Set<VolunteerPosition>()
            .Where(vp => vp.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        var positionIds = sessionVolunteerPositions.Select(vp => vp.Id).ToList();
        if (positionIds.Any())
        {
            var volunteerSignups = await _context.Set<VolunteerSignup>()
                .Where(vs => positionIds.Contains(vs.VolunteerPositionId))
                .ToListAsync(cancellationToken);
            _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);
        }

        // 2. Delete EventAttendances referencing this session
        var sessionAttendances = await _context.Set<EventAttendance>()
            .Where(ea => ea.SessionId == sessionId)
            .ToListAsync(cancellationToken);
        _context.Set<EventAttendance>().RemoveRange(sessionAttendances);

        // 3. Get TicketPurchases that reference TicketTypes linked to this session
        // First, get ticket types linked to this session via the many-to-many relationship
        var ticketTypesWithSession = await _context.Set<TicketType>()
            .Include(tt => tt.Sessions)
            .Where(tt => tt.Sessions.Any(s => s.Id == sessionId))
            .ToListAsync(cancellationToken);

        // Delete ticket purchases for these ticket types
        var ticketTypeIds = ticketTypesWithSession.Select(tt => tt.Id).ToList();
        if (ticketTypeIds.Any())
        {
            // First delete EventAttendances that reference these ticket purchases
            var ticketPurchases = await _context.Set<TicketPurchase>()
                .Where(tp => ticketTypeIds.Contains(tp.TicketTypeId))
                .ToListAsync(cancellationToken);

            var purchaseIds = ticketPurchases.Select(tp => tp.Id).ToList();
            if (purchaseIds.Any())
            {
                var purchaseAttendances = await _context.Set<EventAttendance>()
                    .Where(ea => ea.TicketPurchaseId.HasValue && purchaseIds.Contains(ea.TicketPurchaseId.Value))
                    .ToListAsync(cancellationToken);
                _context.Set<EventAttendance>().RemoveRange(purchaseAttendances);
            }

            _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);
        }

        // 4. Clear the many-to-many relationship (TicketTypeSessions)
        // This is done automatically when we clear the Sessions collection
        foreach (var ticketType in ticketTypesWithSession)
        {
            ticketType.Sessions.Remove(sessionEntity);
        }

        // 5. Delete VolunteerPositions linked to this session
        _context.Set<VolunteerPosition>().RemoveRange(sessionVolunteerPositions);

        // 6. Delete the session itself
        _context.Set<Session>().Remove(sessionEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted test session with cascade: {SessionId}", sessionId);
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete test session: {SessionId}", sessionId);
        return (false, ex.Message);
    }
}
```

---

## Task 2: Update DeleteTestEventAsync

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/TestHelpers/Services/TestHelperService.cs`

**Current Implementation** (lines 462-508):
```csharp
// Currently deletes sessions, ticket types, volunteer positions
// BUT doesn't handle VolunteerSignups, TicketPurchases, EventAttendances
```

**New Implementation Required**:
```csharp
public async Task<(bool Success, string? Error)> DeleteTestEventAsync(
    Guid eventId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Deleting test event with full cascade: {EventId}", eventId);

        var eventEntity = await _context.Set<Event>()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            _logger.LogWarning("Test event not found for deletion: {EventId}", eventId);
            return (false, $"Event not found: {eventId}");
        }

        // 1. Delete VolunteerSignups for all volunteer positions in this event
        var volunteerPositions = await _context.Set<VolunteerPosition>()
            .Where(v => v.EventId == eventId)
            .ToListAsync(cancellationToken);

        var positionIds = volunteerPositions.Select(vp => vp.Id).ToList();
        if (positionIds.Any())
        {
            var volunteerSignups = await _context.Set<VolunteerSignup>()
                .Where(vs => positionIds.Contains(vs.VolunteerPositionId))
                .ToListAsync(cancellationToken);
            _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);
            _logger.LogDebug("Removed {Count} volunteer signups", volunteerSignups.Count);
        }

        // 2. Delete EventAttendances for this event
        var eventAttendances = await _context.Set<EventAttendance>()
            .Where(ea => ea.EventId == eventId)
            .ToListAsync(cancellationToken);
        _context.Set<EventAttendance>().RemoveRange(eventAttendances);
        _logger.LogDebug("Removed {Count} event attendances", eventAttendances.Count);

        // 3. Delete TicketPurchases for ticket types in this event
        var ticketTypes = await _context.Set<TicketType>()
            .Where(t => t.EventId == eventId)
            .ToListAsync(cancellationToken);

        var ticketTypeIds = ticketTypes.Select(tt => tt.Id).ToList();
        if (ticketTypeIds.Any())
        {
            var ticketPurchases = await _context.Set<TicketPurchase>()
                .Where(tp => ticketTypeIds.Contains(tp.TicketTypeId))
                .ToListAsync(cancellationToken);
            _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);
            _logger.LogDebug("Removed {Count} ticket purchases", ticketPurchases.Count);
        }

        // 4. Clear TicketType-Session many-to-many relationships
        // Load ticket types with their sessions
        var ticketTypesWithSessions = await _context.Set<TicketType>()
            .Include(tt => tt.Sessions)
            .Where(t => t.EventId == eventId)
            .ToListAsync(cancellationToken);

        foreach (var ticketType in ticketTypesWithSessions)
        {
            ticketType.Sessions.Clear();
        }

        // 5. Delete TicketTypes
        _context.Set<TicketType>().RemoveRange(ticketTypes);
        _logger.LogDebug("Removed {Count} ticket types", ticketTypes.Count);

        // 6. Delete VolunteerPositions
        _context.Set<VolunteerPosition>().RemoveRange(volunteerPositions);
        _logger.LogDebug("Removed {Count} volunteer positions", volunteerPositions.Count);

        // 7. Delete Sessions
        var sessions = await _context.Set<Session>()
            .Where(s => s.EventId == eventId)
            .ToListAsync(cancellationToken);
        _context.Set<Session>().RemoveRange(sessions);
        _logger.LogDebug("Removed {Count} sessions", sessions.Count);

        // 8. Delete the Event itself
        _context.Set<Event>().Remove(eventEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted test event with cascade: {EventId} (sessions: {Sessions}, tickets: {Tickets}, volunteers: {Volunteers})",
            eventId, sessions.Count, ticketTypes.Count, volunteerPositions.Count);
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete test event: {EventId}", eventId);
        return (false, ex.Message);
    }
}
```

---

## Task 3: Update DeleteTestTicketTypeAsync

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/TestHelpers/Services/TestHelperService.cs`

**Current Implementation** (lines 675-703): Only removes TicketType directly.

**New Implementation Required**:
```csharp
public async Task<(bool Success, string? Error)> DeleteTestTicketTypeAsync(
    Guid ticketTypeId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Deleting test ticket type with cascade: {TicketTypeId}", ticketTypeId);

        var ticketTypeEntity = await _context.Set<TicketType>()
            .Include(tt => tt.Sessions)
            .FirstOrDefaultAsync(t => t.Id == ticketTypeId, cancellationToken);

        if (ticketTypeEntity == null)
        {
            _logger.LogWarning("Test ticket type not found for deletion: {TicketTypeId}", ticketTypeId);
            return (false, $"Ticket type not found: {ticketTypeId}");
        }

        // 1. Delete TicketPurchases for this ticket type
        var ticketPurchases = await _context.Set<TicketPurchase>()
            .Where(tp => tp.TicketTypeId == ticketTypeId)
            .ToListAsync(cancellationToken);

        // 1a. First delete EventAttendances that reference these purchases
        var purchaseIds = ticketPurchases.Select(tp => tp.Id).ToList();
        if (purchaseIds.Any())
        {
            var purchaseAttendances = await _context.Set<EventAttendance>()
                .Where(ea => ea.TicketPurchaseId.HasValue && purchaseIds.Contains(ea.TicketPurchaseId.Value))
                .ToListAsync(cancellationToken);
            _context.Set<EventAttendance>().RemoveRange(purchaseAttendances);
        }

        _context.Set<TicketPurchase>().RemoveRange(ticketPurchases);

        // 2. Clear the many-to-many relationship (TicketTypeSessions)
        ticketTypeEntity.Sessions.Clear();

        // 3. Delete the TicketType itself
        _context.Set<TicketType>().Remove(ticketTypeEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted test ticket type with cascade: {TicketTypeId}", ticketTypeId);
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete test ticket type: {TicketTypeId}", ticketTypeId);
        return (false, ex.Message);
    }
}
```

---

## Task 4: Update DeleteTestVolunteerPositionAsync

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/TestHelpers/Services/TestHelperService.cs`

**Current Implementation** (lines 779-807): Only removes VolunteerPosition directly.

**New Implementation Required**:
```csharp
public async Task<(bool Success, string? Error)> DeleteTestVolunteerPositionAsync(
    Guid positionId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Deleting test volunteer position with cascade: {PositionId}", positionId);

        var positionEntity = await _context.Set<VolunteerPosition>()
            .FirstOrDefaultAsync(v => v.Id == positionId, cancellationToken);

        if (positionEntity == null)
        {
            _logger.LogWarning("Test volunteer position not found for deletion: {PositionId}", positionId);
            return (false, $"Volunteer position not found: {positionId}");
        }

        // 1. Delete VolunteerSignups for this position
        var volunteerSignups = await _context.Set<VolunteerSignup>()
            .Where(vs => vs.VolunteerPositionId == positionId)
            .ToListAsync(cancellationToken);
        _context.Set<VolunteerSignup>().RemoveRange(volunteerSignups);

        // 2. Delete the VolunteerPosition itself
        _context.Set<VolunteerPosition>().Remove(positionEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted test volunteer position with cascade: {PositionId}", positionId);
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete test volunteer position: {PositionId}", positionId);
        return (false, ex.Message);
    }
}
```

---

## Task 5: Add GetOrCreateTestUserAsync Method

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/TestHelpers/Services/TestHelperService.cs`

**Purpose**: Allow tests to get existing user if email exists, or create new one. Prevents "email already exists" errors.

**New Method**:
```csharp
/// <summary>
/// Get an existing user by email, or create a new one if not found.
/// Used for E2E tests that may run multiple times with same test data.
/// </summary>
public async Task<(bool Success, TestUserResponse? Data, string? Error)> GetOrCreateTestUserAsync(
    CreateTestUserRequest request,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("GetOrCreate test user: {Email}", request.Email);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogInformation("Found existing user: {Email} (ID: {UserId})", request.Email, existingUser.Id);

            return (true, new TestUserResponse
            {
                Id = existingUser.Id.ToString(),
                Email = existingUser.Email!,
                SceneName = existingUser.SceneName,
                Role = existingUser.Role ?? "Member",
                CreatedAt = existingUser.CreatedAt
            }, null);
        }

        // User doesn't exist, create new one
        return await CreateTestUserAsync(request, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception in GetOrCreate test user: {Email}", request.Email);
        return (false, null, $"Internal error: {ex.Message}");
    }
}
```

**Also add to ITestHelperService interface**:
```csharp
/// <summary>
/// Get an existing user by email, or create a new one if not found.
/// Used for E2E tests that may run multiple times with same test data.
/// </summary>
Task<(bool Success, TestUserResponse? Data, string? Error)> GetOrCreateTestUserAsync(
    CreateTestUserRequest request,
    CancellationToken cancellationToken = default);
```

**Add new endpoint in TestHelperEndpoints.cs**:
```csharp
// Get or create test user endpoint
app.MapPost("/api/test-helpers/users/get-or-create", async (
    CreateTestUserRequest request,
    ITestHelperService testHelperService,
    CancellationToken cancellationToken) =>
    {
        var (success, data, error) = await testHelperService.GetOrCreateTestUserAsync(request, cancellationToken);

        if (success && data != null)
        {
            return Results.Ok(data);
        }

        return Results.Problem(
            title: "Failed to get or create test user",
            detail: error,
            statusCode: 400);
    })
    .AllowAnonymous()
    .WithName("GetOrCreateTestUser")
    .WithSummary("Get existing user or create new one")
    .WithDescription("Returns existing user if email exists, otherwise creates new user. ONLY available in Development/Test.")
    .WithTags("Testing", "TestHelpers")
    .Produces<object>(200)
    .Produces<object>(400);
```

---

## Required Namespace Imports

Ensure these are at the top of TestHelperService.cs:
```csharp
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
```

---

## Testing Instructions

After implementing the changes, verify with these tests:

### 1. Build Verification
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet build --no-restore
# Expected: 0 errors
```

### 2. Manual API Testing
```bash
# Create test event with session and ticket type
curl -X POST http://localhost:5655/api/test-helpers/events \
  -H "Content-Type: application/json" \
  -d '{"title":"Cascade Test","startDate":"2025-12-20T18:00:00Z","endDate":"2025-12-20T21:00:00Z","isPublished":true}'

# Note the eventId from response, then create session
curl -X POST http://localhost:5655/api/test-helpers/sessions \
  -H "Content-Type: application/json" \
  -d '{"eventId":"<eventId>","name":"Test Session","sessionCode":"S1","startTime":"2025-12-20T18:00:00Z","endTime":"2025-12-20T21:00:00Z","capacity":20}'

# Note sessionId, then create ticket type with session
curl -X POST http://localhost:5655/api/test-helpers/ticket-types \
  -H "Content-Type: application/json" \
  -d '{"eventId":"<eventId>","name":"Test Ticket","price":25.00,"available":20,"sessionIds":["<sessionId>"]}'

# Now test cascade delete of session (should succeed now)
curl -X DELETE http://localhost:5655/api/test-helpers/sessions/<sessionId>
# Expected: 204 No Content

# Or test cascade delete of entire event
curl -X DELETE http://localhost:5655/api/test-helpers/events/<eventId>
# Expected: 204 No Content
```

### 3. E2E Test Verification
```bash
# Run session timing tests (these were failing due to cleanup issues)
cd /home/chad/repos/witchcityrope
npx playwright test session-based-timing.spec.ts --reporter=list

# Run check-in tests (also had cleanup failures)
npx playwright test checkin --reporter=list
```

---

## Success Criteria

1. **API builds with 0 errors**
2. **DELETE /api/test-helpers/sessions/{id} returns 204** even when ticket types are linked
3. **DELETE /api/test-helpers/events/{id} returns 204** even when all child entities exist
4. **DELETE /api/test-helpers/ticket-types/{id} returns 204** even when purchases exist
5. **DELETE /api/test-helpers/volunteer-positions/{id} returns 204** even when signups exist
6. **POST /api/test-helpers/users/get-or-create returns 200** whether user exists or not
7. **E2E tests pass cleanup phase** without 400 errors

---

## Files to Modify

| File | Changes |
|------|---------|
| `/apps/api/Features/TestHelpers/Services/TestHelperService.cs` | Update all 4 delete methods, add GetOrCreateTestUserAsync |
| `/apps/api/Features/TestHelpers/Services/ITestHelperService.cs` | Add GetOrCreateTestUserAsync signature |
| `/apps/api/Features/TestHelpers/Endpoints/TestHelperEndpoints.cs` | Add get-or-create endpoint |

---

## Important Notes

1. **Order matters**: Always delete children before parents
2. **Many-to-many**: Clear the collection before deleting either side
3. **Use `.ToListAsync()`**: Load entities into memory before removing to avoid tracking issues
4. **Log counts**: Helps debugging when cleanup fails
5. **Test helper ONLY**: This is test infrastructure, not production code
