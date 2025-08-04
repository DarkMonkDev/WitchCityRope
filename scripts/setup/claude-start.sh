#!/bin/bash

# WitchCityRope Claude Code Startup Script
# This ensures Context7 and Memory MCP servers are loaded

echo "Starting Claude Code with Context7 and Memory MCP servers..."

# Check if we're in the right directory
if [ ! -f "CLAUDE.md" ]; then
    echo "Warning: Not in WitchCityRope directory. Changing to project root..."
    cd /home/chad/repos/witchcityrope/WitchCityRope
fi

# Start Claude Code with MCP servers
# Using --no-strict ensures other tools remain available
claude --mcp-config /home/chad/.config/claude/mcp.json "$@"

# Alternative: If you want to use only the configured MCP servers
# claude mcp run "$@"