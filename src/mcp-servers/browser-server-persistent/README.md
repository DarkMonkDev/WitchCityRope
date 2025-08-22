# Browser Tools MCP Persistent Server

A persistent browser server implementation for Browser Tools MCP that provides continuous browser instance management with automatic restart capabilities.

## Features

- ✅ Persistent browser instance management
- ✅ Automatic restart on failure
- ✅ Comprehensive logging system
- ✅ Process management with PID tracking
- ✅ Systemd service support
- ✅ Multiple startup methods
- ✅ Health monitoring and testing
- ✅ Cross-platform support (WSL/Linux)

## Quick Start

1. **Start the server:**
   ```bash
   ./browser-server-manager.sh start
   ```

2. **Check status:**
   ```bash
   ./browser-server-manager.sh status
   ```

3. **Test connection:**
   ```bash
   node test-browser-connection.js
   ```

## Files Overview

| File | Description |
|------|-------------|
| `browser-server.js` | Main server application |
| `browser-server-manager.sh` | Management script for server control |
| `browser-server.service` | Systemd service configuration |
| `test-browser-connection.js` | Connection testing script |
| `auto-start.sh` | Auto-start script for WSL |
| `start-browser-server.bat` | Windows batch file for starting |
| `SETUP_DOCUMENTATION.md` | Detailed setup instructions |
| `QUICK_START.md` | Quick reference guide |

## Management Commands

```bash
# Start/stop/restart server
./browser-server-manager.sh start
./browser-server-manager.sh stop
./browser-server-manager.sh restart

# Check status
./browser-server-manager.sh status

# View logs
./browser-server-manager.sh logs [number_of_lines]

# Clean logs
./browser-server-manager.sh clean-logs

# Systemd integration
./browser-server-manager.sh install-systemd
./browser-server-manager.sh uninstall-systemd
```

## Configuration

The server uses these environment variables:
- `BROWSER_SERVER_PORT` (default: 9222)
- `BROWSER_SERVER_HOST` (default: 127.0.0.1)
- `CHROME_PATH` (default: /mnt/c/Program Files/Google/Chrome/Application/chrome.exe)
- `NODE_ENV` (default: production)

## Auto-Start Methods

1. **WSL .bashrc**: Add to `~/.bashrc`:
   ```bash
   source /path/to/auto-start.sh
   ```

2. **Windows Task Scheduler**: Use `start-browser-server.bat`

3. **Systemd** (if available):
   ```bash
   sudo systemctl enable browser-server
   ```

## Logs

- Main log: `browser-server.log`
- Error log: `browser-server.error.log`
- PID file: `browser-server.pid`

## Requirements

- Node.js
- Browser Tools MCP (`npm install browser-tools-mcp`)
- Chrome or Chromium browser
- WSL or Linux environment

## Troubleshooting

See `SETUP_DOCUMENTATION.md` for detailed troubleshooting steps.

## License

This implementation is provided as-is for use with Browser Tools MCP.