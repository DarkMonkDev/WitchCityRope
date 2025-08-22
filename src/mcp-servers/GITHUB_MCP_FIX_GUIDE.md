# GitHub MCP Server Fix Guide

## Current Status
✅ **GitHub Token is VALID and WORKING**
- Authenticated as: DarkMonkDev
- Token expiration: September 22, 2025
- API access confirmed

## Issue Identified
The GitHub MCP server hangs when started directly via `npx`. This appears to be because it's designed to run as a stdio server and waits for input.

## Configuration Location
The GitHub MCP configuration is stored in:
```
C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json
```

## Current Configuration
```json
"github": {
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-github"],
  "env": {
    "GITHUB_TOKEN": "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"
  }
}
```

## How to Update GitHub Token (When Needed)

### 1. Generate a New Token
1. Go to https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Give it a descriptive name (e.g., "Claude MCP Server")
4. Select the required scopes:
   - `repo` (Full control of private repositories)
   - `read:user` (Read user profile data)
   - `read:org` (Read org and team membership)
5. Click "Generate token"
6. Copy the token immediately (it won't be shown again)

### 2. Update the Configuration
1. Open the config file:
   ```
   C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json
   ```
2. Replace the old token in the `GITHUB_TOKEN` field
3. Save the file
4. Restart Claude Desktop

### 3. Test the Token
Run the test script:
```bash
node /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/test-github-access.js
```

## Alternative: Using GitHub CLI
If the MCP server continues to have issues, you can use the GitHub CLI through the commands MCP:

1. Install GitHub CLI (if not already installed):
   ```bash
   # Windows (PowerShell)
   winget install --id GitHub.cli
   
   # Or download from: https://cli.github.com/
   ```

2. Authenticate:
   ```bash
   gh auth login
   ```

3. Use through the commands MCP server which is already configured

## Troubleshooting

### Server Hangs on Start
This is expected behavior - the GitHub MCP server runs as a stdio server and waits for input from Claude Desktop.

### Token Invalid
1. Check if the token has expired
2. Verify the token has the correct permissions
3. Ensure there are no extra spaces or characters in the token
4. Generate a new token following the steps above

### Rate Limiting
- Current limit: 5000 requests per hour
- Check remaining requests in the test script output
- Rate limit resets every hour

## Current Token Info
- **Status**: Active ✅
- **User**: DarkMonkDev
- **Expires**: September 22, 2025
- **Rate Limit**: 5000/hour (4993 remaining as of last test)