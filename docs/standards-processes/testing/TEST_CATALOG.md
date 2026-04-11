# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2026-04-10 (header refresh only — body is 2025-12-13 snapshot pending full catalog update) -->
<!-- Version: 12.12.0 - FULL E2E SUITE EXECUTION (December 2025 snapshot, see current numbers at top) -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->

## ⚠️ Current Numbers (2026-04-10)

**The body of this file (below) is a December 2025 E2E snapshot and is out of date for .NET tests.** For current .NET test counts and known issues, see:

- **`CURRENT_TEST_STATUS.md`** — full 2026-04-10 baseline with per-project numbers, known issues (WAF shared-state bug, EmailTemplate behavioral drift), and the investigation trail
- **`.claude/skills/run-test-suite/SKILL.md`** — the skill that runs the .NET + E2E tests

Quick current numbers (2026-04-10):
- **.NET total**: 1,380 passed / 89 failed / 41 skipped / 1,510 total = 92.8% pass rate
- **E2E**: not run in the 2026-04-10 baseline; last measured 86.9% on 2025-12-13

Test execution (all tests go through the skill):
```bash
bash .claude/skills/run-test-suite/execute.sh --mode unit   # .NET only
bash .claude/skills/run-test-suite/execute.sh --mode e2e    # Playwright only
bash .claude/skills/run-test-suite/execute.sh --mode all    # both
```

A full catalog refresh (listing every test file with current pass/fail per class) is pending — this file's per-feature-area tables below are Dec 2025 snapshots.

---

## FULL E2E TEST SUITE EXECUTION - December 13, 2025 *(snapshot — not current)*

**EXECUTION DATE**: 2025-12-13T21:09:42Z
**STATUS**: 86.9% Pass Rate (Below 90% threshold)
**GIT SHA**: 052a5e5f

### Test Execution Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 794 |
| **Passed** | 690 |
| **Failed** | 75 |
| **Skipped** | 29 |
| **Pass Rate** | **86.9%** |

### Failure Categories

| Category | Count | Root Cause |
|----------|-------|------------|
| DataFactory Session Creation | ~25 | Missing unique `sessionIdentifier` |
| Vetting Modal Visibility | 6 | UI animation timing |
| UI Selector Issues | ~15 | Multiple elements, timeouts |
| API/Data Issues | ~10 | Response format mismatches |
| Infrastructure | ~5 | Environment-specific failures |
| RSVP/Ticket Workflows | ~5 | UI visibility timing |

### Recent Fixes Applied (This Session)

**Check-In Tests - ALL 28 PASSING**

1. **TestHelperService.cs** - Added EventAttendee creation when purchasing tickets
2. **checkin-attendee-workflow.spec.ts** - Updated to search by sceneName
3. **checkin-staff-authentication.spec.ts** - Fixed navigation and attendee creation
4. **admin-checkin-sessions.spec.ts** - Added unique sessionIdentifier, updated UI selectors

**Commits**:
- `052a5e5f` test: fix check-in E2E test failures
- `d9319d37` test: update admin-checkin-sessions tests for checkbox UI
- `749096ef` test: fix duplicate SessionCode errors

---

## CHECK-IN TESTS - 100% PASSING

### checkin-staff-authentication.spec.ts (7/7)
- Valid token allows access to check-in interface
- Invalid token shows error message
- Missing token shows error message
- Token for wrong event returns error
- Revoked token cannot be used
- No authentication required for valid token
- Expired token shows error message

### checkin-attendee-workflow.spec.ts (4/4)
- Check in a registered attendee
- Cannot check in same attendee twice
- Two-step check-in workflow (Covid Test -> Check In)
- Token validation fails for expired token during check-in

### admin-checkin-sessions.spec.ts (6/6)
- Should show "Sessions Attended" column in Attendees tab
- Should display session badges for checked-in attendees
- Should auto-select session for single-session events
- Should require session selection before generating token
- Should show session selector in token generation modal
- Should display session name in generated token list

### checkin-dashboard.spec.ts (5/5)
- Dashboard displays correct statistics
- Dashboard shows event information
- Sync status displays
- Recent check-ins section displays
- Dashboard navigation from check-in interface

---

## RECENTLY FIXED

### 1. DataFactory Session Creation (~25 tests) - ✅ FIXED 2025-12-13

**Problem**: Tests creating multiple sessions without unique `sessionIdentifier`

**Status**: **FIXED** - All 8 files updated with unique sessionIdentifier values

**Files Fixed**:
- ✅ `admin-session-deletion.spec.ts` - 5/5 passing
- ✅ `comprehensive-timing-tests.spec.ts` - 12/12 passing
- ✅ `multi-ticket-purchase.spec.ts` - 3/3 passing
- ✅ `session-availability-counts.spec.ts` - 7/7 passing
- ✅ `session-ticket-availability.spec.ts` - 4/7 passing (3 unrelated business logic issues)
- ✅ `ticket-cancellation-selective.spec.ts` - 3/3 passing
- ✅ `volunteer-auto-cancel.spec.ts` - 3/3 passing
- ✅ `volunteer-session-validation.spec.ts` - 2/2 passing

**Verification Run**: 38/41 tests passing (92.7%)

### 2. Vetting Modal Tests (6 tests)

**Problem**: Modal visibility timing issues

**Files Affected**:
- `vetting-application-detail.spec.ts`
- `vetting-workflow.spec.ts`

---

## BACKEND INTEGRATION TESTS

### RsvpPreservationOnTicketCancellationTests (5 tests) - NEW 2026-03-07

**File**: `tests/integration/api/Features/Participation/RsvpPreservationOnTicketCancellationTests.cs`

**Purpose**: Verifies RSVP preservation logic during partial ticket cancellation in `AttendanceService.CancelTicketPurchasesAsync`.

**Bug Fixed**: RSVP was unconditionally cancelled whenever any ticket was cancelled. Now only cancels RSVP if no active tickets remain.

| Test | Description | Status |
|------|-------------|--------|
| CancelOneTicket_WithOtherActiveTickets_PreservesRsvp | Cancel 1 of 2 tickets, RSVP stays Active | NEW |
| CancelLastTicket_NoRemainingTickets_CancelsRsvp | Cancel only ticket, RSVP auto-cancelled | NEW |
| CancelAllTickets_MultipleSessions_CancelsRsvp | Cancel all tickets in one call, RSVP cancelled | NEW |
| CancelOneTicket_NoRsvpExists_Succeeds | Cancel ticket when no RSVP exists, no error | NEW |
| CancelOneOfThreeTickets_PreservesRsvp | Cancel 1 of 3 tickets, RSVP stays Active | NEW |

### AttendanceServiceCancellationTests (5 tests)

**File**: `tests/integration/api/Features/Participation/AttendanceServiceCancellationTests.cs`

**Purpose**: Tests per-ticket-purchase cancellation eligibility flags in `GetParticipationStatusAsync`.

---

## TEST ENVIRONMENT

**Execution Method**: `test-environment` skill (isolated containers)
- Test containers built from current codebase
- Fresh test database with seed data
- Health checks passed (API, Web, Database)
- Automatic cleanup after execution

**Services**:
- Web: http://web:5173 (inside test network)
- API: http://api:8080 (inside test network)
- Database: `witchcityrope_test`

---

## TEST METRICS HISTORY

| Date | Total | Passed | Failed | Pass Rate |
|------|-------|--------|--------|-----------|
| 2025-12-13 (Full) | 794 | 690 | 75 | 86.9% |
| 2025-12-13 (Check-in) | 28 | 28 | 0 | 100% |

---

## KEY TESTING PATTERNS

### React Strict Mode
- Components render twice in dev mode
- Use `.last()` on button selectors

### Mantine SegmentedControl
- Use `getByRole('button')` not `getByRole('radio')`
- Check `getAttribute('data-active')` for state

### TipTap/ProseMirror
- Use keyboard input, not `.fill()` for contenteditable

### DataFactory Sessions
- **ALWAYS** specify unique `sessionIdentifier`
- Format: 'S1', 'S2', 'S3' per event

---

## NAVIGATION

**For failing test details**:
-> See `/docs/test-baselines/e2e-failing-tests-tracker.md`

**For test execution standards**:
-> See `/docs/standards-processes/testing/TESTING_GUIDE.md`

**For test environment setup**:
-> See `/.claude/skills/test-environment/SKILL.md`

---

## INTEGRATION TESTS - Ticket Assignment & Proxy RSVP

**Added**: 2026-03-18
**Location**: `/tests/integration/Features/TicketAssignment/`
**Status**: NEW - Not yet executed (awaiting test container run)

### AuthorizedContactEndpointTests.cs (16 tests)
- AC-I01: GET returns empty lists for new user
- AC-I02: POST + GET roundtrip creates and lists contact
- AC-I03: DELETE soft-deletes, no longer in GET
- AC-I04: Search returns matching scene names (2 tests)
- AC-I05: Principals filtered by event vetting (2 tests)
- AC-I07: All endpoints require auth (5 tests)
- Edge: Self-authorization returns 400
- Edge: Duplicate active relationship returns 409
- Edge: Non-existent user returns 404
- Edge: Non-principal revoke returns 403

### TicketAssignmentEndpointTests.cs (12 tests)
- TA-I01: Full assign -> accept flow
- TA-I02: Full assign -> decline -> reassign flow
- TA-I03: Pending assignments appear in dashboard
- TA-I04: Assigned tickets appear in purchaser view
- TA-I05: Unauthorized assign returns 403
- Auth: 5 auth-required tests (401 without token)
- Edge: Accept without waiver returns 400
- Edge: Decline by wrong user returns 403

### ProxyRsvpEndpointTests.cs (12 tests)
- PR-I01: Full proxy RSVP create -> accept flow
- PR-I02: Proxy RSVP create -> decline flow
- PR-I03: Capacity enforcement (400 at capacity)
- PR-I04: Vetting enforcement (403 for non-vetted on VettedMembersOnly)
- Auth: 3 auth-required tests (401 without token)
- Edge: Without authorization returns 403
- Edge: Event doesn't allow RSVPs returns 400
- Edge: Duplicate RSVP returns 409
- Edge: Accept without waiver returns 400
- Edge: Decline by wrong user returns 403

### AdminAssignmentEndpointTests.cs (8 tests)
- AA-I01: Admin assigns comp ticket (PendingAcceptance, TotalPrice=0)
- AA-I02: Admin views assignments for event
- AA-I03: Non-admin gets 403 (2 tests)
- Auth: 2 auth-required tests (401 without token)
- Edge: Non-existent event returns 404
- Edge: Duplicate assignment returns 409

### MultiTicketCheckoutEndpointTests.cs (4 tests)
- CO-I01: Single ticket checkout backward compatible
- CO-I02: Multi-ticket with TicketSelections (2 tests)
- Auth: 1 auth-required test (401 without token)

**Total Integration Tests Added**: 52

---

## E2E TESTS - Ticket Assignment & Proxy RSVP

**Added**: 2026-03-18
**Location**: `/tests/e2e/`
**Status**: NEW - Created, awaiting first execution
**Test Plan Reference**: `/docs/functional-areas/events/ticket-assignment-proxy-rsvp/design/test-plan.md` Section 8

### authorized-contacts.spec.ts (5 tests)
- Flow 1: Empty state display (no contacts)
- Flow 1: Add contact via scene name search
- Flow 1: Remove contact with confirmation modal
- Flow 1: Cancel search toggle (progressive disclosure)
- Flow 1: Principal list shows authorizer (bidirectional verification)

### ticket-acceptance.spec.ts (4 tests)
- Flow 2/3: Pending tickets card visibility on dashboard
- Flow 2/3: Acceptance modal waiver checkbox requirement
- Flow 2/3: Full accept flow (waiver + ToS + API confirm)
- Flow 2/3: Decline flow with optional reason

### proxy-rsvp.spec.ts (3 tests)
- Flow 3: Proxy RSVP section visibility for delegate
- Flow 3: Create proxy RSVP with confirmation modal
- Flow 3: Pending RSVP appears on assignee dashboard

### ticket-assignment-checkout.spec.ts (3 tests, 2 fixme)
- Flow 2: Navigate to paid event and see ticket options
- Flow 2: FIXME - Quantity selector and assignee dropdown (needs payment infra)
- Flow 2: FIXME - Complete multi-ticket purchase (needs payment infra)

### ticket-decline-reassign.spec.ts (3 tests, 2 fixme)
- Flow 4: Decline pending ticket with reason
- Flow 4: FIXME - Declined status and reassign from purchaser dashboard
- Flow 4: FIXME - Pending ticket on new assignee after reassignment

**Total E2E Tests Added**: 18 (14 active, 4 fixme)
**Note**: Tests requiring ticket assignment DataFactory support use defensive
skip patterns and will auto-activate when prerequisites are met.

---

**Last Updated**: 2026-03-18
**Updated By**: test-developer agent
**Next Review**: After test execution to verify pass/fail status
