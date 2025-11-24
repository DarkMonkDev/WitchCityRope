# User Database Schema Investigation Report

**Date**: 2025-11-23
**Agent**: Database Designer
**Task**: Analyze current User database schema and constraints
**Context**: Support for allowing duplicate scene names while keeping email unique for login purposes

---

## Executive Summary

The current User database schema (ApplicationUser entity) has a **UNIQUE constraint on SceneName** via index `IX_Users_SceneName`. Email does **NOT have a unique constraint** - only a non-unique index for performance. This is managed through ASP.NET Core Identity's `UserManager` which enforces email uniqueness at the application layer.

---

## Current User Entity Definition

**File Path**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`

### Entity Model
```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    // Core Identity fields are inherited from IdentityUser<Guid>
    // Includes: Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    //           EmailConfirmed, PasswordHash, SecurityStamp, etc.

    /// <summary>
    /// Scene name for the rope bondage community
    /// Required field, must be unique
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? DiscordName { get; set; }
    public string? FetLifeName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public override Guid Id { get; set; } = Guid.NewGuid();

    // Additional fields for vetting, profile management, etc.
    public string EncryptedLegalName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow;
    public string Role { get; set; } = "Member";
    public bool IsActive { get; set; } = true;
    public string PronouncedName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public string? OtherNames { get; set; }

    // Security fields
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedOutUntil { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime? EmailVerificationTokenCreatedAt { get; set; }

    // Vetting fields
    public int VettingStatus { get; set; } = 0;
    public bool HasVettingApplication { get; set; } = false;

    [NotMapped]
    public bool IsVetted
    {
        get => VettingStatus == 3;
        set { /* Ignore setter - kept for backward compatibility */ }
    }

    // Legal compliance
    public bool TermsOfServiceAccepted { get; set; } = false;
    public DateTime? TermsOfServiceAcceptedAt { get; set; }
}
```

---

## Primary Key Strategy

**Primary Key**: `Id` (Guid)
**Generation**: Database-generated via PostgreSQL `gen_random_uuid()`
**Configuration**: Inherited from `IdentityUser<Guid>` base class

**Important Note**: The entity overrides `Id` with a `Guid.NewGuid()` initializer, which conflicts with EF Core best practices as documented in lessons learned. However, this is inherited from Identity framework requirements.

---

## Current Unique Constraints and Indexes

### 1. SceneName - UNIQUE INDEX ✅

**File Path**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs` (Lines 372-374)

```csharp
entity.HasIndex(e => e.SceneName)
    .IsUnique()
    .HasDatabaseName("IX_Users_SceneName");
```

**Database Schema** (from migration `20251108200319_InitialSchema.cs`):
```sql
CREATE UNIQUE INDEX "IX_Users_SceneName"
ON "public"."Users" ("SceneName");
```

**Properties**:
- Index Name: `IX_Users_SceneName`
- Column: `SceneName`
- Max Length: 50 characters
- Required: Yes
- Unique: **YES** ✅
- Schema: `public`
- Table: `Users`

**Current Behavior**: Database enforces that no two users can have the same SceneName. Attempting to insert/update a user with a duplicate SceneName will result in a PostgreSQL unique constraint violation error.

### 2. Email - NON-UNIQUE INDEX ❌

**File Path**: Not explicitly configured in ApplicationDbContext (inherited from ASP.NET Core Identity)

**Database Schema** (from migration `20251108200319_InitialSchema.cs`):
```sql
CREATE INDEX "EmailIndex"
ON "public"."Users" ("NormalizedEmail");
```

**Properties**:
- Index Name: `EmailIndex`
- Column: `NormalizedEmail` (uppercase version of email for case-insensitive lookups)
- Max Length: 256 characters (inherited from Identity)
- Required: No (inherited from Identity)
- Unique: **NO** ❌
- Schema: `public`
- Table: `Users`

**Current Behavior**:
- Database does NOT enforce email uniqueness via constraint
- ASP.NET Core Identity's `UserManager` enforces email uniqueness at the **application layer**
- The index exists purely for query performance (searching users by email)
- `UserManager.CreateAsync()` checks for duplicate emails before insert
- `UserManager.SetEmailAsync()` checks for duplicate emails before update

### 3. UserName (NormalizedUserName) - UNIQUE INDEX ✅

**Inherited from ASP.NET Core Identity**:
```sql
CREATE UNIQUE INDEX "UserNameIndex"
ON "public"."Users" ("NormalizedUserName");
```

**Properties**:
- Index Name: `UserNameIndex`
- Column: `NormalizedUserName`
- Unique: **YES** ✅
- Note: In WitchCityRope, UserName is typically set to match Email during registration

### 4. Other Indexes (Non-Unique)

**IsActive Index** (from ApplicationDbContext.cs, Line 376-377):
```csharp
entity.HasIndex(e => e.IsActive)
    .HasDatabaseName("IX_Users_IsActive");
```

**Role Index** (from ApplicationDbContext.cs, Line 379-380):
```csharp
entity.HasIndex(e => e.Role)
    .HasDatabaseName("IX_Users_Role");
```

---

## Entity Framework Configuration

**File Path**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs` (Lines 296-381)

### SceneName Configuration
```csharp
entity.Property(e => e.SceneName)
    .IsRequired()
    .HasMaxLength(50);
```

### Email Configuration (Inherited)
- Email and NormalizedEmail are configured by ASP.NET Core Identity base class
- Max length: 256 characters
- Not required (nullable)

### DateTime Configuration (Critical for PostgreSQL)
All DateTime properties use `timestamptz` column type:
```csharp
entity.Property(e => e.CreatedAt)
    .IsRequired()
    .HasColumnType("timestamptz");
```

---

## Migration History

### Initial Schema Migration: `20251108200319_InitialSchema`
- Created Users table with SceneName unique index
- Created EmailIndex (non-unique) for performance
- Established current constraint configuration

**No subsequent migrations have modified SceneName or Email constraints.**

---

## Key Findings Summary

| Field       | Max Length | Required | Unique Constraint | Index Type | Enforcement Layer        |
|-------------|------------|----------|-------------------|------------|--------------------------|
| SceneName   | 50 chars   | Yes      | **YES** ✅        | Unique     | **Database** (Postgres)  |
| Email       | 256 chars  | No       | **NO** ❌         | Non-unique | **Application** (UserManager) |
| UserName    | 256 chars  | No       | **YES** ✅        | Unique     | **Database** (Postgres)  |

---

## Implications for Allowing Duplicate Scene Names

### Current State
- **SceneName has a database-level unique constraint** via `IX_Users_SceneName`
- Removing this constraint would allow duplicate scene names
- Email uniqueness is already handled at the application layer

### Required Changes to Allow Duplicate Scene Names

1. **Drop Unique Index** (Migration Required)
   ```sql
   DROP INDEX "public"."IX_Users_SceneName";
   ```

2. **Create Non-Unique Index** (For performance)
   ```sql
   CREATE INDEX "IX_Users_SceneName"
   ON "public"."Users" ("SceneName");
   ```

3. **Update EF Core Configuration** (ApplicationDbContext.cs)
   ```csharp
   entity.HasIndex(e => e.SceneName)
       .IsUnique(false)  // Remove uniqueness
       .HasDatabaseName("IX_Users_SceneName");
   ```

4. **Update Documentation** (ApplicationUser.cs)
   - Change comment from "must be unique" to reflect new behavior
   - Document that scene names can now be duplicated
   - Clarify that login still uses unique email

5. **Application Logic Considerations**
   - User lookup by SceneName will need to handle multiple results
   - UI displaying scene names should consider disambiguation (e.g., "SceneName #1234")
   - Profile URLs may need to use UserId instead of SceneName if previously using scene names

### What Stays the Same
- **Email remains unique** via ASP.NET Core Identity's UserManager
- **UserName remains unique** via database constraint (typically set to email)
- **Login continues to use email** (already unique at application layer)

---

## Recommendations

### 1. Migration Strategy
- Create EF Core migration to drop unique constraint
- Add non-unique index for performance
- Test migration on development database first
- Document rollback procedure

### 2. Data Integrity
- Email uniqueness continues to be enforced by UserManager
- No risk to authentication system
- Scene names become display-only, not identity-critical

### 3. Application Impact Assessment
- Audit all code that queries users by SceneName
- Update any code assuming SceneName uniqueness
- Review profile URLs and navigation patterns
- Update admin UI to handle duplicate scene names gracefully

### 4. Community Communication
- Document why duplicate scene names are allowed
- Clarify that email remains unique for login
- Provide guidance on choosing scene names

---

## Technical Considerations

### PostgreSQL Specifics
- Index name must be explicitly provided for EF Core migrations
- Use `timestamptz` for all DateTime columns (already done)
- Case sensitivity: SceneName comparisons are case-sensitive by default
- Consider `CITEXT` extension for case-insensitive SceneName if needed

### ASP.NET Core Identity Integration
- UserManager.FindByEmailAsync() will continue to work (email still unique)
- UserManager.FindByNameAsync() assumes UserName is unique (unchanged)
- Custom queries by SceneName must handle multiple results after constraint removal

### Performance
- Non-unique index on SceneName maintains query performance
- Email index already exists for login performance
- No significant performance impact expected

---

## Files Referenced

1. **Entity Model**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`
2. **EF Configuration**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`
3. **Initial Migration**: `/home/chad/repos/witchcityrope/apps/api/Migrations/20251108200319_InitialSchema.cs`
4. **Model Snapshot**: `/home/chad/repos/witchcityrope/apps/api/Migrations/ApplicationDbContextModelSnapshot.cs`

---

## Standards Applied

- [Entity Framework Patterns](/home/chad/repos/witchcityrope/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [Database Migrations Guide](/home/chad/repos/witchcityrope/docs/standards-processes/backend/database-migrations-guide.md)
- PostgreSQL UTC DateTime handling (timestamptz)
- ASP.NET Core Identity conventions

---

## Next Steps

1. **Design Phase**: Create migration plan to remove SceneName unique constraint
2. **Impact Analysis**: Audit all SceneName usage in codebase
3. **Implementation**: Execute migration and update application logic
4. **Testing**: Verify duplicate scene names work as expected
5. **Documentation**: Update user-facing documentation

---

**Report Prepared By**: Database Designer Agent
**Investigation Date**: 2025-11-23
**Review Status**: Ready for Design Phase Handoff
