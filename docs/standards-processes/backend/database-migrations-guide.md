# Entity Framework Core Database Migrations Guide

**Version**: 1.0
**Last Updated**: 2025-10-24
**Owner**: Backend Development Team
**Status**: Active Standard

## Purpose

This guide establishes standards and best practices for Entity Framework Core database migrations in the WitchCityRope project. Following these standards prevents common migration issues, ensures database schema consistency, and maintains proper migration tracking.

## Table of Contents

1. [Migration Fundamentals](#migration-fundamentals)
2. [Current Project State](#current-project-state)
3. [Creating New Migrations](#creating-new-migrations)
4. [Migration Best Practices](#migration-best-practices)
5. [Common Problems and Solutions](#common-problems-and-solutions)
6. [Automatic Migration Application](#automatic-migration-application)
7. [Testing Migrations](#testing-migrations)
8. [Deployment Considerations](#deployment-considerations)

## Migration Fundamentals

### What Are Migrations?

Entity Framework Core migrations are a way to incrementally evolve the database schema as your application models change. Each migration represents a set of changes to the database schema, and migrations are applied in order to bring the database to its current state.

### Migration Components

**Migration File** (`YYYYMMDDHHMMSS_MigrationName.cs`):
- `Up()` method: Applies the schema changes
- `Down()` method: Reverts the schema changes

**Model Snapshot** (`ApplicationDbContextModelSnapshot.cs`):
- Represents the current state of your EF Core model
- Used by EF to detect changes and generate new migrations

**Migration History Table** (`__EFMigrationsHistory`):
- PostgreSQL table tracking which migrations have been applied
- Created automatically by EF Core on first migration
- **DO NOT** manually modify this table

### Why Migrations Matter

✅ **Version Control**: Database schema changes are tracked in git
✅ **Consistency**: All environments (dev, staging, production) have identical schemas
✅ **Rollback**: Migrations can be reverted if needed
✅ **Team Coordination**: Multiple developers can work on schema changes safely
✅ **Deployment**: Schema changes are applied automatically during deployment

## Current Project State

### Migration Location

**Directory**: `/apps/api/Migrations/`
**Namespace**: `WitchCityRope.Api.Migrations`

This is EF Core's default behavior when migrations are created from the `/apps/api` directory where the `.csproj` file is located.

### Initial Migration

**File**: `20251024232104_InitialMigration.cs`
**Size**: 2,674 lines (~132KB)
**Purpose**: Contains the complete database schema as of October 2025

This initial migration was created after resetting all previous migrations. Future migrations will only contain incremental changes from this baseline.

**Key Tables** (47 total):
- `Users`, `Roles`, `UserRoles`, `UserClaims` (Identity)
- `Events`, `Sessions`, `TicketTypes`, `TicketPurchases` (Events)
- `VettingApplications`, `VettingEmailTemplates` (Vetting)
- `SafetyIncidents`, `IncidentNotes` (Safety)
- `EventParticipations`, `ParticipationHistory` (Participation)
- `Payments`, `PaymentRefunds` (Payments)
- `ContentPages`, `ContentRevisions` (CMS)
- And many more...

### Database Technology

**Database**: PostgreSQL 15+
**Connection**: Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=devpass123
**Schemas**: `public`, `cms`

## Creating New Migrations

### Prerequisites

1. .NET 8.0 SDK installed
2. EF Core tools installed:
   ```bash
   dotnet tool install --global dotnet-ef
   # Or update existing tools
   dotnet tool update --global dotnet-ef
   ```

### Step-by-Step Process

#### 1. Make Entity Model Changes

Edit entity classes in:
- `/apps/api/Models/` (core models)
- `/apps/api/Features/[Feature]/Entities/` (feature-specific entities)

Example: Adding a new property
```csharp
// In /apps/api/Models/Event.cs
public class Event
{
    // Existing properties...

    // NEW: Add capacity warning threshold
    public int? CapacityWarningThreshold { get; set; }
}
```

#### 2. Navigate to Project Directory

**CRITICAL**: Always run migration commands from `/apps/api` directory

```bash
cd /apps/api
```

#### 3. Create Migration

```bash
# Format: dotnet ef migrations add <DescriptiveName>
dotnet ef migrations add AddCapacityWarningThreshold
```

**✅ DO: Use descriptive names**
- `AddCapacityWarningThreshold`
- `UpdateEventPricingStructure`
- `RemoveDeprecatedUserFields`
- `CreateSafetyIncidentArchive`

**❌ DON'T: Use vague names**
- `Migration1`
- `Updates`
- `Fix`
- `Changes`

#### 4. Review Generated Migration

```bash
# View the new migration file
cat Migrations/*_AddCapacityWarningThreshold.cs
```

**Verify**:
- `Up()` method contains expected schema changes
- Column types match your property types
- Foreign keys are correctly configured
- Indexes are created where needed

Example of what you should see:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "CapacityWarningThreshold",
        schema: "public",
        table: "Events",
        type: "integer",
        nullable: true);
}
```

#### 5. Test Migration Locally

```bash
# From repository root
cd ../..

# Stop containers and remove volumes (fresh database)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v

# Start containers (migration applies automatically)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Check logs to verify migration applied
docker logs witchcity-api --tail 100 | grep -i migration

# Expected output:
# "Successfully applied 1 migrations"
```

#### 6. Verify Database Schema

```bash
# Connect to PostgreSQL
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev

# Check table structure
\d "Events"

# You should see your new column:
# CapacityWarningThreshold | integer |

# Exit PostgreSQL
\q
```

#### 7. Commit Migration Files

```bash
git add apps/api/Migrations/*_AddCapacityWarningThreshold.cs
git add apps/api/Migrations/ApplicationDbContextModelSnapshot.cs
git commit -m "feat(database): add capacity warning threshold to Events table"
```

## Migration Best Practices

### ✅ DO

1. **Always run from /apps/api directory**
   ```bash
   cd /apps/api
   dotnet ef migrations add YourMigration
   ```

2. **Use descriptive migration names**
   - Good: `AddEmailVerificationToUsers`
   - Bad: `Migration123`

3. **Test migrations before committing**
   - Drop database: `docker-compose down -v`
   - Rebuild: `docker-compose up -d`
   - Verify: Check logs and database schema

4. **Review generated SQL**
   ```bash
   dotnet ef migrations script --output migration.sql
   ```

5. **Keep migrations focused**
   - One logical change per migration
   - Easier to review and rollback

6. **Document complex migrations**
   - Add XML comments explaining why
   - Reference related tickets/issues

### ❌ DON'T

1. **NEVER use --output-dir flag**
   ```bash
   # ❌ WRONG: Creates multiple migration directories
   dotnet ef migrations add MyMigration --output-dir Infrastructure/Data/Migrations

   # ✅ CORRECT: Uses default location
   dotnet ef migrations add MyMigration
   ```

   **Why**: The `--output-dir` flag causes EF to create separate migration directories, leading to:
   - Empty migration files generated in the wrong location
   - Confusion about which migrations are active
   - Schema changes not being captured correctly
   - Multiple `ApplicationDbContextModelSnapshot.cs` files

2. **Don't modify existing migrations**
   - Once applied, migrations are immutable
   - Create a new migration to fix issues

3. **Don't manually edit the database schema**
   - Always use migrations
   - Manual changes will be lost on next deployment

4. **Don't delete migration files carelessly**
   - Migrations are version control for your database
   - Only delete if migration was NEVER applied to any environment

5. **Don't skip migrations**
   - Apply migrations in order
   - Missing migrations cause deployment failures

6. **Don't use EnsureCreated() in production code**
   ```csharp
   // ❌ WRONG: Bypasses migration system
   await context.Database.EnsureCreatedAsync();

   // ✅ CORRECT: Uses migration system
   await context.Database.MigrateAsync();
   ```

## Common Problems and Solutions

### Problem 1: Empty Migration File Created

**Symptom**: Migration file has no `Up()` or `Down()` content

**Cause**: EF detected no model changes

**Solution**:
```bash
# Delete the empty migration
dotnet ef migrations remove

# Verify you actually made model changes
# Check git diff to see what changed
git diff

# Make your entity changes, then create migration again
dotnet ef migrations add YourMigration
```

### Problem 2: Migrations in Wrong Directory

**Symptom**: Migrations appear in `Infrastructure/Data/Migrations/` or `Data/Migrations/` instead of `/apps/api/Migrations/`

**Cause**: Used `--output-dir` flag (DON'T DO THIS!)

**Solution**:
```bash
# 1. Delete incorrect migration directories
rm -rf apps/api/Infrastructure/Data/Migrations
rm -rf apps/api/Data/Migrations

# 2. Keep only the correct directory
# /apps/api/Migrations/ should be the ONLY migration directory

# 3. Create migrations correctly (without --output-dir)
cd apps/api
dotnet ef migrations add YourMigration
```

### Problem 3: "Migration Already Applied" Error

**Symptom**: Migration exists in `__EFMigrationsHistory` table but not in code

**Cause**: Migration files were deleted from code but database still has record

**Solution**:
```bash
# Check migration history
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT * FROM \"__EFMigrationsHistory\";"

# Remove orphaned migration record
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = 'YourMigrationId';"

# Or reset completely (only for development!)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Problem 4: Schema and Migrations Out of Sync

**Symptom**: Database schema doesn't match migration files

**Cause**: Manual database changes or missing migrations

**Solution**:
```bash
# Generate SQL script to see what EF thinks needs to change
cd apps/api
dotnet ef migrations script --output current-state.sql

# Review the script
cat current-state.sql

# If needed, create a sync migration
dotnet ef migrations add SyncSchemaChanges

# Apply the migration
cd ../..
docker-compose restart api
```

### Problem 5: Migration Application Timeout

**Symptom**: "Database initialization exceeded 30-second timeout"

**Cause**: PostgreSQL not ready or network issues

**Solution**:
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Check PostgreSQL logs
docker logs witchcity-postgres --tail 50

# Increase timeout in appsettings.json (if needed)
"DatabaseInitialization": {
  "TimeoutSeconds": 60  // Increase from 30 to 60
}

# Restart API
docker-compose restart api
```

## Automatic Migration Application

### How It Works

**Service**: `DatabaseInitializationService.cs`
**Trigger**: API container startup
**Method**: `MigrateAsync()` via BackgroundService

The service automatically:
1. Connects to PostgreSQL with retry logic (3 attempts, exponential backoff)
2. Checks for pending migrations
3. Applies all pending migrations in order
4. Populates seed data (if configured for environment)
5. Logs success or failure

### Configuration

Set in `appsettings.json` or environment variables:

```json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,          // Apply migrations automatically
    "EnableSeedData": true,               // Load seed data
    "TimeoutSeconds": 30,                 // Maximum wait time
    "MaxRetryAttempts": 3,                // Retry count for transient failures
    "RetryDelaySeconds": 2.0,             // Initial retry delay (exponential backoff)
    "FailOnSeedDataError": true,          // Fail startup if seed data fails
    "ExcludedEnvironments": ["Production"] // Skip seed data in these environments
  }
}
```

### Verifying Automatic Migration

```bash
# Check API startup logs
docker logs witchcity-api --tail 100 | grep "Database initialization"

# Expected output:
# [INFO] Starting database initialization for environment: Development
# [INFO] Phase 1: Applying pending migrations
# [INFO] Applying 2 pending migrations
# [INFO] Successfully applied 2 migrations
# [INFO] Phase 2: Populating seed data
# [INFO] Database initialization completed successfully in 1234ms
```

### When It Doesn't Work

If automatic migration fails, check:

1. **PostgreSQL Connection**: `docker logs witchcity-postgres`
2. **API Configuration**: Verify connection string in `docker-compose.dev.yml`
3. **Migration Files**: Ensure migrations exist in `/apps/api/Migrations/`
4. **Permissions**: PostgreSQL user has CREATE/ALTER permissions

## Testing Migrations

### Local Testing (Docker)

**Fresh Database Test** (Recommended before every commit):
```bash
# 1. Stop and remove all data
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v

# 2. Start fresh (migrations apply automatically)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# 3. Check logs
docker logs witchcity-api --tail 100

# 4. Verify database
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "\dt"

# 5. Count seed data
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT COUNT(*) FROM \"Users\";"
```

### Manual Migration Testing

If you need to test migrations manually (without automatic application):

```bash
# Temporarily disable auto-migration
# Edit docker-compose.dev.yml:
# DatabaseInitialization__EnableAutoMigration: "false"

# Restart API
docker-compose restart api

# Apply migrations manually
docker exec -it witchcity-api bash
cd /app
dotnet ef database update
exit
```

### Migration Rollback Testing

```bash
# Rollback to specific migration
docker exec -it witchcity-api bash
cd /app
dotnet ef database update PreviousMigrationName
exit

# Verify schema rolled back
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev
\d YourTable
\q
```

## Deployment Considerations

### Staging Deployment

Migrations are applied automatically by `DatabaseInitializationService` on API startup.

**See**: [/docs/functional-areas/deployment/staging-deployment-guide.md](../../functional-areas/deployment/staging-deployment-guide.md)

**Configuration**: Set `EnableAutoMigration: true` in staging environment

**Verification**:
```bash
# SSH to staging server
ssh deploy@staging.witchcityrope.com

# Check migration status
docker logs wcr-staging-api | grep "Database initialization"

# Verify applied migrations
docker exec wcr-staging-postgres psql -U postgres -d witchcityrope_staging -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

### Production Deployment

**IMPORTANT**: Consider production-specific strategies:

1. **Pre-deployment SQL Review**
   ```bash
   # Generate SQL script for review
   cd apps/api
   dotnet ef migrations script --idempotent --output production-migration.sql

   # Review script with DBA
   # Apply manually if needed
   ```

2. **Blue-Green Deployment**
   - Apply migrations to staging environment first
   - Test thoroughly
   - Deploy to production with confidence

3. **Rollback Plan**
   - Always have a rollback strategy
   - Test rollback migrations in staging
   - Keep database backups before applying migrations

4. **Zero-Downtime Migrations**
   - Add columns as nullable first
   - Backfill data
   - Make columns required in separate migration
   - Drop columns only after code deployed

### CI/CD Integration

Migrations are applied automatically in deployment pipeline:

1. **Build**: Docker image includes migration files
2. **Deploy**: Container starts with new code
3. **Startup**: `DatabaseInitializationService` applies pending migrations
4. **Verify**: Health checks confirm successful startup

## Resetting Migrations (Rare)

**WARNING**: This is a destructive operation. Only use when starting fresh.

### When to Reset

- Initial project setup
- Major database restructuring (rare)
- Consolidating too many incremental migrations (rare)
- **NEVER** in production environment

### Reset Procedure

```bash
# 1. Backup any important data first!
docker exec witchcity-postgres pg_dump -U postgres witchcityrope_dev > backup.sql

# 2. Delete all migration files
rm -rf apps/api/Migrations/*

# 3. Drop database or just migration history
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "DROP TABLE IF EXISTS \"__EFMigrationsHistory\";"

# 4. Create fresh initial migration
cd apps/api
dotnet ef migrations add InitialMigration

# 5. Verify migration contains full schema (should be ~2500+ lines)
wc -l Migrations/*_InitialMigration.cs

# 6. Test with fresh database
cd ../..
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# 7. Verify migration applied
docker logs witchcity-api --tail 100 | grep migration
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

## References

- **Code Documentation**: [/apps/api/Data/ApplicationDbContext.cs](../../../apps/api/Data/ApplicationDbContext.cs) - See XML comments on class
- **Docker Guide**: [/DOCKER_DEV_GUIDE.md](../../../DOCKER_DEV_GUIDE.md) - Database Migrations section
- **Staging Deployment**: [/docs/functional-areas/deployment/staging-deployment-guide.md](../../functional-areas/deployment/staging-deployment-guide.md) - Section 5
- **DatabaseInitializationService**: [/apps/api/Services/DatabaseInitializationService.cs](../../../apps/api/Services/DatabaseInitializationService.cs)

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-24 | Backend Team | Initial comprehensive guide consolidating migration best practices, troubleshooting, and deployment considerations |

## Questions?

For migration-related questions or issues:
1. Check this guide first
2. Review code documentation in `ApplicationDbContext.cs`
3. Check Docker development guide
4. Review staging deployment guide
5. Ask in team chat or create GitHub issue
