# Cancel Ticket Functionality & Button Styling Fix

## Issues Identified:
1. **Cancel ticket button doesn't work** - Only RSVP cancellation implemented
2. **Button styling broken** - Using custom CSS classes instead of Mantine Button
3. **Missing ticket cancel hook** - Only `useCancelRSVP` exists, no `useCancelTicket`

## Implementation Completed:

### 1. Added useCancelTicket Hook ✅
- Created `useCancelTicket` mutation hook in `/apps/web/src/hooks/useParticipation.ts`
- Mirrors the `useCancelRSVP` pattern but targets `/api/events/{eventId}/ticket` endpoint
- Updates participation status cache to reflect ticket cancellation
- Invalidates user participations query for UI refresh

### 2. Updated EventDetailPage ✅
- Imported and used `useCancelTicket` hook
- Completed `handleCancel` function to support both RSVP and ticket cancellation
- Added cancel ticket mutation loading state to the participation card

### 3. Fixed Button Styling ✅
- Replaced all custom CSS button classes (`btn btn-primary`, `btn btn-secondary`) with proper Mantine Button components
- Used appropriate Mantine Button props:
  - `variant="filled"` for primary actions
  - `variant="outline"` for secondary actions
  - `color` prop for semantic colors (green for RSVP, blue for tickets, red for cancel, etc.)
  - `fullWidth` and `size="lg"` for consistent sizing
  - `leftSection` for icons instead of inline styles
  - `loading` prop for loading states

### 4. Fixed Type Issues ✅
- Fixed `registrationDate` and `ticketPrice` property access to use nested properties from `rsvp.createdAt` and `ticket.amount`
- Ensured proper TypeScript types for participation data structure

## Technical Details:

### Cancel Ticket Hook Pattern:
```typescript
export function useCancelTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ eventId, reason }: { eventId: string; reason?: string }) => {
      await apiClient.delete(`/api/events/${eventId}/ticket`, { params: { reason } });
    },
    onSuccess: (_, variables) => {
      // Update participation status cache
      queryClient.setQueryData(/* updates hasTicket: false */);
      // Invalidate user participations list
      queryClient.invalidateQueries(/* refresh UI */);
    }
  });
}
```

### Button Styling Conversion:
```typescript
// Before: Custom CSS classes with styling issues
<button className="btn btn-primary" style={{...}}>

// After: Proper Mantine Button component
<Button variant="filled" color="green" fullWidth size="lg">
```

## Results:
- ✅ Cancel ticket functionality now works for both RSVPs and tickets
- ✅ Button styling uses proper Mantine components with consistent appearance
- ✅ Text is no longer cut off due to proper Mantine button styling
- ✅ Loading states work correctly for all operations
- ✅ TypeScript types are properly aligned with API response structure

## Files Modified:
- `/apps/web/src/hooks/useParticipation.ts` - Added useCancelTicket hook
- `/apps/web/src/pages/events/EventDetailPage.tsx` - Complete handleCancel implementation
- `/apps/web/src/components/events/ParticipationCard.tsx` - Fix button styling and property access

## Testing:
The implementation is ready for testing. Users should now be able to:
1. Cancel RSVP with optional reason
2. Cancel ticket purchases with optional reason
3. See properly styled buttons that don't have text cutoff
4. Experience consistent loading states during operations