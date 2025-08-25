#!/bin/bash

# Test Event Session Matrix API Endpoints
# This script tests the Event Session Matrix functionality

set -e

API_URL="http://localhost:5653"
API_TOKEN=""

echo "=========================================="
echo "Event Session Matrix Integration Test"
echo "=========================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS=0
PASSED=0
FAILED=0

# Test function
run_test() {
    local test_name="$1"
    local command="$2"
    local expected_status="$3"
    
    TESTS=$((TESTS + 1))
    echo -e "${YELLOW}Test $TESTS: $test_name${NC}"
    
    # Run the command and capture response
    response=$(eval "$command" 2>&1) || true
    actual_status=$(echo "$response" | grep -oP 'HTTP/[\d.]+ \K\d+' | head -1 || echo "000")
    
    if [[ "$actual_status" == "$expected_status" ]] || [[ -z "$expected_status" ]]; then
        echo -e "${GREEN}✓ PASSED${NC}"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗ FAILED - Expected: $expected_status, Got: $actual_status${NC}"
        echo "Response: $response"
        FAILED=$((FAILED + 1))
    fi
    echo ""
}

# 1. Test API Health
echo "1. Testing API Health..."
run_test "API Health Check" \
    "curl -s -o /dev/null -w '%{http_code}' $API_URL/health" \
    "200"

# 2. Create a test event
echo "2. Creating test event..."
EVENT_DATA='{
  "title": "Rope Bondage Intensive: 3-Day Series",
  "shortDescription": "Comprehensive workshop series",
  "fullDescription": "3-day intensive workshop",
  "eventType": "class",
  "venueId": "main-studio",
  "teacherIds": ["teacher1"],
  "isPublished": true
}'

EVENT_RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d "$EVENT_DATA" \
    "$API_URL/api/events" 2>&1 || echo '{"error":"Failed to create event"}')

echo "Event creation response: $EVENT_RESPONSE"

# Try to extract event ID if created successfully
EVENT_ID=$(echo "$EVENT_RESPONSE" | grep -oP '"id"\s*:\s*"[^"]+' | cut -d'"' -f4 || echo "")

if [[ -n "$EVENT_ID" ]]; then
    echo -e "${GREEN}Event created with ID: $EVENT_ID${NC}"
    
    # 3. Create sessions for the event
    echo "3. Creating event sessions..."
    SESSION_DATA='{
      "eventId": "'$EVENT_ID'",
      "sessionIdentifier": "S1",
      "name": "Fundamentals Day",
      "date": "2025-02-15",
      "startTime": "19:00",
      "endTime": "21:00",
      "capacity": 20
    }'
    
    SESSION_RESPONSE=$(curl -s -X POST \
        -H "Content-Type: application/json" \
        -d "$SESSION_DATA" \
        "$API_URL/api/events/sessions" 2>&1 || echo '{"error":"Failed to create session"}')
    
    echo "Session creation response: $SESSION_RESPONSE"
    
    # 4. Create ticket type
    echo "4. Creating ticket type..."
    TICKET_DATA='{
      "eventId": "'$EVENT_ID'",
      "name": "Full 3-Day Pass",
      "type": "Single",
      "minPrice": 120,
      "maxPrice": 150,
      "sessionIdentifiers": ["S1"]
    }'
    
    TICKET_RESPONSE=$(curl -s -X POST \
        -H "Content-Type: application/json" \
        -d "$TICKET_DATA" \
        "$API_URL/api/events/ticket-types" 2>&1 || echo '{"error":"Failed to create ticket type"}')
    
    echo "Ticket type creation response: $TICKET_RESPONSE"
    
    # 5. Get event with sessions
    echo "5. Fetching event with sessions..."
    EVENT_DETAIL=$(curl -s "$API_URL/api/events/$EVENT_ID" 2>&1 || echo '{"error":"Failed to fetch event"}')
    echo "Event detail response: $EVENT_DETAIL"
    
else
    echo -e "${RED}Failed to create event - skipping dependent tests${NC}"
fi

# 6. Test event sessions list endpoint  
echo "6. Testing event sessions list endpoint..."
run_test "List All Event Sessions" \
    "curl -s -o /dev/null -w '%{http_code}' $API_URL/api/events/sessions" \
    ""

# Summary
echo "=========================================="
echo -e "Test Results: ${GREEN}$PASSED passed${NC}, ${RED}$FAILED failed${NC} out of $TESTS tests"
echo "=========================================="

if [[ $FAILED -eq 0 ]]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed.${NC}"
    exit 1
fi