#!/bin/bash
# Post-Build OpenAPI Export Hook
# This script is automatically run after API builds to export the OpenAPI spec

set -e

echo "📤 Post-build: Exporting OpenAPI specification..."

# Check if API is running (for development builds)
if curl -s -f "http://localhost:5655/health-check" > /dev/null 2>&1; then
    echo "✅ API detected at http://localhost:5655"

    # Export OpenAPI spec
    curl -s -f "http://localhost:5655/swagger/v1/swagger.json" -o "openapi.json"

    if [ $? -eq 0 ]; then
        echo "✅ OpenAPI spec exported to openapi.json"

        # Count endpoints
        ENDPOINT_COUNT=$(jq '.paths | length' "openapi.json" 2>/dev/null || echo "unknown")
        echo "📊 Spec contains ${ENDPOINT_COUNT} endpoint paths"
    else
        echo "⚠️  Failed to export OpenAPI spec (API may still be starting)"
    fi
else
    echo "ℹ️  API not running, skipping OpenAPI export"
    echo "   Run './dev.sh' to start API and export spec"
fi

echo "✅ Post-build complete"
