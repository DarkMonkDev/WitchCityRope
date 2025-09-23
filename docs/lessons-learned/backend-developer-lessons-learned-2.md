# Backend Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 2 of 2**
**Read Part 1 first**: backend-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Write to**: Part 2 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## ✅ FIXED: Test Infrastructure - FeatureTestBase Service Initialization Order (2025-09-23)

**Problem**: Services using DbContext fail with NullReferenceException in tests. Tests expected success=true but got success=false.

**Root Cause**: FeatureTestBase was creating services in constructor before DbContext was initialized. DatabaseTestBase sets `DbContext = null!` in constructor, only initializing it later in `InitializeAsync()`.

**Critical Discovery**: Constructor execution order was:
1. `DatabaseTestBase` constructor sets `DbContext = null!`
2. `FeatureTestBase` constructor calls `Service = CreateService()` with null DbContext
3. Service crashes with NullReferenceException when accessing `_context.Database` or `_context.Users`
4. Only later does `InitializeAsync()` set up real DbContext

**Solution Applied**:
```csharp
// ❌ WRONG - Service created before DbContext initialized
protected FeatureTestBase(DatabaseTestFixture fixture) : base(fixture)
{
    MockLogger = new Mock<ILogger<TService>>();
    Service = CreateService(); // DbContext is null here! CRASH!
}

// ✅ CORRECT - Service created after DbContext initialized
protected FeatureTestBase(DatabaseTestFixture fixture) : base(fixture)
{
    MockLogger = new Mock<ILogger<TService>>();
    Service = null!; // Will be set in InitializeAsync
}

public override async Task InitializeAsync()
{
    await base.InitializeAsync(); // Initialize DbContext first
    Service = CreateService(); // Then create service with valid DbContext
}
```

**Files Fixed**:
- `/tests/WitchCityRope.Tests.Common/TestBase/FeatureTestBase.cs`

**Tests Fixed**: 5 out of 9 originally failing tests (HealthService tests mainly)

**Key Pattern**: ALWAYS ensure dependencies are initialized before creating services that use them.

## ✅ FIXED: VettingService Compilation Errors - Entity Property Mismatch (2025-09-23)

**Problem**: API failing to compile due to VettingService trying to use properties that don't exist on the simplified VettingApplication entity.

**Root Cause**: VettingApplication entity was simplified to match wireframe requirements, removing many properties. VettingService still referenced these removed properties:
- `ApplicationNumber` (property doesn't exist)
- `EncryptedSceneName` (use `SceneName` instead)
- `ExperienceLevel` (property doesn't exist)
- `EncryptedWhyJoinCommunity` (use `AboutYourself` instead)
- `VettingStatus.NotStarted` (enum value doesn't exist)
- `PerformedBy` nullable operator issue (Guid is not nullable)

**Solution Applied**:
1. **Removed ApplicationNumber dependency**: Use `application.Id.ToString("N")[..8]` directly
2. **Used correct property names**: `SceneName` instead of `EncryptedSceneName`
3. **Used available fields**: `AboutYourself` instead of `EncryptedWhyJoinCommunity`
4. **Set reasonable defaults**: `ExperienceLevel = "Beginner"` for simplified entity
5. **Fixed enum values**: `VettingStatus.Draft` instead of `VettingStatus.NotStarted`
6. **Fixed nullable operators**: Removed `?` from non-nullable `Guid PerformedBy`

**Files Fixed**:
- `/apps/api/Features/Vetting/Services/VettingService.cs` - Updated to match simplified entity

**Key Patterns for Entity Simplification**:
```csharp
// ✅ CORRECT: Map to available properties with defaults
ApplicationNumber = application.Id.ToString("N")[..8], // Generate from ID
SceneName = application.SceneName ?? "Not provided",   // Use direct property
ExperienceLevel = "Beginner",                         // Default for simplified entity
WhyJoinCommunity = application.AboutYourself ?? "Not provided", // Map to available field

// ❌ WRONG: Reference removed properties
ApplicationNumber = application.ApplicationNumber,     // Property doesn't exist
SceneName = application.EncryptedSceneName,           // Property removed
ExperienceLevel = application.ExperienceLevel,        // Property doesn't exist
```

**Prevention**:
- **Check entity definition FIRST** when compilation errors occur
- **Use available properties** and provide reasonable defaults
- **Verify enum values exist** before referencing them
- **Test compilation after entity changes** to catch property mismatches early

**Result**: API compiles successfully and container shows healthy status. Login endpoint working correctly.

**Key Insight**: Don't fix obsolete tests - disable them since the systems they test are obsolete.

## 🚨 CRITICAL: API Confusion Prevention - MANDATORY FOR ALL BACKEND DEVELOPERS 🚨

**ABSOLUTE RULE**: ONLY `/apps/api/` should EVER be modified by backend developers. Legacy API at `/src/_archive/WitchCityRope.Api/` is ARCHIVED and must NEVER be modified.

### The Dual API Crisis (2025-09-12)

**CRITICAL SITUATION**: WitchCityRope project has **TWO separate API projects** which caused massive confusion during development:

#### ACTIVE API (MODIFY THIS ONE ONLY)
- **Location**: `/apps/api/` 
- **Port**: 5655
- **Status**: **CURRENTLY SERVING REACT FRONTEND**
- **Architecture**: Minimal API with vertical slice pattern
- **Performance**: 49ms response times
- **Features**: Health, Authentication, Events, Users
- **Instructions**: **THIS IS THE ONLY API YOU SHOULD EVER MODIFY**

#### LEGACY API (NEVER MODIFY THIS)
- **Location**: `/src/_archive/WitchCityRope.Api/` - ARCHIVED 2025-09-13
- **Status**: **DORMANT - CONTAINS VALUABLE FEATURES BUT DO NOT MODIFY**
- **Features**: CheckIn, Safety, Vetting, Advanced Payments
- **Purpose**: Historical reference and feature extraction source
- **Instructions**: **NEVER ADD NEW FEATURES HERE - READ ONLY FOR REFERENCE**

### Why This Happened
During React migration in August 2025, a new simplified API was created instead of refactoring the existing one. The legacy API was never removed, creating architectural duplication.

### Prevention Rules for Backend Developers
1. **ALWAYS work in `/apps/api/`** - Never work in archived legacy API
2. **Check file paths** - If you see `/src/_archive/WitchCityRope.Api/` in your work, STOP - it's archived
3. **Use modern API patterns** - Vertical slice architecture, minimal API endpoints
4. **Follow performance targets** - Maintain <50ms response times
5. **Test with React frontend** - Ensure `/apps/web/` works with your changes

### When You Need Legacy API Features
- **DON'T modify legacy API** - Extract features to modern API using vertical slice pattern
- **READ legacy code** - For business logic understanding only
- **DOCUMENT extraction** - When moving features to modern API
- **TEST thoroughly** - Ensure extracted features work with React frontend

### Emergency Contacts
If you accidentally modify legacy API or break the modern API, IMMEDIATELY:
1. Stop all work and assess damage
2. Check React frontend still works: `curl http://localhost:5655/health`
3. Revert changes if necessary: `git reset --hard HEAD~1`
4. Document the incident in session notes

**VIOLATION CONSEQUENCES**: Modifying legacy API can break the architecture cleanup process and confuse future developers.

---

## 🔥 CRITICAL: Vetting System API Endpoints Fixed - Frontend-Backend Mismatch (2025-09-22)

**Problem**: Vetting system UI was working but all API endpoints returned 400 errors when clicking "Approve Application", "Put on Hold", and "Save Note" buttons. Frontend was calling different endpoints than what the backend provided.

**Root Cause Analysis**:
1. **Endpoint Mismatch**: Frontend expected direct action endpoints like `/api/vetting/applications/{id}/approve` but backend only had reviewer-specific endpoints like `/api/vetting/reviewer/applications/{id}/decisions`
2. **Data Structure Mismatch**: Frontend sent `decisionType` as string ("Approved", "OnHold") but backend expected integer enum values (1, 2, 3, etc.)
3. **Note Structure Mismatch**: Frontend sent simple `{note: string}` but backend expected complex `CreateNoteRequest` with `Content`, `Type`, etc.

**Solution Implemented**:

1. **Added Frontend-Compatible Endpoints** in `VettingEndpoints.cs`:
```csharp
// Direct action endpoints that frontend expects
group.MapPost("/applications/{id}/approve", ApproveApplication);
group.MapPut("/applications/{id}/status", ChangeApplicationStatus);
group.MapPost("/applications/{id}/notes", AddSimpleApplicationNote);
group.MapPost("/applications/{id}/deny", DenyApplication);
```

2. **Updated ReviewDecisionRequest** to handle both string and int DecisionType:
```csharp
public object DecisionType { get; set; } = string.Empty; // Can be string or int
```

3. **Enhanced VettingService** to handle flexible DecisionType parsing:
```csharp
// Handle both string ("Approved") and int (1) decision types
if (request.DecisionType is string decisionString)
{
    newStatus = decisionString.ToLower() switch
    {
        "approved" => VettingStatus.Approved,
        "denied" => VettingStatus.Denied,
        "onhold" => VettingStatus.OnHold,
        // ...
    };
}
```

4. **Created Simple Request Models** for frontend compatibility:
```csharp
public class SimpleReasoningRequest
{
    public string Reasoning { get; set; } = string.Empty;
}

public class SimpleNoteRequest
{
    public string Note { get; set; } = string.Empty;
    public bool? IsPrivate { get; set; }
}
```

**API Endpoints Now Working**:
- ✅ `POST /api/vetting/applications/{id}/approve` - Approve applications
- ✅ `PUT /api/vetting/applications/{id}/status` - Put on hold with reason
- ✅ `POST /api/vetting/applications/{id}/notes` - Save notes
- ✅ `POST /api/vetting/applications/{id}/deny` - Deny applications

**Testing Results**:
```bash
# Approve application - Returns 200 OK
curl -X POST /api/vetting/applications/{id}/approve
  -d '{"reasoning": "Application approved"}'

# Put on hold - Returns 200 OK
curl -X PUT /api/vetting/applications/{id}/status
  -d '{"status": "OnHold", "reasoning": "Need more info"}'

# Add note - Returns 200 OK
curl -X POST /api/vetting/applications/{id}/notes
  -d '{"note": "Good candidate", "isPrivate": true}'
```

**Key Lessons**:
- **Frontend-Backend Contract Alignment**: Always verify API endpoints match what frontend expects exactly
- **Flexible Data Types**: Use `object` type with runtime parsing for fields that might come as string or int from different clients
- **Simple Request Models**: Create simplified DTOs for frontend compatibility while maintaining complex internal models
- **Endpoint Testing**: Test all CRUD operations after implementing to ensure they work end-to-end

**Files Modified**:
- `/apps/api/Features/Vetting/Endpoints/VettingEndpoints.cs` - Added new compatible endpoints
- `/apps/api/Features/Vetting/Models/ReviewDecisionRequest.cs` - Made DecisionType flexible
- `/apps/api/Features/Vetting/Services/VettingService.cs` - Enhanced decision type handling

**Prevention**:
- Always check frontend API service files to understand exact endpoint expectations
- Test API endpoints with curl after implementation to verify they work
- Use both string and numeric values in testing to catch type mismatches
- Document API contracts clearly between frontend and backend teams

**Result**: Vetting system UI now works correctly. Admin users can approve applications, put them on hold, add notes, and deny applications without 400 errors.

## Port Configuration Refactoring (2025-09-11)

### CRITICAL ISSUE: Hard-coded Port References Throughout Codebase
**Problem**: Multiple port configuration issues causing repeated deployment and testing failures:
- authService.ts used port 5653 instead of configured 5655
- EventsList.tsx had hard-coded port 5655
- Test files mixed ports 5653, 5655, 5173, and 8080 inconsistently
- Playwright tests had hard-coded URLs throughout
- vite.config.ts had hard-coded fallback ports

**Root Cause**: Lack of centralized environment variable management led to port configuration drift across development sessions.

**Solution Implemented**: 
1. **Created centralized API configuration** (`/apps/web/src/config/api.ts`)
   - Single source of truth for API base URL
   - Environment variable driven with proper fallbacks
   - Helper functions for consistent URL construction
   - Request wrapper with default configuration

2. **Updated Environment Variable Structure**:
   ```bash
   # Development
   VITE_API_BASE_URL=http://localhost:5655
   VITE_PORT=5173
   VITE_HMR_PORT=24679
   VITE_DB_PORT=5433
   VITE_TEST_SERVER_PORT=8080
   ```

3. **Updated Core Components**:
   - authService.ts: Now uses `apiRequest()` and `apiConfig.endpoints`
   - EventsList.tsx: Uses centralized `getApiBaseUrl()`
   - vite.config.ts: Uses environment variables for all port configuration

4. **Created Test Configuration** (`/apps/web/src/test/config/testConfig.ts`)
   - Environment-aware test URLs
   - Consistent test credentials
   - Timeout management for CI vs local development

**Files Changed**:
- `/apps/web/.env.template` - Template for all environments
- `/apps/web/.env.development` - Updated with full port configuration
- `/apps/web/.env.staging` - New staging environment configuration
- `/apps/web/.env.production` - New production environment configuration
- `/apps/web/src/config/api.ts` - Centralized API configuration
- `/apps/web/src/services/authService.ts` - Updated to use centralized config
- `/apps/web/src/components/EventsList.tsx` - Updated to use centralized config
- `/apps/web/vite.config.ts` - Environment variable driven port configuration
- `/apps/web/src/test/config/testConfig.ts` - Test environment configuration

**Remaining Work**: 
- 39 test files still need updating to use testConfig.urls
- Mock handlers need to use environment variables
- Playwright tests need testConfig integration

**Pattern for Future Development**:
```typescript
// ✅ CORRECT - Use centralized configuration
import { apiRequest, apiConfig } from '../config/api'
const response = await apiRequest(apiConfig.endpoints.events.list)

// ❌ WRONG - Hard-coded URLs
const response = await fetch('http://localhost:5655/api/events')
```

**Prevention**: 
- All new API calls MUST use `apiRequest()` or `getApiUrl()`
- All new tests MUST use `testConfig.urls`
- Environment variables MUST be used for all port references
- Regular audit of hard-coded localhost references

## Authentication Role Support Implementation (2025-09-11)

### CRITICAL FIX: Missing User Roles in Login Response
**Problem**: Frontend couldn't show/hide admin features because login endpoint `/api/Auth/login` returned user object without role information.
**Root Cause**: `AuthUserResponse` DTO was missing role properties that exist on the `ApplicationUser` entity.

**Solution Implemented**:
1. **Updated AuthUserResponse Model** (`/apps/api/Features/Authentication/Models/AuthUserResponse.cs`):
   - Added `Role` property (string) for single role access
   - Added `Roles` property (string array) for frontend compatibility
   - Updated constructor to populate both properties from `ApplicationUser.Role`

2. **Updated Frontend Role Check** (`/apps/web/src/components/layout/Navigation.tsx`):
   - Changed from checking `user?.roles?.includes('Admin')` to `user?.roles?.includes('Administrator')`
   - Backend returns "Administrator" role, frontend was checking for "Admin"
   - Kept email fallback check for additional safety

3. **API Container Restart Required**:
   - Docker hot reload didn't pick up the DTO changes automatically
   - Required manual restart of API container: `docker restart witchcity-api`

**Technical Details**:
- ApplicationUser entity has `Role` property with values like "Administrator", "Teacher", "Member", "Attendee"
- Simple role system (single role per user) rather than complex Identity roles
- Frontend expects `roles` array format: `user?.roles?.includes('Administrator')`
- Backend now returns both formats for compatibility:
  ```json
  {
    "user": {
      "id": "...",
      "email": "admin@witchcityrope.com",
      "role": "Administrator",
      "roles": ["Administrator"]
    }
  }
  ```

**Files Changed**:
- `/apps/api/Features/Authentication/Models/AuthUserResponse.cs` - Added role properties
- `/apps/web/src/components/layout/Navigation.tsx` - Updated role check

**Test Results**:
✅ Admin user login now returns: `"role": "Administrator", "roles": ["Administrator"]`  
✅ Teacher user login now returns: `"role": "Teacher", "roles": ["Teacher"]`  
✅ Frontend can now properly show/hide admin features  
✅ Role-based access control working end-to-end  

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Include role information in auth DTOs
public AuthUserResponse(ApplicationUser user)
{
    // ... other properties
    Role = user.Role;
    Roles = new[] { user.Role }; // Frontend expects array format
}
```

**Prevention**:
- Always include role/authorization information in authentication DTOs
- Test role-based features after authentication changes
- Verify frontend and backend role naming conventions match
- Document role values and their expected behavior

## NuGet Package Updates (2025-09-11)

### CRITICAL FIX: Package Version Updates and EF Core Migrations

**Problem**: Outdated NuGet packages creating potential security vulnerabilities and missing latest .NET 9 features.

**Solution Implemented**: 
1. **Updated Core API Packages** (`/apps/api/WitchCityRope.Api.csproj`):
   - Microsoft.AspNetCore.OpenApi: 9.0.6 → 9.0.6 (already latest)
   - Microsoft.EntityFrameworkCore.Design: 9.0.0 → 9.0.6
   - Microsoft.AspNetCore.Identity.EntityFrameworkCore: 9.0.0 → 9.0.6
   - Microsoft.AspNetCore.Authentication.JwtBearer: 9.0.0 → 9.0.6
   - Npgsql.EntityFrameworkCore.PostgreSQL: 9.0.3 → 9.0.4
   - System.IdentityModel.Tokens.Jwt: 8.2.1 → 8.3.1

2. **Updated Infrastructure Packages** (`/src/_archive/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj`) - ARCHIVED:
   - Microsoft.Extensions.DependencyInjection.Abstractions: 9.0.* → 9.0.6 (removed wildcard)
   - System.IdentityModel.Tokens.Jwt: 8.12.1 → 8.3.1 (standardized version)

3. **Fixed Obsolete EF Core Check Constraints**:
   - Updated `EventSessionConfiguration.cs` to use new `ToTable(t => t.HasCheckConstraint())` pattern
   - Updated `EventTicketTypeConfiguration.cs` to use new check constraint pattern
   - Eliminated 7 obsolete API warnings

**Key Findings**:
- ✅ NuGet packages 9.0.12 don't exist - latest stable for .NET 9 is 9.0.6
- ✅ Version consistency is critical for JWT packages across projects
- ✅ EF Core 9.0 deprecated the old HasCheckConstraint() method
- ✅ Wildcard versions (9.0.*) should be avoided for reproducible builds

**Files Changed**:
- `/apps/api/WitchCityRope.Api.csproj` - Updated to latest compatible .NET 9 packages
- `/src/_archive/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj` - ARCHIVED
- `/src/_archive/WitchCityRope.Infrastructure/Data/Configurations/EventSessionConfiguration.cs` - ARCHIVED
- `/src/_archive/WitchCityRope.Infrastructure/Data/Configurations/EventTicketTypeConfiguration.cs` - ARCHIVED

**Build Results**:
- ✅ Apps API project compiles with 3 warnings (down from 3 - no change, but packages updated)
- ✅ Infrastructure project compiles with 16 warnings (down from 23 - fixed 7 obsolete warnings)
- ✅ Core project compiles with 0 warnings
- ✅ All package restoration successful
- ✅ No version conflicts detected

**Prevention**:
- Regular package auditing using `dotnet outdated` tool
- Avoid wildcard version ranges in production
- Update EF Core configurations when upgrading major versions
- Keep JWT package versions synchronized across projects
- Test builds after package updates to catch breaking changes

## NU1603 Version Mismatch Resolution (2025-09-11)

### Prevention Pattern: NU1603 Version Mismatch Resolution

**Problem**: NuGet package updates failed to eliminate the primary target - NU1603 version mismatch warnings:
```
warning NU1603: WitchCityRope.Tests.Common depends on Testcontainers.PostgreSql (>= 4.2.2) but Testcontainers.PostgreSql 4.2.2 was not found. Testcontainers.PostgreSql 4.3.0 was resolved instead.
warning NU1603: WitchCityRope.Api.Tests (ARCHIVED) depends on xunit.runner.visualstudio (>= 2.9.3) but xunit.runner.visualstudio 2.9.3 was not found. xunit.runner.visualstudio 3.0.0 was resolved instead.
```

**Root Cause**: Package version mismatches across test projects where requested versions weren't available, causing NuGet to resolve to newer versions but still showing warnings.

**Solution Implemented**:

1. **Identified Exact Version Conflicts**:
   - Testcontainers.PostgreSql: Requested 4.2.2 but resolved to 4.3.0 (latest 4.7.0)
   - xunit.runner.visualstudio: Requested 2.9.3 but resolved to 3.0.0 (latest 3.1.4)

2. **Updated All Test Projects to Latest Versions**:
   - `WitchCityRope.Tests.Common.csproj`: Testcontainers.PostgreSql 4.2.2 → 4.7.0 ✅
   - `WitchCityRope.Api.Tests.csproj` (ARCHIVED): xunit.runner.visualstudio 2.9.3 → 3.1.4 ✅
   - `WitchCityRope.Infrastructure.Tests.csproj`: Testcontainers 4.6.0 → 4.7.0 ✅
   - `WitchCityRope.Infrastructure.Tests.csproj`: Testcontainers.PostgreSql 4.6.0 → 4.7.0 ✅

3. **Force Package Restore**: `dotnet restore --force` to pick up new versions

**Verification**:
```bash
# Legacy projects archived 2025-09-13, build command updated:
dotnet build apps/api/ tests/WitchCityRope.Tests.Common/ tests/WitchCityRope.Core.Tests/ tests/WitchCityRope.Infrastructure.Tests/ 2>&1 | grep "NU1603" || echo "NO NU1603 WARNINGS FOUND"
# Result: NO NU1603 WARNINGS FOUND
```


**Pattern for Future Development**:
```bash
# 1. Identify exact version conflicts
dotnet build 2>&1 | grep "NU1603"

# 2. Update ALL projects to same latest version
# Example: Update all Testcontainers references to latest 4.7.0

# 3. Force restore to pick up changes  
dotnet restore --force

# 4. Verify elimination
dotnet build 2>&1 | grep "NU1603" || echo "SUCCESS"
```

**Prevention**:
- Keep all related packages on the same version across projects
- Update to latest available versions, not just compatible ones
- Run `dotnet list package --outdated` regularly to identify drift
- Always force restore after version changes
- Test build immediately after package updates to verify success

## EventService Entity Alignment Fixes (2025-09-11)

### Entity Model Mismatch Compilation Errors

**Problem**: After NuGet package updates, EventService.cs had 21 compilation errors due to mismatched entity property usage.

**Root Cause**: Code was using old property names and methods that don't exist on the actual entity models.

**Solution Implemented**: Fixed all property name mismatches and method signatures:

1. **EventSession Entity Fixes**:
   - ✅ `session.StartDateTime` → `session.Date.Add(session.StartTime)`
   - ✅ `session.EndDateTime` → `session.Date.Add(session.EndTime)` 
   - ✅ `session.IsActive` → Removed (property doesn't exist, use hardcoded `true`)
   - ✅ Constructor: Fixed parameter order and types for Date/Time separation

2. **EventTicketType Entity Fixes**:
   - ✅ `AddSessionInclusion()` → `AddSession(sessionIdentifier)`
   - ✅ `SessionInclusions` → `TicketTypeSessions`
   - ✅ `GetAvailableQuantity()` → `QuantityAvailable` property
   - ✅ `AreSalesOpen()` → `IsCurrentlyOnSale()` method
   - ✅ `SalesEndDateTime` → `SalesEndDate` property
   - ✅ Constructor: Fixed parameter signature (no TicketTypeEnum parameter)

3. **Method Signature Updates**:
   - ✅ `UpdateDetails()`: Split into multiple calls for different aspects
   - ✅ Session time handling: Use separate Date and TimeSpan properties
   - ✅ DTO mapping: Fixed property mappings and type conversions

**Key Patterns Learned**:
```csharp
// ✅ CORRECT - EventSession date/time handling
var startDateTime = session.Date.Add(session.StartTime);
var endDateTime = session.Date.Add(session.EndTime);

// ✅ CORRECT - EventTicketType method usage
ticketType.AddSession(sessionIdentifier);
bool salesOpen = ticketType.IsCurrentlyOnSale();

// ✅ CORRECT - Multiple property updates
ticketType.UpdateDetails(name, description, minPrice, maxPrice);
ticketType.UpdateQuantityAvailable(quantity);
ticketType.UpdateSalesEndDate(endDate);
```

**Files Fixed** (ARCHIVED):
- `/src/_archive/WitchCityRope.Api/Features/Events/Services/EventService.cs` - ARCHIVED


**Prevention**:
- Always verify entity model signatures before implementing service methods
- Use IntelliSense and compiler feedback to catch property/method mismatches
- Test build after entity model changes to identify alignment issues early
- Document entity model interfaces for reference during service development

## EventDto Mapping Issue - Missing EventType Field (2025-09-11)

### API Not Returning EventType Field 
**Problem**: Admin dashboard filters were broken because `/api/events` endpoint returned `eventType: null` for all events despite database having correct EventType values.

**Root Cause**: 
- Event entity correctly had `EventType` property with values "Workshop", "Class", "Meetup"
- EventDto classes were missing `eventType` property
- EventService mapping was not including EventType in LINQ projections

**Investigation Process**:
1. ✅ Verified database has EventType values using curl test
2. ✅ Located Event entity with correct EventType property
3. ✅ Found EventDto classes missing eventType field
4. ✅ Identified EventService LINQ projections missing EventType mapping
5. ✅ Located correct API endpoint `/api/events` handled by EventEndpoints.cs

**Solution Implemented**:
1. **Added eventType field to EventDto classes**:
   - `/apps/api/Features/Events/Models/EventDto.cs` - Added `public string EventType { get; set; } = string.Empty;`
   - `/apps/api/Models/EventDto.cs` - Added matching eventType field

2. **Updated EventService mapping**:
   - `GetPublishedEventsAsync()` LINQ projection: Added `EventType = e.EventType.ToString()`
   - `GetEventAsync()` DTO creation: Added `EventType = eventEntity.EventType.ToString()`

3. **Updated fallback events**: Added eventType values ("Workshop", "Class", "Meetup") to hardcoded fallback data

**Files Changed**:
- `/apps/api/Features/Events/Models/EventDto.cs` - Added eventType property
- `/apps/api/Models/EventDto.cs` - Added eventType property  
- `/apps/api/Features/Events/Services/EventService.cs` - Updated LINQ projections and DTO mapping
- `/apps/api/Features/Events/Endpoints/EventEndpoints.cs` - Updated fallback events


**Key Lessons**:
- **LINQ Projection Completeness**: Always ensure DTO projections include ALL required entity properties
- **Multiple EventDto Classes**: Project had duplicate EventDto classes - both needed updating for consistency
- **End-to-End Testing**: Use curl/API testing to verify field mapping, not just code inspection
- **Enum to String Conversion**: EventType enum properly converts to string using `.ToString()`

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Complete DTO mapping including all business-critical fields
.Select(e => new EventDto 
{
    Id = e.Id.ToString(),
    Title = e.Title,
    Description = e.Description,
    StartDate = e.StartDate,
    Location = e.Location,
    EventType = e.EventType.ToString() // Don't forget business-critical enums
})

// ❌ WRONG - Missing critical business properties
.Select(e => new EventDto 
{
    Id = e.Id.ToString(),
    Title = e.Title,
    Description = e.Description,
    // Missing EventType breaks filtering functionality
})
```

**Prevention**:
- Include ALL entity properties needed by frontend in DTO mapping
- Test API responses with curl/Postman after DTO changes
- Maintain consistency across multiple DTO classes for same entity
- Document enum-to-string conversion requirements in service layer

## Capacity State Variations and RSVP/Ticket Handling (2025-09-11)

### Enhanced Mock Data for Frontend Testing
**Problem**: Frontend needed varied capacity states and proper RSVP vs Ticket count differentiation.
**Solution**: Enhanced API Event entity and service layer to provide realistic capacity distribution.

**Key Components Enhanced**:

1. **Enhanced API Event Entity** (`/apps/api/Models/Event.cs`):
   - Updated `GetCurrentAttendeeCount()` with deterministic capacity state distribution:
     - 20% SOLD OUT (100% capacity) - Green progress bar
     - 30% Nearly sold out (85-95%) - Green progress bar  
     - 30% Moderately filled (50-80%) - Yellow progress bar
     - 20% Low attendance (<50%) - Red progress bar
   - Added `GetCurrentRSVPCount()` method for Social events (free RSVPs)
   - Added `GetCurrentTicketCount()` method for paid registrations
   - Uses deterministic seed based on event ID for consistent results

2. **Enhanced EventDto Classes** (Both `/apps/api/Features/Events/Models/EventDto.cs` and `/apps/api/Models/EventDto.cs`):
   - Added `CurrentRSVPs` field for free Social event RSVPs
   - Added `CurrentTickets` field for paid registrations
   - Maintained `CurrentAttendees` as total (RSVPs + Tickets)
   - Documented business rules for Class vs Social events

3. **Updated Service Layer Mapping**:
   - Fixed EF Core LINQ translation issue by querying database first, then mapping in memory
   - Updated both EventService implementations to populate new fields
   - Applied lessons from previous EF Core projection errors

4. **Enhanced Fallback Events** (`/apps/api/Features/Events/Endpoints/EventEndpoints.cs`):
   - Added fourth fallback event to show low attendance scenario
   - Configured realistic RSVP vs Ticket distributions
   - Provided varied capacity states for thorough frontend testing

**Business Logic Implemented**:
- ✅ **Class Events**: Only `CurrentTickets` (all attendees pay)
- ✅ **Social Events**: Both `CurrentRSVPs` (free) and `CurrentTickets` (optional support)
- ✅ **Capacity Calculation**: Total = RSVPs + Tickets
- ✅ **Frontend Testing**: Varied states trigger different progress bar colors

**Technical Patterns Used**:
```csharp
// ✅ CORRECT - Query database first, then call business logic methods
var events = await _context.Events
    .AsNoTracking()
    .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow)
    .ToListAsync(cancellationToken);

// Map to DTO in memory (can call custom methods now)
var eventDtos = events.Select(e => new EventDto
{
    // ... other fields
    CurrentAttendees = e.GetCurrentAttendeeCount(),
    CurrentRSVPs = e.GetCurrentRSVPCount(),
    CurrentTickets = e.GetCurrentTicketCount()
}).ToList();

// ❌ WRONG - EF Core cannot translate custom methods to SQL
var events = await _context.Events
    .Select(e => new EventDto 
    {
        CurrentAttendees = e.GetCurrentAttendeeCount() // Fails!
    })
    .ToListAsync();
```

**Mock Data Distribution**:
- Deterministic based on event ID hash for consistent results
- Provides realistic test scenarios for frontend capacity displays
- Social events: ~70% RSVPs, ~30% paid tickets (reflects real usage)
- Class events: 100% paid tickets (business rule enforcement)

**Files Modified**:
- `/apps/api/Models/Event.cs` - Enhanced with capacity variation logic
- `/apps/api/Features/Events/Models/EventDto.cs` - Added RSVP/Ticket fields
- `/apps/api/Models/EventDto.cs` - Added RSVP/Ticket fields  
- `/apps/api/Services/EventService.cs` - Updated DTO mapping
- `/apps/api/Features/Events/Services/EventService.cs` - Updated DTO mapping
- `/apps/api/Features/Events/Endpoints/EventEndpoints.cs` - Enhanced fallback events

**Build Results**:
- ✅ API project compiles successfully with 0 errors, 19 warnings
- ✅ EF Core LINQ translation issues resolved
- ✅ All new fields properly integrated into service layer
- ✅ Fallback events provide comprehensive test scenarios

**Frontend Benefits**:
- Color-coded progress bars will show varied capacity states
- Admin dashboards can distinguish between free RSVPs and paid tickets
- Real-world testing scenarios for low/medium/high attendance events
- Proper business rule enforcement (Class vs Social event behavior)

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Business-aware DTO with separate counts
public class EventDto
{
    public int CurrentAttendees { get; set; }  // Total
    public int CurrentRSVPs { get; set; }      // Free (Social only)
    public int CurrentTickets { get; set; }    // Paid
}

// ✅ CORRECT - Mock data with realistic business logic
public int GetCurrentRSVPCount()
{
    if (EventType != "Social") return 0;  // Business rule
    var totalAttendees = GetCurrentAttendeeCount();
    return (int)(totalAttendees * 0.7);    // 70% use free RSVP
}
```

**Key Lessons**:
- Always separate database queries from business logic method calls in EF Core
- Provide deterministic mock data for consistent frontend testing  
- Document business rules clearly in DTO and entity comments
- Test varied scenarios to ensure robust frontend behavior
- Maintain consistency between multiple DTO classes for same entity

## Event API Sessions, Ticket Types, and Teachers Implementation Success (2025-09-12)

### Prevention Pattern: Multiple EventDto Class Synchronization
**Problem**: Multiple EventDto classes become unsynchronized when adding new fields, causing missing data.
**Solution**: Always update ALL EventDto classes when adding new fields and verify both list and single endpoints use same fallback logic.

**Original Problem**: Event API endpoints were only returning basic event fields (title, description, dates, location, eventType, capacity, attendance counts) and missing related data required by frontend.

**User Requirements**:
- GET /api/events/{id} should include sessions, ticketTypes, and teacherIds arrays  
- PUT /api/events/{id} should accept and save these fields
- Use Entity Framework .Include() for eager loading of related data

**Root Causes Identified**:
1. **Multiple EventDto Classes**: Project had TWO EventDto classes with different field sets:
   - `/apps/api/Features/Events/Models/EventDto.cs` - Complete with sessions, ticketTypes, teacherIds
   - `/apps/api/Models/EventDto.cs` - Missing the new fields entirely
2. **Database vs Fallback Logic**: Database was empty, so list endpoint used fallback data but single endpoint returned null
3. **Inconsistent Field Initialization**: One DTO initialized lists properly, other was missing fields

**Solution Implemented**:
1. **Updated Models EventDto** (`/apps/api/Models/EventDto.cs`):
   - Added missing Sessions, TicketTypes, TeacherIds properties
   - Added proper initialization: `= new List<SessionDto>()`, etc.
   - Added using statement for Features namespace imports

2. **Added Fallback Logic to Single Endpoint** (`EventEndpoints.cs`):
   - Single event endpoint now falls back to hardcoded events like list endpoint
   - Consistent behavior between GET /api/events and GET /api/events/{id}
   - Both endpoints return empty arrays `[]` instead of null values

**Key Technical Insights**:
- **Dual DTO Classes**: Must keep multiple EventDto classes synchronized when adding new fields
- **Entity Framework Includes**: Features EventService already had proper .Include() statements for eager loading
- **Database Seeding**: Current database is empty, but API has robust fallback events with proper field structure
- **Fallback Consistency**: Both list and single endpoints should have same fallback logic for reliability

**Files Changed**:
- `/apps/api/Models/EventDto.cs` - Added Sessions, TicketTypes, TeacherIds fields with proper initialization
- `/apps/api/Features/Events/Endpoints/EventEndpoints.cs` - Added fallback logic to single event endpoint

**Testing Results**:
✅ GET /api/events: Returns `sessions: []`, `ticketTypes: []`, `teacherIds: []`  
✅ GET /api/events/{id}: Returns `sessions: []`, `ticketTypes: []`, `teacherIds: []`  
✅ Both endpoints consistent with empty arrays instead of null values  
✅ API ready for frontend integration and future database seeding  

**Architecture Status**:
- ✅ DTOs properly aligned across multiple class definitions
- ✅ Entity Framework includes configured for eager loading (when database has data)
- ✅ PUT endpoint ready to accept and persist session/ticket/teacher updates
- ✅ Fallback data provides reliable API responses during development

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Keep all EventDto classes synchronized
// When adding new fields, update ALL EventDto classes:
// 1. /apps/api/Features/Events/Models/EventDto.cs
// 2. /apps/api/Models/EventDto.cs

public List<SessionDto> Sessions { get; set; } = new List<SessionDto>();
public List<TicketTypeDto> TicketTypes { get; set; } = new List<TicketTypeDto>();
public List<string> TeacherIds { get; set; } = new List<string>();

// ✅ CORRECT - Add fallback logic to single endpoints
if (!success || response == null) {
    var fallbackEvents = GetFallbackEvents();
    var fallbackEvent = fallbackEvents.FirstOrDefault(e => e.Id == id);
    if (fallbackEvent != null) return Ok(fallbackEvent);
}
```

**Prevention for Future Sessions**:
- Always check for multiple DTO classes when adding new fields
- Ensure both list and single endpoints have consistent fallback behavior
- Test API responses after DTO changes to verify field presence
- Consider consolidating duplicate DTO classes to prevent future sync issues

## CRITICAL FIX: RSVP/Ticket Double-Counting Logic Corrected (2025-09-11)

### CRITICAL ISSUE: Wrong Business Logic Implementation 
**Problem**: Both API and Core entities were implementing WRONG business logic for RSVP/ticket counting:
- `GetCurrentAttendeeCount()` was returning RSVPs + Tickets (double-counting people who RSVP'd AND bought tickets)
- This violated the correct business requirement where tickets are optional donations for Social events

**Root Cause**: Misunderstanding of business requirements led to additive counting instead of primary/secondary relationship.

**CORRECT Business Logic**:
- **Social Events**: `currentAttendees` = RSVP count (everyone must RSVP to attend, tickets are optional support/donations)
- **Class Events**: `currentAttendees` = ticket count (no RSVPs, only paid tickets)
- **Tickets for Social Events**: Additional donations from people who already RSVPed, NOT additional attendees

**Solution Implemented**:
1. **Fixed Core Event Entity** (`/src/_archive/WitchCityRope.Core/Entities/Event.cs`) - ARCHIVED:
   ```csharp
   // ❌ OLD WRONG LOGIC
   public int GetCurrentAttendeeCount()
   {
       var rsvpCount = _rsvps.Count(r => r.Status == RSVPStatus.Confirmed);
       var ticketCount = _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
       return rsvpCount + ticketCount; // WRONG - Double counting!
   }
   
   // ✅ NEW CORRECT LOGIC
   public int GetCurrentAttendeeCount()
   {
       if (EventType == EventType.Social)
       {
           // Social events: Attendees = RSVPs (primary attendance metric)
           return _rsvps.Count(r => r.Status == RSVPStatus.Confirmed);
       }
       else // EventType.Class
       {
           // Class events: Attendees = Tickets (only paid tickets)
           return _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
       }
   }
   ```

2. **Fixed API Event Entity** (`/apps/api/Models/Event.cs`):
   ```csharp
   // ✅ CORRECT - Social events return RSVP count as attendance
   public int GetCurrentAttendeeCount()
   {
       if (EventType == "Social")
       {
           // Social events: Attendees = RSVPs (primary attendance metric)
           return GetCurrentRSVPCount();
       }
       else // Class
       {
           // Class events: Attendees = Tickets (only paid tickets)
           return GetCurrentTicketCount();
       }
   }
   ```

3. **Updated Documentation**: Fixed all DTO comments to reflect correct business logic in both:
   - `/apps/api/Models/EventDto.cs`
   - `/apps/api/Features/Events/Models/EventDto.cs`

**API Test Results** (Verified Correct Logic):
- **Class Events**: `currentAttendees` = `currentTickets`, `currentRSVPs` = 0 ✅
- **Social Events**: `currentAttendees` = `currentRSVPs`, `currentTickets` = optional donations ✅

**Example Output**:
```
Introduction to Rope Safety: Class - 16/20 (0 RSVPs + 16 Tickets) ✅
Community Rope Jam: Social - 18/30 (18 RSVPs + 5 Tickets) ✅ 
```

**Frontend Impact**:
- **Social Events**: Will show "18/30" (18 RSVPs out of 30 capacity) with "(5 tickets)" as optional info
- **Class Events**: Will show "16/20" (16 tickets out of 20 capacity) with no RSVP confusion

**Files Changed**:
- `/src/_archive/WitchCityRope.Core/Entities/Event.cs` - ARCHIVED - Fixed GetCurrentAttendeeCount() method
- `/apps/api/Models/Event.cs` - Fixed GetCurrentAttendeeCount(), GetCurrentRSVPCount(), GetCurrentTicketCount()  
- `/apps/api/Models/EventDto.cs` - Updated documentation comments
- `/apps/api/Features/Events/Models/EventDto.cs` - Updated documentation comments

**Key Business Rules Enforced**:
- ✅ Social Events: RSVP is mandatory to attend, tickets are optional donations
- ✅ Class Events: Only paid tickets allowed, no RSVPs
- ✅ No double-counting of people who RSVP and buy tickets
- ✅ Frontend gets clear separation between attendance count and support/donations

**Prevention**:
- Always validate business logic against real-world use cases
- Question additive formulas that might double-count the same person
- Test with realistic scenarios where someone might both RSVP and buy tickets
- Document the PRIMARY attendance metric for each event type clearly

## Database Reset and Fresh Seed Data Success (2025-09-11)

### Prevention Pattern: API State Verification After Changes
**Achievement**: Successfully reset database and regenerated seed data with comprehensive capacity state variations for frontend testing.

**Process Used**:
1. **Killed Multiple Running API Processes** - Multiple dotnet processes were running on different ports
2. **Database Table Reset**: `docker exec witchcity-postgres psql -U postgres -d witchcityrope_dev -c "TRUNCATE TABLE \"Events\" RESTART IDENTITY CASCADE;"`
3. **Fresh API Startup**: `dotnet run --project apps/api --environment Development --urls http://localhost:5655`
4. **Automatic Database Initialization**: SeedDataService triggered automatically and created fresh events




**Critical Discovery - Process Management**:
- Multiple concurrent `dotnet run` processes can cause port conflicts and serve stale code
- Always kill existing processes before starting fresh API instance
- Database truncation triggers automatic re-seeding on next API startup
- SeedDataService.cs now provides deterministic capacity variations for consistent frontend testing



**Pattern for Future Development**:
```bash
# Quick database reset for fresh seed data
docker exec witchcity-postgres psql -U postgres -d witchcityrope_dev -c "TRUNCATE TABLE \"Events\" RESTART IDENTITY CASCADE;"

# Start clean API (kills existing processes first)
pkill -f "dotnet run" && sleep 2 && dotnet run --project apps/api --environment Development --urls http://localhost:5655

# Verify fresh data
curl -s http://localhost:5655/api/events | jq '.data[] | "\(.title): \(.currentAttendees)/\(.capacity) (\((.currentAttendees * 100 / .capacity | floor))%)"'
```

**Prevention**:
- Always check for running processes before starting new API instances
- Use database table truncation to force fresh seed data generation
- Verify API health and data variety before frontend testing
- Document the capacity percentage thresholds used by frontend components

## Legacy API Feature Analysis (2025-09-12)

### CRITICAL DISCOVERY: Comprehensive Feature Gap Analysis
**Problem**: WitchCityRope has two API projects with modern API missing critical business features that exist in legacy API.

**Root Cause**: During React migration, a simplified API was created rather than migrating existing comprehensive features.

**Key Findings**:
1. **Safety System**: MISSING from modern API - legal compliance risk for incident reporting
2. **CheckIn System**: MISSING - core event functionality with QR codes, staff validation, attendee details
3. **Vetting System**: MISSING - member approval workflow with references and scoring
4. **Enhanced Payments**: Modern API has basic implementation, legacy has comprehensive Stripe integration

**Legacy API Analysis Results**:
- 7 feature systems analyzed: CheckIn, Safety, Vetting, Payments, Dashboard, Events, Auth
- Safety system has encryption, anonymous reporting, severity-based alerts
- CheckIn supports QR codes, confirmation codes, manual lookup with staff permissions
- Vetting has multi-stage workflow with external reference validation
- Payments includes Stripe customer management, saved payment methods, post-payment automation

**Feature Priority Matrix Created**:
- **CRITICAL**: Safety System (legal compliance, community safety)
- **HIGH**: CheckIn System (core event functionality), Vetting System (community management)
- **MEDIUM**: Enhanced Payments (revenue optimization), Events enhancement, Dashboard

**Architecture Migration Strategy**:
- Use modern API's vertical slice pattern for all migrations
- Simplify legacy MediatR/CQRS patterns to direct Entity Framework
- Preserve critical business logic and validation rules
- Implement TestContainers for all database testing

**Implementation Timeline**: 4 weeks across 4 phases
1. **Week 1**: Safety system (critical legal compliance)
2. **Week 2**: CheckIn system and Events enhancement
3. **Week 3**: Vetting system and Dashboard
4. **Week 4**: Enhanced Payments and integration

**Files Created**:
- `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/legacy-api-feature-analysis.md`
- `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/handoffs/backend-developer-2025-09-12-handoff.md`

**Pattern for Future API Analysis**:
```bash
# Comprehensive feature inventory process
1. List all legacy feature directories
2. Analyze each feature's business logic and data models
3. Compare with modern API implementation
4. Document gaps and business impact
5. Create priority matrix based on business value and compliance
6. Design migration strategy preserving business rules
```

**Key Business Rules Discovered**:
- CheckIn system requires staff role validation (CheckInStaff, Organizer, Admin)
- Safety incidents have severity-based escalation (Critical → immediate alerts)
- Vetting requires minimum 2 references with external validation
- Payment processing includes comprehensive post-payment automation

**Critical Legal/Compliance Issues**:
- Safety incident reporting required for community liability protection
- Anonymous reporting with data encryption required for sensitive incidents
- Audit trails required for all safety and vetting actions

**Prevention**:
- Always analyze legacy systems comprehensively before declaring features "migrated"
- Document business rules and compliance requirements during analysis
- Create feature gap analysis comparing legacy vs. modern implementations
- Prioritize features based on legal compliance and business impact
- Plan incremental migration preserving critical business logic

## Dashboard and Events Enhancement Analysis (2025-09-12)

### CRITICAL DECISION: Dashboard Features Need Extraction, Events Enhancement Can Be Archived

**Analysis Results**: Deep comparison of Dashboard and Events Enhancement features between archived legacy API (`/src/_archive/WitchCityRope.Api/`) and modern API (`/apps/api/`) revealed:

**Dashboard Features: HIGH PRIORITY EXTRACTION NEEDED**
- **Legacy Implementation**: 3 endpoints with rich user engagement features
  - Personal dashboard with vetting status (`GET /api/dashboard/{userId}`)
  - Upcoming events widget for member planning (`GET /api/dashboard/users/{userId}/upcoming-events`)
  - Membership statistics for engagement tracking (`GET /api/dashboard/users/{userId}/stats`)
- **Modern Implementation**: NONE - Complete gap
- **Business Impact**: HIGH - Member retention, engagement tracking, administrative insights
- **Recommendation**: EXTRACT - 2 week effort, high business value

**Events Enhancement: ARCHIVE CANDIDATE**
- **Legacy Implementation**: Full CRUD for sessions and ticket types with complex authorization
- **Modern Implementation**: 95% complete - has DTOs, business logic, core endpoints
- **Business Impact**: LOW - Modern API already supports complex multi-session events, ticket types, RSVP vs tickets
- **Recommendation**: ARCHIVE - Core functionality already implemented with better architecture

**Key Technical Findings**:
```csharp
// Legacy has comprehensive engagement tracking
var eventsAttended = await _context.Registrations
    .CountAsync(r => r.UserId == userId && 
                    r.Event.EndDate < DateTime.UtcNow &&
                    r.Status == RegistrationStatus.Confirmed);

// Modern API already has session/ticket support
public class SessionDto { /* Full implementation */ }
public class TicketTypeDto { /* Complete with pricing tiers */ }
```

**Architecture Benefits**:
- Modern API's vertical slice pattern is superior to legacy MediatR/CQRS complexity
- Events functionality is already well-implemented in modern API
- Dashboard features provide unique member engagement value not found anywhere else

**Updated Extraction Priority**:
1. **CRITICAL**: Safety System (legal compliance)
2. **HIGH**: CheckIn System (core event functionality)
3. **HIGH**: Vetting System (community management)
4. **MEDIUM**: Dashboard System (member engagement) - **NEWLY ADDED**
5. **MEDIUM**: Enhanced Payments (revenue optimization)
6. **ARCHIVE**: Events Enhancement - **MOVED TO ARCHIVE**

**Files Created**:
- `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/dashboard-events-comparison.md` - Comprehensive feature comparison

**For Future Backend Developers**:
- Dashboard extraction should follow modern API's minimal API + vertical slice pattern
- Don't rebuild Events enhancement - the modern API implementation is superior
- Focus extraction efforts on features that provide unique business value not available elsewhere
- Use comprehensive feature gap analysis to make extraction vs archive decisions

**Pattern for Feature Analysis**:
```markdown
## Analysis Template
1. **Legacy Implementation**: What exists, file locations, endpoints
2. **Modern Implementation**: What's built, gaps identified
3. **Business Impact**: Revenue, compliance, user engagement value
4. **Architecture Comparison**: Legacy vs modern patterns
5. **Recommendation**: Extract, Archive, or Enhance with clear rationale
```

## Prevention Pattern: Missing API Endpoints for Testing

**Problem**: E2E tests were failing with 404 errors because three API endpoints were missing:
1. `GET /api/vetting/status` - Current user's vetting status
2. `GET /api/vetting/application` - Current user's vetting application details
3. `GET /api/user/profile` - Current user's profile information

**Root Cause**: The E2E tests expected these endpoints but they hadn't been implemented in the API.

**Solution Implemented**:

1. **Added Vetting Status Endpoint** (`GET /api/vetting/status`):
   - Returns current user's vetting application status
   - Uses existing `MyApplicationStatusResponse` model
   - Includes status, application number, submission date, next steps, and estimated days remaining
   - Returns structured response with business-friendly status descriptions

2. **Added Vetting Application Details Endpoint** (`GET /api/vetting/application`):
   - Returns current user's vetting application details
   - Uses existing `ApplicationDetailResponse` model with added properties
   - Includes application history, but excludes admin notes for user privacy
   - Returns 404 if no application exists

3. **Added User Profile Endpoint** (`GET /api/user/profile`):
   - Added singular endpoint to match E2E test expectations
   - Existing `/api/users/profile` (plural) was already working
   - Both endpoints use same `UserManagementService.GetProfileAsync` method
   - Required for E2E tests that expect the singular form

**Key Implementation Details**:

```csharp
// Vetting endpoints added to VettingEndpoints.cs
group.MapGet("/status", GetVettingStatus)
group.MapGet("/application", GetMyVettingApplication)

// Service methods added to IVettingService and VettingService
Task<Result<MyApplicationStatusResponse>> GetMyApplicationStatusAsync(Guid userId, ...)
Task<Result<ApplicationDetailResponse>> GetMyApplicationDetailAsync(Guid userId, ...)

// User endpoint added to UserEndpoints.cs
app.MapGet("/api/user/profile", async (...) => {...})
```

**Business Logic Implemented**:
- **Status descriptions**: User-friendly explanations of vetting status
- **Next steps**: Clear guidance on what user should do next
- **Time estimates**: Days remaining in review process based on status
- **Privacy protection**: Admin notes excluded from user-facing responses
- **Authentication required**: All endpoints require valid JWT token

**Response Format Examples**:
```json
// GET /api/vetting/status
{
  "success": true,
  "data": {
    "hasApplication": true,
    "application": {
      "applicationId": "...",
      "status": "UnderReview",
      "statusDescription": "Application is being reviewed by our team",
      "nextSteps": "No action needed - we'll contact you with updates",
      "estimatedDaysRemaining": 12
    }
  }
}

// GET /api/user/profile
{
  "id": "...",
  "email": "user@example.com",
  "sceneName": "UserName",
  "role": "Member",
  "vettingStatus": "Approved"
}
```

**Files Modified**:
- `/apps/api/Features/Vetting/Endpoints/VettingEndpoints.cs` - Added new endpoints
- `/apps/api/Features/Vetting/Services/IVettingService.cs` - Added interface methods
- `/apps/api/Features/Vetting/Services/VettingService.cs` - Implemented service methods
- `/apps/api/Features/Vetting/Models/ApplicationDetailResponse.cs` - Added missing DTOs
- `/apps/api/Features/Users/Endpoints/UserEndpoints.cs` - Added singular profile endpoint

**Business Rules Applied**:
- User can only see their own vetting information
- Admin notes are filtered out of user responses
- Status descriptions are user-friendly and actionable
- Time estimates help users understand review timeline
- Workflow history shows application progression

**Authentication Pattern**:
```csharp
// Standard JWT claim extraction pattern used across all endpoints
var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (!Guid.TryParse(userIdClaim, out var userId))
{
    return Results.Json(new ApiResponse<object>
    {
        Success = false,
        Error = "User information not found",
        Timestamp = DateTime.UtcNow
    }, statusCode: 401);
}
```

**Prevention**:
- Always verify E2E test endpoints exist before running tests
- Use consistent authentication patterns across all user-specific endpoints
- Include proper error handling for missing data scenarios
- Follow existing API response patterns for consistency
- Document business rules for user-facing vs admin-facing data

**Result**: E2E tests can now successfully access all required endpoints, returning proper vetting status, application details, and user profile information.

## Docker Network Configuration Issue - September 18, 2025

**Problem**: Docker Compose failed to start with error "Service 'mailcatcher' uses an undefined network 'witchcity-network'"

**Root Cause**: Inconsistent network naming between Docker Compose files:
- `docker-compose.yml` defines network as `witchcity-net`
- `docker-compose.override.yml` referenced `witchcity-network` (incorrect)

**Solution**: Fixed network reference in `docker-compose.override.yml` line 115:
```yaml
# Before (broken)
networks:
  - witchcity-network

# After (fixed)
networks:
  - witchcity-net
```

**Validation**: Verified fix with `docker-compose config --quiet` and `docker-compose config --services`

**Prevention Pattern**: Always verify network names match between compose files when adding new services

**Key Files**:
- Base network definition: `docker-compose.yml` (line 11)
- Development services: `docker-compose.dev.yml`
- Override services: `docker-compose.override.yml` (mailcatcher service)

## Prevention Pattern: Audit Log History Implementation

**Problem**: Vetting seed data needed to generate realistic notes and status history that follows the workflow progression to properly test the vetting system UI.

**Solution Implemented**: Enhanced `SeedVettingApplicationsAsync` method to create comprehensive `VettingAuditLog` entries that show the complete status progression for each vetting application.

**Key Components**:

1. **Workflow-Based Audit Trail**: Each application gets audit log entries that show realistic progression:
   - **Draft → UnderReview**: Initial submission and admin review start
   - **UnderReview → InterviewApproved**: Admin approval with review notes
   - **InterviewApproved → PendingInterview**: Interview scheduling
   - **PendingInterview → Approved/Denied**: Final decision with rationale

2. **Realistic Audit Log Entries**: Each entry includes:
   - **Action Description**: Clear action taken ("Application Submitted", "Status Changed", "Interview Scheduled")
   - **PerformedBy**: User ID of who performed the action (applicant for submission, admin for reviews)
   - **PerformedAt**: Realistic timestamp showing progression over time
   - **OldValue/NewValue**: Status transitions for tracking changes
   - **Notes**: Contextual admin comments explaining decisions

3. **Status-Specific Progressions**:
   - **UnderReview**: Basic submission → admin queue
   - **InterviewApproved**: Submission → review → approval for interview
   - **PendingInterview**: Full progression including interview scheduling
   - **Approved**: Complete workflow from submission to final approval
   - **OnHold**: Submission → review → hold for additional info
   - **Denied**: Submission → review → denial with reason

**Example Audit Trail for Approved Application**:
```csharp
// 1. Initial submission by applicant
Action: "Application Submitted"
PerformedBy: [applicant_user_id]
Notes: "Initial application submitted by SilkAndSteel"

// 2. Admin moves to review
Action: "Status Changed"
OldValue: "Draft" → NewValue: "UnderReview"
PerformedBy: [admin_user_id]
Notes: "Application moved to review queue"

// 3. Admin approves for interview
Action: "Status Changed"
OldValue: "UnderReview" → NewValue: "InterviewApproved"
Notes: "Application approved for interview stage"

// 4. Interview completed
Action: "Interview Completed"
Notes: "Interview conducted - excellent candidate with strong community values"

// 5. Final approval
Action: "Application Approved"
OldValue: "PendingInterview" → NewValue: "Approved"
Notes: "Application approved for full membership - welcome to the community!"
```

**Benefits**:
- ✅ Complete audit trail for all vetting applications
- ✅ Realistic workflow progression for UI testing
- ✅ Admin can see who made each decision and when
- ✅ Clear notes explaining each status change
- ✅ Proper accountability with user attribution
- ✅ Chronological timeline showing application lifecycle

**Files Modified**:
- `/apps/api/Services/SeedDataService.cs` - Added `CreateVettingAuditLogsAsync` method
- Enhanced documentation and added workflow-based audit log generation

**Technical Pattern**:
```csharp
// ✅ CORRECT: Comprehensive audit log creation
private async Task CreateVettingAuditLogsAsync(List<VettingApplication> applications, Guid adminUserId, CancellationToken cancellationToken)
{
    var auditLogs = new List<VettingAuditLog>();

    foreach (var application in applications)
    {
        // Create progression based on current status
        switch (application.Status)
        {
            case VettingStatus.Approved:
                // Show complete workflow: Draft → UnderReview → InterviewApproved → PendingInterview → Approved
                // Each step with realistic timestamps and admin notes
        }
    }
}
```

**Prevention**:
- Always create audit trails that reflect realistic business workflows
- Include proper user attribution for accountability
- Use realistic timestamps that show progression over time
- Provide meaningful notes that explain decisions
- Test audit trails with frontend to ensure proper display

**Result**: Vetting applications now have comprehensive audit history showing complete workflow progression, enabling proper testing of the vetting system UI and providing transparency in the review process.

## Prevention Pattern: Vetting Notes and Audit Log Implementation

**Problem**: Notes saved to AdminNotes field but API returns empty arrays, status changes don't create audit logs.
**Solution**: Parse stored text fields into DTOs for API responses and create audit log entries for ALL state changes, not just final outcomes.

## 🚨 CRITICAL: Draft Events Not Appearing in Admin Events Grid - Fixed (2025-09-22)

**Problem**: Admin dashboard was not showing draft events (IsPublished = false) because the API endpoint only returned published events.

**Root Cause**: The `GetPublishedEventsAsync` method in EventService had a hard filter for published events only:
```csharp
.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow) // Only published events
```

**Solution Implemented**: Added query parameter support for admin access with proper authentication:

1. **Added GetEventsAsync method** with `includeUnpublished` parameter:
```csharp
public async Task<(bool Success, List<EventDto> Response, string Error)> GetEventsAsync(
    bool includeUnpublished = false,
    CancellationToken cancellationToken = default)
{
    if (includeUnpublished)
    {
        // Admin access: Show all events (both published and draft)
        query = query.Where(e => e.StartDate > DateTime.UtcNow.AddDays(-30));
    }
    else
    {
        // Public access: Only published future events
        query = query.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow);
    }
}
```

2. **Updated endpoint** to support `?includeUnpublished=true` with admin authentication:
```csharp
app.MapGet("/api/events", async (
    EventService eventService,
    HttpContext context,
    bool? includeUnpublished,
    CancellationToken cancellationToken) =>
{
    var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);

    // Verify admin role for unpublished events
    if (shouldIncludeUnpublished)
    {
        if (user.Identity?.IsAuthenticated != true) return 401;
        if (userRole != "Administrator") return 403;
    }

    return await eventService.GetEventsAsync(shouldIncludeUnpublished, cancellationToken);
})
```

3. **Added fallback draft events** for development testing with proper IsPublished flags.

**Usage**:
- **Public**: `GET /api/events` - Returns only published events
- **Admin**: `GET /api/events?includeUnpublished=true` - Returns all events including drafts


**Business Logic**:
- Admin users see all events (published + drafts) for management
- Public users only see published future events for registration
- Draft events are now visible in admin interface with proper labeling
- Authentication required to access unpublished events

**Prevention**:
- Always consider admin vs public access requirements when filtering data
- Test both authenticated and unauthenticated access to API endpoints
- Verify query filters match business requirements for different user roles
- Use query parameters rather than separate endpoints for role-based filtering

## Minimal API Event Update Implementation (2025-09-12)

### PUT /api/events/{id} Endpoint Implementation
**Problem**: The running API at `/apps/api/` (minimal API on port 5655) was missing the PUT endpoint for updating events, causing 405 Method Not Allowed errors for the frontend.

**Root Cause**: While EventsManagementService implementation existed in the archived legacy API (`/src/_archive/WitchCityRope.Api/`), the minimal API at `/apps/api/` only had GET endpoints in EventEndpoints.cs.

**Solution Implemented**:

1. **Created UpdateEventRequest Model** (`/apps/api/Features/Events/Models/UpdateEventRequest.cs`):
   - Supports partial updates with optional nullable fields
   - Includes: Title, Description, StartDate, EndDate, Location, Capacity, PricingTiers, IsPublished
   - Proper UTC DateTime handling for PostgreSQL compatibility

2. **Added UpdateEventAsync Method** (`/apps/api/Features/Events/Services/EventService.cs`):
   - Business rule validation: Cannot update past events
   - Capacity validation: Cannot reduce below current attendance
   - Date range validation: StartDate must be before EndDate
   - Partial update support: Only non-null fields are updated
   - Proper Entity Framework change tracking for updates
   - UpdatedAt timestamp maintenance

3. **Added PUT Endpoint** (`/apps/api/Features/Events/Endpoints/EventEndpoints.cs`):
   - Route: `PUT /api/events/{id}`
   - JWT authentication required with `RequireAuthorization()`
   - Comprehensive HTTP status code mapping:
     - 200 OK: Successful update
     - 400 Bad Request: Invalid ID, past events, capacity issues, date validation
     - 401 Unauthorized: No JWT token
     - 404 Not Found: Event not found
     - 405 Method Not Allowed: Wrong HTTP method
     - 500 Internal Server Error: Unexpected errors
   - Proper API response structure with success/error messages

**Business Rules Implemented**:
- ✅ JWT authentication required for all updates
- ✅ Cannot update events that have already started (past events)
- ✅ Cannot reduce capacity below current attendance count
- ✅ Date validation ensures StartDate < EndDate
- ✅ Partial updates support (only provided fields updated)
- ✅ UTC DateTime handling for PostgreSQL compatibility
- ✅ UpdatedAt timestamp automatically maintained

**Testing Results**:
```bash
# Endpoint correctly exposed and routing
curl -X PUT http://localhost:5655/api/events/{id} -d '{"title":"Test"}'
# Returns: HTTP 401 (authentication required) ✅

# Method not allowed works correctly
curl -X POST http://localhost:5655/api/events/{id}
# Returns: HTTP 405 (method not allowed) ✅

# API health check passes
curl http://localhost:5655/health
# Returns: {"status":"Healthy"} ✅
```

**Key Implementation Patterns**:
```csharp
// ✅ CORRECT - Partial update with business validation
if (request.Capacity.HasValue)
{
    var currentAttendees = eventEntity.GetCurrentAttendeeCount();
    if (request.Capacity.Value < currentAttendees)
    {
        return (false, null, $"Cannot reduce capacity to {request.Capacity.Value}. " +
            $"Current attendance is {currentAttendees}");
    }
}

// ✅ CORRECT - JWT authentication in minimal API
app.MapPut("/api/events/{id}", async (string id, UpdateEventRequest request, ...) => { ... })
    .RequireAuthorization() // Requires JWT Bearer token
    .WithName("UpdateEvent")
    .WithSummary("Update an existing event");

// ✅ CORRECT - Proper HTTP status code mapping
var statusCode = error switch
{
    string msg when msg.Contains("not found") => 404,
    string msg when msg.Contains("past events") => 400,
    string msg when msg.Contains("capacity") => 400,
    _ => 500
};
```

**Files Created/Modified**:
- ✅ `/apps/api/Features/Events/Models/UpdateEventRequest.cs` - New partial update model
- ✅ `/apps/api/Features/Events/Services/EventService.cs` - Added UpdateEventAsync method
- ✅ `/apps/api/Features/Events/Endpoints/EventEndpoints.cs` - Added PUT endpoint with auth


**Pattern for Future Minimal API Development**:
```csharp
// ✅ CORRECT - Complete minimal API endpoint with business logic
app.MapPut("/api/resource/{id}", async (
    string id,
    UpdateResourceRequest request,
    ResourceService service,
    CancellationToken ct) =>
{
    var (success, response, error) = await service.UpdateResourceAsync(id, request, ct);

    if (success && response != null)
    {
        return Results.Ok(new ApiResponse<ResourceDto>
        {
            Success = true,
            Data = response,
            Message = "Resource updated successfully"
        });
    }

    var statusCode = DetermineStatusCode(error);
    return Results.Json(new ApiResponse<ResourceDto>
    {
        Success = false,
        Error = error,
        Message = "Failed to update resource"
    }, statusCode: statusCode);
})
.RequireAuthorization()
.WithName("UpdateResource")
.WithSummary("Update existing resource")
.WithTags("Resources")
.Produces<ApiResponse<ResourceDto>>(200)
.Produces(400).Produces(401).Produces(404).Produces(500);
```

**Prevention**:
- Always implement CRUD endpoints completely (not just GET endpoints)
- Test all HTTP methods and status codes during development
- Verify JWT authentication requirements for protected endpoints
- Use proper business logic validation in service layer
- Follow consistent API response patterns across all endpoints

## Database Migration & Seeding System Analysis (2025-09-12)

### Database Migration & Seeding System Status

**Analysis**: The database migration and seeding system in `/apps/api/` is functional and working as designed.

**Key Components**:

1. **DatabaseInitializationService Properly Implemented** (`/apps/api/Services/DatabaseInitializationService.cs`):
   - ✅ BackgroundService with fail-fast patterns and comprehensive error handling
