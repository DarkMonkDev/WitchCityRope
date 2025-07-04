# Browser MCP Server Startup Checklist

## 🚀 Quick Start Guide for Browser MCP Servers

### 1. Pre-flight Checks

#### Chrome Installation in WSL
- [ ] Install Chrome in WSL (PRIMARY METHOD)
  ```bash
  # Add Google Chrome repository
  wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | sudo apt-key add -
  sudo sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list'
  
  # Install Chrome
  sudo apt update
  sudo apt install google-chrome-stable
  
  # Verify installation
  google-chrome --version
  ```

#### PowerShell Bridge Setup
- [ ] Install PowerShell in WSL
  ```bash
  # Install PowerShell
  sudo apt update
  sudo apt install -y powershell
  
  # Verify installation
  pwsh --version
  ```

### 2. Chrome Launch Command (WSL Native)

```bash
# PRIMARY METHOD: Launch Chrome directly in WSL
google-chrome \
  --remote-debugging-port=9222 \
  --user-data-dir="$HOME/.chrome-debug" \
  --no-first-run \
  --no-default-browser-check \
  --disable-gpu \
  --disable-software-rasterizer

# For headless operation (no GUI)
google-chrome \
  --headless \
  --remote-debugging-port=9222 \
  --user-data-dir="$HOME/.chrome-debug" \
  --no-first-run \
  --disable-gpu
```

### 3. PowerShell Bridge Commands

```bash
# Create PowerShell bridge script for Windows integration
cat > ~/chrome-bridge.ps1 << 'EOF'
# PowerShell script to manage Chrome from WSL
param(
    [string]$Action = "status"
)

switch ($Action) {
    "start" {
        Start-Process "chrome.exe" -ArgumentList "--remote-debugging-port=9222", "--user-data-dir=C:\temp\chrome-debug"
    }
    "stop" {
        Stop-Process -Name "chrome" -Force -ErrorAction SilentlyContinue
    }
    "status" {
        $chromeProcess = Get-Process chrome -ErrorAction SilentlyContinue
        if ($chromeProcess) {
            Write-Output "Chrome is running"
            netstat -an | findstr :9222
        } else {
            Write-Output "Chrome is not running"
        }
    }
}
EOF

# Make it executable
chmod +x ~/chrome-bridge.ps1

# Use the bridge
pwsh ~/chrome-bridge.ps1 -Action start
pwsh ~/chrome-bridge.ps1 -Action status
pwsh ~/chrome-bridge.ps1 -Action stop
```

### 4. Verification Steps

- [ ] Check Chrome is running with debugging port
  ```bash
  # In WSL - check local Chrome
  ps aux | grep chrome | grep 9222
  lsof -i :9222
  ```

- [ ] Test Chrome DevTools connection
  ```bash
  # Should return JSON with browser info
  curl http://localhost:9222/json/version
  ```

- [ ] List available tabs
  ```bash
  curl http://localhost:9222/json | jq '.[].title'
  ```

- [ ] Verify PowerShell bridge (if using Windows Chrome)
  ```bash
  # Check Windows Chrome status via PowerShell
  pwsh -c "Get-Process chrome -ErrorAction SilentlyContinue | Select-Object Id, ProcessName"
  ```

### 5. Environment Variables to Set

```bash
# Add to ~/.bashrc or ~/.zshrc
# For WSL Chrome
export CHROME_PATH="/usr/bin/google-chrome"
export CHROME_DEBUG_PORT=9222
export CHROME_USER_DATA_DIR="$HOME/.chrome-debug"

# For MCP server configuration
export BROWSER_CDP_URL="ws://localhost:9222"

# Optional: PowerShell bridge path
export CHROME_BRIDGE="$HOME/chrome-bridge.ps1"
```

### 6. Quick Troubleshooting Tips

#### Chrome Won't Start in WSL
- **Issue**: No display available
  - **Fix**: Use `--headless` flag for server environments
  - **Fix**: Install X server (VcXsrv or similar) for GUI mode
  - **Fix**: Set `export DISPLAY=:0` if using X server

#### Can't Connect to Port 9222
- **Issue**: Connection refused
  - **Fix**: Ensure Chrome is running with `--remote-debugging-port=9222`
  - **Fix**: Check if port is already in use: `lsof -i :9222`

#### PowerShell Bridge Issues
- **Issue**: PowerShell command not found
  - **Fix**: Install PowerShell: `sudo apt install powershell`
  - **Fix**: Use full path: `/usr/bin/pwsh`

### 7. Complete Startup Sequence (Simplified)

```bash
#!/bin/bash
# Save as start-browser-mcp.sh

# 1. Kill any existing Chrome processes
pkill -f "chrome.*9222" 2>/dev/null

# 2. Create user data directory
mkdir -p "$HOME/.chrome-debug"

# 3. Start Chrome in WSL
google-chrome \
  --headless \
  --remote-debugging-port=9222 \
  --user-data-dir="$HOME/.chrome-debug" \
  --no-first-run \
  --disable-gpu &

# 4. Wait for Chrome to initialize
echo "Waiting for Chrome to start..."
sleep 3

# 5. Verify connection
if curl -s http://localhost:9222/json/version > /dev/null; then
    echo "✅ Chrome DevTools is ready!"
    curl -s http://localhost:9222/json/version | jq .
else
    echo "❌ Failed to connect to Chrome DevTools"
    exit 1
fi

# 6. Start your MCP server
echo "Starting MCP server..."
npm start
```

### 8. Alternative Methods

#### A. SSH Tunnel (if needed for remote access)
```bash
# Create SSH tunnel to access Chrome DevTools remotely
ssh -L 9222:localhost:9222 -N user@remote-server
```

#### B. TCP Proxy (for network isolation)
```bash
# Install socat if needed
sudo apt install socat

# Create TCP proxy
socat TCP-LISTEN:9223,fork TCP:localhost:9222
```

#### C. Windows Chrome Launch (fallback)
```bash
# Only if WSL Chrome doesn't work
"/mnt/c/Program Files/Google/Chrome/Application/chrome.exe" \
  --remote-debugging-port=9222 \
  --user-data-dir="/mnt/c/temp/chrome-debug"
```

### 9. Health Check Script

```bash
#!/bin/bash
# Save as check-browser-mcp.sh

echo "🔍 Checking Browser MCP Setup..."

# Check Chrome installation
if command -v google-chrome &> /dev/null; then
    echo "✅ Chrome installed: $(google-chrome --version)"
else
    echo "❌ Chrome not installed in WSL"
fi

# Check PowerShell
if command -v pwsh &> /dev/null; then
    echo "✅ PowerShell installed: $(pwsh --version)"
else
    echo "⚠️  PowerShell not installed (optional)"
fi

# Check Chrome process
if pgrep -f "chrome.*9222" > /dev/null; then
    echo "✅ Chrome is running with debug port"
else
    echo "❌ Chrome not running"
fi

# Check DevTools port
if curl -s http://localhost:9222/json/version > /dev/null 2>&1; then
    echo "✅ Chrome DevTools responding on port 9222"
    echo "   Browser: $(curl -s http://localhost:9222/json/version | jq -r .Browser)"
else
    echo "❌ Chrome DevTools not accessible"
fi

# Check environment variables
for var in CHROME_PATH CHROME_DEBUG_PORT BROWSER_CDP_URL; do
    if [ -z "${!var}" ]; then
        echo "⚠️  $var not set"
    else
        echo "✅ $var = ${!var}"
    fi
done
```

### 10. Quick Commands Reference

```bash
# Start Chrome in WSL (headless)
google-chrome --headless --remote-debugging-port=9222 --user-data-dir="$HOME/.chrome-debug" &

# Start Chrome in WSL (with GUI - requires X server)
google-chrome --remote-debugging-port=9222 --user-data-dir="$HOME/.chrome-debug" &

# Check Chrome process
ps aux | grep chrome | grep 9222

# Test connection
curl -s http://localhost:9222/json | jq '.[0] | {id, title, url}'

# Kill Chrome
pkill -f "chrome.*9222"

# Use PowerShell bridge (if set up)
pwsh ~/chrome-bridge.ps1 -Action start
pwsh ~/chrome-bridge.ps1 -Action status
```

---

**Pro Tips:**
- WSL Chrome is more reliable than Windows Chrome for MCP servers
- Use `--headless` for server environments or when you don't need to see the browser
- PowerShell bridge provides Windows integration when needed
- Keep Chrome updated: `sudo apt update && sudo apt upgrade google-chrome-stable`
- Monitor Chrome memory usage for long-running sessions
- The simplified approach eliminates many WSL networking issues