#!/bin/bash
# code-quality-check skill - Comprehensive code quality validation
# Purpose: Run ESLint, TypeScript checks, and security audits
# Performance: ~5 seconds for full codebase, ~2 seconds for changed files only
# Exit codes: 0 = pass, 1 = critical violations, 2 = warnings only

# ============================================================================
# SKILL: Code Quality Check
# ============================================================================
#
# WHAT THIS DOES:
# Runs comprehensive code quality validation including:
# - ESLint validation (errors and warnings)
# - TypeScript type checking (compilation errors)
# - Security audit (npm audit)
# - Unused dependency detection
#
# WHEN TO USE:
# - Before creating a pull request
# - After completing feature development
# - Before committing (via pre-commit hook)
# - During CI/CD pipeline
# - When troubleshooting type errors
#
# WHO USES THIS:
# - react-developer (before PR creation)
# - backend-developer (for TypeScript validation)
# - code-reviewer (quality gate checking)
# - lint-validator agent (automated validation)
# - Pre-commit hooks (automatic validation)
#
# INTEGRATION:
# - Used by pre-commit hook for commit-time validation
# - Referenced by Phase 3 validator for quality gates
# - Called by code-reviewer agent for PR reviews
#
# ============================================================================

set -e

# Color codes for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║           CODE QUALITY CHECK - WitchCityRope                  ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Parse options
QUICK_MODE=false
CHANGED_ONLY=false
FIX_MODE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --quick)
            QUICK_MODE=true
            shift
            ;;
        --changed-only)
            CHANGED_ONLY=true
            shift
            ;;
        --fix)
            FIX_MODE=true
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Track overall status
CRITICAL_ERRORS=0
WARNINGS=0
START_TIME=$(date +%s)

# ============================================================================
# STEP 1: Environment Check
# ============================================================================

echo -e "${CYAN}1️⃣  Checking environment...${NC}"

# Check if we're in the right directory
if [ ! -f "package.json" ]; then
    echo -e "${RED}   ❌ Error: Not in project root (package.json not found)${NC}"
    exit 1
fi

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo -e "${RED}   ❌ Error: node_modules not found. Run: npm install${NC}"
    exit 1
fi

# Verify tools are available
if ! command -v npx &> /dev/null; then
    echo -e "${RED}   ❌ Error: npx not found. Install Node.js${NC}"
    exit 1
fi

echo -e "${GREEN}   ✅ Environment ready${NC}"
echo ""

# ============================================================================
# STEP 2: TypeScript Type Checking
# ============================================================================

echo -e "${CYAN}2️⃣  Running TypeScript type checking...${NC}"

if [ "$QUICK_MODE" = true ]; then
    echo -e "${YELLOW}   ⚡ Quick mode: Skipping TypeScript check${NC}"
else
    # Run TypeScript compiler with no emit (type checking only)
    if npx tsc --noEmit 2>&1 | tee /tmp/tsc-output.txt; then
        echo -e "${GREEN}   ✅ No TypeScript errors${NC}"
    else
        TYPE_ERRORS=$(grep -c "error TS" /tmp/tsc-output.txt || echo "0")
        echo -e "${RED}   ❌ TypeScript errors found: ${TYPE_ERRORS}${NC}"
        echo ""
        echo -e "${YELLOW}   First 10 errors:${NC}"
        head -10 /tmp/tsc-output.txt | grep "error TS" || echo "   (See full output above)"
        CRITICAL_ERRORS=$((CRITICAL_ERRORS + TYPE_ERRORS))
    fi
fi
echo ""

# ============================================================================
# STEP 3: ESLint Validation
# ============================================================================

echo -e "${CYAN}3️⃣  Running ESLint validation...${NC}"

# Determine which files to check
if [ "$CHANGED_ONLY" = true ]; then
    CHANGED_FILES=$(git diff --name-only --diff-filter=ACM | grep -E '\.(ts|tsx|js|jsx)$' || echo "")
    if [ -z "$CHANGED_FILES" ]; then
        echo -e "${GREEN}   ✅ No changed TypeScript/JavaScript files${NC}"
    else
        echo -e "${BLUE}   Checking changed files only:${NC}"
        echo "$CHANGED_FILES" | head -5
        if [ $(echo "$CHANGED_FILES" | wc -l) -gt 5 ]; then
            echo "   ... and $(($(echo "$CHANGED_FILES" | wc -l) - 5)) more"
        fi

        # Run ESLint on changed files
        if echo "$CHANGED_FILES" | xargs npx eslint --format=compact 2>&1 | tee /tmp/eslint-output.txt; then
            echo -e "${GREEN}   ✅ No ESLint errors in changed files${NC}"
        else
            ESLINT_ERRORS=$(grep -c "error" /tmp/eslint-output.txt || echo "0")
            ESLINT_WARNINGS=$(grep -c "warning" /tmp/eslint-output.txt || echo "0")
            echo -e "${RED}   ❌ ESLint errors: ${ESLINT_ERRORS}${NC}"
            echo -e "${YELLOW}   ⚠️  ESLint warnings: ${ESLINT_WARNINGS}${NC}"
            CRITICAL_ERRORS=$((CRITICAL_ERRORS + ESLINT_ERRORS))
            WARNINGS=$((WARNINGS + ESLINT_WARNINGS))
        fi
    fi
else
    # Run ESLint on all TypeScript/JavaScript files
    if npx eslint . --ext .ts,.tsx,.js,.jsx --format=compact --max-warnings=50 2>&1 | tee /tmp/eslint-output.txt; then
        echo -e "${GREEN}   ✅ No ESLint errors${NC}"
    else
        ESLINT_ERRORS=$(grep -c "error" /tmp/eslint-output.txt || echo "0")
        ESLINT_WARNINGS=$(grep -c "warning" /tmp/eslint-output.txt || echo "0")
        echo -e "${RED}   ❌ ESLint errors: ${ESLINT_ERRORS}${NC}"
        echo -e "${YELLOW}   ⚠️  ESLint warnings: ${ESLINT_WARNINGS}${NC}"

        # Show sample errors
        if [ "$ESLINT_ERRORS" -gt 0 ]; then
            echo ""
            echo -e "${YELLOW}   Sample errors (first 5):${NC}"
            grep "error" /tmp/eslint-output.txt | head -5 || echo "   (See full output above)"
        fi

        CRITICAL_ERRORS=$((CRITICAL_ERRORS + ESLINT_ERRORS))
        WARNINGS=$((WARNINGS + ESLINT_WARNINGS))
    fi
fi
echo ""

# ============================================================================
# STEP 4: Security Audit (Quick Mode Skip)
# ============================================================================

echo -e "${CYAN}4️⃣  Running security audit...${NC}"

if [ "$QUICK_MODE" = true ]; then
    echo -e "${YELLOW}   ⚡ Quick mode: Skipping security audit${NC}"
else
    if npm audit --audit-level=high 2>&1 | tee /tmp/npm-audit.txt; then
        echo -e "${GREEN}   ✅ No high/critical security vulnerabilities${NC}"
    else
        VULNERABILITIES=$(grep -c "vulnerabilities" /tmp/npm-audit.txt || echo "0")
        echo -e "${YELLOW}   ⚠️  Security vulnerabilities found${NC}"
        echo -e "${YELLOW}   Run: npm audit fix (to auto-fix)${NC}"
        WARNINGS=$((WARNINGS + 1))
    fi
fi
echo ""

# ============================================================================
# STEP 5: Unused Dependencies Check (Quick Mode Skip)
# ============================================================================

echo -e "${CYAN}5️⃣  Checking for unused dependencies...${NC}"

if [ "$QUICK_MODE" = true ]; then
    echo -e "${YELLOW}   ⚡ Quick mode: Skipping dependency check${NC}"
else
    if command -v depcheck &> /dev/null; then
        if npx depcheck --json 2>&1 | tee /tmp/depcheck.json > /dev/null; then
            UNUSED=$(cat /tmp/depcheck.json | grep -c "\"dependencies\"" || echo "0")
            if [ "$UNUSED" -eq 0 ]; then
                echo -e "${GREEN}   ✅ No unused dependencies detected${NC}"
            else
                echo -e "${YELLOW}   ⚠️  Unused dependencies found${NC}"
                echo -e "${YELLOW}   Run: npx depcheck (for details)${NC}"
                WARNINGS=$((WARNINGS + 1))
            fi
        fi
    else
        echo -e "${YELLOW}   ⚠️  depcheck not installed (optional)${NC}"
    fi
fi
echo ""

# ============================================================================
# STEP 6: Auto-Fix Suggestion (if --fix mode)
# ============================================================================

if [ "$FIX_MODE" = true ] && [ $CRITICAL_ERRORS -gt 0 ]; then
    echo -e "${CYAN}6️⃣  Auto-fix mode enabled...${NC}"
    echo -e "${YELLOW}   Attempting to fix auto-fixable issues...${NC}"

    # Run ESLint with --fix
    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        echo "$CHANGED_FILES" | xargs npx eslint --fix
    else
        npx eslint . --ext .ts,.tsx,.js,.jsx --fix
    fi

    echo -e "${GREEN}   ✅ Auto-fix completed${NC}"
    echo -e "${YELLOW}   ⚠️  Re-run this skill to verify fixes${NC}"
    echo ""
fi

# ============================================================================
# STEP 7: Summary Report
# ============================================================================

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    QUALITY CHECK SUMMARY                       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

if [ $CRITICAL_ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo -e "${GREEN}✅ STATUS: PASS${NC}"
    echo -e "${GREEN}   All quality checks passed!${NC}"
    EXIT_CODE=0
elif [ $CRITICAL_ERRORS -eq 0 ] && [ $WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}⚠️  STATUS: PASS WITH WARNINGS${NC}"
    echo -e "${YELLOW}   Warnings: ${WARNINGS}${NC}"
    echo -e "${YELLOW}   Consider addressing warnings before commit${NC}"
    EXIT_CODE=2
else
    echo -e "${RED}❌ STATUS: FAIL${NC}"
    echo -e "${RED}   Critical errors: ${CRITICAL_ERRORS}${NC}"
    echo -e "${YELLOW}   Warnings: ${WARNINGS}${NC}"
    EXIT_CODE=1
fi

echo ""
echo -e "${CYAN}Performance:${NC}"
echo -e "   Duration: ${DURATION}s"
echo -e "   Mode: $([ "$QUICK_MODE" = true ] && echo "Quick" || echo "Comprehensive")"
echo -e "   Scope: $([ "$CHANGED_ONLY" = true ] && echo "Changed files only" || echo "Full codebase")"
echo ""

# ============================================================================
# STEP 8: Next Steps
# ============================================================================

if [ $CRITICAL_ERRORS -gt 0 ]; then
    echo -e "${YELLOW}📋 Recommended Next Steps:${NC}"
    echo ""
    echo -e "1. Fix TypeScript errors:"
    echo -e "   ${CYAN}npx tsc --noEmit${NC}"
    echo ""
    echo -e "2. Fix ESLint errors:"
    echo -e "   ${CYAN}npx eslint . --ext .ts,.tsx --fix${NC}"
    echo -e "   ${CYAN}(or use code-quality-fix skill)${NC}"
    echo ""
    echo -e "3. Review and fix remaining issues manually"
    echo ""
    echo -e "4. Re-run this skill to verify:"
    echo -e "   ${CYAN}bash /.claude/skills/code-quality-check.md${NC}"
    echo ""
fi

if [ $WARNINGS -gt 0 ] && [ $CRITICAL_ERRORS -eq 0 ]; then
    echo -e "${YELLOW}📋 Optional Improvements:${NC}"
    echo ""
    echo -e "1. Address ESLint warnings (non-blocking)"
    echo -e "2. Fix security vulnerabilities: ${CYAN}npm audit fix${NC}"
    echo -e "3. Remove unused dependencies: ${CYAN}npm uninstall [package]${NC}"
    echo ""
fi

echo -e "${BLUE}════════════════════════════════════════════════════════════════${NC}"
echo ""

# ============================================================================
# TROUBLESHOOTING GUIDE
# ============================================================================
#
# COMMON ISSUES:
#
# 1. "Cannot find module" errors
#    - Run: npm install
#    - Check: package.json has correct dependencies
#
# 2. "tsc: command not found"
#    - Install TypeScript: npm install -g typescript
#    - Or use project TypeScript: npx tsc
#
# 3. Too many ESLint warnings
#    - Use: --max-warnings flag to allow some warnings
#    - Or fix: npx eslint --fix
#
# 4. Slow performance
#    - Use: --quick mode (skips TypeScript and security)
#    - Use: --changed-only (checks only modified files)
#
# 5. False positives
#    - Check: .eslintignore excludes test files, build output
#    - Check: tsconfig.json exclude patterns
#
# PERFORMANCE OPTIONS:
#
# - Full check (default): ~5 seconds
#   bash /.claude/skills/code-quality-check.md
#
# - Quick check (2-3 seconds): Skips TypeScript and security
#   bash /.claude/skills/code-quality-check.md --quick
#
# - Changed files only: Checks only git-modified files
#   bash /.claude/skills/code-quality-check.md --changed-only
#
# - Auto-fix mode: Attempts to fix issues automatically
#   bash /.claude/skills/code-quality-check.md --fix
#
# INTEGRATION EXAMPLES:
#
# Pre-commit hook:
#   bash /.claude/skills/code-quality-check.md --quick --changed-only
#
# CI/CD pipeline:
#   bash /.claude/skills/code-quality-check.md
#
# Before PR:
#   bash /.claude/skills/code-quality-check.md
#
# ============================================================================

exit $EXIT_CODE
