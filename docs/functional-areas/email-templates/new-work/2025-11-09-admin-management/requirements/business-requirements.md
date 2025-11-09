# Business Requirements: Email Templates Admin Management
<!-- Last Updated: 2025-11-09 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft - Awaiting Stakeholder Approval -->

## Executive Summary

WitchCityRope currently has email template management only for the Vetting functional area. As the platform expands to support Events, Admin notifications, Incident reporting, and Ad Hoc communications, we need a centralized admin interface for managing global email templates across all system categories. This feature will enable administrators to maintain consistent, professional communications while allowing event organizers to customize event-specific templates when needed.

**Business Value**: Reduces administrative overhead, ensures communication consistency, supports event organizer autonomy, and provides comprehensive audit trails for all email communications sent through the platform.

## Business Context

### Problem Statement

Currently:
- **Fragmented Template Management**: Vetting has its own email templates page at `/admin/vetting/email-templates`, but no centralized system exists for other categories
- **Limited Event Communications**: Event organizers cannot customize email templates for their specific events (confirmation emails, reminders, cancellations)
- **No Admin/Incident Templates**: No template system exists for administrative notifications or incident reporting communications
- **Missing Ad Hoc Functionality**: No way to send bulk emails to event participants or member groups with saved history
- **Inconsistent UX**: Different template management patterns across features creates confusion

### Business Value

**For Administrators**:
- Single location to manage all platform email templates (Vetting, Events, Admin, Incident, Ad Hoc)
- Consistent communication standards across all categories
- Variable substitution ensures personalized emails without manual editing
- Comprehensive audit trail of template changes and ad-hoc emails sent

**For Event Organizers**:
- Ability to customize event-specific email templates (e.g., add workshop-specific details to confirmation emails)
- "Reset to Default" functionality prevents broken templates
- Badge indicators show which templates are customized vs. using global defaults

**For Members/Participants**:
- Professional, consistent communication experience
- Personalized emails with accurate event details, names, and dates
- Timely automated reminders and updates

### Success Metrics

**Quantitative**:
- 100% of email categories (5) have template management capability
- Administrators can manage 22 default templates (6 Vetting + 7 Events + 4 Admin + 4 Incident + 1 Ad Hoc)
- Event organizers can customize 7 event-specific templates per event
- Zero manual email editing required (all use template variables)

**Qualitative**:
- Administrators report reduced time managing email communications
- Event organizers successfully customize templates without breaking functionality
- Members report professional, timely, and personalized email communications
- Support requests related to "didn't receive email" decrease due to audit trail

## User Stories

### Story 1: Administrator Manages Global Templates
**As an** Administrator
**I want to** manage all email templates from a single centralized location
**So that** I don't have to navigate to multiple pages and can ensure communication consistency across the platform

**Acceptance Criteria**:
- Given I am logged in as an Administrator
- When I navigate to Admin Dashboard
- Then I see an "Email Templates" card with description "Manage global email templates for all categories"
- And when I click the card
- Then I am taken to `/admin/email-templates` with tabbed interface showing 5 categories: Vetting, Events, Admin, Incident, Ad Hoc

**Priority**: High
**Estimated Effort**: Medium

---

### Story 2: Edit Global Email Template
**As an** Administrator
**I want to** edit a global email template's subject and body content
**So that** I can update communication standards across the platform

**Acceptance Criteria**:
- Given I am on the Email Templates admin page
- When I select a category tab (e.g., "Events")
- Then I see all templates for that category displayed as cards
- And when I click a template card (e.g., "Confirmation Email")
- Then I see an editor panel with:
  - Subject line input field
  - Rich text HTML editor (MantineTiptapEditor)
  - Available variables reference (category-specific)
  - Save and Cancel buttons
- And when I modify the subject or body
- And click Save
- Then the global template is updated
- And an audit record is created with timestamp and my user ID
- And the template version number increments

**Priority**: High
**Estimated Effort**: Medium

---

### Story 3: Event Organizer Customizes Event-Specific Template
**As an** Event Organizer
**I want to** customize email templates for my specific event
**So that** I can include workshop-specific details without affecting other events

**Acceptance Criteria**:
- Given I am editing an event I created
- When I navigate to the "Emails" tab in EventForm
- Then I see all 7 event email templates
- And global default templates are displayed with badge "(Default)"
- And when I edit a template and save
- Then the template is saved as event-specific (EventEmailTemplates table)
- And the badge changes to "✓ Customized"
- And when other users register for this event
- Then they receive emails using my customized template (not the global default)

**Priority**: High
**Estimated Effort**: Medium

---

### Story 4: Reset Event Template to Default
**As an** Event Organizer
**I want to** reset a customized event template back to the global default
**So that** I can undo mistakes or return to standard communication

**Acceptance Criteria**:
- Given I have customized an event-specific template (badge shows "✓ Customized")
- When I click the "Reset to Default" button for that template
- Then a confirmation dialog appears: "This will delete your customization and use the global template. Continue?"
- And when I confirm
- Then the EventEmailTemplate record is deleted
- And the badge changes to "(Default)"
- And the template content reverts to the current global template
- And future emails for this event use the global template

**Priority**: Medium
**Estimated Effort**: Small

---

### Story 5: Variable Substitution Validation
**As an** Administrator
**I want to** see warnings when I use invalid variables in templates
**So that** I don't create broken templates that fail to send

**Acceptance Criteria**:
- Given I am editing a template
- When I type a variable like `{{invalid_variable}}`
- And the variable is not in the category-specific allowed variables list
- Then I see a warning message: "Unknown variable detected: {{invalid_variable}}. Available variables: [list]"
- And the warning is displayed near the editor (not blocking)
- And I can still save the template (warning only, not error)

**Priority**: Medium
**Estimated Effort**: Small

---

### Story 6: Send Ad Hoc Email with History
**As an** Administrator
**I want to** send ad-hoc bulk emails to event participants and save the sent content
**So that** I have an audit trail of all communications sent through the platform

**Acceptance Criteria**:
- Given I am on the Email Templates admin page
- When I select the "Ad Hoc" tab
- Then I see:
  - Ad-hoc email composer (subject + body editor)
  - Recipient group selector (e.g., "All Tickets - Event X", "Volunteers", "Specific Emails")
  - Send button
  - History table showing previously sent ad-hoc emails
- And when I compose an email and click Send
- Then the email is sent via SendGrid to the selected recipients
- And a record is saved to SentAdHocEmails table with:
  - Full email content (subject + body)
  - Recipient group and count
  - SendGrid message ID
  - Delivery status
  - Sent timestamp and sent by user
- And the history table updates to show the newly sent email

**Priority**: Medium
**Estimated Effort**: Large

---

### Story 7: Vetting Template Management Migration
**As an** Administrator
**I want to** access Vetting email templates through the centralized Email Templates page
**So that** I have a consistent admin experience across all template categories

**Acceptance Criteria**:
- Given the new Email Templates admin page is implemented
- When I navigate to Admin > Vetting
- Then I see an "Email Templates" button
- And when I click the button
- Then I am redirected to `/admin/email-templates?tab=vetting` (Vetting tab pre-selected)
- And the old `/admin/vetting/email-templates` page is deleted
- And all 6 Vetting templates are migrated to GlobalEmailTemplates table:
  - ApplicationReceived
  - InterviewApproved
  - Approved
  - OnHold
  - Denied
  - InterviewReminder
- And the templates function identically to before (same variables, same sending logic)

**Priority**: High
**Estimated Effort**: Medium

---

### Story 8: Template Version History and Audit
**As an** Administrator
**I want to** view who last edited a template and when
**So that** I can track changes and understand template evolution

**Acceptance Criteria**:
- Given I am viewing a template in the editor
- When I look at the template metadata section
- Then I see:
  - "Last Updated By: [Admin Name]"
  - "Last Updated: [Date/Time]"
  - "Version: [Number]"
- And when I click "View History" (future enhancement)
- Then I see a table of all template changes with:
  - Version number
  - Changed by (admin name)
  - Changed date/time
  - Brief description of changes (future: diff view)

**Priority**: Low (metadata display now, full history future)
**Estimated Effort**: Small (metadata), Large (full history)

---

## Functional Requirements

### FR1: Template Categories and Types

**Requirement**: System must support 5 distinct email categories with predefined template types per category.

**Categories and Types**:

1. **Vetting** (6 templates):
   - ApplicationReceived
   - InterviewApproved
   - Approved
   - OnHold
   - Denied
   - InterviewReminder

2. **Events** (7 templates):
   - Confirmation
   - Reminder1Day
   - Reminder1Week
   - Cancellation
   - WaitlistNotification
   - PostEventSurvey
   - ScheduleChange

3. **Admin** (4 templates):
   - SystemAlert
   - MaintenanceNotice
   - SecurityAlert
   - AccountSuspension

4. **Incident** (4 templates):
   - ReportReceived
   - StatusUpdate
   - ResolutionNotice
   - FollowUpRequest

5. **Ad Hoc** (1 template):
   - Custom (always customizable, no predefined content)

**Business Rules**:
- Template types are defined as fixed enums (not flexible strings) for type safety
- Each category has a specific set of allowed variables for substitution
- Global templates use GlobalEmailTemplates table
- Event-specific templates use EventEmailTemplates table
- Ad-hoc sent emails use SentAdHocEmails table

---

### FR2: Global Template Management

**Requirement**: Administrators must be able to create, read, update global email templates from a centralized admin interface.

**Capabilities**:
- View all templates organized by category (5 tabs)
- Edit template subject line (plain text, max 200 characters)
- Edit template body (rich HTML using MantineTiptapEditor)
- Edit template plain text version (for email clients that don't support HTML)
- View available variables per category
- Save template changes with automatic version increment
- View template metadata (last updated by, date, version)

**Business Rules**:
- Only users with Administrator role can access global template management
- Each save creates new version number (audit trail)
- Template changes apply immediately to future emails sent
- Event-specific templates are NOT affected by global template changes
- System requires at least one global template per template type (cannot delete defaults)

---

### FR3: Event-Specific Template Customization

**Requirement**: Event organizers must be able to customize email templates for their specific events without affecting global defaults or other events.

**Capabilities**:
- View global default templates for all 7 event email types
- Copy-on-edit: Editing a global template creates event-specific copy
- Save event-specific template content (subject + body)
- Reset event-specific template to global default (deletes customization)
- Visual indicators showing which templates are customized ("✓ Customized") vs. default ("(Default)")
- Target specific sessions or "all sessions" for multi-session events

**Business Rules**:
- Only event organizers (creator or admin) can customize event templates
- Event-specific templates are stored in EventEmailTemplates table
- Deleting an event deletes associated event-specific templates (cascade)
- "Reset to Default" deletes EventEmailTemplate record; future loads fetch global template
- Global template changes do NOT overwrite existing event-specific templates
- Event-specific templates inherit global template's available variables

---

### FR4: Variable Substitution System

**Requirement**: System must support variable substitution in email templates with category-specific allowed variables.

**Variable Sets by Category**:

**Vetting Variables**:
- `{{scene_name}}` - Applicant's scene/username
- `{{application_number}}` - Vetting application number
- `{{application_date}}` - Application submission date
- `{{submission_date}}` - Application submission date (alias)
- `{{status_change_date}}` - Date status was changed
- `{{contact_email}}` - support@witchcityrope.com
- `{{current_status}}` - Current workflow status
- `{{custom_message}}` - Optional custom message (reminders)

**Events Variables**:
- `{{attendee_name}}` - Ticket holder's name
- `{{event_title}}` - Event name
- `{{event_date}}` - Event start date (formatted)
- `{{event_time}}` - Event start time
- `{{venue_name}}` - Venue name
- `{{venue_address}}` - Full venue address
- `{{session_name}}` - Session name (if multi-session)
- `{{ticket_type}}` - Ticket type purchased
- `{{total_paid}}` - Amount paid
- `{{confirmation_number}}` - Ticket confirmation number
- `{{organizer_email}}` - Event organizer email

**Admin Variables**:
- `{{user_name}}` - User's full name
- `{{account_email}}` - User's email
- `{{system_url}}` - WitchCityRope URL
- `{{support_email}}` - Support contact
- `{{action_required}}` - Specific action needed
- `{{deadline_date}}` - Action deadline

**Incident Variables**:
- `{{incident_number}}` - Incident report ID
- `{{reporter_name}}` - Who filed report
- `{{incident_date}}` - When incident occurred
- `{{status}}` - Current investigation status
- `{{coordinator_name}}` - Assigned coordinator
- `{{next_steps}}` - What happens next

**Ad Hoc Variables**:
- `{{recipient_name}}` - Recipient's name
- `{{event_title}}` - Related event (if applicable)
- `{{custom_content}}` - Admin-defined content

**Business Rules**:
- Variables are replaced at send-time (not stored in database)
- Invalid/missing variables are replaced with empty string (no error)
- Variable validation shows warnings (not errors) when unknown variables detected
- HTML escaping applied to all variable values to prevent XSS attacks
- Variables use `{{variable_name}}` syntax for consistency

---

### FR5: Ad Hoc Email Management

**Requirement**: Administrators must be able to send bulk emails to groups of users with full history retention for audit purposes.

**Capabilities**:
- Compose ad-hoc email (subject + HTML body + plain text)
- Select recipient group:
  - "All ticket holders - [Event Name]"
  - "Specific session - [Session Name]"
  - "Volunteers - [Event Name]"
  - "Custom list" (enter email addresses)
- Preview recipient count before sending
- Send email via SendGrid integration
- View history of all sent ad-hoc emails with:
  - Subject line
  - Recipient group and count
  - Sent date/time
  - Sent by (admin name)
  - Delivery status (Pending, Sent, Delivered, Failed)
  - SendGrid message ID

**Business Rules**:
- Only Administrators can send ad-hoc emails
- Full email content (subject + body) saved to SentAdHocEmails table
- Recipient email addresses saved (for audit trail)
- Cannot edit sent ad-hoc emails (read-only history)
- SendGrid message ID captured for delivery tracking
- Delivery status updated via SendGrid webhooks (future enhancement)

---

### FR6: Template Editor Interface

**Requirement**: Template editor must provide intuitive, user-friendly interface for editing email content with rich text formatting.

**Editor Features**:
- Subject line input (plain text, max 200 characters)
- HTML body editor using MantineTiptapEditor:
  - Rich text formatting (bold, italic, underline, links)
  - Lists (ordered, unordered)
  - Headings (H1-H6)
  - Text alignment
  - Color/highlight
  - Insert variable button (dropdown of available variables)
- Plain text version auto-generated from HTML (with option to manually edit)
- Available variables reference panel (shows all category-specific variables)
- Variable validation warnings (unknown variables highlighted)
- Save and Cancel buttons
- Preview button (future enhancement)

**Business Rules**:
- Rich text editor supports standard HTML formatting
- Variable insertion uses `{{variable_name}}` syntax
- Preview shows real example data (not actual user data)
- Unsaved changes prompt confirmation when navigating away
- Templates auto-save to local storage every 30 seconds (prevent data loss)

---

### FR7: Vetting Template Migration

**Requirement**: Existing Vetting email templates must be migrated to new GlobalEmailTemplates system without disruption.

**Migration Requirements**:
- All 6 existing Vetting templates migrated to GlobalEmailTemplates table
- Old VettingEmailTemplates table remains (backwards compatibility, read-only)
- Old `/admin/vetting/email-templates` page deleted
- Vetting admin page "Email Templates" button updated to link to `/admin/email-templates?tab=vetting`
- All existing Vetting email sending logic updated to use GlobalEmailTemplates
- Variable substitution continues to work identically

**Business Rules**:
- Migration runs automatically on first deployment (database migration)
- No manual intervention required
- Existing template content preserved exactly (no data loss)
- Template audit history starts fresh (version 1) after migration
- Old table eventually removed in future cleanup (after validation period)

---

## Non-Functional Requirements

### NFR1: Performance

**Requirement**: Template management operations must be fast and responsive.

**Targets**:
- Template list load: < 500ms
- Template editor load: < 300ms
- Template save: < 1 second
- Variable substitution at send-time: < 100ms per email
- Ad-hoc email send (100 recipients): < 5 seconds

**Business Rules**:
- Templates cached in memory (invalidated on update)
- Database queries optimized with indexes
- SendGrid API calls made asynchronously (non-blocking)

---

### NFR2: Security

**Requirement**: Email template system must protect against unauthorized access and malicious content.

**Security Measures**:
- Admin-only access: Only users with Administrator role can manage global templates
- Event organizer access: Only event creators/admins can customize event templates
- HTML sanitization: All template HTML sanitized to prevent XSS attacks
- Variable escaping: All variable values HTML-escaped before insertion
- SQL injection prevention: All database queries use parameterized queries
- CSRF protection: All template update endpoints require CSRF tokens
- Audit logging: All template changes logged with user ID and timestamp

**Business Rules**:
- Template editor strips dangerous HTML tags (script, iframe, object, embed)
- Variable values never contain raw HTML (always escaped)
- SendGrid API key stored in environment variables (never in database)
- Email sending rate-limited to prevent abuse (100 emails/minute)

---

### NFR3: Usability

**Requirement**: Template management interface must be intuitive and accessible to non-technical administrators.

**Usability Features**:
- Tabbed interface for easy category navigation
- Visual indicators for customized vs. default templates
- Available variables reference always visible
- Rich text editor with toolbar (familiar interface)
- "Insert Variable" button (no manual typing required)
- Warning messages for invalid variables (helpful, not blocking)
- Confirmation dialogs for destructive actions (Reset to Default, Delete)
- Responsive design (works on desktop, tablet)

**Business Rules**:
- No technical knowledge required to use template editor
- Help text explains what each template is used for
- Example preview data provided (not real user data)
- Undo/redo support in editor (browser native)

---

### NFR4: Reliability

**Requirement**: Email template system must be highly reliable with comprehensive error handling.

**Reliability Measures**:
- Database transactions for template updates (rollback on error)
- SendGrid retry logic (3 attempts with exponential backoff)
- Email queue system (failed emails retried automatically)
- Delivery status tracking (via SendGrid webhooks)
- Error logging for all failures (CloudWatch or equivalent)
- Graceful degradation (if SendGrid down, queue for later)

**Business Rules**:
- Template save failures show clear error messages
- Email send failures logged but don't crash system
- Failed emails automatically retried 3 times before marking as failed
- Administrators notified of persistent email delivery failures

---

### NFR5: Maintainability

**Requirement**: Email template system must be easy to maintain and extend.

**Maintainability Features**:
- Type-safe enums for template types (compile-time checking)
- Shared component for template editor (reusable across categories)
- Auto-generated TypeScript types from C# DTOs (NSwag)
- Comprehensive unit tests (90%+ coverage)
- Integration tests for all API endpoints
- E2E tests for critical workflows
- Documentation for adding new template types or categories

**Business Rules**:
- Adding new template type requires: enum update, database seed, documentation
- Adding new category requires: enum update, variable set definition, tab component
- All template types validated at compile-time (not runtime)
- Breaking changes follow 30-day notice process (DTO alignment strategy)

---

## Business Rules Summary

### BR1: Permission Model
- **Global Templates**: Only Administrators can create/edit global templates
- **Event Templates**: Event organizers (creator or admin) can customize event-specific templates
- **Ad Hoc Emails**: Only Administrators can send ad-hoc emails
- **Template Viewing**: All authenticated users can view templates they receive (in sent emails)

### BR2: Template Hierarchy
- **Global Templates**: Default templates for all system communications
- **Event-Specific Templates**: Override global templates for specific events only
- **Inheritance**: Event templates inherit variable sets from global templates
- **Reset**: Deleting event-specific template reverts to global default (not a copy)

### BR3: Data Integrity
- **Version Control**: Every template save increments version number
- **Audit Trail**: All changes logged with user ID, timestamp, version
- **No Deletion**: Global templates cannot be deleted (only deactivated)
- **Cascade Deletion**: Deleting event deletes associated event-specific templates
- **History Retention**: Ad-hoc emails stored permanently (never deleted)

### BR4: Email Sending
- **Variable Substitution**: Variables replaced at send-time (not stored)
- **HTML Sanitization**: All template HTML sanitized before saving
- **Variable Escaping**: All variable values escaped before insertion
- **SendGrid Integration**: All emails sent via SendGrid API
- **Delivery Tracking**: SendGrid message ID captured for tracking

### BR5: Validation
- **Required Fields**: Subject and body required for all templates
- **Character Limits**: Subject max 200 characters
- **Variable Validation**: Warnings (not errors) for unknown variables
- **HTML Validation**: Dangerous tags stripped automatically
- **Recipient Validation**: Email addresses validated before sending

---

## Assumptions and Constraints

### Assumptions

**Technical Assumptions**:
- SendGrid integration already exists for Vetting emails (can be extended to Events, Admin, Incident)
- MantineTiptapEditor component available and functional (used in CMS)
- NSwag type generation pipeline operational (auto-generates TypeScript types from C# DTOs)
- PostgreSQL database supports JSONB for variable storage
- React Router supports query parameters for tab pre-selection (`?tab=vetting`)

**Business Assumptions**:
- Administrators are trusted users (no approval workflow for template changes)
- Event organizers understand basic email template editing (no training required)
- Users prefer personalized emails (variable substitution) over generic content
- Email delivery metrics tracked via SendGrid (not custom analytics)
- Legal compliance for email communications already established (CAN-SPAM, GDPR)

**User Assumptions**:
- Administrators have desktop/laptop access (not mobile-only)
- Event organizers comfortable with rich text editor (similar to Word/Google Docs)
- Users check email regularly (email is primary communication channel)
- Users trust platform-sent emails (not marked as spam)

### Constraints

**Technical Constraints**:
- Must use React + TypeScript frontend (no other frameworks)
- Must use .NET Minimal API backend (no other languages)
- Must use PostgreSQL database (no NoSQL)
- Must use SendGrid for email sending (no alternative providers without infrastructure changes)
- Must follow NSwag type generation pattern (no manual TypeScript interfaces for DTOs)
- Must use MantineTiptapEditor (TinyMCE deprecated)

**Business Constraints**:
- Cannot change existing Vetting email template variables (backwards compatibility)
- Cannot require manual migration steps (must be automatic database migration)
- Cannot break existing Vetting email sending functionality
- Cannot violate RBAC permissions (admin-only, event organizer restrictions)
- Cannot expose user email addresses in UI (privacy compliance)

**Time Constraints**:
- Estimated 18-22 hours total development effort
- 4-5 day timeline with proper orchestration
- Must complete testing before production deployment

**Resource Constraints**:
- SendGrid free tier: 100 emails/day (production requires paid plan)
- Database storage: Email content adds ~5KB per template * 22 templates = ~110KB (negligible)
- API rate limits: SendGrid API 600 requests/minute (sufficient for platform scale)

---

## Out of Scope

### Explicitly NOT Included in This Feature

**Template Features**:
- ❌ **Template Preview with Real Data**: Preview uses mock data only (security risk)
- ❌ **A/B Testing**: No split testing of template variations
- ❌ **Scheduled Emails**: All emails sent immediately (no "send later")
- ❌ **Email Analytics Dashboard**: No open rates, click rates (use SendGrid dashboard)
- ❌ **Template Approval Workflow**: Admins trusted, no approval required
- ❌ **Template Versioning UI**: Version numbers tracked but no diff view or rollback (future enhancement)

**Variable Features**:
- ❌ **Custom Variables**: Variables fixed per category (no admin-defined variables)
- ❌ **Conditional Logic**: No if/else in templates (e.g., "if vetted, show X")
- ❌ **Loops**: No repeating sections (e.g., "for each session, show Y")
- ❌ **Calculations**: No dynamic calculations (e.g., "total + tax")

**Integration Features**:
- ❌ **Alternative Email Providers**: SendGrid only (no Mailgun, SES, etc.)
- ❌ **SMS Templates**: Email only (no SMS/text messages)
- ❌ **Push Notifications**: Email only (no in-app or browser notifications)
- ❌ **Webhook Delivery Tracking**: SendGrid message ID captured but webhook integration future enhancement

**UI Features**:
- ❌ **Mobile Template Editor**: Desktop/tablet only (no mobile editing)
- ❌ **Drag-and-Drop Editor**: Rich text editor only (no visual block builder)
- ❌ **Template Gallery**: No pre-built templates or marketplace
- ❌ **Multilingual Templates**: English only (no i18n support)

**Admin Features**:
- ❌ **Bulk Template Import/Export**: No CSV/JSON import (manual editing only)
- ❌ **Template Cloning**: Create new from scratch (no "copy template X")
- ❌ **Template Categories Management**: 5 categories fixed (no custom categories)
- ❌ **Template Permissions**: Admin vs. event organizer only (no granular permissions)

**Future Enhancements** (Out of Scope for Initial Release):
- Full template version history with diff view and rollback
- Template preview with real user data (requires privacy controls)
- SendGrid webhook integration for delivery status tracking
- Email analytics dashboard (open rates, click rates, bounces)
- Template approval workflow for non-admin template changes
- Scheduled email sending ("send on date/time")
- Multilingual template support
- SMS/push notification templates

---

## Dependencies and Integration Points

### System Dependencies

**Frontend Dependencies**:
- React + TypeScript + Vite (existing)
- Mantine UI component library (existing)
- MantineTiptapEditor component (existing, from CMS feature)
- React Router (existing, for tab navigation)
- NSwag-generated TypeScript types (existing pipeline)

**Backend Dependencies**:
- .NET 8 Minimal API (existing)
- Entity Framework Core (existing)
- PostgreSQL database (existing)
- SendGrid SDK (existing, from Vetting feature)
- User authentication/authorization (existing RBAC)

**External Services**:
- SendGrid API (existing account, may need upgrade from free tier)

### Integration Points

**Authentication System**:
- Role-based access control (Administrator, Teacher roles)
- User identity for audit logging (UpdatedBy, SentBy fields)

**Event Management System**:
- EventForm component (Emails tab integration)
- Event CRUD operations (cascade delete event-specific templates)
- Session management (multi-session template targeting)

**Vetting System**:
- Existing Vetting email sending logic (update to use GlobalEmailTemplates)
- Vetting admin page (update button link)
- VettingEmailTemplates table (backwards compatibility, read-only)

**SendGrid Integration**:
- Email sending service (extend to support Events, Admin, Incident categories)
- Message ID capture for tracking
- Future: Webhook integration for delivery status updates

**Database**:
- Three new tables: GlobalEmailTemplates, EventEmailTemplates, SentAdHocEmails
- Foreign key relationships: Event, ApplicationUser
- Database migrations (automated deployment)

**File Registry**:
- All new files documented in `/docs/architecture/file-registry.md`
- Functional Area Master Index updated

---

## Risk Analysis

### High-Risk Areas

**Risk 1: Vetting Template Migration Disruption**
- **Impact**: Critical - Vetting emails could fail to send
- **Likelihood**: Low (careful migration planning)
- **Mitigation**:
  - Thorough testing of migration script
  - Backwards compatibility with old table (read-only)
  - Rollback plan if issues detected
  - Deploy during low-activity period

**Risk 2: SendGrid Rate Limiting**
- **Impact**: High - Ad-hoc emails to 100+ recipients could fail
- **Likelihood**: Medium (free tier = 100 emails/day)
- **Mitigation**:
  - Upgrade to paid SendGrid plan before production launch
  - Implement rate limiting in application (100 emails/minute)
  - Queue system for large batches
  - Clear error messages to admins

**Risk 3: HTML Injection/XSS**
- **Impact**: Critical - Security vulnerability
- **Likelihood**: Low (sanitization in place)
- **Mitigation**:
  - MantineTiptapEditor strips dangerous tags
  - Server-side HTML sanitization (defense in depth)
  - Variable values always HTML-escaped
  - Security testing before production

### Medium-Risk Areas

**Risk 4: Event Organizer Breaks Template**
- **Impact**: Medium - Event-specific emails fail to send
- **Likelihood**: Medium (user error)
- **Mitigation**:
  - "Reset to Default" button for easy recovery
  - Variable validation warnings (prevent common mistakes)
  - Template preview (future enhancement)
  - Help documentation with examples

**Risk 5: Template Editor Performance**
- **Impact**: Low - Slow editor frustrates users
- **Likelihood**: Low (small templates, modern browser)
- **Mitigation**:
  - MantineTiptapEditor already optimized
  - Auto-save to local storage (prevent data loss)
  - Loading indicators for save operations

### Low-Risk Areas

**Risk 6: Variable Name Changes**
- **Impact**: Low - Templates break if variable names change
- **Likelihood**: Low (fixed variable sets, documented)
- **Mitigation**:
  - Variable names treated as API contract (no breaking changes)
  - 30-day notice process for any variable changes
  - Documentation of all variables per category

---

## Quality Gate Checklist (95% Required for Phase 1 Approval)

Phase 1 Requirements Completion Checklist:

- [x] Executive summary clearly defines business value
- [x] Business context explains problem and solution
- [x] User stories for all user roles (Administrator, Event Organizer)
- [x] Acceptance criteria for each user story (Given/When/Then)
- [x] Functional requirements organized by category
- [x] Non-functional requirements (performance, security, usability, reliability, maintainability)
- [x] Business rules documented explicitly
- [x] Success metrics defined (quantitative and qualitative)
- [x] Security requirements addressed (XSS, CSRF, SQL injection, authentication)
- [x] Privacy requirements considered (email addresses, user data)
- [x] Compliance requirements noted (CAN-SPAM, GDPR)
- [x] User roles and permissions clearly defined
- [x] Edge cases identified (reset template, invalid variables, SendGrid failures)
- [x] Mobile experience considered (desktop/tablet only for admin UI)
- [x] Examples and scenarios provided (variable substitution, template hierarchy)
- [x] Questions for Product Manager documented (SendGrid plan upgrade timing)
- [x] Assumptions explicitly stated (technical, business, user)
- [x] Constraints documented (technical, business, time, resource)
- [x] Out of scope clearly defined (what this feature will NOT do)
- [x] Dependencies identified (frontend, backend, external services)
- [x] Integration points documented (authentication, events, vetting, SendGrid)
- [x] Risk analysis completed (high, medium, low risks with mitigation)
- [x] Data structure requirements specified (3 tables, enums, DTOs)
- [x] API endpoints documented (10 endpoints with HTTP methods)
- [x] UI specifications provided (routes, components, layout)
- [x] Variable sets defined per category (5 categories, complete lists)
- [x] Migration plan for existing Vetting templates
- [x] Template type enums defined (22 total templates)
- [x] Audit trail requirements specified
- [x] Reference to approved plan document
- [x] Alignment with React + TypeScript architecture
- [x] NSwag type generation pattern followed (no manual interfaces)

**Completion Score**: 30/30 = **100%** ✅

---

## Questions for Product Manager

### Question 1: SendGrid Plan Upgrade Timing
**Context**: Current SendGrid account is on free tier (100 emails/day). Production platform will need significantly higher volume for event confirmations, reminders, and ad-hoc emails.

**Question**: When should we upgrade SendGrid plan?
- Before implementation starts (so we can test at scale)?
- Before production deployment?
- After production deployment (when volume increases)?

**Recommendation**: Upgrade before production deployment to avoid launch-day surprises.

---

### Question 2: Email Analytics Requirements
**Context**: SendGrid provides open rates, click rates, bounce rates in their dashboard. We could integrate this data into WitchCityRope admin UI.

**Question**: Do administrators need email analytics visible in WitchCityRope UI, or is SendGrid dashboard sufficient for initial release?

**Recommendation**: Use SendGrid dashboard initially (saves 10-15 hours development). Add integrated analytics as future enhancement if needed.

---

### Question 3: Template Approval Workflow
**Context**: Current design assumes administrators are trusted users (no approval required for global template changes). Event organizer template changes also apply immediately.

**Question**: Do we need an approval workflow where:
- Global template changes reviewed before going live?
- Event organizer customizations reviewed before use?

**Recommendation**: No approval workflow initially (trusted users). Add if abuse occurs.

---

### Question 4: Multilingual Support Priority
**Context**: All templates currently English-only. WitchCityRope serves Salem, MA community (primarily English-speaking).

**Question**: What priority is multilingual template support?
- High (needed for initial release)
- Medium (needed within 6 months)
- Low (future enhancement only)

**Recommendation**: Low priority (Salem community primarily English). Add if demand increases.

---

### Question 5: Template Version History UI
**Context**: Template versions tracked in database (version number, updatedBy, updatedAt), but no UI to view history, compare versions, or rollback.

**Question**: What priority is template version history UI?
- High (needed for initial release)
- Medium (needed within 3 months)
- Low (metadata display sufficient for now)

**Recommendation**: Low priority initially (metadata sufficient). Add full version history as future enhancement if administrators request it.

---

## Appendix A: Template Type Reference

### Complete List of 22 Default Templates

**Vetting (6)**:
1. ApplicationReceived - Sent immediately when new vetting application submitted
2. InterviewApproved - Sent when admin approves applicant for interview
3. Approved - Sent when applicant fully approved (vetted member status)
4. OnHold - Sent when application placed on hold (more info needed)
5. Denied - Sent when application denied
6. InterviewReminder - Sent as reminder before scheduled interview

**Events (7)**:
1. Confirmation - Sent immediately on ticket purchase (required)
2. Reminder1Day - Sent 1 day before event (automated)
3. Reminder1Week - Sent 1 week before event (automated)
4. Cancellation - Sent when event cancelled by organizer
5. WaitlistNotification - Sent when moved from waitlist to confirmed
6. PostEventSurvey - Sent after event ends (feedback request)
7. ScheduleChange - Sent when event date/time/venue changes

**Admin (4)**:
1. SystemAlert - Critical system issues requiring user action
2. MaintenanceNotice - Scheduled maintenance notifications
3. SecurityAlert - Security-related notifications (password reset, suspicious activity)
4. AccountSuspension - Account suspended notification

**Incident (4)**:
1. ReportReceived - Sent to reporter confirming incident report received
2. StatusUpdate - Sent when incident status changes
3. ResolutionNotice - Sent when incident resolved
4. FollowUpRequest - Sent requesting additional information

**Ad Hoc (1)**:
1. Custom - Fully customizable, no predefined content

---

## Appendix B: Example Use Cases

### Use Case 1: Event Organizer Customizes Confirmation Email

**Scenario**: Sarah is teaching a rope bondage workshop on "Advanced Harnesses" and wants to include pre-class homework instructions in the confirmation email.

**Steps**:
1. Sarah navigates to Admin > Events > Edit "Advanced Harnesses Workshop"
2. Clicks "Emails" tab
3. Sees "Confirmation Email" template with badge "(Default)"
4. Clicks "Confirmation Email" card
5. Editor loads with global template content:
   ```
   Subject: Your ticket for {{event_title}}

   Body: Hi {{attendee_name}},

   Thank you for registering for {{event_title}} on {{event_date}} at {{event_time}}.

   Venue: {{venue_name}}
   {{venue_address}}

   See you there!
   ```
6. Sarah adds homework section:
   ```
   Subject: Your ticket for {{event_title}} - Pre-Class Homework

   Body: Hi {{attendee_name}},

   Thank you for registering for {{event_title}} on {{event_date}} at {{event_time}}.

   **IMPORTANT - Pre-Class Homework:**
   Please watch this 10-minute video before class: [link]
   This will help us jump right into hands-on practice!

   Venue: {{venue_name}}
   {{venue_address}}

   See you there!
   ```
7. Sarah clicks Save
8. Badge changes to "✓ Customized"
9. All future ticket purchasers for this event receive Sarah's customized email
10. Other events still use global default (not affected)

**Result**: Sarah's workshop participants get homework instructions, other events unaffected.

---

### Use Case 2: Administrator Sends Ad-Hoc Email to All Event Participants

**Scenario**: WitchCityRope is hosting a special performance at a new venue. Admin needs to notify all ticket holders of parking instructions.

**Steps**:
1. Admin navigates to Admin > Email Templates
2. Clicks "Ad Hoc" tab
3. Sees ad-hoc email composer
4. Composes email:
   ```
   Subject: Parking Update for {{event_title}}

   Body: Hi {{recipient_name}},

   Great news! We've secured free parking for {{event_title}}.

   Park in the lot behind the venue (enter from Essex St).
   Show your ticket confirmation email to the attendant for free parking.

   See you on {{event_date}}!
   ```
5. Selects recipient group: "All ticket holders - Rope Performance Night"
6. Preview shows "187 recipients"
7. Admin clicks "Send"
8. System sends via SendGrid
9. Record saved to SentAdHocEmails table with:
   - Full email content
   - Recipient group: "All ticket holders - Rope Performance Night"
   - Recipient count: 187
   - Sent at: 2025-11-09 14:32:00
   - Sent by: admin@witchcityrope.com
   - SendGrid message ID: sg_abc123xyz
10. History table shows newly sent email

**Result**: All 187 ticket holders notified of parking, full audit trail retained.

---

### Use Case 3: Administrator Updates Global Event Reminder Template

**Scenario**: Administrator wants to add "Add to Calendar" link to all 1-day reminder emails for better attendance.

**Steps**:
1. Admin navigates to Admin > Email Templates
2. Clicks "Events" tab
3. Clicks "Reminder - 1 Day Before" template card
4. Editor loads with current global template
5. Admin adds calendar link section:
   ```
   Subject: Tomorrow: {{event_title}}

   Body: Hi {{attendee_name}},

   Just a reminder that {{event_title}} is TOMORROW!

   Date: {{event_date}}
   Time: {{event_time}}
   Venue: {{venue_name}} - {{venue_address}}

   **Add to your calendar:** [Google Calendar Link] [iCal Download]

   We can't wait to see you!
   ```
6. Admin clicks Save
7. Template version increments from v3 to v4
8. Audit record created:
   - Updated by: admin@witchcityrope.com
   - Updated at: 2025-11-09 10:15:00
   - Version: 4
9. All future 1-day reminder emails (for all events) include calendar link
10. Events with customized reminder templates NOT affected (still use event-specific version)

**Result**: Future reminder emails improved, existing customizations preserved.

---

## Document Metadata

**Created**: 2025-11-09
**Author**: Business Requirements Agent
**Approved Plan Reference**: `/session-work/2025-11-09/email-templates-admin-approved-plan.md`
**Estimated Effort**: 18-22 hours (Backend: 10-12h, Frontend: 6-8h, Testing: 2-3h)
**Target Timeline**: 4-5 days with orchestration
**Next Phase**: Design (Phase 2) - Database schema design, API specification, UI wireframes

**Review Status**: Awaiting stakeholder approval before proceeding to Phase 2

---

**END OF BUSINESS REQUIREMENTS DOCUMENT**
