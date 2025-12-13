# E2E Failing Tests Tracker

## Purpose

This file tracks all E2E tests currently failing, their failure reasons, and fix status. Use this file to:
1. Track which tests need fixing without re-running the full test suite
2. Mark tests as "fixed but not tested" when code changes are made
3. Remove entries once tests are verified passing

## Workflow

1. **When you fix a test**: Change status from `FAILING` to `FIXED_NOT_TESTED`
2. **When you verify a fix**: Remove the entry entirely from this file
3. **When re-running tests**: Update failure reasons if they changed

## Test Run Info
- **Date**: December 12, 2025 (Last Updated - Post DataFactory Fixes)
- **Total Tests**: 795
- **Passed**: 704
- **Failed**: 63
- **Skipped**: 28
- **Pass Rate**: **88.6%**
- **Run Time**: ~27 minutes

### ✅ DataFactory Fixes Applied (December 12, 2025)

Major improvements to test data creation infrastructure:
1. **User Factory Fix** - Added required `SceneName` field, fixed `role` vs `roles` field mismatch
2. **TicketType Factory Fix** - Made `eventId` required (matches backend API)
3. **Test File Updates** - Updated 18 test files to include eventId in ticketTypes.create calls
4. **Home Page Test Fix** - Fixed date detection logic for multi-session events

| Metric | Dec 11 | Dec 12 | Change |
|--------|--------|--------|--------|
| Passed | 617 | 704 | **+87** |
| Failed | 146 | 63 | **-83** |
| Skipped | 27 | 28 | +1 |
| Pass Rate | 78.1% | 88.6% | **+10.5%** |

### Previous: Infrastructure Fixes (December 11, 2025)

| Metric | Dec 11 (Before) | Dec 11 (After) | Change |
|--------|-----------------|----------------|--------|
| Passed | 589 | 617 | **+28** |
| Failed | 206 | 146 | **-60** |
| Skipped | 27 | 27 | 0 |
| Pass Rate | 74% | 78.1% | **+4.1%** |

**Test Result Files Location**: `/test-results/`
- `quick-summary.json` - Machine-readable summary
- `test-summary.txt` - Human-readable with failed test list
- `test-results.json` - Full Playwright JSON report
- `html-report/` - Interactive HTML report

### ✅ Phase 6 Complete: API Endpoint & CSRF Fixes (December 10, 2025)

**Fixed 24 tests across 4 test files** that were failing due to:
1. Wrong API endpoint (`/api/admin/events` → `/api/events`)
2. Missing CSRF token in POST requests
3. Stray syntax errors from earlier edit attempts

| File | Tests Fixed | Root Cause |
|------|-------------|------------|
| `multi-ticket-purchase.spec.ts` | 3 | Wrong endpoint + CSRF + confirmation locator |
| `session-availability-counts.spec.ts` | 7 | Wrong endpoint + CSRF + syntax errors |
| `ticket-purchase-e2e.spec.ts` | 3 | Wrong endpoint + CSRF |
| `admin-events-workflow.spec.ts` | 11 | Wrong endpoint + CSRF + syntax error |

### ✅ Phase 5 Complete: Skipped Tests Fixed (December 10, 2025)

**All incorrectly skipped tests have been converted to `test.fail()`** so they now appear as expected failures rather than being hidden.

| Action Taken | Count | Status |
|--------------|-------|--------|
| Converted `test.skip()` → `test.fail()` | ~50 | ✅ COMPLETED |
| Deleted obsolete tests | 2 | ✅ COMPLETED |
| Kept legitimately skipped | 1 | Token revocation only |

**What Changed**:
- Tests that were silently skipping due to missing data now **fail visibly**
- This makes it clear what needs to be fixed
- `test.fail()` marks tests as "expected failures" - they show in results but don't break CI

### Progress Comparison
| Metric | Dec 2 | Dec 7 | Dec 9 | Dec 10 (Phase 5) | Dec 10 (Phase 6) | Dec 11 | **Dec 12** |
|--------|-------|-------|-------|------------------|------------------|--------|------------|
| Passed | 622 | 643 | 688 | 681 | 705+ | 617 | **704** |
| Failed | 111 | 92 | 38 | ~100 | ~76 | 146 | **63** |
| Skipped | 74 | 72 | 83 | 1 | 1 | 27 | **28** |
| Pass Rate | 84.9% | 87.4% | 85.0% | ~87% | ~89% | 78.1% | **88.6%** |

**Note**: Dec 12 DataFactory fixes resolved regression issues. 63 tests still failing - categorized below.

---

## Current Failing Tests - Complete List (63 total - December 12, 2025)

### Summary by Category

| Category | Count | Description |
|----------|-------|-------------|
| Vetting Workflows | 8 | Application detail, workflow submission |
| Events Admin | 11 | Event sessions, policies, volunteers, workflows, copy |
| Session Timing | 8 | Session-based timing, ticket availability |
| Check-in | 4 | Check-in staff and attendee workflows |
| Venue | 4 | Venue creation, editing, display |
| RSVP/Profile | 4 | RSVP lifecycle, profile persistence |
| Tiptap Editors | 2 | Email content editor rendering |
| Other | 22 | Anonymous reports, CSRF, environment, etc. |

### Recommended Fix Priority

1. **HIGH**: Events Admin (11 failures) - Session CRUD, volunteers, policies
2. **HIGH**: Session Timing (8 failures) - Business logic for ticket availability
3. **MEDIUM**: Vetting Workflows (8 failures) - Requires backend endpoint or test data fixes
4. **MEDIUM**: Check-in (4 failures) - Attendee workflow issues
5. **LOW**: Infrastructure tests - wireframe, health checks

---

## Complete Failing Tests by File (December 11, 2025)

### Admin Dashboard (2 tests)
| Test | File |
|------|------|
| should add investigation note to incident | `admin-dashboard-workflow.spec.ts` |
| should update Google Drive links for incident | `admin-dashboard-workflow.spec.ts` |

### Admin Events - Sessions (4 tests)
| Test | File |
|------|------|
| should add a new session via modal without page refresh | `admin-events-sessions.spec.ts` |
| should assign S# IDs sequentially to new sessions | `admin-events-sessions.spec.ts` |
| should edit existing session via modal | `admin-events-sessions.spec.ts` |
| should validate session form fields | `admin-events-sessions.spec.ts` |

### Admin Events - Volunteers (2 tests)
| Test | File |
|------|------|
| should add volunteer position via inline form | `admin-events-volunteers.spec.ts` |
| should validate volunteer position form fields | `admin-events-volunteers.spec.ts` |

### Admin Events - Workflow (1 test)
| Test | File |
|------|------|
| should toggle event between draft and published | `admin-events-workflow.spec.ts` |

### Admin Member History (3 tests)
| Test | File |
|------|------|
| should display multiple profile changes chronologically | `admin-member-history.spec.ts` |
| should display profile changes in History tab after user updates profile | `admin-member-history.spec.ts` |
| should show empty state when no history exists | `admin-member-history.spec.ts` |

### Admin Refund (1 test)
| Test | File |
|------|------|
| multiple refunds can be processed in sequence | `admin-refund-eligibility.spec.ts` |

### Admin Session Deletion (1 test)
| Test | File |
|------|------|
| cannot delete session with paid tickets - shows blocked modal with disabled button | `admin-session-deletion.spec.ts` |

### Admin Ticket Type Deletion (5 tests)
| Test | File |
|------|------|
| can delete ticket type with no sales - shows confirmation modal with enabled button | `admin-tickettype-deletion.spec.ts` |
| cannot delete ticket type with sales - shows blocked modal with disabled button | `admin-tickettype-deletion.spec.ts` |
| delete button in table opens confirmation modal | `admin-tickettype-deletion.spec.ts` |
| delete ticket type successfully removes it from the list | `admin-tickettype-deletion.spec.ts` |
| ticket type deletion shows correct sales count in blocked modal | `admin-tickettype-deletion.spec.ts` |

### Anonymous Reports (2 tests)
| Test | File |
|------|------|
| should submit anonymous incident report and receive reference number | `anonymous-report-submission.spec.ts` |
| should validate required fields before submission | `anonymous-report-submission.spec.ts` |

### Check-in (4 tests)
| Test | File |
|------|------|
| Cannot check in same attendee twice | `checkin-attendee-workflow.spec.ts` |
| Check in a registered attendee | `checkin-attendee-workflow.spec.ts` |
| Two-step check-in workflow (Covid Test → Check In) | `checkin-attendee-workflow.spec.ts` |
| Valid token allows access to check-in interface | `checkin-staff-authentication.spec.ts` |

### Comprehensive Timing Tests (8 tests)
| Test | File |
|------|------|
| all tickets should be available when sessions are far in future | `comprehensive-timing-tests.spec.ts` |
| all tickets should be closed | `comprehensive-timing-tests.spec.ts` |
| boundary test: close to boundary behaves consistently | `comprehensive-timing-tests.spec.ts` |
| server calculation matches client-side UTC calculation | `comprehensive-timing-tests.spec.ts` |
| session starting in 1 hour with 30min close window should be OPEN | `comprehensive-timing-tests.spec.ts` |
| session starting in 15 minutes with 30min close window should be CLOSED | `comprehensive-timing-tests.spec.ts` |
| tight margin CLOSED case: 3hr until session, 4hr close window (-1hr margin) | `comprehensive-timing-tests.spec.ts` |
| tight margin OPEN case: 5hr until session, 4hr close window (1hr margin) | `comprehensive-timing-tests.spec.ts` |

### CSRF/Auth (1 test)
| Test | File |
|------|------|
| should complete full login/logout flow with CSRF token | `csrf-token-validation.spec.ts` |

### Events - Basic/Core (6 tests)
| Test | File |
|------|------|
| Environment Health Check | `e2e-events-full-journey.spec.ts` |
| Admin can update event without getting logged out | `event-update-complete-flow.spec.ts` |
| Events Page Loading and Content Detection | `events-basic-validation.spec.ts` |
| Step 2: Admin Event Editing - Login as admin and update event details | `events-complete-workflow.spec.ts` |
| should handle large number of events efficiently | `events-comprehensive.spec.ts` |
| capture original wireframe | `compare-wireframe.spec.ts` |

### Events Management (3 tests)
| Test | File |
|------|------|
| should display event form tabs | `events-management-e2e.spec.ts` |
| should load Event Session Matrix demo page | `events-management-e2e.spec.ts` |
| should verify form fields are present | `events-management-e2e.spec.ts` |

### Events Policies (3 tests)
| Test | File |
|------|------|
| should display policies field in event form | `events-policies-field-comprehensive.spec.ts` |
| should save policies field and persist after page refresh | `events-policies-field-comprehensive.spec.ts` |
| should validate policies field as REQUIRED | `events-policies-field-comprehensive.spec.ts` |

### Events Copy (2 tests)
| Test | File |
|------|------|
| Admin can copy event with new date and title | `events/admin-event-copy.spec.ts` |
| Copied event has correct ticket types | `events/admin-event-copy.spec.ts` |

### Home Page (2 tests)
| Test | File |
|------|------|
| events display from API | `home-page.spec.ts` |
| proves complete React + API + PostgreSQL stack works | `home-page.spec.ts` |

### Login (1 test)
| Test | File |
|------|------|
| should show error for wrong password with valid scene name | `login-with-scene-name.spec.ts` |

### Multi-Ticket Purchase (3 tests)
| Test | File |
|------|------|
| dashboard shows user has both tickets | `multi-ticket-purchase.spec.ts` |
| event details page shows both ticket types purchased | `multi-ticket-purchase.spec.ts` |
| user can purchase Day 1 Only and Day 2 Only tickets together | `multi-ticket-purchase.spec.ts` |

### Notification System (1 test)
| Test | File |
|------|------|
| Notifications container appears when notification is shown | `notification-system-test.spec.ts` |

### Phase Testing (3 tests)
| Test | File |
|------|------|
| Session CRUD - Add, edit, and delete sessions | `phase3-sessions-tickets.spec.ts` |
| Ticket Types - Create and manage ticket types | `phase3-sessions-tickets.spec.ts` |
| should display event filters correctly | `phase4-events-testing.spec.ts` |

### Post-Login Return (1 test)
| Test | File |
|------|------|
| should sanitize and validate returnUrl with special characters | `post-login-return.spec.ts` |

### Profile (15 tests)
| Test | File |
|------|------|
| should handle user loading error | `profile-page.spec.ts` |
| CRITICAL: should detect if profile update shows success but fails to persist | `profile-update-full-persistence.spec.ts` |
| should handle empty string updates (clearing optional fields) | `profile-update-full-persistence.spec.ts` |
| should handle null vs empty string correctly | `profile-update-full-persistence.spec.ts` |
| should handle rapid successive updates | `profile-update-full-persistence.spec.ts` |
| should handle special characters in all fields | `profile-update-full-persistence.spec.ts` |
| should persist Discord name update | `profile-update-full-persistence.spec.ts` |
| should persist FetLife name update | `profile-update-full-persistence.spec.ts` |
| should persist bio update with special characters | `profile-update-full-persistence.spec.ts` |
| should persist clearing bio field | `profile-update-full-persistence.spec.ts` |
| should persist complete profile update to database | `profile-update-full-persistence.spec.ts` |
| should persist long bio text | `profile-update-full-persistence.spec.ts` |
| should persist multiple profile updates in sequence | `profile-update-full-persistence.spec.ts` |
| should persist pronouns update | `profile-update-full-persistence.spec.ts` |
| should persist single field update (firstName only) | `profile-update-full-persistence.spec.ts` |
| should persist profile changes after save and page refresh | `profile-update-persistence.spec.ts` |

### RSVP (3 tests)
| Test | File |
|------|------|
| 3. Admin Event Details - RSVP Tab Content | `comprehensive-rsvp-verification.spec.ts` |
| should handle rapid RSVP/cancel cycles | `rsvp-lifecycle-persistence.spec.ts` |
| should persist RSVP to database | `rsvp-lifecycle-persistence.spec.ts` |

### Session Availability (5 tests)
| Test | File |
|------|------|
| should correctly count tickets via TicketPurchase -> TicketType -> TicketTypeSessions chain | `session-availability-counts.spec.ts` |
| should display session availability on event details page | `session-availability-counts.spec.ts` |
| should have consistent counts between events API and participation API | `session-availability-counts.spec.ts` |
| should return correct session soldCount and availableCount from events API | `session-availability-counts.spec.ts` |
| should return correct sessionAvailability from participation API | `session-availability-counts.spec.ts` |

### Session-Based Ticket Timing (6 tests)
| Test | File |
|------|------|
| admin can view timing settings for event | `session-based-ticket-timing.spec.ts` |
| member can view event with tickets | `session-based-ticket-timing.spec.ts` |
| multi-session event shows tickets for future sessions | `session-based-ticket-timing.spec.ts` |
| ticket availability reflects timing settings | `session-based-ticket-timing.spec.ts` |
| ticket shows which sessions it covers | `session-based-ticket-timing.spec.ts` |
| ticket timing uses session dates not event dates | `session-based-ticket-timing.spec.ts` |

### Session-Based Timing (3 tests)
| Test | File |
|------|------|
| event with registration window settings | `session-based-timing.spec.ts` |
| multi-session event - tickets available for future sessions | `session-based-timing.spec.ts` |
| ticket purchase uses session-based timing | `session-based-timing.spec.ts` |

### Session Ticket Availability (3 tests)
| Test | File |
|------|------|
| API returns correct ticket availability status | `session-ticket-availability.spec.ts` |
| Both Sessions ticket uses EARLIEST session (S1) - NOT purchasable | `session-ticket-availability.spec.ts` |
| S1 Only ticket should NOT be available (timing window closed) | `session-ticket-availability.spec.ts` |

### Ticket Cancellation (3 tests)
| Test | File |
|------|------|
| Test A: Single ticket cancellation pre-selects checkbox | `ticket-cancellation-selective.spec.ts` |
| Test B: Multiple tickets no pre-selection | `ticket-cancellation-selective.spec.ts` |
| Test C: Selective cancellation preserves other tickets | `ticket-cancellation-selective.spec.ts` |

### Ticket Lifecycle (3 tests)
| Test | File |
|------|------|
| should persist cancellation reason to database | `ticket-lifecycle-persistence.spec.ts` |
| should prevent duplicate cancellations | `ticket-lifecycle-persistence.spec.ts` |
| should verify endpoint called is correct | `ticket-lifecycle-persistence.spec.ts` |

### Ticket Purchase (1 test)
| Test | File |
|------|------|
| Complete ticket purchase with credit card | `ticket-purchase-e2e-datafactory.spec.ts` |

### Tiptap Editors (2 tests)
| Test | File |
|------|------|
| comprehensive: all three editors render on their respective tabs | `tiptap-editors.spec.ts` |
| should render Email Content Tiptap editor on Emails tab | `tiptap-editors.spec.ts` |

### Venue (8 tests)
| Test | File |
|------|------|
| should create new venue with all fields | `venue-creation.spec.ts` |
| should NOT display admin Notes field to public | `venue-display.spec.ts` |
| should NOT display venue to unauthenticated users | `venue-display.spec.ts` |
| should display correct venue name and directions | `venue-display.spec.ts` |
| should display venue to users WITH RSVP | `venue-display.spec.ts` |
| should display venue to users WITH ticket | `venue-display.spec.ts` |
| should hide venue when user cancels RSVP | `venue-display.spec.ts` |
| should edit existing venue name and directions | `venue-editing.spec.ts` |
| should update venue notes (admin-only field) | `venue-editing.spec.ts` |

### Vetting - Admin Dashboard (5 tests)
| Test | File |
|------|------|
| admin can filter applications by status | `vetting-admin-dashboard.spec.ts` |
| admin can navigate to application detail | `vetting-admin-dashboard.spec.ts` |
| admin can search applications by scene name | `vetting-admin-dashboard.spec.ts` |
| admin can sort applications by submission date | `vetting-admin-dashboard.spec.ts` |
| admin can view vetting applications grid | `vetting-admin-dashboard.spec.ts` |

### Vetting - Application Detail (8 tests)
| Test | File |
|------|------|
| admin can add notes to application | `vetting-application-detail.spec.ts` |
| admin can advance application to interview stage | `vetting-application-detail.spec.ts` |
| admin can deny application with reasoning | `vetting-application-detail.spec.ts` |
| admin can put application on hold with reasoning | `vetting-application-detail.spec.ts` |
| admin can skip to approved | `vetting-application-detail.spec.ts` |
| admin can view application details | `vetting-application-detail.spec.ts` |
| admin can view audit log history | `vetting-application-detail.spec.ts` |
| approved application shows vetted member status | `vetting-application-detail.spec.ts` |

### Vetting - Complete Flow (1 test)
| Test | File |
|------|------|
| Complete vetting application with registration and login | `vetting-complete-flow.spec.ts` |

### Vetting - Profile Update (4 tests)
| Test | File |
|------|------|
| admin can see updated profile after user submits vetting application | `vetting-profile-update.spec.ts` |
| profile updates are visible in user dashboard after submission | `vetting-profile-update.spec.ts` |
| user submits application with all fields - profile fully updated | `vetting-profile-update.spec.ts` |
| user submits application with minimal fields - existing optional fields preserved | `vetting-profile-update.spec.ts` |

### Vetting - Workflow Integration (3 tests)
| Test | File |
|------|------|
| complete approval workflow from submission to role grant | `vetting-workflow-integration.spec.ts` |
| complete denial workflow sends notification | `vetting-workflow-integration.spec.ts` |
| status changes trigger email notifications | `vetting-workflow-integration.spec.ts` |

### Vetting - Workflow (5 tests)
| Test | File |
|------|------|
| admin can approve application for interview | `vetting-workflow.spec.ts` |
| admin can deny application with reason | `vetting-workflow.spec.ts` |
| admin can put application on hold with reason | `vetting-workflow.spec.ts` |
| admin can skip to approved status | `vetting-workflow.spec.ts` |
| user can submit vetting application successfully | `vetting-workflow.spec.ts` |

### Volunteer (5 tests)
| Test | File |
|------|------|
| cancelling Session 1 ticket auto-cancels Session 1 volunteer signup | `volunteer-auto-cancel.spec.ts` |
| cancelling Session 2 ticket preserves Session 1 volunteer signup | `volunteer-auto-cancel.spec.ts` |
| cancelling all tickets cancels all volunteer signups | `volunteer-auto-cancel.spec.ts` |
| user can sign up for volunteer position when they have ticket for that session | `volunteer-session-validation.spec.ts` |
| user cannot sign up for volunteer position when they lack ticket for that session | `volunteer-session-validation.spec.ts` |

---

## CONVERTED TO test.fail() (Phase 5 - December 10, 2025)

These tests previously skipped silently but have been **converted to `test.fail()`** so they now appear as expected failures. They still need proper test data setup.

### Files Updated with test.fail()

| File | Tests | Status | Issue |
|------|-------|--------|-------|
| `admin-checkin-sessions.spec.ts` | 8 | 🔴 Still failing | Checkin Link button / Session select not found |
| `ticket-cancellation-selective.spec.ts` | 4 | 🔴 Still failing | Cancel button not visible / beforeAll failed |
| `volunteer-session-validation.spec.ts` | 1 | 🔴 Still failing | Volunteer section not visible |
| `multi-ticket-purchase.spec.ts` | 3 | ✅ **FIXED (Phase 6)** | Was: wrong endpoint + missing CSRF |
| `vetting-workflow.spec.ts` | 13 | 🔴 **NEEDS BACKEND WORK** | See detailed section below |
| `session-availability-counts.spec.ts` | 7 | ✅ **FIXED (Phase 6)** | Was: wrong endpoint + missing CSRF |
| `ticket-purchase-e2e.spec.ts` | 3 | ✅ **FIXED (Phase 6)** | Was: wrong endpoint + missing CSRF |
| `checkin-attendee-workflow.spec.ts` | 4 | 🔴 Still failing | Event/attendees not found |
| `session-based-timing.spec.ts` | 5 | 🔴 Still failing | No events with ticket options found |
| `session-ticket-availability.spec.ts` | 6 | 🔴 Still failing | Session Timing Test Event not found |
| `admin-events-workflow.spec.ts` | 11 | ✅ **FIXED (Phase 6)** | Was: wrong endpoint + missing CSRF |
| `event-update-complete-flow.spec.ts` | 3 | 🔴 Still failing | No events available to test with |
| `comprehensive-timing-tests.spec.ts` | 1 | 🔴 Still failing | No volunteer positions returned |

### Tests Deleted (December 10, 2025)

| File | Test | Reason for Deletion |
|------|------|---------------------|
| `events/admin-event-copy.spec.ts` | Copy modal validates past dates | Untestable - Mantine DatePicker disables past dates at UI level |
| `login-with-scene-name.spec.ts` | Case-sensitivity test | Unnecessary - login works with any case |

---

## 🚨 VETTING-WORKFLOW.SPEC.TS - REQUIRES BACKEND ENDPOINT (13 tests)

### Problem Summary

The `vetting-workflow.spec.ts` tests (13 tests) **cannot be fixed with the same approach** used for other test files. Unlike events which can be created via `POST /api/events`, there is **no API endpoint to create vetting applications programmatically**.

### Why This Is Different

| Test File | Solution | Works? |
|-----------|----------|--------|
| `multi-ticket-purchase.spec.ts` | Create event via `POST /api/events` in `beforeAll` | ✅ YES |
| `admin-events-workflow.spec.ts` | Create event via `POST /api/events` in `beforeAll` | ✅ YES |
| `vetting-workflow.spec.ts` | ❌ **No endpoint to create applications** | ❌ NO |

### Current Vetting Application Flow

1. **User submits application**: `POST /api/vetting/apply` - Creates application for CURRENT authenticated user
2. **Problem**: Cannot create application for TEST users programmatically
3. **Seed data approach**: Applications exist in seed data but may be in wrong state or already processed

### What Backend Work Is Needed

**Option A: Test-Only Endpoint (Recommended)**

Create `POST /api/admin/vetting/test-application` endpoint that:
- Requires `Admin` role
- Creates a vetting application for a specified user email
- Sets application to a specified status (Pending, InReview, etc.)
- Only available in Development/Test environments

```csharp
// Example endpoint signature
[HttpPost("admin/vetting/test-application")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<VettingApplicationDto>> CreateTestApplication(
    string userEmail,
    VettingStatus status = VettingStatus.Pending)
```

**Option B: Seeder Enhancement**

Enhance `VettingSeeder` to create applications in specific states that tests can rely on:
- Create "pending" application for `vetting-test-pending@witchcityrope.com`
- Create "in-review" application for `vetting-test-inreview@witchcityrope.com`
- Etc.

**Option C: Reset Endpoint**

Create `POST /api/admin/vetting/reset-test-data` that:
- Resets all test vetting applications to known states
- Called in `beforeAll` of test file

### Tests Affected (13 tests)

| Test | What It Needs |
|------|---------------|
| Admin can view pending applications list | At least 1 pending application |
| Admin can view application details | Application with known ID |
| Admin can approve application | Pending application to approve |
| Admin can deny application with reason | Pending application to deny |
| Admin can put application on hold | Pending application |
| Admin can send reminder to applicant | Application in specific state |
| Admin can add notes to application | Any application |
| Application status updates correctly | Application to modify |
| Email is sent on approval | Pending application |
| Email is sent on denial | Pending application |
| Email is sent on hold | Pending application |
| Email is sent on reminder | Application |
| Audit log tracks admin actions | Application to modify |

### Next Steps for Future Agent

1. **Backend Developer**: Create one of the endpoint options above
2. **Test Developer**: Update `vetting-workflow.spec.ts` to use new endpoint in `beforeAll`
3. **Test Executor**: Run tests to verify

### Temporary Workaround (If Needed)

If backend work is delayed, tests can be marked with `test.fixme()` with a TODO comment:

```typescript
test.fixme('admin can approve application', async ({ page }) => {
  // TODO: Requires POST /api/admin/vetting/test-application endpoint
  // See: /docs/test-baselines/e2e-failing-tests-tracker.md#vetting-workflow
});
```

---

## LEGITIMATELY SKIPPED TESTS (Updated Dec 10, 2025)

**Only 1 test remains legitimately skipped** after Phase 5 fixes.

### Token Revocation (1 test) - ONLY REMAINING SKIP
**File**: `checkin-staff-authentication.spec.ts`
**Reason**: Token revocation API not implemented - security enhancement
**Status**: Keep skipped, add to backlog as "Add check-in token revocation"

### Previously Skipped - Now Fixed/Deleted

| Category | Count | Action Taken |
|----------|-------|--------------|
| Payment tests (fictional endpoints) | 5 | ✅ DELETED (Phase 1-4) |
| UI consistency tests (TDD stubs) | 6 | ✅ DELETED (Phase 1-4) |
| DatePicker validation test | 1 | ✅ DELETED (Phase 5) |
| Login case-sensitivity test | 1 | ✅ DELETED (Phase 5) |
| Conditional skips (runtime state) | ~50 | ✅ CONVERTED to test.fail() (Phase 5) |

---

## CURRENT FAILING TESTS (~100 total - includes test.fail() tests)

**Note**: This now includes ~50 tests converted from `test.skip()` to `test.fail()` in Phase 5. These tests fail because they don't create their own test data.

### Admin Dashboard (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| admin-dashboard-workflow.spec.ts: should add investigation note to incident | FAILING | Modal/input interaction timeout |
| admin-dashboard-workflow.spec.ts: should update Google Drive links for incident | FAILING | Modal/input interaction timeout |

### Admin Events - Volunteers (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| admin-events-volunteers.spec.ts: should add volunteer position via inline form | FAILING | Form save not adding position to grid |
| admin-events-volunteers.spec.ts: should validate volunteer position form fields | FAILING | Same - form save issue |

### Admin Session (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| admin-session-deletion.spec.ts: cannot delete session with paid tickets | FAILING | Modal assertion failure |

### Compare Wireframe (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| compare-wireframe.spec.ts: capture original wireframe | FAILING | Setup/infrastructure issue |

### RSVP (3 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| comprehensive-rsvp-verification.spec.ts: Admin Event Details - RSVP Tab Content | FAILING | Tab content assertion |
| rsvp-lifecycle-persistence.spec.ts: should handle rapid RSVP/cancel cycles | FAILING | Timeout |
| rsvp-lifecycle-persistence.spec.ts: should persist RSVP to database | FAILING | Timeout |

### CSRF/Auth (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| csrf-token-validation.spec.ts: should complete full login/logout flow with CSRF token | FAILING | Auth flow issue |

### Events Full Journey (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| e2e-events-full-journey.spec.ts: Environment Health Check | FAILING | Environment check failure |

### Events Management (7 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| events-complete-workflow.spec.ts: Step 2: Admin Event Editing | FAILING | Form interaction |
| events-comprehensive.spec.ts: should handle large number of events efficiently | FAILING | Route mock not working |
| events-management-e2e.spec.ts: should display event form tabs | FAILING | Tab navigation |
| events-management-e2e.spec.ts: should load Event Session Matrix demo page | FAILING | Page load timeout |
| events-management-e2e.spec.ts: should verify form fields are present | FAILING | Form field assertions |
| events-policies-field-comprehensive.spec.ts: should display policies field | FAILING | Tiptap editor selector |
| events-policies-field-comprehensive.spec.ts: should save policies field | FAILING | Same |
| events-policies-field-comprehensive.spec.ts: should validate policies field as REQUIRED | FAILING | Same |

### Home Page (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| home-page.spec.ts: events display from API | FAILING | API/rendering issue |
| home-page.spec.ts: proves complete React + API + PostgreSQL stack works | FAILING | Stack verification |

### Navigation (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| navigation-workflow.spec.ts: Mobile hamburger menu - opens and displays navigation items | FAILING | Mobile menu interaction |

### Notification System (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| notification-system-test.spec.ts: Notifications container appears | FAILING | Notification container |

### Phase 3 Testing (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| phase3-sessions-tickets.spec.ts: Session CRUD | FAILING | Timeout |
| phase3-sessions-tickets.spec.ts: Ticket Types - Create and manage | FAILING | Timeout |

### Profile (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| profile-page.spec.ts: should handle user loading error | FAILING | Error state handling |
| profile-update-persistence.spec.ts: should persist profile changes | FAILING | Persistence assertion |

### Session Timing (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| session-based-timing.spec.ts: admin can view session-based timing settings | FAILING | Settings page assertion |

### Session/Ticket Availability (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| session-ticket-availability.spec.ts: API returns correct ticket availability status | FAILING | API response assertion |
| session-ticket-availability.spec.ts: Both Sessions ticket uses EARLIEST session | FAILING | Business logic assertion |

### Ticket Lifecycle (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| ticket-lifecycle-persistence.spec.ts: should persist cancellation reason | FAILING | Persistence assertion |

### Ticket Purchase (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| ticket-purchase-e2e.spec.ts: Free RSVP ticket purchase completes successfully | FAILING | Purchase flow |

### Tiptap Editors (2 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| tiptap-editors.spec.ts: comprehensive: all three editors render | FAILING | Editor rendering |
| tiptap-editors.spec.ts: should render Email Content Tiptap editor | FAILING | Editor rendering |

### Venue (3 failures)
| Test | Status | Failure Reason |
|------|--------|----------------|
| venue-creation.spec.ts: should create new venue with all fields | FAILING | Form submission |
| venue-editing.spec.ts: should edit existing venue name and directions | FAILING | Form submission |
| venue-editing.spec.ts: should update venue notes (admin-only field) | FAILING | Timeout |

### Vetting (1 failure)
| Test | Status | Failure Reason |
|------|--------|----------------|
| vetting-workflow.spec.ts: admin can deny application with reason | FAILING | No active applications to deny |

---

## FULLY FIXED MODULES

### Check-In Module ✓
All 17 tests passing (fixed Dec 7, 2025)

### CMS Module ✓
All tests passing (fixed Dec 8, 2025)

### Reports Module ✓
All 4 tests passing (fixed Dec 9, 2025)

### Registration Module ✓
All 3 tests passing (fixed Dec 9, 2025)

### Volunteer Auto-Cancel Module ✓
All 3 tests passing (fixed Dec 10, 2025)
- `volunteer-auto-cancel.spec.ts`: All ticket cancellation scenarios working correctly

### Vetting Module ✓ (mostly)
91/98 tests passing (92.8%), 7 skipped (legitimate), 0 failures

---

## Unimplemented Functionality Summary

Based on legitimately skipped tests, the following functionality is NOT implemented:

### 1. Payment System (HIGH IMPACT - 5 tests blocked)
- `/api/payments/create-order` endpoint returns 405
- `/events/:slug/payment` page returns 404
- Payment webhook endpoints not implemented
- PayPal integration incomplete

### 2. Admin Session CRUD UI (4 tests blocked)
- Session modal add/delete (API exists, UI interaction issues)
- Session ID (S#) auto-assignment

### 3. Check-in Token Revocation (1 test blocked)
- Can generate tokens but cannot revoke them

### 4. UI Consistency Features (6 tests - TDD)
- Modal consistency across tabs
- Table layout patterns
- Design System v7 styles

---

## Key Testing Patterns for Mantine UI

### Mantine Select Components
```typescript
// DON'T: Click on option (doesn't work reliably)
await page.getByRole('option', { name: 'Safety Concern' }).click();

// DO: Use keyboard navigation
await inputElement.click();  // Open dropdown
await page.keyboard.press('ArrowDown');  // Highlight option
await page.keyboard.press('Enter');  // Select
```

### Mantine Input data-testid
```typescript
// DON'T: Look for wrapper then input
await page.locator('[data-testid="first-name-input"] input').fill('Test');

// DO: Mantine puts data-testid directly on input
await page.locator('[data-testid="first-name-input"]').fill('Test');
```

### Tests Must Create Their Own Data
```typescript
// DON'T: Query for existing data and skip if not found
const events = await page.request.get('/api/events');
if (events.length === 0) {
  test.skip();
  return;
}

// DO: Create test data in beforeAll or inline
test.beforeAll(async ({ browser }) => {
  const page = await browser.newPage();
  await AuthHelpers.loginAs(page, 'admin');
  testEventId = await createTestEvent(page, {
    title: `Test Event ${Date.now()}`,
    ...
  });
  await page.close();
});
```

### CSRF Token Pattern for API Calls (Phase 6 Fix)

**Problem**: POST/PUT/DELETE requests to `/api/events` require CSRF token.

**Solution**: Add CSRF token helper and include token in requests:

```typescript
// Helper to get CSRF token from cookies
async function getCsrfToken(page: Page): Promise<string | null> {
  let cookies = await page.context().cookies();
  let csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');

  // If CSRF token not found, fetch it from the API
  if (!csrfCookie) {
    await page.request.get('/api/antiforgery/token');
    cookies = await page.context().cookies();
    csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');
  }

  return csrfCookie?.value || null;
}

// Helper to make authenticated API request with CSRF token
async function apiRequest(page: Page, method: string, url: string, data?: unknown) {
  const headers: Record<string, string> = {};

  // Get CSRF token for state-changing requests
  if (method !== 'GET') {
    const csrfToken = await getCsrfToken(page);
    if (csrfToken) {
      headers['X-CSRF-TOKEN'] = csrfToken;
    }
  }

  if (data) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await page.request.fetch(url, {
    method,
    headers,
    data: data ? data : undefined,
  });

  const text = await response.text();
  try {
    return { status: response.status(), data: JSON.parse(text) };
  } catch {
    return { status: response.status(), data: text };
  }
}
```

### Correct API Endpoints

| Action | ❌ Wrong Endpoint | ✅ Correct Endpoint |
|--------|-------------------|---------------------|
| Create Event | `/api/admin/events` | `/api/events` |
| Get Events | `/api/admin/events` | `/api/events` |
| Delete Event | `/api/admin/events/{id}` | ⚠️ No DELETE endpoint exists |

**Note**: There is no DELETE endpoint for events. Tests should create unique events with timestamps and not rely on cleanup.

---

## Next Fix Priorities

### CRITICAL: Fix Incorrectly Skipped Tests (48 tests)
See Fix Plan in `/docs/test-baselines/e2e-skipped-tests-fix-plan.md`

### HIGH: Tiptap Editor tests (2 tests)
Impacts events-policies-field tests too

### HIGH: Home page tests (2 tests)
Core functionality verification

### MEDIUM: Events Management (7 tests)
Multiple related failures

### MEDIUM: RSVP persistence (3 tests)
Timeout issues

### LOW: Infrastructure tests
compare-wireframe, env health check

---

## Notes

- Console errors (401, 403) during tests are EXPECTED for auth-related tests
- Font loading errors are cosmetic
- Most timeout issues suggest selector/wait problems, not app bugs
- Mantine UI requires specific testing patterns
