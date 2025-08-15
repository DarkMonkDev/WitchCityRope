# New Session Setup - WitchCityRope Project
<!-- Last Updated: 2025-01-20 -->
<!-- Version: 2.0 -->
<!-- Purpose: Quick session initialization and environment verification -->

## Session Initialization

Welcome to a new WitchCityRope development session! This command sets up your environment and provides quick access to project context.

<session-checklist>
### Quick Start Checklist

1. **[ ] Read Project Configuration**
   ```bash
   # Read the main project configuration file
   cat /home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md
   ```

2. **[ ] Check Docker Environment**
   ```bash
   docker ps
   # Should show: witchcity-web, witchcity-api, witchcity-postgres
   
   # Or run comprehensive status check:
   ./check-dev-tools-status.sh
   ```

3. **[ ] Verify Application Access**
   - Web UI: http://localhost:5651
   - API: http://localhost:5653
   - Database: localhost:5433

4. **[ ] Review Recent Progress**
   ```bash
   # Check recent work
   tail -50 /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md
   ```

5. **[ ] Create Initial Todo List**
   Use TodoWrite to track your session tasks
</session-checklist>

<quick-reference>
### Essential Information

**Project**: WitchCityRope - Membership & event management for Salem's rope bondage community
**Architecture**: Pure Blazor Server (.NET 9) with PostgreSQL
**Current Phase**: Production-ready with active development

**Key Files**:
- `/home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md` - Complete project configuration (READ FIRST)
- `/home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md` - Development history and current state
- `/home/chad/repos/witchcityrope/.claude/test-credentials.env` - Test account credentials

**Critical Rules**:
- 🚨 NEVER use SQL Server, Playwright, or MudBlazor
- ✅ ALWAYS use PostgreSQL, Puppeteer, and Syncfusion
- 🚨 NEVER create Razor Pages (.cshtml files)
- ✅ ALWAYS use Pure Blazor Server components (.razor)

**Development Tools Status**:
- ✅ Docker, GitHub CLI, PostgreSQL, Puppeteer - All working via CLI commands
- ✅ File operations - Working via built-in Claude Code tools
- ✅ Context7 & Memory MCP - Configured (requires `claude mcp run` to load)
- 🚨 **IMPORTANT**: Start sessions with `claude mcp run` or `claude-witch` for MCP servers!
- 📝 See `CLAUDE_CODE_MCP_SETUP.md` for MCP setup details
</quick-reference>

<common-tasks>
### Common Session Tasks

**Start Development Environment**:
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope
./scripts/dev-start.sh
```

**Quick Container Restart** (when hot reload fails):
```bash
./restart-web.sh
```

**Run Tests**:
```bash
# Unit tests
dotnet test tests/WitchCityRope.Core.Tests/

# Integration tests (check health first)
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# E2E tests
cd tests/e2e && npm test
```

**Access Test Credentials**:
```bash
cat /home/chad/repos/witchcityrope/.claude/test-credentials.env
```
</common-tasks>

<next-steps>
### Next Steps

1. **For New Feature Work**: Use `/new_work "implement [feature description]"`
2. **For Bug Fixes**: Use `/new_work "fix [issue description]"`
3. **For Testing**: Use `/new_work "test [component/feature]"`
4. **For Documentation**: Use `/new_work "document [topic]"`

Remember: The `/new_work` command assumes you've run `/new_session` first!
</next-steps>

<session-end>
### Session End Reminders

Before ending your session:
- [ ] Update PROGRESS.md with session summary
- [ ] Commit any pending changes
- [ ] Clean up temporary files
- [ ] Document any unresolved issues
- [ ] Update phase status if applicable
</session-end>

<!-- This is a lightweight session setup. For detailed project information, see CLAUDE.md -->