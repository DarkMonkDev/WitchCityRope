#!/bin/bash

echo "GitHub MCP Quick Status Check"
echo "============================"
echo ""

# Check if token is set in config
CONFIG_FILE="/mnt/c/users/chad/AppData/Roaming/Claude/claude_desktop_config.json"
if [ -f "$CONFIG_FILE" ]; then
    TOKEN=$(grep -o '"GITHUB_TOKEN": "[^"]*"' "$CONFIG_FILE" | cut -d'"' -f4)
    if [ -n "$TOKEN" ]; then
        echo "✅ GitHub token found in config"
        echo "   Token: ${TOKEN:0:10}...${TOKEN: -4}"
    else
        echo "❌ No GitHub token found in config"
    fi
else
    echo "❌ Config file not found at: $CONFIG_FILE"
fi

echo ""
echo "To run full test suite:"
echo "  node test-github-mcp-full.js"
echo ""
echo "To test API access only:"
echo "  node test-github-access.js"
echo ""
echo "For documentation:"
echo "  cat GITHUB_MCP_FIX_GUIDE.md"