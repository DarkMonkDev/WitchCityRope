# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-10-11 (Seed Data Verification) -->
<!-- Version: 5.3 - Test Verification Phase -->
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

**Latest Updates** (2025-10-11 - TEST FIXES & VERIFICATION):
- ✅ **DYNAMIC EVENT ID TEST FIXTURE FIX COMPLETE** (2025-10-11 04:20):
  - **Affected Tests**: 3 test files (verify-event-fixes.spec.ts, debug-form-fields.spec.ts, debug-save-button-regression.spec.ts)
  - **Issue**: Tests using hardcoded event IDs that no longer exist after database reseed
  - **Solution**: Created EventHelpers to fetch events dynamically from API
  - **Helper Created**: `/apps/web/tests/playwright/helpers/event.helpers.ts` - Dynamic event fetching utility
  - **Result**: All 8 tests now PASSING (5/5 + 1/1 + 2/2 = 8/8 = 100%)
  - **Benefits**: Tests work with any seed data, no more hardcoded ID brittleness
  - **Duration**: 1.5 hours (as estimated in task)
  - **Impact**: +6 tests passing (was 2/8, now 8/8)
- ✅ **RSVP TEST TEMPLATE FIX COMPLETE** (2025-10-11 04:15):
  - **Test**: `rsvp-lifecycle-persistence.spec.ts` (10 tests)
  - **Template**: `rsvp-persistence-template.ts`
  - **Issue Fixed**: Removed non-existent confirmation dialog expectation
  - **Changes**: RSVP runs silently (no modal, no notification), Cancel RSVP has modal
  - **Status**: No longer timing out - now progresses to database verification
  - **Next Issue**: Database status enum mismatch (Registered vs Active)
  - **Report**: `/test-results/rsvp-test-confirmation-dialog-fix-2025-10-11.md`
  - **Impact**: Template now matches actual UI behavior, ready for commit
- ✅ **DATABASE RESEED SUCCESSFUL** (2025-10-11 03:30):
  - **Status**: All 8 events have shortDescription, description, and policies fields
  - **Field Lengths**: ShortDescription (63-110 chars), Description (480-2167 chars), Policies (190-2307 chars)
  - **Verification**: API endpoints returning all fields correctly in JSON responses
  - **Test**: `events-management-e2e.spec.ts:47` - ✅ PASSING (4.0s) - "should display events list from API"
  - **Report**: `/test-results/seed-data-verification-2025-10-11.md`
  - **Impact**: Seed data fully functional, ready for persistence fixes

- ✅ **TEST FIXTURE ISSUES RESOLVED** (2025-10-11 04:20):
  - **Previously Failing**: `verify-event-fixes.spec.ts` (2/5 passing), `debug-form-fields.spec.ts` (0/1), `debug-save-button-regression.spec.ts` (0/2)
  - **Issue**: Tests using hardcoded event ID `1439490f-f8f5-4688-9aee-070bba569ec5` that no longer exists after database reseed
  - **Root Cause**: Event IDs changed when database was reseeded (expected behavior)
  - **Fix Applied**: Created EventHelpers to fetch events dynamically from API (see above)
  - **Current Status**: ALL 8 TESTS PASSING (100%)
  - **Report**: `/test-results/seed-data-verification-2025-10-11.md`

- ❌ **POLICIES COMPREHENSIVE TEST TIMEOUT** (2025-10-11 03:30):
  - **Test**: `events-policies-field-comprehensive.spec.ts` (5 tests)
  - **Status**: ❌ TEST HUNG (exceeded 180s timeout)
  - **Issue**: TipTap rich text editor operations taking too long
  - **Not a Bug**: Policies field data is correct, test needs performance optimization
  - **Fix Required**: Optimize test or split into smaller units (1 hour - test-developer)
  - **Priority**: LOW (policies functionality verified working via API)
  - **Report**: `/test-results/seed-data-verification-2025-10-11.md`

**Previous Session** (2025-10-10 - RSVP & EVENTS VERIFICATION):
- ✅ **EVENTS LIST DISPLAY VERIFIED WORKING** (2025-10-10 22:15):
  - **Test**: `events-management-e2e.spec.ts:47` - "should display events list from API"
  - **Status**: ✅ PASSING (3.9s)
  - **Verification**: API unwrapping fix confirmed working - 6 events display correctly
  - **Report**: `/test-results/rsvp-events-verification-2025-10-10.md`
- ✅ **NAVIGATION COMPREHENSIVE TEST VERIFIED** (2025-10-10 22:15):
  - **Test**: `navigation-comprehensive.spec.ts` (19 tests)
  - **Status**: ✅ 18/19 PASSING (94.7%)
  - **Report**: `/test-results/rsvp-events-verification-2025-10-10.md`

**Pass Rate Progress**:
- Previous Baseline: 68.1% (243/357 tests - from PROGRESS.md)
- Phase 2 Completion: 72.5% (259/357 tests - +4.4% improvement)
- Dashboard Quick Wins: 74.5% (263/357 tests - +6.4% improvement from baseline)
- Performance Test Fix: 75.0% (264/357 tests - +6.9% improvement from baseline)
- **Current Estimate: 75.5% (265/357 tests) - +7.4% improvement from baseline** ⬆️
  - **+1 test verified**: events-management-e2e.spec.ts:47 (events list display - reconfirmed with new seed data)
- **Next Milestone**: 80% (286/357 tests - need +21 more tests)
- **Events Category**: 66.9% (95/142 tests) - 11 failures diagnosed
- **Dashboard Tests**: 79.5% (58/73 tests) - ⬆️ +5 tests fixed
- **Performance Tests**: 100% (5/5 tests) - ✅ ALL PASSING
- **Navigation Tests**: 94.7% (18/19 tests) - ✅ CONSOLIDATION SUCCESS
- **Remaining Failures**: 42 tests (11.8%) - all analyzed with fix plans
- **Skipped Tests**: 45 tests (12.6%) - mostly wireframe validation
- Ultimate Goal: 90%+

**Today's Major Work** (2025-10-11):
- **SEED DATA VERIFICATION SESSION**: Applied new event fields and verified API
  - **Database Reseed**: ✅ SUCCESSFUL (forced reseed with new shortDescription, description, policies fields)
  - **API Fields**: ✅ ALL FIELDS WORKING (shortDescription, description, policies returned correctly)
  - **Events List Display**: ✅ RECONFIRMED PASSING (4.0s) - Works with new seed data
  - **Test Infrastructure**: ⚠️ 2 test files need fixture updates (hardcoded event IDs)
  - **Environment Health**: ✅ 100% HEALTHY (Docker containers operational)
  - **Document**: `/test-results/seed-data-verification-2025-10-11.md`
  - **Duration**: 30 minutes
  - **Impact**: Seed data ready for persistence fixes, test fixtures need minor updates

---

### Test Categories

#### E2E Tests (Playwright)
**Location**: `/apps/web/tests/playwright/`
**Count**: 89 spec files (83 in root, 6 in subdirectories)
**Status**: AuthHelpers migration 100% complete (2025-10-10)

**Full Suite Results** (2025-10-11 00:15):
- **Total Tests**: 357 tests
- **Passed**: 259 tests (72.5%) ⬆️ +4.4% from baseline
- **Estimated Current**: 265 tests (75.5%) ⬆️ +7.4% from baseline
- **Failed**: 42 tests (11.8%) - down from 44 (events list display verified)
- **Skipped**: 45 tests (12.6%)
- **Did Not Run**: 9 tests (2.5%)
- **Duration**: 7.3 minutes (6 workers)
- **Report**: `/test-results/phase2-final-verification-2025-10-10.md`

**Key Files with Updated Status**:
- `events-management-e2e.spec.ts` - ✅ Line 47 VERIFIED PASSING (2025-10-11 03:30 - reconfirmed with new seed data)
  - Test: "should display events list from API"
  - Status: ✅ PASSING (4.0s) - Works with new shortDescription field
  - Verification: API data unwrapping fix working correctly with new seed data
  - Report: `/test-results/seed-data-verification-2025-10-11.md`
- `verify-event-fixes.spec.ts` - ✅ 5/5 PASSING (2025-10-11 04:20)
  - Status: ✅ ALL TESTS PASSING (100%)
  - Fix Applied: Dynamic event fetching via EventHelpers (no more hardcoded IDs)
  - Uses: EventHelpers.getFirstActiveEvent() and getAllActiveEvents()
  - Report: `/test-results/seed-data-verification-2025-10-11.md`
- `debug-form-fields.spec.ts` - ✅ 1/1 PASSING (2025-10-11 04:20)
  - Status: ✅ PASSING (uses dynamic event ID)
  - Fix Applied: Uses EventHelpers.getFirstActiveEvent() instead of hardcoded ID
- `debug-save-button-regression.spec.ts` - ✅ 2/2 PASSING (2025-10-11 04:20)
  - Status: ✅ ALL TESTS PASSING (uses dynamic event ID)
  - Fix Applied: Uses EventHelpers.getFirstActiveEvent() for both tests
- `events-policies-field-comprehensive.spec.ts` - ❌ TEST TIMEOUT (2025-10-11 03:30)
  - Status: ❌ TIMEOUT (exceeded 180s)
  - Issue: TipTap editor operations too slow
  - Priority: LOW (policies field data verified working via API)
  - Report: `/test-results/seed-data-verification-2025-10-11.md`
- `navigation-comprehensive.spec.ts` - ✅ 18/19 PASSING (2025-10-10 22:15)
- `dashboard-comprehensive.spec.ts` - ✅ 8/14 PASSING - 4 selector fixes COMPLETE (2025-10-11 01:30)
- `events-comprehensive.spec.ts` - ✅ 5/5 performance tests PASSING (2025-10-11 01:45)

#### Unit Tests
**Location**: `/tests/unit/api/`
**Status**: Core domain logic and API service layer coverage
**Pass Rate**: Improving with test isolation fixes

**Key Projects**:
- `WitchCityRope.Api.Tests` - API service layer tests
- `WitchCityRope.Core.Tests` - Core domain logic tests

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
- **Seed Data Verification**: `/test-results/seed-data-verification-2025-10-11.md` ✅ NEW (2025-10-11 03:30)
- **RSVP & Events Verification**: `/test-results/rsvp-events-verification-2025-10-10.md` ✅ (2025-10-10 22:15)
- **Performance Test Fix**: `/test-results/performance-test-fix-2025-10-10.md` ✅ COMPLETE
- **Dashboard Selector Fixes**: `/test-results/dashboard-selector-fixes-2025-10-10.md` ✅ COMPLETE
- **Quick Win Analysis**: `/test-results/quick-win-test-analysis-2025-10-10.md`
- **Final Verification**: `/test-results/phase2-final-verification-2025-10-10.md`
- **Events Category Diagnosis**: `/test-results/events-category-diagnosis-2025-10-10.md`
- **Admin Event Editing Diagnosis**: `/test-results/admin-event-editing-diagnosis-2025-10-10.md`
- **Events List Display Diagnosis**: `/test-results/events-list-display-diagnosis-2025-10-10.md`
- **Status Badges Diagnosis**: `/test-results/status-badges-diagnosis-2025-10-10.md`
- **Failing Test Analysis**: `/test-results/failing-test-analysis-2025-10-10.md` ✅ COMPLETE

---

## 🚀 Running Tests

### Prerequisites
```bash
# Ensure Docker is running
sudo systemctl start docker

# Start development environment
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### E2E Tests (Playwright)
```bash
cd apps/web

# Run all E2E tests
npm run test:e2e

# Run specific verified tests
npm run test:e2e -- tests/playwright/events-management-e2e.spec.ts:47 --project=chromium
npm run test:e2e -- tests/playwright/navigation-comprehensive.spec.ts --project=chromium

# Run events category tests
npm run test:e2e -- --grep "event"

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

### Docker-Only Testing
**ENFORCED**: All tests run against Docker containers only

- Web Service: http://localhost:5173
- API Service: http://localhost:5655
- PostgreSQL: localhost:5433

**Violation**: Local dev servers are DISABLED to prevent confusion

---

## 📊 Test Metrics & Goals

### Current Coverage (2025-10-11 03:30 - SEED DATA VERIFICATION)
- **E2E Tests**: 89 Playwright spec files
  - **All Tests**: 265/357 passing (75.5% estimated) ⬆️ +7.4% from baseline
  - **Failed**: 42 tests (11.8%) - down from 44
  - **Skipped**: 45 tests (12.6%)
  - **Events Category**: 96/142 passing (67.6%) - events list display reconfirmed with new seed data
  - **Dashboard Category**: 58/73 passing (79.5%)
  - **Performance Category**: 5/5 passing (100%)
  - **Navigation Category**: 18/19 passing (94.7%)
- **React Unit Tests**: 20 test files (Vitest + React Testing Library)
- **C# Backend Tests**: 56 active test files (xUnit + Moq + FluentAssertions)
- **Integration Tests**: 5 test files (PostgreSQL with TestContainers)
- **Legacy/Obsolete**: 29+ test files (marked as legacy-obsolete or disabled)
- **Total Active Tests**: 170 test files across all types

### Target Coverage
- **Current Estimate**: 75.5% (265/357 tests)
- **Next Milestone**: 80% (286/357 tests) - need +21 more tests
- **Effort to 80%**: 18-28 hours (React 12-18h, Backend 6-10h)
- **Ultimate Goal**: 90%+
- **Critical Paths**: 100% coverage for authentication, events, payments
- **Performance**: All tests < 90 seconds timeout

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
- Keep this index < 650 lines (expanded for seed verification notes)
- Move detailed content to Part 2 or Part 3
- Update "Last Updated" date when making changes
- Maintain clear navigation structure

### Test Verification Notes
- All verification reports go to `/test-results/`
- Update catalog with verification results
- Track progress in "Latest Updates" section
- Document test status changes (PASSING, FAILING, BLOCKED)
- **Latest verification**: `/test-results/seed-data-verification-2025-10-11.md`

---

*This is a navigation index only. For detailed test information, see Part 2, 3, and 4.*
*For current test execution, see CURRENT_TEST_STATUS.md*
*For testing standards, see TESTING_GUIDE.md*
*For latest verification, see `/test-results/seed-data-verification-2025-10-11.md`*
