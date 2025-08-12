# Admin Members Management - Database Changes

## Overview
This document details the database changes required for the Admin Members Management feature, specifically the creation of a general-purpose notes system that extends beyond vetting-specific notes.

## Current State Analysis

### Existing Notes Implementation
Currently, notes are stored as properties on existing entities:
1. **VettingApplication.DecisionNotes**: Final decision notes (varchar 1000)
2. **VettingReview.Notes**: Individual reviewer notes (varchar 1000)

These notes are tightly coupled to the vetting process and cannot be used for general member management.

## Proposed Changes

### 1. New UserNotes Table
Create a new general-purpose notes table that can store various types of notes about members.

```sql
CREATE TABLE UserNotes (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL,
    NoteType VARCHAR(50) NOT NULL DEFAULT 'General',
    Content TEXT NOT NULL,
    CreatedById UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    DeletedAt TIMESTAMP NULL,
    DeletedById UUID NULL,
    
    CONSTRAINT FK_UserNotes_User FOREIGN KEY (UserId) 
        REFERENCES auth."Users" (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserNotes_CreatedBy FOREIGN KEY (CreatedById) 
        REFERENCES auth."Users" (Id),
    CONSTRAINT FK_UserNotes_DeletedBy FOREIGN KEY (DeletedById) 
        REFERENCES auth."Users" (Id)
);

-- Indexes for performance
CREATE INDEX IX_UserNotes_UserId ON UserNotes(UserId) WHERE NOT IsDeleted;
CREATE INDEX IX_UserNotes_NoteType ON UserNotes(NoteType) WHERE NOT IsDeleted;
CREATE INDEX IX_UserNotes_CreatedAt ON UserNotes(CreatedAt DESC) WHERE NOT IsDeleted;
CREATE INDEX IX_UserNotes_CreatedById ON UserNotes(CreatedById) WHERE NOT IsDeleted;
```

### 2. Note Types Enumeration
Define standard note types for categorization:

```csharp
public enum NoteType
{
    General = 0,
    Vetting = 1,
    Incident = 2,
    Event = 3,
    Administrative = 4,
    Safety = 5,
    Community = 6
}
```

### 3. Entity Model

```csharp
namespace WitchCityRope.Core.Entities;

public class UserNote
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NoteType NoteType { get; private set; }
    public string Content { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    // Navigation properties
    public WitchCityRopeUser User { get; private set; }
    public WitchCityRopeUser CreatedBy { get; private set; }
    public WitchCityRopeUser? DeletedBy { get; private set; }

    // Factory method
    public static UserNote Create(
        Guid userId, 
        NoteType noteType, 
        string content, 
        Guid createdById)
    {
        return new UserNote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NoteType = noteType,
            Content = content,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    // Domain methods
    public void Update(string newContent)
    {
        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(Guid deletedById)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedById = deletedById;
    }
}
```

### 4. Data Migration Strategy

#### Step 1: Create New Table
Create the UserNotes table with the schema defined above.

#### Step 2: Migrate Existing Vetting Notes
```sql
-- Migrate vetting decision notes
INSERT INTO UserNotes (UserId, NoteType, Content, CreatedById, CreatedAt, UpdatedAt)
SELECT 
    va.ApplicantId,
    'Vetting',
    'Vetting Decision: ' || va.DecisionNotes,
    COALESCE(
        (SELECT vr.ReviewerId FROM VettingReviews vr 
         WHERE vr.ApplicationId = va.Id 
         ORDER BY vr.ReviewedAt DESC LIMIT 1),
        va.ApplicantId -- Fallback to applicant if no reviewer found
    ),
    COALESCE(va.ReviewedAt, va.UpdatedAt),
    COALESCE(va.ReviewedAt, va.UpdatedAt)
FROM VettingApplications va
WHERE va.DecisionNotes IS NOT NULL AND va.DecisionNotes != '';

-- Migrate individual vetting review notes
INSERT INTO UserNotes (UserId, NoteType, Content, CreatedById, CreatedAt, UpdatedAt)
SELECT 
    va.ApplicantId,
    'Vetting',
    'Vetting Review: ' || vr.Notes,
    vr.ReviewerId,
    vr.ReviewedAt,
    vr.ReviewedAt
FROM VettingReviews vr
INNER JOIN VettingApplications va ON vr.ApplicationId = va.Id
WHERE vr.Notes IS NOT NULL AND vr.Notes != '';
```

#### Step 3: Update Foreign Keys
No changes needed to existing tables - the notes remain in their original locations for backward compatibility.

### 5. Repository Implementation

```csharp
public interface IUserNoteRepository
{
    Task<IEnumerable<UserNote>> GetUserNotesAsync(
        Guid userId, 
        bool includeDeleted = false);
    
    Task<UserNote?> GetNoteByIdAsync(Guid noteId);
    
    Task<UserNote> AddNoteAsync(UserNote note);
    
    Task UpdateNoteAsync(UserNote note);
    
    Task DeleteNoteAsync(Guid noteId, Guid deletedById);
    
    Task<int> GetNoteCountAsync(Guid userId, NoteType? noteType = null);
}
```

### 6. API DTOs

```csharp
public class UserNoteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string NoteType { get; set; }
    public string Content { get; set; }
    public string CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class CreateUserNoteDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public NoteType NoteType { get; set; }
    
    [Required]
    [StringLength(5000)]
    public string Content { get; set; }
}

public class UpdateUserNoteDto
{
    [Required]
    [StringLength(5000)]
    public string Content { get; set; }
}
```

### 7. Database Configuration

```csharp
public class UserNoteConfiguration : IEntityTypeConfiguration<UserNote>
{
    public void Configure(EntityTypeBuilder<UserNote> builder)
    {
        builder.ToTable("UserNotes");
        
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.NoteType)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(n => n.Content)
            .IsRequired();
            
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(n => n.CreatedBy)
            .WithMany()
            .HasForeignKey(n => n.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(n => n.DeletedBy)
            .WithMany()
            .HasForeignKey(n => n.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasQueryFilter(n => !n.IsDeleted);
    }
}
```

### 8. Migration File

```csharp
public partial class AddUserNotesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                NoteType = table.Column<string>(maxLength: 50, nullable: false),
                Content = table.Column<string>(nullable: false),
                CreatedById = table.Column<Guid>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false),
                IsDeleted = table.Column<bool>(nullable: false),
                DeletedAt = table.Column<DateTime>(nullable: true),
                DeletedById = table.Column<Guid>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserNotes_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserNotes_Users_CreatedById",
                    column: x => x.CreatedById,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserNotes_Users_DeletedById",
                    column: x => x.DeletedById,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserNotes_UserId",
            table: "UserNotes",
            column: "UserId");
            
        migrationBuilder.CreateIndex(
            name: "IX_UserNotes_CreatedById",
            table: "UserNotes",
            column: "CreatedById");
            
        migrationBuilder.CreateIndex(
            name: "IX_UserNotes_DeletedById",
            table: "UserNotes",
            column: "DeletedById");
            
        migrationBuilder.CreateIndex(
            name: "IX_UserNotes_CreatedAt",
            table: "UserNotes",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserNotes");
    }
}
```

## Performance Considerations

1. **Indexes**: Created on UserId, NoteType, CreatedAt, and CreatedById for query optimization
2. **Soft Delete**: Uses IsDeleted flag with query filters to maintain data integrity
3. **Pagination**: Notes should be loaded with pagination (20-50 per page)
4. **Caching**: Consider caching note counts for performance

## Security Considerations

1. **Access Control**: Only administrators can view/add notes
2. **Audit Trail**: All changes tracked with timestamps and user IDs
3. **Soft Delete**: Notes are never permanently deleted, maintaining audit history
4. **Content Validation**: 5000 character limit prevents abuse
5. **XSS Prevention**: Content should be sanitized before display

## Rollback Strategy

If issues arise, the migration can be rolled back:
1. The new UserNotes table can be dropped without affecting existing data
2. Original vetting notes remain in their original locations
3. No breaking changes to existing functionality

## Testing Requirements

1. **Unit Tests**: Test UserNote entity creation and methods
2. **Integration Tests**: Test repository methods and database operations
3. **Migration Tests**: Verify data migration accuracy
4. **Performance Tests**: Ensure queries perform well with 10,000+ notes