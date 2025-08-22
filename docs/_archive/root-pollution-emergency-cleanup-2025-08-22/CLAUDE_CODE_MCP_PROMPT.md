# Claude Code MCP Setup Prompt

## Copy-Paste Prompt for New Projects

Use this prompt when starting a Claude Code session in a new project on this machine:

```
I'm working on a project on a Windows machine with WSL2 where MCP servers are already installed and configured. Please help me enable these shared MCP servers for my project.

The machine has the following MCP servers installed in WSL:
- GitHub CLI at /home/chad/.local/bin/gh (already authenticated)
- Browser Tools MCP at /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/
- Stagehand at /mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand/
- Node.js v24.3.0 via nvm
- Docker MCP and other standard MCP servers

Please:
1. Create a test script to verify I can access these MCP servers from my current project
2. Add MCP usage documentation to my project's CLAUDE.md or README
3. Create any necessary wrapper scripts for my project type
4. Show me how to use these MCP servers for common tasks in my project

The WSL configuration for Claude Desktop is available at:
C:\Users\chad\AppData\Roaming\Claude\claude_desktop_config_wsl.json
```

## Alternative Detailed Prompt

For more specific setup needs:

```
I need to set up WSL-based MCP server access for my [PROJECT TYPE] project. This machine already has a shared WSL environment with pre-installed MCP servers that other projects are using.

Shared MCP Resources:
- Location: WSL2 Ubuntu 24.04
- GitHub CLI: /home/chad/.local/bin/gh (authenticated with token)
- Browser automation: /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/
- AI browser control: /home/chad/stagehand-auto-connect.sh
- Node.js: v24.3.0 at /home/chad/.nvm/versions/node/v24.3.0/

My project details:
- Type: [Web App / API / Full-Stack / CLI Tool / etc.]
- Location: [Your project path]
- Testing needs: [Unit tests / Integration tests / E2E tests / etc.]
- Version control: [GitHub / GitLab / etc.]

Please create:
1. A verification script to test MCP access
2. Project-specific wrapper scripts for the MCP servers I'll need
3. Documentation for my team on how to use these MCP servers
4. Examples of using MCP servers for my specific project type
```

## Quick Reference Commands

After setup, you can use these commands:

```bash
# Test MCP server access
/home/chad/.local/bin/gh --version
node --version
ls /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/

# Use GitHub MCP
/home/chad/.local/bin/gh issue create --title "Bug" --body "Description"
/home/chad/.local/bin/gh pr create --title "Feature" --body "Details"

# Start Browser Tools (for web testing)
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers && ./start-browser-tools.sh

# Use Stagehand (requires Chrome with --remote-debugging-port=9222)
/home/chad/stagehand-auto-connect.sh
```

## Key Points to Remember

1. **No Installation Needed**: MCP servers are already installed in WSL
2. **Shared Resources**: All projects on this machine share the same MCP servers
3. **Pre-Authenticated**: GitHub is already authenticated
4. **WSL Path**: Your Windows paths become `/mnt/c/...` in WSL
5. **Chrome Debugging**: For browser tools, start Chrome with `--remote-debugging-port=9222`

## For Different Project Types

### Web Development Projects
Emphasize: Browser Tools MCP, Stagehand for UI testing

### API Projects
Emphasize: Commands MCP (curl), GitHub for issue tracking

### Full-Stack Projects
Emphasize: All MCP servers, especially Docker MCP

### Data Science Projects
Emphasize: FileSystem MCP, Memory MCP for data persistence

### Documentation Projects
Emphasize: GitHub MCP, FileSystem MCP for file management