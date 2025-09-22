# Business Requirements: WitchCityRope Vetting System Implementation
<!-- Last Updated: 2025-09-22 -->
<!-- Version: 2.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Updated Based on User Feedback -->

## Executive Summary

The WitchCityRope Vetting System enables community members to apply for vetted member status, providing access to advanced workshops and private events. This system streamlines the application process while maintaining the community's high safety and trust standards. The implementation builds upon existing API infrastructure (September 13, 2025) and wireframe designs to deliver a complete end-to-end vetting workflow.

## Business Context

### Problem Statement

Currently, the vetting process for WitchCityRope members is manual and inconsistent, creating barriers for legitimate applicants and administrative burden for staff. The lack of a structured digital application process results in:

- Lost applications and inconsistent communication
- No clear status tracking for applicants
- Administrative burden on staff for basic communication
- Inconsistent vetting standards and decision tracking
- Poor applicant experience with no visibility into process

### Business Value

- **Improved Member Experience**: Clear application process with status tracking
- **Administrative Efficiency**: Automated communications and structured review process
- **Process Consistency**: Standardized vetting workflow with audit trail
- **Community Growth**: Reduced friction for quality members to join vetted tier
- **Safety Enhancement**: Better documentation and tracking of vetting decisions

### Success Metrics

- **Application Completion Rate**: >85% of started applications completed
- **Review Processing Time**: <7 days average from submission to decision
- **Admin Efficiency**: 50% reduction in manual communication tasks
- **Member Satisfaction**: >4.0/5.0 rating for application experience
- **Decision Quality**: <5% appeal/reversal rate on vetting decisions

## Current State Analysis

### Existing Implementation (September 13, 2025)

Based on test-executor handoff, the following components exist:

**✅ API Implementation**:
- Core vetting entities and domain logic (98.1% test pass rate)
- Database schema for vetting applications
- Business rules for vetting workflow validated

**✅ Design Assets**:
- Complete wireframes for application form
- User interface flow documented
- Admin review screen layouts defined

**❌ Integration Gaps**:
- Database schema sync issues blocking integration tests
- React components not fully implemented
- Email notification system not connected
- Admin review screens not implemented

### Gap Analysis

| Component | Current State | Required State | Gap |
|-----------|---------------|----------------|-----|
| **Application Form** | Wireframe only | Complete React component | High |
| **Email System** | Not implemented | SendGrid integration with templates | High |
| **Admin Review** | Design only | Full admin interface | High |
| **User Dashboard Status** | Not implemented | Status display in user dashboard | Medium |
| **Database Schema** | Out of sync | Migrated and validated | Critical |
| **Authentication** | Partial integration | Required before application | Medium |

## User Stories

### Story 1: Member Submits Vetting Application
**As a** General Member
**I want to** submit a vetting application
**So that** I can gain access to advanced workshops and community events

**Acceptance Criteria:**
- Given I am logged in as a General Member
- When I navigate to the vetting application page
- Then I see a clear application form with all required fields
- And I can complete and submit the application in one session
- And I receive email confirmation immediately after submission
- And I see a confirmation page similar to ticket purchase confirmation
- And after submission, I see application status on my dashboard
- And if I visit the application page again after submitting, I see current status and cannot resubmit

**Business Rules:**
- User must have active account before applying
- Only one application per member allowed
- No draft functionality - application submitted in one session
- Application creates audit trail entry
- Email confirmation sent via SendGrid automatically
- After submission, user cannot create new application (one application per member)

### Story 2: Admin Reviews Vetting Application
**As an** Admin
**I want to** review and process vetting applications
**So that** I can maintain community standards and approve qualified members

**Acceptance Criteria:**
- Given I am logged in as an Admin
- When I access the vetting review screen
- Then I see a grid of all pending applications
- And I can click any row to view full application details
- And I can add optional notes and change application status
- And status changes trigger appropriate email notifications
- And all actions create audit trail entries
- And I can perform bulk operations on multiple applications

**Business Rules:**
- Only Admins can access review functionality
- Admin notes are OPTIONAL - manual notes are not required
- Automatic notes generated for status changes (who made change, what changed, timestamp)
- Notes visible in membership admin area after approval
- Bulk operations available for efficiency

### Story 3: Member Tracks Application Progress
**As a** General Member
**I want to** see my application status on my dashboard
**So that** I know where my application stands in the review process

**Acceptance Criteria:**
- Given I have submitted a vetting application
- When I view my user dashboard
- Then I see current application stage prominently displayed
- And I see any applicable next steps or requirements
- And I receive email notifications for status changes
- And if I visit the application page after submitting, I see current status instead of form

**Business Rules:**
- Application status integrated into user dashboard (no separate status page)
- Status display prevents confusion about application state
- Clear indication of next steps for each stage

### Story 4: Admin Manages Email Templates in Vetting Section
**As an** Admin
**I want to** customize email templates within the vetting administration area
**So that** I can maintain professional and consistent messaging

**Acceptance Criteria:**
- Given I am logged in as an Admin
- When I access the vetting admin section
- Then I see email template management as part of the vetting workflow
- And I can edit templates for all vetting email types
- And changes take effect immediately for new notifications
- And I can preview templates before saving

**Business Rules:**
- Email templates managed within vetting functional area, NOT generic email management
- Templates co-located with their functional purpose for admin efficiency

**Email Template Types Required:**
1. Application received (initial thank you)
2. Interview Approved (ready to schedule but not yet scheduled)
3. Application approved email
4. Application on hold email
5. Application denied email
6. Interview Reminder (for old interview-approved applications)

### Story 5: System Sends Automated Notifications
**As the** System
**I want to** send automated email notifications
**So that** applicants stay informed throughout the process

**Acceptance Criteria:**
- Given an application status changes
- When the change is saved
- Then appropriate email template is selected
- And email is sent via SendGrid
- And delivery is logged for audit purposes

### Story 6: Admin Performs Bulk Operations
**As an** Admin
**I want to** perform bulk operations on applications
**So that** I can efficiently manage the review process

**Acceptance Criteria:**
- Given I am in the admin review grid
- When I select multiple applications using checkboxes
- Then I can send reminder emails to interview-approved applications older than configurable days
- And I can bulk change status to "On Hold" for applications older than configurable days
- And bulk operations exclude already approved/denied/on-hold applications
- And all bulk actions create appropriate audit trail entries

**Business Rules:**
- Reminder emails only sent to applications in "Interview Approved" status
- Bulk status change to "On Hold" only for applications that haven't responded to interview approval
- Time thresholds configurable by admin
- Bulk operations maintain data integrity and audit trails

## Business Rules

### Application Process Rules
1. **Authentication Required**: User must create account and login before accessing application form
2. **Single Application**: Only one application per member allowed - no resubmission
3. **Single Session Submission**: No draft functionality - complete application in one session
4. **Immediate Confirmation**: System sends email confirmation upon successful submission
5. **Audit Trail**: All application events logged with timestamp and user information
6. **Dashboard Integration**: Application status displayed on user dashboard, not separate page

### Review Process Rules
1. **Admin-Only Access**: Only users with Admin role can access review functionality
2. **Optional Notes**: Admin notes are optional - automatic notes sufficient for audit trail
3. **Automatic Documentation**: System generates automatic notes for who made changes and when
4. **Email Notifications**: Status changes trigger appropriate email templates
5. **Bulk Operations**: Admins can perform bulk operations for efficiency

### Status Workflow Rules
1. **Application Stages**: Submitted → Under Review → Interview Approved → Interview Scheduled → Interview Completed → Final Decision (Approved/Denied/On Hold)
2. **Email Triggers**: Each status change sends appropriate template to applicant
3. **Reversible Decisions**: Approved members can be changed to other statuses if needed
4. **Historical Tracking**: All status changes preserved in audit log
5. **On Hold Communication**: On Hold emails direct users to contact support@witchcityrope.com to reactivate

### Integration Rules
1. **SendGrid Integration**: All emails sent through SendGrid service
2. **Template Management**: Admin-editable templates within vetting section for all email types
3. **Database Consistency**: All changes use database transactions for consistency
4. **Authentication Integration**: Leverage existing authentication system

## Data Structure Requirements

### Vetting Application Data
- **applicantId**: Guid (required, foreign key to User)
- **submittedAt**: DateTime (required, ISO 8601 format)
- **status**: VettingStatus (required, enum: Submitted, UnderReview, InterviewApproved, InterviewScheduled, Approved, Denied, OnHold)
- **adminNotes**: string (optional, rich text support)
- **sceneName**: string (required, 3-50 characters)
- **realName**: string (required, 3-100 characters)
- **email**: string (required, valid email format)
- **phoneNumber**: string (optional, formatted)
- **experience**: string (required, 500-2000 characters)
- **safetyTraining**: string (required, 200-1000 characters)
- **additionalInfo**: string (optional, max 1000 characters)

**Removed Fields** (per wireframe simplification):
- emergencyContact (removed)
- references (removed)
- fileUpload capability (removed)

### Email Template Data
- **templateId**: Guid (required, primary key)
- **templateType**: EmailTemplateType (required, enum)
- **subject**: string (required, max 200 characters)
- **body**: string (required, rich text with variables)
- **isActive**: boolean (required, default: true)
- **createdAt**: DateTime (required)
- **updatedAt**: DateTime (required)
- **updatedBy**: Guid (required, foreign key to User)

### Audit Log Data
- **logId**: Guid (required, primary key)
- **applicationId**: Guid (required, foreign key)
- **action**: string (required, action description)
- **performedBy**: Guid (required, foreign key to User)
- **performedAt**: DateTime (required)
- **oldValue**: string (optional, serialized)
- **newValue**: string (optional, serialized)
- **notes**: string (optional)

### Bulk Operation Configuration Data
- **reminderEmailThresholdDays**: int (required, default: 7)
- **onHoldThresholdDays**: int (required, default: 14)
- **lastReminderSent**: DateTime (optional, per application)

## Constraints & Assumptions

### Constraints
- **Technical**: Must use existing authentication system
- **Technical**: SendGrid integration required for email delivery
- **Technical**: Must follow NSwag auto-generation for TypeScript types
- **Business**: Admin role required for all review functions
- **Business**: No offline capability - must be online to submit
- **Business**: One application per member lifetime (no resubmission)

### Assumptions
- **User Behavior**: Members will complete application in single session
- **Admin Availability**: Admins will review applications within 7 days
- **Technical Infrastructure**: Database migrations will be fixed before implementation
- **Email Delivery**: SendGrid service will be available and configured
- **Calendar Integration**: Future consideration for Google Calendar integration

## Security & Privacy Requirements

### Authentication Requirements
- **Pre-Authentication**: Must be logged in to access application form
- **Role-Based Access**: Admin role required for review functions
- **Session Management**: Standard session timeout applies

### Data Protection Requirements
- **PII Handling**: Real names and contact information encrypted at rest
- **Access Logging**: All admin access to applications logged
- **Data Retention**: Applications retained for 2 years minimum
- **Privacy**: Vetting status visible only to applicant and admins

### Safety Considerations
- **Community Standards**: Maintain high safety and trust standards
- **Professional Communication**: All system communications maintain professional tone

## Compliance Requirements

### Platform Policies
- **Community Standards**: Vetting criteria align with community values
- **Non-Discrimination**: Fair and consistent review process
- **Transparency**: Clear communication of requirements and process

### Legal Requirements
- **Data Protection**: Comply with applicable privacy laws
- **Email Marketing**: CAN-SPAM compliance for automated emails
- **Record Keeping**: Maintain audit trail for decisions

## User Impact Analysis

| User Type | Impact | Priority | Change Required |
|-----------|--------|----------|----------------|
| **General Members** | New vetting application process | High | Learn new application form |
| **Vetted Members** | No direct impact | Low | Awareness of process |
| **Teachers** | No direct impact | Low | May refer students to process |
| **Admins** | New review and management duties | High | Training on review interface |
| **Guests** | Indirect motivation to register | Medium | Awareness of path to membership |

## Implementation Scenarios

### Scenario 1: Successful Application (Happy Path)
1. **Member Login**: Existing member logs into platform
2. **Navigate to Application**: Clicks "Apply for Vetting" link
3. **Complete Form**: Fills out comprehensive application form
4. **Submit Application**: Clicks submit and sees confirmation page
5. **Email Confirmation**: Receives "Application Received" email immediately
6. **Dashboard Status**: Sees application status on dashboard
7. **Admin Review**: Admin sees application in review grid
8. **Status Update**: Admin updates status to "Interview Approved" with optional notes
9. **Email Notification**: Member receives "Interview Approved" email
10. **Interview Scheduled**: Admin updates to "Interview Scheduled"
11. **Final Decision**: Admin approves application
12. **Approval Email**: Member receives approval email and gains vetted status

### Scenario 2: Bulk Reminder Operation
1. **Admin Review**: Admin accesses vetting admin section
2. **Identify Old Applications**: Sees applications in "Interview Approved" status older than threshold
3. **Select Applications**: Uses checkboxes to select multiple applications
4. **Send Reminders**: Clicks "Send Reminder Emails" bulk operation
5. **Email Delivery**: System sends reminder emails using template
6. **Audit Trail**: All reminder actions logged for each application

### Scenario 3: Application On Hold Process
1. **Application Review**: Admin reviews application and needs more information
2. **Status Change**: Admin sets status to "On Hold" with optional notes about requirements
3. **Email Notification**: Member receives "Application On Hold" email directing them to contact support@witchcityrope.com
4. **Member Contact**: Member contacts support to provide additional information
5. **Admin Update**: Admin manually updates application based on member contact
6. **Process Continuation**: Application moves back to "Under Review" for final decision

## Email Template Requirements

### Template Types and Content

#### 1. Application Received Template
- **Trigger**: Immediately after successful application submission
- **Purpose**: Confirm receipt and set expectations
- **Required Variables**: {applicantName}, {submissionDate}, {applicationId}
- **Content Requirements**: Thank you message, next steps, estimated timeline

#### 2. Interview Approved Template (NEW)
- **Trigger**: When admin changes status to "Interview Approved"
- **Purpose**: Notify applicant they can schedule interview but haven't yet
- **Required Variables**: {applicantName}, {approvalDate}, {contactInfo}
- **Content Requirements**: Interview approval notice, how to schedule, contact information

#### 3. Application Approved Template
- **Trigger**: When admin changes status to "Approved"
- **Purpose**: Welcome new vetted member
- **Required Variables**: {applicantName}, {approvalDate}, {membershipBenefits}
- **Content Requirements**: Congratulations, benefits explanation, next steps

#### 4. Application On Hold Template
- **Trigger**: When admin changes status to "On Hold"
- **Purpose**: Request additional information or explain delay
- **Required Variables**: {applicantName}, {contactEmail}
- **Content Requirements**: Status explanation, contact support@witchcityrope.com to reactivate

#### 5. Application Denied Template
- **Trigger**: When admin changes status to "Denied"
- **Purpose**: Communicate decision professionally
- **Required Variables**: {applicantName}, {decisionDate}
- **Content Requirements**: Professional denial message, encouragement for future growth
- **Note**: NO appeal process - decisions are final

#### 6. Interview Reminder Template (NEW)
- **Trigger**: Bulk operation for old interview-approved applications
- **Purpose**: Remind applicants to schedule their approved interview
- **Required Variables**: {applicantName}, {originalApprovalDate}, {contactInfo}
- **Content Requirements**: Gentle reminder, how to schedule, contact information

## Technical Integration Points

### SendGrid Integration
- **Service Configuration**: API key configuration in application settings
- **Template Storage**: Templates stored in database, not SendGrid
- **Delivery Tracking**: Log email delivery status for audit
- **Error Handling**: Graceful handling of email delivery failures

### Authentication System Integration
- **Role Checking**: Verify Admin role for review access
- **User Context**: Populate application with logged-in user information
- **Session Management**: Standard session handling applies

### Database Integration
- **Migration Requirements**: Sync existing schema with EF Core model
- **Transaction Consistency**: Use database transactions for status changes
- **Audit Logging**: All changes logged with user and timestamp
- **Foreign Key Integrity**: Maintain referential integrity with User table

### API Endpoint Requirements
- **Application Submission**: POST /api/vetting/applications
- **Application Status**: GET /api/vetting/applications/my-status
- **Admin Review List**: GET /api/admin/vetting/applications
- **Admin Status Update**: PUT /api/admin/vetting/applications/{id}/status
- **Template Management**: CRUD operations for email templates in vetting admin
- **Bulk Operations**: POST /api/admin/vetting/applications/bulk-reminder
- **Bulk Status Change**: POST /api/admin/vetting/applications/bulk-status-change

## Quality Gates & Success Criteria

### Functional Requirements (95% Implementation Required)
- [ ] Application form captures all required data fields (simplified per wireframe)
- [ ] Email confirmation sent immediately after submission
- [ ] Application status displays on user dashboard
- [ ] Admin review grid displays all applications with status
- [ ] Status changes trigger appropriate email notifications
- [ ] Optional notes system with automatic audit trail
- [ ] All email templates editable within vetting admin section
- [ ] Bulk operations for reminder emails and status changes
- [ ] Authentication properly integrated
- [ ] Database schema synchronized and tested
- [ ] One application per member enforcement

### Performance Requirements
- **Application Submission**: <3 seconds response time
- **Email Delivery**: <30 seconds from trigger to SendGrid
- **Admin Review Load**: <2 seconds to display application grid
- **Status Updates**: <1 second for status change operations
- **Bulk Operations**: <10 seconds for bulk email sends

### Security Requirements
- **Access Control**: 100% compliance with role-based restrictions
- **Data Encryption**: PII encrypted at rest in database
- **Audit Logging**: 100% of admin actions logged
- **Session Security**: Standard platform session management

### User Experience Requirements
- **Mobile Compatibility**: Application form works on mobile devices
- **Accessibility**: WCAG 2.1 AA compliance for forms
- **Error Handling**: Clear error messages for validation failures
- **Progress Indication**: Clear status indicators throughout process

## Features Explicitly Removed

The following features were removed based on user feedback to keep the implementation focused:

### Removed Features
- **Appeal Process**: No appeals for denied applications - decisions are final
- **Anonymous Feedback System**: Not required for MVP
- **Membership Cards**: Physical or digital cards not included
- **References System**: Removed from application form
- **Emergency Contacts**: Removed from application form
- **File Upload Capability**: Not included in simplified form
- **Additional Information Requests**: Handled during interview process
- **Separate Application Status Page**: Status integrated into user dashboard

### Future Considerations
- **Calendar Integration**: Google Calendar integration noted for future development
- **Advanced Reporting**: Detailed analytics on vetting process
- **Member Referral System**: Allow vetted members to refer new applicants

## Risk Assessment

### High Risk
- **Database Migration Failure**: Could block entire implementation
  - *Mitigation*: Prioritize schema sync, backup before migration
- **SendGrid Integration Issues**: Could break notification system
  - *Mitigation*: Implement fallback email logging, thorough testing

### Medium Risk
- **Admin Training Requirements**: Admins need training on new interface including bulk operations
  - *Mitigation*: Create admin guide, provide training session
- **User Adoption**: Members may not understand new process
  - *Mitigation*: Clear documentation, announcement communications

### Low Risk
- **Template Customization Complexity**: Admins may struggle with template editing
  - *Mitigation*: Simple WYSIWYG editor, default templates provided

## Questions for Product Manager

- [ ] Should there be a maximum number of applications per member per time period? (Currently: one per member lifetime)
- [ ] Do we need integration with calendar systems for interview scheduling? (Noted for future)
- [ ] Should we implement anonymous feedback for the application process? (Currently: not included)
- [ ] What should be the default threshold days for bulk reminder and on-hold operations?
- [ ] Should bulk operations have additional confirmation steps to prevent accidental use?

## Implementation Dependencies

### Critical Path Dependencies
1. **Database Schema Sync**: Must be resolved before any testing
2. **SendGrid Configuration**: Required for email functionality
3. **Admin Role Verification**: Needed for proper access control
4. **NSwag Type Generation**: Required for TypeScript interfaces

### Parallel Development Opportunities
1. **Email Templates**: Can be designed while forms are being built
2. **Admin Interface**: Can be developed alongside application form
3. **Documentation**: Can be created during development phases
4. **Bulk Operations**: Can be developed after core workflow is complete

## Success Validation Plan

### Phase 1: Core Functionality (Week 1)
- Application form submission working
- Email confirmation sending
- Dashboard status display
- Basic admin review interface operational

### Phase 2: Full Workflow (Week 2)
- Status change workflow complete
- All email templates implemented
- Admin notes and audit trail working
- Bulk operations functional

### Phase 3: Polish & Testing (Week 3)
- Mobile responsiveness verified
- Performance requirements met
- Security requirements validated
- User acceptance testing completed

---

**Document Status**: Updated based on comprehensive user feedback - Ready for design phase handoff
**Next Phase**: UI/UX design and technical specification
**Critical Dependencies**: Database schema sync, SendGrid configuration
**Quality Gate**: 95% functional requirements implementation required
**Key Changes**: Simplified application form, dashboard integration, bulk operations, interview approved stage, template management in vetting section