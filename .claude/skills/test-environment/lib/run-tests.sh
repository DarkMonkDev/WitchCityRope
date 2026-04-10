#!/bin/bash
# run-tests.sh - Execute tests in test environment containers
# CRITICAL: Extracts and summarizes ALL test results before container cleanup

set -e

# Load colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Get PROJECT_ROOT from parent script or calculate it
if [ -z "$PROJECT_ROOT" ]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
fi

# Extract and summarize test results
# NOTE: With volume mount, test-results.json is written directly to host
# This function now just verifies the file exists and copies additional artifacts
extract_test_results() {
    local container_name="witchcity-test-runner"
    local results_dir="$PROJECT_ROOT/test-results"

    echo -e "${BLUE}📋 Checking test results...${NC}"

    # With volume mount, test-results.json should already be on host
    if [ -f "$results_dir/test-results.json" ]; then
        echo -e "  ${GREEN}✓${NC} test-results.json available (via volume mount)"
    else
        echo -e "  ${YELLOW}⚠${NC} test-results.json not found - tests may have failed to complete"
    fi

    echo ""
}

# Generate comprehensive test summary from JSON results
generate_test_summary() {
    local results_dir="$PROJECT_ROOT/test-results"
    local json_file="$results_dir/test-results.json"
    local summary_file="$results_dir/test-summary.txt"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')

    echo -e "${BLUE}📊 Generating test summary...${NC}"

    if [ ! -f "$json_file" ]; then
        echo -e "${YELLOW}⚠ No test-results.json found - cannot generate full summary${NC}"
        echo "Test run at $timestamp" > "$summary_file"
        echo "ERROR: test-results.json not found" >> "$summary_file"
        return 1
    fi

    # Parse JSON and extract stats using jq
    if command -v jq &> /dev/null; then
        local stats=$(cat "$json_file" | jq -r '
            .stats as $stats |
            .suites as $suites |
            {
                total: (.suites | [.. | .specs? | select(.) | .[]] | length),
                passed: ([.. | .tests? | select(.) | .[] | select(.status == "passed" or .status == "expected")] | length),
                failed: ([.. | .tests? | select(.) | .[] | select(.status == "failed" or .status == "unexpected")] | length),
                skipped: ([.. | .tests? | select(.) | .[] | select(.status == "skipped")] | length),
                flaky: ([.. | .tests? | select(.) | .[] | select(.status == "flaky")] | length)
            }
        ' 2>/dev/null)

        if [ -n "$stats" ] && [ "$stats" != "null" ]; then
            local total=$(echo "$stats" | jq -r '.total')
            local passed=$(echo "$stats" | jq -r '.passed')
            local failed=$(echo "$stats" | jq -r '.failed')
            local skipped=$(echo "$stats" | jq -r '.skipped')
            local flaky=$(echo "$stats" | jq -r '.flaky')

            # Calculate pass rate
            local pass_rate=0
            if [ "$total" -gt 0 ]; then
                pass_rate=$(echo "scale=1; $passed * 100 / $total" | bc 2>/dev/null || echo "0")
            fi

            # Write summary file
            cat > "$summary_file" << EOF
================================================================================
                         E2E TEST RESULTS SUMMARY
================================================================================
Timestamp: $timestamp
================================================================================

RESULTS:
  ✓ Passed:  $passed
  ✗ Failed:  $failed
  ○ Skipped: $skipped
  ⚡ Flaky:   $flaky
  ─────────────────
  Total:     $total

PASS RATE: ${pass_rate}%

================================================================================
EOF

            # Print summary to console
            echo ""
            echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
            echo -e "${CYAN}                    E2E TEST RESULTS SUMMARY${NC}"
            echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
            echo ""
            echo -e "  ${GREEN}✓ Passed:${NC}  $passed"
            echo -e "  ${RED}✗ Failed:${NC}  $failed"
            echo -e "  ${YELLOW}○ Skipped:${NC} $skipped"
            echo -e "  ${BLUE}⚡ Flaky:${NC}   $flaky"
            echo "  ─────────────────"
            echo -e "  Total:     $total"
            echo ""
            if [ "$failed" -eq 0 ]; then
                echo -e "  ${GREEN}PASS RATE: ${pass_rate}% ✓${NC}"
            else
                echo -e "  ${YELLOW}PASS RATE: ${pass_rate}%${NC}"
            fi
            echo ""
            echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
            echo ""

            # List failed tests if any
            if [ "$failed" -gt 0 ]; then
                echo -e "${RED}FAILED TESTS:${NC}"
                # Use a proper jq query that navigates the Playwright JSON structure
                cat "$json_file" | jq -r '
                    [.suites | .. | objects | select(.file?) |
                     .specs[]? |
                     select(.tests[0].results[0].status == "failed" or .tests[0].results[0].status == "unexpected") |
                     "  ✗ \(.file): \(.title)"
                    ] | sort | unique | .[:20][]
                ' 2>/dev/null || echo "  (Could not parse test names)"

                local failed_count=$(cat "$json_file" | jq '
                    [.suites | .. | objects | select(.file?) |
                     .specs[]? |
                     select(.tests[0].results[0].status == "failed" or .tests[0].results[0].status == "unexpected")
                    ] | length
                ' 2>/dev/null || echo "0")
                if [ "$failed_count" -gt 20 ]; then
                    echo -e "  ${YELLOW}... and $((failed_count - 20)) more${NC}"
                fi
                echo ""

                # Append failed tests to summary file
                echo "" >> "$summary_file"
                echo "FAILED TESTS:" >> "$summary_file"
                cat "$json_file" | jq -r '
                    [.suites | .. | objects | select(.file?) |
                     .specs[]? |
                     select(.tests[0].results[0].status == "failed" or .tests[0].results[0].status == "unexpected") |
                     "  ✗ \(.file): \(.title)"
                    ] | sort | unique[]
                ' 2>/dev/null >> "$summary_file" || echo "  (Could not parse test names)" >> "$summary_file"
            fi

            echo -e "${GREEN}Summary saved to: $summary_file${NC}"

            # Write small quick-summary JSON file for easy parsing
            local quick_summary_file="$results_dir/quick-summary.json"
            cat > "$quick_summary_file" << QSEOF
{
  "timestamp": "$timestamp",
  "total": $total,
  "passed": $passed,
  "failed": $failed,
  "skipped": $skipped,
  "flaky": $flaky,
  "pass_rate": "$pass_rate",
  "status": "$([ "$failed" -eq 0 ] && echo "passed" || echo "failed")"
}
QSEOF
            echo ""

            # Output structured JSON for automated parsing
            # This block appears at the END of all output for easy extraction
            echo "=== TEST_RESULTS_JSON ==="
            cat "$quick_summary_file"
            echo "=== END_TEST_RESULTS_JSON ==="
            echo ""

            return 0
        fi
    fi

    # Fallback: count from .last-run.json if jq parsing failed
    echo -e "${YELLOW}⚠ Could not parse test-results.json, using .last-run.json${NC}"
    if [ -f "$results_dir/.last-run.json" ]; then
        local failed_count=$(cat "$results_dir/.last-run.json" | jq '.failedTests | length' 2>/dev/null || echo "?")
        echo "  Failed tests: $failed_count"
        echo "Test run at $timestamp" > "$summary_file"
        echo "Failed tests: $failed_count" >> "$summary_file"
        echo "(Full breakdown unavailable - JSON parsing failed)" >> "$summary_file"
    fi
}

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
        # NOTE: the dead .NET modes ("all", "unit", "integration", "dotnet") were
        # removed on 2026-04-10. They tried to `docker-compose exec api dotnet test`
        # into a container whose test stage only copied apps/api/ (no test projects),
        # so they silently produced zero results on every invocation since they were
        # added (2025-11-27). See .claude/skills/run-test-suite/ for the replacement
        # that runs .NET tests from the host, and its SKILL.md "Why This Skill Exists"
        # section for the full history.
        #
        # This skill now handles ONLY E2E and failed-only. For .NET tests or for
        # running everything (.NET + E2E), use:
        #     bash .claude/skills/run-test-suite/execute.sh --mode all
        "e2e")
            echo "Running E2E tests in test-runner container..."
            echo "  Using web service: http://web:5173"
            echo "  Using API service: http://api:8080"

            # Build playwright command arguments
            local playwright_args=""
            local filter_args=""

            # Add filter if specified - use --grep for regex patterns
            if [ -n "$filter" ]; then
                echo "  Filtering tests: $filter"
                # Quote the filter to prevent shell interpretation of pipe characters
                filter_args="--grep '$filter'"
            fi

            # IMPORTANT: Don't override reporter settings here!
            # The playwright.config.ts defines reporters (list, json, html)
            # and output paths. We use those settings to ensure:
            # - JSON report captures error messages (critical for debugging)
            # - HTML report provides detailed failure analysis
            # - Traces are captured on failure
            playwright_args=""

            # Add workers if specified
            if [ -n "$PW_WORKERS" ]; then
                playwright_args="$playwright_args --workers=$PW_WORKERS"
                echo "  Using $PW_WORKERS parallel workers"
            fi

            # Execute in test-runner container
            # The baseURL will use WEB_BASE_URL env var from docker-compose.test.yml
            docker exec witchcity-test-runner sh -c "
                cd /app && \
                export PLAYWRIGHT_BASE_URL=http://web:5173 && \
                npx playwright test $filter_args $playwright_args
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

    # CRITICAL: Extract test results BEFORE cleanup can happen.
    # Only e2e mode produces the Playwright test-results.json that these helpers
    # parse; failed-only will eventually too once implemented. The dead .NET modes
    # that used to be handled here were removed on 2026-04-10.
    if [ "$mode" = "e2e" ]; then
        extract_test_results
        generate_test_summary
    fi

    # Save run metadata
    mkdir -p "$PROJECT_ROOT/test-results"
    echo "mode=$mode filter=$filter exit_code=$exit_code timestamp=$(date +%s)" > "$PROJECT_ROOT/test-results/.last-run-meta"

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
