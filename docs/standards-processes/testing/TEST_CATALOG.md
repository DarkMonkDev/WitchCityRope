# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-12-13 -->
<!-- Version: 12.12.0 - FULL E2E SUITE EXECUTION -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->


## FULL E2E TEST SUITE EXECUTION - December 13, 2025

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

**Last Updated**: 2025-12-13T21:10:00Z
**Updated By**: test-environment skill
**Next Review**: After fixing DataFactory session tests
