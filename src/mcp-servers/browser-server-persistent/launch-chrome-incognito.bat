@echo off
REM Universal Chrome Launcher - Always launches in incognito mode
REM This ensures Chrome is launched with privacy mode enabled

echo Launching Chrome in incognito mode with remote debugging...

REM Launch Chrome directly with incognito flag
"C:\Program Files\Google\Chrome\Application\chrome.exe" --incognito --remote-debugging-port=9222 --no-first-run --no-default-browser-check

if %ERRORLEVEL% NEQ 0 (
    echo Failed to launch Chrome. Please ensure Chrome is installed.
    pause
    exit /b 1
)

echo Chrome launched successfully in incognito mode!
echo DevTools available at: http://localhost:9222
pause