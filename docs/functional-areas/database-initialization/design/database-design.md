# Database Design: Auto-Initialization Feature
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer -->
<!-- Status: Design Complete -->

## Executive Summary

This document provides a comprehensive database design for the auto-initialization feature that transforms manual 4-step database setup into seamless zero-configuration developer experience. The design leverages IHostedService pattern with EF Core 9's UseAsyncSeeding methods, ensuring robust initialization within a 30-second timeout while maintaining production safety through environment-aware operations.

## Architecture Alignment

### Microservices Architecture Integration
**CRITICAL**: This design operates within the established Web+API microservices architecture:
- **API Service** (.NET Minimal API): Handles database initialization at startup
- **Database** (PostgreSQL): Target for migrations and seed data operations
- **Pattern**: API → EF Core → PostgreSQL (no direct database access from Web service)
- **Integration Point**: ApplicationDbContext with existing UTC handling and audit field patterns

### Existing Infrastructure Leverage
- **ApplicationDbContext**: Builds upon existing UTC DateTime handling (lines 159-227)
- **Schema Configuration**: Respects 'public' and 'auth' schema separation (lines 29-36)
- **Audit Fields**: Uses established CreatedAt/UpdatedAt patterns (lines 45-51, 141-147)
- **Migration Framework**: Extends existing EF Core migration pipeline

## Entity Relationship Diagram (ERD)

### Core Initialization Entities

```
┌─────────────────────────────────────┐
│            EF Core System           │
├─────────────────────────────────────┤
│ __EFMigrationsHistory               │
│ - MigrationId (PK)                  │
│ - ProductVersion                    │
├─────────────────────────────────────┤
│ __EFSeedHistory (EF Core 9)         │
│ - Id (PK)                          │
│ - SeedDataName                     │
│ - ExecutedOn                       │
└─────────────────────────────────────┘
              │
              │ manages
              ▼
┌─────────────────────────────────────┐
│        InitializationLog            │
├─────────────────────────────────────┤
│ Id (PK, Guid)                      │
│ StartedAt (timestamptz)            │
│ CompletedAt (timestamptz, nullable) │
│ Status (enum: Running|Success|Failed)│
│ Environment (text)                  │
│ MigrationsApplied (int)            │
│ SeedRecordsCreated (int)           │
│ Duration (interval, nullable)       │
│ ErrorMessage (text, nullable)      │
│ StackTrace (text, nullable)        │
│ InitializationOptions (jsonb)       │
│ CreatedAt (timestamptz)            │
│ UpdatedAt (timestamptz)            │
└─────────────────────────────────────┘
              │
              │ tracks
              ▼
┌─────────────────────────────────────┐
│       SeedDataOperation             │
├─────────────────────────────────────┤
│ Id (PK, Guid)                      │
│ InitializationLogId (FK)           │
│ OperationType (enum: Users|Events|  │
│               VettingStatuses)      │
│ RecordsCreated (int)               │
│ StartedAt (timestamptz)            │
│ CompletedAt (timestamptz)          │
│ Duration (interval)                │
│ Success (boolean)                  │
│ ErrorMessage (text, nullable)      │
│ CreatedAt (timestamptz)            │
│ UpdatedAt (timestamptz)            │
└─────────────────────────────────────┘
```

### Relationship to Existing Entities

```
┌─────────────────────────────────────┐
│         ApplicationUser             │
│         (auth.Users)                │
├─────────────────────────────────────┤
│ Id (PK, Guid)                      │
│ SceneName (text, unique)           │
│ Email (text, unique)               │
│ Role (text)                        │
│ IsVetted (boolean)                 │
│ CreatedAt (timestamptz)            │
│ UpdatedAt (timestamptz)            │
│ ... (existing fields)              │
└─────────────────────────────────────┘
              │
              │ seeded by
              ▼
┌─────────────────────────────────────┐
│      SeedDataOperation              │
│    (OperationType = Users)          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│             Event                   │
│        (public.Events)              │
├─────────────────────────────────────┤
│ Id (PK, Guid)                      │
│ Title (text)                       │
│ Description (text)                 │
│ StartDate (timestamptz)            │
│ EndDate (timestamptz)              │
│ Capacity (int)                     │
│ EventType (text)                   │
│ Location (text)                    │
│ IsPublished (boolean)              │
│ PricingTiers (text)                │
│ CreatedAt (timestamptz)            │
│ UpdatedAt (timestamptz)            │
└─────────────────────────────────────┘
              │
              │ seeded by
              ▼
┌─────────────────────────────────────┐
│      SeedDataOperation              │
│    (OperationType = Events)         │
└─────────────────────────────────────┘
```

## Schema Design (SQL DDL)

### Initialization Tracking Schema

```sql
-- ============================================================================
-- Database Initialization Tracking Schema
-- Schema: public (initialization tracking is application-level, not auth-specific)
-- ============================================================================

-- Comprehensive initialization log for diagnostics and monitoring
CREATE TABLE "InitializationLog" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "StartedAt" timestamptz NOT NULL DEFAULT NOW(),
    "CompletedAt" timestamptz NULL,
    "Status" text NOT NULL CHECK ("Status" IN ('Running', 'Success', 'Failed')),
    "Environment" text NOT NULL,
    "MigrationsApplied" integer NOT NULL DEFAULT 0,
    "SeedRecordsCreated" integer NOT NULL DEFAULT 0,
    "Duration" interval NULL,
    "ErrorMessage" text NULL,
    "StackTrace" text NULL,
    "InitializationOptions" jsonb NOT NULL DEFAULT '{}',
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW()
);

-- Detailed seed operation tracking for granular monitoring
CREATE TABLE "SeedDataOperation" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "InitializationLogId" uuid NOT NULL REFERENCES "InitializationLog"("Id") ON DELETE CASCADE,
    "OperationType" text NOT NULL CHECK ("OperationType" IN ('Users', 'Events', 'VettingStatuses')),
    "RecordsCreated" integer NOT NULL DEFAULT 0,
    "StartedAt" timestamptz NOT NULL DEFAULT NOW(),
    "CompletedAt" timestamptz NOT NULL,
    "Duration" interval NOT NULL,
    "Success" boolean NOT NULL DEFAULT FALSE,
    "ErrorMessage" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- Performance Indexes for Initialization Tracking
-- ============================================================================

-- Query initialization status by environment and date
CREATE INDEX "IX_InitializationLog_Environment_StartedAt" 
ON "InitializationLog" ("Environment", "StartedAt" DESC);

-- Query recent initialization attempts for monitoring
CREATE INDEX "IX_InitializationLog_Status_StartedAt" 
ON "InitializationLog" ("Status", "StartedAt" DESC);

-- Query seed operations by type for performance analysis
CREATE INDEX "IX_SeedDataOperation_OperationType_Duration" 
ON "SeedDataOperation" ("OperationType", "Duration");

-- Foreign key performance for operation lookup
CREATE INDEX "IX_SeedDataOperation_InitializationLogId" 
ON "SeedDataOperation" ("InitializationLogId");

-- Partial index for failed operations (sparse index optimization)
CREATE INDEX "IX_InitializationLog_Failed_StartedAt" 
ON "InitializationLog" ("StartedAt" DESC) 
WHERE "Status" = 'Failed';

-- ============================================================================
-- Database Constraints for Data Integrity
-- ============================================================================

-- Ensure completed operations have duration
ALTER TABLE "InitializationLog" 
ADD CONSTRAINT "CHK_InitializationLog_CompletedDuration" 
CHECK (
    ("CompletedAt" IS NULL AND "Duration" IS NULL) OR 
    ("CompletedAt" IS NOT NULL AND "Duration" IS NOT NULL)
);

-- Ensure failed operations have error message
ALTER TABLE "InitializationLog" 
ADD CONSTRAINT "CHK_InitializationLog_FailedError" 
CHECK (
    ("Status" != 'Failed') OR 
    ("Status" = 'Failed' AND "ErrorMessage" IS NOT NULL)
);

-- Ensure completed seed operations have positive duration
ALTER TABLE "SeedDataOperation" 
ADD CONSTRAINT "CHK_SeedDataOperation_PositiveDuration" 
CHECK ("Duration" > INTERVAL '0 seconds');

-- Ensure logical record counts
ALTER TABLE "SeedDataOperation" 
ADD CONSTRAINT "CHK_SeedDataOperation_LogicalCounts" 
CHECK (
    ("Success" = TRUE AND "RecordsCreated" >= 0) OR 
    ("Success" = FALSE AND "RecordsCreated" = 0)
);

-- ============================================================================
-- Audit Triggers for Automatic UpdatedAt Handling
-- ============================================================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger for InitializationLog
CREATE TRIGGER update_initializationlog_updated_at 
    BEFORE UPDATE ON "InitializationLog" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger for SeedDataOperation
CREATE TRIGGER update_seeddataoperation_updated_at 
    BEFORE UPDATE ON "SeedDataOperation" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- Data Cleanup and Retention Policy
-- ============================================================================

-- Function to clean up old initialization logs (keep last 100 per environment)
CREATE OR REPLACE FUNCTION cleanup_initialization_logs()
RETURNS void AS $$
BEGIN
    -- Keep last 100 successful runs per environment
    DELETE FROM "InitializationLog" 
    WHERE "Id" IN (
        SELECT "Id" 
        FROM (
            SELECT "Id", 
                   ROW_NUMBER() OVER (
                       PARTITION BY "Environment", "Status" 
                       ORDER BY "StartedAt" DESC
                   ) as row_num
            FROM "InitializationLog" 
            WHERE "Status" = 'Success'
        ) ranked 
        WHERE row_num > 100
    );
    
    -- Keep all failed runs for diagnostics (manual cleanup)
    -- Keep last 30 days of running status (likely stuck processes)
    DELETE FROM "InitializationLog" 
    WHERE "Status" = 'Running' 
    AND "StartedAt" < NOW() - INTERVAL '30 days';
END;
$$ LANGUAGE plpgsql;

-- Note: Cleanup function should be called periodically via application or cron job
```

### Seed Data Entity Models

```sql
-- ============================================================================
-- Seed Data Models (leveraging existing schema)
-- ============================================================================

-- ApplicationUser seed data (uses existing auth.Users table)
-- 5 test accounts per DATABASE-SEED-DATA-2.md
INSERT INTO auth."Users" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "SceneName", "EncryptedLegalName", "DateOfBirth", "Role", "PronouncedName",
    "Pronouns", "IsActive", "IsVetted", "EmailVerificationToken",
    "CreatedAt", "UpdatedAt"
) VALUES
-- Admin Account
(gen_random_uuid(), 'admin@witchcityrope.com', 'ADMIN@WITCHCITYROPE.COM',
 'admin@witchcityrope.com', 'ADMIN@WITCHCITYROPE.COM', TRUE,
 'AQAAAAIAAYagAAAAEG...', -- BCrypt hash for 'Test123!'
 gen_random_uuid()::text, gen_random_uuid()::text,
 'RopeMaster', 'encrypted_legal_name_admin', '1990-01-01 00:00:00+00'::timestamptz,
 'Administrator', 'Rope Master', 'they/them', TRUE, TRUE,
 gen_random_uuid()::text, NOW(), NOW()),

-- Staff Account (4 more accounts following same pattern)
-- ... (complete seed data per DATABASE-SEED-DATA-2.md specifications)

-- Event seed data (uses existing public.Events table)
-- 12 events: 10 upcoming, 2 past per DATABASE-SEED-DATA-2.md
INSERT INTO public."Events" (
    "Id", "Title", "Description", "StartDate", "EndDate",
    "Capacity", "EventType", "Location", "IsPublished", "PricingTiers",
    "CreatedAt", "UpdatedAt"
) VALUES
-- Introduction to Rope Safety (7 days from now)
(gen_random_uuid(), 'Introduction to Rope Safety',
 'Learn the fundamentals of safe rope bondage practices',
 (NOW() + INTERVAL '7 days')::date + TIME '18:00:00',
 (NOW() + INTERVAL '7 days')::date + TIME '21:00:00',
 20, 'Workshop', 'Main Dungeon Space', TRUE, '$25-$45 (sliding scale)',
 NOW(), NOW()),

-- 11 more events following same pattern...
-- ... (complete event data per DATABASE-SEED-DATA-2.md specifications)
```

## Entity Framework Configuration

### Initialization Tracking Entity Configuration

```csharp
// Models/InitializationLog.cs
public class InitializationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public InitializationStatus Status { get; set; } = InitializationStatus.Running;
    public string Environment { get; set; } = string.Empty;
    public int MigrationsApplied { get; set; }
    public int SeedRecordsCreated { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public JsonDocument InitializationOptions { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual ICollection<SeedDataOperation> SeedDataOperations { get; set; } = new List<SeedDataOperation>();
}

public enum InitializationStatus
{
    Running,
    Success,
    Failed
}

// Models/SeedDataOperation.cs
public class SeedDataOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InitializationLogId { get; set; }
    public SeedOperationType OperationType { get; set; }
    public int RecordsCreated { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual InitializationLog InitializationLog { get; set; } = null!;
}

public enum SeedOperationType
{
    Users,
    Events,
    VettingStatuses
}
```

### EF Core Configuration Extensions

```csharp
// Data/Configurations/InitializationTrackingConfiguration.cs
public class InitializationTrackingConfiguration : IEntityTypeConfiguration<InitializationLog>
{
    public void Configure(EntityTypeBuilder<InitializationLog> builder)
    {
        // Table configuration
        builder.ToTable("InitializationLog", "public");
        builder.HasKey(e => e.Id);
        
        // Property configurations
        builder.Property(e => e.StartedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.CompletedAt)
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);
               
        builder.Property(e => e.Environment)
               .IsRequired()
               .HasMaxLength(50);
               
        builder.Property(e => e.ErrorMessage)
               .HasColumnType("text");
               
        builder.Property(e => e.StackTrace)
               .HasColumnType("text");
               
        builder.Property(e => e.InitializationOptions)
               .HasColumnType("jsonb")
               .IsRequired();
               
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
        
        // Indexes for performance
        builder.HasIndex(e => new { e.Environment, e.StartedAt })
               .HasDatabaseName("IX_InitializationLog_Environment_StartedAt");
               
        builder.HasIndex(e => new { e.Status, e.StartedAt })
               .HasDatabaseName("IX_InitializationLog_Status_StartedAt");
               
        // Partial index for failed operations
        builder.HasIndex(e => e.StartedAt)
               .HasDatabaseName("IX_InitializationLog_Failed_StartedAt")
               .HasFilter("\"Status\" = 'Failed'");
        
        // Relationships
        builder.HasMany(e => e.SeedDataOperations)
               .WithOne(e => e.InitializationLog)
               .HasForeignKey(e => e.InitializationLogId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SeedDataOperationConfiguration : IEntityTypeConfiguration<SeedDataOperation>
{
    public void Configure(EntityTypeBuilder<SeedDataOperation> builder)
    {
        // Table configuration
        builder.ToTable("SeedDataOperation", "public");
        builder.HasKey(e => e.Id);
        
        // Property configurations
        builder.Property(e => e.OperationType)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);
               
        builder.Property(e => e.StartedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.CompletedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.Duration)
               .IsRequired()
               .HasColumnType("interval");
               
        builder.Property(e => e.ErrorMessage)
               .HasColumnType("text");
               
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
               
        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");
        
        // Indexes for performance
        builder.HasIndex(e => new { e.OperationType, e.Duration })
               .HasDatabaseName("IX_SeedDataOperation_OperationType_Duration");
               
        builder.HasIndex(e => e.InitializationLogId)
               .HasDatabaseName("IX_SeedDataOperation_InitializationLogId");
    }
}

// ApplicationDbContext.cs extensions
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Existing configuration...
    base.OnModelCreating(modelBuilder);
    
    // Add initialization tracking configurations
    modelBuilder.ApplyConfiguration(new InitializationTrackingConfiguration());
    modelBuilder.ApplyConfiguration(new SeedDataOperationConfiguration());
}

// Add DbSets to ApplicationDbContext
public DbSet<InitializationLog> InitializationLogs { get; set; }
public DbSet<SeedDataOperation> SeedDataOperations { get; set; }
```

## Migration Strategy

### Migration Versioning Approach

```csharp
// Migration: AddDatabaseInitializationTracking
public partial class AddDatabaseInitializationTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create InitializationLog table
        migrationBuilder.CreateTable(
            name: "InitializationLog",
            schema: "public",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                StartedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                Status = table.Column<string>(type: "text", maxLength: 20, nullable: false),
                Environment = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                MigrationsApplied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                SeedRecordsCreated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                ErrorMessage = table.Column<string>(type: "text", nullable: true),
                StackTrace = table.Column<string>(type: "text", nullable: true),
                InitializationOptions = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InitializationLog", x => x.Id);
                table.CheckConstraint("CHK_InitializationLog_CompletedDuration", 
                    "(\"CompletedAt\" IS NULL AND \"Duration\" IS NULL) OR (\"CompletedAt\" IS NOT NULL AND \"Duration\" IS NOT NULL)");
                table.CheckConstraint("CHK_InitializationLog_FailedError", 
                    "(\"Status\" != 'Failed') OR (\"Status\" = 'Failed' AND \"ErrorMessage\" IS NOT NULL)");
                table.CheckConstraint("CHK_InitializationLog_Status", 
                    "\"Status\" IN ('Running', 'Success', 'Failed')");
            });

        // Create SeedDataOperation table
        migrationBuilder.CreateTable(
            name: "SeedDataOperation",
            schema: "public",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                InitializationLogId = table.Column<Guid>(type: "uuid", nullable: false),
                OperationType = table.Column<string>(type: "text", maxLength: 20, nullable: false),
                RecordsCreated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                StartedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                ErrorMessage = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SeedDataOperation", x => x.Id);
                table.ForeignKey("FK_SeedDataOperation_InitializationLog_InitializationLogId", 
                    x => x.InitializationLogId, "public", "InitializationLog", "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CHK_SeedDataOperation_PositiveDuration", 
                    "\"Duration\" > INTERVAL '0 seconds'");
                table.CheckConstraint("CHK_SeedDataOperation_LogicalCounts", 
                    "(\"Success\" = TRUE AND \"RecordsCreated\" >= 0) OR (\"Success\" = FALSE AND \"RecordsCreated\" = 0)");
                table.CheckConstraint("CHK_SeedDataOperation_OperationType", 
                    "\"OperationType\" IN ('Users', 'Events', 'VettingStatuses')");
            });

        // Create performance indexes
        migrationBuilder.CreateIndex(
            name: "IX_InitializationLog_Environment_StartedAt",
            schema: "public",
            table: "InitializationLog",
            columns: new[] { "Environment", "StartedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InitializationLog_Status_StartedAt",
            schema: "public",
            table: "InitializationLog",
            columns: new[] { "Status", "StartedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InitializationLog_Failed_StartedAt",
            schema: "public",
            table: "InitializationLog",
            column: "StartedAt",
            filter: "\"Status\" = 'Failed'");

        migrationBuilder.CreateIndex(
            name: "IX_SeedDataOperation_OperationType_Duration",
            schema: "public",
            table: "SeedDataOperation",
            columns: new[] { "OperationType", "Duration" });

        migrationBuilder.CreateIndex(
            name: "IX_SeedDataOperation_InitializationLogId",
            schema: "public",
            table: "SeedDataOperation",
            column: "InitializationLogId");

        // Create audit triggers
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.""UpdatedAt"" = NOW();
                RETURN NEW;
            END;
            $$ language 'plpgsql';

            CREATE TRIGGER update_initializationlog_updated_at 
                BEFORE UPDATE ON public.""InitializationLog"" 
                FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

            CREATE TRIGGER update_seeddataoperation_updated_at 
                BEFORE UPDATE ON public.""SeedDataOperation"" 
                FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Create cleanup function
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION cleanup_initialization_logs()
            RETURNS void AS $$
            BEGIN
                DELETE FROM public.""InitializationLog"" 
                WHERE ""Id"" IN (
                    SELECT ""Id"" 
                    FROM (
                        SELECT ""Id"", 
                               ROW_NUMBER() OVER (
                                   PARTITION BY ""Environment"", ""Status"" 
                                   ORDER BY ""StartedAt"" DESC
                               ) as row_num
                        FROM public.""InitializationLog"" 
                        WHERE ""Status"" = 'Success'
                    ) ranked 
                    WHERE row_num > 100
                );
                
                DELETE FROM public.""InitializationLog"" 
                WHERE ""Status"" = 'Running' 
                AND ""StartedAt"" < NOW() - INTERVAL '30 days';
            END;
            $$ LANGUAGE plpgsql;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop triggers and functions
        migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS update_initializationlog_updated_at ON public.""InitializationLog"";
            DROP TRIGGER IF EXISTS update_seeddataoperation_updated_at ON public.""SeedDataOperation"";
            DROP FUNCTION IF EXISTS update_updated_at_column();
            DROP FUNCTION IF EXISTS cleanup_initialization_logs();
        ");

        // Drop tables in reverse order (foreign key dependencies)
        migrationBuilder.DropTable(name: "SeedDataOperation", schema: "public");
        migrationBuilder.DropTable(name: "InitializationLog", schema: "public");
    }
}
```

### Idempotent Seed Data Patterns

```csharp
// Services/SeedDataService.cs
public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SeedDataService> _logger;

    public async Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default)
    {
        var initLog = new InitializationLog 
        { 
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };
        
        _context.InitializationLogs.Add(initLog);
        await _context.SaveChangesAsync(cancellationToken);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var totalRecords = 0;
            
            // Use EF Core 9's UseAsyncSeeding for automatic idempotency
            await _context.Database.UseAsyncSeeding(async (context, _, ct) =>
            {
                // Seed test accounts using comprehensive idempotency checks
                var userOperation = await SeedTestAccountsAsync(initLog.Id, ct);
                totalRecords += userOperation.RecordsCreated;
                
                // Seed events using date-based idempotency
                var eventOperation = await SeedEventsAsync(initLog.Id, ct);
                totalRecords += eventOperation.RecordsCreated;
                
                // Seed vetting statuses using name-based idempotency
                var vettingOperation = await SeedVettingStatusesAsync(initLog.Id, ct);
                totalRecords += vettingOperation.RecordsCreated;
                
            }, cancellationToken);

            // Update completion status
            initLog.CompletedAt = DateTime.UtcNow;
            initLog.Status = InitializationStatus.Success;
            initLog.Duration = initLog.CompletedAt - initLog.StartedAt;
            initLog.SeedRecordsCreated = totalRecords;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new InitializationResult
            {
                Success = true,
                Duration = initLog.Duration.Value,
                SeedRecordsCreated = totalRecords,
                CompletedAt = initLog.CompletedAt.Value
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            initLog.Status = InitializationStatus.Failed;
            initLog.ErrorMessage = ex.Message;
            initLog.StackTrace = ex.StackTrace;
            initLog.CompletedAt = DateTime.UtcNow;
            initLog.Duration = initLog.CompletedAt - initLog.StartedAt;
            
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<SeedDataOperation> SeedTestAccountsAsync(Guid initLogId, CancellationToken cancellationToken)
    {
        var operation = new SeedDataOperation
        {
            InitializationLogId = initLogId,
            OperationType = SeedOperationType.Users,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var accountsCreated = 0;
            
            var testAccounts = new[]
            {
                new { Email = "admin@witchcityrope.com", SceneName = "RopeMaster", Role = "Administrator" },
                new { Email = "staff@witchcityrope.com", SceneName = "SafetyFirst", Role = "Moderator" },
                new { Email = "member@witchcityrope.com", SceneName = "RopeEnthusiast", Role = "Member" },
                new { Email = "guest@witchcityrope.com", SceneName = "Newcomer", Role = "Attendee" },
                new { Email = "organizer@witchcityrope.com", SceneName = "EventMaker", Role = "Organizer" }
            };

            foreach (var account in testAccounts)
            {
                // Idempotent check: Skip if user already exists
                var existingUser = await _userManager.FindByEmailAsync(account.Email);
                if (existingUser != null)
                {
                    _logger.LogDebug("User {Email} already exists, skipping creation", account.Email);
                    continue;
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = account.Email,
                    Email = account.Email,
                    EmailConfirmed = true,
                    SceneName = account.SceneName,
                    Role = account.Role,
                    IsActive = true,
                    IsVetted = account.Role == "Administrator" || account.Role == "Moderator",
                    EncryptedLegalName = $"encrypted_legal_name_{account.SceneName.ToLower()}",
                    DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    PronouncedName = account.SceneName,
                    Pronouns = "they/them",
                    EmailVerificationToken = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    accountsCreated++;
                    _logger.LogDebug("Created test user: {Email} ({SceneName})", account.Email, account.SceneName);
                }
                else
                {
                    _logger.LogWarning("Failed to create user {Email}: {Errors}", 
                        account.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            operation.CompletedAt = DateTime.UtcNow;
            operation.Duration = operation.CompletedAt - operation.StartedAt;
            operation.Success = true;
            operation.RecordsCreated = accountsCreated;

            _context.SeedDataOperations.Add(operation);
            return operation;
        }
        catch (Exception ex)
        {
            operation.CompletedAt = DateTime.UtcNow;
            operation.Duration = operation.CompletedAt - operation.StartedAt;
            operation.Success = false;
            operation.ErrorMessage = ex.Message;
            
            _context.SeedDataOperations.Add(operation);
            throw;
        }
    }

    private async Task<SeedDataOperation> SeedEventsAsync(Guid initLogId, CancellationToken cancellationToken)
    {
        var operation = new SeedDataOperation
        {
            InitializationLogId = initLogId,
            OperationType = SeedOperationType.Events,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Idempotent check: Skip if events already exist for recent dates
            var hasRecentEvents = await _context.Events
                .AnyAsync(e => e.StartDate > DateTime.UtcNow.AddDays(-7), cancellationToken);
                
            if (hasRecentEvents)
            {
                _logger.LogDebug("Recent events already exist, skipping event seeding");
                operation.CompletedAt = DateTime.UtcNow;
                operation.Duration = operation.CompletedAt - operation.StartedAt;
                operation.Success = true;
                operation.RecordsCreated = 0;
                
                _context.SeedDataOperations.Add(operation);
                return operation;
            }

            var eventsCreated = 0;
            var seedEvents = CreateSeedEventsData();

            foreach (var eventData in seedEvents)
            {
                _context.Events.Add(eventData);
                eventsCreated++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            operation.CompletedAt = DateTime.UtcNow;
            operation.Duration = operation.CompletedAt - operation.StartedAt;
            operation.Success = true;
            operation.RecordsCreated = eventsCreated;

            _context.SeedDataOperations.Add(operation);
            return operation;
        }
        catch (Exception ex)
        {
            operation.CompletedAt = DateTime.UtcNow;
            operation.Duration = operation.CompletedAt - operation.StartedAt;
            operation.Success = false;
            operation.ErrorMessage = ex.Message;
            
            _context.SeedDataOperations.Add(operation);
            throw;
        }
    }

    private List<Event> CreateSeedEventsData()
    {
        return new List<Event>
        {
            // Beginner Events
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Introduction to Rope Safety",
                Description = "Learn the fundamentals of safe rope bondage practices",
                StartDate = DateTime.UtcNow.AddDays(7).Date.AddHours(18), // 6 PM, 7 days from now
                EndDate = DateTime.UtcNow.AddDays(7).Date.AddHours(21),   // 9 PM
                Capacity = 20,
                EventType = "Workshop",
                Location = "Main Dungeon Space",
                IsPublished = true,
                PricingTiers = "$25-$45 (sliding scale)",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Beginner's Rope Jam",
                Description = "Practice your rope skills in a supportive environment",
                StartDate = DateTime.UtcNow.AddDays(10).Date.AddHours(19), // 7 PM, 10 days from now
                EndDate = DateTime.UtcNow.AddDays(10).Date.AddHours(22),   // 10 PM
                Capacity = 30,
                EventType = "Social",
                Location = "Community Hall",
                IsPublished = true,
                PricingTiers = "$10-$20 (sliding scale)",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            // Continue with remaining 10 events per DATABASE-SEED-DATA-2.md...
        };
    }
}
```

## Transaction Boundaries

### Atomic Operation Design

```csharp
// Services/DatabaseInitializationService.cs
public class DatabaseInitializationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds)); // 30-second timeout
            
            // Phase 1: Apply migrations with retry policy
            await ApplyMigrationsWithRetryAsync(context, cts.Token);
            
            // Phase 2: Seed data with comprehensive transaction management
            if (ShouldPopulateSeedData())
            {
                await ExecuteSeedDataWithTransactionAsync(context, cts.Token);
            }
            
            _initializationCompleted = true;
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Database initialization cancelled due to application shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database initialization failed - application will not start properly");
            Environment.Exit(1); // Milan Jovanovic's fail-fast pattern
        }
    }

    private async Task ExecuteSeedDataWithTransactionAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        // Use EF Core 9's UseAsyncSeeding with comprehensive transaction management
        await context.Database.UseAsyncSeeding(async (ctx, _, ct) =>
        {
            // Nested transaction scope for seed operations
            using var nestedTransaction = await ctx.Database.BeginTransactionAsync(ct);
            
            try
            {
                var seedService = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<ISeedDataService>();
                var result = await seedService.SeedAllDataAsync(ct);
                
                // Commit only if all seed operations succeed
                await nestedTransaction.CommitAsync(ct);
                
                _logger.LogInformation("Seed data operations completed successfully: {RecordsCreated} records", 
                    result.SeedRecordsCreated);
            }
            catch (Exception ex)
            {
                await nestedTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Seed data transaction rolled back due to error");
                throw;
            }
        }, cancellationToken);
    }

    private async Task ApplyMigrationsWithRetryAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        // Polly retry policy for migration application
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _options.MaxRetryAttempts,  // 3 retries
                retryAttempt => TimeSpan.FromSeconds(_options.RetryDelaySeconds * Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, _) =>
                {
                    _logger.LogWarning("Migration attempt {RetryCount} failed, retrying in {Delay}ms: {Error}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            // Check for pending migrations within timeout
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                    
                // Apply migrations with timeout protection
                await context.Database.MigrateAsync(cancellationToken);
                
                _logger.LogInformation("Successfully applied {Count} migrations", pendingMigrations.Count());
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }
        });
    }
}
```

## Index Strategies

### Performance Optimization Indexes

```sql
-- ============================================================================
-- Primary Query Pattern Indexes
-- ============================================================================

-- Environment-based initialization monitoring (most common query)
CREATE INDEX "IX_InitializationLog_Environment_StartedAt" 
ON "InitializationLog" ("Environment", "StartedAt" DESC);

-- Status-based health check queries
CREATE INDEX "IX_InitializationLog_Status_StartedAt" 
ON "InitializationLog" ("Status", "StartedAt" DESC);

-- Seed operation performance analysis
CREATE INDEX "IX_SeedDataOperation_OperationType_Duration" 
ON "SeedDataOperation" ("OperationType", "Duration");

-- ============================================================================
-- Sparse Indexes for Optimization
-- ============================================================================

-- Failed operations (only ~1-5% of records expected)
CREATE INDEX "IX_InitializationLog_Failed_StartedAt" 
ON "InitializationLog" ("StartedAt" DESC) 
WHERE "Status" = 'Failed';

-- Running operations (should be very rare in steady state)
CREATE INDEX "IX_InitializationLog_Running_StartedAt" 
ON "InitializationLog" ("StartedAt" DESC) 
WHERE "Status" = 'Running';

-- Long-running seed operations (performance monitoring)
CREATE INDEX "IX_SeedDataOperation_LongRunning" 
ON "SeedDataOperation" ("OperationType", "Duration" DESC) 
WHERE "Duration" > INTERVAL '30 seconds';

-- ============================================================================
-- JSONB Indexes for Configuration Queries
-- ============================================================================

-- GIN index for JSON queries on initialization options
CREATE INDEX "IX_InitializationLog_Options_GIN" 
ON "InitializationLog" USING GIN ("InitializationOptions");

-- Specific path indexes for common configuration queries
CREATE INDEX "IX_InitializationLog_TimeoutConfig" 
ON "InitializationLog" (("InitializationOptions" ->> 'TimeoutSeconds')) 
WHERE "InitializationOptions" ? 'TimeoutSeconds';

-- ============================================================================
-- Composite Indexes for Complex Queries
-- ============================================================================

-- Health check dashboard query optimization
CREATE INDEX "IX_InitializationLog_HealthCheck" 
ON "InitializationLog" ("Environment", "Status", "StartedAt" DESC, "Duration");

-- Performance monitoring query optimization
CREATE INDEX "IX_SeedDataOperation_Performance" 
ON "SeedDataOperation" ("InitializationLogId", "OperationType", "Success", "Duration");

-- ============================================================================
-- Index Usage Analysis Query
-- ============================================================================

-- Query to monitor index effectiveness
CREATE OR REPLACE VIEW initialization_index_usage AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    ROUND(
        CASE 
            WHEN idx_scan = 0 THEN 0 
            ELSE (100.0 * idx_tup_read / idx_scan) 
        END, 2
    ) AS avg_tuples_per_scan
FROM pg_stat_user_indexes 
WHERE schemaname = 'public' 
AND (tablename = 'InitializationLog' OR tablename = 'SeedDataOperation')
ORDER BY idx_scan DESC;
```

### Index Maintenance Strategy

```sql
-- ============================================================================
-- Index Maintenance and Statistics
-- ============================================================================

-- Function to update table statistics for optimal query planning
CREATE OR REPLACE FUNCTION update_initialization_statistics()
RETURNS void AS $$
BEGIN
    ANALYZE public."InitializationLog";
    ANALYZE public."SeedDataOperation";
    
    -- Log statistics update
    INSERT INTO public."InitializationLog" 
    ("Environment", "Status", "MigrationsApplied", "SeedRecordsCreated", "StartedAt", "CompletedAt", "Duration")
    VALUES 
    ('System', 'Success', 0, 0, NOW(), NOW(), INTERVAL '0 seconds');
END;
$$ LANGUAGE plpgsql;

-- Function to monitor index bloat and recommend maintenance
CREATE OR REPLACE FUNCTION check_initialization_index_health()
RETURNS TABLE (
    table_name text,
    index_name text,
    size_mb numeric,
    bloat_ratio numeric,
    maintenance_needed boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        t.tablename::text,
        t.indexname::text,
        ROUND(pg_relation_size(t.indexrelid) / (1024.0 * 1024.0), 2) as size_mb,
        ROUND(
            CASE 
                WHEN pg_relation_size(t.indexrelid) = 0 THEN 0 
                ELSE (pg_relation_size(t.indexrelid)::numeric / NULLIF(pg_total_relation_size(c.oid), 0)) * 100 
            END, 2
        ) as bloat_ratio,
        (pg_relation_size(t.indexrelid) > 50 * 1024 * 1024) as maintenance_needed -- 50MB threshold
    FROM pg_stat_user_indexes t
    JOIN pg_class c ON c.oid = t.relid
    WHERE t.schemaname = 'public' 
    AND (t.tablename = 'InitializationLog' OR t.tablename = 'SeedDataOperation')
    ORDER BY pg_relation_size(t.indexrelid) DESC;
END;
$$ LANGUAGE plpgsql;
```

## Performance Considerations

### Initialization Performance Benchmarks

```csharp
// Models/InitializationPerformanceMetrics.cs
public class InitializationPerformanceMetrics
{
    public TimeSpan MigrationDuration { get; set; }
    public TimeSpan UserSeedingDuration { get; set; }
    public TimeSpan EventSeedingDuration { get; set; }
    public TimeSpan VettingSeedingDuration { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int DatabaseConnections { get; set; }
    public string Environment { get; set; } = string.Empty;
    
    public bool MeetsPerformanceTargets =>
        TotalDuration.TotalSeconds <= 30 && // 30-second timeout requirement
        MemoryUsageBytes <= 100 * 1024 * 1024; // 100MB memory target
}

// Services/PerformanceMonitoringService.cs
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    
    public async Task<InitializationPerformanceMetrics> MeasureInitializationPerformanceAsync(
        Func<Task> initializationAction)
    {
        var metrics = new InitializationPerformanceMetrics
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };
        
        var startMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await initializationAction();
            
            stopwatch.Stop();
            metrics.TotalDuration = stopwatch.Elapsed;
            metrics.MemoryUsageBytes = GC.GetTotalMemory(true) - startMemory;
            
            // Log performance metrics
            _logger.LogInformation(
                "Initialization Performance: Duration={Duration}ms, Memory={MemoryMB}MB, MeetsTargets={MeetsTargets}",
                metrics.TotalDuration.TotalMilliseconds,
                metrics.MemoryUsageBytes / (1024.0 * 1024.0),
                metrics.MeetsPerformanceTargets);
                
            return metrics;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.TotalDuration = stopwatch.Elapsed;
            metrics.MemoryUsageBytes = GC.GetTotalMemory(true) - startMemory;
            
            _logger.LogError(ex, 
                "Initialization failed after {Duration}ms using {MemoryMB}MB memory",
                metrics.TotalDuration.TotalMilliseconds,
                metrics.MemoryUsageBytes / (1024.0 * 1024.0));
                
            throw;
        }
    }
}
```

### Database Connection Optimization

```csharp
// Configuration/DbInitializationOptions.cs
public class DbInitializationOptions
{
    public bool EnableAutoMigration { get; set; } = true;
    public bool EnableSeedData { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public bool FailOnSeedDataError { get; set; } = true;
    public List<string> ExcludedEnvironments { get; set; } = new() { "Production" };
    public int MaxRetryAttempts { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 2.0;
    public bool EnableHealthChecks { get; set; } = true;
    
    // Performance optimization settings
    public int MaxDatabaseConnections { get; set; } = 5; // Limited for initialization
    public int BatchSize { get; set; } = 100; // For bulk operations
    public bool EnableParallelSeeding { get; set; } = false; // Serial by default for safety
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(120);
}

// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseInitialization(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure DbContext with optimized settings for initialization
        services.ConfigureDbContextForInitialization(configuration);
        
        // Register initialization services
        services.Configure<DbInitializationOptions>(
            configuration.GetSection("DatabaseInitialization"));
            
        services.AddScoped<ISeedDataService, SeedDataService>();
        services.AddScoped<PerformanceMonitoringService>();
        
        // Register hosted service for startup initialization
        services.AddHostedService<DatabaseInitializationService>();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseInitializationHealthCheck>("db-initialization");
            
        return services;
    }
    
    private static void ConfigureDbContextForInitialization(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.Configure<DbContextOptions>(options =>
        {
            // Optimize connection pool for initialization workload
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var optimizedConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                MaxPoolSize = 5, // Limited pool for initialization
                MinPoolSize = 1,
                ConnectionLifetime = 300, // 5 minutes
                CommandTimeout = 120, // 2 minutes for long operations
                KeepAlive = 30
            }.ToString();
            
            // Configure for performance during initialization
            services.PostConfigure<DbContextOptions<ApplicationDbContext>>(dbOptions =>
            {
                // Disable change tracking for read operations during seeding
                dbOptions.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                
                // Enable sensitive data logging in development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    dbOptions.EnableSensitiveDataLogging();
                    dbOptions.EnableDetailedErrors();
                }
            });
        });
    }
}
```

## Security & Monitoring

### Security Considerations

```csharp
// Security/InitializationSecurityService.cs
public class InitializationSecurityService
{
    private readonly ILogger<InitializationSecurityService> _logger;
    
    public async Task<bool> ValidateInitializationSecurityAsync(string environment)
    {
        var securityChecks = new List<(string Check, bool Passed, string Message)>();
        
        // Environment-specific security validation
        securityChecks.Add(ValidateEnvironmentSafety(environment));
        securityChecks.Add(await ValidateDatabasePermissionsAsync());
        securityChecks.Add(ValidateConnectionSecurity());
        securityChecks.Add(ValidateSeedDataSecurity());
        
        var failedChecks = securityChecks.Where(c => !c.Passed).ToList();
        
        if (failedChecks.Any())
        {
            _logger.LogError("Initialization security validation failed: {FailedChecks}",
                string.Join(", ", failedChecks.Select(c => c.Message)));
            return false;
        }
        
        _logger.LogInformation("All initialization security checks passed");
        return true;
    }
    
    private (string Check, bool Passed, string Message) ValidateEnvironmentSafety(string environment)
    {
        // Production safety: No automatic seed data in production
        var isProd = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
        var isSecure = !isProd || !ShouldRunSeedData(environment);
        
        return ("Environment Safety", isSecure, 
            isProd && !isSecure ? "Seed data disabled in production" : "Environment is safe");
    }
    
    private async Task<(string Check, bool Passed, string Message)> ValidateDatabasePermissionsAsync()
    {
        try
        {
            // Validate database connection and permissions
            using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Check schema permissions
            var hasPermissions = await CheckSchemaPermissionsAsync(connection);
            
            return ("Database Permissions", hasPermissions, 
                hasPermissions ? "Database permissions validated" : "Insufficient database permissions");
        }
        catch (Exception ex)
        {
            return ("Database Permissions", false, $"Permission check failed: {ex.Message}");
        }
    }
    
    private (string Check, bool Passed, string Message) ValidateConnectionSecurity()
    {
        var connectionString = GetConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        
        // Security requirements for initialization
        var isSecure = !string.IsNullOrEmpty(builder.Password) && // Has password
                      builder.SslMode != SslMode.Disable &&      // SSL enabled
                      !builder.TrustServerCertificate &&         // Certificate validation
                      builder.MaxPoolSize <= 10;                 // Reasonable pool size
        
        return ("Connection Security", isSecure, 
            isSecure ? "Connection security validated" : "Connection security requirements not met");
    }
    
    private (string Check, bool Passed, string Message) ValidateSeedDataSecurity()
    {
        // Validate that seed data uses secure patterns
        var testPassword = "Test123!"; // Standard test password
        var isSecure = testPassword.Length >= 8 &&
                      testPassword.Any(char.IsUpper) &&
                      testPassword.Any(char.IsLower) &&
                      testPassword.Any(char.IsDigit) &&
                      testPassword.Any(c => !char.IsLetterOrDigit(c));
        
        return ("Seed Data Security", isSecure, 
            isSecure ? "Seed data uses secure patterns" : "Seed data security requirements not met");
    }
}
```

### Monitoring and Health Checks

```csharp
// HealthChecks/DatabaseInitializationHealthCheck.cs
public class DatabaseInitializationHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseInitializationHealthCheck> _logger;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if initialization has completed successfully
            var latestInit = await _context.InitializationLogs
                .AsNoTracking()
                .OrderByDescending(i => i.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (latestInit == null)
            {
                return HealthCheckResult.Degraded("No initialization attempts found");
            }
            
            var healthData = new Dictionary<string, object>
            {
                ["initializationCompleted"] = latestInit.Status == InitializationStatus.Success,
                ["lastInitialization"] = latestInit.StartedAt,
                ["migrationsApplied"] = latestInit.MigrationsApplied,
                ["seedDataCreated"] = latestInit.SeedRecordsCreated,
                ["duration"] = latestInit.Duration?.ToString() ?? "N/A",
                ["environment"] = latestInit.Environment
            };
            
            return latestInit.Status switch
            {
                InitializationStatus.Success => HealthCheckResult.Healthy("Database initialization completed successfully", healthData),
                InitializationStatus.Running => HealthCheckResult.Degraded("Database initialization in progress", healthData),
                InitializationStatus.Failed => HealthCheckResult.Unhealthy($"Database initialization failed: {latestInit.ErrorMessage}", healthData),
                _ => HealthCheckResult.Unhealthy("Unknown initialization status", healthData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check failed", new { error = ex.Message });
        }
    }
}

// Monitoring/InitializationMetricsCollector.cs
public class InitializationMetricsCollector
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InitializationMetricsCollector> _logger;
    
    public async Task<InitializationMetrics> CollectMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new InitializationMetrics();
        
        // Collect initialization statistics
        var initStats = await _context.InitializationLogs
            .AsNoTracking()
            .GroupBy(i => i.Environment)
            .Select(g => new EnvironmentInitStats
            {
                Environment = g.Key,
                TotalAttempts = g.Count(),
                SuccessfulAttempts = g.Count(i => i.Status == InitializationStatus.Success),
                FailedAttempts = g.Count(i => i.Status == InitializationStatus.Failed),
                AverageInitTime = g.Where(i => i.Duration.HasValue)
                                  .Average(i => i.Duration!.Value.TotalSeconds),
                LastInitialization = g.Max(i => i.StartedAt)
            })
            .ToListAsync(cancellationToken);
        
        metrics.EnvironmentStats = initStats;
        
        // Collect seed operation performance
        var seedStats = await _context.SeedDataOperations
            .AsNoTracking()
            .GroupBy(s => s.OperationType)
            .Select(g => new SeedOperationStats
            {
                OperationType = g.Key.ToString(),
                TotalOperations = g.Count(),
                SuccessfulOperations = g.Count(s => s.Success),
                AverageRecordsCreated = g.Average(s => s.RecordsCreated),
                AverageDuration = g.Average(s => s.Duration.TotalSeconds),
                MaxDuration = g.Max(s => s.Duration.TotalSeconds)
            })
            .ToListAsync(cancellationToken);
        
        metrics.SeedOperationStats = seedStats;
        
        return metrics;
    }
}

public class InitializationMetrics
{
    public List<EnvironmentInitStats> EnvironmentStats { get; set; } = new();
    public List<SeedOperationStats> SeedOperationStats { get; set; } = new();
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class EnvironmentInitStats
{
    public string Environment { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double AverageInitTime { get; set; }
    public DateTime LastInitialization { get; set; }
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulAttempts / TotalAttempts * 100 : 0;
}

public class SeedOperationStats
{
    public string OperationType { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public double AverageRecordsCreated { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
}
```

## Multi-Context Support

### Future Multi-Context Architecture

```csharp
// Services/IMultiContextInitializationService.cs
public interface IMultiContextInitializationService
{
    Task<InitializationResult> InitializeAllContextsAsync(CancellationToken cancellationToken = default);
    Task<InitializationResult> InitializeContextAsync<TContext>(CancellationToken cancellationToken = default) 
        where TContext : DbContext;
    Task<List<ContextInitializationStatus>> GetContextStatusesAsync(CancellationToken cancellationToken = default);
}

// Services/MultiContextInitializationService.cs
public class MultiContextInitializationService : IMultiContextInitializationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MultiContextInitializationService> _logger;
    private readonly DbInitializationOptions _options;
    
    public async Task<InitializationResult> InitializeAllContextsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<InitializationResult>();
        var totalDuration = TimeSpan.Zero;
        
        // Get all registered DbContext types
        var contextTypes = GetRegisteredDbContextTypes();
        
        foreach (var contextType in contextTypes)
        {
            try
            {
                _logger.LogInformation("Initializing context: {ContextType}", contextType.Name);
                
                var result = await InitializeContextByTypeAsync(contextType, cancellationToken);
                results.Add(result);
                totalDuration = totalDuration.Add(result.Duration);
                
                if (!result.Success && _options.FailOnSeedDataError)
                {
                    throw new InvalidOperationException($"Context initialization failed: {contextType.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize context: {ContextType}", contextType.Name);
                throw;
            }
        }
        
        return new InitializationResult
        {
            Success = results.All(r => r.Success),
            Duration = totalDuration,
            MigrationsApplied = results.Sum(r => r.MigrationsApplied),
            SeedRecordsCreated = results.Sum(r => r.SeedRecordsCreated),
            CompletedAt = DateTime.UtcNow,
            Errors = results.SelectMany(r => r.Errors).ToList()
        };
    }
    
    public async Task<InitializationResult> InitializeContextAsync<TContext>(CancellationToken cancellationToken = default) 
        where TContext : DbContext
    {
        return await InitializeContextByTypeAsync(typeof(TContext), cancellationToken);
    }
    
    private async Task<InitializationResult> InitializeContextByTypeAsync(Type contextType, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
        
        var stopwatch = Stopwatch.StartNew();
        var result = new InitializationResult();
        
        try
        {
            // Apply migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
                result.MigrationsApplied = pendingMigrations.Count();
            }
            
            // Seed data if supported
            if (context is ISeedableContext seedableContext)
            {
                await context.Database.UseAsyncSeeding(async (ctx, _, ct) =>
                {
                    var seedResult = await seedableContext.SeedDataAsync(ct);
                    result.SeedRecordsCreated = seedResult.RecordsCreated;
                }, cancellationToken);
            }
            
            result.Success = true;
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Duration = stopwatch.Elapsed;
            result.Errors.Add(ex.Message);
            
            throw;
        }
    }
    
    private List<Type> GetRegisteredDbContextTypes()
    {
        // Reflection to find all registered DbContext types
        using var scope = _serviceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        var dbContextTypes = new List<Type>();
        
        // Get all services that inherit from DbContext
        var services = serviceProvider.GetType()
            .GetProperty("ServiceCollection", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(serviceProvider) as IServiceCollection;
            
        if (services != null)
        {
            dbContextTypes.AddRange(
                services
                    .Where(s => s.ServiceType.IsSubclassOf(typeof(DbContext)))
                    .Select(s => s.ServiceType)
                    .Distinct()
            );
        }
        
        return dbContextTypes;
    }
    
    public async Task<List<ContextInitializationStatus>> GetContextStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<ContextInitializationStatus>();
        var contextTypes = GetRegisteredDbContextTypes();
        
        foreach (var contextType in contextTypes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                
                var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                
                statuses.Add(new ContextInitializationStatus
                {
                    ContextType = contextType.Name,
                    CanConnect = canConnect,
                    HasPendingMigrations = pendingMigrations.Any(),
                    PendingMigrationCount = pendingMigrations.Count(),
                    IsInitialized = canConnect && !pendingMigrations.Any()
                });
            }
            catch (Exception ex)
            {
                statuses.Add(new ContextInitializationStatus
                {
                    ContextType = contextType.Name,
                    CanConnect = false,
                    HasError = true,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        return statuses;
    }
}

// Models/ContextInitializationStatus.cs
public class ContextInitializationStatus
{
    public string ContextType { get; set; } = string.Empty;
    public bool CanConnect { get; set; }
    public bool HasPendingMigrations { get; set; }
    public int PendingMigrationCount { get; set; }
    public bool IsInitialized { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}

// Interfaces/ISeedableContext.cs
public interface ISeedableContext
{
    Task<SeedDataResult> SeedDataAsync(CancellationToken cancellationToken = default);
}

public class SeedDataResult
{
    public int RecordsCreated { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

## Rollback Considerations

### No Automatic Rollback Policy

Per stakeholder decision, this system implements a **no automatic rollback** policy with fail-fast behavior:

```csharp
// Services/InitializationFailureHandler.cs
public class InitializationFailureHandler
{
    private readonly ILogger<InitializationFailureHandler> _logger;
    
    public async Task HandleInitializationFailureAsync(Exception exception, InitializationContext context)
    {
        var errorId = Guid.NewGuid();
        
        // Log comprehensive failure information
        _logger.LogCritical(
            "Database initialization failure [{ErrorId}] - Environment: {Environment}, " +
            "Phase: {Phase}, Duration: {Duration}ms, Error: {Error}",
            errorId, context.Environment, context.CurrentPhase, 
            context.ElapsedTime.TotalMilliseconds, exception.Message);
        
        // Detailed diagnostic information
        _logger.LogDebug("Full exception details [{ErrorId}]: {Exception}", errorId, exception);
        
        // Record failure in database if possible
        await RecordFailureInDatabaseAsync(exception, context, errorId);
        
        // Provide specific guidance based on failure type
        var guidance = GenerateResolutionGuidance(exception, context);
        _logger.LogError("Resolution guidance [{ErrorId}]: {Guidance}", errorId, guidance);
        
        // No automatic rollback - fail fast
        throw new DatabaseInitializationException(
            $"Database initialization failed [{errorId}]: {exception.Message}. {guidance}", 
            exception);
    }
    
    private async Task RecordFailureInDatabaseAsync(Exception exception, InitializationContext context, Guid errorId)
    {
        try
        {
            // Record failure if database connection is available
            using var scope = context.ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var failureLog = new InitializationLog
            {
                Id = errorId,
                StartedAt = context.StartTime,
                Status = InitializationStatus.Failed,
                Environment = context.Environment,
                ErrorMessage = exception.Message,
                StackTrace = exception.StackTrace,
                Duration = context.ElapsedTime
            };
            
            dbContext.InitializationLogs.Add(failureLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogWarning(dbEx, "Could not record failure in database");
        }
    }
    
    private string GenerateResolutionGuidance(Exception exception, InitializationContext context)
    {
        return exception switch
        {
            NpgsqlException npgsqlEx when npgsqlEx.Message.Contains("connection") =>
                "Database connection failed. Verify PostgreSQL is running on localhost:5433 and connection string is correct.",
                
            InvalidOperationException when exception.Message.Contains("migration") =>
                "Migration error detected. Review recent migrations and database schema. Manual intervention required.",
                
            TimeoutException =>
                "Initialization timeout exceeded. Check database performance and network connectivity. Consider increasing timeout.",
                
            ArgumentException when exception.Message.Contains("seed") =>
                "Seed data error. Check for data conflicts, constraint violations, or duplicate records.",
                
            SecurityException =>
                "Security validation failed. Review database permissions and connection security settings.",
                
            _ => $"Initialization failed in {context.CurrentPhase} phase. Review logs for detailed error information and contact system administrator."
        };
    }
}

// Models/InitializationContext.cs
public class InitializationContext
{
    public string Environment { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Exceptions/DatabaseInitializationException.cs
public class DatabaseInitializationException : Exception
{
    public Guid ErrorId { get; }
    public string? Phase { get; }
    public string? Environment { get; }
    
    public DatabaseInitializationException(string message) : base(message)
    {
        ErrorId = Guid.NewGuid();
    }
    
    public DatabaseInitializationException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorId = Guid.NewGuid();
    }
    
    public DatabaseInitializationException(string message, string phase, string environment) : base(message)
    {
        ErrorId = Guid.NewGuid();
        Phase = phase;
        Environment = environment;
    }
}
```

### Manual Recovery Procedures

```sql
-- ============================================================================
-- Manual Recovery Procedures Documentation
-- ============================================================================

-- 1. Check initialization status
SELECT 
    "Environment",
    "Status",
    "StartedAt",
    "CompletedAt",
    "Duration",
    "ErrorMessage"
FROM public."InitializationLog"
ORDER BY "StartedAt" DESC
LIMIT 10;

-- 2. Check for stuck/running initializations
SELECT 
    "Id",
    "Environment",
    "StartedAt",
    "Duration",
    EXTRACT(EPOCH FROM (NOW() - "StartedAt")) / 60 as minutes_running
FROM public."InitializationLog"
WHERE "Status" = 'Running'
AND "StartedAt" < NOW() - INTERVAL '5 minutes';

-- 3. Mark stuck initializations as failed (manual cleanup)
UPDATE public."InitializationLog"
SET 
    "Status" = 'Failed',
    "CompletedAt" = NOW(),
    "Duration" = NOW() - "StartedAt",
    "ErrorMessage" = 'Manually marked as failed - process timeout',
    "UpdatedAt" = NOW()
WHERE "Status" = 'Running'
AND "StartedAt" < NOW() - INTERVAL '30 minutes';

-- 4. Check seed data operation details
SELECT 
    il."Environment",
    sdo."OperationType",
    sdo."Success",
    sdo."RecordsCreated",
    sdo."Duration",
    sdo."ErrorMessage"
FROM public."SeedDataOperation" sdo
JOIN public."InitializationLog" il ON il."Id" = sdo."InitializationLogId"
WHERE il."Status" = 'Failed'
ORDER BY sdo."StartedAt" DESC;

-- 5. Clean up test data (if needed for fresh start)
-- WARNING: Only run in development environments
DO $$
BEGIN
    IF current_setting('application_name') = 'development' THEN
        -- Remove test users
        DELETE FROM auth."Users" 
        WHERE "Email" IN (
            'admin@witchcityrope.com',
            'staff@witchcityrope.com', 
            'member@witchcityrope.com',
            'guest@witchcityrope.com',
            'organizer@witchcityrope.com'
        );
        
        -- Remove seed events (be careful with date ranges)
        DELETE FROM public."Events"
        WHERE "Title" IN (
            'Introduction to Rope Safety',
            'Beginner''s Rope Jam',
            'Spring Rope Basics (Past)',
            'Valentine''s Rope Social (Past)'
        );
        
        RAISE NOTICE 'Test data cleaned up successfully';
    ELSE
        RAISE EXCEPTION 'Manual cleanup only allowed in development environment';
    END IF;
END $$;

-- 6. Check EF Core migration history
SELECT * FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";

-- 7. Check EF Core 9 seed history (if available)
SELECT * FROM public."__EFSeedHistory" ORDER BY "ExecutedOn" DESC;

-- 8. Performance analysis of initialization operations
SELECT 
    "Environment",
    "Status",
    AVG(EXTRACT(EPOCH FROM "Duration")) as avg_seconds,
    MAX(EXTRACT(EPOCH FROM "Duration")) as max_seconds,
    COUNT(*) as total_attempts
FROM public."InitializationLog"
WHERE "StartedAt" > NOW() - INTERVAL '30 days'
GROUP BY "Environment", "Status"
ORDER BY "Environment", "Status";
```

## Configuration and Environment Settings

### Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,
    "EnableSeedData": true,
    "TimeoutSeconds": 30,
    "FailOnSeedDataError": true,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 2.0,
    "ExcludedEnvironments": [],
    "EnableHealthChecks": true,
    "MaxDatabaseConnections": 5,
    "BatchSize": 100,
    "EnableParallelSeeding": false,
    "CommandTimeout": "00:02:00",
    "EnablePerformanceMonitoring": true,
    "EnableSecurityValidation": true
  },
  "Logging": {
    "LogLevel": {
      "WitchCityRope.Api.Services.DatabaseInitializationService": "Debug",
      "WitchCityRope.Api.Services.SeedDataService": "Debug"
    }
  }
}

// appsettings.Staging.json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,
    "EnableSeedData": true,
    "TimeoutSeconds": 60,
    "FailOnSeedDataError": false,
    "MaxRetryAttempts": 5,
    "RetryDelaySeconds": 3.0,
    "ExcludedEnvironments": [],
    "EnableHealthChecks": true,
    "MaxDatabaseConnections": 3,
    "BatchSize": 50,
    "EnableParallelSeeding": false,
    "CommandTimeout": "00:05:00",
    "EnablePerformanceMonitoring": true,
    "EnableSecurityValidation": true
  }
}

// appsettings.Production.json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,
    "EnableSeedData": false,
    "TimeoutSeconds": 120,
    "FailOnSeedDataError": false,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5.0,
    "ExcludedEnvironments": ["Production"],
    "EnableHealthChecks": true,
    "MaxDatabaseConnections": 2,
    "BatchSize": 25,
    "EnableParallelSeeding": false,
    "CommandTimeout": "00:10:00",
    "EnablePerformanceMonitoring": false,
    "EnableSecurityValidation": true
  },
  "Logging": {
    "LogLevel": {
      "WitchCityRope.Api.Services.DatabaseInitializationService": "Information",
      "WitchCityRope.Api.Services.SeedDataService": "Warning"
    }
  }
}
```

## Summary

This comprehensive database design provides:

1. **Robust Initialization Tracking**: Complete audit trail of all initialization operations with performance metrics
2. **EF Core 9 Integration**: Leverages UseAsyncSeeding for automatic idempotency and modern seeding patterns
3. **PostgreSQL Optimization**: Indexes, constraints, and triggers optimized for PostgreSQL performance
4. **Security & Monitoring**: Comprehensive health checks, security validation, and performance monitoring
5. **Multi-Context Ready**: Architecture supports future expansion to multiple DbContext scenarios
6. **Production Safety**: Environment-aware operations with fail-fast error handling and no automatic rollback
7. **Performance Benchmarks**: 30-second timeout requirement with detailed performance tracking
8. **Comprehensive Error Handling**: Milan Jovanovic's fail-fast patterns with detailed diagnostic information

The design builds upon existing ApplicationDbContext patterns while adding enterprise-grade initialization capabilities that transform the developer experience from manual 4-step setup to seamless zero-configuration operation.

### Key Design Decisions

- **No Automatic Rollback**: Fail-fast approach requires manual intervention for safety
- **Environment Awareness**: Production only runs migrations, development gets full seed data
- **Transaction Boundaries**: Complete transaction management for atomic operations
- **Performance Focus**: 30-second timeout with comprehensive optimization strategies
- **Monitoring Integration**: Health checks and metrics collection for operational visibility
- **Security First**: Comprehensive validation and audit trail for all operations

This design enables the transformation from manual database setup to automatic initialization while maintaining enterprise-grade reliability, security, and performance standards.