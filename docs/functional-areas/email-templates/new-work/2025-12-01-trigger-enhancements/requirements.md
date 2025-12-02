# Email Template Trigger Enhancements - Requirements
<!-- Date: 2025-12-01 -->
<!-- Owner: Chad (User) -->
<!-- Status: APPROVED (v3.0) -->
<!-- Last Updated: 2025-12-01 -->

## Original User Requirements

From conversation on 2025-12-01:

### 1. Fixed Event Triggers (Vetting/Admin/Incident Tabs)
Templates fire when specific events occur:
- Ticket purchase
- Ticket cancellation
- Password reset
- Vetting status changes
- etc.

**IMPORTANT**: These are action-based triggers hardcoded in service code. NO admin UI configuration needed for these tabs. The TemplateType already identifies the trigger event.

### 2. Time-Based Triggers (Events Tab ONLY)
Templates fire X days before/after **session start time**:
- Positive numbers = days BEFORE session (e.g., 3 = 3 days before)
- Negative numbers = days AFTER session (e.g., -2 = 2 days after, for post-event surveys)
- Reference point: **Session start time** (NOT event start time)

This is NEW infrastructure - currently no scheduled email jobs exist.

**Multi-Session Events**: Each session triggers emails independently based on its own start time.

### 3. Recipient Group Selection (Events Tab)

#### Event-Specific Recipients (NOT UserSegment)
| Recipient Group | Description | Business Logic |
|-----------------|-------------|----------------|
| **SessionAttendees** | Users who actually attended a session | Based on check-in records |
| **RSVPTicketHolders** | RSVP users (socials) OR ticket holders (classes) | Event type determines which; deduplicate if user has both |
| **SessionVolunteers** | Volunteers assigned to that specific session | From volunteer assignments |
| **Teachers** | Teachers assigned to the session | From session teacher assignments |

**Note**:
- Waitlist NOT implemented - excluded
- EventVolunteers excluded (only per-session volunteers exist)
- No "buy for friend" feature - no separate purchaser/attendee distinction needed
- Cancellations do not need recipient option

#### Ad Hoc Tab Recipients
Uses existing UserSegment enum (8 segments):
- AllVettedMembers, AllPreVettedMembers, AllTeachers, AllDMs
- AllSafetyTeam, AllAdmins, EmailNotVerified, VettingPending

### 4. Enhanced Template Cards (Events Tab UI)
Current cards show: Name, Subject
Need to add:
- Trigger type indicator (Fixed Event / Time-Based)
- Timing offset display (e.g., "3 days before" or "2 days after")
- Recipient group display

### 5. Two-Level Configuration
- **Global Templates** (Admin > Email Templates): Default trigger settings
- **Event-Specific Overrides** (Event Details > Emails Tab): Per-event customization using copy-on-edit pattern

### 6. Ad Hoc Tab Enhancements

#### Scheduled Send (Nice to Have - APPROVED)
- Add ability to schedule an ad hoc email for future delivery
- Use case: Queue up monthly newsletters, schedule end-of-month communications
- ScheduledSendAt field (nullable DateTime)
- If null = send immediately; if set = send at that time

#### Save as Template (APPROVED)
- Save an ad hoc email as a reusable template
- Template appears in the Ad Hoc templates area (top of tab)
- Use subject line as template name (or allow custom title)
- Use case: Monthly newsletter template that can be modified and reused

#### Delete Template (APPROVED)
- Ability to delete saved ad hoc templates
- ADD and DELETE features ONLY appear on Ad Hoc tab
- Other tabs (Vetting, Admin, Events, Incident) have fixed template sets

---

## Constraints

- No live data - no backwards compatibility concerns
- NO MediatR - use direct service pattern per ARCHITECTURE-WITHOUT-MEDIATR.md
- Follow existing vertical slice architecture
- Hangfire available for scheduled jobs (currently only used for backups)
- Follow existing copy-on-edit pattern for event overrides
- Follow Result<T> pattern for error handling

---

## Tab-by-Tab Behavior Summary

| Tab | Trigger Type | Trigger Config UI | Recipient Config | Template Add/Delete |
|-----|--------------|-------------------|------------------|---------------------|
| **Events** | Time-Based | Yes (days before/after session) | EventRecipientGroup dropdown | No |
| **Vetting** | Fixed (action-based) | No | N/A (hardcoded) | No |
| **Admin** | Fixed (action-based) | No | N/A (hardcoded) | No |
| **Incident** | Fixed (action-based) | No | N/A (hardcoded) | No |
| **Ad Hoc** | Manual/Scheduled | Scheduled send date | UserSegment dropdown | Yes (add/delete) |

---

## Current System State (Verified)

### Email Templates
- **GlobalEmailTemplate**: Category, TemplateType, Subject, HtmlBody, PlainTextBody, Variables
  - TemplateType = trigger event identifier (e.g., "ApplicationReceived", "Confirmation")
  - NO trigger configuration fields
  - NO recipient targeting fields

- **EventEmailTemplate**: Event-specific overrides (copy-on-edit pattern)
  - Has RecipientGroup (nullable string) - NOT used systematically
  - Has TargetSessions (string[])
  - NO trigger configuration

### Email Sending
- **IEmailService**: Core SendGrid integration
- **VettingEmailService**: Domain-specific wrapper with hardcoded switch statements

### Background Jobs
- Hangfire configured for PostgreSQL storage
- Only BackupJob and RestoreJob exist - NO email scheduling jobs

### User Segments
UserSegment enum: AllVettedMembers, AllPreVettedMembers, AllTeachers, AllDMs, AllSafetyTeam, AllAdmins, EmailNotVerified, VettingPending

---

## What's Missing (To Be Implemented)

### Events Tab
1. Trigger configuration fields on GlobalEmailTemplate (Events category only)
2. Override fields on EventEmailTemplate
3. New EventRecipientGroup enum
4. EmailSchedulerJob Hangfire job (session-based timing)
5. EmailTriggerLog for audit/idempotency
6. UI for trigger configuration and recipient selection
7. Enhanced template cards with trigger/recipient display

### Ad Hoc Tab
1. ScheduledSendAt field on SentAdHocEmail (or new ScheduledAdHocEmail entity)
2. SaveAsTemplate functionality
3. Delete template functionality
4. Separate storage for saved ad hoc templates (AdHocEmailTemplate entity)
5. UI for scheduling and template management

---

## Implementation Scope Assessment

### Core Scope (Events Tab Triggers)
- Estimated effort: 3-4 sessions
- Low risk - extends existing patterns

### Ad Hoc Enhancements
- Scheduled send: +0.5-1 session (uses same EmailSchedulerJob)
- Save/Delete templates: +0.5-1 session (new entity, simple CRUD)
- Total additional: ~1-2 sessions
- Low-medium risk - straightforward extensions

**Recommendation**: Include Ad Hoc enhancements. The infrastructure for scheduled sends (EmailSchedulerJob) is already needed for Events triggers, so adding ad hoc scheduling is minimal extra work.

---

## Agent Delegation Requirements

**CRITICAL FOR ALL SUB-AGENTS:**

1. **MANDATORY CODE REVIEW**: Before proposing ANY implementation, you MUST:
   - Read the actual source files (not just assume structure)
   - Verify patterns by reading existing code (EmailTemplateService, BackupJob, etc.)
   - Confirm NO MediatR is used (read ARCHITECTURE-WITHOUT-MEDIATR.md)
   - Check existing entity structures before adding fields

2. **FOLLOW DOCUMENTED PATTERNS**:
   - Direct service injection (no command/handler abstraction)
   - Result<T> for error handling
   - Copy-on-edit for event overrides
   - Hangfire job pattern from BackupJob.cs

3. **REFERENCE FILES BEFORE IMPLEMENTING**:
   - `/apps/api/Features/EmailTemplates/Services/EmailTemplateService.cs`
   - `/apps/api/Features/EmailTemplates/Entities/GlobalEmailTemplate.cs`
   - `/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
   - `/apps/api/Features/Backup/Jobs/BackupJob.cs`
   - `/docs/architecture/ARCHITECTURE-WITHOUT-MEDIATR.md`

4. **CREATE HANDOFF DOCUMENTS**: Each phase must produce a handoff document in `/docs/functional-areas/email-templates/new-work/2025-12-01-trigger-enhancements/handoffs/`
