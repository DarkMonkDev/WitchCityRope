# Business Requirements Handoff - Vetting System Implementation
<!-- Date: 2025-09-22 -->
<!-- From: Business Requirements Agent -->
<!-- To: UI Designer & React Developer -->
<!-- Status: Requirements Phase Complete - Updated Based on User Feedback -->

## Phase: Business Requirements Complete (Updated)
## Date: 2025-09-22
## Feature: WitchCityRope Vetting System Implementation
## Version: 2.0 (Updated based on comprehensive user feedback)

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **Authentication Required Before Application**: Users MUST create account and login before accessing vetting form
   - ✅ Correct: Check authentication state, redirect to login if not authenticated
   - ❌ Wrong: Allow anonymous users to start application process

2. **Single Application Per Member**: Only ONE application allowed per member - no resubmission
   - ✅ Correct: Check if user already has application, prevent new submissions, show status instead
   - ❌ Wrong: Allowing multiple applications from same user

3. **Single Session Submission Only**: NO draft functionality - application completed in one session
   - ✅ Correct: Form clears on browser refresh, no save/resume functionality
   - ❌ Wrong: Implementing auto-save or draft storage system

4. **Dashboard Status Integration**: Application status shown on user dashboard, NOT separate page
   - ✅ Correct: Status prominently displayed on user dashboard with clear next steps
   - ❌ Wrong: Creating separate application status page

5. **Optional Admin Notes**: Admin notes are OPTIONAL - automatic notes sufficient for audit trail
   - ✅ Correct: Admin can add notes but not required, system generates automatic notes
   - ❌ Wrong: Making admin notes mandatory for status changes

6. **Email Templates in Vetting Admin**: Templates managed within vetting section, NOT generic email management
   - ✅ Correct: Email template management co-located with vetting admin interface
   - ❌ Wrong: Generic email template system separate from vetting workflow

7. **Bulk Operations**: Admins can perform bulk reminder emails and bulk status changes
   - ✅ Correct: Checkbox selection with bulk actions for efficiency
   - ❌ Wrong: Only individual application management

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| **Business Requirements v2.0** | `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/requirements/business-requirements.md` | Updated User Stories (lines 74-192), New Email Templates (lines 356-396), Bulk Operations (lines 174-192) |
| **Test Executor Handoff** | `/docs/functional-areas/vetting-system/handoffs/test-executor-2025-09-13-handoff.md` | Critical database migration issues, existing API status |
| **DTO Alignment Strategy** | `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` | NSwag auto-generation requirements, NEVER create manual DTOs |
| **Domain Architecture** | `/docs/architecture/react-migration/domain-layer-architecture.md` | Type generation pipeline, build dependencies |

## 🚨 UPDATED BUSINESS RULES & FEATURES

### Major Changes from User Feedback:

1. **Application Stages Updated**:
   - OLD: Submitted → Under Review → Interview Scheduled → Final Decision
   - NEW: Submitted → Under Review → **Interview Approved** → Interview Scheduled → Final Decision
   - **Interview Approved**: User can schedule but hasn't yet (NEW stage)

2. **Email Templates Updated** (now 6 types):
   1. Application Received
   2. **Interview Approved** (NEW - ready to schedule)
   3. Application Approved
   4. Application On Hold
   5. Application Denied
   6. **Interview Reminder** (NEW - for old interview-approved applications)

3. **Application Form Simplified**:
   - **REMOVED**: Emergency contact field
   - **REMOVED**: References section
   - **REMOVED**: File upload capability
   - **KEEP**: Use existing wireframe as guide

4. **Features Explicitly Removed**:
   - ❌ Appeal process for denials (decisions are final)
   - ❌ Anonymous feedback system
   - ❌ Membership cards
   - ❌ References system
   - ❌ Emergency contacts
   - ❌ Separate application status page

5. **New Bulk Operations**:
   - **Send Reminder Emails**: For interview-approved applications older than X days
   - **Bulk Status Change to On Hold**: For applications older than X days that haven't responded

6. **On Hold Process**:
   - On Hold emails direct users to contact support@witchcityrope.com to reactivate
   - No automated reactivation process

## 🚨 KNOWN PITFALLS

1. **Database Schema Sync Issue**: EF Core migrations are out of sync from September 13 testing
   - **Why it happens**: Database schema doesn't match current model
   - **How to avoid**: Run migration sync BEFORE implementing any database operations

2. **Manual DTO Interface Creation**: Creating TypeScript interfaces manually instead of using NSwag
   - **Why it happens**: Developer unfamiliarity with type generation pipeline
   - **How to avoid**: ALWAYS use generated types from @witchcityrope/shared-types package

3. **Implementing Draft Functionality**: Natural developer instinct to add save/resume
   - **Why it happens**: Common UX pattern, seems helpful
   - **How to avoid**: Explicitly documented as NOT REQUIRED in business rules

4. **Creating Separate Status Page**: Developer might create dedicated application status page
   - **Why it happens**: Seems like natural separation of concerns
   - **How to avoid**: Status MUST be integrated into user dashboard per requirements

5. **Making Admin Notes Required**: Making notes mandatory for status changes
   - **Why it happens**: Seems like good audit practice
   - **How to avoid**: Notes are OPTIONAL - automatic notes are sufficient

6. **Generic Email Management**: Putting templates in general admin email section
   - **Why it happens**: Cleaner separation of concerns
   - **How to avoid**: Templates MUST be in vetting admin section for workflow efficiency

## ✅ VALIDATION CHECKLIST

Before proceeding to implementation phase, verify:

- [ ] Authentication check implemented before application access
- [ ] One application per member enforcement
- [ ] No draft/save functionality in application form
- [ ] Application status integrated into user dashboard
- [ ] Admin notes are optional, automatic notes generated
- [ ] Email templates managed within vetting admin section
- [ ] All 6 email template types designed and manageable
- [ ] Bulk operations for reminders and status changes
- [ ] Interview Approved stage included in workflow
- [ ] On Hold emails direct to support@witchcityrope.com
- [ ] SendGrid integration configured and tested
- [ ] Admin role verification for review interface access
- [ ] NSwag type generation pipeline understood
- [ ] Database migration issues resolved
- [ ] Existing API endpoints identified and mapped

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing API Implementation**: Vetting system backend largely complete from September 13
   - **Impact**: Frontend needs to integrate with existing endpoints
   - **Required Changes**: Review existing API structure before new development

2. **Database Migration Blocker**: Schema sync issues prevent integration testing
   - **Impact**: Cannot validate database operations until migrations fixed
   - **Required Changes**: Backend developer must resolve migration sync first

3. **SendGrid Configuration**: Email service not yet configured
   - **Impact**: Email functionality cannot be tested until service setup
   - **Required Changes**: DevOps setup of SendGrid API keys and configuration

4. **Authentication System**: Uses existing platform authentication
   - **Impact**: Must integrate with current login system
   - **Required Changes**: Use existing auth context and role checking patterns

5. **Calendar Integration**: Future consideration for Google Calendar
   - **Impact**: Interview scheduling may need future enhancement
   - **Required Changes**: Note in technical debt for future development

## 📊 DATA MODEL DECISIONS (UPDATED)

### VettingApplication Entity (Simplified)
```
Entity: VettingApplication
- Id: Guid (Required) - Primary key
- ApplicantId: Guid (Required) - Foreign key to User
- SubmittedAt: DateTime (Required) - ISO 8601 format
- Status: VettingStatus (Required) - Enum: Submitted, UnderReview, InterviewApproved, InterviewScheduled, Approved, Denied, OnHold
- AdminNotes: string (Optional) - Rich text support
- SceneName: string (Required) - 3-50 characters
- RealName: string (Required) - 3-100 characters
- Email: string (Required) - Valid email format
- PhoneNumber: string (Optional) - Formatted phone number
- Experience: string (Required) - 500-2000 characters
- SafetyTraining: string (Required) - 200-1000 characters
- AdditionalInfo: string (Optional) - Max 1000 characters

REMOVED FIELDS:
- EmergencyContact (removed per user feedback)
- References (removed per user feedback)
- FileUpload capability (removed per user feedback)

Business Logic:
- Must validate all required fields before submission
- Status changes trigger email notifications
- Admin notes OPTIONAL for status changes
- Automatic audit trail created for all modifications
- One application per member enforcement
```

### EmailTemplate Entity (Updated)
```
Entity: EmailTemplate
- Id: Guid (Required) - Primary key
- TemplateType: EmailTemplateType (Required) - Enum: ApplicationReceived, InterviewApproved, Approved, OnHold, Denied, InterviewReminder
- Subject: string (Required) - Max 200 characters
- Body: string (Required) - Rich text with variable substitution
- IsActive: boolean (Required) - Default true
- CreatedAt: DateTime (Required) - Timestamp
- UpdatedAt: DateTime (Required) - Last modification
- UpdatedBy: Guid (Required) - Foreign key to User who modified

Business Logic:
- Only one active template per type at a time
- Variables must be replaced before sending (e.g., {applicantName})
- Admin-only access for template modifications
- Templates managed within vetting admin section
```

### Bulk Operation Configuration (NEW)
```
Entity: BulkOperationConfiguration
- ReminderEmailThresholdDays: int (Required) - Default 7 days
- OnHoldThresholdDays: int (Required) - Default 14 days
- LastReminderSent: DateTime (Optional) - Per application tracking

Business Logic:
- Configurable thresholds for bulk operations
- Reminder emails only for InterviewApproved status
- Bulk status change only for non-responsive applications
- Exclude already approved/denied/on-hold from bulk operations
```

## 🎯 SUCCESS CRITERIA (UPDATED)

1. **Authentication Flow Test**:
   - **Input**: Unauthenticated user navigates to /vetting/apply
   - **Expected Output**: Redirected to login page, then back to application after successful login

2. **Single Application Enforcement Test**:
   - **Input**: User who already submitted application tries to access application form
   - **Expected Output**: Redirected to dashboard with current status, cannot resubmit

3. **Dashboard Status Display Test**:
   - **Input**: User with submitted application logs in
   - **Expected Output**: Dashboard shows application status prominently with next steps

4. **Application Submission Test**:
   - **Input**: Complete valid application form and submit
   - **Expected Output**: Confirmation page displayed, email sent to applicant, application stored in database, status shown on dashboard

5. **Admin Review Test**:
   - **Input**: Admin user accesses review interface
   - **Expected Output**: Grid of applications displayed, can click to view details, can update status with optional notes

6. **Email Template Management Test (In Vetting Admin)**:
   - **Input**: Admin modifies "Application Approved" template within vetting admin section
   - **Expected Output**: Template saved, next approval email uses new template content

7. **Status Change Notification Test**:
   - **Input**: Admin changes application status to "Interview Approved"
   - **Expected Output**: Email sent to applicant using interview approved template, automatic audit log entry created

8. **Bulk Reminder Email Test**:
   - **Input**: Admin selects multiple interview-approved applications older than threshold
   - **Expected Output**: Reminder emails sent using reminder template, audit trail for each action

9. **Bulk Status Change Test**:
   - **Input**: Admin selects multiple old applications and bulk changes to "On Hold"
   - **Expected Output**: Status changed for selected applications, on-hold emails sent, audit trail created

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT create draft/save functionality for applications
- ❌ DO NOT allow multiple applications per member
- ❌ DO NOT create separate application status page (integrate with dashboard)
- ❌ DO NOT make admin notes mandatory for status changes
- ❌ DO NOT put email templates in generic email management (keep in vetting admin)
- ❌ DO NOT allow non-Admin users to access review interface
- ❌ DO NOT create manual TypeScript interfaces for API data
- ❌ DO NOT hardcode email templates in code
- ❌ DO NOT implement file upload for references (removed from requirements)
- ❌ DO NOT implement appeal process (decisions are final)
- ❌ DO NOT implement emergency contact fields (removed)
- ❌ DO NOT implement references system (removed)
- ❌ DO NOT assume database schema is synchronized (must verify first)
- ❌ DO NOT bypass authentication checks for any functionality

## 📝 TERMINOLOGY DICTIONARY (UPDATED)

| Term | Definition | Example |
|------|------------|---------|
| **Vetting Application** | Complete application for vetted member status (simplified form) | User submits form with experience, safety training, additional info |
| **Vetting Status** | Current stage of application review (updated workflow) | Submitted, Under Review, Interview Approved, Interview Scheduled, etc. |
| **Interview Approved** | NEW stage where user can schedule but hasn't yet | Admin approves for interview, user needs to schedule |
| **Email Template** | Admin-configurable email format (6 types) | "Interview Approved" template with {applicantName} variable |
| **Admin Notes** | Optional internal notes visible only to admins | "Great experience, approved for interview" |
| **Automatic Notes** | System-generated audit trail notes | "Admin John changed status to Interview Approved at 2025-09-22 14:30" |
| **Bulk Operations** | Admin efficiency features for multiple applications | Send reminder emails to old interview-approved applications |
| **Dashboard Integration** | Status display within user dashboard | Application status shows on main dashboard, not separate page |
| **SendGrid Integration** | Third-party email service for notifications | Automated emails sent through SendGrid API |

## 🔗 NEXT AGENT INSTRUCTIONS

### For UI Designer:
1. **FIRST**: Read existing wireframes at `/docs/functional-areas/vetting-system/design/wireframes/`
2. **SECOND**: Review updated business requirements focusing on dashboard integration and bulk operations
3. **THIRD**: Design admin review interface with bulk operation checkboxes and actions
4. **FOURTH**: Create mockups for email template management within vetting admin section
5. **FIFTH**: Design dashboard status display integration (NOT separate status page)
6. **THEN**: Create detailed mockups for all 6 email template types including new Interview Approved and Interview Reminder

### For React Developer:
1. **FIRST**: Verify database migrations are fixed (coordinate with Backend Developer)
2. **SECOND**: Review existing API endpoints in `/apps/api/Features/Vetting/`
3. **THIRD**: Set up NSwag type generation for vetting DTOs
4. **FOURTH**: Implement authentication checks and single application enforcement
5. **FIFTH**: Integrate application status display into user dashboard
6. **SIXTH**: Build simplified application form (no emergency contacts, references, file uploads)
7. **SEVENTH**: Implement bulk operations with checkbox selection
8. **THEN**: Integrate email template management within vetting admin section

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-09-22 (Updated based on user feedback)
**Key Finding**: Requirements significantly simplified and focused. Dashboard integration and bulk operations are new key features. Email template management co-located with vetting admin for workflow efficiency.

**Next Agent Should Be**: UI Designer (for updated mockups) then React Developer (for implementation)
**Next Phase**: Design & Implementation
**Estimated Effort** (Updated):
- UI Design: 3-4 days (additional bulk operations and dashboard integration)
- React Implementation: 6-8 days (dashboard integration, bulk operations, simplified form)
- Integration & Testing: 3-4 days (bulk operations testing)

## 🚨 CRITICAL DEPENDENCIES

### Must Be Resolved Before Implementation:
1. **Database Migration Sync**: Backend Developer must fix EF Core schema sync
2. **SendGrid Configuration**: DevOps must configure email service
3. **API Endpoint Verification**: Confirm all required endpoints exist including bulk operations
4. **Dashboard Integration**: Coordinate with existing dashboard implementation

### Can Be Parallel Development:
1. **UI Design**: Can proceed while database issues are resolved
2. **Email Template Design**: Can be designed while technical issues are fixed (6 templates now)
3. **Frontend Component Structure**: Can be built with mock data
4. **Bulk Operations UI**: Can be designed independently

## 🎯 NEW FEATURES TO HIGHLIGHT

### Dashboard Integration:
- Application status prominently displayed on user dashboard
- Clear next steps and current stage information
- No separate application status page needed

### Bulk Operations:
- Checkbox selection in admin grid
- Bulk reminder emails for interview-approved applications
- Bulk status change to "On Hold" for non-responsive applications
- Configurable time thresholds for bulk actions

### Simplified Application:
- Removed emergency contacts, references, file uploads
- Focus on core experience and safety training questions
- One application per member lifetime

### Email Template Co-location:
- Templates managed within vetting admin section
- 6 template types including new Interview Approved and Interview Reminder
- Templates stay with their functional context

---

**Status**: Business Requirements phase complete with user feedback incorporated, ready for design and implementation
**Critical Success Factor**: Dashboard integration and bulk operations implementation
**Review Date**: 2025-09-23 (verify all critical dependencies resolved and updated requirements understood)