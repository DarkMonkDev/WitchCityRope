# Implementation Plan: Per-Ticket-Purchase Cancellation Flags

**Date**: 2025-12-11
**Status**: Approved for Implementation
**Type**: Bug Fix / Enhancement

## Problem Statement

In multi-session events, the Event Details Page incorrectly disables ticket cancellation and purchase buttons based on the **event's start date** rather than the **individual session start times**.

### Example Scenario
- User has a ticket for Session A (starts in 24 hours)
- Session B starts in several weeks
- Cancellation cutoff is set to 28 hours
- **Current Bug**: System blocks purchasing a ticket for Session B even though it's weeks away
- **Root Cause**: `GetParticipationStatusAsync` uses `Event.StartDate` for timing calculations instead of per-session timing

## Solution Overview

Add a `CanCancel` boolean property to each `TicketPurchaseInfoDto` so that cancellation eligibility is calculated per-ticket-purchase based on that ticket's sessions, not the event's start date.

## Files to Modify

### Backend Files

1. **`/home/chad/repos/witchcityrope/apps/api/Features/Participation/Models/EnhancedParticipationStatusDto.cs`**
   - Add `CanCancel` property to `TicketPurchaseInfoDto` class
   - Add `CancellationMessage` property for UI messaging (optional but recommended)

2. **`/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs`**
   - Modify `GetParticipationStatusAsync` method (around lines 147-159)
   - Calculate `CanCancel` for each ticket purchase based on its reference session
   - Update the top-level `CanCancelTicket` to be `true` if ANY ticket purchase is cancelable

### Frontend Files

3. **`/home/chad/repos/witchcityrope/apps/web/src/components/events/ParticipationCard.tsx`**
   - In cancel mode, disable checkbox for tickets where `canCancel === false`
   - Show cancellation message explaining why (if provided)

### Generated Types (Auto-updated)

4. **`/home/chad/repos/witchcityrope/packages/shared-types/src/generated/api-types.ts`**
   - Will be auto-regenerated after backend changes
   - Run: `cd packages/shared-types && npm run generate`

## Detailed Implementation Steps

### Phase 1: Backend DTO Changes

#### Step 1.1: Update TicketPurchaseInfoDto

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Models/EnhancedParticipationStatusDto.cs`

**Current Code** (lines 187-203):
```csharp
public class TicketPurchaseInfoDto
{
    public string TicketTypeName { get; set; } = string.Empty;
    public List<Guid> SessionIds { get; set; } = new();
    public decimal TotalPrice { get; set; }
}
```

**Change to**:
```csharp
/// <summary>
/// Information about a ticket purchase including ticket type name and cancellation eligibility
/// Used for displaying proper ticket names in cancel mode and per-purchase cancellation control
/// </summary>
public class TicketPurchaseInfoDto
{
    /// <summary>
    /// Name of the ticket type (e.g., "Day 1 Only", "Full Weekend Pass")
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Session IDs included in this ticket purchase
    /// </summary>
    public List<Guid> SessionIds { get; set; } = new();

    /// <summary>
    /// Total price paid for this ticket purchase
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Whether this specific ticket purchase can be cancelled based on its sessions' timing
    /// True if the reference session (earliest session in this ticket) is within the cancellation window
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Message explaining why cancellation is not available (e.g., "Cancellation window closed for Session A")
    /// Null when CanCancel is true
    /// </summary>
    public string? CancellationMessage { get; set; }
}
```

### Phase 2: Backend Service Logic Changes

#### Step 2.1: Update GetParticipationStatusAsync

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs`

**CRITICAL CONTEXT FOR DEVELOPER**:
- The current code at lines 147-159 builds the `ticketPurchases` dictionary
- The current code at lines 235-242 calculates `canCancelTicket` using EVENT-level timing (THIS IS THE BUG)
- You need to calculate per-purchase cancellation during the dictionary building phase

**Current Code** (lines 147-159):
```csharp
// Build TicketPurchases: maps TicketPurchaseId -> TicketPurchaseInfoDto (includes ticket type name and price)
var ticketPurchases = userTicketAttendanceData
    .Where(x => x.TicketPurchaseId.HasValue && x.SessionId.HasValue)
    .GroupBy(x => x.TicketPurchaseId!.Value)
    .ToDictionary(
        g => g.Key,
        g => new TicketPurchaseInfoDto
        {
            TicketTypeName = g.First().TicketTypeName ?? "Event Ticket",
            SessionIds = g.Select(x => x.SessionId!.Value).ToList(),
            TotalPrice = g.First().TotalPrice // All records in group have same price (same TicketPurchase)
        }
    );
```

**REPLACE WITH** (this is more complex - needs to load TicketTypes for reference session calculation):

```csharp
// Build TicketPurchases with per-purchase cancellation eligibility
// Need to load TicketTypes to get session mappings for reference session calculation
var ticketPurchaseIds = userTicketAttendanceData
    .Where(x => x.TicketPurchaseId.HasValue)
    .Select(x => x.TicketPurchaseId!.Value)
    .Distinct()
    .ToList();

var ticketPurchaseEntities = await _context.TicketPurchases
    .AsNoTracking()
    .Include(tp => tp.TicketType)
        .ThenInclude(tt => tt.Sessions)
    .Where(tp => ticketPurchaseIds.Contains(tp.Id))
    .ToListAsync(cancellationToken);

var ticketPurchases = new Dictionary<Guid, TicketPurchaseInfoDto>();
var hasAnyCancelableTicket = false;

foreach (var ticketPurchaseEntity in ticketPurchaseEntities)
{
    var sessionIds = userTicketAttendanceData
        .Where(x => x.TicketPurchaseId == ticketPurchaseEntity.Id && x.SessionId.HasValue)
        .Select(x => x.SessionId!.Value)
        .ToList();

    // Calculate cancellation eligibility for this specific ticket purchase
    var canCancelThisPurchase = false;
    string? cancellationMessage = null;

    if (ticketPurchaseEntity.TicketType != null)
    {
        var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
            ticketPurchaseEntity.TicketType,
            eventEntity.Sessions);

        if (referenceSession == null)
        {
            // All sessions have passed
            canCancelThisPurchase = false;
            cancellationMessage = "All sessions for this ticket have passed";
        }
        else
        {
            canCancelThisPurchase = _timeZoneService.IsActionAllowedForSession(
                referenceSession,
                null, // No open restriction for cancellation
                eventEntity.CancellationCloseHours);

            if (!canCancelThisPurchase)
            {
                // Calculate a helpful message
                var hoursUntilSession = (referenceSession.StartTime - DateTime.UtcNow).TotalHours;
                var closeHours = eventEntity.CancellationCloseHours ?? 0;
                cancellationMessage = $"Cancellation window closed (closes {closeHours}h before session)";
            }
        }
    }
    else
    {
        // Legacy data without TicketType - allow cancellation
        canCancelThisPurchase = true;
    }

    if (canCancelThisPurchase)
    {
        hasAnyCancelableTicket = true;
    }

    var ticketTypeName = userTicketAttendanceData
        .Where(x => x.TicketPurchaseId == ticketPurchaseEntity.Id)
        .Select(x => x.TicketTypeName)
        .FirstOrDefault() ?? "Event Ticket";

    ticketPurchases[ticketPurchaseEntity.Id] = new TicketPurchaseInfoDto
    {
        TicketTypeName = ticketTypeName,
        SessionIds = sessionIds,
        TotalPrice = ticketPurchaseEntity.TotalPrice,
        CanCancel = canCancelThisPurchase,
        CancellationMessage = cancellationMessage
    };
}
```

#### Step 2.2: Update canCancelTicket assignment

**Current Code** (lines 235-242):
```csharp
var canCancelTicket = false;
if (ticketAttendance != null)
{
    canCancelTicket = await _timeZoneService.IsActionAllowedAsync(
        eventEntity,
        EventActionType.CancelTicket,
        cancellationToken);
}
```

**REPLACE WITH**:
```csharp
// canCancelTicket is now derived from per-purchase calculations above
// True if ANY ticket purchase is cancelable (for showing/hiding the cancel button)
var canCancelTicket = ticketAttendance != null && hasAnyCancelableTicket;
```

**IMPORTANT**: The variable `hasAnyCancelableTicket` is set in the ticket purchase loop above. Make sure this variable is declared BEFORE the ticket purchase loop and is accessible here.

### Phase 3: Frontend Changes

#### Step 3.1: Update ParticipationCard Cancel Mode

**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/ParticipationCard.tsx`

**Location**: Around lines 720-772 (the cancel mode checkbox rendering)

**Current Code** (simplified):
```tsx
<Checkbox
  checked={isSelected}
  onChange={() => {}}
  readOnly
  color="red"
  mt={2}
/>
```

**CHANGE TO**:
```tsx
{/* Determine if this ticket can be cancelled */}
{(() => {
  const ticketInfo = ticketPurchases?.[ticketPurchaseId];
  const canCancelThis = ticketInfo?.canCancel !== false; // Default to true if not specified

  return (
    <Box
      onClick={() => canCancelThis && toggleTicketPurchaseSelection(ticketPurchaseId)}
      style={{
        cursor: canCancelThis ? 'pointer' : 'not-allowed',
        padding: '8px',
        borderRadius: '8px',
        backgroundColor: isSelected ? 'rgba(220, 53, 69, 0.1)' : 'transparent',
        border: isSelected ? '1px solid rgba(220, 53, 69, 0.3)' : '1px solid transparent',
        opacity: canCancelThis ? 1 : 0.6
      }}
    >
      <Group gap="sm" align="flex-start">
        <Checkbox
          checked={isSelected}
          onChange={() => {}}
          readOnly
          disabled={!canCancelThis}
          color="red"
          mt={2}
        />
        <Box style={{ flex: 1 }}>
          <Text size="sm" fw={500}>{ticketName}</Text>
          {/* Show cancellation message if ticket can't be cancelled */}
          {!canCancelThis && ticketInfo?.cancellationMessage && (
            <Text size="xs" c="red" mt={4}>
              {ticketInfo.cancellationMessage}
            </Text>
          )}
          {/* ... rest of session display ... */}
        </Box>
      </Group>
    </Box>
  );
})()}
```

**ALSO UPDATE**: The `purchaseEntries` mapping to include the full ticket info:
```tsx
const purchaseEntries = ticketPurchases
  ? Object.entries(ticketPurchases).map(([id, info]) => ({
      ticketPurchaseId: id,
      sessionIds: info.sessionIds,
      ticketTypeName: info.ticketTypeName,
      canCancel: info.canCancel,
      cancellationMessage: info.cancellationMessage
    }))
  : Object.entries(ticketPurchaseMap || {}).map(([id, sessionIds]) => ({
      ticketPurchaseId: id,
      sessionIds,
      ticketTypeName: null as string | null,
      canCancel: true, // Legacy data - assume cancelable
      cancellationMessage: null as string | null
    }));
```

### Phase 4: Regenerate TypeScript Types

After backend changes are complete:

```bash
cd /home/chad/repos/witchcityrope/packages/shared-types
npm run generate
```

This will auto-update the TypeScript types to include `canCancel` and `cancellationMessage` on `TicketPurchaseInfoDto`.

## Testing Requirements

### Unit Tests to Add/Modify

1. **AttendanceService Tests** - Test `GetParticipationStatusAsync`:
   - Multi-session event where first session is past cancel window, second session is not
   - Verify per-purchase `CanCancel` flags are calculated correctly
   - Verify `CanCancelTicket` is `true` when ANY purchase is cancelable

### Integration Tests to Add/Modify

2. **Participation API Tests**:
   - Create multi-session event with 2 sessions (24h apart)
   - Purchase ticket for Session A (starting soon) and Session B (starting later)
   - Set cancellation cutoff to 48 hours
   - Verify Session A ticket shows `canCancel: false`
   - Verify Session B ticket shows `canCancel: true`

### E2E Tests to Add/Modify

3. **Event Details Page Tests**:
   - Verify cancel button is enabled when any ticket is cancelable
   - Verify non-cancelable tickets show disabled checkbox in cancel mode
   - Verify cancellation message is displayed for non-cancelable tickets

## Rollback Plan

If issues are discovered:
1. Revert `TicketPurchaseInfoDto` changes (remove CanCancel, CancellationMessage)
2. Revert `GetParticipationStatusAsync` to use event-level timing
3. Regenerate TypeScript types
4. Frontend will gracefully handle missing properties (treats as cancelable)

## Success Criteria

1. ✅ User with tickets for multiple sessions sees per-ticket cancellation eligibility
2. ✅ User can cancel tickets for future sessions even if they have tickets for past/imminent sessions
3. ✅ Cancel button appears if ANY ticket is cancelable
4. ✅ Non-cancelable tickets show disabled state with explanation message
5. ✅ All existing tests pass
6. ✅ New tests verify per-ticket cancellation logic
