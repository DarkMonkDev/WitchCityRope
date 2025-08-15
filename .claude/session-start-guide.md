# Claude Code Session Start Guide
<!-- Last Updated: 2025-08-04 -->
<!-- Purpose: Quick start guide for new Claude Code sessions -->

## Quick Session Start Checklist

When starting a new Claude Code session with the WitchCityRope project, follow these steps:

### 1. Review Documentation Structure (NEW - August 2025)
```bash
# Start with the documentation navigation guide
cat docs/00-START-HERE.md

# Check current project status
cat PROGRESS.md

# Review Claude-specific configuration
cat CLAUDE.md
```

### 2. Verify Environment
```bash
# Check Docker is running
docker ps

# Check development containers
docker-compose ps

# If containers aren't running, start them
./dev.sh  # Select option 1
```

### 3. Check Current Status
```bash
# Run health checks to verify system state
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# Check for any build errors
cd src/WitchCityRope.Web
dotnet build
```

### 4. Review Recent Changes
```bash
# Check git status for uncommitted changes
git status

# View recent commits
git log --oneline -10
```

### 5. Session-Specific Context

#### If Working on Testing
- Read test catalog: `/docs/standards-processes/testing/TEST_CATALOG.md`
- Check test status in `PROGRESS.md`
- Review lessons learned: `/docs/lessons-learned/test-writers.md`
- Run health checks before any integration tests

#### If Working on UI/Components
- Check UI developer lessons: `/docs/lessons-learned/ui-developers.md`
- Verify Blazor Server render modes
- Check Syncfusion license is configured
- Use `./restart-web.sh` for hot reload issues

#### If Working on Database
- Review infrastructure docs: `/docs/standards-processes/`
- Ensure PostgreSQL container is running
- All DateTime values must be UTC
- Check migrations are up to date

#### If Working on Authentication
- Review auth docs: `/docs/functional-areas/authentication/`
- Check current implementation: `/docs/functional-areas/authentication/current-state/`
- Remember: Web uses cookies, API uses JWT
- Restart containers after auth changes

#### If Working on New Features
- Check if feature area exists: `/docs/functional-areas/`
- Use template if creating new area: `/docs/functional-areas/_template/`
- Update status in: `/docs/functional-areas/[feature]/new-work/status.md`

### 6. Common Session Tasks

#### Start Fresh Development
```bash
# Clean start with development build
docker-compose down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Watch logs
docker-compose logs -f web
```

#### Resume Development
```bash
# Quick restart if hot reload failing
./restart-web.sh

# Full rebuild if needed
./dev.sh  # Select rebuild option
```

#### Run Tests
```bash
# Always run health checks first
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# Then run desired tests
dotnet test
```

### 7. Important Reminders

- **NEVER** use `docker-compose up` directly (use dev build)
- **NEVER** create Razor Pages (.cshtml files)
- **NEVER** use SQL Server or Playwright
- **ALWAYS** use PostgreSQL and Puppeteer
- **ALWAYS** use UTC for DateTime values
- **ALWAYS** restart containers after major changes

### 8. Getting Help

- **Documentation Navigation**: `/docs/00-START-HERE.md`
- **Architecture Questions**: `/docs/architecture/` and `ARCHITECTURE.md`
- **Testing Issues**: `/docs/standards-processes/testing/TEST_CATALOG.md`
- **Docker Problems**: `DOCKER_DEV_GUIDE.md`
- **Feature Documentation**: `/docs/functional-areas/[feature-name]/`
- **Development Standards**: `/docs/standards-processes/`
- **Lessons Learned**: `/docs/lessons-learned/[role].md`
- **Project Status**: `PROGRESS.md`
- **Claude Configuration**: `CLAUDE.md`

### 9. Session End Checklist

Before ending your session:
- [ ] Commit any changes with descriptive messages
- [ ] Update documentation if needed
- [ ] Run tests to verify nothing is broken
- [ ] Leave notes about work in progress
- [ ] Stop containers if not needed: `docker-compose down`

### 10. Documentation Quick Reference

The documentation was reorganized in August 2025 for better maintainability:

| Content Type | Location |
|--------------|----------|
| Navigation Guide | `/docs/00-START-HERE.md` |
| Project Status | `PROGRESS.md` |
| Feature Docs | `/docs/functional-areas/[feature]/` |
| Standards | `/docs/standards-processes/` |
| Lessons Learned | `/docs/lessons-learned/[role].md` |
| Architecture | `/docs/architecture/` |
| Session Handoffs | `/docs/standards-processes/session-handoffs/` |

---

**Important**: This guide is Claude-specific. Always start with `/docs/00-START-HERE.md` for comprehensive documentation navigation.