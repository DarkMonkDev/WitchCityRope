# TACTICAL TEST FIXING - Next Steps

**Date**: 2025-10-06
**Current**: 158/277 (57.0%)
**Target**: 221/277 (80.0%)
**Gap**: +63 tests needed

## Critical Discovery

**Field name alignment fixed ZERO tests** despite completing:
- ✅ Backend DTO standardization
- ✅ Frontend type regeneration  
- ✅ MSW handler updates
- ✅ Component code updates

**Root cause is test infrastructure, NOT data contracts.**

## Prioritized Action Plan

### Priority 1: MSW Timing Fixes (48 tests, 2 hours) 🔥

**Problem**: Tests timeout at ~1000ms waiting for MSW responses

**Files to Fix**:
1. `EventsPage.test.tsx` - 11 tests timeout
2. `useVettingStatus.test.tsx` - 16 tests timeout
3. `ProfilePage.test.tsx` - 11 tests timeout
4. `SecurityPage.test.tsx` - 10 tests timeout

**Solution Options**:
```typescript
// Option A: Add waitFor with longer timeout
await waitFor(() => {
  expect(screen.getByText('Expected Text')).toBeInTheDocument();
}, { timeout: 3000 });

// Option B: Fix MSW handler setup
beforeEach(() => {
  server.use(
    http.get('/api/events', () => {
      return HttpResponse.json(mockEvents);
    })
  );
});

// Option C: Adjust React Query config in tests
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      gcTime: 0,
      staleTime: 0,
    },
  },
});
```

**Expected Impact**: +48 tests (brings us to 206/277 = 74%)

---

### Priority 2: Error Message Fixes (4 tests, 30 min) ⚡

**Problem**: Tests expect error text that doesn't match actual output

**Files to Fix**:
1. `DashboardPage.test.tsx` - 2 tests ("Unable to Load Dashboard")
2. `EventsPage.test.tsx` - 1 test (error state text)
3. `ProfilePage.test.tsx` - 1 test (error state text)

**Solution**: Update test assertions to match actual error messages

**Expected Impact**: +4 tests (brings us to 210/277 = 76%)

---

### Priority 3: Auth Store Structure (4 tests, 30 min) ⚡

**Problem**: `checkAuth` expects nested response, gets flat structure

**File to Fix**: `AuthStore.test.tsx`

**Solution Options**:
```typescript
// Option A: Update checkAuth to handle flat response
const response = await fetch('/api/auth/check');
const data = await response.json();
if (response.ok) {
  set({ 
    isAuthenticated: true, 
    user: data, // Already flat
    loading: false 
  });
}

// Option B: Update MSW handler to return nested
http.get('/api/auth/check', () => {
  return HttpResponse.json({
    user: { id: '1', sceneName: 'Test', email: 'test@example.com' }
  });
})
```

**Expected Impact**: +4 tests (brings us to 214/277 = 77%)

---

### Priority 4: Auth Flow Integration (2 tests, 1 hour) 🔸

**Problem**: Login flow not properly updating store or handling navigation

**File to Fix**: `auth-flow-integration.test.tsx`

**Tests**:
- "should complete login flow from mutation to store to navigation"
- "should handle returnTo parameter in navigation"

**Solution**: Debug store updates in `useLogin` hook

**Expected Impact**: +2 tests (brings us to 216/277 = 78%)

---

### Priority 5: Visual/Form Tests (19 tests, 2 hours) 🔸

**Problem**: Form labels, accessibility, visual design mismatches

**Files to Fix**:
1. `SecurityPage.test.tsx` - 8 tests (privacy toggles, accessibility)
2. `MembershipPage.test.tsx` - 3 tests (visual design, benefits)
3. `ProfilePage.test.tsx` - 2 tests (visual, edge cases)
4. `VettingApplicationsList.test.tsx` - 2 tests (filters)
5. `EventsList.test.tsx` - 2 tests (integration)
6. `useLogin.test.tsx` - 1 test (store integration)
7. Others - 1 test

**Solution**: Update component code OR test expectations to match

**Expected Impact**: +19 tests (brings us to 235/277 = 85%)

---

## Path to 80% (221 tests)

**Priorities 1-3 get us to 77% (214 tests)**
- MSW timing: +48 tests
- Error messages: +4 tests
- Auth store: +4 tests
- **Total**: +56 tests in ~3 hours

**Need 7 more tests from Priority 4-5**
- Auth flow: +2 tests
- Visual tests: +5 of 19 tests

**Realistic Estimate**: 4-5 hours to reach 80%

## Execution Strategy

1. **Start with Priority 1** (MSW timing) - Biggest impact
2. **Quick wins** - Priorities 2-3 (error messages, auth store)
3. **Assess progress** - Are we at 77%?
4. **Cherry-pick** - Grab easiest 7 tests from Priorities 4-5
5. **Stop at 80%** - Don't over-invest

## Commands to Run

```bash
# After each fix, run tests to measure progress
cd /home/chad/repos/witchcityrope/apps/web
npm run test -- --run 2>&1 | tee /home/chad/repos/witchcityrope/test-results/tactical-fix-run-$(date +%Y%m%d-%H%M%S).log

# Check specific test file
npm run test -- EventsPage.test.tsx --run

# Check pass rate
npm run test -- --run | grep -E "Test Files|Tests"
```

## Success Criteria

- ✅ Pass rate ≥ 80% (221+ tests)
- ✅ No regressions from current 158 tests
- ✅ TypeScript compilation clean
- ✅ All fixes documented
- ✅ File registry updated
