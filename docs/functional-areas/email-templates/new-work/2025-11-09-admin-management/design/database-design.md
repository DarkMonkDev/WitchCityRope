# Database Design: Email Templates Admin Management

**Feature**: Email Templates Admin Management
**Date**: 2025-11-09
**Version**: 1.0
**Designer**: Database Designer Agent
**Status**: Ready for Implementation

---

## Executive Summary

This database design establishes a centralized email template management system supporting 5 categories (Vetting, Events, Admin, Incident, Ad Hoc) with 22 default templates. The architecture uses a **copy-on-edit pattern** to minimize database storage while allowing event organizers to customize event-specific templates.

**Key Design Decisions**:
- **GlobalEmailTemplates**: Single source of truth for default templates across all categories
- **EventEmailTemplates**: Created ONLY when organizer saves customizations (copy-on-edit)
- **SentAdHocEmails**: Permanent audit trail for bulk email operations
- **VettingEmailTemplates Migration**: Existing 6 Vetting templates migrated to GlobalEmailTemplates
- **PostgreSQL Optimization**: JSONB with GIN indexes for variable storage

---

## Table of Contents

1. [Entity Relationship Diagram](#entity-relationship-diagram)
2. [Schema Design (SQL DDL)](#schema-design-sql-ddl)
3. [Entity Framework Configuration](#entity-framework-configuration)
4. [Migration Strategy](#migration-strategy)
5. [Index Strategy](#index-strategy)
6. [Performance Considerations](#performance-considerations)
7. [Security & Data Integrity](#security--data-integrity)
8. [Seed Data Specifications](#seed-data-specifications)
9. [Testing Data](#testing-data)

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      AspNetUsers                                 │
│  (ApplicationUser - Identity Framework)                         │
│                                                                  │
│  Id: UUID (PK)                                                  │
│  Email: string                                                   │
│  ...                                                             │
└─────────────────────────────────────────────────────────────────┘
         ▲                    ▲                    ▲
         │ UpdatedBy          │ UpdatedBy          │ SentBy
         │ (RESTRICT)         │ (RESTRICT)         │ (RESTRICT)
         │                    │                    │
┌────────┴─────────┐  ┌──────┴──────────┐  ┌─────┴───────────┐
│ GlobalEmail      │  │ EventEmail      │  │ SentAdHocEmail  │
│ Templates        │  │ Templates       │  │                 │
├──────────────────┤  ├─────────────────┤  ├─────────────────┤
│ Id: UUID (PK)    │  │ Id: UUID (PK)   │  │ Id: UUID (PK)   │
│ Category: int    │  │ EventId: UUID   │  │ Subject: string │
│ TemplateType:    │  │ GlobalTemplateId│  │ HtmlBody: text  │
│   string         │  │ TemplateType:   │  │ RecipientGroup  │
│ Subject: string  │  │   string        │  │ RecipientEmails │
│ HtmlBody: text   │  │ Subject: string │  │ RecipientCount  │
│ PlainTextBody    │  │ HtmlBody: text  │  │ EventId: UUID?  │
│ Variables: JSONB │  │ PlainTextBody   │  │ SendGridMsgId   │
│ IsActive: bool   │  │ TargetSessions  │  │ DeliveryStatus  │
│ Version: int     │  │ IsCustomized    │  │ SentAt: datetime│
│ CreatedAt        │  │ CreatedAt       │  │ SentBy: UUID    │
│ UpdatedAt        │  │ UpdatedAt       │  └─────────────────┘
│ UpdatedBy: UUID  │  │ UpdatedBy: UUID │           │
└──────────────────┘  └─────────────────┘           │ EventId
         │                     │                     │ (SET NULL)
         │                     │ EventId             │
         │                     │ (CASCADE)           │
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               ▼
                      ┌─────────────────┐
                      │     Events      │
                      ├─────────────────┤
                      │ Id: UUID (PK)   │
                      │ Title: string   │
                      │ StartTime       │
                      │ ...             │
                      └─────────────────┘

**Relationship Cardinalities**:
- GlobalEmailTemplates → ApplicationUser: Many-to-One (UpdatedBy)
- EventEmailTemplates → Events: Many-to-One (CASCADE delete)
- EventEmailTemplates → ApplicationUser: Many-to-One (UpdatedBy)
- EventEmailTemplates → GlobalEmailTemplates: Reference ONLY (no FK constraint)
- SentAdHocEmails → Events: Many-to-One (SET NULL on delete)
- SentAdHocEmails → ApplicationUser: Many-to-One (SentBy)

**Unique Constraints**:
- GlobalEmailTemplates: (Category, TemplateType)
- EventEmailTemplates: (EventId, TemplateType)
- SentAdHocEmails: None (can send multiple emails to same group)
```

---

## Schema Design (SQL DDL)

### Table 1: GlobalEmailTemplates

```sql
CREATE TABLE "GlobalEmailTemplates" (
    -- Primary Key
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),

    -- Category and Type
    "Category" INTEGER NOT NULL,
    "TemplateType" VARCHAR(50) NOT NULL,

    -- Content
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Metadata
    "Variables" JSONB NOT NULL DEFAULT '{}'::jsonb,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,

    -- Audit
    "Version" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedBy" UUID NOT NULL,

    -- Constraints
    CONSTRAINT "PK_GlobalEmailTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GlobalEmailTemplates_Users_UpdatedBy"
        FOREIGN KEY ("UpdatedBy")
        REFERENCES "AspNetUsers" ("Id")
        ON DELETE RESTRICT,
    CONSTRAINT "UQ_GlobalEmailTemplates_Category_Type"
        UNIQUE ("Category", "TemplateType"),
    CONSTRAINT "CHK_GlobalEmailTemplates_Category"
        CHECK ("Category" IN (0, 1, 2, 3, 4)),
    CONSTRAINT "CHK_GlobalEmailTemplates_Subject_NotEmpty"
        CHECK (LENGTH(TRIM("Subject")) > 0),
    CONSTRAINT "CHK_GlobalEmailTemplates_HtmlBody_NotEmpty"
        CHECK (LENGTH(TRIM("HtmlBody")) > 0),
    CONSTRAINT "CHK_GlobalEmailTemplates_PlainTextBody_NotEmpty"
        CHECK (LENGTH(TRIM("PlainTextBody")) > 0),
    CONSTRAINT "CHK_GlobalEmailTemplates_Version"
        CHECK ("Version" >= 1)
);

-- Indexes
CREATE INDEX "IX_GlobalEmailTemplates_Category"
    ON "GlobalEmailTemplates" ("Category");

CREATE INDEX "IX_GlobalEmailTemplates_UpdatedBy"
    ON "GlobalEmailTemplates" ("UpdatedBy");

CREATE INDEX "IX_GlobalEmailTemplates_Variables_Gin"
    ON "GlobalEmailTemplates" USING GIN ("Variables");

CREATE INDEX "IX_GlobalEmailTemplates_UpdatedAt"
    ON "GlobalEmailTemplates" ("UpdatedAt" DESC);

-- Comments
COMMENT ON TABLE "GlobalEmailTemplates" IS 'Global email templates serving as defaults for all categories';
COMMENT ON COLUMN "GlobalEmailTemplates"."Category" IS '0=Vetting, 1=Events, 2=Admin, 3=Incident, 4=AdHoc';
COMMENT ON COLUMN "GlobalEmailTemplates"."TemplateType" IS 'Enum value as string (e.g., "Confirmation", "Reminder1Day")';
COMMENT ON COLUMN "GlobalEmailTemplates"."Variables" IS 'JSONB array of available variables for this template';
COMMENT ON COLUMN "GlobalEmailTemplates"."Version" IS 'Increments on every update for audit trail';
```

### Table 2: EventEmailTemplates

```sql
CREATE TABLE "EventEmailTemplates" (
    -- Primary Key
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),

    -- Relationships
    "EventId" UUID NOT NULL,
    "GlobalTemplateId" UUID NOT NULL,

    -- Template Info
    "TemplateType" VARCHAR(50) NOT NULL,

    -- Content (override of global)
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Configuration
    "TargetSessions" TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    "RecipientGroup" VARCHAR(100) NULL,

    -- Metadata
    "IsCustomized" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedBy" UUID NOT NULL,

    -- Constraints
    CONSTRAINT "PK_EventEmailTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventEmailTemplates_Events_EventId"
        FOREIGN KEY ("EventId")
        REFERENCES "Events" ("Id")
        ON DELETE CASCADE,
    CONSTRAINT "FK_EventEmailTemplates_Users_UpdatedBy"
        FOREIGN KEY ("UpdatedBy")
        REFERENCES "AspNetUsers" ("Id")
        ON DELETE RESTRICT,
    CONSTRAINT "UQ_EventEmailTemplates_EventId_Type"
        UNIQUE ("EventId", "TemplateType"),
    CONSTRAINT "CHK_EventEmailTemplates_Subject_NotEmpty"
        CHECK (LENGTH(TRIM("Subject")) > 0),
    CONSTRAINT "CHK_EventEmailTemplates_HtmlBody_NotEmpty"
        CHECK (LENGTH(TRIM("HtmlBody")) > 0),
    CONSTRAINT "CHK_EventEmailTemplates_PlainTextBody_NotEmpty"
        CHECK (LENGTH(TRIM("PlainTextBody")) > 0)
);

-- Indexes
CREATE INDEX "IX_EventEmailTemplates_EventId"
    ON "EventEmailTemplates" ("EventId");

CREATE INDEX "IX_EventEmailTemplates_UpdatedBy"
    ON "EventEmailTemplates" ("UpdatedBy");

CREATE INDEX "IX_EventEmailTemplates_EventId_TemplateType"
    ON "EventEmailTemplates" ("EventId", "TemplateType");

CREATE INDEX "IX_EventEmailTemplates_UpdatedAt"
    ON "EventEmailTemplates" ("UpdatedAt" DESC);

-- Comments
COMMENT ON TABLE "EventEmailTemplates" IS 'Event-specific email template overrides (copy-on-edit pattern)';
COMMENT ON COLUMN "EventEmailTemplates"."GlobalTemplateId" IS 'Reference to global template (NOT enforced FK)';
COMMENT ON COLUMN "EventEmailTemplates"."TargetSessions" IS 'Array of session identifiers (["all"] or ["S1", "S2"])';
COMMENT ON COLUMN "EventEmailTemplates"."IsCustomized" IS 'Always true for event-specific templates';
```

### Table 3: SentAdHocEmails

```sql
CREATE TABLE "SentAdHocEmails" (
    -- Primary Key
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),

    -- Email Details
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Recipients
    "RecipientGroup" VARCHAR(100) NOT NULL,
    "RecipientEmails" TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    "RecipientCount" INTEGER NOT NULL,

    -- Context
    "EventId" UUID NULL,

    -- SendGrid
    "SendGridMessageId" VARCHAR(100) NULL,
    "DeliveryStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending',

    -- Audit
    "SentAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "SentBy" UUID NOT NULL,

    -- Constraints
    CONSTRAINT "PK_SentAdHocEmails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SentAdHocEmails_Events_EventId"
        FOREIGN KEY ("EventId")
        REFERENCES "Events" ("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_SentAdHocEmails_Users_SentBy"
        FOREIGN KEY ("SentBy")
        REFERENCES "AspNetUsers" ("Id")
        ON DELETE RESTRICT,
    CONSTRAINT "CHK_SentAdHocEmails_Subject_NotEmpty"
        CHECK (LENGTH(TRIM("Subject")) > 0),
    CONSTRAINT "CHK_SentAdHocEmails_RecipientCount"
        CHECK ("RecipientCount" >= 0),
    CONSTRAINT "CHK_SentAdHocEmails_DeliveryStatus"
        CHECK ("DeliveryStatus" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced'))
);

-- Indexes
CREATE INDEX "IX_SentAdHocEmails_EventId"
    ON "SentAdHocEmails" ("EventId")
    WHERE "EventId" IS NOT NULL;

CREATE INDEX "IX_SentAdHocEmails_SentBy"
    ON "SentAdHocEmails" ("SentBy");

CREATE INDEX "IX_SentAdHocEmails_SentAt"
    ON "SentAdHocEmails" ("SentAt" DESC);

CREATE INDEX "IX_SentAdHocEmails_DeliveryStatus"
    ON "SentAdHocEmails" ("DeliveryStatus")
    WHERE "DeliveryStatus" IN ('Pending', 'Failed');

-- Comments
COMMENT ON TABLE "SentAdHocEmails" IS 'Permanent audit trail for ad-hoc bulk emails (read-only after creation)';
COMMENT ON COLUMN "SentAdHocEmails"."RecipientEmails" IS 'Array of actual email addresses sent to';
COMMENT ON COLUMN "SentAdHocEmails"."EventId" IS 'Nullable - not all ad-hoc emails are event-related';
```

---

## Entity Framework Configuration

### GlobalEmailTemplate Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a global email template for a specific category and type.
/// Serves as default template unless overridden by event-specific template.
/// </summary>
public class GlobalEmailTemplate
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email category (Vetting, Events, Admin, Incident, AdHoc)
    /// </summary>
    [Required]
    public EmailCategory Category { get; set; }

    /// <summary>
    /// Template type within category (stored as enum string)
    /// Examples: "Confirmation", "Reminder1Day", "ApplicationReceived"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line (max 200 characters)
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
    /// Plain text email body for clients that don't support HTML
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// JSONB field containing available variables for this template
    /// Example: ["{{attendee_name}}", "{{event_title}}"]
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string Variables { get; set; } = "[]";

    /// <summary>
    /// Soft delete flag (false = hidden, never hard delete)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number (increments on each update for audit trail)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who last updated this template
    /// </summary>
    [Required]
    public Guid UpdatedBy { get; set; }

    // Navigation properties
    public ApplicationUser UpdatedByUser { get; set; } = null!;
}

/// <summary>
/// Email category enumeration
/// </summary>
public enum EmailCategory
{
    Vetting = 0,
    Events = 1,
    Admin = 2,
    Incident = 3,
    AdHoc = 4
}
```

### GlobalEmailTemplateConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class GlobalEmailTemplateConfiguration : IEntityTypeConfiguration<GlobalEmailTemplate>
{
    public void Configure(EntityTypeBuilder<GlobalEmailTemplate> builder)
    {
        builder.ToTable("GlobalEmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Category (stored as integer)
        builder.Property(e => e.Category)
            .IsRequired()
            .HasConversion<int>();

        // Template Type
        builder.Property(e => e.TemplateType)
            .IsRequired()
            .HasMaxLength(50);

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

        // Variables (JSONB for PostgreSQL optimization)
        builder.Property(e => e.Variables)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        // IsActive
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Version
        builder.Property(e => e.Version)
            .IsRequired()
            .HasDefaultValue(1);

        // Timestamps (UTC timestamptz for PostgreSQL)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Constraint (Category, TemplateType)
        builder.HasIndex(e => new { e.Category, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_GlobalEmailTemplates_Category_Type");

        // Indexes
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_GlobalEmailTemplates_Category");

        builder.HasIndex(e => e.UpdatedBy)
            .HasDatabaseName("IX_GlobalEmailTemplates_UpdatedBy");

        builder.HasIndex(e => e.UpdatedAt)
            .IsDescending()
            .HasDatabaseName("IX_GlobalEmailTemplates_UpdatedAt");

        // GIN Index for JSONB Variables (PostgreSQL-specific)
        builder.HasIndex(e => e.Variables)
            .HasDatabaseName("IX_GlobalEmailTemplates_Variables_Gin")
            .HasMethod("gin");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Category",
            "\"Category\" IN (0, 1, 2, 3, 4)"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_HtmlBody_NotEmpty",
            "LENGTH(TRIM(\"HtmlBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_PlainTextBody_NotEmpty",
            "LENGTH(TRIM(\"PlainTextBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Version",
            "\"Version\" >= 1"
        );
    }
}
```

### EventEmailTemplate Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents an event-specific email template override.
/// Created only when event organizer customizes a template (copy-on-edit).
/// </summary>
public class EventEmailTemplate
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event this template is associated with
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to global template (for metadata, NOT foreign key constraint)
    /// </summary>
    [Required]
    public Guid GlobalTemplateId { get; set; }

    /// <summary>
    /// Template type (e.g., "Confirmation", "Reminder1Day")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Customized email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Customized HTML email body
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Customized plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Target sessions for multi-session events
    /// ["all"] = all sessions, ["S1", "S2"] = specific sessions
    /// </summary>
    public string[] TargetSessions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Recipient group (for future ad-hoc use)
    /// </summary>
    [MaxLength(100)]
    public string? RecipientGroup { get; set; }

    /// <summary>
    /// Always true for event-specific templates
    /// </summary>
    public bool IsCustomized { get; set; } = true;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created/updated this template
    /// </summary>
    [Required]
    public Guid UpdatedBy { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ApplicationUser UpdatedByUser { get; set; } = null!;

    // GlobalTemplate navigation (optional, not enforced)
    public GlobalEmailTemplate? GlobalTemplate { get; set; }
}
```

### EventEmailTemplateConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class EventEmailTemplateConfiguration : IEntityTypeConfiguration<EventEmailTemplate>
{
    public void Configure(EntityTypeBuilder<EventEmailTemplate> builder)
    {
        builder.ToTable("EventEmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // EventId (required, cascade delete)
        builder.Property(e => e.EventId)
            .IsRequired();

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // GlobalTemplateId (reference only, NO foreign key constraint)
        builder.Property(e => e.GlobalTemplateId)
            .IsRequired();

        // Navigation to GlobalTemplate (optional, not enforced)
        builder.HasOne(e => e.GlobalTemplate)
            .WithMany()
            .HasForeignKey(e => e.GlobalTemplateId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Template Type
        builder.Property(e => e.TemplateType)
            .IsRequired()
            .HasMaxLength(50);

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

        // TargetSessions (PostgreSQL array)
        builder.Property(e => e.TargetSessions)
            .HasColumnType("text[]")
            .HasDefaultValue(Array.Empty<string>());

        // RecipientGroup
        builder.Property(e => e.RecipientGroup)
            .HasMaxLength(100);

        // IsCustomized
        builder.Property(e => e.IsCustomized)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps (UTC timestamptz)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Constraint (EventId, TemplateType)
        builder.HasIndex(e => new { e.EventId, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_EventEmailTemplates_EventId_Type");

        // Indexes
        builder.HasIndex(e => e.EventId)
            .HasDatabaseName("IX_EventEmailTemplates_EventId");

        builder.HasIndex(e => e.UpdatedBy)
            .HasDatabaseName("IX_EventEmailTemplates_UpdatedBy");

        builder.HasIndex(e => e.UpdatedAt)
            .IsDescending()
            .HasDatabaseName("IX_EventEmailTemplates_UpdatedAt");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_EventEmailTemplates_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_EventEmailTemplates_HtmlBody_NotEmpty",
            "LENGTH(TRIM(\"HtmlBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_EventEmailTemplates_PlainTextBody_NotEmpty",
            "LENGTH(TRIM(\"PlainTextBody\")) > 0"
        );
    }
}
```

### SentAdHocEmail Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a sent ad-hoc email with full audit trail.
/// Read-only after creation (never modified or deleted).
/// </summary>
public class SentAdHocEmail
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body (stored for audit)
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Recipient group description
    /// Examples: "all-tickets", "session-1", "volunteers"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Actual email addresses sent to (for audit)
    /// </summary>
    public string[] RecipientEmails { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Total number of recipients
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Related event (nullable - not all ad-hoc emails are event-related)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// SendGrid message ID for delivery tracking
    /// </summary>
    [MaxLength(100)]
    public string? SendGridMessageId { get; set; }

    /// <summary>
    /// Delivery status: Pending, Sent, Delivered, Failed, Bounced
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    /// <summary>
    /// Timestamp when email was sent (UTC)
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who sent this email
    /// </summary>
    [Required]
    public Guid SentBy { get; set; }

    // Navigation properties
    public Event? Event { get; set; }
    public ApplicationUser SentByUser { get; set; } = null!;
}
```

### SentAdHocEmailConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class SentAdHocEmailConfiguration : IEntityTypeConfiguration<SentAdHocEmail>
{
    public void Configure(EntityTypeBuilder<SentAdHocEmail> builder)
    {
        builder.ToTable("SentAdHocEmails");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

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

        // Recipient Group
        builder.Property(e => e.RecipientGroup)
            .IsRequired()
            .HasMaxLength(100);

        // Recipient Emails (PostgreSQL array)
        builder.Property(e => e.RecipientEmails)
            .HasColumnType("text[]")
            .HasDefaultValue(Array.Empty<string>());

        // Recipient Count
        builder.Property(e => e.RecipientCount)
            .IsRequired();

        // EventId (nullable, SET NULL on delete)
        builder.Property(e => e.EventId);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        // SendGrid Message ID
        builder.Property(e => e.SendGridMessageId)
            .HasMaxLength(100);

        // Delivery Status
        builder.Property(e => e.DeliveryStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        // SentAt (UTC timestamptz)
        builder.Property(e => e.SentAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.SentByUser)
            .WithMany()
            .HasForeignKey(e => e.SentBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.EventId)
            .HasDatabaseName("IX_SentAdHocEmails_EventId")
            .HasFilter("\"EventId\" IS NOT NULL");

        builder.HasIndex(e => e.SentBy)
            .HasDatabaseName("IX_SentAdHocEmails_SentBy");

        builder.HasIndex(e => e.SentAt)
            .IsDescending()
            .HasDatabaseName("IX_SentAdHocEmails_SentAt");

        builder.HasIndex(e => e.DeliveryStatus)
            .HasDatabaseName("IX_SentAdHocEmails_DeliveryStatus")
            .HasFilter("\"DeliveryStatus\" IN ('Pending', 'Failed')");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_RecipientCount",
            "\"RecipientCount\" >= 0"
        );

        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_DeliveryStatus",
            "\"DeliveryStatus\" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced')"
        );
    }
}
```

### DbContext Registration

```csharp
// In ApplicationDbContext.cs

public DbSet<GlobalEmailTemplate> GlobalEmailTemplates { get; set; }
public DbSet<EventEmailTemplate> EventEmailTemplates { get; set; }
public DbSet<SentAdHocEmail> SentAdHocEmails { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfiguration(new GlobalEmailTemplateConfiguration());
    modelBuilder.ApplyConfiguration(new EventEmailTemplateConfiguration());
    modelBuilder.ApplyConfiguration(new SentAdHocEmailConfiguration());
}
```

---

## Migration Strategy

### Migration Script: AddEmailTemplatesSystem

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplatesSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // 1. CREATE GlobalEmailTemplates TABLE
            // ========================================
            migrationBuilder.CreateTable(
                name: "GlobalEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.UniqueConstraint("UQ_GlobalEmailTemplates_Category_Type", x => new { x.Category, x.TemplateType });
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Category", "\"Category\" IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_HtmlBody_NotEmpty", "LENGTH(TRIM(\"HtmlBody\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_PlainTextBody_NotEmpty", "LENGTH(TRIM(\"PlainTextBody\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Version", "\"Version\" >= 1");
                });

            // Indexes for GlobalEmailTemplates
            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_Category",
                table: "GlobalEmailTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_UpdatedBy",
                table: "GlobalEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_UpdatedAt",
                table: "GlobalEmailTemplates",
                column: "UpdatedAt",
                descending: true);

            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_GlobalEmailTemplates_Variables_Gin""
                ON ""GlobalEmailTemplates"" USING GIN (""Variables"");
            ");

            // ========================================
            // 2. CREATE EventEmailTemplates TABLE
            // ========================================
            migrationBuilder.CreateTable(
                name: "EventEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    TargetSessions = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[] { }),
                    RecipientGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsCustomized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEmailTemplates_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.UniqueConstraint("UQ_EventEmailTemplates_EventId_Type", x => new { x.EventId, x.TemplateType });
                    table.CheckConstraint("CHK_EventEmailTemplates_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.CheckConstraint("CHK_EventEmailTemplates_HtmlBody_NotEmpty", "LENGTH(TRIM(\"HtmlBody\")) > 0");
                    table.CheckConstraint("CHK_EventEmailTemplates_PlainTextBody_NotEmpty", "LENGTH(TRIM(\"PlainTextBody\")) > 0");
                });

            // Indexes for EventEmailTemplates
            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_EventId",
                table: "EventEmailTemplates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_UpdatedBy",
                table: "EventEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_UpdatedAt",
                table: "EventEmailTemplates",
                column: "UpdatedAt",
                descending: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_EventId_TemplateType",
                table: "EventEmailTemplates",
                columns: new[] { "EventId", "TemplateType" });

            // ========================================
            // 3. CREATE SentAdHocEmails TABLE
            // ========================================
            migrationBuilder.CreateTable(
                name: "SentAdHocEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    RecipientGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientEmails = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[] { }),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SendGridMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SentBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentAdHocEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentAdHocEmails_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SentAdHocEmails_Users_SentBy",
                        column: x => x.SentBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint("CHK_SentAdHocEmails_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.CheckConstraint("CHK_SentAdHocEmails_RecipientCount", "\"RecipientCount\" >= 0");
                    table.CheckConstraint("CHK_SentAdHocEmails_DeliveryStatus", "\"DeliveryStatus\" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced')");
                });

            // Indexes for SentAdHocEmails
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_SentAdHocEmails_EventId""
                ON ""SentAdHocEmails"" (""EventId"")
                WHERE ""EventId"" IS NOT NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_SentBy",
                table: "SentAdHocEmails",
                column: "SentBy");

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_SentAt",
                table: "SentAdHocEmails",
                column: "SentAt",
                descending: true);

            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_SentAdHocEmails_DeliveryStatus""
                ON ""SentAdHocEmails"" (""DeliveryStatus"")
                WHERE ""DeliveryStatus"" IN ('Pending', 'Failed');
            ");

            // ========================================
            // 4. MIGRATE VettingEmailTemplates → GlobalEmailTemplates
            // ========================================
            migrationBuilder.Sql(@"
                INSERT INTO ""GlobalEmailTemplates""
                    (""Id"", ""Category"", ""TemplateType"", ""Subject"", ""HtmlBody"", ""PlainTextBody"", ""Variables"", ""IsActive"", ""Version"", ""CreatedAt"", ""UpdatedAt"", ""UpdatedBy"")
                SELECT
                    ""Id"",
                    0 AS ""Category"",  -- EmailCategory.Vetting
                    CAST(""TemplateType"" AS VARCHAR(50)) AS ""TemplateType"",
                    ""Subject"",
                    ""HtmlBody"",
                    ""PlainTextBody"",
                    COALESCE(""Variables"", '[]'::jsonb) AS ""Variables"",
                    ""IsActive"",
                    ""Version"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""UpdatedBy""
                FROM ""VettingEmailTemplates""
                WHERE EXISTS (SELECT 1 FROM ""VettingEmailTemplates"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order
            migrationBuilder.DropTable(name: "SentAdHocEmails");
            migrationBuilder.DropTable(name: "EventEmailTemplates");
            migrationBuilder.DropTable(name: "GlobalEmailTemplates");
        }
    }
}
```

### Migration Seed Data (22 Default Templates)

**NOTE**: Seed data will be added via DatabaseSeeder service, NOT in migration.

Location: `/apps/api/Services/Seeding/EmailTemplateSeeder.cs` (NEW FILE)

```csharp
public class EmailTemplateSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, Guid adminUserId)
    {
        // Seed 6 Vetting templates (migrated)
        // Seed 7 Events templates (new)
        // Seed 4 Admin templates (new)
        // Seed 4 Incident templates (new)
        // Seed 1 Ad Hoc template (new)
    }
}
```

**Seed data details provided in [Seed Data Specifications](#seed-data-specifications) section below.**

---

## Index Strategy

### GlobalEmailTemplates Indexes

| Index Name | Column(s) | Type | Purpose | Justification |
|------------|-----------|------|---------|---------------|
| `PK_GlobalEmailTemplates` | Id | B-tree (PK) | Primary key lookup | Standard unique identifier |
| `UQ_GlobalEmailTemplates_Category_Type` | (Category, TemplateType) | B-tree (Unique) | Enforce one template per type per category | Business rule constraint |
| `IX_GlobalEmailTemplates_Category` | Category | B-tree | Filter templates by category | Admin UI loads all templates for specific category |
| `IX_GlobalEmailTemplates_UpdatedBy` | UpdatedBy | B-tree | Audit queries by user | Track who updated templates |
| `IX_GlobalEmailTemplates_UpdatedAt` | UpdatedAt DESC | B-tree | Recent changes queries | Admin dashboard shows recently modified templates |
| `IX_GlobalEmailTemplates_Variables_Gin` | Variables | GIN | JSONB containment queries | Fast variable lookup for validation |

**Expected Query Performance**:
- Category filter: O(log n) via B-tree index
- Variable containment: O(1) via GIN index
- Unique constraint enforcement: O(1) via unique index

### EventEmailTemplates Indexes

| Index Name | Column(s) | Type | Purpose | Justification |
|------------|-----------|------|---------|---------------|
| `PK_EventEmailTemplates` | Id | B-tree (PK) | Primary key lookup | Standard unique identifier |
| `UQ_EventEmailTemplates_EventId_Type` | (EventId, TemplateType) | B-tree (Unique) | Enforce one custom template per type per event | Business rule constraint |
| `IX_EventEmailTemplates_EventId` | EventId | B-tree | Load all templates for an event | Event edit page loads all templates |
| `IX_EventEmailTemplates_UpdatedBy` | UpdatedBy | B-tree | Audit queries by user | Track who customized templates |
| `IX_EventEmailTemplates_UpdatedAt` | UpdatedAt DESC | B-tree | Recent changes queries | Admin dashboard shows recently customized templates |
| `IX_EventEmailTemplates_EventId_TemplateType` | (EventId, TemplateType) | B-tree | Composite lookup for specific template | GET /api/events/{id}/email-templates/{type} |

**Expected Query Performance**:
- Event template load: O(log n) via EventId index
- Specific template lookup: O(1) via composite index
- Unique constraint enforcement: O(1) via unique index

### SentAdHocEmails Indexes

| Index Name | Column(s) | Type | Purpose | Justification |
|------------|-----------|------|---------|---------------|
| `PK_SentAdHocEmails` | Id | B-tree (PK) | Primary key lookup | Standard unique identifier |
| `IX_SentAdHocEmails_EventId` | EventId (WHERE NOT NULL) | B-tree (Partial) | Filter sent emails by event | Event admin history page |
| `IX_SentAdHocEmails_SentBy` | SentBy | B-tree | Audit queries by sender | Track who sent bulk emails |
| `IX_SentAdHocEmails_SentAt` | SentAt DESC | B-tree | Chronological history queries | Admin dashboard shows recent emails |
| `IX_SentAdHocEmails_DeliveryStatus` | DeliveryStatus (WHERE Pending/Failed) | B-tree (Partial) | Filter emails needing retry | SendGrid retry logic |

**Expected Query Performance**:
- Event-related email history: O(log n) via partial index (only non-null EventId)
- Failed email detection: O(1) via partial index (only Pending/Failed status)
- Chronological sorting: O(log n) via descending index

### Index Maintenance

**PostgreSQL Auto-Vacuum**:
- Enabled by default for all tables
- Threshold: 50 dead tuples + 20% of live tuples
- Monitoring: Check `pg_stat_user_tables` for last vacuum timestamps

**Index Bloat Prevention**:
- GIN indexes auto-compress via `autovacuum`
- B-tree indexes rebalanced automatically
- Partial indexes have minimal bloat (filtered subsets)

---

## Performance Considerations

### Query Optimization

**1. AsNoTracking for Read-Only Queries**

```csharp
// ✅ CORRECT - Read-only queries
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()
    .Where(t => t.Category == EmailCategory.Events && t.IsActive)
    .ToListAsync();

// ❌ WRONG - Tracking overhead for read-only operation
var templates = await _context.GlobalEmailTemplates
    .Where(t => t.Category == EmailCategory.Events && t.IsActive)
    .ToListAsync();
```

**Performance Impact**: 40% memory reduction, 30% faster queries

**2. Explicit Include for Relationships**

```csharp
// ✅ CORRECT - Explicit join
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()
    .Include(t => t.UpdatedByUser)
    .Where(t => t.Category == category)
    .ToListAsync();

// ❌ WRONG - N+1 query problem
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()
    .Where(t => t.Category == category)
    .ToListAsync();

foreach (var template in templates)
{
    var email = template.UpdatedByUser.Email;  // Separate query per template
}
```

**Performance Impact**: N+1 queries = 22 queries for 22 templates. Explicit Include = 1 query.

**3. Projection to DTO**

```csharp
// ✅ CORRECT - Project to DTO in database
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()
    .Where(t => t.Category == category && t.IsActive)
    .Select(t => new GlobalEmailTemplateDto
    {
        Id = t.Id,
        Category = t.Category.ToString(),
        Subject = t.Subject,
        // ... only needed fields
    })
    .ToListAsync();

// ❌ WRONG - Load full entities, then map
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()
    .Where(t => t.Category == category && t.IsActive)
    .ToListAsync();

var dtos = templates.Select(t => new GlobalEmailTemplateDto { ... });
```

**Performance Impact**: 50% less data transfer, faster serialization

### JSONB Performance

**Variables Field Optimization**:
- **Storage**: JSONB binary format (faster than JSON text)
- **Indexing**: GIN index supports containment queries (`@>`)
- **Query Pattern**: `WHERE Variables @> '["{{attendee_name}}"]'`

**Example Query**:
```sql
-- Find all Events templates that use {{event_title}} variable
SELECT "Id", "TemplateType", "Subject"
FROM "GlobalEmailTemplates"
WHERE "Category" = 1
  AND "Variables" @> '["{{event_title}}"]';
```

**Performance**: O(1) lookup via GIN index

### Caching Strategy

**Global Templates Caching**:
```csharp
// Cache global templates for 30 minutes
var cacheKey = $"email-templates-{category}";
var templates = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
    return await _context.GlobalEmailTemplates
        .AsNoTracking()
        .Where(t => t.Category == category && t.IsActive)
        .ToListAsync();
});
```

**Invalidation**: When admin updates global template, invalidate cache for that category.

**Event Templates Caching**:
- **NO caching** - frequently customized, event-specific
- Fetch fresh from database on each request

### Database Connection Pooling

**Npgsql Configuration**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=witchcityrope;Username=postgres;Password=xxx;Maximum Pool Size=20;Connection Lifetime=300;"
  }
}
```

**Settings**:
- **Maximum Pool Size**: 20 connections (production)
- **Connection Lifetime**: 300 seconds (5 minutes)
- **Pooling**: Enabled by default

---

## Security & Data Integrity

### Authorization Rules

**GlobalEmailTemplates**:
- **Read**: Administrator role required
- **Write**: Administrator role required
- **Delete**: Soft delete only (IsActive = false)

**EventEmailTemplates**:
- **Read**: Event creator OR Administrator
- **Write**: Event creator OR Administrator
- **Delete**: Event creator OR Administrator (hard delete allowed - reverts to global)

**SentAdHocEmails**:
- **Read**: Administrator role required
- **Write**: Administrator role required (send operation)
- **Delete**: NEVER allowed (permanent audit trail)

### Data Validation Constraints

**Database-Level Constraints**:
```sql
-- Subject: non-empty, max 200 characters
CHECK (LENGTH(TRIM("Subject")) > 0)

-- HtmlBody: non-empty
CHECK (LENGTH(TRIM("HtmlBody")) > 0)

-- PlainTextBody: non-empty
CHECK (LENGTH(TRIM("PlainTextBody")) > 0)

-- Category: valid enum values only
CHECK ("Category" IN (0, 1, 2, 3, 4))

-- Version: must be >= 1
CHECK ("Version" >= 1)

-- RecipientCount: must be >= 0
CHECK ("RecipientCount" >= 0)

-- DeliveryStatus: valid status strings only
CHECK ("DeliveryStatus" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced'))
```

**Application-Level Validation**:
- FluentValidation for request DTOs
- HTML sanitization before save (strip dangerous tags)
- Variable HTML-escaping before email send

### XSS Prevention

**HTML Sanitization** (Server-Side):
```csharp
private static string SanitizeHtml(string html)
{
    var sanitizer = new HtmlSanitizer();

    // Allow safe tags
    sanitizer.AllowedTags.Clear();
    sanitizer.AllowedTags.Add("p");
    sanitizer.AllowedTags.Add("h1");
    sanitizer.AllowedTags.Add("h2");
    sanitizer.AllowedTags.Add("h3");
    sanitizer.AllowedTags.Add("strong");
    sanitizer.AllowedTags.Add("em");
    sanitizer.AllowedTags.Add("a");
    sanitizer.AllowedTags.Add("ul");
    sanitizer.AllowedTags.Add("ol");
    sanitizer.AllowedTags.Add("li");
    sanitizer.AllowedTags.Add("br");

    // Strip dangerous tags
    sanitizer.AllowedTags.Remove("script");
    sanitizer.AllowedTags.Remove("iframe");
    sanitizer.AllowedTags.Remove("object");
    sanitizer.AllowedTags.Remove("embed");
    sanitizer.AllowedTags.Remove("form");

    return sanitizer.Sanitize(html);
}
```

**Variable Escaping** (Send-Time):
```csharp
private static string EscapeHtml(string value)
{
    return System.Net.WebUtility.HtmlEncode(value);
}

private static string RenderTemplate(string template, Dictionary<string, string> variables)
{
    var result = template;
    foreach (var kvp in variables)
    {
        var escapedValue = EscapeHtml(kvp.Value);
        result = result.Replace(kvp.Key, escapedValue);
    }
    return result;
}
```

### Cascade Delete Behaviors

**Event Deletion**:
```sql
-- EventEmailTemplates: CASCADE delete
-- When Event deleted, all EventEmailTemplates for that event are deleted
CONSTRAINT "FK_EventEmailTemplates_Events_EventId"
    FOREIGN KEY ("EventId") REFERENCES "Events" ("Id")
    ON DELETE CASCADE

-- SentAdHocEmails: SET NULL
-- When Event deleted, EventId set to NULL (preserve audit trail)
CONSTRAINT "FK_SentAdHocEmails_Events_EventId"
    FOREIGN KEY ("EventId") REFERENCES "Events" ("Id")
    ON DELETE SET NULL
```

**User Deletion**:
```sql
-- All tables: RESTRICT delete
-- Cannot delete user if they have updated templates or sent emails
CONSTRAINT "FK_GlobalEmailTemplates_Users_UpdatedBy"
    FOREIGN KEY ("UpdatedBy") REFERENCES "AspNetUsers" ("Id")
    ON DELETE RESTRICT
```

**Rationale**: Preserve audit trail. Use soft delete for users instead.

### Audit Trail

**GlobalEmailTemplates Audit**:
- **Version**: Increments on every update
- **UpdatedAt**: Timestamp of last modification (UTC)
- **UpdatedBy**: User ID who made changes
- **CreatedAt**: Original creation timestamp (UTC)

**EventEmailTemplates Audit**:
- **CreatedAt**: When organizer first customized template
- **UpdatedAt**: Last modification timestamp
- **UpdatedBy**: User ID who customized/updated

**SentAdHocEmails Audit**:
- **SentAt**: When email was sent (UTC)
- **SentBy**: User ID who sent email
- **RecipientEmails**: Full list of recipients (privacy consideration)
- **SendGridMessageId**: External tracking reference
- **DeliveryStatus**: Current delivery state

---

## Seed Data Specifications

### Template Categories and Types

**Total**: 22 default templates across 5 categories

#### Category 1: Vetting (6 templates)

**Migrated from VettingEmailTemplates table** - preserve existing templates

| Template Type | Subject | Variables |
|---------------|---------|-----------|
| ApplicationReceived | "Your vetting application - Application #{{application_number}}" | scene_name, application_number, application_date, contact_email |
| InterviewApproved | "Interview Scheduled - WitchCityRope Vetting" | scene_name, application_number, status_change_date, contact_email |
| Approved | "Welcome to WitchCityRope - You're Vetted!" | scene_name, application_number, status_change_date, contact_email |
| OnHold | "Vetting Application On Hold - Next Steps" | scene_name, application_number, status_change_date, contact_email, custom_message |
| Denied | "Vetting Application Status Update" | scene_name, application_number, status_change_date, contact_email |
| InterviewReminder | "Interview Reminder - {{submission_date}}" | scene_name, application_number, submission_date, contact_email, custom_message |

**Note**: Content copied from existing VettingEmailTemplates during migration.

#### Category 2: Events (7 templates)

| Template Type | Subject | Variables |
|---------------|---------|-----------|
| Confirmation | "Your ticket for {{event_title}}" | attendee_name, event_title, event_date, event_time, venue_name, venue_address, ticket_type, total_paid, confirmation_number, organizer_email |
| Reminder1Week | "One week until {{event_title}}" | attendee_name, event_title, event_date, event_time, venue_name, venue_address |
| Reminder1Day | "Tomorrow: {{event_title}}" | attendee_name, event_title, event_date, event_time, venue_name, venue_address |
| Reminder2Hours | "Starting soon: {{event_title}}" | attendee_name, event_title, event_time, venue_name |
| Cancellation | "Event Cancelled: {{event_title}}" | attendee_name, event_title, event_date, organizer_email, custom_message |
| SessionChange | "Session Update: {{event_title}}" | attendee_name, event_title, session_name, event_date, event_time, custom_message |
| ThankYou | "Thank you for attending {{event_title}}" | attendee_name, event_title, event_date, organizer_email |

**Default Content**: Professional, community-focused, includes all relevant event details.

#### Category 3: Admin (4 templates)

| Template Type | Subject | Variables |
|---------------|---------|-----------|
| AccountCreated | "Welcome to WitchCityRope - Account Created" | user_name, account_email, system_url, support_email |
| PasswordReset | "Password Reset Request - WitchCityRope" | user_name, system_url, support_email |
| RoleChanged | "Your WitchCityRope Role Has Been Updated" | user_name, action_required, support_email |
| SystemNotification | "WitchCityRope System Notification" | user_name, action_required, deadline_date, support_email |

**Default Content**: Clear, actionable, includes support contact.

#### Category 4: Incident (4 templates)

| Template Type | Subject | Variables |
|---------------|---------|-----------|
| ReportReceived | "Incident Report Received - #{{incident_number}}" | reporter_name, incident_number, incident_date, coordinator_name, next_steps |
| StatusUpdate | "Incident #{{incident_number}} Status Update" | reporter_name, incident_number, status, next_steps |
| AssignmentNotification | "You've been assigned to Incident #{{incident_number}}" | coordinator_name, incident_number, incident_date, next_steps |
| Resolved | "Incident #{{incident_number}} Resolved" | reporter_name, incident_number, incident_date, next_steps |

**Default Content**: Professional, empathetic, clear next steps.

#### Category 5: Ad Hoc (1 template)

| Template Type | Subject | Variables |
|---------------|---------|-----------|
| General | "Message from WitchCityRope" | recipient_name, custom_content |

**Default Content**: Minimal template for ad-hoc communications.

### Seed Data Implementation

**Location**: `/apps/api/Services/Seeding/EmailTemplateSeeder.cs`

```csharp
public class EmailTemplateSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateSeeder> _logger;

    public async Task SeedAsync(Guid adminUserId, CancellationToken cancellationToken = default)
    {
        // Check if migration already populated Vetting templates
        var existingVettingTemplates = await _context.GlobalEmailTemplates
            .Where(t => t.Category == EmailCategory.Vetting)
            .CountAsync(cancellationToken);

        if (existingVettingTemplates > 0)
        {
            _logger.LogInformation("Vetting templates already migrated, skipping...");
        }

        // Seed Events templates (7)
        await SeedEventsTemplatesAsync(adminUserId, cancellationToken);

        // Seed Admin templates (4)
        await SeedAdminTemplatesAsync(adminUserId, cancellationToken);

        // Seed Incident templates (4)
        await SeedIncidentTemplatesAsync(adminUserId, cancellationToken);

        // Seed Ad Hoc template (1)
        await SeedAdHocTemplateAsync(adminUserId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEventsTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Id = Guid.NewGuid(),
                Category = EmailCategory.Events,
                TemplateType = "Confirmation",
                Subject = "Your ticket for {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Thank you for registering for <strong>{{event_title}}</strong>!</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p><strong>Ticket Type:</strong> {{ticket_type}}<br><strong>Total Paid:</strong> {{total_paid}}<br><strong>Confirmation Number:</strong> {{confirmation_number}}</p><p>We look forward to seeing you!</p><p>Questions? Email {{organizer_email}}</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThank you for registering for {{event_title}}!\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nTicket Type: {{ticket_type}}\nTotal Paid: {{total_paid}}\nConfirmation Number: {{confirmation_number}}\n\nWe look forward to seeing you!\n\nQuestions? Email {{organizer_email}}",
                Variables = "[\"{{attendee_name}}\",\"{{event_title}}\",\"{{event_date}}\",\"{{event_time}}\",\"{{venue_name}}\",\"{{venue_address}}\",\"{{ticket_type}}\",\"{{total_paid}}\",\"{{confirmation_number}}\",\"{{organizer_email}}\"]",
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            // ... additional 6 Events templates
        };

        foreach (var template in templates)
        {
            var exists = await _context.GlobalEmailTemplates
                .AnyAsync(t => t.Category == template.Category && t.TemplateType == template.TemplateType, cancellationToken);

            if (!exists)
            {
                await _context.GlobalEmailTemplates.AddAsync(template, cancellationToken);
            }
        }
    }

    // Similar methods for Admin, Incident, Ad Hoc categories...
}
```

---

## Testing Data

### Sample Test Records

**GlobalEmailTemplate Test Record**:
```csharp
var testTemplate = new GlobalEmailTemplate
{
    Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
    Category = EmailCategory.Events,
    TemplateType = "TestConfirmation",
    Subject = "Test: Your ticket for {{event_title}}",
    HtmlBody = "<p>Test HTML body</p>",
    PlainTextBody = "Test plain text body",
    Variables = "[\"{{attendee_name}}\",\"{{event_title}}\"]",
    IsActive = true,
    Version = 1,
    CreatedAt = new DateTime(2025, 11, 9, 10, 0, 0, DateTimeKind.Utc),
    UpdatedAt = new DateTime(2025, 11, 9, 10, 0, 0, DateTimeKind.Utc),
    UpdatedBy = adminUserId
};
```

**EventEmailTemplate Test Record** (Copy-on-Edit):
```csharp
var testEventTemplate = new EventEmailTemplate
{
    Id = Guid.NewGuid(),
    EventId = eventId,
    GlobalTemplateId = globalTemplateId,
    TemplateType = "Confirmation",
    Subject = "Your Advanced Harnesses Workshop Ticket - Pre-Class Homework",
    HtmlBody = "<p>Custom HTML with pre-class instructions</p>",
    PlainTextBody = "Custom plain text with pre-class instructions",
    TargetSessions = new[] { "all" },
    IsCustomized = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    UpdatedBy = teacherUserId
};
```

**SentAdHocEmail Test Record**:
```csharp
var testSentEmail = new SentAdHocEmail
{
    Id = Guid.NewGuid(),
    Subject = "Parking Update for Rope Performance Night",
    HtmlBody = "<p>Free parking available at Essex Street Garage</p>",
    PlainTextBody = "Free parking available at Essex Street Garage",
    RecipientGroup = "all-tickets",
    RecipientEmails = new[] { "attendee1@example.com", "attendee2@example.com" },
    RecipientCount = 2,
    EventId = eventId,
    SendGridMessageId = "sg_abc123xyz",
    DeliveryStatus = "Sent",
    SentAt = DateTime.UtcNow,
    SentBy = adminUserId
};
```

### Test Scenarios

**1. GlobalEmailTemplate Unique Constraint Test**:
```csharp
// Attempt to create duplicate (Category, TemplateType)
var template1 = new GlobalEmailTemplate { Category = EmailCategory.Events, TemplateType = "Confirmation", ... };
await _context.GlobalEmailTemplates.AddAsync(template1);
await _context.SaveChangesAsync();  // Success

var template2 = new GlobalEmailTemplate { Category = EmailCategory.Events, TemplateType = "Confirmation", ... };
await _context.GlobalEmailTemplates.AddAsync(template2);
await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());  // Unique constraint violation
```

**2. EventEmailTemplate Cascade Delete Test**:
```csharp
// Create Event and EventEmailTemplate
var eventEntity = new Event { Id = Guid.NewGuid(), ... };
await _context.Events.AddAsync(eventEntity);

var template = new EventEmailTemplate { EventId = eventEntity.Id, ... };
await _context.EventEmailTemplates.AddAsync(template);
await _context.SaveChangesAsync();

// Delete Event
_context.Events.Remove(eventEntity);
await _context.SaveChangesAsync();

// Verify EventEmailTemplate also deleted (CASCADE)
var templateExists = await _context.EventEmailTemplates.AnyAsync(t => t.Id == template.Id);
Assert.False(templateExists);
```

**3. SentAdHocEmail SET NULL Test**:
```csharp
// Create Event and SentAdHocEmail
var eventEntity = new Event { Id = Guid.NewGuid(), ... };
await _context.Events.AddAsync(eventEntity);

var sentEmail = new SentAdHocEmail { EventId = eventEntity.Id, ... };
await _context.SentAdHocEmails.AddAsync(sentEmail);
await _context.SaveChangesAsync();

// Delete Event
_context.Events.Remove(eventEntity);
await _context.SaveChangesAsync();

// Verify SentAdHocEmail EventId set to NULL (SET NULL)
var email = await _context.SentAdHocEmails.FindAsync(sentEmail.Id);
Assert.Null(email.EventId);
```

**4. JSONB GIN Index Performance Test**:
```csharp
// Query templates that use specific variable
var templates = await _context.GlobalEmailTemplates
    .FromSqlRaw(@"
        SELECT * FROM ""GlobalEmailTemplates""
        WHERE ""Variables"" @> '[""{{event_title}}""]'
    ")
    .ToListAsync();

// Should use IX_GlobalEmailTemplates_Variables_Gin index (verify with EXPLAIN ANALYZE)
```

---

## Summary

This database design provides a robust, scalable foundation for centralized email template management across all WitchCityRope categories. Key achievements:

**✅ Centralization**: Single GlobalEmailTemplates table for all 5 categories
**✅ Copy-on-Edit**: EventEmailTemplates created only when needed (minimizes storage)
**✅ Audit Trail**: Complete version tracking and user attribution
**✅ Performance**: Strategic indexes (B-tree + GIN) for fast queries
**✅ Data Integrity**: Unique constraints, check constraints, proper cascade behaviors
**✅ PostgreSQL Optimization**: JSONB with GIN indexes, timestamptz, array types
**✅ Security**: RESTRICT delete on users, SET NULL for optional relationships
**✅ Migration Strategy**: Preserves existing Vetting templates, seeds new defaults

**Next Steps for Backend Developer**:
1. Implement Entity Framework entities and configurations (copy from this document)
2. Create migration using provided DDL scripts
3. Implement EmailTemplateSeeder for 22 default templates
4. Verify migration success and seed data population
5. Create API services following vertical slice pattern

**Critical Files to Create**:
- `/apps/api/Features/EmailTemplates/Entities/GlobalEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/SentAdHocEmail.cs`
- `/apps/api/Features/EmailTemplates/Entities/Configuration/*.cs` (3 files)
- `/apps/api/Data/Migrations/YYYYMMDDHHMMSS_AddEmailTemplatesSystem.cs`
- `/apps/api/Services/Seeding/EmailTemplateSeeder.cs`

**Database Design Complete** ✅
