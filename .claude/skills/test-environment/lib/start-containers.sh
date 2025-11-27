#!/bin/bash
# start-containers.sh - Start test containers

set -e

#SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
#PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

# Load colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

start_test_containers() {
    echo -e "${YELLOW}🚀 Starting test containers...${NC}"

    cd "$PROJECT_ROOT"

    # Start containers in detached mode (--force-recreate to avoid ContainerConfig errors)
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml up -d --force-recreate

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Test containers started${NC}"

        # Show running containers
        echo ""
        echo "Running test containers:"
        docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml ps

        return 0
    else
        echo -e "${RED}❌ Failed to start test containers${NC}"
        return 1
    fi
}

# If sourced, don't run automatically
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    start_test_containers "$@"
fi
