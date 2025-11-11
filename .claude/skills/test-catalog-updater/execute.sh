#!/bin/bash
# TEST_CATALOG Updater Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script automates TEST_CATALOG updates after test execution with:
# - Test metrics recording (count, pass/fail, coverage)
# - Status tracking (PASSING/FAILING/FLAKY)
# - Historical data preservation

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

# Check if we got the required parameters
if [ "$#" -lt 4 ]; then
    echo "📊 TEST_CATALOG Updater"
    echo "======================="
    echo ""
    echo "📋 Purpose: Update TEST_CATALOG with test execution results"
    echo ""
    echo "✅ Use when:"
    echo "   • After running any tests (unit, integration, E2E)"
    echo "   • To record test metrics and status"
    echo "   • To maintain test health history"
    echo ""
    echo "📊 What this script does:"
    echo "   1. Records test execution metrics"
    echo "   2. Calculates pass rate and status"
    echo "   3. Updates TEST_CATALOG with results"
    echo "   4. Preserves historical data (creates backup)"
    echo "   5. Appends failure details if tests failed"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <test-type> <passed> <failed> <total> [execution-time] [coverage]"
    echo ""
    echo "   test-type:      unit | integration | e2e"
    echo "   passed:         Number of tests that passed"
    echo "   failed:         Number of tests that failed"
    echo "   total:          Total number of tests"
    echo "   execution-time: (optional) Total execution time in seconds"
    echo "   coverage:       (optional) Coverage percentage"
    echo ""
    echo "   Example:"
    echo "   bash execute.sh unit 45 0 45 12.3 85"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

TEST_TYPE="$1"
PASSED="$2"
FAILED="$3"
TOTAL="$4"
EXECUTION_TIME="${5:-N/A}"
COVERAGE="${6:-N/A}"

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📊 TEST_CATALOG Update"
echo "======================"
echo ""

# Check 1: Validate test type
echo "1️⃣  Validating parameters..."
case "$TEST_TYPE" in
    "unit"|"integration"|"e2e")
        echo "   Test Type: $TEST_TYPE ✅"
        ;;
    *)
        echo "   ❌ FAIL: Invalid test type: $TEST_TYPE"
        echo ""
        echo "💡 Valid types: unit, integration, e2e"
        exit 1
        ;;
esac

# Check 2: Validate numbers
if ! [[ "$PASSED" =~ ^[0-9]+$ ]] || ! [[ "$FAILED" =~ ^[0-9]+$ ]] || ! [[ "$TOTAL" =~ ^[0-9]+$ ]]; then
    echo "   ❌ FAIL: Invalid numeric parameters"
    echo "   Passed: $PASSED, Failed: $FAILED, Total: $TOTAL"
    echo ""
    echo "💡 All counts must be positive integers"
    exit 1
fi
echo "   Results: $PASSED passed, $FAILED failed, $TOTAL total ✅"

# Check 3: Validate TEST_CATALOG exists
echo ""
echo "2️⃣  Locating TEST_CATALOG..."
CATALOG="/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md"

if [ ! -f "$CATALOG" ]; then
    echo "   ❌ FAIL: TEST_CATALOG not found at:"
    echo "   $CATALOG"
    echo ""
    echo "💡 Ensure you're in the project root directory"
    exit 1
fi
echo "   ✅ TEST_CATALOG found"
echo ""

# Check 4: Verify catalog is writable
echo "3️⃣  Checking permissions..."
if [ ! -w "$CATALOG" ]; then
    echo "   ❌ FAIL: TEST_CATALOG is not writable"
    echo ""
    echo "💡 Check file permissions:"
    echo "   ls -l $CATALOG"
    exit 1
fi
echo "   ✅ Catalog is writable"
echo ""

echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - CATALOG UPDATE
# ============================================

# Generate timestamp
TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")

# Calculate pass rate (avoid division by zero)
if [ "$TOTAL" -eq 0 ]; then
    PASS_RATE=0
else
    PASS_RATE=$((PASSED * 100 / TOTAL))
fi

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

echo "📝 Update Details:"
echo "   Test Type: $TEST_TYPE"
echo "   Status: $STATUS_EMOJI $STATUS"
echo "   Metrics: $PASSED/$TOTAL passed ($PASS_RATE%)"
echo "   Execution Time: ${EXECUTION_TIME}s"
echo "   Coverage: ${COVERAGE}%"
echo ""

# Determine catalog section to update
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
esac

echo "1️⃣  Creating backup..."
# Create backup
cp "$CATALOG" "${CATALOG}.backup"
echo "   ✅ Backup created: ${CATALOG}.backup"
echo ""

echo "2️⃣  Updating TEST_CATALOG..."
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

echo "   ✅ Metrics table updated"
echo "   Section: $SECTION"
echo ""

# Step 3: Update last updated timestamp in header
echo "3️⃣  Updating catalog timestamp..."
# Try macOS sed first, fall back to Linux sed
sed -i '' "s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$CATALOG" 2>/dev/null || \
sed -i "s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$CATALOG"
echo "   ✅ Catalog timestamp updated"
echo ""

# Step 4: Append failure details if tests failed
if [ "$FAILED" -gt 0 ]; then
    echo "4️⃣  Appending failure details..."
    echo "" >> "$CATALOG"
    echo "#### Recent Failures ($TIMESTAMP)" >> "$CATALOG"
    echo "" >> "$CATALOG"
    echo "Test Type: $TEST_TYPE" >> "$CATALOG"
    echo "Failures: $FAILED/$TOTAL tests" >> "$CATALOG"
    echo "Pass Rate: $PASS_RATE%" >> "$CATALOG"
    echo "" >> "$CATALOG"
    echo "**Action Required**: Investigate and fix failing tests." >> "$CATALOG"
    echo "" >> "$CATALOG"
    echo "   ⚠️  Failure details appended to catalog"
    echo ""
fi

echo "✅ TEST_CATALOG Update Complete"
echo "================================"
echo ""
echo "📊 Summary:"
echo "   • Test Type: $TEST_TYPE"
echo "   • Status: $STATUS_EMOJI $STATUS"
echo "   • Results: $PASSED passed, $FAILED failed, $TOTAL total"
echo "   • Pass Rate: $PASS_RATE%"
echo "   • Execution Time: ${EXECUTION_TIME}s"
echo "   • Coverage: ${COVERAGE}%"
echo "   • Updated: $TIMESTAMP"
echo ""
echo "📁 Files:"
echo "   • Catalog: $CATALOG"
echo "   • Backup: ${CATALOG}.backup"
echo ""

if [ "$FAILED" -gt 0 ]; then
    echo "🎯 Next Steps:"
    echo "   1. Review TEST_CATALOG for failure details"
    echo "   2. Investigate failing tests"
    echo "   3. Fix issues and re-run tests"
    echo "   4. Update catalog again after fixes"
    echo ""
fi

exit 0
