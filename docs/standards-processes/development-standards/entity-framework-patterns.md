# Entity Framework Core Development Standards

## Overview

This document defines mandatory patterns and best practices for Entity Framework Core usage in the WitchCityRope project. Following these standards prevents common issues and ensures database operations are reliable and maintainable.

## Critical DbContext Usage

### Correct DbContext Name

**Always use `WitchCityRopeIdentityDbContext`, NOT `WitchCityRopeDbContext`**

```csharp
// ✅ CORRECT
var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();

// ❌ WRONG - Old DbContext no longer exists
var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
```

## DateTime Handling with PostgreSQL

### All DateTime Values MUST Be UTC

PostgreSQL's `timestamp with time zone` type requires UTC DateTimes.

**❌ WRONG - Causes "Cannot write DateTime with Kind=Unspecified" Error:**
```csharp
new DateTime(1990, 1, 1)  // Kind is Unspecified
```

**✅ CORRECT - Specify UTC:**
```csharp
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### Automatic UTC Conversion

The DbContext automatically converts DateTime values to UTC in the `UpdateAuditFields()` method:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    UpdateAuditFields();
    return await base.SaveChangesAsync(cancellationToken);
}

private void UpdateAuditFields()
{
    var entries = ChangeTracker.Entries<BaseEntity>();
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

## Test Data Requirements

### Always Use Unique Values

Test data MUST use unique values to avoid constraint violations:

**❌ WRONG - Causes Duplicate Key Violations:**
```csharp
SceneName.Create("TestUser")
Email.Create("test@example.com")
```

**✅ CORRECT - Use GUIDs for Uniqueness:**
```csharp
SceneName.Create($"TestUser_{Guid.NewGuid():N}")
Email.Create($"test_{Guid.NewGuid():N}@example.com")
```

## Entity Configuration Best Practices

### Value Objects vs Entities

#### Value Objects
Configure value objects as owned types or store as primitives:

```csharp
// Option 1: Owned Type Configuration
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Email, email =>
    {
        email.Property(e => e.Value).HasColumnName("Email");
    });

// Option 2: Store as Primitive (Recommended)
modelBuilder.Entity<User>()
    .Property(u => u.EmailAddress)
    .HasColumnName("Email")
    .IsRequired();
```

### Nullable Owned Entities Issue

**CRITICAL**: EF Core cannot configure nullable owned entities with required properties.

**❌ WRONG - Will Fail:**
```csharp
public Money? RefundAmount { get; set; }

// With OwnsOne configuration
builder.OwnsOne(p => p.RefundAmount);
```

**✅ CORRECT - Use Separate Properties:**
```csharp
public decimal? RefundAmountValue { get; private set; }
public string? RefundCurrency { get; private set; }

public Money? RefundAmount => RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency)
    ? Money.Create(RefundAmountValue.Value, RefundCurrency) 
    : null;
```

### Entity Constructor Requirements

All entities MUST initialize required fields in constructors:

**❌ WRONG - Causes Duplicate Key Violations:**
```csharp
public class Rsvp
{
    public Rsvp(Guid userId, Event @event)
    {
        UserId = userId;
        Event = @event;
        // Missing Id initialization!
    }
}
```

**✅ CORRECT - Initialize All Required Fields:**
```csharp
public class Rsvp
{
    public Rsvp(Guid userId, Event @event)
    {
        Id = Guid.NewGuid();  // CRITICAL: Must set this!
        UserId = userId;
        Event = @event;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

## Navigation Property Management

### Removing Navigation Properties

When removing entities or changing relationships, remove ALL navigation properties:

**❌ WRONG - Causes Entity Discovery Issues:**
```csharp
// In DbContext
modelBuilder.Ignore<User>();

// But VolunteerAssignment still has:
public User User { get; set; }  // EF Core will discover User through this!
```

**✅ CORRECT - Remove Navigation, Keep Foreign Key:**
```csharp
public class VolunteerAssignment
{
    public Guid UserId { get; set; }
    // User navigation property removed
}
```

### Updating Include Statements

After removing navigation properties, update service queries:

**❌ WRONG - Include Fails Without Navigation:**
```csharp
var assignment = await _context.VolunteerAssignments
    .Include(a => a.User)  // Fails - no User navigation
    .FirstOrDefaultAsync(a => a.Id == id);
```

**✅ CORRECT - Fetch Related Data Separately:**
```csharp
var assignment = await _context.VolunteerAssignments
    .FirstOrDefaultAsync(a => a.Id == id);

var user = await _userManager.FindByIdAsync(assignment.UserId.ToString());
```

## Migration Best Practices

### Before Generating Migrations

1. **Ensure all code compiles:**
   ```bash
   dotnet build
   ```

2. **Check for unused navigation properties**

3. **Review entity configurations in DbContext**

4. **Use the provided script:**
   ```bash
   ./scripts/generate-migration.sh MigrationName
   ```

### Common Migration Issues

#### "Entity requires a primary key"
- Check for unwanted entity discovery through navigation properties
- Ensure value objects are configured with `OwnsOne` or ignored

#### Value objects treated as entities
- Configure as owned types
- Or store as primitive properties

#### After Architecture Changes
- Remove all navigation properties to replaced entities
- Update all `Include()` statements in services
- Add explicit `Ignore()` statements for removed entities

## Integration Testing with PostgreSQL

### Use Testcontainers

Integration tests MUST use real PostgreSQL via Testcontainers:

```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyIntegrationTest : IClassFixture<PostgreSqlFixture>
{
    // Test implementation
}
```

### Benefits of Real Database Testing
- Catches real database constraints
- Validates migrations
- Tests actual SQL generation
- No false positives from in-memory provider

## Common Pitfalls and Solutions

### 1. Entity Discovery Through Navigation
**Problem**: Ignored entities discovered through navigation properties
**Solution**: Remove all navigation properties to ignored entities

### 2. DateTime UTC Issues
**Problem**: PostgreSQL rejects non-UTC timestamps
**Solution**: Always use `DateTime.UtcNow` or specify `DateTimeKind.Utc`

### 3. Duplicate Test Data
**Problem**: Unique constraints fail in parallel tests
**Solution**: Use GUIDs in all test data

### 4. Nullable Owned Entities
**Problem**: EF Core configuration fails
**Solution**: Use separate nullable properties

### 5. Missing Id Initialization
**Problem**: Duplicate key violations with Guid.Empty
**Solution**: Initialize Id in entity constructors

## Health Check System

Always run health checks before integration tests:

```bash
# 1. Run health checks first
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. Only if health checks pass, run tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

Health checks validate:
- Database connection
- Schema and migrations
- Seed data presence

## Advanced Entity Configuration Patterns

### Separate Configuration Classes

**Issue**: EF configurations in OnModelCreating getting huge  
**Solution**: Use separate configuration classes

```csharp
// ✅ CORRECT - Separate configuration class
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        
        // Configure relationships
        builder.HasMany(e => e.Registrations)
               .WithOne(r => r.Event)
               .HasForeignKey(r => r.EventId);
    }
}

// In DbContext OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new EventConfiguration());
    modelBuilder.ApplyConfiguration(new UserConfiguration());
    // etc.
}
```

### PostgreSQL Specific Patterns

**Reference**: See [Database Developer Lessons](/docs/lessons-learned/database-developers.md) for PostgreSQL-specific patterns including:
- Case sensitivity handling with CITEXT and collation
- JSONB configuration and indexing
- Timezone handling and conversions
- UUID vs GUID mappings
- Reserved word handling

### Value Object Storage Strategies

#### Option 1: Owned Type Configuration
```csharp
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Email, email =>
    {
        email.Property(e => e.Value)
             .HasColumnName("Email")
             .IsRequired()
             .HasMaxLength(256);
    });
```

#### Option 2: Store as Primitive (Recommended)
```csharp
modelBuilder.Entity<User>()
    .Property(u => u.EmailAddress)
    .HasColumnName("Email")
    .IsRequired()
    .HasMaxLength(256);
```

## Query Optimization Patterns

### Selective Loading vs Include
```csharp
// ❌ WRONG - Loading entire entities when only need few fields
var names = await _context.Users
    .Include(u => u.Events)
    .Select(u => u.Name)
    .ToListAsync();

// ✅ CORRECT - Only loads what's needed
var names = await _context.Users
    .Select(u => u.Name)
    .ToListAsync();
```

### Pagination Best Practices
```csharp
// ✅ CORRECT - Always paginate large datasets
var pagedResults = await query
    .OrderBy(e => e.CreatedAt) // MUST order before Skip/Take
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### AsNoTracking for Read-Only Queries
```csharp
// ✅ CORRECT - Use AsNoTracking for read-only operations
var events = await _context.Events
    .AsNoTracking()
    .Where(e => e.StartTime > DateTime.UtcNow)
    .ToListAsync();
```

## Additional Resources

- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [EF Core Testing Documentation](https://docs.microsoft.com/ef/core/testing/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [EF Core Performance Best Practices](https://docs.microsoft.com/ef/core/performance/)