# Test Execution Guide
---
title: Test Execution Guide
scope: How to RUN tests (setup, execution, debugging)
audience: test-executor agent, developers running tests
related:
  - Test Creation Guide (how to WRITE tests)
  - Docker-Only Testing Standard (environment requirements)
  - Test Catalog (test inventory)
last_updated: 2025-12-01
version: 1.0
---

## What This Guide Covers

This guide teaches you HOW TO RUN tests for WitchCityRope:

✅ **Test execution commands** - Unit, integration, E2E test execution
✅ **Docker container setup** - Dev vs test containers, project isolation
✅ **Environment verification** - Health checks, port configuration
✅ **Skills automation** - test-environment, container-restart, database-reset
✅ **Debugging failed tests** - Logs, screenshots, network inspection
✅ **Test results management** - Playwright reports, test-results location
✅ **CI/CD execution** - GitHub Actions configuration

## What This Guide Does NOT Cover

❌ **How to WRITE tests** → See [Test Creation Guide](TEST-CREATION-GUIDE.md)
❌ **Test patterns and structure** → See [Test Creation Guide](TEST-CREATION-GUIDE.md)
❌ **Selector strategies** → See [Playwright Guide](browser-automation/playwright-guide.md)
❌ **Authentication helpers** → See [Test Creation Guide](TEST-CREATION-GUIDE.md)

## Prerequisites

Before running tests:
- [ ] Docker must be installed and running
- [ ] .NET 9.0 SDK installed
- [ ] Node.js 18+ installed (for E2E tests)
- [ ] Read [Docker-Only Testing Standard](docker-only-testing-standard.md)

---

## 🚨 CRITICAL: Docker-Only Testing Environment

**MANDATORY REQUIREMENT**: ALL testing MUST use Docker containers exclusively.

### The Rule

**NEVER run `npm run dev`** - It is DISABLED and will ERROR.
**ONLY use Docker containers** via `./dev.sh` or skills.

### Approved Testing Environment

✅ **ONLY APPROVED**:
- React App: Docker container on port 5173
- API Service: Docker container on port 5655
- PostgreSQL Database: Docker container on port 5433

❌ **FORBIDDEN**:
- Local dev servers (ports 5174, 5175, 3000)
- Mixed Docker + local dev environments
- Testing against localhost development servers
- Any `npm run dev` or `npm start` commands

**Why this matters**: Port conflicts and environment inconsistencies cause 80%+ of false test failures.

See [Docker-Only Testing Standard](docker-only-testing-standard.md) for complete details.

---

## Quick Start

### Step 1: Verify Docker Environment

**Use the `container-restart` skill** for automated verification:

```bash
# Skill handles:
# - Stopping containers
# - Starting with dev compose overlay
# - Health checks
# - Compilation verification
```

The skill ensures:
- ✅ Docker daemon is running
- ✅ Containers are healthy on correct ports
- ✅ API compiles without errors
- ✅ Frontend builds successfully

### Step 2: Run Tests

```bash
# Unit tests (fast, no dependencies)
dotnet test tests/WitchCityRope.Core.Tests/

# Integration tests (requires Docker)
dotnet test tests/WitchCityRope.IntegrationTests/

# E2E tests (requires running application)
npx playwright test

# All .NET tests
dotnet test
```

---

## Test Execution Methods

### Method 1: Using Skills (RECOMMENDED)

**For Complete Test Isolation**:

Use the **test-environment** skill to run tests in isolated containers:

```bash
# Run E2E tests in isolated test containers
Use test-environment skill

# Run all tests (unit + integration + E2E)
Use test-environment skill with --mode all

# Run specific E2E test file
Use test-environment skill with --filter "admin-events-dashboard"

# Keep containers for debugging
Use test-environment skill with --keep-containers
```

**test-environment skill benefits**:
- ✅ Complete isolation from dev environment
- ✅ Fresh database state for each run
- ✅ Prevents test interference with development
- ✅ Automatic cleanup after tests
- ✅ Works while other agents use dev containers

**How it works**:
1. Builds test containers (`test-web`, `test-api`, `test-db`)
2. Starts isolated environment with fresh database
3. Runs specified tests
4. Captures results to `/test-results/`
5. Cleans up containers (unless `--keep-containers`)

See `/.claude/skills/test-environment/README.md` for complete documentation.

**For Dev Container Testing**:

Use **container-restart** skill before running tests in dev environment:

```bash
# Restart dev containers with health verification
Use container-restart skill

# Then run tests manually
dotnet test
npx playwright test
```

### Method 2: Manual Execution

**Prerequisites**:
```bash
# 1. Verify Docker is running
docker ps

# 2. Start containers if needed
./dev.sh

# 3. Verify ports
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep witchcity
# Should show:
# witchcity-web: 0.0.0.0:5173->3000/tcp
# witchcity-api: 0.0.0.0:5655->8080/tcp
# witchcity-postgres: 0.0.0.0:5433->5432/tcp
```

**Run specific test types**:
```bash
# Unit tests only (fast)
dotnet test tests/WitchCityRope.Core.Tests/

# Integration tests only (slower, requires Docker)
dotnet test tests/WitchCityRope.IntegrationTests/

# E2E tests
npx playwright test

# Specific test file
dotnet test --filter "FullyQualifiedName~AuthenticationTests"
npx playwright test auth/login.spec.ts

# With detailed logging
dotnet test -v normal
```

---

## Docker Container Management

### Dev Containers vs Test Containers

WitchCityRope uses **TWO separate Docker environments**:

**Dev Containers** (for development):
- Project: `witchcityrope-dev` (or `witchcityrope`)
- Containers: `witchcity-web`, `witchcity-api`, `witchcity-db`
- Ports: 5173 (web), 5655 (API), 5433 (database)
- Started by: `./dev.sh` or `container-restart` skill

**Test Containers** (for test isolation):
- Project: `witchcityrope-test`
- Containers: `test-web`, `test-api`, `test-db`
- Ports: Same ports (5173, 5655, 5433)
- Started by: `test-environment` skill
- Lifecycle: Created → Run tests → Destroyed

**Project Isolation**:
```bash
# Dev containers
docker-compose -p witchcityrope-dev up -d

# Test containers
docker-compose -p witchcityrope-test up -d

# Both can exist simultaneously (different projects)
```

**When to use which**:
- **Dev containers**: Quick test runs during development
- **Test containers**: Full isolation, CI/CD, before commits

### Container Restart (Dev Environment)

**Use container-restart skill** for proper restart with verification:

```bash
# Skill handles:
# 1. docker-compose down
# 2. docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
# 3. Health check endpoints (React, API, database)
# 4. API compilation verification
# 5. Frontend build verification
```

**Manual restart** (if skill unavailable):
```bash
# Stop containers
docker-compose down

# Start with dev overlay
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Verify health
curl -f http://localhost:5173/
curl -f http://localhost:5655/health
```

### Database Reset

**Use database-reset-dev skill** for clean database state:

```bash
# Skill handles:
# 1. Deletes all data
# 2. Restarts API container
# 3. Auto-seeding triggers on startup
# 4. Verifies seeding completed
```

**Manual reset** (if skill unavailable):
```bash
# Delete all data
docker exec witchcity-db psql -U postgres -d witchcityrope -c "
  DROP SCHEMA public CASCADE;
  CREATE SCHEMA public;
"

# Restart API to trigger seeding
docker restart witchcity-api

# Verify seeding
docker logs witchcity-api | grep "Seeding"
```

---

## Pre-Flight Checks (MANDATORY)

### Before Running ANY Tests

**EVERY test execution MUST verify this checklist**:

```bash
# 1. Verify Docker containers running
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep witchcity
# MUST show all 3 containers on correct ports

# 2. Kill any rogue local dev servers
./scripts/kill-local-dev-servers.sh

# 3. Check for conflicting processes
lsof -i :5174 -i :5175 -i :3000 | grep node && echo "❌ CONFLICT" || echo "✅ OK"

# 4. Verify correct services respond
curl -f http://localhost:5173/ | grep -q "Witch City Rope" && echo "✅ React" || echo "❌ FAIL"
curl -f http://localhost:5655/health && echo "✅ API" || echo "❌ FAIL"

# 5. Final verification
echo "✅ Docker-only environment verified"
```

**Use `container-restart` skill to automate these checks.**

### Health Check Tests (MANDATORY)

**Before integration tests, ALWAYS run health checks**:

```bash
# Verify infrastructure is healthy
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# These verify:
# - React dev server (port 5173)
# - API service (port 5655)
# - PostgreSQL database (port 5433)
# - Docker containers healthy
```

**Why**: Port misconfigurations cause hours of debugging. Health checks validate infrastructure FIRST.

See [Integration Test Patterns](integration-test-patterns.md) for complete setup.

---

## Running Specific Test Types

### Unit Tests

**Characteristics**:
- Fast (< 1ms per test)
- No external dependencies
- Test business logic in isolation

```bash
# All unit tests
dotnet test tests/WitchCityRope.Core.Tests/

# With coverage
dotnet test tests/WitchCityRope.Core.Tests/ /p:CollectCoverage=true

# Watch mode (TDD)
dotnet watch test --project tests/WitchCityRope.Core.Tests/

# Specific test class
dotnet test --filter "FullyQualifiedName~EventServiceTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~CreateEvent_WithValidData_Succeeds"
```

### Integration Tests

**Characteristics**:
- Slower (< 100ms per test)
- Requires Docker (PostgreSQL containers)
- Tests database operations, API endpoints

```bash
# 1. FIRST: Run health checks (MANDATORY)
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. ONLY if health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/

# With detailed logging
dotnet test tests/WitchCityRope.IntegrationTests/ -v normal

# Specific feature tests
dotnet test --filter "FullyQualifiedName~Authentication"
```

**CRITICAL**: Never skip health checks. They prevent hours of debugging false failures.

### E2E Tests (Playwright)

**Characteristics**:
- Slowest (< 5s per test)
- Requires running application
- Tests complete user workflows

```bash
# All E2E tests
npx playwright test

# Specific file
npx playwright test auth/login.spec.ts

# Specific test
npx playwright test -g "should login with valid credentials"

# Headed mode (see browser)
npx playwright test --headed

# UI mode (interactive debugging)
npx playwright test --ui

# Specific browser
npx playwright test --project=chromium

# Parallel execution (default: 10 workers)
npx playwright test --workers=10

# Override worker count
npx playwright test --workers=5

# Update snapshots
npx playwright test --update-snapshots
```

**Working directory (CRITICAL)**:
```bash
# ✅ CORRECT - Run from project root
cd /home/chad/repos/witchcityrope/apps/web
npx playwright test

# ❌ WRONG - Running from test subdirectory will miss tests
cd /home/chad/repos/witchcityrope/apps/web/tests
npx playwright test  # Only finds tests in current directory
```

**Test structure**:
```
apps/web/
├── tests/                      # All E2E tests (flat structure)
│   ├── admin/
│   ├── auth/
│   ├── events/
│   └── vetting/
├── test-utils/                 # Shared utilities
│   ├── pages/                 # Page objects
│   ├── fixtures/              # Test fixtures
│   └── helpers/               # Helpers
└── playwright.config.ts        # Points to ./tests
```

### React Component Tests (Vitest)

```bash
# Run component tests
npm test

# Watch mode
npm test -- --watch

# Coverage
npm test -- --coverage

# Specific file
npm test Button.test.tsx
```

---

## Debugging Failed Tests

### Playwright Debugging

**UI Mode** (recommended):
```bash
# Interactive debugging with time-travel
npx playwright test --ui

# Features:
# - Step through test execution
# - Inspect DOM at each step
# - View network requests
# - Time-travel debugging
# - Live test editing
```

**Debug Mode**:
```bash
# Runs test with debugger attached
npx playwright test --debug

# Specific test
npx playwright test --debug auth/login.spec.ts
```

**Screenshots on Failure**:
```bash
# Automatic screenshots saved to /test-results/
npx playwright test

# Screenshots saved at:
# /test-results/[test-name]/[failure-screenshot].png
```

**Verbose Output**:
```bash
# See all network requests
DEBUG=pw:api npx playwright test

# See browser console logs
npx playwright test --headed
```

### .NET Test Debugging

**Detailed Logging**:
```bash
# Verbose output
dotnet test -v detailed

# Diagnostic logging
dotnet test -v diagnostic > test-output.log
```

**Debugging Specific Test**:
```bash
# Run single test with debugger
dotnet test --filter "FullyQualifiedName~SpecificTestName" -v normal
```

**Environment Variables**:
```bash
# Enable detailed logs
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug

# Run tests
dotnet test
```

### Chrome DevTools MCP

**Available for enhanced debugging**:

```bash
# MCP provides:
# - Performance tracing
# - Browser automation
# - Runtime inspection
# - Network monitoring
# - Visual debugging
```

**Use cases**:
- Inspect page state during test failures
- View console errors
- Monitor API calls
- Capture screenshots
- Analyze performance bottlenecks

See `/docs/standards-processes/MCP/MCP_SERVERS.md` for configuration.

---

## Test Results Management

### Test Results Location

**CRITICAL STANDARD**: ALL test results MUST go in `/test-results/`

```
/test-results/
├── playwright-report/          # Playwright HTML reports
├── screenshots/                # Test failure screenshots
├── test-results.json          # Playwright JSON results
└── [test-name]/               # Individual test artifacts
    ├── trace.zip              # Playwright traces
    └── failure-screenshot.png
```

**Path Standards**:
- ✅ Screenshots: `./test-results/[descriptive-name].png`
- ✅ JSON Reports: `./test-results/test-results.json`
- ✅ HTML Reports: `./test-results/html-report/`
- ✅ Markdown Reports: `./test-results/[feature]-report.md`

**NEVER use**:
- ❌ `/playwright-results/` (removed)
- ❌ `/playwright-report/` (removed)
- ❌ Hardcoded absolute paths in test files

### Viewing Playwright Reports

```bash
# Generate and open HTML report
npx playwright show-report test-results/playwright-report

# Automatically opens in browser with:
# - Test pass/fail status
# - Screenshots
# - Traces (time-travel debugging)
# - Network logs
```

### Test Catalog Updates

**After creating/modifying tests, update the test catalog**:

Use **test-catalog-updater** skill:
```bash
# Updates catalog with:
# - Test counts
# - Pass/fail status
# - Coverage metrics
# - Last execution time
```

Manual update (if skill unavailable):
- Update `/docs/standards-processes/testing/TEST_CATALOG.md`
- Log test metadata (file, purpose, status)

---

## CI/CD Integration

### GitHub Actions Configuration

```yaml
name: Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Unit Tests
        run: dotnet test tests/WitchCityRope.Core.Tests/ --logger "trx"

  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3

      - name: Run Health Checks
        run: dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

      - name: Run Integration Tests
        run: dotnet test tests/WitchCityRope.IntegrationTests/ --logger "trx"

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Playwright
        run: npx playwright install --with-deps

      - name: Start Application
        run: ./dev.sh
        timeout-minutes: 2

      - name: Run E2E Tests
        run: npx playwright test

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: test-results/
```

### Test Environment Variables

```bash
# CI/CD environment
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
export DOCKER_HOST=unix:///var/run/docker.sock
export CI=true

# Playwright in CI
export PLAYWRIGHT_BROWSERS_PATH=0  # Install to system location
```

---

## Troubleshooting

### Common Issues

**"React app not loading"**
```bash
# Check Docker container
docker ps | grep witchcity-web

# If not running, use container-restart skill
# Or manually:
./dev.sh
```

**"API endpoints returning 404"**
```bash
# Verify API container health
curl -f http://localhost:5655/health

# If failing:
docker restart witchcity-api

# Or use container-restart skill
```

**"Tests timing out"**
```bash
# Check for port conflicts
lsof -i :5173 -i :5655 | grep -v docker

# Kill conflicts
./scripts/kill-local-dev-servers.sh

# Restart containers
Use container-restart skill
```

**"Connection refused errors"**
```bash
# Restart containers with health checks
Use container-restart skill

# Verify all healthy
docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity
```

**"Database already exists" errors**
```bash
# Use unique database names
var dbName = $"Test_{Guid.NewGuid():N}";

# Or reset dev database
Use database-reset-dev skill
```

**"Flaky E2E tests"**
```bash
# Use proper wait strategies (not fixed timeouts)
await expect(page.locator('[data-testid="loaded"]')).toBeVisible();

# Not:
await page.waitForTimeout(2000);  // Flaky
```

**"Only 8 of 855 tests running"**
```bash
# ALWAYS run from project root
cd /home/chad/repos/witchcityrope/apps/web
npx playwright test

# NOT from test subdirectory
```

### Emergency Protocols

**If local dev servers detected**:
1. ❌ STOP ALL TESTING - Results will be invalid
2. 🔥 Kill processes: `./scripts/kill-local-dev-servers.sh`
3. 🐳 Verify Docker: Use `container-restart` skill
4. ✅ Re-validate: Run pre-flight checklist
5. 🔄 Restart tests

**If containers won't start**:
1. Check Docker daemon: `docker info`
2. Check disk space: `df -h`
3. Check logs: `docker logs [container]`
4. Nuclear option: `docker system prune -a --volumes`
5. Use `container-restart` skill

**If tests fail unexpectedly**:
1. Run health checks FIRST
2. Check container status: `docker ps`
3. Review logs: `docker logs witchcity-api`
4. Enable verbose logging: `dotnet test -v detailed`
5. Use Playwright UI mode: `npx playwright test --ui`

---

## Performance Guidelines

### Target Metrics

- Unit Tests: < 1ms per test
- Integration Tests: < 100ms per test
- E2E Tests: < 5s per test
- Full Suite: < 5 minutes

### Optimization Tips

✅ Run tests in parallel (Playwright default: 10 workers)
✅ Use test-environment skill for isolation
✅ Share expensive resources (database containers)
✅ Use `[Collection]` for sequential .NET tests
✅ Minimize file I/O in tests

❌ Don't run E2E tests unnecessarily (use unit tests for logic)
❌ Don't use fixed timeouts (causes delays)
❌ Don't skip health checks (causes debugging delays)

---

## Best Practices

### Pre-Execution Checklist

- [ ] Read [Docker-Only Testing Standard](docker-only-testing-standard.md)
- [ ] Verify Docker containers running
- [ ] Run pre-flight checks
- [ ] Review [Test Catalog](TEST_CATALOG.md) for existing tests
- [ ] Use appropriate skill for test type

### During Execution

- [ ] Use test-environment skill for isolation
- [ ] Run health checks before integration tests
- [ ] Monitor test results in `/test-results/`
- [ ] Update test catalog after execution

### After Execution

- [ ] Review failed tests in Playwright report
- [ ] Update test catalog with results
- [ ] Clean up test containers (if manual)
- [ ] Document any new issues discovered

---

## Additional Resources

### Documentation

- **[Test Creation Guide](TEST-CREATION-GUIDE.md)** - How to WRITE tests
- **[Docker-Only Testing Standard](docker-only-testing-standard.md)** - Environment requirements
- **[Test Catalog](TEST_CATALOG.md)** - All existing tests
- **[Playwright Guide](browser-automation/playwright-guide.md)** - E2E patterns
- **[Integration Test Patterns](integration-test-patterns.md)** - PostgreSQL testing

### Lessons Learned

- **[Test Executor Lessons](/docs/lessons-learned/test-executor-lessons-learned.md)** - Execution issues
- **[Test Developer Lessons](/docs/lessons-learned/test-developer-lessons-learned.md)** - Test creation mistakes

### Skills

- **test-environment** - Run tests in isolated containers
- **container-restart** - Restart dev containers with health checks
- **database-reset-dev** - Reset dev database with auto-seeding
- **test-catalog-updater** - Update test catalog after execution
- **phase-4-validator** - Validate testing phase completion

---

**Remember**: Proper test execution prevents hours of debugging. Use skills for automation, run health checks, and verify environment BEFORE running tests.
