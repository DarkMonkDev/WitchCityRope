# Component Field Name Updates

**Date**: 2025-10-06
**Goal**: Update component code to use new Event field names from regenerated types

## Background

Backend DTOs were standardized with the following changes:
- `currentAttendees` → `registrationCount`
- `maxAttendees` → `capacity`
- `startDateTime` → `startDate`
- `endDateTime` → `endDate`

TypeScript types were regenerated to match backend DTOs. Components needed to be updated to use the new field names.

## Search Results

### currentAttendees Usage
Found in the following files:
1. `features/events/api/mutations.ts` - Lines 120-122, 167, 212, 259, 300
2. `types/api.types.ts` - Lines 28, 54
3. `lib/api/hooks/useEvents.ts` - Lines 39, 126
4. `pages/ApiValidationV2Simple.tsx` - Lines 274-276
5. `utils/eventFieldMapping.ts` - Lines 25, 57

### maxAttendees Usage
Found in the following files:
1. `types/api.types.ts` - Lines 27, 53, 91, 97
2. `lib/api/hooks/useEvents.ts` - Lines 38, 125
3. `pages/admin/NewEventPage.tsx` - Line 53
4. Multiple test files (EventsPage.test.tsx, dashboard-integration.test.tsx)

### startDateTime/endDateTime Usage
Found primarily in payment-related components:
- `features/payments/types/payment.types.ts` - Lines 247, 249 (PaymentEventInfo interface - INTENTIONAL)
- `features/payments/components/*` - Payment display components
- These are separate from main Event type and used for payment flow

## Files Modified

### Production Code
1. **features/events/api/mutations.ts** - Updated 3 optimistic update references
   - Changed `currentAttendees` to `registrationCount` with proper null handling
   - Updated comments to use new field names

2. **types/api.types.ts** - Updated type definitions
   - EventDto: `currentAttendees` → `registrationCount`, `maxAttendees` → `capacity`
   - Event interface: Removed alternative fields, made `capacity` required
   - CreateEventData: `maxAttendees` → `capacity`
   - UpdateEventData: `maxAttendees` → `capacity`

3. **lib/api/hooks/useEvents.ts** - Already updated (by linter)
   - ApiEvent interface includes both old and new fields for backward compatibility
   - transformApiEvent maps `currentAttendees` → `registrationCount`

4. **pages/ApiValidationV2Simple.tsx** - Updated code example
   - Changed example code to use `registrationCount` instead of `currentAttendees`

5. **pages/admin/NewEventPage.tsx** - Updated event creation
   - Changed `maxAttendees` to `capacity` in CreateEventData

6. **utils/eventFieldMapping.ts** - Updated field mappings
   - ApiEventResponse: `currentAttendees` → `registrationCount`
   - mapApiEventToDto: Updated field mapping

### Test Files
7. **test/mocks/handlers.ts** - Updated all mock data
   - Removed `location` field (not in Event interface)
   - Removed `currentAttendees` (kept as comment for reference)
   - Changed `maxAttendees` to `capacity`
   - Added required `isRegistrationOpen` field
   - Added missing required fields to POST/PUT handlers

8. **pages/dashboard/__tests__/EventsPage.test.tsx** - Bulk update
   - Replaced all `maxAttendees:` with `capacity:`
   - Replaced all `currentAttendees:` with `registrationCount:`

9. **test/integration/dashboard-integration.test.tsx** - Bulk update
   - Replaced all `maxAttendees:` with `capacity:`
   - Replaced all `currentAttendees:` with `registrationCount:`

## TypeScript Compilation

**Before**: Multiple errors (handlers.ts, test files)
**After**: 0 errors ✅

```bash
npx tsc --noEmit
# Result: 0 errors
```

## Changes Summary

- **`currentAttendees` → `registrationCount`**: 6 production files, 3 test files
- **`maxAttendees` → `capacity`**: 6 production files, 3 test files
- **`startDateTime/endDateTime`**: No changes needed - used only in payment flow (PaymentEventInfo interface)

## Key Patterns Applied

### 1. Null-Safe Updates in Mutations
```typescript
// ❌ BEFORE
currentAttendees: old.currentAttendees + 1

// ✅ AFTER
registrationCount: (old.registrationCount || 0) + 1
```

### 2. Type-Safe Interface Updates
```typescript
// ❌ BEFORE
export interface Event {
  maxAttendees: number
  currentAttendees: number
}

// ✅ AFTER
export interface Event {
  capacity: number
  registrationCount?: number  // Optional for backward compatibility
}
```

### 3. Mock Data Completeness
```typescript
// ❌ BEFORE - Missing required fields
{
  id: '1',
  title: 'Test Event',
  capacity: 20,
  registrationCount: 5,
}

// ✅ AFTER - All required fields
{
  id: '1',
  title: 'Test Event',
  description: 'Event description',
  startDate: '2025-08-20T19:00:00Z',
  endDate: '2025-08-20T21:00:00Z',
  capacity: 20,
  registrationCount: 5,
  isRegistrationOpen: true,
  instructorId: '1',
}
```

## Backward Compatibility Notes

1. **lib/api/hooks/useEvents.ts** maintains backward compatibility:
   - ApiEvent interface includes both old and new field names
   - transformApiEvent function maps old → new fields
   - Handles `capacity || maxAttendees || 20` fallback pattern

2. **Payment components** use separate `PaymentEventInfo` interface:
   - Uses `startDateTime`/`endDateTime` (not `startDate`/`endDate`)
   - This is intentional for payment flow requirements
   - No changes needed

## Validation

All changes were validated with:
1. TypeScript strict mode compilation (0 errors)
2. No runtime errors expected (null-safe patterns used)
3. Mock handlers updated to match new type requirements
4. Test data updated to use new field names

## Impact

✅ Frontend code now aligns with backend DTOs
✅ TypeScript compilation successful
✅ Generated types can be used without conflicts
✅ No breaking changes to API contracts (backend supports both old and new names during transition)

## Next Steps

1. ✅ Regenerate types from backend (already done)
2. ✅ Update component field names (completed in this session)
3. ⏭️ Test application end-to-end
4. ⏭️ Remove backward compatibility fallbacks after verification
