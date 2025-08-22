# Browser MCP Universal Prompt for WSL2 Environments

## 🎯 Key Discovery: Chrome CAN Be Launched From WSL!

**IMPORTANT: Chrome can be launched directly from WSL2 and will automatically bind to the Windows localhost!** This eliminates most network bridging complexities that plague browser automation in WSL environments.

### Proven Working Example:
```bash
# From WSL2 - Chrome launches on Windows and binds to Windows localhost:9222
/mnt/c/Program\ Files/Google/Chrome/Application/chrome.exe \
  --remote-debugging-port=9222 \
  --user-data-dir=/mnt/c/Users/$(cmd.exe /c echo %USERNAME% 2>/dev/null | tr -d '\r')/AppData/Local/Temp/chrome-debug \
  --no-first-run \
  --no-default-browser-check
```

**Result**: Chrome runs on Windows but is accessible at `localhost:9222` from WSL - no complex networking required!

## Universal Application - Works for ANY Browser Automation Tool

This guide applies to **ANY browser automation tool**, including:
- MCP (Model Context Protocol) servers
- Puppeteer
- Playwright  
- Selenium
- Stagehand
- Chrome DevTools Protocol (CDP) clients
- WebDriver
- Any tool that needs to control a browser

**Once Chrome is properly launched with remote debugging enabled, any automation tool can connect to it.**

## Context: Why This Matters

WSL2 runs in an isolated network environment with its own virtual network adapter. This traditionally prevented direct communication between:
- WSL2 processes and Windows host processes
- WSL2 processes and browsers running on Windows
- Tools expecting localhost connections across the WSL2/Windows boundary

**BUT**: When Chrome is launched from WSL, it bridges this gap automatically!

## Recommended Solutions (In Order of Reliability)

### 1. PowerShell Bridge (MOST RELIABLE - Recommended for Production)
**This is the most reliable method for connecting browser automation tools in WSL2.** PowerShell ensures Chrome launches with proper Windows context and environment variables:

```bash
# From WSL2 - Launch Chrome via PowerShell (RECOMMENDED)
powershell.exe -Command "Start-Process 'chrome.exe' -ArgumentList '--remote-debugging-port=9222', '--user-data-dir=C:\Users\$env:USERNAME\AppData\Local\Temp\chrome-debug', '--no-first-run', '--no-default-browser-check'"

# Or with explicit path:
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --user-data-dir=C:\Users\$env:USERNAME\AppData\Local\Temp\chrome-debug --no-first-run --no-default-browser-check"
```

**Why PowerShell Bridge is Most Reliable:**
- Handles Windows environment variables correctly
- Ensures proper process context
- Works consistently across different WSL distributions
- Avoids path and permission issues

### 2. Direct WSL Launch (Simple but Less Reliable)
Launch Chrome directly from WSL2 - it automatically binds to Windows localhost:
```bash
# Chrome launches on Windows, accessible at localhost:9222
"/mnt/c/Program Files/Google/Chrome/Application/chrome.exe" --remote-debugging-port=9222
```
**Note**: While simpler, this method may have issues with environment variables and user profiles.

### 3. SSH Tunnel
Create an SSH tunnel from WSL2 to Windows host:
```bash
# From WSL2, tunnel local port 9222 to Windows Chrome DevTools port
ssh -L 9222:localhost:9222 <windows-username>@<windows-host-ip>
```

### 4. Windows Port Forwarding
Configure Windows to forward ports to WSL2:
```powershell
# Run in elevated PowerShell on Windows
netsh interface portproxy add v4tov4 listenport=9222 listenaddress=0.0.0.0 connectport=9222 connectaddress=<wsl2-ip>
```

## Reference Scripts Created
- `setup-chrome-bridge.sh` - Automated setup for Chrome connectivity
- `test-chrome-connection.sh` - Diagnostic tool for connection issues
- Port forwarding helper scripts for persistent configuration

## Quick Test Commands

### 1. Test Chrome DevTools Availability
```bash
# From WSL2 - check if Chrome DevTools is accessible
curl http://<windows-host-ip>:9222/json/version
```

### 2. Get Windows Host IP
```bash
# From WSL2
ip route | grep default | awk '{print $3}'
# or
cat /etc/resolv.conf | grep nameserver | awk '{print $2}'
```

### 3. Verify WebSocket Connection
```bash
# Test WebSocket connectivity (critical for browser automation)
wscat -c ws://<windows-host-ip>:9222/devtools/browser/<id>
```

## Universal Debugging Steps
1. **Identify the connection method** your tool uses (HTTP, WebSocket, CDP)
2. **Check if Chrome is running** with remote debugging enabled
3. **Test basic connectivity** using curl to Chrome DevTools endpoints
4. **Apply appropriate solution** based on your setup preferences
5. **Update tool configuration** to use the bridged connection

## Key Principle
The core issue is network isolation between WSL2 and Windows. However, we've discovered that **Chrome launched from WSL2 binds to the Windows localhost**, making connectivity much simpler. Once Chrome is running with remote debugging enabled (via any of the four methods above), all browser automation tools will work correctly.

## Proven Working Example: MCP Browser Tools

Here's a real-world example that demonstrates the PowerShell bridge method working with MCP Browser Tools:

```bash
# Step 1: Launch Chrome from WSL using PowerShell bridge
powershell.exe -Command "Start-Process 'chrome.exe' -ArgumentList '--remote-debugging-port=9222', '--user-data-dir=C:\Users\$env:USERNAME\AppData\Local\Temp\chrome-debug'"

# Step 2: Start your browser automation tool (example with MCP)
npx @modelcontextprotocol/server-browser-tools

# Step 3: Tool connects to localhost:9222 automatically!
```

**Key Insight**: When Chrome is launched from WSL (via PowerShell or directly), it binds to the Windows localhost. This means your automation tools can use standard localhost connections without any special configuration!

## Common Configuration Updates

### When Chrome is launched from WSL (Methods 1 & 2):
**No configuration changes needed!** Chrome binds to Windows localhost, so standard connection strings work:
- `localhost:9222` ✓
- `127.0.0.1:9222` ✓
- `ws://localhost:9222` ✓

### When using network bridging methods (Methods 3 & 4):
Update connection strings to use the Windows host IP:
- `localhost:9222` → `<windows-host-ip>:9222`
- `127.0.0.1:9222` → `<windows-host-ip>:9222`
- `ws://localhost:9222` → `ws://<windows-host-ip>:9222`

## Summary: The Game-Changing Discovery

1. **Chrome CAN be launched from WSL** - This works and is reliable!
2. **PowerShell bridge is the MOST reliable method** - Use this for production
3. **Chrome binds to Windows localhost when launched from WSL** - No complex networking needed
4. **This works for ANY browser automation tool** - Not just MCP servers
5. **Standard localhost connections work** - No need to find Windows host IPs

Remember: The tools themselves work perfectly. With the discovery that Chrome can be launched directly from WSL, the network gap is already bridged for you!