# Frontend Bug Fixes Summary - September 20, 2025

## Bug 1: Admin Events List Showing 0/40 Instead of 2/40

### Problem
The admin events list page was showing "0/40" in the capacity column instead of the correct "2/40" when the API returns `currentRSVPs: 2`.

### Root Cause
The `EventsTableView` component was hardcoded to use `event.currentAttendees` for all events, but:
- For **Social Events**: The API populates `currentRSVPs` (2) but `currentAttendees` is 0
- For **Class Events**: The API should populate `currentTickets`

### Solution Applied
**File**: `/home/chad/repos/witchcityrope-react/apps/web/src/components/events/EventsTableView.tsx`

1. **Added helper function** to determine correct count based on event type:
```typescript
// Helper function to get the correct current count based on event type
const getCorrectCurrentCount = (event: EventDto): number => {
  const isSocialEvent = event.eventType?.toLowerCase() === 'social';
  return isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
};
```

2. **Updated CapacityDisplay usage** to use the helper:
```typescript
<CapacityDisplay
  current={getCorrectCurrentCount(event)}
  max={event.capacity}
/>
```

### Pattern Used
This follows the same pattern already implemented in `EventCard.tsx`:
```typescript
const isSocialEvent = event.eventType?.toLowerCase() === 'social';
const currentCount = isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
```

## Bug 2: RSVPs and Tickets Tab Not Showing

### Problem
The RSVPs and Tickets tab was not visible in the admin event details page for the "Rope Social & Discussion" event, even though:
- The API returns 2 participation records correctly at `/api/admin/events/{id}/participations`
- The EventForm component was updated to use `useEventParticipations` hook
- The tab exists in the code

### Investigation
**File**: `/home/chad/repos/witchcityrope-react/apps/web/src/components/events/EventForm.tsx`

1. **Tab is properly defined** in the component (line 456):
```typescript
<Tabs.Tab value="rsvp-tickets" data-testid="rsvp-tickets-tab">RSVP/Tickets</Tabs.Tab>
```

2. **Tab panel exists** (line 946):
```typescript
<Tabs.Panel value="rsvp-tickets" pt="xl" data-testid="rsvp-tickets-tab">
```

3. **useEventParticipations hook is called** correctly (line 85):
```typescript
const { data: participationsData, isLoading: participationsLoading, error: participationsError } = useEventParticipations(eventId || '', !!eventId);
```

4. **AdminEventDetailsPage passes eventId** correctly (line 343):
```typescript
<EventForm
  eventId={id}
  // ... other props
/>
```

### Potential Causes
The tab may be hidden due to:
1. **JavaScript/TypeScript compilation errors** preventing proper rendering
2. **Missing permissions** or authentication issues
3. **CSS/styling issues** making the tab invisible
4. **State management issues** in the tab switching logic

### Current Status
- **Bug 1**: ✅ **FIXED** - Logic implemented to show correct counts
- **Bug 2**: 🔍 **NEEDS VERIFICATION** - Code appears correct but needs runtime testing

## Next Steps
1. **Verify Bug 1 fix** by running the application and checking admin events list
2. **Debug Bug 2** by:
   - Checking browser console for JavaScript errors
   - Verifying authentication state when viewing event details
   - Testing tab visibility with developer tools
   - Ensuring no CSS is hiding the tab

## Files Modified
- `/home/chad/repos/witchcityrope-react/apps/web/src/components/events/EventsTableView.tsx`

## Testing Required
- Admin login and navigation to `/admin/events`
- Check capacity display shows correct numbers (should show 2/40 not 0/40)
- Navigate to event details and verify RSVPs/Tickets tab is visible
- Click on tab and verify participation data loads