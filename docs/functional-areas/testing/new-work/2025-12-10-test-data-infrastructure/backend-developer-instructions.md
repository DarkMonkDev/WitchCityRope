# Backend Developer Instructions: Test Helper Endpoints

**Date**: 2025-12-10
**Agent**: backend-developer
**Priority**: CRITICAL - Blocks all E2E test fixes

## Overview

Add missing test helper endpoints to `/apps/api/Features/TestHelpers/` to enable programmatic test data creation. These endpoints are ONLY available in Development/Test environments (safety check already exists).

## CRITICAL: Read Before Starting

1. **Read existing code first**:
   - `/apps/api/Features/TestHelpers/Endpoints/TestHelperEndpoints.cs`
   - `/apps/api/Features/TestHelpers/Services/TestHelperService.cs`
   - `/apps/api/Features/TestHelpers/Services/ITestHelperService.cs`
   - `/apps/api/Features/TestHelpers/Models/*.cs`

2. **Follow existing patterns EXACTLY** - the existing code is the template

3. **All endpoints must be AllowAnonymous** - no auth required for test helpers

4. **All endpoints must check environment** - already handled by `MapTestHelperEndpoints`

## Entities to Support

### 1. Events

**Create POST `/api/test-helpers/events`**

Request model (`CreateTestEventRequest.cs`):
```csharp
public record CreateTestEventRequest
{
    public required string Title { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public EventType EventType { get; init; } = EventType.Class;
    public EventStatus Status { get; init; } = EventStatus.Published;
    public bool IsPublic { get; init; } = true;
    public Guid? VenueId { get; init; }  // Optional, can be null for tests
}
```

Response model (`TestEventResponse.cs`):
```csharp
public record TestEventResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required string Status { get; init; }
}
```

**Create DELETE `/api/test-helpers/events/{eventId:guid}`**
- Returns 204 NoContent on success
- Returns 400 Problem on failure

### 2. Sessions

**Create POST `/api/test-helpers/sessions`**

Request model (`CreateTestSessionRequest.cs`):
```csharp
public record CreateTestSessionRequest
{
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public int MaxCapacity { get; init; } = 20;
    public bool RequiresRegistration { get; init; } = true;
}
```

Response model (`TestSessionResponse.cs`):
```csharp
public record TestSessionResponse
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
}
```

**Create DELETE `/api/test-helpers/sessions/{sessionId:guid}`**

### 3. Ticket Types

**Create POST `/api/test-helpers/ticket-types`**

Request model (`CreateTestTicketTypeRequest.cs`):
```csharp
public record CreateTestTicketTypeRequest
{
    public required Guid SessionId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required decimal Price { get; init; }
    public int QuantityAvailable { get; init; } = 100;
    public bool IsActive { get; init; } = true;
    public DateTime? SalesStartDate { get; init; }
    public DateTime? SalesEndDate { get; init; }
}
```

Response model (`TestTicketTypeResponse.cs`):
```csharp
public record TestTicketTypeResponse
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required int QuantityAvailable { get; init; }
}
```

**Create DELETE `/api/test-helpers/ticket-types/{ticketTypeId:guid}`**

### 4. Volunteer Positions

**Create POST `/api/test-helpers/volunteer-positions`**

Request model (`CreateTestVolunteerPositionRequest.cs`):
```csharp
public record CreateTestVolunteerPositionRequest
{
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int SlotsAvailable { get; init; } = 3;
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
}
```

Response model (`TestVolunteerPositionResponse.cs`):
```csharp
public record TestVolunteerPositionResponse
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public required int SlotsAvailable { get; init; }
}
```

**Create DELETE `/api/test-helpers/volunteer-positions/{positionId:guid}`**

### 5. Vetting Applications

**Create POST `/api/test-helpers/vetting-applications`**

Request model (`CreateTestVettingApplicationRequest.cs`):
```csharp
public record CreateTestVettingApplicationRequest
{
    public required string UserId { get; init; }
    public VettingStatus Status { get; init; } = VettingStatus.Pending;
    public string? Notes { get; init; }
}
```

Response model (`TestVettingApplicationResponse.cs`):
```csharp
public record TestVettingApplicationResponse
{
    public required Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

**Create DELETE `/api/test-helpers/vetting-applications/{applicationId:guid}`**

## Implementation Steps

### Step 1: Create Request/Response Models

Create files in `/apps/api/Features/TestHelpers/Models/`:

1. `CreateTestEventRequest.cs`
2. `TestEventResponse.cs`
3. `CreateTestSessionRequest.cs`
4. `TestSessionResponse.cs`
5. `CreateTestTicketTypeRequest.cs`
6. `TestTicketTypeResponse.cs`
7. `CreateTestVolunteerPositionRequest.cs`
8. `TestVolunteerPositionResponse.cs`
9. `CreateTestVettingApplicationRequest.cs`
10. `TestVettingApplicationResponse.cs`

### Step 2: Update ITestHelperService Interface

Add to `/apps/api/Features/TestHelpers/Services/ITestHelperService.cs`:

```csharp
// Event operations
Task<(bool Success, TestEventResponse? Data, string? Error)> CreateTestEventAsync(
    CreateTestEventRequest request,
    CancellationToken cancellationToken = default);

Task<(bool Success, string? Error)> DeleteTestEventAsync(
    Guid eventId,
    CancellationToken cancellationToken = default);

// Session operations
Task<(bool Success, TestSessionResponse? Data, string? Error)> CreateTestSessionAsync(
    CreateTestSessionRequest request,
    CancellationToken cancellationToken = default);

Task<(bool Success, string? Error)> DeleteTestSessionAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default);

// Ticket type operations
Task<(bool Success, TestTicketTypeResponse? Data, string? Error)> CreateTestTicketTypeAsync(
    CreateTestTicketTypeRequest request,
    CancellationToken cancellationToken = default);

Task<(bool Success, string? Error)> DeleteTestTicketTypeAsync(
    Guid ticketTypeId,
    CancellationToken cancellationToken = default);

// Volunteer position operations
Task<(bool Success, TestVolunteerPositionResponse? Data, string? Error)> CreateTestVolunteerPositionAsync(
    CreateTestVolunteerPositionRequest request,
    CancellationToken cancellationToken = default);

Task<(bool Success, string? Error)> DeleteTestVolunteerPositionAsync(
    Guid positionId,
    CancellationToken cancellationToken = default);

// Vetting application operations
Task<(bool Success, TestVettingApplicationResponse? Data, string? Error)> CreateTestVettingApplicationAsync(
    CreateTestVettingApplicationRequest request,
    CancellationToken cancellationToken = default);

Task<(bool Success, string? Error)> DeleteTestVettingApplicationAsync(
    Guid applicationId,
    CancellationToken cancellationToken = default);
```

### Step 3: Implement TestHelperService Methods

Add implementations to `/apps/api/Features/TestHelpers/Services/TestHelperService.cs`.

**Example for Events** (follow this pattern for all entities):

```csharp
public async Task<(bool Success, TestEventResponse? Data, string? Error)> CreateTestEventAsync(
    CreateTestEventRequest request,
    CancellationToken cancellationToken = default)
{
    try
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ShortDescription = request.ShortDescription ?? $"Test event: {request.Title}",
            Description = request.Description ?? $"Test event description for {request.Title}",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            EventType = request.EventType,
            Status = request.Status,
            IsPublic = request.IsPublic,
            VenueId = request.VenueId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Test event created: {EventId} - {Title}", eventEntity.Id, eventEntity.Title);

        return (true, new TestEventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Status = eventEntity.Status.ToString()
        }, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create test event: {Title}", request.Title);
        return (false, null, ex.Message);
    }
}

public async Task<(bool Success, string? Error)> DeleteTestEventAsync(
    Guid eventId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var eventEntity = await _context.Events.FindAsync(new object[] { eventId }, cancellationToken);
        if (eventEntity == null)
        {
            return (false, $"Event not found: {eventId}");
        }

        // Also delete related entities (sessions, ticket types, etc.)
        var sessions = await _context.Sessions
            .Where(s => s.EventId == eventId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            var ticketTypes = await _context.TicketTypes
                .Where(t => t.SessionId == session.Id)
                .ToListAsync(cancellationToken);
            _context.TicketTypes.RemoveRange(ticketTypes);
        }

        _context.Sessions.RemoveRange(sessions);
        _context.Events.Remove(eventEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Test event deleted: {EventId}", eventId);
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete test event: {EventId}", eventId);
        return (false, ex.Message);
    }
}
```

### Step 4: Add Endpoints

Add to `/apps/api/Features/TestHelpers/Endpoints/TestHelperEndpoints.cs`:

Follow the exact pattern used for existing endpoints. Example:

```csharp
// Create test event endpoint
app.MapPost("/api/test-helpers/events", async (
    CreateTestEventRequest request,
    ITestHelperService testHelperService,
    CancellationToken cancellationToken) =>
    {
        var (success, data, error) = await testHelperService.CreateTestEventAsync(request, cancellationToken);

        if (success && data != null)
        {
            return Results.Created($"/api/test-helpers/events/{data.Id}", data);
        }

        return Results.Problem(
            title: "Failed to create test event",
            detail: error,
            statusCode: 400);
    })
    .AllowAnonymous()
    .WithName("CreateTestEvent")
    .WithSummary("Create test event for E2E testing")
    .WithDescription("Programmatically create an event with specific properties for testing. ONLY available in Development/Test.")
    .WithTags("Testing", "TestHelpers")
    .Produces<object>(201)
    .Produces<object>(400);

// Delete test event endpoint
app.MapDelete("/api/test-helpers/events/{eventId:guid}", async (
    Guid eventId,
    ITestHelperService testHelperService,
    CancellationToken cancellationToken) =>
    {
        var (success, error) = await testHelperService.DeleteTestEventAsync(eventId, cancellationToken);

        if (success)
        {
            return Results.NoContent();
        }

        return Results.Problem(
            title: "Failed to delete test event",
            detail: error,
            statusCode: 400);
    })
    .AllowAnonymous()
    .WithName("DeleteTestEvent")
    .WithSummary("Delete test event for cleanup")
    .WithDescription("Delete a test event by ID. Also deletes related sessions and ticket types. ONLY available in Development/Test.")
    .WithTags("Testing", "TestHelpers")
    .Produces(204)
    .Produces<object>(400);
```

## Verification

After implementation:

1. **Build the project**: `dotnet build apps/api/`
2. **Run the API**: Start test containers and verify endpoints work
3. **Test each endpoint**:
   - POST to create entity - should return 201 with ID
   - DELETE to remove entity - should return 204
   - Verify entities are actually created/deleted in database

## Important Notes

1. **Use correct entity names** - check the actual DbContext for entity class names
2. **Check foreign key requirements** - some entities need parent entities first
3. **Handle cascading deletes** - when deleting events, also delete sessions, ticket types
4. **All dates should be UTC** - use `DateTime.UtcNow`
5. **Log all operations** - use `_logger` for debugging

## Files to Create

| File | Type |
|------|------|
| `Models/CreateTestEventRequest.cs` | Request DTO |
| `Models/TestEventResponse.cs` | Response DTO |
| `Models/CreateTestSessionRequest.cs` | Request DTO |
| `Models/TestSessionResponse.cs` | Response DTO |
| `Models/CreateTestTicketTypeRequest.cs` | Request DTO |
| `Models/TestTicketTypeResponse.cs` | Response DTO |
| `Models/CreateTestVolunteerPositionRequest.cs` | Request DTO |
| `Models/TestVolunteerPositionResponse.cs` | Response DTO |
| `Models/CreateTestVettingApplicationRequest.cs` | Request DTO |
| `Models/TestVettingApplicationResponse.cs` | Response DTO |

## Files to Modify

| File | Change |
|------|--------|
| `Services/ITestHelperService.cs` | Add 10 new method signatures |
| `Services/TestHelperService.cs` | Implement 10 new methods |
| `Endpoints/TestHelperEndpoints.cs` | Add 10 new endpoints |

## Completion Checklist

- [ ] All 10 request models created
- [ ] All 10 response models created
- [ ] All 10 interface methods added
- [ ] All 10 service methods implemented
- [ ] All 10 endpoints added
- [ ] Project builds successfully
- [ ] All endpoints return correct status codes
- [ ] Cascading deletes work correctly
