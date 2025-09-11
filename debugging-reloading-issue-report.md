# Events Management API Demo Reloading Issue - Debugging Report

**Date**: 2025-09-06  
**Issue**: Page at `/admin/events-management-api-demo` constantly reloading  
**Working Directory**: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`  

## Problem Summary
The Events Management API Demo page was experiencing constant reloading, making it unusable for development and testing.

## Debugging Strategy Implemented

### 1. Component Isolation
Created progressively simpler versions of the component to isolate the cause:

**Files Created:**
- `/apps/web/src/pages/admin/EventsManagementApiDemo.minimal.tsx` - Mantine-based minimal version
- `/apps/web/src/pages/admin/EventsManagementApiDemo.test.tsx` - Ultra-minimal HTML/CSS only version

### 2. Layout Bypass Testing
Created a route that bypasses RootLayout entirely:
- Route: `/test-no-layout` - Renders component without Navigation/UtilityBar components

### 3. Current Test Status

**Available Test Routes:**
1. **http://localhost:5174/admin/events-management-api-demo** - Original route with RootLayout
2. **http://localhost:5174/test-no-layout** - Route without RootLayout

**Components Available for Testing:**
- `EventsManagementApiDemoTest` - Ultra minimal (currently active)
- `EventsManagementApiDemoMinimal` - Mantine-based minimal 
- `EventsManagementApiDemo` - Original full component

## Potential Root Causes Identified

### 1. Auth Store Issues (HIGH PROBABILITY)
**Component**: `/src/components/layout/Navigation.tsx`
**Issue**: Uses Zustand auth hooks (`useUser`, `useIsAuthenticated`, `useAuthActions`)
**Symptoms**: 
- Auth store has `checkAuth()` function that runs on app mount
- Persistent storage could be causing state conflicts
- Token expiration checks might trigger re-renders

### 2. API Call Issues (MEDIUM PROBABILITY)
**Component**: Original `EventsManagementApiDemo.tsx`
**Issue**: TanStack Query hooks with aggressive refetch settings
**Symptoms**:
- Legacy API calls to `/api/events`
- Query retries and error states could trigger component remounts

### 3. Router Configuration (LOW PROBABILITY)
**Component**: `/src/routes/router.tsx`
**Issue**: Route loader or error boundary issues
**Symptoms**: Routes serve properly via curl, unlikely to be router issue

## Next Steps for User

### IMMEDIATE TESTING (Do These First)

1. **Test the ultra-minimal component** at http://localhost:5174/test-no-layout
   - If this reloads: Issue is NOT in component (likely App.tsx, auth store, or environment)  
   - If this is stable: Issue is in RootLayout components (Navigation/UtilityBar)

2. **Test with RootLayout** at http://localhost:5174/admin/events-management-api-demo  
   - Compare behavior with the no-layout version
   - If this reloads but no-layout is stable: Issue is in Navigation/UtilityBar components

### SYSTEMATIC DEBUGGING

If the minimal component is stable, gradually add back features:

1. **Switch to minimal Mantine version**:
   ```typescript
   // In router.tsx, change:
   element: <EventsManagementApiDemoMinimal />
   ```

2. **Add back API calls one by one**:
   - First add just the legacy events API
   - Then add event details API
   - Monitor console logs for when reloading starts

3. **Check auth store behavior**:
   - Open browser dev tools
   - Watch for auth-related console logs
   - Check for repeated `checkAuth()` calls

### DIAGNOSTIC COMMANDS

```bash
# Check dev server logs for errors
# (Use browser dev tools for client-side errors)

# Monitor network requests
# Open browser dev tools > Network tab

# Check for memory leaks
# Open browser dev tools > Memory tab
```

## Files Modified for Testing

**Router Updated**: `/apps/web/src/routes/router.tsx`
- Added test route `/test-no-layout`
- Temporarily using ultra-minimal component

**Test Components Created**:
- `EventsManagementApiDemo.test.tsx` - Pure HTML/CSS component
- `EventsManagementApiDemo.minimal.tsx` - Mantine-based component

## Hypothesis Ranking

1. **Auth Store/Navigation Component** (85% confidence)
   - Navigation uses auth hooks that trigger on state changes
   - Auth store has persistent storage and async operations
   - Scroll listener in Navigation could be causing issues

2. **API Calls/TanStack Query** (10% confidence)
   - Query retries or error states causing component remounts
   - Less likely since minimal versions should isolate this

3. **Environment/Hot Reload** (5% confidence)  
   - Vite HMR issues or port conflicts
   - Less likely since other pages work fine

## Expected Resolution

Most likely the issue will be resolved by:
1. Fixing auth store state management
2. Optimizing Navigation component re-renders  
3. Adjusting TanStack Query settings to prevent aggressive refetching

The systematic testing approach should pinpoint the exact cause within 10-15 minutes of browser testing.