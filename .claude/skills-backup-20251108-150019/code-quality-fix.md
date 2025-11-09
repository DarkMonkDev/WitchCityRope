#!/bin/bash
# code-quality-fix skill - Automated code quality fixes
# Purpose: Auto-fix ESLint errors, format with Prettier, organize imports
# Performance: ~3-7 seconds for full codebase, ~2 seconds for changed files
# Exit codes: 0 = fixes applied successfully, 1 = manual fixes required

# ============================================================================
# SKILL: Code Quality Fix
# ============================================================================
#
# WHAT THIS DOES:
# Automatically fixes code quality issues including:
# - ESLint auto-fixable errors and warnings
# - Prettier formatting issues
# - Import statement organization
# - Trailing whitespace and newlines
#
# WHEN TO USE:
# - After completing development session (cleanup)
# - Before creating a pull request
# - After merging branches (resolve conflicts)
# - When quality checks fail with auto-fixable issues
# - As part of code refactoring
#
# WHO USES THIS:
# - react-developer (cleanup after feature development)
# - backend-developer (fix TypeScript issues)
# - All developers (before committing)
# - code-reviewer agent (suggest auto-fix for PRs)
#
# INTEGRATION:
# - Complements code-quality-check skill (run check first, then fix)
# - Used after merge conflicts to fix formatting
# - Referenced by development workflow guides
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
echo -e "${BLUE}║           CODE QUALITY AUTO-FIX - WitchCityRope                ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Parse options
CHANGED_ONLY=false
DRY_RUN=false
VERIFY_AFTER=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --changed-only)
            CHANGED_ONLY=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --no-verify)
            VERIFY_AFTER=false
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Usage: bash code-quality-fix.md [--changed-only] [--dry-run] [--no-verify]"
            exit 1
            ;;
    esac
done

START_TIME=$(date +%s)
FIXES_APPLIED=0
MANUAL_FIXES_NEEDED=0

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

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}   ⚠️  DRY RUN MODE: No changes will be made${NC}"
fi

echo ""

# ============================================================================
# STEP 2: Determine File Scope
# ============================================================================

echo -e "${CYAN}2️⃣  Determining files to fix...${NC}"

if [ "$CHANGED_ONLY" = true ]; then
    # Only fix git-modified files
    CHANGED_FILES=$(git diff --name-only --diff-filter=ACM | grep -E '\.(ts|tsx|js|jsx)$' || echo "")

    if [ -z "$CHANGED_FILES" ]; then
        echo -e "${GREEN}   ✅ No changed TypeScript/JavaScript files${NC}"
        echo ""
        exit 0
    fi

    FILE_COUNT=$(echo "$CHANGED_FILES" | wc -l)
    echo -e "${BLUE}   Scope: Changed files only (${FILE_COUNT} files)${NC}"

    # Show first few files
    echo "$CHANGED_FILES" | head -5 | while read file; do
        echo -e "   - $file"
    done
    if [ $FILE_COUNT -gt 5 ]; then
        echo -e "   ... and $((FILE_COUNT - 5)) more"
    fi
else
    echo -e "${BLUE}   Scope: Full codebase${NC}"
    echo -e "${BLUE}   Pattern: **/*.{ts,tsx,js,jsx}${NC}"
fi

echo ""

# ============================================================================
# STEP 3: Run ESLint Auto-Fix
# ============================================================================

echo -e "${CYAN}3️⃣  Running ESLint auto-fix...${NC}"

if [ "$DRY_RUN" = true ]; then
    # Dry run: show what would be fixed
    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        echo "$CHANGED_FILES" | xargs npx eslint --fix-dry-run --format=compact 2>&1 | tee /tmp/eslint-dry-run.txt
    else
        npx eslint . --ext .ts,.tsx,.js,.jsx --fix-dry-run --format=compact 2>&1 | tee /tmp/eslint-dry-run.txt
    fi

    FIXABLE=$(grep -c "fixable" /tmp/eslint-dry-run.txt || echo "0")
    echo -e "${YELLOW}   ⚠️  DRY RUN: ${FIXABLE} issues would be auto-fixed${NC}"
else
    # Actually fix issues
    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        echo "$CHANGED_FILES" | xargs npx eslint --fix --format=compact 2>&1 | tee /tmp/eslint-fix.txt
    else
        npx eslint . --ext .ts,.tsx,.js,.jsx --fix --format=compact 2>&1 | tee /tmp/eslint-fix.txt
    fi

    # Count fixed issues
    FIXED=$(grep -c "fixed" /tmp/eslint-fix.txt || echo "0")
    REMAINING=$(grep -c "problem" /tmp/eslint-fix.txt || echo "0")

    if [ "$FIXED" -gt 0 ]; then
        echo -e "${GREEN}   ✅ Auto-fixed ${FIXED} ESLint issues${NC}"
        FIXES_APPLIED=$((FIXES_APPLIED + FIXED))
    fi

    if [ "$REMAINING" -gt 0 ]; then
        echo -e "${YELLOW}   ⚠️  ${REMAINING} issues require manual fixes${NC}"
        MANUAL_FIXES_NEEDED=$((MANUAL_FIXES_NEEDED + REMAINING))
    fi

    if [ "$FIXED" -eq 0 ] && [ "$REMAINING" -eq 0 ]; then
        echo -e "${GREEN}   ✅ No ESLint issues found${NC}"
    fi
fi

echo ""

# ============================================================================
# STEP 4: Run Prettier Formatting
# ============================================================================

echo -e "${CYAN}4️⃣  Running Prettier formatting...${NC}"

if [ "$DRY_RUN" = true ]; then
    # Dry run: check what would be formatted
    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        PRETTIER_FILES=$(echo "$CHANGED_FILES" | xargs npx prettier --list-different || echo "")
    else
        PRETTIER_FILES=$(npx prettier --list-different "**/*.{ts,tsx,js,jsx,json,md}" || echo "")
    fi

    if [ -n "$PRETTIER_FILES" ]; then
        PRETTIER_COUNT=$(echo "$PRETTIER_FILES" | wc -l)
        echo -e "${YELLOW}   ⚠️  DRY RUN: ${PRETTIER_COUNT} files would be formatted${NC}"
    else
        echo -e "${GREEN}   ✅ All files already formatted${NC}"
    fi
else
    # Actually format files
    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        echo "$CHANGED_FILES" | xargs npx prettier --write 2>&1 > /tmp/prettier-write.txt
    else
        npx prettier --write "**/*.{ts,tsx,js,jsx,json,md}" 2>&1 > /tmp/prettier-write.txt
    fi

    FORMATTED=$(grep -c "ms" /tmp/prettier-write.txt || echo "multiple")
    echo -e "${GREEN}   ✅ Formatted files with Prettier${NC}"
    FIXES_APPLIED=$((FIXES_APPLIED + 1))
fi

echo ""

# ============================================================================
# STEP 5: Show Changes Made
# ============================================================================

if [ "$DRY_RUN" = false ]; then
    echo -e "${CYAN}5️⃣  Reviewing changes...${NC}"

    # Show git diff statistics
    if git diff --stat 2>/dev/null | head -15 | tee /tmp/git-diff-stat.txt; then
        MODIFIED_FILES=$(grep -c "file changed\|files changed" /tmp/git-diff-stat.txt || echo "0")
        if [ "$MODIFIED_FILES" -gt 0 ]; then
            echo ""
            echo -e "${BLUE}   ${MODIFIED_FILES} files modified${NC}"
            echo -e "${YELLOW}   Review with: ${CYAN}git diff${NC}"
        else
            echo -e "${GREEN}   No changes needed (files already clean)${NC}"
        fi
    else
        echo -e "${GREEN}   No changes made${NC}"
    fi

    echo ""
fi

# ============================================================================
# STEP 6: Verify Fixes (Re-run Quality Check)
# ============================================================================

if [ "$VERIFY_AFTER" = true ] && [ "$DRY_RUN" = false ]; then
    echo -e "${CYAN}6️⃣  Verifying fixes...${NC}"
    echo ""

    # Run code-quality-check skill to verify
    if [ "$CHANGED_ONLY" = true ]; then
        bash /.claude/skills/code-quality-check.md --quick --changed-only
    else
        bash /.claude/skills/code-quality-check.md --quick
    fi

    VERIFY_EXIT=$?

    if [ $VERIFY_EXIT -eq 0 ]; then
        echo -e "${GREEN}   ✅ All quality checks now pass!${NC}"
    elif [ $VERIFY_EXIT -eq 2 ]; then
        echo -e "${YELLOW}   ⚠️  Quality checks pass with warnings${NC}"
    else
        echo -e "${YELLOW}   ⚠️  Some issues require manual fixes${NC}"
        MANUAL_FIXES_NEEDED=$((MANUAL_FIXES_NEEDED + 1))
    fi

    echo ""
fi

# ============================================================================
# STEP 7: Summary Report
# ============================================================================

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                  AUTO-FIX SUMMARY                              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}🔍 DRY RUN COMPLETE (No changes made)${NC}"
    echo -e "${YELLOW}   Re-run without --dry-run to apply fixes${NC}"
    EXIT_CODE=0
elif [ $MANUAL_FIXES_NEEDED -eq 0 ]; then
    echo -e "${GREEN}✅ STATUS: ALL ISSUES FIXED${NC}"
    echo -e "${GREEN}   Fixes applied: ${FIXES_APPLIED}${NC}"
    echo -e "${GREEN}   Manual fixes needed: 0${NC}"
    EXIT_CODE=0
else
    echo -e "${YELLOW}⚠️  STATUS: PARTIALLY FIXED${NC}"
    echo -e "${GREEN}   Fixes applied: ${FIXES_APPLIED}${NC}"
    echo -e "${YELLOW}   Manual fixes needed: ${MANUAL_FIXES_NEEDED}${NC}"
    EXIT_CODE=1
fi

echo ""
echo -e "${CYAN}Performance:${NC}"
echo -e "   Duration: ${DURATION}s"
echo -e "   Mode: $([ "$DRY_RUN" = true ] && echo "Dry run" || echo "Apply fixes")"
echo -e "   Scope: $([ "$CHANGED_ONLY" = true ] && echo "Changed files" || echo "Full codebase")"
echo ""

# ============================================================================
# STEP 8: Next Steps
# ============================================================================

if [ $MANUAL_FIXES_NEEDED -gt 0 ]; then
    echo -e "${YELLOW}📋 Manual Fixes Required:${NC}"
    echo ""
    echo -e "1. Review remaining issues:"
    echo -e "   ${CYAN}bash /.claude/skills/code-quality-check.md${NC}"
    echo ""
    echo -e "2. Fix issues that couldn't be auto-fixed"
    echo ""
    echo -e "3. Re-run this skill to verify:"
    echo -e "   ${CYAN}bash /.claude/skills/code-quality-fix.md${NC}"
    echo ""
fi

if [ "$DRY_RUN" = false ] && [ $FIXES_APPLIED -gt 0 ]; then
    echo -e "${YELLOW}💡 Recommended Actions:${NC}"
    echo ""
    echo -e "1. Review the changes made:"
    echo -e "   ${CYAN}git diff${NC}"
    echo ""
    echo -e "2. Test your application:"
    echo -e "   ${CYAN}npm run dev${NC}"
    echo -e "   ${CYAN}npm test${NC}"
    echo ""
    echo -e "3. Commit the fixes:"
    echo -e "   ${CYAN}git add .${NC}"
    echo -e "   ${CYAN}git commit -m \"Fix: Auto-fix code quality issues\"${NC}"
    echo ""
fi

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}📋 To Apply Fixes:${NC}"
    echo ""
    if [ "$CHANGED_ONLY" = true ]; then
        echo -e "   ${CYAN}bash /.claude/skills/code-quality-fix.md --changed-only${NC}"
    else
        echo -e "   ${CYAN}bash /.claude/skills/code-quality-fix.md${NC}"
    fi
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
# 1. "Some issues cannot be auto-fixed"
#    - Expected: Not all ESLint rules are auto-fixable
#    - Solution: Fix remaining issues manually
#    - Check: ESLint output for specific error messages
#
# 2. "Changes break application"
#    - Review: Use git diff to see what changed
#    - Test: Run npm run dev and npm test
#    - Revert: git checkout . (if needed)
#
# 3. "Prettier conflicts with ESLint"
#    - Install: eslint-config-prettier (disables conflicting rules)
#    - Add: "prettier" to .eslintrc.json extends array
#
# 4. "Too many changes at once"
#    - Use: --changed-only flag for incremental fixes
#    - Use: --dry-run to preview changes first
#
# 5. "Verification fails after fix"
#    - Some issues require manual fixes
#    - Run: bash /.claude/skills/code-quality-check.md
#    - Fix: Remaining issues manually
#
# USAGE EXAMPLES:
#
# Fix all auto-fixable issues:
#   bash /.claude/skills/code-quality-fix.md
#
# Fix changed files only:
#   bash /.claude/skills/code-quality-fix.md --changed-only
#
# Preview fixes without applying:
#   bash /.claude/skills/code-quality-fix.md --dry-run
#
# Fix without verification:
#   bash /.claude/skills/code-quality-fix.md --no-verify
#
# WHAT GETS FIXED:
#
# ESLint auto-fixable rules:
# - Missing semicolons
# - Quote style (single vs double)
# - Spacing issues
# - Import ordering
# - Unused imports
# - Trailing commas
#
# Prettier formatting:
# - Indentation
# - Line breaks
# - Maximum line length
# - Bracket spacing
# - Arrow function parentheses
#
# WHAT REQUIRES MANUAL FIX:
#
# - Type errors
# - Logic errors
# - Complex ESLint violations
# - Security vulnerabilities
# - Unused variables (must decide if needed)
# - Missing function returns
#
# INTEGRATION:
#
# Typical workflow:
# 1. Develop feature
# 2. Run: bash /.claude/skills/code-quality-fix.md
# 3. Review: git diff
# 4. Test: npm test
# 5. Commit: git commit
#
# Before PR:
# 1. bash /.claude/skills/code-quality-fix.md
# 2. bash /.claude/skills/code-quality-check.md
# 3. Fix any remaining issues
# 4. Create PR
#
# ============================================================================

exit $EXIT_CODE
