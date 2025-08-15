# MCP Server Setup for Ubuntu Native Environment

## Overview

This document describes the MCP (Model Context Protocol) server setup for the WitchCityRope project running on Ubuntu 24.04 (native Linux, not WSL). All browser automation tools work directly without SSH tunnels or Windows-specific workarounds.

## Installed MCP Servers

### 1. Browser-tools MCP Server
**Purpose**: Low-level browser automation with Puppeteer  
**Location**: `/home/chad/mcp-servers/browser-tools-server/`  
**Source**: Official puppeteer server from modelcontextprotocol/servers-archived

#### Configuration Files
- `mcp-config.json` - Standard config with no-sandbox (for compatibility)
- `mcp-config-safe.json` - Secure config without dangerous flags (recommended)

#### Starting the Server
```bash
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh

# Options:
# 1. Headless mode with safe config
# 2. Headed mode with safe config (RECOMMENDED for debugging)
# 3. Headless mode with no-sandbox
# 4. Headed mode with no-sandbox
```

#### Direct Testing
```bash
# Test Puppeteer directly
cd /home/chad/mcp-servers/browser-tools-server
node test-puppeteer-direct.js

# Test browser server integration
node test-browser-server.js
```

### 2. Stagehand MCP Server
**Purpose**: AI-powered browser automation with natural language commands  
**Location**: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`  
**Source**: https://github.com/browserbase/mcp-server-browserbase

#### Configuration
- Configured for local Chrome (not Browserbase cloud)
- Uses OpenAI API for natural language processing
- Debug port: 9222

#### Starting the Server
```bash
# Set your OpenAI API key
export OPENAI_API_KEY='your-api-key-here'

# Run the quickstart script
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Or manually:
# 1. Start Chrome debug
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/scripts/start-chrome-debug.sh

# 2. Start Stagehand server
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/scripts/start-stagehand-server.sh
```

#### Available Tools
- `stagehand_navigate` - Navigate to URLs
- `stagehand_act` - Interact with page elements using natural language
- `stagehand_extract` - Extract data from pages
- `stagehand_observe` - Observe page elements
- `screenshot` - Capture screenshots

### 3. Other MCP Servers (Standard Claude Desktop)

| Server | Purpose | Status |
|--------|---------|--------|
| FileSystem | File operations | ✅ Available |
| GitHub | GitHub API integration | ✅ Configured with PAT |
| PostgreSQL | Database operations | ✅ Ready (PostgreSQL 16 installed) |
| Docker | Container management | ✅ Available (Docker installed) |
| Memory | Persistent knowledge | ✅ Available |

## Chrome Browser Configuration

### Installation
- Chrome Version: 138.0.7204.92
- Location: `/usr/bin/google-chrome`
- No special configuration needed for Ubuntu

### Launching Chrome for Debugging
```bash
# Standard launch
google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check

# With specific profile (clean state)
google-chrome --remote-debugging-port=9222 --user-data-dir=/tmp/chrome-debug --no-first-run

# Headless mode
google-chrome --headless --remote-debugging-port=9222
```

## Testing Browser Automation

### Quick Test Script
A comprehensive test script is available at:
```bash
cd /home/chad/repos/witchcityrope
node test-browser-automation.js
```

This script tests:
- Browser launch
- Page navigation
- Screenshot capture
- Form interaction
- Local server connectivity

### Manual Testing
```bash
# Test Chrome DevTools connection
curl http://localhost:9222/json/version

# Check if Chrome is running with debug port
ps aux | grep chrome | grep 9222

# Test Puppeteer directly
cd /home/chad/mcp-servers/browser-tools-server
node test-puppeteer-direct.js
```

## Common Use Cases

### 1. UI Testing with Natural Language (Stagehand)
```javascript
// Navigate and interact
stagehand_navigate({ url: "http://localhost:5000" })
stagehand_act({ action: "Click the login button" })
stagehand_act({ action: "Type 'admin@witchcityrope.com' in the email field" })
stagehand_act({ action: "Type 'Test123!' in the password field" })
stagehand_act({ action: "Submit the form" })

// Extract information
stagehand_extract({ instruction: "Get all event titles from the page" })

// Take screenshot
screenshot({ path: "login-test.png" })
```

### 2. Precise Automation (Browser-tools)
```javascript
// Direct control with selectors
browser_navigate({ url: "http://localhost:5000" })
browser_click({ selector: "#login-button" })
browser_type({ selector: "input[name='email']", text: "admin@witchcityrope.com" })
browser_screenshot({ path: "login-page.png", fullPage: true })

// Performance testing
browser_evaluate({ script: "return window.performance.timing" })
```

### 3. Testing Different Viewports
```javascript
// Mobile testing
browser_setViewport({ width: 375, height: 667 })
browser_navigate({ url: "http://localhost:5000" })
browser_screenshot({ path: "mobile-view.png" })

// Tablet testing
browser_setViewport({ width: 768, height: 1024 })
browser_screenshot({ path: "tablet-view.png" })
```

## Troubleshooting

### Cannot Connect to Chrome
```bash
# Check if Chrome is running
ps aux | grep chrome

# Check if port 9222 is in use
lsof -i :9222

# Kill existing Chrome processes
pkill -f chrome

# Restart Chrome with debug port
google-chrome --remote-debugging-port=9222 --no-first-run
```

### Permission Errors
```bash
# Use safe mode configuration first
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh
# Choose option 2 (Headed + Safe)

# Only use no-sandbox if absolutely necessary
```

### Stagehand Connection Issues
```bash
# Verify Chrome is running with debug port
curl http://localhost:9222/json/version

# Check Stagehand logs
cd /home/chad/mcp-servers/mcp-server-browserbase/stagehand
npm run test:connection

# Ensure OPENAI_API_KEY is set
echo $OPENAI_API_KEY
```

## Best Practices

1. **Start with Safe Mode**: Always try the safe configuration first
2. **Use Headed Mode for Debugging**: Visual feedback helps identify issues
3. **Clean Browser State**: Each test should start with a fresh browser
4. **Check Logs**: Both MCP servers provide detailed logging
5. **Test Connection First**: Verify Chrome DevTools before running automation

## Differences from WSL Setup

| Feature | WSL Setup | Ubuntu Native |
|---------|-----------|---------------|
| Chrome Launch | PowerShell bridge required | Direct execution |
| Network | SSH tunnels needed | Direct localhost |
| File Paths | Windows paths (/mnt/c/) | Native Linux paths |
| Permissions | Complex permissions | Standard Linux |
| Performance | Overhead from WSL | Native performance |
| Debugging | Limited by WSL | Full debugging support |

## Quick Reference

```bash
# Start WitchCityRope app
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web

# Start browser-tools MCP
/home/chad/mcp-servers/browser-tools-server/start-server.sh

# Start Stagehand MCP
export OPENAI_API_KEY='your-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Test browser automation
cd /home/chad/repos/witchcityrope
node test-browser-automation.js

# Check all services
ps aux | grep -E "chrome|dotnet|node"
```

## Additional Resources

- Browser-tools docs: `/home/chad/mcp-servers/browser-tools-server/README-UBUNTU.md`
- Stagehand docs: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/INSTALLATION-UBUNTU.md`
- Test results: `/home/chad/repos/witchcityrope/test-*.png`