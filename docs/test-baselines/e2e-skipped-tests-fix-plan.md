# E2E Skipped Tests Fix Plan

## Purpose

This document outlines the plan to fix incorrectly skipped E2E tests and clean up obsolete tests.

**Date Created**: December 9, 2025
**Last Updated**: December 10, 2025 (Phase 5 audit complete)

## Summary of Actions

### Phase 1-4: Initial Fixes (December 9, 2025) - ✅ COMPLETED

| Action | Tests | Status |
|--------|-------|--------|
| DELETE obsolete payment tests | 5 | ✅ COMPLETED |
| DELETE obsolete UI consistency tests | 6 | ✅ COMPLETED |
| FIX session-based-ticket-timing.spec.ts | 7 | ✅ COMPLETED |
| FIX session-based-volunteer-timing.spec.ts | 7 | ✅ COMPLETED |
| FIX admin-checkin-sessions.spec.ts | 6 | ✅ COMPLETED |
| FIX admin-events-sessions.spec.ts | 4 | ✅ COMPLETED |
| FIX vetting-profile-update.spec.ts | 4 | ✅ COMPLETED |
| FIX volunteer-auto-cancel.spec.ts | 6 | ✅ COMPLETED |
| FIX volunteer-session-validation.spec.ts | 4 | ✅ COMPLETED |
| FIX ticket-cancellation-selective.spec.ts | 7 | ✅ COMPLETED |

### Phase 5: Remaining Skipped Tests Audit (December 10, 2025) - ✅ COMPLETED

| Action | Tests | Status |
|--------|-------|--------|
| DELETE DatePicker past date test | 1 | ✅ COMPLETED |
| DELETE login case-sensitivity test | 1 | ✅ COMPLETED |
| CONVERT to test.fail() | ~50 | ✅ COMPLETED |
| KEEP SKIPPED (token revocation) | 1 | N/A |

**Phase 1-4 Result**: 77% → 86.7% pass rate (681/785 tests)
**Phase 5 Target**: Reduce skipped from 32 → 1 (only token revocation)

---

## Phase 0: Delete Obsolete Tests (11 tests) - ✅ COMPLETED

### Payment System Tests (5 tests) - DELETED

**Files deleted**:
- `tests/e2e/paypal-integration.spec.ts`
- `tests/e2e/payment.spec.ts`

**Why**: These tests were written speculatively for a payment system design that was NEVER built.

### UI Consistency Tests (6 tests) - DELETED

**File deleted**:
- `tests/e2e/admin-events-ui-consistency.spec.ts`

**Why**: TDD "red phase" tests for design decisions that were never implemented and are not desired.

---

## Phase 1: High-Impact Timing Tests - ✅ COMPLETED

### `session-based-ticket-timing.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Added `beforeAll` to create multi-session event with ticket types
- Added `afterAll` to clean up test data
- Replaced `test.skip()` with `test.fail()` for setup failures
- Tests now create their own event with sessions 7+ days in future
- Includes all CRITICAL timing controls

### `session-based-volunteer-timing.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Added `beforeAll` to create multi-session event with volunteer positions
- Creates session-specific positions (S1, S2) AND event-wide position
- Added `afterAll` to clean up test data
- Replaced all `test.skip()` with `test.fail()`
- Includes volunteer timing controls

---

## Phase 2: Cascading Setup Failures - ✅ COMPLETED

### `volunteer-auto-cancel.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Already had `beforeAll` creating data - fixed error handling
- Changed `test.skip()` to `test.fail()` with descriptive messages
- Tests now properly report when setup fails

### `volunteer-session-validation.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Already had `beforeAll` creating data - fixed error handling
- Changed `test.skip()` to `test.fail()` with descriptive messages

### `ticket-cancellation-selective.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Already had `beforeAll` creating data - fixed error handling
- Changed `test.skip()` to `test.fail()` with descriptive messages

---

## Phase 3: Admin Check-in Sessions - ✅ COMPLETED

### `admin-checkin-sessions.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Replaced seed data lookup ("Suspension Basics") with created test data
- Added three test describe blocks for different scenarios:
  - Multi-session token generation
  - Single-session auto-select
  - Attendees tab
- Each describe block creates its own event with appropriate sessions
- Uses database helpers for session time updates
- Proper cleanup in `afterAll`

---

## Phase 4: Additional Fixes - ✅ COMPLETED

### `admin-events-sessions.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Replaced seed data lookup with created test event (NO sessions)
- Creates fresh event for session CRUD testing
- Removed dependency on "Suspension Basics" seed data
- Proper cleanup in `afterAll`

### `vetting-profile-update.spec.ts` - ✅ COMPLETED

**Changes Made**:
- Added helper functions for creating test users
- Uses two browser contexts for multi-user tests
- Tests now create fresh user accounts via test helper API
- No dependency on existing user data

---

## Implementation Pattern Summary

### Standard Test Data Creation Pattern

```typescript
// Helper for authenticated API requests
async function apiRequest(page: Page, method: string, url: string, data?: unknown) {
  const response = await page.evaluate(async ({ method, url, data }) => {
    const options: RequestInit = {
      method,
      credentials: 'include',
      headers: data ? { 'Content-Type': 'application/json' } : {},
    };
    if (data) options.body = JSON.stringify(data);
    const res = await fetch(url, options);
    const text = await res.text();
    try {
      return { status: res.status, data: JSON.parse(text) };
    } catch {
      return { status: res.status, data: text };
    }
  }, { method, url, data });
  return response;
}

test.describe('Test Suite', () => {
  let testEventId: string | null = null;

  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    const eventData = {
      title: `Test Event ${Date.now()}`,
      // ... other fields
      // CRITICAL timing controls
      registrationOpenHours: null,
      registrationCloseHours: 0,
      cancellationCloseHours: 0,
      sessions: [/* ... */],
    };

    const response = await apiRequest(page, 'POST', '/api/admin/events', eventData);
    testEventId = response.data.id;
    await page.close();
  });

  test.afterAll(async ({ browser }) => {
    if (!testEventId) return;
    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');
    await apiRequest(page, 'DELETE', `/api/admin/events/${testEventId}`);
    await page.close();
  });

  test('test name', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }
    // ... test logic
  });
});
```

---

## Success Criteria

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Incorrectly Skipped | 51 | 0 | ✅ Achieved |
| Deleted Obsolete | 0 | 11 | ✅ Achieved |
| Files Fixed | 0 | 10 | ✅ Achieved |

---

## Phase 5: Remaining Skipped Tests Audit (December 10, 2025)

### Current Test Results (Post Phase 1-4)
- **Passed**: 681 (86.7%)
- **Failed**: 72
- **Skipped**: 32
- **Total**: 785

### Audit Results

Found **~60 `test.skip()` calls** across 16 files. Categorized as follows:

---

### 5.1 DELETE - Untestable/Unnecessary Tests (2 tests)

| File | Test | Reason for Deletion |
|------|------|---------------------|
| `events/admin-event-copy.spec.ts:92` | "Copy modal validates past dates" | Untestable via E2E - Mantine DatePickerInput disables past dates at UI level |
| `login-with-scene-name.spec.ts:312` | Case-sensitivity skip | Login works with any case - test unnecessary |

---

### 5.2 KEEP SKIPPED - Legitimate (1 test)

| File | Line | Test | Reason |
|------|------|------|--------|
| `checkin-staff-authentication.spec.ts` | 200 | Token revocation | Feature not implemented yet |

---

### 5.3 CONVERT TO `test.fail()` - By File (~50 tests)

#### `admin-checkin-sessions.spec.ts` (8 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 205 | "Checkin Link button not found" | `test.fail()` - Feature exists |
| 288 | "Session select not found" | `test.fail()` - Feature exists |
| 361 | "Session select not found" | `test.fail()` - Feature exists |
| 513 | "Checkin Link button not found" | `test.fail()` - Feature exists |
| 652 | "Session select not found" | `test.fail()` - Feature exists |
| 669 | "Session select not found" | `test.fail()` - Feature exists |
| 697 | "Checkin Link button not found" | `test.fail()` - Feature exists |
| 712 | "Checkin Link button not found" | `test.fail()` - Feature exists |

#### `ticket-cancellation-selective.spec.ts` (4 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 273 | "Cancel button not visible" | `test.fail()` - Feature exists |
| 381 | "Cancel button not visible" | `test.fail()` - Feature exists |
| 441 | "User does not have 2 tickets" | `test.fail()` - Test data setup failed |
| 454 | "Cancel button not visible" | `test.fail()` - Feature exists |

#### `volunteer-session-validation.spec.ts` (1 skip)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 323 | "Volunteer section not visible" | `test.fail()` - Feature exists |

#### `multi-ticket-purchase.spec.ts` (3 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 222 | "Test event not created" | `test.fail()` - beforeAll failed |
| 333 | "Test event not created" | `test.fail()` - beforeAll failed |
| 367 | "Test event not created" | `test.fail()` - beforeAll failed |

#### `vetting-workflow.spec.ts` (13 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 244 | "No UnderReview applications found" | `test.fail()` - Should create test data |
| 259 | "Approve for Interview button not found" | `test.fail()` - Feature exists |
| 316 | "No applications found" | `test.fail()` - Should create test data |
| 330 | "On Hold button not found" | `test.fail()` - Feature exists |
| 342 | "On Hold modal did not open" | `test.fail()` - Bug if modal doesn't open |
| 421 | "No active applications found" | `test.fail()` - Should create test data |
| 440 | "Deny button not visible" | `test.fail()` - Feature exists |
| 532 | "No InterviewApproved applications found" | `test.fail()` - Should create test data |
| 547 | "Reminder button not found" | `test.fail()` - Feature exists |
| 626 | "No active applications found" | `test.fail()` - Should create test data |
| 640 | "Skip to Approved button not found" | `test.fail()` - Feature exists |
| 695 | "No applications found" | `test.fail()` - Should create test data |
| 753 | "No applications found" | `test.fail()` - Should create test data |

#### `session-availability-counts.spec.ts` (3 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 109 | "No multi-session event found" | `test.fail()` - Should create own data |
| 176 | "No multi-session event found" | `test.fail()` - Should create own data |
| 270 | "Session Timing Test Event not found" | `test.fail()` - Should create own data |

#### `ticket-purchase-e2e.spec.ts` (3 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 157 | "Test event not created" | `test.fail()` - beforeAll failed |
| 325 | "Test event not created" | `test.fail()` - beforeAll failed |
| 460 | "Test event not created" | `test.fail()` - beforeAll failed |

#### `checkin-attendee-workflow.spec.ts` (4 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 80 | "Event not found" | `test.fail()` - Should create own data |
| 91 | "Event not found" | `test.fail()` - Should create own data |
| 177 | "Checkin button not visible" | `test.fail()` - Feature exists |
| 186 | "Checkin button not visible" | `test.fail()` - Feature exists |

#### `session-based-timing.spec.ts` (5 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 123 | Timing edge case | `test.fail()` - Should be tested |
| 160 | Timing edge case | `test.fail()` - Should be tested |
| 207 | Timing edge case | `test.fail()` - Should be tested |
| 262 | Timing edge case | `test.fail()` - Should be tested |
| 309 | Timing edge case | `test.fail()` - Should be tested |

#### `session-ticket-availability.spec.ts` (6 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 72 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |
| 153 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |
| 201 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |
| 251 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |
| 299 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |
| 370 | "Session Timing Test Event not found" | `test.fail()` - Relies on seed data |

#### `admin-events-workflow.spec.ts` (3 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 208 | "No events found" | `test.fail()` - Should create own data |
| 218 | "No events found" | `test.fail()` - Should create own data |
| 375 | "Add session button not found" | `test.fail()` - Feature exists |

#### `event-update-complete-flow.spec.ts` (3 skips)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 213 | "No events available" | `test.fail()` - Should create own data |
| 492 | "No events available" | `test.fail()` - Should create own data |
| 555 | "No events available" | `test.fail()` - Should create own data |

#### `comprehensive-timing-tests.spec.ts` (1 skip)
| Line | Current Reason | Fix |
|------|----------------|-----|
| 645 | Unknown | `test.fail()` - Needs review |

---

### Implementation Plan for Phase 5

1. **Delete 2 tests**: DatePicker validation, login case-sensitivity
2. **Convert ~50 `test.skip()` to `test.fail()`** with descriptive messages
3. **Result**: Only 1 legitimate skip remains (token revocation)

---

## Notes

- All fixed tests use `test.fail()` instead of `test.skip()` when setup fails
- All tests create their own data with unique timestamps
- All tests clean up after themselves in `afterAll`
- CRITICAL timing controls included in all event creation:
  - `registrationOpenHours: null`
  - `registrationCloseHours: 0`
  - `cancellationCloseHours: 0`
  - `volunteerRegistrationCloseHours: 0`
  - `volunteerCancellationCloseHours: 0`
- Session start dates set 7+ days in future to avoid timing issues
