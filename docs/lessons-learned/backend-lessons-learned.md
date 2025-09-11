# Backend Developer Lessons Learned

This document tracks critical lessons learned during backend development to prevent recurring issues and speed up future development.

## 🚨 CRITICAL: Testing Requirements for Backend Developers

**MANDATORY BEFORE ANY TESTING**: Even for quick test runs, you MUST:

1. **Read testing documentation FIRST**:
   - `/docs/standards-processes/testing-prerequisites.md` - MANDATORY pre-flight checks
   - `/docs/standards-processes/testing/TESTING.md` - Testing procedures
   - `/docs/lessons-learned/test-executor-lessons-learned.md` - Common issues

2. **Run health checks BEFORE any tests**:
   ```bash
   dotnet test tests/WitchCityRope.Core.Tests --filter "Category=HealthCheck"
   ```

3. **Why this matters**: Port misconfigurations are the #1 cause of false test failures. Running tests without health checks wastes hours debugging non-existent issues.

**Never skip health checks** - they take < 1 second and prevent hours of confusion.

---

## Entity Framework Core & PostgreSQL Issues

### PostgreSQL Check Constraint Case Sensitivity (2025-08-25)
**Problem**: EF Core migrations failed with "column does not exist" errors when applying check constraints to PostgreSQL.
**Root Cause**: PostgreSQL requires column names in check constraints to be quoted when using PascalCase naming.
**Solution**: 
- Use quoted column names in check constraints: `"ColumnName"` instead of `ColumnName`
- Example: `builder.HasCheckConstraint("CK_Table_Column", "\"ColumnName\" > 0");`

**Files Changed**:
- `EventSessionConfiguration.cs`
- `EventTicketTypeConfiguration.cs`

### Migration History Corruption
**Problem**: Database had phantom migration entries that didn't exist in code, causing migration failures.
**Root Cause**: Migration history table can become out of sync with actual migration files.
**Solution**:
1. Manually remove phantom entries from `__EFMigrationsHistory` table
2. Drop and recreate database for clean state
3. Apply all migrations from scratch
4. Created `database-reset.sh` script for future use

### DesignTimeDbContextFactory Configuration
**Problem**: EF CLI couldn't find correct connection strings when run from Infrastructure project.
**Root Cause**: Factory was looking in wrong directory for appsettings files.
**Solution**: Updated factory to look for API project's configuration files with fallback.

## Database Seeding

### Robust Database Initialization
**Current Implementation**: DbInitializer correctly:
- Applies pending migrations
- Seeds users with authentication records
- Seeds events with proper relationships  
- Seeds vetting applications
- Handles existing data gracefully

**Pattern**: Always check for existing data before seeding to prevent conflicts.

## Tools and Scripts

### Database Reset Workflow
**Script Created**: `database-reset.sh` provides:
- Automated database drop/recreation
- Migration application
- Ready for API startup with seeding

**Usage**: `./database-reset.sh` from project root

## Prevention Strategies

1. **Always test migrations** on clean database before committing
2. **Use proper PostgreSQL column naming** in constraints (quoted)
3. **Keep DesignTimeDbContextFactory updated** when changing configuration structure
4. **Document database reset procedures** for team members
5. **Test full end-to-end flow** after migration changes

## Critical Business Requirements Analysis (2025-09-07)

### CRITICAL DISCOVERY: Business Requirements vs Implementation Mismatch

**Problem**: Major discrepancy discovered between stated business requirements and actual implementation.

**Business Requirements State**:
- **Classes** (EventType.Workshop/Class): Require ticket purchase (paid only)
- **Social Events** (EventType.Social): Have RSVP (free) PLUS optional ticket purchases

**Current Implementation Issues**:
1. ✅ Event entity DOES have EventType field to distinguish Classes from Social Events
2. ❌ Registration entity assumes ALL registrations require payment (see Registration.Confirm() method)
3. ❌ NO RSVP entity or concept in the Core domain
4. ✅ Old Blazor code had "Free with RSVP" for events with Price = 0
5. ❌ Current API treats Price = 0 as "free" but still requires Registration with Payment confirmation

**Key Findings**:
- The domain model CAN distinguish event types (Classes vs Social Events)
- The Registration.Confirm() method ALWAYS requires a Payment object (line 128-132)
- For Price = 0 events, the system still creates Registration entities instead of RSVP entities
- The old Blazor logic: `Price = 0` → "Free with RSVP", `Price > 0` → Payment required

**Required Changes**:
1. Create RSVP entity for free Social Events
2. Modify Registration.Confirm() to handle free events without Payment requirement
3. Add business logic to distinguish: Classes → Registration+Payment, Social Events → RSVP or Registration+Payment
4. Update EventService to route appropriately based on EventType and pricing

**Impact**: This affects the entire registration/RSVP flow and may require significant domain model changes.

## RSVP Implementation Success (2025-09-07)

### RSVP Entity and Business Logic Implementation
**Implementation**: Successfully created complete RSVP system alongside existing Registration system

**Key Components Created**:
1. **RSVP Entity** (`/src/WitchCityRope.Core/Entities/RSVP.cs`)
   - Separate entity for free social event RSVPs
   - Business rules enforced: Only Social events allow RSVP
   - Capacity validation with combined RSVP + Registration count
   - Cancellation support with reasons and timestamps
   - Link to optional ticket purchase (upgrade path)

2. **RSVPStatus Enum** (`/src/WitchCityRope.Core/Enums/RSVPStatus.cs`)
   - Confirmed, Cancelled, CheckedIn states
   - Simple enum compared to Registration complexity

3. **Updated Event Entity** (`/src/WitchCityRope.Core/Entities/Event.cs`)
   - Added RSVPs navigation property
   - Business rule properties: `AllowsRSVP`, `RequiresPayment`
   - Updated capacity calculations: `GetCurrentAttendeeCount()` includes both RSVPs and Registrations
   - `GetAvailableSpots()` now considers total attendance

4. **EF Core Configuration** (`/src/WitchCityRope.Infrastructure/Data/Configurations/RSVPConfiguration.cs`)
   - Proper foreign key relationships
   - Unique constraint on active RSVPs per user/event
   - Performance indexes on Status, CreatedAt, ConfirmationCode

5. **DTOs and API Layer** (`/src/WitchCityRope.Api/Features/Events/DTOs/RSVPDto.cs`)
   - RSVPDto, RSVPRequest, AttendanceStatusDto
   - Clean separation between RSVP and Ticket information

6. **Service Layer** (`/src/WitchCityRope.Api/Features/Events/Services/EventsManagementService.cs`)
   - CreateRSVPAsync, GetAttendanceStatusAsync, CancelRSVPAsync
   - Full business rule validation
   - Comprehensive error handling and logging

7. **API Endpoints** (`/src/WitchCityRope.Api/Features/Events/Endpoints/EventsManagementEndpoints.cs`)
   - POST `/api/events/{id}/rsvp` - Create RSVP
   - GET `/api/events/{id}/attendance` - Get attendance status
   - DELETE `/api/events/{id}/rsvp` - Cancel RSVP
   - Proper authentication and error handling

8. **Database Migration** (`Migrations/20250907163642_AddRSVPSupport.cs`)
   - RSVPs table with all constraints and indexes
   - Foreign keys to Users, Events, and optional Registration

**Business Rules Successfully Implemented**:
- ✅ Social Events allow BOTH free RSVP AND optional ticket purchase
- ✅ Classes/Workshops ONLY allow ticket purchase (no RSVP)
- ✅ Users with RSVP can still purchase tickets later
- ✅ Capacity tracking includes both RSVPs and paid registrations
- ✅ Unique RSVP per user per event (non-cancelled)
- ✅ Authentication required for all RSVP operations

**Architecture Decisions**:
- **Separate Entity Approach**: Created dedicated RSVP entity rather than modifying Registration
- **Business Rule Enforcement**: Domain model enforces event type restrictions
- **Capacity Management**: Combined counting prevents overbooking across both systems
- **Upgrade Path**: RSVPs can be linked to later ticket purchases

**Key Patterns Used**:
- Result tuple pattern for service methods
- Domain-driven design with business rule validation
- Proper EF Core entity configuration with constraints
- Authentication via ClaimsPrincipal in minimal API endpoints
- Structured logging with contextual information

## Database Service Registration Issues (2025-09-10)

### CRITICAL FIX: Missing Service Registrations
**Problem**: DatabaseInitializationService and SeedDataService were created but never registered in the DI container, preventing automatic database initialization.

**Root Cause**: Services were implemented correctly but not added to `ServiceCollectionExtensions.cs`.

**Solution**: Added proper service registrations:
```csharp
// Database initialization services
services.AddScoped<ISeedDataService, SeedDataService>();
services.AddHostedService<DatabaseInitializationService>();
```

**Key Points**:
- DatabaseInitializationService inherits from BackgroundService, requires `AddHostedService`
- SeedDataService implements ISeedDataService, requires `AddScoped` registration
- Added proper using statement for `WitchCityRope.Api.Services` namespace
- Services were already being called via `builder.Services.AddFeatureServices()` in Program.cs

**Files Changed**:
- `/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`

**Verification**:
- API health endpoint shows 5 users (database is seeded)
- Application starts without DI errors
- Services are now properly registered and available

**Prevention**: Always verify that new services are properly registered in DI container after creation.

## Events Management API CRUD Implementation (2025-09-11)

### UPDATE and DELETE Endpoints Successfully Implemented
**Implemented**: Missing PUT and DELETE endpoints for Events Management API

**Key Components Added**:
1. **PUT /api/events/{id} Endpoint** (`EventsManagementEndpoints.cs`)
   - Updates existing events with business rule validation
   - Only organizers and administrators can update events
   - Cannot update past events or reduce capacity below current attendance
   - Handles nullable properties from existing `UpdateEventRequest` model

2. **DELETE /api/events/{id} Endpoint** (`EventsManagementEndpoints.cs`)
   - Deletes events with comprehensive safety checks
   - Cannot delete events with active registrations or RSVPs
   - Automatically unpublishes events before deletion
   - Cascades deletion to related sessions and ticket types

3. **Service Layer Methods** (`EventsManagementService.cs`)
   - `UpdateEventAsync()`: Full event update with validation
   - `DeleteEventAsync()`: Safe event deletion with business rules
   - Integrated with existing Event Session Matrix system
   - Follows tuple return pattern from lessons learned

**Business Rules Implemented**:
- ✅ Only event organizers or administrators can modify events
- ✅ Cannot update or delete past events
- ✅ Cannot reduce capacity below current attendance
- ✅ Cannot delete events with active registrations or RSVPs
- ✅ Events are unpublished before deletion for safety
- ✅ Supports partial updates (only non-null properties updated)

**Technical Decisions**:
- **Reused Existing DTOs**: Used existing `UpdateEventRequest` from Models namespace instead of creating duplicate
- **Nullable Property Handling**: Implemented proper null-checking for partial updates
- **Business Rule Enforcement**: Domain model enforces event modification restrictions
- **Authorization**: JWT-based authentication required for both endpoints
- **Error Handling**: Specific HTTP status codes for different failure scenarios

**Integration Points**:
- ✅ Works with Event Session Matrix (sessions and ticket types)
- ✅ Respects RSVP and Registration systems for capacity validation
- ✅ Integrates with existing authentication middleware
- ✅ Follows existing API patterns and conventions

**Files Changed**:
- `/src/WitchCityRope.Api/Features/Events/Endpoints/EventsManagementEndpoints.cs`
- `/src/WitchCityRope.Api/Features/Events/Services/EventsManagementService.cs`
- `/src/WitchCityRope.Core/Entities/Registration.cs` (fixed missing method issue)

**Frontend Integration**:
- ✅ Endpoints match expected routes for `useUpdateEvent` and `useDeleteEvent` mutations
- ✅ HTTP methods (PUT/DELETE) align with REST conventions
- ✅ Response formats compatible with existing frontend expectations

**Limitations**:
- ⚠️ EventType updates not supported (property has private setter - requires domain model enhancement)
- ⚠️ Complex session/ticket type updates should use dedicated endpoints for those resources

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Use business rule validation in service layer
var currentAttendance = eventEntity.GetCurrentAttendeeCount();
if (request.Capacity.HasValue && request.Capacity.Value < currentAttendance)
{
    return (false, null, $"Cannot reduce capacity to {request.Capacity}. Current attendance is {currentAttendance}");
}

// ✅ CORRECT - Handle nullable properties for partial updates
if (request.StartDate.HasValue || request.EndDate.HasValue)
{
    var startDate = request.StartDate?.ToUniversalTime() ?? eventEntity.StartDate;
    var endDate = request.EndDate?.ToUniversalTime() ?? eventEntity.EndDate;
    eventEntity.UpdateDates(startDate, endDate);
}
```

## EventType Enum Simplification (2025-09-11)

### CRITICAL CHANGE: EventType Enum Reduced to Two Values Only
**Problem**: Admin dashboard filters required only "Class" and "Social" event types, but EventType enum had many values (Workshop, Class, Social, PlayParty, Performance, etc.)

**Solution Implemented**: 
1. **Updated Core EventType Enum** (`/src/WitchCityRope.Core/Enums/EventType.cs`):
   - Removed all values except `Class` and `Social`
   - Updated documentation to reflect business rules:
     - `Class`: Educational class or workshop - requires ticket purchase (paid)
     - `Social`: Social gathering - allows both RSVP (free) and optional ticket purchases

2. **Updated API Features EventType Enum** (`/src/WitchCityRope.Api/Features/Events/Models/Enums.cs`):
   - Aligned with Core enum to only have `Class` and `Social`

3. **Updated SeedDataService** (`/apps/api/Services/SeedDataService.cs`):
   - Removed local EventType enum definition
   - Added using statement for `WitchCityRope.Core.Enums`
   - Updated all sample events to use only `Class` or `Social` types
   - Changed `Workshop` → `Class`, `Meetup` → `Social`

4. **Updated Core Business Logic** (`/src/WitchCityRope.Core/Entities/Event.cs`):
   - Fixed `RequiresPayment` property to only check for `EventType.Class`
   - Removed references to deprecated `EventType.Workshop`

5. **Updated Infrastructure Mapping** (`/src/WitchCityRope.Infrastructure/Mapping/EventProfile.cs`):
   - Removed vetting requirements logic that referenced deprecated event types
   - Set `RequiresVetting` to always `false` since no event types require vetting now

6. **Updated Database Initializer** (`/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs`):
   - Changed all `EventType.Workshop` → `EventType.Class`
   - Changed `EventType.PlayParty` → `EventType.Social`
   - Changed `EventType.Virtual` → `EventType.Class`
   - Changed `EventType.Conference` → `EventType.Class`

7. **Added Project References** (`/apps/api/WitchCityRope.Api.csproj`):
   - Added missing project references to WitchCityRope.Core and WitchCityRope.Infrastructure
   - This was required for the SeedDataService to access the Core EventType enum

**Build Results**:
✅ Core project compiles with 0 errors  
✅ Infrastructure project compiles with 0 errors  
✅ API project compiles with 0 errors  
⚠️ Docker container needs rebuild to reflect project reference changes

**Business Impact**:
- Admin dashboard filters will now work with only "Class" and "Social" options
- All existing events will be categorized as either Class or Social
- Business rules simplified: Classes require payment, Social events allow both RSVP and payment

**Files Changed**:
- `/src/WitchCityRope.Core/Enums/EventType.cs`
- `/src/WitchCityRope.Api/Features/Events/Models/Enums.cs`
- `/apps/api/Services/SeedDataService.cs`
- `/src/WitchCityRope.Core/Entities/Event.cs`
- `/src/WitchCityRope.Infrastructure/Mapping/EventProfile.cs`
- `/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs`
- `/apps/api/WitchCityRope.Api.csproj`

**Pattern for Future Development**:
```csharp
// ✅ CORRECT - Use only Class or Social
public enum EventType
{
    Class,   // Educational - requires payment
    Social   // Social gathering - allows RSVP or payment
}

// ✅ CORRECT - Business logic based on simplified enum
public bool RequiresPayment => EventType == EventType.Class;
public bool AllowsRSVP => EventType == EventType.Social;
```

**Prevention**:
- Keep EventType enum values aligned across all projects
- Use consistent naming between backend enums and frontend filter options
- Always add project references when using types from other projects
- Test API endpoints after enum changes to verify DTO mapping works correctly

## Success Metrics

- ✅ All 16 tables created successfully
- ✅ Database seeding works (5 users, 10 events, 2 vetting applications)  
- ✅ API starts without errors
- ✅ Health checks pass
- ✅ Event Session Matrix tables functional
- ✅ RSVP system fully implemented with business rules
- ✅ 17 tables now (added RSVPs table)
- ✅ Database initialization services properly registered
- ✅ PUT /api/events/{id} endpoint implemented and compiling
- ✅ DELETE /api/events/{id} endpoint implemented and compiling
- ✅ NuGet packages updated to latest .NET 9 compatible versions
- ✅ EF Core check constraint warnings eliminated (23 → 16 warnings)
- ✅ EventService.cs compilation errors fixed (21 → 0 errors)
- ✅ EventType enum simplified to Class and Social only
- ⚠️ Business requirements mismatch identified and documented

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

2. **Updated Infrastructure Packages** (`/src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj`):
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
- `/src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj` - Standardized versions
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventSessionConfiguration.cs` - Fixed obsolete constraints
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventTicketTypeConfiguration.cs` - Fixed obsolete constraints

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

### CRITICAL SUCCESS: Eliminated NU1603 Version Mismatch Warnings

**Problem**: NuGet package updates failed to eliminate the primary target - NU1603 version mismatch warnings:
```
warning NU1603: WitchCityRope.Tests.Common depends on Testcontainers.PostgreSql (>= 4.2.2) but Testcontainers.PostgreSql 4.2.2 was not found. Testcontainers.PostgreSql 4.3.0 was resolved instead.
warning NU1603: WitchCityRope.Api.Tests depends on xunit.runner.visualstudio (>= 2.9.3) but xunit.runner.visualstudio 2.9.3 was not found. xunit.runner.visualstudio 3.0.0 was resolved instead.
```

**Root Cause**: Package version mismatches across test projects where requested versions weren't available, causing NuGet to resolve to newer versions but still showing warnings.

**Solution Implemented**:

1. **Identified Exact Version Conflicts**:
   - Testcontainers.PostgreSql: Requested 4.2.2 but resolved to 4.3.0 (latest 4.7.0)
   - xunit.runner.visualstudio: Requested 2.9.3 but resolved to 3.0.0 (latest 3.1.4)

2. **Updated All Test Projects to Latest Versions**:
   - `WitchCityRope.Tests.Common.csproj`: Testcontainers.PostgreSql 4.2.2 → 4.7.0 ✅
   - `WitchCityRope.Api.Tests.csproj`: xunit.runner.visualstudio 2.9.3 → 3.1.4 ✅
   - `WitchCityRope.Infrastructure.Tests.csproj`: Testcontainers 4.6.0 → 4.7.0 ✅
   - `WitchCityRope.Infrastructure.Tests.csproj`: Testcontainers.PostgreSql 4.6.0 → 4.7.0 ✅

3. **Force Package Restore**: `dotnet restore --force` to pick up new versions

**Verification**:
```bash
dotnet build src/ tests/WitchCityRope.Tests.Common/ tests/WitchCityRope.Api.Tests/ tests/WitchCityRope.Core.Tests/ tests/WitchCityRope.Infrastructure.Tests/ 2>&1 | grep "NU1603" || echo "NO NU1603 WARNINGS FOUND"
# Result: NO NU1603 WARNINGS FOUND
```

**Key Success Factors**:
- ✅ Updated ALL package versions consistently across projects
- ✅ Used latest available versions (not just compatible ones) 
- ✅ Force restored packages after version updates
- ✅ Systematic approach to identify exact version conflicts
- ✅ Fixed all cross-project version mismatches at once

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

### CRITICAL FIX: Entity Model Mismatch Causing 21 Compilation Errors

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

**Files Fixed**:
- `/src/WitchCityRope.Api/Features/Events/Services/EventService.cs`

**Build Results**:
- ✅ Compilation errors: 21 → 0
- ✅ Build succeeded with only warnings (no critical issues)
- ✅ Entity models now properly aligned with actual implementation

**Prevention**:
- Always verify entity model signatures before implementing service methods
- Use IntelliSense and compiler feedback to catch property/method mismatches
- Test build after entity model changes to identify alignment issues early
- Document entity model interfaces for reference during service development

## EventDto Mapping Issue - Missing EventType Field (2025-09-11)

### CRITICAL FIX: API Not Returning EventType Field 
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

**Test Results**:
✅ API endpoint `/api/events` now returns eventType field for all events  
✅ Individual event endpoint `/api/events/{id}` includes eventType  
✅ EventType values correctly mapped: "Workshop", "Class", "Meetup"  
✅ Admin dashboard filters now functional  
✅ Build compiles with 0 errors, 3 warnings (unchanged)

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