# MCP Status Checker Documentation

## Overview

The `check-mcp-status.sh` script is a comprehensive diagnostic tool for verifying the status of all 8 MCP (Model Context Protocol) servers used in the WitchCityRope project. It provides real-time status updates, identifies configuration issues, and offers quick fixes for common problems.

## Features

- **Complete Environment Check**: Verifies all required dependencies
- **Individual Server Testing**: Tests each of the 8 MCP servers
- **Color-Coded Output**: Easy-to-read status indicators
- **Quick Fix Suggestions**: Actionable commands to resolve issues
- **WSL/Linux Compatible**: Works in both WSL2 and native Linux environments

## Usage

### Basic Usage
```bash
./check-mcp-status.sh
```

### Run at Claude Startup
Add to your shell profile (`.bashrc` or `.zshrc`):
```bash
# Check MCP status on Claude startup
if [ -f ~/source/repos/WitchCityRope/check-mcp-status.sh ]; then
    alias mcp-status='~/source/repos/WitchCityRope/check-mcp-status.sh'
    # Optional: Run automatically (commented out by default)
    # ~/source/repos/WitchCityRope/check-mcp-status.sh
fi
```

### Save Output to File
```bash
./check-mcp-status.sh > mcp-status-report-$(date +%Y%m%d-%H%M%S).txt
```

## Status Indicators

- ✅ **Green**: Server is working correctly
- ⚠️ **Yellow**: Server needs setup or configuration
- ❌ **Red**: Server is failing or not available
- ℹ️ **Blue**: Informational message
- 🔧 **Purple**: Quick fix command

## MCP Servers Checked

1. **FileSystem MCP**: File system access and manipulation
2. **Memory MCP**: Persistent memory/knowledge graph
3. **Docker MCP**: Container management
4. **Commands MCP**: System command execution
5. **GitHub MCP**: GitHub API integration
6. **PostgreSQL MCP**: Database queries
7. **Browser Tools MCP**: Browser automation
8. **Stagehand MCP**: AI-powered browser control

## Common Issues and Fixes

### PostgreSQL Password Not Set
```bash
# 1. Check the actual password in docker-compose.yml
grep POSTGRES_PASSWORD docker-compose.yml

# 2. Update claude_desktop_config.json with the correct password
# Replace "your_password_here" with the actual password
```

### Browser Automation Not Working
```bash
# For WSL users - run on Windows side:
chrome.exe --remote-debugging-port=9222

# For Linux users:
google-chrome --remote-debugging-port=9222 &

# Start browser server
cd src/mcp-servers/browser-server-persistent
./browser-server-manager.sh start
```

### GitHub CLI Not Authenticated
```bash
# Install GitHub CLI
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh

# Authenticate
gh auth login
```

### Docker Not Running
```bash
# For WSL users:
# Start Docker Desktop on Windows

# For Linux users:
sudo systemctl start docker
sudo systemctl enable docker
```

## Environment Variables

The script checks for these key paths:
- Claude Config: `C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config.json`
- Memory Data: `C:\Users\chad\AppData\Roaming\Claude\memory-data\memory.json`
- Stagehand: `C:\Users\chad\source\repos\WitchCityRope\mcp-server-browserbase\stagehand`

## Integration with CI/CD

You can integrate this script into your CI/CD pipeline:

```yaml
# Example GitHub Actions workflow
- name: Check MCP Status
  run: |
    chmod +x ./check-mcp-status.sh
    ./check-mcp-status.sh
  continue-on-error: true
```

## Customization

### Add Custom Checks
Edit the script to add custom server checks:
```bash
# Add after the existing server checks
test_mcp_server "Custom MCP" \
    "your-test-command-here" \
    "Your fix suggestion here"
```

### Change Output Format
For JSON output, pipe through jq:
```bash
./check-mcp-status.sh | grep -E "(✅|❌|⚠)" | jq -R -s -c 'split("\n")[:-1]'
```

## Troubleshooting

### Script Not Executable
```bash
chmod +x check-mcp-status.sh
```

### Command Not Found
Ensure the script directory is in your PATH or use full path:
```bash
/mnt/c/users/chad/source/repos/WitchCityRope/check-mcp-status.sh
```

### Permission Denied
Some checks may require sudo:
```bash
sudo ./check-mcp-status.sh
```

## Related Documentation

- [MCP Servers Documentation](docs/MCP_SERVERS.md)
- [MCP Debugging Guide](docs/MCP_DEBUGGING_GUIDE.md)
- [MCP Quick Reference](docs/MCP_QUICK_REFERENCE.md)
- [Browser MCP Setup](src/mcp-servers/README_BROWSER_MCP.md)

## Support

For issues specific to:
- **Script problems**: Check this README
- **MCP configuration**: See `docs/MCP_SERVERS.md`
- **Individual server setup**: Check server-specific documentation
- **Claude integration**: Restart Claude Desktop after configuration changes