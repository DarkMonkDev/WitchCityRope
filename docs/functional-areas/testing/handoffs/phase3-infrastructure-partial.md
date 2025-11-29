# Phase 3 Handoff: Test Infrastructure Fixes - Partial (BLOCKED)

**Date**: 2025-11-29
**Phase**: 3 - Test Infrastructure Standardization
**Status**: BLOCKED by critical application bug
**Next Phase**: Fix application bug, then resume Phase 3

## Summary

Fixed test infrastructure networkidle timeout issue. Discovered CRITICAL APPLICATION BUG in App.tsx causing infinite render loop that blocks ALL tests from running.

## Metrics

| Metric | Before | After | Status |
|--------|--------|-------|--------|
| Test Files | 124 | 124 | No change |
| Infrastructure Fix | networkidle timeouts | ✅ FIXED | networkidle → domcontentloaded |
| Tests Executable | 0% | 0% | ❌ BLOCKED by App.tsx bug |

## Work Completed

### 1. networkidle Wait Strategy Fixed ✅

**Problem**: All authentication-dependent tests failing with 30-second timeouts on `waitForLoadState('networkidle')`.

**Root Cause**: App has continuous background requests (polling, analytics) preventing network from ever becoming "idle".

**Solution**: Replaced all `networkidle` with `domcontentloaded` in auth helpers.

**File Modified**: `/tests/e2e/test-utils/helpers/auth.helpers.ts`

**Changes Made** (7 instances):
- Line 36: `loginAs()` - navigate to login
- Line 47: `loginAs()` - wait after login
- Line 59: `loginWith()` - navigate to login
- Line 70: `loginWith()` - wait after login
- Line 80: `loginExpectingError()` - navigate to login
- Line 167: `clearAuthState()` - navigate to login
- Line 306: `waitForDashboardReady()` - wait for dashboard

**Pattern Applied**:
```typescript
// ❌ BEFORE - Caused 30s timeouts
await page.waitForLoadState('networkidle');

// ✅ AFTER - Works with background requests
await page.waitForLoadState('domcontentloaded');
```

### 2. clearAuthState Improved ✅

**Problem**: `page.evaluate()` could hang if page not fully loaded.

**Solution**: Added error handling and timeout protection.

**Changes**:
- Use `waitUntil: 'domcontentloaded'` in `page.goto()`
- Wrap storage clearing in `.catch()` to prevent hanging
- Non-critical failures logged but don't block tests

## ❌ CRITICAL APPLICATION BUG DISCOVERED (BLOCKING)

### Bug Details

**Location**: `/apps/web/src/App.tsx` lines 45-49

**Error**:
```
Warning: Maximum update depth exceeded. This can happen when a component
calls setState inside useEffect, but useEffect either doesn't have a
dependency array, or one of the dependencies changes on every render.
at App (http://localhost:5173/src/App.tsx:28:25)
```

**Root Cause**: Infinite render loop in CSRF initialization useEffect

```typescript
// ❌ WRONG CODE (current - BROKEN)
useEffect(() => {
  debugLog('🔍 App.tsx: Initializing CSRF protection via store...');
  csrfStore.initialize();
}, [csrfStore]); // BUG: csrfStore is new object every render!
```

**Why This Breaks**:
1. `csrfStore` created via `useCSRFStore()` - new object reference each render
2. useEffect runs because dependency `csrfStore` changed
3. `csrfStore.initialize()` updates state
4. Component re-renders → new `csrfStore` object
5. Infinite loop → React crashes

**Impact**:
- **100% of tests blocked** - cannot even load login page
- Application completely unusable in dev environment
- Caused by Phase 2 "CSRF fix" attempt
- Same issue exists on lines 52-57 for authenticated CSRF refresh

### Required Fix

**Assigned To**: react-developer (application code bug, not test infrastructure)

**Fix Required in `/apps/web/src/App.tsx`**:

```typescript
// ✅ CORRECT - Line 45-49
useEffect(() => {
  debugLog('🔍 App.tsx: Initializing CSRF protection via store...');
  csrfStore.initialize();
}, []); // Empty array - run only on mount, csrfStore doesn't need to be dependency

// ✅ CORRECT - Line 52-57
useEffect(() => {
  if (isAuthenticated) {
    debugLog('🔍 App.tsx: User authenticated, refreshing CSRF protection...');
    csrfStore.initialize();
  }
}, [isAuthenticated]); // Only re-run when auth status changes, NOT when csrfStore changes
```

**Explanation**:
- `csrfStore` is a Zustand store - it's stable across renders at the data level
- We don't need `csrfStore` as a dependency because we're calling a method, not reading state
- Only `isAuthenticated` should trigger CSRF refresh, not store object changes

## Test Execution Results

### Before networkidle Fix
```
✘ ALL tests fail with networkidle timeout (30 seconds each)
Pattern: "page.waitForLoadState: Timeout 30000ms exceeded"
Location: AuthHelpers.loginAs() → clearAuthState() → networkidle wait
```

### After networkidle Fix
```
❌ BLOCKED: Application broken by infinite render loop
Error: "Maximum update depth exceeded"
Location: App.tsx line 28 (CSRF useEffect)
Impact: Tests cannot even load login page
```

### Current Blocker

**Cannot proceed with test infrastructure fixes until App.tsx bug is fixed.**

Tests are now properly configured but the application code itself is broken.

## Recommendations for Next Phase

### IMMEDIATE (CRITICAL)

**react-developer MUST fix App.tsx bug before ANY testing can resume**:
1. Remove `csrfStore` from dependency array on line 49
2. Remove `csrfStore` from dependency array on line 57
3. Rebuild containers: `./dev.sh`
4. Verify app loads without infinite loop
5. Run simple test to confirm fix

### AFTER App Bug Fixed

Resume Phase 3 test infrastructure work:

1. **Authentication Standardization** (HIGH PRIORITY)
   - Audit tests NOT using `AuthHelpers.loginAs()`
   - Replace ad-hoc login code with standardized helper
   - Verify CSRF token handling works after App.tsx fix

2. **Wait Strategy Standardization** (MEDIUM PRIORITY)
   - Search for remaining `networkidle` in test files (not just helpers)
   - Replace arbitrary `page.waitForTimeout()` with proper selectors
   - Use `WaitHelpers` consistently

3. **Selector Updates** (MEDIUM PRIORITY)
   - Find stale selectors that don't match current UI
   - Update to use `data-testid` attributes where available
   - Fix hardcoded text selectors

## Files Modified

1. `/tests/e2e/test-utils/helpers/auth.helpers.ts`
   - Fixed 7 instances of `networkidle` → `domcontentloaded`
   - Improved error handling in `clearAuthState()`

## Application Bugs Discovered

1. **App.tsx Infinite Render Loop** (CRITICAL - BLOCKING)
   - File: `/apps/web/src/App.tsx` lines 45-49, 52-57
   - Severity: P0 - Blocks all development and testing
   - Assigned: react-developer
   - Fix: Remove `csrfStore` from useEffect dependency arrays

## Next Steps

1. ✅ **react-developer**: Fix App.tsx infinite render loop (URGENT - BLOCKING)
2. ⏸️ **test-developer**: Resume Phase 3 after App.tsx fixed
3. ⏸️ **Update TEST_CATALOG**: After tests can run, update pass/fail metrics

## Status: ⚠️ BLOCKED - Waiting for react-developer to fix App.tsx

**Conclusion**: Test infrastructure networkidle issue is FIXED. However, discovered that Phase 2 "CSRF fix" actually BROKE the application with an infinite render loop. All testing is blocked until application code is fixed by react-developer.

**Handoff To**: react-developer (URGENT bug fix required)
**Blocked On**: App.tsx infinite render loop fix
**Resume When**: Application loads without crashing
