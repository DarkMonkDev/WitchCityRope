# ApplicationDbContext Updates for Venue Management

## File: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`

## Required Updates

### 1. Add Venue DbSet Property

**Location**: After line 250 (after UserNotes DbSet)

```csharp
/// <summary>
/// Venues table for venue management
/// </summary>
public DbSet<Venue> Venues { get; set; }
```

### 2. Add Venue Entity Configuration

**Location**: After line 413 (after Event entity configuration closing brace)

Insert this complete configuration block:

```csharp
// Venue entity configuration
modelBuilder.Entity<Venue>(entity =>
{
    // Table mapping
    entity.ToTable("Venues", "public");
    entity.HasKey(v => v.Id);

    // Property configurations
    entity.Property(v => v.Name)
          .IsRequired()
          .HasMaxLength(100);

    entity.Property(v => v.Directions)
          .HasMaxLength(500);

    entity.Property(v => v.Notes)
          .HasMaxLength(1000);

    entity.Property(v => v.IsActive)
          .IsRequired()
          .HasDefaultValue(true);

    // CRITICAL: Use timestamptz for PostgreSQL timezone awareness
    entity.Property(v => v.CreatedAt)
          .IsRequired()
          .HasColumnType("timestamptz");

    entity.Property(v => v.UpdatedAt)
          .IsRequired()
          .HasColumnType("timestamptz");

    // Indexes
    entity.HasIndex(v => v.Name)
          .IsUnique()
          .HasDatabaseName("IX_Venues_Name");

    entity.HasIndex(v => v.IsActive)
          .HasDatabaseName("IX_Venues_IsActive");

    // Navigation property to Events
    entity.HasMany(v => v.Events)
          .WithOne(e => e.Venue)
          .HasForeignKey(e => e.VenueId)
          .OnDelete(DeleteBehavior.SetNull);  // Preserve events if venue soft-deleted
});
```

### 3. Update Event Entity Configuration

**Location**: Inside Event entity configuration block (around line 390-412)

Add this AFTER the existing navigation properties and BEFORE the closing brace:

```csharp
// Venue relationship
entity.HasOne(e => e.Venue)
      .WithMany(v => v.Events)
      .HasForeignKey(e => e.VenueId)
      .OnDelete(DeleteBehavior.SetNull);

entity.HasIndex(e => e.VenueId)
      .HasDatabaseName("IX_Events_VenueId");
```

### 4. Update UpdateAuditFields Method

**Location**: Inside UpdateAuditFields() method at the end (around line 1540, after UserNote entities)

Add this before the closing brace of the method:

```csharp
// Handle Venue entities
var venueEntries = ChangeTracker.Entries<Venue>();
foreach (var entry in venueEntries)
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
```

## Verification Checklist

After making these changes:

- [ ] Build solution: `dotnet build`
- [ ] Verify no compilation errors
- [ ] ApplicationDbContext has Venues DbSet
- [ ] Venue configuration present in OnModelCreating
- [ ] Event-Venue relationship configured
- [ ] UpdateAuditFields handles Venue entities
- [ ] Ready to create migration

## Next Step

After verifying these changes, create the migration:

```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddVenueManagement
```
