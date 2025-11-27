#!/bin/bash
# cleanup.sh - Test Environment Cleanup
# Removes all test containers, images, volumes to prevent orphans

set -e

#SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
#PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

# Load colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

cleanup_test_environment() {
    local keep_images="${1:-false}"
    local keep_containers="${2:-false}"

    echo -e "${YELLOW}🧹 Cleaning up test environment...${NC}"

    cd "$PROJECT_ROOT"

    # Stop containers
    if [ "$keep_containers" = "false" ]; then
        echo "  Stopping test containers..."
        docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml down 2>/dev/null || true

        # Remove containers
        echo "  Removing test containers..."
        docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml rm -f 2>/dev/null || true

        # Remove volumes (fresh database next run)
        echo "  Removing test volumes..."
        docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml down -v 2>/dev/null || true
    else
        echo "  Keeping containers for debugging (--keep-containers flag)"
    fi

    # Remove test images (prevent orphans)
    if [ "$keep_images" = "false" ]; then
        echo "  Removing test images..."
        docker images --filter "label=witchcity.test=true" -q | xargs -r docker rmi -f 2>/dev/null || true

        # Prune dangling images
        echo "  Pruning dangling images..."
        docker image prune -f >/dev/null 2>&1 || true
    else
        echo "  Keeping images for faster reruns (--keep-images flag)"
    fi

    # Remove test networks if isolated
    echo "  Cleaning up test networks..."
    docker network prune -f >/dev/null 2>&1 || true

    echo -e "${GREEN}✅ Test environment cleaned${NC}"
}

# If sourced, don't run automatically
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    cleanup_test_environment "$@"
fi
