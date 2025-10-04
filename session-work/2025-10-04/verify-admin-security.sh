#!/bin/bash
# Admin Vetting Security Fix Verification Script
# Date: 2025-10-04
# Purpose: Verify that non-admin users cannot access admin vetting routes

echo "=========================================="
echo "Admin Vetting Security Fix Verification"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track results
TESTS_PASSED=0
TESTS_FAILED=0

# Function to run a test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_result="$3"

    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "TEST: $test_name"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    eval "$test_command"
    local result=$?

    if [ $result -eq $expected_result ]; then
        echo -e "${GREEN}✓ PASSED${NC}"
        ((TESTS_PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC}"
        echo "  Expected exit code: $expected_result"
        echo "  Actual exit code: $result"
        ((TESTS_FAILED++))
    fi
    echo ""
}

echo "Checking implementation files..."
echo ""

# Check if new files exist
echo "1. Verifying new files were created:"
if [ -f "apps/web/src/routes/loaders/adminLoader.ts" ]; then
    echo -e "  ${GREEN}✓${NC} adminLoader.ts exists"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} adminLoader.ts NOT FOUND"
    ((TESTS_FAILED++))
fi

if [ -f "apps/web/src/pages/UnauthorizedPage.tsx" ]; then
    echo -e "  ${GREEN}✓${NC} UnauthorizedPage.tsx exists"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} UnauthorizedPage.tsx NOT FOUND"
    ((TESTS_FAILED++))
fi

echo ""
echo "2. Verifying router.tsx uses adminLoader for admin routes:"

# Check if adminLoader is imported
if grep -q "import { adminLoader }" apps/web/src/routes/router.tsx; then
    echo -e "  ${GREEN}✓${NC} adminLoader imported in router.tsx"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} adminLoader NOT imported in router.tsx"
    ((TESTS_FAILED++))
fi

# Check if admin/vetting route uses adminLoader
if grep -A 2 'path: "admin/vetting"' apps/web/src/routes/router.tsx | grep -q "loader: adminLoader"; then
    echo -e "  ${GREEN}✓${NC} /admin/vetting route uses adminLoader"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} /admin/vetting route does NOT use adminLoader"
    ((TESTS_FAILED++))
fi

# Check if admin/vetting/applications route uses adminLoader
if grep -A 2 'path: "admin/vetting/applications/:applicationId"' apps/web/src/routes/router.tsx | grep -q "loader: adminLoader"; then
    echo -e "  ${GREEN}✓${NC} /admin/vetting/applications/:applicationId route uses adminLoader"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} /admin/vetting/applications/:applicationId route does NOT use adminLoader"
    ((TESTS_FAILED++))
fi

# Check if unauthorized route exists
if grep -q 'path: "unauthorized"' apps/web/src/routes/router.tsx; then
    echo -e "  ${GREEN}✓${NC} /unauthorized route exists"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} /unauthorized route NOT FOUND"
    ((TESTS_FAILED++))
fi

echo ""
echo "3. Verifying adminLoader implementation:"

# Check for role validation in adminLoader
if grep -q 'user.role !== .Administrator.' apps/web/src/routes/loaders/adminLoader.ts; then
    echo -e "  ${GREEN}✓${NC} adminLoader checks for 'Administrator' role"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} adminLoader does NOT check for 'Administrator' role"
    ((TESTS_FAILED++))
fi

# Check for unauthorized redirect
if grep -q "redirect('/unauthorized')" apps/web/src/routes/loaders/adminLoader.ts; then
    echo -e "  ${GREEN}✓${NC} adminLoader redirects to /unauthorized"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} adminLoader does NOT redirect to /unauthorized"
    ((TESTS_FAILED++))
fi

echo ""
echo "4. Verifying component-level guards:"

# Check AdminVettingPage has role guard
if grep -q "user.role !== 'Administrator'" apps/web/src/pages/admin/AdminVettingPage.tsx; then
    echo -e "  ${GREEN}✓${NC} AdminVettingPage has role guard"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} AdminVettingPage does NOT have role guard"
    ((TESTS_FAILED++))
fi

# Check AdminVettingApplicationDetailPage has role guard
if grep -q "user.role !== 'Administrator'" apps/web/src/pages/admin/AdminVettingApplicationDetailPage.tsx; then
    echo -e "  ${GREEN}✓${NC} AdminVettingApplicationDetailPage has role guard"
    ((TESTS_PASSED++))
else
    echo -e "  ${RED}✗${NC} AdminVettingApplicationDetailPage does NOT have role guard"
    ((TESTS_FAILED++))
fi

echo ""
echo "5. Verifying TypeScript compilation:"

# Run TypeScript compiler
if npx tsc --noEmit --project apps/web/tsconfig.json 2>&1 | grep -q "error"; then
    echo -e "  ${RED}✗${NC} TypeScript compilation has errors"
    ((TESTS_FAILED++))
    npx tsc --noEmit --project apps/web/tsconfig.json 2>&1 | grep "error" | head -5
else
    echo -e "  ${GREEN}✓${NC} TypeScript compilation successful (no errors)"
    ((TESTS_PASSED++))
fi

echo ""
echo "=========================================="
echo "VERIFICATION SUMMARY"
echo "=========================================="
echo ""
echo -e "Tests Passed: ${GREEN}$TESTS_PASSED${NC}"
echo -e "Tests Failed: ${RED}$TESTS_FAILED${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${GREEN}✓ ALL VERIFICATION CHECKS PASSED!${NC}"
    echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo "Next Steps:"
    echo "1. Run E2E tests to verify security fix:"
    echo "   npm run test:e2e:playwright -- --grep \"Non-admin user cannot access vetting grid\""
    echo ""
    echo "2. Start the application and manually test:"
    echo "   ./dev.sh"
    echo "   - Login as admin@witchcityrope.com → Can access /admin/vetting ✓"
    echo "   - Login as member@witchcityrope.com → Redirected to /unauthorized ✓"
    echo ""
    exit 0
else
    echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${RED}✗ VERIFICATION FAILED${NC}"
    echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo "Please review the failed checks above."
    echo ""
    exit 1
fi
