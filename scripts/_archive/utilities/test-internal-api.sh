#!/bin/bash

echo "Testing API connectivity from inside Web container"
echo "=================================================="

# Test 1: Check if API is reachable from web container using internal name
echo -e "\n1. Testing internal API connectivity (http://api:8080)..."
docker-compose exec -T web curl -s http://api:8080/health || echo "Failed to reach API via internal name"

# Test 2: Test API login from inside web container
echo -e "\n\n2. Testing API login from inside web container..."
docker-compose exec -T web curl -s -X POST http://api:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@witchcityrope.com", "password": "Test123!"}' | head -c 100

echo -e "\n\n3. Checking Web container environment..."
docker-compose exec -T web printenv | grep -E "(ApiUrl|ASPNETCORE_ENVIRONMENT)"

echo -e "\n\nTest complete!"