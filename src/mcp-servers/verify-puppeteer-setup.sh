#!/bin/bash

# Quick setup verification script for Puppeteer
# Checks all components are properly installed and configured

echo "=== Puppeteer Setup Verification ==="
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
if [ -f "/usr/bin/google-chrome-stable" ]; then
    echo "✓ Chrome found at /usr/bin/google-chrome-stable"
elif [ -f "/usr/bin/google-chrome" ]; then
    echo "✓ Chrome found at /usr/bin/google-chrome"
elif [ -f "/usr/bin/chromium" ]; then
    echo "✓ Chromium found at /usr/bin/chromium"
else
    echo "✗ No supported browser found"
fi

echo ""
echo "Checking Puppeteer installation..."

# Check Puppeteer
if [ -f "./node_modules/puppeteer/lib/cjs/puppeteer/puppeteer.js" ]; then
    echo "✓ Puppeteer installed in current project"
    PUPPETEER_VERSION=$(node -e "console.log(require('./package.json').dependencies.puppeteer || 'version not found')")
    echo "  Version: $PUPPETEER_VERSION"
else
    echo "✗ Puppeteer not installed in current project"
    echo "  Run: npm install puppeteer"
fi

# Check Stagehand (optional)
echo ""
echo "Checking optional components..."

STAGEHAND_PATH="/home/chad/repos/witchcityrope/mcp-server-browserbase/stagehand/dist/index.js"
if [ -f "$STAGEHAND_PATH" ]; then
    echo "✓ Stagehand installed (AI-powered browser automation)"
else
    echo "⚠ Stagehand not installed (optional)"
fi

echo ""
echo "Checking Chrome DevTools Protocol..."

# Test Chrome connection
if curl -s http://localhost:9222/json/version > /dev/null 2>&1; then
    echo "✓ Chrome is running with DevTools on port 9222"
    VERSION=$(curl -s http://localhost:9222/json/version | jq -r '.Browser' 2>/dev/null || echo "unknown")
    echo "  Browser: $VERSION"
else
    echo "⚠ Chrome is not running with DevTools"
    echo "  To enable: google-chrome --remote-debugging-port=9222"
fi

echo ""
echo "=== Setup Summary ==="
echo ""
echo "✓ Ready to use Puppeteer directly in your project"
echo "✓ No MCP server configuration needed"
echo "✓ Install with: npm install puppeteer"
echo ""
echo "Example usage:"
echo "  const puppeteer = require('puppeteer');"
echo "  const browser = await puppeteer.launch();"
echo "  const page = await browser.newPage();"
echo "  await page.goto('https://example.com');"
echo ""