# MCP vs CLI Tools Guide for WitchCityRope

## Important Clarification: MCP Servers vs CLI Tools

This guide clarifies the confusion between MCP (Model Context Protocol) servers and regular CLI tools that accomplish the same tasks. Many tools listed as "MCP servers" in various scripts are actually just CLI tools that Claude Code can use via the Bash tool.

## Current Session Reality (Claude Code on Ubuntu)

### What's Actually Available in Claude Code Sessions

**Built-in Claude Code Tools:**
- ✅ **Bash** - Execute any CLI command
- ✅ **Read/Write/Edit** - File operations
- ✅ **Grep/Glob/LS** - File searching
- ✅ **WebFetch/WebSearch** - Web operations
- ✅ **TodoWrite** - Task management
- ✅ **NotebookRead/Edit** - Jupyter notebooks

**MCP Tools in This Session:**
- ❌ **None** - No MCP servers are loaded in standard Claude Code sessions

### The Tool Confusion Explained

The `check-mcp-status.sh` script lists 8 "MCP servers" but most are actually CLI tools:

| Listed as "MCP Server" | Reality | How to Use in Claude Code |
|------------------------|---------|---------------------------|
| **FileSystem MCP** | Built-in tool | Use `Read`, `Write`, `Edit`, `Glob`, `LS` tools directly |
| **Memory MCP** | Not available | No persistent memory between sessions currently |
| **Docker MCP** | CLI tool | Use `docker` commands via Bash tool |
| **Commands MCP** | Redundant | Use Bash tool directly |
| **GitHub MCP** | CLI tool | Use `gh` commands via Bash tool |
| **PostgreSQL MCP** | CLI tool | Use `psql` commands via Bash tool |
| **Browser Tools MCP** | Not needed | Run Puppeteer scripts via `node` command |
| **Stagehand MCP** | Not available | Not installed or configured |
| **Context7 MCP** | Not loaded | Would provide documentation lookup if configured |

## How to Actually Use These Tools

### 1. Docker Operations
```bash
# Instead of Docker MCP, use:
docker ps
docker-compose logs -f web
docker exec -it witchcity-postgres psql -U postgres
```

### 2. GitHub Operations
```bash
# Instead of GitHub MCP, use:
gh pr create --title "Feature X" --body "Description"
gh issue list
gh api repos/owner/repo/pulls
```

### 3. PostgreSQL Operations
```bash
# Instead of PostgreSQL MCP, use:
psql -h localhost -p 5433 -U postgres -d witchcityrope_db -c "SELECT * FROM events;"
docker exec -it witchcity-postgres psql -U postgres
```

### 4. Puppeteer/Browser Automation
```bash
# Instead of Browser Tools MCP, use:
cd /home/chad/repos/witchcityrope/WitchCityRope/tests/e2e
node test-login.js
node run-all-tests.js
```

### 5. File Operations
```python
# Instead of FileSystem MCP, use built-in tools:
# Read: Use Read tool
# Write: Use Write tool
# Search: Use Grep or Glob tools
# List: Use LS tool
```

### 6. Memory/Context (Currently Not Available)
The Memory MCP would provide persistent context between sessions, but it's not currently available in Claude Code. Each session starts fresh.

### 7. Context7 (Documentation Lookup)
Context7 would provide up-to-date documentation lookup but is not loaded in standard sessions. Use WebSearch or WebFetch for documentation needs.

## Starting Claude Code with MCP Servers (Optional)

If you actually want MCP servers, start Claude Code with:

```bash
# Load MCP configuration
claude --mcp-config /home/chad/.config/claude/mcp.json

# Or create an alias
echo 'alias claude-mcp="claude --mcp-config ~/.config/claude/mcp.json"' >> ~/.bashrc
source ~/.bashrc
claude-mcp
```

**Note:** This is usually unnecessary since CLI tools work perfectly fine.

## Key Takeaways

1. **Most "MCP servers" are just CLI tools** - Use them via Bash commands
2. **Claude Code has excellent built-in tools** - They cover most needs
3. **MCP servers are optional** - Not required for development
4. **No persistent memory** - Each session starts fresh
5. **Everything works via CLI** - Docker, GitHub, PostgreSQL, Puppeteer all work perfectly

## Updated check-mcp-status.sh Output Interpretation

When you see:
- "❌ Docker MCP is not available" → Docker CLI is working fine, use `docker` commands
- "❌ GitHub MCP is not available" → GitHub CLI is working fine, use `gh` commands  
- "✅ FileSystem MCP is working" → Built-in file tools are available
- "❌ Memory MCP is not available" → No persistent memory (this is truly missing)

## For Future Sessions

To avoid confusion:
1. Ignore the `check-mcp-status.sh` script - it's checking for unnecessary MCP configurations
2. Use CLI tools directly via Bash - they work perfectly
3. Only consider MCP servers if you need specific features like persistent memory
4. Remember: everything you've been doing for weeks works without MCP servers!

---
*Last Updated: January 20, 2025*
*Purpose: Clarify MCP servers vs CLI tools confusion for WitchCityRope development*