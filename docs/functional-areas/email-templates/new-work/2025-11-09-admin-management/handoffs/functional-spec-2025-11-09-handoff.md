# AGENT HANDOFF DOCUMENT

## Phase: Functional Specification (Phase 2)
## Date: 2025-11-09
## Feature: Email Templates Admin Management

---

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

### 1. **Copy-on-Edit Pattern**: Event-specific templates created ONLY when user saves changes
   - ✅ Correct: EventEmailTemplate record created when user clicks "Save Template"
   - ❌ Wrong: Pre-creating EventEmailTemplate records for all events when global template exists

**Why**: Minimizes database bloat. Most events use default templates unchanged.

**Implementation**:
```csharp
// On PUT /api/events/{eventId}/email-templates/{type}
var existingTemplate = await _context.EventEmailTemplates
    .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == type);

if (existingTemplate == null)
{
    // CREATE new event-specific template (copy-on-edit)
    var newTemplate = new EventEmailTemplate { /* ... */ };
    _context.EventEmailTemplates.Add(newTemplate);
}
else
{
    // UPDATE existing event-specific template
    existingTemplate.Subject = request.Subject;
    // ...
}
```

---

### 2. **Reset to Default = DELETE EventEmailTemplate**: No cascade to global templates
   - ✅ Correct: DELETE EventEmailTemplate record, future loads fetch global template
   - ❌ Wrong: Updating EventEmailTemplate to copy global template content (creates permanent record)

**Why**: Reset should revert to using global defaults, not create a static copy.

**Implementation**:
```csharp
// On DELETE /api/events/{eventId}/email-templates/{type}
var template = await _context.EventEmailTemplates
    .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == type);

if (template != null)
{
    _context.EventEmailTemplates.Remove(template);  // DELETE record
    await _context.SaveChangesAsync();
}
```

---

### 3. **Variable Validation Warns, Never Blocks**: Unknown variables show warnings but allow save
   - ✅ Correct: Show yellow Alert with unknown variables, Save button enabled
   - ❌ Wrong: Disable Save button when unknown variables detected (blocks admins)

**Why**: Admins might intentionally use placeholder variables like `{{custom_field}}` for future expansion.

**Frontend Implementation**:
```tsx
{invalidVariables.length > 0 && (
  <Alert icon={<IconAlertCircle />} color="yellow" variant="light">
    Unknown variables: {invalidVariables.join(', ')}
  </Alert>
)}

<Button
  className="btn btn-primary"
  onClick={handleSave}
  loading={isSaving}
  // NO disabled={invalidVariables.length > 0}  ← WRONG
>
  Save Template
</Button>
```

---

### 4. **NSwag Auto-Generation Is THE Solution**: NEVER manually create DTO TypeScript interfaces
   - ✅ Correct: `import type { components } from '@witchcityrope/shared-types'`
   - ❌ Wrong: `interface GlobalEmailTemplateDto { id: string; ... }` in React code

**Why**: Manual interfaces create field name mismatches, silent failures, undefined values in UI.

**Critical Pattern**:
```typescript
// ✅ CORRECT - Auto-generated types
import type { components } from '@witchcityrope/shared-types';
export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto'];

// ❌ WRONG - Manual interface
interface GlobalEmailTemplateDto {
  id: string;
  subject: string;
  // ...
}
```

---

### 5. **HTML Sanitization on Save**: Strip dangerous tags BEFORE storing in database
   - ✅ Correct: Sanitize HtmlBody in API service before `SaveChangesAsync()`
   - ❌ Wrong: Sanitizing only at send-time (allows XSS payloads in database)

**Why**: Defense in depth. Database should never contain dangerous HTML.

**Implementation**:
```csharp
public async Task<Result<GlobalEmailTemplateDto>> UpdateAsync(
    Guid id,
    UpdateGlobalTemplateRequest request,
    Guid updatedByUserId)
{
    var template = await _context.GlobalEmailTemplates.FindAsync(id);

    // Sanitize BEFORE saving
    var sanitizedHtml = SanitizeHtml(request.HtmlBody);

    template.HtmlBody = sanitizedHtml;  // Store sanitized version
    await _context.SaveChangesAsync();
}

private static string SanitizeHtml(string html)
{
    // Strip: <script>, <iframe>, <object>, <embed>, <form>
    // Allow: <p>, <h1-h6>, <strong>, <em>, <a>, <ul>, <ol>, <li>, <br>
    // Use HtmlSanitizer library or custom regex
    return html;
}
```

---

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| **Functional Specification** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/functional-spec.md` | Full document - Complete technical implementation |
| **DTO Alignment Strategy** | `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` | Lines 85-110: NSwag is THE solution |
| **Domain Layer Architecture** | `/docs/architecture/react-migration/domain-layer-architecture.md` | Lines 144-179: packages/shared-types structure |
| **API Architecture** | `/docs/architecture/API-ARCHITECTURE-OVERVIEW.md` | Lines 85-134: Simple vertical slice pattern |
| **Business Requirements** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/business-requirements.md` | Lines 1-45: Feature overview |
| **UI Design** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/design/ui-design.md` | Lines 80-1517: Complete UI specifications |
| **UI Designer Handoff** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/handoffs/ui-designer-2025-11-09-handoff.md` | Lines 205-290: API endpoint requirements |
| **Email Template Architecture** | `/session-work/2025-11-09/email-template-architecture-exploration.md` | Lines 23-51: VettingEmailTemplate pattern |

---

## 🚨 KNOWN PITFALLS

### 1. **Using EventForm Email Tab Mock Data**: Existing EventForm email tab has hardcoded placeholder data
   - **Why it happens**: EventForm.tsx lines 1155-1435 show UI but NO backend integration
   - **How to avoid**: Replace mock functions (`getTemplateSubject`, `getTemplateContent`) with API calls to `/api/events/{eventId}/email-templates`

**Current Code (WRONG)**:
```typescript
// EventForm.tsx lines 1320-1340 (MOCK)
const getTemplateSubject = () => {
  if (activeTemplate === 'confirmation') {
    return 'Your ticket for {{event_title}}';
  }
  // ... hardcoded subjects
};
```

**Correct Implementation**:
```typescript
// EventForm.tsx (NEW)
const { data: templates } = useQuery({
  queryKey: ['event-templates', eventId],
  queryFn: () => emailTemplatesApi.getForEvent(eventId),
});

const selectedTemplate = templates?.find(t => t.templateType === activeTemplate);
const subject = selectedTemplate?.subject || '';
```

---

### 2. **Manually Creating DTO TypeScript Interfaces**: Temptation to define types manually instead of using NSwag
   - **Why it happens**: Developers used to creating types manually, unfamiliar with NSwag workflow
   - **How to avoid**: ALWAYS check `packages/shared-types/src/generated/` for auto-generated types BEFORE writing any interface

**Violation Example (WRONG)**:
```typescript
// ❌ NEVER DO THIS
interface GlobalEmailTemplateDto {
  id: string;
  category: string;
  subject: string;
  // ...
}
```

**Correct Pattern**:
```typescript
// ✅ ALWAYS DO THIS
import type { components } from '@witchcityrope/shared-types';
export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto'];
```

**Verification**:
- After backend changes: Run `cd packages/shared-types && npm run generate`
- Check `packages/shared-types/src/generated/api-types.ts` for new types
- Import from `@witchcityrope/shared-types` in React components

---

### 3. **Copy-on-Edit Timing**: Creating EventEmailTemplate on page load instead of on save
   - **Why it happens**: Misunderstanding "copy-on-edit" as "copy when editor opens"
   - **How to avoid**: EventEmailTemplate created ONLY when user clicks "Save Template" button

**Wrong Implementation**:
```csharp
// ❌ WRONG - Creating on GET
public async Task<EventEmailTemplateDto> GetOrCreateEventTemplateAsync(Guid eventId, string type)
{
    var existing = await _context.EventEmailTemplates.FindAsync(eventId, type);

    if (existing == null)
    {
        // ❌ Creating record on read operation
        existing = new EventEmailTemplate { /* ... */ };
        _context.EventEmailTemplates.Add(existing);
        await _context.SaveChangesAsync();
    }

    return existing;
}
```

**Correct Implementation**:
```csharp
// ✅ CORRECT - Create on PUT only
// GET endpoint returns merged view (global + event-specific)
public async Task<List<EventEmailTemplateDto>> GetForEventAsync(Guid eventId)
{
    var globalTemplates = await _context.GlobalEmailTemplates
        .Where(t => t.Category == EmailCategory.Events).ToListAsync();

    var eventTemplates = await _context.EventEmailTemplates
        .Where(t => t.EventId == eventId).ToListAsync();

    // Merge: If event-specific exists, use it; otherwise use global
    // NO records created here
}

// PUT endpoint creates record
public async Task<EventEmailTemplateDto> UpdateEventTemplateAsync(Guid eventId, string type, UpdateEventTemplateRequest request)
{
    var existing = await _context.EventEmailTemplates.FindAsync(eventId, type);

    if (existing == null)
    {
        // ✅ Create new record only when user saves
        existing = new EventEmailTemplate { /* ... */ };
        _context.EventEmailTemplates.Add(existing);
    }

    // Update fields
    await _context.SaveChangesAsync();
}
```

---

### 4. **GlobalTemplate Foreign Key Constraint**: Adding foreign key constraint from EventEmailTemplate.GlobalTemplateId → GlobalEmailTemplates.Id
   - **Why it happens**: Standard practice to enforce referential integrity
   - **How to avoid**: GlobalTemplateId is reference ONLY, NOT enforced foreign key (global template might be deleted/changed)

**Database Configuration (CORRECT)**:
```csharp
// EventEmailTemplateConfiguration.cs
builder.Property(e => e.GlobalTemplateId)
    .IsRequired();

// ✅ NO foreign key constraint
// builder.HasOne(e => e.GlobalTemplate)  ← DO NOT ADD THIS

// Navigation property nullable (optional)
builder.Navigation(e => e.GlobalTemplate)
    .IsRequired(false);
```

**Why**: Global template might be deleted or replaced. EventEmailTemplate should persist (it has its own content copy).

---

### 5. **Variable Substitution at Wrong Time**: Replacing variables when saving template instead of when sending email
   - **Why it happens**: Misunderstanding where variable substitution happens
   - **How to avoid**: Variables stored as-is in database (`{{variable_name}}`), replaced ONLY at send-time with actual values

**Wrong Implementation**:
```csharp
// ❌ WRONG - Replacing variables on save
public async Task UpdateAsync(Guid id, UpdateGlobalTemplateRequest request)
{
    var template = await _context.GlobalEmailTemplates.FindAsync(id);

    // ❌ Replacing variables when saving
    var renderedHtml = request.HtmlBody
        .Replace("{{attendee_name}}", "John Doe");  // WRONG TIME

    template.HtmlBody = renderedHtml;  // Stores "John Doe" instead of "{{attendee_name}}"
}
```

**Correct Implementation**:
```csharp
// ✅ CORRECT - Store variables as-is
public async Task UpdateAsync(Guid id, UpdateGlobalTemplateRequest request)
{
    var template = await _context.GlobalEmailTemplates.FindAsync(id);

    template.HtmlBody = request.HtmlBody;  // ✅ Stores "{{attendee_name}}" as-is
    await _context.SaveChangesAsync();
}

// ✅ CORRECT - Replace variables at send-time
public async Task SendEmailAsync(EventRegistration registration, Event eventDetails)
{
    var template = await GetTemplateAsync("Confirmation");

    var renderedHtml = template.HtmlBody
        .Replace("{{attendee_name}}", registration.AttendeeName)
        .Replace("{{event_title}}", eventDetails.Title);

    await _sendGridClient.SendEmailAsync(renderedHtml);
}
```

---

## ✅ VALIDATION CHECKLIST

Before proceeding to implementation, verify:

- [ ] **Copy-on-Edit**: EventEmailTemplate created ONLY on PUT /api/events/{eventId}/email-templates/{type}
- [ ] **Reset-to-Default**: DELETE /api/events/{eventId}/email-templates/{type} removes EventEmailTemplate record
- [ ] **Variable Validation**: Unknown variables show warning, Save button remains enabled
- [ ] **NSwag Types**: All DTO interfaces imported from `@witchcityrope/shared-types` (NEVER manual interfaces)
- [ ] **HTML Sanitization**: HtmlBody sanitized in API service before `SaveChangesAsync()`
- [ ] **Variable Substitution**: Variables stored as `{{variable_name}}` in database, replaced at send-time
- [ ] **No Foreign Key**: EventEmailTemplate.GlobalTemplateId is reference only (NOT foreign key constraint)
- [ ] **Merge Logic**: GET /api/events/{eventId}/email-templates returns global + event-specific merged view
- [ ] **Badge Logic**: Frontend shows "✓ Customized" if EventEmailTemplate exists, "(Default)" if using global
- [ ] **Vetting Migration**: VettingEmailTemplates copied to GlobalEmailTemplates WHERE Category = Vetting

---

## 🔄 DISCOVERED CONSTRAINTS

### 1. **Existing Code**: VettingEmailTemplate system fully functional
   - **Location**: `/apps/api/Features/Vetting/Entities/VettingEmailTemplate.cs`
   - **Impact**: Proven pattern to replicate for GlobalEmailTemplate
   - **Required Changes**: Migrate VettingEmailTemplates → GlobalEmailTemplates WHERE Category = Vetting

**Migration Strategy**:
```csharp
// In migration script
migrationBuilder.Sql(@"
    INSERT INTO ""GlobalEmailTemplates""
        (""Id"", ""Category"", ""TemplateType"", ""Subject"", ""HtmlBody"", ""PlainTextBody"", ""Variables"", ""IsActive"", ""Version"", ""CreatedAt"", ""UpdatedAt"", ""UpdatedBy"")
    SELECT
        ""Id"",
        0 AS ""Category"",  -- EmailCategory.Vetting
        CAST(""TemplateType"" AS VARCHAR(50)),
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
```

---

### 2. **Existing Code**: EventForm Emails tab has UI but NO backend integration
   - **Location**: `/apps/web/src/components/events/EventForm.tsx` lines 1155-1435
   - **Impact**: UI design approved, backend API must match UI expectations
   - **Required Changes**: Replace mock functions with API calls to new endpoints

**Mock Code to Replace**:
```typescript
// EventForm.tsx lines 1320-1435 (MOCK DATA)
const getTemplateSubject = () => { /* hardcoded */ };
const getTemplateContent = () => { /* hardcoded */ };
```

**New API Integration**:
```typescript
// EventForm.tsx (NEW)
const { data: templates } = useQuery({
  queryKey: ['event-templates', eventId],
  queryFn: () => emailTemplatesApi.getForEvent(eventId),
});
```

---

### 3. **Existing Code**: MantineTiptapEditor integration in Vetting admin page
   - **Location**: `/apps/web/src/features/admin/vetting/pages/EmailTemplates.tsx`
   - **Impact**: Reuse exact editor pattern for new EmailCategoryPanel component
   - **Required Changes**: Extract MantineTiptapEditor + variable reference display into reusable TemplateEditor component

**Pattern to Reuse**:
```tsx
<MantineTiptapEditor
  value={htmlBody}
  onChange={setHtmlBody}
  placeholder="Enter email template content..."
  minRows={12}
/>

<Text size="xs" c="dimmed" mt="xs">
  Available variables: {{scene_name}}, {{application_number}}, ...
</Text>
```

---

## 📊 DATA MODEL DECISIONS

### GlobalEmailTemplates Table

```sql
CREATE TABLE "GlobalEmailTemplates" (
    "Id" UUID PRIMARY KEY,
    "Category" INTEGER NOT NULL,  -- 0=Vetting, 1=Events, 2=Admin, 3=Incident, 4=AdHoc
    "TemplateType" VARCHAR(50) NOT NULL,  -- Enum value as string
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,
    "Variables" JSONB NOT NULL DEFAULT '{}'::jsonb,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL,
    "UpdatedBy" UUID NOT NULL,

    CONSTRAINT "UQ_GlobalEmailTemplates_Category_Type"
        UNIQUE ("Category", "TemplateType")
);
```

**Business Logic**:
- **Unique Constraint**: Only ONE template per type per category
- **Soft Delete**: IsActive = false hides template (NEVER hard delete)
- **Version Tracking**: Increment on every update for audit trail
- **Variables JSONB**: Store allowed variables as JSON array for PostgreSQL optimization

---

### EventEmailTemplates Table

```sql
CREATE TABLE "EventEmailTemplates" (
    "Id" UUID PRIMARY KEY,
    "EventId" UUID NOT NULL,
    "GlobalTemplateId" UUID NOT NULL,  -- Reference only, NOT FK constraint
    "TemplateType" VARCHAR(50) NOT NULL,
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,
    "TargetSessions" TEXT[] NOT NULL DEFAULT '{}',
    "IsCustomized" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL,
    "UpdatedBy" UUID NOT NULL,

    CONSTRAINT "UQ_EventEmailTemplates_EventId_Type"
        UNIQUE ("EventId", "TemplateType"),

    CONSTRAINT "FK_EventEmailTemplates_Events_EventId"
        FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE
);
```

**Business Logic**:
- **Copy-on-Edit**: Created ONLY when user saves changes
- **Reset-to-Default**: DELETE record to revert to global
- **Cascade Delete**: Deleting Event deletes all EventEmailTemplates
- **No Global FK**: GlobalTemplateId reference only (NOT enforced)

---

### SentAdHocEmails Table

```sql
CREATE TABLE "SentAdHocEmails" (
    "Id" UUID PRIMARY KEY,
    "Subject" VARCHAR(200) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "PlainTextBody" TEXT NOT NULL,
    "RecipientGroup" VARCHAR(100) NOT NULL,
    "RecipientEmails" TEXT[] NOT NULL,
    "RecipientCount" INTEGER NOT NULL,
    "EventId" UUID NULL,
    "SendGridMessageId" VARCHAR(100) NULL,
    "DeliveryStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "SentAt" TIMESTAMP NOT NULL,
    "SentBy" UUID NOT NULL,

    CONSTRAINT "FK_SentAdHocEmails_Events_EventId"
        FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE SET NULL
);
```

**Business Logic**:
- **Audit Trail**: NEVER deleted (permanent record)
- **Read-Only**: No updates after creation
- **Nullable EventId**: Not all ad-hoc emails are event-related

---

## 🎯 SUCCESS CRITERIA

### Test Case 1: Admin Updates Global Vetting Template

**Input**:
1. Admin navigates to `/admin/email-templates?tab=vetting`
2. Clicks "Application Received" card
3. Edits subject: "Your vetting application - Next steps"
4. Clicks "Save Template"

**Expected Output**:
- PUT /api/email-templates/{id} called with updated subject
- Template Version incremented from 1 to 2
- UpdatedAt timestamp updated
- UpdatedBy = current admin user ID
- Success notification: "Template saved successfully"
- Editor panel closes

---

### Test Case 2: Event Organizer Customizes Event Template (Copy-on-Edit)

**Input**:
1. Event organizer navigates to `/events/{eventId}/edit` (Emails tab)
2. Clicks "Confirmation Email" card (badge shows "(Default)")
3. Edits subject: "Your Advanced Harnesses Workshop Ticket - Pre-Class Homework"
4. Clicks "Save Template"

**Expected Output**:
- PUT /api/events/{eventId}/email-templates/Confirmation called
- NEW EventEmailTemplate record created in database
- EventId = current event ID
- TemplateType = "Confirmation"
- IsCustomized = true
- Badge changes from "(Default)" to "✓ Customized"
- Success notification: "Template saved successfully"

**Database Verification**:
```sql
SELECT * FROM "EventEmailTemplates"
WHERE "EventId" = '{eventId}' AND "TemplateType" = 'Confirmation';

-- Expected: 1 row returned (newly created)
```

---

### Test Case 3: Event Organizer Resets to Default

**Input**:
1. Event organizer navigates to `/events/{eventId}/edit` (Emails tab)
2. Clicks "Confirmation Email" card (badge shows "✓ Customized")
3. Clicks "Reset to Default" button
4. Confirms in modal: "Yes, reset to default"

**Expected Output**:
- DELETE /api/events/{eventId}/email-templates/Confirmation called
- EventEmailTemplate record DELETED from database
- Editor content reverts to global template
- Badge changes from "✓ Customized" to "(Default)"
- Success notification: "Template reset to default"

**Database Verification**:
```sql
SELECT * FROM "EventEmailTemplates"
WHERE "EventId" = '{eventId}' AND "TemplateType" = 'Confirmation';

-- Expected: 0 rows returned (deleted)
```

---

### Test Case 4: Variable Validation Warning (Non-Blocking)

**Input**:
1. Admin navigates to `/admin/email-templates?tab=events`
2. Clicks "Confirmation Email" card
3. Edits HTML body, adds `{{invalid_variable}}`
4. Observes warning appears

**Expected Output**:
- Yellow Alert component displays: "Unknown Variables Detected"
- Message: "{{invalid_variable}}"
- Suggested: "Available variables: {{attendee_name}}, {{event_title}}, ..."
- Save button remains ENABLED (not disabled)
- User can click "Save Template" and save successfully

**Frontend Validation**:
```typescript
const extractedVariables = extractVariables(htmlBody);
// ["{{attendee_name}}", "{{event_title}}", "{{invalid_variable}}"]

const allowedVariables = ["{{attendee_name}}", "{{event_title}}", ...];
const invalidVariables = extractedVariables.filter(v => !allowedVariables.includes(v));
// ["{{invalid_variable}}"]

// Show warning, but allow save
```

---

### Test Case 5: NSwag Type Generation

**Input**:
1. Backend developer adds `GlobalEmailTemplateDto` to API
2. Runs OpenAPI generation
3. Runs `cd packages/shared-types && npm run generate`

**Expected Output**:
- File created: `packages/shared-types/src/generated/api-types.ts`
- Contains `GlobalEmailTemplateDto` interface
- Frontend imports: `import type { components } from '@witchcityrope/shared-types'`
- Type alias: `export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto']`

**Verification**:
```typescript
// ✅ CORRECT
import type { components } from '@witchcityrope/shared-types';
export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto'];

const template: GlobalEmailTemplateDto = {
  id: '...',
  category: 'Events',
  subject: '...',
  // TypeScript autocomplete works, field names match backend
};
```

---

## ⚠️ DO NOT IMPLEMENT

- ❌ **DO NOT** create EventEmailTemplate records on page load (copy-on-edit means create on save only)
- ❌ **DO NOT** add foreign key constraint from EventEmailTemplate.GlobalTemplateId → GlobalEmailTemplates.Id
- ❌ **DO NOT** manually create TypeScript DTO interfaces (use NSwag auto-generation)
- ❌ **DO NOT** disable Save button when unknown variables detected (warnings only, not blocking)
- ❌ **DO NOT** replace variables when saving templates (variable substitution happens at send-time)
- ❌ **DO NOT** hard delete global templates (use IsActive = false for soft delete)
- ❌ **DO NOT** store rendered HTML in database (store templates with `{{variables}}`)
- ❌ **DO NOT** assume all templates work the same (5 categories with different variable sets)
- ❌ **DO NOT** create generic "email template" entity (use separate GlobalEmailTemplates + EventEmailTemplates)
- ❌ **DO NOT** use MediatR or repository pattern (direct Entity Framework access per vertical slice architecture)

---

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| **Global Template** | Default email template in GlobalEmailTemplates table, used by all events unless overridden | Vetting "Application Received" template used by all applications |
| **Event-Specific Template** | Customized email template in EventEmailTemplates table, overrides global for specific event | "Advanced Harnesses Workshop" has custom confirmation email with pre-class homework |
| **Copy-on-Edit** | Pattern where EventEmailTemplate created ONLY when user saves changes (not pre-created) | User clicks "Edit" on confirmation email → NO record created until "Save" clicked |
| **Reset to Default** | Delete EventEmailTemplate record, revert to global template | User clicks "Reset to Default" → EventEmailTemplate deleted, global template used |
| **Badge Indicator** | UI label showing "✓ Customized" (green) or "(Default)" (gray) on template cards | EventForm Emails tab shows customization status per template |
| **Variable Validation** | Check `{{variable_name}}` patterns against category-specific allowed variables, show warnings if unknown | Template contains `{{invalid}}` → Warning appears, save not blocked |
| **Category** | High-level email grouping: Vetting, Events, Admin, Incident, Ad Hoc | Events category has 7 template types (Confirmation, Reminder1Day, etc.) |
| **Template Type** | Specific email purpose within category (stored as enum string) | EventTemplateType.Confirmation stored as "Confirmation" |
| **Variable Substitution** | Replace `{{variable_name}}` with actual values at send-time (not when saving template) | `{{attendee_name}}` → "John Doe" when email sent |
| **HTML Sanitization** | Strip dangerous tags (`<script>`, `<iframe>`) before saving to database | User pastes `<script>alert('XSS')</script>` → Stripped before save |

---

## 🔗 NEXT AGENT INSTRUCTIONS

### For Database Designer (Phase 3 - Database Schema):

**FIRST**: Read Functional Specification document:
- **Location**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/functional-spec.md`
- **Critical Sections**:
  - Lines 221-434: Database Schema (tables, indexes, constraints)
  - Lines 436-593: Entity Models (C# entities with full documentation)

**SECOND**: Review existing Vetting pattern:
- **Location**: `/apps/api/Features/Vetting/Entities/VettingEmailTemplate.cs`
- **Pattern**: JSONB Variables field, GIN index, unique constraint on TemplateType
- **Replicate**: Same patterns for GlobalEmailTemplates table

**THIRD**: Create migration script:
- Create 3 new tables: GlobalEmailTemplates, EventEmailTemplates, SentAdHocEmails
- Migrate VettingEmailTemplates → GlobalEmailTemplates WHERE Category = Vetting
- Seed 22 default templates (6 Vetting + 7 Events + 4 Admin + 4 Incident + 1 Ad Hoc)
- Create indexes for performance (Category, EventId, SentAt)

**FOURTH**: Validate query patterns:
- Verify unique constraints prevent duplicates
- Test cascade delete (Event deletion deletes EventEmailTemplates)
- Check JSONB GIN index performance for Variables field

**CRITICAL**: DO NOT add foreign key constraint from EventEmailTemplate.GlobalTemplateId → GlobalEmailTemplates.Id (reference only)

---

### For Backend Developer (Phase 3 - API Implementation):

**FIRST**: Read Functional Specification document:
- **Location**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/functional-spec.md`
- **Critical Sections**:
  - Lines 595-1025: API Specifications (10 endpoints with request/response examples)
  - Lines 1027-1316: Business Logic Services (GlobalEmailTemplateService, EventEmailTemplateService, VariableValidationService)

**SECOND**: Review existing Vetting service:
- **Location**: `/apps/api/Features/Vetting/Services/VettingEmailService.cs`
- **Patterns**: SendGrid integration, variable substitution, email logging
- **Replicate**: Same patterns for EventEmailService

**THIRD**: Implement vertical slice architecture:
- Direct Entity Framework access (NO MediatR, NO repository pattern)
- Simple service methods returning `Result<T>`
- Minimal API endpoints with OpenAPI annotations
- DTOs with XML documentation for NSwag generation

**FOURTH**: Implement copy-on-edit logic:
- EventEmailTemplate created ONLY on PUT (not GET)
- Reset-to-default DELETES EventEmailTemplate record
- GET endpoint merges global + event-specific templates

**CRITICAL**: Run `cd packages/shared-types && npm run generate` after adding DTOs to verify TypeScript generation

---

### For React Developer (Phase 3 - Frontend Implementation):

**FIRST**: Read UI Design document:
- **Location**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/design/ui-design.md`
- **Critical Sections**:
  - Lines 80-517: Component Specifications (EmailTemplatesAdminPage, EmailCategoryPanel)
  - Lines 950-1162: Interaction Patterns (template selection, variable validation, save workflow)

**SECOND**: Review UI Designer Handoff:
- **Location**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/handoffs/ui-designer-2025-11-09-handoff.md`
- **Critical Sections**:
  - Lines 399-680: Mantine components with exact props
  - Lines 682-703: Recommended component structure

**THIRD**: Implement NSwag type imports:
- NEVER create manual DTO interfaces
- ALWAYS import from `@witchcityrope/shared-types`
- Example: `import type { components } from '@witchcityrope/shared-types'`

**FOURTH**: Replace EventForm mock data:
- Delete mock functions: `getTemplateSubject`, `getTemplateContent`
- Replace with API calls: `emailTemplatesApi.getForEvent(eventId)`
- Add badge logic: "✓ Customized" if EventEmailTemplate exists, "(Default)" if using global

**CRITICAL**: Use React Query for all API calls (`useQuery`, `useMutation`), NOT manual fetch

---

### For Test Developer (Phase 4 - Testing):

**FIRST**: Read Testing Requirements:
- **Location**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/functional-spec.md`
- **Critical Sections**:
  - Lines 1565-1671: Testing Requirements (unit, integration, E2E)

**SECOND**: Implement test scenarios from Success Criteria:
- Test Case 1: Admin updates global Vetting template
- Test Case 2: Event organizer customizes event template (copy-on-edit)
- Test Case 3: Event organizer resets to default
- Test Case 4: Variable validation warning (non-blocking)
- Test Case 5: NSwag type generation

**THIRD**: Verify critical business rules:
- Copy-on-edit creates EventEmailTemplate ONLY on save
- Reset-to-default DELETES EventEmailTemplate record
- Variable validation warns but never blocks save
- HTML sanitization strips dangerous tags

**CRITICAL**: 100% test coverage for copy-on-edit and reset-to-default workflows (most complex logic)

---

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: UI Designer Agent
**Previous Phase Completed**: 2025-11-09 (Phase 2 - UI Design)
**Key Finding**: EventForm Emails tab UI exists but has NO backend integration (mock data)

**Next Agent Should Be**: Database Designer OR Backend Developer (parallel work possible)
**Next Phase**: Phase 3 - Implementation
**Estimated Effort**:
- Database Schema: 4-6 hours (3 tables + migration + seeding)
- Backend API: 10-12 hours (10 endpoints + 3 services + DTOs)
- Frontend Implementation: 15-20 hours (admin page + EventForm integration + variable validation)
- Testing: 8-10 hours (unit + integration + E2E)
- **Total**: ~40-48 hours (5-6 days with proper orchestration)

**Critical Success Factor**: Frontend developer MUST use NSwag auto-generated types (NEVER manual interfaces)

---

**END OF HANDOFF DOCUMENT**
