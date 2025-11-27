#!/bin/bash
# run-tests.sh - Execute tests in test environment containers

set -e

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
    local project="witchcityrope-test"

    echo -e "${BLUE}🧪 Running tests: mode=$mode${NC}"
    [ -n "$filter" ] && echo -e "${BLUE}   Filter: $filter${NC}"

    local test_cmd=""
    local exit_code=0

    case "$mode" in
        "all")
            echo "Running all tests (unit + integration + e2e)..."
            # Run .NET tests
            docker-compose -p $project -f docker-compose.yml -f docker-compose.test.yml \
                exec -T api dotnet test || exit_code=$?

            # Run E2E tests in test-runner container
            docker exec witchcity-test-runner \
                npx playwright test --config=playwright.config.ts \
                --reporter=list,json,html || exit_code=$?
            ;;

        "unit")
            echo "Running unit tests..."
            docker-compose -p $project -f docker-compose.yml -f docker-compose.test.yml \
                exec -T api dotnet test --filter "Category=Unit" || exit_code=$?
            ;;

        "integration")
            echo "Running integration tests..."
            docker-compose -p $project -f docker-compose.yml -f docker-compose.test.yml \
                exec -T api dotnet test --filter "Category=Integration" || exit_code=$?
            ;;

        "e2e")
            echo "Running E2E tests in test-runner container..."
            echo "  Using web service: http://web:5173"
            echo "  Using API service: http://api:8080"

            # Build playwright command
            local playwright_cmd="npx playwright test"

            # Add filter if specified
            if [ -n "$filter" ]; then
                echo "  Filtering tests: $filter"
                playwright_cmd="$playwright_cmd $filter"
            fi

            # Add coverage if requested
            if [ "$coverage" = "true" ]; then
                playwright_cmd="$playwright_cmd --reporter=list,json,html"
            else
                playwright_cmd="$playwright_cmd --reporter=list"
            fi

            # Execute in test-runner container
            # The baseURL will use WEB_BASE_URL env var from docker-compose.test.yml
            docker exec witchcity-test-runner sh -c "
                export PLAYWRIGHT_BASE_URL=http://web:5173 && \
                $playwright_cmd
            " || exit_code=$?
            ;;

        "failed-only")
            echo "Running previously failed tests..."
            if [ -f "$PROJECT_ROOT/test-results/.failed-tests.json" ]; then
                # TODO: Implement failed test tracking
                echo -e "${YELLOW}⚠️  Failed test tracking not yet implemented${NC}"
                echo "   Running all E2E tests for now..."
                docker exec witchcity-test-runner \
                    npx playwright test --reporter=list || exit_code=$?
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

    # Copy test results from container to host
    if [ "$mode" = "e2e" ] || [ "$mode" = "all" ]; then
        echo -e "${BLUE}📋 Copying test results from container...${NC}"
        mkdir -p "$PROJECT_ROOT/test-results"
        docker cp witchcity-test-runner:/app/test-results/. "$PROJECT_ROOT/test-results/" 2>/dev/null || true
        docker cp witchcity-test-runner:/app/playwright-results/. "$PROJECT_ROOT/test-results/" 2>/dev/null || true
    fi

    # Save run metadata
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
