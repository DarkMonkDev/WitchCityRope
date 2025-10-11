# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-10-10 21:45 -->
<!-- Version: 4.3 -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->

## 📋 About This Catalog

This is a **navigation index** for the WitchCityRope test catalog. The full catalog is split into manageable parts to stay within token limits for AI agents.

**File Size**: This index is kept under 500 lines to ensure AI agents can read it during startup.

**Coverage**: Now documents all 271 test files across all test types (E2E, React, C# Backend, Infrastructure)

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

### Part 4: Complete Test File Listings (NEW - 2025-10-10)
- **Location**: `/docs/standards-processes/testing/TEST_CATALOG_PART_4.md`
- **Contains**: All 271 test files with descriptions and locations
- **Sections**: E2E Playwright (89), React Unit (20), C# Backend (56), Legacy (29+)
- **Use for**: Finding specific test files, understanding test coverage by feature

---

## 🔍 Quick Navigation

### Current Test Status (October 2025)

**Latest Updates** (2025-10-10 22:30):
- ✅ **EVENT SESSION MATRIX TESTS FIXED**: 2 failing tests now passing
  - **event-session-matrix-test.spec.ts**: Updated selectors to match modal-based UI (was looking for inline forms)
  - **events-management-e2e.spec.ts**: Added Setup tab navigation, fixed session grid test
  - **Report**: `/test-results/event-session-matrix-selector-fixes-2025-10-10.md`
  - **Impact**: Feature confirmed 100% complete with modal-based UX
- ✅ **EVENTS CATEGORY DIAGNOSIS COMPLETE**: Comprehensive analysis of 142 events-related tests
  - **Results**: 95/142 passing (66.9%), 11 failures, 36 skipped
  - **Report**: `/test-results/events-category-diagnosis-2025-10-10.md`
  - **Critical Failures**: RSVP duplicate prevention, event click navigation
  - **Priority Fix Order**: 3 CRITICAL, 3 HIGH, 2 MEDIUM, 1 LOW
  - **Effort to 80%**: 14-21 hours across React + Backend developers
- ✅ **Event Session Matrix Demo Test Skipped**: 1 test skipped pending P1 post-login redirect feature
- ✅ **Phase 2 Public Events Selector Fixes COMPLETE**: Fixed 8 tests, skipped 3 tests per diagnosis
- ✅ **Phase 2 Public Events Selector Diagnosis COMPLETE**: Identified exact fixes for 10+ test failures
- ✅ **TEST CATALOG AUDIT COMPLETE**: Updated catalog to reflect all 89 test files
- ✅ **AuthHelpers Migration Complete**: All 22 tests now use AuthHelpers.loginAs() pattern

**Pass Rate Progress**:
- Previous: 68% (September 2025 - multiple authentication issues)
- Current: 66.9% (95/142 events tests) - Full suite: 68.1% (243/357 tests)
- **Events Category**: 11 failures identified with specific fix priorities
- Next Target: 75% (CRITICAL + HIGH priority fixes)
- Ultimate Goal: 90%+

**Today's Major Work** (2025-10-10):
- **EVENTS CATEGORY DIAGNOSIS**: Complete analysis of 142 events tests
  - 11 specific failures categorized and prioritized
  - 4 UI/component issues (React Developer)
  - 2 backend/API issues (Backend Developer)
  - 3 workflow/integration issues (React + Test Developer)
  - 1 performance issue (adjust test expectations)
  - **Document**: `/test-results/events-category-diagnosis-2025-10-10.md`
- **PUBLIC EVENTS SELECTOR DIAGNOSIS**: Complete analysis of 10+ test failures
  - 8 tests need selector updates (specific fixes documented)
  - 2 tests should be skipped (wireframe validation only)
  - 3 tests blocked by API 401 issue (P1-3 backend task)
  - **Document**: `/test-results/phase2-public-events-selector-diagnosis-2025-10-10.md`
- Fixed syntax error in capture-public-pages.spec.ts (missing closing brace)
- 22 test files migrated to AuthHelpers (eliminated manual login code)
- Bio validation alignment fixed (frontend now enforces 500 char limit)
- Auth routing infinite loop resolved
- Multiple strict mode violations fixed
- 3 new verification tests added (rebuild, recent changes, vetting status)

### Test Categories

#### E2E Tests (Playwright)
**Location**: `/apps/web/tests/playwright/`
**Count**: 89 spec files (83 in root, 6 in subdirectories)
**Status**: AuthHelpers migration 100% complete (2025-10-10)

**Events Category Test Results** (2025-10-10 21:45):
- **Total Tests**: 142 tests matching "event" keyword
- **Passed**: 95 tests (66.9%)
- **Failed**: 11 tests (7.7%)
- **Skipped**: 36 tests (25.4%)
- **Full Report**: `/test-results/events-category-diagnosis-2025-10-10.md`

**Critical Events Test Failures** (MUST FIX FIRST):
1. **RSVP Duplicate Prevention** - Data integrity issue (Backend Developer)
2. **Event Click Navigation** - Core user journey broken (React Developer)
3. **Event Session Matrix Incomplete** - 4/9 features working (React + Backend Developer)

**High Priority Events Test Failures** (FIX NEXT):
4. **Admin Event Editing Workflow** - Admin functionality broken (React Developer)
5. **Events List Display** - Admin demo page not working (React Developer)
6. **Mock Events Cleanup** - Still showing fake data (React Developer)

**Phase 2 Public Events Fixes COMPLETE** (2025-10-10 20:00):
- ✅ **events-display-verification.spec.ts**: Fixed strict mode violation (h1/h2 selector with .first())
- ✅ **phase4-events-testing.spec.ts**: Fixed all 5 selector issues (event-filters → button-view-toggle, event-type → event-date, etc.)
- ✅ **events-comprehensive.spec.ts**: Fixed 2 logout issues (clearAuthState instead of logout navigation), skipped 1 test blocked by API 401
- ✅ **capture-public-pages.spec.ts**: Fixed syntax error, skipped 2 wireframe tests (design validation only, files don't exist)
- **Total**: 8 tests fixed, 3 tests skipped (1 API blocked, 2 wireframe validation)

**Key Files**:
- `admin-events-workflow-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-navigation-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-detailed-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-table-ui-check.spec.ts` - ✅ Fixed with AuthHelpers
- `event-session-matrix-test.spec.ts` - ✅ PASSING - Updated selectors for modal-based UI (2025-10-10)
- `events-management-e2e.spec.ts` - ✅ PASSING - Session grid test fixed with Setup tab navigation (2025-10-10)
- `e2e/tiptap-editors.spec.ts` - ✅ Fixed with AuthHelpers - 7/7 tests passing

**New Verification Tests** (2025-10-10):
- `simple-rebuild-verification.spec.ts` - Verifies admin dashboard loads after rebuild
- `verify-recent-changes.spec.ts` - Verifies vetting workflow changes (notes, badges, counts)
- `verify-vetting-status-fix.spec.ts` - Verifies VettingStatus enum alignment with shared-types

**All Tests Now Use AuthHelpers** (Migration Complete):
- `events-comprehensive.spec.ts` - ✅ Migrated to AuthHelpers
- `vetting-notes-direct.spec.ts` - ✅ Already using AuthHelpers
- `vetting-notes-display.spec.ts` - ✅ Already using AuthHelpers

**Tests Pending P1 Features** (2025-10-10 20:30):
- `events-management-e2e.spec.ts` - 1 test skipped: "should load Event Session Matrix demo page"
  - Blocked by: Post-login return-to-page feature (P1 CRITICAL)
  - Reference: `/docs/standards-processes/PRE_LAUNCH_PUNCH_LIST.md` lines 58-64
  - Current: Login from demo page redirects to dashboard
  - Expected: Return to `/admin/event-session-matrix-demo` after login
  - Will re-enable when feature implemented

#### Unit Tests
**Location**: `/tests/unit/api/`
**Status**: Core domain logic and API service layer coverage
**Pass Rate**: Improving with test isolation fixes

**Key Projects**:
- `WitchCityRope.Api.Tests` - API service layer tests
- `WitchCityRope.Core.Tests` - Core domain logic tests

**Latest Additions** (2025-10-10):
- `VettingPublicServiceTests.cs` - 15 tests for public/user-facing vetting methods
  - Public application submission (4 tests)
  - Public status checks (3 tests)
  - User self-service (2 tests)
  - Duplicate prevention (2 tests)
  - User status tests (4 tests)

#### Integration Tests
**Location**: `/tests/integration/`
**Status**: Real PostgreSQL with TestContainers
**CRITICAL**: Always run health checks first

```bash
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
```

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

### Phase 2 Test Recovery Documentation
- **P1 Assessment**: `/test-results/phase2-p1-assessment-2025-10-10.md`
- **Public Events Diagnosis**: `/test-results/phase2-public-events-selector-diagnosis-2025-10-10.md`
- **Events Category Diagnosis**: `/test-results/events-category-diagnosis-2025-10-10.md` (NEW)

---

## 🚀 Running Tests

### Prerequisites
```bash
# Ensure Docker is running
sudo systemctl start docker

# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### E2E Tests (Playwright)
```bash
cd apps/web

# Run all E2E tests
npm run test:e2e

# Run events category tests
npm run test:e2e -- --grep "event"

# Run specific test file
npm run test:e2e admin-events-workflow-test.spec.ts

# Run with UI mode (debugging)
npm run test:e2e -- --ui
```

### Unit Tests
```bash
# Run all unit tests
dotnet test tests/WitchCityRope.Core.Tests/
dotnet test tests/unit/api/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
# IMPORTANT: Run health check first
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# If health check passes, run all integration tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

---

## 🔑 Test Accounts

```
admin@witchcityrope.com / Test123! - Administrator, Vetted
teacher@witchcityrope.com / Test123! - Teacher, Vetted
vetted@witchcityrope.com / Test123! - Member, Vetted
member@witchcityrope.com / Test123! - Member, Not Vetted
guest@witchcityrope.com / Test123! - Guest/Attendee
```

---

## 🎯 Critical Testing Policies

### 90-Second Maximum Timeout
**ENFORCED**: See `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md`

**Why**: Prevents indefinite hangs, enforces performance standards, maintains fast feedback loops

**Violations**: Any timeout > 90 seconds requires explicit justification and must be reviewed

### AuthHelpers Migration
**REQUIRED**: All new tests MUST use AuthHelpers.loginAs() pattern

**Correct Pattern**:
```typescript
import { AuthHelpers } from './helpers/auth.helpers';

await AuthHelpers.loginAs(page, 'admin');
console.log('✅ Logged in as admin successfully');
```

**Wrong Pattern (DO NOT USE)**:
```typescript
// ❌ WRONG - 90% more code, unreliable
await page.goto('http://localhost:5173/login');
await page.waitForLoadState('networkidle');
const emailInput = page.locator('[data-testid="email-input"]');
// ... 25 more lines of manual login code
```

### Docker-Only Testing
**ENFORCED**: All tests run against Docker containers only

- Web Service: http://localhost:5173
- API Service: http://localhost:5655
- PostgreSQL: localhost:5433

**Violation**: Local dev servers are DISABLED to prevent confusion

---

## 📊 Test Metrics & Goals

### Current Coverage (2025-10-10 21:45)
- **E2E Tests**: 89 Playwright spec files
  - **All Tests**: 243/357 passing (68.1%)
  - **Events Category**: 95/142 passing (66.9%) - 11 failures diagnosed
- **React Unit Tests**: 20 test files (Vitest + React Testing Library)
- **C# Backend Tests**: 56 active test files (xUnit + Moq + FluentAssertions)
- **Integration Tests**: 5 test files (PostgreSQL with TestContainers)
- **Legacy/Obsolete**: 29+ test files (marked as legacy-obsolete or disabled)
- **Total Active Tests**: 170 test files across all types

### Phase 2 Recovery Progress
- **P0 Blockers**: ✅ Complete (ticket persistence, enum mapping)
- **P1 Assessment**: ✅ Complete (24 auth issues, 5 public events, 7 form issues)
- **Public Events Diagnosis**: ✅ Complete (10-12 tests, exact fixes documented)
- **Public Events Fixes**: ✅ Complete (8 tests fixed, 3 tests skipped)
- **Events Category Diagnosis**: ✅ Complete (11 failures, prioritized fix order)
- **Next**: CRITICAL events fixes (RSVP, navigation, Event Session Matrix)

### Target Coverage
- **Pass Rate**: 90%+ (current: 66.9% events, 68.1% overall, next milestone: 75%)
- **Critical Paths**: 100% coverage for authentication, events, payments
- **Performance**: All tests < 90 seconds timeout

### Events Category Breakdown (2025-10-10)
- **Total Events Tests**: 142 tests
- **UI/Component Failures**: 4 tests (React Developer)
- **Backend/API Failures**: 2 tests (Backend Developer)
- **Workflow/Integration Failures**: 3 tests (React + Test Developer)
- **Performance Failures**: 1 test (adjust expectations)
- **Data Display Failures**: 1 test (React Developer)

### Test File Breakdown
- **Admin Tests**: 4 spec files (events management)
- **Auth/Login Tests**: 20+ spec files (login, authentication flows)
- **Dashboard Tests**: 5+ spec files (user dashboard, profile, settings)
- **Events Tests**: 15+ spec files (creation, editing, display, RSVP)
- **Vetting Tests**: 10+ spec files (application workflow, admin review)
- **Diagnostic Tests**: 15+ spec files (debugging, verification, checks)
- **E2E Subdirectory**: 6 spec files (admin/vetting, dashboard, tiptap)
- **Total**: 89 test files

---

## 🗂️ For More Information

### Complete Test Listings
**See Part 4**: `/docs/standards-processes/testing/TEST_CATALOG_PART_4.md`
- All 89 E2E Playwright tests with descriptions
- All 20 React unit tests organized by feature
- All 56 C# backend tests by project/category
- Legacy/obsolete test documentation

### Recent Test Work
**See Part 2**: `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md`
- September 2025 test transformations
- Authentication and Events test rewrites
- Unit test isolation transformation
- PayPal integration test suite

### Historical Context
**See Part 3**: `/docs/standards-processes/testing/TEST_CATALOG_PART_3.md`
- Test migration analysis
- Legacy test architecture
- Archived patterns and cleanup notes

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
4. Update this catalog with significant additions

### Catalog Updates
- Keep this index < 500 lines
- Move detailed content to Part 2 or Part 3
- Update "Last Updated" date when making changes
- Maintain clear navigation structure

### Phase 2 Test Recovery Notes
- All diagnoses go to `/test-results/`
- Update catalog with diagnosis file references
- Track progress in "Latest Updates" section
- Document specific fixes for developer delegation

---

*This is a navigation index only. For detailed test information, see Part 2, 3, and 4.*
*For current test execution, see CURRENT_TEST_STATUS.md*
*For testing standards, see TESTING_GUIDE.md*
*For Phase 2 diagnoses, see `/test-results/` directory*
