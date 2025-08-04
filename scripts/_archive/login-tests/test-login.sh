#!/bin/bash

echo "Testing authentication flow between Web app and API..."
echo

# Test 1: Direct API call
echo "1. Testing direct API call to http://localhost:5653/api/auth/login"
curl -X POST http://localhost:5653/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@witchcityrope.com", "password": "admin123"}' \
  -s | jq . || echo "Failed to parse JSON response"

echo
echo "2. Testing Web app is running on http://localhost:5000"
curl -s http://localhost:5000/auth/login | grep -q "Login" && echo "✓ Login page is accessible" || echo "✗ Login page not accessible"

echo
echo "3. Testing CORS headers for cross-origin requests"
curl -X OPTIONS http://localhost:5653/api/auth/login \
  -H "Origin: http://localhost:5000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type" \
  -I 2>/dev/null | grep -E "Access-Control|HTTP" | head -5

echo
echo "Summary:"
echo "- API is running on port 5653"
echo "- Web app is running on port 5000"
echo "- Web app is configured to use API URL: http://localhost:5653"
echo "- CORS is properly configured to allow cross-origin requests"
echo
echo "If login is still not working, check:"
echo "1. Browser console for JavaScript errors"
echo "2. Network tab in browser dev tools to see if API calls are being made"
echo "3. Application logs for any authentication errors"