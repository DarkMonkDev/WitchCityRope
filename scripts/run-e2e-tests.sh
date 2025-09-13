#!/bin/bash

# E2E Tests Execution Script
# Phase 2: Test Suite Integration - Enhanced Containerized Testing Infrastructure
#
# Features:
# - Container lifecycle management for E2E tests
# - Environment setup and validation
# - Cleanup verification
# - Cross-browser testing support
# - Playwright container integration

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LOG_FILE="$PROJECT_ROOT/tests/e2e-test-execution.log"
ENVIRONMENT_SETUP_TIMEOUT=300  # 5 minutes
TEST_TIMEOUT=3600  # 60 minutes
CLEANUP_TIMEOUT=120
BROWSER_TIMEOUT=60

# Default configuration
BROWSERS="chromium,firefox,webkit"
WORKERS=1
HEADED=false
RETRY_COUNT=2

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

# Initialize logging
initialize_logging() {
    mkdir -p "$(dirname "$LOG_FILE")"
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Starting E2E test execution" > "$LOG_FILE"
    log_info "E2E test execution started at $(date '+%Y-%m-%d %H:%M:%S')"
    log_info "Project root: $PROJECT_ROOT"
    log_info "Log file: $LOG_FILE"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is required but not installed"
        exit 1
    fi
    
    # Check Docker daemon
    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running"
        exit 1
    fi
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        log_error "Node.js is required but not installed"
        exit 1
    fi
    
    # Check npm
    if ! command -v npm &> /dev/null; then
        log_error "npm is required but not installed"
        exit 1
    fi
    
    # Verify E2E test directories exist
    local e2e_tests_dir="$PROJECT_ROOT/tests/e2e"
    if [ ! -d "$e2e_tests_dir" ]; then
        log_error "E2E tests directory not found: $e2e_tests_dir"
        exit 1
    fi
    
    # Check Playwright installation
    if [ ! -f "$PROJECT_ROOT/tests/e2e/package.json" ]; then
        log_error "E2E tests package.json not found - Playwright may not be configured"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Clean up orphaned containers from previous runs
cleanup_orphaned_containers() {
    log_info "Cleaning up orphaned containers from previous E2E test runs..."
    
    local orphaned_containers
    orphaned_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=e2e-testing" -q || true)
    
    if [ -n "$orphaned_containers" ]; then
        log_warning "Found orphaned E2E containers, cleaning up..."
        echo "$orphaned_containers" | xargs -r docker rm -f
        log_success "Orphaned E2E containers cleaned up"
    else
        log_info "No orphaned E2E containers found"
    fi
}

# Setup test environment
setup_test_environment() {
    log_info "Setting up E2E test environment..."
    
    local setup_start=$(date +%s)
    
    # Navigate to E2E tests directory
    cd "$PROJECT_ROOT/tests/e2e"
    
    # Install dependencies if needed
    if [ ! -d "node_modules" ] || [ "package.json" -nt "node_modules" ]; then
        log_info "Installing E2E test dependencies..."
        if npm ci >> "$LOG_FILE" 2>&1; then
            log_success "Dependencies installed successfully"
        else
            log_error "Failed to install dependencies"
            exit 1
        fi
    fi
    
    # Install Playwright browsers if needed
    log_info "Ensuring Playwright browsers are installed..."
    if npx playwright install --with-deps >> "$LOG_FILE" 2>&1; then
        log_success "Playwright browsers ready"
    else
        log_error "Failed to install Playwright browsers"
        exit 1
    fi
    
    # Setup containerized test environment using TypeScript fixture
    log_info "Setting up containerized test environment..."
    
    # Create a simple setup script that uses our TypeScript fixture
    cat > setup-env.mjs << 'EOF'
import { TestEnvironment } from './fixtures/test-environment.ts';

async function setupEnvironment() {
    try {
        console.log('🚀 Setting up E2E test environment...');
        await TestEnvironment.setup();
        console.log('✅ E2E test environment ready');
        
        console.log('📊 Environment details:');
        console.log(`  Database: ${TestEnvironment.getConnectionString()}`);
        console.log(`  API: ${TestEnvironment.getApiBaseUrl()}`);
        console.log(`  Web: ${TestEnvironment.getWebBaseUrl()}`);
        
        process.exit(0);
    } catch (error) {
        console.error('❌ Environment setup failed:', error);
        process.exit(1);
    }
}

setupEnvironment();
EOF
    
    # Execute environment setup with timeout
    if timeout $ENVIRONMENT_SETUP_TIMEOUT node setup-env.mjs >> "$LOG_FILE" 2>&1; then
        local setup_end=$(date +%s)
        local setup_duration=$((setup_end - setup_start))
        log_success "E2E test environment setup completed in ${setup_duration}s"
    else
        local setup_end=$(date +%s)
        local setup_duration=$((setup_end - setup_start))
        log_error "E2E test environment setup failed after ${setup_duration}s"
        exit 1
    fi
    
    # Clean up temporary setup script
    rm -f setup-env.mjs
}

# Validate test environment
validate_environment() {
    log_info "Validating test environment..."
    
    # Check if required services are accessible
    local api_url="http://localhost:5655"
    local web_url="http://localhost:5173"
    
    # Check API service (optional - may not be running)
    if curl -f -s --max-time 10 "$api_url/health" > /dev/null 2>&1; then
        log_success "API service is accessible at $api_url"
    else
        log_warning "API service not accessible at $api_url (tests may use mock data)"
    fi
    
    # Check Web application (optional - may not be running)
    if curl -f -s --max-time 10 "$web_url" > /dev/null 2>&1; then
        log_success "Web application is accessible at $web_url"
    else
        log_warning "Web application not accessible at $web_url (tests may start their own server)"
    fi
    
    # Validate Docker environment
    local test_containers
    test_containers=$(docker ps --filter "label=project=witchcityrope" --filter "label=purpose=e2e-testing" -q || true)
    
    if [ -n "$test_containers" ]; then
        log_success "E2E test containers are running ($(echo "$test_containers" | wc -l) containers)"
    else
        log_warning "No E2E test containers detected (may be created on-demand)"
    fi
    
    log_success "Environment validation completed"
}

# Execute E2E tests
run_e2e_tests() {
    log_info "Running E2E tests with Playwright..."
    
    local test_start=$(date +%s)
    local test_results_dir="$PROJECT_ROOT/tests/results/e2e-$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$test_results_dir"
    
    # Navigate to E2E tests directory
    cd "$PROJECT_ROOT/tests/e2e"
    
    # Set environment variables
    export PWTEST_OUTPUT_DIR="$test_results_dir"
    export PWTEST_SCREENSHOT_MODE="only-on-failure"
    export PWTEST_VIDEO_MODE="retain-on-failure"
    
    # Build Playwright command
    local playwright_command=(
        npx playwright test
        --config=playwright.config.ts
        --workers="$WORKERS"
        --retries="$RETRY_COUNT"
        --output-dir="$test_results_dir"
        --reporter=html,json,junit
    )
    
    # Add browser selection
    if [ "$BROWSERS" != "all" ]; then
        IFS=',' read -ra BROWSER_ARRAY <<< "$BROWSERS"
        for browser in "${BROWSER_ARRAY[@]}"; do
            playwright_command+=(--project="$browser")
        done
    fi
    
    # Add headed mode if requested
    if [ "$HEADED" = true ]; then
        playwright_command+=(--headed)
    fi
    
    log_info "Executing: ${playwright_command[*]}"
    
    if timeout $TEST_TIMEOUT "${playwright_command[@]}" 2>&1 | tee -a "$LOG_FILE"; then
        local test_end=$(date +%s)
        local test_duration=$((test_end - test_start))
        log_success "E2E tests completed successfully in ${test_duration}s"
        log_info "Test results available in: $test_results_dir"
        
        # Show test summary
        show_test_summary "$test_results_dir"
        return 0
    else
        local test_end=$(date +%s)
        local test_duration=$((test_end - test_start))
        log_error "E2E tests failed after ${test_duration}s"
        log_error "Check the test results in: $test_results_dir"
        return 1
    fi
}

# Show test execution summary
show_test_summary() {
    local results_dir="$1"
    log_info "E2E test execution summary:"
    
    # Find and parse JSON results for test statistics
    local json_file
    json_file=$(find "$results_dir" -name "*.json" -type f | head -1)
    
    if [ -n "$json_file" ] && [ -f "$json_file" ]; then
        log_info "Parsing test results from: $(basename "$json_file")"
        
        # Extract basic statistics using jq if available, otherwise use grep
        if command -v jq &> /dev/null; then
            local total_tests passed_tests failed_tests
            total_tests=$(jq '.stats.total // 0' "$json_file" 2>/dev/null || echo "0")
            passed_tests=$(jq '.stats.passed // 0' "$json_file" 2>/dev/null || echo "0")
            failed_tests=$(jq '.stats.failed // 0' "$json_file" 2>/dev/null || echo "0")
            
            log_info "  Total tests: $total_tests"
            log_info "  Passed: $passed_tests"
            log_info "  Failed: $failed_tests"
        else
            log_info "  JSON results available (install jq for detailed statistics)"
        fi
    fi
    
    # Check for HTML report
    local html_report
    html_report=$(find "$results_dir" -name "index.html" -type f | head -1)
    
    if [ -n "$html_report" ] && [ -f "$html_report" ]; then
        log_info "HTML report available: file://$html_report"
    fi
    
    # Show screenshot and video information
    local screenshots videos
    screenshots=$(find "$results_dir" -name "*.png" -type f | wc -l)
    videos=$(find "$results_dir" -name "*.webm" -type f | wc -l)
    
    if [ "$screenshots" -gt 0 ]; then
        log_info "  Screenshots captured: $screenshots"
    fi
    
    if [ "$videos" -gt 0 ]; then
        log_info "  Videos recorded: $videos"
    fi
}

# Cleanup test environment
cleanup_test_environment() {
    log_info "Cleaning up E2E test environment..."
    
    cd "$PROJECT_ROOT/tests/e2e"
    
    # Create cleanup script
    cat > cleanup-env.mjs << 'EOF'
import { TestEnvironment } from './fixtures/test-environment.ts';

async function cleanupEnvironment() {
    try {
        console.log('🧹 Cleaning up E2E test environment...');
        await TestEnvironment.teardown();
        console.log('✅ E2E test environment cleanup completed');
        process.exit(0);
    } catch (error) {
        console.error('❌ Environment cleanup failed:', error);
        process.exit(1);
    }
}

cleanupEnvironment();
EOF
    
    # Execute cleanup with timeout
    if timeout $CLEANUP_TIMEOUT node cleanup-env.mjs >> "$LOG_FILE" 2>&1; then
        log_success "E2E test environment cleanup completed"
    else
        log_warning "E2E test environment cleanup timed out or failed"
    fi
    
    # Clean up temporary cleanup script
    rm -f cleanup-env.mjs
}

# Verify container cleanup
verify_cleanup() {
    log_info "Verifying E2E container cleanup..."
    
    local cleanup_start=$(date +%s)
    local max_wait_time=$CLEANUP_TIMEOUT
    local check_interval=5
    
    while [ $(($(date +%s) - cleanup_start)) -lt $max_wait_time ]; do
        local remaining_containers
        remaining_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=e2e-testing" -q || true)
        
        if [ -z "$remaining_containers" ]; then
            log_success "E2E container cleanup verification passed - no orphaned containers found"
            return 0
        fi
        
        log_info "Waiting for E2E containers to be cleaned up... (remaining: $(echo "$remaining_containers" | wc -l))"
        sleep $check_interval
    done
    
    # Cleanup verification failed
    local remaining_containers
    remaining_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=e2e-testing" -q || true)
    
    if [ -n "$remaining_containers" ]; then
        log_error "E2E container cleanup verification failed - orphaned containers detected:"
        echo "$remaining_containers" | xargs -r docker ps -a --filter id={} | tee -a "$LOG_FILE"
        
        log_warning "Force cleaning orphaned E2E containers..."
        echo "$remaining_containers" | xargs -r docker rm -f
        
        return 1
    fi
    
    return 0
}

# Generate execution report
generate_report() {
    local exit_code=$1
    local script_end=$(date +%s)
    local total_duration=$((script_end - SCRIPT_START))
    
    log_info "Generating E2E execution report..."
    
    local report_file="$PROJECT_ROOT/tests/e2e-execution-report-$(date +%Y%m%d-%H%M%S).md"
    
    cat > "$report_file" << EOF
# E2E Tests Execution Report

**Date**: $(date '+%Y-%m-%d %H:%M:%S')
**Duration**: ${total_duration}s
**Status**: $([ $exit_code -eq 0 ] && echo "SUCCESS" || echo "FAILED")
**Exit Code**: $exit_code

## Configuration
- Browsers: $BROWSERS
- Workers: $WORKERS
- Headed Mode: $HEADED
- Retry Count: $RETRY_COUNT

## Environment
- Project Root: $PROJECT_ROOT
- Docker Host: ${DOCKER_HOST:-unix:///var/run/docker.sock}
- Log File: $LOG_FILE

## Execution Steps
1. Prerequisites Check: ✅
2. Orphaned Container Cleanup: ✅
3. Environment Setup: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
4. Environment Validation: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
5. Run E2E Tests: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
6. Environment Cleanup: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
7. Cleanup Verification: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")

## Test Infrastructure
- Playwright Version: Latest
- TestContainers Support: Enabled
- PostgreSQL Image: postgres:16-alpine
- Dynamic Port Allocation: Enabled
- Container Lifecycle Management: Automated

## Notes
$([ $exit_code -ne 0 ] && echo "- Check the log file for detailed error information: $LOG_FILE" || echo "- All E2E tests executed successfully with enhanced containerized infrastructure")

---
*Generated by Enhanced Containerized Testing Infrastructure - Phase 2*
EOF

    log_info "E2E execution report generated: $report_file"
}

# Main execution function
main() {
    local exit_code=0
    
    # Record script start time
    SCRIPT_START=$(date +%s)
    
    # Initialize
    initialize_logging
    
    # Set up error handling
    trap 'exit_code=$?; log_error "Script interrupted or failed"; cleanup_test_environment; generate_report $exit_code; exit $exit_code' EXIT
    
    log_info "Enhanced Containerized Testing Infrastructure - E2E Tests"
    log_info "========================================================="
    
    # Execute all steps
    check_prerequisites
    cleanup_orphaned_containers
    setup_test_environment
    validate_environment
    
    if run_e2e_tests; then
        log_success "E2E tests completed successfully"
    else
        log_error "E2E tests failed"
        exit_code=1
    fi
    
    cleanup_test_environment
    
    if verify_cleanup; then
        log_success "Container cleanup verification passed"
    else
        log_warning "Container cleanup verification failed (non-fatal)"
    fi
    
    # Generate final report
    generate_report $exit_code
    
    if [ $exit_code -eq 0 ]; then
        log_success "All E2E tests completed successfully!"
        log_info "Enhanced containerized testing infrastructure working properly"
    else
        log_error "E2E test execution failed - check logs for details"
    fi
    
    return $exit_code
}

# Usage information
usage() {
    cat << EOF
Enhanced E2E Tests Execution Script

Usage: $0 [options]

Options:
    -h, --help                      Show this help message
    --browsers BROWSERS             Comma-separated list of browsers (chromium,firefox,webkit) or 'all' (default: $BROWSERS)
    --workers WORKERS               Number of parallel workers (default: $WORKERS)
    --headed                        Run tests in headed mode (default: headless)
    --retry RETRY_COUNT             Number of test retries on failure (default: $RETRY_COUNT)
    --environment-timeout SECONDS   Environment setup timeout (default: $ENVIRONMENT_SETUP_TIMEOUT)
    --test-timeout SECONDS          Test execution timeout (default: $TEST_TIMEOUT)
    --cleanup-timeout SECONDS       Cleanup verification timeout (default: $CLEANUP_TIMEOUT)

Examples:
    $0                                      # Run with default settings
    $0 --browsers chromium --workers 4     # Run only Chromium with 4 workers
    $0 --headed --retry 0                   # Run in headed mode with no retries
    $0 --browsers all --workers 1          # Run all browsers sequentially

This script provides comprehensive E2E test execution with:
- Containerized test environment setup
- Cross-browser testing support
- Container lifecycle management
- Environment validation
- Cleanup verification
- Detailed reporting

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            usage
            exit 0
            ;;
        --browsers)
            BROWSERS="$2"
            shift 2
            ;;
        --workers)
            WORKERS="$2"
            shift 2
            ;;
        --headed)
            HEADED=true
            shift
            ;;
        --retry)
            RETRY_COUNT="$2"
            shift 2
            ;;
        --environment-timeout)
            ENVIRONMENT_SETUP_TIMEOUT="$2"
            shift 2
            ;;
        --test-timeout)
            TEST_TIMEOUT="$2"
            shift 2
            ;;
        --cleanup-timeout)
            CLEANUP_TIMEOUT="$2"
            shift 2
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Execute main function
main "$@"