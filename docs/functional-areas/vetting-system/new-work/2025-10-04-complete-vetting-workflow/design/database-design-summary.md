# Vetting Email System - Database Design Summary
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer -->
<!-- Status: Complete -->

## Overview

This document summarizes the database schema extensions for the vetting email template and delivery tracking system. The design supports SendGrid integration with comprehensive delivery tracking, admin-editable email templates with variable substitution, and complete audit trails.

## Entity Summary

### New Entities Created

1. **VettingEmailLog** - SendGrid delivery tracking (NEW)
2. **VettingEmailTemplate** - Email templates (ENHANCED with HTML/PlainText support)

### Enhanced Entities

- **VettingEmailTemplate** - Added `HtmlBody`, `PlainTextBody`, and `Variables` fields for better email support

## Detailed Entity Specifications

### 1. VettingEmailTemplate (Enhanced)

**Purpose**: Store admin-editable email templates with variable substitution support

**File Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingEmailTemplate.cs`

**Properties**:
```csharp
public Guid Id { get; set; }                    // Primary Key
public EmailTemplateType TemplateType { get; set; }  // Template identifier (0-5)
public string Subject { get; set; }             // Email subject (max 200 chars)
public string HtmlBody { get; set; }            // HTML email body (NEW)
public string PlainTextBody { get; set; }       // Plain text fallback (NEW)
public string Variables { get; set; }           // JSONB - available variables (NEW)
public bool IsActive { get; set; }              // Template active flag
public int Version { get; set; }                // Template version number
public DateTime CreatedAt { get; set; }         // Creation timestamp (UTC)
public DateTime UpdatedAt { get; set; }         // Last update timestamp (UTC)
public DateTime LastModified { get; set; }      // Last modification (UTC)
public Guid UpdatedBy { get; set; }             // FK to Users
```

**Enum - EmailTemplateType**:
```csharp
ApplicationReceived = 0    // Confirmation email
InterviewApproved = 1      // Interview invitation
Approved = 2               // Application approved
OnHold = 3                 // Application on hold
Denied = 4                 // Application denied
InterviewReminder = 5      // Interview follow-up
```

**Indexes**:
- `UQ_VettingEmailTemplates_TemplateType` - Unique constraint on TemplateType
- `IX_VettingEmailTemplates_IsActive` - Partial index for active templates
- `IX_VettingEmailTemplates_UpdatedAt` - Date-based queries
- `IX_VettingEmailTemplates_Variables` - GIN index for JSONB queries

**Constraints**:
- Subject length: 5-200 characters
- HtmlBody length: minimum 10 characters
- PlainTextBody length: minimum 10 characters

**PostgreSQL Features**:
- **JSONB** for `Variables` column (stores available template variables)
- **GIN Index** for efficient JSON queries
- **Partial Index** for active templates only

---

### 2. VettingEmailLog (NEW)

**Purpose**: Track SendGrid email delivery status and history

**File Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingEmailLog.cs`

**Properties**:
```csharp
public Guid Id { get; set; }                        // Primary Key
public Guid ApplicationId { get; set; }             // FK to VettingApplications
public EmailTemplateType TemplateType { get; set; }  // Template used
public string RecipientEmail { get; set; }          // Recipient address (max 256)
public string Subject { get; set; }                 // Email subject (max 200)
public DateTime SentAt { get; set; }                // Send timestamp (UTC)
public EmailDeliveryStatus DeliveryStatus { get; set; }  // Delivery status (0-4)
public string? SendGridMessageId { get; set; }      // SendGrid tracking ID (max 100)
public string? ErrorMessage { get; set; }           // Error details if failed
public int RetryCount { get; set; }                 // Retry attempts (0-10)
public DateTime? LastRetryAt { get; set; }          // Last retry timestamp (UTC)
```

**Enum - EmailDeliveryStatus**:
```csharp
Pending = 0     // Queued for sending
Sent = 1        // Sent to SendGrid
Delivered = 2   // Confirmed delivery
Failed = 3      // Delivery failed
Bounced = 4     // Email bounced
```

**Indexes**:
- `IX_VettingEmailLogs_ApplicationId` - Application history queries
- `IX_VettingEmailLogs_DeliveryStatus` - Status monitoring
- `IX_VettingEmailLogs_SentAt` - Date range queries
- `IX_VettingEmailLogs_DeliveryStatus_RetryCount_SentAt` - Composite partial index for failed emails
- `IX_VettingEmailLogs_SendGridMessageId` - SendGrid webhook lookups (partial, non-null only)

**Constraints**:
- RetryCount range: 0-10 attempts

**Cascade Behavior**:
- CASCADE DELETE with VettingApplications (logs deleted when application deleted)

---

## Database Migration

**Migration File**: Will be created as `/home/chad/repos/witchcityrope/apps/api/Migrations/YYYYMMDDHHMMSS_AddVettingEmailTemplatesEnhancementsAndLogs.cs`

### Migration Steps

1. **Alter VettingEmailTemplates Table**:
   - Add `HtmlBody` column (text, required)
   - Add `PlainTextBody` column (text, required)
   - Add `Variables` column (jsonb, default '{}')
   - Migrate existing `Body` data to `HtmlBody`
   - Create GIN index on `Variables` column
   - Add check constraints for new columns

2. **Create VettingEmailLogs Table**:
   - Create table with all columns
   - Add primary key constraint
   - Add foreign key to VettingApplications with CASCADE DELETE
   - Create performance indexes
   - Add check constraint on RetryCount

3. **Seed Default Email Templates** (6 templates):
   - Application Received confirmation
   - Interview Approved invitation
   - Application Approved welcome
   - Application OnHold notification
   - Application Denied message
   - Interview Reminder follow-up

### Migration SQL Preview

```sql
-- Alter VettingEmailTemplates
ALTER TABLE "VettingEmailTemplates"
  ADD COLUMN "HtmlBody" TEXT NOT NULL DEFAULT '',
  ADD COLUMN "PlainTextBody" TEXT NOT NULL DEFAULT '',
  ADD COLUMN "Variables" JSONB NOT NULL DEFAULT '{}';

-- Migrate existing Body data to HtmlBody
UPDATE "VettingEmailTemplates" SET "HtmlBody" = "Body";

-- Create GIN index for JSONB
CREATE INDEX "IX_VettingEmailTemplates_Variables"
  ON "VettingEmailTemplates" USING GIN ("Variables");

-- Add check constraints
ALTER TABLE "VettingEmailTemplates"
  ADD CONSTRAINT "CHK_VettingEmailTemplates_HtmlBody_Length"
    CHECK (LENGTH("HtmlBody") >= 10),
  ADD CONSTRAINT "CHK_VettingEmailTemplates_PlainTextBody_Length"
    CHECK (LENGTH("PlainTextBody") >= 10);

-- Create VettingEmailLogs table
CREATE TABLE "VettingEmailLogs" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "ApplicationId" UUID NOT NULL,
    "TemplateType" INTEGER NOT NULL,
    "RecipientEmail" VARCHAR(256) NOT NULL,
    "Subject" VARCHAR(200) NOT NULL,
    "SentAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "DeliveryStatus" INTEGER NOT NULL DEFAULT 0,
    "SendGridMessageId" VARCHAR(100),
    "ErrorMessage" TEXT,
    "RetryCount" INTEGER NOT NULL DEFAULT 0,
    "LastRetryAt" TIMESTAMPTZ,
    CONSTRAINT "PK_VettingEmailLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VettingEmailLogs_VettingApplications"
      FOREIGN KEY ("ApplicationId")
      REFERENCES "VettingApplications"("Id")
      ON DELETE CASCADE,
    CONSTRAINT "CHK_VettingEmailLogs_RetryCount"
      CHECK ("RetryCount" >= 0 AND "RetryCount" <= 10)
);

-- Create indexes for VettingEmailLogs
CREATE INDEX "IX_VettingEmailLogs_ApplicationId"
  ON "VettingEmailLogs"("ApplicationId");

CREATE INDEX "IX_VettingEmailLogs_DeliveryStatus"
  ON "VettingEmailLogs"("DeliveryStatus");

CREATE INDEX "IX_VettingEmailLogs_SentAt"
  ON "VettingEmailLogs"("SentAt");

CREATE INDEX "IX_VettingEmailLogs_DeliveryStatus_RetryCount_SentAt"
  ON "VettingEmailLogs"("DeliveryStatus", "RetryCount", "SentAt")
  WHERE "DeliveryStatus" IN (3, 4) AND "RetryCount" < 5;

CREATE INDEX "IX_VettingEmailLogs_SendGridMessageId"
  ON "VettingEmailLogs"("SendGridMessageId")
  WHERE "SendGridMessageId" IS NOT NULL;
```

---

## Performance Considerations

### Indexing Strategy

1. **Template Lookups**: Unique index on TemplateType ensures fast template retrieval
2. **Application History**: ApplicationId index supports efficient email history queries
3. **Delivery Monitoring**: DeliveryStatus index for admin monitoring dashboards
4. **Failed Email Retry**: Composite partial index for efficient retry queue processing
5. **SendGrid Webhooks**: Partial index on SendGridMessageId for webhook processing
6. **JSONB Variables**: GIN index for template variable queries

### Query Optimization

**Common Query Patterns**:

```sql
-- Get email history for application
SELECT * FROM "VettingEmailLogs"
WHERE "ApplicationId" = $1
ORDER BY "SentAt" DESC;

-- Get failed emails for retry
SELECT * FROM "VettingEmailLogs"
WHERE "DeliveryStatus" IN (3, 4)  -- Failed or Bounced
  AND "RetryCount" < 5
ORDER BY "SentAt" ASC;

-- Get active template by type
SELECT * FROM "VettingEmailTemplates"
WHERE "TemplateType" = $1
  AND "IsActive" = TRUE;

-- SendGrid webhook lookup
SELECT * FROM "VettingEmailLogs"
WHERE "SendGridMessageId" = $1;
```

### Storage Estimates

**VettingEmailTemplates**:
- 6 templates × ~5KB per template = ~30KB total
- Minimal storage impact

**VettingEmailLogs**:
- Estimated 4 emails per application × 1KB per log = 4KB per application
- For 1000 applications: ~4MB
- Growth rate: Linear with application volume

---

## Security & Data Integrity

### Data Protection

1. **Email Addresses**: Stored in plain text for delivery (SendGrid requirement)
2. **Template Security**: Admin-only access via authorization policies
3. **Audit Trail**: Complete email delivery history with timestamps

### Cascade Delete Behavior

- **EmailLogs → Applications**: CASCADE (logs deleted with application)
- **EmailTemplates**: Standalone (no cascade, referenced by logs)

### Constraints Enforced

1. **Template Uniqueness**: One active template per type
2. **Retry Limits**: Maximum 10 retry attempts
3. **Field Validation**: Minimum length requirements for email content
4. **Foreign Keys**: Referential integrity with VettingApplications

---

## Integration Points

### SendGrid Integration

**VettingEmailLog tracks**:
- SendGrid Message ID for webhook correlation
- Delivery status updates via webhooks
- Bounce and failure notifications
- Retry management for failed deliveries

### Application Workflow

**Email triggers on status changes**:
- `Submitted` → Application Received email
- `InterviewApproved` → Interview Approved email
- `Approved` → Application Approved email
- `OnHold` → Application OnHold email
- `Denied` → Application Denied email

**Reminder emails**:
- Interview Reminder for applications in `InterviewApproved` status > 7 days

---

## DbContext Updates

**File**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`

**Changes Made**:

1. Added `DbSet<VettingEmailLog> VettingEmailLogs` property
2. Applied `VettingEmailLogConfiguration` in `OnModelCreating`
3. Added UTC handling for `VettingEmailLog` in `UpdateAuditFields` method
4. Added UTC handling for `VettingEmailTemplate` in `UpdateAuditFields` method

---

## Testing Recommendations

### Unit Tests

1. **Entity Validation**:
   - Verify constraint enforcement (retry count, field lengths)
   - Test enum conversions
   - Validate UTC DateTime handling

2. **Configuration Tests**:
   - Verify index creation
   - Test cascade delete behavior
   - Validate JSONB column operations

### Integration Tests

1. **Template Management**:
   - Create/update/delete templates
   - Variable substitution
   - Active/inactive filtering

2. **Email Logging**:
   - Log creation on email send
   - Delivery status updates
   - Retry tracking
   - SendGrid ID correlation

3. **Query Performance**:
   - Application history queries
   - Failed email retry queries
   - Template lookups

---

## Migration Commands

### Create Migration

```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddVettingEmailTemplatesEnhancementsAndLogs --context ApplicationDbContext
```

### Apply Migration

```bash
dotnet ef database update --context ApplicationDbContext
```

### Rollback (if needed)

```bash
dotnet ef database update PreviousMigrationName --context ApplicationDbContext
```

---

## Files Created/Modified

### New Files

1. `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingEmailLog.cs`
2. `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/Configuration/VettingEmailLogConfiguration.cs`

### Modified Files

1. `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingEmailTemplate.cs`
   - Added `HtmlBody` property
   - Added `PlainTextBody` property
   - Added `Variables` property (JSONB)
   - Added obsolete `Body` property for backward compatibility

2. `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/Configuration/VettingEmailTemplateConfiguration.cs`
   - Configured `HtmlBody` mapping
   - Configured `PlainTextBody` mapping
   - Configured `Variables` as JSONB with GIN index
   - Added check constraints for new fields
   - Ignored obsolete `Body` property

3. `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`
   - Added `VettingEmailLogs` DbSet
   - Applied `VettingEmailLogConfiguration`
   - Added UTC handling for both entities in `UpdateAuditFields`

---

## Design Decisions

### 1. Separate Email Log Entity

**Decision**: Create separate `VettingEmailLog` entity instead of using existing `VettingNotification`

**Rationale**:
- Focused on SendGrid-specific tracking
- Cleaner separation of concerns
- Optimized indexes for email delivery monitoring
- Simpler schema for email-only functionality

### 2. HTML and Plain Text Bodies

**Decision**: Store both HTML and plain text versions of email templates

**Rationale**:
- Email client compatibility (some clients don't support HTML)
- Better deliverability (spam filters prefer multi-part emails)
- Accessibility compliance
- Follows email best practices

### 3. JSONB for Variables

**Decision**: Use PostgreSQL JSONB column for template variables

**Rationale**:
- Flexible schema for different template types
- Efficient storage and querying with GIN indexes
- Native PostgreSQL support for JSON operations
- Easy to extend with new variables

### 4. Partial Indexes

**Decision**: Use partial indexes for failed emails and SendGrid message IDs

**Rationale**:
- Smaller index size (only indexes relevant rows)
- Faster queries for retry operations
- Reduced storage overhead
- Improved query performance for specific use cases

### 5. Cascade Delete Behavior

**Decision**: CASCADE delete email logs with applications

**Rationale**:
- Email logs are dependent on applications
- No orphaned logs when applications are deleted
- Simplifies data management
- Audit trail preserved until application deletion

---

## PostgreSQL Patterns Applied

### 1. UTC DateTime Handling

All DateTime columns use `timestamptz` type and values are stored as UTC via `UpdateAuditFields` method in ApplicationDbContext.

### 2. JSONB with GIN Indexes

Variables column uses JSONB with GIN index for efficient JSON querying:
```sql
CREATE INDEX "IX_VettingEmailTemplates_Variables"
  ON "VettingEmailTemplates" USING GIN ("Variables");
```

### 3. Partial Indexes for Sparse Data

```sql
-- Only index failed/bounced emails with retry attempts remaining
CREATE INDEX "IX_VettingEmailLogs_DeliveryStatus_RetryCount_SentAt"
  ON "VettingEmailLogs"("DeliveryStatus", "RetryCount", "SentAt")
  WHERE "DeliveryStatus" IN (3, 4) AND "RetryCount" < 5;

-- Only index logs with SendGrid Message IDs
CREATE INDEX "IX_VettingEmailLogs_SendGridMessageId"
  ON "VettingEmailLogs"("SendGridMessageId")
  WHERE "SendGridMessageId" IS NOT NULL;
```

### 4. Check Constraints for Business Rules

```sql
CONSTRAINT "CHK_VettingEmailLogs_RetryCount"
  CHECK ("RetryCount" >= 0 AND "RetryCount" <= 10)
```

### 5. Explicit Constraint Naming

All constraints have explicit names for clear error messages and easier management.

---

## Next Steps

### 1. Create Migration

Generate the EF Core migration with:
```bash
cd apps/api
dotnet ef migrations add AddVettingEmailTemplatesEnhancementsAndLogs
```

### 2. Review Migration Script

Inspect the generated migration to ensure:
- Correct column additions for VettingEmailTemplates
- Proper VettingEmailLogs table creation
- All indexes created correctly
- Check constraints applied
- Seed data included

### 3. Apply Migration

```bash
dotnet ef database update
```

### 4. Verify Schema

Connect to PostgreSQL and verify:
- New columns in VettingEmailTemplates
- VettingEmailLogs table structure
- All indexes created
- Constraints active
- Seed data populated

### 5. Test Queries

Run sample queries to validate:
- Template lookups by type
- Email log history queries
- Failed email retry queries
- SendGrid webhook lookups

---

## Summary

This database design provides a robust foundation for the vetting email system with:

- ✅ **Comprehensive email template management** with HTML/plain text support
- ✅ **Complete SendGrid delivery tracking** with retry management
- ✅ **Optimized query performance** via strategic indexing
- ✅ **PostgreSQL best practices** (JSONB, partial indexes, UTC handling)
- ✅ **Data integrity** through constraints and cascade behavior
- ✅ **Scalable architecture** for growing application volume

The schema is ready for migration creation and database deployment.

---

**Document Version**: 1.0
**Last Updated**: 2025-10-04
**Status**: Complete - Ready for Migration
