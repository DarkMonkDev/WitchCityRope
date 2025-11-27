#!/bin/bash
# health-checks.sh - Verify test environment health

set -e

#SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
#PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

# Load colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

check_container_health() {
    local container_name="$1"
    local max_wait="${2:-60}"
    local waited=0

    echo -e "${YELLOW}⏳ Waiting for $container_name to be healthy...${NC}"

    while [ $waited -lt $max_wait ]; do
        local health=$(docker inspect --format='{{.State.Health.Status}}' "$container_name" 2>/dev/null || echo "not_found")

        if [ "$health" = "healthy" ]; then
            echo -e "${GREEN}✅ $container_name is healthy${NC}"
            return 0
        fi

        if [ "$health" = "not_found" ]; then
            echo -e "${RED}❌ Container $container_name not found${NC}"
            return 1
        fi

        sleep 2
        waited=$((waited + 2))
    done

    echo -e "${RED}❌ $container_name failed health check after ${max_wait}s${NC}"
    echo "Container logs:"
    docker logs "$container_name" --tail 50
    return 1
}

check_compilation() {
    local container_name="$1"
    local service_type="$2"

    echo -e "${YELLOW}🔍 Checking $service_type compilation...${NC}"

    local logs=$(docker logs "$container_name" --tail 100 2>&1)

    if echo "$logs" | grep -iq "error\|failed\|exception"; then
        if echo "$logs" | grep -iq "Build succeeded\|Compiled successfully"; then
            echo -e "${GREEN}✅ $service_type compiled successfully${NC}"
            return 0
        else
            echo -e "${RED}❌ $service_type has compilation errors:${NC}"
            echo "$logs" | grep -i "error" | tail -20
            return 1
        fi
    else
        echo -e "${GREEN}✅ $service_type compiled successfully${NC}"
        return 0
    fi
}

check_database_seeded() {
    echo -e "${YELLOW}🔍 Checking database seed data...${NC}"

    local user_count=$(docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml exec -T postgres \
        psql -U postgres -d witchcityrope_test -t -c "SELECT COUNT(*) FROM \"Users\"" 2>/dev/null | tr -d ' ')

    if [ "$user_count" -gt 0 ]; then
        echo -e "${GREEN}✅ Database seeded with $user_count test users${NC}"
        return 0
    else
        echo -e "${RED}❌ Database not seeded (no users found)${NC}"
        return 1
    fi
}

verify_test_environment() {
    local errors=0

    echo -e "${YELLOW}🏥 Verifying test environment health...${NC}"
    echo ""

    # Check PostgreSQL
    check_container_health "witchcity-postgres" 30 || ((errors++))

    # Check API
    check_container_health "witchcity-api" 60 || ((errors++))
    check_compilation "witchcity-api" "API" || ((errors++))

    # Check Web
    check_container_health "witchcity-web" 60 || ((errors++))
    check_compilation "witchcity-web" "Web" || ((errors++))

    # Check database seeded
    sleep 5  # Give migrations time to run
    check_database_seeded || ((errors++))

    echo ""
    if [ $errors -eq 0 ]; then
        echo -e "${GREEN}✅ All health checks passed - ready for testing${NC}"
        return 0
    else
        echo -e "${RED}❌ $errors health check(s) failed${NC}"
        return 1
    fi
}

# If sourced, don't run automatically
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    cd "$PROJECT_ROOT"
    verify_test_environment "$@"
fi
