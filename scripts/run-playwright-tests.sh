#!/bin/bash

# Playwright Test Runner for WitchCityRope
# Main test runner with options for specific suites, browsers, and modes

set -e

# Default values
BROWSER="chromium"
HEADED="false"
TEST_SUITE=""
REPORT="true"
BASE_URL="http://localhost:5651"
WORKERS=""
RETRIES=""

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -s, --suite <name>       Run specific test suite (auth, events, admin, all)"
    echo "  -b, --browser <name>     Browser to use (chromium, firefox, webkit, all)"
    echo "  -h, --headed             Run in headed mode (show browser)"
    echo "  -u, --url <url>          Base URL (default: http://localhost:5651)"
    echo "  -w, --workers <num>      Number of parallel workers"
    echo "  -r, --retries <num>      Number of retries for failed tests"
    echo "  --no-report              Disable HTML report generation"
    echo "  --help                   Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0 -s auth -h                    # Run auth tests in headed mode"
    echo "  $0 -s events -b firefox          # Run events tests in Firefox"
    echo "  $0 -b all --workers 4            # Run all tests in all browsers with 4 workers"
    echo "  $0 -s admin --no-report          # Run admin tests without report"
    exit 1
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--suite)
            TEST_SUITE="$2"
            shift 2
            ;;
        -b|--browser)
            BROWSER="$2"
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
        -w|--workers)
            WORKERS="$2"
            shift 2
            ;;
        -r|--retries)
            RETRIES="$2"
            shift 2
            ;;
        --no-report)
            REPORT="false"
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
echo "Checking if application is running at $BASE_URL..."
if ! curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|301\|302"; then
    echo "❌ Error: Application is not running at $BASE_URL"
    echo "Please start the application first using:"
    echo "  docker-compose up -d"
    echo "  # OR"
    echo "  ./docker-dev.sh"
    exit 1
fi
echo "✅ Application is running"

# Determine test pattern based on suite
case $TEST_SUITE in
    auth)
        TEST_PATTERN="**/auth/**/*.spec.ts **/login/**/*.spec.ts **/authentication/**/*.spec.ts"
        echo "🔐 Running authentication tests..."
        ;;
    events)
        TEST_PATTERN="**/events/**/*.spec.ts **/event-*.spec.ts"
        echo "📅 Running events tests..."
        ;;
    admin)
        TEST_PATTERN="**/admin/**/*.spec.ts **/admin-*.spec.ts"
        echo "👤 Running admin tests..."
        ;;
    all|"")
        TEST_PATTERN=""
        echo "🚀 Running all tests..."
        ;;
    *)
        echo "❌ Unknown test suite: $TEST_SUITE"
        echo "Valid suites: auth, events, admin, all"
        exit 1
        ;;
esac

# Build Playwright command
PLAYWRIGHT_CMD="npx playwright test"

# Add test pattern if specified
if [[ -n "$TEST_PATTERN" ]]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD $TEST_PATTERN"
fi

# Add browser selection
if [[ "$BROWSER" != "all" ]]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --project=$BROWSER"
fi

# Add headed mode
if [[ "$HEADED" == "true" ]]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --headed"
    # Reduce workers for headed mode to avoid too many windows
    if [[ -z "$WORKERS" ]]; then
        WORKERS="1"
    fi
fi

# Add workers if specified
if [[ -n "$WORKERS" ]]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --workers=$WORKERS"
fi

# Add retries if specified
if [[ -n "$RETRIES" ]]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --retries=$RETRIES"
fi

# Set environment variables
export BASE_URL="$BASE_URL"
export PWTEST_SKIP_TEST_OUTPUT=1  # Cleaner output

# Check if Playwright is installed
if ! command -v npx &> /dev/null || ! npx playwright --version &> /dev/null 2>&1; then
    echo "📦 Installing Playwright..."
    npm install --save-dev @playwright/test
    npx playwright install --with-deps
fi

# Create directories for test results
mkdir -p test-results
mkdir -p playwright-report

# Display test configuration
echo ""
echo "📋 Test Configuration:"
echo "  Suite: ${TEST_SUITE:-all}"
echo "  Browser: $BROWSER"
echo "  Mode: $([ "$HEADED" == "true" ] && echo "headed" || echo "headless")"
echo "  Base URL: $BASE_URL"
if [[ -n "$WORKERS" ]]; then
    echo "  Workers: $WORKERS"
fi
if [[ -n "$RETRIES" ]]; then
    echo "  Retries: $RETRIES"
fi
echo ""

# Run tests
echo "🧪 Running tests..."
echo "Command: $PLAYWRIGHT_CMD"
echo ""

# Execute tests and capture exit code
set +e
$PLAYWRIGHT_CMD
TEST_EXIT_CODE=$?
set -e

# Generate and open report if enabled and tests were run
if [[ "$REPORT" == "true" && -d "playwright-report" ]]; then
    echo ""
    echo "📊 Test report generated at: playwright-report/index.html"
    
    # Offer to open report
    if [[ "$TEST_EXIT_CODE" -ne 0 ]]; then
        echo ""
        read -p "❌ Tests failed. Open HTML report? (y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            npx playwright show-report
        fi
    else
        echo "✅ All tests passed!"
        echo ""
        read -p "Open HTML report? (y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            npx playwright show-report
        fi
    fi
fi

# Exit with test exit code
exit $TEST_EXIT_CODE