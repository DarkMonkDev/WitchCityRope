#!/bin/bash
# code-format skill - Automated code formatting with Prettier
# Purpose: Check and apply consistent code formatting across the codebase
# Performance: ~1-3 seconds for full codebase, <1 second for changed files
# Exit codes: 0 = formatted/no changes needed, 1 = formatting needed (check mode)

# ============================================================================
# SKILL: Code Format
# ============================================================================
#
# WHAT THIS DOES:
# Formats code using Prettier for consistent style across:
# - TypeScript/JavaScript files (.ts, .tsx, .js, .jsx)
# - JSON configuration files
# - Markdown documentation
# - CSS/SCSS stylesheets
#
# WHEN TO USE:
# - Before committing code
# - After completing development session
# - When resolving merge conflicts
# - Before creating a pull request
# - When files have inconsistent formatting
#
# WHO USES THIS:
# - react-developer (before committing React components)
# - backend-developer (for TypeScript formatting)
# - prettier-formatter agent (automated formatting)
# - Pre-commit hooks (automatic formatting check)
# - Any developer for consistent code style
#
# INTEGRATION:
# - Used by pre-commit hook for commit-time formatting check
# - Referenced by Phase 3 validator for quality gates
# - Called by prettier-formatter agent for batch formatting
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
echo -e "${BLUE}║              CODE FORMATTING - WitchCityRope                   ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Parse options
CHECK_ONLY=false
CHANGED_ONLY=false
FILE_TYPE="all"

while [[ $# -gt 0 ]]; do
    case $1 in
        --check)
            CHECK_ONLY=true
            shift
            ;;
        --changed-only)
            CHANGED_ONLY=true
            shift
            ;;
        --type)
            FILE_TYPE="$2"
            shift 2
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Usage: bash code-format.md [--check] [--changed-only] [--type ts|js|json|md|all]"
            exit 1
            ;;
    esac
done

START_TIME=$(date +%s)
FILES_FORMATTED=0
FILES_CHECKED=0

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

# Verify Prettier is available
if ! command -v npx &> /dev/null; then
    echo -e "${RED}   ❌ Error: npx not found. Install Node.js${NC}"
    exit 1
fi

# Check Prettier configuration
if [ ! -f ".prettierrc" ] && [ ! -f ".prettierrc.json" ] && [ ! -f "prettier.config.js" ]; then
    echo -e "${YELLOW}   ⚠️  Warning: No Prettier config found (.prettierrc)${NC}"
    echo -e "${YELLOW}   Using Prettier defaults${NC}"
fi

echo -e "${GREEN}   ✅ Environment ready${NC}"
echo ""

# ============================================================================
# STEP 2: Determine File Scope
# ============================================================================

echo -e "${CYAN}2️⃣  Determining files to format...${NC}"

# Set file pattern based on type
case $FILE_TYPE in
    ts)
        FILE_PATTERN="**/*.{ts,tsx}"
        echo -e "${BLUE}   Type: TypeScript only${NC}"
        ;;
    js)
        FILE_PATTERN="**/*.{js,jsx}"
        echo -e "${BLUE}   Type: JavaScript only${NC}"
        ;;
    json)
        FILE_PATTERN="**/*.json"
        echo -e "${BLUE}   Type: JSON only${NC}"
        ;;
    md)
        FILE_PATTERN="**/*.md"
        echo -e "${BLUE}   Type: Markdown only${NC}"
        ;;
    all|*)
        FILE_PATTERN="**/*.{ts,tsx,js,jsx,json,md,css,scss}"
        echo -e "${BLUE}   Type: All supported files${NC}"
        ;;
esac

# Get list of files to process
if [ "$CHANGED_ONLY" = true ]; then
    # Only check git-modified files
    CHANGED_FILES=$(git diff --name-only --diff-filter=ACM | grep -E '\.(ts|tsx|js|jsx|json|md|css|scss)$' || echo "")

    if [ -z "$CHANGED_FILES" ]; then
        echo -e "${GREEN}   ✅ No changed files to format${NC}"
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
    echo -e "${BLUE}   Pattern: $FILE_PATTERN${NC}"
fi

echo ""

# ============================================================================
# STEP 3: Check or Format
# ============================================================================

if [ "$CHECK_ONLY" = true ]; then
    echo -e "${CYAN}3️⃣  Checking formatting (no changes will be made)...${NC}"

    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        # Check changed files only
        if echo "$CHANGED_FILES" | xargs npx prettier --check 2>&1 | tee /tmp/prettier-check.txt; then
            echo -e "${GREEN}   ✅ All files are properly formatted${NC}"
            EXIT_CODE=0
        else
            # Count files that need formatting
            NEEDS_FORMAT=$(grep -c "Code style issues found" /tmp/prettier-check.txt || grep -c "\[warn\]" /tmp/prettier-check.txt || echo "1")
            echo -e "${YELLOW}   ⚠️  ${NEEDS_FORMAT} file(s) need formatting${NC}"
            echo ""
            echo -e "${YELLOW}   To format these files, run:${NC}"
            echo -e "${CYAN}   bash /.claude/skills/code-format.md --changed-only${NC}"
            EXIT_CODE=1
        fi
    else
        # Check all files matching pattern
        if npx prettier --check "$FILE_PATTERN" 2>&1 | tee /tmp/prettier-check.txt; then
            echo -e "${GREEN}   ✅ All files are properly formatted${NC}"
            EXIT_CODE=0
        else
            # Count files that need formatting
            NEEDS_FORMAT=$(grep -c "\[warn\]" /tmp/prettier-check.txt || echo "multiple")
            echo -e "${YELLOW}   ⚠️  Files need formatting${NC}"
            echo ""
            echo -e "${YELLOW}   To format all files, run:${NC}"
            echo -e "${CYAN}   bash /.claude/skills/code-format.md${NC}"
            EXIT_CODE=1
        fi
    fi
else
    echo -e "${CYAN}3️⃣  Formatting files...${NC}"

    if [ "$CHANGED_ONLY" = true ] && [ -n "$CHANGED_FILES" ]; then
        # Format changed files only
        echo "$CHANGED_FILES" | xargs npx prettier --write 2>&1 | tee /tmp/prettier-write.txt
        FILES_FORMATTED=$(echo "$CHANGED_FILES" | wc -l)
    else
        # Format all files matching pattern
        npx prettier --write "$FILE_PATTERN" 2>&1 | tee /tmp/prettier-write.txt
        # Count files written (look for filename patterns in output)
        FILES_FORMATTED=$(grep -c "ms" /tmp/prettier-write.txt || echo "multiple")
    fi

    echo -e "${GREEN}   ✅ Formatting completed${NC}"
    EXIT_CODE=0
fi

echo ""

# ============================================================================
# STEP 4: Show What Changed (if formatting applied)
# ============================================================================

if [ "$CHECK_ONLY" = false ] && [ -n "$CHANGED_FILES" ]; then
    echo -e "${CYAN}4️⃣  Reviewing changes...${NC}"

    # Show git diff summary
    if git diff --stat 2>/dev/null | head -10; then
        echo ""
        echo -e "${BLUE}   Files were modified by formatting${NC}"
        echo -e "${YELLOW}   Review changes with: ${CYAN}git diff${NC}"
    else
        echo -e "${GREEN}   No changes made (files already formatted)${NC}"
    fi
    echo ""
fi

# ============================================================================
# STEP 5: Summary Report
# ============================================================================

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                   FORMATTING SUMMARY                           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

if [ "$CHECK_ONLY" = true ]; then
    if [ $EXIT_CODE -eq 0 ]; then
        echo -e "${GREEN}✅ STATUS: ALL FILES PROPERLY FORMATTED${NC}"
    else
        echo -e "${YELLOW}⚠️  STATUS: FORMATTING NEEDED${NC}"
        echo -e "${YELLOW}   Some files need to be formatted${NC}"
    fi
else
    echo -e "${GREEN}✅ STATUS: FORMATTING COMPLETE${NC}"
    echo -e "${GREEN}   Files formatted: ${FILES_FORMATTED}${NC}"
fi

echo ""
echo -e "${CYAN}Performance:${NC}"
echo -e "   Duration: ${DURATION}s"
echo -e "   Mode: $([ "$CHECK_ONLY" = true ] && echo "Check only" || echo "Format and write")"
echo -e "   Scope: $([ "$CHANGED_ONLY" = true ] && echo "Changed files" || echo "Full codebase")"
echo -e "   File type: ${FILE_TYPE}"
echo ""

# ============================================================================
# STEP 6: Next Steps
# ============================================================================

if [ $EXIT_CODE -ne 0 ]; then
    echo -e "${YELLOW}📋 Next Steps:${NC}"
    echo ""
    echo -e "1. Format the files:"
    if [ "$CHANGED_ONLY" = true ]; then
        echo -e "   ${CYAN}bash /.claude/skills/code-format.md --changed-only${NC}"
    else
        echo -e "   ${CYAN}bash /.claude/skills/code-format.md${NC}"
    fi
    echo ""
    echo -e "2. Review the changes:"
    echo -e "   ${CYAN}git diff${NC}"
    echo ""
    echo -e "3. Commit the formatted files:"
    echo -e "   ${CYAN}git add .${NC}"
    echo -e "   ${CYAN}git commit -m \"Format code with Prettier\"${NC}"
    echo ""
fi

if [ "$CHECK_ONLY" = false ]; then
    echo -e "${YELLOW}💡 Tip: Enable format-on-save in your editor${NC}"
    echo -e "   VS Code: \"editor.formatOnSave\": true"
    echo -e "   This skill runs automatically via pre-commit hook"
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
# 1. "No files matching pattern"
#    - Check: File pattern is correct for your file types
#    - Try: Use --changed-only to format only modified files
#
# 2. "Prettier not found"
#    - Install: npm install --save-dev prettier
#    - Or use global: npm install -g prettier
#
# 3. "Config file not found"
#    - Create: .prettierrc in project root
#    - Or use: Default Prettier configuration
#
# 4. Formatting conflicts with ESLint
#    - Install: npm install --save-dev eslint-config-prettier
#    - Add to: .eslintrc.json extends array
#
# 5. Large files taking too long
#    - Use: --changed-only flag for faster formatting
#    - Or: --type flag to format specific file types only
#
# USAGE EXAMPLES:
#
# Check if formatting needed:
#   bash /.claude/skills/code-format.md --check
#
# Format all files:
#   bash /.claude/skills/code-format.md
#
# Format changed files only:
#   bash /.claude/skills/code-format.md --changed-only
#
# Check changed files only:
#   bash /.claude/skills/code-format.md --check --changed-only
#
# Format TypeScript files only:
#   bash /.claude/skills/code-format.md --type ts
#
# Format JSON files only:
#   bash /.claude/skills/code-format.md --type json
#
# PERFORMANCE OPTIONS:
#
# - Full format: ~3 seconds
#   bash /.claude/skills/code-format.md
#
# - Changed files only: <1 second
#   bash /.claude/skills/code-format.md --changed-only
#
# - Check only (no writes): ~1 second
#   bash /.claude/skills/code-format.md --check
#
# INTEGRATION EXAMPLES:
#
# Pre-commit hook:
#   bash /.claude/skills/code-format.md --check --changed-only
#
# CI/CD pipeline:
#   bash /.claude/skills/code-format.md --check
#
# Before commit (manual):
#   bash /.claude/skills/code-format.md --changed-only
#
# CONFIGURATION:
#
# Default .prettierrc:
#   {
#     "semi": true,
#     "trailingComma": "es5",
#     "singleQuote": true,
#     "printWidth": 100,
#     "tabWidth": 2
#   }
#
# Files to ignore (.prettierignore):
#   node_modules/
#   dist/
#   build/
#   *.min.js
#   *.bundle.js
#
# ============================================================================

exit $EXIT_CODE
