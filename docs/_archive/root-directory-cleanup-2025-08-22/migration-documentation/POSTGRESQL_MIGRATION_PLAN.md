# PostgreSQL Migration Plan

## Overview
This document outlines the migration strategy from SQLite to PostgreSQL for the WitchCityRope application.

## Migration Objectives
- Migrate from SQLite to PostgreSQL for better scalability and performance
- Maintain data integrity during the migration process
- Minimize application downtime
- Support both development and production environments

## Current State
- **Database**: SQLite
- **Location**: `src/WitchCityRope.Api/witchcityrope.db` (production)
- **Dev Database**: `src/WitchCityRope.Api/witchcityrope_dev.db` (development)
- **ORM**: Entity Framework Core with SQLite provider

## Target State
- **Database**: PostgreSQL 16
- **Connection String**: `postgresql://postgres:password@localhost:5432/witchcityrope_db`
- **ORM**: Entity Framework Core with Npgsql provider
- **Deployment**: Docker container for local development, managed PostgreSQL for production

## Migration Steps

### Phase 1: Environment Setup
1. **Set up PostgreSQL Docker Container**
   ```bash
   docker-compose -f docker-compose.postgres.yml up -d
   ```

2. **Update NuGet Packages**
   - Remove: `Microsoft.EntityFrameworkCore.Sqlite`
   - Add: `Npgsql.EntityFrameworkCore.PostgreSQL`
   - Update: Entity Framework Core packages to latest version

3. **Update Connection Strings**
   - Update `appsettings.json` in all projects
   - Update `appsettings.Development.json` with local Docker connection
   - Update `appsettings.Staging.json` and `appsettings.Production.json` accordingly

### Phase 2: Code Migration
1. **Update DbContext Configuration**
   - Modify `WitchCityRopeDbContext` to use PostgreSQL
   - Update `Program.cs` to configure Npgsql
   - Review and update any SQLite-specific configurations

2. **Migration Adjustments**
   - Review existing migrations for SQLite-specific syntax
   - Create new initial migration for PostgreSQL
   - Test migrations on fresh PostgreSQL database

3. **Data Type Mappings**
   - Review and update data type mappings:
     - SQLite `INTEGER` → PostgreSQL `INTEGER` or `BIGINT`
     - SQLite `TEXT` → PostgreSQL `VARCHAR` or `TEXT`
     - SQLite `REAL` → PostgreSQL `DECIMAL` or `DOUBLE PRECISION`
     - SQLite `BLOB` → PostgreSQL `BYTEA`
     - DateTime handling differences

### Phase 3: Data Migration
1. **Export Data from SQLite**
   ```bash
   # Use SQLite tools to export data
   sqlite3 witchcityrope.db .dump > sqlite_dump.sql
   ```

2. **Transform Data**
   - Create transformation scripts to convert SQLite dump to PostgreSQL format
   - Handle sequence/identity columns
   - Convert date/time formats
   - Update boolean representations

3. **Import to PostgreSQL**
   ```bash
   # Import transformed data
   psql -U postgres -d witchcityrope_db -f postgresql_dump.sql
   ```

### Phase 4: Testing
1. **Unit Tests**
   - Update test database configurations
   - Run all existing tests against PostgreSQL
   - Fix any failing tests due to database differences

2. **Integration Tests**
   - Test all CRUD operations
   - Verify transaction handling
   - Test concurrent access scenarios

3. **Performance Testing**
   - Compare query performance between SQLite and PostgreSQL
   - Optimize indexes and queries as needed
   - Test under load conditions

### Phase 5: Deployment
1. **Staging Deployment**
   - Deploy to staging environment with PostgreSQL
   - Run full regression tests
   - Monitor for any issues

2. **Production Migration**
   - Schedule maintenance window
   - Backup current SQLite database
   - Deploy PostgreSQL version
   - Verify data integrity
   - Monitor application performance

## Rollback Plan
1. Keep SQLite database files as backup
2. Maintain ability to switch connection strings
3. Document rollback procedures
4. Test rollback process in staging

## Timeline
- **Week 1**: Environment setup and code migration
- **Week 2**: Data migration scripts and testing
- **Week 3**: Staging deployment and testing
- **Week 4**: Production migration

## Risks and Mitigations
| Risk | Impact | Mitigation |
|------|--------|------------|
| Data loss during migration | High | Multiple backups, test migrations |
| Performance degradation | Medium | Performance testing, query optimization |
| Compatibility issues | Medium | Thorough testing, staged rollout |
| Extended downtime | Low | Practice migration, optimize scripts |

## Success Criteria
- [ ] All data successfully migrated with 100% integrity
- [ ] All tests passing with PostgreSQL
- [ ] Application performance meets or exceeds SQLite baseline
- [ ] Zero data loss incidents
- [ ] Minimal downtime (< 30 minutes for production)

## Resources Required
- PostgreSQL 16 Docker image
- pgAdmin for database management
- Migration scripts and tools
- Testing environment
- Backup storage

## Post-Migration Tasks
- [ ] Remove SQLite dependencies
- [ ] Update documentation
- [ ] Train team on PostgreSQL management
- [ ] Set up PostgreSQL monitoring and backups
- [ ] Optimize PostgreSQL configuration for production workload