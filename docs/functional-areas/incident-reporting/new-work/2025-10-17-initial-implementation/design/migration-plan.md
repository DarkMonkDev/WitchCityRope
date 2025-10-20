# Database Migration Plan: Incident Reporting System
<!-- Last Updated: 2025-10-18 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer Agent -->
<!-- Status: Draft - Awaiting Approval -->

## Executive Summary

This document provides step-by-step procedures for migrating the existing SafetyIncident database schema to support the new 5-stage workflow. The migration includes status enum updates (BREAKING CHANGE), new coordinator field, notes table creation, and Google Drive link fields.

**⚠️ CRITICAL WARNING**: This migration includes BREAKING CHANGES to the IncidentStatus enum. All code referencing the old enum values will fail after migration.

**Migration Complexity**: HIGH
**Estimated Duration**: 30 minutes (development), 5 minutes (production with no data)
**Rollback Time**: 10 minutes
**Breaking Changes**: YES (IncidentStatus enum)

---

## Pre-Migration Checklist

### Environment Preparation

- [ ] **Database Backup**: Create full backup of production database
- [ ] **Backup Verification**: Verify backup is restorable
- [ ] **Development Testing**: Run complete migration on development environment
- [ ] **Staging Validation**: Run migration on staging with production-like data
- [ ] **Rollback Plan**: Review rollback procedures
- [ ] **Maintenance Window**: Schedule maintenance window (if production has data)
- [ ] **Team Notification**: Notify team of breaking changes

### Code Preparation

- [ ] **Backend Code**: Update IncidentStatus enum in all backend files
- [ ] **API Endpoints**: Update all API endpoints to use new enum values
- [ ] **Tests**: Update all tests to use new enum values
- [ ] **NSwag Generation**: Prepare to regenerate TypeScript types after migration
- [ ] **Frontend Code**: Prepare to update React components after NSwag regeneration

### Data Assessment

```sql
-- Check current incident count
SELECT COUNT(*) FROM "SafetyIncidents";

-- Check status distribution
SELECT "Status", COUNT(*) as count
FROM "SafetyIncidents"
GROUP BY "Status"
ORDER BY "Status";

-- Check for any NULL status values (should be none)
SELECT COUNT(*) FROM "SafetyIncidents" WHERE "Status" IS NULL;
```

---

## Migration Steps

### Phase 1: Add New Columns (Non-Breaking)

**Entity Framework Migration Name**: `AddCoordinatorAndGoogleDriveFields`

#### Step 1.1: Create EF Core Migration

```bash
cd /home/chad/repos/witchcityrope-react/apps/api

dotnet ef migrations add AddCoordinatorAndGoogleDriveFields \
  --context ApplicationDbContext \
  --output-dir Migrations
```

#### Step 1.2: Review Generated Migration

**Expected Up() Method**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<Guid>(
        name: "CoordinatorId",
        table: "SafetyIncidents",
        type: "uuid",
        nullable: true,
        comment: "Assigned incident coordinator - ANY user can be assigned");

    migrationBuilder.AddColumn<string>(
        name: "GoogleDriveFolderUrl",
        table: "SafetyIncidents",
        type: "character varying(500)",
        maxLength: 500,
        nullable: true,
        comment: "Phase 1: Manual link entry. Phase 2: Automated creation");

    migrationBuilder.AddColumn<string>(
        name: "GoogleDriveFinalReportUrl",
        table: "SafetyIncidents",
        type: "character varying(500)",
        maxLength: 500,
        nullable: true,
        comment: "Phase 1: Manual link entry. Phase 2: Automated generation");

    migrationBuilder.CreateIndex(
        name: "IX_SafetyIncidents_CoordinatorId_Status",
        table: "SafetyIncidents",
        columns: new[] { "CoordinatorId", "Status" });

    migrationBuilder.CreateIndex(
        name: "IX_SafetyIncidents_Status_CoordinatorId",
        table: "SafetyIncidents",
        columns: new[] { "Status", "CoordinatorId" },
        filter: "\"CoordinatorId\" IS NULL");

    migrationBuilder.CreateIndex(
        name: "IX_SafetyIncidents_UpdatedAt",
        table: "SafetyIncidents",
        column: "UpdatedAt",
        descending: true,
        filter: "\"Status\" IN (1, 2, 3, 4)");

    migrationBuilder.AddForeignKey(
        name: "FK_SafetyIncidents_Coordinator_CoordinatorId",
        table: "SafetyIncidents",
        column: "CoordinatorId",
        principalTable: "AspNetUsers",
        principalColumn: "Id",
        onDelete: ReferentialAction.SetNull);
}
```

#### Step 1.3: Apply Migration

```bash
# Development environment
dotnet ef database update --context ApplicationDbContext

# Production environment (when ready)
dotnet ef database update --context ApplicationDbContext --connection "[production-connection-string]"
```

#### Step 1.4: Verify Column Addition

```sql
-- Verify new columns exist
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'SafetyIncidents'
AND column_name IN ('CoordinatorId', 'GoogleDriveFolderUrl', 'GoogleDriveFinalReportUrl');

-- Expected result:
-- CoordinatorId        | uuid                  | YES
-- GoogleDriveFolderUrl | character varying(500)| YES
-- GoogleDriveFinalReportUrl | character varying(500)| YES
```

**Success Criteria**: All new columns added, all indexes created, foreign key constraint exists

---

### Phase 2: Create IncidentNote Table (Non-Breaking)

**Entity Framework Migration Name**: `CreateIncidentNoteTable`

#### Step 2.1: Create EF Core Migration

```bash
dotnet ef migrations add CreateIncidentNoteTable \
  --context ApplicationDbContext \
  --output-dir Migrations
```

#### Step 2.2: Review Generated Migration

**Expected Up() Method**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "IncidentNotes",
        columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
            Content = table.Column<string>(type: "text", nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            IsPrivate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
            Tags = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_IncidentNotes", x => x.Id);
            table.CheckConstraint("CHK_IncidentNotes_Type", "\"Type\" IN (1, 2)");
            table.CheckConstraint("CHK_IncidentNotes_Content_NotEmpty", "LENGTH(TRIM(\"Content\")) > 0");
            table.ForeignKey(
                name: "FK_IncidentNotes_Incidents_IncidentId",
                column: x => x.IncidentId,
                principalTable: "SafetyIncidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_IncidentNotes_Users_AuthorId",
                column: x => x.AuthorId,
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        },
        comment: "Investigation notes for incident coordination - mirrors vetting ApplicationNoteDto pattern");

    migrationBuilder.CreateIndex(
        name: "IX_IncidentNotes_IncidentId_CreatedAt",
        table: "IncidentNotes",
        columns: new[] { "IncidentId", "CreatedAt" },
        descending: new[] { false, true });

    migrationBuilder.CreateIndex(
        name: "IX_IncidentNotes_AuthorId",
        table: "IncidentNotes",
        column: "AuthorId",
        filter: "\"AuthorId\" IS NOT NULL");

    migrationBuilder.CreateIndex(
        name: "IX_IncidentNotes_Type",
        table: "IncidentNotes",
        column: "Type");
}
```

#### Step 2.3: Apply Migration

```bash
dotnet ef database update --context ApplicationDbContext
```

#### Step 2.4: Verify Table Creation

```sql
-- Verify table exists
SELECT table_name, table_type
FROM information_schema.tables
WHERE table_name = 'IncidentNotes';

-- Verify indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'IncidentNotes';

-- Verify constraints
SELECT conname, contype, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'IncidentNotes'::regclass;
```

**Success Criteria**: Table created with all columns, indexes, and constraints

---

### Phase 3: Update IncidentStatus Enum (BREAKING CHANGE)

**⚠️ CRITICAL**: This phase BREAKS all code using old enum values

**Entity Framework Migration Name**: `UpdateIncidentStatusEnum`

#### Step 3.1: Update Enum in Code

**File**: `/apps/api/Features/Safety/Entities/SafetyIncident.cs`

**OLD Enum** (Lines 163-169):
```csharp
public enum IncidentStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3,
    Archived = 4
}
```

**NEW Enum**:
```csharp
/// <summary>
/// Incident status workflow (5-stage process)
/// Migration from 4-stage: New→ReportSubmitted, InProgress→InformationGathering, Resolved/Archived→Closed
/// </summary>
public enum IncidentStatus
{
    /// <summary>
    /// Initial state - incident submitted, awaiting assignment
    /// OLD: New (1)
    /// </summary>
    ReportSubmitted = 1,

    /// <summary>
    /// Coordinator assigned, gathering additional information
    /// OLD: InProgress (2)
    /// </summary>
    InformationGathering = 2,

    /// <summary>
    /// Investigation complete, preparing final documentation
    /// NEW: No direct mapping from old enum
    /// </summary>
    ReviewingFinalReport = 3,

    /// <summary>
    /// Paused pending external information or processes
    /// NEW: No direct mapping from old enum
    /// </summary>
    OnHold = 4,

    /// <summary>
    /// Investigation complete, archived
    /// OLD: Resolved (3) OR Archived (4)
    /// </summary>
    Closed = 5
}
```

#### Step 3.2: Create Data Migration Script

**Manual SQL Migration** (run BEFORE EF migration):

```sql
-- ========================================
-- INCIDENT STATUS ENUM MIGRATION
-- ========================================
-- This script migrates existing IncidentStatus values from 4-stage to 5-stage enum
-- CRITICAL: Run this BEFORE updating code to new enum values
-- ========================================

BEGIN TRANSACTION;

-- Log migration start
INSERT INTO "IncidentAuditLog" (
    "Id", "IncidentId", "UserId", "ActionType", "ActionDescription",
    "OldValues", "NewValues", "CreatedAt"
)
SELECT
    gen_random_uuid(),
    "Id",
    NULL,  -- System action
    'StatusEnumMigration',
    'Automated migration from 4-stage to 5-stage status enum',
    json_build_object('OldStatus', "Status")::text,
    json_build_object('NewStatus',
        CASE
            WHEN "Status" = 1 THEN 1  -- New → ReportSubmitted
            WHEN "Status" = 2 THEN 2  -- InProgress → InformationGathering
            WHEN "Status" = 3 THEN 5  -- Resolved → Closed
            WHEN "Status" = 4 THEN 5  -- Archived → Closed
            ELSE "Status"             -- Preserve any other values (should not exist)
        END
    )::text,
    NOW()
FROM "SafetyIncidents"
WHERE "Status" IN (1, 2, 3, 4);

-- Migrate status values
UPDATE "SafetyIncidents"
SET "Status" = CASE
    WHEN "Status" = 1 THEN 1  -- New → ReportSubmitted (1)
    WHEN "Status" = 2 THEN 2  -- InProgress → InformationGathering (2)
    WHEN "Status" = 3 THEN 5  -- Resolved → Closed (5)
    WHEN "Status" = 4 THEN 5  -- Archived → Closed (5)
    ELSE "Status"             -- Preserve any other values
END,
"UpdatedAt" = NOW()
WHERE "Status" IN (1, 2, 3, 4);

-- Verify migration
DO $$
DECLARE
    v_count INTEGER;
BEGIN
    -- Check for any status values outside new range (1-5)
    SELECT COUNT(*) INTO v_count
    FROM "SafetyIncidents"
    WHERE "Status" NOT IN (1, 2, 3, 4, 5);

    IF v_count > 0 THEN
        RAISE EXCEPTION 'Migration failed: % incidents have invalid status values', v_count;
    END IF;

    -- Log migration completion
    RAISE NOTICE 'Migration completed successfully. All status values updated.';
END $$;

-- Show migration results
SELECT
    'Migration Results' as description,
    COUNT(*) as total_incidents,
    SUM(CASE WHEN "Status" = 1 THEN 1 ELSE 0 END) as report_submitted_count,
    SUM(CASE WHEN "Status" = 2 THEN 1 ELSE 0 END) as information_gathering_count,
    SUM(CASE WHEN "Status" = 3 THEN 1 ELSE 0 END) as reviewing_report_count,
    SUM(CASE WHEN "Status" = 4 THEN 1 ELSE 0 END) as on_hold_count,
    SUM(CASE WHEN "Status" = 5 THEN 1 ELSE 0 END) as closed_count
FROM "SafetyIncidents";

COMMIT;
```

#### Step 3.3: Run Data Migration

```bash
# Development
psql -h localhost -U postgres -d witchcityrope_dev -f status-enum-migration.sql

# Production (when ready)
psql -h [production-host] -U [production-user] -d [production-db] -f status-enum-migration.sql
```

#### Step 3.4: Verify Data Migration

```sql
-- Verify status distribution after migration
SELECT "Status", COUNT(*) as count
FROM "SafetyIncidents"
GROUP BY "Status"
ORDER BY "Status";

-- Expected: Only values 1, 2, and 5 (no 3 or 4 unless new incidents created)
-- 1 = ReportSubmitted (was: New)
-- 2 = InformationGathering (was: InProgress)
-- 5 = Closed (was: Resolved OR Archived)

-- Verify audit log entries created
SELECT COUNT(*) as audit_entries_created
FROM "IncidentAuditLog"
WHERE "ActionType" = 'StatusEnumMigration';

-- Should equal total number of incidents migrated
```

**Success Criteria**: All status values in range 1-5, audit log entries created for all migrations

#### Step 3.5: Update Code References

**CRITICAL**: Update ALL code files referencing old enum values

**Files to Update**:
1. `/apps/api/Features/Safety/Entities/SafetyIncident.cs` - Enum definition ✅
2. All API endpoints using IncidentStatus
3. All service classes using IncidentStatus
4. All tests using IncidentStatus
5. Database seed data using IncidentStatus

**Example Code Updates**:
```csharp
// OLD
if (incident.Status == IncidentStatus.New) { ... }
if (incident.Status == IncidentStatus.InProgress) { ... }
if (incident.Status == IncidentStatus.Resolved) { ... }
if (incident.Status == IncidentStatus.Archived) { ... }

// NEW
if (incident.Status == IncidentStatus.ReportSubmitted) { ... }
if (incident.Status == IncidentStatus.InformationGathering) { ... }
if (incident.Status == IncidentStatus.Closed) { ... }
// Add new statuses
if (incident.Status == IncidentStatus.ReviewingFinalReport) { ... }
if (incident.Status == IncidentStatus.OnHold) { ... }
```

#### Step 3.6: Regenerate NSwag TypeScript Types

```bash
cd /home/chad/repos/witchcityrope-react/apps/api

# Regenerate API types
dotnet build
dotnet run --launch-profile "NSwag"  # Or your OpenAPI generation profile

# This will update:
# /packages/shared-types/src/api-types.ts
```

#### Step 3.7: Update Frontend Code

After NSwag regeneration, update React components:

```typescript
// OLD
import { IncidentStatus } from '@witchcityrope/shared-types';

if (incident.status === IncidentStatus.New) { ... }
if (incident.status === IncidentStatus.InProgress) { ... }
if (incident.status === IncidentStatus.Resolved) { ... }
if (incident.status === IncidentStatus.Archived) { ... }

// NEW
import { IncidentStatus } from '@witchcityrope/shared-types';

if (incident.status === IncidentStatus.ReportSubmitted) { ... }
if (incident.status === IncidentStatus.InformationGathering) { ... }
if (incident.status === IncidentStatus.Closed) { ... }
if (incident.status === IncidentStatus.ReviewingFinalReport) { ... }
if (incident.status === IncidentStatus.OnHold) { ... }
```

**Success Criteria**: All code compiled, all tests pass, NSwag types regenerated

---

## Post-Migration Validation

### Comprehensive Testing

#### Test 1: Verify Schema Changes
```sql
-- Verify SafetyIncident columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'SafetyIncidents'
ORDER BY ordinal_position;

-- Verify IncidentNote table
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'IncidentNotes'
ORDER BY ordinal_position;
```

#### Test 2: Verify Indexes
```sql
-- SafetyIncident indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'SafetyIncidents'
ORDER BY indexname;

-- IncidentNote indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'IncidentNotes'
ORDER BY indexname;
```

#### Test 3: Verify Foreign Keys
```sql
SELECT
    tc.table_name,
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name,
    rc.delete_rule
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.referential_constraints AS rc
    ON tc.constraint_name = rc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
AND tc.table_name IN ('SafetyIncidents', 'IncidentNotes')
ORDER BY tc.table_name, tc.constraint_name;
```

#### Test 4: Verify Data Integrity
```sql
-- Verify status values are valid
SELECT "Status", COUNT(*) as count
FROM "SafetyIncidents"
GROUP BY "Status"
ORDER BY "Status";

-- Should only show: 1, 2, 5 (and potentially 3, 4 if new incidents created)

-- Verify no orphaned records
SELECT COUNT(*) as orphaned_notes
FROM "IncidentNotes" n
LEFT JOIN "SafetyIncidents" i ON n."IncidentId" = i."Id"
WHERE i."Id" IS NULL;
-- Should return: 0

-- Verify coordinator assignments
SELECT COUNT(*) as assigned_incidents
FROM "SafetyIncidents"
WHERE "CoordinatorId" IS NOT NULL;
-- Should return: 0 (no coordinators assigned yet in fresh migration)
```

#### Test 5: Integration Tests

Run complete test suite:
```bash
cd /home/chad/repos/witchcityrope-react/apps/api

# Unit tests
dotnet test

# Integration tests (requires Docker PostgreSQL)
docker-compose up -d postgres
dotnet test --filter "Category=Integration"
```

**Success Criteria**: All tests pass, no schema warnings, foreign keys intact

---

## Rollback Procedures

### Scenario 1: Rollback Before Status Enum Migration

**If you need to rollback BEFORE running status enum migration:**

```bash
# Revert to previous migration
dotnet ef database update [PreviousMigrationName] --context ApplicationDbContext

# Or revert all incident reporting migrations
dotnet ef database update CreateIncidentNotificationTable --context ApplicationDbContext
```

**SQL Rollback**:
```sql
BEGIN TRANSACTION;

-- Drop new indexes
DROP INDEX IF EXISTS "IX_SafetyIncidents_CoordinatorId_Status";
DROP INDEX IF EXISTS "IX_SafetyIncidents_Status_CoordinatorId";
DROP INDEX IF EXISTS "IX_SafetyIncidents_UpdatedAt";

-- Drop IncidentNotes table
DROP TABLE IF EXISTS "IncidentNotes" CASCADE;

-- Drop new columns
ALTER TABLE "SafetyIncidents" DROP COLUMN IF EXISTS "CoordinatorId";
ALTER TABLE "SafetyIncidents" DROP COLUMN IF EXISTS "GoogleDriveFolderUrl";
ALTER TABLE "SafetyIncidents" DROP COLUMN IF EXISTS "GoogleDriveFinalReportUrl";

COMMIT;
```

---

### Scenario 2: Rollback After Status Enum Migration

**⚠️ CRITICAL**: Rollback after enum migration requires reversing data changes

**SQL Rollback**:
```sql
BEGIN TRANSACTION;

-- Log rollback start
INSERT INTO "IncidentAuditLog" (
    "Id", "IncidentId", "UserId", "ActionType", "ActionDescription",
    "OldValues", "NewValues", "CreatedAt"
)
SELECT
    gen_random_uuid(),
    "Id",
    NULL,
    'StatusEnumRollback',
    'Rolling back from 5-stage to 4-stage status enum',
    json_build_object('OldStatus', "Status")::text,
    json_build_object('NewStatus',
        CASE
            WHEN "Status" = 1 THEN 1  -- ReportSubmitted → New
            WHEN "Status" = 2 THEN 2  -- InformationGathering → InProgress
            WHEN "Status" = 3 THEN 3  -- ReviewingFinalReport → Resolved
            WHEN "Status" = 4 THEN 2  -- OnHold → InProgress (best approximation)
            WHEN "Status" = 5 THEN 3  -- Closed → Resolved
            ELSE "Status"
        END
    )::text,
    NOW()
FROM "SafetyIncidents"
WHERE "Status" IN (1, 2, 3, 4, 5);

-- Reverse status values
UPDATE "SafetyIncidents"
SET "Status" = CASE
    WHEN "Status" = 1 THEN 1  -- ReportSubmitted → New (1)
    WHEN "Status" = 2 THEN 2  -- InformationGathering → InProgress (2)
    WHEN "Status" = 3 THEN 3  -- ReviewingFinalReport → Resolved (3)
    WHEN "Status" = 4 THEN 2  -- OnHold → InProgress (2) - LOSSY CONVERSION
    WHEN "Status" = 5 THEN 3  -- Closed → Resolved (3)
    ELSE "Status"
END,
"UpdatedAt" = NOW()
WHERE "Status" IN (1, 2, 3, 4, 5);

-- Verify rollback
SELECT "Status", COUNT(*) as count
FROM "SafetyIncidents"
GROUP BY "Status"
ORDER BY "Status";

-- Should only show: 1, 2, 3 (old enum values)

COMMIT;
```

**⚠️ WARNING**: Rollback is LOSSY:
- ReviewingFinalReport (3) → Resolved (3) ✅ Recoverable
- OnHold (4) → InProgress (2) ❌ LOSSY - cannot distinguish from real InProgress
- Closed (5) → Resolved (3) ✅ Recoverable

**Recommendation**: Do NOT rollback after production deployment. Forward-only migrations preferred.

---

## Emergency Recovery Procedures

### Database Corruption

If migration causes corruption:

1. **Stop all services**:
   ```bash
   docker-compose down
   ```

2. **Restore from backup**:
   ```bash
   # Restore PostgreSQL backup
   psql -h localhost -U postgres -d witchcityrope_dev < backup-YYYY-MM-DD.sql
   ```

3. **Verify restoration**:
   ```sql
   SELECT COUNT(*) FROM "SafetyIncidents";
   SELECT "Status", COUNT(*) FROM "SafetyIncidents" GROUP BY "Status";
   ```

4. **Investigate migration failure**: Review logs, fix migration script, retry

### Partial Migration Failure

If migration fails partway through:

1. **Check database state**:
   ```sql
   -- Check which columns exist
   SELECT column_name FROM information_schema.columns
   WHERE table_name = 'SafetyIncidents';

   -- Check which tables exist
   SELECT table_name FROM information_schema.tables
   WHERE table_schema = 'public';
   ```

2. **Identify failed step**: Compare against expected schema

3. **Manual completion**: Run remaining migration steps manually

4. **Update migration history**:
   ```sql
   -- If EF migration history is out of sync
   SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
   ```

---

## Monitoring During Migration

### Key Metrics

**Before Migration**:
```sql
SELECT
    'Before Migration' as checkpoint,
    COUNT(*) as total_incidents,
    COUNT(DISTINCT "Status") as distinct_statuses,
    MIN("CreatedAt") as oldest_incident,
    MAX("CreatedAt") as newest_incident
FROM "SafetyIncidents";
```

**After Migration**:
```sql
SELECT
    'After Migration' as checkpoint,
    COUNT(*) as total_incidents,
    COUNT(DISTINCT "Status") as distinct_statuses,
    MIN("CreatedAt") as oldest_incident,
    MAX("CreatedAt") as newest_incident,
    SUM(CASE WHEN "CoordinatorId" IS NOT NULL THEN 1 ELSE 0 END) as assigned_count,
    (SELECT COUNT(*) FROM "IncidentNotes") as total_notes
FROM "SafetyIncidents";
```

### Performance Validation

```sql
-- Query performance test: Unassigned queue
EXPLAIN ANALYZE
SELECT * FROM "SafetyIncidents"
WHERE "Status" = 1 AND "CoordinatorId" IS NULL
ORDER BY "ReportedAt" DESC;

-- Expected: Uses IX_SafetyIncidents_Status_CoordinatorId index

-- Query performance test: Coordinator workload
EXPLAIN ANALYZE
SELECT * FROM "SafetyIncidents"
WHERE "CoordinatorId" = '00000000-0000-0000-0000-000000000000'::uuid
AND "Status" IN (2, 3, 4)
ORDER BY "UpdatedAt" DESC;

-- Expected: Uses IX_SafetyIncidents_CoordinatorId_Status index

-- Query performance test: Notes retrieval
EXPLAIN ANALYZE
SELECT * FROM "IncidentNotes"
WHERE "IncidentId" = '00000000-0000-0000-0000-000000000000'::uuid
ORDER BY "CreatedAt" DESC;

-- Expected: Uses IX_IncidentNotes_IncidentId_CreatedAt index
```

---

## Communication Plan

### Pre-Migration Notification

**Email Template**:
```
Subject: [MAINTENANCE] Incident Reporting System Upgrade - [DATE]

Team,

We will be upgrading the Incident Reporting database on [DATE] at [TIME].

BREAKING CHANGES:
- IncidentStatus enum values are changing (4-stage → 5-stage)
- All code referencing old enum values will need updates

EXPECTED IMPACT:
- Downtime: 5 minutes (if production has no data)
- Code updates required: YES
- NSwag regeneration required: YES

TIMELINE:
1. [TIME]: Begin maintenance window
2. [TIME+5min]: Complete database migration
3. [TIME+10min]: Verify schema changes
4. [TIME+15min]: Resume normal operations

Please do not deploy code changes until migration is complete.

Questions? Contact [Database Team]
```

### Post-Migration Notification

**Email Template**:
```
Subject: [COMPLETE] Incident Reporting System Upgrade - [DATE]

Team,

The Incident Reporting database upgrade is complete.

COMPLETED ACTIONS:
✅ Added CoordinatorId field
✅ Added Google Drive link fields
✅ Created IncidentNote table
✅ Migrated IncidentStatus enum (4-stage → 5-stage)
✅ Regenerated NSwag TypeScript types

NEXT STEPS FOR DEVELOPERS:
1. Pull latest code (includes enum updates)
2. Run `npm install` to get updated shared-types
3. Update any local code using old enum values:
   - IncidentStatus.New → IncidentStatus.ReportSubmitted
   - IncidentStatus.InProgress → IncidentStatus.InformationGathering
   - IncidentStatus.Resolved → IncidentStatus.Closed
   - IncidentStatus.Archived → IncidentStatus.Closed

VALIDATION:
- All tests passing: ✅
- Schema validated: ✅
- Performance verified: ✅

Migration logs available: [LINK]

Questions? Contact [Database Team]
```

---

## Appendix A: Complete Migration Timeline

### Development Environment (Estimated)

| Time | Duration | Step | Responsibility |
|------|----------|------|----------------|
| T+0  | 5 min | Create and review migrations | Database Developer |
| T+5  | 2 min | Apply Phase 1 migration (new columns) | Database Developer |
| T+7  | 2 min | Apply Phase 2 migration (IncidentNote) | Database Developer |
| T+9  | 5 min | Run status enum data migration | Database Developer |
| T+14 | 3 min | Update code references to new enum | Backend Developer |
| T+17 | 2 min | Regenerate NSwag types | Backend Developer |
| T+19 | 5 min | Update frontend code | React Developer |
| T+24 | 5 min | Run test suite | Test Developer |
| T+29 | 1 min | Verify completion | Database Developer |

**Total Time**: ~30 minutes

### Production Environment (Estimated)

| Time | Duration | Step | Responsibility |
|------|----------|------|----------------|
| T+0  | 1 min | Begin maintenance window | DevOps |
| T+1  | 1 min | Create database backup | DevOps |
| T+2  | 2 min | Apply all migrations | Database Developer |
| T+4  | 1 min | Run validation queries | Database Developer |
| T+5  | 0 min | End maintenance window | DevOps |

**Total Time**: ~5 minutes (assuming no existing incidents to migrate)

---

## Appendix B: Testing Procedures

### Manual Test Cases

#### Test Case 1: Create Incident with New Status
```csharp
[Fact]
public async Task CreateIncident_WithReportSubmittedStatus_Succeeds()
{
    // Arrange
    var incident = new SafetyIncident
    {
        ReferenceNumber = "SAF-20251018-0001",
        Severity = IncidentSeverity.High,
        Status = IncidentStatus.ReportSubmitted,  // New enum value
        Location = "Test Location",
        EncryptedDescription = "Test Description"
    };

    // Act
    await _context.SafetyIncidents.AddAsync(incident);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.SafetyIncidents.FindAsync(incident.Id);
    Assert.NotNull(saved);
    Assert.Equal(IncidentStatus.ReportSubmitted, saved.Status);
}
```

#### Test Case 2: Assign Coordinator
```csharp
[Fact]
public async Task AssignCoordinator_UpdatesCoordinatorId_Succeeds()
{
    // Arrange
    var incident = await CreateTestIncidentAsync();
    var coordinator = await CreateTestUserAsync();

    // Act
    incident.CoordinatorId = coordinator.Id;
    incident.Status = IncidentStatus.InformationGathering;
    await _context.SaveChangesAsync();

    // Assert
    var updated = await _context.SafetyIncidents
        .Include(i => i.Coordinator)
        .FirstAsync(i => i.Id == incident.Id);
    Assert.NotNull(updated.Coordinator);
    Assert.Equal(coordinator.Id, updated.CoordinatorId);
}
```

#### Test Case 3: Create Manual Note
```csharp
[Fact]
public async Task CreateNote_WithValidData_Succeeds()
{
    // Arrange
    var incident = await CreateTestIncidentAsync();
    var author = await CreateTestUserAsync();
    var note = new IncidentNote
    {
        IncidentId = incident.Id,
        Content = "Test investigation note",
        Type = IncidentNoteType.Manual,
        AuthorId = author.Id,
        IsPrivate = false
    };

    // Act
    await _context.IncidentNotes.AddAsync(note);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.IncidentNotes
        .Include(n => n.Author)
        .FirstAsync(n => n.Id == note.Id);
    Assert.NotNull(saved);
    Assert.Equal("Test investigation note", saved.Content);
    Assert.Equal(IncidentNoteType.Manual, saved.Type);
}
```

#### Test Case 4: Create System Note
```csharp
[Fact]
public async Task CreateSystemNote_WithNullAuthor_Succeeds()
{
    // Arrange
    var incident = await CreateTestIncidentAsync();
    var note = new IncidentNote
    {
        IncidentId = incident.Id,
        Content = "Status changed from ReportSubmitted to InformationGathering",
        Type = IncidentNoteType.System,
        AuthorId = null,  // System notes have no author
        IsPrivate = false
    };

    // Act
    await _context.IncidentNotes.AddAsync(note);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.IncidentNotes.FindAsync(note.Id);
    Assert.NotNull(saved);
    Assert.Null(saved.AuthorId);
    Assert.Equal(IncidentNoteType.System, saved.Type);
}
```

---

**Created**: 2025-10-18
**Author**: Database Designer Agent
**Version**: 1.0
**Status**: Ready for Backend Developer Review
