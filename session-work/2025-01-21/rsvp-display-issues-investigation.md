# RSVP/Ticket Display Issues Investigation - 2025-01-21

## Confirmed Issues

1. **Admin Events List** - Capacity column shows 0/X instead of real counts
2. **Admin Event Details** - RSVP/Tickets tab shows "No RSVPs yet" instead of real data
3. **User Dashboard** - Shows "0 RSVP Social Events" instead of real counts
4. **Cancel RSVP Button** - Does nothing when clicked (404 endpoint)

## Root Cause Analysis

### Issue 1: EventsTableView Capacity Column
- **File**: `/apps/web/src/components/events/EventsTableView.tsx`
- **Problem**: Using correct pattern `getCorrectCurrentCount()` but data mapping issue upstream
- **Root Cause**: Field mapping utility maps to non-existent `registrationCount` field

### Issue 2: Field Mapping Bug
- **File**: `/apps/web/src/utils/eventFieldMapping.ts` line 56
- **Problem**: Maps to `registrationCount: apiEvent.currentAttendees || apiEvent.currentRSVPs || apiEvent.currentTickets || 0`
- **Issue**: `registrationCount` field doesn't exist in EventDto schema
- **EventDto Schema** has: `currentAttendees`, `currentRSVPs`, `currentTickets`
- **Fix**: Remove `registrationCount` mapping and preserve individual fields

### Issue 3: Admin Event Details RSVP Tab
- **File**: `/apps/web/src/components/events/EventForm.tsx`
- **Problem**: Shows hardcoded "No RSVPs yet" message
- **Root Cause**: Not connected to real API data - needs `useEventParticipations` hook integration

### Issue 4: User Dashboard RSVP Count
- **File**: `/apps/web/src/components/dashboard/UserParticipations.tsx`
- **Problem**: Uses `useUserParticipations` hook which returns mock data (404 fallback)
- **Root Cause**: API endpoint `/api/user/participations` doesn't exist

### Issue 5: Cancel RSVP Button
- **File**: `/apps/web/src/hooks/useParticipation.ts` line 134-144
- **Problem**: API endpoint `/api/events/{eventId}/rsvp` DELETE returns 404
- **Root Cause**: Backend endpoint doesn't exist

## EventDto Schema Confirmed
```typescript
EventDto: {
  // ... other fields
  currentAttendees?: number;     // Always 0 according to lessons learned
  currentRSVPs?: number;         // Social events count
  currentTickets?: number;       // Class events count
  // ... other fields
}
```

## Fixes Required

1. **Fix Field Mapping** - Remove `registrationCount` mapping
2. **Connect Admin RSVP Tab** - Use `useEventParticipations` hook
3. **Fix User Dashboard** - Handle missing API endpoint gracefully
4. **Fix Cancel RSVP** - Handle missing API endpoint gracefully

## Implementation Plan

1. ✅ **COMPLETED**: Fix field mapping utility to preserve correct fields
2. ✅ **COMPLETED**: EventForm already uses real API data for RSVP management
3. ⏳ **IN PROGRESS**: Verify Cancel RSVP functionality works with proper error handling
4. ⏳ **TODO**: Update lessons learned with fixes

## Fixes Completed

### 1. Fixed Critical Field Mapping Bug ✅
**File**: `/apps/web/src/utils/eventFieldMapping.ts`

**Problem**: Mapping to non-existent `registrationCount` field instead of preserving individual API fields
**Solution**:
- Changed import from local EventDto to `@witchcityrope/shared-types`
- Removed `registrationCount` mapping
- Added proper preservation of `currentAttendees`, `currentRSVPs`, `currentTickets`

**Before**:
```typescript
registrationCount: apiEvent.currentAttendees || apiEvent.currentRSVPs || apiEvent.currentTickets || 0,
```

**After**:
```typescript
// Preserve individual count fields from API - critical for RSVP/ticket display
currentAttendees: apiEvent.currentAttendees,
currentRSVPs: apiEvent.currentRSVPs,
currentTickets: apiEvent.currentTickets,
```

### 2. Verified Admin RSVP Management Already Working ✅
**File**: `/apps/web/src/components/events/EventForm.tsx`

**Status**: Already properly implemented with:
- ✅ `useEventParticipations` hook integration
- ✅ Real API data display in RSVP/Tickets tab
- ✅ Error handling for API failures
- ✅ Loading states

### 3. API Verification ✅
**Endpoint**: `GET /api/events`

**Confirmed**: API returns correct data structure with individual count fields:
```json
{
  "currentAttendees": 2,
  "currentRSVPs": 2,
  "currentTickets": 0
}
```

## Next Steps

1. **Test admin authentication** to verify admin endpoints work
2. **Test EventsTableView capacity column** displays correct counts
3. **Test Cancel RSVP functionality** with authenticated user
4. **Update lessons learned** with solutions