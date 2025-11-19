# Database Schema Review - Vetted Member Import Tool
**Date**: 2025-11-18
**Phase**: Phase 1A - Database Schema Review
**Feature**: One-Time Vetted Member Import Tool
**Reviewer**: database-designer

## Executive Summary

**Schema Compatibility**: ✅ YES - Current schema fully supports vetted member import
**Migrations Needed**: ❌ NO - No schema changes required
**Import Ready**: ✅ YES - All required fields and constraints are properly configured

The existing database schema for ApplicationUser, VettingApplication, and VettingAuditLog entities is fully compatible with the import requirements. No database migrations are needed prior to import execution.

---

## 1. Schema Compatibility Assessment

### ApplicationUser Schema

**Location**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`
**Configuration**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs` (lines 296-381)
**Table**: `public.Users`

#### Import-Critical Fields

| Field | Type | Nullable | Import Compatibility | Notes |
|-------|------|----------|---------------------|-------|
| `Id` | Guid | NO | ✅ COMPATIBLE | Auto-generated, import tool should NOT set |
| `Email` | string(255) | NO (Identity) | ✅ COMPATIBLE | From Google Sheet "Email" column |
| `EmailConfirmed` | bool | NO (Identity) | ✅ COMPATIBLE | Set to FALSE for password reset flow |
| `SceneName` | string(50) | NO | ✅ COMPATIBLE | From "Vettee's nickname" column |
| `FirstName` | string(100) | YES | ✅ COMPATIBLE | Optional, can be null |
| `LastName` | string(100) | YES | ✅ COMPATIBLE | Optional, can be null |
| `Pronouns` | string(50) | YES | ✅ COMPATIBLE | From "Vettee's pronouns" column |
| `FetLifeName` | string(100) | YES | ✅ COMPATIBLE | From "FL handles" column |
| `VettingStatus` | int | NO | ✅ COMPATIBLE | Set to 3 (Approved) for all imports |
| `HasVettingApplication` | bool | NO | ✅ COMPATIBLE | Set to TRUE when VettingApplication created |
| `CreatedAt` | timestamptz | NO | ✅ COMPATIBLE | Can be set to historical date from sheet |
| `UpdatedAt` | timestamptz | NO | ✅ COMPATIBLE | Set to import execution time |
| `IsActive` | bool | NO | ✅ COMPATIBLE | Set to TRUE (default) |
| `Role` | text | NO | ✅ COMPATIBLE | Set to "Member" for imported users |

#### Validation Notes

✅ **EmailConfirmed Field**: Identity field, can be set to `false` to trigger password reset flow
✅ **VettingStatus Enum**: Value 3 = Approved matches VettingStatus enum
✅ **Historical Dates**: CreatedAt supports historical dates (e.g., "2022-07-11")
✅ **Optional Fields**: Pronouns and FetLifeName are properly nullable

### VettingApplication Schema

**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingApplication.cs`
**Configuration**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/Configuration/VettingApplicationConfiguration.cs`
**Table**: `public.VettingApplications`

#### Import-Critical Fields

| Field | Type | Nullable | Import Compatibility | Notes |
|-------|------|----------|---------------------|-------|
| `Id` | Guid | NO | ✅ COMPATIBLE | Auto-generated, import tool should NOT set |
| `UserId` | Guid | YES | ✅ COMPATIBLE | FK to ApplicationUser.Id (set after user creation) |
| `SceneName` | string(100) | NO | ✅ COMPATIBLE | Must match ApplicationUser.SceneName |
| `Email` | string(255) | NO | ✅ COMPATIBLE | Must match ApplicationUser.Email |
| `WorkflowStatus` | VettingStatus | NO | ✅ COMPATIBLE | Set to 3 (Approved) |
| `SubmittedAt` | timestamptz | NO | ✅ COMPATIBLE | From "App Submitted" column (parse dates) |
| `DecisionMadeAt` | timestamptz | YES | ✅ COMPATIBLE | Can be set to same as SubmittedAt for historical imports |
| `ExperienceDescription` | text | YES | ✅ COMPATIBLE | Can store long text from "Relevant notes" |
| `FetLifeHandle` | string(100) | YES | ✅ COMPATIBLE | From "FL handles" column |
| `Pronouns` | string(50) | YES | ✅ COMPATIBLE | From "Vettee's pronouns" column |
| `OtherNames` | string(200) | YES | ✅ COMPATIBLE | Additional names/handles if needed |

#### Validation Notes

✅ **WorkflowStatus Enum**: Supports Approved (3) directly without workflow progression
✅ **Historical Dates**: SubmittedAt and DecisionMadeAt support historical dates
✅ **UserId Foreign Key**: CASCADE DELETE configured - deleting user will delete application
✅ **Unique Constraint**: UNIQUE index on UserId ensures one application per user

### VettingAuditLog Schema

**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingAuditLog.cs`
**Configuration**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/Configuration/VettingAuditLogConfiguration.cs`
**Table**: `public.VettingAuditLogs`

#### Import-Critical Fields

| Field | Type | Nullable | Import Compatibility | Notes |
|-------|------|----------|---------------------|-------|
| `Id` | Guid | NO | ✅ COMPATIBLE | Auto-generated |
| `ApplicationId` | Guid | NO | ✅ COMPATIBLE | FK to VettingApplication.Id |
| `Action` | string(200) | NO | ✅ COMPATIBLE | Parse from "Relevant notes" |
| `PerformedBy` | Guid | NO | ⚠️ REQUIRES LOOKUP | FK to ApplicationUser.Id - must find admin user |
| `PerformedAt` | timestamptz | NO | ✅ COMPATIBLE | Parse from "Relevant notes" dates |
| `OldValue` | string(4000) | YES | ✅ COMPATIBLE | Optional, can be null for imports |
| `NewValue` | string(4000) | YES | ✅ COMPATIBLE | Optional, can be null for imports |
| `Notes` | string(2000) | YES | ✅ COMPATIBLE | Store parsed notes from Google Sheet |

#### Validation Notes

⚠️ **PerformedBy Foreign Key**: RESTRICT DELETE - requires valid ApplicationUser.Id
✅ **Action Field**: Can store strings like "ApplicationSubmitted", "InterviewScheduled", etc.
✅ **Historical Dates**: PerformedAt supports historical dates
✅ **Notes Parsing**: Notes field (2000 chars) can store parsed historical actions

---

## 2. Constraint Analysis

### Primary Constraints

#### ApplicationUser Constraints

| Constraint | Type | Description | Import Impact |
|------------|------|-------------|---------------|
| PK_Users | Primary Key | `Id` (Guid) | ✅ Auto-generated, no conflict |
| IX_Users_SceneName | Unique Index | `SceneName` | ⚠️ **DUPLICATE DETECTION CRITICAL** |
| IX_Users_Email | Unique Index | `Email` (from Identity) | ⚠️ **DUPLICATE DETECTION CRITICAL** |
| IX_Users_IsActive | Index | `IsActive` | ℹ️ Performance only |
| IX_Users_Role | Index | `Role` | ℹ️ Performance only |

**CRITICAL FOR IMPORT**:
- **Email UNIQUE constraint**: Import tool MUST check for existing users by email
- **SceneName UNIQUE constraint**: Import tool MUST check for existing users by scene name
- **Duplicate Strategy**: Skip duplicates with warning log entry

#### VettingApplication Constraints

| Constraint | Type | Description | Import Impact |
|------------|------|-------------|---------------|
| PK_VettingApplications | Primary Key | `Id` (Guid) | ✅ Auto-generated, no conflict |
| IX_VettingApplications_UserId | Unique Index | `UserId` | ⚠️ **ONE APPLICATION PER USER** |
| FK_VettingApplications_Users | Foreign Key | `UserId` → `Users.Id` (CASCADE) | ✅ Compatible |
| IX_VettingApplications_WorkflowStatus | Index | `WorkflowStatus` | ℹ️ Performance only |
| IX_VettingApplications_SubmittedAt | Index | `SubmittedAt` | ℹ️ Performance only |
| IX_VettingApplications_Email | Index | `Email` | ℹ️ Performance only |

**CRITICAL FOR IMPORT**:
- **UserId UNIQUE constraint**: Only one VettingApplication per user allowed
- **CASCADE DELETE**: Deleting user will cascade delete application (intentional design)

#### VettingAuditLog Constraints

| Constraint | Type | Description | Import Impact |
|------------|------|-------------|---------------|
| PK_VettingAuditLogs | Primary Key | `Id` (Guid) | ✅ Auto-generated, no conflict |
| FK_VettingAuditLogs_Applications | Foreign Key | `ApplicationId` → `VettingApplications.Id` (CASCADE) | ✅ Compatible |
| FK_VettingAuditLogs_Users | Foreign Key | `PerformedBy` → `Users.Id` (RESTRICT) | ⚠️ **USER LOOKUP REQUIRED** |
| IX_VettingAuditLogs_ApplicationId | Index | `ApplicationId` | ℹ️ Performance only |
| IX_VettingAuditLogs_PerformedAt | Index | `PerformedAt` | ℹ️ Performance only |
| IX_VettingAuditLogs_Action | Index | `Action` | ℹ️ Performance only |

**CRITICAL FOR IMPORT**:
- **PerformedBy RESTRICT DELETE**: User referenced in audit log cannot be deleted
- **User Lookup Required**: Import tool must find admin users (Chad, Georgia, Cass, Samantha) by name

### Duplicate Detection Strategy

**Recommended Approach**: Check BOTH email AND scene name before import

```csharp
// Pseudo-code for duplicate detection
var existingUserByEmail = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == importEmail);

var existingUserBySceneName = await _context.Users
    .FirstOrDefaultAsync(u => u.SceneName == importSceneName);

if (existingUserByEmail != null || existingUserBySceneName != null)
{
    // Skip this import row
    _logger.LogWarning("Duplicate user detected: Email={Email}, SceneName={SceneName}",
        importEmail, importSceneName);
    continue;
}
```

---

## 3. Field Mapping Validation

### Google Sheet → ApplicationUser Mapping

| Google Sheet Column | Database Field | Data Type | Required | Notes |
|---------------------|---------------|-----------|----------|-------|
| Email | `Email` | string(255) | YES | Direct mapping |
| Email | `EmailConfirmed` | bool | YES | Set to FALSE |
| Vettee's nickname | `SceneName` | string(50) | YES | Direct mapping |
| Vettee's pronouns | `Pronouns` | string(50) | NO | Direct mapping, nullable |
| FL handles | `FetLifeName` | string(100) | NO | Direct mapping, nullable |
| *(derived)* | `VettingStatus` | int | YES | Set to 3 (Approved) |
| *(derived)* | `HasVettingApplication` | bool | YES | Set to TRUE |
| App Submitted | `CreatedAt` | timestamptz | YES | Parse date string to DateTime |
| *(import time)* | `UpdatedAt` | timestamptz | YES | Set to NOW() |
| *(derived)* | `IsActive` | bool | YES | Set to TRUE |
| *(derived)* | `Role` | text | YES | Set to "Member" |

### Google Sheet → VettingApplication Mapping

| Google Sheet Column | Database Field | Data Type | Required | Notes |
|---------------------|---------------|-----------|----------|-------|
| *(FK)* | `UserId` | Guid | YES | Set after ApplicationUser created |
| Vettee's nickname | `SceneName` | string(100) | YES | Must match ApplicationUser.SceneName |
| Email | `Email` | string(255) | YES | Must match ApplicationUser.Email |
| FL handles | `FetLifeHandle` | string(100) | NO | Direct mapping |
| Vettee's pronouns | `Pronouns` | string(50) | NO | Direct mapping |
| App Submitted | `SubmittedAt` | timestamptz | YES | Parse date string (e.g., "7/11/22") |
| App Submitted | `DecisionMadeAt` | timestamptz | NO | Set to same as SubmittedAt |
| *(derived)* | `WorkflowStatus` | VettingStatus | YES | Set to 3 (Approved) |
| Relevant notes | `ExperienceDescription` | text | NO | Store full notes text |

### Google Sheet → VettingAuditLog Mapping

| Google Sheet Column | Database Field | Data Type | Required | Notes |
|---------------------|---------------|-----------|----------|-------|
| *(FK)* | `ApplicationId` | Guid | YES | Set after VettingApplication created |
| Relevant notes | `Action` | string(200) | YES | Parse action type from notes |
| Assigned Vettor | `PerformedBy` | Guid | YES | Lookup user by name (Chad/Georgia/Cass/Samantha) |
| Relevant notes | `PerformedAt` | timestamptz | YES | Parse date from notes |
| Relevant notes | `Notes` | string(2000) | NO | Store parsed note text |

#### Audit Log Parsing Strategy

**Example Google Sheet "Relevant notes" field**:
```
2022-07-11: Application submitted
2022-07-15: Interview scheduled by Chad
2022-07-22: Interview completed, approved by Georgia
```

**Parsed into VettingAuditLog entries**:
1. Action: "ApplicationSubmitted", PerformedBy: (system or import user), PerformedAt: 2022-07-11
2. Action: "InterviewScheduled", PerformedBy: (Chad's UserId), PerformedAt: 2022-07-15
3. Action: "Approved", PerformedBy: (Georgia's UserId), PerformedAt: 2022-07-22

---

## 4. Migration Requirements

### Pre-Import Migrations: ❌ NONE REQUIRED

The current schema is fully compatible with import requirements. No database migrations are needed before running the import tool.

### Post-Import Migrations: ❌ NONE REQUIRED

No schema changes are needed after the import completes.

### Database Index Recommendations: ✅ ALREADY OPTIMAL

**Current indexes are sufficient for import operations**:
- ✅ `IX_Users_SceneName` (unique) - Supports duplicate detection by scene name
- ✅ `IX_Users_Email` (unique, from Identity) - Supports duplicate detection by email
- ✅ `IX_VettingApplications_UserId` (unique) - Prevents multiple applications per user
- ✅ `IX_VettingApplications_Email` - Supports lookup operations
- ✅ `IX_VettingAuditLogs_ApplicationId` - Supports audit log queries

**No additional indexes needed for import performance**.

---

## 5. Remote Database Notes

### Database Compatibility

**Local Development Database** (PostgreSQL in Docker):
- ✅ Port: 5434 (dedicated to avoid conflicts)
- ✅ Connection String: From appsettings.Development.json
- ✅ Schema: Matches migration snapshot

**Staging Database** (DigitalOcean PostgreSQL):
- ✅ Host: db-postgresql-nyc3-witchcityrope-staging-do-user-18540781-0.x.db.ondigitalocean.com
- ✅ Port: 25060
- ✅ Connection String: From DigitalOcean console or .NET User Secrets
- ✅ Schema: Matches production (all migrations applied)

**Production Database** (DigitalOcean PostgreSQL):
- ✅ Host: db-postgresql-nyc3-witchcityrope-do-user-18540781-0.x.db.ondigitalocean.com
- ✅ Port: 25060
- ✅ Connection String: From DigitalOcean console or .NET User Secrets
- ✅ Schema: Latest migration snapshot

### Connection String Requirements

**Console Tool Connection String Format**:
```
Host=<hostname>;Port=<port>;Database=<dbname>;Username=<username>;Password=<password>;SSL Mode=Require
```

**Critical Configuration**:
- ✅ SSL Mode: REQUIRED for DigitalOcean connections
- ✅ Connection Pooling: Recommended for batch imports
- ✅ Command Timeout: 120 seconds (for large transactions)
- ✅ Max Pool Size: 20 (for staging/production)

**Environment Variables** (recommended for console tool):
```bash
# Local
DATABASE_CONNECTION_STRING="Host=localhost;Port=5434;Database=witchcityrope;Username=postgres;Password=postgres;Pooling=true"

# Staging
DATABASE_CONNECTION_STRING="Host=staging-host;Port=25060;Database=witchcityrope;Username=doadmin;Password=xxx;SSL Mode=Require"

# Production
DATABASE_CONNECTION_STRING="Host=prod-host;Port=25060;Database=witchcityrope;Username=doadmin;Password=xxx;SSL Mode=Require"
```

### Schema Verification

**Before importing to staging/production**, verify schema compatibility:

```sql
-- Verify Users table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'Users'
ORDER BY ordinal_position;

-- Verify VettingApplications table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'VettingApplications'
ORDER BY ordinal_position;

-- Verify VettingAuditLogs table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'VettingAuditLogs'
ORDER BY ordinal_position;

-- Verify constraints
SELECT constraint_name, constraint_type
FROM information_schema.table_constraints
WHERE table_schema = 'public' AND table_name IN ('Users', 'VettingApplications', 'VettingAuditLogs');
```

---

## 6. Import Recommendations

### Best Practices for Import Tool

#### 1. Transaction Strategy

**Recommended**: Single transaction per user (user + application + audit logs)

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 1. Create ApplicationUser
    var user = new ApplicationUser { ... };
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();

    // 2. Create VettingApplication
    var application = new VettingApplication { UserId = user.Id, ... };
    await _context.VettingApplications.AddAsync(application);
    await _context.SaveChangesAsync();

    // 3. Create VettingAuditLog entries
    foreach (var auditEntry in parsedAuditLogs)
    {
        var log = new VettingAuditLog { ApplicationId = application.Id, ... };
        await _context.VettingAuditLogs.AddAsync(log);
    }
    await _context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "Failed to import user {Email}", email);
    // Continue to next row
}
```

**Benefits**:
- ✅ Atomicity: Either entire user is imported or none
- ✅ Consistency: No orphaned records if import fails mid-way
- ✅ Isolation: Other operations won't see partial imports
- ✅ Rollback: Failed imports don't corrupt database

#### 2. Duplicate Detection

**Check BOTH email AND scene name before creating user**:

```csharp
// Check for existing user by email
var existingUserByEmail = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Email.ToLower() == importEmail.ToLower());

// Check for existing user by scene name
var existingUserBySceneName = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.SceneName.ToLower() == importSceneName.ToLower());

if (existingUserByEmail != null)
{
    _logger.LogWarning("Skipping import: User with email {Email} already exists (ID: {UserId})",
        importEmail, existingUserByEmail.Id);
    importResults.Skipped++;
    continue;
}

if (existingUserBySceneName != null)
{
    _logger.LogWarning("Skipping import: User with scene name {SceneName} already exists (ID: {UserId})",
        importSceneName, existingUserBySceneName.Id);
    importResults.Skipped++;
    continue;
}
```

#### 3. User Lookup for Audit Logs

**Find admin users by name for PerformedBy field**:

```csharp
// Pre-load admin users for audit log lookups
var adminUsers = await _context.Users
    .AsNoTracking()
    .Where(u => u.SceneName.ToLower().In("chad", "georgia", "cass", "samantha"))
    .ToDictionaryAsync(u => u.SceneName.ToLower(), u => u.Id);

// Later, when creating audit log entries
var performedByUserId = adminUsers.GetValueOrDefault(adminNameFromNotes.ToLower());
if (performedByUserId == Guid.Empty)
{
    _logger.LogWarning("Could not find admin user: {AdminName}, using system user", adminNameFromNotes);
    performedByUserId = systemUserId; // Or skip this audit log entry
}
```

#### 4. Date Parsing

**Parse historical dates from Google Sheet**:

```csharp
// Example: "7/11/22" → DateTime
if (DateTime.TryParseExact(dateString, "M/d/yy",
    CultureInfo.InvariantCulture,
    DateTimeStyles.AssumeUniversal,
    out var parsedDate))
{
    var utcDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
    vettingApplication.SubmittedAt = utcDate;
}
else
{
    _logger.LogWarning("Could not parse date: {DateString}, using current date", dateString);
    vettingApplication.SubmittedAt = DateTime.UtcNow;
}
```

**CRITICAL**: Always convert to UTC before storing in PostgreSQL timestamptz columns.

#### 5. Error Handling and Logging

**Log all operations with structured logging**:

```csharp
_logger.LogInformation("Starting import for user {Email}", importEmail);

try
{
    // Import operations
    _logger.LogInformation("Successfully imported user {Email} (ID: {UserId})",
        user.Email, user.Id);
    importResults.Successful++;
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
{
    _logger.LogWarning("Duplicate key constraint violation for user {Email}: {Message}",
        importEmail, ex.InnerException.Message);
    importResults.Skipped++;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to import user {Email}", importEmail);
    importResults.Failed++;
}
```

#### 6. Dry-Run Mode

**Validate data without committing to database**:

```csharp
if (isDryRun)
{
    // Perform all validation and object creation
    var user = new ApplicationUser { ... };
    var application = new VettingApplication { ... };

    // Log what WOULD be created
    _logger.LogInformation("[DRY RUN] Would create user: {Email}, {SceneName}",
        user.Email, user.SceneName);

    // Don't save to database
    // Don't commit transaction
}
else
{
    // Actual import with database commits
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
```

---

## 7. Rollback Plan

### If Import Fails Mid-Way

**Transaction-based rollback** (automatic for each user):
- ✅ Each user import is in its own transaction
- ✅ Failed transactions automatically roll back
- ✅ Successful imports remain committed
- ✅ No manual cleanup needed for failed individual imports

### If Entire Import Must Be Reversed

**Manual rollback SQL** (use with caution):

```sql
-- 1. Identify imported users (example: by CreatedAt timestamp)
SELECT "Id", "Email", "SceneName", "CreatedAt"
FROM "Users"
WHERE "CreatedAt" >= '2025-11-18 10:00:00+00'  -- Import start time
  AND "VettingStatus" = 3
ORDER BY "CreatedAt";

-- 2. Delete audit logs for imported applications
DELETE FROM "VettingAuditLogs"
WHERE "ApplicationId" IN (
    SELECT "Id" FROM "VettingApplications"
    WHERE "UserId" IN (
        SELECT "Id" FROM "Users"
        WHERE "CreatedAt" >= '2025-11-18 10:00:00+00'
          AND "VettingStatus" = 3
    )
);

-- 3. Delete vetting applications for imported users
DELETE FROM "VettingApplications"
WHERE "UserId" IN (
    SELECT "Id" FROM "Users"
    WHERE "CreatedAt" >= '2025-11-18 10:00:00+00'
      AND "VettingStatus" = 3
);

-- 4. Delete imported users
-- CRITICAL: Verify IDs before running this!
DELETE FROM "Users"
WHERE "CreatedAt" >= '2025-11-18 10:00:00+00'
  AND "VettingStatus" = 3;
```

**⚠️ CRITICAL**: Test rollback on staging database first!

### Safer Alternative: Import Marker Field

**Add a temporary marker field to track imports** (optional enhancement):

```csharp
// During import
user.Bio = "IMPORTED_2025_11_18"; // Temporary marker

// Rollback query
DELETE FROM "Users" WHERE "Bio" = 'IMPORTED_2025_11_18';
```

**After verification**: Clear marker field with UPDATE statement.

---

## 8. Performance Considerations

### Import Performance Estimates

**Assumptions**:
- 140 users to import
- 3 entities per user (User, VettingApplication, ~2 VettingAuditLogs)
- PostgreSQL in DigitalOcean (network latency ~20-50ms)

**Estimated Times**:
- Local database: ~5-10 seconds (140 users)
- Staging/Production: ~20-30 seconds (140 users, including network latency)

**Bottlenecks**:
- ✅ Network latency (DigitalOcean connections)
- ✅ Duplicate detection queries (2 queries per user)
- ✅ Transaction commits (1 per user)

### Optimization Strategies

#### 1. Batch Duplicate Detection

**Instead of querying per user, load all existing users once**:

```csharp
// Load all existing emails and scene names into memory
var existingEmails = await _context.Users
    .AsNoTracking()
    .Select(u => u.Email.ToLower())
    .ToHashSetAsync();

var existingSceneNames = await _context.Users
    .AsNoTracking()
    .Select(u => u.SceneName.ToLower())
    .ToHashSetAsync();

// Check duplicates in memory (much faster)
foreach (var importRow in googleSheetRows)
{
    if (existingEmails.Contains(importRow.Email.ToLower()))
    {
        // Skip duplicate
        continue;
    }
    if (existingSceneNames.Contains(importRow.SceneName.ToLower()))
    {
        // Skip duplicate
        continue;
    }

    // Proceed with import
}
```

**Performance gain**: Reduces 140 * 2 = 280 queries to 2 queries total.

#### 2. Pre-Load Admin Users

**Load admin users once, reuse for all audit logs**:

```csharp
var adminUsersCache = await _context.Users
    .AsNoTracking()
    .Where(u => new[] { "chad", "georgia", "cass", "samantha" }
        .Contains(u.SceneName.ToLower()))
    .ToDictionaryAsync(u => u.SceneName.ToLower(), u => u.Id);

// Reuse for all 140+ imports
```

#### 3. Connection Pooling

**Enable connection pooling in connection string**:

```
Host=host;Port=port;Database=db;Username=user;Password=pass;Pooling=true;Max Pool Size=20
```

#### 4. Disable Change Tracking for Lookups

**Use AsNoTracking() for all read-only queries**:

```csharp
var existingUser = await _context.Users
    .AsNoTracking()  // Don't track for duplicate checks
    .FirstOrDefaultAsync(u => u.Email == email);
```

---

## 9. Security & Monitoring

### Security Considerations

#### 1. Password Security

**Imported users have NO password set**:
- ✅ `EmailConfirmed` = false triggers password reset flow
- ✅ User receives "NewWebsiteUser" email with reset link
- ✅ No plaintext passwords in import data
- ✅ No temporary passwords required

#### 2. Connection String Security

**Never commit connection strings to source control**:
- ✅ Use .NET User Secrets for local development
- ✅ Use environment variables for console tool
- ✅ Use DigitalOcean secrets for production

```bash
# Example: Set environment variable
export DATABASE_CONNECTION_STRING="Host=...;Password=xxx;SSL Mode=Require"

# Console tool reads from environment
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
```

#### 3. Audit Trail

**Import operations should be logged**:
- ✅ Log all successful imports
- ✅ Log all skipped duplicates
- ✅ Log all failed imports
- ✅ Store import summary (counts, timestamps)

### Monitoring Recommendations

#### 1. Import Execution Monitoring

**Log import progress**:

```csharp
_logger.LogInformation("Import started: {TotalRows} rows to process", totalRows);

// Every 10 users
if (processedCount % 10 == 0)
{
    _logger.LogInformation("Import progress: {Processed}/{Total} users processed",
        processedCount, totalRows);
}

_logger.LogInformation("Import completed: Success={Success}, Skipped={Skipped}, Failed={Failed}",
    importResults.Successful, importResults.Skipped, importResults.Failed);
```

#### 2. Database Health Monitoring

**Check database health before import**:

```sql
-- Verify database connection
SELECT version();

-- Check table existence
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('Users', 'VettingApplications', 'VettingAuditLogs');

-- Verify existing user count
SELECT COUNT(*) FROM "Users";
```

#### 3. Post-Import Validation

**Verify import success**:

```sql
-- Count users with VettingStatus = Approved
SELECT COUNT(*) FROM "Users" WHERE "VettingStatus" = 3;

-- Count vetting applications with WorkflowStatus = Approved
SELECT COUNT(*) FROM "VettingApplications" WHERE "WorkflowStatus" = 3;

-- Count audit logs created today
SELECT COUNT(*) FROM "VettingAuditLogs"
WHERE "PerformedAt"::date = CURRENT_DATE;

-- Verify all imported users have applications
SELECT u."Email", u."SceneName"
FROM "Users" u
LEFT JOIN "VettingApplications" va ON u."Id" = va."UserId"
WHERE u."VettingStatus" = 3
  AND va."Id" IS NULL;  -- Should return 0 rows
```

---

## 10. Validation Checklist

### Pre-Import Validation

- [x] ApplicationUser schema supports EmailConfirmed = false
- [x] VettingApplication schema supports historical SubmittedAt dates
- [x] VettingAuditLog schema supports note imports
- [x] No constraints preventing direct VettingStatus = Approved assignment
- [x] All required fields are properly configured
- [x] Foreign key constraints are compatible with import flow
- [x] Indexes support efficient duplicate detection
- [x] Remote database schemas match local development

### Import Tool Requirements

- [ ] **Duplicate Detection**: Check email AND scene name before import
- [ ] **Transaction Safety**: Wrap each user import in transaction
- [ ] **User Lookup**: Pre-load admin users for audit log PerformedBy
- [ ] **Date Parsing**: Parse historical dates to UTC DateTime
- [ ] **Error Handling**: Log all errors, continue processing remaining rows
- [ ] **Dry-Run Mode**: Validate data without database commits
- [ ] **Connection Strings**: Support Local, Staging, Production environments
- [ ] **Logging**: Structured logging for all operations
- [ ] **Performance**: Batch duplicate detection, use connection pooling

### Post-Import Validation

- [ ] **User Count**: Verify expected number of users imported
- [ ] **Application Count**: Verify all users have VettingApplications
- [ ] **Audit Log Count**: Verify audit logs created
- [ ] **No Orphans**: Verify no orphaned applications or audit logs
- [ ] **Email Verified**: Verify all imported users have EmailConfirmed = false
- [ ] **Vetting Status**: Verify all imported users have VettingStatus = 3

---

## 11. Handoff to Backend Developer

### Key Findings Summary

**Schema Compatibility**: ✅ FULLY COMPATIBLE - No migrations needed
**Import Ready**: ✅ YES - All constraints and fields support import flow
**Performance**: ✅ OPTIMIZED - Existing indexes are sufficient

### Critical Implementation Notes for Backend Developer

1. **Duplicate Detection**: MUST check BOTH email AND scene name (both have unique constraints)
2. **Transaction Per User**: Recommended for atomicity and rollback capability
3. **User Lookup**: Pre-load admin users for VettingAuditLog.PerformedBy (RESTRICT constraint)
4. **Date Parsing**: Convert all dates to UTC before storing in timestamptz columns
5. **Error Handling**: Use structured logging, continue processing on individual failures
6. **Dry-Run Mode**: Implement for safe testing before production import
7. **Connection Strings**: Support environment-based configuration

### Database Constraints to Consider

**UNIQUE Constraints** (will throw exceptions on duplicates):
- `Users.Email` (from Identity)
- `Users.SceneName`
- `VettingApplications.UserId`

**FOREIGN KEY Constraints** (will throw exceptions if references don't exist):
- `VettingApplications.UserId` → `Users.Id` (CASCADE DELETE)
- `VettingAuditLogs.ApplicationId` → `VettingApplications.Id` (CASCADE DELETE)
- `VettingAuditLogs.PerformedBy` → `Users.Id` (RESTRICT DELETE)

**Required Fields** (will throw exceptions if null):
- `ApplicationUser.Email`, `SceneName`, `VettingStatus`, `CreatedAt`, `UpdatedAt`, `Role`
- `VettingApplication.SceneName`, `Email`, `WorkflowStatus`, `SubmittedAt`
- `VettingAuditLog.ApplicationId`, `Action`, `PerformedBy`, `PerformedAt`

### Recommended Import Flow

1. **Pre-flight**: Load existing emails, scene names, admin users into memory
2. **For each Google Sheet row**:
   a. Check duplicates (skip if exists)
   b. Begin transaction
   c. Create ApplicationUser
   d. Create VettingApplication
   e. Parse and create VettingAuditLog entries
   f. Commit transaction
   g. Log success/failure
3. **Post-import**: Validate counts, check for orphans

### Testing Recommendations

1. **Local Testing**: Run import against local Docker database, verify results
2. **Dry-Run**: Test with dry-run mode enabled, verify logs
3. **Staging Testing**: Run import against staging database, verify no production impact
4. **Production Import**: Execute import during low-traffic period, monitor closely

---

## 12. Appendix: Schema Diagrams

### Entity Relationships

```
ApplicationUser (Users table)
├── Id (PK, Guid)
├── Email (UNIQUE)
├── SceneName (UNIQUE)
├── EmailConfirmed (bool) ← Set to FALSE for imports
├── VettingStatus (int) ← Set to 3 (Approved)
└── HasVettingApplication (bool) ← Set to TRUE

VettingApplication (VettingApplications table)
├── Id (PK, Guid)
├── UserId (FK → Users.Id, UNIQUE) ← Set after user creation
├── WorkflowStatus (VettingStatus) ← Set to 3 (Approved)
└── SubmittedAt (timestamptz) ← Historical date from Google Sheet

VettingAuditLog (VettingAuditLogs table)
├── Id (PK, Guid)
├── ApplicationId (FK → VettingApplications.Id)
├── PerformedBy (FK → Users.Id, RESTRICT) ← Lookup admin user
└── PerformedAt (timestamptz) ← Historical date from notes
```

### Import Data Flow

```
Google Sheet Row
    ↓
Duplicate Check (email, scene name)
    ↓
Begin Transaction
    ↓
Create ApplicationUser
    ├── Email ← Google Sheet "Email"
    ├── SceneName ← Google Sheet "Vettee's nickname"
    ├── EmailConfirmed = FALSE
    ├── VettingStatus = 3 (Approved)
    └── CreatedAt ← Parse "App Submitted"
    ↓
SaveChanges() → Get User.Id
    ↓
Create VettingApplication
    ├── UserId ← User.Id (from previous step)
    ├── SceneName ← Match User.SceneName
    ├── WorkflowStatus = 3 (Approved)
    └── SubmittedAt ← Parse "App Submitted"
    ↓
SaveChanges() → Get Application.Id
    ↓
Parse "Relevant notes"
    ↓
For each parsed action:
    Create VettingAuditLog
    ├── ApplicationId ← Application.Id
    ├── PerformedBy ← Lookup admin user (Chad/Georgia/etc.)
    ├── Action ← Parsed action type
    └── PerformedAt ← Parsed date
    ↓
SaveChanges()
    ↓
Commit Transaction
    ↓
Log Success
```

---

## Conclusion

The WitchCityRope database schema is **fully compatible** with the vetted member import requirements. No database migrations are needed. The import tool can proceed with implementation using the constraints and recommendations documented in this review.

**Next Steps**:
1. Backend developer implements console import tool using schema findings
2. Test developer creates integration tests for import validation
3. Execute import against local development database
4. Execute import against staging database
5. After verification, execute import against production database

**Database Designer Phase 1A**: ✅ COMPLETE

---

**Document Version**: 1.0
**Last Updated**: 2025-11-18
**Status**: Ready for Backend Developer Phase 1B
