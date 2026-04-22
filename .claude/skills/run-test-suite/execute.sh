#!/bin/bash
# WitchCityRope - Run Test Suite
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Runs .NET unit/integration tests and/or Playwright E2E tests.
# Ported from inventory-purchasing-workflow/.claude/skills/run-test-suite/
# on 2026-04-10, adapted for WCR's three test projects.
#
# Arguments:
#   --mode unit    Run .NET unit/integration tests (host-based dotnet test)
#   --mode react   Run React/vitest unit tests (host-based npx vitest)
#   --mode e2e     Run Playwright E2E tests (delegates to test-environment skill)
#   --mode all     Run all of the above
#   --verbose      Show detailed test output
#   --filter NAME  Filter tests by name (unit + react modes); for vitest this
#                  is forwarded as a positional pattern arg, matching against
#                  test file paths and test names.
#
# Architecture notes:
# - .NET tests run on the HOST via `dotnet test <csproj>`, not inside a container.
#   Each test project uses Testcontainers.PostgreSql, which spins up its own
#   sibling postgres container per-run via the host Docker daemon.
# - E2E tests delegate to the `test-environment` skill, which handles the full
#   container lifecycle for Playwright.
# - The previous `test-environment --mode dotnet|unit|integration` paths were
#   architecturally broken (they tried to `docker-compose exec api dotnet test`
#   into a container that contained no test projects). They have been removed.

set -e  # Exit on error

# ============================================
# CONFIGURATION
# ============================================

PROJECT_ROOT="/home/chad/repos/witchcityrope"

# The four WCR .NET test projects that this skill runs.
# WitchCityRope.Tests.Common is a shared test-library, not a runnable test project,
# so it's not listed here.
#
# WitchCityRope.SystemTests was added on 2026-04-10 after an earlier decision
# to exclude it was reversed. The project contains 6 pre-flight health check
# tests ([Trait("Category", "HealthCheck")]) that verify the dev environment
# (React dev server on :5173, API on :5655, postgres on :5434) is up. These
# tests HIT DEV URLS, not test container URLs, so they'll fail if dev containers
# aren't running at the time of the test run. That's intentional — they're
# meant as a "is the dev environment actually up?" sanity check. If you don't
# want them run during a .NET test pass, filter them out:
#   bash .claude/skills/run-test-suite/execute.sh --mode unit \
#     --filter "Category!=HealthCheck"
TEST_PROJECTS=(
    "$PROJECT_ROOT/tests/unit/api/WitchCityRope.Api.Tests.csproj"
    "$PROJECT_ROOT/tests/WitchCityRope.Core.Tests/WitchCityRope.Core.Tests.csproj"
    "$PROJECT_ROOT/tests/integration/WitchCityRope.IntegrationTests.csproj"
    "$PROJECT_ROOT/tests/WitchCityRope.SystemTests/WitchCityRope.SystemTests.csproj"
)

# Human-readable labels, same order as TEST_PROJECTS, used in summary output.
TEST_PROJECT_LABELS=(
    "API Unit Tests (tests/unit/api)"
    "Core Tests (tests/WitchCityRope.Core.Tests)"
    "Integration Tests (tests/integration)"
    "System Tests (tests/WitchCityRope.SystemTests)"
)

TEST_ENVIRONMENT_SKILL="$PROJECT_ROOT/.claude/skills/test-environment/execute.sh"

# React/vitest tests live in tests/unit/web/ and are configured by
# apps/web/vitest.config.ts. Vitest is invoked from the apps/web directory
# (so node_modules and config are picked up correctly).
REACT_TEST_DIR="$PROJECT_ROOT/apps/web"

# ============================================
# PARSE ARGUMENTS
# ============================================

MODE=""
VERBOSE=false
FILTER=""

print_usage() {
    cat <<EOF
Usage: $0 --mode unit|react|e2e|all [--verbose] [--filter NAME]

Options:
  --mode unit    Run .NET unit/integration tests (host dotnet test)
  --mode react   Run React/vitest unit tests (host npx vitest)
  --mode e2e     Run Playwright E2E tests (via test-environment skill)
  --mode all     Run .NET + React + E2E
  --verbose      Detailed test output (verbosity=detailed for dotnet,
                 reporter=verbose for vitest)
  --filter NAME  Filter tests by name. For unit mode this is a dotnet
                 --filter expression (e.g. VettingService or
                 "Category!=HealthCheck"). For react mode it is a
                 positional pattern that vitest matches against test
                 file paths AND test names.

Examples:
  $0 --mode unit                            # All .NET tests
  $0 --mode unit --filter VettingService    # Filtered .NET run
  $0 --mode react                           # All React/vitest tests
  $0 --mode react --filter VettingApps      # Filtered vitest run
  $0 --mode e2e                             # Playwright only
  $0 --mode all --verbose                   # Everything with detail

Requires:
  --mode unit:  dotnet 10.0+ on PATH, Docker running (for Testcontainers)
  --mode react: node + npm/npx in apps/web (managed by Docker dev workflow,
                also runs against host node if installed)
  --mode e2e:   Docker running, test-environment skill present
EOF
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --mode)
            MODE="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            print_usage
            exit 1
            ;;
    esac
done

if [ -z "$MODE" ]; then
    echo "ERROR: Must specify --mode unit, --mode react, --mode e2e, or --mode all"
    echo ""
    print_usage
    exit 1
fi

if [[ "$MODE" != "unit" && "$MODE" != "react" && "$MODE" != "e2e" && "$MODE" != "all" ]]; then
    echo "ERROR: Mode must be 'unit', 'react', 'e2e', or 'all'"
    print_usage
    exit 1
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

if [[ "$MODE" == "unit" || "$MODE" == "all" ]]; then
    # Need dotnet on PATH for .NET tests
    if ! command -v dotnet &> /dev/null; then
        echo "ERROR: dotnet not found on PATH"
        echo "  WCR .NET tests run from the host. Install .NET 10 SDK or add it to PATH."
        echo "  Expected location: /home/chad/.dotnet/dotnet"
        exit 1
    fi

    # Need Docker for Testcontainers to spin up per-test postgres
    if ! docker info &> /dev/null; then
        echo "ERROR: Docker daemon not reachable"
        echo "  .NET tests use Testcontainers.PostgreSql which needs Docker."
        exit 1
    fi
fi

if [[ "$MODE" == "react" || "$MODE" == "all" ]]; then
    # Need npx (comes with node) and the apps/web directory
    if ! command -v npx &> /dev/null; then
        echo "ERROR: npx not found on PATH"
        echo "  React/vitest tests need node + npm. Install Node 20+ or add it to PATH."
        exit 1
    fi
    if [ ! -d "$REACT_TEST_DIR" ]; then
        echo "ERROR: React app directory not found at $REACT_TEST_DIR"
        exit 1
    fi
    if [ ! -f "$REACT_TEST_DIR/vitest.config.ts" ]; then
        echo "ERROR: vitest.config.ts not found at $REACT_TEST_DIR/vitest.config.ts"
        echo "  React mode expects vitest config in apps/web/."
        exit 1
    fi
fi

if [[ "$MODE" == "e2e" || "$MODE" == "all" ]]; then
    if [ ! -f "$TEST_ENVIRONMENT_SKILL" ]; then
        echo "ERROR: test-environment skill not found at $TEST_ENVIRONMENT_SKILL"
        echo "  E2E mode delegates to test-environment skill which is missing."
        exit 1
    fi
fi

# ============================================
# HEADER
# ============================================

echo "WitchCityRope Test Suite Runner"
echo "================================"
echo ""
echo "  Mode: $MODE"
[ "$VERBOSE" = true ] && echo "  Verbose: enabled"
[ -n "$FILTER" ] && echo "  Filter: $FILTER"
echo ""

cd "$PROJECT_ROOT"

# ============================================
# UNIT TEST RUNNER
# ============================================
#
# Runs all three WCR .NET test projects in sequence. Each project runs even if
# an earlier one fails — we want a complete picture of what's broken, not a
# fail-fast. Results are aggregated across projects for the summary.
#
# Counters are accumulated as globals because bash doesn't support returning
# structured data from functions cleanly. The accumulators are:
#   UNIT_TOTAL_PASSED
#   UNIT_TOTAL_FAILED
#   UNIT_TOTAL_SKIPPED
#   UNIT_TOTAL_TOTAL
# Plus UNIT_PROJECT_RESULTS — an array of "LABEL|passed|failed|skipped|total|exit"
# for the per-project summary table.

UNIT_TOTAL_PASSED=0
UNIT_TOTAL_FAILED=0
UNIT_TOTAL_SKIPPED=0
UNIT_TOTAL_TOTAL=0
UNIT_PROJECT_RESULTS=()

# sum_matches: extract numbers from dotnet output using grep -oP \K and sum them.
# Used for parsing dotnet test summary lines, which come in two formats:
#
#   minimal/quiet:
#     Passed!  - Failed:     0, Passed:   167, Skipped:     0, Total:   167, Duration: 5 s
#   normal/detailed:
#     Test Run Successful.
#     Total tests: 167
#          Passed: 167
#
# Both forms use `Passed:`, `Failed:`, `Skipped:`. Only the "Total" line differs:
# minimal uses `Total:` and normal uses `Total tests:`. The regex
# `Total(?:\s+tests)?:\s+\K\d+` handles both.
#
# We SUM across multiple matches via awk rather than taking the last with
# `tail -1`. `tail -1` would silently undercount if dotnet ever printed more
# than one summary block (e.g., `dotnet test` across a solution with multiple
# projects). This function is only called per single-project output, so there's
# normally only one match — but summation is defense in depth.
sum_matches() {
    local input="$1"
    local pattern="$2"
    echo "$input" | grep -oP "$pattern" | awk '{s+=$1} END {print s+0}'
}

run_single_project() {
    local project_path="$1"
    local project_label="$2"

    echo ""
    echo "──────────────────────────────────────────────"
    echo "  $project_label"
    echo "──────────────────────────────────────────────"

    if [ ! -f "$project_path" ]; then
        echo "FAIL: Test project not found: $project_path"
        UNIT_PROJECT_RESULTS+=("$project_label|0|0|0|0|1|MISSING")
        return 1
    fi

    # Build dotnet command as a bash ARRAY, not a string + eval.
    #
    # History: the inventory skill's earlier version stored this as a string
    # `"dotnet test ... --logger console;verbosity=$VERBOSITY"` and ran it via
    # `eval`. That form has a silent, dangerous bug: the unquoted semicolon
    # inside the --logger argument becomes a bash command separator when eval
    # splits the string. The result is:
    #   1. `dotnet test ... --logger console`    (runs, real exit code)
    #   2. `verbosity=normal`                    (shell assignment, exits 0)
    # and `$?` reflects the LAST command (the assignment), so the real dotnet
    # exit code is always lost and the skill reports PASSED even on a broken
    # build with zero tests run.
    #
    # Fix: pass args as a bash array so each element is a literal argv token,
    # no shell interpretation. The safety nets below are kept as defense in
    # depth in case someone "improves" this back to a string form.
    local verbosity="normal"
    if [ "$VERBOSE" = true ]; then
        verbosity="detailed"
    fi

    local dotnet_cmd=(dotnet test "$project_path" --logger "console;verbosity=$verbosity" --nologo)
    if [ -n "$FILTER" ]; then
        dotnet_cmd+=(--filter "$FILTER")
    fi

    echo "  Running: ${dotnet_cmd[*]}"
    echo ""

    # Capture output and exit code. Calling the array directly means bash sees
    # each element as a separate argv word, and the semicolon in the --logger
    # value stays intact where dotnet wants it.
    #
    # set -e is intentionally NOT toggled here — this function is always called
    # from run_unit_tests which wraps the call in `set +e`, so set -e is already
    # ignored for this function body. If we did `set +e; ... ; set -e` inside,
    # the re-enabled `set -e` would leak back to the caller and kill the script
    # when the function returns non-zero, even though the caller wrapped it in
    # set +e to capture the exit code. See bash manual:
    #   "If a compound command or shell function sets -e while executing in a
    #    context where -e is being ignored, that setting will not have any
    #    effect until the compound command or the command containing the
    #    function call completes."
    # We hit this bug on 2026-04-10 during initial baseline — the script exited
    # immediately after the first project FAIL instead of iterating through the
    # other two projects. Leaving the set -e toggle out of the function body is
    # the fix.
    local unit_output unit_exit
    # Use if/then/else (not `|| true`) to capture the real exit code without
    # letting set -e trip. `var=$(cmd) || true` would mask dotnet's exit code
    # because `$?` after `|| true` is always 0 (from `true`). Inside an `if`,
    # command failure doesn't trip set -e and we can capture $? cleanly in the
    # else branch.
    if unit_output=$("${dotnet_cmd[@]}" 2>&1); then
        unit_exit=0
    else
        unit_exit=$?
    fi

    echo "$unit_output"
    echo ""

    # Parse results out of the output.
    local passed failed skipped total
    passed=$(sum_matches "$unit_output" 'Passed:\s+\K\d+')
    failed=$(sum_matches "$unit_output" 'Failed:\s+\K\d+')
    skipped=$(sum_matches "$unit_output" 'Skipped:\s+\K\d+')
    total=$(sum_matches "$unit_output"  'Total(?:\s+tests)?:\s+\K\d+')

    # Defensive defaults — if awk returned empty string (no match), treat as 0
    # so the arithmetic comparisons below never see an unset var.
    passed=${passed:-0}
    failed=${failed:-0}
    skipped=${skipped:-0}
    total=${total:-0}

    # Safety net #1: "exit 0 but all counters zero" forces FAIL.
    # This catches silent test-discovery failures — e.g., a new dotnet output
    # format that breaks the regex, or a --filter that matches zero tests
    # (almost always a typo; loud failure is better than silent false green).
    if [ "$unit_exit" -eq 0 ] && \
       [ "$passed"    -eq 0 ] && \
       [ "$failed"    -eq 0 ] && \
       [ "$skipped"   -eq 0 ] && \
       [ "$total"     -eq 0 ]; then
        echo "[WARN] dotnet test exited 0 but every counter is zero — forcing FAIL"
        echo "       (broken build, silent discovery failure, or --filter matching nothing)"
        unit_exit=1
    fi

    # Safety net #2: any `error CS\d+` in output forces FAIL.
    # dotnet test can exit 0 on edge-case compile errors. This catches them.
    if echo "$unit_output" | grep -qE 'error CS[0-9]+'; then
        echo "[WARN] C# compile errors detected in test output — forcing FAIL"
        unit_exit=1
    fi

    # Accumulate into totals
    UNIT_TOTAL_PASSED=$((UNIT_TOTAL_PASSED + passed))
    UNIT_TOTAL_FAILED=$((UNIT_TOTAL_FAILED + failed))
    UNIT_TOTAL_SKIPPED=$((UNIT_TOTAL_SKIPPED + skipped))
    UNIT_TOTAL_TOTAL=$((UNIT_TOTAL_TOTAL + total))

    local status_word
    if [ "$unit_exit" -eq 0 ]; then
        status_word="OK"
        echo "  [OK] $project_label"
    else
        status_word="FAIL"
        echo "  [FAIL] $project_label"
    fi
    echo "    Passed: $passed | Failed: $failed | Skipped: $skipped | Total: $total"

    UNIT_PROJECT_RESULTS+=("$project_label|$passed|$failed|$skipped|$total|$unit_exit|$status_word")

    return $unit_exit
}

run_unit_tests() {
    echo "Running .NET Unit/Integration Tests"
    echo "===================================="

    local worst_exit=0
    local i
    # Iterate over both arrays using an index, so label and path stay aligned.
    # ${!TEST_PROJECTS[@]} gives indices. Project count is small (3) so this is
    # not a hot loop.
    for i in "${!TEST_PROJECTS[@]}"; do
        local project="${TEST_PROJECTS[$i]}"
        local label="${TEST_PROJECT_LABELS[$i]}"

        # Run the project. We deliberately do NOT fail fast — we want a full
        # picture of what's broken across all three projects, not a bail-out
        # after the first failure.
        set +e
        run_single_project "$project" "$label"
        local this_exit=$?
        set -e

        if [ "$this_exit" -ne 0 ]; then
            worst_exit=1
        fi
    done

    # Per-project summary table
    echo ""
    echo "=============================================="
    echo "  .NET Test Summary"
    echo "=============================================="
    echo ""
    printf "  %-48s %8s %8s %8s %8s %6s\n" "Project" "Passed" "Failed" "Skipped" "Total" "Status"
    printf "  %-48s %8s %8s %8s %8s %6s\n" "------------------------------------------------" "--------" "--------" "--------" "--------" "------"
    for result in "${UNIT_PROJECT_RESULTS[@]}"; do
        # Split the pipe-delimited row. Format: LABEL|passed|failed|skipped|total|exit|status_word
        IFS='|' read -r r_label r_passed r_failed r_skipped r_total r_exit r_status <<< "$result"
        printf "  %-48s %8s %8s %8s %8s %6s\n" "$r_label" "$r_passed" "$r_failed" "$r_skipped" "$r_total" "$r_status"
    done
    printf "  %-48s %8s %8s %8s %8s %6s\n" "------------------------------------------------" "--------" "--------" "--------" "--------" "------"
    printf "  %-48s %8s %8s %8s %8s %6s\n" "TOTALS" "$UNIT_TOTAL_PASSED" "$UNIT_TOTAL_FAILED" "$UNIT_TOTAL_SKIPPED" "$UNIT_TOTAL_TOTAL" ""
    echo ""

    if [ "$worst_exit" -eq 0 ]; then
        echo "  [OK] .NET tests PASSED"
    else
        echo "  [FAIL] .NET tests FAILED"
    fi
    echo ""

    return $worst_exit
}

# ============================================
# REACT/VITEST TEST RUNNER
# ============================================
#
# Runs the WCR React unit tests via vitest. Vitest is invoked from
# apps/web/ so it picks up vitest.config.ts and node_modules. The config
# already points include = ['../../tests/unit/web/**/*.test.{ts,tsx}']
# so we don't need to pass paths.
#
# This was added 2026-04-22 after the first attempt to run React unit
# tests during a vetting admin feature was blocked by
# block-manual-test-runs.py with no skill alternative. Same pattern as the
# .NET runner above: capture output, parse counters, set safety nets.
#
# Vitest output to parse (standard reporter):
#     Test Files  3 passed (3)
#          Tests  47 passed (47)
# When failures exist:
#     Test Files  1 failed | 2 passed (3)
#          Tests  2 failed | 45 passed (47)
# We pull numbers off the "Tests" line for passed/failed/skipped/total,
# falling back to counters of 0 and triggering the safety nets if the
# regex doesn't match anything.

REACT_TOTAL_PASSED=0
REACT_TOTAL_FAILED=0
REACT_TOTAL_SKIPPED=0
REACT_TOTAL_TOTAL=0

run_react_tests() {
    echo "Running React/vitest Unit Tests"
    echo "================================"
    echo ""

    # Build vitest command as a bash array (same rationale as the dotnet
    # command above — see comment in run_single_project for why eval+string
    # is dangerous). `vitest run` is the single-shot, non-watch form.
    local reporter="default"
    if [ "$VERBOSE" = true ]; then
        reporter="verbose"
    fi

    local vitest_cmd=(npx vitest run --reporter="$reporter")
    if [ -n "$FILTER" ]; then
        # Vitest treats positional args as file/test name patterns. Passing
        # the filter as a positional arg matches the user's mental model
        # from --mode unit (where --filter VettingService matches by class
        # name). For vitest, "VettingApplicationsList" matches the test
        # file by path or test name.
        vitest_cmd+=("$FILTER")
    fi

    echo "  Running: ${vitest_cmd[*]} (cwd: apps/web)"
    echo ""

    cd "$REACT_TEST_DIR"

    local react_output react_exit
    if react_output=$("${vitest_cmd[@]}" 2>&1); then
        react_exit=0
    else
        react_exit=$?
    fi

    cd "$PROJECT_ROOT"

    echo "$react_output"
    echo ""

    # Parse the "Tests" summary line. Vitest emits separated counter
    # phrases like "2 failed", "45 passed", "1 skipped", "3 todo".
    # We extract each independently from the line that starts with "Tests".
    local tests_line
    tests_line=$(echo "$react_output" | grep -E '^\s*Tests\s+' | tail -1)

    local passed failed skipped
    passed=$(echo "$tests_line" | grep -oP '\d+(?=\s+passed)' | head -1)
    failed=$(echo "$tests_line" | grep -oP '\d+(?=\s+failed)' | head -1)
    skipped=$(echo "$tests_line" | grep -oP '\d+(?=\s+skipped)' | head -1)

    passed=${passed:-0}
    failed=${failed:-0}
    skipped=${skipped:-0}
    local total=$((passed + failed + skipped))

    # Safety net #1: exit 0 with all counters zero forces FAIL. Catches
    # silent test-discovery failures (bad filter, broken include glob,
    # vitest output format change that breaks our regex).
    if [ "$react_exit" -eq 0 ] && [ "$total" -eq 0 ]; then
        echo "[WARN] vitest exited 0 but no test counters parsed — forcing FAIL"
        echo "       (broken config, --filter matching nothing, or output format change)"
        react_exit=1
    fi

    # Safety net #2: TypeScript compile errors. Vitest sometimes exits 0
    # when transform errors occur in setup files. The phrases below cover
    # both vitest's own error formatting and the underlying esbuild output.
    if echo "$react_output" | grep -qE 'Transform failed|Cannot find module|SyntaxError'; then
        if [ "$react_exit" -eq 0 ]; then
            echo "[WARN] Transform/import errors detected in vitest output — forcing FAIL"
            react_exit=1
        fi
    fi

    REACT_TOTAL_PASSED=$passed
    REACT_TOTAL_FAILED=$failed
    REACT_TOTAL_SKIPPED=$skipped
    REACT_TOTAL_TOTAL=$total

    echo ""
    echo "=============================================="
    echo "  React/vitest Summary"
    echo "=============================================="
    echo ""
    printf "  Passed: %d | Failed: %d | Skipped: %d | Total: %d\n" \
        "$passed" "$failed" "$skipped" "$total"
    echo ""

    if [ "$react_exit" -eq 0 ]; then
        echo "  [OK] React/vitest tests PASSED"
    else
        echo "  [FAIL] React/vitest tests FAILED"
    fi
    echo ""

    return $react_exit
}

# ============================================
# E2E TEST RUNNER
# ============================================
#
# E2E tests delegate to the existing test-environment skill which handles the
# full container lifecycle (build test containers, health checks, Playwright
# via docker exec, cleanup). We do not duplicate that logic here.

run_e2e_tests() {
    echo "Running Playwright E2E Tests (via test-environment skill)"
    echo "=========================================================="
    echo ""

    # Delegate to test-environment skill. Pass --skip-confirm so it doesn't
    # prompt interactively when called from this skill.
    local e2e_exit=0
    set +e
    SKIP_CONFIRMATION=true bash "$TEST_ENVIRONMENT_SKILL" --mode e2e --skip-confirm
    e2e_exit=$?
    set -e

    if [ $e2e_exit -eq 0 ]; then
        echo "  [OK] E2E tests PASSED"
    else
        echo "  [FAIL] E2E tests FAILED"
        if [ -d "$PROJECT_ROOT/test-results" ]; then
            echo "  Test results: $PROJECT_ROOT/test-results/"
        fi
    fi
    echo ""

    return $e2e_exit
}

# ============================================
# EXECUTE TESTS
# ============================================

UNIT_RESULT=0
REACT_RESULT=0
E2E_RESULT=0

if [[ "$MODE" == "unit" || "$MODE" == "all" ]]; then
    set +e
    run_unit_tests
    UNIT_RESULT=$?
    set -e
    echo ""
fi

if [[ "$MODE" == "react" || "$MODE" == "all" ]]; then
    set +e
    run_react_tests
    REACT_RESULT=$?
    set -e
    echo ""
fi

if [[ "$MODE" == "e2e" || "$MODE" == "all" ]]; then
    set +e
    run_e2e_tests
    E2E_RESULT=$?
    set -e
    echo ""
fi

# ============================================
# OVERALL SUMMARY
# ============================================

echo "=============================================="
echo "  Test Suite Summary"
echo "=============================================="
echo ""

OVERALL_STATUS="success"

if [[ "$MODE" == "unit" || "$MODE" == "all" ]]; then
    if [ $UNIT_RESULT -eq 0 ]; then
        echo "  .NET tests:    PASSED ($UNIT_TOTAL_PASSED passed, $UNIT_TOTAL_FAILED failed, $UNIT_TOTAL_SKIPPED skipped)"
    else
        echo "  .NET tests:    FAILED ($UNIT_TOTAL_PASSED passed, $UNIT_TOTAL_FAILED failed, $UNIT_TOTAL_SKIPPED skipped)"
        OVERALL_STATUS="failure"
    fi
fi

if [[ "$MODE" == "react" || "$MODE" == "all" ]]; then
    if [ $REACT_RESULT -eq 0 ]; then
        echo "  React tests:   PASSED ($REACT_TOTAL_PASSED passed, $REACT_TOTAL_FAILED failed, $REACT_TOTAL_SKIPPED skipped)"
    else
        echo "  React tests:   FAILED ($REACT_TOTAL_PASSED passed, $REACT_TOTAL_FAILED failed, $REACT_TOTAL_SKIPPED skipped)"
        OVERALL_STATUS="failure"
    fi
fi

if [[ "$MODE" == "e2e" || "$MODE" == "all" ]]; then
    if [ $E2E_RESULT -eq 0 ]; then
        echo "  E2E tests:     PASSED"
    else
        echo "  E2E tests:     FAILED"
        OVERALL_STATUS="failure"
    fi
fi

echo ""

# Structured result block for programmatic parsing by agents/hooks
echo "=== SKILL_RESULT ==="
cat <<EOF
{
  "skill": "run-test-suite",
  "status": "$OVERALL_STATUS",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "mode": "$MODE",
  "results": {
    "dotnet": {
      "exitCode": $UNIT_RESULT,
      "passed": $UNIT_TOTAL_PASSED,
      "failed": $UNIT_TOTAL_FAILED,
      "skipped": $UNIT_TOTAL_SKIPPED,
      "total": $UNIT_TOTAL_TOTAL
    },
    "react": {
      "exitCode": $REACT_RESULT,
      "passed": $REACT_TOTAL_PASSED,
      "failed": $REACT_TOTAL_FAILED,
      "skipped": $REACT_TOTAL_SKIPPED,
      "total": $REACT_TOTAL_TOTAL
    },
    "e2e": {"exitCode": $E2E_RESULT}
  }
}
EOF
echo "=== END_SKILL_RESULT ==="

if [ "$OVERALL_STATUS" = "failure" ]; then
    exit 1
fi

exit 0
