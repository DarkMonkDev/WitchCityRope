#!/bin/bash
# test-environment - Isolated Test Container Management
# Runs all tests in dedicated containers separate from dev environment
#
# This skill:
# 1. Calls restart-test-containers skill for container setup
# 2. Runs the specified tests
# 3. Cleans up (unless --keep-containers is used)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Load helper scripts (only run-tests and cleanup needed now)
source "$SCRIPT_DIR/lib/cleanup.sh"
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
SKIP_REBUILD=false
SKIP_CONFIRMATION="${SKIP_CONFIRMATION:-false}"

show_usage() {
    cat << EOF
${CYAN}test-environment${NC} - Isolated Test Container Management

${YELLOW}USAGE:${NC}
    bash .claude/skills/test-environment/execute.sh [OPTIONS]

${YELLOW}OPTIONS:${NC}
    --mode MODE         Test mode (default: e2e)
    --filter PATTERN    Filter E2E tests by pattern (only works with e2e mode)
    --coverage          Generate coverage reports
    --keep-images       Keep built images after cleanup
    --keep-containers   Keep containers running for debugging
    --skip-rebuild      Skip container rebuild (faster when code hasn't changed)
    --skip-confirm      Skip confirmation prompts

${YELLOW}TEST MODES:${NC}
    e2e (default)   Playwright browser tests only (tests/e2e/*.spec.ts)
                    - Runs in witchcity-test-runner container
                    - Tests full UI workflows via Chromium
                    - Supports --filter for pattern matching
                    - Use for: UI regression testing, feature verification

    dotnet          All .NET tests (unit + integration, NO E2E)
                    - Runs: dotnet test (all .NET test projects)
                    - Includes: tests/integration/, tests/WitchCityRope.*.Tests/
                    - Use for: Backend verification without slow E2E tests
                    - RECOMMENDED for CI and quick backend verification

    all             Everything (.NET tests + E2E tests)
                    - Runs dotnet test, then Playwright tests
                    - Use for: Full regression before deployment

    unit            .NET tests with [Trait("Category", "Unit")] only
                    - Uses: dotnet test --filter "Category=Unit"
                    - NOTE: Few tests have this trait, prefer 'dotnet' mode

    integration     .NET tests with [Trait("Category", "Integration")] only
                    - Uses: dotnet test --filter "Category=Integration"
                    - NOTE: Few tests have this trait, prefer 'dotnet' mode

    failed-only     Re-run previously failed tests (NOT FULLY IMPLEMENTED)
                    - Falls back to running all E2E tests

${YELLOW}EXAMPLES:${NC}
    # Run all E2E tests (default)
    bash .claude/skills/test-environment/execute.sh

    # Run with skip rebuild (faster if containers already exist)
    bash .claude/skills/test-environment/execute.sh --skip-rebuild

    # Run specific E2E tests
    bash .claude/skills/test-environment/execute.sh --mode e2e --filter "admin-events"

    # Run all test types
    bash .claude/skills/test-environment/execute.sh --mode all

    # Run all .NET tests (unit + integration, no E2E) - RECOMMENDED for backend
    bash .claude/skills/test-environment/execute.sh --mode dotnet

    # Run all .NET tests with fast rebuild
    bash .claude/skills/test-environment/execute.sh --mode dotnet --skip-rebuild

    # Keep containers for debugging
    bash .claude/skills/test-environment/execute.sh --mode e2e --keep-containers

${YELLOW}FEATURES:${NC}
    ✓ Complete isolation from dev containers (-p witchcityrope-test)
    ✓ Fresh database each run (unless --skip-rebuild)
    ✓ Builds from current codebase (unless --skip-rebuild)
    ✓ Automatic cleanup (prevents orphaned images)
    ✓ Health checks before running tests
    ✓ Supports all test types (unit, integration, E2E)

${YELLOW}RELATED SKILLS:${NC}
    • restart-test-containers - Container setup only (called internally)
    • restart-dev-containers - For development containers

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
  1. Call restart-test-containers skill to setup test environment
EOF
    if [ "$SKIP_REBUILD" = "true" ]; then
        echo "     (using --skip-rebuild mode - faster, no container rebuild)"
    else
        echo "     (full rebuild with --no-cache for clean test environment)"
    fi
    cat << EOF
  2. Run health checks (compilation, database, services)
  3. Execute tests: ${BLUE}${MODE}${NC}${FILTER:+ (filter: ${FILTER})}
  4. Save results to /test-results/
EOF
    if [ "$KEEP_CONTAINERS" = "false" ]; then
        echo "  5. Clean up containers and images"
    else
        echo "  5. Keep containers for debugging (--keep-containers)"
    fi
    cat << EOF

${YELLOW}Test Mode:${NC} ${MODE}
${YELLOW}Filter:${NC} ${FILTER:-none}
${YELLOW}Coverage:${NC} ${COVERAGE}
${YELLOW}Skip Rebuild:${NC} ${SKIP_REBUILD}
${YELLOW}Keep Images:${NC} ${KEEP_IMAGES}
${YELLOW}Keep Containers:${NC} ${KEEP_CONTAINERS}

${YELLOW}⚠️  NOTE:${NC}
  - Dev containers will NOT be touched (isolated by -p witchcityrope-test)
  - Test containers use separate database (witchcityrope_test)
  - Fresh environment each run (unless --skip-rebuild flag used)

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
        --skip-rebuild)
            SKIP_REBUILD=true
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
if [[ ! "$MODE" =~ ^(all|unit|integration|e2e|dotnet|failed-only)$ ]]; then
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

# Step 1: Call restart-test-containers skill for container setup
echo -e "${BLUE}📦 Setting up test containers via restart-test-containers skill...${NC}"
echo ""

RESTART_CMD="SKIP_CONFIRMATION=true bash $PROJECT_ROOT/.claude/skills/restart-test-containers/execute.sh"
if [ "$SKIP_REBUILD" = "true" ]; then
    RESTART_CMD="$RESTART_CMD --skip-rebuild"
fi

if ! eval "$RESTART_CMD"; then
    echo -e "${RED}❌ Failed to setup test containers${NC}"
    exit 1
fi
echo ""

# Step 2: Run tests
echo -e "${BLUE}🧪 Running tests...${NC}"
if ! run_tests "$MODE" "$FILTER" "$COVERAGE"; then
    echo -e "${RED}❌ Tests failed${NC}"
    exit 1
fi
echo ""

echo -e "${GREEN}✅ Test execution complete${NC}"
echo ""

# Cleanup happens automatically via trap
