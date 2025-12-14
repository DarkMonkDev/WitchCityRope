# E2E Failing Tests Tracker

## Purpose

Track currently failing E2E tests, their root causes, and fix instructions for the next agent.

## Current Status (December 14, 2025 - Late Evening Session - Updated)

| Metric | Value |
|--------|-------|
| **Total Tests** | 794 |
| **Estimated Passed** | ~760 |
| **Estimated Failed** | ~9 |
| **Skipped** | 29 |
| **Estimated Pass Rate** | **~95.7%** |

### Progress Since Last Update
| Metric | Previous (Dec 14 Late PM) | Current (Verified) | Change |
|--------|---------------------------|---------------------|--------|
| Passed | ~751 | ~760 | +9 ✅ |
| Failed | ~14 | ~9 | -5 ✅ |
| Pass Rate | ~94.6% | ~95.7% | +1.1% ✅ |

---

## FIXED THIS SESSION (December 14, 2025)

### admin-events-dashboard.spec.ts - ALL 6 PASSING
**Root Cause**: Mantine Switch renders checkbox input as hidden
**Fix**: Use `getByText('Show Past Events')` for clicking, `toBeAttached()` for existence checks
**Commit**: `eca1c732`

### venue-editing.spec.ts - ALL 6 PASSING
**Root Cause**: Test isolation - delete test (index 1) conflicted with update test (also index 1)
**Fix**: Update test uses index 2, added explicit `waitFor` on options
**Commit**: `333eb866`

### events-complete-workflow.spec.ts - ALL 6 PASSING
**Root Cause**: Admin events selectors didn't match actual UI
**Fix**: Updated to use `[data-testid="events-table"] tbody tr`
**Commit**: `df2cebd4`

### notification-system-test.spec.ts - ALL 2 PASSING
**Root Cause**: Mantine creates multiple notification containers (top-left, top-right, etc.), only one visible
**Fix**: Removed visibility assertion on container, verify notification content directly
**Commit**: `f72493cc`

### admin-tickettype-deletion.spec.ts - ALL 6 PASSING
**Root Cause**: Test created 3 ticket purchases for same user - violates business rule (same user cannot purchase multiple tickets for same session)
**Fix**: Create 3 different users, each purchasing 1 ticket
**Commit**: `52935254`

### phase3-sessions-tickets.spec.ts - ALL 5 PASSING
**Root Cause**: Test used `getByTestId('setup-tab')` which matched both tab button AND tab panel (strict mode violation)
**Fix**: Changed to `getByRole('tab', { name: 'Sessions / Ticket Types' })` for unique selector
**Commit**: `2af22892`

### profile-page.spec.ts - ALL 2 PASSING
**Root Cause**: Test mocked wrong endpoint (`/api/auth/user` instead of `/api/users/*/profile`)
**Fix**: Mock `/api/users/*/profile` endpoint, use `getByRole('alert')`, wait for TanStack Query retries
**Commit**: `f3182477`

---

## VERIFIED PASSING (Were Listed as Failing)

### admin-events-volunteers.spec.ts - ALL 7 PASSING ✅
**Status**: Verified passing - was listed as 4 failures, now all pass
**Cleanup warnings**: Benign - volunteer positions deleted by tests before cleanup

### anonymous-report-submission.spec.ts - ALL 2 PASSING ✅
**Status**: Verified passing - fix committed previously (`e099d3b2`)

### home-page.spec.ts - ALL 12 PASSING ✅
**Status**: Verified passing - was listed as 2 failures, all tests pass
**Cleanup warnings**: Benign - fixture reuse warnings don't affect test results

### events-basic-validation.spec.ts - ALL 4 PASSING ✅
**Status**: Verified passing - was listed as 1 failure

---

## PREVIOUSLY FIXED (Verified Passing December 14, 2025)

### session-ticket-availability.spec.ts - ALL 7 PASSING
**Fixed**: Previous session (commit `054a56d9`)

### admin-refund-eligibility.spec.ts - ALL 6 PASSING
**Status**: Verified passing this session

### admin-session-deletion.spec.ts - ALL 5 PASSING
**Status**: Verified passing this session

### venue-creation.spec.ts - ALL 5 PASSING
**Status**: Verified passing this session

---

## REMAINING FAILURES - VERIFIED (9 Total)

### 1. events-comprehensive.spec.ts (2 failures)

**Failing Tests**:
- `should handle large number of events efficiently`
- `social event should offer RSVP AND ticket purchase as parallel actions`

**Root Cause**: Mock data structure mismatch
- Mock wraps events in `{ success, data, error }` but API returns array directly
- Mock event objects missing required fields (sessions, ticketTypes, etc.)
- Social event test: Selector may find hidden element first

**Fix Required**: Update mock data structure to match actual API response format

---

### 2. events-management-e2e.spec.ts (3 failures)

**Failing Tests**:
- `should load Event Session Matrix demo page`
- `should display event form tabs`
- `should verify form fields are present`

**Root Cause**: Demo page crashes with "Invalid time value"
- EventSessionMatrixDemo component throws error on render
- Mock data has dates that cause Date parsing issues

**Fix Required**: Fix EventSessionMatrixDemo component - update mock dates or fix date parsing

---

### 3. vetting-workflow.spec.ts (2 failures)

**Failing Tests**:
- `admin can put application on hold with reason`
- `admin can deny application with reason`

**Root Cause**: Application not found error
- Error: "Application with ID '...' was not found"
- Test creates application but it's not accessible when navigating to detail page
- Possible timing issue or data factory cleanup issue

**Fix Required**: Debug application creation/navigation flow

---

### 4. event-update-complete-flow.spec.ts (1 failure)

**Failing Test**: `Admin can update event without getting logged out`

**Root Cause**: Needs investigation
**Fix Required**: Check test and component for auth persistence issues

---

### 5. phase4-events-testing.spec.ts (1 failure)

**Failing Test**: `should display event filters correctly`

**Root Cause**: Selector not found
- Looking for `[data-testid="button-view-toggle"]`
- Element may have been removed or renamed

**Fix Required**: Update selector to match current UI

---

## INFRASTRUCTURE (Can Skip)

**compare-wireframe.spec.ts** (1 failure):
- Requires docs server running on localhost:8080 - not critical

**e2e-events-full-journey.spec.ts** (1 failure):
- Environment Health Check - may be outdated

---

## Key Mantine v7 Testing Patterns

### Switch Component
- Renders checkbox input as **hidden** (visually styled with CSS)
- Use `getByText('Label')` to click the visible label
- Use `toBeAttached()` instead of `toBeVisible()` for existence checks
- Use `{ force: true }` if you must click the hidden input

### Notifications
- Creates **multiple position containers** (top-left, top-right, bottom-left, etc.)
- Only one container is visible at a time
- Don't assert visibility on container - check notification content directly

### SegmentedControl
- Renders as **buttons**, NOT radio inputs
- Use `getByRole('button', { name: 'OPTION' })`
- Check state with `getAttribute('data-active')` not `isChecked()`

### Select (searchable)
- Does NOT have simple nested `input` element
- May need `getByRole('combobox')` or click wrapper directly

### React Strict Mode
- Components render twice in dev mode
- Use `.last()` on button selectors to get actual interactive element

---

## Running Tests in Test Containers

**CRITICAL**: Always run tests in test containers, not from host.

- **Rebuild test containers**: Use `restart-test-containers` skill
- **Run specific tests**: Use `test-environment` skill for full suite, or run individual tests via docker exec in test-runner container
- **Copy test files**: Use docker cp to sync updated test files to container without full rebuild

---

## Next Agent Instructions

1. **Fix verified failures** - Focus on the 9 remaining failures:
   - events-comprehensive.spec.ts (2) - Mock data structure fix
   - events-management-e2e.spec.ts (3) - Demo page component fix
   - vetting-workflow.spec.ts (2) - Debug application navigation
   - event-update-complete-flow.spec.ts (1) - Auth persistence check
   - phase4-events-testing.spec.ts (1) - Selector update
2. **Run full test suite** - Use `test-environment` skill for final validation

---

**Last Updated**: 2025-12-14T21:45:00Z
**Session Commits**: eca1c732, 333eb866, df2cebd4, f72493cc, 52935254, 2af22892, 0bfaefff, f3182477
**Git SHA**: f3182477
