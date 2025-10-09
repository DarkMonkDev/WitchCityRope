#!/bin/bash
# Export OpenAPI Specification Script
# This script fetches the OpenAPI spec from the running API and saves it to a file

set -e

API_URL="${API_URL:-http://localhost:5655}"
OUTPUT_FILE="${OUTPUT_FILE:-./apps/api/openapi.json}"

echo "🔍 Checking if API is running at ${API_URL}..."
if ! curl -s -f "${API_URL}/health-check" > /dev/null 2>&1; then
    echo "❌ ERROR: API is not running at ${API_URL}"
    echo "   Start the API with: ./dev.sh"
    exit 1
fi

echo "✅ API is running"
echo "📥 Fetching OpenAPI specification..."

# Fetch the OpenAPI spec and save to file
curl -s -f "${API_URL}/swagger/v1/swagger.json" -o "${OUTPUT_FILE}"

if [ $? -eq 0 ]; then
    echo "✅ OpenAPI spec exported to: ${OUTPUT_FILE}"

    # Display spec summary
    ENDPOINT_COUNT=$(jq '.paths | length' "${OUTPUT_FILE}")
    echo "📊 Spec contains ${ENDPOINT_COUNT} endpoint paths"

    # Show sample endpoint
    echo "📝 Sample endpoints:"
    jq -r '.paths | keys | .[]' "${OUTPUT_FILE}" | head -5

    exit 0
else
    echo "❌ Failed to fetch OpenAPI spec from ${API_URL}/swagger/v1/swagger.json"
    exit 1
fi
