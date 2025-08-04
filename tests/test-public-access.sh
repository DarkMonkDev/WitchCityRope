#!/bin/bash
# Test script to verify public access fixes

echo "=== Public Access Test Report ==="
echo "Date: $(date)"
echo "================================"

# Test public routes
echo -e "\n1. Testing Public Routes (Should be accessible without authentication):"

# Home page
echo -n "   - Home page (/): "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651/)
if [ "$STATUS" = "200" ]; then
    echo "✅ PASS (HTTP $STATUS)"
else
    echo "❌ FAIL (HTTP $STATUS)"
fi

# Events page
echo -n "   - Events page (/events): "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651/events)
if [ "$STATUS" = "200" ]; then
    echo "✅ PASS (HTTP $STATUS)"
else
    echo "❌ FAIL (HTTP $STATUS)"
fi

# About page
echo -n "   - About page (/about): "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651/about)
if [ "$STATUS" = "200" ]; then
    echo "✅ PASS (HTTP $STATUS)"
else
    echo "❌ FAIL (HTTP $STATUS)"
fi

# Contact page
echo -n "   - Contact page (/contact): "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651/contact)
if [ "$STATUS" = "200" ]; then
    echo "✅ PASS (HTTP $STATUS)"
else
    echo "❌ FAIL (HTTP $STATUS)"
fi

# Test protected routes
echo -e "\n2. Testing Protected Routes (Should redirect to login):"

# Admin dashboard
echo -n "   - Admin dashboard (/admin): "
RESPONSE=$(curl -s -L -w "HTTPSTATUS:%{http_code}" http://localhost:5651/admin)
STATUS=$(echo $RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
if [[ "$RESPONSE" == *"Login"* ]] && [ "$STATUS" = "200" ]; then
    echo "✅ PASS (Redirected to login)"
else
    echo "❌ FAIL (Status: $STATUS)"
fi

# Admin events
echo -n "   - Admin events (/admin/events): "
RESPONSE=$(curl -s -L -w "HTTPSTATUS:%{http_code}" http://localhost:5651/admin/events)
STATUS=$(echo $RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
if [[ "$RESPONSE" == *"Login"* ]] && [ "$STATUS" = "200" ]; then
    echo "✅ PASS (Redirected to login)"
else
    echo "❌ FAIL (Status: $STATUS)"
fi

# Member dashboard
echo -n "   - Member dashboard (/member): "
RESPONSE=$(curl -s -L -w "HTTPSTATUS:%{http_code}" http://localhost:5651/member)
STATUS=$(echo $RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
if [[ "$RESPONSE" == *"Login"* ]] && [ "$STATUS" = "200" ]; then
    echo "✅ PASS (Redirected to login)"
else
    echo "❌ FAIL (Status: $STATUS)"
fi

# Test static resources
echo -e "\n3. Testing Static Resources (Should be accessible):"

# CSS bundle
echo -n "   - CSS bundle: "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651/WitchCityRope.Web.styles.css)
if [ "$STATUS" = "200" ]; then
    echo "✅ PASS (HTTP $STATUS)"
else
    echo "❌ FAIL (HTTP $STATUS)"
fi

# Count results
echo -e "\n=== Summary ==="
TOTAL_TESTS=8
PASSED=$(grep -c "✅ PASS" <<< "$0")
echo "Tests passed: $PASSED/$TOTAL_TESTS"