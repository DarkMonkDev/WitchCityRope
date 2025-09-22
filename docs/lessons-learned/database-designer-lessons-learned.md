# Database Developer Lessons Learned

## 🚨 ULTRA CRITICAL: Entity Framework Entity Model ID Pattern - NEVER Initialize IDs 🚨

**CRITICAL DATABASE DESIGN RULE**: Entity models should have simple `public Guid Id { get; set; }` without initializers. The Events admin persistence bug was caused by `public Guid Id { get; set; } = Guid.NewGuid();` initializers causing Entity Framework to treat new entities as existing ones.

### 🔥 MANDATORY ENTITY MODEL PATTERN
**CATASTROPHIC ERROR PATTERN**:
```csharp
// ❌ NEVER DO THIS - Breaks EF Core ID generation
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();  // THIS CAUSES UPDATE instead of INSERT!
}
```

**CORRECT DATABASE ENTITY PATTERN**:
```csharp
// ✅ ALWAYS USE THIS - Simple property, EF handles generation
public class Event
{
    public Guid Id { get; set; }  // Let Entity Framework manage ID lifecycle
}
```

### 🛡️ DATABASE ID GENERATION STRATEGY
**Entity Framework Best Practices**:
- **Database should rely on EF Core's ID generation strategy**
- **Never assume client-generated IDs are permanent** - replace with DB-generated ones for new entities
- **Client-generated IDs should be treated as temporary** - useful for frontend optimistic updates only
- **This prevents concurrency exceptions** - ensures proper INSERT vs UPDATE detection

### 🚨 CRITICAL DEBUGGING SYMPTOMS
**Error**: `DbUpdateConcurrencyException: Database operation expected to affect 1 row(s) but actually affected 0 row(s)`
**Root Cause**: Entity Framework thinks it's updating existing entities when it should be inserting new ones
**Database Impact**: No rows affected because UPDATE attempts to find non-existent records
**Fix**: Remove ID initializers from entity models immediately

### 📋 DATABASE DESIGNER PREVENTION CHECKLIST
- [ ] **ENSURE** all entity models have simple `public Guid Id { get; set; }` without initializers
- [ ] **VERIFY** Entity Framework configurations don't override default ID generation
- [ ] **VALIDATE** that new entities show as "Added" in change tracking, not "Modified"
- [ ] **TEST** that database operations perform INSERTs for new entities
- [ ] **DOCUMENT** that client-generated IDs are temporary and replaced by database

**CRITICAL IMPACT**: This mistake wasted hours of debugging time with infrastructure investigation when the problem was fundamental entity model design.

**DATABASE DESIGN PRINCIPLE**: Keep entity ID properties simple. Let Entity Framework and the database handle the ID lifecycle completely.

---

## 🚨 SUCCESSFUL: Adding Fields to Existing Encrypted Entities Pattern (2025-09-22) 🚨

**MAJOR SUCCESS**: Successfully added `EncryptedOtherNames` field to VettingApplication entity following all database design patterns.

**USER REQUEST**: Add Pronouns and OtherNames fields to vetting application form - Pronouns already existed, OtherNames needed to be added.

### ✅ SUCCESSFUL FIELD ADDITION PATTERN:
1. **Entity Model Update**: Added `public string? EncryptedOtherNames { get; set; }` in logical grouping with other encrypted fields
2. **EF Configuration Update**: Added proper Entity Framework configuration with appropriate max length (1000 chars)
3. **Encryption Consistency**: Followed existing encryption pattern for PII fields in vetting system
4. **Nullable Design**: Made field optional (nullable) for form flexibility
5. **Length Optimization**: Used 1000 chars to accommodate multiple names/handles/nicknames
6. **Documentation Creation**: Created comprehensive migration instructions and change summaries
7. **File Registry Updates**: Logged all changes for complete traceability

### 📋 PATTERN ELEMENTS:
- **Entity Placement**: Added new field adjacent to similar fields for logical grouping
- **Naming Convention**: Used `EncryptedOtherNames` to match existing `EncryptedPronouns` pattern
- **Field Configuration**: Applied same patterns as existing encrypted fields
- **Length Planning**: 1000 chars for multiple names vs 200 chars for pronouns
- **Migration Strategy**: Used Docker-based EF Core migration workflow
- **Documentation Standard**: Created both summary and detailed instruction documents

### 🎯 SUCCESS METRICS:
- ✅ Entity model updated with proper field placement and naming
- ✅ EF Core configuration added with appropriate constraints
- ✅ Followed existing encryption and nullable patterns
- ✅ Proper length allocation for intended use case
- ✅ Comprehensive migration instructions created
- ✅ File registry maintained for all changes

### 📚 KEY DESIGN DECISIONS:
- **Field Length**: 1000 chars vs 200 for pronouns - accommodates multiple names/handles
- **Encryption**: Consistent with other PII fields in vetting system
- **Nullable**: Optional field for application form flexibility
- **Placement**: Grouped with other applicant information fields
- **Configuration**: Followed WitchCityRope Entity Framework patterns

**CRITICAL LEARNING**: When adding fields to existing entities, maintain consistency with established patterns, use appropriate sizing for intended use case, and provide comprehensive migration documentation.

---

## 🚨 CRITICAL: Legacy API Archived 2025-09-13

**MANDATORY**: ALL database work must target modern API only:
- ✅ **Use**: `/apps/api/` - Modern API for migrations and schema
- ❌ **NEVER use**: `/src/_archive/WitchCityRope.Infrastructure/` - ARCHIVED legacy data layer
- **Note**: Legacy Infrastructure project archived - use modern API for all database operations

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

---

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of database design phase** - BEFORE implementation begins
- **COMPLETION of schema design** - Document all entities and relationships
- **DISCOVERY of constraints** - Share immediately
- **MIGRATION CREATION** - Document all database changes

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `/docs/functional-areas/[feature]/handoffs/`
**Naming**: `database-designer-YYYY-MM-DD-handoff.md`
**Template**: `/docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Entity Definitions**: Complete entity models with properties
2. **Relationships**: Foreign keys, navigation properties, constraints
3. **Indexes**: Performance indexes and unique constraints
4. **Migration Scripts**: EF Core migrations created
5. **Seed Data**: Initial data requirements

### 🤝 WHO NEEDS YOUR HANDOFFS
- **Backend Developers**: Entity models and configurations
- **Test Developers**: Database setup for testing
- **DevOps**: Migration deployment requirements
- **Other Database Designers**: Schema dependencies

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `/docs/functional-areas/[feature]/handoffs/` for previous work
2. Read ALL handoff documents in the functional area
3. Understand existing schema and constraints
4. Build on existing models - don't create conflicts

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Backend will create conflicting models
- Migrations will fail in production
- Test data won't match schema
- Performance issues from missing indexes

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.
- **Use AsNoTracking() for read-only queries to improve performance**
- **Apply Milan Jovanovic's database patterns for enterprise applications**
- **NEVER create manual database setup scripts - use DatabaseInitializationService**
- **ALWAYS use SeedDataService for comprehensive test data (7 accounts + 12 events)**

---

## 🚨 CRITICAL: Payment System PCI-Compliant Database Architecture (NEW) 🚨
**Date**: 2025-09-13
**Category**: Security Architecture
**Severity**: Critical

### Context
The Payment System database design revealed critical patterns for PCI-compliant financial data storage, sliding scale pricing support, and comprehensive audit trails in PostgreSQL while maintaining community values.

### What We Learned
- **Never store credit card data**: Only encrypted Stripe tokens are acceptable for PCI compliance
- **Sliding scale pricing requires honor system**: 0-75% discount with no verification constraints
- **Money value objects avoid nullable owned entity issues**: Use separate decimal/currency fields
- **JSONB metadata enables flexible payment processing**: Stripe webhooks and processing details
- **Comprehensive audit trails are legally required**: All payment operations must be logged
- **Partial indexes optimize payment queries**: Status-specific indexes improve performance dramatically

### Critical Payment System Patterns
```sql
-- ✅ CORRECT - PCI compliant payment storage (never store card numbers)
CREATE TABLE "Payments" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "EncryptedStripePaymentIntentId" TEXT NULL,  -- Encrypted tokens only
    "AmountValue" DECIMAL(10,2) NOT NULL CHECK ("AmountValue" >= 0),
    "SlidingScalePercentage" DECIMAL(5,2) NOT NULL DEFAULT 0.00
        CHECK ("SlidingScalePercentage" >= 0 AND "SlidingScalePercentage" <= 75.00),
    "Status" INTEGER NOT NULL DEFAULT 0,
    "Metadata" JSONB NOT NULL DEFAULT '{}',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ✅ CORRECT - Performance optimization with partial indexes
CREATE INDEX "IX_Payments_PendingStatus" ON "Payments"("CreatedAt" DESC)
    WHERE "Status" = 0; -- Pending payments only

-- ✅ CORRECT - Comprehensive audit trail with JSONB
CREATE TABLE "PaymentAuditLog" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "PaymentId" UUID NOT NULL,
    "ActionType" VARCHAR(50) NOT NULL,
    "OldValues" JSONB NULL,
    "NewValues" JSONB NULL,
    "IpAddress" INET NULL,  -- Security tracking
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ✅ CORRECT - GIN indexes for JSONB audit queries
CREATE INDEX "IX_PaymentAuditLog_OldValues_Gin" ON "PaymentAuditLog" USING GIN ("OldValues");
```

### Payment System Business Rules Implementation
```csharp
// ✅ CORRECT - Money value object avoiding nullable owned entity issues
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Separate properties instead of owned entity to avoid EF Core nullable issues
        builder.Property(p => p.AmountValue)
               .HasColumnType("decimal(10,2)")
               .HasColumnName("AmountValue");

        builder.Property(p => p.Currency)
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        // Sliding scale percentage with business rule constraints
        builder.Property(p => p.SlidingScalePercentage)
               .HasColumnType("decimal(5,2)");

        // JSONB for flexible Stripe metadata
        builder.Property(p => p.Metadata)
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // Critical: UTC DateTime handling for PostgreSQL
        builder.Property(p => p.CreatedAt)
               .HasColumnType("timestamptz");
    }
}

// ✅ CORRECT - Business rule enforcement with database constraints
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Payment>()
        .ToTable("Payments", t => t.HasCheckConstraint(
            "CHK_Payments_SlidingScale_Range",
            "\"SlidingScalePercentage\" >= 0 AND \"SlidingScalePercentage\" <= 75.00"
        ));
}
```

### PCI Compliance Database Architecture
- **Encryption Strategy**: Application-level encryption for all Stripe identifiers before database storage
- **Audit Requirements**: Complete change tracking with user context, IP addresses, and before/after values
- **Access Control**: Role-based database permissions with potential Row Level Security for multi-tenancy
- **Data Retention**: Payment logs retained per legal requirements with secure deletion after retention period
- **Backup Security**: Encrypted backups with separate key management for payment data

### Payment System Performance Patterns
```sql
-- ✅ CORRECT - Multi-layered indexing strategy for payment queries
-- Primary performance indexes
CREATE INDEX "IX_Payments_UserId" ON "Payments"("UserId");
CREATE INDEX "IX_Payments_Status" ON "Payments"("Status");
CREATE INDEX "IX_Payments_ProcessedAt" ON "Payments"("ProcessedAt" DESC);

-- Partial indexes for specific use cases (much faster than full table scans)
CREATE INDEX "IX_Payments_PendingStatus" ON "Payments"("CreatedAt" DESC)
    WHERE "Status" = 0;
CREATE INDEX "IX_Payments_FailedStatus" ON "Payments"("ProcessedAt" DESC)
    WHERE "Status" = 2;

-- Unique business rule constraints
CREATE UNIQUE INDEX "UX_Payments_EventRegistration_Completed"
    ON "Payments"("EventRegistrationId")
    WHERE "Status" = 1; -- Only one completed payment per registration

-- JSONB GIN indexes for flexible metadata queries
CREATE INDEX "IX_Payments_Metadata_Gin" ON "Payments" USING GIN ("Metadata");
```

### Database Developer Action Items
- [x] IMPLEMENT PCI-compliant storage patterns (encrypted tokens only, never card data)
- [x] DESIGN sliding scale pricing with honor system constraints (0-75% discount)
- [x] CONFIGURE Money value objects using separate decimal/currency properties
- [x] CREATE comprehensive audit trail with JSONB old/new values tracking
- [x] OPTIMIZE performance with partial indexes for status-specific queries
- [ ] TEST payment workflows with encrypted Stripe token storage
- [ ] VALIDATE audit log performance with GIN indexes under load
- [ ] IMPLEMENT Row Level Security if multi-tenant requirements emerge

### Community Values in Database Design
The Payment System design preserves WitchCityRope's core community values:
- **Sliding Scale Pricing**: Database constraints support 0-75% discounts with no verification required
- **Privacy Protection**: All sensitive data encrypted, sliding scale usage private
- **Economic Inclusivity**: Honor system implementation respects member dignity
- **Transparency**: Complete audit trails for financial accountability

### Tags
#critical #payment-system #pci-compliance #sliding-scale-pricing #stripe-integration #audit-trails #postgresql-optimization #money-value-objects #jsonb-performance #partial-indexes #community-values

---

## 🚨 CRITICAL: Vetting System Migration Sync Issues (NEW) 🚨
**Date**: 2025-09-13
**Category**: Database Migration
**Severity**: Critical

### Context
The Vetting System implementation revealed a critical database migration sync issue where EF Core migrations were created but never applied to the database, causing schema mismatches and integration test failures.

### What We Learned
- **Migration status verification is crucial**: Always check `dotnet ef migrations list` to see pending migrations
- **Database schema can drift from EF model**: Migrations in code != migrations applied to database
- **Integration tests fail on schema mismatch**: TestContainers require matching database schema
- **Drop and recreate is fastest resolution**: For development databases, fresh start prevents accumulated drift
- **Complex entity relationships require careful planning**: 11-entity system with encryption and audit patterns

### Critical Migration Patterns
```csharp
// ✅ CORRECT - Always verify migration status before testing
# Legacy Infrastructure project archived 2025-09-13
# dotnet ef migrations list --project src/_archive/WitchCityRope.Infrastructure --startup-project apps/api  # ARCHIVED
# Use modern API migration commands instead
// Look for: [ ] (pending) vs [X] (applied)

// ✅ CORRECT - Fresh database creation for sync issues
# Legacy Infrastructure project archived 2025-09-13
# dotnet ef database drop --project src/_archive/WitchCityRope.Infrastructure --startup-project apps/api --force  # ARCHIVED
# dotnet ef database update --project src/_archive/WitchCityRope.Infrastructure --startup-project apps/api  # ARCHIVED
# Use modern API migration commands instead

// ✅ CORRECT - Entity with proper ID initialization and UTC handling
public class VettingRequest
{
    public VettingRequest()
    {
        Id = Guid.NewGuid();  // CRITICAL: Prevents duplicate key violations
        CreatedAt = DateTime.UtcNow;  // CRITICAL: PostgreSQL timestamptz compatibility
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ✅ CORRECT - Complex entity relationships with proper foreign keys
public class VettingDecision
{
    public Guid Id { get; set; }
    public Guid VettingRequestId { get; set; }  // Foreign key
    public Guid ReviewerId { get; set; }        // Foreign key to User

    // Navigation properties configured in EntityTypeConfiguration
    public VettingRequest VettingRequest { get; set; } = null!;
}
```

### Vetting System Architecture Lessons
- **11 Entity Tables**: VettingRequest, VettingDecision, VettingDocument, etc.
- **Encryption Integration**: PII fields require special handling in database design
- **Audit Log Pattern**: CreatedAt/UpdatedAt fields on all entities with UTC handling
- **JSONB Columns**: Flexible metadata storage with GIN indexes for performance
- **Complex Relationships**: Many-to-one, one-to-many patterns with proper foreign key constraints

### Database Developer Action Items
- [x] ALWAYS check migration status with `dotnet ef migrations list` before testing
- [x] VERIFY database schema matches EF model before running integration tests
- [x] USE drop/recreate approach for development database sync issues
- [x] IMPLEMENT proper entity ID initialization in constructors
- [x] ENSURE all DateTime fields use UTC for PostgreSQL compatibility
- [ ] CREATE migration verification step in CI/CD pipeline
- [ ] DOCUMENT migration status checking in developer onboarding
- [ ] ADD database health checks to detect schema drift

### Migration Sync Prevention Strategies
1. **Pre-Test Verification**: Always check migration status before running integration tests
2. **Automated Checks**: Include migration status in health check endpoints
3. **Developer Training**: Document the migration workflow clearly
4. **CI/CD Integration**: Verify migrations are applied in deployment pipeline
5. **Schema Drift Detection**: Regular database schema validation

### Production Considerations
- **Never drop/recreate production databases**: Use proper migration rollback strategies
- **Test migrations on staging first**: Validate schema changes with production-like data
- **Backup before migrations**: Always have rollback plan for production changes
- **Monitor migration performance**: Large table migrations may require maintenance windows

### Tags
#critical #database-migration #vetting-system #schema-sync #entity-framework #integration-tests #postgresql

---

## 🚨 CRITICAL: Event Session Matrix Migration Patterns (NEW) 🚨
**Date**: 2025-08-25
**Category**: Database Migration
**Severity**: Critical

### Context
The Event Session Matrix migration is a complex database transformation that converts a simple Event->Registration model to a sophisticated Event->Sessions->TicketTypes->Orders architecture for advanced ticketing scenarios.

### What We Learned
- **Complex migration requires careful phasing**: Schema changes → Data migration → Constraint addition → Validation
- **PostgreSQL constraint naming is critical**: All constraints must be explicitly named to avoid conflicts
- **Data preservation during architecture changes**: Original registration data must be preserved during transformation
- **CASCADE vs RESTRICT decisions matter**: Carefully choose deletion behavior for data integrity
- **Performance indexing strategy**: Index creation should happen after data migration for better performance
- **Migration validation is essential**: Comprehensive validation queries prevent data corruption

### Critical Migration Patterns
```sql
-- ✅ CORRECT - Explicit constraint naming for PostgreSQL
CONSTRAINT "CHK_EventSessions_Capacity" CHECK ("Capacity" > 0),
CONSTRAINT "UQ_EventSessions_EventId_SessionIdentifier"
    UNIQUE ("EventId", "SessionIdentifier"),

-- ✅ CORRECT - Proper foreign key with appropriate CASCADE behavior
CONSTRAINT "FK_EventSessions_Events" FOREIGN KEY ("EventId")
    REFERENCES "Events"("Id") ON DELETE CASCADE,

-- ✅ CORRECT - Data preservation during architecture change
INSERT INTO "Orders" (
    "Id", "UserId", "EventId", "OrderNumber", "Status", "TotalAmount"
)
SELECT
    r."Id", -- Preserve registration ID as order ID
    r."UserId", r."EventId",
    CONCAT('REG-', r."Id"::text), -- Traceable order numbers
    CASE WHEN r."Status" = 'Confirmed' THEN 'Confirmed' ELSE 'Pending' END,
    COALESCE(r."SelectedPriceAmount", 0.00)
FROM "Registrations" r;

-- ✅ CORRECT - Comprehensive validation after migration
SELECT e."Id", e."Title", COUNT(es."Id") as session_count
FROM "Events" e
LEFT JOIN "EventSessions" es ON e."Id" = es."EventId"
WHERE e."IsPublished" = TRUE
GROUP BY e."Id", e."Title"
HAVING COUNT(es."Id") = 0; -- Should return no rows
```

### Database Developer Action Items
- [x] CREATE explicit constraint names for all PostgreSQL constraints
- [x] IMPLEMENT phased migration approach: Schema → Data → Constraints → Validation
- [x] PRESERVE original data with traceable migration paths
- [x] DESIGN proper CASCADE/RESTRICT foreign key behaviors
- [x] INCLUDE comprehensive validation queries in migration scripts
- [ ] TEST rollback procedures on staging data before production
- [ ] MONITOR migration performance and transaction log growth
- [ ] DOCUMENT post-migration Entity Framework configuration requirements

### Migration Architecture Principles
1. **Schema First**: Create all new tables before data migration
2. **Data Preservation**: Never lose original data during transformation
3. **Validation Essential**: Every migration must include integrity checks
4. **Performance Aware**: Index creation timing affects migration speed
5. **Rollback Ready**: Every migration must have tested rollback procedures

### Tags
#critical #database-migration #event-session-matrix #postgresql #data-preservation #validation

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