# MSW Handler Fixes - Quick Reference

**Date**: 2025-10-06
**Task**: Fix MSW mock handlers to match actual backend API responses

## Problem
Components stuck in React Query loading state indefinitely during tests because MSW mock responses didn't match backend API structure.

## Solution
Verified actual backend API responses via curl and updated MSW handlers to match exactly.

## Critical Fixes Made

### 1. `/api/dashboard` - Returns Minimal Dashboard DTO
```typescript
// ❌ BEFORE - Full UserDto
{ id, email, sceneName, firstName, lastName, roles, isActive, createdAt, ... }

// ✅ AFTER - Minimal Dashboard DTO
{ sceneName, role, vettingStatus, hasVettingApplication, isVetted, email, joinDate, pronouns }
```

### 2. `/api/dashboard/events` - Returns Dashboard Event DTO
```typescript
// ❌ BEFORE - Full Event objects
{ id, title, description, startDate, endDate, maxAttendees, currentAttendees, instructorId, ... }

// ✅ AFTER - Dashboard Event DTO with registration info
{ id, title, startDate, endDate, location, eventType, instructorName, registrationStatus, ticketId, confirmationCode }
```

### 3. `/api/dashboard/statistics` - Returns User Statistics
```typescript
// ❌ BEFORE - Organization-wide stats
{ upcomingEvents, totalRegistrations, activeMembers }

// ✅ AFTER - User-centric statistics
{ isVerified, eventsAttended, monthsAsMember, recentEvents, joinDate, vettingStatus, nextInterviewDate, upcomingRegistrations, cancelledRegistrations }
```

## Impact
- ✅ Components now render actual content (no longer stuck in loading)
- ✅ React Query hooks transition to success state correctly
- ✅ Test count unchanged (156/277) but infrastructure issue resolved

## What This Enables
Now that components render, we can:
1. Fix test expectations to match actual component output
2. Investigate child component rendering issues
3. Verify data flows correctly through component tree

## Files Modified
- `/apps/web/src/test/mocks/handlers.ts` - Updated 3 endpoint handlers

## Documentation
Full details: `/test-results/msw-handler-response-structure-fixes-20251006.md`

---
**Key Insight**: MSW responses MUST match backend API exactly, including:
- Response wrapper structure (or lack thereof)
- Property names and casing
- Data types (string vs number for vettingStatus!)
- Nested object structure
