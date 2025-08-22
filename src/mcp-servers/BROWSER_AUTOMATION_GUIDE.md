# Browser Automation Complete Guide

## Overview

This guide provides a comprehensive solution for browser automation in WSL, combining Chrome DevTools Protocol, Browser Tools MCP, and Stagehand for powerful web automation capabilities within Claude Desktop.

## Quick Start

### 1. Start All Services

From WSL terminal:
```bash
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers
./start-browser-automation.sh
```

This single command will:
- Launch Chrome with remote debugging enabled
- Start the Browser Tools MCP server
- Set up all necessary connections
- Display status and logs

### 2. Stop All Services

```bash
./stop-browser-automation.sh
```

## Architecture

### Components

1. **Chrome with Remote Debugging**
   - Runs on port 9222
   - Enables programmatic control via DevTools Protocol
   - Uses temporary profile for clean state

2. **Browser Tools MCP Server**
   - Provides browser automation tools to Claude
   - Runs in WSL, controls Windows Chrome
   - Handles navigation, clicking, typing, etc.

3. **Stagehand (Optional)**
   - Advanced AI-powered browser automation
   - Requires OpenAI API key
   - Provides intelligent element selection

## Daily Usage Workflows

### Basic Web Automation

1. Start services:
   ```bash
   ./start-browser-automation.sh
   ```

2. In Claude, you can now:
   - Navigate to websites
   - Click buttons and links
   - Fill out forms
   - Take screenshots
   - Extract page content

### Testing Web Applications

1. Start browser automation
2. Use Claude to:
   - Navigate to your application
   - Perform user interactions
   - Verify page elements
   - Test workflows end-to-end

### Web Scraping

1. Start services
2. Request Claude to:
   - Navigate to target sites
   - Extract specific data
   - Handle pagination
   - Export results

## Configuration

### Browser Selection

The script automatically detects Chrome or Edge. To force a specific browser, edit `start-browser-automation.sh`:

```bash
# Force Chrome
BROWSER_PATH="/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"

# Force Edge
BROWSER_PATH="/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"
```

### Claude Desktop Configuration

1. **Option 1: Use the provided configuration**
   ```bash
   # Copy the provided config
   cp claude-config-browser-automation.json /mnt/c/Users/chad/AppData/Roaming/Claude/claude_desktop_config.json
   ```

2. **Option 2: Manually update your config**
   
   Add to `claude_desktop_config.json`:
   ```json
   {
     "mcpServers": {
       "browser-tools-wsl": {
         "command": "wsl",
         "args": [
           "-e",
           "bash",
           "-c",
           "cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers && export CHROME_PATH='/mnt/c/Program Files/Google/Chrome/Application/chrome.exe' && node ./node_modules/.bin/browser-tools-mcp"
         ]
       }
     }
   }
   ```

### Stagehand Configuration

If using Stagehand, ensure your OpenAI API key is set in the configuration:

```json
"stagehand": {
  "command": "node",
  "args": ["C:\\Users\\chad\\source\\repos\\WitchCityRope\\mcp-server-browserbase\\stagehand\\dist\\index.js"],
  "env": {
    "OPENAI_API_KEY": "your-api-key-here",
    "LOCAL_CDP_URL": "http://localhost:9222"
  }
}
```

## Testing

### Run Complete Test Suite

```bash
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers
node test-browser-automation.js
```

This will verify:
- Chrome DevTools Protocol connection
- Browser Tools MCP functionality
- Stagehand requirements
- Display recommended configuration

### Manual Testing

1. **Check Chrome connection:**
   ```bash
   curl http://localhost:9222/json/version
   ```

2. **View available tabs:**
   ```bash
   curl http://localhost:9222/json
   ```

3. **Test Browser Tools:**
   ```bash
   node test-browser-connection.js
   ```

## Troubleshooting

### Common Issues

1. **Browser not starting**
   - Check if Chrome/Edge is installed on Windows
   - Verify the path in the script
   - Check Windows Firewall settings

2. **Connection refused on port 9222**
   - Browser may not be running
   - Port might be blocked by firewall
   - Try killing all Chrome processes and restarting

3. **MCP tools not available in Claude**
   - Restart Claude Desktop completely
   - Verify configuration file syntax
   - Check Claude logs: `C:\Users\chad\AppData\Roaming\Claude\logs\`

4. **Permission denied errors**
   ```bash
   chmod +x start-browser-automation.sh
   chmod +x stop-browser-automation.sh
   ```

### Viewing Logs

All logs are stored in `./logs/` directory:

```bash
# View main log
tail -f logs/browser-automation-*.log

# View Chrome debug log
tail -f logs/chrome-debug.log

# View MCP server log
tail -f logs/browser-tools-mcp.log
```

### Debug Mode

For more verbose output, edit `start-browser-automation.sh`:

```bash
export DEBUG=browser-tools:*
export NODE_ENV=development
```

## Advanced Usage

### Custom Chrome Flags

Add flags to `start-browser-automation.sh`:

```bash
"$BROWSER_PATH" \
    --user-data-dir="$TEMP_PROFILE_DIR" \
    --remote-debugging-port=$DEBUG_PORT \
    --window-size=1920,1080 \
    --disable-web-security \
    --allow-insecure-localhost \
    "$@"
```

### Multiple Instances

To run multiple browser instances:

```bash
# Instance 1
DEBUG_PORT=9222 ./start-browser-automation.sh

# Instance 2 (in another terminal)
DEBUG_PORT=9223 ./start-browser-automation.sh
```

### Integration with CI/CD

Example GitHub Actions workflow:

```yaml
- name: Start Browser Automation
  run: |
    cd src/mcp-servers
    ./start-browser-automation.sh &
    sleep 10
    node test-browser-automation.js
```

## Security Considerations

1. **Temporary Profiles**: Each session uses a clean profile
2. **Local Only**: Chrome debugging is bound to localhost
3. **API Keys**: Store securely, never commit to version control
4. **Sandbox**: Disabled for WSL compatibility (use only in development)

## Best Practices

1. **Always stop services when done** to free resources
2. **Monitor logs** for errors and performance issues
3. **Use specific selectors** for reliable automation
4. **Test incrementally** when building complex automations
5. **Keep Chrome updated** for latest features and security

## Maintenance

### Updating Browser Tools MCP

```bash
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers
npm update browser-tools-mcp
```

### Cleaning Up

```bash
# Remove logs older than 7 days
find ./logs -name "*.log" -mtime +7 -delete

# Clear temporary profiles
rm -rf /tmp/chrome-automation-profile-*
```

## FAQ

**Q: Can I use this with Edge instead of Chrome?**
A: Yes, the script automatically detects and uses Edge if Chrome is not available.

**Q: How do I use this in headless mode?**
A: Add `--headless` flag in the Chrome launch command in `start-browser-automation.sh`.

**Q: Can multiple users use this simultaneously?**
A: Yes, each user session gets its own temporary profile and can use different ports.

**Q: Is this compatible with Docker?**
A: The browser runs on Windows host, but you can control it from Docker containers in WSL.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review logs in the `./logs` directory
3. Test individual components with the test scripts
4. Ensure all prerequisites are installed

---

Created by Claude Code - 2025-06-29