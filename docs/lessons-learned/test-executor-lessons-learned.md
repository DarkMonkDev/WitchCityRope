# Test Executor Lessons Learned
<!-- Last Updated: 2025-12-01 -->
<!-- Version: 25.0 -->
<!-- Owner: Test Team -->
<!-- Status: Active -->

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL TESTING DOCUMENTS (MUST READ): 🚨

1. **Test Execution Guide** - **HOW TO RUN TESTS (PRIMARY GUIDE)**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST-EXECUTION-GUIDE.md`

2. **Test Creation Guide** - **HOW TO WRITE TESTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST-CREATION-GUIDE.md`

3. **Docker-Only Testing Standard** - **ENVIRONMENT REQUIREMENTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/docker-only-testing-standard.md`

4. **Test Catalog** - **ALL EXISTING TESTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`

5. **Current Test Status** - **WHAT'S BROKEN/WORKING**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/CURRENT_TEST_STATUS.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/home/chad/repos/witchcityrope/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Playwright Guide** - `/home/chad/repos/witchcityrope/docs/standards-processes/testing/browser-automation/playwright-guide.md`
- **Integration Test Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/testing/integration-test-patterns.md`
- **Skills Usage Guide** - `/.claude/skills/HOW-TO-USE-SKILLS.md` - Complete guide on when/how to use skills
- **Agent Boundaries** - `/home/chad/repos/witchcityrope/docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Test Execution Guide FIRST** - How to run tests
- [ ] Verify Docker containers running:
  - For running tests: use `test-environment` skill (PREFERRED - isolated containers)
  - For test container issues only: use `restart-test-containers` skill
  - For dev container issues only: use `restart-dev-containers` skill
- [ ] Check correct ports: API=5655, Web=5173, DB=5433
- [ ] Review Current Test Status for known failures
- [ ] Check Test Catalog for test locations

## 🚨 IMPORTANT: This File Documents PROBLEMS ONLY

**ALL execution procedures, debugging, and solutions are in the guides above.**

This lessons learned file contains ONLY:
- ✅ **Problem discovery context** (when/what/symptoms)
- ✅ **References to guides** for solutions
- ❌ **NOT execution procedures** (they're in Test Execution Guide)
- ❌ **NOT test patterns** (they're in Test Creation Guide)

**When you discover a new execution issue, ADD IT TO THE GUIDES, not here.**

---

## Prevention Pattern: Running Playwright from Wrong Directory

**Problem Discovered**: Running `npx playwright test` from `/tests/` subdirectory caused only 8 of 855 tests to execute.

**When**: 2025-11-24 (test structure simplification)

**Symptoms**: Tests report "All passing" but 99% silently skipped

**Solution**: See Test Execution Guide, section "E2E Tests (Playwright)" → "Working directory (CRITICAL)"

**Critical Rule**: ALWAYS run from project root `/home/chad/repos/witchcityrope/apps/web`, NEVER from test subdirectories

**Verification**: Use test-catalog-updater skill to verify full test suite discovered (~855 tests)

---

## Prevention Pattern: Dev Containers vs Test Containers

**Problem Discovered**: Tests interfering with dev environment, port conflicts between environments.

**When**: test-environment skill creation (2025-11-27)

**Symptoms**: Dev work broken when tests running, port already in use errors

**Solution**: See Test Execution Guide, section "Docker Container Management" → "Dev Containers vs Test Containers"

**Critical Knowledge**:
- **Dev containers**: `witchcityrope-dev` project, used during development
- **Test containers**: `witchcityrope-test` project, isolated for test execution
- Both can run simultaneously (different projects)
- Use `test-environment` skill for complete isolation

---

## Prevention Pattern: Skipping Health Checks

**Problem Discovered**: Integration tests failing with mysterious connection errors, spending hours debugging.

**When**: Multiple integration test sessions

**Symptoms**: "Connection refused", "404 Not Found", tests hanging

**Root Cause**: Infrastructure not ready (containers unhealthy, wrong ports, API not compiled)

**Solution**: See Test Execution Guide, section "Pre-Flight Checks (MANDATORY)"

**Mandatory Procedure**:
1. FIRST: Run health checks `dotnet test --filter "Category=HealthCheck"`
2. ONLY if pass: Run integration tests

**Why Critical**: Health checks take 10 seconds, save hours of debugging

---

## Prevention Pattern: Local Dev Server Conflicts

**Problem Discovered**: Tests failing with connection errors when local dev servers running on wrong ports.

**When**: Multiple test execution sessions (2025-08-18 to 2025-11-24)

**Symptoms**: Tests connect to port 5174/5175 instead of Docker port 5173

**Solution**: See Test Execution Guide, section "Pre-Flight Checks (MANDATORY)"

**Emergency Procedure**:
1. Kill rogue processes: `./scripts/kill-local-dev-servers.sh`
2. Verify Docker containers:
   - For test environment: Use `restart-test-containers` skill
   - For dev environment: Use `restart-dev-containers` skill
3. Confirm ports: `docker ps --format "table {{.Names}}\t{{.Ports}}"`

---

## Prevention Pattern: Database State Interference

**Problem Discovered**: Tests failing because previous test runs left stale data.

**When**: Integration and E2E test execution

**Symptoms**: "Duplicate key", unexpected data in results, tests pass individually but fail together

**Solution**: See Test Execution Guide, section "Docker Container Management" → "Database Reset"

**Options**:
- **Isolated tests**: Use `test-environment` skill (fresh database each run)
- **Dev environment**: Use `database-reset-dev` skill (deletes all data, reseeds)

---

## Prevention Pattern: Test Results Not in Correct Location

**Problem Discovered**: Test results scattered across multiple directories, hard to find.

**When**: Playwright execution sessions

**Symptoms**: Results in `/playwright-results/`, `/playwright-report/`, various locations

**Solution**: See Test Execution Guide, section "Test Results Management"

**Standard Location**: ALL results in `/test-results/` directory

**Enforcement**: Playwright config uses `./test-results/` for all outputs

---

## Prevention Pattern: Missing Test Catalog Updates

**Problem Discovered**: Test catalog out of sync with actual tests, causing duplicate test creation.

**When**: After test creation sessions

**Symptoms**: Test developers recreate existing tests, catalog shows stale information

**Solution**: See Test Execution Guide, section "Test Catalog Updates"

**Mandatory**: Use `test-catalog-updater` skill after EVERY test execution session

**Skill updates**:
- Test counts
- Pass/fail status
- Coverage metrics
- Last execution time

---

## Prevention Pattern: Container Won't Start

**Problem Discovered**: Docker containers failing to start, preventing all tests.

**When**: After system updates, Docker upgrades, disk space issues

**Symptoms**: `docker-compose up` fails, containers immediately exit

**Solution**: See Test Execution Guide, section "Troubleshooting" → "If containers won't start"

**Diagnostic Steps**:
1. Check Docker daemon: `docker info`
2. Check disk space: `df -h`
3. Check logs: `docker logs [container]`
4. Nuclear option: `docker system prune -a --volumes`
5. Restart containers:
   - For test environment: Use `restart-test-containers` skill
   - For dev environment: Use `restart-dev-containers` skill

---

## Prevention Pattern: Flaky Tests in CI/CD

**Problem Discovered**: Tests pass locally, fail randomly in CI/CD pipeline.

**When**: GitHub Actions execution

**Symptoms**: Intermittent failures, timing issues, resource constraints

**Solution**: See Test Execution Guide, section "CI/CD Integration"

**Common Causes**:
- Fixed timeouts (use condition-based waits)
- Resource constraints (reduce parallel workers)
- Environment variables not set (check GitHub Actions config)
- Container networking issues (verify test-environment skill usage)

---

## Prevention Pattern: Playwright Report Not Generated

**Problem Discovered**: Test execution completes but no HTML report generated.

**When**: Playwright execution sessions

**Symptoms**: `test-results/` directory empty or missing HTML files

**Solution**: See Test Execution Guide, section "Viewing Playwright Reports"

**Command**: `npx playwright show-report test-results/playwright-report`

**Auto-opens**: Browser with detailed test results, screenshots, traces

---

## Prevention Pattern: Chrome DevTools MCP Available

**Problem Discovered**: Agents unaware of Chrome DevTools MCP for debugging test failures.

**When**: MCP setup (2025-10-03)

**Knowledge**: MCP provides performance tracing, browser automation, screenshot capture, network monitoring

**Solution**: See Test Execution Guide, section "Chrome DevTools MCP"

**Use for**: Debugging failed E2E tests, inspecting page state, viewing console errors, monitoring API calls

---

## Prevention Pattern: Test Environment Skill Not Used

**Problem Discovered**: Tests run in dev environment causing interference, no isolation.

**When**: Manual test execution

**Symptoms**: Dev work disrupted during testing, port conflicts, shared database state

**Solution**: See Test Execution Guide, section "Test Execution Methods" → "Method 1: Using Skills (RECOMMENDED)"

**Benefits of test-environment skill**:
- Complete isolation from dev
- Fresh database state
- No interference with development
- Automatic cleanup

---

**REMEMBER**: This file documents EXECUTION PROBLEMS. For procedures and solutions, see Test Execution Guide and Test Creation Guide.
