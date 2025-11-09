# Functional Specification: Email Templates Admin Management
<!-- Last Updated: 2025-11-09 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft - Ready for Database Design & Implementation -->

## Executive Summary

This functional specification defines the complete technical implementation for centralizing email template management across all WitchCityRope categories (Vetting, Events, Admin, Incident, Ad Hoc). The system will provide a unified admin interface for managing global templates while allowing event organizers to customize event-specific templates using a copy-on-edit pattern.

**Key Technical Achievements**:
- Centralized template management via `/admin/email-templates` with tabbed interface
- Copy-on-edit pattern minimizes database storage (event-specific templates created only when customized)
- Type-safe enum system prevents runtime errors
- NSwag auto-generation ensures frontend/backend type alignment
- Variable validation provides warnings without blocking saves
- Complete audit trail with version control and user tracking

---

## Architecture Discovery Phase (MANDATORY PHASE 0)

### Documents Reviewed

**1. DTO Alignment Strategy** - `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- Lines 85-110: NSwag auto-generation is THE solution for type generation
- Lines 127-139: Frontend developers MUST use @witchcityrope/shared-types, NEVER manual interfaces
- Lines 229-240: CRITICAL violation examples - manual DTO interfaces forbidden

**2. Domain Layer Architecture** - `/docs/architecture/react-migration/domain-layer-architecture.md`
- Lines 144-179: packages/shared-types structure with NSwag pipeline
- Lines 725-829: NSwag configuration details and generation process
- Lines 756-826: Generation pipeline: OpenAPI → NSwag → TypeScript types

**3. API Architecture Overview** - `/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`
- Lines 85-134: Simple vertical slice service pattern (direct Entity Framework, NO MediatR)
- Lines 141-172: Minimal API endpoint registration patterns
- Lines 203-254: Database access patterns (AsNoTracking for reads, tracked entities for writes)

**4. Existing Email Template Architecture** - `/session-work/2025-11-09/email-template-architecture-exploration.md`
- Lines 23-51: VettingEmailTemplate entity structure (proven pattern to replicate)
- Lines 132-165: VettingEmailService SendGrid integration patterns
- Lines 169-182: Variable substitution with `{{variable_name}}` syntax
- Lines 230-278: React admin page with MantineTiptapEditor integration

### Existing Solutions Found

**1. VettingEmailTemplate Pattern** (Lines 23-51 in email-template-architecture-exploration.md):
- Database-driven templates with JSONB variables field
- Version tracking and audit trail (CreatedAt, UpdatedAt, UpdatedBy)
- IsActive flag for soft deletes
- Unique constraint on TemplateType

**2. SendGrid Integration** (Lines 132-165):
- Configuration-driven email service with mock mode fallback
- Email logging to VettingEmailLog table with retry mechanism
- SendGrid message ID capture for delivery tracking
- Conditional initialization based on appsettings

**3. MantineTiptapEditor Integration** (Lines 230-278):
- React admin page with template table + editor panel
- Subject line TextInput + MantineTiptapEditor for HTML body
- Variable reference display for category-specific variables
- Save/Cancel buttons with loading states

**4. NSwag Type Generation Pipeline** (Lines 756-826 in domain-layer-architecture.md):
- OpenAPI specification generated from API
- NSwag reads spec and generates TypeScript interfaces
- Auto-generated types in packages/shared-types/src/generated/
- Frontend imports from @witchcityrope/shared-types package

### Verification Statement

"Confirmed no existing solution provides multi-category email template management. VettingEmailTemplate system is single-category only. New GlobalEmailTemplates table required to support 5 categories (Vetting, Events, Admin, Incident, Ad Hoc) with shared architecture patterns."

---

## System Architecture

### Microservices Architecture Context

**CRITICAL**: WitchCityRope uses Web+API microservices architecture:
- **Web Service** (React + Vite): UI at http://localhost:5173
- **API Service** (.NET Minimal API): Business logic at http://localhost:5655
- **Database** (PostgreSQL): localhost:5434
- **Pattern**: Web → HTTP → API → Database (NEVER Web → Database directly)

### Email Template System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        WEB SERVICE (React)                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  /admin/email-templates (Admin UI)                             │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Tabbed Interface: Vetting | Events | Admin | ...      │    │
│  │                                                          │    │
│  │ EmailCategoryPanel                                      │    │
│  │   ├─ Template Cards (horizontal scroll)                │    │
│  │   └─ Editor Panel (MantineTiptapEditor)                │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                       │
│                          │ HTTP Requests                        │
│                          ▼                                       │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ GET /api/email-templates?category=events
                           │ PUT /api/email-templates/{id}
                           │ DELETE /api/events/{eventId}/email-templates/{type}
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API SERVICE (.NET)                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  /api/email-templates/* Endpoints                              │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ GlobalEmailTemplateService                              │    │
│  │   ├─ GetByCategory(category)                           │    │
│  │   ├─ GetByCategoryAndType(category, type)              │    │
│  │   └─ Update(id, request)                               │    │
│  │                                                          │    │
│  │ EventEmailTemplateService                               │    │
│  │   ├─ GetForEvent(eventId)                              │    │
│  │   ├─ UpdateEventTemplate(eventId, type, request)       │    │
│  │   └─ ResetToDefault(eventId, type) [DELETE]            │    │
│  │                                                          │    │
│  │ VariableValidationService                               │    │
│  │   └─ ValidateVariables(htmlBody, allowedVariables)     │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                       │
│                          │ Entity Framework Core 9              │
│                          ▼                                       │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ SQL Queries
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DATABASE (PostgreSQL)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  GlobalEmailTemplates (22 default templates)                   │
│    └─ Unique: (Category, TemplateType)                         │
│                                                                  │
│  EventEmailTemplates (event-specific overrides)                │
│    └─ Unique: (EventId, TemplateType)                          │
│                                                                  │
│  SentAdHocEmails (audit trail)                                 │
│    └─ No unique constraints (can send multiple)                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Component Organization (Vertical Slice Architecture)

```
apps/api/Features/EmailTemplates/
├── Entities/
│   ├── GlobalEmailTemplate.cs
│   ├── EventEmailTemplate.cs
│   ├── SentAdHocEmail.cs
│   └── Configuration/
│       ├── GlobalEmailTemplateConfiguration.cs
│       ├── EventEmailTemplateConfiguration.cs
│       └── SentAdHocEmailConfiguration.cs
├── Services/
│   ├── IGlobalEmailTemplateService.cs
│   ├── GlobalEmailTemplateService.cs
│   ├── IEventEmailTemplateService.cs
│   ├── EventEmailTemplateService.cs
│   ├── IAdHocEmailService.cs
│   ├── AdHocEmailService.cs
│   └── VariableValidationService.cs
├── Models/
│   ├── Requests/
│   │   ├── UpdateGlobalTemplateRequest.cs
│   │   ├── UpdateEventTemplateRequest.cs
│   │   └── SendAdHocEmailRequest.cs
│   └── Responses/
│       ├── GlobalEmailTemplateDto.cs
│       ├── EventEmailTemplateDto.cs
│       └── SentAdHocEmailDto.cs
├── Endpoints/
│   └── EmailTemplateEndpoints.cs
└── Enums/
    ├── EmailCategory.cs
    ├── VettingTemplateType.cs
    ├── EventTemplateType.cs
    ├── AdminTemplateType.cs
    ├── IncidentTemplateType.cs
    └── AdHocTemplateType.cs

apps/web/src/features/admin/email-templates/
├── pages/
│   └── EmailTemplatesAdminPage.tsx
├── components/
│   ├── EmailCategoryPanel.tsx
│   ├── TemplateCard.tsx
│   ├── TemplateEditor.tsx
│   └── ResetConfirmationModal.tsx
├── hooks/
│   ├── useEmailTemplates.ts (React Query)
│   └── useVariableValidation.ts
├── services/
│   └── emailTemplates.api.ts
└── types/
    └── emailTemplates.types.ts (imports from @witchcityrope/shared-types)
```

---

## Data Models

### Database Schema

#### Table 1: GlobalEmailTemplates

```sql
CREATE TABLE "GlobalEmailTemplates" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Category and Type
    "Category" INTEGER NOT NULL,  -- Enum: 0=Vetting, 1=Events, 2=Admin, 3=Incident, 4=AdHoc
    "TemplateType" VARCHAR(50) NOT NULL,  -- Enum value as string (e.g., "Confirmation")

    -- Content
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Metadata
    "Variables" JSONB NOT NULL DEFAULT '{}'::jsonb,  -- Available variables for this template
    "IsActive" BOOLEAN NOT NULL DEFAULT true,

    -- Audit
    "Version" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedBy" UUID NOT NULL,

    -- Foreign Keys
    CONSTRAINT "FK_GlobalEmailTemplates_Users_UpdatedBy"
        FOREIGN KEY ("UpdatedBy") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,

    -- Unique Constraints
    CONSTRAINT "UQ_GlobalEmailTemplates_Category_Type"
        UNIQUE ("Category", "TemplateType")
);

-- Indexes
CREATE INDEX "IX_GlobalEmailTemplates_Category" ON "GlobalEmailTemplates" ("Category");
CREATE INDEX "IX_GlobalEmailTemplates_UpdatedBy" ON "GlobalEmailTemplates" ("UpdatedBy");
CREATE INDEX "IX_GlobalEmailTemplates_Variables" ON "GlobalEmailTemplates" USING GIN ("Variables");
```

**Business Rules**:
- Unique constraint ensures only ONE template per type per category
- Subject max length: 200 characters (enforced in database + validation)
- HtmlBody sanitized on save (strip `<script>`, `<iframe>`, `<object>`, `<embed>`)
- PlainTextBody auto-generated from HtmlBody if not provided
- Version increments on every update (audit trail)
- IsActive = false hides template (soft delete, NEVER hard delete global templates)
- Variables stored as JSONB for PostgreSQL-optimized querying

#### Table 2: EventEmailTemplates

```sql
CREATE TABLE "EventEmailTemplates" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Relationships
    "EventId" UUID NOT NULL,
    "GlobalTemplateId" UUID NOT NULL,  -- Reference only, NOT foreign key constraint

    -- Template Info
    "TemplateType" VARCHAR(50) NOT NULL,  -- e.g., "Confirmation", "Reminder1Day"

    -- Content (override of global)
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Configuration
    "TargetSessions" TEXT[] NOT NULL DEFAULT '{}',  -- ['all'] or ['S1', 'S2']
    "RecipientGroup" VARCHAR(100) NULL,  -- For future use

    -- Metadata
    "IsCustomized" BOOLEAN NOT NULL DEFAULT true,  -- Always true for event-specific
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedBy" UUID NOT NULL,

    -- Foreign Keys
    CONSTRAINT "FK_EventEmailTemplates_Events_EventId"
        FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventEmailTemplates_Users_UpdatedBy"
        FOREIGN KEY ("UpdatedBy") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,

    -- Unique Constraints
    CONSTRAINT "UQ_EventEmailTemplates_EventId_Type"
        UNIQUE ("EventId", "TemplateType")
);

-- Indexes
CREATE INDEX "IX_EventEmailTemplates_EventId" ON "EventEmailTemplates" ("EventId");
CREATE INDEX "IX_EventEmailTemplates_UpdatedBy" ON "EventEmailTemplates" ("UpdatedBy");
```

**Business Rules**:
- Created ONLY when event organizer saves custom template (copy-on-edit)
- Deleting this record = "Reset to Default" (revert to global template)
- GlobalTemplateId for reference only (NOT enforced foreign key - global template might change/delete)
- TargetSessions for multi-session events (e.g., only send confirmation to Session 1 attendees)
- CASCADE delete: Deleting Event deletes all EventEmailTemplates for that event
- IsCustomized always true (event-specific templates are by definition customized)

#### Table 3: SentAdHocEmails

```sql
CREATE TABLE "SentAdHocEmails" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Email Details
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,

    -- Recipients
    "RecipientGroup" VARCHAR(100) NOT NULL,  -- e.g., "all-tickets", "session-1", "volunteers"
    "RecipientEmails" TEXT[] NOT NULL DEFAULT '{}',  -- Actual emails sent to
    "RecipientCount" INTEGER NOT NULL,

    -- Context
    "EventId" UUID NULL,  -- Nullable - may not be event-related

    -- SendGrid
    "SendGridMessageId" VARCHAR(100) NULL,
    "DeliveryStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Sent, Delivered, Failed

    -- Audit
    "SentAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "SentBy" UUID NOT NULL,

    -- Foreign Keys
    CONSTRAINT "FK_SentAdHocEmails_Events_EventId"
        FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SentAdHocEmails_Users_SentBy"
        FOREIGN KEY ("SentBy") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

-- Indexes
CREATE INDEX "IX_SentAdHocEmails_EventId" ON "SentAdHocEmails" ("EventId");
CREATE INDEX "IX_SentAdHocEmails_SentBy" ON "SentAdHocEmails" ("SentBy");
CREATE INDEX "IX_SentAdHocEmails_SentAt" ON "SentAdHocEmails" ("SentAt" DESC);
```

**Business Rules**:
- Read-only after creation (NO edits to sent emails)
- RecipientEmails array stores actual email addresses (for audit, privacy considerations)
- DeliveryStatus updated via SendGrid webhooks (future enhancement)
- EventId nullable (admin might send ad-hoc email to "all vetted members" - not event-related)
- NEVER deleted - permanent audit trail
- No unique constraints (can send multiple ad-hoc emails to same group)

### Entity Models (C#)

#### GlobalEmailTemplate Entity

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
    [Key]
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
    public string Variables { get; set; } = "{}";

    /// <summary>
    /// Soft delete flag (false = hidden, never hard delete)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number (increments on each update for audit trail)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated
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

#### EventEmailTemplate Entity

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
    [Key]
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
    /// Timestamp when template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated
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

#### SentAdHocEmail Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a sent ad-hoc email with full audit trail.
/// Read-only after creation (never modified or deleted).
/// </summary>
public class SentAdHocEmail
{
    [Key]
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
    /// Delivery status: Pending, Sent, Delivered, Failed
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    /// <summary>
    /// Timestamp when email was sent
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

### DTOs and Requests/Responses

#### GlobalEmailTemplateDto (Response)

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Models.Responses;

/// <summary>
/// Response DTO for global email template
/// Auto-generates TypeScript interface via NSwag
/// </summary>
public class GlobalEmailTemplateDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Email category: Vetting, Events, Admin, Incident, AdHoc
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Template type within category (e.g., "Confirmation")
    /// </summary>
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// Display-friendly name for template type
    /// </summary>
    public string TemplateTypeName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Available variables for this template
    /// Example: ["{{attendee_name}}", "{{event_title}}"]
    /// </summary>
    public string[] Variables { get; set; } = Array.Empty<string>();

    public bool IsActive { get; set; }
    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
```

#### UpdateGlobalTemplateRequest (Request)

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models.Requests;

/// <summary>
/// Request model for updating global email template
/// </summary>
public class UpdateGlobalTemplateRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;
}
```

#### EventEmailTemplateDto (Response)

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Models.Responses;

/// <summary>
/// Response DTO for event-specific email template
/// </summary>
public class EventEmailTemplateDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid GlobalTemplateId { get; set; }

    public string TemplateType { get; set; } = string.Empty;
    public string TemplateTypeName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public string[] TargetSessions { get; set; } = Array.Empty<string>();
    public bool IsCustomized { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
    public string UpdatedByEmail { get; set; } = string.Empty;
}
```

#### UpdateEventTemplateRequest (Request)

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models.Requests;

/// <summary>
/// Request model for updating/creating event-specific email template
/// </summary>
public class UpdateEventTemplateRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Target sessions for this template
    /// ["all"] or ["S1", "S2", ...]
    /// </summary>
    public string[] TargetSessions { get; set; } = new[] { "all" };
}
```

#### SendAdHocEmailRequest (Request)

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models.Requests;

/// <summary>
/// Request model for sending ad-hoc email
/// </summary>
public class SendAdHocEmailRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTML body is required")]
    public string HtmlBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plain text body is required")]
    public string PlainTextBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "Recipient group is required")]
    [MaxLength(100)]
    public string RecipientGroup { get; set; } = string.Empty;

    /// <summary>
    /// Optional event ID if email is event-related
    /// </summary>
    public Guid? EventId { get; set; }
}
```

#### SentAdHocEmailDto (Response)

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Models.Responses;

/// <summary>
/// Response DTO for sent ad-hoc email
/// </summary>
public class SentAdHocEmailDto
{
    public Guid Id { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    public string RecipientGroup { get; set; } = string.Empty;
    public int RecipientCount { get; set; }

    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }

    public string? SendGridMessageId { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public Guid SentBy { get; set; }
    public string SentByEmail { get; set; } = string.Empty;
}
```

---

## API Specifications

### Endpoint Summary

| Method | Endpoint | Authorization | Purpose |
|--------|----------|---------------|---------|
| **Global Templates** ||||
| GET | `/api/email-templates?category={category}` | Admin | List templates by category |
| GET | `/api/email-templates/{id}` | Admin | Get single global template |
| PUT | `/api/email-templates/{id}` | Admin | Update global template |
| **Event Templates** ||||
| GET | `/api/events/{eventId}/email-templates` | Event Organizer | Get all event templates (global + overrides) |
| GET | `/api/events/{eventId}/email-templates/{type}` | Event Organizer | Get specific event template |
| PUT | `/api/events/{eventId}/email-templates/{type}` | Event Organizer | Create/update event template |
| DELETE | `/api/events/{eventId}/email-templates/{type}` | Event Organizer | Reset to default (delete override) |
| **Ad Hoc Emails** ||||
| POST | `/api/email-templates/ad-hoc` | Admin | Send ad-hoc email |
| GET | `/api/email-templates/ad-hoc/history?eventId={id}` | Admin | Get sent ad-hoc history |
| GET | `/api/email-templates/ad-hoc/history/{id}` | Admin | Get specific sent email |

### Endpoint Implementations

#### GET /api/email-templates?category={category}

**Purpose**: Retrieve all global templates for a specific category

**Authorization**: Administrator role required

**Request**:
```http
GET /api/email-templates?category=events
```

**Query Parameters**:
- `category` (required): EmailCategory enum value ("vetting", "events", "admin", "incident", "adhoc")

**Response** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "category": "Events",
    "templateType": "Confirmation",
    "templateTypeName": "Confirmation Email",
    "subject": "Your ticket for {{event_title}}",
    "htmlBody": "<p>Hi {{attendee_name}},</p><p>Thank you for registering...</p>",
    "plainTextBody": "Hi {{attendee_name}},\n\nThank you for registering...",
    "variables": ["{{attendee_name}}", "{{event_title}}", "{{event_date}}"],
    "isActive": true,
    "version": 2,
    "createdAt": "2025-11-09T10:00:00Z",
    "updatedAt": "2025-11-09T14:30:00Z",
    "updatedBy": "a1b2c3d4-...",
    "updatedByEmail": "admin@witchcityrope.com"
  }
]
```

**Response** (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid category",
  "status": 400,
  "detail": "Category 'invalid' is not a valid EmailCategory value"
}
```

**Implementation Notes**:
- Filter by Category enum
- Order by TemplateType
- Include UpdatedByUser relationship
- Map to GlobalEmailTemplateDto

#### GET /api/email-templates/{id}

**Purpose**: Retrieve single global template by ID

**Authorization**: Administrator role required

**Request**:
```http
GET /api/email-templates/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "Events",
  "templateType": "Confirmation",
  "templateTypeName": "Confirmation Email",
  "subject": "Your ticket for {{event_title}}",
  "htmlBody": "<p>Hi {{attendee_name}},</p>",
  "plainTextBody": "Hi {{attendee_name}},",
  "variables": ["{{attendee_name}}", "{{event_title}}"],
  "isActive": true,
  "version": 1,
  "createdAt": "2025-11-09T10:00:00Z",
  "updatedAt": "2025-11-09T10:00:00Z",
  "updatedBy": "a1b2c3d4-...",
  "updatedByEmail": "admin@witchcityrope.com"
}
```

**Response** (404 Not Found):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Template not found",
  "status": 404,
  "detail": "Template with ID '3fa85f64-...' not found"
}
```

#### PUT /api/email-templates/{id}

**Purpose**: Update global template content

**Authorization**: Administrator role required

**Request**:
```http
PUT /api/email-templates/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "subject": "Updated: Your ticket for {{event_title}}",
  "htmlBody": "<p>Hi {{attendee_name}},</p><p>Updated content...</p>",
  "plainTextBody": "Hi {{attendee_name}},\n\nUpdated content..."
}
```

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "Events",
  "templateType": "Confirmation",
  "templateTypeName": "Confirmation Email",
  "subject": "Updated: Your ticket for {{event_title}}",
  "htmlBody": "<p>Hi {{attendee_name}},</p><p>Updated content...</p>",
  "plainTextBody": "Hi {{attendee_name}},\n\nUpdated content...",
  "variables": ["{{attendee_name}}", "{{event_title}}"],
  "isActive": true,
  "version": 2,  // Incremented
  "createdAt": "2025-11-09T10:00:00Z",
  "updatedAt": "2025-11-09T14:45:00Z",  // Updated timestamp
  "updatedBy": "a1b2c3d4-...",
  "updatedByEmail": "admin@witchcityrope.com"
}
```

**Response** (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Subject": ["Subject is required"],
    "HtmlBody": ["HTML body is required"]
  }
}
```

**Implementation Notes**:
- Validate request using FluentValidation
- Sanitize HtmlBody (strip dangerous tags: `<script>`, `<iframe>`, etc.)
- Increment Version number
- Update UpdatedAt timestamp and UpdatedBy user ID
- Return updated template with new version

#### GET /api/events/{eventId}/email-templates

**Purpose**: Get all email templates for an event (global + event-specific)

**Authorization**: Event organizer (creator or admin)

**Request**:
```http
GET /api/events/7c9e6679-7425-40de-944b-e07fc1f90ae7/email-templates
```

**Response** (200 OK):
```json
[
  {
    "id": "event-specific-id",
    "eventId": "7c9e6679-...",
    "globalTemplateId": "global-id",
    "templateType": "Confirmation",
    "templateTypeName": "Confirmation Email",
    "subject": "Your Advanced Harnesses Workshop Ticket",
    "htmlBody": "<p>Hi {{attendee_name}},</p><p>**Pre-Class Homework**...</p>",
    "plainTextBody": "Hi {{attendee_name}},\n\n**Pre-Class Homework**...",
    "targetSessions": ["all"],
    "isCustomized": true,  // Event-specific
    "createdAt": "2025-11-09T12:00:00Z",
    "updatedAt": "2025-11-09T12:00:00Z",
    "updatedBy": "teacher-id",
    "updatedByEmail": "teacher@witchcityrope.com"
  },
  {
    "id": "global-id",
    "eventId": "7c9e6679-...",
    "globalTemplateId": "global-id",
    "templateType": "Reminder1Day",
    "templateTypeName": "Reminder - 1 Day Before",
    "subject": "Tomorrow: {{event_title}}",
    "htmlBody": "<p>Reminder...</p>",
    "plainTextBody": "Reminder...",
    "targetSessions": ["all"],
    "isCustomized": false,  // Using global default
    "createdAt": "2025-11-09T10:00:00Z",
    "updatedAt": "2025-11-09T10:00:00Z",
    "updatedBy": "admin-id",
    "updatedByEmail": "admin@witchcityrope.com"
  }
]
```

**Implementation Notes**:
- Fetch all global templates for Events category
- Fetch all event-specific templates for this eventId
- Merge: If event-specific exists for a type, use it; otherwise use global
- Set isCustomized = true if event-specific, false if global
- Authorization: Check if current user is event creator or has Administrator role

#### PUT /api/events/{eventId}/email-templates/{type}

**Purpose**: Create or update event-specific template (copy-on-edit)

**Authorization**: Event organizer (creator or admin)

**Request**:
```http
PUT /api/events/7c9e6679-7425-40de-944b-e07fc1f90ae7/email-templates/Confirmation
Content-Type: application/json

{
  "subject": "Your {{event_title}} Ticket - Pre-Class Homework",
  "htmlBody": "<p>Hi {{attendee_name}},</p><p>**Pre-Class Homework**...</p>",
  "plainTextBody": "Hi {{attendee_name}},\n\n**Pre-Class Homework**...",
  "targetSessions": ["all"]
}
```

**Response** (200 OK):
```json
{
  "id": "new-event-template-id",
  "eventId": "7c9e6679-...",
  "globalTemplateId": "global-id",
  "templateType": "Confirmation",
  "templateTypeName": "Confirmation Email",
  "subject": "Your {{event_title}} Ticket - Pre-Class Homework",
  "htmlBody": "<p>Hi {{attendee_name}},</p><p>**Pre-Class Homework**...</p>",
  "plainTextBody": "Hi {{attendee_name}},\n\n**Pre-Class Homework**...",
  "targetSessions": ["all"],
  "isCustomized": true,
  "createdAt": "2025-11-09T14:30:00Z",
  "updatedAt": "2025-11-09T14:30:00Z",
  "updatedBy": "teacher-id",
  "updatedByEmail": "teacher@witchcityrope.com"
}
```

**Implementation Notes**:
- Check if EventEmailTemplate exists for (EventId, TemplateType)
- If NOT exists: Create new EventEmailTemplate record
- If exists: Update existing record
- Sanitize HtmlBody
- Record current user ID in UpdatedBy
- Return created/updated template

#### DELETE /api/events/{eventId}/email-templates/{type}

**Purpose**: Reset to default (delete event-specific template)

**Authorization**: Event organizer (creator or admin)

**Request**:
```http
DELETE /api/events/7c9e6679-7425-40de-944b-e07fc1f90ae7/email-templates/Confirmation
```

**Response** (204 No Content):
```
(No body returned)
```

**Response** (404 Not Found):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Event template not found",
  "status": 404,
  "detail": "No event-specific template exists for type 'Confirmation'"
}
```

**Implementation Notes**:
- Find EventEmailTemplate by (EventId, TemplateType)
- If found: Delete record
- If not found: Return 404
- Authorization: Check if current user is event creator or admin
- Future loads will fetch global template (no event-specific override)

#### POST /api/email-templates/ad-hoc

**Purpose**: Send ad-hoc bulk email

**Authorization**: Administrator role required

**Request**:
```http
POST /api/email-templates/ad-hoc
Content-Type: application/json

{
  "subject": "Parking Update for {{event_title}}",
  "htmlBody": "<p>Hi {{recipient_name}},</p><p>Free parking available...</p>",
  "plainTextBody": "Hi {{recipient_name}},\n\nFree parking available...",
  "recipientGroup": "all-tickets",
  "eventId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

**Response** (200 OK):
```json
{
  "id": "sent-email-id",
  "subject": "Parking Update for Rope Performance Night",
  "htmlBody": "<p>Hi Sarah,</p><p>Free parking available...</p>",
  "plainTextBody": "Hi Sarah,\n\nFree parking available...",
  "recipientGroup": "all-tickets",
  "recipientCount": 187,
  "eventId": "7c9e6679-...",
  "eventTitle": "Rope Performance Night",
  "sendGridMessageId": "sg_abc123xyz",
  "deliveryStatus": "Sent",
  "sentAt": "2025-11-09T15:00:00Z",
  "sentBy": "admin-id",
  "sentByEmail": "admin@witchcityrope.com"
}
```

**Implementation Notes**:
- Resolve recipient group to email addresses
- Replace variables in subject and body for each recipient
- Send via SendGrid
- Capture SendGrid message ID
- Store full email content + recipients in SentAdHocEmails table
- Return sent email record

#### GET /api/email-templates/ad-hoc/history?eventId={id}

**Purpose**: Get sent ad-hoc email history

**Authorization**: Administrator role required

**Request**:
```http
GET /api/email-templates/ad-hoc/history?eventId=7c9e6679-7425-40de-944b-e07fc1f90ae7
```

**Query Parameters**:
- `eventId` (optional): Filter by event ID

**Response** (200 OK):
```json
[
  {
    "id": "sent-email-id",
    "subject": "Parking Update for Rope Performance Night",
    "htmlBody": "...",
    "plainTextBody": "...",
    "recipientGroup": "all-tickets",
    "recipientCount": 187,
    "eventId": "7c9e6679-...",
    "eventTitle": "Rope Performance Night",
    "sendGridMessageId": "sg_abc123xyz",
    "deliveryStatus": "Delivered",
    "sentAt": "2025-11-09T15:00:00Z",
    "sentBy": "admin-id",
    "sentByEmail": "admin@witchcityrope.com"
  }
]
```

**Implementation Notes**:
- Query SentAdHocEmails table
- Filter by EventId if provided (nullable field)
- Order by SentAt descending (newest first)
- Limit to 50 results (pagination future enhancement)
- Include Event relationship for eventTitle

---

## Business Logic Services

### GlobalEmailTemplateService

**Interface**:
```csharp
public interface IGlobalEmailTemplateService
{
    Task<Result<List<GlobalEmailTemplateDto>>> GetByCategoryAsync(
        EmailCategory category,
        CancellationToken cancellationToken = default);

    Task<Result<GlobalEmailTemplateDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<GlobalEmailTemplateDto>> UpdateAsync(
        Guid id,
        UpdateGlobalTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);
}
```

**Implementation** (Simplified Vertical Slice Pattern):
```csharp
public class GlobalEmailTemplateService : IGlobalEmailTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GlobalEmailTemplateService> _logger;

    public GlobalEmailTemplateService(
        ApplicationDbContext context,
        ILogger<GlobalEmailTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<GlobalEmailTemplateDto>>> GetByCategoryAsync(
        EmailCategory category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .Where(t => t.Category == category && t.IsActive)
                .Include(t => t.UpdatedByUser)
                .OrderBy(t => t.TemplateType)
                .Select(t => new GlobalEmailTemplateDto
                {
                    Id = t.Id,
                    Category = t.Category.ToString(),
                    TemplateType = t.TemplateType,
                    TemplateTypeName = GetTemplateTypeName(t.Category, t.TemplateType),
                    Subject = t.Subject,
                    HtmlBody = t.HtmlBody,
                    PlainTextBody = t.PlainTextBody,
                    Variables = JsonSerializer.Deserialize<string[]>(t.Variables) ?? Array.Empty<string>(),
                    IsActive = t.IsActive,
                    Version = t.Version,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UpdatedBy = t.UpdatedBy,
                    UpdatedByEmail = t.UpdatedByUser.Email ?? string.Empty
                })
                .ToListAsync(cancellationToken);

            return Result<List<GlobalEmailTemplateDto>>.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for category {Category}", category);
            return Result<List<GlobalEmailTemplateDto>>.Failure("Failed to retrieve templates");
        }
    }

    public async Task<Result<GlobalEmailTemplateDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .Include(t => t.UpdatedByUser)
                .Where(t => t.Id == id)
                .Select(t => new GlobalEmailTemplateDto
                {
                    Id = t.Id,
                    Category = t.Category.ToString(),
                    TemplateType = t.TemplateType,
                    TemplateTypeName = GetTemplateTypeName(t.Category, t.TemplateType),
                    Subject = t.Subject,
                    HtmlBody = t.HtmlBody,
                    PlainTextBody = t.PlainTextBody,
                    Variables = JsonSerializer.Deserialize<string[]>(t.Variables) ?? Array.Empty<string>(),
                    IsActive = t.IsActive,
                    Version = t.Version,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UpdatedBy = t.UpdatedBy,
                    UpdatedByEmail = t.UpdatedByUser.Email ?? string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                return Result<GlobalEmailTemplateDto>.Failure("Template not found");
            }

            return Result<GlobalEmailTemplateDto>.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve template {Id}", id);
            return Result<GlobalEmailTemplateDto>.Failure("Failed to retrieve template");
        }
    }

    public async Task<Result<GlobalEmailTemplateDto>> UpdateAsync(
        Guid id,
        UpdateGlobalTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.GlobalEmailTemplates
                .Include(t => t.UpdatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (template == null)
            {
                return Result<GlobalEmailTemplateDto>.Failure("Template not found");
            }

            // Sanitize HTML body
            var sanitizedHtml = SanitizeHtml(request.HtmlBody);

            // Update template
            template.Subject = request.Subject.Trim();
            template.HtmlBody = sanitizedHtml;
            template.PlainTextBody = request.PlainTextBody.Trim();
            template.Version++;  // Increment version
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync(cancellationToken);

            // Return updated template
            var dto = new GlobalEmailTemplateDto
            {
                Id = template.Id,
                Category = template.Category.ToString(),
                TemplateType = template.TemplateType,
                TemplateTypeName = GetTemplateTypeName(template.Category, template.TemplateType),
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Variables = JsonSerializer.Deserialize<string[]>(template.Variables) ?? Array.Empty<string>(),
                IsActive = template.IsActive,
                Version = template.Version,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                UpdatedBy = template.UpdatedBy,
                UpdatedByEmail = template.UpdatedByUser.Email ?? string.Empty
            };

            return Result<GlobalEmailTemplateDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {Id}", id);
            return Result<GlobalEmailTemplateDto>.Failure("Failed to update template");
        }
    }

    private static string SanitizeHtml(string html)
    {
        // Strip dangerous tags: <script>, <iframe>, <object>, <embed>
        // Allow safe tags: <p>, <h1-h6>, <strong>, <em>, <a>, <ul>, <ol>, <li>
        // Implementation: Use HtmlSanitizer library or Regex patterns
        // TODO: Implement HTML sanitization
        return html;
    }

    private static string GetTemplateTypeName(EmailCategory category, string templateType)
    {
        // Convert enum value to display name
        // Example: "Reminder1Day" → "Reminder - 1 Day Before"
        return templateType switch
        {
            "Confirmation" => "Confirmation Email",
            "Reminder1Day" => "Reminder - 1 Day Before",
            "Reminder1Week" => "Reminder - 1 Week Before",
            _ => templateType
        };
    }
}
```

### EventEmailTemplateService

**Interface**:
```csharp
public interface IEventEmailTemplateService
{
    Task<Result<List<EventEmailTemplateDto>>> GetForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<Result<EventEmailTemplateDto>> GetOrCreateEventTemplateAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default);

    Task<Result<EventEmailTemplateDto>> UpdateEventTemplateAsync(
        Guid eventId,
        string templateType,
        UpdateEventTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);

    Task<Result> ResetToDefaultAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default);
}
```

**Key Method Implementation** (Copy-on-Edit):
```csharp
public async Task<Result<EventEmailTemplateDto>> UpdateEventTemplateAsync(
    Guid eventId,
    string templateType,
    UpdateEventTemplateRequest request,
    Guid updatedByUserId,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Check if event-specific template exists
        var existingTemplate = await _context.EventEmailTemplates
            .Include(t => t.UpdatedByUser)
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == templateType, cancellationToken);

        if (existingTemplate != null)
        {
            // UPDATE existing event-specific template
            existingTemplate.Subject = request.Subject.Trim();
            existingTemplate.HtmlBody = SanitizeHtml(request.HtmlBody);
            existingTemplate.PlainTextBody = request.PlainTextBody.Trim();
            existingTemplate.TargetSessions = request.TargetSessions;
            existingTemplate.UpdatedAt = DateTime.UtcNow;
            existingTemplate.UpdatedBy = updatedByUserId;
        }
        else
        {
            // CREATE new event-specific template (copy-on-edit)
            var globalTemplate = await _context.GlobalEmailTemplates
                .FirstOrDefaultAsync(t => t.Category == EmailCategory.Events && t.TemplateType == templateType, cancellationToken);

            if (globalTemplate == null)
            {
                return Result<EventEmailTemplateDto>.Failure("Global template not found");
            }

            var newTemplate = new EventEmailTemplate
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                GlobalTemplateId = globalTemplate.Id,
                TemplateType = templateType,
                Subject = request.Subject.Trim(),
                HtmlBody = SanitizeHtml(request.HtmlBody),
                PlainTextBody = request.PlainTextBody.Trim(),
                TargetSessions = request.TargetSessions,
                IsCustomized = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = updatedByUserId
            };

            _context.EventEmailTemplates.Add(newTemplate);
            existingTemplate = newTemplate;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Return updated/created template
        var dto = new EventEmailTemplateDto
        {
            Id = existingTemplate.Id,
            EventId = existingTemplate.EventId,
            GlobalTemplateId = existingTemplate.GlobalTemplateId,
            TemplateType = existingTemplate.TemplateType,
            TemplateTypeName = GetTemplateTypeName(existingTemplate.TemplateType),
            Subject = existingTemplate.Subject,
            HtmlBody = existingTemplate.HtmlBody,
            PlainTextBody = existingTemplate.PlainTextBody,
            TargetSessions = existingTemplate.TargetSessions,
            IsCustomized = true,
            CreatedAt = existingTemplate.CreatedAt,
            UpdatedAt = existingTemplate.UpdatedAt,
            UpdatedBy = existingTemplate.UpdatedBy,
            UpdatedByEmail = existingTemplate.UpdatedByUser.Email ?? string.Empty
        };

        return Result<EventEmailTemplateDto>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update event template for event {EventId}, type {Type}", eventId, templateType);
        return Result<EventEmailTemplateDto>.Failure("Failed to update event template");
    }
}

public async Task<Result> ResetToDefaultAsync(
    Guid eventId,
    string templateType,
    CancellationToken cancellationToken = default)
{
    try
    {
        var template = await _context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == templateType, cancellationToken);

        if (template == null)
        {
            return Result.Failure("Event template not found");
        }

        // Delete event-specific template (revert to global)
        _context.EventEmailTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to reset template for event {EventId}, type {Type}", eventId, templateType);
        return Result.Failure("Failed to reset template");
    }
}
```

### Variable Validation Service

**Purpose**: Validate variables in email templates against category-specific allowed variables

**Interface**:
```csharp
public interface IVariableValidationService
{
    /// <summary>
    /// Extract all {{variable}} patterns from HTML content
    /// </summary>
    List<string> ExtractVariables(string htmlContent);

    /// <summary>
    /// Validate variables against allowed list for category
    /// </summary>
    ValidationResult ValidateVariables(string htmlContent, EmailCategory category);

    /// <summary>
    /// Get allowed variables for a specific category
    /// </summary>
    string[] GetAllowedVariables(EmailCategory category);
}
```

**Implementation**:
```csharp
public class VariableValidationService : IVariableValidationService
{
    private static readonly Regex VariablePattern = new(@"\{\{([a-z_]+)\}\}", RegexOptions.IgnoreCase);

    private static readonly Dictionary<EmailCategory, string[]> AllowedVariables = new()
    {
        [EmailCategory.Vetting] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{application_date}}",
            "{{submission_date}}", "{{status_change_date}}", "{{contact_email}}",
            "{{current_status}}", "{{custom_message}}"
        },
        [EmailCategory.Events] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}",
            "{{venue_name}}", "{{venue_address}}", "{{session_name}}", "{{ticket_type}}",
            "{{total_paid}}", "{{confirmation_number}}", "{{organizer_email}}"
        },
        [EmailCategory.Admin] = new[]
        {
            "{{user_name}}", "{{account_email}}", "{{system_url}}", "{{support_email}}",
            "{{action_required}}", "{{deadline_date}}"
        },
        [EmailCategory.Incident] = new[]
        {
            "{{incident_number}}", "{{reporter_name}}", "{{incident_date}}",
            "{{status}}", "{{coordinator_name}}", "{{next_steps}}"
        },
        [EmailCategory.AdHoc] = new[]
        {
            "{{recipient_name}}", "{{event_title}}", "{{custom_content}}"
        }
    };

    public List<string> ExtractVariables(string htmlContent)
    {
        var matches = VariablePattern.Matches(htmlContent);
        return matches.Select(m => $"{{{{{m.Groups[1].Value}}}}}").Distinct().ToList();
    }

    public ValidationResult ValidateVariables(string htmlContent, EmailCategory category)
    {
        var extractedVariables = ExtractVariables(htmlContent);
        var allowedVariables = GetAllowedVariables(category);
        var invalidVariables = extractedVariables.Where(v => !allowedVariables.Contains(v)).ToList();

        return new ValidationResult
        {
            IsValid = invalidVariables.Count == 0,
            InvalidVariables = invalidVariables,
            AllowedVariables = allowedVariables
        };
    }

    public string[] GetAllowedVariables(EmailCategory category)
    {
        return AllowedVariables.TryGetValue(category, out var variables) ? variables : Array.Empty<string>();
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> InvalidVariables { get; set; } = new();
    public string[] AllowedVariables { get; set; } = Array.Empty<string>();
}
```

---

## Variable Substitution System

### Variable Sets by Category

#### Vetting Variables (6 templates)
```csharp
public static class VettingVariables
{
    public const string SceneName = "{{scene_name}}";
    public const string ApplicationNumber = "{{application_number}}";
    public const string ApplicationDate = "{{application_date}}";
    public const string SubmissionDate = "{{submission_date}}";
    public const string StatusChangeDate = "{{status_change_date}}";
    public const string ContactEmail = "{{contact_email}}";
    public const string CurrentStatus = "{{current_status}}";
    public const string CustomMessage = "{{custom_message}}";

    public static readonly string[] All = {
        SceneName, ApplicationNumber, ApplicationDate, SubmissionDate,
        StatusChangeDate, ContactEmail, CurrentStatus, CustomMessage
    };
}
```

#### Events Variables (7 templates)
```csharp
public static class EventsVariables
{
    public const string AttendeeName = "{{attendee_name}}";
    public const string EventTitle = "{{event_title}}";
    public const string EventDate = "{{event_date}}";
    public const string EventTime = "{{event_time}}";
    public const string VenueName = "{{venue_name}}";
    public const string VenueAddress = "{{venue_address}}";
    public const string SessionName = "{{session_name}}";
    public const string TicketType = "{{ticket_type}}";
    public const string TotalPaid = "{{total_paid}}";
    public const string ConfirmationNumber = "{{confirmation_number}}";
    public const string OrganizerEmail = "{{organizer_email}}";

    public static readonly string[] All = {
        AttendeeName, EventTitle, EventDate, EventTime, VenueName,
        VenueAddress, SessionName, TicketType, TotalPaid,
        ConfirmationNumber, OrganizerEmail
    };
}
```

#### Admin Variables (4 templates)
```csharp
public static class AdminVariables
{
    public const string UserName = "{{user_name}}";
    public const string AccountEmail = "{{account_email}}";
    public const string SystemUrl = "{{system_url}}";
    public const string SupportEmail = "{{support_email}}";
    public const string ActionRequired = "{{action_required}}";
    public const string DeadlineDate = "{{deadline_date}}";

    public static readonly string[] All = {
        UserName, AccountEmail, SystemUrl, SupportEmail,
        ActionRequired, DeadlineDate
    };
}
```

#### Incident Variables (4 templates)
```csharp
public static class IncidentVariables
{
    public const string IncidentNumber = "{{incident_number}}";
    public const string ReporterName = "{{reporter_name}}";
    public const string IncidentDate = "{{incident_date}}";
    public const string Status = "{{status}}";
    public const string CoordinatorName = "{{coordinator_name}}";
    public const string NextSteps = "{{next_steps}}";

    public static readonly string[] All = {
        IncidentNumber, ReporterName, IncidentDate, Status,
        CoordinatorName, NextSteps
    };
}
```

#### Ad Hoc Variables (1 template)
```csharp
public static class AdHocVariables
{
    public const string RecipientName = "{{recipient_name}}";
    public const string EventTitle = "{{event_title}}";
    public const string CustomContent = "{{custom_content}}";

    public static readonly string[] All = {
        RecipientName, EventTitle, CustomContent
    };
}
```

### Variable Substitution at Send-Time

**Example Implementation** (from VettingEmailService pattern):
```csharp
private static string RenderTemplate(string template, EventRegistration registration, Event eventDetails)
{
    return template
        .Replace(EventsVariables.AttendeeName, registration.AttendeeName)
        .Replace(EventsVariables.EventTitle, eventDetails.Title)
        .Replace(EventsVariables.EventDate, eventDetails.StartTime.ToString("MMMM dd, yyyy"))
        .Replace(EventsVariables.EventTime, eventDetails.StartTime.ToString("h:mm tt"))
        .Replace(EventsVariables.VenueName, eventDetails.VenueName)
        .Replace(EventsVariables.VenueAddress, eventDetails.VenueAddress)
        .Replace(EventsVariables.TicketType, registration.TicketType)
        .Replace(EventsVariables.TotalPaid, registration.TotalPaid.ToString("C"))
        .Replace(EventsVariables.ConfirmationNumber, registration.ConfirmationNumber);
}
```

**HTML Escaping** (XSS Prevention):
```csharp
private static string EscapeHtml(string value)
{
    return System.Net.WebUtility.HtmlEncode(value);
}

private static string RenderTemplateWithEscaping(string template, Dictionary<string, string> variables)
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

---

## Security Requirements

### Authentication and Authorization

**Global Templates**:
- **Endpoint**: `/api/email-templates/*`
- **Required Role**: `Administrator`
- **Implementation**: `[Authorize(Roles = "Administrator")]` attribute on endpoints
- **Validation**: Check user claims for Administrator role

**Event Templates**:
- **Endpoint**: `/api/events/{eventId}/email-templates/*`
- **Required**: User must be event creator OR Administrator
- **Implementation**:
```csharp
var eventEntity = await _context.Events.FindAsync(eventId);
if (eventEntity == null) return Results.NotFound();

var currentUserId = GetCurrentUserId(httpContext);
var isAuthorized = eventEntity.CreatedBy == currentUserId ||
                   httpContext.User.IsInRole("Administrator");

if (!isAuthorized) return Results.Forbid();
```

**Ad Hoc Emails**:
- **Endpoint**: `/api/email-templates/ad-hoc/*`
- **Required Role**: `Administrator`
- **Reason**: Bulk email sending has abuse potential

### Input Validation

**Subject Line**:
- Required, non-empty
- Max length: 200 characters
- Trimmed of whitespace
- No HTML allowed (plain text only)

**HTML Body**:
- Required, non-empty
- Sanitized to strip dangerous tags: `<script>`, `<iframe>`, `<object>`, `<embed>`, `<form>`
- Allow safe tags: `<p>`, `<h1-h6>`, `<strong>`, `<em>`, `<a>`, `<ul>`, `<ol>`, `<li>`, `<br>`
- Implementation: Use HtmlSanitizer library or custom sanitization

**Plain Text Body**:
- Required, non-empty
- Auto-generated from HtmlBody if not provided
- Strip all HTML tags

### XSS Prevention

**Variable Values**:
- ALWAYS HTML-escape before insertion
- Example: `attendee_name = "<script>alert('XSS')</script>"` → `"&lt;script&gt;alert('XSS')&lt;/script&gt;"`
- Implementation: `System.Net.WebUtility.HtmlEncode(variableValue)`

**Template Content**:
- Sanitize HtmlBody on save (server-side)
- MantineTiptapEditor provides client-side sanitization (defense in depth)
- Never trust client input

### SQL Injection Prevention

**All Database Queries**:
- Use Entity Framework parameterized queries (NO raw SQL)
- LINQ queries automatically parameterized
- Example: `_context.GlobalEmailTemplates.Where(t => t.Id == id)` (safe)

### CSRF Protection

**All State-Changing Endpoints**:
- Require CSRF token (ASP.NET Core anti-forgery middleware)
- PUT/POST/DELETE endpoints protected
- GET endpoints read-only (no state changes)

---

## Performance Considerations

### Caching Strategy

**Global Templates**:
- Cache in memory with 30-minute expiration
- Invalidate on template update
- Key: `email-template-{category}`

**Event Templates**:
- NO caching (event-specific, frequently customized)
- Fetch from database on each load

**Variable Sets**:
- Static constants (compile-time, no database queries)

### Database Optimization

**Indexes**:
```sql
CREATE INDEX "IX_GlobalEmailTemplates_Category" ON "GlobalEmailTemplates" ("Category");
CREATE INDEX "IX_GlobalEmailTemplates_Variables" ON "GlobalEmailTemplates" USING GIN ("Variables");
CREATE INDEX "IX_EventEmailTemplates_EventId" ON "EventEmailTemplates" ("EventId");
CREATE INDEX "IX_EventEmailTemplates_EventId_TemplateType" ON "EventEmailTemplates" ("EventId", "TemplateType");
CREATE INDEX "IX_SentAdHocEmails_SentAt" ON "SentAdHocEmails" ("SentAt" DESC);
```

**Query Optimization**:
- Use `AsNoTracking()` for read-only queries (40% memory reduction)
- Explicit `Include()` for relationships (prevent N+1 queries)
- `Select()` to DTO to reduce data transfer

**Example Optimized Query**:
```csharp
var templates = await _context.GlobalEmailTemplates
    .AsNoTracking()  // Read-only optimization
    .Where(t => t.Category == category && t.IsActive)
    .Include(t => t.UpdatedByUser)  // Explicit relationship load
    .Select(t => new GlobalEmailTemplateDto  // Project to DTO
    {
        Id = t.Id,
        Subject = t.Subject,
        // ... only fields needed
    })
    .ToListAsync(cancellationToken);
```

### SendGrid Rate Limiting

**Application-Level Rate Limit**:
- Max 100 emails per minute
- Queue emails if limit exceeded
- Retry with exponential backoff (3 attempts)

**Implementation**:
```csharp
private static readonly SemaphoreSlim SendGridThrottle = new(100, 100);  // 100 concurrent

public async Task<Result> SendEmailAsync(EmailMessage message)
{
    await SendGridThrottle.WaitAsync();
    try
    {
        await _sendGridClient.SendEmailAsync(message);
        return Result.Success();
    }
    finally
    {
        await Task.Delay(600);  // 100 emails/minute = 600ms delay
        SendGridThrottle.Release();
    }
}
```

---

## Testing Requirements

### Unit Tests

**GlobalEmailTemplateService**:
- Test GetByCategory with valid category
- Test GetByCategory with invalid category
- Test UpdateAsync increments version
- Test UpdateAsync sanitizes HTML
- Test UpdateAsync with non-existent template

**EventEmailTemplateService**:
- Test copy-on-edit creates new record
- Test copy-on-edit updates existing record
- Test ResetToDefault deletes event template
- Test ResetToDefault with non-existent template

**VariableValidationService**:
- Test ExtractVariables finds all `{{variable}}` patterns
- Test ValidateVariables detects unknown variables
- Test ValidateVariables allows known variables
- Test GetAllowedVariables returns correct sets per category

**Target**: 90%+ code coverage for services

### Integration Tests

**API Endpoints**:
- GET /api/email-templates?category=events (200 OK, valid data)
- GET /api/email-templates/{id} (200 OK, 404 Not Found)
- PUT /api/email-templates/{id} (200 OK, 400 Bad Request validation)
- GET /api/events/{eventId}/email-templates (200 OK, 403 Forbidden)
- PUT /api/events/{eventId}/email-templates/{type} (200 OK, creates new)
- PUT /api/events/{eventId}/email-templates/{type} (200 OK, updates existing)
- DELETE /api/events/{eventId}/email-templates/{type} (204 No Content, 404 Not Found)

**Target**: 100% endpoint coverage with happy path + error scenarios

### E2E Tests (Playwright)

**Test Scenarios**:
1. **Admin manages global Vetting template**:
   - Navigate to /admin/email-templates
   - Select Vetting tab
   - Click "Application Received" card
   - Edit subject and body
   - Save template
   - Verify version incremented, success notification

2. **Event organizer customizes event template**:
   - Navigate to /events/{id}/edit
   - Click Emails tab
   - Click "Confirmation Email" card (badge shows "(Default)")
   - Edit subject and body
   - Save template
   - Verify badge changes to "✓ Customized"

3. **Event organizer resets to default**:
   - Navigate to /events/{id}/edit
   - Click Emails tab
   - Click "Confirmation Email" card (badge shows "✓ Customized")
   - Click "Reset to Default" button
   - Confirm in modal
   - Verify badge changes to "(Default)"
   - Verify content reverts to global template

4. **Variable validation warnings**:
   - Navigate to /admin/email-templates
   - Select Events tab
   - Click "Confirmation Email" card
   - Add invalid variable `{{invalid_var}}`
   - Verify warning appears: "Unknown variable detected"
   - Verify save is NOT blocked (warning only)

**Target**: 100% critical user workflows covered

---

## Migration Requirements

### Vetting Template Migration

**Objective**: Migrate existing VettingEmailTemplates → GlobalEmailTemplates

**Migration Steps**:
1. Create GlobalEmailTemplates table
2. Copy all records from VettingEmailTemplates:
   - Category = EmailCategory.Vetting (0)
   - TemplateType = existing EmailTemplateType enum value
   - Subject, HtmlBody, PlainTextBody, Variables copied verbatim
   - CreatedAt, UpdatedAt, UpdatedBy preserved
3. Keep VettingEmailTemplates table (read-only backup, not used)
4. Update VettingEmailService to query GlobalEmailTemplates WHERE Category = Vetting
5. Delete old `/admin/vetting/email-templates` page
6. Update Vetting admin button to link to `/admin/email-templates?tab=vetting`

**Database Migration Script** (EF Core):
```csharp
public partial class MigrateVettingTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create GlobalEmailTemplates table
        migrationBuilder.CreateTable(
            name: "GlobalEmailTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Category = table.Column<int>(nullable: false),
                TemplateType = table.Column<string>(maxLength: 50, nullable: false),
                Subject = table.Column<string>(maxLength: 200, nullable: false),
                HtmlBody = table.Column<string>(nullable: false),
                PlainTextBody = table.Column<string>(nullable: false),
                Variables = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                Version = table.Column<int>(nullable: false, defaultValue: 1),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false),
                UpdatedBy = table.Column<Guid>(nullable: false)
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
            });

        // Migrate existing Vetting templates
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
                ""Variables"",
                ""IsActive"",
                ""Version"",
                ""CreatedAt"",
                ""UpdatedAt"",
                ""UpdatedBy""
            FROM ""VettingEmailTemplates""
        ");

        // Keep VettingEmailTemplates table (backwards compatibility, read-only)
        // DO NOT drop VettingEmailTemplates table
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop GlobalEmailTemplates table
        migrationBuilder.DropTable(name: "GlobalEmailTemplates");

        // VettingEmailTemplates table remains (not dropped in Up migration)
    }
}
```

**Validation Steps**:
1. Run migration script
2. Verify all 6 Vetting templates copied to GlobalEmailTemplates
3. Test Vetting email sending uses GlobalEmailTemplates
4. Test /admin/email-templates?tab=vetting shows all templates
5. Test editing Vetting template updates GlobalEmailTemplates (not old table)

---

## Dependencies

### External Services

**SendGrid**:
- Purpose: Email delivery
- Configuration: API key in appsettings.json
- Rate Limit: 100 emails/minute (application-level)
- Message ID: Captured for delivery tracking
- Sandbox Mode: For testing without actual delivery

**Configuration**:
```json
{
  "Email": {
    "SendGridApiKey": "SG.xxx",
    "FromEmail": "noreply@witchcityrope.com",
    "FromName": "WitchCityRope",
    "SendGridSandboxMode": false,
    "RateLimitPerMinute": 100
  }
}
```

### NuGet Packages

**Backend**:
- `SendGrid` - SendGrid API client
- `HtmlSanitizer` - HTML sanitization (XSS prevention)
- `FluentValidation` - Request DTO validation

**Frontend**:
- `@witchcityrope/shared-types` - Auto-generated TypeScript types
- `@tanstack/react-query` - API state management
- `@mantine/core` - UI components
- `@mantine/tiptap` - Rich text editor

### Integration Points

**Authentication System**:
- User ID extraction from JWT claims
- Role-based authorization (Administrator, event creator)
- User email for audit trail (UpdatedByEmail, SentByEmail)

**Event Management System**:
- Event entity relationship (EventEmailTemplates.EventId → Events.Id)
- Cascade delete when event deleted
- Event organizer authorization (CreatedBy field check)

**Vetting System**:
- VettingEmailService updated to query GlobalEmailTemplates
- Backwards compatibility with VettingEmailTemplates (read-only)
- Vetting admin page button redirects to /admin/email-templates?tab=vetting

---

## Error Handling

### Error Codes and Messages

**Template Not Found** (404):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Template not found",
  "status": 404,
  "detail": "Template with ID '3fa85f64-...' not found"
}
```

**Validation Failed** (400):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Subject": ["Subject is required", "Subject cannot exceed 200 characters"],
    "HtmlBody": ["HTML body is required"]
  }
}
```

**Unauthorized** (401):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication required"
}
```

**Forbidden** (403):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to edit this event's templates"
}
```

**SendGrid Failure** (500):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Email send failed",
  "status": 500,
  "detail": "Failed to send email via SendGrid. Message: [SendGrid error message]"
}
```

### Retry Logic

**SendGrid API Calls**:
- Retry 3 times with exponential backoff: 1s, 2s, 4s
- Log all attempts to database (SentAdHocEmails or VettingEmailLog)
- Mark as "Failed" after 3 failed attempts
- Notification to admin if persistent failures

**Implementation**:
```csharp
private async Task<Result> SendWithRetryAsync(EmailMessage message, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await _sendGridClient.SendEmailAsync(message);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SendGrid attempt {Attempt} failed", attempt);

            if (attempt == maxRetries)
            {
                _logger.LogError(ex, "SendGrid failed after {MaxRetries} attempts", maxRetries);
                return Result.Failure("Failed to send email after multiple attempts");
            }

            // Exponential backoff: 1s, 2s, 4s
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
        }
    }

    return Result.Failure("Unexpected error");
}
```

---

## Implementation Notes

### For Database Designer

**Create Migration**:
- Three new tables: GlobalEmailTemplates, EventEmailTemplates, SentAdHocEmails
- Unique constraints: (Category, TemplateType) on GlobalEmailTemplates, (EventId, TemplateType) on EventEmailTemplates
- Indexes for performance (see Database Schema section)
- JSONB field for Variables (PostgreSQL-specific optimization)
- Cascade delete: Event deletion deletes EventEmailTemplates

**Seed Default Templates**:
- 22 default templates total (6 Vetting + 7 Events + 4 Admin + 4 Incident + 1 Ad Hoc)
- Copy existing Vetting templates from VettingEmailTemplates table
- Define default content for Events, Admin, Incident templates
- Set Variables JSONB field with category-specific allowed variables

### For Backend Developer

**Follow Vertical Slice Pattern**:
- Direct Entity Framework access (NO MediatR, NO repository pattern)
- Services with simple method signatures returning `Result<T>`
- Minimal API endpoints with full OpenAPI documentation
- DTOs with validation attributes for NSwag generation

**Critical Patterns**:
- Copy-on-edit: EventEmailTemplate created only when user saves changes
- Reset-to-default: DELETE EventEmailTemplate record, future loads fetch global
- Variable substitution: Replace `{{variable}}` at send-time with HTML-escaped values
- HTML sanitization: Strip dangerous tags on save

**NSwag Integration**:
- Add OpenAPI annotations to all endpoints
- Ensure DTOs are public and have XML documentation comments
- Run `npm run generate:types` to verify TypeScript generation

### For React Developer

**CRITICAL: Use Generated Types**:
- NEVER create manual DTO interfaces
- ALWAYS import from `@witchcityrope/shared-types`
- Example: `import type { components } from '@witchcityrope/shared-types'`
- Type alias: `export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto']`

**Component Structure**:
- Reuse EventForm Emails tab pattern (template cards + editor)
- EmailCategoryPanel component reusable across all 5 tabs
- MantineTiptapEditor for HTML content (same as CMS feature)
- Badge indicators: "✓ Customized" (green) vs "(Default)" (gray)

**State Management**:
- React Query for API calls (`useQuery`, `useMutation`)
- Local state for selected template, editor content
- URL query parameter for active tab (`?tab=events`)

**Accessibility**:
- ARIA labels on all buttons and cards
- Keyboard navigation (Tab, Enter, Escape)
- Screen reader announcements for save success/failure
- Color contrast verified (WCAG 2.1 AA)

---

## Acceptance Criteria

### Technical Criteria

- [ ] All 10 API endpoints functional and documented (OpenAPI)
- [ ] NSwag auto-generates TypeScript types from C# DTOs
- [ ] GlobalEmailTemplates table seeded with 22 default templates
- [ ] Copy-on-edit creates EventEmailTemplate only when user saves
- [ ] Reset-to-default deletes EventEmailTemplate, reverts to global
- [ ] Variable validation shows warnings for unknown variables (non-blocking)
- [ ] HTML sanitization strips dangerous tags (`<script>`, `<iframe>`, etc.)
- [ ] Variable values HTML-escaped before insertion (XSS prevention)
- [ ] All database queries use Entity Framework (NO raw SQL)
- [ ] SendGrid integration with retry logic (3 attempts, exponential backoff)

### Functional Criteria

- [ ] Administrator can manage global templates from `/admin/email-templates`
- [ ] Templates organized by 5 tabs (Vetting, Events, Admin, Incident, Ad Hoc)
- [ ] Event organizers can customize event-specific templates
- [ ] Event-specific templates show "✓ Customized" badge
- [ ] "Reset to Default" button appears when template customized
- [ ] Vetting admin button redirects to `/admin/email-templates?tab=vetting`
- [ ] Old `/admin/vetting/email-templates` page deleted
- [ ] Ad-hoc email history stored in SentAdHocEmails table
- [ ] Variable substitution works at send-time (not stored in database)

### Performance Criteria

- [ ] Template list load: < 500ms
- [ ] Template editor load: < 300ms
- [ ] Template save: < 1 second
- [ ] Variable substitution: < 100ms per email
- [ ] SendGrid rate limit enforced (100 emails/minute)

### Quality Criteria

- [ ] Unit test coverage: 90%+ for services
- [ ] Integration test coverage: 100% for API endpoints
- [ ] E2E test coverage: 100% for critical workflows
- [ ] TypeScript types auto-generated (NO manual interfaces)
- [ ] All endpoints documented with OpenAPI annotations
- [ ] Code review approval from both frontend and backend teams

---

**END OF FUNCTIONAL SPECIFICATION**
