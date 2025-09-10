#!/bin/bash

# Script to test the improved dashboard error detection
# This will demonstrate the difference between the old (broken) test and new (fixed) test

echo "🎯 Testing Dashboard E2E Test Error Detection Fix"
echo "================================================"
echo ""

echo "📍 Working directory: $(pwd)"
echo "📁 Changing to Playwright test directory..."
cd tests/playwright || {
    echo "❌ ERROR: Could not find tests/playwright directory"
    echo "Make sure you're running this from the project root"
    exit 1
}

echo "📦 Installing Playwright dependencies if needed..."
npm install --silent

echo ""
echo "🔧 Running FIXED dashboard test with proper error monitoring..."
echo "This test will now catch JavaScript errors that crash the dashboard"
echo ""

# Run only the dashboard tests
npx playwright test dashboard.spec.ts --reporter=line

# Check the exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ SUCCESS: Dashboard test passed - no JavaScript errors detected"
    echo "The dashboard is working correctly without crashes"
else
    echo ""
    echo "❌ FAILURE: Dashboard test failed - JavaScript errors detected!"
    echo "This is GOOD - the test is now properly catching errors that would crash the dashboard"
    echo ""
    echo "🔍 Check the test output above for specific JavaScript errors"
    echo "💡 Look for patterns like:"
    echo "   - RangeError: Invalid time value"
    echo "   - JavaScript Error: ..."
    echo "   - Console Error: ..."
    echo ""
    echo "🚨 CRITICAL: The old test would have reported SUCCESS while ignoring these errors!"
    echo "🎯 BENEFIT: We now have accurate error detection instead of false positives"
fi

echo ""
echo "📸 Screenshots saved to test-results/ directory for debugging"
echo "🔍 Look for dashboard-error-check.png and dashboard-after-error-check.png"
echo ""
echo "✅ Error detection test completed"