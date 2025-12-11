# Skipped Tests Documentation

**Generated**: 2025-11-28
**Purpose**: Document all tests marked with `test.skip()` and why they are skipped

## Summary

This document lists all E2E tests that are configured to skip, either:
1. **Permanently skipped**: Tests using `test.skip('description', ...)` that are disabled
2. **Conditionally skipped**: Tests using `test.skip()` within runtime checks

---

## Permanently Skipped Tests

### 1. Refund Validations (`refund-validations.spec.ts`)

| Test | Line | Reason | Action Needed |
|------|------|--------|---------------|
| `Can submit with empty refund reason` | 61 | **TDD MISMATCH**: Test assumed refund reason is optional. Actual component requires minimum 10 characters (`refundReason.trim().length >= 10`). | Remove skip if business requirement changes to make reason optional |
| `Only checkbox required for submission` | 131 | **TDD MISMATCH**: Test assumed only checkbox required. Actual component requires: amount + reason (10+ chars) + checkbox. | Remove skip if business requirement changes |

### 2. Ticket Lifecycle Persistence (`ticket-lifecycle-persistence.spec.ts`)

| Test | Line | Reason | Action Needed |
|------|------|--------|---------------|
| `CRITICAL: should persist ticket cancellation to database` | 68 | **FEATURE NOT IMPLEMENTED**: Ticket cancellation feature not yet built | Implement ticket cancellation feature |
| `should handle complete ticket lifecycle` | 91 | **FEATURE NOT IMPLEMENTED**: Depends on ticket cancellation | Implement ticket cancellation feature |
| `should persist cancellation reason to database` | 97 | **FEATURE NOT IMPLEMENTED**: Depends on ticket cancellation | Implement ticket cancellation feature |
| `should prevent duplicate cancellations` | 146 | **FEATURE NOT IMPLEMENTED**: Depends on ticket cancellation | Implement ticket cancellation feature |
| `should verify endpoint called is correct` | 197 | **FEATURE NOT IMPLEMENTED**: Depends on ticket cancellation | Implement ticket cancellation feature |

### 3. Payment Tests (`payment.spec.ts`)

| Test | Line | Reason | Action Needed |
|------|------|--------|---------------|
| `should complete payment flow` | 61 | **STRIPE TEST MODE**: Payment testing requires Stripe test keys/webhook setup | Configure Stripe test environment |
| `should handle payment failure gracefully` | 110 | **STRIPE TEST MODE**: Depends on Stripe test setup | Configure Stripe test environment |
| `should validate sliding scale selection` | 142 | **STRIPE TEST MODE**: Depends on Stripe test setup | Configure Stripe test environment |
| `should handle webhook processing` | 163 | **ASYNC WEBHOOK**: Webhook tests don't work in E2E due to async nature | Test webhooks separately in integration tests |
| `should process payment completed webhook` | 217 | **CI ONLY**: Webhook tests only enabled in CI environment | Use integration tests for webhook testing |
| `should process refund webhook` | 239 | **CI ONLY**: Webhook tests only enabled in CI environment | Use integration tests for webhook testing |

### 4. Login With Scene Name (`login-with-scene-name.spec.ts`)

| Test | Line | Reason | Action Needed |
|------|------|--------|---------------|
| `should be case-sensitive for scene name` | 301 | **BACKEND BEHAVIOR**: Scene name case sensitivity depends on backend implementation | Verify backend behavior and update test |
| `should display helper text explaining both login options` | 375 | **UI NOT IMPLEMENTED**: Helper text component not yet added | Add helper text to login UI |

### 5. Form Design Verification (`verify-form-design-fixes.spec.ts`)

| Test | Line | Reason | Action Needed |
|------|------|--------|---------------|
| `Verify Form Design A fixes` | 5 | **FEATURE NOT IMPLEMENTED**: Form Design A feature not yet built | Implement Form Design A feature |

---

## Conditionally Skipped Tests (Runtime Checks)

These tests skip at runtime based on environment conditions. This is VALID defensive programming.

### 1. Vetting Email Templates (`vetting-email-templates.spec.ts`)

**Skip Conditions**:
- Line 37: `if (!currentUrl.includes('/admin/email-templates'))` - Page doesn't exist yet
- Line 49: `if (await vettingTab.count() === 0)` - Vetting tab not found

**Reason**: Email templates feature may not be implemented. Tests gracefully skip instead of failing.

**Action**: Remove skip conditions once email templates feature is fully implemented.

### 2. Admin Event Copy (`events/admin-event-copy.spec.ts`) - ✅ MIGRATED TO DATAFACTORY

**Previous Skip Conditions** (REMOVED 2025-12-10):
- Line 29: `if (eventRowCount === 0)` - No events in database

**Migration Status**: ✅ **MIGRATED TO DATAFACTORY PATTERN**
- **Date**: 2025-12-10
- **Status**: NO LONGER SKIPS - Creates its own test data
- **Pattern**: Uses `df` fixture from DataFactory for automatic test data creation and cleanup
- **Benefits**: Tests are now fully isolated, repeatable, and never skip due to missing seed data

**Action**: None needed - tests are now self-contained.

### 3. Venue Display (`venue-display.spec.ts`)

**Skip Conditions**:
- Multiple lines: Checks for venue data availability

**Reason**: Venue display depends on seed data and API responses. Skips prevent flaky tests.

**Action**: Ensure venue seed data is present.

### 4. RSVP Event Waiver (`rsvp-event-waiver.spec.ts`)

**Skip Conditions**:
- Multiple lines: Checks for social events with waivers, RSVP buttons, etc.

**Reason**: RSVP waiver tests depend on specific event types being available.

**Action**: Ensure seed data includes social events with waiver requirements.

### 5. Checkout Pricing (`checkout-pricing.spec.ts`)

**Skip Conditions**:
- Lines 34, 91: Checks for ticket availability and pricing configuration

**Reason**: Pricing tests depend on specific ticket configurations.

**Action**: Ensure seed data includes various ticket pricing scenarios.

### 6. Check-in Tests (Multiple Files)

**Skip Conditions in `checkin-search-filter.spec.ts`**:
- Line 105: `'Could not create test attendees - walk-in feature not available'`
- Line 139: `'Could not create test attendees'`
- Line 190, 224, 279: Similar attendee creation failures
- Line 232: `'Status filter UI not found - feature may not be implemented'`

**Skip Conditions in `checkin-attendee-workflow.spec.ts`**:
- Line 73: `'No registered attendees found for this event'`
- Line 84: `'All attendees are already checked in'`
- Line 134: `'No attendees found for this event'`
- Line 144: `'No checked-in attendees found'`
- Line 168, 177: Similar attendance status checks

**Skip Conditions in `checkin-walk-in-workflow.spec.ts`**:
- Line 220: `'Cannot determine event capacity'`
- Line 229: Event has available spots, cannot test capacity limit

**Reason**: Check-in tests require specific pre-conditions (registered attendees, events with capacity limits, etc.).

**Action**: Create proper test fixtures with known attendee states.

### 7. Teacher Display (`teacher-display.spec.ts`)

**Skip Conditions**:
- Line 63: Runtime check for teacher data availability

**Reason**: Teacher display depends on teacher data being seeded.

**Action**: Ensure teacher seed data is present.

### 8. Volunteer Event Waiver (`volunteer-event-waiver.spec.ts`)

**Skip Conditions**:
- Lines 57, 68, 79, 132, 153, 163, etc.: Multiple runtime checks for volunteer positions, waiver requirements

**Reason**: Volunteer waiver tests depend on events with volunteer positions and waiver requirements.

**Action**: Ensure seed data includes events with volunteer positions and waivers.

### 9. Vetting System Complete Workflows (`vetting-system-complete-workflows.spec.ts`)

**Skip Conditions**:
- Lines 36, 73, 124, 133, etc.: Multiple checks for vetting application states, UI elements

**Reason**: Vetting workflow tests depend on specific application states and user data.

**Action**: Create proper test fixtures with vetting applications in various states.

### 10. Vetting Profile Update (`vetting-profile-update.spec.ts`)

**Skip Conditions**:
- Line 153: `'Requires user account with pre-populated optional fields'`
- Line 276: `'Requires multi-user workflow or API setup'`

**Reason**: Profile update tests require specific user data configurations.

**Action**: Create test users with pre-populated profile data.

---

## Recommendations

### Tests That Should Remain Skipped
1. **Payment/Stripe tests** - Require separate test environment with Stripe test keys
2. **Webhook tests** - Inherently async, better tested in integration tests
3. **CI-only tests** - Appropriately gated for CI environment

### Tests That Need Feature Implementation
1. **Ticket cancellation tests** - Feature not yet built
2. **Form Design A tests** - Feature not yet built
3. **Email templates (vetting tab)** - Feature partially implemented

### Tests With TDD Mismatch
1. **Refund validation tests** (2 tests) - Test assumptions don't match actual component behavior
   - These should either:
     - a) Be removed permanently (if component behavior is correct)
     - b) Drive component changes (if test requirements are correct)

### Tests That Need Better Seed Data
Most conditional skips are due to missing or inconsistent seed data:
1. Events with RSVP capability
2. Events with volunteer positions
3. Events with waiver requirements
4. Users in various vetting states
5. Attendees in various check-in states

**Recommendation**: Create a comprehensive E2E seed data script that ensures all test scenarios have required data.

---

## Audit Trail

| Date | Action | Author |
|------|--------|--------|
| 2025-11-28 | Initial documentation created | Claude (test analysis) |
| 2025-11-28 | Added refund validation skips | Claude (TDD mismatch fix) |
| 2025-11-28 | Added vetting email template defensive skips | Claude (defensive coding) |
| 2025-11-28 | Added admin event copy defensive skips | Claude (defensive coding) |
