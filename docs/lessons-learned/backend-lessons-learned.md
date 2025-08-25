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

## Success Metrics

- ✅ All 16 tables created successfully
- ✅ Database seeding works (5 users, 10 events, 2 vetting applications)
- ✅ API starts without errors
- ✅ Health checks pass
- ✅ Event Session Matrix tables functional