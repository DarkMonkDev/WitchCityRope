#!/bin/bash
# test-environment - Isolated Test Container Management
# Runs all tests in dedicated containers separate from dev environment

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Load helper scripts
source "$SCRIPT_DIR/lib/cleanup.sh"
source "$SCRIPT_DIR/lib/build-containers.sh"
source "$SCRIPT_DIR/lib/start-containers.sh"
source "$SCRIPT_DIR/lib/health-checks.sh"
source "$SCRIPT_DIR/lib/run-tests.sh"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Default values
MODE="e2e"
FILTER=""
COVERAGE=false
KEEP_IMAGES=false
KEEP_CONTAINERS=false
SKIP_CONFIRMATION="${SKIP_CONFIRMATION:-false}"

show_usage() {
    cat << EOF
${CYAN}test-environment${NC} - Isolated Test Container Management

${YELLOW}USAGE:${NC}
    bash .claude/skills/test-environment/execute.sh [OPTIONS]

${YELLOW}OPTIONS:${NC}
    --mode MODE         Test mode (default: e2e)
                        Values: all, unit, integration, e2e, failed-only
    --filter PATTERN    Filter E2E tests by pattern
    --coverage          Generate coverage reports
    --keep-images       Keep built images after cleanup
    --keep-containers   Keep containers running for debugging
    --skip-confirm      Skip confirmation prompts

${YELLOW}EXAMPLES:${NC}
    # Run all E2E tests (default)
    bash .claude/skills/test-environment/execute.sh

    # Run specific E2E tests
    bash .claude/skills/test-environment/execute.sh --mode e2e --filter "admin-events"

    # Run all test types
    bash .claude/skills/test-environment/execute.sh --mode all

    # Run unit tests only
    bash .claude/skills/test-environment/execute.sh --mode unit

    # Keep containers for debugging
    bash .claude/skills/test-environment/execute.sh --mode e2e --keep-containers

${YELLOW}FEATURES:${NC}
    ✓ Complete isolation from dev containers
    ✓ Fresh database each run
    ✓ Builds from current codebase
    ✓ Automatic cleanup (prevents orphaned images)
    ✓ Health checks before running tests
    ✓ Supports all test types (unit, integration, E2E)

EOF
}

show_preflight() {
    cat << EOF

${CYAN}═══════════════════════════════════════════════════════════════${NC}
${CYAN}  Test Environment - Isolated Container Testing${NC}
${CYAN}═══════════════════════════════════════════════════════════════${NC}

${YELLOW}Purpose:${NC}
  Run tests in isolated containers (separate from dev environment)
  Prevents interference with development work

${YELLOW}What this will do:${NC}
  1. Build fresh test images from current codebase
  2. Start isolated test containers (separate database)
  3. Run health checks (compilation, database, services)
  4. Execute tests: ${BLUE}${MODE}${NC}${FILTER:+ (filter: ${FILTER})}
  5. Save results to /test-results/
  6. Clean up containers and images

${YELLOW}Test Mode:${NC} ${MODE}
${YELLOW}Filter:${NC} ${FILTER:-none}
${YELLOW}Coverage:${NC} ${COVERAGE}
${YELLOW}Keep Images:${NC} ${KEEP_IMAGES}
${YELLOW}Keep Containers:${NC} ${KEEP_CONTAINERS}

${YELLOW}⚠️  NOTE:${NC}
  - Dev containers will NOT be touched
  - Test containers use separate database (witchcityrope_test)
  - Fresh environment each run (unless --keep-* flags used)

${CYAN}═══════════════════════════════════════════════════════════════${NC}

EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --mode)
            MODE="$2"
            shift 2
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --coverage)
            COVERAGE=true
            shift
            ;;
        --keep-images)
            KEEP_IMAGES=true
            shift
            ;;
        --keep-containers)
            KEEP_CONTAINERS=true
            shift
            ;;
        --skip-confirm)
            SKIP_CONFIRMATION=true
            shift
            ;;
        --help|-h)
            show_usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            show_usage
            exit 1
            ;;
    esac
done

# Validate mode
if [[ ! "$MODE" =~ ^(all|unit|integration|e2e|failed-only)$ ]]; then
    echo -e "${RED}❌ Invalid mode: $MODE${NC}"
    show_usage
    exit 1
fi

# Show pre-flight information
show_preflight

# Confirmation prompt
if [ "$SKIP_CONFIRMATION" != "true" ]; then
    echo -e "${YELLOW}Continue? (y/N):${NC} "
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        echo "Cancelled."
        exit 0
    fi
    echo ""
fi

# Setup cleanup trap
cleanup_on_exit() {
    local exit_code=$?
    echo ""
    cleanup_test_environment "$KEEP_IMAGES" "$KEEP_CONTAINERS"
    exit $exit_code
}
trap cleanup_on_exit EXIT INT TERM

# Main execution
echo -e "${CYAN}Starting test environment...${NC}"
echo ""

# Step 1: Build test images
if ! build_test_images; then
    echo -e "${RED}❌ Failed to build test images${NC}"
    exit 1
fi
echo ""

# Step 2: Start test containers
if ! start_test_containers; then
    echo -e "${RED}❌ Failed to start test containers${NC}"
    exit 1
fi
echo ""

# Step 3: Health checks
if ! verify_test_environment; then
    echo -e "${RED}❌ Test environment not healthy${NC}"
    exit 1
fi
echo ""

# Step 4: Run tests
if ! run_tests "$MODE" "$FILTER" "$COVERAGE"; then
    echo -e "${RED}❌ Tests failed${NC}"
    exit 1
fi
echo ""

echo -e "${GREEN}✅ Test execution complete${NC}"
echo ""

# Cleanup happens automatically via trap
