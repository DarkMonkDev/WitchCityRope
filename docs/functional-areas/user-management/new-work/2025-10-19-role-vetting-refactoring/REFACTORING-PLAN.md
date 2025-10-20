# Role & Vetting Status Refactoring Plan
<!-- Last Updated: 2025-10-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer + Backend Developer -->
<!-- Status: Planning -->

## Executive Summary

### What We're Changing and Why

We are implementing **Option 1: Simplified Role Model** which fundamentally restructures how we handle user permissions and vetting status in the WitchCityRope system.

**Current Problem**: Our system conflates two distinct concepts:
1. **Permission Levels** (what users can DO) - currently expressed as roles
2. **Member Status** (where users are in the vetting process) - tracked by both `IsVetted` boolean AND `VettingStatus` enum

This creates:
- **Data Redundancy**: `IsVetted` boolean duplicates information already in `VettingStatus` enum
- **Role Confusion**: "Member", "Attendee", and "VettedMember" roles mix permission levels with vetting status
- **Two "OnHold" Concepts**: `VettingStatus.OnHold` vs `IsActive=false` cause confusion
- **Unused Roles**: `VettedMember` role exists but is never used
- **Inconsistent Seed Data**: `SafetyTeam` role exists in code but not in seed data roles list

**Solution**:
- **Remove** redundant `IsVetted` boolean field entirely
- **Remove** status-based roles: Member, Attendee, VettedMember
- **Keep** only permission-based roles: Administrator, Teacher, SafetyTeam
- **Use** `VettingStatus` enum as the ONLY source of truth for member vetting state
- **Create** computed property `IsVetted => VettingStatus == 3` for backward compatibility during migration

### Benefits of This Refactoring

1. **Single Source of Truth**: `VettingStatus` enum becomes the definitive source for vetting state
2. **Clear Separation of Concerns**: Roles = permissions, VettingStatus = member state
3. **Reduced Complexity**: Eliminates dual tracking of vetting status (boolean + enum)
4. **Simplified Authorization**: Clear logic - check role for permissions, check VettingStatus for member state
5. **Better Maintainability**: Future developers won't be confused by redundant fields
6. **Easier Testing**: Fewer combinations to test, clearer test scenarios
7. **Database Efficiency**: One less boolean column to maintain
8. **Consistent Seed Data**: All roles properly documented and seeded

### Risks and Mitigation Strategies

| Risk | Impact | Likelihood | Mitigation Strategy |
|------|--------|------------|---------------------|
| Breaking existing authorization logic | HIGH | MEDIUM | Comprehensive grep search (120 files found), systematic review of each usage |
| Test failures due to changed data structure | MEDIUM | HIGH | Run all tests after changes, fix failures systematically |
| Frontend components breaking | MEDIUM | MEDIUM | Regenerate API types immediately after backend changes |
| Migration script data loss | HIGH | LOW | Use computed property during transition, keep migration reversible |
| Production deployment issues | HIGH | LOW | Deploy with database migration, monitor for 1 release cycle before final cleanup |
| Confusion during transition | MEDIUM | MEDIUM | Keep computed property temporarily, comprehensive documentation |

**Risk Reduction**: Keep `IsVetted` as computed property for 1 release cycle before final removal.

---

## Current State Analysis

### Current Fields in ApplicationUser Model

```csharp
public class ApplicationUser : IdentityUser
{
    // Authentication & Account Status
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;  // Account enabled/disabled

    // ROLE FIELD - Currently problematic
    public string Role { get; set; } = "Member";
    // Possible values: Administrator, Teacher, Member, Attendee, VettedMember, SafetyTeam

    // VETTING STATUS FIELDS - REDUNDANT!
    public bool IsVetted { get; set; } = false;  // ❌ REDUNDANT with VettingStatus
    public int VettingStatus { get; set; } = 0;  // ✅ SOURCE OF TRUTH (should be)

    // VettingStatus Enum Values:
    // 0 = UnderReview
    // 1 = InterviewApproved
    // 2 = FinalReview
    // 3 = Approved (this means IsVetted should be true)
    // 4 = Denied
    // 5 = OnHold (DIFFERENT from IsActive=false)
    // 6 = Withdrawn
}
```

### Current Role List

**In Code** (6 roles):
- Administrator - Full system access
- Teacher - Teaching privileges
- Member - Default role (status-based, not permission-based)
- Attendee - Guest role (status-based, not permission-based)
- VettedMember - NEVER USED (status-based, not permission-based)
- SafetyTeam - Safety incident management

**In Seed Data** (5 roles):
- Administrator
- Teacher
- Member
- Attendee
- VettedMember
- ❌ **MISSING**: SafetyTeam (exists in code but not seeded)

### Problems Identified

#### 1. "Member" and "Attendee" as Roles
**Problem**: These conflate permission levels with vetting status.
- "Member" should mean "has member privileges" but currently means "registered user"
- "Attendee" should mean "event guest" but currently means "unvetted user"
- Creates confusion: Is a "Member" vetted? Not necessarily!

**Current Logic**:
```csharp
// Confusing - Member role doesn't mean vetted
if (user.Role == "Member" && user.IsVetted) // Requires BOTH checks
if (user.Role == "VettedMember") // Never used, always false
```

#### 2. IsVetted Boolean Duplicates VettingStatus
**Problem**: Same information tracked in two places.
- `IsVetted = true` should ALWAYS equal `VettingStatus == 3` (Approved)
- But they can get out of sync
- Creates "which is correct?" questions

**Current Usage Patterns** (from 120 files):
```csharp
// Pattern 1: Check IsVetted directly
if (user.IsVetted) { /* allow access */ }

// Pattern 2: Check VettingStatus
if (user.VettingStatus == 3) { /* approved */ }

// Pattern 3: Check both (redundant)
if (user.IsVetted && user.VettingStatus == 3) { /* definitely approved */ }
```

#### 3. Two "OnHold" Concepts
**Problem**: Confusing overlap between vetting workflow and account status.
- `VettingStatus.OnHold` (5) = Vetting process paused
- `IsActive = false` = Account suspended/disabled
- Users ask: "Which OnHold are we talking about?"

**Current Behavior**:
```csharp
// Vetting on hold (still can log in)
user.VettingStatus = 5; // OnHold
user.IsActive = true;

// Account suspended (cannot log in)
user.IsActive = false;
user.VettingStatus = [any value]; // Doesn't matter

// Both? What does this mean?
user.VettingStatus = 5; // OnHold
user.IsActive = false; // Suspended
```

#### 4. VettedMember Role Never Used
**Problem**: Role exists but no code path ever sets it.
```bash
$ grep -r "VettedMember" --include="*.cs" | grep -v "Member" | grep -v Designer
# Results: Only in role seed data, never assigned to users
```

#### 5. SafetyTeam Role Missing from Seed Data
**Problem**: Role exists and is used but not properly initialized.
```csharp
// In code: SafetyTeam role is checked
if (user.Role == "SafetyTeam") { /* safety privileges */ }

// In seed data: Only Admin, Teacher, Member, Attendee, VettedMember
// SafetyTeam coordinators can't get the role!
```

---

## Target State Design

### Simplified Roles (Permission Levels ONLY)

**Three Permission-Based Roles**:
1. **Administrator** - Full system access (unchanged)
2. **Teacher** - Teaching/workshop privileges (unchanged)
3. **SafetyTeam** - Safety incident management (now properly seeded)

**Removed Roles**:
- ❌ Member (everyone is a member, tracked by VettingStatus instead)
- ❌ Attendee (guest status tracked by VettingStatus instead)
- ❌ VettedMember (never used, redundant with VettingStatus.Approved)

### Vetting Status (Member Status - Source of Truth)

**VettingStatus Enum** (unchanged, 0-6):
- 0 = UnderReview - Application submitted, pending review
- 1 = InterviewApproved - Interview complete, awaiting final decision
- 2 = FinalReview - Final review stage
- **3 = Approved** - ✅ **THIS IS "VETTED"** (the only true vetted state)
- 4 = Denied - Application rejected
- 5 = OnHold - Process paused (user can still log in if IsActive=true)
- 6 = Withdrawn - User withdrew application

**IsVetted Field**:
- ❌ **REMOVE** from database (drop column)
- ✅ **ADD** computed property for migration period:
  ```csharp
  public bool IsVetted => VettingStatus == 3; // Approved
  ```

### Account Status (Unchanged)

**IsActive Field** (unchanged):
- `true` = Account enabled, user can log in
- `false` = Account suspended/disabled, blocks login

### Authorization Logic Examples

```csharp
// ✅ Check if user is a vetted member
if (user.VettingStatus == 3) // or VettingStatus.Approved
{
    // User is fully vetted, show vetted-member content
}

// ✅ Check if user has admin permissions
if (user.Role == "Administrator")
{
    // User has admin privileges
}

// ✅ Check if user has teaching permissions
if (user.Role == "Teacher")
{
    // User can create/manage workshops
}

// ✅ Check if user can manage safety incidents
if (user.Role == "SafetyTeam")
{
    // User can view/manage safety reports
}

// ✅ Check if account is active (not suspended)
if (!user.IsActive)
{
    return Unauthorized("Your account has been suspended.");
}

// ✅ Combine checks for specific scenarios
if (user.VettingStatus == 3 && user.IsActive)
{
    // Vetted member with active account
}

// ❌ NEVER CHECK (these won't exist anymore)
if (user.Role == "Member") // ❌ REMOVED
if (user.Role == "VettedMember") // ❌ REMOVED
if (user.IsVetted) // ❌ COLUMN REMOVED (only computed property exists)
```

---

## Implementation Phases

### Phase 1: Database Schema Changes

**Estimated Time**: 2-3 hours
**Owner**: Database Designer + Backend Developer

#### Tasks:
1. **Create EF Core Migration**
   ```bash
   dotnet ef migrations add RemoveIsVettedAndSimplifyRoles -p apps/api
   ```

2. **Migration Up (Forward)**
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       // No data migration needed - computed property handles it
       migrationBuilder.DropColumn(
           name: "IsVetted",
           table: "AspNetUsers");
   }
   ```

3. **Migration Down (Rollback)**
   ```csharp
   protected override void Down(MigrationBuilder migrationBuilder)
   {
       // Restore column if rollback needed
       migrationBuilder.AddColumn<bool>(
           name: "IsVetted",
           table: "AspNetUsers",
           type: "boolean",
           nullable: false,
           defaultValue: false);

       // Populate based on VettingStatus
       migrationBuilder.Sql(
           "UPDATE \"AspNetUsers\" SET \"IsVetted\" = CASE WHEN \"VettingStatus\" = 3 THEN true ELSE false END");
   }
   ```

4. **Update ApplicationUser Model**
   ```csharp
   public class ApplicationUser : IdentityUser
   {
       // ... other properties ...

       // REMOVE this field
       // public bool IsVetted { get; set; } = false;

       // ADD computed property (temporary, 1 release cycle)
       [NotMapped] // Don't map to database
       public bool IsVetted => VettingStatus == 3;

       public int VettingStatus { get; set; } = 0;
       public string Role { get; set; } = ""; // Default to empty, no longer "Member"
       public bool IsActive { get; set; } = true;
   }
   ```

5. **Update Seed Data - SeedDataService.cs**
   ```csharp
   // REMOVE these roles from seed data
   // new IdentityRole { Name = "Member", NormalizedName = "MEMBER" },
   // new IdentityRole { Name = "Attendee", NormalizedName = "ATTENDEE" },
   // new IdentityRole { Name = "VettedMember", NormalizedName = "VETTEDMEMBER" },

   // KEEP only permission-based roles
   var roles = new[]
   {
       new IdentityRole { Id = "1", Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
       new IdentityRole { Id = "2", Name = "Teacher", NormalizedName = "TEACHER" },
       new IdentityRole { Id = "3", Name = "SafetyTeam", NormalizedName = "SAFETYTEAM" }, // ADD THIS
   };

   // Update test accounts
   new ApplicationUser
   {
       Email = "admin@witchcityrope.com",
       Role = "Administrator", // Permission role
       VettingStatus = 3, // Approved (vetted)
       IsActive = true
   },
   new ApplicationUser
   {
       Email = "teacher@witchcityrope.com",
       Role = "Teacher", // Permission role
       VettingStatus = 3, // Approved (vetted)
       IsActive = true
   },
   new ApplicationUser
   {
       Email = "vetted@witchcityrope.com",
       Role = "", // No special permissions
       VettingStatus = 3, // Approved (vetted)
       IsActive = true
   },
   new ApplicationUser
   {
       Email = "member@witchcityrope.com",
       Role = "", // No special permissions
       VettingStatus = 0, // UnderReview (not vetted yet)
       IsActive = true
   },
   new ApplicationUser
   {
       Email = "coordinator1@witchcityrope.com",
       Role = "SafetyTeam", // Permission role
       VettingStatus = 3, // Approved (vetted)
       IsActive = true
   }
   ```

#### Validation:
- [ ] Migration created successfully
- [ ] Migration up executes without errors
- [ ] Migration down executes without errors (rollback test)
- [ ] ApplicationUser model compiles
- [ ] Computed property returns correct values
- [ ] Seed data creates only 3 roles
- [ ] Test accounts have correct Role values (empty string for non-privileged users)

---

### Phase 2: Backend Code Updates

**Estimated Time**: 8-12 hours
**Owner**: Backend Developer

**Files Found**: 120 files reference `IsVetted`

#### Critical Backend Files (Must Update - Priority Order)

##### 1. Core Model & Data Layer
- `/apps/api/Models/ApplicationUser.cs` ✅ Done in Phase 1
- `/apps/api/Data/ApplicationDbContext.cs` - Check for any IsVetted references
- `/apps/api/Migrations/ApplicationDbContextModelSnapshot.cs` - Will regenerate automatically

##### 2. Seed Data & Test Helpers
- `/apps/api/Services/SeedDataService.cs` ✅ Done in Phase 1
- `/apps/api/Features/TestHelpers/Services/TestHelperService.cs` - Update test user creation
- `/apps/api/Features/TestHelpers/Models/TestUserResponse.cs` - Update DTO
- `/apps/api/Features/TestHelpers/Models/CreateTestUserRequest.cs` - Update DTO

##### 3. DTOs (Critical - Frontend Impact)
- `/apps/api/Features/Users/Models/UserDto.cs` - Change to computed property
- `/apps/api/Features/Authentication/Models/UserDto.cs` - Change to computed property
- `/apps/api/Features/Authentication/Models/AuthUserResponse.cs` - Change to computed property
- `/apps/api/Features/Dashboard/Models/UserDashboardResponse.cs` - Change to computed property
- `/apps/api/Models/Auth/UserDto.cs` - Change to computed property

**DTO Update Pattern**:
```csharp
// BEFORE
public class UserDto
{
    public string Id { get; set; }
    public bool IsVetted { get; set; } // Database field
    public int VettingStatus { get; set; }
}

// AFTER
public class UserDto
{
    public string Id { get; set; }
    public bool IsVetted { get; set; } // Computed from VettingStatus == 3
    public int VettingStatus { get; set; }
}

// Mapping in service
return new UserDto
{
    Id = user.Id,
    IsVetted = user.VettingStatus == 3, // Explicit computation
    VettingStatus = user.VettingStatus
};
```

##### 4. Services (Business Logic)
- `/apps/api/Features/Users/Services/UserManagementService.cs` - Update queries and logic
- `/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs` - Update logic
- `/apps/api/Features/Vetting/Services/VettingService.cs` - Update status sync (CRITICAL)
- `/apps/api/Features/Participation/Services/ParticipationService.cs` - Update authorization checks

**VettingService Critical Update**:
```csharp
// BEFORE - Kept IsVetted in sync manually
public async Task ApproveApplication(string applicationId)
{
    var application = await GetApplication(applicationId);
    application.VettingStatus = 3; // Approved

    var user = await _userManager.FindByIdAsync(application.UserId);
    user.IsVetted = true; // ❌ Manual sync
    await _userManager.UpdateAsync(user);
}

// AFTER - IsVetted computed automatically
public async Task ApproveApplication(string applicationId)
{
    var application = await GetApplication(applicationId);
    application.VettingStatus = 3; // Approved

    var user = await _userManager.FindByIdAsync(application.UserId);
    // user.IsVetted automatically computes to true
    await _userManager.UpdateAsync(user); // Just update VettingStatus
}
```

##### 5. Authorization & Validation
Search pattern for authorization checks:
```bash
grep -r "IsVetted" --include="*.cs" apps/api/Features/
grep -r "Role == \"Member\"" --include="*.cs" apps/api/
grep -r "Role == \"Attendee\"" --include="*.cs" apps/api/
grep -r "Role == \"VettedMember\"" --include="*.cs" apps/api/
```

**Update Pattern**:
```csharp
// ❌ BEFORE - Status-based role check
if (user.Role == "Member" && user.IsVetted)
{
    // Allow access
}

// ✅ AFTER - VettingStatus check
if (user.VettingStatus == 3) // Approved
{
    // Allow access
}

// ❌ BEFORE - Never-used role
if (user.Role == "VettedMember")
{
    // This never happened anyway
}

// ✅ AFTER - Direct status check
if (user.VettingStatus == 3) // Approved
{
    // Clear and accurate
}
```

#### Validation:
- [ ] All 120 files reviewed and updated
- [ ] No database references to IsVetted column remain
- [ ] All DTOs compute IsVetted from VettingStatus
- [ ] VettingService no longer syncs IsVetted manually
- [ ] Authorization checks use VettingStatus instead of IsVetted
- [ ] Role checks removed for Member/Attendee/VettedMember
- [ ] SafetyTeam role checks work correctly

---

### Phase 3: Frontend Code Updates

**Estimated Time**: 4-6 hours
**Owner**: React Developer

#### Step 1: Regenerate API Types (CRITICAL FIRST STEP)
```bash
cd /home/chad/repos/witchcityrope-react
npm run generate:api-types
```

**Files Updated Automatically**:
- `/packages/shared-types/src/generated/api-types.ts`
- `/packages/shared-types/src/generated/api-client.ts`
- `/packages/shared-types/src/generated/api-helpers.ts`

**What Changes**:
- `IsVetted` will still exist in DTOs (computed property)
- TypeScript types will remain the same
- **NO BREAKING CHANGES** for frontend during transition

#### Step 2: Update React Components

**Search for IsVetted Usage**:
```bash
grep -r "isVetted" apps/web/src/ --include="*.tsx" --include="*.ts"
```

**Expected Files** (from documentation):
- Vetting system components
- User dashboard components
- Admin members list components
- Profile components

**Update Pattern** (Semantic only - no code changes needed):
```typescript
// BEFORE & AFTER are the SAME during transition
// IsVetted is still available as computed property

interface User {
  id: string;
  isVetted: boolean; // Still exists (computed from vettingStatus === 3)
  vettingStatus: number; // 0-6 enum
  role: string; // Now: "", "Administrator", "Teacher", "SafetyTeam"
}

// No code changes needed - computed property handles it
if (user.isVetted) {
  // Show vetted member content
}

// Can also check VettingStatus directly
if (user.vettingStatus === 3) {
  // Show approved content (same as isVetted)
}
```

**Component Review Checklist**:
- [ ] Vetting status display components
- [ ] User profile components
- [ ] Admin members list filters
- [ ] Dashboard conditional rendering
- [ ] Navigation menu visibility logic

#### Step 3: Update Filters and Searches

**Admin Members List Component**:
```typescript
// BEFORE - Filter by IsVetted boolean
const filters = {
  isVetted: true // Boolean filter
};

// AFTER - Can keep same filter (computed property works)
// OR switch to VettingStatus for more granularity
const filters = {
  vettingStatus: 3 // Approved only
  // OR multiple statuses
  vettingStatus: [1, 2, 3] // InterviewApproved, FinalReview, Approved
};
```

#### Validation:
- [ ] API types regenerated successfully
- [ ] TypeScript compilation succeeds (npm run build)
- [ ] No new TypeScript errors introduced
- [ ] All components render correctly
- [ ] Filters work as expected
- [ ] Vetting status badges display correctly

---

### Phase 4: Test Updates

**Estimated Time**: 6-8 hours
**Owner**: Test Developer

**Files Found**: Extensive test coverage across unit/integration/E2E

#### Unit Tests - Backend (C#)

**Critical Test Files**:
- `/tests/unit/api/Services/SeedDataServiceTests.cs` - Update role expectations
- `/tests/unit/api/Features/Users/UserManagementServiceTests.cs` - Update IsVetted assertions
- `/tests/unit/api/Features/Dashboard/UserDashboardProfileServiceTests.cs` - Update logic tests
- `/tests/unit/api/Features/Safety/SafetyServiceTests.cs` - Update authorization tests
- `/tests/unit/api/Features/Safety/SafetyServiceExtendedTests.cs` - Update authorization tests
- `/tests/unit/api/Features/Participation/ParticipationServiceTests.cs` - Update access tests
- `/tests/unit/api/Features/Participation/ParticipationServiceTests_Extended.cs` - Update access tests

**Update Pattern**:
```csharp
// BEFORE
[Fact]
public async Task ApproveApplication_SetsIsVettedTrue()
{
    var user = await CreateTestUser();
    await _vettingService.ApproveApplication(applicationId);

    Assert.True(user.IsVetted); // Direct property check
}

// AFTER
[Fact]
public async Task ApproveApplication_SetsVettingStatusApproved()
{
    var user = await CreateTestUser();
    await _vettingService.ApproveApplication(applicationId);

    Assert.Equal(3, user.VettingStatus); // Check VettingStatus enum
    Assert.True(user.IsVetted); // Computed property still works
}
```

#### Test Builders

**Files**:
- `/tests/WitchCityRope.Tests.Common/Builders/UserDtoBuilder.cs` - Update builder pattern

**Update Pattern**:
```csharp
// BEFORE
public class UserDtoBuilder
{
    private bool _isVetted = false;

    public UserDtoBuilder WithVettedStatus()
    {
        _isVetted = true;
        return this;
    }

    public UserDto Build() => new UserDto { IsVetted = _isVetted };
}

// AFTER
public class UserDtoBuilder
{
    private int _vettingStatus = 0;

    public UserDtoBuilder WithVettedStatus()
    {
        _vettingStatus = 3; // Approved
        return this;
    }

    public UserDto Build() => new UserDto
    {
        VettingStatus = _vettingStatus,
        IsVetted = _vettingStatus == 3 // Computed
    };
}
```

#### Integration Tests

**Files**:
- `/tests/integration/DatabaseInitializationIntegrationTests.cs.disabled` - Check when re-enabled
- Search for role assertions in integration tests

**Update Pattern**:
```csharp
// BEFORE
var members = await _db.Users.Where(u => u.Role == "Member").ToListAsync();
Assert.NotEmpty(members);

// AFTER
var nonPrivilegedUsers = await _db.Users
    .Where(u => u.Role == "" || u.Role == null)
    .ToListAsync();
Assert.NotEmpty(nonPrivilegedUsers);
```

#### E2E Tests (Playwright)

**Files**:
- `/tests/WitchCityRope.E2E.Tests/Fixtures/TestDataManager.cs` - Update test user creation
- `/tests/WitchCityRope.E2E.Tests/PageObjects/Members/DashboardPage.cs` - Update assertions

**Search Pattern**:
```bash
grep -r "IsVetted" tests/ --include="*.cs"
grep -r "Member" tests/ --include="*.cs" | grep "Role"
```

#### React Unit Tests

**Search Pattern**:
```bash
grep -r "isVetted" apps/web/src/ --include="*.test.tsx" --include="*.spec.tsx"
```

**Update Pattern**:
```typescript
// BEFORE & AFTER are mostly the same
// IsVetted still works as computed property

describe('VettingStatusBox', () => {
  it('shows vetted badge when user is vetted', () => {
    const user = {
      isVetted: true, // Still works
      vettingStatus: 3 // Approved
    };
    render(<VettingStatusBox user={user} />);
    expect(screen.getByText('Vetted Member')).toBeInTheDocument();
  });
});
```

#### Validation:
- [ ] All backend unit tests pass
- [ ] All integration tests pass
- [ ] All E2E tests pass
- [ ] Test builders updated correctly
- [ ] No role assertions for removed roles (Member/Attendee/VettedMember)
- [ ] SafetyTeam role tests work correctly

---

### Phase 5: Documentation Updates

**Estimated Time**: 3-4 hours
**Owner**: Librarian + Backend Developer

#### Documentation Files to Update

**Functional Specifications**:
- `/docs/functional-areas/user-management/README.md` - Update role descriptions
- `/docs/functional-areas/user-management/business-requirements/functional-specifications.md` - Update authorization logic
- `/docs/functional-areas/user-management/current-state/VETTING_STATUS_GUIDE.md` - Update field descriptions
- `/docs/functional-areas/user-management/current-state/technical-design-membership.md` - Update data model

**API Documentation**:
- `/docs/functional-areas/api-architecture-modernization/new-work/2025-08-22-minimal-api-research/research/current-api-architecture-analysis.md` - Update authorization patterns
- `/docs/guides-setup/dto-quick-reference.md` - Update DTO examples

**Database Documentation**:
- `/docs/functional-areas/database-initialization/design/database-design.md` - Update schema docs
- `/docs/functional-areas/seed-data/VETTING_STATUS_SEED_DATA.md` - Update seed data documentation

**Developer Guides**:
- Create new guide: `/docs/guides-setup/role-vetting-status-guide.md` - Comprehensive developer reference

**Architecture Decisions**:
- Create ADR: `/docs/architecture/adrs/ADR-XXX-simplified-role-model.md`

#### New Documentation to Create

**1. Role & Vetting Status Developer Guide**
```markdown
# Role & Vetting Status Guide

## Quick Reference

### Roles (Permission Levels)
- Administrator - Full system access
- Teacher - Teaching privileges
- SafetyTeam - Safety incident management
- "" (empty string) - No special privileges

### Vetting Status (Member State)
- 0 = UnderReview - Not vetted
- 1 = InterviewApproved - Not vetted
- 2 = FinalReview - Not vetted
- **3 = Approved** - VETTED ✅
- 4 = Denied - Not vetted
- 5 = OnHold - Not vetted
- 6 = Withdrawn - Not vetted

## Authorization Patterns

### Check if User is Vetted
```csharp
// Backend
if (user.VettingStatus == 3) { /* vetted */ }
if (user.IsVetted) { /* also works - computed property */ }

// Frontend
if (user.vettingStatus === 3) { /* vetted */ }
if (user.isVetted) { /* also works */ }
```

### Check if User has Admin Permissions
```csharp
if (user.Role == "Administrator") { /* admin */ }
```

[Continue with more examples...]
```

**2. Architecture Decision Record**
```markdown
# ADR-XXX: Simplified Role Model

## Status
Accepted - 2025-10-19

## Context
[Copy from Executive Summary above]

## Decision
We will remove the IsVetted boolean field and status-based roles...

## Consequences
[Copy benefits and risks from above]

## Implementation
[Link to this refactoring plan]
```

#### Validation:
- [ ] All documentation files updated
- [ ] New developer guide created
- [ ] ADR created and linked
- [ ] Seed data documentation updated
- [ ] API documentation reflects new DTOs
- [ ] Database schema documentation updated

---

## Detailed File Changes

### Files Requiring Updates (Organized by Category)

#### Category 1: Core Model & Data (15 files)
```
apps/api/Models/ApplicationUser.cs ⭐ CRITICAL
apps/api/Data/ApplicationDbContext.cs
apps/api/Migrations/ApplicationDbContextModelSnapshot.cs (auto-generated)
apps/api/Migrations/20251003000750_InitialCreate.cs
apps/api/Migrations/20251003000750_InitialCreate.Designer.cs
[All migration .Designer.cs files - 30+ files]
```

#### Category 2: Seed Data & Test Helpers (5 files)
```
apps/api/Services/SeedDataService.cs ⭐ CRITICAL
apps/api/Features/TestHelpers/Services/TestHelperService.cs
apps/api/Features/TestHelpers/Models/TestUserResponse.cs
apps/api/Features/TestHelpers/Models/CreateTestUserRequest.cs
scripts/seed-database.sql
```

#### Category 3: DTOs (8 files) ⭐ CRITICAL - Frontend Impact
```
apps/api/Features/Users/Models/UserDto.cs ⭐
apps/api/Features/Authentication/Models/UserDto.cs ⭐
apps/api/Features/Authentication/Models/AuthUserResponse.cs ⭐
apps/api/Features/Dashboard/Models/UserDashboardResponse.cs ⭐
apps/api/Models/Auth/UserDto.cs ⭐
apps/api/Features/Users/Models/UpdateUserRequest.cs
apps/api/Features/Users/Models/UserSearchRequest.cs
packages/shared-types/src/generated/api-types.ts (auto-generated) ⭐
```

#### Category 4: Services (Business Logic) (10 files)
```
apps/api/Features/Users/Services/UserManagementService.cs ⭐
apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs
apps/api/Features/Vetting/Services/VettingService.cs ⭐ CRITICAL
apps/api/Features/Participation/Services/ParticipationService.cs
apps/api/Features/Safety/Services/SafetyService.cs (if checking IsVetted)
```

#### Category 5: Unit Tests (15 files)
```
tests/unit/api/Services/SeedDataServiceTests.cs
tests/unit/api/Features/Users/UserManagementServiceTests.cs
tests/unit/api/Features/Dashboard/UserDashboardProfileServiceTests.cs
tests/unit/api/Features/Safety/SafetyServiceTests.cs
tests/unit/api/Features/Safety/SafetyServiceExtendedTests.cs
tests/unit/api/Features/Participation/ParticipationServiceTests.cs
tests/unit/api/Features/Participation/ParticipationServiceTests_Extended.cs
tests/WitchCityRope.Tests.Common/Builders/UserDtoBuilder.cs
```

#### Category 6: Integration & E2E Tests (5 files)
```
tests/integration/DatabaseInitializationIntegrationTests.cs.disabled
tests/WitchCityRope.E2E.Tests/Fixtures/TestDataManager.cs
tests/WitchCityRope.E2E.Tests/PageObjects/Members/DashboardPage.cs
```

#### Category 7: Frontend (Post API Regeneration) (20+ files estimated)
```
packages/shared-types/src/generated/*.ts (auto-generated)
apps/web/src/features/vetting/**/*.tsx (components)
apps/web/src/features/admin/members/**/*.tsx (admin screens)
apps/web/src/features/dashboard/**/*.tsx (dashboard)
apps/web/src/**/*.test.tsx (React tests)
```

#### Category 8: Documentation (25 files)
```
docs/functional-areas/user-management/README.md
docs/functional-areas/user-management/business-requirements/functional-specifications.md
docs/functional-areas/user-management/current-state/VETTING_STATUS_GUIDE.md
docs/functional-areas/user-management/current-state/technical-design-membership.md
docs/functional-areas/vetting-system/**/*.md (multiple files)
docs/functional-areas/payments/new-work/*/requirements/*.md
docs/guides-setup/dto-quick-reference.md
docs/functional-areas/database-initialization/design/database-design.md
docs/functional-areas/seed-data/VETTING_STATUS_SEED_DATA.md
[Create new: ADR, developer guide]
```

#### Category 9: Legacy/Archive (Reference Only - No Changes)
```
src/_archive/**/*.cs (reference only, don't update)
docs/_archive/**/*.md (reference only, don't update)
```

---

## Migration Strategy

### Step-by-Step Execution Order

#### Preparation (Before Starting)
1. ✅ Create feature branch: `git checkout -b feature/simplify-roles-remove-isvetted`
2. ✅ Ensure all tests pass on main: `npm test && dotnet test`
3. ✅ Document current state: `git log --oneline -10 > pre-refactor-commits.txt`
4. ✅ Create backup: Development databases only (PostgreSQL)

#### Phase 1: Database Changes (Day 1 Morning)
1. Update ApplicationUser model (remove IsVetted, add computed property)
2. Update SeedDataService (remove status roles, add SafetyTeam)
3. Create EF migration: `dotnet ef migrations add RemoveIsVettedAndSimplifyRoles`
4. Test migration up: `dotnet ef database update`
5. Test migration down: `dotnet ef migrations remove --force`
6. Re-apply migration: `dotnet ef database update`
7. Commit: "refactor: remove IsVetted field and simplify role model (schema)"

#### Phase 2: Backend Services (Day 1 Afternoon)
8. Update all DTOs (8 files) - change IsVetted to computed
9. Update VettingService - remove manual IsVetted sync
10. Update UserManagementService - update queries
11. Update DashboardService - update logic
12. Update ParticipationService - update authorization
13. Run backend tests: `dotnet test apps/api.sln`
14. Fix failing tests systematically
15. Commit: "refactor: update services and DTOs for computed IsVetted"

#### Phase 3: Backend Tests (Day 2 Morning)
16. Update unit test assertions (15 files)
17. Update test builders (UserDtoBuilder)
18. Update integration test role checks
19. Run all backend tests: `dotnet test`
20. Achieve 100% pass rate
21. Commit: "test: update backend tests for simplified role model"

#### Phase 4: Frontend Updates (Day 2 Afternoon)
22. Regenerate API types: `npm run generate:api-types`
23. Review generated TypeScript types
24. Search for IsVetted usage in React: `grep -r "isVetted" apps/web/src/`
25. Update any problematic usages (should be minimal)
26. Run frontend tests: `npm test`
27. Build frontend: `npm run build`
28. Commit: "refactor: regenerate API types for simplified roles"

#### Phase 5: E2E Tests (Day 3 Morning)
29. Update E2E test data creation
30. Update E2E assertions
31. Run E2E tests: `npm run test:e2e`
32. Fix failures systematically
33. Commit: "test: update E2E tests for simplified role model"

#### Phase 6: Documentation (Day 3 Afternoon)
34. Update functional specifications
35. Update database documentation
36. Create developer guide
37. Create ADR
38. Update API documentation
39. Update file registry
40. Commit: "docs: update documentation for simplified role model"

#### Phase 7: Final Validation (Day 4)
41. Run complete test suite: `npm test && dotnet test && npm run test:e2e`
42. Manual testing: Login as each test user type
43. Verify admin members list works
44. Verify vetting workflow works
45. Verify safety incident access works
46. Check for console errors in browser
47. Review all commits for clarity
48. Create PR with detailed description

### Rollback Plan

**IF ISSUES FOUND** within 1 release cycle:

#### Option 1: Rollback Migration (Emergency)
```bash
# Rollback database migration
dotnet ef database update [previous-migration-name]

# Revert code changes
git revert [commit-hash-range]

# Re-deploy previous version
```

#### Option 2: Keep Computed Property Longer
- **DO NOTHING** - computed property already handles backward compatibility
- Monitor for issues
- Plan final removal for next release cycle

#### Option 3: Re-add IsVetted Column (Nuclear Option)
```csharp
// Create migration to re-add column
dotnet ef migrations add ReAddIsVettedColumn

protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<bool>(
        name: "IsVetted",
        table: "AspNetUsers",
        type: "boolean",
        nullable: false,
        defaultValue: false);

    // Populate from VettingStatus
    migrationBuilder.Sql(
        "UPDATE \"AspNetUsers\" SET \"IsVetted\" = CASE WHEN \"VettingStatus\" = 3 THEN true ELSE false END");
}
```

### Post-Deployment Monitoring (Week 1)

**Metrics to Watch**:
- ✅ Authentication success rate (should be unchanged)
- ✅ Authorization errors (should not increase)
- ✅ Vetting workflow completions (should be unchanged)
- ✅ Admin members list performance (should be unchanged or better)
- ✅ Safety incident access (should work correctly)

**Daily Checks**:
1. Review application logs for authorization errors
2. Check database for data integrity issues
3. Monitor user reports/support tickets
4. Verify computed property returns correct values

**Success Criteria** (End of Week 1):
- Zero authorization-related bugs
- All user workflows operational
- No increase in support tickets
- Clean application logs

---

## Validation Checklist

### Pre-Implementation Validation
- [x] All 120 files identified and categorized
- [ ] Database migration strategy confirmed
- [ ] Rollback plan documented and understood
- [ ] Test environment prepared
- [ ] Stakeholders notified of changes

### Phase 1: Database (Schema) Validation
- [ ] Migration created successfully
- [ ] Migration up executes without errors
- [ ] Migration down executes without errors (rollback test)
- [ ] ApplicationUser model compiles
- [ ] Computed property `IsVetted` returns correct values:
  - [ ] Returns `true` when VettingStatus == 3
  - [ ] Returns `false` when VettingStatus != 3
- [ ] Seed data creates exactly 3 roles (Administrator, Teacher, SafetyTeam)
- [ ] Test accounts created with correct Role values:
  - [ ] Admin: Role = "Administrator", VettingStatus = 3
  - [ ] Teacher: Role = "Teacher", VettingStatus = 3
  - [ ] Vetted: Role = "", VettingStatus = 3
  - [ ] Member: Role = "", VettingStatus = 0
  - [ ] Coordinator: Role = "SafetyTeam", VettingStatus = 3

### Phase 2: Backend Services Validation
- [ ] All 8 DTOs updated to compute IsVetted from VettingStatus
- [ ] VettingService no longer manually sets IsVetted
- [ ] UserManagementService queries work correctly
- [ ] DashboardService logic handles computed property
- [ ] ParticipationService authorization checks work
- [ ] No direct database assignments to IsVetted (grep verification)
- [ ] All services compile without errors
- [ ] Backend unit tests run (failures expected, will fix in Phase 3)

### Phase 3: Backend Tests Validation
- [ ] All unit tests updated for new model
- [ ] All unit tests pass (100% pass rate)
- [ ] Test builders updated (UserDtoBuilder)
- [ ] Integration tests updated
- [ ] Integration tests pass
- [ ] Mock helpers updated
- [ ] No assertions for removed roles (Member/Attendee/VettedMember)
- [ ] SafetyTeam role tests work correctly

### Phase 4: Frontend Validation
- [ ] API types regenerated successfully (`npm run generate:api-types`)
- [ ] TypeScript compilation succeeds (`npm run build`)
- [ ] Zero new TypeScript errors introduced
- [ ] IsVetted property still exists in generated types (computed)
- [ ] All components render without errors
- [ ] React unit tests pass
- [ ] No console errors in development mode

### Phase 5: E2E Tests Validation
- [ ] E2E test data creation updated
- [ ] E2E assertions updated
- [ ] All E2E tests pass
- [ ] Login flows work for all user types
- [ ] Admin members page works correctly
- [ ] Vetting system works correctly
- [ ] Safety incident access works correctly
- [ ] Dashboard displays correct content

### Phase 6: Documentation Validation
- [ ] Functional specifications updated
- [ ] Database documentation updated
- [ ] Developer guide created
- [ ] ADR created and linked
- [ ] API documentation reflects new DTOs
- [ ] Seed data documentation updated
- [ ] File registry updated with all changes
- [ ] Master index updated if needed

### Post-Deployment Validation (Production)
- [ ] Migration applied successfully to production database
- [ ] Zero authorization errors in logs (first 24 hours)
- [ ] All user workflows operational
- [ ] Admin members list functional
- [ ] Vetting workflow functional
- [ ] Safety incident management functional
- [ ] No increase in support tickets
- [ ] Performance metrics unchanged or improved

---

## Success Criteria

### Technical Success Criteria

#### 1. Zero References to IsVetted Boolean in Production Code
**Metric**: `grep -r "IsVetted" apps/api/ --include="*.cs" | grep -v "IsVetted =>" | wc -l` should return 0
**Validation**: Only computed property definitions (`IsVetted =>`) should exist

#### 2. Only 3 Roles in System
**Database Query**:
```sql
SELECT "Name" FROM "AspNetRoles" ORDER BY "Name";
-- Expected: Administrator, SafetyTeam, Teacher
-- Count: 3 rows
```

#### 3. All Authorization Checks Use VettingStatus or Role
**Pattern**: No boolean field checks, only:
- `user.VettingStatus == 3` (for vetted status)
- `user.Role == "Administrator"` (for admin permissions)
- `user.Role == "Teacher"` (for teacher permissions)
- `user.Role == "SafetyTeam"` (for safety permissions)

#### 4. All Tests Passing
**Metrics**:
- Backend unit tests: 100% pass rate
- Integration tests: 100% pass rate
- Frontend unit tests: 100% pass rate
- E2E tests: 100% pass rate on launch-critical workflows

#### 5. Admin Members Page Works Correctly
**Functional Validation**:
- ✅ Displays all users correctly
- ✅ Filters by VettingStatus work
- ✅ Shows computed IsVetted value accurately
- ✅ Role column shows correct permission roles
- ✅ No "Member" or "Attendee" roles displayed

#### 6. Vetting System Works Correctly
**Workflow Validation**:
- ✅ Application submission works
- ✅ Status transitions update VettingStatus
- ✅ Approval sets VettingStatus = 3
- ✅ IsVetted computes correctly after approval
- ✅ Denial sets VettingStatus = 4
- ✅ OnHold works without confusion

#### 7. No Confusion Between VettingStatus.OnHold and IsActive=false
**Documentation**: Clear developer guide explaining:
- `VettingStatus = 5` = Vetting process paused, user can still log in
- `IsActive = false` = Account suspended, user cannot log in

---

## Timeline & Resource Allocation

### Estimated Timeline

| Phase | Duration | Owner | Dependencies | Risk Level |
|-------|----------|-------|--------------|------------|
| **Phase 1: Database** | 2-3 hours | Database Designer + Backend Dev | None | LOW |
| **Phase 2: Backend Services** | 8-12 hours | Backend Developer | Phase 1 complete | MEDIUM |
| **Phase 3: Backend Tests** | 6-8 hours | Test Developer | Phase 2 complete | LOW |
| **Phase 4: Frontend** | 4-6 hours | React Developer | Phase 2 complete | LOW |
| **Phase 5: E2E Tests** | 4-6 hours | Test Developer | Phase 4 complete | MEDIUM |
| **Phase 6: Documentation** | 3-4 hours | Librarian + Backend Dev | All phases complete | LOW |
| **Total** | **27-39 hours** | Multiple | Sequential | MEDIUM |

**Calendar Timeline** (with 1 developer, 8-hour days):
- **Day 1**: Phase 1 + Start Phase 2
- **Day 2**: Complete Phase 2 + Phase 3
- **Day 3**: Phase 4 + Phase 5
- **Day 4**: Phase 6 + Final validation
- **Day 5**: Buffer for issues

**Calendar Timeline** (with team of 3, parallel work):
- **Day 1**: Phase 1 + 2 (Backend developer)
- **Day 2**: Phase 3 (Test developer) + Phase 4 (React developer)
- **Day 3**: Phase 5 (Test developer) + Phase 6 (Librarian)
- **Day 4**: Final validation (All)

### Resource Requirements

**Required Team Members**:
1. **Database Designer** (Phase 1) - 2-3 hours
2. **Backend Developer** (Phases 1, 2, 6) - 13-19 hours
3. **Test Developer** (Phases 3, 5) - 10-14 hours
4. **React Developer** (Phase 4) - 4-6 hours
5. **Librarian** (Phase 6) - 3-4 hours

**Total Effort**: 32-46 developer hours

---

## Appendix

### A. File Count Summary

**Total Files Affected**: 120 files (from grep search)

**Breakdown by Category**:
- Core Model & Data: 15 files
- Seed Data & Test Helpers: 5 files
- DTOs (Critical): 8 files
- Services: 10 files
- Unit Tests: 15 files
- Integration/E2E Tests: 5 files
- Frontend (estimated): 20+ files
- Documentation: 25 files
- Legacy/Archive (no changes): 17 files

### B. Key Decision Points

**Decision 1**: Remove IsVetted column vs keep as real column
- **Decision**: REMOVE and use computed property
- **Rationale**: Eliminates redundancy, VettingStatus is source of truth

**Decision 2**: Immediate removal vs phased removal
- **Decision**: PHASED - keep computed property for 1 release cycle
- **Rationale**: Safer migration, easier rollback if issues found

**Decision 3**: Update all 120 files vs leave some legacy
- **Decision**: UPDATE ALL except archived files
- **Rationale**: Complete migration prevents confusion

**Decision 4**: SafetyTeam in seed data or not
- **Decision**: ADD to seed data
- **Rationale**: Needed for safety coordinators to get correct role

### C. Testing Scenarios

**Scenario 1: Vetted Member Login**
```
Given: User with VettingStatus = 3 (Approved), Role = ""
When: User logs in
Then: IsVetted computes to true
And: User sees vetted member content
And: User has no special permissions
```

**Scenario 2: Admin Login**
```
Given: User with VettingStatus = 3, Role = "Administrator"
When: User logs in
Then: IsVetted computes to true
And: User sees admin interface
And: User has full system access
```

**Scenario 3: Unvetted Member**
```
Given: User with VettingStatus = 0 (UnderReview), Role = ""
When: User logs in
Then: IsVetted computes to false
And: User sees limited content
And: User sees vetting status information
```

**Scenario 4: Safety Coordinator**
```
Given: User with VettingStatus = 3, Role = "SafetyTeam"
When: User logs in
Then: IsVetted computes to true
And: User can access safety incident management
And: User cannot access admin features
```

**Scenario 5: Vetting Approval**
```
Given: User with VettingStatus = 0
When: Admin approves vetting application
Then: VettingStatus updates to 3
And: IsVetted computes to true automatically
And: No manual IsVetted update needed
```

### D. SQL Queries for Validation

**Check Role Distribution**:
```sql
SELECT "Role", COUNT(*) as UserCount
FROM "AspNetUsers"
GROUP BY "Role"
ORDER BY UserCount DESC;
```

**Check VettingStatus Distribution**:
```sql
SELECT "VettingStatus", COUNT(*) as UserCount
FROM "AspNetUsers"
GROUP BY "VettingStatus"
ORDER BY "VettingStatus";
```

**Verify Computed IsVetted Logic**:
```sql
SELECT
  "Email",
  "VettingStatus",
  CASE WHEN "VettingStatus" = 3 THEN 'true' ELSE 'false' END as ComputedIsVetted,
  "Role"
FROM "AspNetUsers"
ORDER BY "VettingStatus", "Email";
```

**Find Users with Empty Roles**:
```sql
SELECT "Email", "Role", "VettingStatus"
FROM "AspNetUsers"
WHERE "Role" = '' OR "Role" IS NULL
ORDER BY "VettingStatus" DESC;
```

### E. Communication Plan

**Stakeholders to Notify**:
1. **Development Team** - Technical changes, migration timeline
2. **QA Team** - Testing requirements, validation checklist
3. **Product Owner** - Business impact, benefits
4. **Support Team** - What changed, how to answer user questions

**Communication Timeline**:
- **T-1 week**: Initial notification of upcoming refactor
- **T-2 days**: Detailed technical briefing for developers
- **T-1 day**: Final confirmation, branch creation
- **T+0**: Implementation begins
- **T+4 days**: Completion notification, QA handoff
- **T+1 week**: Production deployment
- **T+2 weeks**: Post-deployment review

---

## Document Control

**Version History**:
- v1.0 (2025-10-19) - Initial comprehensive refactoring plan created

**Approvals Required**:
- [ ] Database Designer - Schema changes approved
- [ ] Backend Developer Lead - Service changes approved
- [ ] Test Developer Lead - Test strategy approved
- [ ] React Developer Lead - Frontend impact approved
- [ ] Product Owner - Business impact approved

**Related Documents**:
- Business Requirements: TBD
- Functional Specification: TBD
- Technical Design: This document
- Test Plan: TBD
- Deployment Plan: TBD

**File Registry Entry**:
- Date: 2025-10-19
- File: `/docs/functional-areas/user-management/new-work/2025-10-19-role-vetting-refactoring/REFACTORING-PLAN.md`
- Action: CREATED
- Purpose: Comprehensive refactoring plan for role & vetting status simplification
- Status: ACTIVE
- Cleanup: Never (permanent planning document)
