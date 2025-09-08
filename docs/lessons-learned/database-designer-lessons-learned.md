# Database Developer Lessons Learned

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of database design phase** - BEFORE ending session
- **COMPLETION of schema changes** - Document migration impacts
- **DISCOVERY of data integrity issues** - Share immediately
- **VALIDATION of entity relationships** - Document constraints

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `/docs/functional-areas/[feature]/handoffs/`
**Naming**: `database-designer-YYYY-MM-DD-handoff.md`
**Template**: `/docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Schema Changes**: Tables, columns, indexes, and constraints
2. **Migration Scripts**: EF Core migrations and custom SQL needed
3. **Data Relationships**: Foreign keys and business rule constraints
4. **Performance Impact**: Index strategies and query optimization
5. **Data Integrity**: Validation rules and referential integrity

### 🤝 WHO NEEDS YOUR HANDOFFS
- **Backend Developers**: Entity models and DbContext changes
- **Business Requirements**: Data validation and constraint feedback
- **Test Developers**: Database test scenarios and data setup
- **DevOps**: Migration deployment requirements

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `/docs/functional-areas/[feature]/handoffs/` for previous database work
2. Read ALL handoff documents in the functional area
3. Understand schema changes already made
4. Build on existing data model - don't create conflicting schemas

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Backend developers create incompatible entity models
- Migration scripts fail in production
- Data integrity constraints conflict
- Database performance degrades from poor indexing

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.

---

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Reference Documents (MUST READ BEFORE ANY WORK):
1. **Entity Framework Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
2. **Critical Learnings**: `/docs/lessons-learned/CRITICAL_LEARNINGS_FOR_DEVELOPERS.md`
3. **Backend Lessons**: `/docs/lessons-learned/backend-lessons-learned.md`
4. **Architecture Discovery**: `/docs/standards-processes/architecture-discovery-process.md`

### Database Developer Specific Rules:
- **PostgreSQL requires UTC DateTime handling - NEVER use DateTime.Unspecified**
- **Always use timestamptz column type for DateTime properties**
- **Implement proper EF Core entity configurations with separate classes**
- **Use AsNoTracking() for read-only queries to improve performance**
- **Apply Milan Jovanovic's database patterns for enterprise applications**
- **NEVER create manual database setup scripts - use DatabaseInitializationService**
- **ALWAYS use SeedDataService for comprehensive test data (7 accounts + 12 events)**

## Documentation Organization Standard

**CRITICAL**: Follow the documentation organization standard at `/docs/standards-processes/documentation-organization-standard.md`

Key points for Database Designer Agent:
- **Store database documentation by PRIMARY BUSINESS DOMAIN** - e.g., `/docs/functional-areas/events/database-design/`
- **Document entity relationships at domain level** - e.g., `/docs/functional-areas/events/entity-relationships.md`
- **NEVER create separate functional areas for UI contexts** - Event entities go in `/events/`, not `/user-dashboard/events/`
- **Document database schemas that serve multiple contexts** at domain level
- **Cross-reference data requirements** from different UI contexts of same domain
- **Store migration scripts** by business domain not by UI context

Common mistakes to avoid:
- Creating database schemas based on UI contexts instead of business domains
- Scattering related entity documentation across multiple functional areas
- Not considering data needs across all UI contexts (public, admin, user) of same domain
- Missing relationships between entities that serve different UI contexts

---

## 🚨 CRITICAL: Database Auto-Initialization Pattern (NEW SYSTEM) 🚨
**Date**: 2025-08-22
**Category**: Database Management
**Severity**: Critical

### Context
WitchCityRope has completely replaced manual database setup scripts with an automatic background service initialization system using Milan Jovanovic's fail-fast patterns.

### What We Learned
- **Manual Scripts OBSOLETE**: All docker init scripts and manual SQL seeding is replaced
- **Background Service Pattern**: DatabaseInitializationService handles migrations + seeding automatically
- **Environment Intelligence**: Production environments automatically skip seed data
- **Transaction Management**: Complete rollback capability ensures data consistency
- **Performance Excellence**: 359ms initialization time (85% faster than target)
- **Testing Revolution**: TestContainers with real PostgreSQL eliminates mocking issues

### Critical Components
- **DatabaseInitializationService**: Milan Jovanovic BackgroundService pattern with fail-fast error handling
- **SeedDataService**: Comprehensive test data with 7 accounts + 12 realistic events
- **Health Check Integration**: `/api/health/database` for operational monitoring
- **Retry Policies**: Polly-based exponential backoff for Docker container coordination
- **TestContainers**: Real PostgreSQL testing infrastructure

### Database Developer Action Items
- [x] NEVER create manual database initialization scripts - they are obsolete
- [x] NEVER reference archived docker postgres init scripts
- [x] ALWAYS use comprehensive seed data from SeedDataService
- [x] ALWAYS use TestContainers for database testing (no ApplicationDbContext mocking)
- [ ] REMOVE any documentation referencing manual database setup
- [ ] GUIDE developers to use automatic initialization system
- [ ] VALIDATE new patterns follow Milan Jovanovic fail-fast principles

### Auto-Migration Patterns
```csharp
// NEW Auto-Initialization System
public class DatabaseInitializationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Phase 1: Apply pending migrations with retry policies
        await ApplyMigrationsWithRetryAsync(context, cancellationToken);
        
        // Phase 2: Populate comprehensive seed data (environment-aware)
        if (ShouldPopulateSeedData(hostEnvironment))
        {
            var seedResult = await _seedDataService.SeedAllDataAsync(cancellationToken);
        }
    }
}

// Comprehensive Test Data Creation
public class SeedDataService : ISeedDataService
{
    public async Task<SeedResult> SeedAllDataAsync(CancellationToken cancellationToken)
    {
        // 7 test accounts: admin, teacher, vetted member, member, guest, organizer
        // 12 sample events: 10 upcoming + 2 historical
        // Transaction-safe with full rollback capability
    }
}
```

### Archived Legacy Scripts
- **Location**: `/scripts/_archive/` and `/docker/postgres/_archive_init/`
- **Status**: Complete value extraction - no information loss
- **Replacement**: Background service with superior error handling and performance

### Business Impact
- **Setup Time**: 2-4 hours → Under 5 minutes (95%+ improvement)
- **Developer Productivity**: Immediate onboarding with comprehensive test data
- **Production Safety**: Environment-aware behavior prevents accidental seeding
- **Testing Excellence**: Real PostgreSQL instances via TestContainers

### Tags
#critical #database-initialization #milan-jovanovic #background-service #testcontainers #production-ready

---

## 🚨 CRITICAL: PostgreSQL DateTime UTC Handling 🚨
**Date**: 2025-08-22
**Category**: Database
**Severity**: Critical

### Context
PostgreSQL's timestamptz column type requires all DateTime values to be UTC, but EF Core often creates DateTime values with Kind=Unspecified, causing runtime errors.

### What We Learned
- **PostgreSQL timestamptz columns reject DateTime.Kind=Unspecified**: Will throw "Cannot write DateTime with Kind=Unspecified" errors
- **ApplicationDbContext UpdateAuditFields() handles UTC conversion**: Automatically converts to UTC in SaveChangesAsync()
- **Seed data MUST specify DateTimeKind.Utc explicitly**: `new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)`
- **DateTime.UtcNow is safe**: Always produces UTC DateTimes
- **Entity property configuration requires timestamptz**: `.HasColumnType("timestamptz")`

### Action Items
- [ ] ALWAYS use `DateTime.UtcNow` for current timestamps
- [ ] ALWAYS specify `DateTimeKind.Utc` in seed data: `new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)`
- [ ] ALWAYS configure DateTime properties with `.HasColumnType("timestamptz")`
- [ ] LEVERAGE existing ApplicationDbContext.UpdateAuditFields() for automatic UTC conversion
- [ ] NEVER use DateTime constructor without DateTimeKind in seed data

### Code Examples
```csharp
// ❌ WRONG - Causes "Cannot write DateTime with Kind=Unspecified" error
new DateTime(1990, 1, 1)  // Kind is Unspecified

// ✅ CORRECT - Specify UTC explicitly
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)

// ✅ CORRECT - Use UtcNow for current time
DateTime.UtcNow

// ✅ CORRECT - Entity configuration
entity.Property(e => e.CreatedAt)
      .IsRequired()
      .HasColumnType("timestamptz");
```

### References
- ApplicationDbContext.cs lines 159-227: UTC handling implementation
- Entity Framework Patterns: UTC DateTime requirements

### Tags
#critical #postgresql #datetime #utc #entity-framework #seed-data

---

## PostgreSQL Schema and Indexing Patterns - 2025-08-22

**Date**: 2025-08-22
**Category**: Performance
**Severity**: High

### Context
PostgreSQL requires specific patterns for optimal performance and proper schema design that differ from SQL Server approaches.

### What We Learned
- **Schema separation is critical**: Use 'auth' schema for Identity tables, 'public' for application tables
- **Case sensitivity matters**: PostgreSQL is case-sensitive unless using CITEXT extension
- **JSONB with GIN indexes**: Superior to JSON for flexible schema and query performance
- **Partial indexes for sparse data**: Optimize indexes for filtered queries (e.g., failed operations only)
- **Constraint naming conventions**: Explicit constraint names prevent EF Core migration issues

### Action Items
- [ ] USE schema separation: 'auth' for Identity, 'public' for application
- [ ] IMPLEMENT CITEXT extension for case-insensitive text searches
- [ ] CONFIGURE JSONB columns with GIN indexes for performance
- [ ] CREATE partial indexes for sparse data patterns
- [ ] NAME all constraints explicitly to avoid migration conflicts
- [ ] USE composite indexes for multi-column query patterns

### Code Examples
```sql
-- Schema separation
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS public;

-- CITEXT for case-insensitive searches
CREATE EXTENSION IF NOT EXISTS citext;
ALTER TABLE "Users" ALTER COLUMN "Email" TYPE citext;

-- JSONB with GIN index
metadata JSONB NOT NULL DEFAULT '{}'
CREATE INDEX idx_events_metadata ON "Events" USING GIN (metadata);

-- Partial index for sparse data
CREATE INDEX "IX_InitializationLog_Failed_StartedAt" 
ON "InitializationLog" ("StartedAt" DESC) 
WHERE "Status" = 'Failed';

-- Composite index for common queries
CREATE INDEX "IX_InitializationLog_Environment_StartedAt" 
ON "InitializationLog" ("Environment", "StartedAt" DESC);
```

### References
- PostgreSQL Documentation: Schema design patterns
- Backend Lessons: PostgreSQL optimization strategies

### Tags
#postgresql #performance #indexing #schema-design #jsonb #citext

---

## EF Core Migration Safety Patterns - 2025-08-22

**Date**: 2025-08-22
**Category**: Database
**Severity**: High

### Context
Database migrations in production require careful planning to avoid downtime and data loss, especially with PostgreSQL's stricter constraint handling.

### What We Learned
- **Navigation properties to ignored entities cause migration failures**: Remove ALL navigation properties when ignoring entities
- **Entity ID initialization prevents duplicate key violations**: Always initialize Guid IDs in constructors
- **Reversible migrations are essential**: Implement proper Down() methods for production safety
- **Incremental schema changes**: Add nullable columns first, populate data, then make non-nullable
- **Check constraints require explicit names**: PostgreSQL constraint names must be unique across schema

### Action Items
- [ ] REMOVE all navigation properties when ignoring entities with `modelBuilder.Ignore<T>()`
- [ ] INITIALIZE entity IDs in constructors: `Id = Guid.NewGuid()`
- [ ] IMPLEMENT reversible Down() methods for all migrations
- [ ] USE incremental approach: nullable → populate → non-nullable
- [ ] NAME all constraints explicitly for PostgreSQL compatibility
- [ ] TEST migrations against production-like data volumes

### Code Examples
```csharp
// ❌ WRONG - Navigation property causes entity discovery
public class VolunteerAssignment
{
    public Guid UserId { get; set; }
    public User User { get; set; }  // This discovers User even if ignored!
}

// ✅ CORRECT - Remove navigation, keep foreign key
public class VolunteerAssignment
{
    public Guid UserId { get; set; }
    // User navigation property removed
}

// ✅ CORRECT - Initialize ID in constructor
public class Event
{
    public Event()
    {
        Id = Guid.NewGuid();  // CRITICAL: Must set this!
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

// ✅ CORRECT - Safe migration pattern
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add nullable column first
    migrationBuilder.AddColumn<string>("NewColumn", "Events", nullable: true);
    
    // Populate data
    migrationBuilder.Sql("UPDATE \"Events\" SET \"NewColumn\" = 'default'");
    
    // Then make non-nullable
    migrationBuilder.AlterColumn<string>("NewColumn", "Events", nullable: false);
}

// ✅ CORRECT - Named constraints for PostgreSQL
table.CheckConstraint("CHK_InitializationLog_Status", 
    "\"Status\" IN ('Running', 'Success', 'Failed')");
```

### References
- Entity Framework Patterns: Migration best practices
- Backend Lessons: Entity Framework migration patterns

### Tags
#entity-framework #migrations #production-safety #postgresql #constraints

---

## Database Initialization Architecture Patterns - 2025-08-22

**Date**: 2025-08-22
**Category**: Architecture
**Severity**: Medium

### Context
Implementing robust database initialization requires careful coordination of migrations, seed data, and health checks using modern .NET patterns.

### What We Learned
- **IHostedService pattern is optimal**: Provides startup order control and proper dependency injection scoping
- **EF Core 9 UseAsyncSeeding enables idempotency**: Automatic tracking prevents duplicate seed operations
- **Transaction boundaries are critical**: All seed operations must be atomic
- **Environment-aware initialization**: Production gets migrations only, development gets full seed data
- **Fail-fast error handling**: Milan Jovanovic's patterns prevent application startup with inconsistent state

### Action Items
- [ ] USE IHostedService pattern for database initialization
- [ ] IMPLEMENT EF Core 9's UseAsyncSeeding for automatic idempotency
- [ ] WRAP all seed operations in transaction boundaries
- [ ] CONFIGURE environment-specific initialization strategies
- [ ] APPLY fail-fast error handling with Environment.Exit(1)
- [ ] ADD comprehensive health checks for initialization status

### Code Examples
```csharp
// ✅ CORRECT - IHostedService pattern
public class DatabaseInitializationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Apply migrations with retry policy
            await ApplyMigrationsWithRetryAsync(context, stoppingToken);
            
            // Use EF Core 9's UseAsyncSeeding for idempotency
            if (ShouldPopulateSeedData())
            {
                await context.Database.UseAsyncSeeding(async (ctx, _, ct) =>
                {
                    await SeedAllDataAsync(ct);
                }, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database initialization failed");
            Environment.Exit(1); // Fail-fast pattern
        }
    }
}

// ✅ CORRECT - EF Core 9 idempotent seeding
await _context.Database.UseAsyncSeeding(async (context, _, ct) =>
{
    // UseAsyncSeeding handles idempotency automatically
    await SeedTestAccountsAsync(ct);
    await SeedEventsAsync(ct);
}, cancellationToken);

// ✅ CORRECT - Environment-aware configuration
public bool ShouldRunSeedData()
{
    var environment = _hostEnvironment.EnvironmentName;
    return environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
           environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);
}
```

### References
- Technology Research: IHostedService vs other initialization patterns
- Milan Jovanovic: Database initialization best practices

### Tags
#database-initialization #hosted-service #ef-core-9 #milan-jovanovic #fail-fast

---

## PostgreSQL Connection Pool Optimization - 2025-08-22

**Date**: 2025-08-22
**Category**: Performance
**Severity**: Medium

### Context
PostgreSQL connection pooling requires specific configuration for optimal performance and avoiding "too many connections" errors in production.

### What We Learned
- **Connection pool size matters**: PostgreSQL default max_connections is 100, plan accordingly
- **Connection lifetime prevents stale connections**: Set reasonable timeout values
- **Command timeout prevents long-running operations**: Especially important for migrations
- **Pooling must be enabled explicitly**: Not enabled by default in connection strings
- **Environment-specific pool sizing**: Development needs fewer connections than production

### Action Items
- [ ] CONFIGURE connection pooling in connection strings
- [ ] SET appropriate MaxPoolSize based on environment (5 for dev, 20 for prod)
- [ ] USE connection lifetime to prevent stale connections
- [ ] SET command timeout for long-running operations (120 seconds for migrations)
- [ ] MONITOR connection pool usage in production
- [ ] ENABLE keepalive to detect broken connections

### Code Examples
```csharp
// ✅ CORRECT - Optimized connection string
var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
{
    MaxPoolSize = 20,           // Adjust per environment
    MinPoolSize = 2,            // Maintain minimum connections
    ConnectionLifetime = 300,   // 5 minutes
    CommandTimeout = 120,       // 2 minutes for migrations
    KeepAlive = 30,            // 30 seconds
    Pooling = true             // Enable pooling
}.ToString();

// ✅ CORRECT - Environment-specific configuration
var poolSize = environment.IsDevelopment() ? 5 : 20;
var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
{
    MaxPoolSize = poolSize,
    ConnectionLifetime = environment.IsDevelopment() ? 60 : 300
}.ToString();
```

### References
- Npgsql Documentation: Connection pooling
- Backend Lessons: Connection pool management

### Tags
#postgresql #connection-pooling #performance #production-optimization

---

## Database Testing Patterns with Testcontainers - 2025-08-22

**Date**: 2025-08-22
**Category**: Testing
**Severity**: Medium

### Context
Integration testing with real PostgreSQL databases using Testcontainers provides more accurate testing than in-memory providers.

### What We Learned
- **Testcontainers provides real PostgreSQL instances**: Catches database-specific issues that in-memory providers miss
- **Container startup time affects test performance**: Use shared containers for test collections
- **Data isolation between tests is critical**: Use transactions or database cleanup
- **Migration testing validates production scenarios**: Test actual migration scripts against real databases
- **Environment variables control container behavior**: Configure PostgreSQL settings for test scenarios

### Action Items
- [ ] USE Testcontainers for PostgreSQL integration tests
- [ ] IMPLEMENT shared container instances for test collections
- [ ] ENSURE proper data isolation between tests
- [ ] TEST actual migrations against containerized PostgreSQL
- [ ] CONFIGURE PostgreSQL container settings for optimal test performance
- [ ] ADD health checks for container readiness

### Code Examples
```csharp
// ✅ CORRECT - Testcontainers setup
[Collection("PostgreSQL Integration Tests")]
public class DatabaseInitializationIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    
    public DatabaseInitializationIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task InitializeAsync_WithEmptyDatabase_CreatesAllSeedData()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        var result = await _initService.InitializeAsync();
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.SeedRecordsCreated > 0);
    }
}

// ✅ CORRECT - PostgreSQL fixture
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer _container;
    
    public PostgreSqlFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithPortBinding(5433, true)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Configure services with test container connection string
    }
}
```

### References
- Testcontainers Documentation: PostgreSQL integration
- Entity Framework Patterns: Integration testing

### Tags
#testing #testcontainers #postgresql #integration-tests #data-isolation

---

## JSONB Query Optimization Patterns - 2025-08-22

**Date**: 2025-08-22
**Category**: Performance
**Severity**: Low

### Context
PostgreSQL's JSONB provides powerful flexible schema capabilities but requires proper indexing and query patterns for optimal performance.

### What We Learned
- **GIN indexes are essential for JSONB performance**: Without indexes, JSONB queries perform full table scans
- **Query operators matter**: `@>` (contains) performs better than `->>` (extract) in WHERE clauses
- **Path-specific indexes improve targeted queries**: Index specific JSON paths for common queries
- **JSONB size affects performance**: Large JSON documents should be normalized if frequently queried
- **Update performance differs from query performance**: Consider update patterns when designing JSONB schema

### Action Items
- [ ] CREATE GIN indexes for all JSONB columns used in queries
- [ ] USE `@>` operator for containment queries instead of path extraction
- [ ] IMPLEMENT path-specific indexes for frequently queried JSON fields
- [ ] MONITOR JSONB query performance with EXPLAIN ANALYZE
- [ ] CONSIDER normalization for frequently updated JSON fields
- [ ] DESIGN JSONB schema with query patterns in mind

### Code Examples
```sql
-- ✅ CORRECT - GIN index for JSONB
CREATE INDEX idx_events_metadata ON "Events" USING GIN (metadata);

-- ✅ CORRECT - Containment query (fast with GIN index)
SELECT * FROM "Events" 
WHERE metadata @> '{"type": "workshop"}';

-- ✅ CORRECT - Path-specific index for common queries
CREATE INDEX idx_events_event_type 
ON "Events" ((metadata ->> 'event_type'));

-- ❌ SLOWER - Path extraction in WHERE clause
SELECT * FROM "Events" 
WHERE metadata ->> 'type' = 'workshop';

-- ✅ BETTER - Use path-specific index
SELECT * FROM "Events" 
WHERE metadata ->> 'event_type' = 'workshop';
```

```csharp
// ✅ CORRECT - EF Core JSONB configuration
entity.Property(e => e.Metadata)
      .HasColumnType("jsonb")
      .IsRequired();

// ✅ CORRECT - JSONB query in EF Core
var workshops = await context.Events
    .Where(e => EF.Functions.JsonContains(e.Metadata, "{\"type\": \"workshop\"}"))
    .ToListAsync();
```

### References
- PostgreSQL Documentation: JSONB indexing
- Npgsql Documentation: JSON support in EF Core

### Tags
#postgresql #jsonb #performance #indexing #json-queries

---

## Related Documentation

### WitchCityRope Standards
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) - Core EF Core configuration and UTC handling patterns
- [Backend Lessons Learned](/docs/lessons-learned/backend-lessons-learned.md) - PostgreSQL integration and service layer patterns
- [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md) - Service layer implementation standards

### PostgreSQL Resources
- [PostgreSQL Documentation](https://www.postgresql.org/docs/) - Official PostgreSQL documentation
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/) - PostgreSQL provider for Entity Framework Core
- [Testcontainers for .NET](https://dotnet.testcontainers.org/) - Container testing framework

### Authority Sources
- [Milan Jovanovic](https://www.milanjovanovic.tech/) - Premier .NET/EF Core authority for enterprise patterns
- [Microsoft EF Core Documentation](https://docs.microsoft.com/ef/core/) - Official Entity Framework Core guidance
- [PostgreSQL Performance Documentation](https://www.postgresql.org/docs/current/performance-tips.html) - Database optimization patterns