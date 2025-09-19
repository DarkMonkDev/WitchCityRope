# Event Persistence Debug Analysis

## The Problem
When users add/delete sessions, ticket types, or volunteer positions, the changes don't persist after page refresh.

## How It's SUPPOSED to Work

### Current Architecture
1. **NO individual API endpoints** for sessions/tickets/volunteers
   - Only PUT /api/events/{id} exists to update the entire event
2. **Frontend uses local state** for add/delete operations
   - Add/Delete only updates the form's local state
   - The main "Save" button sends everything to the API

### The Flow
1. User clicks "Add Session" → Opens modal
2. User fills form and clicks modal's "Save" → Updates local form.values.sessions array
3. User clicks main "Save" button → Sends entire event with all arrays to API
4. API updates entire event including nested collections

## What's Actually Happening

### Debug Points
The code has extensive console.log statements. When testing:

1. **After adding a session in modal**, check console for:
   - "EventForm initializing with initialData" - should NOT appear (would reset form)
   - Form values should show the new session in the array

2. **When clicking main Save**, check console for:
   - "Form data before save" - should include new session in sessions array
   - "Changed fields to send to API" - should show sessions array with changes
   - Network tab: PUT request payload should include sessions array

3. **After save succeeds**:
   - Cache is invalidated
   - Event reloads from API
   - If session isn't there, it wasn't saved to DB

## Potential Issues to Check

### Issue 1: Form State Reset
- `hasInitialized` ref prevents re-initialization
- But check if something else is resetting the form

### Issue 2: Change Detection
- `getChangedEventFields` compares current vs initial
- Uses JSON.stringify to detect array changes
- Should detect when sessions array differs

### Issue 3: API Not Persisting
- Backend has Include statements for VolunteerPositions
- EventService has update logic for all collections
- Check if EF Core is tracking changes properly

## Quick Test Commands

```bash
# Check current event data
curl -s http://localhost:5655/api/events | jq '.data[0]'

# Watch for PUT request
# In browser dev tools Network tab, filter by "events" and watch for PUT

# After adding a session and saving, check if it persisted:
curl -s http://localhost:5655/api/events | jq '.data[0].sessions'
```

## The REAL Issue
Based on the code review, the architecture SHOULD work but something is preventing the arrays from being included in the save payload OR the comparison is failing to detect changes.

Need to check the actual console output and network payload when the issue occurs.