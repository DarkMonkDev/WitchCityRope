# Universal WSL MCP Setup Guide for Claude Code

This guide provides instructions for Claude Code sessions to enable WSL-based MCP servers on any project running on this machine. The MCP servers are already installed in WSL and can be shared across all projects.

## Quick Start Prompt for Claude Code

Copy and paste this prompt when starting a new Claude Code session:

```
I need to enable WSL-based MCP servers for this project. The machine already has MCP servers installed in WSL including GitHub CLI, Browser Tools, and Stagehand. Please:

1. Check if I can access the WSL environment and the installed MCP tools
2. Create or update my project's CLAUDE.md file with MCP usage patterns specific to WSL
3. Create wrapper scripts if needed for my project to use the MCP servers
4. Test that the MCP servers are accessible from my project

The MCP servers are already installed at:
- GitHub CLI: /home/chad/.local/bin/gh (authenticated)
- Browser Tools MCP: /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/
- Stagehand: /mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand/
- WSL Configuration: /mnt/c/Users/chad/AppData/Roaming/Claude/claude_desktop_config_wsl.json

The machine is running WSL2 with Ubuntu 24.04 and has Node.js v24.3.0 installed via nvm.
```

## Detailed Setup Instructions

### Step 1: Verify WSL Access

First, verify you can access WSL and the installed tools:

```bash
# Check WSL access
uname -a  # Should show Linux kernel

# Check Node.js
node --version  # Should show v24.3.0

# Check GitHub CLI
/home/chad/.local/bin/gh --version  # Should show gh version 2.62.0

# Check npm global packages
npm list -g --depth=0  # Should show browser-tools-mcp and mcp-server-docker
```

### Step 2: Add MCP Usage Patterns to Your Project

Create or update your project's `CLAUDE.md` file with these WSL-specific MCP patterns:

```markdown
## MCP Server Configuration (WSL)

This project uses WSL-based MCP servers shared across the machine. The servers are pre-installed and authenticated.

### Available MCP Servers

1. **GitHub CLI** (`gh`)
   - Location: `/home/chad/.local/bin/gh`
   - Already authenticated with token
   - Use for: Creating issues, PRs, managing repositories

2. **Browser Tools MCP**
   - Location: `/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/`
   - Start script: `./start-browser-tools.sh`
   - Use for: Browser automation, screenshots, testing

3. **Stagehand MCP**
   - Location: `/mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand/`
   - Wrapper: `/home/chad/stagehand-auto-connect.sh`
   - Use for: AI-powered browser automation
   - Requires: Chrome with `--remote-debugging-port=9222`

### WSL Path Conversion

When working with files:
- Windows path: `C:\Users\chad\source\repos\YourProject`
- WSL path: `/mnt/c/users/chad/source/repos/YourProject`

### Quick Commands

```bash
# Test GitHub access
/home/chad/.local/bin/gh auth status

# Start Browser Tools MCP
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers && ./start-browser-tools.sh

# Start Stagehand (first start Chrome with debugging on Windows)
/home/chad/stagehand-auto-connect.sh
```
```

### Step 3: Create Project-Specific Wrapper Scripts

Create these helper scripts in your project root:

#### `test-mcp-access.sh`
```bash
#!/bin/bash
# Test MCP server access from this project

echo "Testing MCP Server Access..."
echo "=========================="

# Test GitHub CLI
echo -n "GitHub CLI: "
if /home/chad/.local/bin/gh --version &>/dev/null; then
    echo "✓ Available"
else
    echo "✗ Not found"
fi

# Test Browser Tools
echo -n "Browser Tools MCP: "
if [ -f "/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/node_modules/.bin/browser-tools-mcp" ]; then
    echo "✓ Available"
else
    echo "✗ Not found"
fi

# Test Stagehand
echo -n "Stagehand: "
if [ -f "/mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand/dist/index.js" ]; then
    echo "✓ Available"
else
    echo "✗ Not found"
fi

# Test Node.js
echo -n "Node.js: "
node --version 2>/dev/null || echo "✗ Not found"

echo "=========================="
```

#### `use-github-mcp.sh`
```bash
#!/bin/bash
# Wrapper to use GitHub MCP from this project

export PATH="/home/chad/.local/bin:$PATH"
gh "$@"
```

### Step 4: Configure Claude Desktop (One-Time Setup)

If Claude Desktop needs to be configured for your project:

1. **Option A: Use Existing WSL Configuration**
   ```bash
   # The WSL configuration already exists at:
   # C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config_wsl.json
   
   # To use it, either:
   # 1. Copy it as your main config (backs up existing)
   # 2. Use the switch-config.bat utility
   ```

2. **Option B: Add Individual MCP Servers**
   Add these to your Claude Desktop config:
   ```json
   {
     "github-wsl": {
       "command": "wsl.exe",
       "args": ["/home/chad/.local/bin/gh"],
       "env": {}
     },
     "browser-tools-wsl": {
       "command": "wsl.exe",
       "args": [
         "bash", "-c",
         "cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers && ./start-browser-tools.sh"
       ]
     }
   }
   ```

### Step 5: Test Your Setup

Run these commands to verify everything works:

```bash
# From your project directory
chmod +x test-mcp-access.sh
./test-mcp-access.sh

# Test GitHub integration
./use-github-mcp.sh repo list --limit 5

# Test browser tools (start Chrome with debugging first)
# On Windows: chrome.exe --remote-debugging-port=9222
# Then from WSL:
curl http://localhost:9222/json/version  # Should return Chrome version info
```

## Common Issues and Solutions

### Issue: GitHub CLI says "not authenticated"
```bash
# The auth should already be configured, but if needed:
/home/chad/.local/bin/gh auth status
# Token is stored in ~/.config/gh/hosts.yml
```

### Issue: Browser Tools can't find Chrome
```bash
# Check if Chrome path is accessible from WSL
ls -la "/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"
# If not found, check Edge or update the path in browser-tools-config.json
```

### Issue: Stagehand can't connect to Chrome
```bash
# Ensure Chrome is running with debug flag on Windows
# Test connection:
curl http://localhost:9222/json/version
# If fails, try the Windows host IP:
curl http://$(ip route show | grep default | awk '{print $3}'):9222/json/version
```

## Project-Specific Considerations

### For Web Projects
- Use Browser Tools MCP for automated testing
- Use Stagehand for user flow testing
- Configure your test scripts to use the shared MCP servers

### For API Projects
- Use the Commands MCP with curl for API testing
- Use GitHub MCP for issue tracking
- Document API endpoints in your CLAUDE.md

### For Full-Stack Projects
- Combine all MCP servers for comprehensive testing
- Use Docker MCP if your project uses containers
- Create project-specific wrapper scripts for common tasks

## Sharing MCP Servers Across Projects

The beauty of this setup is that all MCP servers are installed once in WSL and shared across all projects:

1. **No Duplicate Installations**: MCP servers are installed globally or in a shared location
2. **Consistent Versions**: All projects use the same MCP server versions
3. **Shared Authentication**: GitHub authentication is configured once
4. **Resource Efficient**: Only one instance of each server needs to run

## Advanced Usage

### Creating Project-Specific MCP Aliases

Add to your project's `.bashrc` or create a `mcp-aliases.sh`:

```bash
# GitHub shortcuts
alias gh-issues='/home/chad/.local/bin/gh issue list'
alias gh-prs='/home/chad/.local/bin/gh pr list'

# Browser testing
alias test-browser='cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers && ./start-browser-tools.sh'

# Stagehand
alias stagehand='/home/chad/stagehand-auto-connect.sh'
```

### Automating MCP Server Startup

Create a `start-mcp-servers.sh` in your project:

```bash
#!/bin/bash
echo "Starting MCP servers for development..."

# Start Chrome with debugging (Windows side)
echo "Please start Chrome with: chrome.exe --remote-debugging-port=9222"

# Wait for Chrome
echo "Waiting for Chrome debugging port..."
while ! curl -s http://localhost:9222/json/version > /dev/null; do
    sleep 1
done
echo "Chrome debugging port ready!"

# Start other services as needed
echo "MCP servers ready for use!"
```

## Maintenance Notes

- MCP servers are maintained at the system level, not per-project
- Updates to MCP servers benefit all projects
- Authentication tokens are shared (be mindful of security)
- WSL2 networking is shared across all projects

---

**Remember**: This setup leverages shared MCP servers across all projects on the machine. Each project only needs to create lightweight wrapper scripts and documentation, not reinstall the servers.