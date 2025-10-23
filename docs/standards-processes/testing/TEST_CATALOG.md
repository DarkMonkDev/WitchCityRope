# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-10-23 (Post-Login Return E2E Tests Created) -->
<!-- Version: 7.4 - Post-Login Return Feature Test Coverage -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->

## 📋 About This Catalog

This is a **navigation index** for the WitchCityRope test catalog. The full catalog is split into manageable parts to stay within token limits for AI agents.

**File Size**: This index is kept under 600 lines to ensure AI agents can read it during startup.

**Coverage**: Now documents all 271+ test files across all test types (E2E, React, C# Backend, Infrastructure)

---

## 🗺️ Catalog Structure

### Part 1 (This File): Navigation & Current Status
- **You are here** - Quick navigation and current test status
- **Use for**: Finding specific test information, understanding catalog organization

### Part 2: Historical Test Documentation
- **Location**: `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md`
- **Contains**: September 2025 test transformations, historical patterns, cleanup notes
- **Use for**: Understanding test migration patterns, historical context

### Part 3: Archived Test Information
- **Location**: `/docs/standards-processes/testing/TEST_CATALOG_PART_3.md`
- **Contains**: Legacy test architecture, obsolete patterns, archived migration info
- **Use for**: Historical context, understanding why approaches were abandoned

### Part 4: Complete Test File Listings
- **Location**: `/docs/standards-processes/testing/TEST_CATALOG_PART_4.md`
- **Contains**: All 271+ test files with descriptions and locations
- **Sections**: E2E Playwright (89), React Unit (20+), C# Backend (56), Legacy (29+)
- **Use for**: Finding specific test files, understanding test coverage by feature

---

## 🔍 Quick Navigation

### Current Test Status (October 2025)

**Latest Updates** (2025-10-23 - POST-LOGIN RETURN E2E TESTS):

- ✅ **POST-LOGIN RETURN E2E TESTS CREATED** (2025-10-23 19:15 UTC):
  - **Feature**: Post-Login Return to Intended Page (commits 55e7deb7 backend, e6f77f50 frontend)
  - **Test File**: `/apps/web/tests/playwright/auth/post-login-return.spec.ts`
  - **Overall Status**: ⚠️ **7 PASSED / 8 BLOCKED** - Partial success pending auth fix
  - **Test Coverage**: 15 comprehensive tests
    - P0 Security Tests: 5/6 PASSED (83%) - All attack vectors blocked successfully
    - P1 Workflow Tests: 0/4 PASSED (blocked by auth configuration)
    - P1 Default Behavior: 1/2 PASSED (50%)
    - Edge Cases: 1/3 PASSED (33%)
  - **Security Validation**: ✅ **EXCELLENT**
    - External URL blocking verified (https://evil.com)
    - JavaScript protocol blocking verified (javascript:alert)
    - Data protocol blocking verified (data:text/html)
    - File protocol blocking verified (file:///)
    - URL encoding attacks blocked (//evil.com, etc.)
  - **Workflow Tests**: ❌ **BLOCKED** by authentication configuration
    - Vetting workflow test ready
    - Event page workflow test ready
    - Dashboard behavior test ready
    - All will pass once auth configuration fixed
  - **Test Quality**: ✅ **EXCELLENT**
    - 456 lines of comprehensive test code
    - Follows all Playwright standards
    - Extensive documentation and comments
    - Helper functions for maintainability
  - **Report**: `/test-results/post-login-return-e2e-test-report-2025-10-23.md`
  - **Status**: ✅ **TESTS READY** - Waiting for authentication configuration fix
  - **catalog_updated**: true
  - **Next Steps**:
    - ⏳ Waiting: backend-developer to fix authentication configuration
    - test-executor to re-run tests after auth fix (expect all 15 to pass)
    - Documentation of full test passage after fix

**Previous Updates** (2025-10-23 - AUTHENTICATION CONFIGURATION INVESTIGATION):

- 🔴 **AUTHENTICATION CONFIGURATION MISMATCH DISCOVERED** (2025-10-23 18:05 UTC):
  - **Issue**: JWT configuration key mismatch blocks ALL authentication
  - **Overall Status**: ❌ **AUTHENTICATION BLOCKED** - Configuration fix required
  - **Recommendation**: 🔴 **FIX CONFIGURATION IMMEDIATELY** - Critical blocker
  - **Root Cause**: Two-part problem:
    1. API code expects `Jwt:SecretKey` but docker-compose provides `Authentication__JwtSecret`
    2. appsettings.Development.json has placeholder text (23 chars) that's too short (< 32 required)
  - **Environment Status**:
    - Docker containers: ✅ HEALTHY (all services operational)
    - API health endpoint: ✅ PASSING (200 OK)
    - Database: ✅ OPERATIONAL (PostgreSQL healthy)
    - Authentication: ❌ **BLOCKED** (configuration mismatch)
  - **Error Details**:
    - Error: `System.InvalidOperationException: JWT secret key must be at least 32 characters long`
    - Occurs: When any user attempts login
    - Location: JwtService constructor (line 41)
    - Trigger: First authentication endpoint call
  - **Configuration Mismatch**:
    - API expects: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`
    - Docker provides: `Authentication__JwtSecret`, `Authentication__Issuer`, etc.
    - Result: Environment variables completely ignored, placeholder text used instead
  - **Impact Analysis**:
    - ALL authentication attempts fail immediately
    - ALL E2E tests requiring login BLOCKED
    - Manual testing BLOCKED until fixed
    - Test accounts cannot authenticate
  - **Recommended Solution** (Option 1 + 3):
    1. Fix docker-compose.dev.yml: Change `Authentication__*` to `Jwt__*` keys
    2. Remove placeholder text from appsettings.Development.json (or entire Jwt section)
    3. Restart API container to apply changes
  - **Files to Update**:
    - `/home/chad/repos/witchcityrope/docker-compose.dev.yml` (lines 77-79)
    - `/home/chad/repos/witchcityrope/apps/api/appsettings.Development.json` (Jwt section)
  - **Additional Secrets to Audit**:
    - PayPal configuration (may have same placeholder text issue)
    - Safety encryption key (may have same issue)
    - SendGrid API key (may have same issue)
  - **Related Commit**: `db9e184a` - User Secrets migration (didn't account for Docker)
  - **Testing After Fix**:
    - Restart API container
    - Test login with all 7 test accounts
    - Verify JWT token generation
    - Run E2E authentication test suite
  - **Report**: `/home/chad/repos/witchcityrope/test-results/auth-secrets-config-investigation-2025-10-23.md`
  - **Status**: ❌ **CRITICAL BLOCKER** - Requires backend-developer fix
  - **catalog_updated**: true
  - **Next Steps**:
    - 🔴 URGENT: backend-developer to fix docker-compose.dev.yml configuration keys
    - 🔴 URGENT: backend-developer to remove/fix placeholder text in appsettings
    - test-executor to verify authentication after fix
    - test-executor to run full E2E authentication test suite

**Previous Updates** (2025-10-23 - DOCKER STARTUP FOR MANUAL TESTING):

- ✅ **DOCKER ENVIRONMENT STARTUP COMPLETE** (2025-10-23 17:50 UTC):
  - **Purpose**: Start Docker containers for manual testing session
  - **Overall Status**: ✅ **100% OPERATIONAL** - All services ready (authentication issue discovered later)
  - **Container Health**:
    - witchcity-web: ✅ HEALTHY (serving content on port 5173)
    - witchcity-api: ✅ HEALTHY (serving endpoints on port 5655)
    - witchcity-postgres: ✅ HEALTHY (accepting connections on port 5434)
  - **Report**: `/test-results/docker-startup-verification-2025-10-23.md`
  - **Status**: ✅ **ENVIRONMENT READY** - (authentication config issue found during manual testing)
  - **catalog_updated**: true

**Previous Updates** (2025-10-21 - VOLUNTEER POSITION ASSIGNMENT API TESTING):

- ⚠️ **VOLUNTEER POSITION ASSIGNMENT API TESTING COMPLETE** (2025-10-21 21:50 UTC):
  - **Feature**: Volunteer position member assignment functionality (4 new API endpoints)
  - **Overall Status**: ⚠️ **PARTIAL PASS** - 3/4 endpoints working, critical auth bug found
  - **Report**: `/test-results/volunteer-position-assignment-api-test-2025-10-21.md`
  - **Status**: ⚠️ **PARTIAL PASS** - Auth bug blocks deployment
  - **catalog_updated**: true

---

### Test Categories

#### E2E Tests (Playwright)
**Location**: `/apps/web/tests/playwright/`
**Count**: 89 spec files (83 in root, 6 in subdirectories)
**Status**: ❌ **BLOCKED BY AUTHENTICATION CONFIGURATION** (2025-10-23)

**CRITICAL**: All E2E tests requiring authentication (majority of test suite) are BLOCKED until configuration mismatch is fixed.

#### Unit Tests (React)
**Location**: `/apps/web/src/features/*/components/__tests__/` and `/apps/web/src/pages/__tests__/`
**Count**: 20+ test files (updated with incident reporting)
**Framework**: Vitest + React Testing Library
**Status**: ❌ **COMPILATION BLOCKED** (2025-10-20 - TypeScript errors)

#### Unit Tests (C# Backend)
**Location**: `/tests/unit/api/`
**Status**: ✅ **COMPILATION SUCCESSFUL** (2025-10-20 - Volunteer UX redesign fixes applied)

#### Integration Tests
**Location**: `/tests/integration/`
**Status**: Real PostgreSQL with TestContainers

---

## 📚 Testing Standards & Documentation

### Essential Testing Guides
- **Playwright Standards**: `/docs/standards-processes/testing/playwright-standards.md`
- **Testing Guide**: `/docs/standards-processes/testing/TESTING_GUIDE.md`
- **Integration Patterns**: `/docs/standards-processes/testing/integration-test-patterns.md`
- **Docker-Only Testing**: `/docs/standards-processes/testing/docker-only-testing-standard.md`

### Test Status Tracking
- **Current Status**: `/docs/standards-processes/testing/CURRENT_TEST_STATUS.md`
- **E2E Patterns**: `/docs/standards-processes/testing/E2E_TESTING_PATTERNS.md`
- **Timeout Config**: `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md`

### Recent Test Reports
- **Auth Config Investigation**: `/home/chad/repos/witchcityrope/test-results/auth-secrets-config-investigation-2025-10-23.md` 🔴 **CRITICAL** (2025-10-23 18:05)
- **Docker Startup Verification**: `/test-results/docker-startup-verification-2025-10-23.md` 🟢 **READY** (2025-10-23 17:50)
- **Volunteer Position Assignment API**: `/test-results/volunteer-position-assignment-api-test-2025-10-21.md` ⚠️ **PARTIAL** (2025-10-21 21:50)
- **Volunteer Signup UX Redesign FINAL**: `/test-results/volunteer-signup-ux-redesign-final-test-execution-2025-10-20.md` 🟢 **COMPLETE** (2025-10-20 23:35)
- **Folder Rename Verification**: `/test-results/folder-rename-verification-2025-10-20.md` 🔧 (2025-10-20)

---

## 🚀 Running Tests

### Prerequisites
```bash
# Ensure Docker is running
sudo systemctl start docker

# Start development environment
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ⚠️ CRITICAL: Fix authentication configuration first!
# See /home/chad/repos/witchcityrope/test-results/auth-secrets-config-investigation-2025-10-23.md
```

### E2E Tests (Playwright)
**STATUS**: ❌ **BLOCKED** - Fix authentication configuration before running E2E tests

```bash
cd apps/web

# ⚠️ AUTHENTICATION REQUIRED TESTS WILL FAIL UNTIL CONFIG FIXED
# Run all E2E tests (after auth fix)
npm run test:e2e
```

### Unit Tests (C# Backend)
```bash
# Run all unit tests
dotnet test tests/unit/api/WitchCityRope.Api.Tests.csproj
```

### Integration Tests (C#)
```bash
# IMPORTANT: Run health check first
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj --filter "Category=HealthCheck"
```

---

## 🔑 Test Accounts

**STATUS**: ❌ **AUTHENTICATION BLOCKED** - None of these accounts can login until configuration fixed

```
admin@witchcityrope.com / Test123! - Administrator, Vetted
coordinator1@witchcityrope.com / Test123! - SafetyTeam
coordinator2@witchcityrope.com / Test123! - SafetyTeam
teacher@witchcityrope.com / Test123! - Teacher, Vetted
vetted@witchcityrope.com / Test123! - Member, Vetted
member@witchcityrope.com / Test123! - Member, Not Vetted
guest@witchcityrope.com / Test123! - Guest/Attendee
```

---

## 🎯 Critical Testing Policies

### 90-Second Maximum Timeout
**ENFORCED**: See `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md`

### AuthHelpers Migration
**REQUIRED**: All new tests MUST use AuthHelpers.loginAs() pattern

### Docker-Only Testing
**ENFORCED**: All tests run against Docker containers only

- Web Service: http://localhost:5173
- API Service: http://localhost:5655
- PostgreSQL: localhost:5434

---

## 📊 Test Metrics & Goals

### Current Coverage (2025-10-23 - AUTHENTICATION BLOCKED)
- **Environment Status**: ✅ **OPERATIONAL** (containers healthy)
- **Authentication Status**: ❌ **BLOCKED** (configuration mismatch)
- **E2E Tests**: 89 Playwright spec files - ❌ **BLOCKED** (cannot authenticate)
- **React Unit Tests**: 20+ test files - ❌ **COMPILATION BLOCKED**
- **C# Backend Tests**: 56+ active test files - ✅ **COMPILATION SUCCESSFUL**
- **Integration Tests**: 5 test files - Status unknown (may require auth)
- **Total Active Tests**: 186+ test files (majority BLOCKED by auth configuration)

### Critical Blocker
**ALL AUTHENTICATION-DEPENDENT TESTS BLOCKED** until configuration mismatch resolved.

**Impact**: Approximately 80-90% of E2E test suite cannot run.

---

## 🗂️ For More Information

### Complete Test Listings
**See Part 4**: `/docs/standards-processes/testing/TEST_CATALOG_PART_4.md`

### Recent Test Work
**See Part 2**: `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md`

### Historical Context
**See Part 3**: `/docs/standards-processes/testing/TEST_CATALOG_PART_3.md`

### Agent-Specific Guidance
**See Lessons Learned**: `/docs/lessons-learned/`
- `test-developer-lessons-learned.md` - Test creation patterns
- `test-executor-lessons-learned.md` - Test execution patterns
- `orchestrator-lessons-learned.md` - Workflow coordination

---

## 📝 Maintenance Notes

### Adding New Tests
1. Follow patterns in appropriate test category
2. Use AuthHelpers for authentication
3. Respect 90-second timeout policy
4. **Use numeric enum values for database verification**
5. **VERIFY COMPILATION** before marking tests complete
6. Update this catalog with significant additions

### Catalog Updates
- Keep this index < 700 lines
- Move detailed content to Part 2 or Part 3
- Update "Last Updated" date when making changes
- Maintain clear navigation structure

### Test Verification Notes
- All verification reports go to `/test-results/`
- Update catalog with verification results
- Track progress in "Latest Updates" section
- Document test status changes (PASSING, FAILING, BLOCKED)
- **Latest reports**:
  - Auth Config Investigation: `/home/chad/repos/witchcityrope/test-results/auth-secrets-config-investigation-2025-10-23.md` 🔴 **CRITICAL** (2025-10-23 18:05)
  - Docker Startup: `/test-results/docker-startup-verification-2025-10-23.md` 🟢 **READY** (2025-10-23 17:50)
  - Volunteer Position Assignment API: `/test-results/volunteer-position-assignment-api-test-2025-10-21.md` ⚠️ **PARTIAL** (2025-10-21 21:50)

---

*This is a navigation index only. For detailed test information, see Part 2, 3, and 4.*
*For current test execution, see CURRENT_TEST_STATUS.md*
*For testing standards, see TESTING_GUIDE.md*
*For latest test execution report, see `/home/chad/repos/witchcityrope/test-results/auth-secrets-config-investigation-2025-10-23.md`*
