# 80% Pass Rate Attempt Report

**Date**: 2025-10-06
**Session Start**: 158/277 (57.0%)
**Session End**: 158/277 (57.0%)
**Target**: 221/277 (80.0%)
**Gap Remaining**: +63 tests
**Status**: TARGET NOT ACHIEVED

## Work Completed

### Priority 1: MSW Event Field Names - PARTIAL ✅
**Goal**: Fix field name mismatches in MSW handlers
**Time Spent**: 1.5 hours
**Tests Fixed**: 0 (changes made but didn't resolve failures)

**Changes Made**:
1. Updated `/apps/web/src/test/mocks/handlers.ts`:
   - Changed `startDateTime` → `startDate`
   - Changed `endDateTime` → `endDate`
   - Added missing `location` field to all event mocks
   
2. Updated `/apps/web/src/pages/dashboard/EventsPage.tsx`:
   - Changed `event.currentAttendees` → `event.registrationCount`
   - Changed badge condition from `event.status` → `event.registrationStatus`

**Result**: No test improvement. Tests still timing out.

**Root Cause Analysis**: 
- The MSW handler field names were correct for the default handlers
- Individual tests override handlers with their own mocks
- Those test-specific mocks also need field name fixes
- Component/API field alignment exists but tests aren't reaching the rendering stage

### Priority 2: useVettingStatus Tests - PARTIAL ✅
**Goal**: Fix 16 failing useVettingStatus tests
**Time Spent**: 1 hour
**Tests Fixed**: 0

**Changes Made**:
1. Updated `/apps/web/src/features/vetting/hooks/useVettingStatus.test.tsx`:
   - Changed `mockFetch.mockResolvedValueOnce()` → `mockFetch.mockResolvedValue()`
   - Applied to 3 test cases and the forEach loop (all status tests)

**Reason for Change**: 
- Hook has `refetchOnMount: true` which causes multiple fetch calls
- `mockResolvedValueOnce()` only mocks first call
- Second call was unmocked, returning undefined

**Result**: No improvement. Tests still fail with same error pattern.

**Current Failure Pattern**:
```
AssertionError: expected undefined to be 'Draft' // Object.is equality

Expected: "Draft"
Received: undefined

At: result.current.data?.application?.status
```

**Why Still Failing**: Unknown. The mock setup looks correct:
- `isSuccess` is `true` (query succeeds)
- But `data?.application?.status` is `undefined`
- Suggests data structure mismatch or query not receiving mocked response

## Deep Dive: Why Tests Are Still Failing

### Category 1: Component Rendering Timeouts (48 tests)
**Affected Files**:
- `DashboardPage.test.tsx` (12 tests)
- `EventsPage.test.tsx` (11 tests)  
- `ProfilePage.test.tsx` (11 tests)
- `MembershipPage.test.tsx` (6 tests)
- `SecurityPage.test.tsx` (9 tests)

**Pattern**: 
- Tests timeout at ~1000ms waiting for content to render
- Components appear stuck in loading state
- MSW handlers exist but components don't receive data

**Hypotheses**:
1. **React Query + MSW timing issue**: Query might be making requests before MSW handlers are ready
2. **Query cache pollution**: Despite `gcTime: 0`, cache might persist between tests
3. **Handler URL mismatch**: Tests might be calling different URLs than handlers expect
4. **Missing MSW handler setup**: Some handlers might not be registered properly

### Category 2: useVettingStatus Structure Mismatch (16 tests)
**Pattern**:
- Query succeeds (`isSuccess: true`)
- Data is returned but structure is wrong
- `data?.application?.status` is `undefined`

**Hypotheses**:
1. **Mock not being used**: Despite `mockFetch.mockResolvedValue()`, real fetch might be called
2. **Data transformation issue**: Hook might be transforming data incorrectly
3. **Type mismatch**: Response might not match expected interface
4. **Async timing**: Data might not be fully populated when assertion runs

### Category 3: MSW Error Handlers Not Triggering (5 tests)
**Pattern**:
- Test defines error handler with `server.use()`
- Success handler still responds instead
- Test expects error state but gets success state

**Root Cause**: Handler priority/override issue with MSW

## Lessons Learned

### 1. MockResolvedValueOnce vs MockResolvedValue
**Issue**: React Query hooks with `refetchOnMount: true` make multiple calls
**Solution**: Use `mockResolvedValue()` for all calls, not just first
**Status**: Applied but didn't fix tests (deeper issue exists)

### 2. Field Name Consistency Critical
**Issue**: API uses different field names than frontend expects
**Examples**:
- API: `startDateTime/endDateTime` vs Frontend: `startDate/endDate`
- API: `currentAttendees` vs EventDto: `registrationCount`
**Solution**: Align field names across API, types, and mocks
**Status**: Partially fixed in handlers, but individual test mocks need updates

### 3. Component-Type-API Alignment
**Discovery**: Three-way mismatch:
- `Event` type (test mocks): `maxAttendees`, `currentAttendees`, `startDate`
- `ApiEvent` interface (API response): `capacity`, `currentAttendees`, `startDate`
- `EventDto` type (frontend): `capacity`, `registrationCount`, `startDate`

**Impact**: Tests using `Event` type don't match what components expect
**Solution Needed**: Standardize on one field name set across entire stack

### 4. MSW + React Query Integration Fragile
**Issue**: Tests timeout despite MSW handlers being defined
**Possible Causes**:
- Handler URL patterns don't match request URLs
- Timing race between MSW setup and React Query execution
- Query cache interfering despite configuration

**Solution Needed**: Deeper investigation of MSW/React Query interaction

## What Didn't Work

### Attempt 1: Fix MSW Handler Field Names
- **Expected**: 10-15 tests to pass
- **Result**: 0 tests improved
- **Why**: Individual test mocks also need updates, not just default handlers

### Attempt 2: Fix useVettingStatus Mock Setup
- **Expected**: 16 tests to pass
- **Result**: 0 tests improved  
- **Why**: Unknown - mock setup appears correct but data not reaching assertions

## What's Needed to Reach 80%

### Immediate Actions (High Impact)
1. **Debug MSW+React Query Integration**:
   - Add logging to track if handlers are being called
   - Verify request URLs match handler patterns
   - Check query client setup in tests

2. **Fix Test Mock Data Structure**:
   - All test mocks must match `ApiEvent` interface (use `capacity`, not `maxAttendees`)
   - All test mocks must include required fields (`location`, `startDate`, `endDate`)
   - Update 48+ component test mocks

3. **Investigate useVettingStatus Data Flow**:
   - Add console.log to see actual response data
   - Verify mock is being applied correctly
   - Check if `beforeEach` reset is interfering

### Estimated Effort
- **Debug and fix MSW integration**: 2-3 hours
- **Update all test mocks to match ApiEvent**: 2-3 hours  
- **Fix useVettingStatus data issue**: 1 hour
- **Verify and test**: 1 hour
- **Total**: 6-8 hours

### Alternative Approach
Given the complexity and time required:
1. **Accept 57% pass rate** as baseline
2. **Document known issues** (this report)
3. **Create focused test improvement backlog**
4. **Prioritize E2E tests** which provide more value
5. **Return to unit tests** when field name standardization is complete

## Recommendations

### Short Term (Next Session)
1. **DO NOT** continue attempting to fix unit tests
2. **DO** focus on E2E Playwright tests which test real integration
3. **DO** document the field name alignment as a technical debt item
4. **DO** create a separate ticket for "Standardize Event Field Names Across Stack"

### Medium Term (Next Sprint)
1. **Backend Task**: Standardize event field names in API
2. **Frontend Task**: Update all EventDto/Event types to match API
3. **Test Task**: Update all MSW handlers and test mocks
4. **Then** return to fixing unit tests with consistent foundation

### Long Term (Architecture)
1. **Generate TypeScript types from C# DTOs** via NSwag (already documented)
2. **Use generated types in MSW handlers** (no manual types)
3. **Add type validation tests** to catch mismatches early
4. **Consider API contract testing** with tools like Pact

## Files Modified

| File | Changes | Status |
|------|---------|--------|
| `/apps/web/src/test/mocks/handlers.ts` | Fixed event field names (startDate, endDate, location) | COMMITTED |
| `/apps/web/src/pages/dashboard/EventsPage.tsx` | Fixed component field usage (registrationCount) | COMMITTED |
| `/apps/web/src/features/vetting/hooks/useVettingStatus.test.tsx` | Changed mockResolvedValueOnce → mockResolvedValue | COMMITTED |

## Test Progress Tracking
- **Starting**: 158/277 (57.0%)
- **Current**: 158/277 (57.0%)
- **Target**: 221/277 (80.0%)
- **Gap**: +63 tests
- **Change**: 0 tests (+0%)

## Files for File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-10-06 | `/home/chad/repos/witchcityrope/test-results/80-percent-attempt-report-20251006.md` | CREATED | Document 80% achievement attempt and findings | Test suite improvement | ACTIVE | After review |
| 2025-10-06 | `/home/chad/repos/witchcityrope/apps/web/src/test/mocks/handlers.ts` | MODIFIED | Fix event field names to match API | MSW handler fixes | ACTIVE | N/A |
| 2025-10-06 | `/home/chad/repos/witchcityrope/apps/web/src/pages/dashboard/EventsPage.tsx` | MODIFIED | Fix component to use correct EventDto field names | Component bug fix | ACTIVE | N/A |
| 2025-10-06 | `/home/chad/repos/witchcityrope/apps/web/src/features/vetting/hooks/useVettingStatus.test.tsx` | MODIFIED | Fix mock setup for refetchOnMount behavior | Test fixes | ACTIVE | N/A |

---

**Created**: 2025-10-06 23:30 UTC
**Session Duration**: 3 hours
**Outcome**: Investigation and groundwork laid, but 80% target not achieved
**Next Steps**: Recommend shifting focus to E2E tests and documenting field name standardization as technical debt
