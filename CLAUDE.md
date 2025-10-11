# Claude Code Project Configuration - WitchCityRope

### 🚨 CRITICAL: DATE VERIFICATION REQUIRED 🚨
**ALWAYS CHECK TODAY'S DATE**: The system provides today's date in the <env> section.
**USE THIS DATE FOR ALL DOCUMENTATION**: Files, commit messages, and documentation entries.
**COMMON ERROR**: Using January instead of the actual month. ALWAYS verify!
**Example**: If env says "Today's date: 2025-09-11", use September 11, 2025, NOT January 11, 2025!

### 🚨 CRITICAL: DOCUMENTATION STRUCTURE ENFORCEMENT 🚨

**NEVER CREATE FILES OR FOLDERS IN /docs/ ROOT**

**ZERO FILES ALLOWED IN /docs/ ROOT**

All navigation now uses:
- **Functional Areas**: `/docs/architecture/functional-area-master-index.md` 
- **Agent Guides**: `/.claude/agents/` and `/docs/lessons-learned/`
- **Standards**: `/docs/standards-processes/`

### 🚨 CRITICAL: DOCUMENTATION ORGANIZATION STANDARD 🚨

**MANDATORY**: Follow `/docs/standards-processes/documentation-organization-standard.md`

**KEY PRINCIPLES**:
1. **Cross-cutting features organized by PRIMARY BUSINESS DOMAIN**
2. **Events is a DOMAIN, dashboard/admin/public are UI CONTEXTS**
3. **Use subfolders for different UI contexts of same domain**
4. **NEVER create separate functional area folders for UI contexts of existing domains**

**CORRECT STRUCTURE**: `/docs/functional-areas/events/[context]/`
**WRONG STRUCTURE**: `/docs/functional-areas/user-dashboard/events/`

**ALL OTHER FILES MUST GO IN:**
- `/docs/functional-areas/[area]/` - For feature work
- `/docs/guides-setup/` - For guides and setup docs
- `/docs/lessons-learned/` - For lessons learned
- `/docs/standards-processes/` - For standards
- `/docs/architecture/` - For architecture decisions
- `/docs/design/` - For design documents
- `/docs/_archive/` - For archived content

**VIOLATIONS WILL BE IMMEDIATELY DETECTED AND REVERSED**

**MANDATORY PRE-FLIGHT CHECKLIST:**
- [ ] Check functional-area-master-index.md for proper location
- [ ] Verify NOT creating in /docs/ root
- [ ] Use existing functional area structure
- [ ] Update file registry for all operations
- [ ] Run structure validator: `bash /docs/architecture/docs-structure-validator.sh`

## 🤖 AI Workflow Orchestration Active

**Project**: WitchCityRope - A membership and event management platform for Salem's rope bondage community.
**Technology Stack**: React + TypeScript + Vite (migrated from Blazor Server)

### ⚡ WORKFLOW ORCHESTRATION
**Complex development requests should trigger orchestration workflow.**
For multi-step tasks, use the `/orchestrator` command to coordinate work.

### 🚨 TRIGGER WORD DETECTION - CHECK FIRST! 🚨
**BEFORE ANY ACTION, check for these triggers:**
- **continue** (ANY work/development context) → Use `/orchestrator` command
- **test/testing/debug/fix** → Delegate to test-executor agent via Task tool
- **implement/create/build/develop** → Use `/orchestrator` command for complex work
- **complete/finish/finalize** → Use `/orchestrator` command to coordinate
- **Multi-step tasks** → Use `/orchestrator` command

**IMPORTANT**: The main agent IS the orchestrator. For complex workflows:
- Use `/orchestrator` command (NOT Task tool with orchestrator type)
- For single-agent work, use Task tool directly with appropriate agent

### ⚠️ CRITICAL: PROPER DELEGATION REQUIRED ⚠️
**When delegating work:**
1. **Complex multi-agent work** → Use `/orchestrator` command
2. **Single agent work** → Use Task tool with specific agent type
3. **YOU MUST** actually invoke the proper command or tool
4. **IF NO COMMAND/TOOL VISIBLE** = Delegation failed

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
- **librarian**: Documentation and file organization
- **git-manager**: Version control operations
- **business-requirements**: Requirements analysis
- **react-developer**: React + TypeScript component development
- **backend-developer**: API and backend services development
- **test-developer**: Test suite creation
- **test-executor**: Test execution and reporting
- **ui-designer**: UI/UX design and wireframes
- **database-designer**: Database schema and architecture
- Additional agents being added for full workflow support

**NOTE**: Main agent coordinates complex workflows. Use `/orchestrator` command for multi-agent coordination.

### Workflow Process
**Phases**: Requirements → Design → Implementation → Testing → Finalization
**Quality Gates**: Configurable by work type (Feature/Bug/Hotfix/Docs/Refactor)
**Human Reviews**: After requirements, after first vertical slice
**Details**: See `/docs/functional-areas/ai-workflow-orchstration/`

### 🚨 MANDATORY: Agent Handoff Documentation System
**CRITICAL**: All agents MUST create and read handoff documents between phases.

**Handoff Process Document**: `/docs/standards-processes/workflow-orchestration-process.md`
**Handoff Template**: `/docs/standards-processes/agent-handoff-template.md`

**Orchestrator Enforcement**:
1. **VERIFY** handoff documents exist before phase transitions
2. **REQUIRE** agents to read handoffs in delegation prompts
3. **CHECK** `/docs/functional-areas/[feature]/handoffs/` for documents
4. **BLOCK** workflow if handoffs are missing

**All 15 agents have mandatory handoff instructions** in their lessons learned files.
Failure to create/read handoffs = implementation failures.


> 📚 **DOCUMENTATION STRUCTURE** 📚
> 
> **Essential Documentation:**
> - **Navigation Guide**: [/docs/architecture/functional-area-master-index.md](./docs/architecture/functional-area-master-index.md)
> - **Project Status**: [PROGRESS.md](./PROGRESS.md)
> - **Architecture**: [ARCHITECTURE.md](./ARCHITECTURE.md)
> - **Docker Guide**: [DOCKER_DEV_GUIDE.md](./DOCKER_DEV_GUIDE.md)
> - **Standards**: [/docs/standards-processes/](./docs/standards-processes/)
> - **Features**: [/docs/functional-areas/](./docs/functional-areas/)

## 🚨 CRITICAL ARCHITECTURE WARNINGS

### 1. 🐳 DOCKER-ONLY DEVELOPMENT ENVIRONMENT
**📍 MUST READ**: [DOCKER_ONLY_DEVELOPMENT.md](./DOCKER_ONLY_DEVELOPMENT.md)

- **Local dev servers are DISABLED** to prevent confusion
- **Only Docker containers allowed** for development
- **npm run dev WILL FAIL** - use `./dev.sh` instead
- **Tests ONLY run against Docker** - fail if containers not running

### 2. Web+API Microservices Architecture
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173 (Docker only)
- **API Service** (Minimal API): Business logic at http://localhost:5655 (Docker only)
- **Database** (PostgreSQL): localhost:5433 (Docker only)
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### 3. 🚨 DTO ALIGNMENT STRATEGY - CRITICAL FOR ALL DEVELOPERS
- **📍 MUST READ**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **API DTOs ARE SOURCE OF TRUTH** - Never create manual TypeScript interfaces for API data
- **TypeScript Interfaces MUST Match C# DTOs** - Use NSwag type generation
- **React Developers**: Use generated types from `@witchcityrope/shared-types` package
- **Backend Developers**: Any DTO changes require frontend type regeneration
- **VIOLATION = BROKEN BUILDS** - Manual interfaces will conflict with generated types

### 4. Pure React with TypeScript - Component Best Practices
**ALWAYS USE:**
- ✅ `.tsx` files for React components
- ✅ TypeScript for type safety
- ✅ Functional components with hooks
- ✅ React Router for navigation
- ✅ Strict component prop typing

**NEVER CREATE:**
- ❌ Class components (use functional components)
- ❌ Direct DOM manipulation (use React refs when needed)
- ❌ Inline event handlers for complex logic

### 5. Authentication Pattern
- ❌ **NEVER** store auth tokens in localStorage (XSS risk)
- ✅ **ALWAYS** use httpOnly cookies via API endpoints: `/auth/login`, `/auth/logout`, `/auth/register`
- ✅ **Pattern**: React → API endpoints → Cookie-based auth
- ✅ **Use** React Context for auth state management

### 6. E2E Testing - Playwright ONLY
- ✅ **Location**: `/tests/playwright/`
- ✅ **Run**: `npm run test:e2e:playwright`
- ❌ **NO Puppeteer**: All tests use Playwright

### 7. Docker Development Build
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
- **Project Path**: `/home/chad/repos/witchcityrope` ⚠️ **CRITICAL: NOT witchcityrope-react**
- **MCP Servers**: `/home/chad/mcp-servers/`
- **GitHub**: https://github.com/DarkMonkDev/WitchCityRope.git

### 🚨 CRITICAL: Test Results Standardization
**MANDATORY**: All test outputs MUST use standardized paths and folders.

**SINGLE TEST RESULTS FOLDER**: `/test-results/`
- ✅ **ONLY location** for ALL test artifacts (Playwright, Jest, screenshots, reports)
- ✅ **Configured** in `playwright.config.ts` and all test files
- ❌ **NEVER** use `/playwright-results/` or `/playwright-report/` (removed)
- ❌ **NEVER** hardcode absolute paths in test files (use relative: `./test-results/`)

**Path Standards for Tests**:
- ✅ **Screenshots**: `./test-results/[descriptive-name].png`
- ✅ **JSON Reports**: `./test-results/test-results.json`
- ✅ **HTML Reports**: `./test-results/html-report/`
- ✅ **Markdown Reports**: `./test-results/[feature]-report.md`

**CRITICAL PATH RULE**:
- Project path is `/home/chad/repos/witchcityrope` (NO `-react` suffix)
- Using old path `/home/chad/repos/witchcityrope-react` = WRONG COMPUTER
- All agents and tests MUST use current project path only

### Available Tools
1. **TodoWrite**: Track multi-step tasks
2. **Context7**: Add "use context7" for current library docs
3. **Chrome DevTools MCP**: Debug web pages, performance analysis, browser control for testing
4. **Docker MCP**: Container management - use the Docker CLI first
5. **FileSystem MCP**: File operations in allowed paths
6. **GitHub MCP**: Issues, PRs, repository management - use the GitHub CLI first
7. **Memory MCP**: Store project knowledge

### Quick Commands
```bash
# Start development
./dev.sh

# Start React dev server only
npm run dev

# Build React app
npm run build

# Run tests
npm run test                    # React unit tests
npm run test:e2e:playwright    # E2E tests
dotnet test tests/WitchCityRope.Core.Tests/     # API tests

# Health check before integration tests
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
```

## ⚠️ Common Pitfalls
1. **React State**: Never mutate state directly - always use setState or state setters
2. **useEffect Dependencies**: Always include all dependencies in dependency array
3. **DateTime**: Always use UTC for PostgreSQL and format consistently
4. **Test Data**: Use GUIDs for uniqueness in tests
5. **Hot Reload**: Vite dev server auto-restarts on file changes
6. **Props vs State**: Use props for data from parent, state for component-local data
7. **Key Prop**: Always provide unique keys for list items
8. **Event Handlers**: Use useCallback for functions passed as props to prevent re-renders

## 📋 Session Checklist
- [ ] Check [Functional Area Master Index](./docs/architecture/functional-area-master-index.md) for navigation
- [ ] Check [PROGRESS.md](./PROGRESS.md) for current status
- [ ] **LOG ALL FILES** in [/docs/architecture/file-registry.md](./docs/architecture/file-registry.md)
- [ ] Use `./dev.sh` for Docker operations
- [ ] Use TodoWrite for multi-step tasks
- [ ] Add "use context7" for library documentation
- [ ] Follow React + TypeScript best practices
- [ ] Use functional components with hooks
- [ ] Run E2E tests with Playwright only
- [ ] **CLEANUP FILES** at session end per file registry

## Project Overview
WitchCityRope is a React + TypeScript application (migrated from Blazor Server) for a rope bondage community in Salem, offering workshops, performances, and social events. The frontend uses Vite for development and build tooling, with a .NET Minimal API backend.
