#!/bin/bash

# ================================================
# CLAUDE CODE PRE-FLIGHT VALIDATION SCRIPT
# ================================================
# Purpose: Prevent documentation/process failures before they happen
# Must run at the start of EVERY Claude Code session
# 
# This script prevents common documentation and process failures

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "================================================"
echo "🚀 CLAUDE CODE PRE-FLIGHT VALIDATION"
echo "================================================"
echo ""

# Track validation results
ERRORS=0
WARNINGS=0

# ================================================
# 1. CHECK CURRENT DIRECTORY
# ================================================
echo -e "${BLUE}[1/8] Checking current directory...${NC}"
CURRENT_DIR=$(pwd)
EXPECTED_DIR="/home/chad/repos/witchcityrope"

if [ "$CURRENT_DIR" != "$EXPECTED_DIR" ]; then
    echo -e "${RED}✗ ERROR: Wrong directory!${NC}"
    echo "  Current:  $CURRENT_DIR"
    echo "  Expected: $EXPECTED_DIR"
    ERRORS=$((ERRORS + 1))
else
    echo -e "${GREEN}✓ Correct directory${NC}"
fi

# ================================================
# 2. CHECK GIT BRANCH STATUS
# ================================================
echo -e "${BLUE}[2/8] Checking git branch status...${NC}"
CURRENT_BRANCH=$(git branch --show-current)

echo "  Current branch: $CURRENT_BRANCH"

# ================================================
# 3. CHECK FOR UNCOMMITTED CHANGES
# ================================================
echo -e "${BLUE}[3/8] Checking for uncommitted changes...${NC}"
STAGED_COUNT=$(git diff --cached --numstat | wc -l)
UNSTAGED_COUNT=$(git diff --numstat | wc -l)
UNTRACKED_COUNT=$(git ls-files --others --exclude-standard | wc -l)

if [ "$STAGED_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠ WARNING: $STAGED_COUNT staged files${NC}"
    WARNINGS=$((WARNINGS + 1))
fi

if [ "$UNSTAGED_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠ WARNING: $UNSTAGED_COUNT unstaged changes${NC}"
    WARNINGS=$((WARNINGS + 1))
fi

if [ "$UNTRACKED_COUNT" -gt 0 ]; then
    echo "  $UNTRACKED_COUNT untracked files"
fi

# ================================================
# 4. CHECK DOCKER STATUS
# ================================================
echo -e "${BLUE}[4/8] Checking Docker status...${NC}"
if docker compose ps 2>/dev/null | grep -q "Up"; then
    echo -e "${GREEN}✓ Docker services running${NC}"
    
else
    echo "  Docker services not running (run ./dev.sh to start)"
fi

# ================================================
# 5. CHECK CRITICAL FILES EXIST
# ================================================
echo -e "${BLUE}[5/8] Checking critical files...${NC}"
CRITICAL_FILES=(
    "CLAUDE.md"
    "docs/architecture/functional-area-master-index.md"
    "docs/architecture/file-registry.md"
    "docs/standards-processes/documentation-process/DOCUMENTATION_GUIDE.md"
    ".env"
    "docker-compose.yml"
)

for file in "${CRITICAL_FILES[@]}"; do
    if [ ! -f "$file" ]; then
        echo -e "${RED}✗ Missing: $file${NC}"
        ERRORS=$((ERRORS + 1))
    fi
done
echo -e "${GREEN}✓ Critical files present${NC}"

# ================================================
# 6. CHECK DOCUMENTATION STRUCTURE
# ================================================
echo -e "${BLUE}[6/8] Checking documentation structure...${NC}"
ROOT_DOCS_COUNT=$(find docs -maxdepth 1 -type f 2>/dev/null | wc -l)

if [ "$ROOT_DOCS_COUNT" -gt 0 ]; then
    echo -e "${RED}✗ ERROR: $ROOT_DOCS_COUNT files in /docs/ root (should be 0)${NC}"
    echo -e "${RED}  Files must be in subdirectories, not root!${NC}"
    ERRORS=$((ERRORS + 1))
else
    echo -e "${GREEN}✓ No files in /docs/ root${NC}"
fi

# ================================================
# 7. CHECK SESSION HANDOFF
# ================================================
echo -e "${BLUE}[7/8] Checking for session handoff...${NC}"
TODAY=$(date +%Y-%m-%d)
YESTERDAY=$(date -d "yesterday" +%Y-%m-%d)
HANDOFF_EXISTS=false

if ls docs/lessons-learned/*"$TODAY"* 2>/dev/null || ls docs/lessons-learned/*"$YESTERDAY"* 2>/dev/null; then
    echo -e "${GREEN}✓ Recent handoff document found${NC}"
    HANDOFF_EXISTS=true
else
    echo -e "${YELLOW}⚠ WARNING: No recent handoff document${NC}"
    echo "  Consider reviewing previous session work"
    WARNINGS=$((WARNINGS + 1))
fi

# ================================================
# 8. CHECK STASHED WORK
# ================================================
echo -e "${BLUE}[8/8] Checking for stashed work...${NC}"
STASH_COUNT=$(git stash list | wc -l)

if [ "$STASH_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠ WARNING: $STASH_COUNT stashes exist${NC}"
    echo "  Review with: git stash list"
    WARNINGS=$((WARNINGS + 1))
else
    echo -e "${GREEN}✓ No stashed work${NC}"
fi

# ================================================
# FINAL REPORT
# ================================================
echo ""
echo "================================================"
echo "📊 VALIDATION SUMMARY"
echo "================================================"

if [ "$ERRORS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
    echo -e "${GREEN}✅ ALL CHECKS PASSED - Safe to proceed${NC}"
    exit 0
elif [ "$ERRORS" -eq 0 ]; then
    echo -e "${YELLOW}⚠ WARNINGS: $WARNINGS issues need attention${NC}"
    echo ""
    echo "Recommended actions:"
    
    
    if [ "$STAGED_COUNT" -gt 0 ] || [ "$UNSTAGED_COUNT" -gt 0 ]; then
        echo "  • Review uncommitted changes with: git status"
        echo "  • Commit or stash changes before proceeding"
    fi
    
    if [ "$HANDOFF_EXISTS" = false ]; then
        echo "  • Review CONTINUATION-GUIDE.md for context"
    fi
    
    if [ "$STASH_COUNT" -gt 0 ]; then
        echo "  • Check stashes for lost work: git stash list"
    fi
    
    exit 0
else
    echo -e "${RED}❌ ERRORS: $ERRORS critical issues must be fixed${NC}"
    echo -e "${YELLOW}⚠ WARNINGS: $WARNINGS issues need attention${NC}"
    echo ""
    echo -e "${RED}STOP: Do not proceed until errors are resolved!${NC}"
    echo ""
    echo "Required fixes:"
    
    if [ "$CURRENT_DIR" != "$EXPECTED_DIR" ]; then
        echo "  • Change to correct directory: cd $EXPECTED_DIR"
    fi
    
    
    if [ "$ROOT_DOCS_COUNT" -gt 0 ]; then
        echo "  • Move files from /docs/ root to appropriate subdirectories"
    fi
    
    exit 1
fi