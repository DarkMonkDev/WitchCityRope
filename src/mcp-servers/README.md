# MCP Servers - Clean Working Solutions

This folder contains the working MCP server solutions for browser automation in WSL.

## Core Scripts

### Browser Automation
- **`launch-chrome-wsl.sh`** - Launches Chrome from WSL with debugging enabled
- **`browser-bridge.ps1`** - PowerShell bridge for full browser control
- **`test-browser-mcp.sh`** - Comprehensive test suite for Browser Tools and Stagehand
- **`browser`** - Simple command-line wrapper for browser-bridge.ps1

### Setup & Verification
- **`verify-setup.sh`** - Verifies all dependencies are installed

## Documentation

### Key Guides
- **`BROWSER_AUTOMATION_WSL_GUIDE.md`** - Complete guide for browser automation in WSL
- **`BROWSER_MCP_UNIVERSAL_PROMPT.md`** - Universal prompt for other projects
- **`BROWSER_MCP_STARTUP_CHECKLIST.md`** - Step-by-step startup checklist
- **`BROWSER_MCP_KEY_FINDINGS.md`** - Technical explanation of WSL networking

### Quick References
- **`README_BROWSER_MCP.md`** - Quick start guide
- **`BROWSER_MCP_WORKING_SOLUTION.md`** - Summary of working solution

### Test Results
- **`MEMORY-SERVER-TEST-RESULTS.md`** - Memory MCP server test documentation
- **`POSTGRES-MCP-TEST-RESULTS.md`** - PostgreSQL MCP server test documentation
- **`GITHUB_MCP_FIX_GUIDE.md`** - GitHub MCP server configuration and troubleshooting

## Directories

- **`browser-server-persistent/`** - Complete persistent browser server solution (optional)
- **`logs/`** - Browser automation logs
- **`node_modules/`** - NPM dependencies

## Quick Start

1. Launch Chrome from WSL:
   ```bash
   ./launch-chrome-wsl.sh
   ```

2. Use browser automation:
   ```bash
   # Check status
   ./browser status
   
   # Navigate
   ./browser navigate -Url "https://example.com"
   
   # List tabs
   ./browser tabs
   ```

3. Test MCP functionality:
   ```bash
   ./test-browser-mcp.sh
   ```

## GitHub MCP Quick Check

Test GitHub MCP configuration:
```bash
# Quick status check
./check-github-mcp.sh

# Full test suite
node test-github-mcp-full.js

# API access test only
node test-github-access.js
```

## Key Discovery

Chrome CAN be launched directly from WSL! The PowerShell bridge method provides full access to Browser Tools and Stagehand MCPs without complex SSH tunnels or port forwarding.