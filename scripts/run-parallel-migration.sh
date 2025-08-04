#!/bin/bash

# Parallel Test Runner for Puppeteer to Playwright Migration
# Runs both test suites in parallel to compare results

set -e

# Configuration
BASE_URL="http://localhost:5651"
RESULTS_DIR="test-results/migration-comparison"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
COMPARISON_DIR="$RESULTS_DIR/$TIMESTAMP"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -s, --suite <name>       Run specific test suite (auth, events, admin, all)"
    echo "  -h, --headed             Run in headed mode (show browser)"
    echo "  -u, --url <url>          Base URL (default: http://localhost:5651)"
    echo "  --puppeteer-only         Run only Puppeteer tests"
    echo "  --playwright-only        Run only Playwright tests"
    echo "  --no-comparison          Skip comparison report"
    echo "  --help                   Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0 -s auth                      # Compare auth tests in both frameworks"
    echo "  $0 -h --suite events            # Compare events tests in headed mode"
    echo "  $0 --puppeteer-only             # Run only Puppeteer tests"
    exit 1
}

# Default values
TEST_SUITE=""
HEADED="false"
RUN_PUPPETEER="true"
RUN_PLAYWRIGHT="true"
COMPARE_RESULTS="true"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--suite)
            TEST_SUITE="$2"
            shift 2
            ;;
        -h|--headed)
            HEADED="true"
            shift
            ;;
        -u|--url)
            BASE_URL="$2"
            shift 2
            ;;
        --puppeteer-only)
            RUN_PLAYWRIGHT="false"
            COMPARE_RESULTS="false"
            shift
            ;;
        --playwright-only)
            RUN_PUPPETEER="false"
            COMPARE_RESULTS="false"
            shift
            ;;
        --no-comparison)
            COMPARE_RESULTS="false"
            shift
            ;;
        --help)
            usage
            ;;
        *)
            echo "Unknown option: $1"
            usage
            ;;
    esac
done

# Check if application is running
echo -e "${BLUE}Checking if application is running at $BASE_URL...${NC}"
if ! curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|301\|302"; then
    echo -e "${RED}❌ Error: Application is not running at $BASE_URL${NC}"
    echo "Please start the application first"
    exit 1
fi
echo -e "${GREEN}✅ Application is running${NC}"

# Create results directory
mkdir -p "$COMPARISON_DIR"
mkdir -p "$COMPARISON_DIR/puppeteer"
mkdir -p "$COMPARISON_DIR/playwright"

# Function to run Puppeteer tests
run_puppeteer_tests() {
    echo -e "\n${YELLOW}🎭 Running Puppeteer tests...${NC}"
    
    local PUPPETEER_CMD="npm test -- tests/e2e"
    
    # Add suite filter if specified
    if [[ -n "$TEST_SUITE" ]]; then
        case $TEST_SUITE in
            auth)
                PUPPETEER_CMD="$PUPPETEER_CMD --grep='(auth|login|authentication)'"
                ;;
            events)
                PUPPETEER_CMD="$PUPPETEER_CMD --grep='event'"
                ;;
            admin)
                PUPPETEER_CMD="$PUPPETEER_CMD --grep='admin'"
                ;;
        esac
    fi
    
    # Add headed mode
    if [[ "$HEADED" == "true" ]]; then
        export PUPPETEER_HEADLESS="false"
    fi
    
    # Run tests and capture output
    export BASE_URL="$BASE_URL"
    set +e
    $PUPPETEER_CMD > "$COMPARISON_DIR/puppeteer/output.log" 2>&1
    local exit_code=$?
    set -e
    
    # Generate summary
    echo "Exit Code: $exit_code" > "$COMPARISON_DIR/puppeteer/summary.txt"
    echo "Command: $PUPPETEER_CMD" >> "$COMPARISON_DIR/puppeteer/summary.txt"
    echo "Duration: $(grep -E 'passing|failing' "$COMPARISON_DIR/puppeteer/output.log" | head -1 || echo "N/A")" >> "$COMPARISON_DIR/puppeteer/summary.txt"
    
    # Extract test counts
    grep -E '(passing|failing|pending)' "$COMPARISON_DIR/puppeteer/output.log" >> "$COMPARISON_DIR/puppeteer/summary.txt" || true
    
    return $exit_code
}

# Function to run Playwright tests
run_playwright_tests() {
    echo -e "\n${YELLOW}🎭 Running Playwright tests...${NC}"
    
    local PLAYWRIGHT_CMD="npx playwright test"
    
    # Add suite filter if specified
    if [[ -n "$TEST_SUITE" ]]; then
        case $TEST_SUITE in
            auth)
                PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD **/auth/**/*.spec.ts **/login/**/*.spec.ts"
                ;;
            events)
                PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD **/events/**/*.spec.ts **/event-*.spec.ts"
                ;;
            admin)
                PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD **/admin/**/*.spec.ts **/admin-*.spec.ts"
                ;;
        esac
    fi
    
    # Add headed mode
    if [[ "$HEADED" == "true" ]]; then
        PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --headed --workers=1"
    fi
    
    # Add JSON reporter for comparison
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --reporter=json"
    
    # Run tests and capture output
    export BASE_URL="$BASE_URL"
    set +e
    $PLAYWRIGHT_CMD > "$COMPARISON_DIR/playwright/results.json" 2> "$COMPARISON_DIR/playwright/output.log"
    local exit_code=$?
    set -e
    
    # Generate summary
    echo "Exit Code: $exit_code" > "$COMPARISON_DIR/playwright/summary.txt"
    echo "Command: $PLAYWRIGHT_CMD" >> "$COMPARISON_DIR/playwright/summary.txt"
    
    # Extract test counts from JSON if available
    if [[ -f "$COMPARISON_DIR/playwright/results.json" ]]; then
        if command -v jq &> /dev/null; then
            local total=$(jq '.stats.expected' "$COMPARISON_DIR/playwright/results.json" 2>/dev/null || echo "0")
            local passed=$(jq '.stats.expected - .stats.unexpected - .stats.skipped' "$COMPARISON_DIR/playwright/results.json" 2>/dev/null || echo "0")
            local failed=$(jq '.stats.unexpected' "$COMPARISON_DIR/playwright/results.json" 2>/dev/null || echo "0")
            local skipped=$(jq '.stats.skipped' "$COMPARISON_DIR/playwright/results.json" 2>/dev/null || echo "0")
            
            echo "Total Tests: $total" >> "$COMPARISON_DIR/playwright/summary.txt"
            echo "Passed: $passed" >> "$COMPARISON_DIR/playwright/summary.txt"
            echo "Failed: $failed" >> "$COMPARISON_DIR/playwright/summary.txt"
            echo "Skipped: $skipped" >> "$COMPARISON_DIR/playwright/summary.txt"
        fi
    fi
    
    return $exit_code
}

# Function to generate comparison report
generate_comparison_report() {
    echo -e "\n${BLUE}📊 Generating comparison report...${NC}"
    
    local report_file="$COMPARISON_DIR/comparison-report.md"
    
    cat > "$report_file" << EOF
# Test Migration Comparison Report

**Date:** $(date)
**Test Suite:** ${TEST_SUITE:-all}
**Mode:** $([ "$HEADED" == "true" ] && echo "headed" || echo "headless")
**Base URL:** $BASE_URL

## Summary

### Puppeteer Results
\`\`\`
$(cat "$COMPARISON_DIR/puppeteer/summary.txt" 2>/dev/null || echo "No results available")
\`\`\`

### Playwright Results
\`\`\`
$(cat "$COMPARISON_DIR/playwright/summary.txt" 2>/dev/null || echo "No results available")
\`\`\`

## Detailed Logs

- Puppeteer output: [puppeteer/output.log](puppeteer/output.log)
- Playwright output: [playwright/output.log](playwright/output.log)
- Playwright JSON results: [playwright/results.json](playwright/results.json)

## Migration Notes

### Advantages observed with Playwright:
- Better error messages and debugging information
- Built-in test retry mechanism
- Native TypeScript support
- Better parallel execution
- More comprehensive reporting options

### Migration considerations:
- Update selectors to use Playwright's more robust locator strategies
- Leverage Playwright's auto-waiting features
- Use page fixtures for better test isolation
- Consider using Playwright's built-in assertions

## Next Steps

1. Review failed tests in both frameworks
2. Update Playwright tests to match Puppeteer functionality
3. Gradually migrate remaining Puppeteer tests
4. Remove Puppeteer dependencies once migration is complete

---
Generated on: $(date)
EOF

    echo -e "${GREEN}✅ Comparison report saved to: $report_file${NC}"
}

# Main execution
echo -e "${BLUE}🚀 Starting parallel test execution...${NC}"
echo -e "${BLUE}Results will be saved to: $COMPARISON_DIR${NC}\n"

# Store PIDs for parallel execution
PIDS=()

# Run Puppeteer tests in background if enabled
if [[ "$RUN_PUPPETEER" == "true" ]]; then
    run_puppeteer_tests &
    PIDS+=($!)
fi

# Run Playwright tests in background if enabled
if [[ "$RUN_PLAYWRIGHT" == "true" ]]; then
    run_playwright_tests &
    PIDS+=($!)
fi

# Wait for all tests to complete
PUPPETEER_EXIT=0
PLAYWRIGHT_EXIT=0

if [[ "$RUN_PUPPETEER" == "true" && "$RUN_PLAYWRIGHT" == "true" ]]; then
    wait ${PIDS[0]}
    PUPPETEER_EXIT=$?
    wait ${PIDS[1]}
    PLAYWRIGHT_EXIT=$?
elif [[ "$RUN_PUPPETEER" == "true" ]]; then
    wait ${PIDS[0]}
    PUPPETEER_EXIT=$?
elif [[ "$RUN_PLAYWRIGHT" == "true" ]]; then
    wait ${PIDS[0]}
    PLAYWRIGHT_EXIT=$?
fi

# Generate comparison report if enabled
if [[ "$COMPARE_RESULTS" == "true" ]]; then
    generate_comparison_report
fi

# Display final results
echo -e "\n${BLUE}📋 Final Results:${NC}"
if [[ "$RUN_PUPPETEER" == "true" ]]; then
    if [[ $PUPPETEER_EXIT -eq 0 ]]; then
        echo -e "${GREEN}✅ Puppeteer tests: PASSED${NC}"
    else
        echo -e "${RED}❌ Puppeteer tests: FAILED (exit code: $PUPPETEER_EXIT)${NC}"
    fi
fi

if [[ "$RUN_PLAYWRIGHT" == "true" ]]; then
    if [[ $PLAYWRIGHT_EXIT -eq 0 ]]; then
        echo -e "${GREEN}✅ Playwright tests: PASSED${NC}"
    else
        echo -e "${RED}❌ Playwright tests: FAILED (exit code: $PLAYWRIGHT_EXIT)${NC}"
    fi
fi

echo -e "\n${BLUE}📁 All results saved to: $COMPARISON_DIR${NC}"

# Open comparison report if tests were compared
if [[ "$COMPARE_RESULTS" == "true" && -f "$COMPARISON_DIR/comparison-report.md" ]]; then
    echo -e "${BLUE}📊 Comparison report: $COMPARISON_DIR/comparison-report.md${NC}"
    
    # Offer to open report
    read -p "Open comparison report? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if command -v xdg-open &> /dev/null; then
            xdg-open "$COMPARISON_DIR/comparison-report.md"
        elif command -v open &> /dev/null; then
            open "$COMPARISON_DIR/comparison-report.md"
        else
            echo "Please open: $COMPARISON_DIR/comparison-report.md"
        fi
    fi
fi

# Exit with failure if any tests failed
if [[ $PUPPETEER_EXIT -ne 0 || $PLAYWRIGHT_EXIT -ne 0 ]]; then
    exit 1
fi

exit 0