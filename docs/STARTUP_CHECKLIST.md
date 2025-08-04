# Claude Code Session Startup Checklist

## Quick Start Steps

### 1. Verify MCP Servers
```bash
# Check if MCP servers are listed in Claude config
cat ~/.claude/claude_desktop_config.json | grep -A 5 "mcpServers"

# Test filesystem server
ls /mnt/c/Users/chad/source/repos/WitchCityRope

# Test browser automation (if configured)
# Should see Chrome-related MCP server in config
```

### 2. Start Chrome for Browser Automation
```bash
# Windows (from WSL)
/mnt/c/Program\ Files/Google/Chrome/Application/chrome.exe --remote-debugging-port=9222

# Alternative: Use existing Chrome profile
/mnt/c/Program\ Files/Google/Chrome/Application/chrome.exe --remote-debugging-port=9222 --user-data-dir="C:\Users\chad\AppData\Local\Google\Chrome\User Data"
```

### 3. Important Files Reference
- **MCP Config**: `~/.claude/claude_desktop_config.json`
- **Project Root**: `/mnt/c/Users/chad/source/repos/WitchCityRope`
- **MCP Servers**: `/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers`
- **Chrome User Data**: `C:\Users\chad\AppData\Local\Google\Chrome\User Data`

### 4. Common Commands
```bash
# Navigate to project
cd /mnt/c/Users/chad/source/repos/WitchCityRope

# Check git status
git status

# View recent commits
git log --oneline -10

# Search for files
find . -name "*.py" -type f
find . -name "*.js" -type f

# Search content
grep -r "search_term" . --include="*.py"
```

### 5. Troubleshooting

#### MCP Server Issues
- **Not showing in Claude**: Restart Claude Desktop
- **Permission errors**: Check file permissions with `ls -la`
- **Path issues**: Use absolute paths starting with `/mnt/c/`

#### Chrome Automation Issues
- **Port already in use**: Kill existing Chrome processes or use different port
- **Can't connect**: Ensure Chrome started with `--remote-debugging-port=9222`
- **Profile locked**: Close all Chrome instances before starting with debug port

#### File Access Issues
- **WSL path mapping**: Windows `C:\` maps to `/mnt/c/` in WSL
- **Line endings**: Use `dos2unix` if needed for Windows files
- **Permissions**: May need to use `chmod` for executable scripts

### Quick Verification Script
```bash
#!/bin/bash
echo "=== Claude Code Session Check ==="
echo "Working directory: $(pwd)"
echo "Git repo: $(git rev-parse --is-inside-work-tree 2>/dev/null || echo 'No')"
echo "MCP Config exists: $([ -f ~/.claude/claude_desktop_config.json ] && echo 'Yes' || echo 'No')"
echo "Chrome check: $(tasklist.exe 2>/dev/null | grep -i chrome | head -1 || echo 'Not running')"
```