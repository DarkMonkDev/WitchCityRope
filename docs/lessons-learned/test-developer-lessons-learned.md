# Test Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL TESTING DOCUMENTS (MUST READ): 🚨

1. **Test Creation Guide** - **HOW TO WRITE TESTS (PRIMARY GUIDE)**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST-CREATION-GUIDE.md`

2. **Test Execution Guide** - **HOW TO RUN TESTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST-EXECUTION-GUIDE.md`

3. **Playwright Guide** - **E2E TESTING PATTERNS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/browser-automation/playwright-guide.md`

4. **Test Catalog** - **ALL EXISTING TESTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`

5. **Docker-Only Testing Standard** - **ENVIRONMENT REQUIREMENTS**
`/home/chad/repos/witchcityrope/docs/standards-processes/testing/docker-only-testing-standard.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/home/chad/repos/witchcityrope/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Skills Usage Guide** - `/.claude/skills/HOW-TO-USE-SKILLS.md` - Complete guide on when/how to use skills
- **Workflow Process** - `/home/chad/repos/witchcityrope/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/home/chad/repos/witchcityrope/docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Test Creation Guide FIRST** - How to write tests
- [ ] Review Test Catalog to avoid duplicating tests
- [ ] Check Playwright Guide for E2E patterns
- [ ] Verify Docker containers running (use container-restart skill)
- [ ] Review Docker-Only Testing Standard for environment

## 🚨 IMPORTANT: This File Documents PROBLEMS ONLY

**ALL testing patterns, procedures, and solutions are in the guides above.**

This lessons learned file contains ONLY:
- ✅ **Problem discovery context** (when/what/symptoms)
- ✅ **References to guides** for solutions
- ❌ **NOT pattern details** (they're in Test Creation Guide)
- ❌ **NOT execution procedures** (they're in Test Execution Guide)

**When you discover a new pattern, ADD IT TO THE GUIDES, not here.**

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: test-developer-lessons-learned.md (THIS FILE)
**Part 2**: test-developer-lessons-learned-2.md (MUST READ)
**Read ALL**: Parts 1 AND 2 are MANDATORY
**Write to**: Part 2 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

## 🚨 HARD BLOCK ENFORCEMENT (CRITICAL)
If you cannot read ANY part of these lessons learned:
1. **STOP ALL WORK IMMEDIATELY**
2. **DO NOT PROCEED** with any task or request
3. **FIX THE PROBLEM** using lessons-learned-validator skill
4. **ONLY PROCEED** when all files read successfully
5. These files contain critical knowledge - **NO EXCEPTIONS**

---

## Prevention Pattern: E2E Tests Must Use Login Helper

**Problem Discovered**: Tests kept implementing custom login code, causing maintenance nightmare and breaking when authentication changed.

**When**: Multiple test creation sessions (2025-08-22 to 2025-11-10)

**Symptoms**: Tests failing after authentication updates, duplicated login logic across 20+ test files

**Solution**: See Test Creation Guide, section "Authentication Patterns (MANDATORY)"

**Reference**: `loginAsAdmin`, `loginAsMember`, `loginAsVetted` helpers in `/tests/test-utils/auth-helpers`

---

## Prevention Pattern: Password Escaping Breaks Authentication

**Problem Discovered**: Test creation frequently introduced password escaping (`Test123\!`) breaking authentication.

**When**: 2025-09-22 (and multiple previous sessions)

**Symptoms**: "Invalid credentials" errors, tests failing with correct-looking password

**Root Cause**: JSON doesn't require exclamation mark escaping - backslash becomes part of password

**Solution**: See Test Creation Guide, section "Test Data Management" → "Seeded Test Users"

**Correct**: `"password": "Test123!"`
**Wrong**: `"password": "Test123\!"` (backslash breaks auth)

---

## Prevention Pattern: Docker-Only Testing Environment

**Problem Discovered**: Tests failed when running against local dev servers (ports 5174, 5175) instead of Docker (port 5173).

**When**: Multiple sessions (2025-08-18 to 2025-11-24)

**Symptoms**: Connection refused, 404 errors, flaky tests, port conflicts

**Solution**: See Test Execution Guide, section "Docker-Only Testing Environment"

**Critical Rules**:
- NEVER start local dev servers
- ALWAYS verify Docker running (use container-restart skill)
- ONLY use port 5173 (Docker)
- Kill rogue processes: `./scripts/kill-local-dev-servers.sh`

---

## Prevention Pattern: Container-Compatible URL Patterns

**Problem Discovered**: Tests hardcoded `http://localhost:5173` breaking in test containers.

**When**: 2025-10-07 (E2E stabilization)

**Symptoms**: Tests work in dev, fail in test-environment skill containers

**Solution**: See Test Creation Guide, section "E2E Test Creation" → "Container-Compatible URL Patterns"

**Correct**: `await page.goto('/events')` (relative URL)
**Wrong**: `await page.goto('http://localhost:5173/events')` (hardcoded)

---

## Prevention Pattern: Mantine v7 CSS Classes as Selectors

**Problem Discovered**: Tests using Mantine CSS classes broke when Mantine updated class names.

**When**: Mantine v7 migration (2025-10-08)

**Symptoms**: `Selector not found` errors, tests failing after Mantine updates

**Solution**: See Test Creation Guide, section "Selector Patterns (Mantine v7 + React Strict Mode)"

**Correct**: `[data-testid="submit-btn"]`
**Wrong**: `.mantine-Button-root` (class names change)

---

## Prevention Pattern: Fixed Timeouts Cause Flaky Tests

**Problem Discovered**: `await page.waitForTimeout(2000)` caused intermittent test failures.

**When**: Multiple E2E test creation sessions

**Symptoms**: Tests pass locally, fail in CI/CD, random timeouts

**Solution**: See Test Creation Guide, section "Wait Strategies (CRITICAL)"

**Correct**: `await expect(page.locator('[data-testid="loaded"]')).toBeVisible()`
**Wrong**: `await page.waitForTimeout(2000)` (flaky)

---

## Prevention Pattern: React Strict Mode Double-Mount

**Problem Discovered**: Components mount twice in development causing test state issues.

**When**: React 18 upgrade, Strict Mode enabled

**Symptoms**: Tests interact with component before it stabilizes, stale element errors

**Solution**: See Test Creation Guide, section "Selector Patterns" → "React Strict Mode Double-Mount Handling"

**Pattern**: Wait for component visibility, brief stabilization, then interact

---

## Prevention Pattern: CSRF Token Handling in E2E Tests

**Problem Discovered**: E2E tests needed CSRF tokens for POST/PUT/DELETE requests.

**When**: CSRF protection implementation (2025-11-23)

**Symptoms**: 400 Bad Request, "Invalid CSRF token" errors

**Solution**: See Test Creation Guide, section "CSRF Token Handling"

**Critical Knowledge**: Frontend Axios interceptor handles tokens automatically. E2E tests don't need manual token management.

---

## Prevention Pattern: Running Playwright from Wrong Directory

**Problem Discovered**: Running `npx playwright test` from `/tests/` subdirectory only executed 8 of 855 tests.

**When**: 2025-11-24 (test structure simplification)

**Symptoms**: "All tests pass" but 99% of tests silently skipped

**Solution**: See Test Execution Guide, section "E2E Tests (Playwright)" → "Working directory (CRITICAL)"

**Correct**: Run from `/home/chad/repos/witchcityrope/apps/web`
**Wrong**: Run from `/home/chad/repos/witchcityrope/apps/web/tests` (misses tests)

---

## Prevention Pattern: Test Data Uniqueness

**Problem Discovered**: Tests using hardcoded emails/usernames caused database constraint violations.

**When**: Integration test creation sessions

**Symptoms**: "Duplicate key value violates unique constraint", tests fail when run together

**Solution**: See Test Creation Guide, section "Test Data Management" → "Unique Test Data (CRITICAL)"

**Correct**: `var email = $"test-{Guid.NewGuid():N}@example.com"`
**Wrong**: `var email = "test@example.com"` (collisions)

---

## Prevention Pattern: TestHelperService for Complex Test Data

**Problem Discovered**: Tests created incomplete entities missing required relationships.

**When**: Integration test creation

**Symptoms**: Foreign key violations, null reference exceptions

**Solution**: See Test Creation Guide, section "Test Data Management" → "TestHelperService Pattern (RECOMMENDED)"

**Pattern**: Use TestHelperService for automatic relationship management and cleanup

---

## Prevention Pattern: Chrome DevTools MCP Available

**Problem Discovered**: Agents unaware of Chrome DevTools MCP capabilities for test debugging.

**When**: MCP setup (2025-10-03)

**Knowledge**: MCP provides performance tracing, browser automation, runtime inspection, visual debugging

**Solution**: See Test Creation Guide, section "Additional Resources" and Test Execution Guide, section "Chrome DevTools MCP"

**Use for**: Selector validation, test debugging, performance testing, screenshot capture

---

## Optional Reading

**When writing tests for CMS features**, consult:
- `/home/chad/repos/witchcityrope/docs/guides-setup/cms-implementation-guide.md` - CMS architecture, testing patterns

---

**REMEMBER**: This file documents PROBLEMS. For solutions and patterns, see Test Creation Guide and Test Execution Guide.
