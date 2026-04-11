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
- **DataFactory README** - `/home/chad/repos/witchcityrope/tests/lib/datafactory/README.md` - E2E test data creation infrastructure
- **Skills Usage Guide** - `/.claude/skills/HOW-TO-USE-SKILLS.md` - Complete guide on when/how to use skills
- **Agent Boundaries** - `/home/chad/repos/witchcityrope/docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Test Execution Guide FIRST** - How to run tests
- [ ] Verify Docker containers running:
  - **For running ANY tests**: use `run-test-suite` skill (unified entry point — .NET via host `dotnet test`, E2E delegated to `test-environment`)
  - For standalone E2E runs: use `test-environment` skill directly (same underlying flow as `run-test-suite --mode e2e`)
  - For test container issues only (no test run): use `restart-test-containers` skill
  - For dev container issues only: use `restart-dev-containers` skill
- [ ] Check correct ports: API=5655, Web=5173, DB=5433
- [ ] Review Current Test Status for known failures
- [ ] Check Test Catalog for test locations
- [ ] **NEVER** run test-runner commands (.NET, Node, or Playwright CLIs) directly — a pre-commit hook blocks them and they produce unreliable results

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

**Problem Discovered**: Running Playwright from `/tests/` subdirectory caused only 8 of 855 tests to execute.

**When**: 2025-11-24 (test structure simplification)

**Symptoms**: Tests report "All passing" but 99% silently skipped

**Solution**: Use `run-test-suite` or `test-environment` skill which handle correct working directory automatically. For manual execution details, see Test Execution Guide, section "E2E Tests (Playwright)" → "Working directory (CRITICAL)"

**Prevention**: Always use `run-test-suite` (default) or `test-environment` (E2E-only) skill instead of running test commands directly. A pre-commit hook now enforces this — direct test-runner invocations are blocked at the Bash tool level.

---

## Prevention Pattern: Dev Containers vs Test Containers

**Problem Discovered**: Tests interfering with dev environment, port conflicts between environments.

**When**: test-environment skill creation (2025-11-27)

**Symptoms**: Dev work broken when tests running, port already in use errors

**Solution**: See Test Execution Guide, section "Docker Container Management" → "Dev Containers vs Test Containers"

**Critical Knowledge**:
- **Dev containers**: `witchcityrope-dev` project, used during development
- **Test containers**: `witchcityrope-test` project, isolated for test execution (used for E2E only — .NET tests use per-test TestContainers via the `run-test-suite` skill)
- Both can run simultaneously (different projects)
- Use `run-test-suite` skill for unified test execution; `test-environment` for direct E2E container management

---

## Prevention Pattern: Skipping Health Checks

**Problem Discovered**: Integration tests failing with mysterious connection errors, spending hours debugging.

**When**: Multiple integration test sessions

**Symptoms**: "Connection refused", "404 Not Found", tests hanging

**Root Cause**: Infrastructure not ready (containers unhealthy, wrong ports, API not compiled)

**Solution**: See Test Execution Guide, section "Pre-Flight Checks (MANDATORY)"

**Mandatory Procedure**:
1. FIRST: Run health checks via the skill: `bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "Category=HealthCheck"`
2. ONLY if pass: Run integration tests via `bash .claude/skills/run-test-suite/execute.sh --mode unit`

**Note**: The old direct-command form `dotnet test --filter "Category=HealthCheck"` is now blocked by a pre-commit hook and will fail. Always go through the skill.

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
- **Isolated .NET tests**: Use `run-test-suite --mode unit` — each test project spins up its own per-run postgres via TestContainers
- **Isolated E2E tests**: Use `run-test-suite --mode e2e` or `test-environment --mode e2e` (fresh container-based test database each run)
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
- Container networking issues (verify `run-test-suite` / `test-environment` skill usage)
- Shared-state pollution between test classes (see "WAF entry point exited" pattern — some classes only fail when run after other classes in the full suite; check with `run-test-suite --mode unit --filter "FullyQualifiedName~YourClass"` in isolation)

---

## Prevention Pattern: Playwright Report Not Generated

**Problem Discovered**: Test execution completes but no HTML report generated.

**When**: Playwright execution sessions

**Symptoms**: `test-results/` directory empty or missing HTML files

**Solution**: Use `run-test-suite --mode e2e` or `test-environment` skill directly — both include report generation. For viewing reports manually, see Test Execution Guide, section "Viewing Playwright Reports"

**Auto-opens**: Browser with detailed test results, screenshots, traces

---

## Prevention Pattern: Chrome DevTools MCP Available

**Problem Discovered**: Agents unaware of Chrome DevTools MCP for debugging test failures.

**When**: MCP setup (2025-10-03)

**Knowledge**: MCP provides performance tracing, browser automation, screenshot capture, network monitoring

**Solution**: See Test Execution Guide, section "Chrome DevTools MCP"

**Use for**: Debugging failed E2E tests, inspecting page state, viewing console errors, monitoring API calls

---

## Prevention Pattern: Test Skills Not Used

**Problem Discovered**: Tests run in dev environment causing interference, no isolation. Agents running test-runner commands directly and getting bogus results.

**When**: Manual test execution

**Symptoms**: Dev work disrupted during testing, port conflicts, shared database state, tests passing/failing inconsistently with dev environment state

**Solution**: See Test Execution Guide, section "Test Execution Methods" → "Method 1: Using Skills (RECOMMENDED)"

**Benefits of the test skills**:
- `run-test-suite` handles .NET + E2E unified; `test-environment` handles E2E standalone
- Complete isolation from dev
- Fresh database state (per-test-project TestContainers for .NET, fresh container stack for E2E)
- No interference with development
- Automatic cleanup
- Safety nets for silent failures (detects `dotnet test` exiting 0 with zero counters, detects compile errors in output)

**Enforcement**: A pre-commit hook (`.claude/hooks/block-manual-test-runs.py`) blocks direct test-runner invocations at the Bash tool level — this covers the .NET CLI's test command, Node package scripts, Playwright's CLI, and the standard JS test runners (Vitest, Jest, Mocha, Karma). The hook tokenizes via shlex to detect runners at command position, so substring matches in quoted strings and heredocs are not false-positived. If you try to run them, the hook returns a block response with a message pointing at the skills. Use the skills.

---

## Prevention Pattern: "Entry point exited without ever building an IHost" (shared-state pollution)

**Problem Discovered**: Integration tests in `[Collection("Sequential")]` fail with:
```
System.InvalidOperationException : The entry point exited without ever building an IHost.
   at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory.CreateClient()
```
...but only when run as part of the full suite. The same test classes pass cleanly when run in isolation via `run-test-suite --mode unit --filter "FullyQualifiedName~YourClass"`.

**When**: 2026-04-10 investigation in WCR — affected `AdminAssignmentEndpointTests` (8 tests), `AuthorizedContactEndpointTests` (16), `ProxyRsvpEndpointTests` (11), `MultiTicketCheckoutEndpointTests` (1) at various points. Full suite run showed 24–36 failures; isolation showed 0 failures.

**Symptoms**: `"The entry point exited without ever building an IHost"` at `WebApplicationFactory.CreateClient()` in the shared-`_sharedFactory` pattern used by `TicketAssignment/*EndpointTests.cs`.

**Root cause**: Shared-state pollution between test classes. Some earlier test in the sequential collection corrupts global process state (likely a singleton or static field somewhere in the API DI container) in a way that prevents a subsequent `WebApplicationFactory` from booting a fresh IHost. The `private static WebApplicationFactory<Program>? _sharedFactory` pattern caches the corrupted state for the class's entire lifetime.

**Status**: UNRESOLVED. During the 2026-04-10 investigation, three root-cause theories were tried and ALL wrong:
1. `HostAbortedException` exception filter — `WebApplicationFactory` in .NET 10 uses `DeferredHostBuilder` + `TaskCompletionSource`, not exception throwing
2. `Serilog.CreateBootstrapLogger()` → `CreateLogger()` — per issue serilog/serilog-aspnetcore#289, which applies only to parallel test runs (our tests run sequentially)
3. Respawn 7.0.0 upgrade — correlation was coincidental timing drift

**Diagnostic procedure** when you see this error:
1. Confirm it's the shared-state pattern by running the failing class in isolation:
   ```bash
   bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "FullyQualifiedName~FailingClassName"
   ```
2. If it passes in isolation but fails in the full suite, it's this bug. Do NOT try to "fix" the test class — the code is fine.
3. Report the symptom and move on. The real fix requires identifying which earlier test corrupts which global state, and that investigation hasn't been done yet.

**What does NOT work** (don't bother trying again):
- Adding exception filters to the try/catch around `app.Run()` in `Program.cs`
- Switching Serilog to `CreateLogger()` (unless tests are actually running in parallel)
- Upgrading or downgrading Respawn
- Restarting test containers
- Rebuilding

See commit `e1c0dd8e` in WCR for the full investigation notes. Future attempts at this bug should start with: "which earlier test is corrupting which global state?" rather than assuming it's a framework/library issue.

---

**REMEMBER**: This file documents EXECUTION PROBLEMS. For procedures and solutions, see Test Execution Guide and Test Creation Guide.
