# ARCHIVE: Consolidated into backend-lessons-learned.md

**This file has been consolidated into [backend-lessons-learned.md](./backend-lessons-learned.md) and should be archived.**

# Lessons Learned - Database Developers
<!-- Last Updated: 2025-08-12 -->
<!-- Next Review: 2025-09-12 -->

## PostgreSQL Specific

### DateTime Handling  
**Reference**: See [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md#datetime-handling-with-postgresql) for complete DateTime UTC implementation with PostgreSQL

### Case Sensitivity
**Issue**: Queries not matching due to case  
**Solution**: Use CITEXT or proper collation
```sql
-- Option 1: CITEXT extension
CREATE EXTENSION IF NOT EXISTS citext;
ALTER TABLE "Users" ALTER COLUMN "Email" TYPE citext;

-- Option 2: Lowercase in queries
WHERE LOWER(email) = LOWER(@email)
```
**Applies to**: Email, username lookups

### JSON Column Support
**Issue**: Complex queries on JSON data  
**Solution**: Use JSONB with indexes
```sql
-- Column definition
metadata JSONB NOT NULL DEFAULT '{}'

-- Index for performance
CREATE INDEX idx_events_metadata ON "Events" USING GIN (metadata);

-- Query example
SELECT * FROM "Events" WHERE metadata @> '{"type": "workshop"}';
```
**Applies to**: Flexible schema requirements

## Migration Best Practices

### Schema Separation
**Issue**: Mixing auth and business tables  
**Solution**: Use schemas for organization
```sql
-- Auth tables
CREATE SCHEMA IF NOT EXISTS auth;
CREATE TABLE auth."Users" (...);

-- Business tables  
CREATE SCHEMA IF NOT EXISTS public;
CREATE TABLE public."Events" (...);
```
**Applies to**: New table creation

### Safe Migration Patterns
**Issue**: Migrations failing in production  
**Solution**: Make migrations reversible
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add nullable column first
    migrationBuilder.AddColumn<string>("NewColumn", "Events", 
        nullable: true);
    
    // Populate data
    migrationBuilder.Sql("UPDATE \"Events\" SET \"NewColumn\" = 'default'");
    
    // Then make non-nullable
    migrationBuilder.AlterColumn<string>("NewColumn", "Events", 
        nullable: false);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn("NewColumn", "Events");
}
```
**Applies to**: All schema changes

### Index Strategy
**Issue**: Slow queries on large tables  
**Solution**: Strategic index placement
```sql
-- Foreign keys (automatic in EF)
CREATE INDEX idx_events_userid ON "Events" ("UserId");

-- Common query patterns
CREATE INDEX idx_events_starttime ON "Events" ("StartTime");
CREATE INDEX idx_events_status ON "Events" ("Status") WHERE "Status" != 'Cancelled';

-- Composite for complex queries
CREATE INDEX idx_rsvp_lookup ON "Rsvps" ("UserId", "EventId");
```
**Applies to**: Tables with >1000 rows

## Performance Optimization

### Connection Pooling
**Issue**: "too many connections" errors  
**Solution**: Configure connection pooling
```
# Connection string
Host=localhost;Database=witchcityrope_db;Username=postgres;Password=xxx;
Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;

# PostgreSQL config
max_connections = 200
shared_buffers = 256MB
```
**Applies to**: All environments

### Query Optimization
**Issue**: Slow queries with multiple joins  
**Solution**: Use EXPLAIN ANALYZE
```sql
EXPLAIN (ANALYZE, BUFFERS) 
SELECT e.*, u."DisplayName"
FROM "Events" e
JOIN "Users" u ON e."OrganizerId" = u."Id"
WHERE e."StartTime" > NOW();

-- Look for:
-- Seq Scan (bad on large tables)
-- High cost numbers
-- Missing indexes
```
**Applies to**: Queries taking >100ms

### Bulk Operations
**Issue**: Slow bulk inserts  
**Solution**: Use COPY or batch inserts
```csharp
// EF Core bulk operations
context.BulkInsert(entities);

// Or raw SQL
COPY "Events" (Id, Title, StartTime) 
FROM STDIN WITH (FORMAT CSV);
```
**Applies to**: Inserting >100 rows

## Data Integrity

### Constraints Beyond EF
**Issue**: EF Core doesn't create all constraints  
**Solution**: Add database-level constraints
```sql
-- Business rule constraints
ALTER TABLE "Events" 
ADD CONSTRAINT chk_event_dates 
CHECK ("EndTime" > "StartTime");

-- Unique constraints
CREATE UNIQUE INDEX idx_unique_email 
ON "Users" (LOWER("Email"));

-- Exclusion constraints (no overlapping events)
ALTER TABLE "Events" ADD CONSTRAINT no_overlap
EXCLUDE USING gist (
    "VenueId" WITH =,
    tsrange("StartTime", "EndTime") WITH &&
);
```
**Applies to**: Critical business rules

### Audit Triggers
**Issue**: Need to track all changes  
**Solution**: Database-level audit
```sql
CREATE TABLE audit_log (
    id SERIAL PRIMARY KEY,
    table_name TEXT,
    operation TEXT,
    user_id UUID,
    changed_at TIMESTAMPTZ DEFAULT NOW(),
    old_data JSONB,
    new_data JSONB
);

CREATE OR REPLACE FUNCTION audit_trigger()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO audit_log(table_name, operation, user_id, old_data, new_data)
    VALUES (TG_TABLE_NAME, TG_OP, current_setting('app.user_id')::UUID,
            row_to_json(OLD), row_to_json(NEW));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```
**Applies to**: Sensitive data tables

## Testing Considerations

### Test Database Setup
**Issue**: Tests polluting each other's data  
**Solution**: Transaction rollback pattern
```csharp
// In test base class
[SetUp]
public void BeginTransaction()
{
    _transaction = _context.Database.BeginTransaction();
}

[TearDown]
public void RollbackTransaction()
{
    _transaction?.Rollback();
}
```
**Applies to**: Integration tests

### Seed Data
**Issue**: Inconsistent test data  
**Solution**: Deterministic seeding
```sql
-- Use specific IDs for test data
INSERT INTO "Users" (Id, Email, DisplayName) VALUES
('11111111-1111-1111-1111-111111111111', 'admin@test.com', 'Test Admin'),
('22222222-2222-2222-2222-222222222222', 'user@test.com', 'Test User');
```
**Applies to**: All test environments

## Common Pitfalls

### UUID vs GUID
**Issue**: Format mismatch between C# and PostgreSQL  
**Solution**: Use proper type mapping
```csharp
// In DbContext
modelBuilder.Entity<User>()
    .Property(e => e.Id)
    .HasColumnType("uuid");
```

### Timezone Confusion
**Issue**: Dates showing wrong in UI  
**Solution**: Store UTC, convert for display
```sql
-- Store UTC
event_time TIMESTAMP WITH TIME ZONE

-- Query with timezone conversion
SELECT event_time AT TIME ZONE 'America/New_York' as local_time
FROM "Events";
```

### Reserved Words
**Issue**: Migration fails with syntax errors  
**Solution**: Always quote identifiers
```sql
-- Bad: user, order, group are reserved
CREATE TABLE user (...);

-- Good: Quote everything
CREATE TABLE "User" (...);
```

## Useful Queries

### Database Size
```sql
SELECT pg_database_size('witchcityrope_db') / 1024 / 1024 as size_mb;
```

### Table Sizes
```sql
SELECT schemaname, tablename, 
       pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Active Connections
```sql
SELECT pid, usename, application_name, client_addr, state
FROM pg_stat_activity
WHERE datname = 'witchcityrope_db';
```

### Lock Investigation
```sql
SELECT blocked_locks.pid AS blocked_pid,
       blocking_locks.pid AS blocking_pid,
       blocked_activity.query AS blocked_query,
       blocking_activity.query AS blocking_query
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;
```

---

*Remember: PostgreSQL is not SQL Server. Embrace its unique features like JSONB, arrays, and advanced indexing.*