# Admin Ticket Cancellation Issues - Research & Findings

**Date**: 2025-12-09
**Author**: Claude (AI Assistant)
**Status**: Research Complete - Ready for Implementation
**Priority**: HIGH - Affects refund processing and multi-session ticket handling

---

## Purpose of Research

During implementation of selective ticket cancellation for the public-facing event detail page, critical issues were discovered in the admin ticket cancellation and refund workflows. This document details those findings and proposes solutions for a backend developer to implement.

### Original Context

The user wanted to implement a UI feature allowing users to cancel individual tickets (not all tickets at once) for multi-session events. During research into how the backend handles ticket-to-session relationships, the following issues were discovered in admin endpoints.

---

## Critical Issues Found

### Issue 1: Admin Endpoints Use Deprecated `Payment` Entity

**Severity**: HIGH
**Location**: `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`

**Problem**: Admin refund endpoints look up payments using the `Payment` entity via `EventRegistrationId`, but the actual payment data is stored in `TicketPurchase` entity.

**Affected Code**:

```csharp
// Line 883-888 in AdminRemoveRsvp endpoint
var payment = await context.Payments
    .FirstOrDefaultAsync(p =>
        p.EventRegistrationId == ticketParticipation.Id &&
        p.Status == WitchCityRope.Api.Features.Payments.Models.PaymentStatus.Completed,
        cancellationToken);

// Line 1053-1058 in AdminRefundTicket endpoint
var payment = await context.Payments
    .FirstOrDefaultAsync(p =>
        p.EventRegistrationId == ticketParticipation.Id &&
        p.Status == WitchCityRope.Api.Features.Payments.Models.PaymentStatus.Completed,
        cancellationToken);
```

**Correct Approach** (from `AttendanceService.ProcessAutomaticRefundAsync`):

```csharp
// Line 1371-1384 - Uses TicketPurchase entity correctly
var attendance = await _context.EventAttendances
    .FirstOrDefaultAsync(ea => ea.Id == attendanceId, cancellationToken);

if (attendance == null || !attendance.TicketPurchaseId.HasValue)
{
    // Skip - no ticket purchase
    return;
}

var ticketPurchase = await _context.TicketPurchases
    .FirstOrDefaultAsync(tp => tp.Id == attendance.TicketPurchaseId.Value, cancellationToken);
```

**Impact**: Admin refunds may fail silently because they're looking for payments in the wrong table.

---

### Issue 2: Multi-Session Tickets Only Partially Cancelled

**Severity**: HIGH
**Location**: Multiple admin endpoints and user-facing cancel

**Problem**: When a user purchases a multi-session ticket (e.g., "Both Days"), the system creates ONE `TicketPurchase` and MULTIPLE `EventAttendance` records (one per session). Current cancellation logic only cancels ONE attendance record.

**Affected Endpoints**:

1. `DELETE /api/admin/events/{eventId}/participations/{userId}` (lines 717-807)
   - Only finds first active attendance: `.FirstOrDefaultAsync()`
   - Should find ALL active ticket attendances for this user

2. `DELETE /api/admin/events/{eventId}/participations/{userId}/remove` (lines 809-988)
   - Same issue - only processes one attendance

3. `POST /api/admin/events/{eventId}/tickets/{userId}/refund` (lines 990-1162)
   - Same issue - only finds one ticket participation

4. User-initiated cancel in `AttendanceService.CancelParticipationAsync` (line 922-924)
   - Uses `.FirstOrDefaultAsync()` with `.OrderByDescending(ea => ea.CreatedAt)`
   - Only cancels the most recent attendance, leaving others active

**Example Scenario**:
- User buys "Both Days" ticket → Creates 2 EventAttendance records (S1, S2)
- Admin cancels ticket → Only cancels 1 attendance (e.g., S2)
- S1 remains active → User still appears registered for Day 1
- Refund is processed for full amount, but user retains partial access

---

### Issue 3: Inconsistent Refund Processing Architecture

**Severity**: MEDIUM
**Location**: Architecture-level

**Problem**: Two different payment entity patterns exist:

| Flow | Entity Used | Lookup Method |
|------|-------------|---------------|
| User cancel | `TicketPurchase` | `attendance.TicketPurchaseId` |
| Admin cancel | `Payment` | `p.EventRegistrationId == attendance.Id` |

**Context**: It appears the system was migrated from a `Payment`-based model to a `TicketPurchase`-based model, but admin endpoints were not updated.

**Verification Needed**: The implementing agent should verify:
1. Is `Payment` entity still used anywhere?
2. Are there records in `Payments` table that need migration?
3. Can `Payment` entity be deprecated entirely?

---

## Proposed Solutions

### Solution for Issue 1: Update Admin Endpoints to Use TicketPurchase

**Change From**:
```csharp
var payment = await context.Payments
    .FirstOrDefaultAsync(p => p.EventRegistrationId == ticketParticipation.Id ...);
```

**Change To**:
```csharp
// Get TicketPurchase via the attendance record
var ticketPurchase = ticketParticipation.TicketPurchaseId.HasValue
    ? await context.TicketPurchases
        .FirstOrDefaultAsync(tp => tp.Id == ticketParticipation.TicketPurchaseId.Value, cancellationToken)
    : null;
```

Then use `ticketPurchase.TotalPrice`, `ticketPurchase.PaymentMethod`, etc. for refund processing.

---

### Solution for Issue 2: Cancel ALL Attendances for a TicketPurchase

**Pattern**: When cancelling a ticket, find ALL EventAttendance records with the same `TicketPurchaseId` and cancel them all.

**Proposed Logic**:
```csharp
// Find the first attendance to get TicketPurchaseId
var firstAttendance = await context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.EventId == eventId &&
        ea.UserId == userId &&
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.Status == AttendanceStatus.Active,
        cancellationToken);

if (firstAttendance?.TicketPurchaseId != null)
{
    // Find ALL attendances linked to this ticket purchase
    var allAttendances = await context.EventAttendances
        .Where(ea =>
            ea.TicketPurchaseId == firstAttendance.TicketPurchaseId &&
            ea.Status == AttendanceStatus.Active)
        .ToListAsync(cancellationToken);

    // Cancel all of them
    foreach (var attendance in allAttendances)
    {
        attendance.Status = AttendanceStatus.Cancelled;
        attendance.CancelledAt = DateTime.UtcNow;
        attendance.CancellationReason = reason;
        attendance.UpdatedBy = adminUserId;
    }
}
```

---

### Solution for Issue 3: Standardize on TicketPurchase

**Recommendation**: Fully migrate to `TicketPurchase` entity for all payment operations.

**Steps**:
1. Audit all usages of `Payment` entity in codebase
2. Verify no active records exist that only have `Payment` data
3. Update all admin endpoints to use `TicketPurchase`
4. Consider deprecating `Payment` entity or documenting its purpose

---

## Files to Modify

| File | Changes Needed |
|------|----------------|
| `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` | Update admin endpoints (lines 717-1162) |
| `apps/api/Features/Participation/Services/AttendanceService.cs` | Update `CancelParticipationAsync` to cancel all sessions |
| `apps/api/Features/Participation/Services/IAttendanceService.cs` | Possibly add new method signature |

---

## Additional Research for Implementing Agent

Before implementing, the assigned agent should:

1. **Grep for `Payment` entity usage**:
   ```bash
   grep -r "context.Payments" apps/api/
   grep -r "Payment\." apps/api/Features/
   ```

2. **Check if Payment table has data**:
   - Query production/staging database for records in `Payments` table
   - Determine if migration is needed

3. **Review RefundService**:
   - File: `apps/api/Features/Payments/Services/RefundService.cs`
   - Verify it uses `TicketPurchase` correctly
   - Check `ProcessRefundRequest.PaymentId` - does it expect `Payment.Id` or `TicketPurchase.Id`?

4. **Review related tests**:
   ```bash
   grep -r "AdminRefundTicket\|AdminRemoveRsvp\|AdminRemoveParticipation" tests/
   ```

5. **Check frontend admin components**:
   - Are there admin UI components that call these endpoints?
   - Do they need updates for multi-session awareness?

---

## Acceptance Criteria

When this work is complete:

1. [ ] Admin can cancel a multi-session ticket and ALL sessions are cancelled
2. [ ] Admin can refund a ticket and the refund processes correctly via `TicketPurchase`
3. [ ] User-initiated cancel cancels ALL sessions for a ticket purchase
4. [ ] Refund amount matches the actual paid amount from `TicketPurchase.TotalPrice`
5. [ ] All existing tests pass
6. [ ] New tests cover multi-session cancellation scenarios

---

## Related Work

This research was conducted as part of implementing selective ticket cancellation for the public event detail page. The frontend work includes:

- Adding `TicketPurchaseSessionMap` to `EnhancedParticipationStatusDto` (maps ticket purchase IDs to session IDs)
- Updating `ParticipationCard.tsx` to show tickets (not sessions) in cancel mode
- Allowing users to select which ticket(s) to cancel

The frontend changes depend on the backend correctly handling multi-session cancellation.

---

## References

- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` - Admin endpoints
- `apps/api/Features/Participation/Services/AttendanceService.cs` - Core attendance logic
- `apps/api/Models/TicketPurchase.cs` - Payment data entity
- `apps/api/Features/Participation/Entities/EventAttendance.cs` - Attendance records
- `apps/api/Features/Payments/Services/RefundService.cs` - Refund processing
