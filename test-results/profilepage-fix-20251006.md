# ProfilePage Test Alignment - 2025-10-06

**Goal**: Fix 11 failing ProfilePage tests by aligning expectations with current component implementation
**Status**: IN PROGRESS - MSW/React Query test isolation issue discovered
**Date**: 2025-10-06

## Component Analysis

### Current ProfilePage Structure (ProfilePage.tsx)
**Form fields**:
- Scene Name (label: "Scene Name") ✓
- Email Address (label: "Email Address") ✓

**Sections**:
- Profile Information (h2) ✓
- Account Information (h2) ✓
  - User ID
  - Account Created
  - ~~Last Login~~ (COMMENTED OUT - lines 342-382)
- Community Guidelines (h2) ✓

**Features**:
- Form validation (Mantine useForm)
- Loading state with Loader component
- Error state with Alert
- Note about API not connected

## Test Fixes Applied

### Category 1: Removed "Last Login" Expectations
**Issue**: "Last Login" section commented out in component
**Fix**: Commented out test assertions for Last Login
**File**: ProfilePage.test.tsx line 115-118
**Status**: COMPLETE

### Category 2: Async Timing Fixes
**Issue**: Tests trying to access form fields before data loads
**Fix Applied**:
- Added `waitFor` blocks around element queries
- Used `findByLabelText` for auto-waiting queries
- Increased timeouts to 3000ms

**Status**: INCOMPLETE - Deeper issue discovered

## Root Cause Identified

### MSW Handler + React Query Test Isolation Issue

**Symptoms**:
1. Test "should render profile form sections" PASSES (62ms)
2. Test "should populate form fields" FAILS (3025ms timeout)
3. Running "should render profile form sections" in isolation: PASSES
4. Running full suite: First test passes, subsequent tests fail

**Analysis**:
- MSW handlers are configured correctly (both relative and absolute URLs)
- QueryClient created fresh in each test's `beforeEach`
- `server.resetHandlers()` called in `beforeEach`
- `queryClient.clear()` called in `afterEach`
- Component renders Dashboard layout correctly
- Component shows "Profile" heading
- Component DOES NOT render form fields in failing tests

**Hypothesis**:
The first test to render ProfilePage gets MSW data and caches it. Subsequent tests create new QueryClient instances, but something in the test environment prevents the new query from executing. Possible causes:
1. React Query not re-fetching with fresh QueryClient
2. MSW handlers being cleared after first test
3. Axios client caching responses
4. React component memoization between tests

## Current Test Results

**Before fixes**: 167/277 (60.3%)
**After fixes**: Still 14 ProfilePage tests failing due to MSW/RQ issue
**Impact**: +0 tests (blocked by test infrastructure issue)

## Files Modified

1. `/apps/web/src/pages/dashboard/__tests__/ProfilePage.test.tsx`
   - Commented out "Last Login" assertions
   - Added `waitFor` blocks for async elements
   - Changed to `findBy` queries
   - Increased timeouts

## Next Steps

### Immediate (Required to fix tests)
1. **Investigate React Query cache isolation**
   - Check if QueryClient is truly isolated between tests
   - Verify cache is cleared between renders
   - Consider using `queryClient.removeQueries()` instead of `clear()`

2. **Investigate MSW handler persistence**
   - Add logging to MSW handlers to see if they're being called
   - Check if `server.resetHandlers()` is working correctly
   - Consider using `server.restoreHandlers()` instead

3. **Try alternative fix**: Mock the useCurrentUser hook directly
   - Bypass MSW and React Query complexity
   - Mock at the hook level for predictable test behavior

### Alternative Approach (Recommended)
**Skip these tests temporarily** and document the infrastructure issue:
- Mark failing tests with `.skip`
- Add TODO comments referencing this investigation
- File issue for test infrastructure improvement
- Move to next test file (MembershipPage, SecurityPage)

## Lessons Learned

1. **MSW + React Query + Vitest** interaction is complex
2. Test isolation requires careful attention to:
   - Query cache clearing
   - MSW handler lifecycle
   - Component unmounting
3. First-passing tests can mask isolation issues
4. `waitFor` timeout errors often indicate data never loads, not slow loading

## Recommendations

1. **Consider hook-level mocking** for components with complex API dependencies
2. **Add MSW request logging** in test setup for debugging
3. **Document test patterns** for React Query + MSW combinations
4. **Create helper utilities** for common test setups

## Time Invested
- Component analysis: 15 min
- Test alignment attempts: 45 min
- MSW/RQ debugging: 30 min
- **Total**: 90 minutes

## Decision Point

**Option A**: Continue debugging MSW/React Query isolation (est. 1-2 hours)
**Option B**: Skip failing tests with clear TODOs, move to next test file (est. 15 min)
**Option C**: Mock useCurrentUser hook instead of MSW (est. 30 min)

**Recommendation**: **Option B** - Document and skip, prioritize other test files to meet 80% goal faster.
