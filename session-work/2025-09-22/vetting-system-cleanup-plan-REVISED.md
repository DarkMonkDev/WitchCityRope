# Vetting System Cleanup Plan (REVISED) - 2025-09-22

## Correction: Email Templates ARE in the Wireframe!

After reviewing the wireframe more carefully, I found that email templates and certain features ARE actually needed. The wireframe includes:

## Features That SHOULD Exist (from wireframe):

### 1. Admin Grid (lines 955-1008)
- Checkbox for bulk selection
- Name, FetLife Name, Email, Application Date, Current Status columns
- Row click opens detail modal
- Bulk actions header with "Send Reminder" and "Change to On Hold"

### 2. Application Detail View (lines 1038-1135)
- Full application information display
- Status badge
- Action buttons (Approve/Deny/Hold based on current status)
- Notes section with ability to add notes
- Status history display

### 3. Email Templates Page (lines 1150-1214)
Templates needed:
- Application Received
- Interview Approved
- Interview Scheduled
- On Hold
- Denied
- Approved
- Interview Reminder

With template editor including:
- Subject line editing
- Rich text body editor
- Variable insertion ({{applicant_name}}, etc.)
- Save/Reset/Preview functionality

### 4. User Dashboard Integration (lines 1217-1337)
- Application status widget
- Different states (Under Review, Interview Approved, etc.)
- Schedule Interview button for approved applicants
- View Application button

### 5. Application Form (lines 1340-1447)
- Real Name, Scene Name, FetLife Handle
- Pronouns, Email (readonly from account)
- Other Names/Handles text area
- Tell Us About Yourself text area
- How did you find us field
- Agreement checkbox section

### 6. Bulk Operations (lines 1449-1534)
- Send Reminder modal (for interview-approved applicants)
- Change to On Hold modal
- Shows affected applications
- Confirmation before executing

## What to ACTUALLY Delete (NOT in wireframe):

### Unnecessary Entities:
1. **VettingDecisionAuditLog** - Too complex, just need simple notes
2. **VettingNoteAttachment** - Not in wireframe
3. **VettingBulkOperationLog** - Over-engineered
4. **VettingApplicationAuditLog** - Too complex
5. **VettingDecision** - Status is just a field, not separate entity
6. **VettingReviewer** - Not in wireframe
7. **VettingReference** - Not in wireframe
8. **VettingReferenceResponse** - Not in wireframe
9. **VettingReferenceAuditLog** - Not in wireframe
10. **VettingNotification** - Probably over-engineered

### What to KEEP:
1. **VettingApplication** - Core entity
2. **VettingApplicationNote** - For admin notes (but simplify)
3. **VettingEmailTemplate** - Shown in wireframe!
4. **VettingBulkOperation** - For Send Reminder/Change to Hold
5. **VettingBulkOperationItem** - Track which applications affected
6. **VettingEmailService** - To handle email sending

## Revised Cleanup Strategy:

### 1. Delete These Entities:
```
- VettingDecisionAuditLog.cs and Configuration
- VettingNoteAttachment.cs and Configuration
- VettingBulkOperationLog.cs and Configuration
- VettingApplicationAuditLog.cs and Configuration
- VettingDecision.cs and Configuration
- VettingReviewer.cs and Configuration
- VettingReference.cs and Configuration
- VettingReferenceResponse.cs and Configuration
- VettingReferenceAuditLog.cs and Configuration
- VettingNotification.cs and Configuration (maybe)
```

### 2. Simplify These:
- **VettingApplicationNote** - Make it simpler (just text, date, author)
- **VettingBulkOperation** - Keep but simplify to just what's in wireframe

### 3. Keep These As-Is:
- **VettingApplication** - Main entity
- **VettingEmailTemplate** - For email templates UI
- **VettingEmailService** - For sending emails

## Database Tables to Drop:
- VettingDecisionAuditLogs
- VettingNoteAttachments
- VettingBulkOperationLogs
- VettingApplicationAuditLogs
- VettingDecisions
- VettingReviewers
- VettingReferences
- VettingReferenceResponses
- VettingReferenceAuditLogs
- VettingNotifications (maybe)

## API Endpoints Needed:

### Application Management:
- GET /api/vetting/applications - List for admin grid
- GET /api/vetting/applications/{id} - Detail view
- POST /api/vetting/applications - User submits application
- PUT /api/vetting/applications/{id}/status - Change status
- POST /api/vetting/applications/{id}/notes - Add note

### Email Templates:
- GET /api/vetting/email-templates - List templates
- GET /api/vetting/email-templates/{type} - Get specific template
- PUT /api/vetting/email-templates/{type} - Update template
- POST /api/vetting/email-templates/{type}/preview - Preview with data

### Bulk Operations:
- POST /api/vetting/bulk/send-reminder - Send interview reminders
- POST /api/vetting/bulk/change-status - Bulk status change

## Success Criteria:
✅ Email templates work as shown in wireframe
✅ Bulk operations for reminders and status changes work
✅ Admin grid matches wireframe exactly
✅ Application detail view with notes works
✅ User can view their application status
✅ No unnecessary complexity beyond wireframe requirements