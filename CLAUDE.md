# Claude Code Project Configuration - WitchCityRope

## 🤖 AI Workflow Orchestration Active

**Project**: WitchCityRope - A membership and event management platform for Salem's rope bondage community.

### ⚡ AUTOMATIC ORCHESTRATION
**ANY development request automatically triggers the orchestrator agent.**
You don't need to mention it - just describe what you want built.

### 🚨 TRIGGER WORD DETECTION - CHECK FIRST! 🚨
**BEFORE ANY ACTION, check for these triggers:**
- **continue** (ANY work/development context) → ORCHESTRATOR → MUST USE TASK TOOL
- **test/testing/debug/fix** → ORCHESTRATOR → test-fix-coordinator (AUTOMATIC)
- **implement/create/build/develop** → ORCHESTRATOR → MUST USE TASK TOOL
- **complete/finish/finalize** → ORCHESTRATOR → MUST USE TASK TOOL
- **Multi-step tasks** → ORCHESTRATOR → MUST USE TASK TOOL

**See /.claude/ORCHESTRATOR-TRIGGERS.md for full list**
**VIOLATION = User frustration (justified)**

### ⚠️ CRITICAL: ACTUAL TASK TOOL INVOCATION REQUIRED ⚠️
**When invoking orchestrator or ANY agent:**
1. **DO NOT** just say "I'm invoking the orchestrator"
2. **DO NOT** just say "I'm delegating to test-fix-coordinator"
3. **YOU MUST** actually use the Task tool with proper parameters
4. **IF NO TASK TOOL VISIBLE** = YOU FAILED

### Quick Start for New Sessions
1. **Development tasks auto-trigger orchestrator** (implement, create, fix, etc.)
2. Say "Status" to check workflow progress
3. Orchestrator manages all phases with mandatory human reviews

### ⚠️ CRITICAL: Human Review Points
The orchestrator MUST pause and wait for explicit approval:
- **After Requirements Phase**: Review requirements before design
- **After First Vertical Slice**: Review implementation before full rollout

### Available Sub-Agents
All agents located in `/.claude/agents/`:
- **orchestrator**: Master workflow coordinator (auto-invoked for complex tasks)
- **librarian**: Documentation and file organization
- **git-manager**: Version control operations
- **business-requirements**: Requirements analysis
- **blazor-developer**: Blazor Server component development
- Additional agents being added for full workflow support

### Workflow Process
**Phases**: Requirements → Design → Implementation → Testing → Finalization
**Quality Gates**: Configurable by work type (Feature/Bug/Hotfix/Docs/Refactor)
**Human Reviews**: After requirements, after first vertical slice
**Details**: See `/docs/functional-areas/ai-workflow-orchstration/`


> 📚 **DOCUMENTATION STRUCTURE** 📚
> 
> **Essential Documentation:**
> - **Navigation Guide**: [/docs/00-START-HERE.md](./docs/00-START-HERE.md)
> - **Project Status**: [PROGRESS.md](./PROGRESS.md)
> - **Architecture**: [ARCHITECTURE.md](./ARCHITECTURE.md)
> - **Docker Guide**: [DOCKER_DEV_GUIDE.md](./DOCKER_DEV_GUIDE.md)
> - **Standards**: [/docs/standards-processes/](./docs/standards-processes/)
> - **Features**: [/docs/functional-areas/](./docs/functional-areas/)

## 🚨 CRITICAL ARCHITECTURE WARNINGS

### 1. Web+API Microservices Architecture
- **Web Service** (Blazor Server): UI/Auth at http://localhost:5651
- **API Service** (Minimal API): Business logic at http://localhost:5653  
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: Web → HTTP → API → Database (NEVER Web → Database directly)

### 2. Pure Blazor Server - NO Razor Pages
**NEVER CREATE:**
- ❌ `.cshtml` files (NO Razor Pages)
- ❌ `AddRazorPages()` or `MapRazorPages()`
- ❌ Hybrid Razor Pages + Blazor

**ALWAYS USE:**
- ✅ `.razor` files only
- ✅ `@rendermode="InteractiveServer"` for interactive pages
- ✅ `App.razor` as root HTML document

### 3. Authentication Pattern
- ❌ **NEVER** use SignInManager directly in Blazor components
- ✅ **ALWAYS** use API endpoints: `/auth/login`, `/auth/logout`, `/auth/register`
- ✅ **Pattern**: Blazor → API endpoints → SignInManager → Cookies

### 4. E2E Testing - Playwright ONLY
- ✅ **Location**: `/tests/playwright/`
- ✅ **Run**: `npm run test:e2e:playwright`
- ❌ **NO Puppeteer**: All 180 tests migrated to Playwright

### 5. Docker Development Build
```bash
# ❌ WRONG - Will fail:
docker-compose up

# ✅ CORRECT - Development build:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
# OR
./dev.sh
```

## 📁 MANDATORY FILE TRACKING

### ALL Files Created/Modified/Deleted MUST Be Logged!

**CRITICAL**: Every file you create, modify, or delete MUST be logged in the [File Registry](/docs/architecture/file-registry.md).

**Why**: Too many orphaned files are cluttering the project with no clear ownership or purpose.

**Required for EVERY file operation**:
```markdown
| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| YYYY-MM-DD | /full/path/to/file | CREATED/MODIFIED/DELETED | Why this file exists | Task description | ACTIVE/ARCHIVED/TEMPORARY | When to review |
```

**File Creation Rules**:
1. ❌ **NEVER** create files in the root directory
2. ✅ **ALWAYS** log in `/docs/architecture/file-registry.md`
3. ✅ **USE** `/session-work/YYYY-MM-DD/` for temporary files
4. ✅ **NAME** files descriptively: `authentication-analysis-2025-01-20.md` NOT `status.md`
5. ✅ **REVIEW** and clean up files at session end

**End of Session Checklist**:
- [ ] Review all files created/modified
- [ ] Update file registry with final status
- [ ] Delete temporary files or move to `/session-work/`
- [ ] Commit permanent files to proper locations

## 🔑 Test Accounts
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Guest/Attendee**: guest@witchcityrope.com / Test123!

## 🛠️ Claude-Specific Configuration

### Environment
- **OS**: Ubuntu 24.04 (Native Linux - NOT WSL)
- **Project Path**: `/home/chad/repos/witchcityrope`
- **MCP Servers**: `/home/chad/mcp-servers/`
- **GitHub**: https://github.com/DarkMonkDev/WitchCityRope.git

### Available Tools
1. **TodoWrite**: Track multi-step tasks
2. **Context7**: Add "use context7" for current library docs
3. **Docker MCP**: Container management - use the Docker CLI first 
4. **FileSystem MCP**: File operations in allowed paths
5. **GitHub MCP**: Issues, PRs, repository management - use the GitHub CLI first
6. **Memory MCP**: Store project knowledge

### Quick Commands
```bash
# Start development
./dev.sh

# Restart when hot reload fails
./restart-web.sh

# Run tests
dotnet test tests/WitchCityRope.Core.Tests/
npm run test:e2e:playwright

# Health check before integration tests
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
```

## ⚠️ Common Pitfalls
1. **CSS in Razor**: Use `@@media` and `@@keyframes` (double @)
2. **DateTime**: Always use UTC for PostgreSQL
3. **Test Data**: Use GUIDs for uniqueness
4. **Hot Reload**: Often fails - use `./restart-web.sh`
5. **Syncfusion Only**: NO MudBlazor or other UI frameworks

## 📋 Session Checklist
- [ ] Read [/docs/00-START-HERE.md](./docs/00-START-HERE.md)
- [ ] Check [PROGRESS.md](./PROGRESS.md) for current status
- [ ] **LOG ALL FILES** in [/docs/architecture/file-registry.md](./docs/architecture/file-registry.md)
- [ ] Use `./dev.sh` for Docker operations
- [ ] Use TodoWrite for multi-step tasks
- [ ] Add "use context7" for library documentation
- [ ] Follow pure Blazor Server patterns
- [ ] Run E2E tests with Playwright only
- [ ] **CLEANUP FILES** at session end per file registry

## Project Overview
WitchCityRope is a Blazor Server application for a rope bondage community in Salem, offering workshops, performances, and social events.
