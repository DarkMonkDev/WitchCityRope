#!/bin/bash

# Start UI Monitoring for WitchCityRope on Ubuntu
# This script starts Chrome with remote debugging for MCP browser automation

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
DEBUG_PORT=9222
OPEN_BROWSER=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --open-browser)
            OPEN_BROWSER=true
            shift
            ;;
        --port)
            DEBUG_PORT="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--open-browser] [--port PORT]"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}Starting UI Monitoring for WitchCityRope${NC}"

# Check if Chrome is installed
if ! command -v google-chrome &> /dev/null; then
    echo -e "${RED}Error: Google Chrome is not installed${NC}"
    echo "Install it with: sudo apt install google-chrome-stable"
    exit 1
fi

# Kill any existing Chrome debug instances
if pgrep -f "chrome.*remote-debugging-port" > /dev/null; then
    echo -e "${YELLOW}Stopping existing Chrome debug instances...${NC}"
    pkill -f "chrome.*remote-debugging-port" || true
    sleep 2
fi

# Check if port is available
if lsof -Pi :$DEBUG_PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo -e "${RED}Error: Port $DEBUG_PORT is already in use${NC}"
    echo "Use --port to specify a different port"
    exit 1
fi

# Start Chrome with remote debugging
echo -e "${GREEN}Starting Chrome with remote debugging on port $DEBUG_PORT...${NC}"
google-chrome \
    --remote-debugging-port=$DEBUG_PORT \
    --no-first-run \
    --no-default-browser-check \
    --disable-background-timer-throttling \
    --disable-backgrounding-occluded-windows \
    --disable-renderer-backgrounding \
    --user-data-dir="/tmp/chrome-debug-$USER" &

CHROME_PID=$!

# Wait for Chrome to start
echo "Waiting for Chrome to start..."
for i in {1..10}; do
    if curl -s http://localhost:$DEBUG_PORT/json/version > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Chrome is running and ready for debugging${NC}"
        break
    fi
    sleep 1
done

# Verify connection
if ! curl -s http://localhost:$DEBUG_PORT/json/version > /dev/null 2>&1; then
    echo -e "${RED}Error: Could not connect to Chrome debug port${NC}"
    exit 1
fi

# Get Chrome version info
CHROME_VERSION=$(curl -s http://localhost:$DEBUG_PORT/json/version | grep -o '"Browser":"[^"]*"' | cut -d'"' -f4)
echo -e "${GREEN}Chrome Version: $CHROME_VERSION${NC}"

# Export environment variables
export LOCAL_CDP_URL="http://localhost:$DEBUG_PORT"
export CHROME_DEBUG_URL="http://localhost:$DEBUG_PORT"

echo -e "\n${GREEN}Environment variables set:${NC}"
echo "  LOCAL_CDP_URL=$LOCAL_CDP_URL"
echo "  CHROME_DEBUG_URL=$CHROME_DEBUG_URL"

# Open the application if requested
if [ "$OPEN_BROWSER" = true ]; then
    echo -e "\n${GREEN}Opening WitchCityRope in browser...${NC}"
    sleep 2
    xdg-open "https://localhost:5652" 2>/dev/null || true
fi

echo -e "\n${GREEN}UI Monitoring is ready!${NC}"
echo "Chrome PID: $CHROME_PID"
echo "Debug URL: http://localhost:$DEBUG_PORT"
echo -e "\nTo stop monitoring, run: ${YELLOW}kill $CHROME_PID${NC}"

# Keep script running to maintain Chrome process
echo -e "\n${YELLOW}Press Ctrl+C to stop monitoring${NC}"
wait $CHROME_PID