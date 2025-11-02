# Database Design Handoff Document

## Phase: Database Design
## Date: 2025-11-02
## Feature: Venue Management

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **Soft Delete Only**: Venues MUST use IsActive flag, never hard delete
   - ✅ Correct: `UPDATE Venues SET IsActive = false WHERE Id = 1`
   - ❌ Wrong: `DELETE FROM Venues WHERE Id = 1`
   - **Why**: Preserve event history - events reference venues

2. **Name Uniqueness**: Venue names MUST be unique (case-insensitive)
   - ✅ Correct: Database constraint + unique index on LOWER(Name)
   - ❌ Wrong: Application-only validation without database constraint
   - **Why**: Prevent duplicate venue confusion

3. **Nullable Foreign Key**: Events.VenueId MUST be nullable
   - ✅ Correct: `public int? VenueId { get; set; }`
   - ❌ Wrong: `public int VenueId { get; set; }`
   - **Why**: Allow events without assigned venue, gradual migration

4. **Seed Data on Initialize**: Create 3 default venues ONLY on fresh database
   - ✅ Correct: Check if venues exist before seeding
   - ❌ Wrong: Run seed data on every migration
   - **Why**: Avoid duplicates on subsequent migrations

5. **Public Schema**: Use public schema (NOT cms or custom schema)
   - ✅ Correct: `builder.ToTable("Venues", "public")`
   - ❌ Wrong: `builder.ToTable("Venues", "venues")` or custom schema
   - **Why**: Consistency with other application tables

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Business Requirements | `/docs/functional-areas/venue-management/requirements/venue-management-requirements.md` | FR-1 (Data Model), FR-2 (Seed Data) |
| Database Migrations Guide | `/docs/standards-processes/backend/database-migrations-guide.md` | Migration creation, seeding patterns |
| Vertical Slice Guide | `/docs/standards-processes/backend/vertical-slice-implementation-guide.md` | Entity configuration patterns |

## 🚨 KNOWN PITFALLS

1. **Hard Delete Temptation**: Forgetting soft delete and using DELETE
   - **Why it happens**: Normal CRUD assumption
   - **How to avoid**: Always use IsActive flag, never DbContext.Remove()

2. **Case-Sensitive Uniqueness**: Using simple unique constraint on Name
   - **Why it happens**: Database default is case-sensitive
   - **How to avoid**: Use unique index on LOWER(Name) or computed column

3. **Running Seeds Multiple Times**: Seed data runs on every migration
   - **Why it happens**: Not checking for existing data
   - **How to avoid**: `if (!context.Venues.Any()) { /* seed */ }`

4. **Wrong Schema**: Using custom schema instead of public
   - **Why it happens**: Misunderstanding schema organization
   - **How to avoid**: Review recent CMS schema consolidation (Oct 28, 2025)

## ✅ VALIDATION CHECKLIST

Before proceeding to backend API phase, verify:

- [ ] Venue entity has all 8 required fields (Id, Name, Directions, Notes, IsActive, CreatedAt, UpdatedAt, navigation properties)
- [ ] IsActive defaults to true
- [ ] Name has unique constraint (case-insensitive)
- [ ] VenueId in Events table is nullable (int?)
- [ ] 3 seed venues created with specific names/details
- [ ] Seed data checks for existing venues before creating
- [ ] Migration file uses public schema
- [ ] Foreign key relationship to Events configured
- [ ] Indexes created (Name, IsActive)
- [ ] Migration builds without errors

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing Events Table**: Events table already exists
   - **Impact**: Need to add VenueId column via migration
   - **Required Changes**: Add nullable VenueId, create foreign key, add index

2. **Schema Consolidation**: All app tables now use public schema (Oct 28, 2025)
   - **Impact**: Must use public schema, not custom venue schema
   - **Required Changes**: Explicitly set schema in entity configuration

3. **EF Core Patterns**: Project uses explicit configuration classes
   - **Impact**: Create VenueConfiguration class (not fluent API in OnModelCreating)
   - **Required Changes**: Follow existing patterns in `/apps/api/Features/*/Configuration/`

## 📊 DATA MODEL DECISIONS

### Venue Entity
```csharp
public class Venue
{
    public int Id { get; set; }                    // PK, auto-increment
    public string Name { get; set; } = string.Empty; // Required, max 200, unique
    public string? Directions { get; set; }        // Optional, max 2000
    public string? Notes { get; set; }             // Optional, max 2000
    public bool IsActive { get; set; } = true;     // Soft delete flag
    public DateTime CreatedAt { get; set; }        // Auto-set on create
    public DateTime UpdatedAt { get; set; }        // Auto-set on update

    // Navigation property
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
```

### Event Entity Changes
```csharp
// Add to existing Event entity
public int? VenueId { get; set; }           // Nullable FK
public Venue? Venue { get; set; }           // Navigation property
```

### Database Indexes
```csharp
// Unique index on Name (case-insensitive)
builder.HasIndex(v => v.Name)
    .IsUnique()
    .HasDatabaseName("IX_Venues_Name_Unique");

// Performance index on IsActive
builder.HasIndex(v => v.IsActive)
    .HasDatabaseName("IX_Venues_IsActive");
```

### Foreign Key Relationship
```csharp
// In Event configuration
builder.HasOne(e => e.Venue)
    .WithMany(v => v.Events)
    .HasForeignKey(e => e.VenueId)
    .OnDelete(DeleteBehavior.SetNull); // Preserve event if venue deleted
```

## 🎯 SUCCESS CRITERIA

### Test Case 1: Create Venue Entity
- **Input**: Run `dotnet ef migrations add AddVenueManagement`
- **Expected Output**: Migration file created with Venues table, all fields, constraints, indexes

### Test Case 2: Seed Default Venues
- **Input**: Fresh database, run migrations
- **Expected Output**: 3 venues in database (Main Studio, Community Space, Outdoor Space)

### Test Case 3: Name Uniqueness
- **Input**: Attempt to create two venues with same name (different casing)
- **Expected Output**: Database constraint violation error

### Test Case 4: Soft Delete Preservation
- **Input**: Set venue IsActive=false, query Events referencing that venue
- **Expected Output**: Events still have VenueId, can retrieve venue info even though inactive

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT hard delete venues (use IsActive flag only)
- ❌ DO NOT create custom venues schema (use public schema)
- ❌ DO NOT make VenueId required on Events (must be nullable)
- ❌ DO NOT skip seed data existence check (prevents duplicates)
- ❌ DO NOT use case-sensitive Name uniqueness (use LOWER() or computed column)
- ❌ DO NOT add complex venue features (categories, capacity fields, etc.)
- ❌ DO NOT create venue history tracking (UpdatedAt timestamp is sufficient)

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Soft Delete | Setting IsActive=false instead of removing record | `UPDATE Venues SET IsActive = false WHERE Id = 1` |
| Seed Data | Default data created during database initialization | 3 default venues (Main Studio, Community Space, Outdoor Space) |
| Nullable Foreign Key | Optional relationship (can be null) | `public int? VenueId { get; set; }` allows events without venue |
| Case-Insensitive Unique | Constraint preventing duplicate names regardless of capitalization | "Main Studio" = "main studio" = "MAIN STUDIO" |
| Public Schema | Standard PostgreSQL schema for application tables | `public.Venues` not `venues.Venues` |

## 🔗 NEXT AGENT INSTRUCTIONS

**Next Agent**: Backend API Developer

1. **FIRST**: Read business requirements document (FR-1 Data Model, FR-5 API Endpoints)
2. **SECOND**: Review this handoff document for critical rules
3. **THIRD**: Review existing vertical slice implementations in `/apps/api/Features/`
4. **FOURTH**: Review database migrations guide for migration patterns
5. **THEN**: Begin implementation following constraints

### Specific Tasks for Backend API Phase
1. Create Venue entity class
2. Create VenueConfiguration class
3. Add VenueId to Event entity
4. Create migration (AddVenueManagement)
5. Create seed data in DatabaseInitializationService
6. Implement 6 API endpoints (GET, POST, PUT, DELETE)
7. Create VenueDto, CreateVenueRequest, UpdateVenueRequest
8. Add authorization (Admin-only for CUD operations)
9. Write unit tests for VenueService

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-11-02
**Key Finding**: Clean implementation without complex migration concerns - use soft delete to preserve event history

**Next Agent Should Be**: Backend API Developer
**Next Phase**: Backend API Implementation
**Estimated Effort**: 4-6 hours

---

## Implementation Notes

### Migration File Structure
```csharp
public partial class AddVenueManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Create Venues table
        migrationBuilder.CreateTable(
            name: "Venues",
            schema: "public",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Directions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Venues", x => x.Id);
            });

        // 2. Create unique index on Name
        migrationBuilder.CreateIndex(
            name: "IX_Venues_Name_Unique",
            schema: "public",
            table: "Venues",
            column: "Name",
            unique: true);

        // 3. Create index on IsActive
        migrationBuilder.CreateIndex(
            name: "IX_Venues_IsActive",
            schema: "public",
            table: "Venues",
            column: "IsActive");

        // 4. Add VenueId to Events table
        migrationBuilder.AddColumn<int>(
            name: "VenueId",
            schema: "public",
            table: "Events",
            type: "integer",
            nullable: true);

        // 5. Create foreign key
        migrationBuilder.CreateIndex(
            name: "IX_Events_VenueId",
            schema: "public",
            table: "Events",
            column: "VenueId");

        migrationBuilder.AddForeignKey(
            name: "FK_Events_Venues_VenueId",
            schema: "public",
            table: "Events",
            column: "VenueId",
            principalSchema: "public",
            principalTable: "Venues",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse all operations
        migrationBuilder.DropForeignKey(
            name: "FK_Events_Venues_VenueId",
            schema: "public",
            table: "Events");

        migrationBuilder.DropIndex(
            name: "IX_Events_VenueId",
            schema: "public",
            table: "Events");

        migrationBuilder.DropColumn(
            name: "VenueId",
            schema: "public",
            table: "Events");

        migrationBuilder.DropTable(
            name: "Venues",
            schema: "public");
    }
}
```

### Seed Data Pattern
```csharp
// In DatabaseInitializationService.cs
private async Task SeedVenuesAsync()
{
    if (await _context.Venues.AnyAsync())
    {
        _logger.LogInformation("Venues already exist, skipping seed data");
        return;
    }

    var venues = new List<Venue>
    {
        new Venue
        {
            Name = "Main Studio",
            Directions = "Enter through main entrance, studio is on second floor",
            Notes = "Capacity: 30 people. Parking available in adjacent lot.",
            IsActive = true
        },
        new Venue
        {
            Name = "Community Space",
            Directions = "Community center basement, enter through rear entrance",
            Notes = "Capacity: 50 people. Street parking only.",
            IsActive = true
        },
        new Venue
        {
            Name = "Outdoor Space",
            Directions = "Park pavilion in southwest corner",
            Notes = "Weather-dependent. Backup location: Main Studio",
            IsActive = true
        }
    };

    await _context.Venues.AddRangeAsync(venues);
    await _context.SaveChangesAsync();
    _logger.LogInformation("Created {Count} seed venues", venues.Count);
}
```

This handoff document provides the backend developer with all critical information needed to implement the database layer correctly.
