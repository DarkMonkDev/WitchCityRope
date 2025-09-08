# Project Continuation Guide
<!-- Last Updated: 2025-09-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Quick Start for New Sessions

This guide helps new Claude Code agents quickly get oriented and continue development work on the WitchCityRope project.

## 🚀 Immediate Verification Steps

### 1. Verify System is Operational (< 2 minutes)

```bash
# Change to project directory
cd /home/chad/repos/witchcityrope-react

# Start all services
./dev.sh

# Verify services are running
curl -f http://localhost:5653/api/health  # API health check
curl -f http://localhost:5174             # React dev server
```

**Expected Results:**
- API health check returns `200 OK` with database status
- React dev server responds with HTML content
- No error messages in terminal output

### 2. Quick Functionality Test (< 1 minute)

```bash
# Run critical path test
npm run test:e2e:playwright -- --grep "login and access dashboard"
```

**Expected Result**: Test passes with green checkmark, confirming authentication flow works.

### 3. Verify Database Connection (< 30 seconds)

```bash
# Check PostgreSQL is responding
docker exec witchcityrope-postgres pg_isready -U postgres
```

**Expected Result**: `accepting connections` message.

## 📚 Essential Documentation to Read First

### MUST READ (In Order):
1. **`/SESSION-HANDOFF-2025-09-08.md`** - Complete session status and achievements
2. **`/PROGRESS.md`** - Current project status and migration progress
3. **`/docs/architecture/functional-area-master-index.md`** - File navigation guide
4. **`/CLAUDE.md`** - AI workflow configuration and agent instructions

### Important References:
5. **`/docs/lessons-learned/librarian-lessons-learned.md`** - Critical file management patterns
6. **`/docs/standards-processes/documentation-process/DOCUMENTATION_GUIDE.md`** - Documentation standards

## 🗺️ Navigation Map

### Key File Locations:
```
/home/chad/repos/witchcityrope-react/
├── SESSION-HANDOFF-2025-09-08.md      # TODAY'S HANDOFF - READ FIRST
├── PROGRESS.md                        # Project status
├── CLAUDE.md                          # AI agent configuration
├── ARCHITECTURE.md                    # System overview
├── ./dev.sh                           # Start all services script
├── apps/web/                          # React application
│   ├── src/                          # React source code
│   └── tests/playwright/             # E2E tests
├── src/WitchCityRope.Api/             # .NET API
└── docs/                              # All documentation
    ├── architecture/functional-area-master-index.md  # FILE FINDER
    ├── functional-areas/              # Feature documentation
    ├── lessons-learned/               # Agent-specific knowledge
    └── standards-processes/           # Development standards
```

## 🤖 AI Workflow Integration

### Trigger Commands for Complex Work:
- **`/orchestrator`** - For multi-phase development tasks
- **`continue [description]`** - Triggers orchestration workflow
- **`implement [feature]`** - Triggers full feature development
- **`test [component]`** - Delegates to test-executor agent

### Single Agent Tasks:
- Use **Task tool** with specific agent type for focused work
- Available agents: librarian, git-manager, business-requirements, react-developer, backend-developer, test-developer, test-executor, ui-designer, database-designer

### Human Review Points (Mandatory):
- **After Requirements Phase**: Review requirements before design
- **After First Vertical Slice**: Review implementation before full rollout

## 📝 Current Work Status

### Phase Status:
- **Phases 0-5**: ✅ COMPLETE (83% of frontend migration)
- **Current Phase**: **Phase 6 - Advanced Features & Vetting** (READY TO BEGIN)
- **Scope**: Advanced vetting workflow, admin tools, enhanced member features, community interaction

### Infrastructure Status:
- **Test Coverage**: 100% basic functionality (37 E2E test specs)
- **API Integration**: Real-time data flow operational
- **Authentication**: Production-ready JWT + httpOnly cookies
- **Performance**: API responses 9-13ms, React load 6ms
- **Mobile Experience**: Complete responsive design

### Known Issues (All Minor):
- `/api/auth/user` returns 404 but doesn't block functionality
- Some event test IDs missing (20% of event tests)
- Minor dashboard redirect timing issue

## 🚑 Emergency Protocols

### If Services Won't Start:
```bash
# Stop all containers and restart
docker-compose down
docker system prune -f
./dev.sh
```

### If Tests Are Failing:
1. Check if services are running (`./dev.sh`)
2. Verify database is initialized (API health check)
3. Run single test first: `npm run test:e2e:playwright -- --grep "basic login"`
4. Check recent commits for breaking changes

### If API Calls Fail:
1. Verify CORS configuration in `/apps/web/vite.config.ts` (port should be 5653)
2. Check API is running on http://localhost:5653
3. Test direct API call: `curl http://localhost:5653/api/health`

### If Documentation is Missing:
1. Check `/docs/architecture/functional-area-master-index.md` first
2. Search in `/docs/functional-areas/[feature]/`
3. Look in `/docs/_archive/` for historical content
4. Create new documentation following `/docs/standards-processes/documentation-process/DOCUMENTATION_GUIDE.md`

## 📋 Next Phase Planning

### Phase 6 Requirements:
- **Business Requirements**: Check `/docs/functional-areas/*/requirements/` for vetting workflow
- **Technical Design**: Advanced member management features
- **UI/UX**: Admin interfaces and moderation tools
- **Testing**: Extend current 100% coverage to new features

### Recommended Approach:
1. **Use orchestrator workflow** for Phase 6 (multi-agent coordination required)
2. **Build on existing patterns** from Phases 1-5
3. **Maintain test coverage** - add tests for all new features
4. **Follow mobile-first design** - extend current responsive patterns

## 📈 Quality Standards to Maintain

### Test Coverage:
- **Requirement**: 100% basic functionality coverage
- **Current**: 37 E2E test specs passing
- **Pattern**: Add test specs for all new features

### Performance Targets:
- **API Response**: < 50ms (currently 9-13ms)
- **React Load**: < 2s (currently 6ms)
- **Test Execution**: < 5 minutes for full suite

### Documentation Standards:
- **File Registry**: Update `/docs/architecture/file-registry.md` for ALL file operations
- **Master Index**: Update `/docs/architecture/functional-area-master-index.md` for new functional areas
- **Lessons Learned**: Update agent-specific lessons in `/docs/lessons-learned/`

## 🔧 Development Environment Details

### Working Credentials (All Tested January 9, 2025):
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Guest/Attendee**: guest@witchcityrope.com / Test123!

### Service URLs:
- **React Frontend**: http://localhost:5174
- **API Backend**: http://localhost:5653
- **Database**: PostgreSQL on localhost:5433
- **Database Name**: witchcityrope_dev

### Technology Stack (Confirmed Working):
- **Frontend**: React 18.3.1 + TypeScript + Vite 5.3.1 + Mantine v7
- **Backend**: .NET 9 Minimal API + Entity Framework Core
- **Database**: PostgreSQL 15+ in Docker
- **Testing**: Playwright E2E + Vitest unit tests
- **Authentication**: JWT + httpOnly cookies pattern

## 📞 Support and Resources

### When Stuck:
1. **Check Recent Session**: `/SESSION-HANDOFF-2025-09-08.md` has complete context
2. **Search Documentation**: Use master index to find relevant docs quickly
3. **Review Lessons Learned**: Agent-specific knowledge in `/docs/lessons-learned/`
4. **Test Existing Functionality**: Verify system works before adding new features

### For Architecture Questions:
- **ADRs**: Architecture Decision Records in `/docs/architecture/`
- **Migration Plan**: Original plan in `/docs/architecture/react-migration/`
- **Technology Decisions**: See `/docs/architecture/react-migration/adrs/`

### For Development Patterns:
- **React Components**: Follow existing patterns in `/apps/web/src/components/`
- **API Endpoints**: Follow .NET Minimal API patterns in `/src/WitchCityRope.Api/`
- **Database Changes**: Entity Framework migrations in `/src/WitchCityRope.Infrastructure/`

## ✅ Success Checklist

Before starting significant work, verify:

- [ ] All services start successfully with `./dev.sh`
- [ ] Health checks pass for API and database
- [ ] Authentication flow works (test login with any test account)
- [ ] Recent handoff documentation reviewed
- [ ] Functional area master index consulted
- [ ] File registry ready for updates
- [ ] Appropriate workflow (orchestrator vs single-agent) identified

**If all checkboxes are checked, the project is ready for continued development.**

---

*This guide ensures smooth continuation of WitchCityRope development regardless of session gaps.*