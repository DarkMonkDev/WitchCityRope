#!/bin/bash

# WitchCityRope Test Runner Script
# This script runs all tests and generates coverage reports

echo "🧪 WitchCityRope Test Runner"
echo "============================"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to run tests for a specific project
run_project_tests() {
    local project=$1
    echo -e "\n${YELLOW}Running tests for $project...${NC}"
    
    if dotnet test "tests/$project" \
        --logger "console;verbosity=minimal" \
        --collect:"XPlat Code Coverage" \
        /p:CoverletOutputFormat=opencover \
        /p:CoverletOutput=../../coverage/ \
        /p:MergeWith=../../coverage/coverage.json; then
        echo -e "${GREEN}✓ $project tests passed${NC}"
        return 0
    else
        echo -e "${RED}✗ $project tests failed${NC}"
        return 1
    fi
}

# Clean previous coverage reports
echo "Cleaning previous coverage reports..."
rm -rf coverage/
mkdir -p coverage

# Build the solution first
echo -e "\n${YELLOW}Building solution...${NC}"
if ! dotnet build --configuration Release; then
    echo -e "${RED}Build failed! Please fix build errors before running tests.${NC}"
    exit 1
fi

# Track overall success
all_passed=true

# Run tests for each project
projects=(
    "WitchCityRope.Core.Tests"
    "WitchCityRope.Api.Tests"
    "WitchCityRope.Infrastructure.Tests"
    "WitchCityRope.Web.Tests"
)

for project in "${projects[@]}"; do
    if ! run_project_tests "$project"; then
        all_passed=false
    fi
done

# Generate coverage report
echo -e "\n${YELLOW}Generating coverage report...${NC}"
if command -v reportgenerator &> /dev/null; then
    reportgenerator \
        -reports:"coverage/**/coverage.opencover.xml" \
        -targetdir:"coverage/report" \
        -reporttypes:"HtmlInline_AzurePipelines;Cobertura"
    echo -e "${GREEN}Coverage report generated in coverage/report/index.html${NC}"
else
    echo -e "${YELLOW}ReportGenerator not found. Install it with:${NC}"
    echo "dotnet tool install -g dotnet-reportgenerator-globaltool"
fi

# Summary
echo -e "\n============================"
if [ "$all_passed" = true ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
    
    # Count total tests
    total_tests=$(dotnet test --list-tests | grep -c "Test:")
    echo -e "Total tests: $total_tests"
    
    exit 0
else
    echo -e "${RED}✗ Some tests failed!${NC}"
    exit 1
fi