# E2E Failing Tests Tracker

## Purpose

Track currently failing E2E tests, their root causes, and fix instructions for the next agent.

## Current Test Run (December 13, 2025 - Post Test Fixes)

| Metric | Value |
|--------|-------|
| **Total Tests** | 794 |
| **Passed** | 727 |
| **Failed** | 38 |
| **Skipped** | 29 |
| **Pass Rate** | **91.5%** |

### Progress Since Last Baseline
| Metric | Previous | Current | Change |
|--------|----------|---------|--------|
| Passed | ~710 | 727 | +17 |
| Failed | ~55 | 38 | -17 ✅ |
| Pass Rate | ~89% | 91.5% | +2.5% ✅ |

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

## CURRENT FAILURES BY CATEGORY (38 Total)

### 1. Admin Events & Volunteers - 6 failures

**admin-events-dashboard.spec.ts** (2 failures):
- ✗ should filter events by type when unchecking filters
- ✗ should show both filter chips checked by default

**admin-events-volunteers.spec.ts** (4 failures):
- ✗ should add volunteer position via inline form
- ✗ should display sessions in day format in position assignments
- ✗ should show only current event sessions in dropdown
- ✗ should validate volunteer position form fields

---

### 2. Admin Ticket/Session Operations - 3 failures

**admin-refund-eligibility.spec.ts** (1 failure):
- ✗ multiple refunds can be processed in sequence

**admin-session-deletion.spec.ts** (1 failure):
- ✗ cannot delete session with paid tickets - shows blocked modal with disabled button

**admin-tickettype-deletion.spec.ts** (1 failure):
- ✗ ticket type deletion shows correct sales count in blocked modal

---

### 3. Anonymous Report Submission - 2 failures

**anonymous-report-submission.spec.ts** (2 failures):
- ✗ should submit anonymous incident report and receive reference number
- ✗ should validate required fields before submission

**Note**: Fix was implemented but NOT COMMITTED - needs to be committed!

---

### 4. Events Workflow Tests - 9 failures

**events-basic-validation.spec.ts** (1 failure):
- ✗ Events Page Loading and Content Detection

**events-complete-workflow.spec.ts** (1 failure):
- ✗ Step 2: Admin Event Editing - Login as admin and update event details

**events-comprehensive.spec.ts** (2 failures):
- ✗ should handle large number of events efficiently
- ✗ social event should offer RSVP AND ticket purchase as parallel actions

**events-management-e2e.spec.ts** (3 failures):
- ✗ should display event form tabs
- ✗ should load Event Session Matrix demo page
- ✗ should verify form fields are present

**event-update-complete-flow.spec.ts** (1 failure):
- ✗ Admin can update event without getting logged out

---

### 5. Home Page Tests - 2 failures

**home-page.spec.ts** (2 failures):
- ✗ events display from API with complete card structure
- ✗ proves complete React + API + PostgreSQL stack works

---

### 6. Session/Ticket Availability - 3 failures

**session-ticket-availability.spec.ts** (3 failures):
- ✗ API returns correct ticket availability status
- ✗ Both Sessions ticket uses EARLIEST session (S1) - NOT purchasable
- ✗ S1 Only ticket should NOT be available (timing window closed)

**Root Cause**: Business logic assertion failures - tests expect `canPurchase: false` for past sessions but API returns differently.

---

### 7. Venue Tests - 2 failures

**venue-creation.spec.ts** (1 failure):
- ✗ should create new venue with all fields

**venue-editing.spec.ts** (1 failure):
- ✗ should update venue notes (admin-only field)

---

### 8. Vetting Workflow - 2 failures

**vetting-workflow.spec.ts** (2 failures):
- ✗ admin can deny application with reason
- ✗ user can submit vetting application successfully

---

### 9. Test Environment/Infrastructure - 2 failures

**compare-wireframe.spec.ts** (1 failure):
- ✗ capture original wireframe (localhost:8080 connection refused - docs server not running)

**e2e-events-full-journey.spec.ts** (1 failure):
- ✗ Environment Health Check

---

### 10. Miscellaneous - 7 failures

**notification-system-test.spec.ts** (1 failure):
- ✗ Notifications container appears when notification is shown

**phase3-sessions-tickets.spec.ts** (2 failures):
- ✗ Session CRUD - Add, edit, and delete sessions
- ✗ Ticket Types - Create and manage ticket types

**phase4-events-testing.spec.ts** (1 failure):
- ✗ should display event filters correctly

**profile-page.spec.ts** (1 failure):
- ✗ should handle user loading error

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

1. **IMMEDIATE**: Commit anonymous-report-submission.spec.ts fix
   - Fix was implemented but not committed
   - Will eliminate 2 failures

2. **HIGH**: Fix admin-events-volunteers tests (4 failures)
   - Volunteer position UI tests failing
   - Session dropdown/display issues

3. **HIGH**: Fix events workflow tests (9 failures)
   - Multiple event management tests failing
   - Likely UI selector or state issues

4. **MEDIUM**: Fix home-page tests (2 failures)
   - Event card structure validation failing
   - May be data or timing issues

5. **MEDIUM**: Fix session-ticket-availability tests (3 failures)
   - Business logic assertion mismatches
   - Tests expect `canPurchase: false` but API returns differently

6. **LOW**: Fix infrastructure tests (2 failures)
   - compare-wireframe needs docs server running
   - Environment health check expectations

---

**Last Updated**: 2025-12-13T19:22:04Z
**Test Run By**: test-environment skill
**Git SHA**: c8a7aa8f
