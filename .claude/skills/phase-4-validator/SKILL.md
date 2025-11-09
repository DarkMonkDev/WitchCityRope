---
name: phase-4-validator
description: Validates Testing Phase completion before advancing to Finalization Phase. Checks test execution results, coverage targets, test quality, and environment health. Ensures 100% test pass rate required by quality gates.
---

# Phase 4 (Testing) Validation Skill

**Purpose**: Automate validation of Testing Phase before advancing to Finalization Phase.

**When to Use**: When orchestrator needs to verify all tests are passing and ready for deployment.

## Critical Rule: 100% Test Pass Rate Required

**ALL work types (Feature/Bug/Hotfix/Docs/Refactor) require 100% test pass rate.**

No exceptions. One failing test = Phase 4 fails.

## Quick Validation

```bash
# Environment health check
echo "Checking Docker environment..."
docker ps --format "{{.Names}}\t{{.Status}}" | grep witchcity

# Quick test run
echo "Running all tests..."
./dev.sh test
```

## Quality Gate Checklist (100% Test Pass Rate Required)

### Environment Health (15 points)
- [ ] All Docker containers running (5 points)
- [ ] Database healthy and seeded (5 points)
- [ ] API service responding (3 points)
- [ ] Web service responding (2 points)

### Test Execution (40 points - MUST BE 40/40)
- [ ] Unit tests: 100% passing (15 points)
- [ ] Integration tests: 100% passing (15 points)
- [ ] E2E tests: 100% passing (10 points)

### Test Coverage (15 points)
- [ ] API coverage ≥ 80% (5 points)
- [ ] React coverage ≥ 70% (5 points)
- [ ] Critical paths covered (5 points)

### Test Quality (15 points)
- [ ] No flaky tests (5 points)
- [ ] Test execution time acceptable (3 points)
- [ ] Tests are independent (4 points)
- [ ] Test data cleanup working (3 points)

### Documentation (15 points)
- [ ] TEST_CATALOG updated (5 points)
- [ ] Test results documented (5 points)
- [ ] Known issues documented (3 points)
- [ ] Performance metrics recorded (2 points)

## Automated Validation Script

```bash
#!/bin/bash
# Phase 4 Validation Script

SCORE=0
MAX_SCORE=100
REQUIRED_PERCENTAGE=100  # ALL work types need 100%

echo "Phase 4 Testing Validation"
echo "=========================="
echo ""

# Environment health check (CRITICAL)
echo "Environment Health Check"
echo "------------------------"

# Docker containers
CONTAINER_COUNT=$(docker ps --format "{{.Names}}" | grep -c "witchcity")
if [ "$CONTAINER_COUNT" -eq 3 ]; then
    echo "✅ All Docker containers running"
    ((SCORE+=5))
else
    echo "❌ CRITICAL: Not all containers running ($CONTAINER_COUNT/3)"
    echo "   Run: ./dev.sh to start environment"
    exit 1
fi

# Database health
curl -f http://localhost:5653/health/database > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Database healthy"
    ((SCORE+=5))
else
    echo "❌ CRITICAL: Database unhealthy"
    exit 1
fi

# API service
curl -f http://localhost:5653/health > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ API service responding"
    ((SCORE+=3))
else
    echo "❌ CRITICAL: API service not responding"
    exit 1
fi

# Web service
curl -f http://localhost:5173 > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Web service responding"
    ((SCORE+=2))
else
    echo "❌ CRITICAL: Web service not responding"
    exit 1
fi

echo ""

# Test execution (MUST BE 100%)
echo "Test Execution Results"
echo "----------------------"

# Unit tests
echo "Running unit tests..."
cd /home/chad/repos/witchcityrope
dotnet test tests/WitchCityRope.Core.Tests --logger "console;verbosity=minimal" > /tmp/unit-tests.log 2>&1
UNIT_EXIT_CODE=$?

if [ $UNIT_EXIT_CODE -eq 0 ]; then
    UNIT_PASSED=$(grep -oP "Passed: \K\d+" /tmp/unit-tests.log | tail -1)
    UNIT_FAILED=$(grep -oP "Failed: \K\d+" /tmp/unit-tests.log | tail -1)

    if [ -z "$UNIT_FAILED" ] || [ "$UNIT_FAILED" -eq 0 ]; then
        echo "✅ Unit tests: 100% passing ($UNIT_PASSED tests)"
        ((SCORE+=15))
    else
        echo "❌ Unit tests: $UNIT_FAILED failing"
        echo "   See: /tmp/unit-tests.log"
        echo ""
        echo "CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    echo "❌ Unit tests: Failed to execute"
    echo "   See: /tmp/unit-tests.log"
    exit 1
fi

# Integration tests
echo "Running integration tests..."
dotnet test tests/WitchCityRope.IntegrationTests --logger "console;verbosity=minimal" > /tmp/integration-tests.log 2>&1
INTEGRATION_EXIT_CODE=$?

if [ $INTEGRATION_EXIT_CODE -eq 0 ]; then
    INT_PASSED=$(grep -oP "Passed: \K\d+" /tmp/integration-tests.log | tail -1)
    INT_FAILED=$(grep -oP "Failed: \K\d+" /tmp/integration-tests.log | tail -1)

    if [ -z "$INT_FAILED" ] || [ "$INT_FAILED" -eq 0 ]; then
        echo "✅ Integration tests: 100% passing ($INT_PASSED tests)"
        ((SCORE+=15))
    else
        echo "❌ Integration tests: $INT_FAILED failing"
        echo "   See: /tmp/integration-tests.log"
        echo ""
        echo "CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    echo "❌ Integration tests: Failed to execute"
    exit 1
fi

# E2E tests
echo "Running E2E tests..."
cd tests/playwright
npm test -- --reporter=json > /tmp/e2e-results.json 2>&1
E2E_EXIT_CODE=$?

if [ $E2E_EXIT_CODE -eq 0 ]; then
    E2E_PASSED=$(grep -oP '"expectedStatus":"passed"' /tmp/e2e-results.json | wc -l)
    E2E_FAILED=$(grep -oP '"expectedStatus":"failed"' /tmp/e2e-results.json | wc -l)

    if [ "$E2E_FAILED" -eq 0 ]; then
        echo "✅ E2E tests: 100% passing ($E2E_PASSED tests)"
        ((SCORE+=10))
    else
        echo "❌ E2E tests: $E2E_FAILED failing"
        echo "   See: tests/playwright/playwright-report/"
        echo ""
        echo "CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    echo "❌ E2E tests: Failed to execute"
    exit 1
fi

echo ""

# Test coverage
echo "Test Coverage Analysis"
echo "----------------------"

# API coverage
if [ -f "test-results/coverage/api/coverage.xml" ]; then
    API_COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' test-results/coverage/api/coverage.xml | head -1)
    API_COVERAGE_PCT=$(echo "$API_COVERAGE * 100" | bc | cut -d. -f1)

    if [ "$API_COVERAGE_PCT" -ge 80 ]; then
        echo "✅ API coverage: ${API_COVERAGE_PCT}% (target: 80%)"
        ((SCORE+=5))
    else
        echo "⚠️  API coverage: ${API_COVERAGE_PCT}% (below 80% target)"
        ((SCORE+=3))
    fi
else
    echo "⚠️  API coverage report not found"
fi

# React coverage
if [ -f "test-results/coverage/web/coverage-summary.json" ]; then
    WEB_COVERAGE=$(grep -oP '"lines":\{"total":\d+,"covered":\K\d+' test-results/coverage/web/coverage-summary.json)
    WEB_TOTAL=$(grep -oP '"lines":\{"total":\K\d+' test-results/coverage/web/coverage-summary.json)
    WEB_COVERAGE_PCT=$((WEB_COVERAGE * 100 / WEB_TOTAL))

    if [ "$WEB_COVERAGE_PCT" -ge 70 ]; then
        echo "✅ React coverage: ${WEB_COVERAGE_PCT}% (target: 70%)"
        ((SCORE+=5))
    else
        echo "⚠️  React coverage: ${WEB_COVERAGE_PCT}% (below 70% target)"
        ((SCORE+=3))
    fi
else
    echo "⚠️  React coverage report not found"
fi

# Critical paths covered
CRITICAL_PATH_TESTS=$(grep -r "@critical\|critical test" tests/ --include="*.cs" --include="*.ts" | wc -l)
if [ "$CRITICAL_PATH_TESTS" -ge 5 ]; then
    echo "✅ Critical paths covered ($CRITICAL_PATH_TESTS critical tests)"
    ((SCORE+=5))
else
    echo "⚠️  Limited critical path coverage ($CRITICAL_PATH_TESTS tests)"
    ((SCORE+=3))
fi

echo ""

# Test quality
echo "Test Quality Assessment"
echo "-----------------------"

# Flaky tests check
FLAKY_COUNT=$(grep -i "flaky\|intermittent" test-results/*.log 2>/dev/null | wc -l)
if [ "$FLAKY_COUNT" -eq 0 ]; then
    echo "✅ No flaky tests detected"
    ((SCORE+=5))
else
    echo "⚠️  Flaky tests detected: $FLAKY_COUNT"
    ((SCORE+=2))
fi

# Test execution time
TOTAL_TIME=$(grep -oP "Time: \K[0-9.]+s" /tmp/unit-tests.log /tmp/integration-tests.log 2>/dev/null | awk '{sum+=$1} END {print sum}')
if [ $(echo "$TOTAL_TIME < 300" | bc) -eq 1 ]; then
    echo "✅ Test execution time: ${TOTAL_TIME}s (acceptable)"
    ((SCORE+=3))
else
    echo "⚠️  Test execution time: ${TOTAL_TIME}s (slow)"
    ((SCORE+=1))
fi

# Test independence check
SHARED_STATE_COUNT=$(grep -r "static\|shared\|singleton" tests/ --include="*.cs" --include="*.ts" | wc -l)
if [ "$SHARED_STATE_COUNT" -lt 5 ]; then
    echo "✅ Tests are independent"
    ((SCORE+=4))
else
    echo "⚠️  Potential shared state detected"
    ((SCORE+=2))
fi

# Cleanup check
if grep -q "IDisposable\|cleanup\|teardown" tests/ -r --include="*.cs" --include="*.ts"; then
    echo "✅ Test cleanup implemented"
    ((SCORE+=3))
else
    echo "⚠️  Test cleanup not verified"
    ((SCORE+=1))
fi

echo ""

# Documentation
echo "Documentation Validation"
echo "------------------------"

# TEST_CATALOG updated
TEST_CATALOG="/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md"
CATALOG_UPDATED=$(find "$TEST_CATALOG" -mtime -1 2>/dev/null)
if [ -n "$CATALOG_UPDATED" ]; then
    echo "✅ TEST_CATALOG updated recently"
    ((SCORE+=5))
else
    echo "⚠️  TEST_CATALOG not updated recently"
    ((SCORE+=2))
fi

# Test results documented
if [ -f "test-results/test-execution-report.md" ]; then
    echo "✅ Test results documented"
    ((SCORE+=5))
else
    echo "⚠️  Test results not documented"
    ((SCORE+=2))
fi

# Known issues
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*" | sort | tail -1)
if [ -f "$NEW_WORK_DIR/testing-notes.md" ] && grep -q "## Known Issues" "$NEW_WORK_DIR/testing-notes.md"; then
    echo "✅ Known issues documented"
    ((SCORE+=3))
else
    echo "⚠️  Known issues not documented"
    ((SCORE+=1))
fi

# Performance metrics
if [ -f "test-results/performance-metrics.json" ]; then
    echo "✅ Performance metrics recorded"
    ((SCORE+=2))
else
    echo "⚠️  Performance metrics not recorded"
    ((SCORE+=1))
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo ""

# CRITICAL: Test execution must be 100%
TEST_SCORE=40  # From unit (15) + integration (15) + E2E (10)
if [ "$SCORE" -ge 40 ] && [ "$UNIT_EXIT_CODE" -eq 0 ] && [ "$INTEGRATION_EXIT_CODE" -eq 0 ] && [ "$E2E_EXIT_CODE" -eq 0 ]; then
    echo "✅ PASS - Testing Phase complete"
    echo "   All tests passing at 100%"
    echo "   Ready to advance to Finalization Phase"
    exit 0
else
    echo "❌ FAIL - Testing Phase incomplete"
    echo ""
    echo "CRITICAL: ALL tests must pass (100% pass rate required)"
    echo ""
    if [ "$UNIT_EXIT_CODE" -ne 0 ]; then
        echo "   - Unit tests failing"
    fi
    if [ "$INTEGRATION_EXIT_CODE" -ne 0 ]; then
        echo "   - Integration tests failing"
    fi
    if [ "$E2E_EXIT_CODE" -ne 0 ]; then
        echo "   - E2E tests failing"
    fi
    exit 1
fi
```

## Usage Examples

### From Orchestrator
```
Use the phase-4-validator skill to verify all tests are passing
```

### Manual Validation
```bash
# Run complete validation
bash .claude/skills/phase-4-validator.md
```

## Common Issues

### Issue: Environment Not Healthy
**Solution**: Run environment checks first
```bash
./dev.sh
# Wait for all containers to be healthy
docker ps
```

### Issue: Failing Tests
**Solution**: Phase 4 CANNOT advance with failing tests
- Loop back to react-developer for UI test failures
- Loop back to backend-developer for API test failures
- Loop back to test-developer for test logic issues
- test-executor handles environment issues only

### Issue: Low Coverage
**Warning but not blocker**: Coverage below targets is a warning but doesn't block if all tests pass.

**However**: Should document in testing-notes.md as known limitation.

### Issue: Flaky Tests
**Solution**: Flaky tests = unreliable validation
- Must be fixed before advancing
- Document patterns causing flakiness
- Add to lessons learned

## Zero Tolerance Policy

**Testing Phase has ZERO TOLERANCE for failing tests.**

Even a single failing test means:
- ❌ Phase 4 validation FAILS
- ❌ Cannot advance to Phase 5
- ❌ Must loop back to implementation

**Why**: Deploying with failing tests risks production issues.

## Output Format

```json
{
  "phase": "testing",
  "status": "pass|fail",
  "score": 95,
  "maxScore": 100,
  "percentage": 95,
  "testResults": {
    "unit": {
      "total": 45,
      "passed": 45,
      "failed": 0,
      "passRate": 100
    },
    "integration": {
      "total": 12,
      "passed": 12,
      "failed": 0,
      "passRate": 100
    },
    "e2e": {
      "total": 8,
      "passed": 8,
      "failed": 0,
      "passRate": 100
    }
  },
  "coverage": {
    "api": 85,
    "web": 72
  },
  "environment": {
    "docker": "healthy",
    "database": "healthy",
    "api": "responding",
    "web": "responding"
  },
  "quality": {
    "flakyTests": 0,
    "executionTime": 125.3,
    "independent": true,
    "cleanupWorking": true
  },
  "readyForNextPhase": true
}
```

## Integration with Quality Gates

**ALL work types require 100% test pass rate:**
- **Feature**: 100% pass (40/40 test points) + coverage/quality
- **Bug Fix**: 100% pass (40/40 test points) + coverage/quality
- **Hotfix**: 100% pass (40/40 test points) + minimal other requirements
- **Refactoring**: 100% pass (40/40 test points) + no regressions

## Progressive Disclosure

**Initial Context**: Show pass/fail status only
**On Request**: Show detailed test results and coverage
**On Failure**: Show specific failing tests with logs
**On Pass**: Show concise summary + quality metrics

---

**Remember**: Phase 4 is the quality gate. If tests aren't 100% passing, nothing advances. This protects production from bugs.
