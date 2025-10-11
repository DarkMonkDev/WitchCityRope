# WitchCityRope Test Audit Report
<!-- Date: 2025-10-10 -->
<!-- Author: test-developer agent -->
<!-- Purpose: Complete audit of all test files to address 520+ vs 89 documented discrepancy -->

## Executive Summary

**CRITICAL FINDING**: The TEST_CATALOG.md currently documents only 89 Playwright E2E tests, but the repository contains **271 total test files** across multiple test types and frameworks.

**Action Required**: Determine catalog scope and create comprehensive documentation for all test types.

---

## Total Test File Inventory

### Grand Total: 271 Test Files
*(Excluding build artifacts in bin/obj directories)*

| Test Type | Count | Documented? | Location |
|-----------|-------|-------------|----------|
| **E2E Playwright Tests** | 89 | ✅ YES (TEST_CATALOG.md) | `/apps/web/tests/playwright/` |
| **React Unit Tests** | 20 | ❌ NO | `/apps/web/src/**/*.test.tsx` |
| **C# Backend Tests** | 91 | ❌ NO | `/tests/**/*Tests.cs` |
| **Other Test Files** | 71 | ❌ NO | Various (helpers, fixtures) |
| **TOTAL** | **271** | **33% Coverage** | - |

---

## Detailed Breakdown by Test Category

### 1. E2E Playwright Tests (89 files) ✅ DOCUMENTED

**Location**: `/apps/web/tests/playwright/`
**Status**: FULLY documented in TEST_CATALOG.md (Part 1)
**Framework**: Playwright (TypeScript)
**Pass Rate**: 68.1% (243/357 tests passing - Oct 10, 2025)

**Categories**:
- Admin tests: 4 files (events management)
- Auth/Login tests: 20+ files (authentication flows)
- Dashboard tests: 5+ files (user dashboard, profile, settings)
- Events tests: 15+ files (creation, editing, display, RSVP)
- Vetting tests: 10+ files (application workflow, admin review)
- Diagnostic tests: 15+ files (debugging, verification)
- E2E subdirectory: 6 files (admin/vetting, dashboard, tiptap)

**Notable Files**:
- `admin-events-workflow-test.spec.ts`
- `events-comprehensive.spec.ts`
- `vetting-complete-flow.spec.ts`
- `e2e/tiptap-editors.spec.ts`

---

### 2. React Unit Tests (20 files) ❌ NOT DOCUMENTED

**Location**: `/apps/web/src/**/*.test.tsx` and `*.test.ts`
**Status**: NOT documented in any catalog
**Framework**: Vitest + React Testing Library
**Purpose**: Component unit tests, hook tests, integration tests

**Complete File List**:
1. `/apps/web/src/features/auth/api/__tests__/mutations.test.tsx`
2. `/apps/web/src/features/admin/vetting/components/__tests__/VettingApplicationsList.test.tsx`
3. `/apps/web/src/features/vetting/types/vettingStatus.test.ts`
4. `/apps/web/src/features/vetting/hooks/useMenuVisibility.test.tsx`
5. `/apps/web/src/features/vetting/hooks/useVettingStatus.test.tsx`
6. `/apps/web/src/features/vetting/components/VettingStatusBox.test.tsx`
7. `/apps/web/src/features/vetting/pages/VettingApplicationPage.test.tsx`
8. `/apps/web/src/stores/__tests__/authStore.test.ts`
9. `/apps/web/src/components/events/__tests__/EventSessionForm.test.tsx`
10. `/apps/web/src/components/layout/Navigation.test.tsx`
11. `/apps/web/src/components/__tests__/EventsList.test.tsx`
12. `/apps/web/src/pages/dashboard/__tests__/EventsPage.test.tsx`
13. `/apps/web/src/pages/dashboard/__tests__/SecurityPage.test.tsx`
14. `/apps/web/src/pages/dashboard/__tests__/DashboardPage.test.tsx`
15. `/apps/web/src/pages/dashboard/__tests__/ProfilePage.test.tsx`
16. `/apps/web/src/pages/dashboard/__tests__/MembershipPage.test.tsx`
17. `/apps/web/src/pages/admin/EventsManagementApiDemo.test.tsx`
18. `/apps/web/src/test/integration/auth-flow-simplified.test.tsx`
19. `/apps/web/src/test/integration/msw-verification.test.ts`
20. `/apps/web/src/test/integration/dashboard-integration.test.tsx`

**Categories by Feature**:
- Vetting: 6 files (components, hooks, pages, types)
- Dashboard: 5 files (pages)
- Events: 2 files (components)
- Auth: 2 files (mutations, store)
- Integration: 3 files (auth flow, MSW, dashboard)
- Layout: 1 file (navigation)
- Admin: 1 file (events management demo)

---

### 3. C# Backend Tests (91 files) ❌ NOT DOCUMENTED

**Total**: 91 *Tests.cs files across multiple projects
**Status**: NOT documented in any catalog
**Framework**: xUnit + Moq + FluentAssertions

#### 3.1 Active Test Projects

##### WitchCityRope.Core.Tests (11 files)
**Purpose**: Domain logic, business rules, entities
**Location**: `/tests/WitchCityRope.Core.Tests/`

Key Files:
- `Features/Authentication/AuthenticationServiceTests.cs`
- `Features/Events/EventServiceTests.cs`
- `Features/Events/EventServiceSessionManagementTests.cs`
- `Features/Events/EventServiceOrganizerManagementTests.cs`
- `Features/Health/HealthServiceTests.cs`
- `Entities.disabled/EventTests.cs` (disabled)
- `Entities.disabled/RegistrationTests.cs` (disabled)
- `Entities.disabled/UserTests.cs` (disabled)
- `ValueObjects.disabled/EmailAddressTests.cs` (disabled)
- `ValueObjects.disabled/MoneyTests.cs` (disabled)
- `ValueObjects.disabled/SceneNameTests.cs` (disabled)

##### WitchCityRope.Infrastructure.Tests (15 files)
**Purpose**: Data access, external services, infrastructure concerns
**Location**: `/tests/WitchCityRope.Infrastructure.Tests/`

Key Files:
- **Data Layer** (5 files):
  - `Data/WitchCityRopeDbContextTests.cs`
  - `Data/EntityConfigurationTests.cs`
  - `Data/MigrationTests.cs`
  - `Data/ComplexQueryTests.cs`
  - `Data/ConcurrencyTests.cs`

- **PayPal Integration** (6 files):
  - `PayPal/PayPalServiceTests.cs`
  - `PayPal/PayPalConfigurationTests.cs`
  - `PayPal/RealPayPalSandboxTests.cs`
  - `PayPal/PayPalCiCdIntegrationTests.cs`
  - `PayPal/MockPayPalServiceIntegrationTests.cs`
  - `PayPal/WebhookEndpointTests.cs`

- **Security** (2 files):
  - `Security/EncryptionServiceTests.cs`
  - `Security/JwtTokenServiceTests.cs`

- **Email** (1 file):
  - `Email/EmailServiceTests.cs`

- **Health Checks** (1 file):
  - `HealthChecks/ServiceHealthCheckTests.cs`

##### WitchCityRope.Api.Tests (4 files)
**Purpose**: API service layer tests
**Location**: `/tests/WitchCityRope.Api.Tests/`

Files:
- `Integration/PaymentWorkflowIntegrationTests.cs`
- `Services/MockPayPalServiceTests.cs`
- `Services/PaymentServiceTests.cs`
- `Services/RefundServiceTests.cs`

##### WitchCityRope.E2E.Tests (6 files)
**Purpose**: C#-based E2E tests (may be legacy - Playwright is primary)
**Location**: `/tests/WitchCityRope.E2E.Tests/`

Files:
- `Tests/Authentication/AuthenticationFlowTests.cs`
- `Tests/Authentication/TwoFactorAuthTests.cs`
- `Tests/UserJourneys/EventRegistrationFlowTests.cs`
- `Tests/UserJourneys/UserRegistrationFlowTests.cs`
- `Tests/UserJourneys/VettingApplicationFlowTests.cs`
- `Tests/Visual/VisualRegressionTests.cs`

##### Integration Tests - New Structure (5 files)
**Purpose**: Integration tests in new flat structure
**Location**: `/tests/integration/`

Files:
- `Phase2ValidationIntegrationTests.cs`
- `DtoValidation/AllDtosMappingTests.cs`
- `Dashboard/ProfileUpdateDtoMappingTests.cs`
- `api/Features/Participation/ParticipationEndpointsAccessControlTests.cs`
- `api/Features/Vetting/VettingEndpointsIntegrationTests.cs`

##### Unit Tests - New Structure (17 files)
**Purpose**: Unit tests in new flat structure
**Location**: `/tests/unit/api/`

**By Feature Area**:
- **Services** (6 files):
  - `Services/DatabaseInitializationHealthCheckTests.cs`
  - `Services/DatabaseInitializationServiceTests.cs`
  - `Services/MockPayPalServiceTests.cs`
  - `Services/PaymentServiceTests.cs`
  - `Services/RefundServiceTests.cs`
  - `Services/SeedDataServiceTests.cs`

- **Vetting** (6 files):
  - `Features/Vetting/VettingEndpointsTests.cs`
  - `Features/Vetting/VettingServiceTests.cs`
  - `Features/Vetting/Services/VettingAccessControlServiceTests.cs`
  - `Features/Vetting/Services/VettingEmailServiceTests.cs`
  - `Features/Vetting/Services/VettingPublicServiceTests.cs`
  - `Features/Vetting/Services/VettingServiceStatusChangeTests.cs`

- **Other Features** (5 files):
  - `Features/Dashboard/UserDashboardProfileServiceTests.cs`
  - `Features/Participation/ParticipationServiceTests.cs`
  - `Features/Safety/SafetyServiceTests.cs`
  - `Features/Users/UserManagementServiceTests.cs`
  - `Integration/PaymentWorkflowIntegrationTests.cs`

##### System Tests (1 file)
**Location**: `/tests/WitchCityRope.SystemTests/`
- `SystemHealthCheckTests.cs`

##### Performance Tests
**Location**: `/tests/WitchCityRope.PerformanceTests/`
**Note**: Contains k6 scripts and C# infrastructure
Files:
- `LoadTests/AuthenticationLoadTests.cs`
- `LoadTests/EventsLoadTests.cs`
- `StressTests/SystemStressTests.cs`

#### 3.2 Legacy/Obsolete Test Projects (29+ files)

##### WitchCityRope.Api.Tests.legacy-obsolete (13 files)
**Status**: Legacy - marked as obsolete
**Location**: `/tests/WitchCityRope.Api.Tests.legacy-obsolete/`

Files:
- `Models/RequestModelValidationTests.cs`
- `Validators/SubmitApplicationCommandValidatorTests.cs`
- **Services** (6 files):
  - `Services/AuthServiceTests.cs`
  - `Services/EventServiceTests.cs`
  - `Services/PaymentServiceTests.cs`
  - `Services/UserServiceTests.cs`
  - `Services/VettingServiceTests.cs`
  - `Services/ConcurrencyAndEdgeCaseTests.cs`
- **Features** (5 files):
  - `Features/Auth/LoginCommandTests.cs`
  - `Features/Events/CreateEventCommandTests.cs`
  - `Features/Events/EventSessionTests.cs`
  - `Features/Events/EventsManagementServiceTests.cs`
  - `Features/Events/UpdateEventValidationTests.cs`

##### WitchCityRope.IntegrationTests.blazor-obsolete (8 files)
**Status**: Legacy - Blazor migration to React
**Location**: `/tests/WitchCityRope.IntegrationTests.blazor-obsolete/`

Files:
- `AuthenticationTests.cs`
- `BasicSetupTests.cs`
- `BlazorNavigationTests.cs`
- `DeepLinkValidationTests.cs`
- `HtmlNavigationTests.cs`
- `LoginNavigationTests.cs`
- `NavigationLinksTests.cs`
- `SimpleNavigationTests.cs`

##### WitchCityRope.IntegrationTests.disabled (8 files)
**Status**: Disabled
**Location**: `/tests/WitchCityRope.IntegrationTests.disabled/`

Same files as blazor-obsolete (duplicate/backup?)

#### 3.3 Test Infrastructure Projects (0 test files)

##### WitchCityRope.Tests.Common
**Purpose**: Shared test utilities, builders, fixtures
**Location**: `/tests/WitchCityRope.Tests.Common/`
**Contains**: Helper classes, test builders, base classes - NO test files

---

## Analysis: The 520+ Number Explained

### Where Did 520+ Come From?

When counting ALL `.cs` files in test projects (not just `*Tests.cs`):
```bash
find /tests -name "*.cs" | grep -v bin | grep -v obj | wc -l
# Result: ~520 files
```

This includes:
- **Test files** (*Tests.cs): 91 files
- **Helper classes**: ~100 files
- **Test builders**: ~50 files
- **Fixtures**: ~30 files
- **Page objects** (E2E): ~40 files
- **Base classes**: ~20 files
- **Configuration**: ~15 files
- **Test data classes**: ~50 files
- **Mock implementations**: ~30 files
- **Extensions**: ~20 files
- **Other infrastructure**: ~74 files

**Reality**: 91 C# test files, not 520. The rest are supporting infrastructure.

---

## Documentation Gap Analysis

### What IS Documented (33% coverage)

✅ **E2E Playwright Tests**: Full documentation in TEST_CATALOG.md
- All 89 spec files listed
- Pass rates tracked
- Recent updates documented
- Migration to AuthHelpers tracked

### What IS NOT Documented (67% missing)

❌ **React Unit Tests** (20 files):
- No catalog entry
- No description of what they test
- No pass/fail tracking
- No documentation of coverage areas

❌ **C# Backend Tests** (91 files):
- No catalog entry
- No organization by purpose
- No pass/fail tracking
- No distinction between active/legacy tests
- No documentation of test categories

❌ **Test Infrastructure**:
- Builders and helpers not cataloged
- Shared test utilities not documented
- Fixtures not listed

---

## Recommendations

### Option 1: Expand TEST_CATALOG.md (RECOMMENDED)

**Pros**:
- Single source of truth for all tests
- Easier to maintain
- Better discoverability

**Cons**:
- Large file (may need multi-part structure like lessons-learned)

**Implementation**:
```
TEST_CATALOG.md (Part 1) - Navigation + E2E Tests
TEST_CATALOG_BACKEND.md (Part 2) - C# Backend Tests
TEST_CATALOG_FRONTEND.md (Part 3) - React Unit Tests
TEST_CATALOG_INFRASTRUCTURE.md (Part 4) - Helpers/Builders/Fixtures
```

### Option 2: Separate Catalogs by Test Type

**Pros**:
- Smaller, focused files
- Different maintainers for different test types

**Cons**:
- Multiple sources of truth
- Harder to get complete picture
- More maintenance overhead

**Implementation**:
```
/docs/standards-processes/testing/
  ├── TEST_CATALOG_E2E.md (Playwright tests)
  ├── TEST_CATALOG_REACT.md (React unit tests)
  ├── TEST_CATALOG_BACKEND.md (C# tests)
  └── TEST_CATALOG_INFRASTRUCTURE.md (Helpers/utilities)
```

### Option 3: Automated Test Discovery (FUTURE)

**Pros**:
- Always accurate
- No manual maintenance
- Generated from actual test files

**Cons**:
- Requires tooling
- No human context/descriptions
- Implementation effort

**Implementation**:
- Script to scan test files
- Extract test names and descriptions
- Generate markdown catalog
- Run in CI/CD pipeline

---

## Immediate Action Items

### Phase 1: Document React Unit Tests (20 files)
**Priority**: HIGH
**Effort**: 2-3 hours
**Owner**: test-developer agent

**Tasks**:
1. Add React Unit Tests section to TEST_CATALOG.md or new part
2. List all 20 test files with descriptions
3. Categorize by feature area
4. Document what each test covers
5. Track pass/fail status

### Phase 2: Document C# Backend Tests (91 files)
**Priority**: HIGH
**Effort**: 4-6 hours
**Owner**: test-developer agent + backend-developer agent

**Tasks**:
1. Create TEST_CATALOG_BACKEND.md or Part 2
2. List all active test projects with file counts
3. Document purpose of each test project
4. List key test files in each project
5. Mark legacy/obsolete tests clearly
6. Track pass/fail status by project

### Phase 3: Document Test Infrastructure
**Priority**: MEDIUM
**Effort**: 2-3 hours
**Owner**: test-developer agent

**Tasks**:
1. Create TEST_CATALOG_INFRASTRUCTURE.md or Part 3
2. Document test builders and their purpose
3. Document fixtures and helpers
4. Document shared test utilities
5. Create usage examples

### Phase 4: Establish Maintenance Process
**Priority**: HIGH
**Effort**: 1 hour
**Owner**: librarian agent

**Tasks**:
1. Add test catalog update to test creation workflow
2. Require catalog updates in PR template
3. Add catalog validation to CI/CD
4. Create test catalog maintenance guide

---

## Current Test Catalog Scope

### TEST_CATALOG.md Current Coverage:
- **Part 1**: Navigation + Current E2E Tests (89 files) ✅
- **Part 2**: Historical test documentation ✅
- **Part 3**: Archived test information ✅

### Proposed Expansion:
- **Part 4**: React Unit Tests (20 files) ❌ TO BE ADDED
- **Part 5**: C# Backend Tests (91 files) ❌ TO BE ADDED
- **Part 6**: Test Infrastructure ❌ TO BE ADDED

OR

### Alternative Structure:
- **TEST_CATALOG.md**: Navigation index only
- **TEST_CATALOG_E2E.md**: Playwright tests (current Part 1-3)
- **TEST_CATALOG_REACT.md**: React unit tests (NEW)
- **TEST_CATALOG_BACKEND.md**: C# backend tests (NEW)
- **TEST_CATALOG_INFRASTRUCTURE.md**: Helpers/builders (NEW)

---

## Conclusion

**Finding**: TEST_CATALOG.md covers only 33% of test files (89 of 271)

**Root Cause**: Catalog was created for E2E tests only, never expanded to cover full test suite

**Impact**:
- 182 test files undocumented
- No visibility into React unit test coverage
- No visibility into backend test coverage
- New developers don't know what tests exist

**Recommendation**: Expand TEST_CATALOG to multi-part structure covering all test types

**Next Steps**:
1. User decision on catalog structure (Option 1, 2, or 3)
2. Phase 1: Document React unit tests
3. Phase 2: Document C# backend tests
4. Phase 3: Document test infrastructure
5. Phase 4: Establish maintenance process

---

## Appendix: Test File Locations Reference

### Complete Test Directory Structure
```
/home/chad/repos/witchcityrope/
├── apps/web/
│   ├── src/**/*.test.tsx          # React unit tests (20 files)
│   └── tests/playwright/          # E2E Playwright (89 files)
└── tests/
    ├── WitchCityRope.Core.Tests/          # Domain tests (11 files)
    ├── WitchCityRope.Infrastructure.Tests/ # Infrastructure (15 files)
    ├── WitchCityRope.Api.Tests/            # API tests (4 files)
    ├── WitchCityRope.E2E.Tests/            # C# E2E (6 files)
    ├── WitchCityRope.SystemTests/          # System tests (1 file)
    ├── WitchCityRope.PerformanceTests/     # Performance (3+ files)
    ├── integration/                        # Integration (5 files)
    ├── unit/api/                           # Unit tests (17 files)
    ├── WitchCityRope.Tests.Common/         # Infrastructure (0 test files)
    ├── WitchCityRope.Api.Tests.legacy-obsolete/      # Legacy (13 files)
    ├── WitchCityRope.IntegrationTests.blazor-obsolete/ # Legacy (8 files)
    └── WitchCityRope.IntegrationTests.disabled/        # Disabled (8 files)
```

---

*End of Report*
