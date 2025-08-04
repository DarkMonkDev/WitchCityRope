#!/bin/bash

# Enhanced Playwright Test Runner with Category Support
# Integrates with the CI/CD pipeline and test categorization system

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TEST_CATEGORIES_FILE="$PROJECT_ROOT/tests/playwright/test-categories.json"

# Default values
CATEGORY="smoke"
BROWSER="chromium"  
ENVIRONMENT="docker"
HEADED="false"
DOCKER_COMPOSE_FILE="docker-compose.yml"
MAX_RETRIES=3
TIMEOUT=30000

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Usage information
usage() {
    echo "Enhanced Playwright Test Runner"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --category CATEGORY     Test category to run (default: smoke)"
    echo "  -b, --browser BROWSER       Browser to use (default: chromium)"
    echo "  -e, --environment ENV       Environment: docker, local, staging (default: docker)"
    echo "  -h, --headed                Run in headed mode"
    echo "  -d, --debug                 Run in debug mode"
    echo "  -u, --ui                    Open Playwright UI"
    echo "  -r, --retries N             Number of retries (default: 3)"
    echo "  -t, --timeout N             Timeout in milliseconds (default: 30000)"
    echo "  --docker-file FILE          Docker compose file to use"
    echo "  --list                      List available test categories"
    echo "  --help                      Show this help message"
    echo ""
    echo "Available Categories:"
    echo "  smoke         - Critical path tests"
    echo "  auth          - Authentication tests" 
    echo "  events        - Event management tests"
    echo "  admin         - Admin functionality tests"
    echo "  validation    - Form validation tests"
    echo "  rsvp          - RSVP and membership tests"
    echo "  ui            - User interface tests"
    echo "  api           - API endpoint tests"
    echo "  visual        - Visual regression tests"
    echo "  performance   - Performance tests"
    echo "  all           - All test categories"
    echo ""
    echo "Examples:"
    echo "  $0 --category smoke --browser chromium"
    echo "  $0 --category auth --headed --environment docker"
    echo "  $0 --category all --browser firefox --retries 2"
}

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# List available test categories
list_categories() {
    echo "Available Test Categories:"
    echo ""
    if [[ -f "$TEST_CATEGORIES_FILE" ]]; then
        # Use jq if available, otherwise basic parsing
        if command -v jq &> /dev/null; then
            jq -r '.testCategories | to_entries[] | "  \(.key) - \(.value.description)"' "$TEST_CATEGORIES_FILE"
        else
            grep -E '"[a-zA-Z-]+": {' "$TEST_CATEGORIES_FILE" | sed 's/.*"\([^"]*\)".*/  \1/' | head -10
        fi
    else
        echo "  No test categories configuration found"
    fi
    echo ""
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -c|--category)
                CATEGORY="$2"
                shift 2
                ;;
            -b|--browser)
                BROWSER="$2"
                shift 2
                ;;
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -h|--headed)
                HEADED="true"
                shift
                ;;
            -d|--debug)
                DEBUG="true"
                HEADED="true"
                shift
                ;;
            -u|--ui)
                UI_MODE="true"
                shift
                ;;
            -r|--retries)
                MAX_RETRIES="$2"
                shift 2
                ;;
            -t|--timeout)
                TIMEOUT="$2"
                shift 2
                ;;
            --docker-file)
                DOCKER_COMPOSE_FILE="$2"
                shift 2
                ;;
            --list)
                list_categories
                exit 0
                ;;
            --help)
                usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if we're in the right directory
    if [[ ! -f "$PROJECT_ROOT/playwright.config.ts" ]]; then
        log_error "playwright.config.ts not found. Please run from project root."
        exit 1
    fi
    
    # Check if Node.js and npm are available
    if ! command -v npm &> /dev/null; then
        log_error "npm not found. Please install Node.js and npm."
        exit 1
    fi
    
    # Check if Playwright is installed
    if [[ ! -d "$PROJECT_ROOT/node_modules/@playwright" ]]; then
        log_info "Installing Playwright dependencies..."
        cd "$PROJECT_ROOT"
        npm ci
        npx playwright install --with-deps
    fi
    
    # Environment-specific checks
    case $ENVIRONMENT in
        docker)
            if ! command -v docker &> /dev/null; then
                log_error "Docker not found. Please install Docker."
                exit 1
            fi
            if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
                log_error "Docker Compose not found. Please install Docker Compose."
                exit 1
            fi
            ;;
        local)
            if ! command -v dotnet &> /dev/null; then
                log_error "dotnet CLI not found. Please install .NET SDK."
                exit 1
            fi
            ;;
        staging)
            log_info "Staging environment - assuming external setup"
            ;;
    esac
    
    log_success "Prerequisites check completed"
}

# Setup environment
setup_environment() {
    log_info "Setting up $ENVIRONMENT environment..."
    
    case $ENVIRONMENT in
        docker)
            setup_docker_environment
            ;;
        local)
            setup_local_environment
            ;;
        staging)
            setup_staging_environment
            ;;
    esac
}

# Setup Docker environment
setup_docker_environment() {
    cd "$PROJECT_ROOT"
    
    # Create necessary directories
    mkdir -p logs/api logs/web uploads data
    
    # Use CI-optimized compose file if in CI
    if [[ "$CI" == "true" ]]; then
        DOCKER_COMPOSE_FILE="docker-compose.ci.yml"
        log_info "Using CI-optimized Docker configuration"
    fi
    
    # Start Docker services
    log_info "Starting Docker services..."
    docker compose -f "$DOCKER_COMPOSE_FILE" up -d --wait
    
    # Wait for services to be healthy
    log_info "Waiting for services to be ready..."
    
    local max_attempts=30
    local attempt=0
    
    while [[ $attempt -lt $max_attempts ]]; do
        if curl -f http://localhost:5651/health &> /dev/null; then
            log_success "Web service is ready"
            break
        fi
        
        attempt=$((attempt + 1))
        log_info "Waiting for web service... (attempt $attempt/$max_attempts)"
        sleep 2
    done
    
    if [[ $attempt -eq $max_attempts ]]; then
        log_error "Web service failed to start"
        docker compose -f "$DOCKER_COMPOSE_FILE" logs web
        exit 1
    fi
}

# Setup local environment
setup_local_environment() {
    log_info "Setting up local .NET environment..."
    
    cd "$PROJECT_ROOT"
    
    # Check if application is already running
    if curl -f http://localhost:5651/health &> /dev/null; then
        log_info "Application is already running"
        return
    fi
    
    # Start the application
    log_info "Starting application locally..."
    ./run.sh &
    APP_PID=$!
    
    # Wait for application to start
    local max_attempts=30
    local attempt=0
    
    while [[ $attempt -lt $max_attempts ]]; do
        if curl -f http://localhost:5651/health &> /dev/null; then
            log_success "Local application is ready"
            return
        fi
        
        attempt=$((attempt + 1))
        log_info "Waiting for local application... (attempt $attempt/$max_attempts)"
        sleep 2
    done
    
    log_error "Local application failed to start"
    exit 1
}

# Setup staging environment
setup_staging_environment() {
    log_info "Verifying staging environment access..."
    
    if ! curl -f https://staging.witchcityrope.com/health &> /dev/null; then
        log_error "Cannot access staging environment"
        exit 1
    fi
    
    log_success "Staging environment is accessible"
}

# Get test files for category
get_test_files() {
    local category="$1"
    
    case "$category" in
        smoke)
            echo "tests/playwright/auth/login-basic.spec.ts tests/playwright/infrastructure/page-status.spec.ts"
            ;;
        auth)
            echo "tests/playwright/auth/"
            ;;
        events)
            echo "tests/playwright/events/"
            ;;
        admin)
            echo "tests/playwright/admin/"
            ;;
        validation)
            echo "tests/playwright/validation/"
            ;;
        rsvp)
            echo "tests/playwright/rsvp/"
            ;;
        ui)
            echo "tests/playwright/ui/"
            ;;
        api)
            echo "tests/playwright/api/"
            ;;
        visual)
            echo "tests/playwright/specs/visual/"
            ;;
        performance)
            echo "tests/playwright/validation/performance-validation.spec.ts"
            ;;
        infrastructure)
            echo "tests/playwright/infrastructure/"
            ;;
        all)
            echo "" # Empty means all tests
            ;;
        *)
            log_error "Unknown category: $category"
            exit 1
            ;;
    esac
}

# Run Playwright tests
run_tests() {
    log_info "Running Playwright tests for category: $CATEGORY"
    
    cd "$PROJECT_ROOT"
    
    # Set environment variables
    export BASE_URL
    case $ENVIRONMENT in
        docker)
            BASE_URL="http://localhost:5651"
            ;;
        local)
            BASE_URL="http://localhost:5651"
            ;;
        staging)
            BASE_URL="https://staging.witchcityrope.com"
            ;;
    esac
    
    export CI="${CI:-false}"
    export TEST_CATEGORY="$CATEGORY"
    export TEST_BROWSER="$BROWSER"
    export TEST_ENVIRONMENT="$ENVIRONMENT"
    
    # Get test files for the category
    local test_files
    test_files=$(get_test_files "$CATEGORY")
    
    # Build Playwright command
    local playwright_cmd="npx playwright test"
    
    # Add test files if specified
    if [[ -n "$test_files" ]]; then
        playwright_cmd="$playwright_cmd $test_files"
    fi
    
    # Add browser project
    playwright_cmd="$playwright_cmd --project=$BROWSER"
    
    # Add additional options
    if [[ "$HEADED" == "true" ]]; then
        playwright_cmd="$playwright_cmd --headed"
    fi
    
    if [[ "$DEBUG" == "true" ]]; then
        playwright_cmd="$playwright_cmd --debug"
    fi
    
    if [[ "$UI_MODE" == "true" ]]; then
        playwright_cmd="$playwright_cmd --ui"
    fi
    
    # Add retries
    if [[ "$MAX_RETRIES" -gt 0 ]]; then
        export PLAYWRIGHT_RETRIES="$MAX_RETRIES"
    fi
    
    # Add timeout
    export PLAYWRIGHT_TIMEOUT="$TIMEOUT"
    
    log_info "Executing: $playwright_cmd"
    
    # Run the tests
    if eval "$playwright_cmd"; then
        log_success "Tests completed successfully"
        return 0
    else
        log_error "Tests failed"
        return 1
    fi
}

# Cleanup function
cleanup() {
    log_info "Cleaning up..."
    
    case $ENVIRONMENT in
        docker)
            if [[ "$CI" != "true" ]]; then
                log_info "Stopping Docker services..."
                docker compose -f "$DOCKER_COMPOSE_FILE" down
            else
                log_info "Leaving Docker services running for CI"
            fi
            ;;
        local)
            if [[ -n "$APP_PID" ]]; then
                log_info "Stopping local application..."
                kill "$APP_PID" 2>/dev/null || true
            fi
            ;;
    esac
}

# Main execution
main() {
    parse_args "$@"
    
    log_info "Starting Playwright test execution..."
    log_info "Category: $CATEGORY"
    log_info "Browser: $BROWSER" 
    log_info "Environment: $ENVIRONMENT"
    
    check_prerequisites
    setup_environment
    
    # Set up cleanup trap
    trap cleanup EXIT
    
    # Run tests
    if run_tests; then
        log_success "All tests completed successfully!"
        exit 0
    else
        log_error "Some tests failed!"
        exit 1
    fi
}

# Execute main function if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi