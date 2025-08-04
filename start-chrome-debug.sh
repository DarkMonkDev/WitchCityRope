#!/bin/bash
# Script to start Chrome in debug mode for Stagehand MCP

echo -e "\033[0;32mStarting Chrome in debug mode for Stagehand MCP...\033[0m"

# Determine Chrome executable path based on OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux
    if command -v google-chrome &> /dev/null; then
        CHROME_PATH="google-chrome"
    elif command -v google-chrome-stable &> /dev/null; then
        CHROME_PATH="google-chrome-stable"
    elif command -v chromium-browser &> /dev/null; then
        CHROME_PATH="chromium-browser"
    elif command -v chromium &> /dev/null; then
        CHROME_PATH="chromium"
    else
        echo -e "\033[0;31mError: Chrome/Chromium not found. Please install Chrome or Chromium.\033[0m"
        exit 1
    fi
elif [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    CHROME_PATH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
    if [ ! -f "$CHROME_PATH" ]; then
        echo -e "\033[0;31mError: Chrome not found at $CHROME_PATH\033[0m"
        exit 1
    fi
else
    echo -e "\033[0;31mError: Unsupported OS type: $OSTYPE\033[0m"
    exit 1
fi

# Create temporary user data directory
USER_DATA_DIR="/tmp/chrome-debug-$(date +%s)"
mkdir -p "$USER_DATA_DIR"

# Start Chrome with remote debugging
echo "Starting Chrome with remote debugging on port 9222..."
echo "User data directory: $USER_DATA_DIR"

"$CHROME_PATH" \
    --remote-debugging-port=9222 \
    --user-data-dir="$USER_DATA_DIR" \
    --no-first-run \
    --no-default-browser-check &

CHROME_PID=$!

echo -e "\033[0;32mChrome started with PID: $CHROME_PID\033[0m"
echo -e "\033[0;32mRemote debugging available on port 9222\033[0m"
echo -e "\033[1;33mYou can now use Stagehand in Claude Desktop\033[0m"
echo ""
echo "Press Ctrl+C to stop Chrome and cleanup..."

# Trap Ctrl+C to cleanup
trap cleanup INT

cleanup() {
    echo ""
    echo "Stopping Chrome..."
    kill $CHROME_PID 2>/dev/null
    wait $CHROME_PID 2>/dev/null
    echo "Cleaning up temporary directory..."
    rm -rf "$USER_DATA_DIR"
    echo "Done."
    exit 0
}

# Wait for Chrome process
wait $CHROME_PID