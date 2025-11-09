# AGENT HANDOFF DOCUMENT

## Phase: Business Requirements (Phase 1)
## Date: 2025-11-09
## Feature: Email Templates Admin Management

---

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

### 1. **Two-Tier Template System**: Global templates + Event-specific overrides

- ✅ **Correct**: Global templates in GlobalEmailTemplates table, event-specific overrides in EventEmailTemplates table, event-specific templates take precedence when they exist
- ❌ **Wrong**: Single table for all templates, event organizers editing global templates directly, no inheritance model

**Why This Matters**: Event organizers must customize templates without affecting other events. Global template changes must NOT overwrite event-specific customizations.

---

### 2. **Copy-on-Edit Pattern**: Event templates created only when customized

- ✅ **Correct**: Display global template initially, create EventEmailTemplate record only when user saves changes, delete EventEmailTemplate to revert to global
- ❌ **Wrong**: Pre-creating EventEmailTemplate records for all events, copying global template on event creation, updating global template updates event-specific templates

**Why This Matters**: Minimizes database bloat. Most events use global defaults (no customization needed). Only ~20% of events expected to customize templates.

---

### 3. **Fixed Enums for Template Types**: Type-safe, not flexible strings

- ✅ **Correct**: Use C# enums (VettingTemplateType, EventTemplateType, etc.), store as integer or enum string in database, compile-time type checking
- ❌ **Wrong**: Free-text TemplateType field, admin-defined template types, string concatenation for template identifiers

**Why This Matters**: Type safety prevents typos, ensures API contracts are clear, enables auto-generated TypeScript types via NSwag.

**Enums**:
```csharp
public enum VettingTemplateType { ApplicationReceived, InterviewApproved, Approved, OnHold, Denied, InterviewReminder }
public enum EventTemplateType { Confirmation, Reminder1Day, Reminder1Week, Cancellation, WaitlistNotification, PostEventSurvey, ScheduleChange }
public enum AdminTemplateType { SystemAlert, MaintenanceNotice, SecurityAlert, AccountSuspension }
public enum IncidentTemplateType { ReportReceived, StatusUpdate, ResolutionNotice, FollowUpRequest }
public enum AdHocTemplateType { Custom }
```

---

### 4. **Variable Substitution at Send-Time**: Never stored in database

- ✅ **Correct**: Templates contain `{{variable_name}}` placeholders, variables replaced at send-time with actual user data, HTML-escaped before insertion
- ❌ **Wrong**: Storing personalized content in database, replacing variables when saving template, allowing raw HTML in variable values

**Why This Matters**: Security (prevents XSS), data integrity (template changes apply to future emails), performance (no redundant storage).

**Example**:
```
Template: "Hi {{attendee_name}}, your ticket for {{event_title}} is confirmed."
Send-time: "Hi Sarah Johnson, your ticket for Advanced Harnesses Workshop is confirmed."
Database: Still stores template with {{variables}} intact
```

---

### 5. **Admin-Only Global Templates**: Event organizers customize event-specific only

- ✅ **Correct**: Only Administrator role can access `/admin/email-templates`, event organizers customize only their own events via EventForm Emails tab
- ❌ **Wrong**: Event organizers accessing global templates, Teachers editing Admin/Incident templates, no permission checks on template updates

**Why This Matters**: Communication consistency (global defaults controlled by admins), security (prevent unauthorized template changes), role clarity (admins manage platform, organizers manage events).

---

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| **Business Requirements** | `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/requirements/business-requirements.md` | Entire document (comprehensive requirements) |
| **Approved Implementation Plan** | `/session-work/2025-11-09/email-templates-admin-approved-plan.md` | Database schema (lines 74-183), Enums (lines 189-236), UI specs (lines 240-314) |
| **DTO Alignment Strategy** | `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` | NSwag auto-generation (CRITICAL - no manual TypeScript interfaces) |
| **Vetting Email Templates** | N/A (existing code) | Check `VettingEmailTemplates` entity, sending logic (to be migrated) |
| **MantineTiptapEditor** | N/A (existing component) | Used in CMS feature, reuse for template editor |

---

## 🚨 KNOWN PITFALLS

### Pitfall 1: Creating Manual TypeScript Interfaces for DTOs

**Why it happens**: Frontend developers familiar with manually creating interfaces might create `GlobalEmailTemplateDto` interface by hand.

**How to avoid**:
1. Backend creates C# DTOs with OpenAPI annotations
2. Run `npm run generate:types` (NSwag pipeline)
3. Frontend imports from `@witchcityrope/shared-types`
4. NEVER create manual DTO interfaces

**Example**:
```typescript
// ❌ WRONG - Manual interface
interface GlobalEmailTemplateDto {
  id: string;
  subject: string;
  // ...
}

// ✅ CORRECT - Generated type
import type { components } from '@witchcityrope/shared-types';
export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto'];
```

---

### Pitfall 2: Overwriting Event-Specific Templates When Global Changes

**Why it happens**: Assumption that global template changes should "cascade" to all events.

**How to avoid**:
1. When updating GlobalEmailTemplate, do NOT touch EventEmailTemplates table
2. Event-specific templates are independent copies (not linked by foreign key)
3. GlobalTemplateId in EventEmailTemplates is for reference only (not foreign key constraint)

**Business Rule**: Event organizers explicitly customize templates. Global changes do NOT affect existing customizations.

---

### Pitfall 3: Allowing HTML Injection via Variable Values

**Why it happens**: Variable values come from user input (event names, attendee names, etc.) which could contain malicious HTML.

**How to avoid**:
1. Always HTML-escape variable values before insertion
2. Never use `innerHTML` or equivalent for variable replacement
3. Use text-based replacement with HTML escaping
4. Example: `attendee_name = "<script>alert('XSS')</script>"` → `"&lt;script&gt;alert('XSS')&lt;/script&gt;"`

---

### Pitfall 4: Vetting Template Migration Breaking Existing Functionality

**Why it happens**: Existing Vetting email sending logic references VettingEmailTemplates table directly.

**How to avoid**:
1. Create database migration that copies VettingEmailTemplates → GlobalEmailTemplates
2. Update Vetting email sending service to query GlobalEmailTemplates (Category = Vetting)
3. Keep VettingEmailTemplates table as read-only backup (backwards compatibility)
4. Delete old `/admin/vetting/email-templates` page AFTER confirming new system works
5. Test Vetting emails thoroughly before production deployment

**Migration Validation**:
- [ ] All 6 Vetting templates migrated to GlobalEmailTemplates
- [ ] Vetting email sending uses GlobalEmailTemplates
- [ ] Old table remains (read-only, no writes)
- [ ] New admin page shows Vetting templates correctly
- [ ] Test email sending for all 6 Vetting template types

---

### Pitfall 5: Event Emails Tab Showing Outdated Template Content

**Why it happens**: Event-specific template created, then global template updated, UI shows stale event-specific content.

**How to avoid**:
1. When loading event templates, check if EventEmailTemplate exists
2. If exists, show event-specific content with "✓ Customized" badge
3. If not exists, fetch and display GlobalEmailTemplate with "(Default)" badge
4. "Reset to Default" deletes EventEmailTemplate, then re-fetch global template
5. Never cache template content in frontend (always fetch from API)

---

## ✅ VALIDATION CHECKLIST

Before proceeding to Phase 2 (Design), verify:

- [ ] Database schema supports two-tier template system (GlobalEmailTemplates + EventEmailTemplates)
- [ ] Template types defined as enums (not strings)
- [ ] Variable substitution happens at send-time (not save-time)
- [ ] Permission model enforced (Administrator for global, event organizers for event-specific)
- [ ] Copy-on-edit pattern documented (EventEmailTemplate created only when customized)
- [ ] "Reset to Default" deletes EventEmailTemplate record (documented behavior)
- [ ] Vetting template migration plan includes backwards compatibility
- [ ] Ad-hoc email history stored in SentAdHocEmails table (audit trail)
- [ ] Variable validation warns about unknown variables (not errors)
- [ ] HTML sanitization applies to template body (XSS prevention)
- [ ] Variable values HTML-escaped before insertion (XSS prevention)
- [ ] NSwag type generation pattern followed (no manual interfaces)

---

## 🔄 DISCOVERED CONSTRAINTS

### Constraint 1: Existing Vetting Email Templates System

**Description**: VettingEmailTemplates table and `/admin/vetting/email-templates` page already exist in production.

**Impact**:
- Cannot simply delete old table (data loss risk)
- Must migrate existing template content to GlobalEmailTemplates
- Must update all Vetting email sending logic to use new table
- Must maintain backwards compatibility during migration

**Required Changes**:
1. Database migration to copy VettingEmailTemplates → GlobalEmailTemplates (one-time)
2. Update VettingEmailService to query GlobalEmailTemplates (code change)
3. Keep VettingEmailTemplates as read-only backup (no delete)
4. Update Vetting admin page button to link to `/admin/email-templates?tab=vetting`
5. Delete old `/admin/vetting/email-templates` page AFTER validation

---

### Constraint 2: SendGrid Free Tier Limits

**Description**: Current SendGrid account on free tier (100 emails/day).

**Impact**:
- Production platform will exceed free tier quickly (event confirmations, reminders, ad-hoc emails)
- Testing at scale requires paid plan or separate test account
- Ad-hoc emails to 100+ recipients may hit daily limit

**Required Changes**:
1. Upgrade SendGrid plan before production deployment (Product Manager decision)
2. Implement application-level rate limiting (100 emails/minute)
3. Queue system for large ad-hoc email batches (future enhancement)
4. Clear error messages when SendGrid quota exceeded

---

### Constraint 3: MantineTiptapEditor Component Exists

**Description**: CMS feature already implemented rich text editor using MantineTiptapEditor.

**Impact**:
- MUST reuse MantineTiptapEditor (not TinyMCE - deprecated)
- Editor configuration may need extension for variable insertion
- Existing component handles HTML sanitization (reuse security logic)

**Required Changes**:
1. Review CMS MantineTiptapEditor implementation (`/src/components/cms/`)
2. Extend with "Insert Variable" button (dropdown of category-specific variables)
3. Reuse HTML sanitization logic (don't reinvent)
4. Consider extracting shared editor component if duplication occurs

---

### Constraint 4: React Router Query Parameters

**Description**: Vetting admin button must link to `/admin/email-templates?tab=vetting` (pre-select tab).

**Impact**:
- EmailTemplatesAdminPage must read query parameter on mount
- Tab state controlled by URL (not just local state)
- Browser back/forward must work correctly with tabs

**Required Changes**:
1. Use `useSearchParams()` hook to read `?tab=vetting` query parameter
2. Default to "vetting" tab if no query parameter
3. Update URL when tab changes (for shareable links)
4. Vetting admin button uses `<Link to="/admin/email-templates?tab=vetting">`

---

## 📊 DATA MODEL DECISIONS

### Entity 1: GlobalEmailTemplates

```csharp
public class GlobalEmailTemplate
{
    // Primary Key
    public Guid Id { get; set; }

    // Category and Type
    public EmailCategory Category { get; set; }  // Enum: Vetting, Events, Admin, Incident, AdHoc
    public string TemplateType { get; set; }     // Enum value as string (e.g., "Confirmation")

    // Content
    public string Subject { get; set; }          // Required, max 200 chars
    public string HtmlBody { get; set; }         // Required, rich HTML
    public string PlainTextBody { get; set; }    // Required, plain text version

    // Metadata
    public string Variables { get; set; } = "{}"; // JSONB - available variables for this template
    public bool IsActive { get; set; } = true;

    // Audit
    public int Version { get; set; } = 1;        // Increments on each save
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }          // Foreign key to ApplicationUser

    // Navigation
    public ApplicationUser UpdatedByUser { get; set; }
}

// Unique constraint: (Category, TemplateType) - One template per type per category
```

**Business Logic**:
- Subject max length: 200 characters
- HtmlBody sanitized on save (strip dangerous tags)
- PlainTextBody auto-generated from HtmlBody if not provided
- Version increments on every update (audit trail)
- IsActive = false hides template (soft delete, never hard delete)

---

### Entity 2: EventEmailTemplates

```csharp
public class EventEmailTemplate
{
    // Primary Key
    public Guid Id { get; set; }

    // Relationship
    public Guid EventId { get; set; }            // Foreign key to Event
    public Event Event { get; set; }

    // Reference to global template (for metadata, not foreign key constraint)
    public Guid GlobalTemplateId { get; set; }   // Which global template was copied
    public GlobalEmailTemplate GlobalTemplate { get; set; }  // Navigation only

    // Template Info
    public string TemplateType { get; set; }     // e.g., "Confirmation", "Reminder1Day"

    // Content (override of global)
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string PlainTextBody { get; set; }

    // Configuration
    public string[] TargetSessions { get; set; } = Array.Empty<string>();  // ["all"] or ["S1", "S2"]
    public string? RecipientGroup { get; set; }  // For ad-hoc emails (future use)

    // Metadata
    public bool IsCustomized { get; set; } = true;  // Always true for event-specific
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }

    // Navigation
    public ApplicationUser UpdatedByUser { get; set; }
}

// Unique constraint: (EventId, TemplateType) - One customized template per type per event
// Cascade delete: Deleting Event deletes all EventEmailTemplates for that event
```

**Business Logic**:
- Created only when event organizer saves custom template (copy-on-edit)
- Deleting this record = "Reset to Default" (revert to global template)
- GlobalTemplateId for reference only (not enforced foreign key - global template might change/delete)
- TargetSessions for multi-session events (e.g., only send confirmation to Session 1 attendees)

---

### Entity 3: SentAdHocEmails

```csharp
public class SentAdHocEmail
{
    // Primary Key
    public Guid Id { get; set; }

    // Email Details
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string PlainTextBody { get; set; }

    // Recipients
    public string RecipientGroup { get; set; }   // e.g., "all-tickets", "session-1", "volunteers"
    public string[] RecipientEmails { get; set; } = Array.Empty<string>();  // Actual emails sent to
    public int RecipientCount { get; set; }

    // Context
    public Guid? EventId { get; set; }           // Nullable - may not be event-related
    public Event? Event { get; set; }

    // SendGrid
    public string? SendGridMessageId { get; set; }
    public string DeliveryStatus { get; set; } = "Pending";  // Pending, Sent, Delivered, Failed

    // Audit
    public DateTime SentAt { get; set; }
    public Guid SentBy { get; set; }             // Admin who sent email
    public ApplicationUser SentByUser { get; set; }
}

// No unique constraints - can send multiple ad-hoc emails to same group
// Never deleted - permanent audit trail
```

**Business Logic**:
- Read-only after creation (no edits to sent emails)
- RecipientEmails array stores actual email addresses (for audit, privacy considerations)
- DeliveryStatus updated via SendGrid webhooks (future enhancement)
- EventId nullable (admin might send ad-hoc email to "all vetted members" - not event-related)

---

## 🎯 SUCCESS CRITERIA

### Test Case 1: Administrator Edits Global Event Confirmation Template

**Input**:
1. Admin logs in
2. Navigates to Admin > Email Templates
3. Clicks "Events" tab
4. Clicks "Confirmation Email" card
5. Changes subject to "Your WitchCityRope Ticket Confirmation"
6. Clicks Save

**Expected Output**:
- Template saved to GlobalEmailTemplates table
- Version increments (e.g., v1 → v2)
- Audit record: UpdatedBy = admin user ID, UpdatedAt = current timestamp
- Future ticket purchases receive email with new subject
- Existing event-specific customizations NOT affected
- UI shows success message "Template updated successfully"

---

### Test Case 2: Event Organizer Customizes Then Resets Template

**Input**:
1. Event organizer edits "Advanced Harnesses Workshop"
2. Clicks "Emails" tab
3. Clicks "Confirmation Email" (badge shows "(Default)")
4. Adds pre-class homework instructions
5. Clicks Save
6. Badge changes to "✓ Customized"
7. Later, clicks "Reset to Default" button
8. Confirms in dialog

**Expected Output**:
- After Save: EventEmailTemplate record created with customized content
- After Reset: EventEmailTemplate record deleted
- Badge changes back to "(Default)"
- Editor shows global template content (not customized version)
- Future ticket purchases use global template (homework instructions removed)
- Other events NOT affected

---

### Test Case 3: Variable Substitution at Send-Time

**Input**:
1. Global template body: "Hi {{attendee_name}}, your ticket for {{event_title}} is confirmed."
2. User "Sarah Johnson" purchases ticket for "Advanced Harnesses Workshop"
3. System sends confirmation email

**Expected Output**:
- Email sent to Sarah Johnson with body: "Hi Sarah Johnson, your ticket for Advanced Harnesses Workshop is confirmed."
- Database still contains: "Hi {{attendee_name}}, your ticket for {{event_title}} is confirmed."
- Variable values HTML-escaped (if event title contained HTML tags)
- SendGrid API called with personalized content

---

### Test Case 4: Admin Sends Ad-Hoc Email with History

**Input**:
1. Admin navigates to Email Templates > Ad Hoc tab
2. Composes email: Subject "Parking Update", Body "Free parking available..."
3. Selects recipient group: "All ticket holders - Rope Performance Night"
4. Preview shows "187 recipients"
5. Clicks Send

**Expected Output**:
- 187 emails sent via SendGrid
- SentAdHocEmail record created with:
  - Full email content
  - RecipientGroup = "All ticket holders - Rope Performance Night"
  - RecipientCount = 187
  - RecipientEmails array = [all 187 email addresses]
  - SendGridMessageId captured
  - DeliveryStatus = "Sent"
  - SentBy = admin user ID
- History table shows newly sent email
- Email appears in recipients' inboxes

---

### Test Case 5: Vetting Template Migration

**Input**:
1. Run database migration
2. Vetting admin clicks "Email Templates" button
3. Redirected to `/admin/email-templates?tab=vetting`
4. Vetting tab pre-selected
5. Admin edits "Application Received" template
6. Clicks Save
7. New vetting application submitted

**Expected Output**:
- All 6 Vetting templates migrated to GlobalEmailTemplates (Category = Vetting)
- VettingEmailTemplates table still exists (read-only)
- Template changes saved to GlobalEmailTemplates (not old table)
- Applicant receives email using updated template
- Old `/admin/vetting/email-templates` page returns 404 (deleted)
- No errors in application logs

---

## ⚠️ DO NOT IMPLEMENT

### Explicitly OUT OF SCOPE:

- ❌ **DO NOT** create template approval workflow (admin changes apply immediately)
- ❌ **DO NOT** implement template preview with real user data (security risk - use mock data only)
- ❌ **DO NOT** add A/B testing capability (single template per type)
- ❌ **DO NOT** support custom variables (variables fixed per category)
- ❌ **DO NOT** implement conditional logic in templates (if/else statements)
- ❌ **DO NOT** implement loops in templates (for each session, show X)
- ❌ **DO NOT** add SMS or push notification templates (email only)
- ❌ **DO NOT** support alternative email providers (SendGrid only)
- ❌ **DO NOT** create mobile template editor (desktop/tablet only)
- ❌ **DO NOT** implement drag-and-drop visual editor (rich text editor sufficient)
- ❌ **DO NOT** add template versioning UI with diff view (metadata display only)
- ❌ **DO NOT** create template gallery or marketplace (default templates only)
- ❌ **DO NOT** support multilingual templates (English only)
- ❌ **DO NOT** implement scheduled email sending (send immediately only)
- ❌ **DO NOT** create template cloning feature (create new from scratch)
- ❌ **DO NOT** allow admins to create custom template categories (5 categories fixed)
- ❌ **DO NOT** allow admins to create custom template types (enums fixed)
- ❌ **DO NOT** implement granular permissions beyond Administrator/Event Organizer
- ❌ **DO NOT** create bulk template import/export (manual editing only)
- ❌ **DO NOT** integrate email analytics into WitchCityRope UI (use SendGrid dashboard)

---

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| **Global Template** | Default email template for all system communications | "Confirmation Email" template used by all events unless customized |
| **Event-Specific Template** | Customized email template for a specific event | "Advanced Harnesses Workshop" has custom confirmation with homework instructions |
| **Template Type** | Category-specific email purpose | EventTemplateType.Confirmation, VettingTemplateType.Approved |
| **Email Category** | High-level grouping of templates | Vetting, Events, Admin, Incident, Ad Hoc |
| **Variable** | Placeholder for dynamic content | `{{attendee_name}}`, `{{event_title}}` |
| **Variable Substitution** | Replacing placeholders with actual data at send-time | `{{attendee_name}}` → "Sarah Johnson" |
| **Copy-on-Edit** | Creating event-specific copy only when customized | Event template created when user saves changes, not before |
| **Reset to Default** | Deleting event-specific customization to revert to global | Deletes EventEmailTemplate record, fetches GlobalEmailTemplate |
| **Ad Hoc Email** | One-time bulk email to group of users | "Parking update for all ticket holders" |
| **Audit Trail** | Historical record of template changes and emails sent | Version history, UpdatedBy, SentAdHocEmails table |
| **HTML Sanitization** | Removing dangerous HTML tags | Strip `<script>`, `<iframe>`, `<object>` tags |
| **HTML Escaping** | Converting HTML characters to safe entities | `<script>` → `&lt;script&gt;` |
| **SendGrid Message ID** | Unique identifier for sent email | `sg_abc123xyz` (for tracking delivery) |

---

## 🔗 NEXT AGENT INSTRUCTIONS

### For Database Designer (Phase 2):

1. **FIRST**: Read Business Requirements document (entire document)
2. **SECOND**: Review Approved Implementation Plan (database schema section)
3. **THIRD**: Design database schema with:
   - GlobalEmailTemplates table
   - EventEmailTemplates table
   - SentAdHocEmails table
   - Enums for EmailCategory and template types
   - Unique constraints: (Category, TemplateType) on GlobalEmailTemplates, (EventId, TemplateType) on EventEmailTemplates
   - Foreign keys: UpdatedBy → ApplicationUser, EventId → Event, SentBy → ApplicationUser
   - Cascade delete: Event deletion deletes EventEmailTemplates
4. **FOURTH**: Create migration script that:
   - Creates 3 new tables
   - Copies VettingEmailTemplates → GlobalEmailTemplates (one-time)
   - Seeds 22 default templates (6 Vetting + 7 Events + 4 Admin + 4 Incident + 1 Ad Hoc)
   - Keeps VettingEmailTemplates table (read-only backup)
5. **VALIDATE**: No foreign key constraint on GlobalTemplateId (reference only)

### For Functional Spec / Backend Developer (Phase 2):

1. **FIRST**: Read Business Requirements + Database Design documents
2. **SECOND**: Create DTOs with OpenAPI annotations:
   - GlobalEmailTemplateDto
   - EventEmailTemplateDto
   - SentAdHocEmailDto
   - UpdateGlobalTemplateRequest
   - UpdateEventTemplateRequest
   - SendAdHocEmailRequest
3. **THIRD**: Design 10 API endpoints:
   - GET /api/email-templates?category={category}
   - GET /api/email-templates/{id}
   - PUT /api/email-templates/{id}
   - GET /api/events/{eventId}/email-templates
   - GET /api/events/{eventId}/email-templates/{type}
   - PUT /api/events/{eventId}/email-templates/{type}
   - DELETE /api/events/{eventId}/email-templates/{type}
   - POST /api/email-templates/ad-hoc
   - GET /api/email-templates/ad-hoc/history
4. **FOURTH**: Design services:
   - GlobalEmailTemplateService (GetByCategory, GetByCategoryAndType, Update)
   - EventEmailTemplateService (GetForEvent, GetOrCreateEventTemplate, UpdateEventTemplate, ResetToDefault)
   - AdHocEmailService (SendAdHocEmail, GetHistory)
   - VariableValidationService (ValidateVariables per category)
5. **VALIDATE**: NSwag configuration generates TypeScript types correctly

### For UI Designer (Phase 2):

1. **FIRST**: Read Business Requirements (UI Specifications section)
2. **SECOND**: Review Approved Implementation Plan (UI Specifications section)
3. **THIRD**: Design wireframes for:
   - Admin Dashboard card (Email Templates)
   - `/admin/email-templates` page with 5 tabs
   - EmailCategoryPanel component (template cards + editor panel)
   - EventForm Emails tab updates (badges, reset button)
4. **FOURTH**: Reuse existing patterns:
   - MantineTiptapEditor from CMS feature
   - Tabbed interface (Mantine Tabs component)
   - Card layout similar to Admin Dashboard
   - Badge indicators ("✓ Customized", "(Default)")
5. **VALIDATE**: Mobile-responsive for tablet (desktop minimum), not phone

---

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-11-09 (Phase 1 - Requirements)
**Key Finding**: Two-tier template system (global + event-specific) with copy-on-edit pattern is the cornerstone of this feature. Event-specific templates are NOT foreign key constrained to global templates - they are independent copies.

**Next Agent Should Be**: Database Designer (for schema design) OR Functional Spec Agent (if parallel work)
**Next Phase**: Phase 2 - Design
**Estimated Effort**:
- Database Design: 2-3 hours
- Functional Specification: 6-8 hours
- UI Design: 2-3 hours
- **Total Phase 2**: ~10-14 hours

---

## 📌 QUICK REFERENCE SUMMARY

**Feature**: Centralized admin UI for managing email templates across 5 categories (Vetting, Events, Admin, Incident, Ad Hoc)

**Tables**: GlobalEmailTemplates (22 default), EventEmailTemplates (event-specific overrides), SentAdHocEmails (audit trail)

**Enums**: 5 template type enums (VettingTemplateType, EventTemplateType, AdminTemplateType, IncidentTemplateType, AdHocTemplateType)

**Permissions**: Administrator = global templates, Event Organizer = event-specific templates

**Key Pattern**: Copy-on-edit (EventEmailTemplate created only when customized), Reset-to-default (delete EventEmailTemplate)

**Critical Constraints**:
1. NSwag type generation (no manual TypeScript interfaces)
2. Vetting template migration (backwards compatibility)
3. Variable substitution at send-time (never stored)
4. HTML sanitization (XSS prevention)
5. SendGrid integration (extend from Vetting to Events/Admin/Incident)

**Success Metric**: Administrators manage 22 templates from single UI, event organizers customize 7 event templates, zero manual email editing required.

---

**END OF HANDOFF DOCUMENT**
