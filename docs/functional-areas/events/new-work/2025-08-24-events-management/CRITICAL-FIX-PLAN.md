# CRITICAL FIX PLAN - Events Management System Phase 4

## What Went Wrong

I completely failed to research the requirements before implementing Phase 4. Despite explicit instructions to research first, I:

1. **Created generic "registration" system** instead of understanding Classes vs Social Events distinction
2. **Used wrong terminology** throughout (registration instead of ticket purchase/RSVP)
3. **Missed critical business rule**: Social Events need BOTH RSVP table AND tickets table
4. **Ignored documented requirements** that clearly stated the differences

## Root Cause Analysis

1. **Rushed Implementation**: Jumped straight to coding without reading requirements
2. **Assumed Generic Solution**: Thought all events worked the same way
3. **Ignored Context**: Didn't recognize "registration" was the wrong term from prior documentation
4. **Failed to Delegate**: Should have used business-requirements agent first

## The Correct Business Rules

### Classes (Educational Events)
- **Payment**: REQUIRED - ticket purchase only
- **Free Option**: NONE - must pay
- **UI**: "Buy Ticket" button only
- **Tables**: "Tickets Sold" table only
- **Access**: Open to all members

### Social Events (Community Gatherings)  
- **Payment**: OPTIONAL - can RSVP for free OR buy ticket
- **Free Option**: RSVP system
- **UI**: BOTH "RSVP" button AND "Buy Ticket" button
- **Tables**: BOTH "RSVP" table AND "Tickets Sold" table
- **Access**: Vetted members only
- **RSVP Table Shows**: Member name, RSVP status, whether they also bought ticket

## Files to Clean Up

### Delete (Wrong Implementation)
- [x] `/apps/web/src/components/events/EventRegistrationModal.tsx` - DELETED
- [ ] Remove all "registration" imports from other files

### Files to Fix
1. `/apps/web/src/pages/events/EventDetailsPage.tsx`
   - Remove EventRegistrationModal import
   - Add logic to detect event type
   - Show correct UI based on event type

2. `/apps/web/src/features/events/api/mutations.ts`
   - Rename useRegisterForEvent → usePurchaseTicket
   - Rename useCancelRegistration → useCancelTicket
   - Add useRSVPForEvent
   - Add useCancelRSVP

3. `/apps/web/src/components/events/EventForm.tsx`
   - Verify eventType field is properly used
   - Ensure Tickets/Orders tab shows correct tables based on type

4. `/apps/web/src/pages/events/PublicEventsPage.tsx`
   - Fix any "registration" terminology
   - Show correct action buttons based on event type

5. `/apps/web/src/components/events/SessionFormModal.tsx`
   - Fix "registeredCount" to "ticketsSold" or appropriate term

## New Components to Create

### For Classes
1. `EventTicketPurchaseModal.tsx`
   - Ticket selection
   - Quantity selection
   - Payment method (PayPal/Venmo stubs)
   - Total calculation
   - "Purchase Tickets" confirmation

### For Social Events
1. `EventRSVPModal.tsx`
   - Simple RSVP confirmation
   - Optional ticket upgrade
   - Shows both free RSVP and paid ticket options

2. `EventRSVPTable.tsx`
   - Shows RSVP list
   - Indicates who also bought tickets
   - For admin view in Tickets/Orders tab

## Implementation Steps

### Step 1: Clean Up Wrong Code
- Delete wrong components
- Remove all "registration" terminology
- Fix mutation names

### Step 2: Create Event Type Detection
- Add eventType to all relevant components
- Create conditional rendering based on type

### Step 3: Implement Class-Specific Features
- EventTicketPurchaseModal
- Ticket purchase flow
- Tickets Sold table only

### Step 4: Implement Social Event Features
- EventRSVPModal
- RSVP flow (free)
- Optional ticket purchase
- BOTH RSVP table AND Tickets Sold table

### Step 5: Update Documentation
- Document the correct business rules
- Update test files
- Create clear separation between event types

## Prevention Measures

1. **ALWAYS read requirements first** - No coding until requirements are understood
2. **Use business-requirements agent** for clarification
3. **Check terminology** against existing documentation
4. **Test against business rules** not just technical functionality
5. **Create requirements checklist** before implementation

## Delegation Plan

### Backend Developer
- Fix mutations to support both ticket purchase and RSVP
- Ensure API distinguishes between event types
- Support both RSVP tracking and ticket sales

### React Developer  
- Create proper event-type-aware components
- Fix all UI to show correct options based on event type
- Remove all "registration" terminology

### Test Developer
- Update tests to verify Classes vs Social Events logic
- Test that Social Events show BOTH tables
- Verify terminology is correct

## Success Criteria

1. ✅ NO "registration" terminology anywhere
2. ✅ Classes show ONLY "Buy Ticket" option
3. ✅ Social Events show BOTH "RSVP" and "Buy Ticket" options
4. ✅ Social Events admin view shows BOTH tables
5. ✅ All components respect event type
6. ✅ Tests verify business rules are followed