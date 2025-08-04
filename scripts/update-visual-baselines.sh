#!/bin/bash

# Script to update Playwright visual regression baselines
# Usage: ./scripts/update-visual-baselines.sh [options]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
TEST_PATH=""
BROWSER=""
PROJECT=""
HEADED=false

# Function to display usage
usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -t, --test <path>      Update baselines for specific test file or directory"
    echo "  -b, --browser <name>   Run with specific browser (chromium, firefox, webkit)"
    echo "  -p, --project <name>   Run specific project from playwright.config.ts"
    echo "  -h, --headed           Run in headed mode to see the browser"
    echo "  --help                 Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Update all baselines"
    echo "  $0 -t tests/visual/home.spec.ts      # Update specific test"
    echo "  $0 -b chromium                       # Update for chromium only"
    echo "  $0 -p mobile                         # Update for mobile project"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--test)
            TEST_PATH="$2"
            shift 2
            ;;
        -b|--browser)
            BROWSER="$2"
            shift 2
            ;;
        -p|--project)
            PROJECT="$2"
            shift 2
            ;;
        -h|--headed)
            HEADED=true
            shift
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            exit 1
            ;;
    esac
done

# Build playwright command
PLAYWRIGHT_CMD="npx playwright test"

# Add test path if specified
if [ -n "$TEST_PATH" ]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD $TEST_PATH"
fi

# Add browser if specified
if [ -n "$BROWSER" ]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --browser=$BROWSER"
fi

# Add project if specified
if [ -n "$PROJECT" ]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --project=$PROJECT"
fi

# Add headed mode if specified
if [ "$HEADED" = true ]; then
    PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --headed"
fi

# Always add update snapshots flag
PLAYWRIGHT_CMD="$PLAYWRIGHT_CMD --update-snapshots"

# Display what we're about to do
echo -e "${YELLOW}Updating visual regression baselines...${NC}"
echo -e "${YELLOW}Command: $PLAYWRIGHT_CMD${NC}"
echo ""

# Backup existing baselines (optional)
BACKUP_DIR="tests/playwright/visual-regression/__screenshots__.backup.$(date +%Y%m%d_%H%M%S)"
if [ -d "tests/playwright/visual-regression/__screenshots__" ]; then
    echo -e "${YELLOW}Backing up existing baselines to: $BACKUP_DIR${NC}"
    cp -r tests/playwright/visual-regression/__screenshots__ "$BACKUP_DIR"
fi

# Run the playwright command
if $PLAYWRIGHT_CMD; then
    echo ""
    echo -e "${GREEN}✓ Visual baselines updated successfully!${NC}"
    
    # Show git status for the updated files
    echo ""
    echo -e "${YELLOW}Updated baseline files:${NC}"
    git status --porcelain tests/playwright/visual-regression/__screenshots__ 2>/dev/null | grep -E "^\?\?|^ M" || echo "No changes detected"
    
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Review the updated baselines"
    echo "2. Run tests again to verify they pass: npx playwright test"
    echo "3. Commit the baseline changes: git add tests/playwright/visual-regression/__screenshots__"
    echo ""
    
    # Cleanup old backup if everything succeeded
    if [ -d "$BACKUP_DIR" ]; then
        read -p "Remove backup directory? (y/N) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            rm -rf "$BACKUP_DIR"
            echo -e "${GREEN}Backup removed${NC}"
        else
            echo -e "${YELLOW}Backup kept at: $BACKUP_DIR${NC}"
        fi
    fi
else
    echo ""
    echo -e "${RED}✗ Failed to update baselines${NC}"
    
    # Restore backup if it exists
    if [ -d "$BACKUP_DIR" ]; then
        echo -e "${YELLOW}Restoring from backup...${NC}"
        rm -rf tests/playwright/visual-regression/__screenshots__
        mv "$BACKUP_DIR" tests/playwright/visual-regression/__screenshots__
        echo -e "${GREEN}Backup restored${NC}"
    fi
    
    exit 1
fi