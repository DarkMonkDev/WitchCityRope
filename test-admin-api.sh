#!/bin/bash

echo "Testing admin API access with JWT token..."

# Get JWT token from regular login
echo "Step 1: Getting JWT token..."
RESPONSE=$(curl -s -X POST "http://localhost:5653/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@witchcityrope.com","password":"Test123!"}')

TOKEN=$(echo $RESPONSE | jq -r '.token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "Failed to get JWT token"
    echo "Response: $RESPONSE"
    exit 1
fi

echo "Got JWT token: ${TOKEN:0:50}..."

echo ""
echo "Step 2: Testing admin users API with JWT token..."
curl -X GET "http://localhost:5653/api/admin/users" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Accept: application/json" \
    -w "\nHTTP Status: %{http_code}\n" \
    -s

echo ""
echo "Step 3: Testing admin users stats API..."
curl -X GET "http://localhost:5653/api/admin/users/stats" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Accept: application/json" \
    -w "\nHTTP Status: %{http_code}\n" \
    -s