#!/bin/bash
# Create new worktree with full environment setup
# Usage: ./create-worktree.sh <branch-name> [worktree-name]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
MAIN_REPO="/home/chad/repos/witchcityrope-react"
WORKTREE_BASE="/home/chad/repos/witchcityrope-worktrees"

# Parse arguments
BRANCH_NAME=$1
WORKTREE_NAME=${2:-$BRANCH_NAME}

if [ -z "$BRANCH_NAME" ]; then
    echo -e "${RED}Error: Branch name required${NC}"
    echo "Usage: $0 <branch-name> [worktree-name]"
    echo "Example: $0 feature/2025-08-23-auth feature-2025-08-23-auth"
    exit 1
fi

# Create worktree base directory if it doesn't exist
mkdir -p "$WORKTREE_BASE"

# Full worktree path
WORKTREE_PATH="$WORKTREE_BASE/$WORKTREE_NAME"

echo -e "${YELLOW}Creating worktree...${NC}"
echo "Branch: $BRANCH_NAME"
echo "Path: $WORKTREE_PATH"

# Create the worktree
cd "$MAIN_REPO"
git worktree add "$WORKTREE_PATH" -b "$BRANCH_NAME" || {
    echo -e "${RED}Failed to create worktree${NC}"
    echo "Trying to create from existing branch..."
    git worktree add "$WORKTREE_PATH" "$BRANCH_NAME" || exit 1
}

echo -e "${GREEN}✅ Worktree created${NC}"

# Setup environment
echo -e "${YELLOW}Setting up environment...${NC}"
cd "$WORKTREE_PATH"

# Copy environment files
if [ -f "$MAIN_REPO/.env" ]; then
    cp "$MAIN_REPO/.env" .
    echo -e "${GREEN}✅ Copied .env${NC}"
else
    echo -e "${YELLOW}⚠️  No .env file found in main repo${NC}"
fi

if [ -f "$MAIN_REPO/.env.local" ]; then
    cp "$MAIN_REPO/.env.local" .
    echo -e "${GREEN}✅ Copied .env.local${NC}"
fi

if [ -f "$MAIN_REPO/.env.development" ]; then
    cp "$MAIN_REPO/.env.development" .
    echo -e "${GREEN}✅ Copied .env.development${NC}"
fi

# Install dependencies
echo -e "${YELLOW}Installing dependencies...${NC}"
npm install

# Calculate port offset based on worktree number
PORT_OFFSET=$(git worktree list | grep -c "witchcityrope-worktrees")
WEB_PORT=$((5173 + PORT_OFFSET))
API_PORT=$((5653 + PORT_OFFSET))

# Create port configuration
echo "" >> .env.local
echo "# Worktree-specific ports" >> .env.local
echo "PORT=$WEB_PORT" >> .env.local
echo "VITE_API_PORT=$API_PORT" >> .env.local

echo -e "${GREEN}✅ Environment setup complete${NC}"
echo ""
echo -e "${GREEN}Worktree ready at: $WORKTREE_PATH${NC}"
echo -e "Web port: ${YELLOW}$WEB_PORT${NC}"
echo -e "API port: ${YELLOW}$API_PORT${NC}"
echo ""
echo "To start working:"
echo "  cd $WORKTREE_PATH"
echo "  npm run dev"
echo ""
echo -e "${YELLOW}Remember to cleanup after PR merge:${NC}"
echo "  git worktree remove $WORKTREE_PATH"