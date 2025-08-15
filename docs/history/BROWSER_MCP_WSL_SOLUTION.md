# Browser MCP Servers (Stagehand & Browser Tools) WSL Solution

## Overview

Getting browser-based MCP servers working in WSL2 requires understanding the networking architecture. WSL2 runs in a lightweight VM with its own network stack, which prevents direct localhost connections between WSL and Windows processes.

## Current Status

### Working MCP Servers (6/8)
- ✅ FileSystem 
- ✅ Memory
- ✅ Docker
- ✅ Commands
- ✅ GitHub
- ⚠️ PostgreSQL (needs password)

### Browser MCP Servers (2/8)
- ❌ Browser Tools - Requires browser server connection
- ❌ Stagehand - Requires Chrome DevTools Protocol connection

## The Challenge

1. **WSL2 Network Isolation**: WSL2 cannot access Windows localhost (127.0.0.1)
2. **Dynamic IPs**: WSL2 gets a new IP on each restart
3. **MCP Protocol**: Expects stdio or TCP connections

## Working Solution

### Step 1: Launch Chrome on Windows

Due to WSL2 limitations, Chrome must be launched from Windows. Use one of these methods:

#### Option A: Command Prompt (Recommended)
```cmd
CD C:\Program Files\Google\Chrome\Application\
C:\Program Files\Google\Chrome\Application\chrome.exe --remote-debugging-port=9222 --user-data-dir="%TEMP%\chrome-debug" --no-first-run
```

#### Option B: PowerShell
```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="$env:TEMP\chrome-debug" --no-first-run
```

#### Option C: Create a Desktop Shortcut
1. Right-click desktop → New → Shortcut
2. Location: `"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222`
3. Name: "Chrome Debug Mode"

### Step 2: Connect from WSL

Once Chrome is running with debugging, connect using the Windows host IP:

```bash
# Get Windows host IP
WINDOWS_HOST=$(cat /etc/resolv.conf | grep nameserver | awk '{print $2}')

# Test connection
curl http://$WINDOWS_HOST:9222/json/version
```

### Step 3: Configure MCP Servers

#### For Stagehand

Update your environment or scripts to use the Windows host IP:

```bash
# In your Stagehand wrapper script
export LOCAL_CDP_URL="http://$WINDOWS_HOST:9222"
```

#### For Browser Tools

The Browser Tools MCP expects to connect to a browser server. You have two options:

1. **Run Browser Server on Windows** (then connect from WSL)
2. **Use port forwarding** to make Windows Chrome accessible as localhost in WSL

## Automated Solution

We've created helper scripts in `/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/`:

### `test-browser-connection.sh`
Tests if Chrome is accessible from WSL:

```bash
#!/bin/bash
WINDOWS_HOST=$(cat /etc/resolv.conf | grep nameserver | awk '{print $2}')
echo "Testing connection to Chrome on Windows host: $WINDOWS_HOST"
curl -s http://$WINDOWS_HOST:9222/json/version || echo "Chrome not accessible"
```

### `setup-browser-mcp.sh`
Sets up environment for browser MCP servers:

```bash
#!/bin/bash
# Get Windows host IP
export WINDOWS_HOST=$(cat /etc/resolv.conf | grep nameserver | awk '{print $2}')
export CHROME_DEBUG_URL="http://$WINDOWS_HOST:9222"

# For Stagehand
export LOCAL_CDP_URL="$CHROME_DEBUG_URL"

echo "Browser MCP environment configured:"
echo "  Windows Host: $WINDOWS_HOST"
echo "  Chrome Debug URL: $CHROME_DEBUG_URL"
```

## Best Practices

1. **Daily Workflow**:
   - Start Chrome with debugging from Windows
   - Source the setup script in WSL
   - Use browser MCP tools as needed

2. **Persistent Setup**:
   - Add Chrome debug shortcut to Windows startup
   - Add setup script to `.bashrc` in WSL

3. **Security**:
   - Only expose debugging port when needed
   - Use temporary profiles for testing
   - Close debug Chrome when not in use

## Alternative: Claude Desktop

For seamless browser MCP integration, consider using Claude Desktop on Windows instead of Claude Code in WSL. Claude Desktop can directly access Windows browsers without networking complications.

## Troubleshooting

### Chrome won't start with debugging
- Ensure no other Chrome instances are running
- Check if port 9222 is already in use
- Try a different port (e.g., 9223)

### Can't connect from WSL
- Verify Windows host IP: `cat /etc/resolv.conf`
- Check Windows Firewall settings
- Try disabling Windows Defender briefly to test

### MCP server errors
- Ensure environment variables are set
- Check MCP server logs in Claude logs directory
- Verify Chrome DevTools Protocol is responding

## Technical Details

### Why This Happens
- WSL2 uses Hyper-V virtualization
- Network namespace isolation
- NAT between WSL2 and Windows host
- Dynamic IP assignment on WSL2 restart

### Port Forwarding Option (Advanced)
As administrator in PowerShell:
```powershell
netsh interface portproxy add v4tov4 listenport=9222 listenaddress=0.0.0.0 connectport=9222 connectaddress=127.0.0.1
```

This makes Windows localhost:9222 accessible from WSL2 as localhost:9222.

## Conclusion

While browser MCP servers require extra steps in WSL2, they are functional with the proper setup. The key is understanding that Chrome must run on Windows and WSL must connect using the Windows host IP rather than localhost.

For development requiring heavy browser automation, consider:
1. Using Claude Desktop on Windows
2. Setting up persistent port forwarding
3. Running a dedicated browser server on Windows

The current setup provides a workable solution for occasional browser automation needs while maintaining the benefits of WSL2 for development.