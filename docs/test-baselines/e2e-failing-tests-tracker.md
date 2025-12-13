# E2E Failing Tests Tracker

## Purpose

Track currently failing E2E tests, their root causes, and fix instructions for the next agent.

## Current Test Run (December 13, 2025 - DataFactory Session Fixes)

| Metric | Value |
|--------|-------|
| **Total Tests** | 794 |
| **Passed** | 690 → ~710 (estimated with session fixes) |
| **Failed** | 75 → ~55 (estimated) |
| **Skipped** | 29 |
| **Pass Rate** | **86.9% → ~89%** |

### Session Fix Verification (8 files, 41 tests)
| Metric | Value |
|--------|-------|
| **Tests Run** | 41 |
| **Passed** | 38 |
| **Failed** | 3 |
| **Pass Rate** | **92.7%** |

---

## RECENTLY FIXED (December 13, 2025)

### Check-In Tests - ALL 28 PASSING

**Root Causes Found & Fixed**:

1. **EventAttendee not created with ticket purchases**
   - `CreateTestTicketPurchaseAsync` only created `EventAttendance`, not `EventAttendee`
   - Attendees didn't appear in check-in kiosk
   - **Fix**: Added `EventAttendee` creation to `TestHelperService.cs`

2. **Tests searching by email but UI shows sceneName**
   - Kiosk displays user's sceneName, not email
   - **Fix**: Updated tests to use unique sceneName and search by that

3. **Missing navigation before page.evaluate() fetch calls**
   - Relative URLs don't work without a page context
   - **Fix**: Added `await page.goto('/')` before API calls in tests

**Commits**:
- `052a5e5f` test: fix check-in E2E test failures
- `d9319d37` test: update admin-checkin-sessions tests for checkbox UI
- `749096ef` test: fix duplicate SessionCode errors in admin-checkin-sessions tests

---

## CURRENT FAILURES BY CATEGORY

### 1. DataFactory Session Creation (400 Errors) - ✅ FIXED

**Root Cause**: Tests creating multiple sessions without unique `sessionIdentifier`

**Status**: **FIXED** on 2025-12-13

**Files Fixed** (8 files, ~25 tests):
- ✅ `admin-session-deletion.spec.ts` (5 tests) - PASSING
- ✅ `comprehensive-timing-tests.spec.ts` (12 tests) - PASSING
- ✅ `multi-ticket-purchase.spec.ts` (3 tests) - PASSING
- ✅ `session-availability-counts.spec.ts` (7 tests) - PASSING
- ✅ `session-ticket-availability.spec.ts` (7 tests) - 4 passing, 3 failing (unrelated business logic issues)
- ✅ `ticket-cancellation-selective.spec.ts` (3 tests) - PASSING
- ✅ `volunteer-auto-cancel.spec.ts` (3 tests) - PASSING
- ✅ `volunteer-session-validation.spec.ts` (2 tests) - PASSING

**Fix Applied**:
Added unique `sessionIdentifier: 'S1'`, `'S2'`, etc. to all `df.sessions.create()` calls.

**Remaining Issues** (3 tests in session-ticket-availability.spec.ts):
These failures are **NOT** sessionIdentifier issues - they're business logic assertion failures
where tests expect `canPurchase: false` for past sessions but API returns differently.

---

### 2. Vetting Modal Visibility (UI Timing) - 6 failures

**Root Cause**: Deny/Hold/Interview modals exist in DOM but not visible due to animation timing

**Affected Tests**:
- `vetting-application-detail.spec.ts`:
  - admin can deny application with reasoning
  - admin can put application on hold with reasoning
  - admin can advance application to interview stage
- `vetting-workflow.spec.ts`:
  - admin can deny application with reason

**Fix Required**: Add wait for modal animation or use `force: true` for hidden elements

---

### 3. UI Selector/Element Issues - ~15 failures

**Affected Tests**:
- `admin-events-workflow.spec.ts` (2 tests) - strict mode violation, multiple Save buttons
- `admin-dashboard-workflow.spec.ts` (2 tests) - Google Drive links, investigation notes
- `anonymous-report-submission.spec.ts` (2 tests) - form visibility
- `tiptap-editors.spec.ts` (2 tests) - Email tab editor not visible
- `venue-display.spec.ts` (2 tests) - CSS selector syntax error
- `venue-creation.spec.ts`, `venue-editing.spec.ts` - timeout issues

---

### 4. API/Data Issues - ~10 failures

- `csrf-token-validation.spec.ts` - CSRF token expectations mismatch
- `events-basic-validation.spec.ts` - API response format
- `public-events-anonymous.spec.ts` - EventDto structure mismatch
- `ticket-lifecycle-persistence.spec.ts` - participation record not found
- `profile-update-persistence.spec.ts` - persistence verification failing

---

### 5. Test Environment/Infrastructure - ~5 failures

- `compare-wireframe.spec.ts` - localhost:8080 connection refused (docs server not running)
- `e2e-events-full-journey.spec.ts` - environment health check expectations
- `events-comprehensive.spec.ts` - performance test with large events

---

### 6. RSVP/Ticket Workflow Issues - ~5 failures

- `rsvp-lifecycle-persistence.spec.ts` (2 tests) - RSVP visibility issues
- `session-based-ticket-timing.spec.ts` - ticket availability UI
- `events-comprehensive.spec.ts` - RSVP AND ticket purchase parallel actions

---

## Key Mantine v7 Testing Patterns

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

### TipTap/ProseMirror
- Don't use `.fill()` on contenteditable divs
- Use `click()` -> `keyboard.press('Control+a')` -> `keyboard.type(text)`

### DataFactory Sessions
- **ALWAYS** specify unique `sessionIdentifier` when creating multiple sessions per event
- Format: 'S1', 'S2', 'S3', etc.

---

## Test Results Location

- `/test-results/test-results.json` - Full Playwright JSON report
- `/test-results/test-summary.txt` - Human-readable summary
- `/test-results/html-report/` - Interactive HTML report

---

## Priority Fixes for Next Agent

1. **HIGH**: Fix DataFactory session tests (~25 failures)
   - Add unique sessionIdentifier to all multi-session test files
   - Pattern established in `admin-checkin-sessions.spec.ts`

2. **MEDIUM**: Fix vetting modal tests (6 failures)
   - Debug modal visibility timing
   - May need wait for animation

3. **MEDIUM**: Fix venue tests (4 failures)
   - CSS selector syntax errors
   - Timeout issues

4. **LOW**: Fix infrastructure tests (5 failures)
   - Some tests expect specific environment configurations

---

**Last Updated**: 2025-12-13T21:10:00Z
**Test Run By**: test-environment skill
**Git SHA**: 052a5e5f
