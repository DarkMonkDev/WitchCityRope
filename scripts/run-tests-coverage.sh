#!/bin/bash

# WitchCityRope Test Runner with Coverage
# This script runs all tests and generates a coverage report

set -e

echo "================================================"
echo "WitchCityRope Test Runner with Coverage"
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI is not installed${NC}"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo -e "Using .NET SDK: ${GREEN}$DOTNET_VERSION${NC}"

# Clean previous test results
echo -e "\n${YELLOW}Cleaning previous test results...${NC}"
rm -rf TestResults CoverageReport

# Restore packages
echo -e "\n${YELLOW}Restoring packages...${NC}"
dotnet restore

# Build solution
echo -e "\n${YELLOW}Building solution...${NC}"
dotnet build --no-restore --configuration Release

# Run tests with coverage
echo -e "\n${YELLOW}Running tests with coverage...${NC}"
dotnet test --no-build --configuration Release \
    --logger "trx;LogFileName=test-results.trx" \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Check if tests passed
if [ $? -eq 0 ]; then
    echo -e "\n${GREEN}All tests passed!${NC}"
else
    echo -e "\n${RED}Some tests failed!${NC}"
    exit 1
fi

# Check if reportgenerator is installed
if ! command -v reportgenerator &> /dev/null; then
    echo -e "\n${YELLOW}Installing ReportGenerator tool...${NC}"
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate coverage report
echo -e "\n${YELLOW}Generating coverage report...${NC}"
reportgenerator \
    -reports:"TestResults/**/coverage.opencover.xml" \
    -targetdir:"CoverageReport" \
    -reporttypes:"Html;Cobertura;TextSummary;Badges" \
    -verbosity:"Info" \
    -title:"WitchCityRope Coverage Report" \
    -tag:"$(date +%Y%m%d_%H%M%S)"

# Display coverage summary
echo -e "\n${YELLOW}Coverage Summary:${NC}"
cat CoverageReport/Summary.txt

# Check coverage thresholds
LINE_COVERAGE=$(grep -oP 'Line coverage: \K[0-9.]+' CoverageReport/Summary.txt || echo "0")
BRANCH_COVERAGE=$(grep -oP 'Branch coverage: \K[0-9.]+' CoverageReport/Summary.txt || echo "0")

# Compare with thresholds (60%)
THRESHOLD=60
if (( $(echo "$LINE_COVERAGE < $THRESHOLD" | bc -l) )); then
    echo -e "\n${RED}Warning: Line coverage ($LINE_COVERAGE%) is below threshold ($THRESHOLD%)${NC}"
fi

if (( $(echo "$BRANCH_COVERAGE < $THRESHOLD" | bc -l) )); then
    echo -e "\n${RED}Warning: Branch coverage ($BRANCH_COVERAGE%) is below threshold ($THRESHOLD%)${NC}"
fi

# Open report in browser (if available)
if command -v xdg-open &> /dev/null; then
    echo -e "\n${GREEN}Opening coverage report in browser...${NC}"
    xdg-open CoverageReport/index.html
elif command -v open &> /dev/null; then
    echo -e "\n${GREEN}Opening coverage report in browser...${NC}"
    open CoverageReport/index.html
else
    echo -e "\n${GREEN}Coverage report generated at: CoverageReport/index.html${NC}"
fi

echo -e "\n${GREEN}Test run completed successfully!${NC}"