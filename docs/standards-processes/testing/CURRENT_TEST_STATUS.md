# Current Test Suite Status
<!-- Last Updated: 2025-12-11 -->
<!-- Version: 3.0 -->
<!-- Owner: Testing Team -->
<!-- Status: Operational -->

## Overall Test Health

### Latest E2E Baseline: December 11, 2025

```
E2E Tests (Playwright):
  Total:     790 tests
  Passed:    617 (78.1%)
  Failed:    146
  Skipped:   27

Pass Rate: 78.1%
```

### Infrastructure Improvements (December 11, 2025)

1. **Custom Playwright Reporter** - Shows `Passed - #num - test name` format for easy reading
2. **Fixed Dockerfile.test Layer Ordering** - Playwright install cached BEFORE tests copied (no more re-downloading)
3. **Volume Mount for Results** - test-results.json immediately available on host
4. **Structured JSON Output** - quick-summary.json for automated parsing
5. **Comprehensive Test Summary** - test-summary.txt with all failed test names

### Test Result Files Location

All test artifacts in `/test-results/`:
- `quick-summary.json` - Machine-readable summary (total, passed, failed, pass_rate)
- `test-summary.txt` - Human-readable summary with failed test list
- `test-results.json` - Full Playwright JSON report
- `html-report/` - Interactive HTML report (`npx playwright show-report test-results/html-report`)

## Failed Tests by Category (146 total)

| Category | Count | Description |
|----------|-------|-------------|
| Vetting Workflows | 25 | Admin dashboard, application detail, workflow integration |
| Profile/Persistence | 16 | Profile updates, persistence validation |
| Session Timing | 13 | Session-based timing, ticket availability |
| Ticket Operations | 11 | Ticket cancellation, purchase, lifecycle |
| Events Admin | 14 | Event sessions, policies, volunteers, workflows |
| Venue | 8 | Venue creation, editing, display permissions |
| Check-in | 4 | Check-in staff and attendee workflows |
| Volunteer | 4 | Volunteer auto-cancel, session validation |
| RSVP | 4 | RSVP lifecycle, comprehensive tests |
| Home/Basic | 4 | Home page, events basic validation |
| Other | 43 | Anonymous reports, notifications, CSRF, etc. |

## Test Execution

### Quick Commands

```bash
# Run full E2E test suite (recommended)
bash .claude/skills/test-environment/execute.sh --mode e2e --skip-rebuild

# Run filtered tests
bash .claude/skills/test-environment/execute.sh --mode e2e --filter "admin-events"

# Run with container keep for debugging
bash .claude/skills/test-environment/execute.sh --mode e2e --skip-rebuild --keep-containers

# View results after test run
cat test-results/quick-summary.json
cat test-results/test-summary.txt
npx playwright show-report test-results/html-report
```

### Understanding Test Output

The custom reporter shows:
```
Running 790 tests...

Passed - 1 - Admin Events Navigation > should navigate to events page
Passed - 2 - Admin Events Navigation > should display events list
Failed - 3 - Vetting Workflow > admin can approve application for interview
...

════════════════════════════════════════════════════════════════
                    TEST RESULTS SUMMARY
════════════════════════════════════════════════════════════════

  Passed:  617
  Failed:  146
  Skipped: 27
  ─────────────────
  Total:   790

  PASS RATE: 78.1%

════════════════════════════════════════════════════════════════

FAILED TESTS:
  Failed - 3 - Vetting Workflow > admin can approve application for interview
           Error: Expected element to be visible
  ...
```

## Historical Progress

| Date | Total | Passed | Failed | Pass Rate | Notes |
|------|-------|--------|--------|-----------|-------|
| 2025-12-11 | 790 | 617 | 146 | 78.1% | New baseline with improved infrastructure |
| 2025-11-29 | 790 | 621 | 142 | 78.6% | Previous baseline |
| 2025-11-09 | ~27 files | - | - | ~80% | Phase 3 consolidation complete |
| 2025-10-09 | 348 | 18 | 9 | ~5% | Missing data-testid attributes |

## Known Issues

### High Priority - Vetting System (25 failures)
Most vetting tests fail consistently. Likely causes:
- Missing vetting email service registration
- Incomplete vetting workflow API implementation
- Form submission validation issues

### Medium Priority - Profile Persistence (16 failures)
Profile update tests fail to persist changes. Likely causes:
- API returning success but not saving
- Form data transformation issues
- Cache invalidation problems

### Lower Priority - Session Timing (13 failures)
Timing-based ticket availability tests. Complex business logic.

## Build Status

- **Solution Build**: ✅ Successful (72 warnings, 0 errors)
- **Docker Containers**: ✅ Operational
- **CI/CD Pipeline**: ✅ Test infrastructure stabilized
- **Test Results**: ✅ Automatically generated and available on host

---

*This status is updated with each baseline test run. For detailed failure analysis, see `test-results/test-summary.txt`.*
