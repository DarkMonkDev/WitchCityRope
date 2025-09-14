#!/bin/bash

# PayPal Integration Test Runner
# Runs PayPal tests incrementally to identify failures quickly
# Usage: ./scripts/test/run-paypal-integration-tests.sh [stage] [--verbose] [--mock-only] [--real-only]

set -e

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TEST_PROJECT="$PROJECT_ROOT/tests/WitchCityRope.Infrastructure.Tests"
LOG_FILE="$PROJECT_ROOT/test-results/paypal-integration-test-results.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Flags
VERBOSE=false
MOCK_ONLY=false
REAL_ONLY=false
STAGE=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --mock-only)
            MOCK_ONLY=true
            shift
            ;;
        --real-only)
            REAL_ONLY=true
            shift
            ;;
        --help|-h)
            echo "PayPal Integration Test Runner"
            echo "Usage: $0 [stage] [options]"
            echo ""
            echo "Stages:"
            echo "  health       - Configuration and health checks only"
            echo "  mock         - Mock service tests only"
            echo "  sandbox      - Real PayPal sandbox tests only"
            echo "  webhooks     - Webhook endpoint tests only"
            echo "  cicd         - CI/CD integration tests only"
            echo "  all          - All tests (default)"
            echo ""
            echo "Options:"
            echo "  --verbose    - Enable detailed logging"
            echo "  --mock-only  - Only run tests that use mock service"
            echo "  --real-only  - Only run tests that use real PayPal sandbox"
            echo "  --help       - Show this help message"
            exit 0
            ;;
        *)
            STAGE="$1"
            shift
            ;;
    esac
done

# Default stage
if [[ -z "$STAGE" ]]; then
    STAGE="all"
fi

# Create results directory
mkdir -p "$(dirname "$LOG_FILE")"

# Functions
log() {
    local level=$1
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    case $level in
        INFO)
            echo -e "${GREEN}[INFO]${NC} $message"
            ;;
        WARN)
            echo -e "${YELLOW}[WARN]${NC} $message"
            ;;
        ERROR)
            echo -e "${RED}[ERROR]${NC} $message"
            ;;
        DEBUG)
            if [[ "$VERBOSE" == "true" ]]; then
                echo -e "${BLUE}[DEBUG]${NC} $message"
            fi
            ;;
    esac
    
    echo "[$timestamp] [$level] $message" >> "$LOG_FILE"
}

check_prerequisites() {
    log INFO "Checking prerequisites..."
    
    # Check dotnet
    if ! command -v dotnet &> /dev/null; then
        log ERROR "dotnet CLI not found. Please install .NET 9.0"
        exit 1
    fi
    
    # Check project exists
    if [[ ! -d "$TEST_PROJECT" ]]; then
        log ERROR "Test project not found: $TEST_PROJECT"
        exit 1
    fi
    
    # Check environment
    if [[ "$REAL_ONLY" == "true" ]]; then
        if [[ -z "$PAYPAL_CLIENT_ID" || -z "$PAYPAL_CLIENT_SECRET" ]]; then
            log WARN "Real PayPal credentials not found in environment"
            log WARN "Set PAYPAL_CLIENT_ID and PAYPAL_CLIENT_SECRET to run real sandbox tests"
        fi
    fi
    
    log INFO "Prerequisites check completed"
}

run_test_category() {
    local category=$1
    local description=$2
    local filter=$3
    
    log INFO "Running $description..."
    
    local test_command="dotnet test \"$TEST_PROJECT\" --filter \"$filter\" --logger \"console;verbosity=normal\" --no-build"
    
    if [[ "$VERBOSE" == "true" ]]; then
        test_command="$test_command --logger \"console;verbosity=detailed\""
    fi
    
    log DEBUG "Test command: $test_command"
    
    if eval $test_command; then
        log INFO "✅ $description - PASSED"
        return 0
    else
        log ERROR "❌ $description - FAILED"
        return 1
    fi
}

run_health_checks() {
    log INFO "🏥 Running PayPal Health Checks..."
    
    # Configuration tests
    run_test_category "config" "Configuration Tests" \
        "Component=Configuration&Priority=Critical"
    
    # Basic service creation
    run_test_category "health" "Service Health Tests" \
        "Stage=Health|Stage=ServiceCreation"
}

run_mock_tests() {
    log INFO "🤖 Running Mock Service Tests..."
    
    # Set environment for mock tests
    export USE_MOCK_PAYMENT_SERVICE=true
    
    # Mock service tests
    run_test_category "mock" "Mock PayPal Service Tests" \
        "Service=Mock&Category=Integration"
    
    # CI/CD tests (which should use mock)
    run_test_category "cicd-mock" "CI/CD Integration Tests (Mock)" \
        "Component=CI/CD&Priority=Critical"
}

run_sandbox_tests() {
    log INFO "🏖️ Running PayPal Sandbox Tests..."
    
    # Check for credentials
    if [[ -z "$PAYPAL_CLIENT_ID" || -z "$PAYPAL_CLIENT_SECRET" ]]; then
        log WARN "PayPal sandbox credentials not found, skipping real sandbox tests"
        return 0
    fi
    
    # Set environment for real tests
    export USE_MOCK_PAYMENT_SERVICE=false
    export PAYPAL_CLIENT_ID="${PAYPAL_CLIENT_ID}"
    export PAYPAL_CLIENT_SECRET="${PAYPAL_CLIENT_SECRET}"
    
    # Real PayPal sandbox tests
    run_test_category "sandbox" "Real PayPal Sandbox Tests" \
        "Service=Sandbox&Category=Integration"
}

run_webhook_tests() {
    log INFO "🪝 Running Webhook Tests..."
    
    # Webhook endpoint tests
    run_test_category "webhooks" "Webhook Endpoint Tests" \
        "Component=WebhookEndpoints&Category=Integration"
}

run_cicd_tests() {
    log INFO "🔄 Running CI/CD Integration Tests..."
    
    # Full CI/CD test suite
    run_test_category "cicd" "CI/CD Integration Tests" \
        "Component=CI/CD&Category=Integration"
}

main() {
    log INFO "Starting PayPal Integration Tests - Stage: $STAGE"
    log INFO "Configuration: MOCK_ONLY=$MOCK_ONLY, REAL_ONLY=$REAL_ONLY, VERBOSE=$VERBOSE"
    
    # Check prerequisites
    check_prerequisites
    
    # Build the project first
    log INFO "Building test project..."
    if ! dotnet build "$TEST_PROJECT" --configuration Debug; then
        log ERROR "Failed to build test project"
        exit 1
    fi
    
    local exit_code=0
    
    # Run tests based on stage and flags
    case $STAGE in
        health)
            run_health_checks || exit_code=$?
            ;;
        mock)
            if [[ "$REAL_ONLY" != "true" ]]; then
                run_mock_tests || exit_code=$?
            else
                log INFO "Skipping mock tests (--real-only specified)"
            fi
            ;;
        sandbox)
            if [[ "$MOCK_ONLY" != "true" ]]; then
                run_sandbox_tests || exit_code=$?
            else
                log INFO "Skipping sandbox tests (--mock-only specified)"
            fi
            ;;
        webhooks)
            run_webhook_tests || exit_code=$?
            ;;
        cicd)
            run_cicd_tests || exit_code=$?
            ;;
        all)
            # Run all stages incrementally
            run_health_checks || exit_code=$?
            
            if [[ "$REAL_ONLY" != "true" ]]; then
                run_mock_tests || exit_code=$?
            fi
            
            if [[ "$MOCK_ONLY" != "true" ]]; then
                run_sandbox_tests || exit_code=$?
            fi
            
            run_webhook_tests || exit_code=$?
            run_cicd_tests || exit_code=$?
            ;;
        *)
            log ERROR "Unknown stage: $STAGE"
            log INFO "Use --help to see available stages"
            exit 1
            ;;
    esac
    
    # Summary
    if [[ $exit_code -eq 0 ]]; then
        log INFO "🎉 All PayPal integration tests completed successfully!"
    else
        log ERROR "💥 Some PayPal integration tests failed"
        log INFO "Check the log file for details: $LOG_FILE"
    fi
    
    log INFO "Test results saved to: $LOG_FILE"
    exit $exit_code
}

# Run main function
main "$@"