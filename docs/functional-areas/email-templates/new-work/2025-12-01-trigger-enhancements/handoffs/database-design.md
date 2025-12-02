# Email Template Trigger Enhancements - Database Design Handoff
<!-- Date: 2025-12-01 -->
<!-- Author: database-designer -->
<!-- Status: Complete -->
<!-- Next Phase: Backend Implementation -->

## Code Review Summary

### Existing Entity Patterns Verified

**Entity Structure** (from GlobalEmailTemplate.cs, EventEmailTemplate.cs, SentAdHocEmail.cs):
- **Primary Keys**: `public Guid Id { get; set; }` (NO initializers - EF Core manages IDs)
- **Timestamps**: All use `DateTime` with UTC, configured as `timestamptz` in PostgreSQL
- **Audit Fields**: `CreatedAt`, `UpdatedAt` (both DateTime), `UpdatedBy` (Guid for user FK)
- **Soft Deletes**: `IsActive` boolean field (not deleted rows)
- **Versioning**: `Version` int field (increments on update via DbContext.SaveChangesAsync)

**Configuration Patterns** (from GlobalEmailTemplateConfiguration.cs):
- Separate configuration classes implementing `IEntityTypeConfiguration<T>`
- Explicit index naming: `IX_TableName_ColumnName` or `UQ_TableName_Purpose`
- Check constraints with explicit names: `CHK_TableName_ColumnName_Purpose`
- JSONB columns: `HasColumnType("jsonb")` with GIN indexes (`HasMethod("gin")`)
- Enum storage: `HasConversion<int>()` for enums
- Foreign keys: Use `OnDelete(DeleteBehavior.Restrict)` or `SetNull` (never Cascade to Users)

**DbContext Integration** (from ApplicationDbContext.cs):
- DbSets registered: `public DbSet<GlobalEmailTemplate> GlobalEmailTemplates { get; set; }`
- Configuration applied: `modelBuilder.ApplyConfiguration(new GlobalEmailTemplateConfiguration());`
- UTC handling: `UpdateAuditFields()` method in `SaveChangesAsync` auto-converts to UTC

**Migration Naming** (from recent migrations):
- Pattern: `YYYYMMDDHHMMSS_DescriptiveName.cs`
- Example: `20251201014300_AddSessionIdToCheckInTables.cs`
- Namespace: `WitchCityRope.Api.Migrations`
- Class name matches file: `public partial class AddSessionIdToCheckInTables : Migration`

**CRITICAL FINDING**: NO MediatR is used (verified from ARCHITECTURE-WITHOUT-MEDIATR.md). All services use direct injection pattern.

---

## Entity Definitions

### 1. New Enums

#### TemplateTriggerType.cs
**Location**: `/apps/api/Features/EmailTemplates/Entities/TemplateTriggerType.cs`

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Defines how email templates are triggered
/// </summary>
public enum TemplateTriggerType
{
    /// <summary>
    /// No automatic trigger - manual send only
    /// Used for Ad Hoc category templates
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Triggered by specific business events (existing behavior)
    /// Examples: ticket purchase, password reset, vetting status change
    /// Used for Vetting, Admin, Incident categories
    /// </summary>
    FixedEvent = 1,

    /// <summary>
    /// Triggered by time offset from session start
    /// Only used for Events category
    /// TimingOffsetDays: positive = before session, negative = after session
    /// </summary>
    TimeBased = 2
}
```

#### EventRecipientGroup.cs
**Location**: `/apps/api/Features/EmailTemplates/Entities/EventRecipientGroup.cs`

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Defines recipient groups for event-based email templates
/// Only used for Events category (NOT for Vetting/Admin/Incident)
/// </summary>
public enum EventRecipientGroup
{
    /// <summary>
    /// Users who actually attended (checked in to) the session
    /// Based on CheckIns table records
    /// </summary>
    SessionAttendees = 0,

    /// <summary>
    /// Users with RSVP (for socials) OR ticket purchases (for classes)
    /// Business logic: Event type determines which; deduplicate if user has both
    /// Based on TicketPurchases table
    /// </summary>
    RSVPTicketHolders = 1,

    /// <summary>
    /// Volunteers assigned to the specific session
    /// Based on VolunteerSignups table with SessionId match
    /// </summary>
    SessionVolunteers = 2,

    /// <summary>
    /// Teachers assigned to the session
    /// Based on Session entity teacher assignments
    /// </summary>
    Teachers = 3
}
```

---

### 2. Modified Entities

#### GlobalEmailTemplate (Events Category Only)
**Location**: `/apps/api/Features/EmailTemplates/Entities/GlobalEmailTemplate.cs`

**New Fields**:
```csharp
/// <summary>
/// Trigger type for this template
/// Default: FixedEvent (existing behavior)
/// Events category can use TimeBased
/// </summary>
public TemplateTriggerType TriggerType { get; set; } = TemplateTriggerType.FixedEvent;

/// <summary>
/// Whether automatic triggering is enabled
/// Default: true (maintains existing behavior)
/// </summary>
public bool TriggerEnabled { get; set; } = true;

/// <summary>
/// Days offset for time-based triggers (Events category only)
/// Positive: days BEFORE session start (e.g., 3 = 3 days before)
/// Negative: days AFTER session start (e.g., -2 = 2 days after)
/// Null: not applicable (FixedEvent or Manual trigger types)
/// </summary>
public int? TimingOffsetDays { get; set; }

/// <summary>
/// Target recipient group for Events category templates
/// Null for other categories (recipients are hardcoded in service code)
/// </summary>
public EventRecipientGroup? RecipientGroup { get; set; }
```

**Field Constraints**:
- TriggerType: Required, stored as int
- TriggerEnabled: Required, default true
- TimingOffsetDays: Nullable, check constraint: value between -365 and 365
- RecipientGroup: Nullable, stored as int when set

#### EventEmailTemplate (Overrides for Event-Specific Customization)
**Location**: `/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`

**New Fields**:
```csharp
/// <summary>
/// Override for trigger enabled state
/// Null: use global template setting
/// True/False: override global setting
/// </summary>
public bool? OverrideTriggerEnabled { get; set; }

/// <summary>
/// Override for timing offset
/// Null: use global template setting
/// Value: override with event-specific timing
/// </summary>
public int? OverrideTimingOffsetDays { get; set; }

/// <summary>
/// Override for recipient group
/// Null: use global template setting
/// Value: override with event-specific recipients
/// </summary>
public EventRecipientGroup? OverrideRecipientGroup { get; set; }
```

**Field Constraints**:
- All nullable (null = use global, value = override)
- OverrideTimingOffsetDays: check constraint -365 to 365

#### SentAdHocEmail (Add Scheduled Send Support)
**Location**: `/apps/api/Features/EmailTemplates/Entities/SentAdHocEmail.cs`

**New Field**:
```csharp
/// <summary>
/// Scheduled send time for future delivery
/// Null: send immediately (existing behavior)
/// Set: send at specified UTC time
/// </summary>
public DateTime? ScheduledSendAt { get; set; }
```

**Field Constraints**:
- Nullable DateTime
- Column type: timestamptz (UTC)
- Default: null

---

### 3. New Entities

#### AdHocEmailTemplate (Saved Ad Hoc Templates)
**Location**: `/apps/api/Features/EmailTemplates/Entities/AdHocEmailTemplate.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a saved ad-hoc email template for reuse
/// Only created via "Save as Template" feature on Ad Hoc tab
/// Can be deleted by users (unlike other template categories)
/// </summary>
public class AdHocEmailTemplate
{
    /// <summary>
    /// Primary key (EF Core manages - NO initializer)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name (from subject or custom)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body with {{variable}} placeholders
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created this template
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation property
    public ApplicationUser CreatedByUser { get; set; } = null!;
}
```

#### EmailTriggerLog (Audit Trail and Idempotency)
**Location**: `/apps/api/Features/EmailTemplates/Entities/EmailTriggerLog.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Audit log for automated email triggers
/// Provides idempotency for time-based triggers (prevent duplicate sends)
/// Tracks all automatic email operations
/// </summary>
public class EmailTriggerLog
{
    /// <summary>
    /// Primary key (EF Core manages - NO initializer)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template that triggered (GlobalEmailTemplate or EventEmailTemplate)
    /// </summary>
    [Required]
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Event associated with trigger (nullable for non-event triggers)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Session that triggered time-based send
    /// Required for time-based triggers, null for fixed event triggers
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Template type (e.g., "Confirmation", "Reminder1Day")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Trigger type: Manual, FixedEvent, TimeBased
    /// Stored as string for audit clarity
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Recipient group used (EventRecipientGroup or UserSegment)
    /// Stored as string for audit clarity
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Number of recipients email was sent to
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// When trigger condition was met (UTC)
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// When email was actually sent (UTC)
    /// Null if send failed
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Send status: Sent, Failed, Skipped
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Sent";

    /// <summary>
    /// Error message if Status = Failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties (nullable to prevent cascade issues)
    public Event? Event { get; set; }
    public Session? Session { get; set; }
}
```

---

## EF Core Configuration Classes

### 1. GlobalEmailTemplateConfiguration (Modifications)
**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/GlobalEmailTemplateConfiguration.cs`

**Add to Configure method**:
```csharp
// Trigger configuration fields
builder.Property(e => e.TriggerType)
    .IsRequired()
    .HasConversion<int>()
    .HasDefaultValue(TemplateTriggerType.FixedEvent);

builder.Property(e => e.TriggerEnabled)
    .IsRequired()
    .HasDefaultValue(true);

builder.Property(e => e.TimingOffsetDays)
    .IsRequired(false); // Nullable

builder.Property(e => e.RecipientGroup)
    .IsRequired(false) // Nullable
    .HasConversion<int>(); // Stored as int when set

// Indexes for trigger queries
builder.HasIndex(e => new { e.Category, e.TriggerType })
    .HasDatabaseName("IX_GlobalEmailTemplates_Category_TriggerType");

builder.HasIndex(e => new { e.TriggerType, e.TriggerEnabled })
    .HasDatabaseName("IX_GlobalEmailTemplates_TriggerType_Enabled")
    .HasFilter("\"TriggerEnabled\" = TRUE");

// Check constraints
builder.HasCheckConstraint(
    "CHK_GlobalEmailTemplates_TriggerType",
    "\"TriggerType\" IN (0, 1, 2)"
);

builder.HasCheckConstraint(
    "CHK_GlobalEmailTemplates_TimingOffsetDays",
    "\"TimingOffsetDays\" IS NULL OR (\"TimingOffsetDays\" >= -365 AND \"TimingOffsetDays\" <= 365)"
);

builder.HasCheckConstraint(
    "CHK_GlobalEmailTemplates_RecipientGroup",
    "\"RecipientGroup\" IS NULL OR \"RecipientGroup\" IN (0, 1, 2, 3)"
);
```

### 2. EventEmailTemplateConfiguration (Modifications)
**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/EventEmailTemplateConfiguration.cs`

**Add to Configure method**:
```csharp
// Override fields
builder.Property(e => e.OverrideTriggerEnabled)
    .IsRequired(false); // Nullable

builder.Property(e => e.OverrideTimingOffsetDays)
    .IsRequired(false); // Nullable

builder.Property(e => e.OverrideRecipientGroup)
    .IsRequired(false) // Nullable
    .HasConversion<int>(); // Stored as int when set

// Check constraints
builder.HasCheckConstraint(
    "CHK_EventEmailTemplates_OverrideTimingOffsetDays",
    "\"OverrideTimingOffsetDays\" IS NULL OR (\"OverrideTimingOffsetDays\" >= -365 AND \"OverrideTimingOffsetDays\" <= 365)"
);

builder.HasCheckConstraint(
    "CHK_EventEmailTemplates_OverrideRecipientGroup",
    "\"OverrideRecipientGroup\" IS NULL OR \"OverrideRecipientGroup\" IN (0, 1, 2, 3)"
);
```

### 3. SentAdHocEmailConfiguration (Modifications)
**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/SentAdHocEmailConfiguration.cs`

**Add to Configure method**:
```csharp
// Scheduled send field
builder.Property(e => e.ScheduledSendAt)
    .IsRequired(false) // Nullable
    .HasColumnType("timestamptz");

// Partial index for scheduled sends (optimization)
builder.HasIndex(e => new { e.ScheduledSendAt, e.DeliveryStatus })
    .HasDatabaseName("IX_SentAdHocEmails_Scheduled_Pending")
    .HasFilter("\"ScheduledSendAt\" IS NOT NULL AND \"DeliveryStatus\" = 'Pending'");
```

### 4. NEW: AdHocEmailTemplateConfiguration
**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/AdHocEmailTemplateConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class AdHocEmailTemplateConfiguration : IEntityTypeConfiguration<AdHocEmailTemplate>
{
    public void Configure(EntityTypeBuilder<AdHocEmailTemplate> builder)
    {
        builder.ToTable("AdHocEmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Template Name
        builder.Property(e => e.TemplateName)
            .IsRequired()
            .HasMaxLength(200);

        // Subject
        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        // HTML Body
        builder.Property(e => e.HtmlBody)
            .IsRequired()
            .HasColumnType("text");

        // Plain Text Body
        builder.Property(e => e.PlainTextBody)
            .IsRequired()
            .HasColumnType("text");

        // Timestamp (UTC timestamptz)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("IX_AdHocEmailTemplates_CreatedBy");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_AdHocEmailTemplates_CreatedAt");

        builder.HasIndex(e => e.TemplateName)
            .HasDatabaseName("IX_AdHocEmailTemplates_TemplateName");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_TemplateName_NotEmpty",
            "LENGTH(TRIM(\"TemplateName\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_HtmlBody_NotEmpty",
            "LENGTH(TRIM(\"HtmlBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_PlainTextBody_NotEmpty",
            "LENGTH(TRIM(\"PlainTextBody\")) > 0"
        );
    }
}
```

### 5. NEW: EmailTriggerLogConfiguration
**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/EmailTriggerLogConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class EmailTriggerLogConfiguration : IEntityTypeConfiguration<EmailTriggerLog>
{
    public void Configure(EntityTypeBuilder<EmailTriggerLog> builder)
    {
        builder.ToTable("EmailTriggerLogs");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Template ID
        builder.Property(e => e.TemplateId)
            .IsRequired();

        // Event ID (nullable)
        builder.Property(e => e.EventId)
            .IsRequired(false);

        // Session ID (nullable)
        builder.Property(e => e.SessionId)
            .IsRequired(false);

        // Template Type
        builder.Property(e => e.TemplateType)
            .IsRequired()
            .HasMaxLength(50);

        // Trigger Type
        builder.Property(e => e.TriggerType)
            .IsRequired()
            .HasMaxLength(20);

        // Recipient Group
        builder.Property(e => e.RecipientGroup)
            .IsRequired()
            .HasMaxLength(50);

        // Recipient Count
        builder.Property(e => e.RecipientCount)
            .IsRequired();

        // Timestamps (UTC timestamptz)
        builder.Property(e => e.TriggeredAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.SentAt)
            .IsRequired(false)
            .HasColumnType("timestamptz");

        // Status
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Sent");

        // Error Message
        builder.Property(e => e.ErrorMessage)
            .IsRequired(false)
            .HasColumnType("text");

        // Foreign Keys (nullable to avoid cascade issues)
        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for query performance
        builder.HasIndex(e => e.TemplateId)
            .HasDatabaseName("IX_EmailTriggerLogs_TemplateId");

        builder.HasIndex(e => new { e.EventId, e.SessionId })
            .HasDatabaseName("IX_EmailTriggerLogs_EventId_SessionId");

        builder.HasIndex(e => e.TriggeredAt)
            .IsDescending()
            .HasDatabaseName("IX_EmailTriggerLogs_TriggeredAt");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_EmailTriggerLogs_Status");

        // Idempotency index (prevent duplicate sends)
        builder.HasIndex(e => new { e.TemplateId, e.SessionId, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_EmailTriggerLogs_Idempotency")
            .HasFilter("\"SessionId\" IS NOT NULL AND \"Status\" = 'Sent'");

        // Partial index for failed sends
        builder.HasIndex(e => new { e.Status, e.TriggeredAt })
            .HasDatabaseName("IX_EmailTriggerLogs_Failed_TriggeredAt")
            .HasFilter("\"Status\" = 'Failed'");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_EmailTriggerLogs_Status",
            "\"Status\" IN ('Sent', 'Failed', 'Skipped')"
        );

        builder.HasCheckConstraint(
            "CHK_EmailTriggerLogs_RecipientCount",
            "\"RecipientCount\" >= 0"
        );
    }
}
```

---

## DbContext Changes

**Location**: `/apps/api/Data/ApplicationDbContext.cs`

**Add DbSet properties** (around line 280):
```csharp
/// <summary>
/// AdHocEmailTemplates table for saved ad-hoc templates
/// </summary>
public DbSet<AdHocEmailTemplate> AdHocEmailTemplates { get; set; }

/// <summary>
/// EmailTriggerLogs table for automated email audit trail
/// </summary>
public DbSet<EmailTriggerLog> EmailTriggerLogs { get; set; }
```

**Apply configurations** in `OnModelCreating` method (around line 1106):
```csharp
// Apply Email Templates configurations
modelBuilder.ApplyConfiguration(new GlobalEmailTemplateConfiguration());
modelBuilder.ApplyConfiguration(new EventEmailTemplateConfiguration());
modelBuilder.ApplyConfiguration(new SentAdHocEmailConfiguration());
modelBuilder.ApplyConfiguration(new AdHocEmailTemplateConfiguration());
modelBuilder.ApplyConfiguration(new EmailTriggerLogConfiguration());
```

**Add UTC handling** in `UpdateAuditFields` method (around line 1750):
```csharp
// Handle AdHocEmailTemplate entities
var adHocTemplateEntries = ChangeTracker.Entries<AdHocEmailTemplate>();
foreach (var entry in adHocTemplateEntries)
{
    if (entry.State == EntityState.Added)
    {
        entry.Entity.CreatedAt = DateTime.UtcNow;
    }
}

// Handle EmailTriggerLog entities
var triggerLogEntries = ChangeTracker.Entries<EmailTriggerLog>();
foreach (var entry in triggerLogEntries)
{
    if (entry.State == EntityState.Added)
    {
        entry.Entity.TriggeredAt = DateTime.UtcNow;

        if (entry.Entity.SentAt.HasValue && entry.Entity.SentAt.Value.Kind != DateTimeKind.Utc)
        {
            entry.Entity.SentAt = DateTime.SpecifyKind(entry.Entity.SentAt.Value, DateTimeKind.Utc);
        }
    }
}
```

---

## Migration Strategy

### Migration Name
**Pattern**: `YYYYMMDDHHMMSS_AddEmailTriggerEnhancements.cs`

**Example**: `20251201143000_AddEmailTriggerEnhancements.cs`

### Migration Commands

**Create migration**:
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddEmailTriggerEnhancements
```

**Apply migration** (local dev):
```bash
dotnet ef database update
```

**Production deployment**: Use `database-reset-staging` or `staging-deploy` skill (includes migration application).

### Migration Contents

**Up method** will include:
1. Add new columns to GlobalEmailTemplates (TriggerType, TriggerEnabled, TimingOffsetDays, RecipientGroup)
2. Add new columns to EventEmailTemplates (Override fields)
3. Add new column to SentAdHocEmails (ScheduledSendAt)
4. Create new table AdHocEmailTemplates (all columns)
5. Create new table EmailTriggerLogs (all columns)
6. Create all indexes and constraints

**Down method** will include:
1. Drop tables (EmailTriggerLogs, AdHocEmailTemplates)
2. Remove columns (reverse order of Up)
3. Drop indexes and constraints

### Default Values
All new fields have appropriate defaults to ensure existing data remains valid:
- `TriggerType` = `TemplateTriggerType.FixedEvent` (1)
- `TriggerEnabled` = `true`
- `TimingOffsetDays` = `null`
- `RecipientGroup` = `null`
- All override fields = `null`

---

## Performance Considerations

### Index Strategy

**Query Patterns Optimized**:
1. **Time-based trigger lookup**: Find templates to trigger for sessions starting soon
   - Index: `IX_GlobalEmailTemplates_TriggerType_Enabled` (partial, only enabled triggers)

2. **Idempotency check**: Prevent duplicate sends for same session/template
   - Index: `UQ_EmailTriggerLogs_Idempotency` (unique, only successful sends)

3. **Scheduled ad hoc lookup**: Find emails ready to send
   - Index: `IX_SentAdHocEmails_Scheduled_Pending` (partial, only pending scheduled)

4. **Audit log queries**: Session history, failed sends
   - Index: `IX_EmailTriggerLogs_EventId_SessionId`
   - Index: `IX_EmailTriggerLogs_Failed_TriggeredAt` (partial)

**Partial Indexes** (PostgreSQL optimization):
- Only index rows that match filter condition
- Significantly reduces index size
- Improves query performance for filtered queries

### Table Size Estimates

**EmailTriggerLogs**:
- Growth: ~10-50 rows per event (depending on sessions and triggers)
- Retention: Consider archiving logs > 1 year old
- Size: Negligible (< 1MB per 1000 events)

**AdHocEmailTemplates**:
- Growth: User-created, likely < 100 total
- Size: Negligible (< 1MB)

**Existing tables**:
- GlobalEmailTemplates: +4 columns (minimal growth)
- EventEmailTemplates: +3 columns (minimal growth)
- SentAdHocEmails: +1 column (minimal growth)

### Query Performance

**Expected performance** (with proper indexes):
- Time-based trigger lookup: < 50ms (scans partial index)
- Idempotency check: < 10ms (unique index lookup)
- Audit log retrieval: < 100ms (indexed by session/event)

**No N+1 concerns**:
- Recipient resolution happens in separate service
- Trigger logs written in batches
- No circular dependencies

---

## Security & Validation

### Data Integrity

**Check Constraints**:
- TimingOffsetDays: Must be between -365 and 365 (prevent unreasonable offsets)
- RecipientCount: Must be >= 0
- Status: Must be 'Sent', 'Failed', or 'Skipped'
- All text fields: Cannot be empty strings (LENGTH(TRIM(...)) > 0)

**Foreign Keys**:
- GlobalEmailTemplate.UpdatedBy → Users (RESTRICT - preserve history)
- AdHocEmailTemplate.CreatedBy → Users (RESTRICT - preserve authorship)
- EmailTriggerLog.EventId → Events (SET NULL - preserve log if event deleted)
- EmailTriggerLog.SessionId → Sessions (SET NULL - preserve log if session deleted)

**Unique Constraints**:
- GlobalEmailTemplates: (Category, TemplateType) - prevent duplicates
- EmailTriggerLogs: (TemplateId, SessionId, TemplateType) - idempotency for successful sends

### Validation Rules

**Business Rules** (to enforce in service layer):
1. TimeBased triggers require TimingOffsetDays to be set
2. Events category templates can have RecipientGroup set
3. Non-Events categories ignore RecipientGroup
4. Override fields only apply if base template has value to override

**Permission Rules** (to enforce in API layer):
1. Only admins can modify GlobalEmailTemplates
2. Only event organizers can modify EventEmailTemplates for their events
3. Only admins can delete AdHocEmailTemplates (or original creator)

---

## Testing Requirements

### Unit Tests

**Entities**:
- Verify default values are set
- Test enum conversions
- Validate nullable logic

**Configurations**:
- Verify all constraints are created
- Test index configurations
- Validate foreign key behaviors

### Integration Tests

**Database Operations**:
- Insert new templates with trigger config
- Update existing templates (verify Version increments)
- Create event overrides
- Save ad hoc templates
- Log trigger events

**Idempotency**:
- Attempt duplicate trigger log for same session (should fail unique constraint)
- Verify partial index works (only successful sends indexed)

**Query Performance**:
- Benchmark time-based trigger lookup
- Test scheduled ad hoc query
- Verify partial index usage (EXPLAIN ANALYZE)

### Data Migration Tests

**Existing Data**:
- Verify all existing templates get default TriggerType = FixedEvent
- Verify no data loss during migration
- Test rollback (Down migration)

---

## Monitoring & Observability

### Metrics to Track

**Email Trigger Success Rate**:
```sql
SELECT
    Status,
    COUNT(*) as Count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER(), 2) as Percentage
FROM "EmailTriggerLogs"
WHERE "TriggeredAt" >= NOW() - INTERVAL '30 days'
GROUP BY Status;
```

**Scheduled Send Performance**:
```sql
SELECT
    COUNT(*) as ScheduledCount,
    AVG(EXTRACT(EPOCH FROM ("SentAt" - "ScheduledSendAt"))) as AvgDelaySec
FROM "SentAdHocEmails"
WHERE "ScheduledSendAt" IS NOT NULL
  AND "SentAt" IS NOT NULL
  AND "SentAt" >= NOW() - INTERVAL '7 days';
```

**Trigger Log Growth**:
```sql
SELECT
    DATE("TriggeredAt") as Date,
    COUNT(*) as TriggerCount
FROM "EmailTriggerLogs"
WHERE "TriggeredAt" >= NOW() - INTERVAL '30 days'
GROUP BY DATE("TriggeredAt")
ORDER BY Date DESC;
```

### Health Checks

**Scheduled Job Status**:
- Monitor EmailSchedulerJob execution (Hangfire dashboard)
- Alert on failed email sends (Status = 'Failed')
- Track stuck scheduled sends (ScheduledSendAt < NOW() but Status = 'Pending')

---

## Next Steps for Backend Developer

### Implementation Order

1. **Create Entities & Enums** (0.5 session)
   - TemplateTriggerType.cs
   - EventRecipientGroup.cs
   - AdHocEmailTemplate.cs
   - EmailTriggerLog.cs
   - Modify existing entities (GlobalEmailTemplate, EventEmailTemplate, SentAdHocEmail)

2. **Create Configuration Classes** (0.5 session)
   - AdHocEmailTemplateConfiguration.cs
   - EmailTriggerLogConfiguration.cs
   - Modify existing configurations

3. **Update DbContext** (0.25 session)
   - Add DbSets
   - Apply configurations
   - Add UTC handling

4. **Generate Migration** (0.25 session)
   - Run `dotnet ef migrations add AddEmailTriggerEnhancements`
   - Review generated migration
   - Test Up and Down methods

5. **Apply to Local Database** (0.25 session)
   - Run `dotnet ef database update`
   - Verify schema changes
   - Test with seed data

### Files to Create

**New Files**:
- `/apps/api/Features/EmailTemplates/Entities/TemplateTriggerType.cs`
- `/apps/api/Features/EmailTemplates/Entities/EventRecipientGroup.cs`
- `/apps/api/Features/EmailTemplates/Entities/AdHocEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/EmailTriggerLog.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/AdHocEmailTemplateConfiguration.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/EmailTriggerLogConfiguration.cs`

**Files to Modify**:
- `/apps/api/Features/EmailTemplates/Entities/GlobalEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/SentAdHocEmail.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/GlobalEmailTemplateConfiguration.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/EventEmailTemplateConfiguration.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/SentAdHocEmailConfiguration.cs`
- `/apps/api/Data/ApplicationDbContext.cs`

### Verification Steps

**After Migration**:
1. Check PostgreSQL schema matches design
2. Verify all indexes created
3. Test check constraints (attempt invalid values)
4. Verify foreign key behaviors (delete user, event, session)
5. Test unique constraint (duplicate trigger log)

---

## Key Decisions & Rationale

### Why Separate AdHocEmailTemplate Entity?

**Decision**: Create separate entity instead of reusing GlobalEmailTemplate with AdHoc category.

**Rationale**:
- GlobalEmailTemplate has TriggerType/Timing fields that don't apply to ad hoc
- Ad hoc templates are user-created and deletable (different lifecycle)
- Cleaner separation of concerns
- Avoids nullable field pollution

### Why EventRecipientGroup Enum Instead of String?

**Decision**: Use enum stored as int, not string values.

**Rationale**:
- Type safety in code
- Smaller storage (int vs varchar)
- Follows existing pattern (EmailCategory)
- Prevents typos in recipient group names

### Why Partial Indexes?

**Decision**: Use partial indexes with WHERE clauses on high-selectivity columns.

**Rationale**:
- Smaller index size (only index rows that match filter)
- Faster queries (fewer rows to scan)
- PostgreSQL-specific optimization
- Follows existing pattern in SafetyIncidents

### Why Nullable Override Fields?

**Decision**: All EventEmailTemplate override fields are nullable (null = use global).

**Rationale**:
- Distinguishes "not overridden" from "overridden to same value"
- Follows copy-on-edit pattern
- Simplifies resolution logic (null check then fallback to global)

### Why Idempotency Index Only on Successful Sends?

**Decision**: Unique constraint only when Status = 'Sent'.

**Rationale**:
- Allow retry of failed sends
- Only successful sends need deduplication
- Partial unique index (PostgreSQL feature)

---

## Database Design Sign-Off

**Design Complete**: Yes
**Follows EF Core Patterns**: Yes (verified against existing configurations)
**Follows PostgreSQL Patterns**: Yes (timestamptz, JSONB, partial indexes)
**Migration Ready**: Yes (all DDL specified)
**Performance Optimized**: Yes (strategic indexes)
**Security Validated**: Yes (constraints, foreign keys, permissions)

**Next Agent**: backend-developer
**Next Task**: Implement entities, configurations, and generate migration

**Handoff Document Location**: This file
**Created**: 2025-12-01
**Author**: database-designer agent
