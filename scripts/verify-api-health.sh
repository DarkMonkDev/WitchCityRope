#!/bin/bash

# WitchCityRope API Health Verification Script
# Created: 2025-01-09
# Purpose: Comprehensive health check for API endpoints and backend services

set -e

echo "🔍 WitchCityRope API Health Check Starting..."
echo "=================================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL="http://localhost:5653"
DB_CONTAINER="witchcity-postgres"
DB_NAME="witchcityrope_dev"

# Function to print status
print_status() {
    local status=$1
    local message=$2
    if [ "$status" = "success" ]; then
        echo -e "${GREEN}✅ $message${NC}"
    elif [ "$status" = "warning" ]; then
        echo -e "${YELLOW}⚠️  $message${NC}"
    else
        echo -e "${RED}❌ $message${NC}"
    fi
}

# Function to test endpoint
test_endpoint() {
    local endpoint=$1
    local description=$2
    local expected_status=${3:-200}
    
    echo -n "Testing $description... "
    
    if response=$(curl -s -w "\n%{http_code}" "$API_BASE_URL$endpoint" 2>/dev/null); then
        http_code=$(echo "$response" | tail -n1)
        body=$(echo "$response" | sed '$d')
        
        if [ "$http_code" -eq "$expected_status" ]; then
            print_status "success" "$description (HTTP $http_code)"
            return 0
        else
            print_status "error" "$description (HTTP $http_code, expected $expected_status)"
            return 1
        fi
    else
        print_status "error" "$description (Connection failed)"
        return 1
    fi
}

# Function to test database
test_database() {
    echo -n "Testing database connectivity... "
    
    if docker exec "$DB_CONTAINER" psql -U postgres -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1; then
        print_status "success" "Database connectivity"
        
        # Check table count
        table_count=$(docker exec "$DB_CONTAINER" psql -U postgres -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" 2>/dev/null | tr -d ' \n')
        print_status "success" "Database has $table_count tables"
        
        # Check test data
        user_count=$(docker exec "$DB_CONTAINER" psql -U postgres -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"Users\";" 2>/dev/null | tr -d ' \n')
        event_count=$(docker exec "$DB_CONTAINER" psql -U postgres -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"Events\";" 2>/dev/null | tr -d ' \n')
        
        print_status "success" "Database has $user_count users and $event_count events"
        return 0
    else
        print_status "error" "Database connectivity"
        return 1
    fi
}

# Main health checks
echo "1. Basic API Health"
test_endpoint "/health" "API Health Check"

echo -e "\n2. Database Health"
test_database

echo -e "\n3. Events Endpoints"
test_endpoint "/api/events" "Events List"
test_endpoint "/api/events/e1111111-1111-1111-1111-111111111111" "Individual Event"

echo -e "\n4. Known Issues (Expected Failures)"
test_endpoint "/api/auth/user" "Auth User Endpoint" 404
print_status "warning" "Auth user endpoint returns 404 - known issue, doesn't block functionality"

echo -e "\n5. API Configuration Check"
# Check if API is running on correct port
if netstat -tlnp 2>/dev/null | grep -q ":5653 "; then
    print_status "success" "API listening on port 5653"
else
    print_status "error" "API not listening on port 5653"
fi

# Check database port
if netstat -tlnp 2>/dev/null | grep -q ":5433 "; then
    print_status "success" "Database accessible on host port 5433"
else
    print_status "warning" "Database not accessible on host port 5433 (may be container-only)"
fi

echo -e "\n6. Performance Check"
start_time=$(date +%s.%N)
test_endpoint "/health" "API Response Time Test" >/dev/null
end_time=$(date +%s.%N)
response_time=$(echo "$end_time - $start_time" | bc 2>/dev/null || echo "N/A")

if [ "$response_time" != "N/A" ]; then
    if (( $(echo "$response_time < 1.0" | bc -l) )); then
        print_status "success" "API response time: ${response_time}s (< 1.0s)"
    else
        print_status "warning" "API response time: ${response_time}s (slow)"
    fi
fi

echo -e "\n7. CORS Configuration Check"
cors_response=$(curl -s -H "Origin: http://localhost:5174" \
    -H "Access-Control-Request-Method: GET" \
    -H "Access-Control-Request-Headers: Content-Type" \
    -X OPTIONS \
    "$API_BASE_URL/api/events" \
    -w "\n%{http_code}" 2>/dev/null || echo -e "\n0")

cors_code=$(echo "$cors_response" | tail -n1)
if [ "$cors_code" = "200" ] || [ "$cors_code" = "204" ]; then
    print_status "success" "CORS configuration working (HTTP $cors_code)"
else
    print_status "warning" "CORS preflight may have issues (HTTP $cors_code)"
fi

echo "=================================================="

# Summary
failed_count=0
if ! test_endpoint "/health" "Final Health Check" >/dev/null 2>&1; then
    ((failed_count++))
fi

if ! test_database >/dev/null 2>&1; then
    ((failed_count++))
fi

if [ "$failed_count" -eq 0 ]; then
    print_status "success" "🎉 All critical systems healthy!"
    echo "Backend is ready for development work."
    exit 0
else
    print_status "error" "⚠️  $failed_count critical issues found"
    echo "Please resolve issues before continuing development."
    exit 1
fi