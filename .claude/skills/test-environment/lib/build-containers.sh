#!/bin/bash
# build-containers.sh - Build test container images from current codebase

set -e

#SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
#PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

# Load colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

build_test_images() {
    echo -e "${YELLOW}🔨 Building test container images from current codebase...${NC}"

    cd "$PROJECT_ROOT"

    # Set build timestamp for image labeling
    export BUILD_TIMESTAMP=$(date +%s)

    # Build test images with proper labels
    echo "  Building images (this may take a few minutes)..."
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml build \
        --build-arg BUILD_TIMESTAMP="$BUILD_TIMESTAMP" \
        2>&1 | grep -v "^#" | grep -v "^DEPRECATED" || true

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Test images built successfully${NC}"
        return 0
    else
        echo -e "${RED}❌ Failed to build test images${NC}"
        return 1
    fi
}

# If sourced, don't run automatically
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    build_test_images "$@"
fi
