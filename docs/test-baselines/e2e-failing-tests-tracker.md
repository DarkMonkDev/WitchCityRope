# E2E Failing Tests Tracker

## Purpose

Track currently failing E2E tests, their root causes, and fix instructions for the next agent.

## Current Status (December 14, 2025 - Post Session Fixes)

| Metric | Value |
|--------|-------|
| **Total Tests** | 794 |
| **Estimated Passed** | ~747 |
| **Estimated Failed** | ~18 |
| **Skipped** | 29 |
| **Estimated Pass Rate** | **~94%** |

### Progress Since Last Update
| Metric | Previous (Dec 13) | Current (Dec 14) | Change |
|--------|-------------------|------------------|--------|
| Passed | 727 | ~747 | +20 ✅ |
| Failed | 38 | ~18 | -20 ✅ |
| Pass Rate | 91.5% | ~94% | +2.5% ✅ |

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

---

## PREVIOUSLY FIXED (Verified Passing December 14, 2025)

### session-ticket-availability.spec.ts - ALL 7 PASSING
**Fixed**: Previous session (commit `054a56d9`)
**Root Cause**: Tests rewritten with proper timing logic

### admin-refund-eligibility.spec.ts - ALL 6 PASSING
**Status**: Verified passing this session

### admin-session-deletion.spec.ts - ALL 5 PASSING
**Status**: Verified passing this session

### venue-creation.spec.ts - ALL 5 PASSING
**Status**: Verified passing this session

---

## REMAINING FAILURES - VERIFIED (4 Total)

### 1. admin-tickettype-deletion.spec.ts (1 failure)

**Failing Test**: `ticket type deletion shows correct sales count in blocked modal`

**Root Cause**: Backend DataFactory API error when creating ticket purchases
```
Error: DataFactory API error: POST /ticket-purchases - Status 400
"Internal error: An error occurred while saving the entity changes"
```

**Fix Required**: Backend investigation - TestHelperService ticket purchase creation

---

### 2. phase3-sessions-tickets.spec.ts (2 failures)

**Failing Tests**:
- `Session CRUD - Add, edit, and delete sessions`
- `Ticket Types - Create and manage ticket types`

**Root Cause**: Tests expect "Setup" tab in event edit modal that doesn't exist in current UI
```typescript
await page.locator('button[role="tab"]:has-text("Setup")').click(); // Tab doesn't exist
```

**Fix Required**: Either implement "Setup" tab in UI OR rewrite tests to match actual UI structure

---

### 3. profile-page.spec.ts (1 failure)

**Failing Test**: `should handle user loading error`

**Root Cause**: Test mocks API to return 500 error, expects error UI that may not be implemented
- Component may not display error the way test expects
- Or TanStack Query handles errors differently

**Fix Required**: Verify ProfileSettingsPage error handling, update test selectors

---

## NOT YET VERIFIED (~14 failures from tracker)

These tests were listed as failing but haven't been run this session. May pass or fail.

### HIGH PRIORITY - Should Verify First

**admin-events-volunteers.spec.ts** (4 listed failures):
- should add volunteer position via inline form
- should display sessions in day format in position assignments
- should show only current event sessions in dropdown
- should validate volunteer position form fields

**anonymous-report-submission.spec.ts** (2 listed failures):
- should submit anonymous incident report and receive reference number
- should validate required fields before submission
- **Note**: Tracker said fix was committed (`e099d3b2`) - needs verification

### MEDIUM PRIORITY

**events-comprehensive.spec.ts** (2 listed failures):
- should handle large number of events efficiently
- social event should offer RSVP AND ticket purchase as parallel actions

**events-management-e2e.spec.ts** (3 listed failures):
- should display event form tabs
- should load Event Session Matrix demo page
- should verify form fields are present

**home-page.spec.ts** (2 listed failures):
- events display from API with complete card structure
- proves complete React + API + PostgreSQL stack works

**vetting-workflow.spec.ts** (2 listed failures):
- admin can deny application with reason
- user can submit vetting application successfully

### LOW PRIORITY

**events-basic-validation.spec.ts** (1 listed failure):
- Events Page Loading and Content Detection

**event-update-complete-flow.spec.ts** (1 listed failure):
- Admin can update event without getting logged out

**phase4-events-testing.spec.ts** (1 listed failure):
- should display event filters correctly

### INFRASTRUCTURE (Can Skip)

**compare-wireframe.spec.ts** (1 failure):
- capture original wireframe
- Requires docs server running on localhost:8080 - not critical

**e2e-events-full-journey.spec.ts** (1 failure):
- Environment Health Check
- May be outdated infrastructure test

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

1. **Verify unverified tests** - Run each test file in "NOT YET VERIFIED" section
2. **Update this tracker** - Mark tests as passing or document specific failures
3. **Fix verified failures** - Focus on the 4 verified failing tests
4. **Run full test suite** - Use `test-environment` skill only for final validation

---

**Last Updated**: 2025-12-14T02:15:00Z
**Session Commits**: eca1c732, 333eb866, df2cebd4, f72493cc
**Git SHA**: f72493cc
