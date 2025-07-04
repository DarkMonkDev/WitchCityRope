@echo off
REM launch-chrome-incognito.bat - Universal Chrome launcher for Windows
REM Always launches Chrome in incognito mode with remote debugging

echo Chrome Universal Launcher - Always Incognito
echo ===========================================
echo.

REM Configuration
set CHROME_PORT=9222
set CHROME_PATH=C:\Program Files\Google\Chrome\Application\chrome.exe
set USER_DATA_DIR=%TEMP%\chrome-automation-%RANDOM%

REM Kill any existing Chrome debug instances
echo [1/3] Killing existing Chrome debug instances...
powershell -Command "Get-Process chrome -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*remote-debugging-port=%CHROME_PORT%*' } | Stop-Process -Force" >nul 2>&1

REM Also try to kill by port
powershell -Command "$connection = Get-NetTCPConnection -LocalPort %CHROME_PORT% -ErrorAction SilentlyContinue; if ($connection) { Stop-Process -Id $connection.OwningProcess -Force -ErrorAction SilentlyContinue }" >nul 2>&1

timeout /t 2 /nobreak >nul

REM Check if Chrome exists
if not exist "%CHROME_PATH%" (
    echo ERROR: Chrome not found at %CHROME_PATH%
    echo Please install Google Chrome or update the path in this script.
    pause
    exit /b 1
)

REM Launch Chrome with debugging and incognito (ALWAYS)
echo [2/3] Launching Chrome in INCOGNITO mode with debugging on port %CHROME_PORT%...
start "" "%CHROME_PATH%" ^
    --remote-debugging-port=%CHROME_PORT% ^
    --incognito ^
    --no-first-run ^
    --no-default-browser-check ^
    --disable-popup-blocking ^
    --disable-translate ^
    --disable-background-timer-throttling ^
    --disable-renderer-backgrounding ^
    --disable-device-discovery-notifications ^
    --user-data-dir="%USER_DATA_DIR%"

REM Wait for Chrome to start
echo [3/3] Waiting for Chrome to start...
timeout /t 3 /nobreak >nul

REM Check if Chrome started successfully
netstat -an | findstr ":%CHROME_PORT%" >nul
if %ERRORLEVEL% == 0 (
    echo.
    echo SUCCESS! Chrome is running:
    echo   - Mode: INCOGNITO (Private Browsing)
    echo   - Debug Port: %CHROME_PORT%
    echo   - Remote debugging: http://localhost:%CHROME_PORT%
    echo.
    echo Ready for automation with:
    echo   - Browser Tools MCP
    echo   - Stagehand MCP
    echo.
) else (
    echo.
    echo WARNING: Chrome may not have started properly
    echo.
    echo Troubleshooting:
    echo   1. Check if port %CHROME_PORT% is already in use
    echo   2. Try running as Administrator
    echo   3. Check Windows Firewall settings
    echo.
)

pause