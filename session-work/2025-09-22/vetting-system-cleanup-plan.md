# Vetting System Cleanup Plan - 2025-09-22

## Problem
The vetting system was massively over-engineered by sub-agents who didn't follow the wireframes. The wireframe shows a SIMPLE system but the agents created 14+ unnecessary entities with complex audit logs, bulk operations, email templates, etc.

## What the Wireframe Actually Shows
From `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/design/ui-mockups.html`:

### Required Features:
1. **Admin Grid** with 6 columns:
   - Checkbox (bulk selection)
   - Name (real name)
   - FetLife Name
   - Email
   - Application Date
   - Current Status

2. **Application Detail View** showing:
   - Basic application info
   - About text
   - Simple notes section
   - Status change buttons (Approve/Deny/Hold)

3. **Simple Bulk Actions**:
   - Send reminder
   - Change to on hold

### That's IT! No complex features needed!

## Unnecessary Entities to DELETE

### Backend Entities (all in `/apps/api/Features/Vetting/Entities/`):
1. `VettingNotification.cs` - Not in wireframe
2. `VettingDecisionAuditLog.cs` - Not in wireframe
3. `VettingApplicationNote.cs` - Over-engineered (just need simple notes)
4. `VettingNoteAttachment.cs` - Not in wireframe
5. `VettingEmailTemplate.cs` - Not in wireframe
6. `VettingBulkOperation.cs` - Not in wireframe
7. `VettingBulkOperationItem.cs` - Not in wireframe
8. `VettingBulkOperationLog.cs` - Not in wireframe
9. `VettingApplicationAuditLog.cs` - Not in wireframe
10. `VettingDecision.cs` - Not in wireframe (status is just a field)
11. `VettingReviewer.cs` - Not in wireframe
12. `VettingReference.cs` - Not in wireframe
13. `VettingReferenceResponse.cs` - Not in wireframe
14. `VettingReferenceAuditLog.cs` - Not in wireframe

### Configuration Files (all in `/Configuration/`):
All 14+ configuration files for the above entities

### Services to Delete/Simplify:
1. `VettingEmailService.cs` & `IVettingEmailService.cs` - DELETE completely
2. `VettingService.cs` - SIMPLIFY to basic CRUD operations

## What Should Actually Exist

### Keep These (but simplify):
1. `VettingApplication.cs` - Basic application entity with fields from wireframe
2. Simple notes as a text field or basic collection

### Required Fields for VettingApplication:
- Id
- UserId
- SceneName (from user)
- RealName / FullName
- Email
- FetLifeHandle
- Pronouns
- HowFoundUs
- AboutYourself (text)
- OtherNames (text)
- ApplicationDate
- Status (enum: UnderReview, InterviewApproved, InterviewScheduled, OnHold, Denied, Approved)
- AdminNotes (simple text or basic collection)

## Database Cleanup

### Tables to DROP:
- VettingNotifications
- VettingDecisionAuditLogs
- VettingApplicationNotes (if separate table)
- VettingNoteAttachments
- VettingEmailTemplates
- VettingBulkOperations
- VettingBulkOperationItems
- VettingBulkOperationLogs
- VettingApplicationAuditLogs
- VettingDecisions
- VettingReviewers
- VettingReferences
- VettingReferenceResponses
- VettingReferenceAuditLogs

## Migration Strategy
1. Create new migration to drop all unnecessary tables
2. Simplify VettingApplications table to just required fields
3. Add simple Notes column if needed

## API Simplification

### Keep these endpoints:
- GET /api/vetting/applications (list for admin)
- GET /api/vetting/applications/{id} (detail)
- POST /api/vetting/applications (user submits)
- PUT /api/vetting/applications/{id}/status (admin changes status)
- POST /api/vetting/applications/{id}/notes (admin adds note)

### Delete these endpoints:
- All email template endpoints
- All bulk operation endpoints
- All audit log endpoints
- All reference endpoints
- All complex workflow endpoints

## Frontend Cleanup

### Keep:
- Admin grid component (already fixed to match wireframe)
- Application detail modal
- Simple vetting form

### Delete:
- Complex bulk operation UIs
- Email template management
- Audit log viewers
- Reference management
- Any complex workflow components

## Success Criteria
✅ Database has only 1-2 vetting tables (applications + maybe notes)
✅ API has only 5 basic endpoints
✅ Frontend matches wireframe exactly
✅ No unnecessary complexity
✅ Code is maintainable and simple