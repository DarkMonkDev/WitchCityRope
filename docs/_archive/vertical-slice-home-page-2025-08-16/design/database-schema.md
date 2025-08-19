# Database Schema Design: Vertical Slice Events POC

**Created**: 2025-08-16  
**Purpose**: Simple PostgreSQL schema for technical proof-of-concept  
**Status**: Throwaway code for stack validation  
**Owner**: Database Designer Agent

## Overview

This document defines a minimal PostgreSQL database schema for the vertical slice proof-of-concept. This is **throwaway code** designed solely to validate API ↔ Database communication in Step 2 of progressive testing.

## Architecture Context

- **Database**: PostgreSQL 15+ with Entity Framework Core 9
- **Pattern**: React → HTTP → API → EF Core → PostgreSQL
- **Scope**: Single table for events display testing
- **Lifetime**: Temporary - will be replaced with proper domain model

## Schema Design

### Events Table

```sql
-- Simple Events table for proof-of-concept
CREATE TABLE "Events" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "Location" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Basic index for date queries
CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");
```

### Design Decisions

#### 1. UUID Primary Key
- **Choice**: UUID with `gen_random_uuid()`
- **Reason**: Aligns with WitchCityRope standards, prevents ID conflicts
- **Pattern**: `Guid.NewGuid()` in C# maps to PostgreSQL UUID

#### 2. TIMESTAMPTZ for Dates
- **Choice**: `TIMESTAMPTZ` for all date/time fields
- **Reason**: PostgreSQL best practice, timezone-aware
- **Critical**: EF Core requires `DateTime.UtcNow` or `DateTimeKind.Utc`

#### 3. Case-Sensitive Column Names
- **Choice**: Quoted column names `"Events"`, `"Id"`
- **Reason**: Matches EF Core default naming conventions
- **Pattern**: Prevents PostgreSQL lowercasing issues

#### 4. Minimal Constraints
- **Choice**: Only NOT NULL constraints
- **Reason**: Proof-of-concept doesn't need complex validation
- **Future**: Production schema will have proper check constraints

## Entity Framework Core Configuration

### Event Entity Class

```csharp
// apps/api/Models/Event.cs
using System.ComponentModel.DataAnnotations;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### DbContext Configuration

```csharp
// apps/api/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Event entity configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", "public");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                  .IsRequired()
                  .HasColumnType("text");
            
            entity.Property(e => e.StartDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");
            
            entity.Property(e => e.Location)
                  .IsRequired()
                  .HasMaxLength(200);
            
            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz")
                  .HasDefaultValueSql("NOW()");
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Event>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
```

## Migration Strategy

### 1. Initial Migration Creation

```bash
# From apps/api directory
dotnet ef migrations add InitialCreate_Events_POC \
    --context ApplicationDbContext \
    --output-dir Migrations
```

### 2. Database Update

```bash
# Apply migration to PostgreSQL
dotnet ef database update --context ApplicationDbContext
```

### 3. Rollback Plan

```bash
# Remove migration if needed
dotnet ef migrations remove --context ApplicationDbContext
```

## Test Data

### Sample INSERT Statements

```sql
-- Test data for proof-of-concept validation
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "Location", "CreatedAt") VALUES
(
    gen_random_uuid(),
    'Rope Basics Workshop',
    'Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners.',
    '2025-08-25 14:00:00+00'::timestamptz,
    'Salem Community Center',
    NOW()
),
(
    gen_random_uuid(),
    'Advanced Suspension Techniques',
    'Advanced workshop covering suspension safety, rigging points, and dynamic movements.',
    '2025-08-30 19:00:00+00'::timestamptz,
    'Studio Space Downtown',
    NOW()
),
(
    gen_random_uuid(),
    'Community Social & Practice',
    'Open practice session for all skill levels. Bring your rope and practice with others.',
    '2025-09-05 18:30:00+00'::timestamptz,
    'Salem Arts Collective',
    NOW()
),
(
    gen_random_uuid(),
    'Photography & Rope Art',
    'Artistic rope photography session with professional photographer.',
    '2025-09-12 16:00:00+00'::timestamptz,
    'Private Studio',
    NOW()
);
```

### C# Seed Data Method

```csharp
// For EF Core data seeding in development
public static class EventSeeder
{
    public static void SeedTestData(ApplicationDbContext context)
    {
        if (context.Events.Any()) return; // Already seeded

        var events = new[]
        {
            new Event
            {
                Title = "Rope Basics Workshop",
                Description = "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners.",
                StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
                Location = "Salem Community Center"
            },
            new Event
            {
                Title = "Advanced Suspension Techniques", 
                Description = "Advanced workshop covering suspension safety, rigging points, and dynamic movements.",
                StartDate = new DateTime(2025, 8, 30, 19, 0, 0, DateTimeKind.Utc),
                Location = "Studio Space Downtown"
            },
            new Event
            {
                Title = "Community Social & Practice",
                Description = "Open practice session for all skill levels. Bring your rope and practice with others.",
                StartDate = new DateTime(2025, 9, 5, 18, 30, 0, DateTimeKind.Utc),
                Location = "Salem Arts Collective"
            },
            new Event
            {
                Title = "Photography & Rope Art",
                Description = "Artistic rope photography session with professional photographer.",
                StartDate = new DateTime(2025, 9, 12, 16, 0, 0, DateTimeKind.Utc),
                Location = "Private Studio"
            }
        };

        context.Events.AddRange(events);
        context.SaveChanges();
    }
}
```

## Performance Considerations

### Indexing Strategy

```sql
-- Date-based queries (most common)
CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");

-- Future: Add location index if needed
-- CREATE INDEX "IX_Events_Location" ON "Events" ("Location");
```

### Query Optimization

```csharp
// Efficient queries for the POC
public async Task<List<Event>> GetUpcomingEventsAsync()
{
    return await _context.Events
        .AsNoTracking() // Read-only for display
        .Where(e => e.StartDate > DateTime.UtcNow)
        .OrderBy(e => e.StartDate)
        .Take(10) // Limit results for POC
        .ToListAsync();
}
```

## Validation & Testing

### Database Connection Test

```csharp
[TestMethod]
public async Task Database_CanConnect_AndQueryEvents()
{
    // Arrange
    using var context = new ApplicationDbContext(GetTestDbOptions());
    
    // Act
    var canConnect = await context.Database.CanConnectAsync();
    var eventCount = await context.Events.CountAsync();
    
    // Assert
    Assert.IsTrue(canConnect, "Should connect to PostgreSQL");
    Assert.IsTrue(eventCount >= 0, "Should query Events table");
}
```

### DateTime UTC Validation

```csharp
[TestMethod]
public void Event_StartDate_MustBeUtc()
{
    // Arrange
    var testEvent = new Event
    {
        Title = "Test Event",
        Description = "Test",
        StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
        Location = "Test Location"
    };
    
    // Assert
    Assert.AreEqual(DateTimeKind.Utc, testEvent.StartDate.Kind);
}
```

## Security Considerations

### Minimal for POC

- **SQL Injection**: Protected by EF Core parameterization
- **Connection String**: Use environment variables for production
- **Data Validation**: Basic attributes, full validation in production schema

## Monitoring & Observability

### Database Health Check

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

## Migration Cleanup

### After POC Completion

1. **Document lessons learned** in database developer lessons
2. **Remove migration** if not needed for production
3. **Archive test data** scripts
4. **Update Entity Framework patterns** documentation

```bash
# Clean up test migration after POC
dotnet ef migrations remove --context ApplicationDbContext --force
```

## Related Documentation

- [Entity Framework Patterns](/home/chad/repos/witchcityrope-react/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [Functional Specification](/home/chad/repos/witchcityrope-react/docs/functional-areas/vertical-slice-home-page/requirements/functional-specification.md)
- [PostgreSQL Best Practices](https://www.postgresql.org/docs/current/ddl.html)

## Success Criteria

### Technical Validation
- [ ] Migration creates Events table successfully
- [ ] EF Core can query Events without errors
- [ ] API returns JSON from database queries
- [ ] DateTime handling works with PostgreSQL TIMESTAMPTZ
- [ ] Test data inserts and displays properly

### Cleanup Requirements
- [ ] Document any PostgreSQL-specific issues encountered
- [ ] Update EF Core patterns with lessons learned
- [ ] Prepare for production schema replacement

---

**Note**: This schema is intentionally minimal and will be replaced with a proper domain model after the technical proof-of-concept validates the React + .NET + PostgreSQL technology stack.