# SeedDataService Refactoring Plan

**Date**: 2025-10-27
**Status**: PLANNED - Not Yet Implemented
**Priority**: P2 - Technical Debt / Code Quality
**Estimated Effort**: 8-12 hours

## Problem Statement

The current `SeedDataService.cs` file has grown to **3,799 lines**, making it:
- Too large for sub-agents to read (exceeds 25K token limit)
- Violates Single Responsibility Principle
- Hard to maintain and test
- Causes merge conflicts
- Difficult to understand

## Current Structure Analysis

### File Metrics
- **Total lines**: 3,799
- **Public methods**: 13 (including `SeedAllDataAsync`)
- **Private helper methods**: ~25+
- **Dependencies**: `ApplicationDbContext`, `UserManager`, `RoleManager`, `ILogger`, `IEncryptionService`

### Method Breakdown

**Main Coordinator**:
- `SeedAllDataAsync` (lines 68-140) - Main orchestration with transaction management

**Public Seeding Methods**:
1. `SeedRolesAsync` (lines 153-213) - Create ASP.NET Identity roles (~60 lines)
2. `SeedUsersAsync` (lines 216-475) - Create test user accounts (~260 lines)
3. `SeedEventsAsync` (lines 476-1350) - Create sample events (~875 lines) ⚠️ VERY LARGE
4. `SeedVettingStatusesAsync` (lines 1351-1374) - Create vetting statuses (~24 lines)
5. `SeedVettingApplicationsAsync` (lines 1375-1759) - Create vetting applications (~385 lines)
6. `SeedVettingEmailTemplatesAsync` (lines 1760-2039) - Create email templates (~280 lines)
7. `SeedSessionsAndTicketsAsync` (lines 2040-2118) - Create sessions and tickets (~79 lines)
8. `SeedTicketPurchasesAsync` (lines 2119-2423) - Create ticket purchases (~305 lines)
9. `SeedEventParticipationsAsync` (lines 2424-2536) - Create RSVPs/participations (~113 lines)
10. `SeedVolunteerPositionsAsync` (lines 2537-2786) - Create volunteer positions (~250 lines)
11. `IsSeedDataRequiredAsync` (lines 2787-2809) - Check if seeding needed (~23 lines)
12. `SeedSafetyIncidentsAsync` (lines 3528-3761) - Create safety incidents (~234 lines)

**Private Helper Methods** (~15-20 methods):
- Lines 2810-3527: Event creation helpers (~717 lines)
- Lines 3762-3799: Settings seeder (~38 lines)
- Plus internal helpers for volunteer positions, vetting audit logs, etc.

**Additional Seeding**:
- `CmsSeedData.SeedInitialPagesAsync` - Called from external class
- `SeedSettingsAsync` - Private method

### Current DI Registration
- **File**: `/home/chad/repos/witchcityrope/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`
- **Line**: 117
- **Current**: `services.AddScoped<ISeedDataService, SeedDataService>();`

### Current Interface Location
- **File**: `/home/chad/repos/witchcityrope/apps/api/Services/ISeedDataService.cs`
- **Public methods**: 12 interface methods

## Proposed Refactored Structure

### Directory Organization
```
/apps/api/Services/Seeding/
├── ISeedDataService.cs                    (interface - move from /Services)
├── SeedCoordinator.cs                     (~200-300 lines) - Main orchestrator
├── UserSeeder.cs                          (~350-450 lines) - Roles + Users
├── EventSeeder.cs                         (~600-800 lines) - Events + helpers
├── SessionTicketSeeder.cs                 (~150-250 lines) - Sessions + Ticket Types
├── TicketPurchaseSeeder.cs                (~350-450 lines) - Purchases + historical
├── ParticipationSeeder.cs                 (~150-250 lines) - RSVPs + check-ins
├── VolunteerSeeder.cs                     (~300-400 lines) - Positions + signups
├── VettingSeeder.cs                       (~700-900 lines) - Statuses + applications + templates + audit
├── SafetySeeder.cs                        (~250-350 lines) - Safety incidents
├── SettingsSeeder.cs                      (~50-100 lines) - System settings
└── CmsSeeder.cs                           (~50-100 lines) - CMS content wrapper
```

### Seeder Class Responsibilities

#### 1. **SeedCoordinator.cs** (~200-300 lines)
**Purpose**: Main orchestrator implementing `ISeedDataService`

**Responsibilities**:
- Coordinate all seeders in correct order
- Transaction management (begin/commit/rollback)
- Call `IsSeedDataRequiredAsync` before seeding
- Aggregate counts and timing
- Structured logging for overall process
- Return `InitializationResult`

**Dependencies**:
- `ApplicationDbContext` (for transaction)
- All 10 seeder classes
- `ILogger<SeedCoordinator>`

**Key Methods**:
```csharp
public async Task<InitializationResult> SeedAllDataAsync(CancellationToken)
{
    // Check if seeding required
    // Begin transaction
    // Call seeders in order
    // Aggregate results
    // Commit transaction
    // Return InitializationResult
}
```

#### 2. **UserSeeder.cs** (~350-450 lines) ✅ CREATED
**Purpose**: Handle roles and user account creation

**Methods**:
- `SeedRolesAsync()` - Create Administrator, Teacher, SafetyTeam roles
- `SeedUsersAsync()` - Create 19 test accounts with varied roles/vetting statuses

**Dependencies**:
- `UserManager<ApplicationUser>`
- `RoleManager<IdentityRole<Guid>>`
- `ApplicationDbContext`
- `ILogger<UserSeeder>`

**Note**: This class has been created at `/apps/api/Services/Seeding/UserSeeder.cs`

#### 3. **EventSeeder.cs** (~600-800 lines)
**Purpose**: Create events and all event-related helpers

**Methods**:
- `SeedEventsAsync()` - Create 8 sample events (6 upcoming, 2 past)

**Private Helpers** (keep internal to EventSeeder):
- `CreateSeedEvent()` - Event factory method
- `AddSingleDayEvent()` - Single-day event logic
- `AddMultiDayEvent()` - Multi-day event logic
- `AddSingleDayEventSession()` - Session creation for single-day
- `AddSuspensionBasicsSessions()` - Specialized session logic
- `AddMultiDayEventSessions()` - Multi-day session creation
- All event-related helper methods (~10-15 private methods)

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<EventSeeder>`

#### 4. **SessionTicketSeeder.cs** (~150-250 lines)
**Purpose**: Create sessions and ticket types for events

**Methods**:
- `SeedSessionsAndTicketsAsync()` - Coordinate session and ticket type creation

**Private Helpers**:
- `CreateTicketTypesForSession()` - Ticket type factory
- `CreateMultiDayTicketTypes()` - Multi-day ticket logic

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<SessionTicketSeeder>`

#### 5. **TicketPurchaseSeeder.cs** (~350-450 lines)
**Purpose**: Create ticket purchases (current and historical)

**Methods**:
- `SeedTicketPurchasesAsync()` - Create purchases for vetted users

**Private Helpers**:
- `CreateVettedUserTicketPurchasesAsync()` - Purchase creation logic
- `GetRandomPaymentStatus()` - Payment status randomizer
- `GetRandomPaymentMethod()` - Payment method randomizer
- `GetRandomPurchaseNotes()` - Notes randomizer

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<TicketPurchaseSeeder>`

#### 6. **ParticipationSeeder.cs** (~150-250 lines)
**Purpose**: Create EventParticipation records (RSVPs, check-ins, attendance)

**Methods**:
- `SeedEventParticipationsAsync()` - Create RSVP and participation records

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<ParticipationSeeder>`

#### 7. **VolunteerSeeder.cs** (~300-400 lines)
**Purpose**: Create volunteer positions and assignments

**Methods**:
- `SeedVolunteerPositionsAsync()` - Create positions for events/sessions

**Private Helpers**:
- `SeedVolunteerSignupsAsync()` - Volunteer assignment logic
- `CreateEventVolunteerPositions()` - Event-level positions
- `CreateSessionVolunteerPositions()` - Session-level positions
- `CreateSuspensionBasicsVolunteerPositions()` - Specialized positions

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<VolunteerSeeder>`

#### 8. **VettingSeeder.cs** (~700-900 lines)
**Purpose**: Create vetting statuses, applications, templates, and audit logs

**Methods**:
- `SeedVettingStatusesAsync()` - Create vetting status configuration
- `SeedVettingApplicationsAsync()` - Create 11 sample applications with varied statuses
- `SeedVettingEmailTemplatesAsync()` - Create email templates

**Private Helpers**:
- `CreateVettingAuditLogsAsync()` - Create audit trail for applications

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<VettingSeeder>`

#### 9. **SafetySeeder.cs** (~250-350 lines)
**Purpose**: Create safety incident reports

**Methods**:
- `SeedSafetyIncidentsAsync()` - Create incidents with varied statuses, coordinators, encryption

**Dependencies**:
- `ApplicationDbContext`
- `IEncryptionService` (for PII encryption)
- `ILogger<SafetySeeder>`

#### 10. **SettingsSeeder.cs** (~50-100 lines)
**Purpose**: Create system settings

**Methods**:
- `SeedSettingsAsync()` - Create EventTimeZone and PreStartBufferMinutes settings

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<SettingsSeeder>`

#### 11. **CmsSeeder.cs** (~50-100 lines)
**Purpose**: Wrapper for CMS seed data

**Methods**:
- `SeedCmsContentAsync()` - Call `CmsSeedData.SeedInitialPagesAsync()`

**Dependencies**:
- `ApplicationDbContext`
- `ILogger<CmsSeeder>`

## Implementation Plan

### Phase 1: Preparation (1 hour)
- [x] Create `/apps/api/Services/Seeding/` directory
- [x] Document current structure and metrics
- [ ] Review all method dependencies and helper method usage
- [ ] Create comprehensive mapping of which helpers belong to which seeder

### Phase 2: Extract Seeders (5-7 hours)
**Order of extraction** (easiest to hardest):

1. [x] **UserSeeder.cs** - COMPLETED (~1 hour)
   - Simple, well-defined boundaries
   - Minimal private helpers
   - Already created and tested

2. [ ] **SettingsSeeder.cs** (~0.5 hour)
   - Minimal code
   - No dependencies on other seeders
   - Single private method

3. [ ] **CmsSeeder.cs** (~0.5 hour)
   - Simple wrapper around existing CmsSeedData
   - No complex logic

4. [ ] **VettingSeeder.cs** (~1.5 hours)
   - Three public methods
   - One major private helper (CreateVettingAuditLogsAsync)
   - Self-contained domain

5. [ ] **SafetySeeder.cs** (~1 hour)
   - One public method
   - No private helpers
   - Requires IEncryptionService dependency

6. [ ] **ParticipationSeeder.cs** (~1 hour)
   - One public method
   - Relatively simple logic
   - Depends on events and users existing

7. [ ] **SessionTicketSeeder.cs** (~1 hour)
   - One public method
   - 2-3 private helpers
   - Depends on events existing

8. [ ] **TicketPurchaseSeeder.cs** (~1 hour)
   - One public method
   - 3-4 private helpers (payment randomizers)
   - Depends on users, events, sessions, tickets

9. [ ] **VolunteerSeeder.cs** (~1.5 hours)
   - Two public methods
   - 3-4 private helpers for position creation
   - Depends on events and sessions

10. [ ] **EventSeeder.cs** (~2-3 hours) ⚠️ LARGEST
    - One public method with 875 lines of logic
    - 10-15 private helper methods
    - Most complex logic
    - Foundation for other seeders

### Phase 3: Create Coordinator (~1 hour)
- [ ] Create `SeedCoordinator.cs`
- [ ] Implement `ISeedDataService` interface
- [ ] Wire up all seeder dependencies
- [ ] Implement transaction management
- [ ] Implement `IsSeedDataRequiredAsync` (extract from old file)
- [ ] Add comprehensive logging
- [ ] Return `InitializationResult`

### Phase 4: Update DI Registration (~0.5 hour)
- [ ] Update `/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`
- [ ] Register all 11 seeder classes
- [ ] Register SeedCoordinator as ISeedDataService
- [ ] Remove old registration

**Current (Line 117)**:
```csharp
services.AddScoped<ISeedDataService, SeedDataService>();
```

**New**:
```csharp
// Register all seeder components
services.AddScoped<UserSeeder>();
services.AddScoped<EventSeeder>();
services.AddScoped<SessionTicketSeeder>();
services.AddScoped<TicketPurchaseSeeder>();
services.AddScoped<ParticipationSeeder>();
services.AddScoped<VolunteerSeeder>();
services.AddScoped<VettingSeeder>();
services.AddScoped<SafetySeeder>();
services.AddScoped<SettingsSeeder>();
services.AddScoped<CmsSeeder>();

// Register the coordinator as the main service
services.AddScoped<ISeedDataService, SeedCoordinator>();
```

### Phase 5: Update All References (~1 hour)
- [ ] Update `DatabaseInitializationService.cs` imports
- [ ] Update test files:
  - `/tests/unit/api/Services/SeedDataServiceTests.cs`
  - `/tests/unit/api/TestBase/DatabaseTestBase.cs`
- [ ] Update scripts:
  - `/scripts/seed-database-enhanced.sh`
- [ ] Update documentation references
- [ ] Search for any hardcoded namespace references

### Phase 6: Archive Old File (~0.5 hour)
- [ ] Move `/apps/api/Services/SeedDataService.cs` to archive
- [ ] New location: `/apps/api/Services/_archive/SeedDataService-legacy-2025-10-27.cs`
- [ ] Add archive comment header explaining refactoring
- [ ] Keep old ISeedDataService.cs for reference (then delete after confirming new one works)

### Phase 7: Testing & Validation (~1-2 hours)
- [ ] Run `dotnet build apps/api` - Verify 0 compilation errors
- [ ] Run `dotnet test tests/unit/api` - Verify unit tests pass
- [ ] Run integration tests - Verify seed data creates correctly
- [ ] Test Docker startup with seeding
- [ ] Verify all 8 events created
- [ ] Verify all 19 users created
- [ ] Verify vetting applications created
- [ ] Verify safety incidents created

## Critical Requirements (DO NOT VIOLATE)

### 1. Public Interface Must Not Change
- ✅ `ISeedDataService.SeedAllDataAsync()` signature unchanged
- ✅ Same parameters, same return type (`Task<InitializationResult>`)
- ✅ Same behavior from the outside

### 2. All Existing Seed Data Must Be Preserved
- ✅ All 19 test users
- ✅ All 8 events (6 upcoming, 2 past)
- ✅ All 11 vetting applications
- ✅ All safety incidents
- ✅ All volunteer positions
- ✅ No logic changes during extraction

### 3. Transaction Handling Must Be Preserved
- ✅ Same transaction scope in SeedCoordinator
- ✅ Rollback on any error
- ✅ Commit only after ALL seeders succeed

### 4. Error Handling Must Be Preserved
- ✅ All try/catch blocks
- ✅ All logging statements
- ✅ All exception throwing behavior

### 5. Idempotency Must Be Preserved
- ✅ Still safe to run multiple times
- ✅ Check for existing data before creating
- ✅ No duplicate data creation

## Testing Validation Checklist

After refactoring is complete:

### Compilation
- [ ] `dotnet build apps/api` succeeds with 0 errors
- [ ] `dotnet build tests/unit/api` succeeds with 0 errors
- [ ] `dotnet build tests/integration` succeeds with 0 errors

### Seeder File Sizes
- [ ] SeedCoordinator.cs < 300 lines
- [ ] UserSeeder.cs < 500 lines
- [ ] EventSeeder.cs < 900 lines (largest)
- [ ] All other seeders < 500 lines each
- [ ] No single file > 1,000 lines

### Functionality
- [ ] Seed data creates successfully
- [ ] Transaction rollback works on error
- [ ] All 19 users created with correct roles
- [ ] All 8 events created with correct data
- [ ] All 11 vetting applications created
- [ ] All safety incidents created with encryption
- [ ] Settings created (EventTimeZone, PreStartBufferMinutes)
- [ ] CMS pages created

### Integration
- [ ] DatabaseInitializationService works
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Docker startup seeding works
- [ ] No broken imports
- [ ] No missing dependencies

## References Updated

Files that reference SeedDataService (need update):

### Code Files (7 files)
1. ✅ `/apps/api/Services/SeedDataService.cs` - MOVE TO ARCHIVE
2. ✅ `/apps/api/Services/ISeedDataService.cs` - MOVE TO /Seeding/
3. [ ] `/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs` - UPDATE DI
4. [ ] `/apps/api/Services/DatabaseInitializationService.cs` - UPDATE IMPORTS
5. [ ] `/tests/unit/api/Services/SeedDataServiceTests.cs` - UPDATE IMPORTS
6. [ ] `/tests/unit/api/TestBase/DatabaseTestBase.cs` - UPDATE IMPORTS
7. [ ] `/tests/integration/DatabaseInitializationIntegrationTests.cs.disabled` - UPDATE IMPORTS (disabled file)

### Script Files (1 file)
8. [ ] `/scripts/seed-database-enhanced.sh` - VERIFY (may not need changes)

### Documentation Files (38 files - reference only, no code changes)
- Progress tracking docs
- Test catalogs
- Architecture docs
- Functional area specs
- Staging guides
- These are informational only - update if actively maintained

## Known Challenges

### 1. EventSeeder Size
- Event seeder will be the largest (~600-800 lines)
- Contains complex multi-day event logic
- Has 10-15 private helper methods
- Consider further sub-division if > 900 lines

### 2. Helper Method Ownership
- Some helpers might be shared between seeders
- Decision: Keep all helpers private within their seeder
- Duplicate code if needed rather than create shared helpers
- Keeps each seeder self-contained

### 3. Dependency Order
- Seeders have dependencies (Users → Events → Sessions → Tickets → Purchases)
- SeedCoordinator must call in correct order
- Document the order clearly in coordinator

### 4. Transaction Scope
- All seeders must use same DbContext for transaction
- Pass context to all seeders via constructor
- No new contexts created within seeders

## Benefits After Refactoring

### Code Quality
- ✅ Each file < 1,000 lines (readable by sub-agents)
- ✅ Single Responsibility Principle followed
- ✅ Easier to understand and navigate
- ✅ Easier to test individual seeders

### Maintainability
- ✅ Changes isolated to specific seeders
- ✅ Reduced merge conflicts
- ✅ Clearer ownership of code sections
- ✅ Easier to add new seed data categories

### Testing
- ✅ Can unit test individual seeders
- ✅ Can mock dependencies between seeders
- ✅ Faster test execution (test only what changed)
- ✅ Better test coverage

### Development
- ✅ Sub-agents can read seeder files (< 25K tokens)
- ✅ AI can work with individual seeders
- ✅ Clearer code review scope
- ✅ Better IDE performance

## Timeline Estimate

- **Preparation**: 1 hour
- **Extract Seeders**: 5-7 hours
- **Create Coordinator**: 1 hour
- **Update DI**: 0.5 hour
- **Update References**: 1 hour
- **Archive Old File**: 0.5 hour
- **Testing**: 1-2 hours

**Total**: 10-13 hours of focused work

## Next Steps

1. **Complete Phase 2**: Extract remaining 10 seeders (EventSeeder last)
2. **Complete Phase 3**: Create SeedCoordinator
3. **Complete Phase 4-6**: Update DI, references, archive
4. **Complete Phase 7**: Comprehensive testing

## Session Notes

**2025-10-27 Session**:
- ✅ Created `/apps/api/Services/Seeding/` directory
- ✅ Documented current structure (3,799 lines analyzed)
- ✅ Created UserSeeder.cs with SeedRolesAsync and SeedUsersAsync (~350 lines)
- ✅ Created this comprehensive refactoring plan
- ⏸️ Paused extraction to avoid token limit issues (remaining: 10 seeders)
- 📝 Remaining work: ~9-12 hours to complete full refactoring

**Recommendation**: Due to file size (3,799 lines) and complexity, this refactoring should be done in multiple focused sessions:
- Session 1 (completed): UserSeeder + Plan
- Session 2: Extract 5 simple seeders (Settings, Cms, Vetting, Safety, Participation)
- Session 3: Extract 4 medium seeders (SessionTicket, TicketPurchase, Volunteer, Event)
- Session 4: Create SeedCoordinator, update DI, test
- Session 5: Update all references, archive old file, comprehensive validation
