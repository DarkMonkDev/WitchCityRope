# Email Template Trigger Enhancements - Progress Tracking
<!-- Last Updated: 2025-12-01 -->
<!-- Version: 3.0 (FINAL) -->
<!-- Owner: Orchestrator -->
<!-- Status: Active - Requirements Approved, Ready for Design Phase -->

## Work Summary
- **Work Type**: Feature Enhancement (builds on existing email templates system)
- **Quality Gates**: R:95% → D:90% → I:85% → T:100%
- **Date Started**: 2025-12-01
- **Current Phase**: Phase 1 - Requirements (COMPLETE)
- **Next Phase**: Phase 2 - Design

## Enhancement Scope (APPROVED)

### 1. Events Tab - Time-Based Triggers
- Templates fire relative to **session start time** (not event start time)
- Positive numbers = days BEFORE session
- Negative numbers = days AFTER session (e.g., -2 for post-event surveys)
- Multi-session events: each session triggers independently

### 2. Events Tab - Recipient Groups
| Recipient | Description | Logic |
|-----------|-------------|-------|
| **SessionAttendees** | Users who actually attended | Based on check-in records |
| **RSVPTicketHolders** | RSVP (socials) OR ticket holders (classes) | Deduplicated |
| **SessionVolunteers** | Volunteers for that session | From assignments |
| **Teachers** | Teachers assigned to session | From assignments |

### 3. Ad Hoc Tab Enhancements (APPROVED)
- **Scheduled Send**: Queue emails for future delivery
- **Save as Template**: Store ad hoc emails for reuse
- **Delete Template**: Remove saved templates
- ADD/DELETE only on Ad Hoc tab

### 4. Other Tabs (Vetting/Admin/Incident)
- NO changes - action-based triggers remain hardcoded in services
- NO UI configuration needed

---

## Implementation Plan (Option A - APPROVED)

### Backend Changes

#### 1. New Enums
```csharp
// EventRecipientGroup.cs - For Events tab only
public enum EventRecipientGroup
{
    SessionAttendees,     // Users who checked in
    RSVPTicketHolders,    // RSVP (socials) or ticket holders (classes) - dedupe
    SessionVolunteers,    // Volunteers for session
    Teachers              // Teachers for session
}

// TemplateTriggerType.cs
public enum TemplateTriggerType
{
    Manual = 0,       // No automatic trigger
    FixedEvent = 1,   // Triggered by action (existing behavior)
    TimeBased = 2     // Triggered by session timing
}
```

#### 2. Entity Changes

**GlobalEmailTemplate** (Events category only):
```csharp
public TemplateTriggerType TriggerType { get; set; } = TemplateTriggerType.FixedEvent;
public bool TriggerEnabled { get; set; } = true;
public int? TimingOffsetDays { get; set; }  // +3 = 3 days before, -2 = 2 days after
public EventRecipientGroup? RecipientGroup { get; set; }
```

**EventEmailTemplate** (overrides):
```csharp
public bool? OverrideTriggerEnabled { get; set; }
public int? OverrideTimingOffsetDays { get; set; }
public EventRecipientGroup? OverrideRecipientGroup { get; set; }
```

**NEW: AdHocEmailTemplate** (for saved templates):
```csharp
public class AdHocEmailTemplate
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; }  // From subject or custom
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string PlainTextBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}
```

**SentAdHocEmail** (add field):
```csharp
public DateTime? ScheduledSendAt { get; set; }  // null = immediate
```

**NEW: EmailTriggerLog** (audit/idempotency):
```csharp
public class EmailTriggerLog
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? EventId { get; set; }
    public Guid SessionId { get; set; }  // Required for time-based triggers
    public string TemplateType { get; set; }
    public string TriggerType { get; set; }
    public string RecipientGroup { get; set; }
    public int RecipientCount { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; }  // Sent, Failed, Skipped
}
```

#### 3. New Hangfire Job

**EmailSchedulerJob** (follows BackupJob pattern):
```csharp
public class EmailSchedulerJob
{
    // Runs daily (6am recommended)
    public async Task ExecuteAsync(PerformContext context)
    {
        // 1. Time-based event triggers
        // - Query sessions starting in next 7 days OR ended in last 7 days
        // - For each session, get applicable time-based templates
        // - Check EmailTriggerLog for already-sent (idempotency)
        // - Resolve recipients by EventRecipientGroup (deduplicate)
        // - Send via IEmailService
        // - Log to EmailTriggerLog

        // 2. Scheduled ad hoc emails
        // - Query SentAdHocEmail where ScheduledSendAt <= now AND Status = "Scheduled"
        // - Send via IEmailService
        // - Update status to "Sent"
    }
}
```

#### 4. Service Extensions

**EmailTemplateService** additions:
```csharp
// Events trigger configuration
Task<Result<GlobalEmailTemplateDto>> UpdateTriggerConfigAsync(Guid id, TriggerConfigRequest request, CancellationToken ct);
Task<Result<List<GlobalEmailTemplateDto>>> GetTimeBasedTemplatesAsync(CancellationToken ct);

// Ad hoc template management
Task<Result<List<AdHocEmailTemplateDto>>> GetAdHocTemplatesAsync(CancellationToken ct);
Task<Result<AdHocEmailTemplateDto>> SaveAsTemplateAsync(SaveAsTemplateRequest request, Guid userId, CancellationToken ct);
Task<Result> DeleteAdHocTemplateAsync(Guid id, CancellationToken ct);

// Scheduled ad hoc
Task<Result<SentAdHocEmailDto>> ScheduleAdHocEmailAsync(SendAdHocEmailRequest request, DateTime scheduledAt, Guid userId, CancellationToken ct);
```

**NEW: EventRecipientService** (recipient resolution):
```csharp
public class EventRecipientService
{
    // Resolves recipients based on EventRecipientGroup + SessionId
    Task<List<RecipientInfo>> GetRecipientsAsync(Guid sessionId, EventRecipientGroup group, CancellationToken ct);
}
```

#### 5. API Endpoints

Add to EmailTemplateEndpoints:
```csharp
// Events trigger config
PUT  /api/templates/{id}/trigger-config
GET  /api/templates/time-based

// Ad hoc templates
GET  /api/templates/adhoc
POST /api/templates/adhoc (save as template)
DELETE /api/templates/adhoc/{id}

// Scheduled ad hoc
POST /api/adhoc/schedule

// Trigger logs
GET  /api/email-trigger-logs
GET  /api/email-trigger-logs/session/{sessionId}
```

### Frontend Changes

#### 1. Events Tab - Template Cards
- Add trigger type badge (Time-Based / Fixed Event)
- Add timing display ("3 days before" / "2 days after")
- Add recipient group display
- Add edit button for trigger config

#### 2. Events Tab - Trigger Config Modal
- Radio.Group: Trigger type (Fixed Event / Time-Based)
- NumberInput: Days offset (when Time-Based, allow negative)
- Select: Recipient group (EventRecipientGroup options)
- Switch: Enabled/Disabled

#### 3. Ad Hoc Tab Enhancements
- DateTimePicker: Scheduled send date (optional)
- Button: "Save as Template"
- Saved templates section with delete option

---

## Quality Gates

| Phase | Target | Current | Status |
|-------|--------|---------|--------|
| **Phase 1: Requirements** | 95% | 100% | COMPLETE |
| **Phase 2: Design** | 90% | 0% | NOT STARTED |
| **Phase 3: Implementation** | 85% | 0% | NOT STARTED |
| **Phase 4: Testing** | 100% | 0% | NOT STARTED |
| **Phase 5: Finalization** | 100% | 0% | NOT STARTED |

---

## Effort Estimate

| Component | Sessions | Risk |
|-----------|----------|------|
| Database migration + entities | 0.5 | Low |
| EmailSchedulerJob | 1 | Low |
| EventRecipientService | 1 | Medium |
| EmailTemplateService extensions | 0.5 | Low |
| API endpoints | 0.5 | Low |
| Events tab UI (cards + config) | 1 | Low |
| Ad hoc templates (save/delete) | 0.5 | Low |
| Scheduled send | 0.5 | Low |
| Testing | 1-2 | Low |
| **Total** | **6-8 sessions** | Low-Medium |

---

## Key Files to Reference

### Backend (MANDATORY reading for sub-agents)
- `/apps/api/Features/EmailTemplates/Services/EmailTemplateService.cs`
- `/apps/api/Features/EmailTemplates/Entities/GlobalEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
- `/apps/api/Features/EmailTemplates/Entities/UserSegment.cs`
- `/apps/api/Features/Backup/Jobs/BackupJob.cs` (Hangfire pattern)
- `/docs/architecture/ARCHITECTURE-WITHOUT-MEDIATR.md`

### Frontend
- Existing email template cards in Events tab
- Mantine v7 components (Card, Badge, Modal, Radio.Group, Select, NumberInput)

---

## Agent Delegation Protocol

**CRITICAL - READ BEFORE DELEGATING:**

Previous agents failed to properly research the codebase, resulting in incorrect recommendations (MediatR was suggested despite explicit documentation that it's NOT used).

**All sub-agents MUST:**

1. **READ ACTUAL CODE FIRST**
   - Do NOT assume patterns - verify by reading files
   - Check entity structures before proposing changes
   - Verify Hangfire job patterns from BackupJob.cs
   - Confirm NO MediatR (read ARCHITECTURE-WITHOUT-MEDIATR.md)

2. **FOLLOW EXISTING PATTERNS**
   - Direct service injection (no command/handler)
   - Result<T> for error handling
   - Copy-on-edit for event overrides
   - Endpoint pattern from EmailTemplateEndpoints

3. **CREATE HANDOFF DOCUMENTS**
   - Location: `/docs/functional-areas/email-templates/new-work/2025-12-01-trigger-enhancements/handoffs/`
   - Include: What was done, what needs verification, next steps

4. **VERIFY BEFORE PROPOSING**
   - If unsure, READ THE CODE
   - Do NOT make assumptions about patterns
   - Grep for existing implementations before creating new ones

---

## Next Steps

1. **AWAITING**: User approval of final requirements
2. **Phase 2**: Design phase (database design, UI wireframes, API specs)
3. **Delegation**: Will use explicit instructions for sub-agents with mandatory code review requirements

---

## Human Review Points

1. **NOW**: Final approval of requirements before design phase
2. **After Design**: Approve wireframes and database schema
3. **After First Vertical Slice**: Review working implementation
