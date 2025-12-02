# E2E Test Baseline - December 2, 2025

## Test Run Summary

| Metric | Count |
|--------|-------|
| **Passed** | 622 |
| **Failed** | 111 |
| **Skipped** | 74 |
| **Total Runnable** | 733 |
| **Pass Rate** | **84.9%** |
| **Run Time** | 9.3 minutes |

## Environment
- **Container Type**: Test Container (witchcityrope-test)
- **Test Framework**: Playwright
- **Workers**: 6
- **Date**: 2025-12-02

## Comparison to Previous Baseline (December 1, 2025)

| Metric | Dec 1 | Dec 2 | Change |
|--------|-------|-------|--------|
| Passed | 597 | 622 | **+25** |
| Failed | 121 | 111 | **-10** |
| Skipped | 74 | 74 | 0 |
| Pass Rate | 83.1% | 84.9% | **+1.8%** |

## Changes Since Last Baseline
- **TicketType-Session Migration**: Implemented many-to-many relationship (Bug 1 fix)
- Migration from `TicketType.SessionId` (single) to `TicketType.Sessions` (collection)
- New `TicketTypeSessions` join table created

## Key Validations

### Session-Based Ticket Availability Tests (ALL PASSED)
- ✅ Test 625: verify session timing test event has correct configuration
- ✅ Test 626: S1 Only ticket should NOT be available (timing window closed)
- ✅ Test 627: S2 Only ticket SHOULD be available (future session)
- ✅ Test 628: Both Sessions ticket uses EARLIEST session (S1) - NOT purchasable
- ✅ Test 629: member view shows only available tickets
- ✅ Test 630: API returns correct ticket availability status

## Known Pre-Existing Failures (Not Related to TicketType-Session)

### Check-In Module Tests
- Check-in workflow tests failing (check-in infrastructure issues)
- Token validation tests failing

### Admin Dashboard Tests
- Google Drive links update test failing
- Investigation notes test failing

### Admin Volunteer Position Management
- Add/edit volunteer position tests failing
- Session display format tests failing

### Refund Processing
- Refund workflow tests failing

## Conclusion

**TicketType-Session Migration Validated** ✅

The many-to-many relationship change had NO negative impact on the test suite. In fact, we saw a slight improvement:
- +25 more tests passing
- -10 fewer failures
- +1.8% improvement in pass rate

All session-based ticket availability tests pass, confirming the migration is working correctly.
