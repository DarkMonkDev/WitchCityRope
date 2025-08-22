# Browser Testing Incognito Mode Setup

## Overview
All browser testing for the WitchCityRope project is now configured to run in Chrome's incognito mode by default. This ensures clean testing sessions without cached data, cookies, or stored credentials affecting test results.

## Updated Configurations

### 1. CLAUDE.md - Primary Documentation
Updated all Chrome launch commands to include the `--incognito` flag:

```bash
# Primary method - Launch Chrome from WSL with PowerShell bridge
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
```

### 2. Browser Tools Configuration
Updated `/src/mcp-servers/browser-tools-config.json` to include incognito in the args array:

```json
"args": [
  "--no-sandbox",
  "--disable-setuid-sandbox",
  "--disable-dev-shm-usage",
  "--disable-gpu",
  "--incognito"
]
```

### 3. Browser MCP Documentation
Updated working solution documentation to include incognito flag in all launch examples.

## Benefits of Incognito Mode for Testing

1. **Clean State**: Each test session starts with no cookies, cache, or stored data
2. **Consistent Results**: Tests are not affected by previous browsing sessions
3. **Privacy**: No test data is saved to the user's profile
4. **Accurate Testing**: Simulates first-time user experience
5. **No Interference**: Extensions are disabled by default in incognito mode

## Usage

### Quick Launch Command
```bash
# From WSL terminal
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
```

### Verify Incognito Mode
After launching Chrome, you should see:
- Dark theme browser window
- "You've gone incognito" message
- Incognito icon in the top-right corner

### Testing with MCP Tools
Both Browser Tools and Stagehand MCPs will now automatically use the incognito instance when connecting to port 9222.

## Troubleshooting

### If Chrome doesn't launch in incognito:
1. Close all Chrome instances
2. Check Task Manager for lingering chrome.exe processes
3. Re-run the launch command

### If tests fail to connect:
1. Verify Chrome is running: `curl http://localhost:9222/json/version`
2. Check that no other Chrome instance is using port 9222
3. Ensure the incognito window is not manually closed during testing

## Notes
- The `--incognito` flag is now part of the standard launch configuration
- This applies to all UI testing scenarios
- Manual testing should also use incognito mode for consistency
- Production deployment testing should validate both incognito and normal modes