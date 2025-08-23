#!/bin/bash
# Show status of all worktrees
# Identify stale or ready-to-cleanup worktrees

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

MAIN_REPO="/home/chad/repos/witchcityrope-react"
WORKTREE_BASE="/home/chad/repos/witchcityrope-worktrees"

echo -e "${BLUE}==================================${NC}"
echo -e "${BLUE}    Git Worktree Status Report    ${NC}"
echo -e "${BLUE}==================================${NC}"
echo ""

cd "$MAIN_REPO"

# Get list of worktrees
WORKTREES=$(git worktree list --porcelain | grep "^worktree " | cut -d' ' -f2)

if [ -z "$WORKTREES" ]; then
    echo -e "${GREEN}No worktrees found. Only main repository exists.${NC}"
    exit 0
fi

# Count worktrees
WORKTREE_COUNT=$(echo "$WORKTREES" | grep -c "witchcityrope-worktrees" || echo "0")
echo -e "Active worktrees: ${YELLOW}$WORKTREE_COUNT${NC}"
echo ""

# Check each worktree
for WORKTREE in $WORKTREES; do
    # Skip main repository
    if [[ "$WORKTREE" == "$MAIN_REPO" ]]; then
        continue
    fi
    
    echo -e "${YELLOW}──────────────────────────────────${NC}"
    echo -e "${BLUE}Worktree:${NC} $(basename $WORKTREE)"
    echo -e "${BLUE}Path:${NC} $WORKTREE"
    
    # Get branch name
    BRANCH=$(git -C "$WORKTREE" branch --show-current 2>/dev/null || echo "unknown")
    echo -e "${BLUE}Branch:${NC} $BRANCH"
    
    # Check if branch is merged
    if [ "$BRANCH" != "unknown" ]; then
        if git branch --merged master | grep -q "^  $BRANCH$"; then
            echo -e "${RED}Status: STALE (branch merged to master - ready for cleanup)${NC}"
        else
            echo -e "${GREEN}Status: ACTIVE (branch not merged)${NC}"
        fi
    fi
    
    # Check for uncommitted changes
    if [ -d "$WORKTREE" ]; then
        CHANGES=$(git -C "$WORKTREE" status --porcelain 2>/dev/null | wc -l)
        if [ "$CHANGES" -gt 0 ]; then
            echo -e "${YELLOW}⚠️  Uncommitted changes: $CHANGES files${NC}"
        else
            echo -e "${GREEN}✅ Working directory clean${NC}"
        fi
        
        # Check environment setup
        if [ -f "$WORKTREE/.env" ]; then
            echo -e "${GREEN}✅ Environment configured${NC}"
        else
            echo -e "${RED}❌ Missing .env file${NC}"
        fi
        
        if [ -d "$WORKTREE/node_modules" ]; then
            echo -e "${GREEN}✅ Dependencies installed${NC}"
        else
            echo -e "${RED}❌ Dependencies not installed${NC}"
        fi
        
        # Check disk usage
        if command -v du >/dev/null 2>&1; then
            SIZE=$(du -sh "$WORKTREE" 2>/dev/null | cut -f1)
            echo -e "${BLUE}Disk usage:${NC} $SIZE"
        fi
    else
        echo -e "${RED}❌ Directory not found${NC}"
    fi
done

echo -e "${YELLOW}──────────────────────────────────${NC}"
echo ""

# Summary and recommendations
echo -e "${BLUE}Summary:${NC}"

# Check for stale worktrees
STALE_COUNT=$(
    for WORKTREE in $WORKTREES; do
        if [[ "$WORKTREE" == "$MAIN_REPO" ]]; then
            continue
        fi
        BRANCH=$(git -C "$WORKTREE" branch --show-current 2>/dev/null || echo "")
        if [ -n "$BRANCH" ] && git branch --merged master | grep -q "^  $BRANCH$"; then
            echo "stale"
        fi
    done | wc -l
)

if [ "$STALE_COUNT" -gt 0 ]; then
    echo -e "${RED}⚠️  $STALE_COUNT stale worktree(s) found - cleanup recommended${NC}"
    echo ""
    echo "To cleanup stale worktrees, run:"
    for WORKTREE in $WORKTREES; do
        if [[ "$WORKTREE" == "$MAIN_REPO" ]]; then
            continue
        fi
        BRANCH=$(git -C "$WORKTREE" branch --show-current 2>/dev/null || echo "")
        if [ -n "$BRANCH" ] && git branch --merged master | grep -q "^  $BRANCH$"; then
            echo "  git worktree remove $WORKTREE"
        fi
    done
else
    echo -e "${GREEN}✅ No stale worktrees found${NC}"
fi

if [ "$WORKTREE_COUNT" -gt 3 ]; then
    echo -e "${YELLOW}⚠️  More than 3 active worktrees - consider cleanup${NC}"
elif [ "$WORKTREE_COUNT" -eq 0 ]; then
    echo -e "${GREEN}✅ No worktrees active - ready for new work${NC}"
else
    echo -e "${GREEN}✅ Worktree count within recommended limit (<= 3)${NC}"
fi