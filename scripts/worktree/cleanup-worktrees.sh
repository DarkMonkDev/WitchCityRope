#!/bin/bash
# Remove worktrees for merged branches
# Clean up environment files and directories
# This script is called during Phase 5 finalization

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
echo -e "${BLUE}   Worktree Cleanup (Phase 5)     ${NC}"
echo -e "${BLUE}==================================${NC}"
echo ""

cd "$MAIN_REPO"

# Check if specific worktree provided
SPECIFIC_WORKTREE=$1

if [ -n "$SPECIFIC_WORKTREE" ]; then
    # Cleanup specific worktree
    echo -e "${YELLOW}Cleaning up specific worktree: $SPECIFIC_WORKTREE${NC}"
    
    WORKTREE_PATH="$WORKTREE_BASE/$SPECIFIC_WORKTREE"
    
    if [ ! -d "$WORKTREE_PATH" ]; then
        echo -e "${RED}Error: Worktree not found at $WORKTREE_PATH${NC}"
        exit 1
    fi
    
    # Get branch name
    BRANCH=$(git -C "$WORKTREE_PATH" branch --show-current 2>/dev/null || echo "")
    
    # Check if merged
    if [ -n "$BRANCH" ]; then
        if ! git branch --merged master | grep -q "^  $BRANCH$"; then
            echo -e "${YELLOW}⚠️  Warning: Branch $BRANCH is not merged to master${NC}"
            read -p "Continue with cleanup anyway? (y/N) " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                echo -e "${RED}Cleanup cancelled${NC}"
                exit 1
            fi
        fi
    fi
    
    # Check for uncommitted changes
    CHANGES=$(git -C "$WORKTREE_PATH" status --porcelain 2>/dev/null | wc -l || echo "0")
    if [ "$CHANGES" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  Warning: $CHANGES uncommitted changes in worktree${NC}"
        echo "Stashing changes..."
        git -C "$WORKTREE_PATH" stash push -m "Auto-stash before worktree removal"
    fi
    
    # Remove worktree
    echo -e "${YELLOW}Removing worktree...${NC}"
    git worktree remove "$WORKTREE_PATH" --force 2>/dev/null || {
        echo -e "${YELLOW}Git worktree remove failed, cleaning manually...${NC}"
        rm -rf "$WORKTREE_PATH"
    }
    
    # Prune worktree references
    git worktree prune
    
    echo -e "${GREEN}✅ Worktree $SPECIFIC_WORKTREE cleaned up${NC}"
    
else
    # Cleanup all merged worktrees
    echo -e "${YELLOW}Scanning for merged worktrees...${NC}"
    echo ""
    
    # Get list of worktrees
    WORKTREES=$(git worktree list --porcelain | grep "^worktree " | cut -d' ' -f2)
    CLEANED_COUNT=0
    
    for WORKTREE in $WORKTREES; do
        # Skip main repository
        if [[ "$WORKTREE" == "$MAIN_REPO" ]]; then
            continue
        fi
        
        # Skip if not in worktrees directory
        if [[ "$WORKTREE" != *"witchcityrope-worktrees"* ]]; then
            continue
        fi
        
        # Get branch name
        BRANCH=$(git -C "$WORKTREE" branch --show-current 2>/dev/null || echo "")
        
        if [ -z "$BRANCH" ]; then
            echo -e "${YELLOW}Skipping $WORKTREE - cannot determine branch${NC}"
            continue
        fi
        
        # Check if branch is merged
        if git branch --merged master | grep -q "^  $BRANCH$"; then
            echo -e "${BLUE}Found merged worktree:${NC}"
            echo "  Path: $WORKTREE"
            echo "  Branch: $BRANCH"
            
            # Check for uncommitted changes
            CHANGES=$(git -C "$WORKTREE" status --porcelain 2>/dev/null | wc -l || echo "0")
            if [ "$CHANGES" -gt 0 ]; then
                echo -e "${YELLOW}  ⚠️  Has $CHANGES uncommitted changes - stashing${NC}"
                git -C "$WORKTREE" stash push -m "Auto-stash before worktree removal"
            fi
            
            # Remove worktree
            echo -e "${YELLOW}  Removing...${NC}"
            git worktree remove "$WORKTREE" --force 2>/dev/null || {
                echo -e "${YELLOW}  Git worktree remove failed, cleaning manually...${NC}"
                rm -rf "$WORKTREE"
            }
            
            echo -e "${GREEN}  ✅ Cleaned${NC}"
            echo ""
            
            CLEANED_COUNT=$((CLEANED_COUNT + 1))
        fi
    done
    
    # Prune worktree references
    git worktree prune
    
    if [ "$CLEANED_COUNT" -eq 0 ]; then
        echo -e "${GREEN}No merged worktrees found - nothing to clean${NC}"
    else
        echo -e "${GREEN}✅ Cleaned $CLEANED_COUNT worktree(s)${NC}"
    fi
fi

# Final status check
echo ""
echo -e "${BLUE}Final worktree status:${NC}"
REMAINING=$(git worktree list | grep -c "witchcityrope-worktrees" || echo "0")
echo -e "Remaining worktrees: ${YELLOW}$REMAINING${NC}"

if [ "$REMAINING" -gt 3 ]; then
    echo -e "${YELLOW}⚠️  Warning: More than 3 worktrees active${NC}"
elif [ "$REMAINING" -eq 0 ]; then
    echo -e "${GREEN}✅ All worktrees cleaned - ready for new work${NC}"
else
    echo -e "${GREEN}✅ Worktree count within limit${NC}"
fi

# Check if worktree base directory is empty
if [ -d "$WORKTREE_BASE" ]; then
    if [ -z "$(ls -A $WORKTREE_BASE)" ]; then
        echo -e "${BLUE}Worktree base directory is empty${NC}"
        # Could remove it, but keeping for future use
    fi
fi

echo ""
echo -e "${GREEN}Cleanup complete!${NC}"