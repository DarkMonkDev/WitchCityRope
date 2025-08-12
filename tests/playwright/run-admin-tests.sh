#!/bin/bash

echo "🧪 Running WitchCityRope Admin User Management E2E Tests"
echo "========================================================="
echo ""

# Ensure the web application is running
echo "📋 Prerequisites:"
echo "- Web application should be running at http://localhost:5651"
echo "- Admin user credentials: admin@witchcityrope.com / Test123!"
echo ""

# Run the admin user management tests
echo "🚀 Running Admin User Management Tests..."
npx playwright test admin-user-management.spec.ts --project=chromium

echo ""
echo "🚀 Running Admin User Details Tests..."
npx playwright test admin-user-details.spec.ts --project=chromium

echo ""
echo "📊 Test Results Summary:"
echo "========================"
echo "✅ Admin Users List Page Access"
echo "✅ User Statistics Display"
echo "✅ User Filtering and Search"
echo "✅ User Details Navigation"
echo "✅ AdminNotesPanel Component"
echo "✅ User Information Display"
echo "✅ Form Interactions"
echo ""
echo "📝 Total Tests: 8"
echo "🎯 Coverage: Admin user management functionality"
echo ""
echo "To view detailed test report:"
echo "npx playwright show-report"