# Selective Ticket Cancellation Implementation Summary

**Date**: 2025-12-09
**Implementer**: Backend Developer (Claude)
**Status**: COMPLETE - API compiles successfully
**Related Research**: `/docs/functional-areas/payments/research/2025-12-09-admin-ticket-cancellation-issues.md`

---

## Overview

Implemented proper selective ticket cancellation that allows users to cancel individual ticket purchases instead of all tickets at once. This is critical for multi-session events where users may have multiple tickets and want to cancel only specific ones.

### Key Issue Solved

**BEFORE**: Cancelling a ticket only cancelled ONE EventAttendance record (using `.FirstOrDefaultAsync()`)
- User buys "Both Days" ticket → Creates 2 EventAttendance records (Saturday + Sunday)
- User cancels → Only Saturday gets cancelled
- Sunday attendance remains ACTIVE
- User is still registered for one day despite full refund

**AFTER**: Cancelling a ticket cancels ALL EventAttendance records for that TicketPurchase
- User buys "Both Days" ticket → Creates 2 EventAttendance records linked by TicketPurchaseId
- User cancels → BOTH Saturday AND Sunday get cancelled
- User is fully removed from event
- Refund processed correctly for the full TicketPurchase

---

## Implementation Details

### 1. New Request Model

**File**: `/apps/api/Features/Participation/Models/CancelTicketRequest.cs`

```csharp
public class CancelTicketRequest
{
    public List<Guid>? TicketPurchaseIds { get; set; }
    public string? Reason { get; set; }
}
```

**Purpose**: Allows frontend to specify which ticket purchases to cancel (not just "all tickets" or "most recent ticket")

---

### 2. Enhanced Cancel Endpoint

**File**: `/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`

**Endpoint**: `DELETE /api/events/{eventId:guid}/participation`

**Changes**:
- Added `[FromBody] CancelTicketRequest? request` parameter
- Dual mode support:
  - **NEW Mode**: If `request.TicketPurchaseIds` is provided → Call `CancelTicketPurchasesAsync()`
  - **Legacy Mode**: If `type` parameter is provided → Call `CancelParticipationAsync()` (existing behavior)

**Backward Compatibility**: YES - legacy mode still works for existing clients

---

### 3. New Service Method

**File**: `/apps/api/Features/Participation/Services/AttendanceService.cs`

**Method**: `CancelTicketPurchasesAsync()`

**Logic Flow**:
1. **Validate event exists** - Get event with sessions for timing checks
2. **Find ALL attendances** - Query for ALL EventAttendance records matching the TicketPurchaseIds
   ```csharp
   var attendancesToCancel = await _context.EventAttendances
       .Where(ea =>
           ea.EventId == eventId &&
           ea.UserId == userId &&
           ea.Status == AttendanceStatus.Active &&
           ea.TicketPurchaseId.HasValue &&
           ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value))
       .ToListAsync(cancellationToken);
   ```
3. **Security check** - Verify all ticket purchases belong to requesting user
4. **Timing validation** - Check cancellation window for each ticket type
5. **Process refunds** - ONE refund per TicketPurchase (not per attendance)
   ```csharp
   var processedRefunds = new HashSet<Guid>();
   foreach (var attendance in attendancesToCancel)
   {
       if (attendance.TicketPurchaseId.HasValue &&
           !processedRefunds.Contains(attendance.TicketPurchaseId.Value))
       {
           await ProcessAutomaticRefundAsync(...);
           processedRefunds.Add(attendance.TicketPurchaseId.Value);
       }
   }
   ```
6. **Cancel ALL attendances** - Mark all found attendances as cancelled
7. **Cancel associated RSVP** - If exists (same as existing behavior)
8. **Update EventAttendee** - Set to "cancelled" if no active attendances remain
9. **Auto-cancel volunteer signups** - Integration with volunteer system

---

### 4. Interface Update

**File**: `/apps/api/Features/Participation/Services/IAttendanceService.cs`

**Added**:
```csharp
Task<Result> CancelTicketPurchasesAsync(
    Guid eventId,
    Guid userId,
    List<Guid> ticketPurchaseIds,
    string? reason = null,
    CancellationToken cancellationToken = default);
```

---

## Critical Business Logic

### Multi-Session Ticket Handling

**Architecture Pattern**:
- ONE `TicketPurchase` record = One payment transaction
- MULTIPLE `EventAttendance` records = One per session covered by the ticket
- ALL attendances share the same `TicketPurchaseId`

**Example**:
```
TicketPurchase (ID: abc123, TotalPrice: $40, TicketType: "Both Days")
  └─ EventAttendance (ID: ea1, SessionId: saturday-session, TicketPurchaseId: abc123)
  └─ EventAttendance (ID: ea2, SessionId: sunday-session, TicketPurchaseId: abc123)
```

**Cancellation**:
- Frontend sends `ticketPurchaseIds: [abc123]`
- Backend finds BOTH `ea1` and `ea2`
- Backend cancels BOTH attendances
- Backend processes ONE refund for TicketPurchase `abc123`

---

## Refund Processing

**Pattern Used**: Existing `ProcessAutomaticRefundAsync()` method

**Key Points**:
- Uses `TicketPurchase` entity (correct pattern - documented in research)
- ONE refund per TicketPurchase (not per attendance)
- Refund amount = `TicketPurchase.TotalPrice`
- Only processes PayPal refunds automatically
- Refund failures do NOT block cancellation (logged for manual review)

**Integration**: Reuses existing `RefundService` - no duplicate refund logic

---

## Security Features

### User Authorization
```csharp
var unauthorizedPurchases = ticketPurchases.Where(tp => tp.UserId != userId).ToList();
if (unauthorizedPurchases.Any())
{
    return Result.Failure("Unauthorized: Cannot cancel tickets that don't belong to you");
}
```

**Protection**: Users can only cancel their OWN ticket purchases

### Timing Validation

**Per-ticket-type validation**:
- Uses `TimeZoneService.GetReferenceSessionForTicketType()` to determine earliest session
- Checks if cancellation window is still open for that session
- Different ticket types may have different timing rules

**Error messages**:
- `"Cannot cancel - all sessions for ticket 'Both Days' have passed"`
- `"Cancellation window has closed for ticket 'Saturday Only'"`

---

## Integration Points

### 1. RSVP Cancellation
- If cancelling tickets → Also cancels associated RSVP (if exists)
- Same business rule as existing `CancelParticipationAsync()`
- Prevents orphaned RSVPs

### 2. Volunteer Signups
- Auto-cancels volunteer shifts when attendance is cancelled
- Uses `VolunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync()`
- Failure logged but doesn't block cancellation

### 3. EventAttendee Records
- Check-in system integration
- Updates `RegistrationStatus` to "cancelled" if no active attendances remain
- Preserves status if user has other active tickets/RSVPs

### 4. Audit History
- Creates `AttendanceHistory` records for each cancelled attendance
- Stores old/new values as JSON
- Tracks `ChangedBy` user and reason

---

## Testing Checklist

### Manual Testing Scenarios

1. **Single-session ticket cancellation**
   - [ ] Cancel one ticket → One attendance cancelled
   - [ ] Verify refund processed
   - [ ] Verify RSVP cancelled (if exists)

2. **Multi-session ticket cancellation**
   - [ ] Cancel "Both Days" ticket → ALL sessions cancelled
   - [ ] Verify only ONE refund processed (not per session)
   - [ ] Verify EventAttendee status updated

3. **Selective cancellation**
   - [ ] User has 3 tickets → Cancel 1 specific ticket
   - [ ] Verify only that ticket's attendances cancelled
   - [ ] Verify other tickets remain active

4. **Security**
   - [ ] Try to cancel another user's ticket → 401 Unauthorized
   - [ ] Verify error message doesn't leak ticket details

5. **Timing**
   - [ ] Try to cancel after cancellation window closed → 400 Bad Request
   - [ ] Verify error message specifies which ticket type

6. **Legacy mode**
   - [ ] Use old endpoint (type=ticket) → Still works
   - [ ] Verify backward compatibility

---

## Acceptance Criteria Status

| Criterion | Status | Notes |
|-----------|--------|-------|
| Cancel endpoint accepts `ticketPurchaseIds` | ✅ DONE | Via `CancelTicketRequest` model |
| Cancels ALL EventAttendance records per TicketPurchaseId | ✅ DONE | Query finds all matching attendances |
| Refund processed using TicketPurchase entity | ✅ DONE | Uses existing `ProcessAutomaticRefundAsync()` |
| Associated RSVP cancelled | ✅ DONE | Same business rule as legacy method |
| Volunteer shifts cancelled | ✅ DONE | Integration with `VolunteerAssignmentService` |
| API compiles without errors | ✅ DONE | Verified with `dotnet build` |
| Existing tests still pass | ⏳ PENDING | Requires test executor agent |

---

## Frontend Integration Notes

### API Contract

**Endpoint**: `DELETE /api/events/{eventId:guid}/participation`

**New Request Body**:
```json
{
  "ticketPurchaseIds": ["abc-123-def", "xyz-789-ghi"],
  "reason": "User cancelled via event detail page"
}
```

**Old Request** (still supported):
```
?type=ticket&reason=No+longer+attending
```

**Response**:
- `204 No Content` - Success
- `400 Bad Request` - Timing or validation error
- `401 Unauthorized` - Security check failed
- `404 Not Found` - No active attendances found

### Frontend Requirements

1. **TicketPurchaseSessionMap** already implemented in `EnhancedParticipationStatusDto`
2. Frontend can display tickets (not sessions) in cancel UI
3. User selects which ticket(s) to cancel
4. Frontend sends `ticketPurchaseIds` array
5. Backend handles multi-session cancellation automatically

---

## Future Improvements

### Potential Enhancements

1. **Partial refund support**
   - Currently: Full refund or no refund
   - Future: Allow admin to specify partial refund amount

2. **Bulk cancellation**
   - Currently: One API call per user
   - Future: Admin endpoint to cancel multiple users at once

3. **Cancellation analytics**
   - Track most common cancellation reasons
   - Identify events with high cancellation rates

4. **Email notifications**
   - Send confirmation email when tickets cancelled
   - Include refund timeline and amount

---

## Known Limitations

### Current Constraints

1. **PayPal only**
   - Automatic refunds only work for PayPal payments
   - Cash/Venmo require manual admin processing

2. **All-or-nothing per ticket**
   - Can't cancel individual sessions of a multi-session ticket
   - Must cancel entire ticket (all sessions)

3. **No refund amount override**
   - Refund amount = `TicketPurchase.TotalPrice` (full amount)
   - Admin must use separate refund endpoint for partial refunds

---

## Related Work

### Admin Ticket Cancellation Issues

**Research document**: `/docs/functional-areas/payments/research/2025-12-09-admin-ticket-cancellation-issues.md`

**Issues NOT addressed** (documented for separate work):
1. Admin endpoints still use deprecated `Payment` entity
2. Admin endpoints only cancel one attendance (need same multi-session fix)
3. Architecture inconsistency between user and admin refund flows

**Recommendation**: Apply same multi-session cancellation pattern to admin endpoints in separate task

---

## Files Modified

| File | Changes |
|------|---------|
| `/apps/api/Features/Participation/Models/CancelTicketRequest.cs` | NEW - Request model |
| `/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` | MODIFIED - Enhanced cancel endpoint |
| `/apps/api/Features/Participation/Services/IAttendanceService.cs` | MODIFIED - Added method signature |
| `/apps/api/Features/Participation/Services/AttendanceService.cs` | MODIFIED - Implemented method |

**Build Status**: ✅ API compiles successfully with no errors

---

## Success Metrics

### Implementation Quality

- ✅ Zero compilation errors
- ✅ Follows existing patterns (`ProcessAutomaticRefundAsync`, `Result<T>`)
- ✅ Comprehensive logging (20+ log statements)
- ✅ Security validation
- ✅ Backward compatible
- ✅ Audit history created

### Code Quality

- ✅ Follows SOLID principles (Single Responsibility)
- ✅ Async/await throughout
- ✅ CancellationToken support
- ✅ Structured logging with context
- ✅ Error handling with Result pattern
- ✅ Clear documentation in XML comments

---

## Next Steps

### Immediate

1. **Frontend Integration** - React developer implements UI for selective cancellation
2. **E2E Testing** - Test executor validates multi-session cancellation scenarios
3. **Deployment** - Deploy to staging for manual QA

### Future Work

1. **Admin Endpoint Fixes** - Apply same pattern to admin cancellation endpoints
2. **Payment Entity Migration** - Fully deprecate `Payment` entity in favor of `TicketPurchase`
3. **Refund Enhancements** - Partial refund support, email notifications

---

**Implementation Complete**: 2025-12-09
**Ready for**: Frontend integration and E2E testing
