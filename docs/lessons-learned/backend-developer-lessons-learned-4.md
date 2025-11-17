# Backend Developer Lessons Learned - Part 4

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 4 of 4**
**Part 1**: backend-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Part 2**: backend-developer-lessons-learned-2.md (MUST READ)
**Part 3**: backend-developer-lessons-learned-3.md (MUST READ)
**Part 4**: backend-developer-lessons-learned-4.md (THIS FILE)
**Read ALL**: Parts 1, 2, 3, AND 4 are MANDATORY
**Write to**: Part 4 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

---

## 🚨 CRITICAL: Services MUST Have Interfaces for Unit Testing (2025-11-13)

**Problem**: Service classes without interfaces cannot be mocked by NSubstitute, blocking unit test creation entirely.

**Date Discovered**: November 13, 2025 during Pattern B endpoint unit test creation
**Context**: Created 90 unit tests for 6 Pattern B endpoints, 14 VolunteerEndpointsTests failed

**Root Cause**:
- `VolunteerService` class had no `IVolunteerService` interface
- NSubstitute cannot mock concrete classes without parameterless constructors
- Test creation was blocked until interface was created and DI updated

**Error Message**:
```
Can not instantiate proxy of class: VolunteerService.
Could not find a parameterless constructor.
```

**Impact**:
- 14 VolunteerEndpointsTests failed with constructor error
- Delayed test development by several hours
- Required creating interface, updating DI, updating endpoint code

**Wrong Implementation** (No Interface):
```csharp
// ❌ WRONG - No interface exists
public class VolunteerService
{
    private readonly ApplicationDbContext _context;

    public VolunteerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool success, List<VolunteerPositionDto>? positions, string? error)>
        GetEventVolunteerPositionsAsync(string eventId, string? userId, CancellationToken cancellationToken = default)
    {
        // ... implementation
    }
}

// Service registration
services.AddScoped<VolunteerService>();

// Endpoint usage
app.MapGet("/api/volunteer-positions", async (VolunteerService service) =>
{
    // Cannot mock VolunteerService in tests!
});
```

**Correct Implementation** (With Interface):
```csharp
// ✅ CORRECT - Interface exists
public interface IVolunteerService
{
    Task<(bool success, List<VolunteerPositionDto>? positions, string? error)>
        GetEventVolunteerPositionsAsync(string eventId, string? userId, CancellationToken cancellationToken = default);

    Task<(bool success, VolunteerPositionDto? position, string? error)>
        GetVolunteerPositionAsync(string positionId, CancellationToken cancellationToken = default);

    Task<(bool success, string? error)>
        SignUpForPositionAsync(string positionId, string userId, CancellationToken cancellationToken = default);

    Task<(bool success, string? error)>
        RemoveVolunteerAsync(string positionId, string userId, CancellationToken cancellationToken = default);
}

public class VolunteerService : IVolunteerService
{
    private readonly ApplicationDbContext _context;

    public VolunteerService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ... implementation
}

// DI Registration
services.AddScoped<IVolunteerService, VolunteerService>();

// Endpoint usage
app.MapGet("/api/volunteer-positions", async (IVolunteerService service) =>
{
    // Can now mock IVolunteerService in tests!
});
```

**Unit Test Example**:
```csharp
// ✅ CORRECT - Can mock interface
public class VolunteerEndpointsTests
{
    private readonly IVolunteerService _volunteerService;

    public VolunteerEndpointsTests()
    {
        _volunteerService = Substitute.For<IVolunteerService>();
    }

    [Fact]
    public async Task GetEventVolunteerPositions_WithValidEvent_ReturnsPositions()
    {
        // Arrange
        var eventId = "test-event-id";
        var positions = new List<VolunteerPositionDto>
        {
            new VolunteerPositionDto { Id = "pos-1", Title = "Test Position" }
        };

        _volunteerService.GetEventVolunteerPositionsAsync(eventId, null, Arg.Any<CancellationToken>())
            .Returns((true, positions, null));

        // Act
        var result = await VolunteerEndpoints.GetEventVolunteerPositions(_volunteerService, eventId);

        // Assert
        result.Should().BeOfType<Ok<List<VolunteerPositionDto>>>();
        var okResult = (Ok<List<VolunteerPositionDto>>)result;
        okResult.Value.Should().HaveCount(1);
    }
}
```

**Prevention Rules**:
1. ✅ **EVERY service class MUST have a corresponding interface**
2. ✅ **Interface defines ALL public methods** used by endpoints
3. ✅ **DI registration uses interface**: `services.AddScoped<IServiceName, ServiceName>()`
4. ✅ **Endpoints inject interface**: `async (IServiceName service) => { }`
5. ✅ **Test mocking uses interface**: `Substitute.For<IServiceName>()`

**Why This Matters**:
- Unit testing with mocking frameworks requires interfaces
- NSubstitute cannot mock concrete classes with constructor dependencies
- Without interface, tests cannot isolate endpoint logic
- Future flexibility for multiple implementations
- Dependency injection best practices

**Files Modified** (Example from VolunteerService fix):
- `/apps/api/Features/Volunteers/Services/IVolunteerService.cs` (NEW)
- `/apps/api/Features/Volunteers/Services/VolunteerService.cs` (Updated to implement interface)
- `/apps/api/Features/Volunteers/VolunteerEndpoints.cs` (Updated to inject interface)
- `/apps/api/ServiceCollectionExtensions.cs` (Updated DI registration)

**Related Pattern**: See "Pattern B Endpoint Testing" lesson for complete unit testing approach

---

## 🚨 CRITICAL: Missing ThenInclude for Navigation Properties Causes Null Values 🚨

**Problem**: Calculated properties returning null even though navigation properties are included
**Date**: 2025-11-08
**Impact**: All sold counts showing null in API responses despite EventAttendances existing in database

**Root Cause**:
- EventService included `.Include(e => e.EventAttendances)` to load attendance records
- But Session.CurrentAttendees and TicketType.Sold calculated properties need nested navigation properties:
  - `ea.TicketPurchase` (to check if it's not null)
  - `ea.TicketPurchase.TicketType` (to match session/ticket type)
- Without `.ThenInclude()`, these nested properties were null
- Caused calculated properties to return null instead of actual counts

**Specific Calculated Properties Affected**:

1. **Session.CurrentAttendees** (`apps/api/Models/Session.cs:71-86`):
```csharp
// Needs ea.TicketPurchase and ea.TicketPurchase.TicketType.SessionId
public int CurrentAttendees
{
    get
    {
        if (Event?.EventAttendances == null) return 0;

        return Event.EventAttendances.Count(ea =>
            ea.Status == AttendanceStatus.Active &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.TicketPurchase != null &&  // ← NULL without ThenInclude
            (ea.TicketPurchase.TicketType.SessionId == Id ||  // ← NULL
             (ea.TicketPurchase.TicketType.SessionId == null &&
              ea.TicketPurchase.TicketType.EventId == EventId)));
    }
}
```

2. **TicketType.Sold** (`apps/api/Models/TicketType.cs`):
```csharp
// Similar dependency on TicketPurchase navigation
```

**Solution**: Add ThenInclude chain for complete navigation property loading

**Fix Applied** (`apps/api/Features/Events/Services/EventService.cs`):

```csharp
// ❌ BEFORE - Loads EventAttendances but NOT nested properties
.Include(e => e.EventAttendances)

// ✅ AFTER - Loads full navigation chain
.Include(e => e.EventAttendances)
    .ThenInclude(ea => ea.TicketPurchase)  // Load purchase for each attendance
        .ThenInclude(tp => tp.TicketType);  // Load ticket type for each purchase
```

**Applied to Both Query Methods**:
1. `GetEventsAsync()` - Line 59-61
2. `GetEventAsync()` - Line 144-146

**Testing Verification**:
```bash
# BEFORE fix
curl http://localhost:5655/api/events | jq '.data[0].sessions[0].registrationCount'
# Output: null

# AFTER fix
curl http://localhost:5655/api/events | jq '.data[0].sessions[0].registrationCount'
# Output: 5

# TicketType sold counts also working
curl http://localhost:5655/api/events | jq '.data[0].ticketTypes[0].quantitySold'
# Output: 5
```

**Integration Tests**: 47/71 passing (no regressions, all failures pre-existing auth issues)

**Prevention**:
1. **When using calculated properties**, trace ALL navigation properties accessed
2. **For nested access** (e.g., `ea.TicketPurchase.TicketType.SessionId`), need TWO `.ThenInclude()` calls
3. **Test calculated properties** after adding Include statements - verify they return values, not null
4. **Use AsNoTracking queries** with caution - ensure all needed navigation loaded upfront
5. **Check for null** in calculated properties AND ensure Include chain loads the data

**Pattern for Nested Navigation**:
```csharp
// For property: entity.Child.GrandChild.Property
.Include(e => e.Children)           // First level
    .ThenInclude(c => c.GrandChild) // Second level
        .ThenInclude(gc => gc.Property); // Third level if needed
```

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs` (lines 59-61, 144-146)

**Related Issue**: Seed data was also missing initially (separate fix)

---

## 🚨 CRITICAL: Dependency Injection - Inject Interfaces, Not Concrete Classes (2025-11-16)

**Problem**: CMS page update endpoint returning 500 Internal Server Error for ALL save attempts
**Date**: November 16, 2025
**Impact**: CMS functionality completely broken - users cannot save any content changes

**Root Cause**:
- Endpoint injected **concrete class** `ContentSanitizer` instead of **interface** `IContentSanitizer`
- DI container registered **interface only**: `services.AddSingleton<IContentSanitizer, ContentSanitizer>()`
- ASP.NET Core cannot resolve concrete type when only interface is registered
- Results in 500 error during request processing

**Error Manifestation**:
- Frontend: "Failed to update page: Internal Server Error"
- HTTP Status: 500 Internal Server Error
- Location: `PUT /api/cms/pages/{id}` endpoint
- No specific error message visible to frontend (DI resolution fails before endpoint executes)

**Wrong Implementation** (Concrete Class Injection):
```csharp
// ❌ WRONG - Endpoint injects concrete class
private static async Task<IResult> UpdatePage(
    int id,
    [FromBody] UpdateContentPageRequest request,
    ClaimsPrincipal user,
    [FromServices] ApplicationDbContext db,
    [FromServices] ContentSanitizer sanitizer,  // ← CONCRETE CLASS
    [FromServices] ILogger<Program> logger,
    CancellationToken ct)
{
    var cleanContent = sanitizer.Sanitize(request.Content);
    // ...
}

// DI registration (only interface registered)
services.AddSingleton<IContentSanitizer, ContentSanitizer>();
```

**Correct Implementation** (Interface Injection):
```csharp
// ✅ CORRECT - Endpoint injects interface
private static async Task<IResult> UpdatePage(
    int id,
    [FromBody] UpdateContentPageRequest request,
    ClaimsPrincipal user,
    [FromServices] ApplicationDbContext db,
    [FromServices] IContentSanitizer sanitizer,  // ← INTERFACE
    [FromServices] ILogger<Program> logger,
    CancellationToken ct)
{
    var cleanContent = sanitizer.Sanitize(request.Content);
    // ...
}

// DI registration (interface registered)
services.AddSingleton<IContentSanitizer, ContentSanitizer>();
```

**Prevention Rules**:
1. ✅ **ALWAYS inject interfaces** in endpoint parameters: `[FromServices] IServiceName service`
2. ✅ **NEVER inject concrete classes** unless explicitly registered as concrete type
3. ✅ **Match DI registration** - if registered as interface, inject interface
4. ✅ **Check both sides** - ensure endpoint injection matches DI registration type
5. ✅ **Follow SOLID principles** - Dependency Inversion Principle (depend on abstractions)

**Why This Matters**:
- DI container resolves dependencies based on registered types
- Interface registration does NOT automatically resolve concrete class requests
- 500 errors are cryptic - doesn't tell you it's a DI resolution problem
- Affects ALL endpoints using the incorrectly injected dependency
- Silent failure - no compilation error, only runtime failure

**Detection**:
- 500 errors on endpoints that should work
- Check endpoint parameter types against DI registrations
- Verify `[FromServices]` parameters use interfaces, not concrete classes
- Look for mismatches between registration and injection

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Cms/CmsEndpoints.cs` (line 98)

**Related Pattern**: See "Services MUST Have Interfaces for Unit Testing" lesson for interface creation guidance

---
