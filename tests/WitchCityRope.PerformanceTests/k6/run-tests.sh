#!/bin/bash

# K6 Performance Test Runner
# Usage: ./run-tests.sh [test-name] [environment]

TEST_NAME=${1:-"auth-load-test"}
ENVIRONMENT=${2:-"local"}
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
REPORT_DIR="reports/${TIMESTAMP}"

# Create report directory
mkdir -p ${REPORT_DIR}

# Load environment variables from config
if [ "$ENVIRONMENT" == "local" ]; then
    export BASE_URL="http://localhost:5651"
elif [ "$ENVIRONMENT" == "staging" ]; then
    export BASE_URL="https://staging.witchcityrope.com"
elif [ "$ENVIRONMENT" == "production" ]; then
    export BASE_URL="https://api.witchcityrope.com"
fi

echo "Running ${TEST_NAME} against ${BASE_URL}..."

# Run the test
k6 run \
    --out json=${REPORT_DIR}/results.json \
    --out csv=${REPORT_DIR}/results.csv \
    --summary-export=${REPORT_DIR}/summary.json \
    ${TEST_NAME}.js

# Generate HTML report if test completed
if [ $? -eq 0 ]; then
    echo "Test completed successfully. Reports saved to ${REPORT_DIR}"
    
    # Convert JSON to HTML report (requires additional tooling)
    # k6-reporter ${REPORT_DIR}/summary.json -o ${REPORT_DIR}/report.html
else
    echo "Test failed. Check logs for details."
    exit 1
fi