# Stagehand MCP Server Setup Guide

## Installation Complete!

The Stagehand MCP server has been successfully installed and configured. Here's what was done:

1. **Cloned the repository**: The mcp-server-browserbase repository has been cloned to:
   ```
   C:\Users\chad\source\repos\WitchCityRope\mcp-server-browserbase\
   ```

2. **Built the server**: Dependencies installed and TypeScript compiled successfully.

3. **Updated Claude Desktop configuration**: Added Stagehand to your MCP servers in:
   ```
   C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json
   ```

## Next Steps

### 1. Add Your OpenAI API Key

You need to add your OpenAI API key to the configuration:

1. Get your OpenAI API key from https://platform.openai.com/api-keys
2. Open `C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json`
3. Find the line with `"OPENAI_API_KEY": "<YOUR_OPENAI_API_KEY>"`
4. Replace `<YOUR_OPENAI_API_KEY>` with your actual API key

### 2. Start Chrome in Debug Mode

Before using Stagehand, you need to start Chrome with remote debugging enabled. Use one of these methods:

**Option A: Use the batch file**
```
C:\Users\chad\source\repos\WitchCityRope\start-chrome-debug.bat
```

**Option B: Use PowerShell**
```powershell
C:\Users\chad\source\repos\WitchCityRope\start-chrome-debug.ps1
```

**Option C: Manual command**
```
chrome.exe --remote-debugging-port=9222 --user-data-dir="%TEMP%\chrome-debug"
```

### 3. Restart Claude Desktop

After updating the configuration and starting Chrome:
1. Completely close Claude Desktop
2. Restart Claude Desktop
3. The Stagehand tools should now be available

## Available Stagehand Tools

Once configured, you'll have access to these tools:
- `stagehand_navigate`: Navigate to any URL in the browser
- `stagehand_click`: Click on elements
- `stagehand_type`: Type text into input fields
- `stagehand_scroll`: Scroll the page
- `stagehand_wait`: Wait for elements or conditions
- `stagehand_goBack`: Navigate back in browser history
- `stagehand_goForward`: Navigate forward in browser history
- `stagehand_reload`: Reload the current page
- `stagehand_screenshot`: Take a screenshot of the current page
- `stagehand_pdf`: Generate a PDF of the current page
- `stagehand_getPageHTML`: Get the HTML of the current page
- `stagehand_getPageURL`: Get the current page URL

## Alternative: Browserbase Cloud Setup

If you want to use Browserbase cloud instead of local Chrome:

1. Sign up at https://browserbase.com
2. Get your API key and Project ID
3. Update the configuration in `claude_desktop_config.json`:
   ```json
   "stagehand": {
     "command": "node",
     "args": ["C:\\Users\\chad\\source\\repos\\WitchCityRope\\mcp-server-browserbase\\stagehand\\dist\\index.js"],
     "env": {
       "BROWSERBASE_API_KEY": "<YOUR_BROWSERBASE_API_KEY>",
       "BROWSERBASE_PROJECT_ID": "<YOUR_BROWSERBASE_PROJECT_ID>",
       "OPENAI_API_KEY": "<YOUR_OPENAI_API_KEY>"
     }
   }
   ```

## Troubleshooting

1. **Tools not showing up**: Make sure Claude Desktop is completely closed and restarted
2. **Connection errors**: Ensure Chrome is running with `--remote-debugging-port=9222`
3. **API key errors**: Verify your OpenAI API key is valid and has credits
4. **Check logs**: Look at logs in `C:\Users\chad\AppData\Roaming\Claude\logs\`

## File Locations

- **Stagehand server**: `C:\Users\chad\source\repos\WitchCityRope\mcp-server-browserbase\stagehand\dist\index.js`
- **Configuration**: `C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json`
- **Chrome debug starter**: `C:\Users\chad\source\repos\WitchCityRope\start-chrome-debug.bat`