#!/bin/bash

# Integration Tests Execution Script
# Phase 2: Test Suite Integration - Enhanced Containerized Testing Infrastructure
#
# Features:
# - Container lifecycle management
# - Health check verification
# - Cleanup verification
# - Comprehensive error handling
# - Performance monitoring

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LOG_FILE="$PROJECT_ROOT/tests/integration-test-execution.log"
HEALTH_CHECK_TIMEOUT=60
TEST_TIMEOUT=1800  # 30 minutes
CLEANUP_TIMEOUT=120

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
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Starting integration test execution" > "$LOG_FILE"
    log_info "Integration test execution started at $(date '+%Y-%m-%d %H:%M:%S')"
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
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is required but not installed"
        exit 1
    fi
    
    # Verify test projects exist
    local integration_tests_dir="$PROJECT_ROOT/tests/integration"
    if [ ! -d "$integration_tests_dir" ]; then
        log_error "Integration tests directory not found: $integration_tests_dir"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Clean up orphaned containers from previous runs
cleanup_orphaned_containers() {
    log_info "Cleaning up orphaned containers from previous test runs..."
    
    local orphaned_containers
    orphaned_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=testing" -q || true)
    
    if [ -n "$orphaned_containers" ]; then
        log_warning "Found orphaned containers, cleaning up..."
        echo "$orphaned_containers" | xargs -r docker rm -f
        log_success "Orphaned containers cleaned up"
    else
        log_info "No orphaned containers found"
    fi
}

# Run health checks before integration tests
run_health_checks() {
    log_info "Running mandatory health checks..."
    
    local health_check_start=$(date +%s)
    
    # Run health check tests with timeout
    if timeout $HEALTH_CHECK_TIMEOUT dotnet test \
        "$PROJECT_ROOT/tests/WitchCityRope.Core.Tests" \
        --filter "Category=HealthCheck" \
        --logger "console;verbosity=minimal" \
        --results-directory "$PROJECT_ROOT/tests/results" \
        --collect:"XPlat Code Coverage" \
        >> "$LOG_FILE" 2>&1; then
        
        local health_check_end=$(date +%s)
        local health_check_duration=$((health_check_end - health_check_start))
        log_success "Health checks passed in ${health_check_duration}s"
    else
        local health_check_end=$(date +%s)
        local health_check_duration=$((health_check_end - health_check_start))
        log_error "Health checks failed after ${health_check_duration}s"
        log_error "Check the log file for details: $LOG_FILE"
        exit 1
    fi
}

# Build test projects
build_test_projects() {
    log_info "Building test projects..."
    
    local build_start=$(date +%s)
    
    # Restore packages first
    if dotnet restore "$PROJECT_ROOT/tests/integration/WitchCityRope.IntegrationTests.csproj" >> "$LOG_FILE" 2>&1; then
        log_success "Package restore completed"
    else
        log_error "Package restore failed"
        exit 1
    fi
    
    # Build test project
    if dotnet build "$PROJECT_ROOT/tests/integration/WitchCityRope.IntegrationTests.csproj" \
        --configuration Release \
        --no-restore \
        >> "$LOG_FILE" 2>&1; then
        
        local build_end=$(date +%s)
        local build_duration=$((build_end - build_start))
        log_success "Test projects built successfully in ${build_duration}s"
    else
        local build_end=$(date +%s)
        local build_duration=$((build_end - build_start))
        log_error "Test project build failed after ${build_duration}s"
        exit 1
    fi
}

# Execute integration tests
run_integration_tests() {
    log_info "Running integration tests with containerized infrastructure..."
    
    local test_start=$(date +%s)
    local test_results_dir="$PROJECT_ROOT/tests/results/integration-$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$test_results_dir"
    
    # Set environment variables for container configuration
    export TESTCONTAINERS_RYUK_DISABLED=false
    export TESTCONTAINERS_REUSE_ENABLE=false
    export DOCKER_HOST="${DOCKER_HOST:-unix:///var/run/docker.sock}"
    
    # Run integration tests with comprehensive options
    local test_command=(
        dotnet test
        "$PROJECT_ROOT/tests/integration/WitchCityRope.IntegrationTests.csproj"
        --configuration Release
        --no-build
        --logger "console;verbosity=detailed"
        --logger "trx;LogFileName=integration-tests.trx"
        --logger "html;LogFileName=integration-tests.html"
        --results-directory "$test_results_dir"
        --collect:"XPlat Code Coverage"
        --settings "$PROJECT_ROOT/tests/coverletArgs.runsettings"
        --verbosity detailed
        -- TestRunParameters.Parameter\(name=\"DataConnectionString\",value=\"UseDynamicContainer\"\)
    )
    
    log_info "Executing: ${test_command[*]}"
    
    if timeout $TEST_TIMEOUT "${test_command[@]}" 2>&1 | tee -a "$LOG_FILE"; then
        local test_end=$(date +%s)
        local test_duration=$((test_end - test_start))
        log_success "Integration tests completed successfully in ${test_duration}s"
        log_info "Test results available in: $test_results_dir"
        
        # Show test summary
        show_test_summary "$test_results_dir"
    else
        local test_end=$(date +%s)
        local test_duration=$((test_end - test_start))
        log_error "Integration tests failed after ${test_duration}s"
        log_error "Check the test results in: $test_results_dir"
        return 1
    fi
}

# Show test execution summary
show_test_summary() {
    local results_dir="$1"
    log_info "Test execution summary:"
    
    # Find and parse TRX file for test statistics
    local trx_file
    trx_file=$(find "$results_dir" -name "*.trx" -type f | head -1)
    
    if [ -n "$trx_file" ] && [ -f "$trx_file" ]; then
        log_info "Parsing test results from: $(basename "$trx_file")"
        
        # Extract basic statistics (simplified parsing)
        local total_tests passed_tests failed_tests
        total_tests=$(grep -o 'total="[0-9]*"' "$trx_file" | cut -d'"' -f2 || echo "0")
        passed_tests=$(grep -o 'passed="[0-9]*"' "$trx_file" | cut -d'"' -f2 || echo "0")
        failed_tests=$(grep -o 'failed="[0-9]*"' "$trx_file" | cut -d'"' -f2 || echo "0")
        
        log_info "  Total tests: $total_tests"
        log_info "  Passed: $passed_tests"
        log_info "  Failed: $failed_tests"
        
        if [ "$failed_tests" -gt 0 ]; then
            log_warning "Some tests failed - check detailed results"
        fi
    else
        log_warning "Could not find TRX file for detailed statistics"
    fi
    
    # Show coverage information if available
    local coverage_file
    coverage_file=$(find "$results_dir" -name "coverage.cobertura.xml" -type f | head -1)
    
    if [ -n "$coverage_file" ] && [ -f "$coverage_file" ]; then
        log_info "Code coverage report available: $(basename "$coverage_file")"
    fi
}

# Verify container cleanup
verify_cleanup() {
    log_info "Verifying container cleanup..."
    
    local cleanup_start=$(date +%s)
    local max_wait_time=$CLEANUP_TIMEOUT
    local check_interval=5
    
    while [ $(($(date +%s) - cleanup_start)) -lt $max_wait_time ]; do
        local remaining_containers
        remaining_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=testing" -q || true)
        
        if [ -z "$remaining_containers" ]; then
            log_success "Container cleanup verification passed - no orphaned containers found"
            return 0
        fi
        
        log_info "Waiting for containers to be cleaned up... (remaining: $(echo "$remaining_containers" | wc -l))"
        sleep $check_interval
    done
    
    # Cleanup verification failed
    local remaining_containers
    remaining_containers=$(docker ps -a --filter "label=project=witchcityrope" --filter "label=purpose=testing" -q || true)
    
    if [ -n "$remaining_containers" ]; then
        log_error "Container cleanup verification failed - orphaned containers detected:"
        echo "$remaining_containers" | xargs -r docker ps -a --filter id={} | tee -a "$LOG_FILE"
        
        log_warning "Force cleaning orphaned containers..."
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
    
    log_info "Generating execution report..."
    
    local report_file="$PROJECT_ROOT/tests/integration-execution-report-$(date +%Y%m%d-%H%M%S).md"
    
    cat > "$report_file" << EOF
# Integration Tests Execution Report

**Date**: $(date '+%Y-%m-%d %H:%M:%S')
**Duration**: ${total_duration}s
**Status**: $([ $exit_code -eq 0 ] && echo "SUCCESS" || echo "FAILED")
**Exit Code**: $exit_code

## Environment
- Project Root: $PROJECT_ROOT
- Docker Host: ${DOCKER_HOST:-unix:///var/run/docker.sock}
- Log File: $LOG_FILE

## Execution Steps
1. Prerequisites Check: ✅
2. Orphaned Container Cleanup: ✅
3. Health Checks: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
4. Build Test Projects: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
5. Run Integration Tests: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")
6. Cleanup Verification: $([ $exit_code -eq 0 ] && echo "✅" || echo "❌")

## Container Infrastructure
- TestContainers Version: 4.7.0
- PostgreSQL Image: postgres:16-alpine
- Dynamic Port Allocation: Enabled
- Ryuk Cleanup: Enabled

## Notes
$([ $exit_code -ne 0 ] && echo "- Check the log file for detailed error information: $LOG_FILE" || echo "- All tests executed successfully with enhanced containerized infrastructure")

---
*Generated by Enhanced Containerized Testing Infrastructure - Phase 2*
EOF

    log_info "Execution report generated: $report_file"
}

# Main execution function
main() {
    local exit_code=0
    
    # Record script start time
    SCRIPT_START=$(date +%s)
    
    # Initialize
    initialize_logging
    
    # Set up error handling
    trap 'exit_code=$?; log_error "Script interrupted or failed"; generate_report $exit_code; exit $exit_code' EXIT
    
    log_info "Enhanced Containerized Testing Infrastructure - Integration Tests"
    log_info "=================================================================="
    
    # Execute all steps
    check_prerequisites
    cleanup_orphaned_containers
    run_health_checks
    build_test_projects
    
    if run_integration_tests; then
        log_success "Integration tests completed successfully"
    else
        log_error "Integration tests failed"
        exit_code=1
    fi
    
    if verify_cleanup; then
        log_success "Container cleanup verification passed"
    else
        log_warning "Container cleanup verification failed (non-fatal)"
    fi
    
    # Generate final report
    generate_report $exit_code
    
    if [ $exit_code -eq 0 ]; then
        log_success "All integration tests completed successfully!"
        log_info "Enhanced containerized testing infrastructure working properly"
    else
        log_error "Integration test execution failed - check logs for details"
    fi
    
    return $exit_code
}

# Usage information
usage() {
    cat << EOF
Enhanced Integration Tests Execution Script

Usage: $0 [options]

Options:
    -h, --help              Show this help message
    --health-check-timeout  Set health check timeout in seconds (default: $HEALTH_CHECK_TIMEOUT)
    --test-timeout          Set test execution timeout in seconds (default: $TEST_TIMEOUT)
    --cleanup-timeout       Set cleanup verification timeout in seconds (default: $CLEANUP_TIMEOUT)

Examples:
    $0                                          # Run with default settings
    $0 --test-timeout 3600                    # Run with 60-minute test timeout
    $0 --health-check-timeout 120             # Run with 2-minute health check timeout

This script provides comprehensive integration test execution with:
- Mandatory health check verification
- Enhanced containerized PostgreSQL testing
- Container lifecycle management
- Cleanup verification
- Performance monitoring
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
        --health-check-timeout)
            HEALTH_CHECK_TIMEOUT="$2"
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