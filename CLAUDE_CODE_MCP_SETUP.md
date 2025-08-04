# Claude Code MCP Setup Guide

## Important: Context7 and Memory MCP for All Sessions

This guide ensures Context7 and Memory MCP servers are loaded in all your Claude Code sessions.

## Current Status

**MCP Servers Configured in Claude Code:**
- ✅ **Context7** - Provides up-to-date documentation lookup
- ✅ **Memory** - Provides persistent context between sessions

**IMPORTANT**: These are only loaded if you start Claude Code with the correct command!

## How to Start Claude Code with MCP Servers

### Method 1: Use `claude mcp run` (Recommended)
```bash
# From any directory
claude mcp run

# This loads all configured MCP servers
```

### Method 2: Use the Project Alias
```bash
# From anywhere
claude-witch

# This alias:
# 1. Changes to WitchCityRope directory
# 2. Starts Claude Code with MCP servers
```

### Method 3: Use the Startup Script
```bash
# From project directory
./claude-start.sh
```

## What Each MCP Server Provides

### Context7
- Provides up-to-date documentation for various tools and frameworks
- Searches across multiple documentation sources
- Returns relevant, version-specific information

### Memory
- Persists information between Claude Code sessions
- Allows you to store and retrieve context
- Useful for remembering project-specific decisions and context

## Setup Instructions (Already Complete)

The setup has already been done, but for reference:

1. **MCP servers were added using:**
   ```bash
   claude mcp add-json context7 '{"command": "npx", "args": ["-y", "@upstash/context7-mcp@latest"]}'
   claude mcp add-json memory '{"command": "npx", "args": ["@modelcontextprotocol/server-memory"]}'
   ```

2. **Alias was added to ~/.bash_aliases:**
   ```bash
   alias claude-witch="cd /home/chad/repos/witchcityrope/WitchCityRope && claude mcp run"
   ```

## Verifying MCP Servers Are Loaded

In a Claude Code session, you can check if MCP servers are loaded by:

1. Looking for tools that start with `mcp_` or `mcp__`
2. Trying to use Context7 to search documentation
3. Checking if Memory persistence works between sessions

## Common Issues

### "Context7 not available"
- You started Claude Code with just `claude` instead of `claude mcp run`
- Solution: Exit and restart with `claude mcp run`

### "Memory not persisting"
- Same issue - MCP servers weren't loaded
- Solution: Always use `claude mcp run` or `claude-witch`

## For Future Sessions

**ALWAYS start Claude Code with one of these:**
- `claude mcp run` (from any directory)
- `claude-witch` (alias that includes cd to project)
- `./claude-start.sh` (from project directory)

**NEVER start with just:**
- `claude` (this doesn't load MCP servers)

---
*Last Updated: January 20, 2025*
*Purpose: Ensure Context7 and Memory MCP work in all Claude Code sessions*