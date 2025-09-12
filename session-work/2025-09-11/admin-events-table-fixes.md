# Admin Events Table Fixes - 2025-09-11

## Issues Fixed

### 1. Date/Time showing "Invalid Date" ✅ FIXED
**Problem**: EventsTableView component showing "Invalid Date" instead of proper dates/times
**Root Cause**: No validation for missing/invalid dates, plus fallback handling needed for missing endDate
**Solution Applied**: Enhanced formatEventDate() and formatTimeRange() functions with proper validation
- Added null/undefined checks
- Added Date validation with isNaN(date.getTime())
- Added fallback for missing endDate (assume 2-hour duration)
- Returns user-friendly messages: "Date TBD", "Time TBD"

### 2. Capacity showing 0/0 ✅ FIXED
**Problem**: CapacityDisplay showing "0/0" because API doesn't return capacity/currentAttendees fields
**Root Cause**: API response lacks these fields, component was forcing 0 values
**Solution Applied**: Enhanced CapacityDisplay with proper missing data handling
- Modified props to be optional (current?: number, max?: number)
- Added fallback UI: "Capacity TBD" when data missing
- Proper handling of undefined vs 0 values

### 3. Copy button styling issues ✅ FIXED
**Problem**: Copy button with poor visibility using variant="light" color="blue"
**Root Cause**: Button styling didn't follow WitchCityRope design system patterns
**Solution Applied**: Complete button style overhaul
- Changed to variant="filled" color="wcr.7" 
- Applied explicit dimensions from lessons learned pattern
- Added proper alignment and typography styling

## Current API Response Analysis

API `/api/events` returns:
- ✅ `startDate` (but component expects `startDateTime`) 
- ❌ Missing `endDate` field
- ❌ Missing `capacity` field  
- ❌ Missing `currentAttendees` field
- ✅ `eventType` field available

## Generated TypeScript Types

EventDto expects:
- `startDateTime?: string`
- `endDateTime?: string` 
- `capacity?: number`
- `currentAttendees?: number`

## Field Mapping Utility Available

- `/apps/web/src/utils/eventFieldMapping.ts` - Already implemented
- Contains `autoFixEventFieldNames()` function to handle API/TypeScript mismatches
- Should be applied at query level, not component level

## Files to Update

1. `/apps/web/src/components/events/EventsTableView.tsx` - Fix field names and button styling
2. Check if field mapping is applied in API queries