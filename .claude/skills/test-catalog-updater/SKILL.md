---
name: test-catalog-updater
description: Automates TEST_CATALOG updates after test execution. Records test metrics, pass/fail status, coverage data, and execution time. Maintains single source of truth for all test files and their health status.
---

# TEST_CATALOG Updater Skill

**Purpose**: Automate TEST_CATALOG updates after every test execution.

**When to Use**: After running any tests (unit, integration, or E2E).

## Critical Rule: TEST_CATALOG Must Be Updated

**MANDATORY**: Every test execution MUST update TEST_CATALOG.

**Why**: TEST_CATALOG is the single source of truth for:
- Which tests exist
- Test health status (passing/failing/flaky)
- Test metrics (count, coverage, execution time)
- Test transformation history

## TEST_CATALOG Structure

**Location**: `/docs/standards-processes/testing/TEST_CATALOG.md` (Part 1)

**Multi-File Structure**:
- **Part 1**: Navigation + Current tests (E2E, React, Backend)
- **Part 2**: Historical test transformations
- **Part 3**: Archived/obsolete tests

## Update Types

### 1. Execution Metrics Update
After running tests, update:
- Total tests executed
- Pass/fail counts
- Execution time
- Coverage percentage
- Last run timestamp

### 2. Status Change Update
When test status changes:
- PASSING → FAILING (with reason)
- FAILING → PASSING (resolution)
- STABLE → FLAKY (inconsistent results)

### 3. New Test Discovery
When new tests are added:
- Add test file to catalog
- Document test purpose
- Set initial status: NEW
- Link to feature/requirement

### 4. Test Removal/Archiving
When tests are deleted or obsoleted:
- Move to TEST_CATALOG_PART_3.md
- Document why removed
- Preserve transformation history

## Automated Updater Script

```bash
#!/bin/bash
# TEST_CATALOG Updater Script

TEST_TYPE="$1"  # unit|integration|e2e
PASSED="$2"
FAILED="$3"
TOTAL="$4"
EXECUTION_TIME="$5"
COVERAGE="$6"

CATALOG="/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md"

if [ ! -f "$CATALOG" ]; then
    echo "❌ Error: TEST_CATALOG not found at $CATALOG"
    exit 1
fi

echo "TEST_CATALOG Update"
echo "==================="
echo "Test Type: $TEST_TYPE"
echo "Results: $PASSED passed, $FAILED failed, $TOTAL total"
echo ""

# Timestamp
TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")

# Calculate pass rate
PASS_RATE=$((PASSED * 100 / TOTAL))

# Determine status
if [ "$FAILED" -eq 0 ]; then
    STATUS="PASSING"
    STATUS_EMOJI="✅"
elif [ "$FAILED" -gt 0 ] && [ "$FAILED" -lt "$TOTAL" ]; then
    STATUS="PARTIAL"
    STATUS_EMOJI="⚠️"
else
    STATUS="FAILING"
    STATUS_EMOJI="❌"
fi

# Find the section to update
case "$TEST_TYPE" in
    "unit")
        SECTION="### Backend Unit Tests"
        ;;
    "integration")
        SECTION="### Backend Integration Tests"
        ;;
    "e2e")
        SECTION="### End-to-End Tests \\(Playwright\\)"
        ;;
    *)
        echo "❌ Error: Unknown test type: $TEST_TYPE"
        exit 1
        ;;
esac

# Create backup
cp "$CATALOG" "${CATALOG}.backup"

# Update metrics in catalog
# Find the metrics table and update it
awk -v section="$SECTION" \
    -v status="$STATUS_EMOJI $STATUS" \
    -v total="$TOTAL" \
    -v passed="$PASSED" \
    -v failed="$FAILED" \
    -v rate="$PASS_RATE%" \
    -v time="${EXECUTION_TIME}s" \
    -v coverage="${COVERAGE}%" \
    -v timestamp="$TIMESTAMP" '
    /^'"$SECTION"'/ { in_section=1 }
    in_section && /^\| Status/ {
        print
        getline
        print "| " status " | " total " | " passed " | " failed " | " rate " | " time " | " coverage " | " timestamp " |"
        in_section=0
        next
    }
    { print }
' "${CATALOG}.backup" > "$CATALOG"

echo "✅ TEST_CATALOG updated"
echo "   Section: $SECTION"
echo "   Status: $STATUS_EMOJI $STATUS"
echo "   Metrics: $PASSED/$TOTAL passed ($PASS_RATE%)"
echo ""

# If tests are failing, append failure details
if [ "$FAILED" -gt 0 ]; then
    echo "" >> "$CATALOG"
    echo "#### Recent Failures ($TIMESTAMP)" >> "$CATALOG"
    echo "" >> "$CATALOG"
    echo "Test Type: $TEST_TYPE" >> "$CATALOG"
    echo "Failures: $FAILED/$TOTAL tests" >> "$CATALOG"
    echo "Pass Rate: $PASS_RATE%" >> "$CATALOG"
    echo "" >> "$CATALOG"
    echo "**Action Required**: Investigate and fix failing tests." >> "$CATALOG"
    echo "" >> "$CATALOG"

    echo "⚠️  Failure details appended to catalog"
fi

# Update last catalog update timestamp in Part 1 header
sed -i '' "s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$CATALOG" 2>/dev/null || \
sed -i "s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$CATALOG"

echo "✅ Update complete"
```

## Usage Examples

### From test-executor Agent (Automated)
```
After running tests, I'll use the test-catalog-updater skill to record the results.
```

### Manual Update
```bash
# After running unit tests
bash .claude/skills/test-catalog-updater.md \
  unit \
  45 \  # passed
  0 \   # failed
  45 \  # total
  12.3 \  # execution time (seconds)
  85    # coverage percentage
```

### From CI/CD Pipeline
```bash
# Extract test results and update catalog
PASSED=$(grep "Passed:" test-results.log | cut -d: -f2)
FAILED=$(grep "Failed:" test-results.log | cut -d: -f2)
TOTAL=$((PASSED + FAILED))

bash .claude/skills/test-catalog-updater.md \
  e2e \
  "$PASSED" \
  "$FAILED" \
  "$TOTAL" \
  "125.7" \
  "N/A"
```

## Metrics to Track

### Per Test Suite
| Metric | Description | Source |
|--------|-------------|--------|
| Total Tests | Count of test methods/functions | Test runner output |
| Passed | Number passing | Test runner output |
| Failed | Number failing | Test runner output |
| Pass Rate | Percentage passing | Calculated |
| Execution Time | Total run time | Test runner output |
| Coverage | Code coverage % | Coverage tool output |
| Last Run | Timestamp of execution | System date |
| Status | PASSING/FAILING/FLAKY | Calculated |

### Aggregate Metrics
- Total tests across all suites
- Overall pass rate
- Total execution time
- Average coverage
- Flaky test count
- Tests added/removed this week

## Status Definitions

### PASSING ✅
- All tests passing
- No failures in last 3 runs
- Coverage meets or exceeds target

### FAILING ❌
- One or more tests failing
- Consistent failures
- Requires immediate attention

### FLAKY ⚠️
- Intermittent failures
- Passes sometimes, fails others
- Indicates unreliable test or race condition

### NEW 🆕
- Recently added test
- Not yet run in CI/CD
- Awaiting stability verification

### ARCHIVED 📦
- Test no longer active
- Moved to Part 3 of catalog
- Preserved for historical reference

## Catalog Format

```markdown
## Test Summary Metrics

**Last Updated**: 2025-11-04 14:30:00

### Overall Status
- **Total Tests**: 245
- **Passing**: 240 (98%)
- **Failing**: 5 (2%)
- **Flaky**: 3 (1%)

### End-to-End Tests (Playwright)

**Status**: ⚠️  PARTIAL (some failures)

| Status | Total | Passed | Failed | Pass Rate | Exec Time | Coverage | Last Run |
|--------|-------|--------|--------|-----------|-----------|----------|----------|
| ⚠️  PARTIAL | 45 | 42 | 3 | 93% | 125.3s | N/A | 2025-11-04 14:25 |

**Test Files**: 12 spec files in `/tests/playwright/`

**Known Issues**:
- Login flow test flaky (race condition)
- Admin dashboard timeout on slow machines
- Event registration occasionally times out

### Backend Unit Tests

**Status**: ✅ PASSING

| Status | Total | Passed | Failed | Pass Rate | Exec Time | Coverage | Last Run |
|--------|-------|--------|--------|-----------|-----------|----------|----------|
| ✅ PASSING | 156 | 156 | 0 | 100% | 12.4s | 87% | 2025-11-04 14:20 |

**Test Files**: 24 test classes in `/tests/WitchCityRope.Core.Tests/`

### Backend Integration Tests

**Status**: ✅ PASSING

| Status | Total | Passed | Failed | Pass Rate | Exec Time | Coverage | Last Run |
|--------|-------|--------|--------|-----------|-----------|----------|----------|
| ✅ PASSING | 44 | 44 | 0 | 100% | 45.2s | 82% | 2025-11-04 14:22 |

**Test Files**: 8 test classes in `/tests/WitchCityRope.IntegrationTests/`
```

## Integration with Phase 4 Validator

**phase-4-validator depends on TEST_CATALOG:**
- Reads current test status
- Validates 100% pass rate
- Checks for flaky tests
- Verifies coverage targets

**test-catalog-updater feeds phase-4-validator:**
- Provides real-time test metrics
- Documents test health trends
- Identifies problem areas
- Tracks improvements

## Failure Response Workflow

When TEST_CATALOG shows failures:

1. **test-executor documents failure in catalog**
   - Updates status to FAILING
   - Records error details
   - Notes timestamp

2. **Orchestrator reads catalog**
   - Sees FAILING status
   - Determines responsible agent
   - Delegates fix work

3. **Developer agent fixes issue**
   - Implements fix
   - Verifies locally

4. **test-executor re-runs tests**
   - Confirms fix works
   - Updates catalog to PASSING

5. **phase-4-validator confirms**
   - All tests passing
   - Ready for next phase

## Common Issues

### Issue: Catalog Not Updated
**Symptom**: Tests run but catalog shows old data
**Impact**: Orchestrator makes decisions on stale information
**Solution**: Enforce catalog update in test-executor agent definition

### Issue: Status Lag
**Symptom**: Test passes but catalog still shows FAILING
**Impact**: False negative blocks workflow
**Solution**: Update catalog immediately after test run, before reporting

### Issue: Missing Metrics
**Symptom**: Some columns empty or "N/A"
**Impact**: Incomplete tracking
**Solution**: Extract metrics from test runner output consistently

### Issue: Catalog Merge Conflicts
**Symptom**: Multiple agents update catalog simultaneously
**Impact**: Lost updates or conflicts
**Solution**: Sequential test execution, or use file locking

## Output Format

```json
{
  "catalogUpdate": {
    "testType": "unit",
    "timestamp": "2025-11-04 14:30:00",
    "metrics": {
      "total": 156,
      "passed": 156,
      "failed": 0,
      "passRate": 100,
      "executionTime": 12.4,
      "coverage": 87
    },
    "status": {
      "previous": "PASSING",
      "current": "PASSING",
      "changed": false
    },
    "location": "/docs/standards-processes/testing/TEST_CATALOG.md",
    "section": "### Backend Unit Tests",
    "backup": "/docs/standards-processes/testing/TEST_CATALOG.md.backup"
  },
  "nextSteps": [
    "All tests passing - no action needed",
    "Coverage at 87% (target: 80%) - maintaining"
  ]
}
```

## Progressive Disclosure

**Initial Context**: Show test type and pass/fail summary
**On Request**: Show full metrics and catalog location
**On Failure**: Show failure details and required actions
**On Pass**: Show concise confirmation

---

**Remember**: TEST_CATALOG is the memory for test health. Without it, every test run starts from zero context. With it, trends emerge, problems surface early, and test quality continuously improves.
