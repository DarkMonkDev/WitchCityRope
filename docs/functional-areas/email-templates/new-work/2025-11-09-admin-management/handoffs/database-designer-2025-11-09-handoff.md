# AGENT HANDOFF DOCUMENT

## Phase: Database Design (Phase 2)
## Date: 2025-11-09
## Feature: Email Templates Admin Management
## From: Database Designer Agent
## To: Backend Developer Agent

---

## 🎯 COMPLETION STATUS

**Database Design**: ✅ COMPLETE
**Database Schema**: ✅ 3 tables designed (GlobalEmailTemplates, EventEmailTemplates, SentAdHocEmails)
**Entity Framework Configuration**: ✅ Complete fluent API configurations provided
**Migration Strategy**: ✅ VettingEmailTemplates migration plan documented
**Index Strategy**: ✅ 15 indexes defined with performance justifications
**Seed Data Specifications**: ✅ 22 default templates specified

---

## 📊 DATABASE DESIGN SUMMARY

### Tables Created

**1. GlobalEmailTemplates**
- **Purpose**: Single source of truth for default email templates across all 5 categories
- **Primary Key**: Id (UUID)
- **Unique Constraint**: (Category, TemplateType) - ensures only ONE template per type per category
- **Categories**: 0=Vetting, 1=Events, 2=Admin, 3=Incident, 4=AdHoc
- **Key Fields**: Subject (200 chars), HtmlBody (text), PlainTextBody (text), Variables (JSONB)
- **Audit**: Version (increments on update), CreatedAt, UpdatedAt, UpdatedBy → ApplicationUser
- **Business Rules**:
  - Soft delete via IsActive flag (NEVER hard delete global templates)
  - Version increments on every update for audit trail
  - Variables stored as JSONB array for PostgreSQL optimization

**2. EventEmailTemplates**
- **Purpose**: Event-specific template overrides (copy-on-edit pattern)
- **Primary Key**: Id (UUID)
- **Unique Constraint**: (EventId, TemplateType) - ensures only ONE custom template per type per event
- **Key Fields**: EventId → Events (CASCADE), GlobalTemplateId (reference only), Subject, HtmlBody, TargetSessions (array)
- **Audit**: CreatedAt, UpdatedAt, UpdatedBy → ApplicationUser
- **Business Rules**:
  - Created ONLY when event organizer saves customizations (copy-on-edit)
  - Deleting this record = "Reset to Default" (revert to global template)
  - GlobalTemplateId is reference only (NOT enforced foreign key)
  - CASCADE delete: Deleting Event deletes all EventEmailTemplates for that event

**3. SentAdHocEmails**
- **Purpose**: Permanent audit trail for ad-hoc bulk emails
- **Primary Key**: Id (UUID)
- **No Unique Constraints**: Can send multiple emails to same group
- **Key Fields**: Subject, HtmlBody, RecipientGroup, RecipientEmails (array), RecipientCount, EventId (nullable)
- **SendGrid**: SendGridMessageId, DeliveryStatus (Pending/Sent/Delivered/Failed/Bounced)
- **Audit**: SentAt, SentBy → ApplicationUser
- **Business Rules**:
  - Read-only after creation (NEVER edited or deleted)
  - EventId nullable (not all ad-hoc emails are event-related)
  - SET NULL on Event deletion (preserve audit trail)

---

## 🔗 CRITICAL RELATIONSHIPS

### Foreign Key Constraints

**GlobalEmailTemplates → ApplicationUser**:
- FK: UpdatedBy → AspNetUsers.Id
- Delete Behavior: RESTRICT (cannot delete user if they updated templates)

**EventEmailTemplates → Events**:
- FK: EventId → Events.Id
- Delete Behavior: CASCADE (deleting event deletes all custom templates)

**EventEmailTemplates → ApplicationUser**:
- FK: UpdatedBy → AspNetUsers.Id
- Delete Behavior: RESTRICT (cannot delete user if they customized templates)

**EventEmailTemplates → GlobalEmailTemplates**:
- FK: GlobalTemplateId → GlobalEmailTemplates.Id
- Delete Behavior: NO ACTION (reference only, NOT enforced)
- **CRITICAL**: This is a SOFT reference only - do NOT create foreign key constraint

**SentAdHocEmails → Events**:
- FK: EventId → Events.Id
- Delete Behavior: SET NULL (preserve email record, nullify event reference)

**SentAdHocEmails → ApplicationUser**:
- FK: SentBy → AspNetUsers.Id
- Delete Behavior: RESTRICT (cannot delete user if they sent emails)

---

## 📈 INDEX STRATEGY

### GlobalEmailTemplates Indexes (6 total)

1. **PK_GlobalEmailTemplates** (Id) - B-tree primary key
2. **UQ_GlobalEmailTemplates_Category_Type** (Category, TemplateType) - Unique constraint
3. **IX_GlobalEmailTemplates_Category** (Category) - Filter by category (admin UI)
4. **IX_GlobalEmailTemplates_UpdatedBy** (UpdatedBy) - Audit queries
5. **IX_GlobalEmailTemplates_UpdatedAt** (UpdatedAt DESC) - Recent changes
6. **IX_GlobalEmailTemplates_Variables_Gin** (Variables) - JSONB GIN index for variable lookups

**Performance**: Category filter O(log n), Variable containment O(1)

### EventEmailTemplates Indexes (5 total)

1. **PK_EventEmailTemplates** (Id) - B-tree primary key
2. **UQ_EventEmailTemplates_EventId_Type** (EventId, TemplateType) - Unique constraint
3. **IX_EventEmailTemplates_EventId** (EventId) - Load all templates for event
4. **IX_EventEmailTemplates_UpdatedBy** (UpdatedBy) - Audit queries
5. **IX_EventEmailTemplates_UpdatedAt** (UpdatedAt DESC) - Recent changes

**Performance**: Event template load O(log n), Specific template lookup O(1)

### SentAdHocEmails Indexes (4 total)

1. **PK_SentAdHocEmails** (Id) - B-tree primary key
2. **IX_SentAdHocEmails_EventId** (EventId WHERE NOT NULL) - Partial index for event-related emails
3. **IX_SentAdHocEmails_SentBy** (SentBy) - Audit queries
4. **IX_SentAdHocEmails_SentAt** (SentAt DESC) - Chronological history
5. **IX_SentAdHocEmails_DeliveryStatus** (DeliveryStatus WHERE Pending/Failed) - Partial index for retry logic

**Performance**: Failed email detection O(1) via partial index

---

## 🔄 MIGRATION STRATEGY

### VettingEmailTemplates → GlobalEmailTemplates Migration

**Objective**: Migrate existing 6 Vetting templates to GlobalEmailTemplates table

**Migration SQL**:
```sql
INSERT INTO "GlobalEmailTemplates"
    ("Id", "Category", "TemplateType", "Subject", "HtmlBody", "PlainTextBody", "Variables", "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy")
SELECT
    "Id",
    0 AS "Category",  -- EmailCategory.Vetting
    CAST("TemplateType" AS VARCHAR(50)),
    "Subject",
    "HtmlBody",
    "PlainTextBody",
    COALESCE("Variables", '[]'::jsonb),
    "IsActive",
    "Version",
    "CreatedAt",
    "UpdatedAt",
    "UpdatedBy"
FROM "VettingEmailTemplates"
WHERE EXISTS (SELECT 1 FROM "VettingEmailTemplates");
```

**Critical Notes**:
- Preserve ALL audit data (Version, UpdatedBy, timestamps)
- Keep VettingEmailTemplates table (backwards compatibility, read-only)
- Do NOT drop VettingEmailTemplates table
- Future: Update VettingEmailService to query GlobalEmailTemplates WHERE Category = Vetting

### Seed Data for New Templates

**Required**: 16 new templates to seed (22 total - 6 migrated)

**Categories**:
- **Events**: 7 templates (Confirmation, Reminder1Week, Reminder1Day, Reminder2Hours, Cancellation, SessionChange, ThankYou)
- **Admin**: 4 templates (AccountCreated, PasswordReset, RoleChanged, SystemNotification)
- **Incident**: 4 templates (ReportReceived, StatusUpdate, AssignmentNotification, Resolved)
- **AdHoc**: 1 template (General)

**Implementation**: EmailTemplateSeeder.cs service (NOT in migration script)

**Location**: `/apps/api/Services/Seeding/EmailTemplateSeeder.cs`

---

## 🚨 CRITICAL IMPLEMENTATION NOTES

### 1. **NO Foreign Key Constraint on GlobalTemplateId**

**WRONG**:
```csharp
builder.HasOne(e => e.GlobalTemplate)
    .WithMany()
    .HasForeignKey(e => e.GlobalTemplateId)
    .OnDelete(DeleteBehavior.Restrict);  // ❌ DO NOT CREATE THIS
```

**CORRECT**:
```csharp
// GlobalTemplateId is reference only, NO foreign key constraint
builder.Property(e => e.GlobalTemplateId)
    .IsRequired();

// Navigation property nullable (optional)
builder.HasOne(e => e.GlobalTemplate)
    .WithMany()
    .HasForeignKey(e => e.GlobalTemplateId)
    .OnDelete(DeleteBehavior.NoAction)
    .IsRequired(false);
```

**Rationale**: Global template might be deleted or replaced. EventEmailTemplate should persist (it has its own content copy).

### 2. **PostgreSQL-Specific Features**

**JSONB with GIN Index**:
```csharp
builder.Property(e => e.Variables)
    .IsRequired()
    .HasColumnType("jsonb")
    .HasDefaultValue("[]");

builder.HasIndex(e => e.Variables)
    .HasDatabaseName("IX_GlobalEmailTemplates_Variables_Gin")
    .HasMethod("gin");
```

**Array Types**:
```csharp
builder.Property(e => e.TargetSessions)
    .HasColumnType("text[]")
    .HasDefaultValue(Array.Empty<string>());

builder.Property(e => e.RecipientEmails)
    .HasColumnType("text[]")
    .HasDefaultValue(Array.Empty<string>());
```

**UTC Timestamps**:
```csharp
builder.Property(e => e.CreatedAt)
    .IsRequired()
    .HasColumnType("timestamptz")
    .HasDefaultValueSql("NOW()");
```

### 3. **Entity ID Initialization**

**CRITICAL**: Do NOT initialize Id in entity constructors

```csharp
// ❌ WRONG - Breaks EF Core change tracking
public class GlobalEmailTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();  // ❌ DO NOT DO THIS
}

// ✅ CORRECT - Simple property, EF handles generation
public class GlobalEmailTemplate
{
    public Guid Id { get; set; }  // ✅ Let Entity Framework manage ID
}
```

**Database Configuration**:
```csharp
builder.Property(e => e.Id)
    .HasDefaultValueSql("gen_random_uuid()");  // PostgreSQL generates UUID
```

### 4. **Check Constraints**

**All tables require check constraints for data validation**:

```csharp
builder.HasCheckConstraint(
    "CHK_GlobalEmailTemplates_Subject_NotEmpty",
    "LENGTH(TRIM(\"Subject\")) > 0"
);

builder.HasCheckConstraint(
    "CHK_GlobalEmailTemplates_Category",
    "\"Category\" IN (0, 1, 2, 3, 4)"
);

builder.HasCheckConstraint(
    "CHK_SentAdHocEmails_DeliveryStatus",
    "\"DeliveryStatus\" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced')"
);
```

---

## 📝 WHAT BACKEND DEVELOPER NEEDS TO IMPLEMENT

### 1. Entity Classes (3 files)

**Location**: `/apps/api/Features/EmailTemplates/Entities/`

- `GlobalEmailTemplate.cs` - Entity class with properties
- `EventEmailTemplate.cs` - Entity class with properties
- `SentAdHocEmail.cs` - Entity class with properties

**Template**: Complete entity code provided in database-design.md

### 2. Entity Framework Configurations (3 files)

**Location**: `/apps/api/Features/EmailTemplates/Entities/Configuration/`

- `GlobalEmailTemplateConfiguration.cs` - Fluent API configuration
- `EventEmailTemplateConfiguration.cs` - Fluent API configuration
- `SentAdHocEmailConfiguration.cs` - Fluent API configuration

**Template**: Complete configuration code provided in database-design.md

### 3. Migration Script (1 file)

**Location**: `/apps/api/Data/Migrations/`

- `YYYYMMDDHHMMSS_AddEmailTemplatesSystem.cs` - EF Core migration

**Template**: Complete migration Up/Down methods provided in database-design.md

**Critical**:
- Create 3 new tables
- Create all 15 indexes
- Migrate VettingEmailTemplates → GlobalEmailTemplates
- Do NOT drop VettingEmailTemplates table

### 4. Seed Data Service (1 file)

**Location**: `/apps/api/Services/Seeding/`

- `EmailTemplateSeeder.cs` - Seed 16 new default templates

**Critical**:
- Check if Vetting templates already migrated (skip if exist)
- Seed 7 Events templates
- Seed 4 Admin templates
- Seed 4 Incident templates
- Seed 1 Ad Hoc template

**Template Specifications**: Detailed in database-design.md Section 8 (Seed Data Specifications)

### 5. DbContext Registration

**Location**: `/apps/api/Data/ApplicationDbContext.cs`

```csharp
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

## ✅ VALIDATION CHECKLIST

Before proceeding to API implementation, verify:

- [ ] 3 entity classes created with correct property types
- [ ] 3 EF Core configuration classes created with fluent API
- [ ] Migration script created with Up/Down methods
- [ ] VettingEmailTemplates migration SQL included in Up method
- [ ] All 15 indexes created (6 GlobalEmailTemplates, 5 EventEmailTemplates, 4 SentAdHocEmails)
- [ ] GIN index for JSONB Variables field created
- [ ] Partial indexes for EventId and DeliveryStatus created
- [ ] Check constraints for all validation rules added
- [ ] Unique constraints for (Category, TemplateType) and (EventId, TemplateType) added
- [ ] NO foreign key constraint on EventEmailTemplates.GlobalTemplateId
- [ ] CASCADE delete configured for EventEmailTemplates → Events
- [ ] SET NULL configured for SentAdHocEmails → Events
- [ ] RESTRICT configured for all ApplicationUser relationships
- [ ] EmailTemplateSeeder service created with 16 new templates
- [ ] Seed data includes default Subject, HtmlBody, PlainTextBody, Variables for all templates
- [ ] DbContext registration updated with 3 new DbSets
- [ ] Migration compiled successfully: `dotnet build`
- [ ] Migration applied successfully: `dotnet ef database update`
- [ ] Seed data populated successfully
- [ ] Database verification: 22 templates exist in GlobalEmailTemplates (6 migrated + 16 seeded)

---

## 📚 REFERENCE DOCUMENTS

| Document | Location | Purpose |
|----------|----------|---------|
| **Database Design** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/design/database-design.md` | Complete DDL, configurations, seed specs |
| **Functional Specification** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/functional-spec.md` | API endpoints, business logic, DTOs |
| **Functional Spec Handoff** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/handoffs/functional-spec-2025-11-09-handoff.md` | Business rules, pitfalls, success criteria |
| **Entity Framework Patterns** | `/docs/standards-processes/development-standards/entity-framework-patterns.md` | EF Core conventions, UTC DateTime handling |
| **Database Migrations Guide** | `/docs/standards-processes/backend/database-migrations-guide.md` | Migration best practices |

---

## 🎯 SUCCESS CRITERIA

**Database Design Phase Complete When**:
- ✅ All 3 tables created in database
- ✅ All 15 indexes created and optimized
- ✅ VettingEmailTemplates data migrated to GlobalEmailTemplates (6 templates)
- ✅ 16 new default templates seeded (Events, Admin, Incident, Ad Hoc)
- ✅ Total 22 templates in GlobalEmailTemplates table
- ✅ Unique constraints enforced (cannot create duplicate templates)
- ✅ Cascade delete working (Event deletion deletes EventEmailTemplates)
- ✅ SET NULL working (Event deletion preserves SentAdHocEmails with EventId = NULL)
- ✅ JSONB GIN index performance verified (fast variable containment queries)

**Verification Queries**:
```sql
-- Verify 22 templates exist
SELECT "Category", COUNT(*) as template_count
FROM "GlobalEmailTemplates"
GROUP BY "Category"
ORDER BY "Category";
-- Expected: Vetting=6, Events=7, Admin=4, Incident=4, AdHoc=1

-- Verify unique constraint
SELECT COUNT(*) FROM "GlobalEmailTemplates"
WHERE "Category" = 1 AND "TemplateType" = 'Confirmation';
-- Expected: 1 (only one Confirmation template for Events category)

-- Verify GIN index exists
SELECT indexname FROM pg_indexes
WHERE tablename = 'GlobalEmailTemplates' AND indexname LIKE '%Gin%';
-- Expected: IX_GlobalEmailTemplates_Variables_Gin
```

---

## 🚀 NEXT STEPS FOR BACKEND DEVELOPER

### Phase 3: API Implementation

**1. Create Service Layer** (Vertical Slice Pattern):
- `IGlobalEmailTemplateService` + `GlobalEmailTemplateService`
- `IEventEmailTemplateService` + `EventEmailTemplateService`
- `IAdHocEmailService` + `AdHocEmailService`
- `VariableValidationService`

**2. Create DTOs**:
- `GlobalEmailTemplateDto` (response)
- `UpdateGlobalTemplateRequest` (request)
- `EventEmailTemplateDto` (response)
- `UpdateEventTemplateRequest` (request)
- `SendAdHocEmailRequest` (request)
- `SentAdHocEmailDto` (response)

**3. Create API Endpoints**:
- GET `/api/email-templates?category={category}`
- GET `/api/email-templates/{id}`
- PUT `/api/email-templates/{id}`
- GET `/api/events/{eventId}/email-templates`
- GET `/api/events/{eventId}/email-templates/{type}`
- PUT `/api/events/{eventId}/email-templates/{type}`
- DELETE `/api/events/{eventId}/email-templates/{type}`
- POST `/api/email-templates/ad-hoc`
- GET `/api/email-templates/ad-hoc/history?eventId={id}`
- GET `/api/email-templates/ad-hoc/history/{id}`

**4. Implement Copy-on-Edit Logic**:
- EventEmailTemplate created ONLY on PUT (not GET)
- Reset-to-default DELETES EventEmailTemplate record
- GET endpoint merges global + event-specific templates

**5. Verify NSwag Type Generation**:
- Run `cd packages/shared-types && npm run generate`
- Check `packages/shared-types/src/generated/api-types.ts` for new DTOs

**Critical**: Follow patterns from functional-spec.md Section 5 (API Specifications) and Section 6 (Business Logic Services)

---

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Functional Spec Agent
**Current Agent**: Database Designer Agent ✅ COMPLETE
**Next Agent**: Backend Developer Agent
**Next Phase**: Phase 3 - Implementation (API Layer)

**Estimated Backend Effort**: 10-12 hours
- Entity/Configuration classes: 2 hours
- Migration script: 1 hour
- Seed data service: 2 hours
- Service layer (4 services): 4 hours
- API endpoints (10 endpoints): 3 hours

**Critical Success Factor**: Backend developer MUST use provided entity/configuration code exactly as specified to ensure database schema matches design.

---

**END OF HANDOFF DOCUMENT**
