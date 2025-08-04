#\!/bin/bash

echo "========================================"
echo "WitchCityRope Integration Tests Report"
echo "========================================"
echo "Date: $(date)"
echo ""

# Set environment variables
export DOTNET_USE_POLLING_FILE_WATCHER=1
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=witchcityrope_test;Username=postgres;Password=WitchCity2024\!;Include Error Detail=true"
export ASPNETCORE_ENVIRONMENT=Development

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track totals
total_tests=0
total_passed=0
total_failed=0
total_skipped=0

echo "Docker Container Status:"
echo "------------------------"
docker ps --filter "name=witchcity" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Function to run tests for a project
run_tests() {
    local project=$1
    local filter=$2
    
    echo ""
    echo "Running: $project"
    echo "Filter: ${filter:-None}"
    echo "----------------------------------------"
    
    # Determine if this project contains integration tests
    if [[ "$project" == *"IntegrationTests"* ]] || [[ "$project" == *"E2E"* ]] || [[ "$filter" == "Category=Integration" ]]; then
        test_type="Integration"
    else
        test_type="Unit"
    fi
    
    echo "Test Type: $test_type"
    
    # Build the test command
    test_cmd="dotnet test \"$project\" --no-build --logger \"console;verbosity=normal\" --logger \"trx;LogFileName=testresults.trx\""
    
    if [ \! -z "$filter" ]; then
        test_cmd="$test_cmd --filter \"$filter\""
    fi
    
    # Run the tests and capture output
    output=$(eval $test_cmd 2>&1)
    exit_code=$?
    
    # Parse the output for test counts
    if [[ $output =~ Total[[:space:]]tests:[[:space:]]([0-9]+) ]]; then
        project_total="${BASH_REMATCH[1]}"
        total_tests=$((total_tests + project_total))
    fi
    
    if [[ $output =~ Passed:[[:space:]]([0-9]+) ]]; then
        project_passed="${BASH_REMATCH[1]}"
        total_passed=$((total_passed + project_passed))
    fi
    
    if [[ $output =~ Failed:[[:space:]]([0-9]+) ]]; then
        project_failed="${BASH_REMATCH[1]}"
        total_failed=$((total_failed + project_failed))
    fi
    
    if [[ $output =~ Skipped:[[:space:]]([0-9]+) ]]; then
        project_skipped="${BASH_REMATCH[1]}"
        total_skipped=$((total_skipped + project_skipped))
    fi
    
    # Extract error details if tests failed
    if [ $exit_code -ne 0 ]; then
        echo -e "${RED}FAILED${NC}"
        echo ""
        echo "Failed Test Details:"
        echo "$output"  < /dev/null |  grep -A 5 "\[FAIL\]" | head -50
        echo ""
        if [[ $(echo "$output" | grep -c "\[FAIL\]") -gt 10 ]]; then
            echo "... (showing first 10 failures)"
        fi
    else
        echo -e "${GREEN}PASSED${NC}"
    fi
    
    # Show summary for this project
    echo ""
    echo "Project Summary: Total: ${project_total:-0}, Passed: ${project_passed:-0}, Failed: ${project_failed:-0}, Skipped: ${project_skipped:-0}"
}

# Run all test projects
echo ""
echo "Test Execution:"
echo "==============="

# Integration Tests
run_tests "tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj"

# E2E Tests  
run_tests "tests/WitchCityRope.E2E.Tests/WitchCityRope.E2E.Tests.csproj"

# API Tests (may contain integration tests)
run_tests "tests/WitchCityRope.Api.Tests/WitchCityRope.Api.Tests.csproj" "Category=Integration"

# Infrastructure Tests (may contain integration tests)
run_tests "tests/WitchCityRope.Infrastructure.Tests/WitchCityRope.Infrastructure.Tests.csproj" "Category=Integration"

# Web Tests (may contain integration tests)
run_tests "tests/WitchCityRope.Web.Tests/WitchCityRope.Web.Tests.csproj" "Category=Integration"

# Overall Summary
echo ""
echo "========================================"
echo "Overall Test Summary"
echo "========================================"
echo -e "Total Tests:  $total_tests"
echo -e "Passed:       ${GREEN}$total_passed${NC}"
echo -e "Failed:       ${RED}$total_failed${NC}"
echo -e "Skipped:      ${YELLOW}$total_skipped${NC}"

if [ $total_failed -eq 0 ] && [ $total_tests -gt 0 ]; then
    echo ""
    echo -e "${GREEN}✓ All integration tests passed!${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}✗ Some integration tests failed.${NC}"
    exit 1
fi
