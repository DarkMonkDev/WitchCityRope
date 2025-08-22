# Browser Tools MCP Persistent Server Setup

This document provides instructions for setting up and managing a persistent browser server for Browser Tools MCP.

## Overview

The persistent browser server allows Browser Tools MCP to maintain a continuous browser instance, improving performance and reliability for browser automation tasks.

## Components

1. **browser-server.js** - The main server application that manages the browser instance
2. **browser-server-manager.sh** - Management script for controlling the server
3. **browser-server.service** - Systemd service file for automatic startup
4. **test-browser-connection.js** - Test script to verify connectivity

## Prerequisites

- Node.js installed
- Browser Tools MCP installed (`npm install browser-tools-mcp`)
- Chrome or Chromium browser installed
- WSL or Linux environment

## Installation

### 1. Make Scripts Executable

```bash
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-server-persistent
chmod +x browser-server-manager.sh
chmod +x browser-server.js
chmod +x test-browser-connection.js
```

### 2. Configure Chrome Path

Edit the Chrome path in the scripts if your installation differs:

```bash
# Default path in scripts:
/mnt/c/Program Files/Google/Chrome/Application/chrome.exe

# Alternative paths:
/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe
/mnt/c/Program Files/Microsoft/Edge/Application/msedge.exe
```

## Usage

### Starting the Server

#### Method 1: Using the Manager Script (Recommended)

```bash
./browser-server-manager.sh start
```

#### Method 2: Direct Execution

```bash
node browser-server.js
```

#### Method 3: As a Background Process

```bash
nohup node browser-server.js > browser-server.log 2>&1 &
```

### Managing the Server

```bash
# Check server status
./browser-server-manager.sh status

# Stop the server
./browser-server-manager.sh stop

# Restart the server
./browser-server-manager.sh restart

# View logs (last 50 lines)
./browser-server-manager.sh logs 50

# Clean up log files
./browser-server-manager.sh clean-logs
```

### Systemd Service (If Available)

If your WSL instance supports systemd:

```bash
# Install the service
./browser-server-manager.sh install-systemd

# Start using systemd
sudo systemctl start browser-server

# Enable auto-start on boot
sudo systemctl enable browser-server

# Check service status
sudo systemctl status browser-server

# View service logs
journalctl -u browser-server -f
```

## Testing the Connection

Run the test script to verify Browser Tools MCP can connect:

```bash
node test-browser-connection.js
```

Expected output:
```
Browser Tools MCP Connection Test
=================================

Testing Browser Tools MCP Connection...
Server: 127.0.0.1:9222
Connecting to browser tools...
✓ Connected successfully!

Available Browser Tools:
  - browser_navigate: Navigate browser to a URL
  - browser_screenshot: Take a screenshot
  ...

✓ All tests completed!
```

## Configuration

### Environment Variables

- `BROWSER_SERVER_PORT` - Server port (default: 9222)
- `BROWSER_SERVER_HOST` - Server host (default: 127.0.0.1)
- `CHROME_PATH` - Path to Chrome/Chromium executable
- `NODE_ENV` - Node environment (default: production)

### Server Configuration

Edit `browser-server.js` to modify:
- Retry attempts
- Retry delay
- Log file locations
- Port and host settings

## Troubleshooting

### Server Won't Start

1. Check if port 9222 is already in use:
   ```bash
   netstat -tlnp | grep 9222
   ```

2. Verify Chrome path:
   ```bash
   ls -la "/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"
   ```

3. Check logs:
   ```bash
   tail -f browser-server.log
   tail -f browser-server.error.log
   ```

### Connection Failed

1. Ensure server is running:
   ```bash
   ./browser-server-manager.sh status
   ```

2. Check firewall settings
3. Verify Chrome can launch with the specified arguments

### Chrome Launch Issues

Common Chrome arguments that may help:
```javascript
--no-sandbox
--disable-setuid-sandbox
--disable-dev-shm-usage
--disable-gpu
--disable-software-rasterizer
```

## Auto-Start on WSL Boot

### Option 1: Using Task Scheduler (Windows)

1. Open Task Scheduler on Windows
2. Create a new task
3. Set trigger to "At startup"
4. Set action to run:
   ```
   wsl.exe -d <distro-name> -e /path/to/browser-server-manager.sh start
   ```

### Option 2: WSL Boot Script

Add to your `.bashrc` or `.profile`:
```bash
# Start browser server if not running
if ! pgrep -f "browser-server.js" > /dev/null; then
    /path/to/browser-server-manager.sh start
fi
```

## Security Considerations

1. The server binds to localhost only by default
2. No authentication is implemented - ensure firewall protection
3. Log files may contain sensitive information
4. Consider using proper process isolation in production

## Monitoring

### Health Check Script

Create a simple health check:
```bash
#!/bin/bash
if curl -s http://localhost:9222/json/version > /dev/null; then
    echo "Browser server is healthy"
else
    echo "Browser server is not responding"
    # Restart server
    ./browser-server-manager.sh restart
fi
```

### Log Rotation

Add to crontab for automatic log rotation:
```bash
0 0 * * * /path/to/browser-server-manager.sh clean-logs
```

## Integration with Browser Tools MCP

When using Browser Tools MCP with the persistent server, ensure your MCP configuration includes:

```json
{
  "mcpServers": {
    "browser-tools": {
      "command": "node",
      "args": ["/path/to/node_modules/.bin/browser-tools-mcp"],
      "env": {
        "BROWSER_SERVER_PORT": "9222",
        "BROWSER_SERVER_HOST": "127.0.0.1"
      }
    }
  }
}
```

## Support

For issues:
1. Check the error logs
2. Run the test script
3. Verify all prerequisites are met
4. Check Browser Tools MCP documentation