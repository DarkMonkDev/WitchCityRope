# Vetting System Cleanup - FINAL PLAN Based on Design Docs
## Date: 2025-09-22

After reviewing all design documentation, here's what SHOULD exist based on the actual requirements:

## FROM BUSINESS REQUIREMENTS & FUNCTIONAL SPEC:

### Core Entities Needed (per functional spec line 748-757):
```
User (existing)
  ↓ 1:0..1
VettingApplication
  ↓ 1:many
VettingAuditLog

VettingEmailTemplate (standalone)
```

That's IT! Just 3 vetting entities plus existing User.

### Features Required (from wireframes & requirements):

1. **Application Submission**
   - Simple form with fields shown in wireframe
   - One application per user
   - Email confirmation on submission

2. **Admin Review Grid**
   - 6 columns as shown in wireframe
   - Click row for details
   - Bulk selection checkboxes

3. **Application Detail View**
   - Show application data
   - Add optional admin notes
   - Change status buttons

4. **Email Templates** (7 templates required)
   - Application Received
   - Interview Approved
   - Interview Scheduled
   - On Hold
   - Denied
   - Approved
   - Interview Reminder

5. **Bulk Operations** (2 operations only)
   - Send Reminder (to interview-approved apps)
   - Change to On Hold

6. **Dashboard Widget**
   - Show application status
   - Schedule interview button when approved

## ENTITIES TO DELETE (NOT in design docs):

### Delete These Entity Files:
```bash
# These are NOT in the functional specification!
/apps/api/Features/Vetting/Entities/VettingNotification.cs
/apps/api/Features/Vetting/Entities/VettingDecisionAuditLog.cs
/apps/api/Features/Vetting/Entities/VettingApplicationNote.cs  # Notes are just a field, not entity
/apps/api/Features/Vetting/Entities/VettingNoteAttachment.cs
/apps/api/Features/Vetting/Entities/VettingDecision.cs
/apps/api/Features/Vetting/Entities/VettingReviewer.cs
/apps/api/Features/Vetting/Entities/VettingReference.cs
/apps/api/Features/Vetting/Entities/VettingReferenceResponse.cs
/apps/api/Features/Vetting/Entities/VettingReferenceAuditLog.cs
/apps/api/Features/Vetting/Entities/VettingApplicationAuditLog.cs  # Use VettingAuditLog instead
/apps/api/Features/Vetting/Entities/VettingBulkOperationLog.cs
```

### Delete Configuration Files:
All configuration files for the above entities in `/Configuration/` folder

## ENTITIES TO KEEP (in design docs):

### Keep These (they match the spec):
1. **VettingApplication.cs** - Main application entity
2. **VettingEmailTemplate.cs** - For email template management
3. **VettingBulkOperation.cs** - For bulk operations tracking
4. **VettingBulkOperationItem.cs** - Track which apps affected

### Create/Rename:
1. **VettingAuditLog.cs** - Simple audit log (not the complex ones)

## SIMPLIFICATIONS NEEDED:

### VettingApplication Fields (from business requirements lines 225-237):
```csharp
public class VettingApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }  // FK to User
    public DateTime SubmittedAt { get; set; }
    public VettingStatus Status { get; set; }
    public string? AdminNotes { get; set; }  // Simple text field

    // Application fields from wireframe
    public string SceneName { get; set; }
    public string RealName { get; set; }
    public string Email { get; set; }
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }
    public string AboutYourself { get; set; }
    public string HowFoundUs { get; set; }

    // Navigation
    public User User { get; set; }
    public ICollection<VettingAuditLog> AuditLogs { get; set; }
}
```

### VettingAuditLog (from functional spec lines 253-261):
```csharp
public class VettingAuditLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Action { get; set; }
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public VettingApplication Application { get; set; }
    public User PerformedByUser { get; set; }
}
```

## API ENDPOINTS (from functional spec):

### Keep These (they match the spec):
```
POST /api/vetting/applications - Submit application
GET /api/vetting/applications/my-status - User's status

GET /api/admin/vetting/applications - Admin grid
GET /api/admin/vetting/applications/{id} - Detail view
PUT /api/admin/vetting/applications/{id}/status - Change status
POST /api/admin/vetting/applications/{id}/notes - Add note

GET /api/admin/vetting/templates - List templates
PUT /api/admin/vetting/templates/{type} - Update template
POST /api/admin/vetting/templates/{type}/preview - Preview

POST /api/admin/vetting/applications/bulk-reminder - Send reminders
POST /api/admin/vetting/applications/bulk-status-change - Bulk status
```

### Delete Any Other Endpoints Not Listed Above

## DATABASE MIGRATION:

Create migration to:
1. Drop unnecessary tables (all the complex audit/decision/reference tables)
2. Simplify VettingApplications table
3. Create simple VettingAuditLog table
4. Keep VettingEmailTemplates table
5. Keep simplified bulk operation tables

## SUCCESS CRITERIA:
✅ Only 4 vetting entities (Application, AuditLog, EmailTemplate, BulkOperation/Item)
✅ 11 API endpoints total (matching functional spec)
✅ Admin notes are simple text field, not separate entity
✅ Status is enum field, not separate entity
✅ No reference system
✅ No complex decision tracking
✅ No attachment system
✅ Simple audit log for all changes

## ACTION PLAN:
1. Delete unnecessary entity files
2. Delete their configuration files
3. Simplify VettingApplication entity
4. Create simple VettingAuditLog entity
5. Update VettingService to remove complex logic
6. Keep VettingEmailService (it's needed!)
7. Update endpoints to match spec
8. Create migration to clean database
9. Test everything works with wireframe