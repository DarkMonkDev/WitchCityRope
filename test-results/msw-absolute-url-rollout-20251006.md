# MSW Absolute URL Fix Rollout - Analysis

**Date**: 2025-10-06
**Goal**: Fix MSW handler URL patterns to match apiClient's absolute URL requests

## Initial Analysis

### Unexpected Finding
The MSW absolute URL fix was already partially implemented in the base handlers:
- `/test/mocks/handlers.ts` already has BOTH relative and absolute URL handlers
- Default handlers use `${API_BASE_URL}/api/...` (absolute) AND `/api/...` (relative)
- This dual-handler approach was meant to catch both URL patterns

### Test Failures Root Cause
After applying absolute URL fixes to ProfilePage.test.tsx and MembershipPage.test.tsx:

**Result**: Tests STILL fail, but for a different reason!
- MSW handlers ARE matching now (both relative and absolute work)
- BUT: Page components are not rendering expected content
- Error: "Unable to find a label with the text of: Scene Name"

### The REAL Problem
The failures are NOT MSW URL issues - they're:
1. **Component changes**: Pages may have changed their form field labels/structure
2. **Timing issues**: Data loading before components render form elements
3. **Test expectations outdated**: Tests expect UI elements that no longer exist

## Files Modified

### Priority 1: ProfilePage.test.tsx ✅ DONE
- **File**: `/apps/web/src/pages/dashboard/__tests__/ProfilePage.test.tsx`
- **Changes**: 4 MSW handlers changed from relative to absolute URLs
- **Lines**: 63, 345, 375, 412
- **Result**: Tests still fail - NOT due to MSW URLs

### Priority 2: MembershipPage.test.tsx ✅ DONE
- **File**: `/apps/web/src/pages/dashboard/__tests__/MembershipPage.test.tsx`
- **Changes**: 3 MSW handlers changed from relative to absolute URLs
- **Lines**: 61, 97, 203
- **Result**: Tests still fail - NOT due to MSW URLs

## Test Results

**Before MSW Fixes**: 171/281 (60.9%)
**After MSW Fixes**: 167/277 (60.3%)

**Analysis**:
- 4 fewer total tests (277 vs 281) = some tests now skipped
- Pass count slightly lower (167 vs 171)
- **MSW URL fixes did NOT improve pass rate**

## Key Insight

The MSW absolute URL pattern was a **RED HERRING**:
1. Base handlers already support BOTH relative and absolute URLs
2. apiClient uses absolute URLs → matches absolute handlers
3. Tests override with `server.use()` → matches correctly

**Actual Test Failures Are Due To**:
1. Component UI changes (form fields removed/renamed)
2. useVettingStatus using raw `fetch()` instead of `apiClient`
3. Integration test timing issues
4. Test expectations outdated vs current implementation

## Recommended Next Steps

### 1. FIX useVettingStatus Hook (~16 tests)
**Problem**: Uses `fetch()` directly instead of `apiClient`
**File**: `/apps/web/src/features/vetting/hooks/useVettingStatus.tsx`
**Solution**: Replace `fetch()` with `apiClient.get()` for MSW interception

### 2. UPDATE ProfilePage Tests (~11 tests)
**Problem**: Tests expect form fields that don't exist in current component
**File**: `/apps/web/src/pages/dashboard/ProfilePage.tsx`
**Solution**:
- Option A: Update tests to match current UI
- Option B: Restore form fields to component

### 3. UPDATE MembershipPage Tests (~10 tests)
**Problem**: Similar to ProfilePage - UI expectations mismatch
**Solution**: Align tests with current component structure

### 4. FIX Integration Tests (~8 tests)
**Problem**: API response validation and error handling
**Solution**: Update test assertions to match actual API responses

## Lessons Learned

1. **Don't assume MSW URL patterns are the issue** - check base handlers first
2. **Verify component UI hasn't changed** before fixing tests
3. **Test failures can have multiple root causes** - MSW, timing, UI changes
4. **Always check test results BEFORE and AFTER** to verify fixes work

## Conclusion

**MSW Absolute URL Fix**: Not needed in most places (already supported)
**Real Issues**: Component UI changes, fetch vs apiClient, test expectations

**Recommendation**: STOP MSW URL fixes, START fixing actual root causes above.
