#!/bin/bash

echo "Testing Web Authentication Flow..."

# Get the login page first to extract any anti-forgery tokens
echo "1. Getting login page..."
LOGIN_PAGE=$(curl -s -c cookies.txt "http://localhost:5651/Identity/Account/Login")

# Extract the anti-forgery token if present
ANTIFORGERY_TOKEN=$(echo "$LOGIN_PAGE" | grep -o 'name="__RequestVerificationToken" value="[^"]*"' | cut -d'"' -f4)

if [ -n "$ANTIFORGERY_TOKEN" ]; then
    echo "Found anti-forgery token: ${ANTIFORGERY_TOKEN:0:20}..."
else
    echo "No anti-forgery token found"
fi

# Attempt to log in
echo "2. Attempting login..."
LOGIN_RESPONSE=$(curl -s -b cookies.txt -c cookies.txt -L \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "EmailOrUsername=admin@witchcityrope.com&Password=Test123!&RememberMe=false&__RequestVerificationToken=$ANTIFORGERY_TOKEN" \
    "http://localhost:5651/Identity/Account/Login")

# Check if we're redirected to a dashboard or home page (successful login)
if echo "$LOGIN_RESPONSE" | grep -q "Dashboard\|Welcome\|Admin\|User Management"; then
    echo "✅ Login appears successful!"
    echo "3. Checking for JWT token logs..."
    
    # Wait a moment for logs to appear
    sleep 2
    
    # Check recent logs for JWT token acquisition
    docker logs witchcity-web --since 30s 2>&1 | grep -i "cookie signingin\|jwt token\|user logged in" | tail -10
    
else
    echo "❌ Login failed or unexpected response"
    echo "Response contains: $(echo "$LOGIN_RESPONSE" | grep -o "title.*title" | head -1)"
fi

# Clean up
rm -f cookies.txt