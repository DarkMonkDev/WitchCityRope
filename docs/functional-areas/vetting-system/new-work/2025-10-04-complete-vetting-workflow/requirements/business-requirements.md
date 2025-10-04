# Business Requirements: Complete WitchCityRope Vetting Workflow Implementation
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Planning -->

## Executive Summary

This document outlines the phased implementation plan to complete the WitchCityRope vetting workflow system. The system currently has a functioning public application form and conditional menu visibility. This plan addresses the remaining components needed for a complete end-to-end vetting workflow: access control, email notifications, admin review interface, and bulk operations.

### Current State (October 4, 2025)

**✅ Already Implemented:**
- VettingApplication entity with 10 vetting statuses (Draft, Submitted, UnderReview, InterviewApproved, PendingInterview, InterviewScheduled, OnHold, Approved, Denied, Withdrawn)
- Database schema with migrations applied
- Public vetting application form (simplified, authenticated users only)
- Dashboard integration showing vetting status
- Conditional "How to Join" menu visibility based on vetting status (46 tests passing, 100% coverage)
- VettingStatus enum alignment across frontend and backend
- API endpoint: GET /api/vetting/status
- React hooks: useVettingStatus, useMenuVisibility
- VettingStatusBox component with 10 status variants

**❌ Missing Components (Critical for Complete Workflow):**
1. **Access Control System**: Block Denied/OnHold members from RSVP/ticket purchases
2. **Email Notification System**: SendGrid integration with status change triggers
3. **Admin Review Interface**: Grid, detail view, status changes, notes management
4. **Bulk Operations**: Reminder emails, bulk status changes
5. **Email Template Management**: Admin interface for customizing templates
6. **Additional API Endpoints**: Admin endpoints for review workflow
7. **Complete Status Transition Workflow**: Validation, audit logging, email triggers

### Target State

A complete vetting workflow where:
- Members apply through the simplified form ✅ (implemented)
- System prevents denied/on-hold members from participating in events ❌ (Phase 1)
- Admins review applications through a dedicated interface ❌ (Phase 3)
- Status changes trigger appropriate email notifications ❌ (Phase 2)
- Audit trail captures all changes ❌ (Phase 3)
- Bulk operations streamline admin workflow ❌ (Phase 4)

## Business Context

### Problem Statement

While the vetting application form and status visibility are implemented, critical workflow components are missing:

1. **Access Control Gap**: Denied or on-hold members can still RSVP and purchase tickets for events they shouldn't access
2. **Communication Gap**: No automated email notifications for status changes, creating manual communication burden
3. **Admin Workflow Gap**: No interface for admins to review applications, change statuses, or add notes
4. **Efficiency Gap**: No bulk operations for managing multiple applications simultaneously
5. **Transparency Gap**: No comprehensive audit trail for vetting decisions

These gaps create:
- **Safety Risk**: Denied members can access events they shouldn't
- **Admin Burden**: Manual email composition for every status change
- **Poor Experience**: Applicants don't receive timely status updates
- **Process Inconsistency**: No standardized admin review process
- **Compliance Risk**: Incomplete audit trail for vetting decisions

### Business Value

**Phase 1 (Access Control) - Highest Priority:**
- **Community Safety**: Prevent denied/on-hold members from accessing restricted events
- **Policy Enforcement**: Automated enforcement of vetting requirements
- **Risk Reduction**: Eliminate manual verification at event check-in
- **Clear Boundaries**: System-enforced access controls instead of honor system

**Phase 2 (Email System) - High Priority:**
- **Communication Automation**: 80% reduction in manual email composition
- **Applicant Satisfaction**: Timely, professional status updates
- **Admin Efficiency**: Automated notifications free up admin time
- **Process Transparency**: Consistent communication for all applicants

**Phase 3 (Admin Interface) - High Priority:**
- **Workflow Streamlining**: Centralized application review process
- **Decision Tracking**: Comprehensive audit trail for all actions
- **Process Consistency**: Standardized review and status change workflow
- **Oversight Capability**: Visibility into entire vetting pipeline

**Phase 4 (Bulk Operations) - Medium Priority:**
- **Administrative Efficiency**: 50% reduction in repetitive tasks
- **Proactive Communication**: Automated reminder emails for stale applications
- **Pipeline Management**: Bulk status changes for inactive applications
- **Resource Optimization**: Focus admin time on high-value activities

### Success Metrics

**Phase 1 (Access Control):**
- 100% of denied/on-hold members blocked from RSVP/tickets
- 0 manual check-in overrides required
- <100ms access control check response time
- 95%+ user satisfaction with clear access messaging

**Phase 2 (Email System):**
- 100% of status changes trigger email within 30 seconds
- 95%+ email delivery success rate
- 80% reduction in manual admin emails
- 4.5/5.0+ applicant satisfaction with communications

**Phase 3 (Admin Interface):**
- <7 days average review processing time
- 100% of admin actions logged in audit trail
- <2 seconds admin grid load time
- 90%+ admin satisfaction with review interface

**Phase 4 (Bulk Operations):**
- 50% reduction in time spent on routine communications
- 30% reduction in stale applications (>14 days without action)
- <10 seconds for bulk operations on 100 applications
- 85%+ admin satisfaction with bulk tools

## Phased Implementation Plan

### Phase 1: Access Control System (Highest Priority) ⭐

**Why First:**
- Critical safety and policy enforcement requirement
- Users are actively requesting this feature
- Prevents denied members from accessing events they shouldn't
- Relatively isolated implementation (minimal dependencies)
- Quick win with immediate business value

**Timeline:** 1-2 weeks
**Effort:** Medium
**Risk:** Low
**Dependencies:** Existing vetting status system ✅

**Scope:**
1. Event RSVP access control based on vetting status
2. Ticket purchase access control based on vetting status
3. Clear messaging when access is denied
4. Access control business rules and validation
5. Comprehensive testing of access restrictions

**Out of Scope (Phase 1):**
- Email notifications (Phase 2)
- Admin interface (Phase 3)
- Bulk operations (Phase 4)

### Phase 2: Email Notification System (High Priority)

**Why Second:**
- Critical for applicant communication and experience
- Reduces admin burden significantly
- Can be developed in parallel with Phase 1 if resources available
- SendGrid integration is well-documented pattern
- Template-based approach is straightforward

**Timeline:** 2-3 weeks
**Effort:** Medium-High
**Risk:** Medium (external service dependency)
**Dependencies:** Phase 1 access control (for testing), SendGrid account configuration

**Scope:**
1. SendGrid service integration
2. Database-stored email templates (6 types)
3. Automatic email triggers on status changes
4. Email delivery logging and error handling
5. Template variable replacement system
6. Testing with SendGrid sandbox

**Out of Scope (Phase 2):**
- Email template editing interface (Phase 3)
- Bulk email operations (Phase 4)
- Webhook delivery confirmations (future enhancement)

### Phase 3: Admin Review Interface (High Priority)

**Why Third:**
- Requires access control and email system to be complete for full workflow
- Most complex implementation with multiple sub-components
- Benefits from lessons learned in Phases 1 and 2
- Can leverage existing admin UI patterns from events management

**Timeline:** 3-4 weeks
**Effort:** High
**Risk:** Medium (UI complexity, state management)
**Dependencies:** Phase 1 (access control), Phase 2 (email system)

**Scope:**
1. Admin application review grid with filtering and search
2. Application detail view with full information display
3. Status change interface with validation
4. Admin notes management (optional notes + automatic audit notes)
5. Email template management interface
6. Audit trail display
7. Status transition validation
8. Real-time status updates

**Out of Scope (Phase 3):**
- Bulk operations (Phase 4)
- Analytics dashboard (future enhancement)
- Calendar integration (future enhancement)

### Phase 4: Bulk Operations (Medium Priority)

**Why Fourth:**
- Requires complete admin interface from Phase 3
- Optimization feature rather than core workflow requirement
- Can be deferred if timeline pressures exist
- Benefits from established patterns in previous phases

**Timeline:** 1-2 weeks
**Effort:** Medium
**Risk:** Low
**Dependencies:** Phase 3 (admin interface)

**Scope:**
1. Bulk reminder email sending for interview-approved applications
2. Bulk status change to "On Hold" for stale applications
3. Configurable time thresholds for bulk operations
4. Preview of affected applications before execution
5. Progress tracking for long-running operations
6. Results summary with success/failure counts
7. Individual audit log entries for all bulk changes

**Out of Scope (Phase 4):**
- Automated bulk operations on schedules (future enhancement)
- Advanced reporting on bulk operation history (future enhancement)

## Detailed User Stories by Phase

### Phase 1: Access Control System

#### Story 1.1: Block Denied Members from RSVP
**As a** system administrator
**I want to** prevent denied vetting applicants from RSVPing to events
**So that** only approved members can participate in community events

**Acceptance Criteria:**
- Given a user with vetting status "Denied" (ID: 8)
- When they attempt to RSVP for any event
- Then they see a clear message: "Your vetting application was denied. You cannot RSVP for events at this time."
- And the RSVP button is disabled or hidden
- And no RSVP record is created in the database
- And the action is logged for audit purposes

**Business Rules:**
- Denied status (8) blocks all RSVP attempts
- OnHold status (6) blocks all RSVP attempts
- Withdrawn status (9) blocks all RSVP attempts
- All other statuses allow RSVP based on event access level
- Clear, respectful messaging for blocked users
- Immediate check at UI level + server-side validation

#### Story 1.2: Block On-Hold Members from Ticket Purchases
**As a** system administrator
**I want to** prevent on-hold vetting applicants from purchasing tickets
**So that** only approved members can access paid workshops and classes

**Acceptance Criteria:**
- Given a user with vetting status "OnHold" (ID: 6)
- When they attempt to purchase a ticket for any event
- Then they see a message: "Your application is on hold. Please contact support@witchcityrope.com to reactivate your application."
- And the purchase button is disabled
- And no payment process is initiated
- And the action is logged for audit purposes

**Business Rules:**
- OnHold (6), Denied (8), Withdrawn (9) block all ticket purchases
- Submitted (1), UnderReview (2), InterviewApproved (3), PendingInterview (4), InterviewScheduled (5) allow purchases based on event access level
- Approved/Vetted (7) members have full access
- Payment gateway is never contacted for blocked users
- Server-side validation before PayPal redirect

#### Story 1.3: Display Clear Access Messaging
**As a** member with restricted access
**I want to** understand why I cannot RSVP or purchase tickets
**So that** I know what steps to take to resolve the situation

**Acceptance Criteria:**
- Given a user with restricted vetting status
- When they view an event they cannot access
- Then they see a status-specific message with clear next steps
- And for OnHold status, they see contact information (support@witchcityrope.com)
- And for Denied status, they see a respectful message about the decision
- And the messaging is consistent across RSVP and ticket purchase flows

**Status-Specific Messages:**
- **OnHold (6)**: "Your application is on hold. Please contact support@witchcityrope.com to provide additional information and reactivate your application."
- **Denied (8)**: "Your vetting application was denied. You cannot participate in community events at this time. Decisions are final and there is no appeal process."
- **Withdrawn (9)**: "You have withdrawn your application. Please submit a new application if you would like to join the community."

#### Story 1.4: Event Access Level Integration
**As a** system architect
**I want to** integrate vetting status checks with existing event access levels
**So that** access control is consistently enforced across the platform

**Acceptance Criteria:**
- Given an event with access level "Vetted Members Only"
- When a user attempts to RSVP or purchase tickets
- Then the system checks both event access level AND vetting status
- And users must pass BOTH checks to gain access
- And the most restrictive rule applies
- And clear messaging explains which requirement blocked access

**Access Level Matrix:**
| Event Access Level | Vetting Status Required | Additional Checks |
|-------------------|------------------------|-------------------|
| Public | Any (except OnHold, Denied, Withdrawn) | None |
| General Members | Submitted+ (not OnHold, Denied, Withdrawn) | Active account |
| Vetted Members Only | Approved (7) | IsVetted = true |
| Teacher Events | Approved (7) + Teacher role | IsVetted = true AND Teacher role |
| Admin Events | Admin role | Role check only |

### Phase 2: Email Notification System

#### Story 2.1: Send Confirmation Email on Application Submission
**As an** applicant
**I want to** receive an email confirmation immediately after submitting my application
**So that** I know my application was successfully received

**Acceptance Criteria:**
- Given a user submits a vetting application
- When the application is successfully saved to the database
- Then an email is sent within 30 seconds
- And the email uses the "Application Received" template
- And the email includes application number, submission date, and next steps
- And the email delivery is logged for audit purposes
- And failed email delivery doesn't block application submission

**Email Template Requirements:**
- **Subject**: "Your WitchCityRope Vetting Application - Received"
- **Variables**: `{{applicant_name}}`, `{{application_number}}`, `{{submission_date}}`
- **Content**: Thank you message, what happens next, estimated timeline (1-2 weeks)
- **Footer**: Support contact information

#### Story 2.2: Send Email on Status Change to Interview Approved
**As an** applicant
**I want to** receive an email when my application is approved for interview
**So that** I can schedule my interview with the vetting team

**Acceptance Criteria:**
- Given an admin changes application status to "InterviewApproved" (3)
- When the status change is saved
- Then an email is sent using the "Interview Approved" template
- And the email includes instructions for scheduling the interview
- And the email includes contact information for scheduling
- And the status change is logged with email delivery status

**Email Template Requirements:**
- **Subject**: "Your WitchCityRope Application - Interview Approved"
- **Variables**: `{{applicant_name}}`, `{{approval_date}}`, `{{contact_info}}`
- **Content**: Congratulations on interview approval, how to schedule, what to expect
- **Footer**: Support contact information

#### Story 2.3: Send Email on Final Decision (Approved/Denied/OnHold)
**As an** applicant
**I want to** receive an email when a final decision is made on my application
**So that** I know the outcome and next steps

**Acceptance Criteria:**
- Given an admin changes status to Approved (7), Denied (8), or OnHold (6)
- When the status change is saved
- Then appropriate email template is selected based on new status
- And email is sent with status-specific content
- And for Approved: Welcome message with member benefits
- And for Denied: Professional message (no appeal process mentioned)
- And for OnHold: Instructions to contact support@witchcityrope.com
- And delivery is logged for audit purposes

**Email Templates Required:**
1. **Application Approved**: Welcome, benefits, next steps, community guidelines
2. **Application Denied**: Professional message, encouragement for future growth (no appeal)
3. **Application OnHold**: Reason for hold (if provided), contact support to reactivate

#### Story 2.4: Configure SendGrid Integration
**As a** system administrator
**I want to** configure SendGrid for email delivery
**So that** automated emails are reliably sent to applicants

**Acceptance Criteria:**
- Given SendGrid API key is configured in application settings
- When the application starts
- Then SendGrid client is initialized and validated
- And email service health check passes
- And test email can be sent to admin account
- And error logs capture any configuration issues

**Configuration Requirements:**
- SendGrid API key stored in environment variable
- From email address: noreply@witchcityrope.com
- Reply-to address: support@witchcityrope.com
- Rate limiting: Maximum 100 emails per minute
- Retry logic: 3 attempts with exponential backoff
- Timeout: 30 seconds per send attempt

#### Story 2.5: Log Email Delivery for Audit Trail
**As a** system administrator
**I want to** log all email delivery attempts and results
**So that** I can troubleshoot delivery issues and maintain audit trail

**Acceptance Criteria:**
- Given an email is sent via SendGrid
- When the send operation completes (success or failure)
- Then a log entry is created with timestamp, recipient, template type, delivery status
- And failed deliveries log error details
- And successful deliveries log SendGrid message ID
- And logs are searchable by application ID
- And logs are retained for 2 years minimum

**Log Data Requirements:**
- Application ID (FK)
- Email address (recipient)
- Template type
- Sent timestamp
- Delivery status (Success, Failed, Pending)
- SendGrid message ID (if successful)
- Error message (if failed)
- Retry count

### Phase 3: Admin Review Interface

#### Story 3.1: View Application Review Grid
**As an** admin
**I want to** see a grid of all vetting applications with key information
**So that** I can efficiently review and prioritize applications

**Acceptance Criteria:**
- Given I am logged in as an admin
- When I navigate to the vetting admin section
- Then I see a data grid with columns: Name, Email, Status, Submitted Date, Days Since Submission
- And I can sort by any column
- And I can search by name, email, or scene name
- And I can filter by status (Pending, All, specific statuses)
- And I can filter by date range (Last 7/30/90 days, All time)
- And the grid loads in <2 seconds
- And pagination is available for large datasets

**Grid Features:**
- Sortable columns
- Search bar (name, email, scene name)
- Status filter dropdown
- Date range filter
- Pagination (25 per page default)
- Click row to view details
- Checkbox for bulk selection (Phase 4)

#### Story 3.2: View Application Detail
**As an** admin
**I want to** view full details of a single application
**So that** I can make informed vetting decisions

**Acceptance Criteria:**
- Given I click on an application in the review grid
- When the detail view loads
- Then I see all application information: real name, scene name, email, pronouns, FetLife handle, other names, why join, experience with rope
- And I see current status with status history timeline
- And I see admin notes (optional + automatic audit notes)
- And I see action buttons for valid status transitions
- And I see audit log of all changes
- And I can add optional admin notes
- And the view loads in <1 second

**Detail View Sections:**
1. **Applicant Information**: Name, email, pronouns, handles
2. **Application Content**: Why join, experience (full text)
3. **Status Management**: Current status, action buttons, status history
4. **Notes**: Admin notes timeline (optional manual + automatic audit)
5. **Audit Trail**: All changes with timestamp, admin name, old/new values

#### Story 3.3: Change Application Status
**As an** admin
**I want to** change the status of an application
**So that** I can move it through the vetting workflow

**Acceptance Criteria:**
- Given I am viewing an application detail
- When I click a status change button (e.g., "Approve for Interview")
- Then a confirmation dialog appears with status change summary
- And I can optionally add an admin note (not required)
- And I click "Confirm" to save the change
- And the status updates immediately in the UI
- And an email is sent to the applicant (Phase 2 integration)
- And an audit log entry is created
- And I see a success notification

**Status Transition Validation:**
- Only valid transitions are allowed (per transition matrix)
- Invalid transitions are not shown as buttons
- Server-side validation prevents invalid transitions
- Clear error messages for validation failures

**Valid Status Transitions:**
```
Submitted (1) → UnderReview (2)
UnderReview (2) → InterviewApproved (3), OnHold (6), Denied (8)
InterviewApproved (3) → InterviewScheduled (5), Approved (7), OnHold (6), Denied (8)
InterviewScheduled (5) → Approved (7), OnHold (6), Denied (8)
OnHold (6) → UnderReview (2), InterviewApproved (3), Denied (8)
Approved (7) → OnHold (6) [admin correction only]
Denied (8) → [Final state - no transitions]
Withdrawn (9) → [User action only - no admin transitions]
```

#### Story 3.4: Manage Admin Notes
**As an** admin
**I want to** add optional notes to applications
**So that** I can document important information for my team

**Acceptance Criteria:**
- Given I am viewing an application detail
- When I add an admin note
- Then the note is saved with my name and timestamp
- And the note appears in the notes timeline
- And the note is OPTIONAL (not required for status changes)
- And automatic audit notes are generated for status changes
- And I can view all notes in chronological order
- And notes are searchable (future enhancement)

**Note Types:**
1. **Manual Admin Notes**: Optional notes added by admins, can include sensitive review information
2. **Automatic Audit Notes**: System-generated for status changes (e.g., "Status changed from Submitted to UnderReview by Admin Sarah on 2025-10-15 at 2:30pm")

**Note Display:**
- Timeline format (most recent first)
- Admin name and timestamp
- Note type indicator (manual vs automatic)
- Full note text
- Edit capability (manual notes only, within 1 hour of creation)

#### Story 3.5: Manage Email Templates
**As an** admin
**I want to** customize email templates for vetting communications
**So that** I can maintain professional and consistent messaging

**Acceptance Criteria:**
- Given I am in the vetting admin section
- When I navigate to Email Templates
- Then I see a list of 6 template types
- And I can select a template to edit
- And I see a rich text editor with variable insertion
- And I can preview the template with sample data
- And I can save changes that take effect immediately
- And I can reset to default template if needed
- And changes are logged for audit purposes

**Email Template Types:**
1. Application Received
2. Interview Approved
3. Application Approved
4. Application OnHold
5. Application Denied
6. Interview Reminder (for bulk operations - Phase 4)

**Template Editor Features:**
- Rich text formatting (bold, italic, lists)
- Variable insertion dropdown: `{{applicant_name}}`, `{{application_number}}`, `{{submission_date}}`, `{{contact_email}}`, etc.
- Preview with sample data
- Save button with confirmation
- Reset to default button
- Last updated timestamp and admin name

### Phase 4: Bulk Operations

#### Story 4.1: Send Bulk Reminder Emails
**As an** admin
**I want to** send reminder emails to applicants who haven't responded to interview approval
**So that** I can proactively manage stale applications

**Acceptance Criteria:**
- Given I am in the admin review grid
- When I select multiple applications in "InterviewApproved" status
- And I click "Send Reminder Emails"
- Then I see a confirmation dialog with count of affected applications
- And I can configure the age threshold (default: 7 days since status change)
- And I see a preview list of applications that will receive reminders
- And I click "Send Reminders" to execute
- And the system sends individual emails using "Interview Reminder" template
- And I see a progress indicator during execution
- And I see a results summary (X sent, Y failed)
- And audit log entries are created for each application

**Business Rules:**
- Only InterviewApproved (3) status applications are eligible
- Applications must be older than configured threshold (default: 7 days)
- Exclude applications that received reminder in last 7 days (prevent spam)
- Admin can override threshold for specific selections
- Each email sends individually (no BCC)
- Failed emails don't block other sends

#### Story 4.2: Bulk Status Change to On Hold
**As an** admin
**I want to** change multiple stale applications to "On Hold" status
**So that** I can manage inactive applicants efficiently

**Acceptance Criteria:**
- Given I am in the admin review grid
- When I select multiple applications
- And I click "Change Status to On Hold"
- Then I see a confirmation dialog with count of affected applications
- And I can configure the age threshold (default: 14 days since last status change)
- And I see a preview list of applications that will be affected
- And I can optionally add a bulk admin note
- And I click "Change Status" to execute
- And the system updates each application individually
- And emails are sent to each applicant
- And audit log entries are created
- And I see results summary (X updated, Y failed)

**Business Rules:**
- Only applications in InterviewApproved (3) or InterviewScheduled (5) are eligible
- Applications must be older than configured threshold (default: 14 days)
- Exclude applications already in OnHold (6), Approved (7), or Denied (8)
- Each status change is a separate transaction (partial success possible)
- Failed status changes are reported but don't block others
- Bulk admin note is optional

#### Story 4.3: Configure Bulk Operation Thresholds
**As an** admin
**I want to** configure time thresholds for bulk operations
**So that** I can customize the workflow to my team's needs

**Acceptance Criteria:**
- Given I am in the vetting admin settings
- When I navigate to "Bulk Operation Settings"
- Then I see configurable thresholds for:
  - Reminder email age threshold (default: 7 days)
  - On Hold age threshold (default: 14 days)
  - Reminder email cooldown period (default: 7 days)
- And I can update these values
- And changes take effect immediately
- And changes are logged for audit purposes

**Configuration Defaults:**
- Reminder email threshold: 7 days (applications older than this eligible)
- On Hold threshold: 14 days (applications older than this eligible)
- Reminder cooldown: 7 days (prevent reminder spam)
- All thresholds configurable in admin settings

#### Story 4.4: Monitor Bulk Operation Progress
**As an** admin
**I want to** see real-time progress when executing bulk operations
**So that** I know the operation is working and can monitor for issues

**Acceptance Criteria:**
- Given I execute a bulk operation
- When the operation starts
- Then I see a progress modal with:
  - Progress bar (X of Y complete)
  - Current operation count
  - Estimated time remaining
  - Real-time success/failure counts
- And the modal prevents navigation away during execution
- And I can cancel the operation mid-execution (future enhancement)
- And I see final results summary when complete
- And I can download detailed results (CSV) for record-keeping

**Progress Display:**
- Progress bar: Visual indicator of completion percentage
- Status text: "Processing application 25 of 100..."
- Success count: "Successfully processed: 22"
- Failure count: "Failed: 3"
- Estimated time: "Estimated time remaining: 45 seconds"
- Cancel button: (Future enhancement - not Phase 4)

## Business Rules

### Access Control Rules (Phase 1)

1. **RSVP Access Control**:
   - OnHold (6), Denied (8), Withdrawn (9) members CANNOT RSVP to any events
   - All other statuses can RSVP based on event access level
   - System check at UI level (button disabled/hidden) + server validation
   - Clear status-specific messaging for blocked users

2. **Ticket Purchase Access Control**:
   - OnHold (6), Denied (8), Withdrawn (9) members CANNOT purchase tickets
   - Payment gateway is never contacted for blocked users
   - Server-side validation before PayPal redirect
   - Clear messaging with contact information for OnHold users

3. **Event Access Level Integration**:
   - Access checks combine event access level AND vetting status
   - Most restrictive rule applies
   - Vetted Members Only events require Approved (7) status + IsVetted = true
   - Public events exclude OnHold (6), Denied (8), Withdrawn (9) only

### Email Notification Rules (Phase 2)

1. **Automatic Email Triggers**:
   - Every status change triggers appropriate email template
   - Email sent within 30 seconds of status change
   - Failed email delivery doesn't block status change
   - All delivery attempts logged for audit

2. **Email Template Selection**:
   - Application submission → "Application Received" template
   - Status change to InterviewApproved (3) → "Interview Approved" template
   - Status change to Approved (7) → "Application Approved" template
   - Status change to OnHold (6) → "Application OnHold" template
   - Status change to Denied (8) → "Application Denied" template
   - Bulk reminder operation → "Interview Reminder" template

3. **Email Delivery Logging**:
   - All send attempts logged with timestamp, recipient, template, status
   - Successful deliveries log SendGrid message ID
   - Failed deliveries log error details for troubleshooting
   - Logs retained for 2 years minimum
   - Logs searchable by application ID

### Admin Review Rules (Phase 3)

1. **Status Transition Validation**:
   - Only admins can change application status
   - Only valid transitions allowed (per transition matrix)
   - Invalid transitions not shown as action buttons
   - Server-side validation prevents invalid transitions
   - Denied (8) is final state with no transitions

2. **Admin Notes Management**:
   - Admin notes are OPTIONAL (not required for status changes)
   - Automatic audit notes generated for all status changes
   - Manual notes include admin name and timestamp
   - Notes displayed in chronological timeline
   - Manual notes editable within 1 hour of creation

3. **Audit Trail Requirements**:
   - All status changes create audit log entries
   - Audit logs include: timestamp, admin name, old value, new value, optional note
   - Audit logs are immutable (no updates or deletes)
   - Audit logs retained permanently
   - Audit logs searchable by application ID and admin name

### Bulk Operation Rules (Phase 4)

1. **Reminder Email Criteria**:
   - Only InterviewApproved (3) status applications eligible
   - Applications must be older than configured threshold (default: 7 days)
   - Exclude applications that received reminder in last 7 days
   - Admin can override threshold for specific selections
   - Each email sends individually (no mass BCC)

2. **Bulk Status Change Criteria**:
   - Only InterviewApproved (3) or InterviewScheduled (5) eligible
   - Applications must be older than configured threshold (default: 14 days)
   - Exclude OnHold (6), Approved (7), Denied (8) applications
   - Each status change is separate transaction
   - Failed changes reported but don't block others

3. **Bulk Operation Execution**:
   - Operations process applications individually
   - Partial success is acceptable (some succeed, some fail)
   - Progress updates in real-time
   - Results summary shows success/failure counts
   - Each operation creates individual audit log entries

## Data Structure Requirements

### Phase 1: Access Control (No New Entities)

**Existing Entities Used:**
- `VettingApplication` - Read vetting status for access checks
- `ApplicationUser` - Check IsVetted flag
- `Event` - Event access level for combined checks

**Access Control Response:**
```typescript
interface VettingAccessCheck {
  canRsvp: boolean;
  canPurchaseTickets: boolean;
  vettingStatus: VettingStatus;
  blockReason?: string;
  blockMessage?: string;
}
```

### Phase 2: Email Notification System

**Email Template Entity:**
```csharp
public class VettingEmailTemplate
{
    public Guid Id { get; set; }
    public EmailTemplateType TemplateType { get; set; }  // Enum: ApplicationReceived, InterviewApproved, etc.
    public string Subject { get; set; }  // Max 200 chars
    public string Body { get; set; }  // Rich text with variables
    public bool IsActive { get; set; }  // Default: true
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }  // FK to ApplicationUser
}

public enum EmailTemplateType
{
    ApplicationReceived = 1,
    InterviewApproved = 2,
    ApplicationApproved = 3,
    ApplicationOnHold = 4,
    ApplicationDenied = 5,
    InterviewReminder = 6
}
```

**Email Log Entity:**
```csharp
public class VettingEmailLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }  // FK to VettingApplication
    public string RecipientEmail { get; set; }
    public EmailTemplateType TemplateType { get; set; }
    public DateTime SentAt { get; set; }
    public EmailDeliveryStatus Status { get; set; }  // Enum: Success, Failed, Pending
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public enum EmailDeliveryStatus
{
    Success = 1,
    Failed = 2,
    Pending = 3
}
```

### Phase 3: Admin Review (Uses Existing + Email Entities)

**Audit Log Enhancement (Already Exists):**
```csharp
public class VettingAuditLog  // Existing entity
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }  // FK to VettingApplication
    public string Action { get; set; }  // Description of action
    public Guid PerformedBy { get; set; }  // FK to ApplicationUser
    public DateTime PerformedAt { get; set; }
    public string? OldValue { get; set; }  // Serialized JSON
    public string? NewValue { get; set; }  // Serialized JSON
    public string? Notes { get; set; }  // Optional admin notes
}
```

### Phase 4: Bulk Operations (No New Entities)

**Bulk Operation Configuration:**
```typescript
interface BulkOperationConfig {
  reminderEmailThresholdDays: number;  // Default: 7
  onHoldThresholdDays: number;  // Default: 14
  reminderCooldownDays: number;  // Default: 7
}
```

**Bulk Operation Result:**
```typescript
interface BulkOperationResult {
  totalProcessed: number;
  successCount: number;
  failureCount: number;
  errors: string[];
  processedApplicationIds: string[];
  executionTimeMs: number;
}
```

## Integration Requirements

### Phase 1: Event System Integration

**RSVP System Integration:**
- Add vetting status check to RSVP creation endpoint
- Update RSVP UI to show disabled state for blocked users
- Add status-specific messaging component
- Server-side validation before database insert

**Ticket Purchase Integration:**
- Add vetting status check before PayPal redirect
- Update ticket purchase UI to show disabled state
- Add status-specific messaging with contact info
- Server-side validation before payment processing

### Phase 2: SendGrid Integration

**Service Configuration:**
- SendGrid API key in environment variables
- From email: noreply@witchcityrope.com
- Reply-to: support@witchcityrope.com
- Rate limiting: 100 emails/minute
- Retry logic: 3 attempts, exponential backoff

**Template Variable System:**
- `{{applicant_name}}`: Real name or scene name
- `{{application_number}}`: VET-YYYYMMDD-XXXXX format
- `{{submission_date}}`: Formatted date
- `{{contact_email}}`: support@witchcityrope.com
- `{{status_change_date}}`: Date of status change
- `{{admin_name}}`: Name of admin who made change

### Phase 3: Admin UI Integration

**Navigation Integration:**
- Add "Vetting Admin" menu item (Admin role only)
- Submenu: Review Applications, Email Templates, Settings

**Dashboard Integration:**
- Show vetting application counts by status
- Quick links to pending applications
- Recent activity feed (last 10 status changes)

### Phase 4: Background Job Integration (Future)

**Scheduled Tasks (Future Enhancement):**
- Daily job: Identify applications meeting bulk criteria
- Weekly job: Generate admin summary report
- Monthly job: Cleanup old audit logs (archive, not delete)

## Constraints & Assumptions

### Technical Constraints

**Phase 1:**
- Must integrate with existing event RSVP system
- Must integrate with existing PayPal ticket purchase flow
- Must maintain existing event access level logic
- Response time <100ms for access checks

**Phase 2:**
- SendGrid account required with sufficient sending limits
- Email delivery not guaranteed (network failures possible)
- Template rendering must complete in <2 seconds
- Database must store templates (not SendGrid templates)

**Phase 3:**
- Must follow existing admin UI patterns from events management
- Admin grid must handle 1000+ applications efficiently
- Status transitions must validate at UI and server levels
- Audit trail must be immutable and permanent

**Phase 4:**
- Bulk operations must process applications individually
- Partial success is acceptable (some succeed, some fail)
- Progress updates must be real-time (no polling)
- Maximum 100 applications per bulk operation (performance limit)

### Business Constraints

**All Phases:**
- Admin role required for all review functions
- One application per member lifetime (existing rule)
- Application data retained for 2 years minimum
- Audit trail retained permanently
- GDPR compliance for data export/deletion

**Phase 2:**
- Email templates must be professional and respectful
- No appeal process for denied applications
- OnHold applications direct to support email contact

### Assumptions

**Phase 1:**
- Event system has access level field (exists: ✅)
- RSVP and ticket purchase flows have extension points (to verify)
- Users will understand status-specific messaging
- Performance impact of access checks is negligible

**Phase 2:**
- SendGrid service will be reliable (99.9%+ uptime)
- Email delivery within 30 seconds is acceptable
- Template variable system is sufficient for all use cases
- Admins can edit templates without breaking formatting

**Phase 3:**
- Admins will review applications within 7 days
- Admin notes are optional (automatic audit notes sufficient)
- Status transition matrix is complete (no additional statuses needed)
- 1000+ applications can be displayed in grid with pagination

**Phase 4:**
- Bulk operations on 100 applications is sufficient batch size
- Reminder emails reduce application abandonment
- On Hold status change is appropriate for stale applications
- Real-time progress updates improve admin experience

## Security & Privacy Requirements

### Phase 1: Access Control

**Authentication:**
- All access checks require valid authenticated session
- Vetting status check only for authenticated users
- Anonymous users blocked from all event interactions

**Authorization:**
- Access checks verify both authentication and vetting status
- Server-side validation prevents bypass attempts
- Audit logging for all access denial events

### Phase 2: Email System

**Data Protection:**
- Email addresses encrypted at rest in email logs
- SendGrid API key stored in environment variables (never in code)
- Email content logged but applicant PII redacted in general logs

**Email Security:**
- SPF, DKIM, DMARC configured for noreply@witchcityrope.com
- Unsubscribe links not required (transactional emails only)
- Rate limiting prevents email abuse

### Phase 3: Admin Interface

**Role-Based Access:**
- Only Admin role can access vetting admin section
- Role check at UI level + server-side validation
- Admin actions logged with admin identity

**Data Privacy:**
- Application data shown only to admins
- Audit trail preserves admin accountability
- Manual admin notes may contain sensitive information (encrypted at rest)

**Session Security:**
- Standard session timeout applies (30 minutes)
- Admin actions re-verify authentication
- Sensitive operations require confirmation

### Phase 4: Bulk Operations

**Authorization:**
- Only Admin role can execute bulk operations
- Bulk operations log each individual action
- Preview before execution prevents accidents

**Data Integrity:**
- Each bulk operation is transactional
- Failed operations don't corrupt data
- Partial success is logged and reportable

## Compliance Requirements

### Data Protection

**GDPR Compliance:**
- Applicant data retained for 2 years minimum, longer if required
- Data export available on request (admin can generate)
- Data deletion available (right to be forgotten)
- Audit trail preserved even after data deletion (anonymized)

**Privacy Policy:**
- Vetting application privacy notice updated
- Clear explanation of data usage and retention
- Contact information for privacy inquiries

### Legal Requirements

**Email Marketing Compliance:**
- CAN-SPAM compliance for automated emails
- Transactional emails exempt from unsubscribe requirement
- Clear sender identification in all emails
- Contact information in all email footers

**Record Keeping:**
- All vetting decisions logged permanently
- Audit trail immutable and tamper-proof
- Admin actions traceable to individual admins
- Compliance with community standards documentation

## User Impact Analysis

| User Type | Phase 1 Impact | Phase 2 Impact | Phase 3 Impact | Phase 4 Impact | Priority |
|-----------|----------------|----------------|----------------|----------------|----------|
| **Denied Members** | Blocked from events (High) | Receive denial email (Medium) | N/A | N/A | Critical |
| **OnHold Members** | Blocked from events (High) | Receive OnHold email with contact info (High) | N/A | N/A | Critical |
| **Applicants (Pending)** | No change | Receive status update emails (High) | N/A | Receive reminder emails (Medium) | High |
| **Approved Members** | No change | Receive approval email (High) | N/A | N/A | Medium |
| **Admins** | No change | Reduced manual emails (High) | New review interface (High) | Bulk operations (Medium) | High |
| **Teachers** | No change (may refer applicants) | N/A | N/A | N/A | Low |
| **General Members** | No change | N/A | N/A | N/A | Low |

### Change Management

**Phase 1: Access Control**
- **User Training**: None required (system-enforced)
- **Admin Training**: Brief overview of access control rules
- **Communication**: Announcement of new policy enforcement
- **Support**: FAQ for denied/on-hold members

**Phase 2: Email System**
- **User Training**: None required (automatic emails)
- **Admin Training**: How to edit email templates
- **Communication**: New email notification announcement
- **Support**: Email template customization guide

**Phase 3: Admin Interface**
- **User Training**: None required
- **Admin Training**: Complete training session on review interface
- **Communication**: Admin announcement with training schedule
- **Support**: Admin guide, video walkthrough, Q&A session

**Phase 4: Bulk Operations**
- **User Training**: None required
- **Admin Training**: Bulk operations best practices
- **Communication**: Admin announcement of new capabilities
- **Support**: Bulk operations guide with examples

## Implementation Scenarios

### Phase 1 Scenario: Denied Member Attempts RSVP

1. **User Action**: Sarah (Denied status) clicks RSVP button for "Rope Basics Workshop"
2. **System Check**: Frontend checks vetting status from useVettingStatus hook
3. **UI Response**: RSVP button is disabled with tooltip: "Your vetting application was denied. You cannot RSVP for events at this time."
4. **User Sees**: Clear message explaining access is blocked
5. **Server Validation**: If user somehow submits (API manipulation), server validates and returns 403 Forbidden
6. **Audit Log**: Access denial logged with user ID, event ID, vetting status, timestamp
7. **User Experience**: Sarah understands decision is final, no false hope

### Phase 2 Scenario: Status Change Triggers Email

1. **Admin Action**: Admin changes application status from "UnderReview" to "InterviewApproved"
2. **Status Update**: Database updated, audit log created
3. **Email Trigger**: System detects status change and selects "Interview Approved" template
4. **Template Processing**: Variables replaced: `{{applicant_name}}` → "Sarah Johnson"
5. **Email Send**: SendGrid API called with processed template
6. **Delivery Log**: Success logged with SendGrid message ID
7. **User Receives**: Email within 30 seconds with interview scheduling instructions
8. **Admin Sees**: Success notification, email log entry

### Phase 3 Scenario: Admin Reviews Application

1. **Admin Navigation**: Admin clicks "Vetting Admin" → "Review Applications"
2. **Grid Load**: System queries applications with pagination (25 per page)
3. **Admin Filters**: Clicks "Pending" filter to see only Submitted and UnderReview statuses
4. **Admin Selects**: Clicks on Sarah Johnson's application
5. **Detail View**: Full application details load in <1 second
6. **Admin Reviews**: Reads "Why Join" and "Experience with Rope" responses
7. **Status Change**: Clicks "Approve for Interview" button
8. **Confirmation**: Modal confirms action, admin adds optional note: "Strong background in shibari"
9. **Save**: Status changes to InterviewApproved, email sent, audit log created
10. **Admin Feedback**: Success notification appears

### Phase 4 Scenario: Bulk Reminder Emails

1. **Admin Review**: Admin sees 15 applications in "InterviewApproved" status for >7 days
2. **Bulk Selection**: Admin checks "Select All" in grid (or individually selects)
3. **Bulk Action**: Admin clicks "Send Reminder Emails" button
4. **Configuration**: Modal shows threshold (7 days), list of 15 applications
5. **Confirmation**: Admin clicks "Send Reminders"
6. **Execution**: Progress modal shows: "Sending reminder 5 of 15..."
7. **Results**: Summary shows "14 sent successfully, 1 failed (invalid email)"
8. **Audit Trail**: 15 individual audit log entries created
9. **Email Logs**: 15 email log entries (14 success, 1 failed)
10. **Admin Review**: Downloads results CSV for record-keeping

## Risk Assessment

### High Risk Items

**Phase 1: Access Control**
- **Risk**: RSVP/ticket purchase flows have no extension points for access checks
  - *Mitigation*: Verify integration points early, use middleware pattern if needed
  - *Contingency*: Add access checks at service layer if UI integration not feasible
  - *Timeline Impact*: +3-5 days if major refactoring needed

**Phase 2: Email System**
- **Risk**: SendGrid account not configured or rate limited
  - *Mitigation*: Set up SendGrid account in sprint planning, verify sending limits
  - *Contingency*: Use mock email service for testing, delay production deployment
  - *Timeline Impact*: +5-7 days if account setup delayed

- **Risk**: Email template rendering breaks with complex variables
  - *Mitigation*: Start with simple templates, test variable replacement thoroughly
  - *Contingency*: Use basic string replacement instead of rich templating
  - *Timeline Impact*: +2-3 days if templating system needs simplification

**Phase 3: Admin Interface**
- **Risk**: Admin grid performance poor with 1000+ applications
  - *Mitigation*: Implement pagination, database indexing, caching from start
  - *Contingency*: Reduce page size, add more aggressive filtering
  - *Timeline Impact*: +5-7 days if performance optimization needed

- **Risk**: Status transition validation complexity causes bugs
  - *Mitigation*: TDD approach with comprehensive test coverage of all transitions
  - *Contingency*: Simplify transition matrix if validation becomes too complex
  - *Timeline Impact*: +3-5 days if extensive debugging required

### Medium Risk Items

**Phase 1:**
- **Risk**: Access control messaging confuses users
  - *Mitigation*: User testing with sample messages before deployment
  - *Timeline Impact*: +1-2 days for message refinement

**Phase 2:**
- **Risk**: Email delivery failures not handled gracefully
  - *Mitigation*: Comprehensive error handling and logging from start
  - *Timeline Impact*: +1-2 days for additional error handling

**Phase 3:**
- **Risk**: Admin users don't understand new interface
  - *Mitigation*: Training session, user guide, video walkthrough
  - *Timeline Impact*: +2-3 days for training materials

**Phase 4:**
- **Risk**: Bulk operations accidentally process wrong applications
  - *Mitigation*: Confirmation modal with preview, clear warnings
  - *Timeline Impact*: +1-2 days for enhanced confirmation UX

### Low Risk Items

**All Phases:**
- **Risk**: TypeScript compilation errors during development
  - *Mitigation*: Follow NSwag type generation pattern, strict TypeScript mode
  - *Timeline Impact*: Negligible (normal development)

- **Risk**: Test coverage gaps
  - *Mitigation*: TDD approach, maintain 80%+ coverage requirement
  - *Timeline Impact*: Negligible (tests written first)

## Quality Gates & Success Criteria

### Phase 1: Access Control System

**Functional Requirements (95% Implementation Required):**
- [ ] RSVP access control blocks OnHold (6), Denied (8), Withdrawn (9) statuses
- [ ] Ticket purchase access control blocks same statuses
- [ ] Clear status-specific messaging displayed for blocked users
- [ ] Server-side validation prevents access control bypass
- [ ] Audit logging captures all access denial events
- [ ] Event access level integration works correctly
- [ ] UI responsiveness maintained (<100ms for access checks)

**Testing Requirements:**
- [ ] Unit tests for access control business logic (100% coverage)
- [ ] Integration tests for RSVP and ticket purchase flows
- [ ] E2E tests for all blocked statuses
- [ ] Performance tests verify <100ms response time
- [ ] Security tests verify server-side validation

**Quality Standards:**
- [ ] TypeScript compilation: 0 errors
- [ ] ESLint: 0 violations
- [ ] Test coverage: >80%
- [ ] Performance: <100ms access checks
- [ ] Security: Server-side validation working
- [ ] Accessibility: WCAG 2.1 AA compliant messaging

### Phase 2: Email Notification System

**Functional Requirements (95% Implementation Required):**
- [ ] SendGrid integration configured and operational
- [ ] 6 email templates stored in database
- [ ] Automatic email triggers on all status changes
- [ ] Email delivery within 30 seconds of trigger
- [ ] Email delivery logging with success/failure tracking
- [ ] Template variable replacement working correctly
- [ ] Failed emails don't block status changes
- [ ] Retry logic with exponential backoff

**Testing Requirements:**
- [ ] Unit tests for email service and template rendering
- [ ] Integration tests with SendGrid sandbox
- [ ] E2E tests for each email template type
- [ ] Error handling tests for delivery failures
- [ ] Performance tests verify <30 second delivery

**Quality Standards:**
- [ ] Email delivery success rate: >95%
- [ ] Template rendering: <2 seconds
- [ ] SendGrid integration: Health check passing
- [ ] Error logging: Complete and searchable
- [ ] Retry logic: Working with backoff

### Phase 3: Admin Review Interface

**Functional Requirements (95% Implementation Required):**
- [ ] Admin application review grid with sorting, filtering, search
- [ ] Application detail view with all information
- [ ] Status change interface with validation
- [ ] Admin notes management (optional + automatic)
- [ ] Email template management interface
- [ ] Audit trail display
- [ ] Real-time status updates
- [ ] Pagination for large datasets

**Testing Requirements:**
- [ ] Unit tests for all React components (>80% coverage)
- [ ] Integration tests for data fetching and mutations
- [ ] E2E tests for complete review workflow
- [ ] Performance tests for grid with 1000+ applications
- [ ] Accessibility tests for all components

**Quality Standards:**
- [ ] Grid load time: <2 seconds
- [ ] Detail view load time: <1 second
- [ ] Status update: <1 second
- [ ] Test coverage: >80%
- [ ] Accessibility: WCAG 2.1 AA compliant
- [ ] Mobile responsive: Working on tablets

### Phase 4: Bulk Operations

**Functional Requirements (95% Implementation Required):**
- [ ] Bulk reminder email sending for InterviewApproved applications
- [ ] Bulk status change to OnHold for stale applications
- [ ] Configurable time thresholds
- [ ] Preview of affected applications
- [ ] Progress tracking during execution
- [ ] Results summary with success/failure counts
- [ ] Individual audit log entries for all bulk changes

**Testing Requirements:**
- [ ] Unit tests for bulk operation business logic
- [ ] Integration tests for bulk email sending
- [ ] E2E tests for complete bulk workflows
- [ ] Performance tests for 100 application batches
- [ ] Error handling tests for partial failures

**Quality Standards:**
- [ ] Bulk email send: <10 seconds for 100 applications
- [ ] Bulk status change: <5 seconds for 50 applications
- [ ] Progress updates: Real-time (no lag)
- [ ] Partial success: Handled gracefully
- [ ] Results reporting: Detailed and downloadable

## Implementation Dependencies

### Phase 1: Access Control Dependencies

**Critical Path:**
1. Vetting status system (exists: ✅)
2. Event RSVP system integration points (to verify: ❓)
3. Ticket purchase system integration points (to verify: ❓)
4. API access control endpoint implementation

**Parallel Opportunities:**
- Access control business logic (independent)
- UI messaging components (independent)
- Server-side validation (after API endpoint)
- Audit logging (can use existing audit log entity)

**External Dependencies:**
- None (self-contained implementation)

### Phase 2: Email System Dependencies

**Critical Path:**
1. Phase 1 access control (for testing with real status changes)
2. SendGrid account setup and API key configuration
3. Email template database schema migration
4. Email log database schema migration
5. Template rendering engine implementation
6. SendGrid service integration

**Parallel Opportunities:**
- Email template design (independent)
- Email log entity implementation (independent)
- Template variable system (independent)
- Error handling and retry logic (independent)

**External Dependencies:**
- SendGrid account with sufficient sending limits
- Email authentication (SPF, DKIM, DMARC) for noreply@witchcityrope.com
- Network connectivity to SendGrid API

### Phase 3: Admin Interface Dependencies

**Critical Path:**
1. Phase 1 access control (status changes need access control working)
2. Phase 2 email system (status changes trigger emails)
3. Admin UI framework (React + Mantine, exists: ✅)
4. Admin API endpoints implementation
5. Admin React components implementation
6. Status transition validation implementation

**Parallel Opportunities:**
- Admin grid component (with mock data)
- Application detail component (with mock data)
- Email template management UI (independent)
- Audit trail display component (independent)

**External Dependencies:**
- None (self-contained implementation)

### Phase 4: Bulk Operations Dependencies

**Critical Path:**
1. Phase 3 admin interface (bulk operations UI integrated into admin grid)
2. Bulk operation API endpoints
3. Progress tracking implementation
4. Results summary implementation

**Parallel Opportunities:**
- Bulk operation configuration UI (independent)
- Preview modal component (independent)
- Progress tracking component (independent)
- Results download generation (independent)

**External Dependencies:**
- None (self-contained implementation)

## Questions for Product Manager

### Phase 1: Access Control
- [ ] Should we provide a grace period for OnHold members to access events they already RSVP'd to? (Recommendation: No, immediate block)
- [ ] Should Teachers be able to override access control for specific events? (Recommendation: No, admin-only override)
- [ ] Should we log all access check attempts or only denials? (Recommendation: Denials only for audit trail)

### Phase 2: Email System
- [ ] What should happen if email delivery fails after 3 retries? (Recommendation: Log failure, admin notification)
- [ ] Should we implement email delivery webhooks from SendGrid? (Recommendation: Future enhancement, not Phase 2)
- [ ] Should admins be able to manually resend emails? (Recommendation: Yes, add to Phase 3)

### Phase 3: Admin Interface
- [ ] Should we allow admins to delete applications? (Recommendation: No, only archive/withdraw)
- [ ] Should status changes require a confirmation for all transitions or only Denied? (Recommendation: All transitions require confirmation)
- [ ] Should we implement role-based admin permissions (e.g., Reviewer vs Administrator)? (Recommendation: Future enhancement, single Admin role for Phase 3)

### Phase 4: Bulk Operations
- [ ] What is the maximum batch size for bulk operations? (Recommendation: 100 applications)
- [ ] Should we allow cancellation of in-progress bulk operations? (Recommendation: Future enhancement, not Phase 4)
- [ ] Should bulk operations send a summary email to the admin who executed them? (Recommendation: Yes, good practice)

### General Questions
- [ ] Should we implement a staging environment for testing before production deployment? (Recommendation: Yes, critical for email testing)
- [ ] What is the rollback plan if a phase deployment causes issues? (Recommendation: Feature flags for each phase)
- [ ] Should we conduct user acceptance testing with real admins before production? (Recommendation: Yes, especially for Phase 3)

## Success Validation Plan

### Phase 1 Validation (Week 1-2)

**Development Milestones:**
- [ ] Day 3: Access control business logic implemented and tested
- [ ] Day 5: RSVP integration complete with UI messaging
- [ ] Day 7: Ticket purchase integration complete
- [ ] Day 10: E2E testing complete
- [ ] Day 12: Code review and bug fixes
- [ ] Day 14: Deployment to staging, UAT

**Validation Criteria:**
- [ ] All 3 blocked statuses (OnHold, Denied, Withdrawn) prevent RSVP
- [ ] All 3 blocked statuses prevent ticket purchases
- [ ] Status-specific messaging displays correctly
- [ ] Server-side validation prevents bypass
- [ ] Performance <100ms for all access checks
- [ ] 100% test coverage for access control logic

### Phase 2 Validation (Week 3-5)

**Development Milestones:**
- [ ] Day 3: SendGrid integration configured and tested
- [ ] Day 7: Email template database and service implemented
- [ ] Day 10: Template rendering and variable replacement working
- [ ] Day 14: All 6 email templates tested in sandbox
- [ ] Day 17: Error handling and retry logic complete
- [ ] Day 21: Deployment to staging, UAT

**Validation Criteria:**
- [ ] All 6 email templates send successfully in sandbox
- [ ] Emails deliver within 30 seconds of trigger
- [ ] Template variables replace correctly
- [ ] Failed deliveries log properly
- [ ] Retry logic works with exponential backoff
- [ ] 95%+ email delivery success rate

### Phase 3 Validation (Week 6-9)

**Development Milestones:**
- [ ] Day 5: Admin grid component implemented with mock data
- [ ] Day 10: Application detail view implemented
- [ ] Day 15: Status change workflow implemented
- [ ] Day 20: Email template management interface implemented
- [ ] Day 25: Admin notes and audit trail complete
- [ ] Day 28: E2E testing complete
- [ ] Day 30: Deployment to staging, UAT

**Validation Criteria:**
- [ ] Admin grid loads <2 seconds with 1000+ applications
- [ ] All filtering, sorting, search working correctly
- [ ] Status changes trigger emails and create audit logs
- [ ] Admin notes (optional + automatic) display properly
- [ ] Email templates editable and save correctly
- [ ] All status transitions validate correctly

### Phase 4 Validation (Week 10-11)

**Development Milestones:**
- [ ] Day 3: Bulk reminder email implemented and tested
- [ ] Day 5: Bulk status change implemented and tested
- [ ] Day 7: Configuration interface implemented
- [ ] Day 10: Progress tracking and results summary complete
- [ ] Day 12: E2E testing complete
- [ ] Day 14: Deployment to staging, UAT

**Validation Criteria:**
- [ ] Bulk reminder emails send to correct applications
- [ ] Bulk status changes update correctly
- [ ] Progress tracking updates in real-time
- [ ] Partial success handled gracefully
- [ ] Results summary accurate and downloadable
- [ ] Performance <10 seconds for 100 applications

## Post-Implementation Considerations

### Monitoring & Alerting

**Phase 1: Access Control**
- Monitor access denial rate by vetting status
- Alert on unusual access denial patterns
- Track RSVP/ticket purchase conversion rates by status

**Phase 2: Email System**
- Monitor email delivery success rate (alert if <95%)
- Track email bounce rates and invalid addresses
- Alert on SendGrid API errors or rate limiting
- Monitor email delivery latency (alert if >60 seconds)

**Phase 3: Admin Interface**
- Monitor admin review processing times
- Track status change frequency and patterns
- Alert on long-pending applications (>14 days)
- Monitor admin grid performance

**Phase 4: Bulk Operations**
- Track bulk operation execution frequency
- Monitor bulk operation success rates
- Alert on bulk operation failures
- Track reminder email effectiveness (application progress after reminder)

### Maintenance Requirements

**Weekly Tasks:**
- Review email delivery logs for failures
- Check for applications pending >14 days
- Monitor admin grid performance metrics

**Monthly Tasks:**
- Review email template effectiveness (open rates, bounce rates)
- Analyze vetting pipeline bottlenecks
- Update email templates based on feedback
- Review bulk operation usage and effectiveness

**Quarterly Tasks:**
- Audit trail compliance review
- Database performance optimization
- Email deliverability audit (SPF, DKIM, DMARC)
- Admin training refresh sessions

### Future Enhancement Opportunities

**Access Control Enhancements:**
- Temporary access override for special events
- Graduated access levels (e.g., "Provisional Access" status)
- Integration with payment system for member-only pricing

**Email System Enhancements:**
- Email delivery webhooks for real-time status
- SMS notifications for critical status changes
- Email preference management (frequency, content)
- A/B testing for email template effectiveness

**Admin Interface Enhancements:**
- Analytics dashboard for vetting pipeline
- Automated status transitions based on rules
- Bulk application export (CSV, PDF)
- Advanced search with saved filters
- Integration with calendar systems for interview scheduling

**Bulk Operations Enhancements:**
- Scheduled bulk operations (e.g., every Monday)
- Bulk operation templates (saved configurations)
- Bulk operation history and analytics
- Cancel in-progress bulk operations

**New Features (Not in Current Plan):**
- Member referral system for vetting
- Anonymous feedback for application process
- Public-facing "Check Application Status" page (with token)
- Integration with community events calendar
- Vetting committee voting system (multi-admin approval)

## Timeline Summary

| Phase | Duration | Start Dependencies | Key Deliverables | Risk Level |
|-------|----------|-------------------|------------------|------------|
| **Phase 1** | 1-2 weeks | Vetting status system ✅ | Access control for RSVP and tickets, clear messaging, audit logging | Low |
| **Phase 2** | 2-3 weeks | Phase 1, SendGrid account | 6 email templates, automatic triggers, delivery logging, SendGrid integration | Medium |
| **Phase 3** | 3-4 weeks | Phase 1 + 2 | Admin grid, detail view, status changes, notes, template management, audit trail | Medium |
| **Phase 4** | 1-2 weeks | Phase 3 | Bulk reminder emails, bulk status changes, progress tracking, results summary | Low |
| **Total** | **7-11 weeks** | Sequential implementation | Complete vetting workflow system | Medium |

### Recommended Schedule

**Conservative Estimate (11 weeks):**
- Week 1-2: Phase 1 (Access Control)
- Week 3-5: Phase 2 (Email System)
- Week 6-9: Phase 3 (Admin Interface)
- Week 10-11: Phase 4 (Bulk Operations)

**Aggressive Estimate (7 weeks - with parallel work):**
- Week 1-2: Phase 1 (Access Control) + Phase 2 setup (SendGrid, database)
- Week 2-4: Phase 2 (Email System) - started in parallel with Phase 1
- Week 5-7: Phase 3 (Admin Interface)
- Week 8: Phase 4 (Bulk Operations) - may overlap with Phase 3 completion

**Recommended Approach:**
- Start with conservative 11-week timeline
- Re-evaluate after Phase 1 completion
- Adjust based on actual velocity and resource availability
- Maintain quality over speed (TDD, comprehensive testing)

## Estimated Complexity

### Phase 1: Access Control - **MEDIUM COMPLEXITY** ⭐⭐⭐

**Why Medium:**
- Integration with existing RSVP and ticket purchase systems
- UI messaging components need design consistency
- Server-side validation must prevent bypass
- Audit logging integration

**Complexity Breakdown:**
- Business logic: LOW (simple status checks)
- Integration: MEDIUM (RSVP and ticket flows)
- UI: LOW (messaging components)
- Testing: MEDIUM (multiple integration points)
- Risk: LOW (isolated feature)

**Estimation:**
- Backend: 3-4 days
- Frontend: 3-4 days
- Testing: 2-3 days
- Total: **8-11 days**

### Phase 2: Email System - **MEDIUM-HIGH COMPLEXITY** ⭐⭐⭐⭐

**Why Medium-High:**
- External service integration (SendGrid)
- Template rendering with variable replacement
- Email delivery logging and retry logic
- Database schema for templates and logs
- Error handling for network failures

**Complexity Breakdown:**
- Business logic: MEDIUM (template rendering, triggers)
- Integration: HIGH (SendGrid API, webhooks future)
- UI: LOW (no user-facing UI in Phase 2)
- Testing: HIGH (sandbox testing, error scenarios)
- Risk: MEDIUM (external dependency)

**Estimation:**
- Backend: 7-10 days
- Database: 2-3 days
- Testing: 3-4 days
- Total: **12-17 days**

### Phase 3: Admin Interface - **HIGH COMPLEXITY** ⭐⭐⭐⭐⭐

**Why High:**
- Multiple complex React components
- TanStack Query data fetching and caching
- State management for grid, filters, selection
- Status transition validation
- Email template rich text editor
- Audit trail display
- Real-time updates
- Performance optimization for large datasets

**Complexity Breakdown:**
- Business logic: MEDIUM (status transitions, validation)
- Integration: MEDIUM (API endpoints, email system)
- UI: HIGH (grid, detail view, template editor, notes)
- Testing: HIGH (component tests, E2E workflows)
- Risk: MEDIUM (UI complexity, performance)

**Estimation:**
- Backend: 7-10 days
- Frontend: 12-15 days
- Testing: 5-7 days
- Total: **24-32 days**

### Phase 4: Bulk Operations - **MEDIUM COMPLEXITY** ⭐⭐⭐

**Why Medium:**
- Builds on existing admin interface (Phase 3)
- Bulk processing logic is straightforward
- Progress tracking requires real-time updates
- Results summary and download

**Complexity Breakdown:**
- Business logic: MEDIUM (bulk processing, filters)
- Integration: LOW (uses existing APIs)
- UI: MEDIUM (progress modal, configuration)
- Testing: MEDIUM (bulk scenarios, partial failures)
- Risk: LOW (optimization of existing patterns)

**Estimation:**
- Backend: 3-4 days
- Frontend: 4-5 days
- Testing: 2-3 days
- Total: **9-12 days**

## Critical Dependencies & Recommended Order

### Why Phase 1 First (Access Control) ⭐ **HIGHEST PRIORITY**

**Critical Safety Requirement:**
- Prevents denied/on-hold members from accessing events they shouldn't
- Users are actively requesting this feature
- Safety risk exists today without this control

**Business Value:**
- Immediate safety improvement
- Policy enforcement automation
- Reduces manual check-in verification
- Clear boundaries for community membership

**Technical Benefits:**
- Relatively isolated implementation
- Few dependencies (vetting status system already exists)
- Quick win builds momentum
- Can be deployed independently

**Risks Mitigated:**
- Low risk of implementation issues
- No external service dependencies
- Performance impact is minimal (<100ms)
- Can rollback easily if needed

**Recommendation:** ✅ **START HERE** - Critical for community safety and policy enforcement

### Why Phase 2 Second (Email System)

**Communication Gap:**
- Applicants currently have no visibility into status changes
- Admins manually composing emails for every status change
- Poor applicant experience without timely updates

**Business Value:**
- 80% reduction in manual admin email composition
- Timely, professional communication
- Improved applicant satisfaction
- Standardized messaging

**Technical Dependencies:**
- Requires Phase 1 for complete testing (status changes need access control working)
- SendGrid account setup can happen in parallel
- Can be developed in parallel with Phase 1 if resources available

**Risks:**
- Medium risk due to external service dependency (SendGrid)
- Email delivery not guaranteed (network failures)
- Template rendering complexity

**Recommendation:** ✅ **SECOND PRIORITY** - Critical for communication but can wait for Phase 1 completion

### Why Phase 3 Third (Admin Interface)

**Workflow Completion:**
- Requires both access control (Phase 1) and email system (Phase 2) to be complete
- Status changes trigger emails (Phase 2 dependency)
- Access control needs to be working for full workflow testing (Phase 1 dependency)

**Business Value:**
- Centralized application review process
- Comprehensive audit trail
- Standardized workflow
- Admin efficiency

**Technical Dependencies:**
- **Requires Phase 1**: Status changes affect access control
- **Requires Phase 2**: Status changes trigger emails
- Benefits from lessons learned in previous phases
- Can leverage existing admin UI patterns

**Risks:**
- Medium risk due to UI complexity
- State management for grid and filters
- Performance optimization for large datasets
- Status transition validation

**Recommendation:** ✅ **THIRD PRIORITY** - Most complex, benefits from previous phases

### Why Phase 4 Fourth (Bulk Operations)

**Optimization Feature:**
- Not critical for core workflow (nice-to-have)
- Can be deferred if timeline pressures exist
- Builds on established patterns from Phase 3

**Business Value:**
- Administrative efficiency (50% reduction in repetitive tasks)
- Proactive communication (reminder emails)
- Pipeline management (bulk status changes)

**Technical Dependencies:**
- **Requires Phase 3**: Bulk operations integrated into admin interface
- **Requires Phase 2**: Bulk emails use email system
- Relatively straightforward given Phase 3 foundation

**Risks:**
- Low risk (builds on proven patterns)
- Performance considerations for bulk processing
- User error prevention (confirmation modals)

**Recommendation:** ✅ **FOURTH PRIORITY** - Optimization feature, can be deferred if needed

## Final Recommendations

### Immediate Actions (This Sprint)

1. **Start Phase 1 (Access Control)** ⭐ **TOP PRIORITY**
   - Begin access control business logic implementation
   - Verify RSVP and ticket purchase integration points
   - Design status-specific messaging components
   - Target: 2-week completion

2. **Set Up Phase 2 Dependencies**
   - Create SendGrid account and obtain API key
   - Configure email authentication (SPF, DKIM, DMARC)
   - Set up noreply@witchcityrope.com email address
   - Verify sending limits sufficient for production

3. **Plan Phase 3 Architecture**
   - Review events management admin UI patterns
   - Design admin grid component structure
   - Plan state management approach (TanStack Query + Zustand)
   - Create wireframes for admin interface

### Success Factors

**Quality Over Speed:**
- Maintain TDD approach for all phases
- 80%+ test coverage requirement
- Comprehensive E2E testing before deployment
- Code review for all implementations

**User-Centered Design:**
- Clear, respectful messaging for blocked users
- Admin training for new interfaces
- User acceptance testing with real admins
- Iterative refinement based on feedback

**Phased Deployment:**
- Deploy each phase independently
- Use feature flags for gradual rollout
- Monitor metrics closely after each deployment
- Rollback plan for each phase

**Communication:**
- Regular stakeholder updates on progress
- Early warning on risks or delays
- Celebrate milestones (each phase completion)
- Document lessons learned

---

**Document Status**: Complete - Ready for stakeholder review and planning approval
**Next Steps**:
1. Stakeholder review and approval
2. Begin Phase 1 implementation
3. Create detailed technical specifications for each phase
4. Set up project tracking and milestones

**Recommended Start Date**: Within 1 week of approval
**Estimated Completion Date**: 7-11 weeks from start (conservative estimate)
**Critical Success Factor**: Complete Phase 1 (Access Control) within 2 weeks to address safety concerns
