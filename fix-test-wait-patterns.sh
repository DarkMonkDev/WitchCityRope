#!/bin/bash
# Script to fix wait pattern anti-patterns in E2E tests
# Part of Phase 3: Test Infrastructure Fix

set -e

echo "🔧 Fixing test wait patterns..."
echo ""

# Count occurrences before fixes
NETWORKIDLE_COUNT=$(grep -r "waitForLoadState('networkidle')" tests/e2e --include="*.spec.ts" | wc -l || echo "0")
WAITTIMEOUT_COUNT=$(grep -r "waitForTimeout" tests/e2e --include="*.spec.ts" | wc -l || echo "0")

echo "📊 Before fixes:"
echo "  - networkidle occurrences: $NETWORKIDLE_COUNT"
echo "  - waitForTimeout occurrences: $WAITTIMEOUT_COUNT"
echo ""

# Fix 1: Replace networkidle with domcontentloaded
echo "🔄 Replacing networkidle with domcontentloaded..."
find tests/e2e -name "*.spec.ts" -type f -exec sed -i "s/waitForLoadState('networkidle')/waitForLoadState('domcontentloaded')/g" {} +
find tests/e2e -name "*.spec.ts" -type f -exec sed -i 's/waitForLoadState("networkidle")/waitForLoadState("domcontentloaded")/g' {} +

echo "✅ networkidle replacements complete"
echo ""

# Count occurrences after first fix
NETWORKIDLE_COUNT_AFTER=$(grep -r "waitForLoadState('networkidle')" tests/e2e --include="*.spec.ts" | wc -l || echo "0")
echo "📊 After networkidle fix:"
echo "  - networkidle occurrences remaining: $NETWORKIDLE_COUNT_AFTER"
echo ""

echo "⚠️  Manual Review Required:"
echo "  - waitForTimeout patterns need case-by-case review"
echo "  - Some waitForTimeout calls may be valid (e.g., testing loading states)"
echo "  - Review each usage context before removing"
echo ""

echo "🎯 Next Steps:"
echo "  1. Review remaining waitForTimeout usages manually"
echo "  2. Run test-environment skill to verify fixes"
echo "  3. Update TEST_CATALOG with results"
echo ""

echo "✅ Automated fixes complete!"
