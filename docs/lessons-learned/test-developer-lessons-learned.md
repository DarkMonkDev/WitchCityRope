# Test Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL TESTING DOCUMENTS (MUST READ): 🚨
1. **Docker-Only Testing Standard** - **ALL TESTS RUN IN DOCKER**
`/docs/standards-processes/testing/docker-only-testing-standard.md`

2. **Playwright Standards** - **E2E TESTING FRAMEWORK**
`/docs/standards-processes/testing/playwright-standards.md`

3. **Test Catalog** - **ALL EXISTING TESTS** (SPLIT FOR ACCESSIBILITY)
⭕ **START HERE**: `/docs/standards-processes/testing/TEST_CATALOG.md` (Part 1 - Current Tests)
📚 **If needed**: `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md` (Part 2 - Historical)
📜 **Archives**: `/docs/standards-processes/testing/TEST_CATALOG_PART_3.md` (Part 3 - Archived)

4. **Testing Prerequisites** - **BEFORE YOU START**
`/docs/standards-processes/testing-prerequisites.md`

5. **Project Architecture** - **TECH STACK**
`/ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **Current Test Status** - `/docs/standards-processes/testing/CURRENT_TEST_STATUS.md` - Test state

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **E2E Testing Patterns** - `/docs/standards-processes/testing/E2E_TESTING_PATTERNS.md`
- **Testing Guide** - `/docs/standards-processes/testing/TESTING_GUIDE.md`
- **Workflow Process** - `/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Docker-Only Testing Standard FIRST** - All tests run in Docker containers
- [ ] Review Playwright Standards for E2E patterns
- [ ] Check Test Catalog to avoid duplicating tests
- [ ] Review Testing Prerequisites before starting
- [ ] Verify Docker containers are running with `./dev.sh`

## 🛠️ AVAILABLE TESTING TOOLS

### Chrome DevTools MCP (NEW - 2025-10-03)
**Purpose**: Enhanced test development and debugging for E2E tests

**Key Capabilities for Test Developers**:
- **Test Debugging**: Inspect page state during test execution, view console errors
- **Selector Validation**: Verify selectors work correctly before adding to tests
- **Network Monitoring**: Validate API calls during test scenarios, inspect request/response data
- **Performance Testing**: Measure page load times and component render performance
- **Screenshot Capture**: Take visual snapshots for test documentation and debugging

**Use Cases for Test Development**:
- Test creation - Validate selectors and page interactions before writing tests
- Test debugging - Inspect page state when tests fail to identify root causes
- Visual regression - Capture baseline screenshots for comparison testing
- Performance validation - Add performance assertions based on measured metrics
- Integration testing - Monitor API calls to ensure proper data flow

**Configuration**: Automatically available via MCP - see `/docs/standards-processes/MCP/MCP_SERVERS.md`

**Integration with Playwright**:
- Use alongside Playwright for enhanced debugging capabilities
- Capture screenshots on test failures for easier debugging
- Monitor network traffic to validate API integration in E2E tests
- Profile page performance to set appropriate timeout values

**Best Practices**:
- Use to validate selectors before adding them to Playwright tests
- Capture screenshots for visual regression test baselines
- Monitor console for errors that might cause test flakiness
- Measure performance metrics to set realistic test timeouts
- Inspect API responses to validate data flow in integration tests

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: `/docs/lessons-learned/test-developer-lessons-learned.md` (THIS FILE)
**Part 2**: `/docs/lessons-learned/test-developer-lessons-learned-2.md` (MUST ALSO READ)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: Part 2 ONLY
**Maximum file size**: 1700 lines (to stay under token limits). Both Part 1 and Part 2 files can be up to 1700 lines each
**IF READ FAILS**: STOP and fix per documentation-standards.md

## 🚨 IF THIS FILE EXCEEDS 1700 LINES, CREATE PART 2! BOTH FILES CAN BE UP TO 1700 LINES EACH 🚨

## 🚨 HARD BLOCK ENFORCEMENT (CRITICAL)
If you cannot read ANY part of these lessons learned:
1. **STOP ALL WORK IMMEDIATELY**
2. **DO NOT PROCEED** with any task or request
3. **FIX THE PROBLEM** using procedure in documentation-standards.md
4. **ONLY PROCEED** when all files read successfully
5. These files contain critical knowledge - **NO EXCEPTIONS**

## ⛔ CRITICAL: FILE READ ENFORCEMENT
If you cannot read ANY part of these lessons learned:
1. STOP all work immediately
2. Fix the issue per documentation-standards.md
3. DO NOT proceed until all files are readable
4. This is NON-NEGOTIABLE - these files contain critical knowledge

## 🚨 ULTRA CRITICAL: Password Escaping in JSON - NO ESCAPING REQUIRED (2025-09-22) 🚨

**Problem**: Test creation frequently introduces password escaping that breaks authentication, causing hours of debugging "Invalid credentials" errors.
**Solution**: Never escape exclamation marks in JSON - use "Test123!" not "Test123\!" in all test data.

### ❌ WRONG - These patterns break authentication:
```bash
# WRONG - Escaped exclamation causes login failure
echo '{"email": "admin@witchcityrope.com", "password": "Test123\!"}' > login.json

# WRONG - In test files
const loginData = {
  password: 'Test123\!' // This will fail authentication
}
```

### ✅ CORRECT - Proper password handling:
```bash
# CORRECT - No escaping needed
echo '{"email": "admin@witchcityrope.com", "password": "Test123!"}' > login.json

# CORRECT - In test files
const loginData = {
  password: 'Test123!' // Correct - no backslash
}
```

## 🚨 ULTRA CRITICAL: Docker-Only Testing Environment - MANDATORY 🚨

**Problem**: Tests fail when running against wrong ports or local dev servers instead of Docker containers.
**Solution**: ALWAYS verify Docker containers are running on port 5173 before creating any tests.

### 🛑 CRITICAL RULES FOR TEST DEVELOPERS:
1. **NEVER start local dev servers** - Use Docker only: `./dev.sh`
2. **ALWAYS verify Docker is running** before creating ANY tests
3. **ONLY use port 5173** (Docker) - NEVER 5174, 5175, or any other port
4. **KILL rogue processes**: `./scripts/kill-local-dev-servers.sh` if needed

### ✅ MANDATORY PRE-TEST VERIFICATION:
```bash
# 1. Verify Docker is running (REQUIRED)
docker ps | grep witchcity-web
# Should show: witchcity-web on port 5173

# 2. Kill any rogue local dev servers
./scripts/kill-local-dev-servers.sh

# 3. Verify correct port
curl -f http://localhost:5173/ || echo "ERROR: Docker not on port 5173"
```

## Prevention Pattern: Unit Test Infrastructure Dependencies

**Problem**: Unit tests contained infrastructure dependencies causing failures when Docker containers misconfigured.
**Solution**: Use in-memory database helpers for pure business logic testing; move infrastructure tests to separate project.

## Prevention Pattern: E2E JavaScript Error Detection

**Problem**: E2E tests passed despite JavaScript errors breaking functionality.
**Solution**: Add error monitoring to EVERY E2E test beforeEach to catch JavaScript errors before validating content.

## Prevention Pattern: Authentication Test Blazor Patterns

**Problem**: Post-migration authentication tests used wrong selectors and expected non-existent modal patterns.
**Solution**: Update all authentication tests to use React implementation patterns and current UI selectors.

## Prevention Pattern: MSW API Endpoint Mismatch

**Problem**: Tests failing because MSW handlers didn't match actual API endpoints.
**Solution**: Always check actual hook/API client code when creating MSW handlers.

## 🚨 ULTRA CRITICAL: NEW LESSONS GO TO PART 2, NOT HERE! 🚨

**PART 1 PURPOSE**: Startup procedures and critical navigation ONLY
**ADD ALL NEW LESSONS TO PART 2**: `/docs/lessons-learned/test-developer-lessons-learned-2.md`

## NEVER ADD NEW LESSONS TO THIS FILE (PART 1)

**This file (Part 1) contains ONLY**:
- Mandatory startup procedures
- Critical navigation information
- Essential prevention patterns for immediate safety
- File structure and reading instructions

**All other lessons go to Part 2** - DO NOT add them here!

## 🚨 IF THIS FILE EXCEEDS 1700 LINES, CREATE PART 2! BOTH FILES CAN BE UP TO 1700 LINES EACH 🚨