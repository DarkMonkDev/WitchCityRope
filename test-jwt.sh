#!/bin/bash

echo "Testing JWT service-token endpoint..."

RESPONSE=$(curl -s -X POST http://localhost:5653/api/auth/service-token \
  -H "Content-Type: application/json" \
  -H "X-Service-Secret: DevSecret-WitchCityRope-ServiceToService-Auth-2024!" \
  -d '{"userId": "34b43f88-3f71-41c0-b84d-aaf577637b5c", "email": "admin@witchcityrope.com"}')

echo "Response: $RESPONSE"

# Check if response contains a token
if echo "$RESPONSE" | grep -q "token"; then
    echo "✅ JWT token endpoint is working!"
    
    # Extract and display the token
    TOKEN=$(echo "$RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    echo "Token (first 50 chars): ${TOKEN:0:50}..."
else
    echo "❌ JWT token endpoint failed"
    echo "Full response: $RESPONSE"
fi