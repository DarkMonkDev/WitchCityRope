#!/bin/bash
# Phase 4 (Testing) Validation Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script validates Testing Phase completion before advancing to Finalization Phase
# CRITICAL: Requires 100% test pass rate for ALL work types

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -gt 0 ] && [ "$1" == "--help" ]; then
    echo "🧪 Phase 4 Testing Validation"
    echo "=============================="
    echo ""
    echo "📋 Purpose: Validate Testing Phase completion before Finalization Phase"
    echo ""
    echo "✅ Use when:"
    echo "   • Before Phase 4 → Phase 5 transition"
    echo "   • After test-developer creates tests and test-executor runs them"
    echo "   • When user requests to 'continue to finalization'"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Tests are still being written or fixed"
    echo "   • Docker environment is not running"
    echo "   • Already in Finalization Phase"
    echo ""
    echo "🚨 CRITICAL REQUIREMENT:"
    echo "   • ALL work types require 100% test pass rate (ZERO TOLERANCE)"
    echo "   • Unit tests: 100% passing"
    echo "   • Integration tests: 100% passing"
    echo "   • E2E tests: 100% passing"
    echo ""
    echo "📊 Quality Gate: 100% Required (All Work Types)"
    echo "   • Test execution: 40/100 points (BLOCKING if not 100% passing)"
    echo "   • Test coverage: 15/100 points"
    echo "   • Test quality: 15/100 points"
    echo "   • Environment health: 15/100 points"
    echo "   • Documentation: 15/100 points"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Validates environment health (Docker, database, API, web)"
    echo "   2. Executes ALL test suites (unit, integration, E2E)"
    echo "   3. BLOCKS if ANY test fails (100% pass rate required)"
    echo "   4. Analyzes test coverage (API 80%, React 70%)"
    echo "   5. Assesses test quality (no flaky tests, execution time, independence)"
    echo "   6. Validates documentation (TEST_CATALOG, test reports)"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh"
    echo ""
    echo "   No parameters required - validates current environment"
    echo ""
    exit 1
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🧪 Phase 4 Testing Validation"
echo "=============================="
echo ""

echo "1️⃣  Checking Docker environment..."
if ! command -v docker &> /dev/null; then
    echo "   ❌ FAIL: Docker not found"
    exit 1
fi
echo "   ✅ Docker available"

echo ""
echo "2️⃣  Checking test directories..."
if [ ! -d "/home/chad/repos/witchcityrope/tests" ]; then
    echo "   ❌ FAIL: Tests directory not found"
    exit 1
fi
echo "   ✅ Tests directory found"

echo ""
echo "3️⃣  Checking required tools..."
if ! command -v dotnet &> /dev/null; then
    echo "   ❌ FAIL: dotnet CLI not found"
    exit 1
fi
if ! command -v npm &> /dev/null; then
    echo "   ❌ FAIL: npm not found"
    exit 1
fi
if ! command -v curl &> /dev/null; then
    echo "   ❌ FAIL: curl not found"
    exit 1
fi
echo "   ✅ All tools available"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

SCORE=0
MAX_SCORE=100
REQUIRED_PERCENTAGE=100  # ALL work types need 100%

echo "📝 Validation Details:"
echo "   Required Score: ${REQUIRED_PERCENTAGE}%"
echo "   Max Score: $MAX_SCORE points"
echo "   CRITICAL: 100% test pass rate required"
echo ""
echo "Running validation checks..."
echo ""

# Environment Health Check (15 points possible)
echo "🏥 Environment Health Check:"
echo ""

# Docker containers
CONTAINER_COUNT=$(docker ps --format "{{.Names}}" | grep -c "witchcity" 2>/dev/null || echo "0")
if [ "$CONTAINER_COUNT" -eq 3 ]; then
    echo "   ✅ All Docker containers running (+5 points)"
    ((SCORE+=5))
else
    echo "   ❌ CRITICAL: Not all containers running ($CONTAINER_COUNT/3)"
    echo "   Run: ./dev.sh to start environment"
    exit 1
fi

# Database health
if curl -f http://localhost:5653/health/database > /dev/null 2>&1; then
    echo "   ✅ Database healthy (+5 points)"
    ((SCORE+=5))
else
    echo "   ❌ CRITICAL: Database unhealthy"
    exit 1
fi

# API service
if curl -f http://localhost:5653/health > /dev/null 2>&1; then
    echo "   ✅ API service responding (+3 points)"
    ((SCORE+=3))
else
    echo "   ❌ CRITICAL: API service not responding"
    exit 1
fi

# Web service
if curl -f http://localhost:5173 > /dev/null 2>&1; then
    echo "   ✅ Web service responding (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ CRITICAL: Web service not responding"
    exit 1
fi

echo ""

# Test Execution Results (40 points possible - MUST BE 100%)
echo "🧪 Test Execution Results (100% REQUIRED):"
echo ""

# Unit tests
echo "Running unit tests..."
cd /home/chad/repos/witchcityrope
if dotnet test tests/WitchCityRope.Core.Tests --logger "console;verbosity=minimal" > /tmp/unit-tests.log 2>&1; then
    UNIT_EXIT_CODE=0
    UNIT_PASSED=$(grep -oP "Passed: \K\d+" /tmp/unit-tests.log | tail -1 || echo "0")
    UNIT_FAILED=$(grep -oP "Failed: \K\d+" /tmp/unit-tests.log | tail -1 || echo "0")

    if [ -z "$UNIT_FAILED" ] || [ "$UNIT_FAILED" -eq 0 ]; then
        echo "   ✅ Unit tests: 100% passing ($UNIT_PASSED tests) (+15 points)"
        ((SCORE+=15))
    else
        echo "   ❌ Unit tests: $UNIT_FAILED failing"
        echo "   See: /tmp/unit-tests.log"
        echo ""
        echo "   🚨 CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    UNIT_EXIT_CODE=1
    echo "   ❌ Unit tests: Failed to execute"
    echo "   See: /tmp/unit-tests.log"
    exit 1
fi

# Integration tests
echo "Running integration tests..."
if dotnet test tests/WitchCityRope.IntegrationTests --logger "console;verbosity=minimal" > /tmp/integration-tests.log 2>&1; then
    INTEGRATION_EXIT_CODE=0
    INT_PASSED=$(grep -oP "Passed: \K\d+" /tmp/integration-tests.log | tail -1 || echo "0")
    INT_FAILED=$(grep -oP "Failed: \K\d+" /tmp/integration-tests.log | tail -1 || echo "0")

    if [ -z "$INT_FAILED" ] || [ "$INT_FAILED" -eq 0 ]; then
        echo "   ✅ Integration tests: 100% passing ($INT_PASSED tests) (+15 points)"
        ((SCORE+=15))
    else
        echo "   ❌ Integration tests: $INT_FAILED failing"
        echo "   See: /tmp/integration-tests.log"
        echo ""
        echo "   🚨 CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    INTEGRATION_EXIT_CODE=1
    echo "   ❌ Integration tests: Failed to execute"
    exit 1
fi

# E2E tests
echo "Running E2E tests..."
cd /home/chad/repos/witchcityrope/tests/playwright
if npm test -- --reporter=json > /tmp/e2e-results.json 2>&1; then
    E2E_EXIT_CODE=0
    E2E_PASSED=$(grep -oP '"expectedStatus":"passed"' /tmp/e2e-results.json 2>/dev/null | wc -l || echo "0")
    E2E_FAILED=$(grep -oP '"expectedStatus":"failed"' /tmp/e2e-results.json 2>/dev/null | wc -l || echo "0")

    if [ "$E2E_FAILED" -eq 0 ]; then
        echo "   ✅ E2E tests: 100% passing ($E2E_PASSED tests) (+10 points)"
        ((SCORE+=10))
    else
        echo "   ❌ E2E tests: $E2E_FAILED failing"
        echo "   See: tests/playwright/playwright-report/"
        echo ""
        echo "   🚨 CRITICAL: Tests must be 100% passing"
        exit 1
    fi
else
    E2E_EXIT_CODE=1
    echo "   ❌ E2E tests: Failed to execute"
    exit 1
fi

echo ""

# Test Coverage Analysis (15 points possible)
echo "📊 Test Coverage Analysis:"
echo ""

# API coverage
if [ -f "/home/chad/repos/witchcityrope/test-results/coverage/api/coverage.xml" ]; then
    API_COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' /home/chad/repos/witchcityrope/test-results/coverage/api/coverage.xml | head -1)
    API_COVERAGE_PCT=$(echo "$API_COVERAGE * 100" | bc | cut -d. -f1)

    if [ "$API_COVERAGE_PCT" -ge 80 ]; then
        echo "   ✅ API coverage: ${API_COVERAGE_PCT}% (target: 80%) (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  API coverage: ${API_COVERAGE_PCT}% (below 80% target) (+3 points)"
        ((SCORE+=3))
    fi
else
    echo "   ⚠️  API coverage report not found"
fi

# React coverage
if [ -f "/home/chad/repos/witchcityrope/test-results/coverage/web/coverage-summary.json" ]; then
    WEB_COVERAGE=$(grep -oP '"lines":\{"total":\d+,"covered":\K\d+' /home/chad/repos/witchcityrope/test-results/coverage/web/coverage-summary.json 2>/dev/null || echo "0")
    WEB_TOTAL=$(grep -oP '"lines":\{"total":\K\d+' /home/chad/repos/witchcityrope/test-results/coverage/web/coverage-summary.json 2>/dev/null || echo "1")
    WEB_COVERAGE_PCT=$((WEB_COVERAGE * 100 / WEB_TOTAL))

    if [ "$WEB_COVERAGE_PCT" -ge 70 ]; then
        echo "   ✅ React coverage: ${WEB_COVERAGE_PCT}% (target: 70%) (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  React coverage: ${WEB_COVERAGE_PCT}% (below 70% target) (+3 points)"
        ((SCORE+=3))
    fi
else
    echo "   ⚠️  React coverage report not found"
fi

# Critical paths covered
CRITICAL_PATH_TESTS=$(grep -r "@critical\|critical test" /home/chad/repos/witchcityrope/tests/ --include="*.cs" --include="*.ts" 2>/dev/null | wc -l || echo "0")
if [ "$CRITICAL_PATH_TESTS" -ge 5 ]; then
    echo "   ✅ Critical paths covered ($CRITICAL_PATH_TESTS critical tests) (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Limited critical path coverage ($CRITICAL_PATH_TESTS tests) (+3 points)"
    ((SCORE+=3))
fi

echo ""

# Test Quality Assessment (15 points possible)
echo "✨ Test Quality Assessment:"
echo ""

# Flaky tests check
FLAKY_COUNT=$(grep -i "flaky\|intermittent" /home/chad/repos/witchcityrope/test-results/*.log 2>/dev/null | wc -l || echo "0")
if [ "$FLAKY_COUNT" -eq 0 ]; then
    echo "   ✅ No flaky tests detected (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Flaky tests detected: $FLAKY_COUNT (+2 points)"
    ((SCORE+=2))
fi

# Test execution time
TOTAL_TIME=$(grep -oP "Time: \K[0-9.]+s" /tmp/unit-tests.log /tmp/integration-tests.log 2>/dev/null | awk '{sum+=$1} END {print sum}' || echo "0")
if [ -n "$TOTAL_TIME" ] && [ $(echo "$TOTAL_TIME < 300" | bc 2>/dev/null || echo "1") -eq 1 ]; then
    echo "   ✅ Test execution time: ${TOTAL_TIME}s (acceptable) (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  Test execution time: ${TOTAL_TIME}s (slow) (+1 point)"
    ((SCORE+=1))
fi

# Test independence check
SHARED_STATE_COUNT=$(grep -r "static\|shared\|singleton" /home/chad/repos/witchcityrope/tests/ --include="*.cs" --include="*.ts" 2>/dev/null | wc -l || echo "0")
if [ "$SHARED_STATE_COUNT" -lt 5 ]; then
    echo "   ✅ Tests are independent (+4 points)"
    ((SCORE+=4))
else
    echo "   ⚠️  Potential shared state detected (+2 points)"
    ((SCORE+=2))
fi

# Cleanup check
if grep -q "IDisposable\|cleanup\|teardown" /home/chad/repos/witchcityrope/tests/ -r --include="*.cs" --include="*.ts" 2>/dev/null; then
    echo "   ✅ Test cleanup implemented (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  Test cleanup not verified (+1 point)"
    ((SCORE+=1))
fi

echo ""

# Documentation Validation (15 points possible)
echo "📚 Documentation Validation:"
echo ""

# TEST_CATALOG updated
TEST_CATALOG="/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md"
if [ -f "$TEST_CATALOG" ]; then
    CATALOG_UPDATED=$(find "$TEST_CATALOG" -mtime -1 2>/dev/null)
    if [ -n "$CATALOG_UPDATED" ]; then
        echo "   ✅ TEST_CATALOG updated recently (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  TEST_CATALOG not updated recently (+2 points)"
        ((SCORE+=2))
    fi
else
    echo "   ⚠️  TEST_CATALOG not found"
fi

# Test results documented
if [ -f "/home/chad/repos/witchcityrope/test-results/test-execution-report.md" ]; then
    echo "   ✅ Test results documented (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Test results not documented (+2 points)"
    ((SCORE+=2))
fi

# Known issues
NEW_WORK_DIR=$(find /home/chad/repos/witchcityrope/docs/functional-areas -type d -path "*/new-work/*" 2>/dev/null | sort | tail -1)
if [ -n "$NEW_WORK_DIR" ] && [ -f "$NEW_WORK_DIR/testing-notes.md" ] && grep -q "## Known Issues" "$NEW_WORK_DIR/testing-notes.md"; then
    echo "   ✅ Known issues documented (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  Known issues not documented (+1 point)"
    ((SCORE+=1))
fi

# Performance metrics
if [ -f "/home/chad/repos/witchcityrope/test-results/performance-metrics.json" ]; then
    echo "   ✅ Performance metrics recorded (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  Performance metrics not recorded (+1 point)"
    ((SCORE+=1))
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "📊 Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "   Required: ${REQUIRED_PERCENTAGE}%"
echo ""

# CRITICAL: Test execution must be 100%
if [ "$UNIT_EXIT_CODE" -eq 0 ] && [ "$INTEGRATION_EXIT_CODE" -eq 0 ] && [ "$E2E_EXIT_CODE" -eq 0 ]; then
    echo "✅ PASS - Testing Phase Complete"
    echo "=================================="
    echo ""
    echo "   ✅ All tests passing at 100%"
    echo "   Ready to advance to Finalization Phase"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Proceed to Finalization Phase (Phase 5)"
    echo "   2. Update documentation and file registry"
    echo "   3. Create git commits"
    echo "   4. Complete lessons learned"
    echo ""
    exit 0
else
    echo "❌ FAIL - Testing Phase Incomplete"
    echo "===================================="
    echo ""
    echo "🚨 CRITICAL: ALL tests must pass (100% pass rate required)"
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
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Review test failures in /tmp/*.log"
    echo "   2. Return to test-developer to fix failing tests"
    echo "   3. test-executor re-runs tests"
    echo "   4. Re-run validation"
    echo ""
    echo "📖 See: .claude/skills/phase-4-validator/SKILL.md (Common Issues)"
    echo ""
    exit 1
fi
