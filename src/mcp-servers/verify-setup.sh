#!/bin/bash

# Quick setup verification script
# Checks all components are properly installed and configured

echo "=== Browser Automation Setup Verification ==="
echo ""

# Check for required commands
echo "Checking dependencies..."

if command -v node &> /dev/null; then
    echo "✓ Node.js: $(node --version)"
else
    echo "✗ Node.js not found"
fi

if command -v npm &> /dev/null; then
    echo "✓ npm: $(npm --version)"
else
    echo "✗ npm not found"
fi

if command -v curl &> /dev/null; then
    echo "✓ curl: installed"
else
    echo "✗ curl not found"
fi

if command -v jq &> /dev/null; then
    echo "✓ jq: installed"
else
    echo "⚠ jq not found (optional, for better JSON output)"
fi

echo ""
echo "Checking browser availability..."

# Check for Chrome
if [ -f "/mnt/c/Program Files/Google/Chrome/Application/chrome.exe" ]; then
    echo "✓ Chrome found"
elif [ -f "/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" ]; then
    echo "✓ Edge found"
else
    echo "✗ No supported browser found"
fi

echo ""
echo "Checking MCP servers..."

# Check Browser Tools MCP
if [ -f "./node_modules/.bin/browser-tools-mcp" ]; then
    echo "✓ Browser Tools MCP installed"
else
    echo "✗ Browser Tools MCP not installed"
    echo "  Run: npm install browser-tools-mcp"
fi

# Check Stagehand
STAGEHAND_PATH="/mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand/dist/index.js"
if [ -f "$STAGEHAND_PATH" ]; then
    echo "✓ Stagehand installed"
else
    echo "⚠ Stagehand not installed (optional)"
fi

echo ""
echo "Checking scripts..."

# Check required scripts
for script in start-browser-automation.sh stop-browser-automation.sh test-browser-automation.js; do
    if [ -f "$script" ] && [ -x "$script" ]; then
        echo "✓ $script is executable"
    else
        echo "✗ $script not found or not executable"
    fi
done

echo ""
echo "Checking Claude configuration..."

CLAUDE_CONFIG="/mnt/c/Users/chad/AppData/Roaming/Claude/claude_desktop_config.json"
if [ -f "$CLAUDE_CONFIG" ]; then
    echo "✓ Claude configuration found"
    
    # Check if browser-tools is configured
    if grep -q "browser-tools" "$CLAUDE_CONFIG"; then
        echo "✓ Browser tools configured in Claude"
    else
        echo "⚠ Browser tools not found in Claude config"
        echo "  See claude-config-browser-automation.json for reference"
    fi
else
    echo "✗ Claude configuration not found"
fi

echo ""
echo "=== Setup verification complete ==="
echo ""
echo "To start using browser automation:"
echo "  ./start-browser-automation.sh"
echo ""
echo "For detailed documentation:"
echo "  cat BROWSER_AUTOMATION_GUIDE.md"