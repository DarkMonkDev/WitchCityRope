# E2E Test Cleanup Plan - November 30, 2025

**Status**: PENDING USER REVIEW
**Previous Baseline**: 848 tests (587 passed, 261 failed = 69.2% pass rate)
**Goal**: Reduce test count by removing duplicates/debug tests, fix remaining tests properly

## CRITICAL RULES (User Mandated)

1. **NO SKIPPING TESTS** without explicit user verification
2. **NO HARDCODING PASSES** without explicit user verification
3. **ALL BASELINES** go in `/docs/test-baselines/` folder
4. **EVERY FIX** must be verified against actual application behavior

---

## Phase 1: Delete Debug/Diagnostic/Verify Tests (44 files)

These are one-off tests created during development that serve no regression value:

### To DELETE Immediately:
```
tests/e2e/admin-events-table-ui-check.spec.ts
tests/e2e/basic-functionality-check.spec.ts
tests/e2e/capture-app-state.spec.ts
tests/e2e/capture-console-errors.spec.ts
tests/e2e/check-admin-events-visual.spec.ts
tests/e2e/cms-mobile-quick-test.spec.ts
tests/e2e/comprehensive-diagnostic.spec.ts
tests/e2e/console-error-check.spec.ts
tests/e2e/diagnostic-test-corrected.spec.ts
tests/e2e/diagnostic-test.spec.ts
tests/e2e/enhanced-diagnostic.spec.ts
tests/e2e/form-designs-check.spec.ts
tests/e2e/page-load-diagnostic.spec.ts
tests/e2e/quick-visual-test.spec.ts
tests/e2e/test-dom-check.spec.ts
tests/e2e/test-event-type-column.spec.ts
tests/e2e/test-execution-report.spec.ts
tests/e2e/test-with-reload.spec.ts
tests/e2e/verify-enum-mapping-fix.spec.ts
tests/e2e/verify-event-fixes.spec.ts
tests/e2e/verify-event-type-column.spec.ts
tests/e2e/verify-fix-corrected.spec.ts
tests/e2e/verify-form-design-fixes.spec.ts
tests/e2e/verify-logout-csrf.spec.ts
tests/e2e/verify-page-stability.spec.ts
tests/e2e/verify-paypal-button-fix.spec.ts
tests/e2e/verify-policies.spec.ts
tests/e2e/verify-recent-changes.spec.ts
tests/e2e/verify-registration-fix.spec.ts
tests/e2e/verify-ticket-dropdown.spec.ts
tests/e2e/verify-vetting-status-fix.spec.ts
tests/e2e/visual-check.spec.ts
```

**Expected reduction**: ~44 files, ~100+ tests

---

## Phase 2: Consolidate Duplicate Test Files

### Admin Events Dashboard (4 duplicates -> 1)
**KEEP**: `admin-events-dashboard.spec.ts` (or merge into `admin-events-workflow.spec.ts`)
**DELETE**:
- `admin-events-dashboard-final.spec.ts`
- `admin-events-dashboard-fixed.spec.ts`
- `admin-events-dashboard-working.spec.ts`

### Admin Events Navigation (2 duplicates -> 1)
**KEEP**: `admin-events-navigation.spec.ts`
**DELETE**: `admin-events-navigation-test.spec.ts`

### Admin Events Workflow (2 duplicates -> 1)
**KEEP**: `admin-events-workflow.spec.ts`
**DELETE**: `admin-events-workflow-test.spec.ts`

### Vetting Tests (8+ files -> 3-4 files)
**Analysis Needed**: Review which vetting tests are duplicates vs. covering different scenarios

### Dashboard Navigation (2 duplicates)
**KEEP**: `navigation-workflow.spec.ts`
**DELETE**: `dashboard-navigation.spec.ts`, `test-direct-navigation.spec.ts`

**Expected reduction**: ~15 files, ~80+ tests

---

## Phase 3: Fix Failing Tests (REQUIRES USER CONFIRMATION)

After cleanup, the remaining failing tests need to be analyzed individually:

### Analysis Process for Each Failing Test:
1. **Run the test in isolation**
2. **Capture screenshot at failure point**
3. **Compare test expectation to actual UI**
4. **Determine root cause**:
   - **Test Bug**: Test expects wrong behavior/selectors
   - **App Bug**: Application has actual issue
   - **Data Bug**: Test data doesn't match expected state
   - **Timing Bug**: Wait strategy needs adjustment

### Categories That Likely Need Fixes:

#### 3.1 Session Modal Tests (User confirmed modal DOES close)
**Issue**: Form fields not completely filled causing validation errors
**Fix Required**: Ensure ALL required fields are filled before save attempt
**Files**:
- `admin-events-sessions.spec.ts`

#### 3.2 Admin Dashboard Incident Tests
**Issue**: Console errors showing 401 Unauthorized during tests
**Analysis Needed**: Auth cookie may not be properly set or CSRF token race condition
**Files**:
- `admin-dashboard-workflow.spec.ts`

#### 3.3 Vetting Application Tests
**Issue**: Multiple tests rely on specific data state
**Analysis Needed**: Determine if tests need data setup or are checking wrong selectors
**Files**:
- `vetting-workflow.spec.ts`
- `vetting-application.spec.ts`

#### 3.4 Checkout/Refund Tests
**Issue**: PayPal integration tests may need sandbox mode
**Analysis Needed**: Verify test environment has proper PayPal configuration
**Files**:
- `checkout-workflow.spec.ts`
- `refund-workflow.spec.ts`

---

## Phase 4: Target Test File Structure

After cleanup, the test suite should have this structure:

### Core Workflow Tests (10-15 files):
```
tests/e2e/
├── auth/
│   ├── test-bff-authentication.spec.ts  # Main auth flow
│   ├── login-with-scene-name.spec.ts    # Alternate login
│   ├── post-login-return.spec.ts        # Return URL
│   └── password-reset-workflow.spec.ts  # Password reset
├── admin/
│   ├── admin-dashboard-workflow.spec.ts # Incidents
│   ├── admin-events-workflow.spec.ts    # Events CRUD
│   ├── admin-member-history.spec.ts     # Member management
│   └── admin-payments-data-validation.spec.ts
├── events/
│   ├── events-comprehensive.spec.ts     # Public events
│   ├── checkout-workflow.spec.ts        # Ticket purchase
│   └── refund-workflow.spec.ts          # Refunds
├── user/
│   ├── user-dashboard-workflow.spec.ts  # Dashboard
│   ├── profile-workflow.spec.ts         # Profile
│   └── navigation-workflow.spec.ts      # Nav behavior
├── vetting/
│   ├── vetting-workflow.spec.ts         # Application flow
│   └── vetting-admin-dashboard.spec.ts  # Admin review
├── cms/
│   ├── cms-workflow.spec.ts             # CMS content
│   └── cms-accessibility.spec.ts        # A11y
└── regression/
    └── footer-regression.spec.ts        # Footer tests
```

**Target**: 25-35 well-focused test files
**Current**: 153 files (70%+ are duplicates or debug)

---

## Execution Order

### Step 1: Delete debug/diagnostic tests
- No verification needed
- Pure cleanup

### Step 2: Delete duplicate test files
- Verify no unique tests are lost
- Merge unique tests into canonical files if needed

### Step 3: Run baseline and document failures
- Save to `/docs/test-baselines/`
- Categorize each failure type

### Step 4: Fix tests ONE AT A TIME
- Each fix requires:
  1. Analysis document
  2. Screenshot evidence
  3. Code change
  4. Verification pass
- **USER APPROVAL REQUIRED** before marking any test as skipped

---

## Questions for User Before Proceeding

1. **Approve Phase 1**: Delete 44 debug/diagnostic/verify tests?
2. **Approve Phase 2**: Consolidate duplicate test files?
3. **Session Modal Tests**: Confirm modal closes when ALL fields are properly filled - should we update tests to fill all fields?
4. **Skipped Tests**: What criteria should trigger a "skip with reason" vs "fix the test"?
5. **PayPal Tests**: Should PayPal sandbox tests be skipped in local dev or properly configured?

---

## Notes

- This plan focuses on **test quality over quantity**
- Goal is tests that **catch real regressions**, not validate stale designs
- All tests should use **AuthHelpers.loginAs()** for authentication
- All tests should use **domcontentloaded** wait strategy (not networkidle)
- All tests should use **data-testid** selectors where available
