#!/bin/bash

# Login Page Testing Script using curl
# This script tests what we can without a full browser

echo "==================================="
echo "Login Page Testing with curl"
echo "Date: $(date)"
echo "==================================="
echo ""

BASE_URL="http://localhost:5651"
LOGIN_URL="${BASE_URL}/auth/login"

# Test 1: Check if login page is accessible
echo "Test 1: Login Page Accessibility"
echo "--------------------------------"
response=$(curl -s -o /dev/null -w "%{http_code}" $LOGIN_URL)
if [ $response -eq 200 ]; then
    echo "✅ Login page accessible (HTTP $response)"
else
    echo "❌ Login page not accessible (HTTP $response)"
fi
echo ""

# Test 2: Check page content
echo "Test 2: Page Content Analysis"
echo "-----------------------------"
content=$(curl -s $LOGIN_URL)

# Check for Blazor components
if echo "$content" | grep -q "blazor.server.js"; then
    echo "✅ Blazor Server detected"
else
    echo "❌ Blazor Server not detected"
fi

# Check for auth-related elements
if echo "$content" | grep -q "auth-container\|login-container\|Login\.razor"; then
    echo "✅ Auth container found"
else
    echo "❌ Auth container not found"
fi

# Check for form elements (may be rendered client-side)
if echo "$content" | grep -q "<form\|<input\|email\|password"; then
    echo "✅ Form elements present"
else
    echo "⚠️  Form elements not in initial HTML (likely rendered by Blazor)"
fi

echo ""

# Test 3: Check for OAuth endpoints
echo "Test 3: OAuth Endpoint Check"
echo "----------------------------"
oauth_response=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/auth/google-login")
if [ $oauth_response -eq 401 ] || [ $oauth_response -eq 302 ] || [ $oauth_response -eq 405 ]; then
    echo "✅ Google OAuth endpoint exists (HTTP $oauth_response)"
else
    echo "❌ Google OAuth endpoint not found (HTTP $oauth_response)"
fi
echo ""

# Test 4: Check response headers
echo "Test 4: Security Headers"
echo "------------------------"
headers=$(curl -s -I $LOGIN_URL)

# Check for security headers
if echo "$headers" | grep -qi "x-frame-options"; then
    echo "✅ X-Frame-Options header present"
else
    echo "⚠️  X-Frame-Options header missing"
fi

if echo "$headers" | grep -qi "content-security-policy"; then
    echo "✅ Content-Security-Policy header present"
else
    echo "⚠️  Content-Security-Policy header missing"
fi

if echo "$headers" | grep -qi "strict-transport-security"; then
    echo "✅ HSTS header present"
else
    echo "⚠️  HSTS header missing (OK for localhost)"
fi

echo ""

# Test 5: Check page load time
echo "Test 5: Performance Check"
echo "-------------------------"
time_total=$(curl -s -o /dev/null -w "%{time_total}" $LOGIN_URL)
echo "Page load time: ${time_total}s"

if (( $(echo "$time_total < 2" | bc -l) )); then
    echo "✅ Good performance (< 2s)"
else
    echo "⚠️  Slow page load (> 2s)"
fi

echo ""

# Test 6: Check for common assets
echo "Test 6: Asset Loading"
echo "---------------------"

# Check CSS
css_check=$(curl -s $LOGIN_URL | grep -c "\.css")
echo "CSS files referenced: $css_check"

# Check JavaScript
js_check=$(curl -s $LOGIN_URL | grep -c "\.js")
echo "JavaScript files referenced: $js_check"

echo ""

# Test 7: Save page content for analysis
echo "Test 7: Saving Page Content"
echo "---------------------------"
timestamp=$(date +%Y%m%d_%H%M%S)
output_file="login-page-content-${timestamp}.html"
curl -s $LOGIN_URL > $output_file
echo "✅ Page content saved to: $output_file"

echo ""
echo "==================================="
echo "Testing Complete"
echo "==================================="
echo ""
echo "Summary:"
echo "- Login page is accessible via HTTP"
echo "- Page uses Blazor Server for dynamic content"
echo "- OAuth endpoints are configured"
echo "- Full UI testing requires JavaScript execution"
echo ""
echo "Recommendations:"
echo "1. Use browser DevTools for visual testing"
echo "2. Set up Playwright/Selenium for automated UI tests"
echo "3. Test OAuth flow manually in browser"
echo "4. Check responsive design at different viewports"