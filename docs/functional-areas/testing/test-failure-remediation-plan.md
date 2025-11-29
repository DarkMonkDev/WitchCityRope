# E2E Test Failure Remediation Plan

**Created**: 2025-11-29
**Status**: Planning
**Baseline**: 504 passed / 392 failed (56.3% pass rate)
**Target**: 800+ passed / <100 failed (89%+ pass rate)

## Executive Summary

After fixing the Vite HMR networking issue, we improved from 305 to 504 passing tests (+199). However, 392 tests still fail. Analysis shows the root causes are:

1. **Diagnostic/Temporary Tests (26%)**: ~46 test files appear to be one-off debug tests that should be archived
2. **Authentication Inconsistency**: Tests use different login methods - many don't use the standardized `AuthHelpers`
3. **CSRF Token Issues**: Tests hitting state-changing endpoints without proper CSRF token setup
4. **Stale Selectors**: Tests expecting elements that no longer exist or use outdated selectors
5. **Real Application Bugs**: A smaller subset may reveal actual bugs that need fixing

## Metrics Summary

| Metric | Count | Percentage |
|--------|-------|------------|
| Total Test Files | 173 | 100% |
| Diagnostic/Debug Tests | ~46 | 26% |
| Total Tests | 896 | 100% |
| Currently Passing | 504 | 56.3% |
| Currently Failing | 392 | 43.7% |

## Phased Approach

### Phase 1: Test Audit & Cleanup (Est. 2-3 hours)

**Objective**: Archive or delete diagnostic tests that shouldn't be in the main test suite.

**Criteria for Archive**:
- File name contains: `debug-`, `diagnostic-`, `test-`, `verify-`, `check-`, `capture-`, `quick-`
- Single test describing temporary investigation
- No clear business value
- Duplicate of existing proper tests

**Agent Assignment**: `test-developer`

**Deliverables**:
1. List of tests to archive (with justification)
2. Move diagnostic tests to `tests/e2e/_archived/`
3. Updated test count baseline

**Expected Outcome**: ~100-150 fewer tests to maintain

---

### Phase 2: Test Infrastructure Standardization (Est. 3-4 hours)

**Objective**: Ensure all tests use standardized helpers and fixtures.

**Key Issues to Address**:

1. **Authentication Standardization**
   - Audit all tests for login patterns
   - Replace ad-hoc login code with `AuthHelpers.loginAs()`
   - Add CSRF token handling to auth flow if needed

2. **Wait Strategy Standardization**
   - Replace arbitrary `page.waitForTimeout()` with proper `waitForSelector()` or `waitForResponse()`
   - Use `WaitHelpers` consistently

3. **Selector Standardization**
   - Audit for hardcoded selectors vs `data-testid` usage
   - Update stale selectors to match current component implementations

**Agent Assignment**: `test-developer`

**Deliverables**:
1. Updated `AuthHelpers` with CSRF support if needed
2. Test refactoring guide for remaining tests
3. List of selector updates needed

---

### Phase 3: Fix Failing Tests by Category (Est. 4-6 hours)

Group failing tests by functional area and fix in batches.

**Category Priority Order**:

| Priority | Category | Est. Tests | Complexity |
|----------|----------|------------|------------|
| 1 | Authentication/Login | ~50 | Medium |
| 2 | Admin Events | ~80 | High |
| 3 | Public Events | ~40 | Medium |
| 4 | Vetting System | ~30 | Medium |
| 5 | Check-in System | ~30 | Medium |
| 6 | CMS/Content | ~20 | Low |
| 7 | User Dashboard | ~20 | Low |
| 8 | Other | ~remaining | Variable |

**Agent Assignment**: `test-developer` (fix tests), `react-developer` or `backend-developer` (if real bugs found)

**Process per Category**:
1. Run category tests in isolation
2. Analyze failure patterns
3. Fix test issues (selectors, timing, helpers)
4. If real bug found, create bug report and pass to appropriate agent
5. Verify fixes pass in both DEV and TEST containers

---

### Phase 4: Bug Verification & Documentation (Est. 2-3 hours)

**Objective**: Catalog real application bugs discovered during test fixes.

**Bug Categories**:
- **P1 Critical**: Breaks core user flows
- **P2 Major**: Significant functionality issues
- **P3 Minor**: Edge cases, cosmetic issues

**Agent Assignment**: `test-developer` documents, `code-reviewer` validates

**Deliverables**:
1. Bug tracking document with priorities
2. Fixes for P1/P2 bugs (if time permits)
3. Updated test suite with all tests passing or properly skipped

---

## Orchestration Workflow

```
PHASE 1 (Cleanup)
    │
    ├── test-developer: Audit and archive diagnostic tests
    │       │
    │       └── Handoff: archived-tests-list.md
    │
    ▼
PHASE 2 (Infrastructure)
    │
    ├── test-developer: Standardize auth helpers and fixtures
    │       │
    │       └── Handoff: infrastructure-updates.md
    │
    ▼
PHASE 3 (Fix Tests - Parallel Batches)
    │
    ├── Batch A: Authentication tests
    ├── Batch B: Admin Events tests
    ├── Batch C: Public Events tests
    │       │
    │       └── Handoff: test-fixes-batch-N.md (with any bug reports)
    │
    ▼
PHASE 4 (Bug Documentation)
    │
    ├── test-developer: Consolidate bug reports
    ├── code-reviewer: Validate bug reports
    │       │
    │       └── Final: test-remediation-complete.md
    │
    ▼
COMPLETE
```

## Success Criteria

1. **Pass Rate**: >= 89% (800+ tests passing)
2. **Zero Flaky Tests**: All tests pass consistently on reruns
3. **Container Parity**: Same results in DEV and TEST containers (within 5% variance)
4. **Documentation**: All skipped tests have documented reasons
5. **No Regression**: No new failures introduced

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Too many tests archived | Require justification for each archive; code review |
| Fixing tests hides real bugs | Document any suspicious failures before fixing tests |
| Time overrun | Batch work; stop after Phase 3 if needed |
| Container config issues | Run verification in both environments after each phase |

## Notes

- Do NOT modify application code unless clearly fixing a bug
- Prefer fixing test selectors/timing over changing application
- When in doubt, mark test as `.skip()` with explanation and create follow-up ticket
- All work should be committed in logical chunks with clear messages
