@echo off
REM start-browser-server.bat - Windows batch file to start the browser server in WSL
REM Always uses incognito mode for Chrome

echo Browser Server Launcher for WSL
echo ==============================
echo.

REM First, kill any existing Chrome debug instances
echo [1/4] Killing any existing Chrome debug instances...
powershell -Command "Get-Process chrome -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*remote-debugging-port=9222*' } | Stop-Process -Force" >nul 2>&1
timeout /t 2 /nobreak >nul

REM Launch Chrome with incognito mode (ALWAYS)
echo [2/4] Launching Chrome in INCOGNITO mode with debugging...
powershell -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito --no-first-run --no-default-browser-check --user-data-dir=$env:TEMP\chrome-automation"

REM Wait for Chrome to start
echo [3/4] Waiting for Chrome to start...
timeout /t 3 /nobreak >nul

REM Get the WSL distribution name (change if you use a different distro)
set WSL_DISTRO=Ubuntu

REM Path to the manager script in WSL format
set SCRIPT_PATH=/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-server-persistent/browser-server-manager.sh

REM Start the server
echo [4/4] Starting browser server in WSL...
wsl -d %WSL_DISTRO% -e bash -c "%SCRIPT_PATH% start"

echo.
echo ========================================
echo Browser server startup complete!
echo.
echo Chrome is running in INCOGNITO mode
echo Remote debugging port: 9222
echo.
echo Check status:
echo   wsl -d %WSL_DISTRO% -e bash -c "%SCRIPT_PATH% status"
echo.
echo View logs:
echo   wsl -d %WSL_DISTRO% -e bash -c "%SCRIPT_PATH% logs"
echo.
echo Stop server:
echo   wsl -d %WSL_DISTRO% -e bash -c "%SCRIPT_PATH% stop"
echo ========================================
echo.
pause