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

## 🚨 CRITICAL: ASP.NET Core Minimal API Cannot Bind List<string> from Query Parameters (2025-11-17)

**Problem**: API startup fails with `InvalidOperationException: PaymentMethods must have a valid TryParse method` when endpoint parameter uses `List<string>` for query parameters.

**Date Discovered**: November 17, 2025 during homepage features testing
**Context**: `/api/admin/payments` endpoint had `PaymentMethods` and `Statuses` parameters defined as `List<string>?`

**Root Cause**:
- ASP.NET Core minimal APIs cannot automatically bind `List<string>` from query parameters
- Framework requires either a custom type with `TryParse` method, OR use of `[FromQuery]` with proper binding, OR accept string and split manually
- Query parameters with complex collection types crash API startup

**Error Message**:
```
System.InvalidOperationException: PaymentMethods must have a valid TryParse method to support converting from a string.
No public static bool List<string>.TryParse(string, out List<string>) method found for PaymentMethods.
```

**Impact**:
- API fails to start completely - blocks ALL development and testing
- Error is NOT a compilation error - only appears at runtime startup
- Affects any endpoint using `List<string>` or complex collection types as query parameters

**Wrong Implementation** (List<string> for Query Parameters):
```csharp
// ❌ WRONG - API will fail to start
public class PaymentListQueryParameters
{
    [FromQuery(Name = "paymentMethods")]
    public List<string>? PaymentMethods { get; set; }  // CRASHES API STARTUP!

    [FromQuery(Name = "statuses")]
    public List<string>? Statuses { get; set; }  // CRASHES API STARTUP!
}
```

**Correct Implementation** (Comma-Separated String):
```csharp
// ✅ CORRECT - Use string and split in service layer
public class PaymentListQueryParameters
{
    /// <summary>
    /// Filter by payment methods (PayPal, Free, Venmo) - comma-separated
    /// </summary>
    [FromQuery(Name = "paymentMethods")]
    public string? PaymentMethods { get; set; }  // API starts successfully

    /// <summary>
    /// Filter by payment statuses (Paid, Refunded, Pending, Failed) - comma-separated
    /// </summary>
    [FromQuery(Name = "statuses")]
    public string? Statuses { get; set; }  // API starts successfully
}

// Service layer - split comma-separated values
public async Task<PaymentListResponse> GetPaymentListAsync(
    PaymentListQueryParameters parameters,
    CancellationToken cancellationToken)
{
    // Split comma-separated string into list
    if (!string.IsNullOrWhiteSpace(parameters.PaymentMethods))
    {
        var methods = parameters.PaymentMethods
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.ToUpper())
            .ToList();
        query = query.Where(x => methods.Contains(x.Payment.PaymentMethodType.ToString().ToUpper()));
    }

    if (!string.IsNullOrWhiteSpace(parameters.Statuses))
    {
        var statuses = parameters.Statuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToUpper())
            .ToList();
        query = query.Where(x => statuses.Contains(x.Payment.Status.ToString().ToUpper()));
    }
}
```

**Why This Pattern Works**:
- String binding is natively supported by ASP.NET Core
- Query parameters are naturally strings in URLs: `?paymentMethods=PayPal,Venmo,Free`
- Splitting in service layer provides full control over parsing logic
- `StringSplitOptions.RemoveEmptyEntries | TrimEntries` handles edge cases

**Alternative Solutions** (More Complex):
1. **Custom Type with TryParse**:
```csharp
public class PaymentMethodList
{
    public List<string> Methods { get; set; } = new();

    public static bool TryParse(string? value, out PaymentMethodList result)
    {
        result = new PaymentMethodList();
        if (!string.IsNullOrWhiteSpace(value))
        {
            result.Methods = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        return true;
    }
}
```

2. **Model Binding with IModelBinder** (overkill for simple cases)

**RECOMMENDED**: Use comma-separated string pattern - simplest and most straightforward.

**Prevention Checklist**:
- [ ] Use `string` for query parameters that represent collections
- [ ] Split comma-separated values in service layer
- [ ] Document parameter format in XML comments: "comma-separated"
- [ ] Test API startup after adding query parameters with complex types
- [ ] Use OpenAPI/Swagger to verify parameter types are correct

**Detection**:
- API fails to start with `InvalidOperationException` about `TryParse`
- Error mentions query parameter property name
- Appears immediately on startup, not during requests
- Check all `[FromQuery]` parameters for complex collection types

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Payments/Models/Requests/PaymentListQueryParameters.cs` (lines 32, 38)
- `/home/chad/repos/witchcityrope/apps/api/Features/Payments/Services/PaymentListService.cs` (lines 103-120)

**OpenAPI Impact**:
- Correctly generates `type: string` in OpenAPI spec
- Frontend can pass comma-separated values: `paymentMethods=PayPal,Venmo`
- NSwag type generation produces correct TypeScript types

---

## 🚨 CRITICAL: Missing Change Detection for Audit Logging - Profile Updates Not Tracked (2025-11-20)

**Problem**: UserNote entries with NoteType='ProfileChange' were NOT being created when users updated their profiles. Database query showed ZERO ProfileChange entries despite users actively updating their profiles.

**Date Discovered**: November 20, 2025
**Context**: Profile update functionality existed but lacked change tracking and audit logging

**Root Cause**: UpdateUserProfileAsync method was MISSING:
- Change detection logic before applying updates
- UserNote creation for changed fields
- SaveChangesAsync call for UserNote persistence

**Impact**:
- No audit trail of profile changes
- Admins couldn't see what users changed
- Compliance/safety issues - no record of data modifications
- Database confirmed: `SELECT COUNT(*) FROM "UserNotes" WHERE "NoteType" = 'ProfileChange';` → 0 rows

**Investigation Process**:
1. ✅ Verified database schema - UserNotes table exists with correct structure
2. ✅ Verified DbSet exists - ApplicationDbContext has `DbSet<UserNote> UserNotes`
3. ❌ Examined service code - NO change detection or UserNote creation logic found
4. ❌ Code mentioned in original task (lines 243-266, 287-307) did NOT exist in file

**Solution Implemented**:

**1. Change Detection Logic** (lines 242-286):
```csharp
// Track changed fields for audit logging
var changedFields = new List<(string FieldName, string? OldValue, string? NewValue)>();

// Helper method for change detection (normalize null and empty string)
bool HasChanged(string? oldValue, string? newValue)
{
    var normalizedOld = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
    var normalizedNew = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
    return !string.Equals(normalizedOld, normalizedNew, StringComparison.Ordinal);
}

// Detect changes for each field BEFORE applying updates
if (HasChanged(user.SceneName, request.SceneName))
    changedFields.Add(("Scene Name", user.SceneName, request.SceneName));

if (HasChanged(user.FirstName, request.FirstName))
    changedFields.Add(("First Name", user.FirstName, request.FirstName));

// ... (similar for all profile fields)

_logger.LogInformation("Change detection complete: {Count} changes found for user {UserId}", changedFields.Count, userId);
```

**2. UserNote Creation After Successful Update** (lines 304-331):
```csharp
if (updateResult.Succeeded)
{
    // Create UserNote entries for each changed field
    if (changedFields.Count > 0)
    {
        _logger.LogInformation("Creating {Count} UserNote entries for profile changes (user {UserId})", changedFields.Count, userId);

        foreach (var (fieldName, oldValue, newValue) in changedFields)
        {
            var note = new WitchCityRope.Api.Data.Entities.UserNote
            {
                UserId = userId,
                NoteType = "ProfileChange",
                Content = $"{fieldName} changed from \"{oldValue ?? "(empty)"}\" to \"{newValue ?? "(empty)"}\"",
                AuthorId = userId, // User changed their own profile
                CreatedAt = DateTime.UtcNow
            };
            _context.UserNotes.Add(note);
        }

        // CRITICAL: Save UserNote entries to database
        var savedCount = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {Count} UserNote entries to database", savedCount);
    }
    else
    {
        _logger.LogInformation("No changes detected - skipping UserNote creation");
    }
}
```

**Why This Approach Works**:
- ✅ **Change detection BEFORE updates**: Captures original values before modification
- ✅ **Null handling**: Normalizes null and empty string to prevent false positives
- ✅ **Whitespace trimming**: "test" vs " test" correctly detected as same value
- ✅ **One note per changed field**: Clear audit trail with old/new values
- ✅ **Only creates notes for actual changes**: Checks `changedFields.Count > 0`
- ✅ **Structured logging**: Confirms detection, creation, and persistence
- ✅ **Calls SaveChangesAsync**: Actually persists UserNotes to database

**Testing Verification**:
```bash
# Update profile via API
curl -X PUT 'http://localhost:5173/api/dashboard/profile' \
  -H 'Content-Type: application/json' \
  -b cookies.txt \
  -d '{"bio": "Updated bio text", "pronouns": "they/them"}'

# Check logs for change detection (use container-restart skill for log viewing)
# Expected output: "Change detection complete: 2 changes found"

# Verify database has UserNote entries
docker exec witchcity-postgres psql -U postgres -d witchcityrope_dev \
  -c "SELECT \"Content\" FROM \"UserNotes\" WHERE \"NoteType\" = 'ProfileChange' ORDER BY \"CreatedAt\" DESC LIMIT 5;"

# Expected results:
#  Bio changed from "(empty)" to "Updated bio text"
#  Pronouns changed from "(empty)" to "they/them"
```

**Prevention Checklist**:
- [ ] **Change detection BEFORE entity updates**: Always capture original values first
- [ ] **Normalize null and empty string**: Use helper method for reliable comparison
- [ ] **Log change detection results**: Confirm code execution and change count
- [ ] **Only create audit logs after successful save**: Check updateResult.Succeeded first
- [ ] **Call SaveChangesAsync for audit logs**: UserNotes won't persist without it
- [ ] **Verify database has entries**: Query database to confirm persistence
- [ ] **Test with actual profile updates**: Don't assume code runs - verify in logs

**Pattern - Change Detection for Audit Logging**:
```csharp
// 1. Detect changes BEFORE updating entity
var changedFields = new List<(string Field, string? Old, string? New)>();

// 2. Helper method for reliable comparison
bool HasChanged(string? oldValue, string? newValue)
{
    var normalizedOld = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
    var normalizedNew = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
    return !string.Equals(normalizedOld, normalizedNew, StringComparison.Ordinal);
}

// 3. Compare each field
if (HasChanged(entity.Field, request.Field))
    changedFields.Add(("Field Name", entity.Field, request.Field));

// 4. Log detection results
_logger.LogInformation("Change detection: {Count} changes found", changedFields.Count);

// 5. Apply updates to entity
entity.Field = request.Field;

// 6. Save entity changes
var result = await SaveEntityAsync(entity);

// 7. Create audit logs ONLY if save succeeded
if (result.Succeeded && changedFields.Count > 0)
{
    foreach (var (field, oldVal, newVal) in changedFields)
    {
        CreateAuditLog(field, oldVal, newVal);
    }
    await _context.SaveChangesAsync(); // CRITICAL: Persist audit logs
}
```

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs`
  - Lines 242-286: Change detection logic
  - Lines 304-331: UserNote creation and persistence

**Analysis Document**: `/home/chad/repos/witchcityrope/test-results/profile-change-logging-fix-2025-11-20.md`

**Success Criteria**:
- ✅ API builds with 0 errors
- ✅ Change detection logs show detected changes
- ✅ UserNote creation logs confirm entries added
- ✅ Database query returns ProfileChange entries
- ✅ Each changed field has corresponding UserNote entry
- ✅ Old and new values correctly captured

**Related Patterns**:
- **Defensive Persistence Verification** (Part 2, lines 115-265): Verify data persisted with fresh query
- **EF Core Change Tracking** (Part 2, lines 1211-1320): Explicit Update() and SaveChangesAsync
- Similar to audit logging patterns in VettingService and SafetyService

**Tags**: #critical #change-detection #audit-logging #usernotes #profile-updates #savechanges #missing-implementation #data-integrity

---
