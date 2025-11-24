# Venue Location Privacy - Database Schema Design

## Feature Overview
**Feature**: Venue Location Privacy Enhancement
**Work Type**: Feature Enhancement
**Date**: 2025-11-23
**Database Designer**: database-designer agent

## Purpose
Add a `Location` field to the Venue entity to store general location information (e.g., "Salem, MA") that can be displayed to non-vetted users, while keeping detailed venue information (Name, Directions, Notes) restricted to vetted members only.

---

## Current Venue Entity Structure

### Existing Schema
The Venue entity currently has the following fields:

```csharp
public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Max 100, required, unique
    public string? Directions { get; set; }            // Max 500, nullable
    public string? Notes { get; set; }                 // Max 1000, nullable
    public bool IsActive { get; set; } = true;         // Required, default true
    public DateTime CreatedAt { get; set; }            // UTC timestamp
    public DateTime UpdatedAt { get; set; }            // UTC timestamp
    public ICollection<Event> Events { get; set; }     // Navigation property
}
```

### Existing Entity Framework Configuration
Location: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs` (lines 456-500)

Key configuration elements:
- Table: "Venues" in "public" schema
- Primary Key: `Id` (auto-increment integer)
- Unique Index: `IX_Venues_Name` (case-insensitive)
- Index: `IX_Venues_IsActive`
- Soft delete pattern via `IsActive` flag
- Relationship: One-to-many with Events (DeleteBehavior.SetNull)

---

## New Field Specification

### Field Details
- **Property Name**: `Location`
- **Database Column**: `Location`
- **Data Type**: `string` (PostgreSQL: `VARCHAR(100)`)
- **Nullable**: Yes (for backward compatibility)
- **Max Length**: 100 characters
- **Default Value**: NULL
- **Purpose**: Store city and state for display to non-vetted users (e.g., "Salem, MA")

### Business Rules
1. **Optional Field**: Existing venues without Location data will have NULL values
2. **No Validation**: No database-level constraints on format (e.g., "City, State")
3. **Public Display**: This field is safe to display to all users (no PII)
4. **UTF-8 Support**: Support international characters for global venue locations

---

## Database Schema Changes

### Entity Model Update
Add the following property to the Venue entity:

```csharp
// Location: /home/chad/repos/witchcityrope/apps/api/Models/Venue.cs

/// <summary>
/// General location information (city, state) for public display
/// Max length: 100 characters
/// Example: "Salem, MA"
/// </summary>
[MaxLength(100)]
public string? Location { get; set; }
```

### Entity Framework Configuration Update
Update the Venue configuration in ApplicationDbContext.cs:

```csharp
// Location: /home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs
// Inside: modelBuilder.Entity<Venue>(entity => { ... })

// Add after Notes configuration (around line 476):
entity.Property(v => v.Location)
      .HasMaxLength(100);
```

**Rationale**:
- Nullable field requires no `.IsRequired()` call
- No default value specified (defaults to NULL)
- Max length constraint enforced at both model and database levels

---

## Migration Strategy

### Migration Steps

#### 1. Add Migration
```bash
# From /apps/api directory
dotnet ef migrations add AddLocationToVenue
```

#### 2. Generated Migration Content
Expected migration file structure:

```csharp
public partial class AddLocationToVenue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Location",
            table: "Venues",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Location",
            table: "Venues");
    }
}
```

#### 3. Migration Execution
```bash
# Apply migration to database
dotnet ef database update
```

### Backward Compatibility

**Existing Venues Handling**:
- All existing venue records will have `Location = NULL` after migration
- Application must handle NULL values gracefully
- Admin UI should provide interface to populate Location for existing venues

**No Breaking Changes**:
- Adding a nullable column is a non-breaking change
- No data migration required
- Existing queries and code continue to work

---

## PostgreSQL Considerations

### Column Type
- **PostgreSQL Type**: `VARCHAR(100)`
- **Entity Framework Mapping**: Automatically handled by EF Core PostgreSQL provider (Npgsql)

### Character Encoding
- **UTF-8 Support**: PostgreSQL default UTF-8 encoding supports international characters
- **Collation**: Uses default case-sensitive collation (appropriate for location names)

### Performance Impact
- **Minimal**: Adding a nullable column to existing table is fast (no data rewrites)
- **Index**: No index needed (not used in WHERE clauses for filtering)

---

## DTO Updates Required

The following DTOs will need to be updated (handled by backend-developer):

### VenueDto
```csharp
// Location: /home/chad/repos/witchcityrope/apps/api/DTOs/VenueDto.cs

public class VenueDto
{
    // ... existing properties ...

    /// <summary>
    /// General location information (city, state)
    /// Safe for public display to all users
    /// </summary>
    public string? Location { get; set; }
}
```

### CreateVenueRequest
```csharp
public class CreateVenueRequest
{
    // ... existing properties ...

    /// <summary>
    /// General location (optional, max 100 characters)
    /// Example: "Salem, MA"
    /// </summary>
    public string? Location { get; set; }
}
```

### UpdateVenueRequest
```csharp
public class UpdateVenueRequest
{
    // ... existing properties ...

    /// <summary>
    /// General location (optional, max 100 characters)
    /// </summary>
    public string? Location { get; set; }
}
```

---

## Data Integrity

### Constraints
- **Max Length**: 100 characters enforced by EF Core and PostgreSQL
- **Nullable**: NULL values permitted
- **No Check Constraints**: No format validation at database level

### Foreign Key Impact
- **No Changes**: Location field does not affect existing Event → Venue relationship

### Soft Delete Compatibility
- **Preserved**: Location field follows same soft delete pattern as other Venue fields
- **IsActive**: When venue is soft-deleted, Location remains accessible for historical event data

---

## Testing Considerations

### Test Scenarios
1. **Create Venue with Location**: Verify Location saves correctly
2. **Create Venue without Location**: Verify NULL is accepted
3. **Update Venue Location**: Verify field can be updated
4. **Clear Venue Location**: Verify field can be set to NULL
5. **Max Length Validation**: Verify 100-character limit enforced
6. **UTF-8 Characters**: Verify international characters (e.g., "São Paulo, Brazil")

### Migration Testing
1. **Apply Migration**: Verify migration applies without errors
2. **Existing Data**: Verify existing venues have NULL Location
3. **Rollback**: Verify Down() migration removes column cleanly

---

## Security Considerations

### Privacy Compliance
- **Public Data**: Location field contains NO personally identifiable information
- **Access Control**: Application-layer authorization controls who sees detailed venue info
- **Database Level**: No encryption required (public information)

### Data Sanitization
- **Input Validation**: Application layer should sanitize input
- **XSS Protection**: Frontend must escape HTML when displaying Location
- **SQL Injection**: EF Core parameterized queries prevent injection

---

## Performance Metrics

### Migration Performance
- **Estimated Time**: < 1 second (adding nullable column)
- **Table Locks**: Minimal (PostgreSQL ADD COLUMN is fast)
- **Downtime**: None required

### Query Performance
- **Impact**: None (field not indexed, not used in WHERE clauses)
- **Storage Overhead**: ~100 bytes per venue record (negligible)

---

## Rollback Plan

### Rollback Migration
```bash
# Revert to previous migration
dotnet ef database update <PreviousMigrationName>

# Or remove migration entirely
dotnet ef migrations remove
```

### Data Loss
- **Rollback Impact**: Location data will be LOST if migration is rolled back
- **Mitigation**: Export Location data before rollback if needed
- **Risk**: LOW (Location data can be easily re-entered via admin UI)

---

## Future Considerations

### Potential Enhancements
1. **Geocoding**: Store latitude/longitude for mapping features
2. **Structured Data**: Separate City, State, Country fields for filtering
3. **Localization**: Support multiple languages for location names
4. **Validation**: Add format validation (regex) for "City, State" pattern

### Schema Evolution
- Current design allows easy extension without breaking changes
- Additional location-related fields can be added as nullable columns
- Consider creating separate LocationInfo value object if complexity grows

---

## Standards Compliance

### Entity Framework Patterns
✅ **UTC DateTime Handling**: Not applicable (no DateTime fields added)
✅ **Nullable Fields**: Properly configured as nullable string
✅ **Max Length Constraints**: Applied at both model and EF configuration levels
✅ **PostgreSQL Compatibility**: VARCHAR type automatically mapped by Npgsql

### Database Design Principles
✅ **Data Integrity**: Max length constraint enforced
✅ **Backward Compatibility**: Nullable field preserves existing data
✅ **Soft Delete Pattern**: Consistent with existing Venue design
✅ **Performance**: No indexes on non-filtering fields

---

## Success Criteria

- [ ] Venue entity updated with Location property
- [ ] EF Core configuration updated with proper constraints
- [ ] Migration generated successfully
- [ ] Migration applied without errors
- [ ] Existing venues have NULL Location (verified)
- [ ] New venues can be created with Location
- [ ] Location can be updated/cleared
- [ ] Max length validation enforced
- [ ] DTOs updated (backend-developer responsibility)
- [ ] Tests pass (test-developer responsibility)

---

## References

### Standards Documents
- [Entity Framework Patterns](/home/chad/repos/witchcityrope/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [Database Designer Lessons Learned](/home/chad/repos/witchcityrope/docs/lessons-learned/database-designer-lessons-learned.md)

### Related Code Files
- Entity: `/home/chad/repos/witchcityrope/apps/api/Models/Venue.cs`
- DbContext: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`
- DTOs: `/home/chad/repos/witchcityrope/apps/api/DTOs/VenueDto.cs`

### Database Migration Guide
- [Database Migrations Guide](/home/chad/repos/witchcityrope/docs/standards-processes/backend/database-migrations-guide.md)

---

**Document Status**: Complete
**Next Phase**: Implementation (backend-developer)
**Estimated Implementation Time**: 30 minutes
