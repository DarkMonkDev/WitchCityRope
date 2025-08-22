# Browser MCP Servers - Setup Guide for WSL Users

## Quick Start

Browser-based MCP servers (Stagehand and Browser Tools) require Chrome to be running on Windows with debugging enabled. This is a WSL2 architectural limitation.

### Step 1: Start Chrome on Windows

Open **Windows Command Prompt** (not WSL) and run:
```cmd
chrome.exe --remote-debugging-port=9222 --user-data-dir="%TEMP%\chrome-debug" --no-first-run
```

Or create a desktop shortcut with this target:
```
"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222
```

### Step 2: Verify Connection from WSL

In WSL, run:
```bash
./test-browser-connection.sh
```

If successful, you'll see Chrome version information.

### Step 3: Use Browser MCP Tools

Once Chrome is running with debugging:
- **Stagehand**: Will use the Chrome instance for AI-powered automation
- **Browser Tools**: Can connect for browser testing

## Why Manual Launch?

WSL2 runs in a virtual machine with its own network. It cannot:
- Access Windows localhost (127.0.0.1)
- Launch Windows GUI applications directly
- Share the same network namespace as Windows

## What This Means

✅ **You CAN use browser MCP servers** - Just need Chrome running first
❌ **You CANNOT auto-launch Chrome** - Must be started from Windows
⚠️ **Connection uses Windows IP** - Not localhost (typically 172.25.0.1)

## Daily Workflow

1. **Morning**: Start Chrome with debugging (keep it running)
2. **Development**: Use all MCP tools as needed
3. **Evening**: Close Chrome debug instance

## Alternatives

1. **Heavy Browser Testing**: Use Claude Desktop on Windows instead
2. **Automated Testing**: Consider Playwright/Puppeteer in Docker
3. **Simple HTTP Testing**: Use curl with Commands MCP

## Files in This Directory

- `test-browser-connection.sh` - Test if Chrome is accessible
- `setup-browser-mcp.sh` - Configure environment variables
- `launch-chrome-debug.cmd` - Windows batch file to start Chrome
- Various other scripts for different approaches

## The Bottom Line

Browser MCP servers work in WSL, but require Chrome to be manually started on Windows first. This is not a bug or misconfiguration - it's how WSL2 networking works.