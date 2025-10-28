# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-10-27 21:10 UTC (Login with Email or Scene Name Tests RE-EXECUTION COMPLETED) -->
<!-- Version: 9.0 - Re-execution confirms ALL tests still blocked, claimed fixes NOT applied -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->

## 📋 About This Catalog

This is a **navigation index** for the WitchCityRope test catalog. The full catalog is split into manageable parts to stay within token limits for AI agents.

**File Size**: This index is kept under 700 lines to ensure AI agents can read it during startup.

**Coverage**: Now documents all 271+ test files across all test types (E2E, React, C# Backend, Infrastructure) + CMS verification

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

**Latest Updates** (2025-10-27 21:10 UTC - LOGIN WITH EMAIL OR SCENE NAME TESTS RE-EXECUTION COMPLETED - CRITICAL FINDINGS):

- 🔴 **LOGIN WITH EMAIL OR SCENE NAME FEATURE TESTS - RE-EXECUTION CONFIRMS BLOCKERS REMAIN** (2025-10-27 21:10 UTC):
  - **Purpose**: Comprehensive test coverage for new authentication feature allowing login with email OR scene name
  - **Feature**: Users can now login using either email address or scene name (backend tries email first, then scene name lookup)
  - **Test Execution Status**: 🔴 **CRITICAL - 0/27 TESTS EXECUTED - CLAIMED FIXES NOT APPLIED**
  - **Re-Execution Results**:
    - **Backend Unit Tests** (5 tests): ❌ **NOT RUN - Compilation blocked by NEW error**
      - ✅ NuGet package conflict RESOLVED (9.0.6 → 9.0.10 upgrade successful)
      - ❌ NEW BLOCKER: `PricingTiers` compilation errors in Tests.Common project
      - Error: 'UpdateEventRequest' does not contain a definition for 'PricingTiers'
      - Error: 'CreateEventRequest' does not contain a definition for 'PricingTiers'
      - Location: UpdateEventRequestBuilder.cs:131, CreateEventRequestBuilder.cs:179
      - Root cause: Architectural change (Sessions/TicketTypes migration) not propagated to test helpers
      - **Impact**: Blocks ALL backend test compilation (not just login tests)
      - Fix required: Remove PricingTiers references from test builder classes (5 min)
      - Owner: backend-developer
    - **Backend Integration Tests** (7 tests): ❌ **NOT ATTEMPTED - Blocked by unit test compilation failure**
      - Cannot run integration tests when Tests.Common project fails to compile
      - Tests created but never executed
    - **E2E Tests** (15 tests): ❌ **ALL 14 FAILED - localStorage bug NOT FIXED**
      - ✅ Tests executed (14 of 15 tests ran)
      - ❌ ALL 14 tests failed with SAME localStorage error as before
      - Error: "SecurityError: Failed to read localStorage - Access is denied"
      - Root cause: clearAuthState() STILL called before page.goto() (line 84)
      - **CRITICAL**: Claimed fix was NOT actually applied to test file
      - Test file still has wrong code order in beforeEach hook
      - Fix required: Move clearAuthState() after page.goto() (3 min)
      - Owner: test-developer
  - **Environment Health**: ✅ **100% OPERATIONAL**
    - Docker containers: All healthy (Web: 5173, API: 5655, DB: 5434)
    - API health: http://localhost:5655/health → 200 OK
    - Database: 7 test users seeded
    - No local dev server conflicts
    - OpenAPI spec exported: 93 endpoints
  - **Test Coverage Created** (ready to run after fixes):
    - P1 CRITICAL: Email/scene name login paths (6 tests)
    - P1 Validation: Empty field validation (3 tests)
    - Edge Cases: Whitespace, case sensitivity, UI text (6 tests)
  - **Business Impact**: ⚠️ **FEATURE FUNCTIONALITY UNKNOWN - CANNOT VERIFY**
    - Cannot confirm email login works
    - Cannot confirm scene name login works
    - Cannot verify error handling
    - Cannot test validation logic
    - **RISK**: Feature may be deployed without verification
  - **Critical Blockers** (2 CONFIRMED):
    1. 🔴 PricingTiers compilation errors block ALL backend tests (backend-developer, 5 min)
    2. 🔴 localStorage bug blocks ALL E2E tests (test-developer, 3 min)
  - **Critical Findings from Re-Execution**:
    - ❌ **CLAIMED FIXES NOT APPLIED**: Context said fixes were done, reality shows code unchanged
    - ❌ **NEW REGRESSION**: PricingTiers architectural change broke test infrastructure
    - ❌ **VERIFICATION GAP**: No one actually ran tests after claiming fixes
    - ✅ **NuGet fix confirmed**: Package version conflict successfully resolved
  - **Files Created**:
    - `/tests/unit/api/Features/Auth/LoginIntegrationTests.cs` - NEW integration test file (NOT EXECUTED)
    - `/tests/playwright/auth/login-with-scene-name.spec.ts` - NEW E2E test file (ALL FAILED)
    - `/apps/web/tests/playwright/auth/login-with-scene-name.spec.ts` - Copied to correct location
  - **Files Modified**:
    - `/tests/WitchCityRope.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs` - Added 5 unit tests (NOT EXECUTED)
  - **Test Reports**:
    - `/test-results/login-scene-name-test-execution-report-2025-10-27.md` - Initial failure analysis (from context)
    - `/test-results/login-scene-name-final-test-execution-report-2025-10-27.md` - Re-execution comprehensive analysis
    - `/test-results/login-scene-name-unit-tests-rerun-2025-10-27.log` - Unit test compilation failure details
    - `/test-results/login-scene-name-e2e-tests-rerun-2025-10-27.log` - E2E localStorage failure details
  - **Status**: 🔴 **CRITICAL - CLAIMED FIXES NOT APPLIED - AWAITING ACTUAL FIXES**
  - **Next Actions** (UPDATED):
    1. backend-developer: Remove PricingTiers from test builders (5 min) - **NEW BLOCKER**
    2. test-developer: Fix E2E localStorage bug (3 min) - **STILL BROKEN**
    3. test-executor: Re-run tests after fixes ACTUALLY applied (15 min)
  - **Time to Resolution**: 23 minutes (8 min fixes + 15 min re-test)
  - **catalog_updated**: true (2025-10-27 21:10 UTC - re-execution completed, blockers confirmed still present)

**Previous Updates** (2025-10-27 20:45 UTC - LOGIN WITH EMAIL OR SCENE NAME TESTS EXECUTION ATTEMPTED):

- 🔴 **LOGIN WITH EMAIL OR SCENE NAME FEATURE TESTS - BLOCKED** (2025-10-27 20:45 UTC):
  - **Purpose**: Comprehensive test coverage for new authentication feature allowing login with email OR scene name
  - **Feature**: Users can now login using either email address or scene name (backend tries email first, then scene name lookup)
  - **Test Execution Status**: ⚠️ **BLOCKED - 0/27 TESTS EXECUTED DUE TO INFRASTRUCTURE ISSUES**
  - **Execution Results**:
    - **Backend Unit Tests** (5 tests): ❌ **NOT RUN - NuGet package version conflict**
      - Error: Microsoft.Extensions.* package downgrade from 9.0.10 to 9.0.6
      - Fix required: Update package versions in WitchCityRope.Core.Tests.csproj
      - Estimated fix: 10 minutes (backend-developer)
    - **Backend Integration Tests** (7 tests): ❌ **NOT RUN - 23 compilation errors**
      - Root cause: Breaking API changes not propagated to existing tests
      - Errors: LoginRequest.Email → EmailOrSceneName (10), logger parameters (5), other changes (8)
      - Files affected: 8 test files need updates
      - Estimated fix: 2-3 hours (backend-developer)
    - **E2E Tests** (15 tests): ❌ **ALL FAILED - Test infrastructure bug**
      - Error: "SecurityError: Failed to read localStorage - Access is denied"
      - Root cause: clearAuthState() called before page.goto() in beforeEach hook
      - Location: Line 84 in login-with-scene-name.spec.ts
      - Fix required: Move clearAuthState() after page.goto()
      - Estimated fix: 5 minutes (test-developer)
  - **Environment Health**: ✅ **100% OPERATIONAL**
    - Docker containers: All healthy (Web: 5173, API: 5655, DB: 5434)
    - API health: http://localhost:5655/health → 200 OK
    - Database: 7 test users seeded
    - No local dev server conflicts
  - **Test Coverage Created** (ready to run after fixes):
    - P1 CRITICAL: Email/scene name login paths (6 tests)
    - P1 Validation: Empty field validation (3 tests)
    - Edge Cases: Whitespace, case sensitivity, UI text (6 tests)
  - **Business Impact**: ⚠️ **FEATURE FUNCTIONALITY UNKNOWN - CANNOT VERIFY**
    - Cannot confirm email login works
    - Cannot confirm scene name login works
    - Cannot verify error handling
    - Cannot test validation logic
  - **Critical Blockers** (3):
    1. 🔴 NuGet package conflict blocks unit tests (backend-developer, 10 min)
    2. 🔴 23 compilation errors block integration tests (backend-developer, 2-3 hours)
    3. 🔴 localStorage bug blocks E2E tests (test-developer, 5 min)
  - **Files Created**:
    - `/tests/unit/api/Features/Auth/LoginIntegrationTests.cs` - NEW integration test file (NOT EXECUTED)
    - `/tests/playwright/auth/login-with-scene-name.spec.ts` - NEW E2E test file (BLOCKED)
    - `/apps/web/tests/playwright/auth/login-with-scene-name.spec.ts` - Copied to correct location
  - **Files Modified**:
    - `/tests/WitchCityRope.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs` - Added 5 unit tests (NOT EXECUTED)
  - **Test Reports**:
    - `/test-results/login-scene-name-test-execution-report-2025-10-27.md` - Comprehensive failure analysis
    - `/test-results/login-scene-name-integration-tests.log` - Compilation error details
    - `/test-results/login-scene-name-e2e-tests.log` - E2E infrastructure failures
  - **Status**: 🔴 **BLOCKED - AWAITING FIXES FROM test-developer AND backend-developer**
  - **Next Actions**:
    1. test-developer: Fix E2E localStorage bug (5 min)
    2. backend-developer: Fix package versions (10 min) + update tests (2-3 hours)
    3. test-executor: Re-run tests after fixes applied
  - **catalog_updated**: true (2025-10-27 20:45 UTC - execution attempted, blockers identified)

**Previous Updates** (2025-10-27 20:41 UTC - LOGIN WITH EMAIL OR SCENE NAME TESTS CREATED):

- ✅ **LOGIN WITH EMAIL OR SCENE NAME FEATURE TESTS CREATED** (2025-10-27 20:41 UTC):
  - **Purpose**: Comprehensive test coverage for new authentication feature allowing login with email OR scene name
  - **Feature**: Users can now login using either email address or scene name (backend tries email first, then scene name lookup)
  - **Test Coverage Created**:
    - **Backend Unit Tests** (5 new tests): `/tests/WitchCityRope.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs`
      - ✅ `LoginAsync_WithValidEmail_ShouldSucceed` - Email lookup path validation
      - ✅ `LoginAsync_WithValidSceneName_ShouldSucceed` - Scene name fallback lookup validation
      - ✅ `LoginAsync_WithInvalidEmailOrSceneName_ShouldFail` - Generic error message (security)
      - ✅ `LoginAsync_WithValidEmailButWrongPassword_ShouldFail` - Password validation with email
      - ✅ `LoginAsync_WithValidSceneNameButWrongPassword_ShouldFail` - Password validation with scene name
    - **Backend Integration Tests** (7 new tests): `/tests/unit/api/Features/Auth/LoginIntegrationTests.cs` (NEW FILE)
      - ✅ `LoginEndpoint_WithValidEmail_Returns200AndAuthCookie` - HTTP endpoint with email
      - ✅ `LoginEndpoint_WithValidSceneName_Returns200AndAuthCookie` - HTTP endpoint with scene name
      - ✅ `LoginEndpoint_WithInvalidCredentials_Returns401` - Invalid identifier rejection
      - ✅ `LoginEndpoint_WithValidEmailButWrongPassword_Returns401` - Password validation
      - ✅ `LoginEndpoint_WithValidSceneNameButWrongPassword_Returns401` - Password validation
      - ✅ `LoginEndpoint_WithEmptyEmailOrSceneName_Returns400` - Required field validation
      - ✅ `LoginEndpoint_WithEmptyPassword_Returns400` - Required field validation
    - **E2E Tests** (15 new tests): `/tests/playwright/auth/login-with-scene-name.spec.ts` (NEW FILE)
      - ✅ Email Login Path (2 tests): Valid email login, wrong password error
      - ✅ Scene Name Login Path (2 tests): Valid scene name login, wrong password error
      - ✅ Error Handling (2 tests): Invalid email/scene name, non-existent scene name
      - ✅ Validation (3 tests): Empty email/scene name, empty password, both empty
      - ✅ Edge Cases (6 tests): Whitespace handling, case sensitivity, placeholder text, helper text
  - **Total New Tests**: 27 comprehensive tests across all layers
  - **Backend Changes Updated**: LoginRequest.EmailOrSceneName field, AuthenticationService dual lookup logic
  - **Frontend Changes Updated**: LoginPage.tsx with emailOrSceneName input and validation
  - **Test Execution**: NOT RUN YET - Awaiting test-executor agent
  - **Files Created**:
    - `/tests/unit/api/Features/Auth/LoginIntegrationTests.cs` - NEW integration test file
    - `/tests/playwright/auth/login-with-scene-name.spec.ts` - NEW E2E test file
  - **Files Modified**:
    - `/tests/WitchCityRope.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs` - Added 5 unit tests + updated 3 existing
  - **Status**: ✅ **ALL TESTS CREATED - READY FOR EXECUTION**
  - **catalog_updated**: true (2025-10-27 20:41 UTC)

**Previous Updates** (2025-10-24 - TEST INITIALIZATION PATTERN FIXED - 95.1% PASSING):

- ✅ **PARTICIPATION TEST INITIALIZATION PATTERN FIXED** (2025-10-24):
  - **Purpose**: Fix NullReferenceException in Participation tests caused by service initialization in constructor
  - **Overall Status**: ⚠️ **95.1% PASS RATE - 446/469 PASSING** (up from 94.0%)
  - **Test Execution Summary**:
    - Total tests: 469 tests
    - Passing: 446 (95.1%)
    - Failing: 8 (1.7%)
    - Skipped: 15 (3.2%)
    - Duration: 7m 55s
  - **Root Cause Identified**: ParticipationService created in test constructor before DbContext initialized
  - **Technical Analysis**:
    - Test lifecycle: Constructor → InitializeAsync() → Test Method
    - DatabaseTestBase.InitializeAsync() creates DbContext instance
    - Services were created in constructor when DbContext was null
    - Service stored null reference → NullReferenceException during test execution
  - **Fix Applied**: ✅ Moved service creation from constructor to InitializeAsync()
  - **Files Fixed**:
    - `/tests/unit/api/Features/Participation/ParticipationServiceTests.cs` - Fixed initialization (Lines 19-36)
    - `/tests/unit/api/Features/Participation/ParticipationServiceDiagnosticTest.cs` - Fixed initialization (Lines 17-36)
    - `/apps/api/Features/Participation/Services/ParticipationService.cs` - Re-added EventType enum fix
  - **Participation Tests Result**: 28/32 passing (87.5%) - improved from NullReferenceException failures
  - **Remaining Failures** (8 tests total):
    - **Participation Tests** (4 business logic issues):
      - `GetParticipationStatusAsync_WithNoParticipation_ReturnsNull` - Returns non-null
      - `CreateRSVPAsync_WithNonVettedUser_ReturnsFailure` - Not failing for non-vetted users
      - `GetParticipationStatusAsync_WithCancelledParticipation_ReturnsNull` - Returns non-null for cancelled
      - `CreateTicketPurchaseAsync_AfterCancelledTicket_FailsDueToUniqueConstraint` - Allows duplicate
    - **DatabaseInitialization Tests** (4 constructor issues):
      - `ClassifyError_WithSpecificExceptionTypes` (2 tests) - MissingMethodException for SocketException/NetworkInformationException
      - `ExecuteAsync_WithTimeout_ThrowsTimeoutException` - No exception thrown
      - `ExecuteAsync_WithSeedDataFailure_HandlesBehaviorBasedOnConfiguration` - No exception thrown
  - **Session Context**: User expected ALL Participation tests passing after cleanup
  - **Critical Finding**: Only 28/32 Participation tests passing suggests working fixes may have been lost during code revert
  - **Commit**: 4a3a7c32 "fix(tests): fix Participation test initialization and EventType enum usage"
  - **Test Log**: `/test-results/full-suite-after-participation-fix-2025-10-24.log`
  - **Next Steps**:
    - Review git history to find commit where ALL Participation tests were passing
    - Restore working fixes that were lost during `git checkout` revert
    - Fix 4 DatabaseInitialization test failures
  - **Status**: ⚠️ **INFRASTRUCTURE FIX COMPLETE - BUSINESS LOGIC ISSUES REMAIN**
  - **catalog_updated**: true (2025-10-24 21:30 UTC)

---

### Test Categories

#### E2E Tests (Playwright) - BASELINE COMPLETE ✅
**Location**: `/apps/web/tests/playwright/`
**Count**: 89 spec files (83 in root, 6 in subdirectories) + 1 NEW (login-with-scene-name.spec.ts)
**Total Tests**: 425 test cases + 15 NEW (login with email/scene name)
**Status**: ⚠️ **70.3% PASS RATE - STABLE, NEEDS SYSTEMATIC FIXES** + NEW TESTS BLOCKED

**Pass Rate Details** (2025-10-23 Baseline - excludes new login tests):
- **Passing**: 299 tests (70.3%)
- **Failing**: 75 tests (17.6%)
- **Skipped**: 51 tests (12.0%)
- **Flaky**: 0 tests (0%) ✅

**Login Feature Tests** (2025-10-27 NEW - BLOCKED):
- **Created**: 15 tests
- **Executed**: 14 tests (1 not executed)
- **Passing**: 0 tests (0%)
- **Failing**: 14 tests (100%)
- **Blocker**: localStorage bug in beforeEach hook (clearAuthState called before page.goto)

**Key Strengths**:
- ✅ Authentication system: 100% operational (zero auth failures in baseline tests)
- ✅ Test reliability: Zero flaky tests detected
- ✅ Environment stability: Docker containers healthy
- ✅ Core user workflows: Login, events, RSVP, profile all working

**Known Issues** (6 failing test files from baseline + 1 NEW):
1. `console-error-check.spec.ts` - Application generates 3 console errors (HIGH PRIORITY)
2. `vetting-notes-display.spec.ts` - Test data setup issue (MEDIUM)
3. `vetting-notes-direct.spec.ts` - Element visibility issue (MEDIUM)
4. `test-dom-check.spec.ts` - Navigation timeout (LOW)
5. `test-with-reload.spec.ts` - Navigation timeout (LOW)
6. `_archived/diagnostic-tests/page-load-diagnostic.spec.ts` - Deprecated API (LOW)
7. **NEW**: `login-with-scene-name.spec.ts` - localStorage access error (HIGH PRIORITY - blocks new feature)

**Detailed Report**: `/test-results/e2e-baseline-2025-10-23.md`
**New Feature Report**: `/test-results/login-scene-name-final-test-execution-report-2025-10-27.md`

#### Unit Tests (C# Backend) - PARTICIPATION TESTS STILL FAILING ⚠️
**Location**: `/tests/unit/api/`
**Count**: 476 tests total (399 passing, 62 failing, 15 skipped) + 5 NEW (login with email/scene name - NOT EXECUTED)
**Status**: ⚠️ **83.8% PASS RATE - PARTICIPATION TESTS NEED FIX** (2025-10-24)
**Pass Rate**: 399/476 (83.8%)
**Latest Execution**: Participation tests after AsNoTracking() fix (2025-10-24)

**NEW TESTS** (2025-10-27 - NOT EXECUTED):
- **Created**: 5 new AuthenticationService unit tests for email/scene name login
- **Status**: ❌ **COMPILATION BLOCKED** - PricingTiers errors in Tests.Common project
- **Blocker**: Architectural change not propagated to test helper classes
- **Impact**: Blocks ALL backend test compilation (not just new tests)

**Participation Test Results** (2025-10-24):
- **Total Participation Tests**: 32
- **Passing**: 22 (68.8%)
- **Failing**: 10 (31.3%)
- **Root Cause**: `[NotMapped]` property `ApplicationUser.IsVetted` cannot be accessed in EF queries
- **Fix Required**: Replace `user.IsVetted` with `user.VettingStatus == 3` in ParticipationService
- **Business Impact**: CRITICAL BLOCKER - Users cannot RSVP or purchase tickets

**Overall Backend Test Breakdown**:
- **P1 (Launch Blockers)**: 34 tests (includes 10 Participation + 24 others) - Vetting emails, RSVP logic, status transitions
- **P2 (Important)**: 23 tests - Seed data, dashboard, health checks
- **P3 (Nice-to-Have)**: 5 tests - Infrastructure edge cases, minor fixes

**Critical Issues**:
1. 🔴 Participation/RSVP Service (10 tests) - [NotMapped] property access in queries - **NEW ROOT CAUSE IDENTIFIED**
2. 🔴 Vetting Email Service (13 tests) - No email notifications sent to users
3. 🔴 Participation/RSVP Service (9 tests - original analysis) - Broken event registration
4. 🔴 Vetting Status Logic (2 tests) - Wrong status assignments
5. 🔴 **NEW**: Test Compilation (ALL tests) - PricingTiers architectural change broke test builders

**Estimated Fix Time**: 18 hours total (Participation: 30 min, PricingTiers: 5 min, Others: 17.5 hours)
**Detailed Analysis**: `/test-results/failing-tests-analysis-2025-10-23.md`
**Participation Analysis**: `/test-results/participation-tests-after-asnotracking-fix-2025-10-24-ANALYSIS.md`
**Login Tests**: Not executed yet

#### Integration Tests
**Location**: `/tests/integration/`
**Status**: Real PostgreSQL with TestContainers + 7 NEW (login with email/scene name - NOT EXECUTED)

**NEW TESTS** (2025-10-27 - NOT EXECUTED):
- **Created**: 7 new integration tests for login HTTP endpoints
- **Status**: ❌ **NOT ATTEMPTED** - Blocked by unit test compilation failure
- **File**: `/tests/unit/api/Features/Auth/LoginIntegrationTests.cs` (NEW)

#### Unit Tests (React)
**Location**: `/apps/web/src/features/*/components/__tests__/` and `/apps/web/src/pages/__tests__/`
**Count**: 20+ test files (updated with incident reporting)
**Framework**: Vitest + React Testing Library
**Status**: ❌ **COMPILATION BLOCKED** (2025-10-20 - TypeScript errors)

#### CMS Pages (Database + API Verification)
**Location**: `/apps/api/Features/Cms/`
**Database**: `cms.ContentPages` table (PostgreSQL)
**API Endpoints**: `/api/cms/pages/{slug}` (public), `/api/cms/pages` (admin)
**Status**: ✅ **ALL 10 PAGES OPERATIONAL** (2025-10-23)

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
- **Login Scene Name Final Report**: `/test-results/login-scene-name-final-test-execution-report-2025-10-27.md` 🔴 **RE-EXECUTION CONFIRMS BLOCKERS** (2025-10-27 21:10 UTC)
- **Login Scene Name Initial**: `/test-results/login-scene-name-test-execution-report-2025-10-27.md` 🔴 **BLOCKED** (2025-10-27 20:45 UTC)
- **Participation After Fix**: `/test-results/participation-tests-after-asnotracking-fix-2025-10-24-ANALYSIS.md` ⚠️ **ROOT CAUSE IDENTIFIED** (2025-10-24)
- **Backend Failure Analysis**: `/test-results/failing-tests-analysis-2025-10-23.md` ⚠️ **52 FAILURES CATEGORIZED** (2025-10-23)
- **E2E Baseline**: `/test-results/e2e-baseline-2025-10-23.md` ⚠️ **70.3% PASS RATE** (2025-10-23 23:29)
- **Phase 2 Integration**: `/test-results/phase2-final-results-2025-10-23.md` ✅ **93.1% PASS RATE** (2025-10-23)
- **CMS Pages**: `/test-results/cms-pages-seeding-test-report-2025-10-23.md` ✅ **ALL OPERATIONAL** (2025-10-23 22:30)

---

## 🚀 Running Tests

### Unit Tests (C# Backend)
**STATUS**: ⚠️ **COMPILATION BLOCKED** - PricingTiers errors in Tests.Common

```bash
# Cannot run until PricingTiers errors fixed
dotnet test tests/unit/api/WitchCityRope.Api.Tests.csproj

# Error:
# 'UpdateEventRequest' does not contain a definition for 'PricingTiers'
# 'CreateEventRequest' does not contain a definition for 'PricingTiers'

# Fix required: Remove PricingTiers from test builders (5 min)
```

### E2E Tests (Playwright)
**STATUS**: ⚠️ **70.3% PASSING (baseline)** + **NEW TESTS FAILING (login feature)**

```bash
cd apps/web
npm run test:e2e

# Baseline Results (2025-10-23):
# Total: 425 tests
# Passed: 299 (70.3%)
# Failed: 75 (17.6%)
# Skipped: 51 (12.0%)
# Flaky: 0 (0%)
# Duration: 4.4 minutes

# NEW Login Feature Tests (2025-10-27):
# Total: 14 tests executed (1 not executed)
# Passed: 0 (0%)
# Failed: 14 (100%)
# Blocker: localStorage bug in beforeEach hook
# Fix required: Move clearAuthState() after page.goto() (3 min)
```

---

## 📊 Test Metrics & Goals

### Current Coverage (2025-10-27 21:10 UTC - LOGIN WITH EMAIL OR SCENE NAME TESTS RE-EXECUTION COMPLETE)
- **Environment Status**: ✅ **OPERATIONAL** (containers healthy)
- **CMS Pages**: 10 pages - ✅ **ALL OPERATIONAL**
- **E2E Tests**: 425 tests (baseline) - ⚠️ **70.3% PASSING** + 15 NEW (login) - 🔴 **0% PASSING (14/14 failed)**
- **React Unit Tests**: 20+ test files - ❌ **COMPILATION BLOCKED**
- **C# Backend Tests**: 476 tests (baseline) - ⚠️ **COMPILATION BLOCKED** + 5 NEW (login) - 🔴 **CANNOT RUN**
  - **CRITICAL**: PricingTiers architectural change broke test infrastructure
  - **NEW TESTS**: 5 unit + 7 integration tests created but not executed
  - P1 Blockers: 34 tests (Participation + vetting + RSVP + status logic)
  - P2 Important: 23 tests (seed data, dashboard, health)
  - P3 Nice-to-Have: 5 tests (edge cases, minor fixes)
- **Login with Email/Scene Name Tests**: 27 tests created - 🔴 **0 EXECUTED SUCCESSFULLY**
  - Backend unit tests: 5 tests - ❌ Compilation blocked
  - Backend integration tests: 7 tests - ❌ Not attempted
  - E2E tests: 15 tests - ❌ 14 failed (localStorage bug), 1 not executed

### Backend Test Roadmap
| Phase | Tests | Pass Rate | Status | Target Date |
|-------|-------|-----------|--------|-------------|
| Current | 399/476 | 83.8% | ⚠️ Root Cause ID'd | 2025-10-24 |
| After PricingTiers Fix | 404/481 | 84.0% | 🎯 Quick Win (5 min) | Immediate |
| After Participation Fix | 414/481 | 86.1% | 🎯 Quick Win (30 min) | Immediate |
| After Phase 1 | 438/481 | 91.1% | 🎯 Launch Ready | Week 1 |
| After Phase 2 | 458/481 | 95.2% | ✅ Stable | Week 2 |
| After Phase 3 | 464/481 | 96.5% | 🎯 Production | Week 3 |

---

## 🚨 Launch Readiness Status

### Login with Email or Scene Name Feature: 🔴 **RED - CRITICAL - CANNOT TEST**

**CRITICAL BLOCKERS** (2 - prevent ALL testing):
1. 🔴 **PricingTiers compilation errors** - Backend tests cannot compile (backend-developer, 5 min)
2. 🔴 **localStorage access error** - E2E tests fail before test logic (test-developer, 3 min)

**Feature Status**: ❓ **UNKNOWN - CANNOT VERIFY FUNCTIONALITY**
- Cannot test email login
- Cannot test scene name login
- Cannot verify error handling
- Cannot validate form behavior
- **RISK**: Feature may be deployed without any test verification

**Critical Finding**: Claimed fixes were NOT actually applied. Re-execution confirms:
- ✅ NuGet package conflict resolved
- ❌ PricingTiers errors introduced (NEW regression)
- ❌ localStorage bug still present (fix not applied)

### Backend API Tests: 🔴 **RED - COMPILATION BLOCKED**

**MUST FIX IMMEDIATELY** (New issue - 2025-10-27):
1. 🔴 **BLOCKER**: Remove PricingTiers from test builders
   - **Location**: Tests.Common/Builders/ (2 files)
   - **Fix**: Remove obsolete PricingTiers references
   - **Estimated Time**: 5 minutes
   - **Impact**: Blocks ALL backend test compilation

**Must Fix Before Launch** (24 tests - P1):
1. ❌ Participation/RSVP (10 tests) - [NotMapped] property access
2. ❌ Vetting Email Service (13 tests) - Users won't receive status updates
3. ❌ Vetting Status Transitions (2 tests) - Wrong statuses assigned

**Should Fix Before Launch** (23 tests - P2):
1. ⚠️ SeedDataService (17 tests) - Development environment issues
2. ⚠️ Dashboard Service (3 tests) - User can't see their events
3. ⚠️ Health Checks (3 tests) - Monitoring gaps

**Can Fix After Launch** (5 tests - P3):
1. ✅ DB Init Edge Cases (5 tests) - Infrastructure resilience

### E2E Tests: ⚠️ **YELLOW - 70% READY (baseline)** + 🔴 **RED - NEW TESTS FAILING**

**Must Fix Before Launch**:
1. ❌ Console errors in application (HIGH PRIORITY - baseline)
2. ❌ Vetting admin navigation (HIGH PRIORITY - baseline)
3. ❌ **NEW**: localStorage bug in login tests (HIGH PRIORITY - NEW FEATURE BLOCKER)

**Core Functionality**: ✅ **OPERATIONAL (baseline tests)**
- Users can register, login, browse events, and RSVP
- Admin can manage events and vetting
- Profile updates persist correctly
- Ticket system functional

**NEW Feature**: ❓ **UNKNOWN (new login tests blocked)**

---

## 🔑 Test Accounts

**STATUS**: ✅ **AUTHENTICATION OPERATIONAL**

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

*This is a navigation index only. For detailed test information, see Part 2, 3, and 4.*
*For current test execution, see CURRENT_TEST_STATUS.md*
*For testing standards, see TESTING_GUIDE.md*
*For comprehensive failure analysis, see `/test-results/failing-tests-analysis-2025-10-23.md`*
*For Participation test analysis, see `/test-results/participation-tests-after-asnotracking-fix-2025-10-24-ANALYSIS.md`*
*For Login with Email/Scene Name test execution, see `/test-results/login-scene-name-final-test-execution-report-2025-10-27.md`*
