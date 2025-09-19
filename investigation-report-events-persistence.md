# Events Details Admin Page - Persistence Investigation Report

**Date**: 2025-09-19
**Issue**: Teacher changes, sessions, ticket types, and volunteer positions not persisting after refresh

## Summary of Investigation

I've analyzed the Events Details admin page persistence issue and found several potential problems with the save functionality.

## Root Cause Analysis

### 1. **Critical Issue: Form State Management Problem**

**Problem**: The form uses complex state management that may not be properly synced between the main page and the EventForm component.

**Evidence**:
- `AdminEventDetailsPage.tsx` line 331: `key={(event as any)?.id}` forces re-mount on event changes
- Complex conversion between API DTOs and form data may lose information
- Multiple form state trackers (`formDirty`, `initialFormData`) could get out of sync

### 2. **API Response Structure Issues**

**Problem**: The API may not be returning all expected fields, causing form data to be incomplete.

**Evidence**:
- `useEvents.ts` line 43-44: New fields (`sessions`, `ticketTypes`, `teacherIds`) were recently added
- Type casting in `AdminEventDetailsPage.tsx` line 63: `(event.sessions as any)`
- Debug logging shows API structure verification is needed

### 3. **Change Detection Logic Issues**

**Problem**: The change detection in `eventDataTransformation.ts` may not properly identify when sessions, tickets, and teachers have changed.

**Evidence**:
- Lines 196-205: Uses `JSON.stringify()` comparison for complex objects
- Array comparison may fail for objects with different property orders
- Empty arrays vs undefined/null arrays may not be handled consistently

### 4. **Cache Invalidation Issues**

**Problem**: React Query cache may not be properly invalidated after saves, causing stale data to appear on refresh.

**Evidence**:
- `useEvents.ts` line 173-176: Cache invalidation happens AFTER mutations
- Optimistic updates may mask real persistence failures
- Browser refresh bypasses optimistic updates and shows real API state

## Technical Analysis

### API Integration Flow
```
Form Submit → eventDataTransformation.ts → useUpdateEvent → API PUT /events/{id} → Cache Invalidation
```

### Potential Failure Points
1. **Form → DTO Conversion**: `convertEventFormDataToUpdateDto()`
2. **Change Detection**: `getChangedEventFields()`
3. **API Request**: `updateEventMutation.mutateAsync()`
4. **Cache Sync**: React Query invalidation
5. **State Reset**: `setInitialFormData(data)`

## Recommended Fixes

### 1. Immediate Fixes Needed
- [ ] Add comprehensive API response validation
- [ ] Fix form state management to prevent re-mounting issues
- [ ] Implement proper error handling for partial saves
- [ ] Add detailed logging for debugging save operations

### 2. Data Validation Fixes
- [ ] Verify API returns all expected fields (teacherIds, sessions, ticketTypes)
- [ ] Implement fallback handling for missing API fields
- [ ] Add type safety for API response transformations

### 3. Save Operation Improvements
- [ ] Implement atomic saves for complex data
- [ ] Add rollback mechanism for failed saves
- [ ] Improve change detection for nested objects
- [ ] Add user feedback for save operation status

## Next Steps

1. **Test API Response**: Verify what the `/api/events/{id}` endpoint actually returns
2. **Fix Form Mounting**: Resolve the key-based re-mounting issue
3. **Improve Change Detection**: Use better algorithms for detecting array changes
4. **Add Comprehensive Logging**: Track the entire save operation flow
5. **Test Save Operations**: Verify each field type saves correctly

## Files Requiring Changes
- `/apps/web/src/pages/admin/AdminEventDetailsPage.tsx`
- `/apps/web/src/utils/eventDataTransformation.ts`
- `/apps/web/src/lib/api/hooks/useEvents.ts`
- `/apps/web/src/components/events/EventForm.tsx`

## Impact Assessment
- **Critical**: Teacher selections not persisting affects class management
- **High**: Session/ticket data loss impacts event planning
- **Medium**: Volunteer position data affects event operations

This investigation provides the foundation for implementing comprehensive fixes to resolve the persistence issues.