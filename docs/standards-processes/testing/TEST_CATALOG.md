# WitchCityRope Test Catalog - Navigation Index
<!-- Last Updated: 2025-10-10 -->
<!-- Version: 3.0 -->
<!-- Owner: Testing Team -->
<!-- Status: NAVIGATION INDEX - Lightweight file for agent accessibility -->

## 📋 About This Catalog

This is a **navigation index** for the WitchCityRope test catalog. The full catalog is split into manageable parts to stay within token limits for AI agents.

**File Size**: This index is kept under 500 lines to ensure AI agents can read it during startup.

---

## 🗺️ Catalog Structure

### Part 1 (This File): Navigation & Current Tests
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

---

## 🔍 Quick Navigation

### Current Test Status (October 2025)

**Latest Updates**:
- ✅ **Form Button Strict Mode Fix** (2025-10-10): Fixed strict mode violations in profile/dashboard tests by adding .first() to "Save Changes" button selectors
- ✅ **Profile Test Notification Fix** (2025-10-10): Fixed strict mode violation in 5 profile tests by adding .first() to handle multiple notifications
- ✅ **Phase 1 Unit Test Recovery** (2025-10-09): 79% pass rate achieved (was 68%)
- ✅ **AuthHelpers Migration** (2025-10-09): All admin event tests now use proven AuthHelpers.loginAs() pattern
- ✅ **Phase 4 Public Events Tests** (2025-10-09): 17 tests properly skipped (not yet implemented)

**Pass Rate Progress**:
- Previous: 68% (multiple authentication and test isolation issues)
- Current: 79% (AuthHelpers migration, timeout policy enforcement)
- Target: 90%+ (systematic Phase 2-3 test fixes needed)

### Test Categories

#### E2E Tests (Playwright)
**Location**: `/apps/web/tests/playwright/`
**Count**: 44+ spec files
**Status**: Active development with ongoing AuthHelpers migration

**Key Files**:
- `admin-events-workflow-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-navigation-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-detailed-test.spec.ts` - ✅ Fixed with AuthHelpers
- `admin-events-table-ui-check.spec.ts` - ✅ Fixed with AuthHelpers
- `event-session-matrix-test.spec.ts` - ✅ Login fixed (unrelated button issue remains)
- `e2e/tiptap-editors.spec.ts` - ✅ Fixed with AuthHelpers (2025-10-10) - 7/7 tests passing

**Still Using Manual Login** (High Priority to update):
- `events-comprehensive.spec.ts` - Line 261
- `vetting-notes-direct.spec.ts` - Line 11
- `vetting-notes-display.spec.ts` - Line 11

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
cd apps/web/tests/playwright

# Install dependencies (first time only)
npm install

# Run all E2E tests
npm test

# Run specific test file
npm test admin-events-workflow-test.spec.ts

# Run with UI mode (debugging)
npm test -- --ui
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

### Current Coverage
- **E2E Tests**: 44+ Playwright spec files
- **Unit Tests**: Core domain logic and API services
- **Integration Tests**: Full database integration with PostgreSQL

### Target Coverage
- **Pass Rate**: 90%+ (current: 79%)
- **Critical Paths**: 100% coverage for authentication, events, payments
- **Performance**: All tests < 90 seconds timeout

---

## 🗂️ For More Information

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

---

*This is a navigation index only. For detailed test information, see Part 2 and Part 3.*
*For current test execution, see CURRENT_TEST_STATUS.md*
*For testing standards, see TESTING_GUIDE.md*
