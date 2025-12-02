# Email Template Trigger Enhancement - Research Summary

**Date**: December 1, 2025
**Researcher**: Backend Developer
**Status**: RESEARCH COMPLETE - Ready for Design Phase

---

## What Was Researched

Complete backend architecture analysis for adding trigger capabilities to the email templates system:

1. **Current System Design** - Analyzed GlobalEmailTemplate, EventEmailTemplate, and service layer
2. **Existing Infrastructure** - Reviewed Hangfire background jobs, user segmentation, email service integration
3. **Data Models** - Examined Event, Session, TicketPurchase, EventParticipation entities
4. **Integration Points** - Identified where triggers would execute (ticket purchase, time-based reminders)
5. **Technical Debt** - Found ID initializer issues in multiple entities
6. **Migration Path** - Outlined database schema changes needed

---

## Key Findings

### Positive: Well-Architected System
- Clean vertical slice architecture with clear separation of concerns
- Copy-on-edit pattern for template customization (reusable pattern)
- Result<T> pattern for service layer error handling
- 8 predefined user segments (AllVettedMembers, AllTeachers, etc.)
- Hangfire already configured for PostgreSQL background jobs
- HTML sanitization to prevent XSS

### Critical Issues Identified
1. **ID Initializers**: Event.cs, Session.cs, TicketPurchase.cs all have problematic `= Guid.NewGuid()` initializers that should be removed before proceeding
2. **User Segment Extensibility**: Need to add "EventParticipants" segment for event-specific trigger recipients
3. **RecipientGroup Field**: Currently untyped string - should be strongly typed to UserSegment values

### Architecture Patterns to Leverage
- Hangfire job pattern (see BackupJob.cs for reference)
- Result<T> pattern from EmailTemplateService
- User segment query builders with IQueryable
- Copy-on-edit pattern for event customization

---

## Recommended Entity Changes

### GlobalEmailTemplate (Add 5 properties)
```csharp
public int TriggerType { get; set; }              // Fixed/TimeBased/Manual
public string? EventTriggerName { get; set; }    // TicketPurchase, TicketCancellation, etc.
public int? TimingOffsetDays { get; set; }        // For time-based: +X before, -X after
public string? RecipientTarget { get; set; }     // UserSegment name or event-specific
public bool TriggerEnabled { get; set; }          // Soft disable triggers
```

### EventEmailTemplate (Add 3 properties)
```csharp
public bool? OverrideTriggerEnabled { get; set; }
public string? OverrideRecipientTarget { get; set; }
public int? OverrideTimingOffsetDays { get; set; }
```

### New Enums
```csharp
public enum TemplateTriggerType { Manual = 0, Fixed = 1, TimeBased = 2 }
public enum FixedEventTrigger { TicketPurchase, TicketCancellation, PasswordReset, ... }
```

---

## Integration Points

### Fixed Event Triggers
When: Specific business events occur (ticket purchase, cancellation, etc.)
Where: Hook into Participation/Payment services after state changes
How: Publish event → Job processes → Template selected → Recipients selected → Email sent

### Time-Based Triggers
When: Scheduled job runs daily (morning UTC)
Where: New TimedEmailTriggerJob runs via Hangfire
How: Find sessions matching offset → Select recipients → Send emails

---

## Database Changes Required

**GlobalEmailTemplate**: Add 5 columns + 3 check constraints
**EventEmailTemplate**: Add 3 columns (all nullable for backward compatibility)
**New Table**: EmailTriggerLogs for audit trail (100+ rows expected)

See handoff document for complete SQL schema.

---

## Next Steps (Design Phase)

1. **Define all trigger events** - Complete list of fixed event triggers
2. **Variable replacement strategy** - What {{variables}} available for each trigger type?
3. **Override inheritance** - How granular should event-level customization be?
4. **Timezone handling** - UTC-only or local timezone support?
5. **Delivery guarantees** - Retry policy for failed sends?

---

## Handoff Document

Complete analysis available at:
`/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-12-01-trigger-enhancements/handoffs/backend-analysis.md`

This document contains:
- Current architecture overview (650+ lines)
- All entity extension recommendations with code snippets
- Database schema with SQL statements
- Existing pattern explanations with code examples
- Integration point details for both trigger types
- Critical issues with impact analysis
- Testing considerations
- Migration strategy (5 phases)

---

## Critical Items for Design Team

1. **MUST FIX BEFORE CODING**: ID initializers in Event, Session, TicketPurchase
2. **DEFINITION NEEDED**: Complete enum for FixedEventTrigger values
3. **DECISION NEEDED**: Granularity of event-level overrides
4. **VALIDATION NEEDED**: Variable list for each trigger type

---

Research is COMPLETE. System is ready for Design Phase handoff.
