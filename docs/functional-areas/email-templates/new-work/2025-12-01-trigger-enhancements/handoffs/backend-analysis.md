# Email Templates Trigger Enhancement - Backend Architecture Analysis
**Date**: December 1, 2025
**Analyzed by**: Backend Developer
**Status**: Research Complete - Ready for Design Phase

---

## Executive Summary

The email templates system is architecturally sound and ready for trigger enhancements. Key infrastructure (Hangfire background jobs, EventParticipation tracking, Session scheduling) already exists and can be leveraged. The system uses a clean copy-on-edit pattern with global templates as defaults and event-specific overrides.

**Critical Finding**: TicketPurchase entity has an ID initializer (`= Guid.NewGuid()`) that should be removed per EF Core best practices (documented in backend lessons learned).

---

## Current Architecture Overview

### Email Templates System
- **Location**: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/`
- **Architecture Pattern**: Vertical slice with clear separation of concerns
- **Key Components**:
  - `GlobalEmailTemplate` - System-wide templates (default templates)
  - `EventEmailTemplate` - Event-specific overrides (copy-on-edit pattern)
  - `EmailTemplateService` - Core business logic with Result<T> pattern
  - `UserSegment` enum - 8 predefined recipient groups
  - HTML sanitization for XSS prevention

### Related Systems
- **Hangfire**: Configured in `Program.cs` (lines 109-125) for PostgreSQL background jobs
- **User Management**: ApplicationUser entity with roles and vetting status
- **Participation Tracking**: EventParticipation (RSVP/ticket tracking)
- **Session Management**: Session entity with UTC date/time support
- **Ticket System**: TicketPurchase entity with payment status tracking

---

## Entity Extension Points

### 1. GlobalEmailTemplate Extensions

**Current Properties**:
```csharp
public Guid Id { get; set; }
public EmailCategory Category { get; set; }        // Vetting, Events, Admin, Incident, AdHoc
public string TemplateType { get; set; }           // "Confirmation", "Reminder1Day", etc.
public string Subject { get; set; }
public string HtmlBody { get; set; }
public string PlainTextBody { get; set; }
public string Variables { get; set; }              // JSONB array of {{variables}}
public bool IsActive { get; set; }
public int Version { get; set; }
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }
public Guid UpdatedBy { get; set; }
public ApplicationUser UpdatedByUser { get; set; }
```

**Recommended Additions for Triggers**:
```csharp
// 1. Trigger Type Enum Field
/// <summary>
/// How this template is triggered: Fixed event, time-based, or manual
/// Values: Fixed (0), TimeBased (1), Manual (2)
/// </summary>
public int TriggerType { get; set; } = (int)TemplateTriggerType.Manual;

// 2. Event Trigger Type
/// <summary>
/// For TriggerType=Fixed: Which event triggers this template
/// Examples: "TicketPurchase", "TicketCancellation", "PasswordReset", "RegistrationApproved"
/// NULL for time-based or manual templates
/// </summary>
[MaxLength(50)]
public string? EventTriggerName { get; set; }

// 3. Time Offset (days)
/// <summary>
/// For TriggerType=TimeBased: Days before/after session
/// Positive: days before event start
/// Negative: days after event start
/// Example: -2 = 2 days after session ends (post-event survey)
/// NULL for fixed event triggers
/// </summary>
public int? TimingOffsetDays { get; set; }

// 4. Recipient Target
/// <summary>
/// For TimeBased triggers: Which users should receive this email
/// Example: "AllVettedMembers", "AllTeachers", "EventParticipants", "NoShows"
/// NULL for event-triggered templates (inferred from trigger context)
/// </summary>
[MaxLength(100)]
public string? RecipientTarget { get; set; }

// 5. Enabled/Disabled
/// <summary>
/// Whether this trigger should be executed (soft disable without deleting)
/// </summary>
public bool TriggerEnabled { get; set; } = true;
```

**New Enum (Add to GlobalEmailTemplate.cs file)**:
```csharp
public enum TemplateTriggerType
{
    Manual = 0,          // Manually sent via admin UI
    Fixed = 1,           // Triggered by specific events (ticket purchase, etc.)
    TimeBased = 2        // Triggered by time (X days before/after session)
}

public enum FixedEventTrigger
{
    TicketPurchase = 0,
    TicketCancellation = 1,
    PasswordResetRequested = 2,
    PasswordResetCompleted = 3,
    RegistrationApproved = 4,
    RegistrationDenied = 5,
    VettingStatusChanged = 6,
    EventReminderManual = 7,
    EventCancelled = 8,
    EventPostponed = 9
}
```

### 2. EventEmailTemplate Extensions

**Current Properties**:
```csharp
public Guid Id { get; set; }
public Guid EventId { get; set; }
public Guid GlobalTemplateId { get; set; }
public string TemplateType { get; set; }
public string Subject { get; set; }
public string HtmlBody { get; set; }
public string PlainTextBody { get; set; }
public string[] TargetSessions { get; set; }       // ["all"] or ["S1", "S2"]
public string? RecipientGroup { get; set; }
public bool IsCustomized { get; set; }
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }
public Guid UpdatedBy { get; set; }
```

**Recommended Additions**:
```csharp
// 1. Override Trigger Settings
/// <summary>
/// If set, overrides global template's trigger configuration for this event
/// Allows events to customize WHEN they receive trigger emails
/// Examples: Disable reminder, change timing offset, change recipient group
/// </summary>
public bool? OverrideTriggerEnabled { get; set; }  // Explicit 3-state: true/false/null(use global)

[MaxLength(100)]
public string? OverrideRecipientTarget { get; set; } // Null = use global

public int? OverrideTimingOffsetDays { get; set; }  // Null = use global
```

---

## Existing Patterns to Leverage

### 1. Background Job Infrastructure (Hangfire)
**Location**: `Program.cs` lines 109-125
**Status**: ✅ Already configured for PostgreSQL

**Current Usage**:
- BackupJob runs scheduled database backups
- Uses `PerformContext` for job metadata and cancellation tokens
- Jobs are stored in PostgreSQL `hangfire` schema

**Leverage Points**:
```csharp
// Existing pattern to follow
public class BackupJob
{
    private readonly BackupOrchestrationService _orchestrationService;
    private readonly ILogger<BackupJob> _logger;

    public async Task ExecuteAsync(PerformContext context)
    {
        var jobId = context.BackgroundJob.Id;
        _logger.LogInformation("Job started: {JobId}", jobId);
        // ... work ...
    }
}
```

**For Email Triggers**: Create similar job classes:
- `FixedEventTriggerJob` - Executes when specific events occur
- `TimeBasedTriggerJob` - Runs daily to check for sessions needing reminder emails

### 2. Result<T> Pattern (Service Layer)
**Status**: ✅ Fully implemented in EmailTemplateService

All methods return `Result<T>` or `Result` with proper error handling:
```csharp
public async Task<Result<List<GlobalEmailTemplateDto>>> GetGlobalTemplatesByCategoryAsync(...)
{
    try
    {
        var templates = await _context.GlobalEmailTemplates
            .AsNoTracking()
            .Where(t => t.Category == category && t.IsActive)
            .ToListAsync(cancellationToken);
        // ... mapping ...
        return Result<List<GlobalEmailTemplateDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error message");
        return Result<List<GlobalEmailTemplateDto>>.Failure("User-friendly message");
    }
}
```

### 3. User Segment Targeting
**Status**: ✅ Already implemented (UserSegment enum + service methods)

**Available Segments**:
```csharp
AllVettedMembers        // VettingStatus == Approved, IsActive
AllPreVettedMembers     // VettingStatus != Denied/OnHold, IsActive
AllTeachers             // Role contains "Teacher", IsActive
AllDMs                  // Role contains "DungeonMonitor", IsActive
AllSafetyTeam           // Role contains "SafetyTeam", IsActive
AllAdmins               // Role contains "Administrator", IsActive
EmailNotVerified        // EmailConfirmed == false, IsActive
VettingPending          // VettingStatus == UnderReview, IsActive
```

**Service Methods**:
```csharp
private async Task<List<ApplicationUser>> GetUsersForSegmentAsync(
    UserSegment segment,
    CancellationToken cancellationToken)

private IQueryable<ApplicationUser> BuildSegmentQuery(UserSegment segment)
    // Returns IQueryable with WHERE clauses for filtering
```

**Extension for Event Participants**:
Need to add: `EventParticipants` segment to include users who purchased tickets/RSVP'd for specific event.

### 4. HTML Sanitization
**Status**: ✅ Implemented in EmailTemplateService (lines 750-770)

Removes dangerous HTML tags using regex:
- script, iframe, object, embed, form tags removed
- Simple but functional; TODO suggests considering HtmlSanitizer NuGet package

---

## Database Schema Integration

### Required Column Changes

**GlobalEmailTemplate table** (migration needed):
```sql
ALTER TABLE "GlobalEmailTemplates" ADD COLUMN "TriggerType" INTEGER DEFAULT 2;
ALTER TABLE "GlobalEmailTemplates" ADD COLUMN "EventTriggerName" VARCHAR(50) NULL;
ALTER TABLE "GlobalEmailTemplates" ADD COLUMN "TimingOffsetDays" INTEGER NULL;
ALTER TABLE "GlobalEmailTemplates" ADD COLUMN "RecipientTarget" VARCHAR(100) NULL;
ALTER TABLE "GlobalEmailTemplates" ADD COLUMN "TriggerEnabled" BOOLEAN DEFAULT true;

-- Constraints
ALTER TABLE "GlobalEmailTemplates"
  ADD CONSTRAINT "CHK_TriggerType" CHECK ("TriggerType" IN (0, 1, 2));
ALTER TABLE "GlobalEmailTemplates"
  ADD CONSTRAINT "CHK_TimingOffsetDays" CHECK ("TimingOffsetDays" IS NULL OR "TimingOffsetDays" != 0);
```

**EventEmailTemplate table** (migration needed):
```sql
ALTER TABLE "EventEmailTemplates" ADD COLUMN "OverrideTriggerEnabled" BOOLEAN NULL;
ALTER TABLE "EventEmailTemplates" ADD COLUMN "OverrideRecipientTarget" VARCHAR(100) NULL;
ALTER TABLE "EventEmailTemplates" ADD COLUMN "OverrideTimingOffsetDays" INTEGER NULL;
```

**New Table: EmailTriggerLogs** (audit trail):
```sql
CREATE TABLE "EmailTriggerLogs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TemplateId" UUID NOT NULL REFERENCES "GlobalEmailTemplates"("Id"),
    "EventId" UUID NULL REFERENCES "Events"("Id"),
    "TriggerType" INTEGER NOT NULL,           -- Fixed/TimeBased
    "TriggerName" VARCHAR(50),                 -- TicketPurchase, etc.
    "ScheduledFor" TIMESTAMPTZ NOT NULL,
    "SentAt" TIMESTAMPTZ NULL,
    "Status" VARCHAR(20),                      -- Pending, Sent, Failed
    "ErrorMessage" TEXT NULL,
    "RecipientCount" INTEGER,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);
```

### Relationship Considerations

```
GlobalEmailTemplate
├── EventEmailTemplate (has many, copy-on-edit)
├── EmailTriggerLogs (has many, for audit)
└── UpdatedByUser (many-to-one)

Session
├── Event (many-to-one)
└── TicketType (one-to-many)

TicketPurchase
├── TicketType (many-to-one)
├── Event (via TicketType)
├── User (many-to-one)
└── EventParticipation (conceptually related)
```

---

## Integration Points for Trigger Execution

### 1. Fixed Event Trigger Integration

**Ticket Purchase Flow** (Where to hook trigger):
```
Location: Participation/Services/ParticipationService.cs (or similar)

When: User completes ticket purchase payment
Flow:
  1. TicketPurchase.PaymentStatus = "Completed"
  2. EventParticipation.Status = "Active"
  3. TRIGGER: Publish event "TicketPurchaseCompleted"
  4. Email trigger job receives event
  5. Query: GetEventTemplatesAsync(eventId) + filter by FixedEventTrigger.TicketPurchase
  6. Load recipient list: GetUsersForSegmentAsync(template.RecipientTarget)
  7. Replace {{variables}} in email body
  8. Queue email via SendGrid (integrated via ISendGridClient in Program.cs)
  9. Log in EmailTriggerLogs table
```

**Integration Pattern**:
```csharp
// In ParticipationService.cs after payment completion
if (ticketPurchase.IsPaymentCompleted)
{
    var participation = await _context.EventParticipations
        .FirstOrDefaultAsync(p => p.UserId == ticketPurchase.UserId && p.EventId == eventId);

    // Publish trigger event (using Hangfire or pub/sub)
    await PublishTriggerEventAsync(
        triggerType: FixedEventTrigger.TicketPurchase,
        eventId: eventId,
        userId: ticketPurchase.UserId,
        context: ticketPurchase  // Pass context for variable replacement
    );
}
```

### 2. Time-Based Trigger Integration

**Scheduler Job** (Daily runner):
```csharp
// New job class to create in Features/EmailTemplates/Jobs/
public class TimedEmailTriggerJob
{
    private readonly EmailTemplateService _templateService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TimedEmailTriggerJob> _logger;

    public async Task ExecuteAsync(PerformContext context)
    {
        var now = DateTime.UtcNow;
        var cutoffDate = now.AddDays(1);  // Find sessions in next 24 hours

        var upcomingSessions = await _context.Sessions
            .Where(s => s.StartTime >= now && s.StartTime <= cutoffDate)
            .ToListAsync();

        foreach (var session in upcomingSessions)
        {
            // Find templates with matching offset
            var templates = await _context.GlobalEmailTemplates
                .Where(t => t.TriggerType == (int)TemplateTriggerType.TimeBased
                         && t.TriggerEnabled
                         && t.TimingOffsetDays.HasValue)
                .ToListAsync();

            foreach (var template in templates)
            {
                // Calculate if this is the right time to send
                var sendTime = session.StartTime.AddDays(-template.TimingOffsetDays.Value);
                if (IsTimeToSend(now, sendTime))
                {
                    await SendTemplateToSegmentAsync(template, session);
                }
            }
        }
    }

    private bool IsTimeToSend(DateTime now, DateTime scheduledTime)
    {
        // Send within 1-hour window to avoid duplicate sends
        return now >= scheduledTime && now < scheduledTime.AddHours(1);
    }
}

// Register in Program.cs
// RecurringJob.AddOrUpdate<TimedEmailTriggerJob>(
//     "TimedEmailTriggers",
//     x => x.ExecuteAsync(null),
//     Cron.Daily(9, 0));  // Run at 9am UTC daily
```

### 3. EmailService Integration

**Current Status**: ISendGridClient already injected and configured

**Current Usage** (VettingEmailService pattern):
```csharp
public class VettingEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<VettingEmailService> _logger;

    public async Task SendVettingEmailAsync(string to, string subject, string html, string plainText)
    {
        try
        {
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("noreply@witchcityrope.com", "WitchCityRope"),
                Subject = subject,
                PlainTextContent = plainText,
                HtmlContent = html
            };
            msg.AddTo(new EmailAddress(to));

            await _sendGridClient.SendEmailAsync(msg);
            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
```

**For Email Triggers**:
- Reuse or extend EmailService/IEmailService interface
- Support bulk send with personalization (user variables)
- Track delivery status for audit log

---

## Critical Issues and Recommendations

### 1. **CRITICAL**: TicketPurchase ID Initializer Issue
**Location**: `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`, line 15

**Problem**:
```csharp
public Guid Id { get; set; } = Guid.NewGuid();  // ❌ WRONG
```

**Impact**:
- Entity Framework thinks this entity is already persisted
- Causes UPDATE attempts instead of INSERT operations
- Results in `DbUpdateConcurrencyException`

**Fix** (Remove initializer - EF Core will generate IDs):
```csharp
public Guid Id { get; set; }  // ✅ CORRECT
```

**Same issue check**: Session.cs line 16 has same problem

### 2. **Session Entity Initialization**
**Location**: `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs`, line 16

Same issue as TicketPurchase - remove ID initializer.

### 3. Event Model Initialization Issue
**Location**: `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`, line 16

Also has `= Guid.NewGuid()` initializer - should be removed.

### 4. Vetting Status Mapping
**Current**: Service uses int enum (0-6) for vetting status
**Recommendation**: Create VettingStatus enum in codebase for type safety
```csharp
public enum VettingStatus
{
    UnderReview = 0,
    InterviewApproved = 1,
    FinalReview = 2,
    Approved = 3,
    Denied = 4,
    OnHold = 5,
    Withdrawn = 6
}
```

### 5. RecipientGroup String Field Design
**Current Status**: EventEmailTemplate.RecipientGroup is nullable string

**For Trigger Enhancement**:
- Should be typed enum or lookup to UserSegment values
- Current design is flexible but untyped (could be improved)
- Recommendation: Create `TemplateRecipientTarget` enum matching UserSegment + "EventParticipants"

---

## Testing Considerations

### Unit Tests Needed
1. **Trigger Enumeration Tests**
   - Verify all TemplateTriggerType values map to correct behavior
   - Verify FixedEventTrigger values are discoverable

2. **Time Offset Calculation Tests**
   - Positive offsets (X days before event)
   - Negative offsets (X days after event)
   - Edge cases (same-day events, events in past)

3. **User Segment Tests**
   - Verify RecipientTarget strings map to UserSegment enum
   - Test segment counts with various user configurations
   - Test EventParticipants segment (new)

4. **Template Merge Tests**
   - Override inheritance (global → event-specific)
   - Null vs default value handling

### Integration Tests Needed
1. **Hangfire Job Execution**
   - TimedEmailTriggerJob finds correct sessions
   - Correct templates selected based on offset
   - Jobs execute without errors

2. **Email Sending**
   - Variables replaced correctly ({{user_name}}, {{event_title}})
   - Segment lookup produces correct recipient list
   - SendGrid integration works (or falls back to console logging)

3. **Audit Trail**
   - EmailTriggerLogs created for each send attempt
   - Status transitions tracked (Pending → Sent/Failed)

---

## Open Questions for Design Phase

1. **Variable Replacement**: What variables should be available for different trigger types?
   - Fixed trigger (TicketPurchase): `{{user_name}}`, `{{event_title}}`, `{{ticket_type}}`?
   - Time-based (reminder): `{{event_title}}`, `{{session_date}}`, `{{countdown_days}}`?

2. **Delivery Guarantees**: How should we handle failed deliveries?
   - Retry with exponential backoff?
   - Manual retry from admin UI?
   - Fallback to console logging if SendGrid unavailable?

3. **Timezone Handling**: Currently using UTC everywhere
   - Should reminders be sent at user's local time?
   - Or administrator's configured timezone for event?

4. **EventParticipants Segment**: Need to clarify scope
   - All users with active participation for event?
   - Or also include cancelled participations?
   - Filter by participation type (Ticket vs RSVP)?

5. **Override Inheritance Logic**: How granular should event-level overrides be?
   - Override entire trigger settings?
   - Override only specific properties?
   - Cascade behavior when global template changes?

---

## Migration Strategy

### Phase 1: Add Database Schema
1. Create migration adding new columns to GlobalEmailTemplate
2. Create migration adding override columns to EventEmailTemplate
3. Create migration for EmailTriggerLogs table
4. Add constraints and indexes

### Phase 2: Update Entity Models
1. Add properties to GlobalEmailTemplate (with proper null handling)
2. Add properties to EventEmailTemplate
3. Create new enums (TemplateTriggerType, FixedEventTrigger)
4. Remove ID initializers from Event, Session, TicketPurchase

### Phase 3: Update Service Layer
1. Extend EmailTemplateService with trigger-related methods
2. Add trigger query builders and filters
3. Update DTOs with new properties
4. Add serialization for new enum types

### Phase 4: Implement Background Jobs
1. Create FixedEventTriggerJob
2. Create TimedEmailTriggerJob
3. Register with Hangfire in Program.cs
4. Implement trigger publishing mechanism

### Phase 5: Integration Tests
1. Test job execution
2. Test variable replacement
3. Test recipient selection
4. Test error handling and audit logging

---

## File References

**Email Templates Feature**:
- Entities: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Entities/`
- Services: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Services/`
- Models: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Models/`
- Endpoints: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Endpoints/`

**Related Features**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Participation/` - EventParticipation, ParticipationType
- `/home/chad/repos/witchcityrope/apps/api/Features/Backup/Jobs/BackupJob.cs` - Reference for job pattern
- `/home/chad/repos/witchcityrope/apps/api/Program.cs` - Hangfire configuration (lines 109-125)

**Data Models**:
- `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs` - Event entity
- `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs` - Session entity
- `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs` - TicketPurchase entity

**Vetting/Email Reference**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Services/VettingEmailService.cs` - Email service pattern
- `/home/chad/repos/witchcityrope/apps/api/Features/Shared/Services/EmailService.cs` - IEmailService interface

---

## Conclusion

The WitchCityRope email templates system is well-architected and ready for trigger enhancement. Key infrastructure (Hangfire, user segmentation, template merging) is already in place. The implementation should follow established patterns: Result<T> for service layer, copy-on-edit for event customization, and Hangfire jobs for background execution.

Main technical debt identified: ID initializers in Event/Session/TicketPurchase entities should be removed before implementation begins to avoid silent persistence failures.

Recommend proceeding to Design Phase to finalize enum definitions, variable replacement strategy, and override inheritance logic.
