# MSW Handler Field Name Updates

**Date**: 2025-10-06
**Time**: 23:46
**Goal**: Align MSW mock data with regenerated TypeScript types from shared-types package

## Context

Backend DTOs were standardized and frontend types regenerated using NSwag/OpenAPI. MSW handlers needed updating to match the new field names to prevent test failures.

## Key Field Name Changes

### EventDto (from generated types)
- `capacity` - CORRECT (not `maxAttendees`)
- `registrationCount` - NEW primary field for registration count
- `currentAttendees` - Kept for backward compatibility during transition
- `startDate` / `endDate` - CORRECT (not `startDateTime` / `endDateTime`)
- `location` - Required field

### SessionDto (from generated types)
- `capacity` - CORRECT
- `registrationCount` - NEW primary field
- `registeredCount` - Kept for backward compatibility

## Changes Made

### Event Mocks Updated

**File**: `/home/chad/repos/witchcityrope/apps/web/src/test/mocks/handlers.ts`

Updated 8 occurrences in the following handlers:
1. `GET /api/events/:id` (relative URL) - 2 event objects
2. `GET ${API_BASE_URL}/api/events/:id` (absolute URL) - 2 event objects
3. `GET /api/events` (list, relative URL) - 2 event objects
4. `GET ${API_BASE_URL}/api/events` (list, absolute URL) - 2 event objects
5. `POST /api/events` (create) - 1 event object
6. `PUT /api/events/:id` (update) - 1 event object

**Changes per event object**:
```typescript
// BEFORE
{
  capacity: 20,
  currentAttendees: 5,
  // other fields...
}

// AFTER
{
  capacity: 20,
  registrationCount: 5,        // NEW primary field
  currentAttendees: 5,         // Kept for backward compatibility
  // other fields...
}
```

### Verification

```bash
# Verified no old field names remain
grep -n "maxAttendees\|startDateTime\|endDateTime" handlers.ts
# Result: No matches (✓)
```

## Test Results

**Before**: 158/277 passing (57.0%)
**After**: 158/277 passing (57.0%)
**Tests Fixed**: 0 (no regression, maintaining baseline)

## Notes

1. **No Breaking Changes**: Added `registrationCount` alongside existing `currentAttendees` for gradual migration
2. **Field Name Consistency**: All handlers now use `capacity` (not `maxAttendees`)
3. **Location Field**: Added to all event mocks (required by API)
4. **Backward Compatibility**: Kept `currentAttendees` to support components during migration period

## Next Steps

1. **Component Migration**: Update components to use `registrationCount` instead of `currentAttendees`
2. **Test Migration**: Update test files that still reference `maxAttendees`:
   - `/src/pages/dashboard/__tests__/EventsPage.test.tsx` (15 occurrences)
   - `/src/test/integration/dashboard-integration.test.tsx` (4 occurrences)
3. **Remove Backward Compatibility**: Once all components/tests migrated, remove `currentAttendees` field

## Files Modified

1. `/home/chad/repos/witchcityrope/apps/web/src/test/mocks/handlers.ts` - 8 event objects updated
2. `/home/chad/repos/witchcityrope/apps/web/src/lib/api/hooks/useEvents.ts` - Updated field mapping logic:
   - Added `registrationCount` to ApiEvent interface
   - Updated mapping to prefer `registrationCount` from API, with fallback to legacy fields
   - Line 127: `registrationCount: apiEvent.registrationCount || apiEvent.currentAttendees || ...`

## Field Name Reference

### Correct Field Names (per generated types)
- ✅ `capacity` (event and session capacity)
- ✅ `registrationCount` (current registrations)
- ✅ `startDate` / `endDate` (event dates)
- ✅ `location` (event location)

### Deprecated Field Names (remove after migration)
- ⚠️ `currentAttendees` (use `registrationCount` instead)
- ❌ `maxAttendees` (use `capacity` instead)
- ❌ `startDateTime` / `endDateTime` (use `startDate` / `endDate`)
- ⚠️ `registeredCount` (sessions only, use `registrationCount`)

## Generated Types Location

Source of truth: `/home/chad/repos/witchcityrope/packages/shared-types/src/generated/api-types.ts`

- EventDto: Line 1872
- SessionDto: Line 2354
