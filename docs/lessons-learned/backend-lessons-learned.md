# Backend Developer Lessons Learned

This document tracks critical lessons learned during backend development to prevent recurring issues and speed up future development.

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

## Success Metrics

- ✅ All 16 tables created successfully
- ✅ Database seeding works (5 users, 10 events, 2 vetting applications)  
- ✅ API starts without errors
- ✅ Health checks pass
- ✅ Event Session Matrix tables functional
- ✅ RSVP system fully implemented with business rules
- ✅ 17 tables now (added RSVPs table)
- ⚠️ Business requirements mismatch identified and documented