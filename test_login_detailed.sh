#!/bin/bash

echo "=== WitchCityRope Login Test ==="
echo "Testing login functionality at http://localhost:5651/login"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test URL
LOGIN_URL="http://localhost:5651/login"
BASE_URL="http://localhost:5651"

# Test credentials
EMAIL="admin@witchcityrope.com"
PASSWORD="Test123!"

echo "1. Testing if server is accessible..."
if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|301\|302"; then
    echo -e "${GREEN}✓ Server is running${NC}"
else
    echo -e "${RED}✗ Server is not accessible${NC}"
    exit 1
fi

echo ""
echo "2. Fetching login page to check for JavaScript errors..."
echo "----------------------------------------"

# Get the login page and save it
RESPONSE=$(curl -s -w "\n---STATUS_CODE---%{http_code}" "$LOGIN_URL")
STATUS_CODE=$(echo "$RESPONSE" | grep -oP '(?<=---STATUS_CODE---).*')
HTML_CONTENT=$(echo "$RESPONSE" | sed '/---STATUS_CODE---/,$d')

echo "Status Code: $STATUS_CODE"

# Check for common JavaScript error patterns in the HTML
echo ""
echo "3. Analyzing page for JavaScript issues..."
echo "----------------------------------------"

# Save HTML to temp file for analysis
TEMP_FILE="/tmp/login_page_$$.html"
echo "$HTML_CONTENT" > "$TEMP_FILE"

# Check for console.error calls
if grep -i "console\.error" "$TEMP_FILE" > /dev/null; then
    echo -e "${YELLOW}⚠ Found console.error calls in page${NC}"
    grep -n -i "console\.error" "$TEMP_FILE" | head -5
fi

# Check for common error patterns
ERROR_PATTERNS=(
    "uncaught.*error"
    "syntax.*error"
    "reference.*error"
    "type.*error"
    "cannot.*read.*property"
    "undefined.*is.*not"
    "failed.*to.*load"
    "network.*error"
)

for pattern in "${ERROR_PATTERNS[@]}"; do
    if grep -i "$pattern" "$TEMP_FILE" > /dev/null; then
        echo -e "${RED}✗ Found error pattern: $pattern${NC}"
        grep -n -i "$pattern" "$TEMP_FILE" | head -3
    fi
done

# Check for script tags with errors
echo ""
echo "4. Checking script tags..."
echo "----------------------------------------"
SCRIPT_COUNT=$(grep -c "<script" "$TEMP_FILE")
echo "Found $SCRIPT_COUNT script tags"

# Extract and check external script sources
grep -oP '(?<=<script[^>]*src=")[^"]+' "$TEMP_FILE" | while read -r script_src; do
    # Convert relative URLs to absolute
    if [[ ! "$script_src" =~ ^https?:// ]]; then
        if [[ "$script_src" =~ ^/ ]]; then
            script_url="$BASE_URL$script_src"
        else
            script_url="$BASE_URL/$script_src"
        fi
    else
        script_url="$script_src"
    fi
    
    # Check if script is accessible
    script_status=$(curl -s -o /dev/null -w "%{http_code}" "$script_url")
    if [[ "$script_status" == "200" ]]; then
        echo -e "${GREEN}✓ Script loaded: $script_src${NC}"
    else
        echo -e "${RED}✗ Script failed ($script_status): $script_src${NC}"
    fi
done

# Check for Blazor-specific issues (since this is a Blazor app)
echo ""
echo "5. Checking for Blazor-specific issues..."
echo "----------------------------------------"
if grep -q "blazor\." "$TEMP_FILE"; then
    echo "Blazor framework detected"
    
    # Check for Blazor script
    if grep -q "blazor\.server\.js\|blazor\.webassembly\.js" "$TEMP_FILE"; then
        echo -e "${GREEN}✓ Blazor runtime script found${NC}"
    else
        echo -e "${YELLOW}⚠ Blazor runtime script not found${NC}"
    fi
    
    # Check for SignalR (used by Blazor Server)
    if grep -q "signalr" "$TEMP_FILE"; then
        echo -e "${GREEN}✓ SignalR detected (Blazor Server mode)${NC}"
    fi
fi

# Test form submission
echo ""
echo "6. Testing form submission..."
echo "----------------------------------------"

# First, let's check what form fields exist
echo "Looking for form fields..."
FORM_FIELDS=$(grep -oP '(?<=<input[^>]*name=")[^"]+' "$TEMP_FILE" | sort -u)
if [[ -n "$FORM_FIELDS" ]]; then
    echo "Found form fields:"
    echo "$FORM_FIELDS" | sed 's/^/  - /'
else
    echo -e "${YELLOW}⚠ No named input fields found${NC}"
fi

# Check for CSRF token
CSRF_TOKEN=""
if grep -q "__RequestVerificationToken\|csrf\|_token" "$TEMP_FILE"; then
    echo -e "${GREEN}✓ CSRF protection detected${NC}"
    # Try to extract CSRF token
    CSRF_TOKEN=$(grep -oP '(?<=name="__RequestVerificationToken"[^>]*value=")[^"]+' "$TEMP_FILE" | head -1)
    if [[ -n "$CSRF_TOKEN" ]]; then
        echo "  CSRF Token found (length: ${#CSRF_TOKEN})"
    fi
fi

# Try POST request
echo ""
echo "7. Attempting login POST request..."
echo "----------------------------------------"

# Build form data
FORM_DATA="email=$EMAIL&password=$PASSWORD"
if [[ -n "$CSRF_TOKEN" ]]; then
    FORM_DATA="$FORM_DATA&__RequestVerificationToken=$CSRF_TOKEN"
fi

# Make POST request with cookie jar
COOKIE_JAR="/tmp/cookies_$$.txt"
POST_RESPONSE=$(curl -s -L -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -X POST \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "Origin: $BASE_URL" \
    -H "Referer: $LOGIN_URL" \
    -d "$FORM_DATA" \
    -w "\n---HEADERS---\n%{response_code}\n%{url_effective}" \
    "$LOGIN_URL")

# Extract response code and final URL
RESPONSE_INFO=$(echo "$POST_RESPONSE" | sed -n '/---HEADERS---/,$p')
POST_STATUS=$(echo "$RESPONSE_INFO" | sed -n '2p')
FINAL_URL=$(echo "$RESPONSE_INFO" | sed -n '3p')

echo "Response Status: $POST_STATUS"
echo "Final URL: $FINAL_URL"

if [[ "$FINAL_URL" != *"login"* ]] && [[ "$POST_STATUS" == "200" || "$POST_STATUS" == "302" ]]; then
    echo -e "${GREEN}✓ Login appears successful (redirected away from login page)${NC}"
else
    echo -e "${YELLOW}⚠ Login may have failed (still on login page or error status)${NC}"
fi

# Check cookies
echo ""
echo "8. Checking authentication cookies..."
echo "----------------------------------------"
if [[ -f "$COOKIE_JAR" ]]; then
    COOKIE_COUNT=$(grep -c -E "^$BASE_URL" "$COOKIE_JAR" 2>/dev/null || echo 0)
    if [[ "$COOKIE_COUNT" -gt 0 ]]; then
        echo -e "${GREEN}✓ $COOKIE_COUNT cookie(s) set${NC}"
        grep -E "^$BASE_URL" "$COOKIE_JAR" | awk '{print "  - " $6 "=" substr($7, 1, 20) "..."}'
    else
        echo -e "${YELLOW}⚠ No cookies set${NC}"
    fi
fi

# Clean up
rm -f "$TEMP_FILE" "$COOKIE_JAR"

echo ""
echo "=== Test Complete ==="
echo ""
echo "Summary:"
echo "- Server is accessible: YES"
echo "- JavaScript errors in page: Check output above"
echo "- Form submission tested: YES"
echo "- Authentication attempted: YES"
echo ""
echo "To perform a full browser-based test with console monitoring,"
echo "open browser_test.html in a web browser."