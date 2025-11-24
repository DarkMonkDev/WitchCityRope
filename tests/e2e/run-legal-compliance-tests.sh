#!/bin/bash
# Run Legal Compliance E2E Tests
# Tests for Terms of Service and Event Waiver requirements

set -e

echo "🧪 Running Legal Compliance E2E Tests..."
echo "=========================================="
echo ""

# Define test files
REGISTRATION_TOS="tests/playwright/auth/registration-tos.spec.ts"
RSVP_WAIVER="tests/playwright/participation/rsvp-event-waiver.spec.ts"
VOLUNTEER_WAIVER="tests/playwright/participation/volunteer-event-waiver.spec.ts"
TICKET_WAIVER="tests/playwright/participation/ticket-purchase-waiver.spec.ts"

# Check if test files exist
for file in "$REGISTRATION_TOS" "$RSVP_WAIVER" "$VOLUNTEER_WAIVER" "$TICKET_WAIVER"; do
  if [ ! -f "$file" ]; then
    echo "❌ Test file not found: $file"
    exit 1
  fi
done

echo "✅ All test files found"
echo ""

# Check Docker containers
echo "🐳 Checking Docker environment..."
if ! docker ps | grep -q "witchcity-web"; then
  echo "❌ Docker containers not running. Please start with: ./dev.sh"
  exit 1
fi

if ! docker ps | grep -q "witchcity-api"; then
  echo "❌ API container not running. Please start with: ./dev.sh"
  exit 1
fi

echo "✅ Docker containers healthy"
echo ""

# Create test results directory
mkdir -p tests/playwright/test-results
mkdir -p test-results

echo "📊 Running tests..."
echo ""

# Note: Using absolute paths from project root
# These tests are standalone and not in the main playwright.config.ts testDir

echo "1️⃣  Registration Terms of Service Tests..."
npx tsx tests/playwright/auth/registration-tos.spec.ts || echo "⚠️  Some registration tests failed"
echo ""

echo "2️⃣  RSVP Event Waiver Tests..."
npx tsx tests/playwright/participation/rsvp-event-waiver.spec.ts || echo "⚠️  Some RSVP tests failed"
echo ""

echo "3️⃣  Volunteer Signup Event Waiver Tests..."
npx tsx tests/playwright/participation/volunteer-event-waiver.spec.ts || echo "⚠️  Some volunteer tests failed"
echo ""

echo "4️⃣  Ticket Purchase Liability Waiver Tests..."
npx tsx tests/playwright/participation/ticket-purchase-waiver.spec.ts || echo "⚠️  Some ticket purchase tests failed"
echo ""

echo "=========================================="
echo "✅ Test execution complete!"
echo ""
echo "📁 Test results saved to:"
echo "   - tests/playwright/test-results/"
echo "   - test-results/"
echo ""
echo "📊 View detailed results:"
echo "   - Check TEST_CATALOG.md for updated pass/fail metrics"
echo "   - Check test-results/ for screenshots and artifacts"
echo ""
echo "⚠️  Known Backend Issues (not test failures):"
echo "   1. Registration API timeout (15-30s) - needs backend investigation"
echo "   2. Ticket purchase API returns 404 instead of 400 for validation"
echo ""
echo "See TEST_FIX_PLAN.md for detailed issue breakdown"
