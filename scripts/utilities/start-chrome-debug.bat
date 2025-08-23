@echo off
echo Starting Chrome in debug mode for Stagehand MCP...
start chrome.exe --remote-debugging-port=9222 --user-data-dir="%TEMP%\chrome-debug"
echo Chrome started with remote debugging on port 9222
echo You can now use Stagehand in Claude Desktop
pause