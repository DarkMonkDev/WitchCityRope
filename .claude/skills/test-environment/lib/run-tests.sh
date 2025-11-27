#!/bin/bash
# run-tests.sh - Execute tests in test environment

set -e

#SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
#PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

# Load colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

run_tests() {
    local mode="$1"
    local filter="$2"
    local coverage="${3:-false}"

    echo -e "${BLUE}🧪 Running tests: mode=$mode${NC}"
    [ -n "$filter" ] && echo -e "${BLUE}   Filter: $filter${NC}"

    cd "$PROJECT_ROOT"

    local test_cmd=""
    local exit_code=0

    case "$mode" in
        "all")
            echo "Running all tests (unit + integration + e2e)..."
            # Run .NET tests
            docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml exec -T api dotnet test || exit_code=$?
            # Run E2E tests
            cd apps/web
            npx playwright test || exit_code=$?
            cd "$PROJECT_ROOT"
            ;;

        "unit")
            echo "Running unit tests..."
            docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml exec -T api dotnet test --filter "Category=Unit" || exit_code=$?
            ;;

        "integration")
            echo "Running integration tests..."
            docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml exec -T api dotnet test --filter "Category=Integration" || exit_code=$?
            ;;

        "e2e")
            echo "Running E2E tests..."
            cd apps/web
            if [ -n "$filter" ]; then
                echo "  Filtering tests: $filter"
                npx playwright test "$filter" || exit_code=$?
            else
                npx playwright test || exit_code=$?
            fi
            cd "$PROJECT_ROOT"
            ;;

        "failed-only")
            echo "Running previously failed tests..."
            if [ -f "$PROJECT_ROOT/test-results/.failed-tests.json" ]; then
                # TODO: Implement failed test tracking
                echo -e "${YELLOW}⚠️  Failed test tracking not yet implemented${NC}"
                echo "   Running all E2E tests for now..."
                cd apps/web
                npx playwright test || exit_code=$?
                cd "$PROJECT_ROOT"
            else
                echo -e "${YELLOW}⚠️  No failed tests recorded${NC}"
                return 0
            fi
            ;;

        *)
            echo -e "${RED}❌ Unknown test mode: $mode${NC}"
            return 1
            ;;
    esac

    # Save results
    mkdir -p "$PROJECT_ROOT/test-results"
    echo "mode=$mode filter=$filter exit_code=$exit_code timestamp=$(date +%s)" > "$PROJECT_ROOT/test-results/.last-run"

    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ Tests passed${NC}"
        return 0
    else
        echo -e "${RED}❌ Tests failed (exit code: $exit_code)${NC}"
        return $exit_code
    fi
}

# If sourced, don't run automatically
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    run_tests "$@"
fi
