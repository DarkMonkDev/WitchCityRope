# Chrome Incognito Mode Update Summary

## Overview
All Chrome launch scripts have been updated to ensure Chrome ALWAYS launches in incognito mode using the proven PowerShell bridge method. A new universal launcher script has been created as the standard way to launch Chrome.

## Key Changes

### 1. New Universal Launcher Script
**File**: `/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh`
- Primary script that should be used everywhere
- Always launches Chrome in incognito mode
- Uses the proven PowerShell bridge method
- Includes proper Chrome termination before launch
- Provides detailed status and troubleshooting information

### 2. Updated Scripts

#### Shell Scripts
- **launch-chrome-standard.sh** - Now redirects to universal launcher
- **launch-chrome-wsl.sh** - Now redirects to universal launcher
- **browser-server-manager.sh** - Updated to use universal launcher

#### Batch Files  
- **launch-chrome-incognito.bat** - Enhanced with better process termination
- **start-browser-server.bat** - Updated to always use incognito mode
- **launch-chrome-universal.bat** (NEW) - Windows version of universal launcher

#### PowerShell Scripts
- **browser-bridge.ps1** - Updated to always include --incognito flag

### 3. Documentation Updates
- **CLAUDE.md** - Updated to recommend the universal launcher script
- Added references to the new standardized approach

## Usage

### From WSL (Recommended)
```bash
# Use the universal launcher script
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh launch

# Commands available:
# launch/start - Launch Chrome with debugging in incognito mode
# kill/stop    - Kill existing Chrome debug instances  
# status       - Check Chrome debug status
# restart      - Kill and relaunch Chrome
# test         - Test the DevTools connection
```

### From Windows
```cmd
REM Use the universal batch file
C:\Users\chad\source\repos\WitchCityRope\src\mcp-servers\launch-chrome-universal.bat

REM Or the incognito-specific batch file
C:\Users\chad\source\repos\WitchCityRope\src\mcp-servers\launch-chrome-incognito.bat
```

### Direct PowerShell Command
```powershell
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
```

## Key Features

1. **Always Incognito Mode**
   - Every script now enforces incognito mode
   - Provides privacy and clean session state

2. **Process Management**
   - Automatically kills existing Chrome debug instances
   - Prevents port conflicts and stale processes

3. **Consistent Method**
   - Uses PowerShell bridge method across all scripts
   - Proven to work reliably with both Browser Tools and Stagehand MCPs

4. **Better Error Handling**
   - Clear status messages
   - Troubleshooting tips included
   - Connection verification built-in

## Benefits

- **Privacy**: All browser sessions run in incognito mode
- **Consistency**: One standard method used everywhere
- **Reliability**: Proven PowerShell bridge method
- **Simplicity**: Single command to launch Chrome properly
- **Compatibility**: Works with all browser automation tools

## Testing

To verify Chrome is running properly:
```bash
# Test from WSL
curl http://localhost:9222/json/version

# Or use the test command
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh test
```

## Migration

All existing scripts have been updated to use the new approach. No action required from users - simply use any of the launch scripts and Chrome will always start in incognito mode with proper settings.