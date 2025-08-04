#!/bin/bash
# launch-chrome-incognito.sh - Universal Chrome launcher for Linux
# Always launches Chrome in incognito mode with remote debugging

echo "Chrome Universal Launcher - Always Incognito"
echo "==========================================="
echo

# Configuration
CHROME_PORT=9222
USER_DATA_DIR="/tmp/chrome-automation-$$"

# Find Chrome executable
if command -v google-chrome &> /dev/null; then
    CHROME_PATH="google-chrome"
elif command -v google-chrome-stable &> /dev/null; then
    CHROME_PATH="google-chrome-stable"
elif command -v chromium &> /dev/null; then
    CHROME_PATH="chromium"
elif command -v chromium-browser &> /dev/null; then
    CHROME_PATH="chromium-browser"
else
    echo "ERROR: Chrome/Chromium not found in PATH"
    echo "Please install Google Chrome or Chromium."
    read -p "Press any key to exit..."
    exit 1
fi

# Kill any existing Chrome debug instances
echo "[1/3] Killing existing Chrome debug instances..."
pkill -f "chrome.*remote-debugging-port=$CHROME_PORT" 2>/dev/null

# Also try to kill by port
if lsof -i :$CHROME_PORT &> /dev/null; then
    lsof -ti :$CHROME_PORT | xargs kill -9 2>/dev/null
fi

sleep 2

# Launch Chrome with debugging and incognito (ALWAYS)
echo "[2/3] Launching Chrome in INCOGNITO mode with debugging on port $CHROME_PORT..."
$CHROME_PATH \
    --remote-debugging-port=$CHROME_PORT \
    --incognito \
    --no-first-run \
    --no-default-browser-check \
    --disable-popup-blocking \
    --disable-translate \
    --disable-background-timer-throttling \
    --disable-renderer-backgrounding \
    --disable-device-discovery-notifications \
    --user-data-dir="$USER_DATA_DIR" &

# Wait for Chrome to start
echo "[3/3] Waiting for Chrome to start..."
sleep 3

# Check if Chrome started successfully
if lsof -i :$CHROME_PORT &> /dev/null; then
    echo
    echo "SUCCESS! Chrome is running:"
    echo "  - Mode: INCOGNITO (Private Browsing)"
    echo "  - Debug Port: $CHROME_PORT"
    echo "  - Remote debugging: http://localhost:$CHROME_PORT"
    echo
    echo "Ready for automation with:"
    echo "  - Browser Tools MCP"
    echo "  - Stagehand MCP"
    echo
else
    echo
    echo "WARNING: Chrome may not have started properly"
    echo
    echo "Troubleshooting:"
    echo "  1. Check if port $CHROME_PORT is already in use"
    echo "  2. Try running with sudo"
    echo "  3. Check firewall settings"
    echo
fi

read -p "Press any key to exit..."