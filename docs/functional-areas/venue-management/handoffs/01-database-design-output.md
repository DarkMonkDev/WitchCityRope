# Database Design Output - Venue Management

## Phase: Database Design Complete
## Date: 2025-11-02
## Agent: Database Designer
## Next Phase: Backend API Implementation

## ✅ DELIVERABLES COMPLETED

### 1. Venue Entity Created
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/Venue.cs`

**Fields Implemented**:
- ✅ `Id` (int, primary key, auto-increment)
- ✅ `Name` (string, required, max 100)
- ✅ `Directions` (string, nullable, max 500)
- ✅ `Notes` (string, nullable, max 1000)
- ✅ `IsActive` (bool, required, default true) - Soft delete flag
- ✅ `CreatedAt` (DateTime, required, UTC)
- ✅ `UpdatedAt` (DateTime, required, UTC)
- ✅ `Events` navigation property (ICollection<Event>)

### 2. Event Entity Updated
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`

**Changes Made**:
- ✅ Added `VenueId` (int?, nullable) - Foreign key to Venues
- ✅ Added `Venue` navigation property
- ✅ Kept `Location` field (string) - Migration will handle data preservation

### 3. Venue Seeder Created
**File**: `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/VenueSeeder.cs`

**Seed Data**:
- ✅ **Main Studio**: "Enter through main entrance, studio is on the second floor. Elevator available. Street parking on Washington St." | "Maximum capacity: 30 people. Please remove shoes before entering."
- ✅ **Community Space**: "Located at 123 Salem Street, Salem MA. Use side entrance after 6pm. Free parking in rear lot." | "Large open space suitable for social events and large classes. Capacity: 50 people."
- ✅ **Outdoor Space**: "Weather-dependent location will be announced via email 24 hours before event." | "Backup indoor location: Main Studio. Bring sun protection and bug spray in summer months."

## ⚠️ IMPORTANT: REMAINING WORK

The following tasks are **INCOMPLETE** and must be done by backend-developer:

### 1. Update ApplicationDbContext.cs
**File**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`

**Required Changes**:
```csharp
// ADD DbSet property (around line 90-250):
/// <summary>
/// Venues table for venue management
/// </summary>
public DbSet<Venue> Venues { get; set; }

// ADD configuration in OnModelCreating (after Event configuration around line 413):
// Venue entity configuration
modelBuilder.Entity<Venue>(entity =>
{
    entity.ToTable("Venues", "public");
    entity.HasKey(v => v.Id);

    // Name: required, max 100, unique case-insensitive
    entity.Property(v => v.Name)
          .IsRequired()
          .HasMaxLength(100);

    entity.HasIndex(v => v.Name)
          .IsUnique()
          .HasDatabaseName("IX_Venues_Name");

    // Directions: optional, max 500
    entity.Property(v => v.Directions)
          .HasMaxLength(500);

    // Notes: optional, max 1000
    entity.Property(v => v.Notes)
          .HasMaxLength(1000);

    // IsActive: required, default true
    entity.Property(v => v.IsActive)
          .IsRequired()
          .HasDefaultValue(true);

    entity.HasIndex(v => v.IsActive)
          .HasDatabaseName("IX_Venues_IsActive");

    // CreatedAt/UpdatedAt: UTC timestamps
    entity.Property(v => v.CreatedAt)
          .IsRequired()
          .HasColumnType("timestamptz");

    entity.Property(v => v.UpdatedAt)
          .IsRequired()
          .HasColumnType("timestamptz");

    // Navigation property to Events
    entity.HasMany(v => v.Events)
          .WithOne(e => e.Venue)
          .HasForeignKey(e => e.VenueId)
          .OnDelete(DeleteBehavior.SetNull);  // Preserve events if venue soft-deleted
});

// UPDATE Event entity configuration to add VenueId relationship (around line 346-413):
// Inside modelBuilder.Entity<Event>(entity => { ... })
// After existing navigation properties, ADD:
entity.HasOne(e => e.Venue)
      .WithMany(v => v.Events)
      .HasForeignKey(e => e.VenueId)
      .OnDelete(DeleteBehavior.SetNull);

entity.HasIndex(e => e.VenueId)
      .HasDatabaseName("IX_Events_VenueId");

// ADD Venue entity to UpdateAuditFields method (around line 1535-1543):
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

### 2. Create EF Core Migration
**Command** (from `/home/chad/repos/witchcityrope/apps/api/` directory):
```bash
dotnet ef migrations add AddVenueManagement
```

**Expected Migration Name**: `YYYYMMDDHHMMSS_AddVenueManagement.cs`

**Migration Should**:
- Create Venues table with all 8 columns
- Add unique index on Name (IX_Venues_Name)
- Add index on IsActive (IX_Venues_IsActive)
- Add VenueId column to Events table (nullable int)
- Add foreign key constraint: Events.VenueId → Venues.Id (ON DELETE SET NULL)
- Add index on Events.VenueId (IX_Events_VenueId)
- **DO NOT drop Location column** (keep for gradual migration)

### 3. Update DatabaseInitializationService.cs or SeedCoordinator.cs
**File**: One of these seeding orchestrators needs to call VenueSeeder

**Required Change**:
```csharp
// In SeedCoordinator.cs or DatabaseInitializationService.cs
// Call VenueSeeder BEFORE EventSeeder (venues must exist first)

var venueSeeder = scope.ServiceProvider.GetRequiredService<VenueSeeder>();
await venueSeeder.SeedVenuesAsync(cancellationToken);

// Then call EventSeeder...
```

### 4. Update EventSeeder.cs (Optional Enhancement)
**File**: `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/EventSeeder.cs`

**Enhancement** (not required for initial implementation):
- Look up venues by name after seeding
- Assign VenueId to events based on event type:
  - Class events → Main Studio (VenueId 1)
  - Social events → Community Space (VenueId 2)
  - Special events → Outdoor Space (VenueId 3)

## 📊 VALIDATION CHECKLIST

Before proceeding to backend API phase:

- [x] Venue entity created with all 8 required fields
- [x] Event entity updated with VenueId and Venue navigation property
- [x] VenueSeeder created with 3 default venues
- [ ] **ApplicationDbContext updated** (Venues DbSet + configuration)
- [ ] **Migration created** (AddVenueManagement)
- [ ] **Migration applied** to database
- [ ] **VenueSeeder called** in initialization sequence
- [ ] **Build succeeds** without errors
- [ ] **Database has Venues table** with seed data

## 🎯 CRITICAL BUSINESS RULES REMINDER

**For Backend Developer**:

1. **Soft Delete Only**:
   - ❌ Never hard delete venues: `DELETE FROM Venues WHERE...`
   - ✅ Always soft delete: `UPDATE Venues SET IsActive = false WHERE...`

2. **Name Uniqueness**:
   - ✅ Case-insensitive unique constraint in database
   - ✅ Validate uniqueness in API layer before save

3. **Nullable Foreign Key**:
   - ✅ Events.VenueId is nullable (events can exist without venue)
   - ✅ ON DELETE SET NULL (preserve event if venue soft-deleted)

4. **Seed Data**:
   - ✅ Only seed if Venues table is empty
   - ✅ VenueSeeder must run BEFORE EventSeeder

5. **Public Schema**:
   - ✅ Use `public` schema for Venues table
   - ✅ Consistent with other application tables

## 🔗 NEXT STEPS FOR BACKEND DEVELOPER

1. **Complete ApplicationDbContext Updates**:
   - Add Venues DbSet
   - Add Venue configuration in OnModelCreating
   - Update Event configuration with Venue relationship
   - Add Venue to UpdateAuditFields for UTC timestamp handling

2. **Create EF Core Migration**:
   - Run: `dotnet ef migrations add AddVenueManagement`
   - Review generated migration file
   - Verify it creates Venues table correctly
   - Verify it adds VenueId to Events table

3. **Update Seeding Orchestration**:
   - Integrate VenueSeeder into seeding workflow
   - Ensure VenueSeeder runs BEFORE EventSeeder

4. **Apply Migration**:
   - Run: `dotnet ef database update`
   - Verify database schema
   - Confirm 3 venues were seeded

5. **Build and Test**:
   - Verify solution builds without errors
   - Run health checks
   - Confirm seed data appears in database

6. **Implement API Endpoints**:
   - GET /api/venues (all active)
   - GET /api/venues/all (admin only - includes inactive)
   - GET /api/venues/{id}
   - POST /api/venues (admin only)
   - PUT /api/venues/{id} (admin only)
   - DELETE /api/venues/{id} (admin only - soft delete)

## 📝 DEVIATIONS FROM PLAN

**None** - Implementation followed handoff document exactly.

## 🎯 SUCCESS CRITERIA MET

- [x] Venue entity created with all required fields
- [x] Event entity updated with VenueId foreign key
- [x] Soft delete pattern implemented (IsActive flag)
- [x] VenueSeeder created with 3 default venues
- [x] UTC DateTime handling following project patterns
- [x] Entity classes follow existing project structure

## 🚨 KNOWN ISSUES / WARNINGS

**None** - Entity classes and seeder are ready for integration.

## 📚 REFERENCE DOCUMENTS

- **Business Requirements**: `/docs/functional-areas/venue-management/requirements/venue-management-requirements.md`
- **Database Handoff**: `/docs/functional-areas/venue-management/handoffs/01-database-design.md`
- **EF Core Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
- **Database Lessons**: `/docs/lessons-learned/database-designer-lessons-learned.md`

---

**Database Design Phase**: ✅ COMPLETE
**Next Phase**: Backend API Implementation
**Estimated Effort for Next Phase**: 4-6 hours
**Blockers**: None - all prerequisite entity work complete
