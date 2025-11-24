# Duplicate Scene Name Implementation Options

**Date**: 2025-11-23
**Agent**: Librarian
**Task**: Design options analysis for allowing duplicate scene names
**Related Documents**:
- [Database Schema Investigation Report](./database-schema-investigation-report.md)
- Entity Model: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`
- EF Configuration: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`
- Auth Service: `/home/chad/repos/witchcityrope/apps/api/Features/Authentication/Services/AuthenticationService.cs`
- Auth Endpoints: `/home/chad/repos/witchcityrope/apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs`

---

## Executive Summary

**Current State**:
- SceneName has a **database-level UNIQUE constraint** (IX_Users_SceneName index)
- Email has **NO database constraint** - uniqueness enforced by ASP.NET Core Identity's UserManager at application layer
- Login supports **BOTH email and scene name** (email checked first via index, scene name as fallback via case-insensitive ILIKE query)
- Registration prevents duplicate scene names with error "Scene name is already taken"

**Desired State**:
- Allow duplicate scene names during registration
- Modify login to detect duplicate scene names and require email in that case
- Maintain email as the unique identifier for authentication
- Preserve backward compatibility with existing users

**Recommended Approach**: **Option A - Simple Duplicate Detection** (8.5/10)
- Remove unique constraint from SceneName
- Add duplicate detection logic to login flow
- Minimal code changes, maximum simplicity
- Implementation time: 4-6 hours

---

## Verified User Assumptions

Based on database schema investigation, here are the verified facts about the current implementation:

| Assumption | Verification Result | Evidence |
|------------|-------------------|----------|
| "SceneName is unique" | ✅ **CONFIRMED** | Database has `CREATE UNIQUE INDEX "IX_Users_SceneName"` (Migration 20251108200319_InitialSchema.cs) |
| "Email is the unique identifier" | ⚠️ **PARTIALLY CORRECT** | Email uniqueness is enforced at **application layer** by UserManager, NOT at database level. Database only has non-unique `EmailIndex` on `NormalizedEmail` for performance |
| "Login uses email" | ✅ **CONFIRMED** | AuthenticationService.LoginAsync() checks email first (line 107), then scene name as fallback (lines 110-115) |
| "Registration prevents duplicate scene names" | ✅ **CONFIRMED** | AuthenticationService.RegisterAsync() checks for duplicate scene names (lines 219-227) and returns error "Scene name is already taken" |
| "Login uses scene name as fallback" | ✅ **CONFIRMED** | AuthenticationService.LoginAsync() performs case-insensitive ILIKE query on SceneName if email not found (lines 110-115) |

**Key Discovery**: Email uniqueness is NOT enforced at database level, only at application layer via ASP.NET Core Identity's UserManager. This is intentional and follows Microsoft's Identity framework design pattern.

---

## Implementation Options

### Option A: Simple Duplicate Detection (RECOMMENDED)

**Approach**: Remove unique constraint, detect duplicates at login time, show friendly error message.

#### Database Schema Changes

1. **Create Migration** to drop unique constraint:
```csharp
// Migration: RemoveSceneNameUniqueConstraint
public partial class RemoveSceneNameUniqueConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the unique index
        migrationBuilder.DropIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            schema: "public");

        // Create non-unique index for performance
        migrationBuilder.CreateIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            column: "SceneName",
            schema: "public");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback: Restore unique constraint
        // NOTE: This will FAIL if duplicate scene names exist
        migrationBuilder.DropIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            schema: "public");

        migrationBuilder.CreateIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            column: "SceneName",
            unique: true,
            schema: "public");
    }
}
```

2. **Update EF Core Configuration** (`ApplicationDbContext.cs`, lines 372-374):
```csharp
// BEFORE:
entity.HasIndex(e => e.SceneName)
    .IsUnique()  // Remove this
    .HasDatabaseName("IX_Users_SceneName");

// AFTER:
entity.HasIndex(e => e.SceneName)
    .IsUnique(false)  // Explicitly false for clarity
    .HasDatabaseName("IX_Users_SceneName");
```

#### Backend Code Changes

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Authentication/Services/AuthenticationService.cs`

**Method**: `LoginAsync()` (lines 88-191)

```csharp
// CURRENT IMPLEMENTATION (lines 106-121):
// Try to find user by email first (most common case, indexed)
var user = await _userManager.FindByEmailAsync(request.EmailOrSceneName);

// If not found by email, try by scene name (case-insensitive)
if (user == null)
{
    user = await _context.Users.FirstOrDefaultAsync(
        u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName),
        cancellationToken);
}

if (user == null)
{
    _logger.LogWarning("Login attempt with non-existent email or scene name: {Identifier}", request.EmailOrSceneName);
    return (false, null, "Invalid email/scene name or password");
}

// PROPOSED IMPLEMENTATION (lines 106-135):
// Try to find user by email first (most common case, indexed)
var user = await _userManager.FindByEmailAsync(request.EmailOrSceneName);

// If not found by email, try by scene name (case-insensitive)
if (user == null)
{
    // Check for duplicate scene names
    var usersWithSceneName = await _context.Users
        .Where(u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName))
        .ToListAsync(cancellationToken);

    if (usersWithSceneName.Count == 0)
    {
        // No user found with this scene name or email
        _logger.LogWarning("Login attempt with non-existent email or scene name: {Identifier}", request.EmailOrSceneName);
        return (false, null, "Invalid email/scene name or password");
    }
    else if (usersWithSceneName.Count == 1)
    {
        // Exactly one user with this scene name - proceed normally
        user = usersWithSceneName[0];
    }
    else // usersWithSceneName.Count > 1
    {
        // Multiple users with this scene name - require email
        _logger.LogInformation("Login attempt with duplicate scene name: {SceneName}", request.EmailOrSceneName);
        return (false, null, "This scene name is used by multiple members. Please log in with your email address instead.");
    }
}

if (user == null)
{
    // This should never happen after the above logic, but keeping for safety
    _logger.LogWarning("Login attempt failed for unknown reason: {Identifier}", request.EmailOrSceneName);
    return (false, null, "Invalid email/scene name or password");
}

// Continue with existing password validation logic (lines 123-191 unchanged)...
```

**Method**: `RegisterAsync()` (lines 197-291)

```csharp
// CURRENT IMPLEMENTATION (lines 219-227):
// Check if scene name already exists using direct Entity Framework
var existingSceneName = await _context.Users
    .AsNoTracking()
    .AnyAsync(u => u.SceneName == request.SceneName, cancellationToken);

if (existingSceneName)
{
    return (false, null, "Scene name is already taken");
}

// PROPOSED IMPLEMENTATION (lines 219-223):
// Remove scene name uniqueness check entirely
// (Delete lines 219-227)
// Scene names can now be duplicated

// Continue with user creation (lines 229-291 unchanged)...
```

**Update Documentation** in `ApplicationUser.cs` (line 30-32):
```csharp
// BEFORE:
/// <summary>
/// Scene name for the rope bondage community
/// Required field, must be unique
/// </summary>

// AFTER:
/// <summary>
/// Scene name for the rope bondage community
/// Required field, may be duplicated (use email for unique identification)
/// </summary>
```

#### Frontend Changes

**File**: `/home/chad/repos/witchcityrope/apps/web/src/features/auth/components/LoginForm.tsx`

**Changes Needed**:
1. Update error message display to show the new "Please log in with your email address" message clearly
2. Consider adding helper text: "Having trouble? Try using your email address instead of scene name"
3. No validation logic changes needed - backend handles duplicate detection

**Example Error Message Enhancement**:
```tsx
{error && (
  <Alert color="red" title="Login Failed">
    {error}
    {error.includes("multiple members") && (
      <Text size="sm" mt="xs" color="dimmed">
        Tip: You can always log in with your email address.
      </Text>
    )}
  </Alert>
)}
```

**File**: `/home/chad/repos/witchcityrope/apps/web/src/features/auth/components/RegisterForm.tsx`

**Changes Needed**:
1. Remove client-side scene name uniqueness validation (if any exists)
2. Update help text to indicate scene names don't need to be unique
3. Consider adding: "Scene names don't need to be unique. Your email is your unique identifier."

#### Performance Implications

**Current Performance**:
- Email lookup: Fast (indexed, unique constraint means database stops after finding first match)
- Scene name lookup: Fast (indexed, unique constraint means database stops after finding first match)

**New Performance**:
- Email lookup: **No change** (still fast, indexed)
- Scene name lookup (non-duplicate): Fast (index scan, typically finds one result quickly)
- Scene name lookup (duplicate): Slightly slower (must scan all matching rows, but:
  - Scene names are still indexed (non-unique)
  - Case-insensitive ILIKE query already used
  - Expected duplicate count: Low (2-3 users max per scene name)
  - Query returns immediately after finding 2+ matches

**Performance Assessment**: Negligible impact. Scene name duplicates are expected to be rare, and indexed queries will still be fast.

#### Pros and Cons

**Pros** ✅:
- **Simple Implementation**: Minimal code changes (30 lines in one method)
- **Clear User Experience**: Error message explicitly guides users to use email
- **Backward Compatible**: Existing unique scene names work exactly as before
- **No Data Migration**: Existing users unaffected
- **Fail-Safe**: If database query fails, login safely denies access
- **Standard Pattern**: Similar to how other platforms handle duplicate usernames

**Cons** ❌:
- **Slightly Slower Login**: Users with duplicate scene names must type email instead
- **No Proactive Warning**: Users don't know scene name is duplicated until login attempt
- **Potential Confusion**: Users might wonder why scene name login sometimes works and sometimes doesn't

**Risk Assessment**: **LOW**
- No database-level changes to email uniqueness (already handled at app layer)
- Login logic change is isolated and well-tested pattern
- Rollback is simple (restore unique constraint if no duplicates exist)

---

### Option B: Flag-Based Approach with Proactive Warning

**Approach**: Track whether scene names are unique via database flag, warn users proactively.

#### Database Schema Changes

1. **Add IsSceneNameUnique column** to Users table:
```csharp
// Migration: AddSceneNameUniqueFlag
public partial class AddSceneNameUniqueFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the unique index
        migrationBuilder.DropIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            schema: "public");

        // Add flag column
        migrationBuilder.AddColumn<bool>(
            name: "IsSceneNameUnique",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true,
            schema: "public");

        // Create non-unique index
        migrationBuilder.CreateIndex(
            name: "IX_Users_SceneName",
            table: "Users",
            column: "SceneName",
            schema: "public");

        // Add index on flag for performance
        migrationBuilder.CreateIndex(
            name: "IX_Users_IsSceneNameUnique",
            table: "Users",
            column: "IsSceneNameUnique",
            schema: "public");
    }
}
```

2. **Update Entity Model** (`ApplicationUser.cs`):
```csharp
/// <summary>
/// Indicates if this user's scene name is unique
/// False if multiple users share this scene name
/// </summary>
public bool IsSceneNameUnique { get; set; } = true;
```

3. **Add Background Job** to update flags:
```csharp
// Run after each registration to update flags
public async Task UpdateSceneNameUniquenessFlags(string sceneName)
{
    var usersWithSceneName = await _context.Users
        .Where(u => u.SceneName == sceneName)
        .ToListAsync();

    bool isUnique = usersWithSceneName.Count == 1;

    foreach (var user in usersWithSceneName)
    {
        user.IsSceneNameUnique = isUnique;
    }

    await _context.SaveChangesAsync();
}
```

#### Backend Code Changes

**File**: `AuthenticationService.cs`

**LoginAsync()** method:
```csharp
// Simplified login logic
if (user == null)
{
    user = await _context.Users.FirstOrDefaultAsync(
        u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName),
        cancellationToken);
}

if (user == null)
{
    return (false, null, "Invalid email/scene name or password");
}

// Check flag instead of counting duplicates
if (!user.IsSceneNameUnique)
{
    return (false, null, "This scene name is used by multiple members. Please log in with your email address instead.");
}

// Continue with password validation...
```

**RegisterAsync()** method:
```csharp
// After successful registration
var user = new ApplicationUser
{
    // ... existing fields ...
    IsSceneNameUnique = true  // Assume unique initially
};

var result = await _userManager.CreateAsync(user, request.Password);

if (result.Succeeded)
{
    // Update uniqueness flags for all users with this scene name
    await UpdateSceneNameUniquenessFlags(user.SceneName);
}
```

**New API Endpoint** to check scene name status:
```csharp
app.MapGet("/api/auth/scene-name-status/{sceneName}", async (
    string sceneName,
    ApplicationDbContext context) =>
{
    var count = await context.Users
        .Where(u => EF.Functions.ILike(u.SceneName, sceneName))
        .CountAsync();

    return Results.Ok(new
    {
        SceneName = sceneName,
        IsUnique = count <= 1,
        Count = count
    });
});
```

#### Frontend Changes

**Enhanced User Experience**:
1. **Registration Form**: Real-time scene name uniqueness check
2. **Profile Page**: Show badge indicating "Unique Scene Name" or "Shared Scene Name"
3. **Login Form**: Proactive message if user's scene name is known to be duplicated

**Example Registration Form Enhancement**:
```tsx
const { data: sceneNameStatus } = useQuery({
  queryKey: ['scene-name-status', sceneName],
  queryFn: () => api.get(`/api/auth/scene-name-status/${sceneName}`),
  enabled: sceneName.length > 2
});

{sceneNameStatus?.IsUnique === false && (
  <Alert color="yellow" title="Scene Name Already Exists">
    This scene name is used by other members. You can still register,
    but you'll need to log in with your email address.
  </Alert>
)}
```

#### Performance Implications

**Concerns**:
- **Additional Database Column**: 1 boolean per user (minimal storage)
- **Additional Index**: Small performance cost on writes
- **Background Job**: Runs after each registration (affects 1-2 users typically)
- **API Endpoint**: Additional query during registration form validation

**Optimization**:
- Flag updates are O(n) where n = number of users with scene name (typically 1-3)
- Index on flag improves queries filtering by uniqueness
- API endpoint could be cached for popular scene names

**Performance Assessment**: Small overhead on registration, faster login for duplicate scene names (no need to count), better UX through proactive warnings.

#### Pros and Cons

**Pros** ✅:
- **Proactive User Experience**: Users know scene name is duplicated before attempting login
- **Faster Duplicate Detection**: No need to count users at login time (flag is pre-computed)
- **Profile Enhancement**: Can show uniqueness status on profile page
- **Real-time Validation**: Registration form can warn about duplicates
- **Better Analytics**: Can track how many users have duplicate scene names

**Cons** ❌:
- **More Complex Implementation**: 50+ lines of code, new background job, new API endpoint
- **Database Migration**: Adds column and index (though still backward compatible)
- **Maintenance Overhead**: Flag must be kept in sync (potential for inconsistency)
- **Edge Cases**: Flag update failure could cause incorrect login behavior
- **Testing Complexity**: Must test flag update logic, background job, API endpoint

**Risk Assessment**: **MEDIUM**
- Additional complexity increases chance of bugs
- Flag synchronization could fail under high concurrency
- Background job adds system dependencies
- Rollback requires removing column and indexes

---

### Option C: Email-Only Login with Scene Name Display

**Approach**: Remove scene name from login entirely, use it only for display purposes.

#### Database Schema Changes

**Same as Option A**:
- Remove unique constraint from SceneName
- Keep non-unique index for queries

#### Backend Code Changes

**File**: `AuthenticationService.cs`

**LoginAsync()** method - **Major Simplification**:
```csharp
// DRASTICALLY SIMPLIFIED LOGIN (lines 106-121):
// Only support email login - scene name is display-only
var user = await _userManager.FindByEmailAsync(request.EmailOrSceneName);

if (user == null)
{
    // Check if they entered a scene name instead of email
    var sceneNameExists = await _context.Users
        .AnyAsync(u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName), cancellationToken);

    if (sceneNameExists)
    {
        _logger.LogInformation("Login attempt with scene name instead of email: {SceneName}", request.EmailOrSceneName);
        return (false, null, "Please log in with your email address, not your scene name.");
    }

    _logger.LogWarning("Login attempt with non-existent email: {Email}", request.EmailOrSceneName);
    return (false, null, "Invalid email or password");
}

// Continue with password validation (lines 123-191 unchanged)...
```

**RegisterAsync()** method:
```csharp
// Remove scene name uniqueness check (same as Option A)
// Scene names are purely for display now
```

#### Frontend Changes

**Major UI Changes Required**:

1. **Login Form** - Update placeholder and labels:
```tsx
<TextInput
  label="Email Address"  // Changed from "Email or Scene Name"
  placeholder="your.email@example.com"  // Changed from "Email or Scene Name"
  required
  {...form.getInputProps('email')}
/>

<Text size="sm" color="dimmed" mt="xs">
  Note: Login requires your email address. Scene names are for display only.
</Text>
```

2. **Registration Form** - Update messaging:
```tsx
<TextInput
  label="Scene Name"
  description="Your display name in the community. This doesn't need to be unique."
  placeholder="YourSceneName"
  required
  {...form.getInputProps('sceneName')}
/>
```

3. **Password Reset** - Already email-only (no changes needed)

4. **User Profile** - Emphasize email as login identifier:
```tsx
<Alert color="blue" title="Login Information">
  <Text>Email: {user.email} (use this to log in)</Text>
  <Text>Scene Name: {user.sceneName} (display only)</Text>
</Alert>
```

#### Performance Implications

**Best Performance**:
- **Fastest Login**: Only one database query (email lookup via indexed EmailIndex)
- **No Duplicate Detection**: No need to count scene name matches
- **Simplest Query Path**: Direct index lookup, no fallback logic

**Performance Assessment**: Fastest option. Single indexed query, no conditional logic, no duplicate checking.

#### Pros and Cons

**Pros** ✅:
- **Simplest Code**: Minimal backend logic (10 lines of code)
- **Fastest Login**: Only one database query required
- **Clearest UX**: No confusion about what to enter (always email)
- **No Duplicate Issues**: Scene names are display-only, duplicates don't matter
- **Industry Standard**: Most modern platforms use email-only login
- **Easier Support**: Support staff always know to ask for email
- **Better Security**: Email-only login reduces phishing attack surface

**Cons** ❌:
- **User Expectation Violation**: Users accustomed to scene name login will be confused
- **Convenience Loss**: Some users prefer logging in with memorable scene name vs email
- **Community Culture**: Rope bondage community may value scene name privacy (prefer not typing email on public devices)
- **Breaking Change**: Existing users who only remember their scene name will struggle
- **Migration Communication**: Requires clear user communication about change
- **Potential Resistance**: Community may reject email-only requirement

**Risk Assessment**: **HIGH (User Experience)**
- Significant change to established user behavior
- May frustrate existing users who prefer scene name login
- Requires community buy-in and clear communication
- Potential for user complaints and support tickets
- May not align with community culture/values

---

## Migration Strategy

### For All Options

**Pre-Migration Checks**:
1. **Verify Current Uniqueness**: Confirm no duplicate scene names exist currently
```sql
SELECT "SceneName", COUNT(*)
FROM "public"."Users"
GROUP BY "SceneName"
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

2. **Backup Database**: Create full backup before migration
```bash
# Production backup command
pg_dump -h $DB_HOST -U $DB_USER -d witchcityrope > backup_before_scenename_migration_$(date +%Y%m%d).sql
```

3. **Communication Plan**:
   - Email to all users explaining change
   - In-app notification about new login behavior
   - FAQ document addressing common questions
   - Support team briefing on expected user questions

### Option A Migration Steps

**Step 1: Database Migration** (5 minutes)
```bash
# Development
dotnet ef migrations add RemoveSceneNameUniqueConstraint
dotnet ef database update

# Staging
dotnet ef database update --connection "Host=staging..."

# Production (after staging validation)
dotnet ef database update --connection "Host=production..."
```

**Step 2: Backend Code Deployment** (10 minutes)
```bash
# Deploy updated AuthenticationService.cs
git add apps/api/Features/Authentication/Services/AuthenticationService.cs
git add apps/api/Models/ApplicationUser.cs
git commit -m "feat(auth): allow duplicate scene names with email fallback requirement"
git push origin main

# Trigger staging deployment
# Verify staging tests pass
# Trigger production deployment
```

**Step 3: Frontend Code Deployment** (10 minutes)
```bash
# Deploy updated LoginForm.tsx and RegisterForm.tsx
git add apps/web/src/features/auth/components/LoginForm.tsx
git add apps/web/src/features/auth/components/RegisterForm.tsx
git commit -m "feat(auth): update login form messaging for duplicate scene names"
git push origin main

# Frontend deploys automatically via CI/CD
```

**Step 4: Testing & Validation** (30 minutes)
1. Create test users with duplicate scene names in staging
2. Verify login behavior:
   - Unique scene name login: Works
   - Duplicate scene name login: Shows error message
   - Email login with duplicate scene name: Works
3. Verify registration: Allows duplicate scene names
4. Monitor error logs for unexpected issues

**Step 5: Monitoring** (24 hours)
- Monitor login error rates
- Track "duplicate scene name" error frequency
- Watch support ticket volume
- Check for unexpected authentication failures

### Option B Migration Steps

**Additional Steps Beyond Option A**:
1. Deploy background job infrastructure
2. Initial flag population (run UPDATE query on all existing users)
3. Test background job execution
4. Deploy new API endpoint
5. Update frontend to use new endpoint
6. Verify flag synchronization under load

**Estimated Time**: 2-3 hours (vs 1 hour for Option A)

### Option C Migration Steps

**Additional Steps Beyond Option A**:
1. User communication campaign (2 weeks notice)
2. In-app warnings about upcoming change
3. Grace period with helpful error messages
4. Gradual rollout: Staging → 10% production → 100% production
5. Support team training on handling user complaints

**Estimated Time**: 2-4 weeks (includes communication period)

### Rollback Plan

**Option A/B Rollback**:
```sql
-- Check for duplicate scene names
SELECT "SceneName", COUNT(*) as "Count"
FROM "public"."Users"
GROUP BY "SceneName"
HAVING COUNT(*) > 1;

-- If NO duplicates exist, can safely rollback:
CREATE UNIQUE INDEX "IX_Users_SceneName" ON "public"."Users" ("SceneName");

-- If duplicates exist, must resolve them first:
-- Option 1: Suffix duplicates (SceneName_2, SceneName_3)
-- Option 2: Manual resolution with user contact
-- Option 3: Force email-only login until resolved
```

**Option C Rollback**:
```csharp
// Restore scene name login logic
// Redeploy previous version of AuthenticationService.cs
// Frontend changes are additive (more permissive), so safe to keep
```

**Rollback Risk Assessment**:
- **Option A**: Low risk if no duplicates created yet
- **Option B**: Medium risk (must also rollback flags)
- **Option C**: High risk (user behavior already changed)

---

## Test Impact Analysis

### Test Categories Affected

#### 1. **Authentication Integration Tests**
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Authentication/Tests/`

**Tests Requiring Updates**:

**AuthenticationServiceTests.cs**:
```csharp
[Fact]
public async Task LoginAsync_WithSceneName_WhenUnique_ShouldSucceed()
{
    // Arrange: Create user with unique scene name
    // Act: Login with scene name
    // Assert: Login succeeds
}

[Fact]
public async Task LoginAsync_WithSceneName_WhenDuplicate_ShouldRequireEmail()
{
    // Arrange: Create TWO users with same scene name
    // Act: Login with scene name
    // Assert: Returns error "Please log in with your email address instead"
}

[Fact]
public async Task LoginAsync_WithEmail_WhenSceneNameDuplicate_ShouldSucceed()
{
    // Arrange: Create TWO users with same scene name
    // Act: Login with email (not scene name)
    // Assert: Login succeeds for correct user
}

[Fact]
public async Task RegisterAsync_WithDuplicateSceneName_ShouldSucceed()
{
    // Arrange: Create user with scene name "TestUser"
    // Act: Register new user with same scene name "TestUser"
    // Assert: Registration succeeds (no more "Scene name is already taken" error)
}

[Fact]
public async Task RegisterAsync_WithDuplicateSceneName_BothUsersCanLoginWithEmail()
{
    // Arrange: Register two users with same scene name
    // Act: Login both users with their respective emails
    // Assert: Both logins succeed
}
```

**Tests Requiring Deletion**:
```csharp
// REMOVE THIS TEST (no longer applicable):
[Fact]
public async Task RegisterAsync_WithDuplicateSceneName_ShouldFail()
{
    // This test expects "Scene name is already taken" error
    // But we now ALLOW duplicate scene names
}
```

#### 2. **Frontend Unit Tests**
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/auth/__tests__/`

**Tests Requiring Updates**:

**LoginForm.test.tsx**:
```tsx
it('should show friendly error when scene name is duplicated', async () => {
  // Arrange: Mock API to return duplicate scene name error
  mockApiError('This scene name is used by multiple members. Please log in with your email address instead.');

  // Act: Submit login form with scene name

  // Assert: Error message displayed with helper text about using email
});

it('should allow login with email when scene name is duplicated', async () => {
  // Arrange: Mock successful login with email

  // Act: Submit login form with email

  // Assert: Login succeeds, user redirected to dashboard
});
```

**RegisterForm.test.tsx**:
```tsx
it('should NOT show error for duplicate scene name', async () => {
  // Arrange: Mock API response allowing duplicate scene name

  // Act: Submit registration form with existing scene name

  // Assert: Registration succeeds without error
});

// REMOVE THIS TEST (no longer applicable):
it('should show error for duplicate scene name', async () => {
  // This test expects registration to fail with duplicate scene name
  // But we now ALLOW duplicate scene names
});
```

#### 3. **End-to-End Tests (Playwright)**
**Location**: `/home/chad/repos/witchcityrope/tests/`

**New Test Scenarios Required**:

**duplicate-scene-name-login.spec.ts**:
```typescript
test.describe('Duplicate Scene Name Login Flow', () => {
  test('should reject login with duplicate scene name and suggest email', async ({ page }) => {
    // Arrange: Create two users with same scene name via API
    await createTestUser({ email: 'user1@test.com', sceneName: 'DuplicateName' });
    await createTestUser({ email: 'user2@test.com', sceneName: 'DuplicateName' });

    // Act: Navigate to login page
    await page.goto('/login');
    await page.fill('[name="emailOrSceneName"]', 'DuplicateName');
    await page.fill('[name="password"]', 'Test123!');
    await page.click('[type="submit"]');

    // Assert: Error message displayed
    await expect(page.locator('text=multiple members')).toBeVisible();
    await expect(page.locator('text=log in with your email address')).toBeVisible();
  });

  test('should allow login with email when scene name is duplicated', async ({ page }) => {
    // Arrange: Create two users with same scene name
    await createTestUser({ email: 'user1@test.com', sceneName: 'DuplicateName' });
    await createTestUser({ email: 'user2@test.com', sceneName: 'DuplicateName' });

    // Act: Login with email
    await page.goto('/login');
    await page.fill('[name="emailOrSceneName"]', 'user1@test.com');
    await page.fill('[name="password"]', 'Test123!');
    await page.click('[type="submit"]');

    // Assert: Login succeeds, redirected to dashboard
    await expect(page).toHaveURL('/dashboard');
    await expect(page.locator('text=DuplicateName')).toBeVisible();  // Scene name displayed
  });

  test('should allow registration with duplicate scene name', async ({ page }) => {
    // Arrange: Create user with scene name
    await createTestUser({ email: 'first@test.com', sceneName: 'PopularName' });

    // Act: Register new user with same scene name
    await page.goto('/register');
    await page.fill('[name="email"]', 'second@test.com');
    await page.fill('[name="sceneName"]', 'PopularName');
    await page.fill('[name="password"]', 'Test123!');
    await page.click('[type="submit"]');

    // Assert: Registration succeeds
    await expect(page.locator('text=Registration successful')).toBeVisible();
  });
});
```

#### 4. **Database Integration Tests**
**Location**: Test infrastructure setup

**New Test Data Scenarios**:
```csharp
// Test fixture with duplicate scene names
public class DuplicateSceneNameTestFixture
{
    public async Task<(ApplicationUser user1, ApplicationUser user2)> CreateUsersWithDuplicateSceneName()
    {
        var user1 = new ApplicationUser
        {
            Email = "user1@test.com",
            UserName = "user1@test.com",
            SceneName = "SharedName",
            EmailConfirmed = true
        };

        var user2 = new ApplicationUser
        {
            Email = "user2@test.com",
            UserName = "user2@test.com",
            SceneName = "SharedName",  // Same scene name
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user1, "Test123!");
        await userManager.CreateAsync(user2, "Test123!");

        return (user1, user2);
    }
}
```

### Test Execution Strategy

**Phase 1: Unit Tests** (30 minutes)
```bash
# Run backend authentication tests
cd apps/api
dotnet test --filter "Category=Authentication"

# Run frontend auth component tests
cd apps/web
npm test -- --testPathPattern=auth
```

**Phase 2: Integration Tests** (1 hour)
```bash
# Run backend integration tests with real database
cd apps/api
dotnet test --filter "Category=Integration&Category=Authentication"

# Verify database state after tests
psql -d witchcityrope_test -c "SELECT \"SceneName\", COUNT(*) FROM \"Users\" GROUP BY \"SceneName\" HAVING COUNT(*) > 1;"
```

**Phase 3: E2E Tests** (1 hour)
```bash
# Start test environment
docker-compose -f docker-compose.test.yml up -d

# Run tests - Use test-catalog-updater skill for test execution and reporting
# The skill handles: test execution, result reporting, and catalog updates
```

**Phase 4: Manual Testing** (30 minutes)
1. Staging environment smoke tests
2. Create duplicate scene name users manually
3. Test login flows with different browsers
4. Verify error messages are clear and helpful

### Expected Test Results

**Before Changes**:
- Registration with duplicate scene name: ❌ Fails with "Scene name is already taken"
- Login with scene name: ✅ Works (because all scene names are unique)
- Login with email: ✅ Works

**After Changes (Option A)**:
- Registration with duplicate scene name: ✅ Succeeds
- Login with unique scene name: ✅ Works (backward compatible)
- Login with duplicate scene name: ❌ Fails with friendly error message
- Login with email (duplicate scene name user): ✅ Works

**Test Coverage Goals**:
- Unit Tests: 100% coverage of LoginAsync and RegisterAsync methods
- Integration Tests: 90% coverage of authentication flows
- E2E Tests: 100% coverage of critical user paths

---

## Recommended Approach

### **Option A: Simple Duplicate Detection** (8.5/10)

**Recommendation Rationale**:

1. **Simplicity** (9/10):
   - 30 lines of code change in one method
   - No new database columns or background jobs
   - Easy to understand and maintain
   - Clear error messages guide users

2. **User Experience** (8/10):
   - Backward compatible: Existing users unaffected
   - Clear guidance when duplicate detected
   - Minimal disruption to login flow
   - Users can still choose unique scene names if they prefer

3. **Performance** (9/10):
   - Negligible performance impact
   - Indexed query remains fast
   - Only affects duplicate scene name users (expected to be rare)

4. **Risk** (9/10):
   - Low implementation risk
   - Easy rollback if issues arise
   - No data migration required
   - Well-tested pattern

5. **Time to Implement** (9/10):
   - 4-6 hours total implementation time
   - 1 hour testing and validation
   - Can be completed in single day

6. **Community Fit** (8/10):
   - Respects scene name culture (allows duplicates)
   - Maintains email privacy option (login with email)
   - Doesn't force breaking changes on existing users
   - Gradual adoption: Users choose duplicates if desired

**Why Not Option B?** (7.5/10):
- More complex (50+ lines, background job, new API endpoint)
- Flag synchronization adds potential failure points
- Marginal UX benefit (proactive warning) doesn't justify complexity
- Increased testing and maintenance overhead
- Performance overhead on registration

**Why Not Option C?** (6/10):
- Breaking change for existing users
- Violates user expectations and community culture
- High risk of user frustration and complaints
- Requires extensive communication campaign
- May not align with rope bondage community's preference for scene name privacy

---

## Implementation Checklist for Option A

### Pre-Implementation
- [ ] Review current SceneName usage across codebase
- [ ] Verify no existing duplicate scene names in production database
- [ ] Create backup of production database
- [ ] Communicate upcoming change to users (1 week notice)
- [ ] Brief support team on expected user questions

### Database Changes
- [ ] Create migration `RemoveSceneNameUniqueConstraint`
- [ ] Update `ApplicationDbContext.cs` EF Core configuration
- [ ] Test migration on development database
- [ ] Test migration on staging database
- [ ] Verify rollback works on development database

### Backend Changes
- [ ] Update `LoginAsync()` method with duplicate detection logic
- [ ] Update `RegisterAsync()` method (remove uniqueness check)
- [ ] Update `ApplicationUser.cs` documentation
- [ ] Add comprehensive logging for duplicate detection events
- [ ] Update API documentation (Swagger/OpenAPI)

### Frontend Changes
- [ ] Update `LoginForm.tsx` error message display
- [ ] Add helper text about email login option
- [ ] Update `RegisterForm.tsx` scene name help text
- [ ] Remove client-side scene name uniqueness validation (if exists)
- [ ] Update user-facing documentation

### Testing
- [ ] Write unit tests for duplicate scene name detection
- [ ] Write unit tests for registration with duplicate scene names
- [ ] Update existing tests that expect uniqueness errors
- [ ] Write integration tests for duplicate login scenarios
- [ ] Write E2E tests for duplicate scene name user flows
- [ ] Execute full test suite in staging environment

### Deployment
- [ ] Deploy to staging environment
- [ ] Create test users with duplicate scene names in staging
- [ ] Manual testing of all scenarios in staging
- [ ] Monitor staging logs for 24 hours
- [ ] Deploy to production during low-traffic window
- [ ] Monitor production logs for 48 hours

### Post-Deployment
- [ ] Verify no authentication errors in production logs
- [ ] Track frequency of "duplicate scene name" errors
- [ ] Monitor support ticket volume for auth issues
- [ ] Collect user feedback about new behavior
- [ ] Update FAQ with common questions
- [ ] Document lessons learned

### Documentation
- [ ] Update authentication documentation
- [ ] Update user guide with login recommendations
- [ ] Create internal wiki page for support team
- [ ] Document rollback procedure
- [ ] Update architecture diagrams

---

## Appendix: Technical Considerations

### PostgreSQL Specifics

**Case Sensitivity**:
- Current implementation uses `EF.Functions.ILike()` (case-insensitive)
- Scene name "RopeNinja" matches "ropeninja", "ROPENINJA", "RopeNinja"
- This behavior is preserved in all options

**Index Performance**:
- Non-unique indexes still provide performance benefit
- B-tree index structure supports efficient range scans
- ILIKE queries can use indexes with proper collation settings

**Constraint Verification**:
```sql
-- Verify unique constraint removed
SELECT conname, contype
FROM pg_constraint
WHERE conrelid = 'Users'::regclass
  AND contype = 'u'
  AND conname = 'IX_Users_SceneName';
-- Should return 0 rows after migration

-- Verify index still exists (non-unique)
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Users'
  AND indexname = 'IX_Users_SceneName';
-- Should return 1 row with indexdef NOT containing "UNIQUE"
```

### ASP.NET Core Identity Integration

**UserManager Behavior**:
- Email uniqueness still enforced by `UserManager.CreateAsync()`
- No changes needed to Identity configuration
- `FindByEmailAsync()` remains fast (indexed)
- Login lockout behavior unchanged

**SignInManager Behavior**:
- `CheckPasswordSignInAsync()` validates password after user lookup
- Lockout tracking continues to work (per user, not per scene name)
- Security stamp validation unchanged

### Entity Framework Core Patterns

**Query Performance**:
```csharp
// GOOD: Efficient duplicate detection
var users = await _context.Users
    .Where(u => EF.Functions.ILike(u.SceneName, sceneName))
    .ToListAsync();
// Uses index, returns list, checks count in memory

// BAD: Multiple database queries
var count = await _context.Users.CountAsync(u => EF.Functions.ILike(u.SceneName, sceneName));
var user = await _context.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.SceneName, sceneName));
// Two separate queries, slower
```

**Migration Best Practices**:
- Always provide `Down()` method for rollback
- Test migrations on copy of production data
- Use transactions for data migrations (not needed here)
- Document breaking changes in migration comments

### Security Considerations

**No Security Impact**:
- Email remains the unique identifier (unchanged)
- Password validation unchanged
- Token generation unchanged (JTI still uses UserId, not SceneName)
- Lockout behavior per user, not per scene name

**Potential Security Improvements**:
- Email-only login (Option C) slightly reduces phishing surface
- But scene name login is common convention in online communities
- Current approach (allow both) provides flexibility

**Privacy Considerations**:
- Users typing email on public devices: Acceptable risk
- Scene name privacy: Users who want private scene names can choose unique ones
- Profile visibility: Scene names are already public within community

### Monitoring and Observability

**Key Metrics to Track**:
```csharp
// Log duplicate scene name login attempts
_logger.LogInformation("Duplicate scene name login blocked: {SceneName}, UserCount: {Count}",
    sceneName, usersWithSceneName.Count);

// Log registration with duplicate scene name
_logger.LogInformation("User registered with duplicate scene name: {Email}, {SceneName}",
    user.Email, user.SceneName);
```

**Application Insights Queries** (if using Azure):
```kusto
// Duplicate scene name login attempts
traces
| where message contains "Duplicate scene name login blocked"
| summarize count() by bin(timestamp, 1h)

// Scene names with duplicates
customEvents
| where name == "UserRegistered"
| extend sceneName = tostring(customDimensions.SceneName)
| summarize userCount = count() by sceneName
| where userCount > 1
| order by userCount desc
```

---

## Document Metadata

**Document Type**: Technical Design Analysis
**Status**: Draft for Review
**Version**: 1.0
**Created**: 2025-11-23
**Author**: Librarian Agent
**Reviewed By**: Pending
**Approved By**: Pending

**Related Documents**:
- [Database Schema Investigation Report](./database-schema-investigation-report.md)
- Authentication Service Implementation: `/home/chad/repos/witchcityrope/apps/api/Features/Authentication/Services/AuthenticationService.cs`
- User Entity Model: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`

**Change History**:
| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-11-23 | 1.0 | Librarian | Initial draft with 3 implementation options |

---

**Next Steps**:
1. Review this document with development team
2. Select preferred implementation option (recommended: Option A)
3. Create implementation tasks in project management system
4. Assign to appropriate development agents (Backend → Frontend → Test)
5. Schedule deployment during low-traffic window
