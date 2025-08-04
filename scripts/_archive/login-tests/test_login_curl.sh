#!/bin/bash

echo "Testing login functionality on http://localhost:5651"
echo "================================================"

# Step 1: Check if the server is running
echo -e "\nStep 1: Checking if server is running..."
if curl -s -o /dev/null -w "%{http_code}" http://localhost:5651 | grep -q "200\|301\|302"; then
    echo "✓ Server is responding"
else
    echo "✗ Server is not responding at http://localhost:5651"
    exit 1
fi

# Step 2: Get the initial page and look for navigation
echo -e "\nStep 2: Fetching initial page..."
INITIAL_PAGE=$(curl -s http://localhost:5651)

echo "Looking for navigation elements..."
echo "$INITIAL_PAGE" | grep -i "login" | head -5
echo "$INITIAL_PAGE" | grep -i "nav" | head -5

# Step 3: Look for login form or login page
echo -e "\nStep 3: Looking for login endpoint..."

# Check common login endpoints
for endpoint in "/login" "/signin" "/auth/login" "/user/login" "/account/login"; do
    RESPONSE_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651$endpoint)
    if [ "$RESPONSE_CODE" == "200" ] || [ "$RESPONSE_CODE" == "301" ] || [ "$RESPONSE_CODE" == "302" ]; then
        echo "✓ Found login endpoint at: $endpoint (HTTP $RESPONSE_CODE)"
        LOGIN_ENDPOINT=$endpoint
        break
    fi
done

if [ -z "$LOGIN_ENDPOINT" ]; then
    echo "Could not find a login endpoint"
    # Try to find login form in the main page
    echo -e "\nChecking for login form in main page..."
    echo "$INITIAL_PAGE" | grep -i "form" | grep -i "login" | head -5
fi

# Step 4: Attempt login with provided credentials
echo -e "\nStep 4: Attempting login..."

# Try different login methods
echo "Trying POST to /login with JSON..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5651/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' \
    -c cookies.txt \
    -D headers.txt)

# Check response headers
echo -e "\nResponse headers:"
cat headers.txt | head -10

# Check for redirect or success indicators
if grep -qi "location:" headers.txt; then
    REDIRECT_URL=$(grep -i "location:" headers.txt | cut -d' ' -f2 | tr -d '\r')
    echo "✓ Login redirected to: $REDIRECT_URL"
fi

# Step 5: Test authenticated request
echo -e "\nStep 5: Testing authenticated request..."
AUTH_PAGE=$(curl -s http://localhost:5651 -b cookies.txt)

echo "Checking for authenticated navigation elements..."
echo "$AUTH_PAGE" | grep -i "dashboard" | head -3
echo "$AUTH_PAGE" | grep -i "logout" | head -3
echo "$AUTH_PAGE" | grep -i "profile" | head -3

# Clean up
rm -f cookies.txt headers.txt

echo -e "\nTest complete."