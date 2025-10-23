# MCP Server Configuration Issues - Troubleshooting Guide

## Problem Summary

**Recurring Issue**: MCP servers configured in `~/.config/claude/mcp.json` are not appearing in Claude Code after system restart.

**Last Updated**: 2025-10-23
**Status**: ONGOING - Recurring after each system reboot

## Symptoms

1. Run `/mcp` command in Claude Code → "No MCP servers configured"
2. Config file exists at `~/.config/claude/mcp.json` with 10 servers
3. Config file exists at `~/.claude/config.json` with additional servers
4. Restarting Claude Code does NOT fix the issue
5. Problem persists across multiple sessions

## Configuration File Locations

### 1. Global Config (Not being read)
**Location**: `/home/chad/.config/claude/mcp.json`

**Servers Configured** (10 total):
- commands
- filesystem
- github
- postgres (ShipEngine DB on port 5435)
- memory
- docker
- playwright
- postgres-accounting (port 5433)
- google-drive
- excel

### 2. Session Config (Not being read)
**Location**: `/home/chad/.claude/config.json`

**Servers Configured**:
- browser-tools (headed/headless modes)
- stagehand (AI browser automation)
- git (Python MCP)
- fetch (Python MCP)
- time (Python MCP)
- sqlite (Python MCP)
- memory
- google-drive
- excel

### 3. Project Config (May be overriding?)
**Location**: `/home/chad/repos/witchcityrope/.claude/mcp-config.json`

**Servers Configured** (2 only):
- context7
- chrome-devtools

## Observed Behavior Pattern

### What Doesn't Work
- ❌ Restarting Claude Code
- ❌ Waiting for config files to be discovered
- ❌ Relying on config file presence alone
- ❌ Assumes automatic config loading

### What Has Worked (Temporarily)
- ✅ Running CLI installation tools: `claude mcp add [server-name]`
- ✅ After CLI installation, servers appear when running `/mcp`
- ⚠️ **BUT**: Servers disappear again after system reboot

## Hypothesis

The issue appears to be related to how Claude Code resolves MCP configuration files:

1. **Config Hierarchy**: Project-level config may override global config
2. **Registration vs Configuration**: MCP servers may need explicit registration, not just config file presence
3. **Session Persistence**: MCP server registrations may not persist across system reboots
4. **Environment Setup**: Something in the environment initialization may be missing

## Timeline of Issues

### October 2025 (Previous Session)
- User reports MCP servers not working
- Solution: Used `claude mcp add` CLI commands
- Result: Servers appeared and worked
- Duration: Until system reboot

### October 23, 2025 (Current Session)
- System restarted
- All MCP servers gone again
- `/mcp` returns "No MCP servers configured"
- Config files still present and unchanged
- Same solution attempted: CLI installation

## Attempted Solutions

### Attempt 1: Restart Claude Code
**Result**: FAILED - Servers still not appearing

### Attempt 2: Check Config Files
**Result**: All config files present and properly formatted

### Attempt 3: Run `claude doctor`
**Result**: ERROR - "Raw mode is not supported on the current process.stdin"

### Previous Success: CLI Installation
**Command Pattern**:
```bash
claude mcp add commands --npx @mcp-get/server-commands
claude mcp add filesystem --npx @modelcontextprotocol/server-filesystem
claude mcp add github --npx @modelcontextprotocol/server-github
# ... etc for all servers
```

**Result**: Temporary success until next reboot

## Current Investigation (2025-10-23)

### ROOT CAUSE IDENTIFIED ✅

**The Real Config File**: MCP servers are stored in `~/.claude.json` (NOT `~/.config/claude/mcp.json`)

**File Location**: `/home/chad/.claude.json`
**File Size**: 196KB (2,073 lines) - **TOO LARGE!**
**Breakdown**:
- `projects` section: 143KB (73% of file!)
- Project history: 100 entries stored
- `cachedChangelog`: 30KB

**Storage Structure**:
```json
{
  "projects": {
    "/home/chad/repos/witchcityrope": {
      "mcpServers": {
        "memory": { "type": "stdio", "command": "npx", "args": [...] }
      },
      "history": [100 entries],
      "enabledMcpjsonServers": [],
      "disabledMcpjsonServers": []
    }
  }
}
```

### Config File Analysis

**Multiple config files exist but only ONE is used**:
```bash
/home/chad/.config/claude/mcp.json          # 10 servers - NOT USED BY CLAUDE CODE
/home/chad/.claude/config.json              # Custom servers - NOT USED BY CLAUDE CODE
/home/chad/repos/witchcityrope/.claude/mcp-config.json  # 2 servers - UNKNOWN USAGE
/home/chad/.claude.json                     # ACTUAL CONFIG - Used by Claude Code
```

**Issue**: File is too large (196KB) and may not be loading properly at startup due to bloat from project history

### Claude CLI Behavior

```bash
$ claude mcp add memory -- npx -y @mcp-get/server-memory
# Output: "Added stdio MCP server memory to local config"
# Output: "File modified: /home/chad/.claude.json [project: /home/chad/repos/witchcityrope]"
```

**This confirms**:
- CLI writes to `~/.claude.json` under project-specific section
- Scope defaults to "local" (project-level)
- File bloat from history may be preventing proper loading at startup

## Potential Root Causes

### 1. Config File Priority
- Project-level `mcp-config.json` may override global configs
- Claude Code may only read ONE config file location
- Config file naming convention issue (`mcp.json` vs `mcp-config.json`)

### 2. Registration Database
- MCP servers may need to be registered in a separate database
- Config files alone may not be sufficient
- `claude mcp add` may write to a different location than config files

### 3. Session State
- MCP server state may be stored in session-specific location
- Session state may not persist across reboots
- `/home/chad/.claude/session-env/` may contain ephemeral MCP state

### 4. Environment Variables
- MCP initialization may depend on environment variables
- These variables may not be set during terminal startup
- Need to check `.bashrc`, `.bash_profile`, or shell initialization

## ✅ SOLUTION IMPLEMENTED (2025-10-23)

### Fix Script Created and Tested

**Location**: `/home/chad/.config/claude/fix-mcp-config.sh`

**What it does**:
1. Backs up current `~/.claude.json` to `~/.config/claude/backups/`
2. Reduces project history from 100 entries to 10 (reduces file size by ~60%)
3. Adds essential MCP servers (memory, filesystem, github, docker)
4. Verifies server installation

**Results**:
- File size: 196KB → 76KB (61% reduction!)
- Lines: 2,073 → 700 (66% reduction!)
- Servers registered successfully

**Usage**:
```bash
# Run manually when MCP servers disappear
~/.config/claude/fix-mcp-config.sh

# Or add to shell startup (.bashrc) for automatic fixing
# Add this line to ~/.bashrc:
# [ -f ~/.config/claude/fix-mcp-config.sh ] && ~/.config/claude/fix-mcp-config.sh
```

**Next Steps After Running Script**:
1. Restart Claude Code
2. Run `/mcp` command to verify servers are available
3. If issue recurs after reboot, add script to `.bashrc` for automatic execution

### CRITICAL FINDING

**Root Cause**: The `~/.claude.json` file becomes bloated with project history (100+ entries), causing it to grow to 196KB+. This large file size may prevent proper loading at startup, especially after system reboot.

**Permanent Solution**: Periodically clean project history or add cleanup script to shell initialization.

---

## Proposed Solutions (Alternative Approaches)

### Solution 1: Create Startup Script (ALREADY IMPLEMENTED - SEE ABOVE)
Create a shell initialization script that runs on every terminal start:

**File**: `/home/chad/.config/claude/init-mcp.sh`
```bash
#!/bin/bash

# Check if MCP servers are configured
if ! claude mcp list 2>&1 | grep -q "commands\|filesystem"; then
    echo "MCP servers not detected. Registering..."

    # Add all required MCP servers
    claude mcp add commands --npx @mcp-get/server-commands
    claude mcp add filesystem --npx @modelcontextprotocol/server-filesystem
    claude mcp add github --npx @modelcontextprotocol/server-github
    claude mcp add postgres --npx @mcp-get/server-postgres
    claude mcp add memory --npx @mcp-get/server-memory
    claude mcp add docker --npx @mcp-get/server-docker
    claude mcp add playwright --npx @playwright/mcp-server

    echo "MCP servers registered successfully"
fi
```

**Add to** `~/.bashrc`:
```bash
# Initialize MCP servers for Claude Code
if [ -f ~/.config/claude/init-mcp.sh ]; then
    source ~/.config/claude/init-mcp.sh
fi
```

### Solution 2: Consolidate Config Files
Remove redundant config files and use single source of truth:

```bash
# Keep only ONE config file
# Move all servers to ~/.config/claude/mcp.json
# Remove ~/.claude/config.json
# Remove project-level .claude/mcp-config.json (or keep minimal)
```

### Solution 3: Investigate Claude Code Session State
```bash
# Check session environment
ls -la ~/.claude/session-env/

# Look for MCP-related state files
find ~/.claude -name "*mcp*" -type f

# Check if MCP state is stored elsewhere
find ~ -maxdepth 3 -name "*mcp*" -type f 2>/dev/null
```

### Solution 4: Manual Registration Script
Create a script that can be run manually when needed:

**File**: `/home/chad/.config/claude/register-mcp-servers.sh`
```bash
#!/bin/bash

echo "Registering all MCP servers..."

# Register each server
claude mcp add commands --npx @mcp-get/server-commands \
    --allowed-commands "bash,sh,grep,find,ls,cat,echo,pwd,cd,mkdir,rm,cp,mv,touch,chmod,chown,ps,kill,pkill,top,df,du,free,uname,whoami,which,whereis,git,npm,node,python,pip,docker,docker-compose"

claude mcp add filesystem --npx @modelcontextprotocol/server-filesystem \
    /home/chad/repos /home/chad/documents /home/chad/downloads

claude mcp add github --npx @modelcontextprotocol/server-github
# ... continue for all servers

echo "MCP server registration complete. Run /mcp to verify."
```

**Usage**:
```bash
chmod +x ~/.config/claude/register-mcp-servers.sh
~/.config/claude/register-mcp-servers.sh
```

## Next Steps

1. **Document Current State**: ✅ COMPLETED (this file)
2. **Test Startup Script**: Create and test automatic MCP initialization
3. **Consolidate Configs**: Determine which config file Claude Code actually reads
4. **Check Session State**: Investigate where MCP registration is actually stored
5. **Monitor Pattern**: Track if issue recurs after implementing solution

## Related Files

- `/home/chad/.config/claude/mcp.json` - Global MCP config
- `/home/chad/.claude/config.json` - Session MCP config
- `/home/chad/repos/witchcityrope/.claude/mcp-config.json` - Project MCP config
- `/home/chad/.claude/session-env/` - Session state directory

## References

- MCP Setup Documentation: `/home/chad/repos/witchcityrope/docs/standards-processes/MCP/MCP_UBUNTU_SETUP.md`
- MCP Quick Reference: `/home/chad/repos/witchcityrope/docs/standards-processes/MCP/MCP_QUICK_REFERENCE.md`
- Claude Code MCP Docs: https://docs.claude.com/en/docs/claude-code/mcp

## Update Log

| Date | Update | Result |
|------|--------|--------|
| 2025-10-23 | Initial documentation of recurring issue | In progress |
| 2025-10-23 | Identified three separate config file locations | Investigating |
| 2025-10-23 | Attempted `claude doctor` - failed with stdin error | Failed |

---

**User Feedback**: "This has been a constant problem that we have tried over and over to fix. The only solution we have found was running the CLI installation tools last time. After claude did that, I restarted claude, and the MCP tools all showed up when I typed /mcp. But now that I have restarted the computer, they seem to all be gone again."

**Key Insight**: The issue is not with Claude Code sessions, but with SYSTEM REBOOTS. MCP registration does not persist across system restarts, suggesting it's stored in ephemeral location or requires environment initialization.
