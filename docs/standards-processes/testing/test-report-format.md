# Test Report Format Standard

**Last Updated**: 2025-11-18
**Version**: 1.0
**Owner**: test-executor agent
**Status**: Active

## Purpose

This document defines the **standardized format** for test execution reports. This format is used by:
- **test-executor agent**: Creates reports after running tests
- **staging-deploy skill**: Validates tests passed before deployment
- **Orchestrator**: Parses results for workflow decisions

## Single Source of Truth

**Location**: `/test-results/test-execution-report.md`

**Format**: YAML frontmatter + Markdown body

## YAML Frontmatter Fields

```yaml
---
status: PASS                          # REQUIRED: PASS or FAIL
pass_rate: 95.5                       # REQUIRED: Percentage (0.0-100.0)
tests_total: 245                      # REQUIRED: Total tests executed
tests_passed: 234                     # REQUIRED: Tests that passed
tests_failed: 11                      # REQUIRED: Tests that failed
tests_skipped: 0                      # REQUIRED: Tests skipped
timestamp: 2025-11-18T03:15:00Z       # REQUIRED: ISO 8601 UTC format
git_sha: abc123f                      # REQUIRED: Short git SHA (7 chars)
---
```

## Status Determination Rules

**90% Pass Rate Threshold**:
- `status: PASS` if `pass_rate >= 90.0`
- `status: FAIL` if `pass_rate < 90.0`

**Calculation**:
```bash
PASS_RATE = (tests_passed / tests_total) * 100
```

## Complete Example

```markdown
---
status: PASS
pass_rate: 95.5
tests_total: 245
tests_passed: 234
tests_failed: 11
tests_skipped: 0
timestamp: 2025-11-18T03:15:00Z
git_sha: abc123f
---

# Test Execution Report

## Summary
- **Status**: ✅ PASS (95.5% pass rate)
- **Total Tests**: 245
- **Passed**: 234
- **Failed**: 11
- **Skipped**: 0
- **Threshold**: 90% (PASS if >= 90%)

## Pass Rate Analysis
- **Required**: 90.0%
- **Achieved**: 95.5%
- **Margin**: +5.5%

## Test Categories

### Unit Tests
- Passed: 150/150 (100%)
- Failed: 0
- Duration: 1m 30s

### Integration Tests
- Passed: 50/55 (90.9%)
- Failed: 5
- Duration: 2m 15s

### E2E Tests (Playwright)
- Passed: 34/40 (85%)
- Failed: 6
- Duration: 3m 45s

## Failed Tests

### Integration Test Failures (5 total)

1. **EventRegistrationTests.CancelRegistration_ValidRequest_SuccessfulCancellation**
   - Error: Assert.Equal() Failure - Expected 0, Actual 1
   - File: EventRegistrationTests.cs:145
   - Category: Integration
   - Suggested Fix: backend-developer

2. **PaymentProcessingTests.RefundTicket_ValidRequest_ProcessesRefund**
   - Error: HTTP 500 Internal Server Error
   - File: PaymentProcessingTests.cs:89
   - Category: Integration
   - Suggested Fix: backend-developer

### E2E Test Failures (6 total)

1. **admin-events.spec.ts: Create new event with sessions**
   - Error: Timeout waiting for element [data-testid="save-event"]
   - Screenshot: test-results/admin-events-create-1.png
   - Category: E2E
   - Suggested Fix: react-developer

2. **admin-vetting.spec.ts: Approve vetting application**
   - Error: Element [data-testid="approve-button"] not found
   - Screenshot: test-results/admin-vetting-approve-1.png
   - Category: E2E
   - Suggested Fix: react-developer

## Environment

### Docker Containers
- ✅ **witchcity-web**: Healthy (Up 15m)
- ✅ **witchcity-api**: Healthy (Up 15m)
- ✅ **witchcity-db**: Healthy (Up 15m)

### Services
- ✅ **Web**: http://localhost:5173 - Responding
- ✅ **API**: http://localhost:5655 - Responding
- ✅ **Database**: localhost:5434 - Connected

### Test Data
- ✅ Database seeded with test accounts
- ✅ Test events created
- ✅ Test users created (5 accounts)

## Artifacts

### Test Result Files
- Unit Tests: `/test-results/unit-results.trx`
- Integration Tests: `/test-results/integration-results.trx`
- E2E Tests: `/test-results/playwright/index.html`

### Screenshots
- Failed E2E tests: `/test-results/*.png`

### Logs
- Test execution log: `/test-results/test-execution.log`

## TEST_CATALOG Updated

✅ Metrics updated in `/docs/standards-processes/testing/TEST_CATALOG.md`

**Updates Made**:
- Updated pass/fail counts for all test categories
- Marked 5 integration tests as FAILING
- Marked 6 E2E tests as FAILING
- Updated last execution timestamp

## Execution Details

- **Started**: 2025-11-18T03:10:00Z
- **Completed**: 2025-11-18T03:15:00Z
- **Duration**: 5m 0s
- **Git SHA**: abc123f
- **Branch**: main
- **Executor**: test-executor agent

## Next Steps

### For Failed Tests (11 total)
1. **Integration Failures (5)**: Assign to backend-developer
2. **E2E Failures (6)**: Assign to react-developer

### Deployment Readiness
✅ **READY FOR STAGING**: Pass rate 95.5% exceeds 90% threshold

Despite 11 test failures, the overall pass rate of 95.5% meets the deployment threshold. Failed tests should be addressed in subsequent iterations.
```

## Implementation Guide

### For test-executor Agent

**Creating the Report**:

```bash
#!/bin/bash
# Calculate test results
TESTS_TOTAL=245
TESTS_PASSED=234
TESTS_FAILED=11
TESTS_SKIPPED=0

# Calculate pass rate (1 decimal place)
PASS_RATE=$(awk "BEGIN {printf \"%.1f\", ($TESTS_PASSED/$TESTS_TOTAL)*100}")

# Determine status based on 90% threshold
if (( $(awk "BEGIN {print ($PASS_RATE >= 90.0)}") )); then
    STATUS="PASS"
    STATUS_EMOJI="✅"
else
    STATUS="FAIL"
    STATUS_EMOJI="❌"
fi

# Get timestamp and git SHA
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
GIT_SHA=$(git rev-parse --short HEAD)

# Create report
cat > test-results/test-execution-report.md << EOF
---
status: $STATUS
pass_rate: $PASS_RATE
tests_total: $TESTS_TOTAL
tests_passed: $TESTS_PASSED
tests_failed: $TESTS_FAILED
tests_skipped: $TESTS_SKIPPED
timestamp: $TIMESTAMP
git_sha: $GIT_SHA
---

# Test Execution Report

## Summary
- **Status**: $STATUS_EMOJI $STATUS ($PASS_RATE% pass rate)
- **Total Tests**: $TESTS_TOTAL
- **Passed**: $TESTS_PASSED
- **Failed**: $TESTS_FAILED
- **Skipped**: $TESTS_SKIPPED
- **Threshold**: 90% (PASS if >= 90%)

## Pass Rate Analysis
- **Required**: 90.0%
- **Achieved**: $PASS_RATE%
- **Margin**: $(awk "BEGIN {printf \"%.1f\", $PASS_RATE - 90.0}")%

[... rest of report body ...]
EOF
```

### For staging-deploy Skill

**Validating the Report**:

```bash
# Check report exists
if [ ! -f "test-results/test-execution-report.md" ]; then
    echo "❌ FAIL: No test execution report found"
    exit 1
fi

# Check status is PASS (90%+ pass rate)
if ! grep -q "^status: PASS" test-results/test-execution-report.md; then
    echo "❌ FAIL: Tests not passing or below 90% threshold"
    echo "Current status:"
    grep "^status:" test-results/test-execution-report.md
    echo "Pass rate:"
    grep "^pass_rate:" test-results/test-execution-report.md
    exit 1
fi

# Extract pass rate for logging
PASS_RATE=$(grep "^pass_rate:" test-results/test-execution-report.md | cut -d':' -f2 | tr -d ' ')
echo "✅ Tests passing (${PASS_RATE}% pass rate, threshold: 90%)"
```

## Why This Format?

### Industry Standard: YAML Frontmatter
- ✅ Used by Jekyll, Hugo, and many static site generators
- ✅ Machine-readable (easy to parse with `grep`, `awk`)
- ✅ Human-readable (clear key-value pairs)
- ✅ Extensible (can add fields without breaking parsers)

### Benefits
1. **Consistent**: Single location, single format
2. **Simple**: Easy to parse with standard tools (`grep`)
3. **Flexible**: Markdown body can contain detailed information
4. **Standard**: YAML frontmatter is widely recognized
5. **Parseable**: Both machines and humans can read it
6. **Reliable**: No ambiguity in status determination

## Parsing Examples

### Extract Status
```bash
grep "^status:" test-results/test-execution-report.md | cut -d':' -f2 | tr -d ' '
# Output: PASS
```

### Extract Pass Rate
```bash
grep "^pass_rate:" test-results/test-execution-report.md | cut -d':' -f2 | tr -d ' '
# Output: 95.5
```

### Check if Passing
```bash
if grep -q "^status: PASS" test-results/test-execution-report.md; then
    echo "Tests passed"
else
    echo "Tests failed"
fi
```

### Extract All Metrics
```bash
# Using awk to parse YAML frontmatter
awk '/^---$/{if (++n==2) exit} n==1' test-results/test-execution-report.md
```

## Version History

- **2025-11-18 v1.0**: Initial standardized format with 90% threshold
  - YAML frontmatter for machine-readable data
  - Markdown body for human-readable details
  - 90% pass rate threshold (changed from 100%)
  - Single location: `/test-results/test-execution-report.md`

## Related Documentation

- **test-executor agent**: `/.claude/agents/testing/test-executor.md`
- **staging-deploy skill**: `/.claude/skills/staging-deploy/SKILL.md`
- **Test Catalog**: `/docs/standards-processes/testing/TEST_CATALOG.md`
- **Testing Guide**: `/docs/standards-processes/testing/TESTING_GUIDE.md`
