@echo off
REM launch-chrome-universal.bat - Universal Chrome launcher (Windows version)
REM This is the Windows equivalent of launch-chrome-universal.sh
REM Always launches Chrome in incognito mode with the proven method

echo Chrome Universal Launcher - Always Incognito
echo ===========================================
echo.

REM Configuration
set CHROME_PORT=9222
if not "%1"=="" set CHROME_PORT=%1

set CHROME_PATH=C:\Program Files\Google\Chrome\Application\chrome.exe
set USER_DATA_DIR=%TEMP%\chrome-automation-%RANDOM%

REM Function to kill existing Chrome debug instances
echo [Step 1/3] Terminating existing Chrome debug instances...
powershell -Command ^
    "Get-Process chrome -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*remote-debugging-port=%CHROME_PORT%*' } | Stop-Process -Force; " ^
    "$connection = Get-NetTCPConnection -LocalPort %CHROME_PORT% -ErrorAction SilentlyContinue; " ^
    "if ($connection) { Stop-Process -Id $connection.OwningProcess -Force -ErrorAction SilentlyContinue }"

timeout /t 2 /nobreak >nul

REM Check if Chrome exists
if not exist "%CHROME_PATH%" (
    echo.
    echo ERROR: Chrome not found at %CHROME_PATH%
    echo Please install Google Chrome or update CHROME_PATH in this script.
    echo.
    pause
    exit /b 1
)

REM Launch Chrome with the PROVEN PowerShell method
echo [Step 2/3] Launching Chrome with PowerShell bridge method...
echo            Mode: INCOGNITO (Private Browsing)
echo            Port: %CHROME_PORT%
echo.

powershell -Command "& '%CHROME_PATH%' --remote-debugging-port=%CHROME_PORT% --incognito --no-first-run --no-default-browser-check --disable-popup-blocking --disable-translate --disable-background-timer-throttling --disable-renderer-backgrounding --disable-device-discovery-notifications --user-data-dir='%USER_DATA_DIR%'"

REM Wait for Chrome to start
echo [Step 3/3] Verifying Chrome startup...
timeout /t 3 /nobreak >nul

REM Verify Chrome is running
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:%CHROME_PORT%/json/version' -TimeoutSec 2; Write-Host 'SUCCESS: Chrome DevTools is accessible' -ForegroundColor Green } catch { Write-Host 'WARNING: Cannot connect to Chrome DevTools' -ForegroundColor Yellow }"

echo.
echo ==========================================
echo Chrome Universal Launcher Status:
echo.

REM Check if Chrome started successfully
netstat -an | findstr ":%CHROME_PORT%" >nul
if %ERRORLEVEL% == 0 (
    echo [SUCCESS] Chrome is running!
    echo.
    echo Details:
    echo   - Mode: INCOGNITO (Always)
    echo   - Debug Port: %CHROME_PORT%
    echo   - DevTools URL: http://localhost:%CHROME_PORT%
    echo.
    echo Compatible with:
    echo   - Browser Tools MCP
    echo   - Stagehand MCP
    echo   - Puppeteer/Playwright
    echo.
) else (
    echo [WARNING] Chrome may not have started properly
    echo.
    echo Troubleshooting tips:
    echo   1. Try a different port: launch-chrome-universal.bat 9223
    echo   2. Check Windows Firewall settings
    echo   3. Run as Administrator
    echo   4. Close all Chrome windows and try again
    echo.
)

echo Press any key to exit...
pause >nul