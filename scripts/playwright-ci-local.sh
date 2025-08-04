#!/bin/bash

# Playwright CI Environment Simulator
# Simulates CI environment locally for testing

set -e

# Configuration
CI_RESULTS_DIR="test-results/ci-simulation"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_DIR="$CI_RESULTS_DIR/$TIMESTAMP"
BASE_URL="http://localhost:5651"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -s, --suite <name>       Run specific test suite (auth, events, admin, all)"
    echo "  -b, --browser <name>     Browser to use (chromium, firefox, webkit, all)"
    echo "  --skip-deps              Skip dependency installation"
    echo "  --skip-build             Skip application build"
    echo "  --keep-artifacts         Keep all test artifacts (screenshots, videos, traces)"
    echo "  --parallel <num>         Number of parallel workers (default: 1 for CI)"
    echo "  --docker                 Run tests against Docker environment"
    echo "  --help                   Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0                              # Run all tests in CI mode"
    echo "  $0 -s auth -b firefox           # Run auth tests in Firefox"
    echo "  $0 --docker --parallel 4        # Run in Docker with 4 workers"
    exit 1
}

# Default values
TEST_SUITE=""
BROWSER="chromium"
SKIP_DEPS="false"
SKIP_BUILD="false"
KEEP_ARTIFACTS="false"
PARALLEL_WORKERS="1"
USE_DOCKER="false"

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
        --skip-deps)
            SKIP_DEPS="true"
            shift
            ;;
        --skip-build)
            SKIP_BUILD="true"
            shift
            ;;
        --keep-artifacts)
            KEEP_ARTIFACTS="true"
            shift
            ;;
        --parallel)
            PARALLEL_WORKERS="$2"
            shift 2
            ;;
        --docker)
            USE_DOCKER="true"
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

# Function to print section header
print_section() {
    echo -e "\n${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${PURPLE}  $1${NC}"
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
}

# Function to check system requirements
check_requirements() {
    print_section "System Requirements Check"
    
    local all_good=true
    
    # Check Node.js
    if command -v node &> /dev/null; then
        local node_version=$(node --version)
        echo -e "${GREEN}✓${NC} Node.js: $node_version"
    else
        echo -e "${RED}✗${NC} Node.js: Not installed"
        all_good=false
    fi
    
    # Check npm
    if command -v npm &> /dev/null; then
        local npm_version=$(npm --version)
        echo -e "${GREEN}✓${NC} npm: $npm_version"
    else
        echo -e "${RED}✗${NC} npm: Not installed"
        all_good=false
    fi
    
    # Check Docker if needed
    if [[ "$USE_DOCKER" == "true" ]]; then
        if command -v docker &> /dev/null; then
            local docker_version=$(docker --version | cut -d' ' -f3 | sed 's/,$//')
            echo -e "${GREEN}✓${NC} Docker: $docker_version"
        else
            echo -e "${RED}✗${NC} Docker: Not installed"
            all_good=false
        fi
    fi
    
    # Check available memory
    if command -v free &> /dev/null; then
        local mem_available=$(free -m | awk 'NR==2{printf "%.1f", $7/1024}')
        if (( $(echo "$mem_available > 2" | bc -l) )); then
            echo -e "${GREEN}✓${NC} Available memory: ${mem_available}GB"
        else
            echo -e "${YELLOW}⚠${NC} Available memory: ${mem_available}GB (recommended: >2GB)"
        fi
    fi
    
    if [[ "$all_good" == "false" ]]; then
        echo -e "\n${RED}❌ System requirements not met. Please install missing dependencies.${NC}"
        exit 1
    fi
}

# Function to set up CI environment
setup_ci_environment() {
    print_section "CI Environment Setup"
    
    # Set CI environment variables
    export CI=true
    export FORCE_COLOR=0
    export NODE_ENV=test
    export PWTEST_SKIP_TEST_OUTPUT=1
    export BASE_URL="$BASE_URL"
    
    # Create results directory structure
    mkdir -p "$RESULTS_DIR"/{screenshots,videos,traces,reports}
    
    echo -e "${GREEN}✓${NC} CI environment variables set"
    echo -e "${GREEN}✓${NC} Results directory created: $RESULTS_DIR"
    
    # Save environment info
    cat > "$RESULTS_DIR/environment.json" << EOF
{
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "node_version": "$(node --version)",
  "npm_version": "$(npm --version)",
  "os": "$(uname -s)",
  "os_version": "$(uname -r)",
  "ci_mode": true,
  "base_url": "$BASE_URL",
  "browser": "$BROWSER",
  "workers": $PARALLEL_WORKERS,
  "test_suite": "${TEST_SUITE:-all}"
}
EOF
}

# Function to install dependencies
install_dependencies() {
    if [[ "$SKIP_DEPS" == "true" ]]; then
        echo -e "${YELLOW}⚠${NC} Skipping dependency installation"
        return
    fi
    
    print_section "Installing Dependencies"
    
    # Install npm dependencies
    echo -e "${BLUE}Installing npm packages...${NC}"
    npm ci --prefer-offline --no-audit
    
    # Install Playwright browsers
    echo -e "${BLUE}Installing Playwright browsers...${NC}"
    npx playwright install --with-deps $BROWSER
    
    echo -e "${GREEN}✓${NC} Dependencies installed successfully"
}

# Function to build application
build_application() {
    if [[ "$SKIP_BUILD" == "true" || "$USE_DOCKER" == "true" ]]; then
        echo -e "${YELLOW}⚠${NC} Skipping application build"
        return
    fi
    
    print_section "Building Application"
    
    # Run build command (adjust based on your project)
    echo -e "${BLUE}Building application...${NC}"
    # npm run build
    echo -e "${YELLOW}⚠${NC} Build step not implemented - assuming pre-built"
}

# Function to start application
start_application() {
    print_section "Starting Application"
    
    if [[ "$USE_DOCKER" == "true" ]]; then
        echo -e "${BLUE}Starting Docker environment...${NC}"
        docker-compose up -d
        
        # Wait for application to be ready
        echo -e "${BLUE}Waiting for application to be ready...${NC}"
        local retries=30
        while [ $retries -gt 0 ]; do
            if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|301\|302"; then
                echo -e "${GREEN}✓${NC} Application is ready"
                return
            fi
            retries=$((retries - 1))
            sleep 2
        done
        
        echo -e "${RED}✗${NC} Application failed to start"
        exit 1
    else
        # Check if application is already running
        if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|301\|302"; then
            echo -e "${GREEN}✓${NC} Application is already running"
        else
            echo -e "${RED}✗${NC} Application is not running. Please start it manually or use --docker"
            exit 1
        fi
    fi
}

# Function to run tests
run_tests() {
    print_section "Running Tests"
    
    # Build Playwright command
    local PLAYWRIGHT_CMD="npx playwright test"
    
    # Add test pattern based on suite
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
    
    # Add browser selection
    if [[ "$BROWSER" != "all" ]]; then
        PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --project=$BROWSER"
    fi
    
    # Add CI-specific options
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --workers=$PARALLEL_WORKERS"
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --retries=2"
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --reporter=list,json,junit,html"
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --output=$RESULTS_DIR"
    
    # Configure artifact collection
    export PLAYWRIGHT_JSON_OUTPUT_NAME="$RESULTS_DIR/reports/results.json"
    export PLAYWRIGHT_JUNIT_OUTPUT_NAME="$RESULTS_DIR/reports/results.xml"
    export PLAYWRIGHT_HTML_REPORT="$RESULTS_DIR/reports/html"
    
    echo -e "${BLUE}Test command:${NC} $PLAYWRIGHT_CMD"
    echo -e "${BLUE}Starting test execution...${NC}\n"
    
    # Run tests and capture output
    set +e
    $PLAYWRIGHT_CMD 2>&1 | tee "$RESULTS_DIR/test-output.log"
    local TEST_EXIT_CODE=$?
    set -e
    
    return $TEST_EXIT_CODE
}

# Function to collect artifacts
collect_artifacts() {
    print_section "Collecting Artifacts"
    
    # Move screenshots, videos, and traces
    if [[ -d "test-results" ]]; then
        find test-results -name "*.png" -exec cp {} "$RESULTS_DIR/screenshots/" \; 2>/dev/null || true
        find test-results -name "*.webm" -exec cp {} "$RESULTS_DIR/videos/" \; 2>/dev/null || true
        find test-results -name "trace.zip" -exec cp {} "$RESULTS_DIR/traces/" \; 2>/dev/null || true
    fi
    
    # Generate summary report
    cat > "$RESULTS_DIR/summary.md" << EOF
# CI Test Run Summary

**Date:** $(date)
**Duration:** $SECONDS seconds
**Exit Code:** $1

## Configuration
- Test Suite: ${TEST_SUITE:-all}
- Browser: $BROWSER
- Workers: $PARALLEL_WORKERS
- Base URL: $BASE_URL

## Results
- Full log: [test-output.log](test-output.log)
- JSON report: [reports/results.json](reports/results.json)
- JUnit XML: [reports/results.xml](reports/results.xml)
- HTML report: [reports/html/index.html](reports/html/index.html)

## Artifacts
- Screenshots: $(find "$RESULTS_DIR/screenshots" -name "*.png" 2>/dev/null | wc -l) files
- Videos: $(find "$RESULTS_DIR/videos" -name "*.webm" 2>/dev/null | wc -l) files
- Traces: $(find "$RESULTS_DIR/traces" -name "*.zip" 2>/dev/null | wc -l) files
EOF
    
    echo -e "${GREEN}✓${NC} Artifacts collected in: $RESULTS_DIR"
    
    # Clean up temporary files unless keeping artifacts
    if [[ "$KEEP_ARTIFACTS" != "true" ]]; then
        echo -e "${BLUE}Cleaning up temporary files...${NC}"
        rm -rf test-results/
        rm -rf playwright-report/
    fi
}

# Function to stop services
cleanup() {
    if [[ "$USE_DOCKER" == "true" ]]; then
        print_section "Cleanup"
        echo -e "${BLUE}Stopping Docker services...${NC}"
        docker-compose down
    fi
}

# Main execution
echo -e "${PURPLE}🚀 Playwright CI Environment Simulator${NC}"
echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Start timer
SECONDS=0

# Set up trap for cleanup
trap cleanup EXIT

# Execute CI pipeline steps
check_requirements
setup_ci_environment
install_dependencies
build_application
start_application

# Run tests
TEST_EXIT_CODE=0
run_tests || TEST_EXIT_CODE=$?

# Collect artifacts
collect_artifacts $TEST_EXIT_CODE

# Final report
print_section "Final Report"

if [[ $TEST_EXIT_CODE -eq 0 ]]; then
    echo -e "${GREEN}✅ All tests passed!${NC}"
else
    echo -e "${RED}❌ Tests failed with exit code: $TEST_EXIT_CODE${NC}"
fi

echo -e "\n${BLUE}📊 Full results available at: $RESULTS_DIR${NC}"
echo -e "${BLUE}⏱️  Total duration: $SECONDS seconds${NC}"

# Offer to open HTML report
if [[ -d "$RESULTS_DIR/reports/html" ]]; then
    echo ""
    read -p "Open HTML report? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        npx playwright show-report "$RESULTS_DIR/reports/html"
    fi
fi

exit $TEST_EXIT_CODE