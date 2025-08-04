#!/bin/bash

# Test API Endpoints
BASE_URL="http://localhost:5254"

echo "Testing API Endpoints..."
echo "========================"

# Test 1: Get all endpoints
echo -e "\n1. Testing GET /api/healthcheck/endpoints"
curl -X GET "$BASE_URL/api/healthcheck/endpoints" -H "Accept: application/json" | jq .

# Test 2: Get all events
echo -e "\n2. Testing GET /api/events"
curl -X GET "$BASE_URL/api/events" -H "Accept: application/json" | jq .

# Test 3: Get specific event (will fail if no events exist)
echo -e "\n3. Testing GET /api/events/{id}"
curl -X GET "$BASE_URL/api/events/00000000-0000-0000-0000-000000000001" -H "Accept: application/json" | jq .

# Test 4: Get upcoming events
echo -e "\n4. Testing GET /api/events/upcoming"
curl -X GET "$BASE_URL/api/events/upcoming" -H "Accept: application/json" | jq .

echo -e "\n\nNOTE: Authenticated endpoints require a valid JWT token."
echo "To test authenticated endpoints:"
echo "1. POST /api/events - Create event (requires auth)"
echo "2. POST /api/events/{id}/rsvp - RSVP to event (requires auth)"
echo "3. GET /api/user/profile - Get user profile (requires auth)"
echo "4. GET /api/users/me/rsvps - Get user's RSVPs (requires auth)"