#!/bin/bash

# WitchCityRope Login Test Script
# This script simulates a login form submission using curl

# Configuration
BASE_URL="http://localhost:8180"
API_ENDPOINT="/api/auth/login"
EMAIL="admin@witchcityrope.com"
PASSWORD="Test123!"

echo "================================================"
echo "WitchCityRope Login Test Script"
echo "================================================"
echo ""

# First, let's check if the server is running
echo "1. Checking if server is accessible..."
HEALTH_CHECK=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/health" 2>/dev/null || echo "000")

if [ "$HEALTH_CHECK" = "000" ]; then
    echo "   ❌ Server is not running at ${BASE_URL}"
    echo "   Please start the application first."
    echo ""
    echo "   You can start it with:"
    echo "   cd src/WitchCityRope.Api && dotnet run"
    exit 1
fi

echo "   ✓ Server is accessible (Status: ${HEALTH_CHECK})"
echo ""

# Attempt to get the login page first (in case there are any tokens)
echo "2. Fetching login page to check for CSRF tokens..."
LOGIN_PAGE=$(curl -s -L "${BASE_URL}/login" -c cookies.txt)

# Check if there's an antiforgery token (common in ASP.NET Core)
if echo "$LOGIN_PAGE" | grep -q "__RequestVerificationToken"; then
    echo "   ✓ Found CSRF token in page"
    CSRF_TOKEN=$(echo "$LOGIN_PAGE" | grep -oP '(?<=__RequestVerificationToken" value=")[^"]*' | head -1)
    echo "   Token: ${CSRF_TOKEN:0:20}..."
else
    echo "   ℹ No CSRF token found (API might not require it)"
fi

echo ""

# Prepare the login request
echo "3. Preparing login request..."
echo "   Email: ${EMAIL}"
echo "   Password: ********"
echo ""

# Create JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
    "email": "${EMAIL}",
    "password": "${PASSWORD}"
}
EOF
)

echo "4. Sending login request to ${BASE_URL}${API_ENDPOINT}..."
echo ""

# Make the login request
RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -b cookies.txt \
    -c cookies.txt \
    -w "\n\nHTTP_STATUS:%{http_code}" \
    -d "${JSON_PAYLOAD}" \
    "${BASE_URL}${API_ENDPOINT}")

# Extract HTTP status code
HTTP_STATUS=$(echo "$RESPONSE" | grep -oP 'HTTP_STATUS:\K\d+')
RESPONSE_BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "Response Status: ${HTTP_STATUS}"
echo "Response Body:"
echo "${RESPONSE_BODY}" | jq . 2>/dev/null || echo "${RESPONSE_BODY}"
echo ""

# Check the result
if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ Login successful!"
    
    # Extract tokens if they exist
    if echo "$RESPONSE_BODY" | grep -q "accessToken"; then
        ACCESS_TOKEN=$(echo "$RESPONSE_BODY" | jq -r '.accessToken' 2>/dev/null)
        REFRESH_TOKEN=$(echo "$RESPONSE_BODY" | jq -r '.refreshToken' 2>/dev/null)
        
        echo ""
        echo "Tokens received:"
        echo "Access Token: ${ACCESS_TOKEN:0:50}..."
        echo "Refresh Token: ${REFRESH_TOKEN:0:50}..."
        
        # Save tokens for future use
        echo "$ACCESS_TOKEN" > access_token.txt
        echo "$REFRESH_TOKEN" > refresh_token.txt
        echo ""
        echo "Tokens saved to access_token.txt and refresh_token.txt"
    fi
    
    # Check cookies
    echo ""
    echo "Cookies received:"
    cat cookies.txt | grep -v "^#" | grep -v "^$" || echo "No cookies set"
    
elif [ "$HTTP_STATUS" = "401" ]; then
    echo "❌ Login failed: Unauthorized (Invalid credentials)"
elif [ "$HTTP_STATUS" = "400" ]; then
    echo "❌ Login failed: Bad Request"
    echo "Check that the email and password format are correct"
else
    echo "❌ Login failed with status: ${HTTP_STATUS}"
fi

echo ""
echo "================================================"
echo "Additional curl commands you can use:"
echo ""
echo "# Basic login request:"
echo "curl -X POST '${BASE_URL}${API_ENDPOINT}' \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}'"
echo ""
echo "# With cookie jar:"
echo "curl -X POST '${BASE_URL}${API_ENDPOINT}' \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -c cookies.txt \\"
echo "  -d '{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}'"
echo ""
echo "# Using saved access token for authenticated requests:"
echo "curl -H 'Authorization: Bearer \$(cat access_token.txt)' '${BASE_URL}/api/user/profile'"
echo ""

# Cleanup
rm -f cookies.txt 2>/dev/null