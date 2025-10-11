# WitchCityRope Test Catalog - Part 4: Complete Test File Listings
<!-- Last Updated: 2025-10-10 -->
<!-- Version: 1.0 -->
<!-- Owner: Testing Team -->
<!-- Purpose: Comprehensive listing of all 271 test files across all test types -->

## 📋 Navigation

**This is Part 4 of the Test Catalog** - Complete test file listings

**Back to Navigation**: [Part 1 - TEST_CATALOG.md](./TEST_CATALOG.md)

---

## 📊 Test Inventory Summary

### Total Test Files: 271
*(Excludes build artifacts in bin/obj directories)*

| Test Type | Count | Status | Framework |
|-----------|-------|--------|-----------|
| **E2E Playwright Tests** | 89 | ✅ Active | Playwright + TypeScript |
| **React Unit Tests** | 20 | ✅ Active | Vitest + React Testing Library |
| **C# Backend Tests (Active)** | 56 | ✅ Active | xUnit + Moq + FluentAssertions |
| **C# Integration Tests** | 5 | ✅ Active | xUnit + TestContainers |
| **C# Performance Tests** | 3 | ✅ Active | xUnit + k6 |
| **C# System Tests** | 1 | ✅ Active | xUnit |
| **Legacy/Obsolete Tests** | 29+ | ⚠️ Legacy | xUnit (disabled/obsolete) |
| **Test Infrastructure** | 71+ | 📚 Support | Helpers, Fixtures, Builders |
| **TOTAL** | **271** | - | - |

---

## 🎭 E2E Playwright Tests (89 files)

**Location**: `/apps/web/tests/playwright/`
**Framework**: Playwright + TypeScript
**Status**: 68.1% passing (243/357 tests) - October 10, 2025
**Recent**: AuthHelpers migration 100% complete (2025-10-10)

### Admin Tests (4 files)

1. **admin-events-workflow-test.spec.ts**
   - Purpose: Complete admin event management workflow
   - Status: ✅ Using AuthHelpers
   - Tests: Event creation, editing, deletion workflow

2. **admin-events-navigation-test.spec.ts**
   - Purpose: Admin event navigation and routing
   - Status: ✅ Using AuthHelpers
   - Tests: Navigation between admin event views

3. **admin-events-detailed-test.spec.ts**
   - Purpose: Detailed admin event functionality
   - Status: ✅ Using AuthHelpers
   - Tests: Complex event operations

4. **admin-events-table-ui-check.spec.ts**
   - Purpose: Admin events table UI verification
   - Status: ✅ Using AuthHelpers
   - Tests: Table rendering, sorting, filtering

### Auth/Login Tests (20+ files)

5. **auth-complete-flows-simple.spec.ts**
   - Purpose: Complete authentication flows
   - Tests: Login, logout, session persistence

6. **auth-flow-test.spec.ts**
   - Purpose: Authentication workflow verification
   - Tests: Login/logout sequences

7. **auth-test-enhanced.spec.ts**
   - Purpose: Enhanced authentication testing
   - Tests: Error handling, validation

8. **auth-test-fixed.spec.ts**
   - Purpose: Fixed authentication test patterns
   - Tests: Corrected auth flows

9. **login-comprehensive.spec.ts**
   - Purpose: Comprehensive login testing
   - Tests: Valid/invalid credentials, error messages

10. **login-fail-check.spec.ts**
    - Purpose: Login failure scenarios
    - Tests: Error handling, validation messages

11. **login-form-check.spec.ts**
    - Purpose: Login form validation
    - Tests: Form fields, validation rules

12. **login-form-test.spec.ts**
    - Purpose: Login form functionality
    - Tests: Form submission, field validation

13. **login-navigation.spec.ts**
    - Purpose: Login page navigation
    - Tests: Routing to/from login page

14. **login-redirect-test.spec.ts**
    - Purpose: Post-login redirect behavior
    - Tests: Redirect after successful login

15. **login-session-check.spec.ts**
    - Purpose: Session management verification
    - Tests: Session persistence, expiration

16. **login-strict.spec.ts**
    - Purpose: Strict mode login testing
    - Tests: Strict validation rules

17. **login-test-basic.spec.ts**
    - Purpose: Basic login functionality
    - Tests: Simple login/logout flows

18. **login-test-comprehensive.spec.ts**
    - Purpose: Comprehensive login coverage
    - Tests: All login scenarios

19. **login-test-simple.spec.ts**
    - Purpose: Simplified login tests
    - Tests: Core login functionality

20. **login-test-stable.spec.ts**
    - Purpose: Stable login test suite
    - Tests: Reliable login patterns

21. **logout-test.spec.ts**
    - Purpose: Logout functionality
    - Tests: Session termination, cleanup

22. **session-test.spec.ts**
    - Purpose: Session management
    - Tests: Session lifecycle

23. **simple-login-test.spec.ts**
    - Purpose: Simple login verification
    - Tests: Basic login flow

24. **simple-login-verify.spec.ts**
    - Purpose: Login verification checks
    - Tests: Post-login state validation

### Dashboard Tests (5+ files)

25. **dashboard-all-tabs-test.spec.ts**
    - Purpose: Dashboard tab navigation
    - Tests: All dashboard tab views

26. **dashboard-quick.spec.ts**
    - Purpose: Quick dashboard checks
    - Tests: Dashboard load, basic functionality

27. **dashboard-test.spec.ts**
    - Purpose: Main dashboard testing
    - Tests: Dashboard components, data display

28. **profile-update-full-persistence.spec.ts**
    - Purpose: Profile update with full persistence verification
    - Status: ✅ Migrated to unique users (Oct 2025)
    - Tests: 14 tests with database verification
    - Features: Unique user creation, cleanup

29. **profile-update-persistence.spec.ts**
    - Purpose: Profile update persistence
    - Status: ✅ Migrated to unique users (Oct 2025)
    - Tests: 2 tests with database verification

### Events Tests (15+ files)

30. **e2e-events-full-journey.spec.ts**
    - Purpose: Complete event user journey
    - Tests: Event discovery, RSVP, attendance

31. **event-session-matrix-test.spec.ts**
    - Purpose: Event session matrix functionality
    - Status: ✅ Using AuthHelpers
    - Tests: Multi-session events, scheduling

32. **events-actual-routes-test.spec.ts**
    - Purpose: Event routing verification
    - Tests: Actual route navigation

33. **events-actual-test.spec.ts**
    - Purpose: Event functionality testing
    - Tests: Real event operations

34. **events-all-pages.spec.ts**
    - Purpose: All event page coverage
    - Tests: All event-related pages

35. **events-api-test.spec.ts**
    - Purpose: Event API integration
    - Tests: API endpoint responses

36. **events-comprehensive.spec.ts**
    - Purpose: Comprehensive event testing
    - Status: ✅ Migrated to AuthHelpers
    - Tests: All event operations

37. **events-crud-test.spec.ts**
    - Purpose: Event CRUD operations
    - Tests: Create, read, update, delete events

38. **events-deep-test.spec.ts**
    - Purpose: Deep event functionality
    - Tests: Complex event scenarios

39. **events-detailed.spec.ts**
    - Purpose: Detailed event testing
    - Tests: Event details, metadata

40. **events-enhanced-api.spec.ts**
    - Purpose: Enhanced event API testing
    - Tests: Advanced API operations

41. **events-enhanced.spec.ts**
    - Purpose: Enhanced event features
    - Tests: Extended event functionality

42. **events-full.spec.ts**
    - Purpose: Full event coverage
    - Tests: Complete event lifecycle

43. **events-list-display.spec.ts**
    - Purpose: Event list rendering
    - Tests: Event list UI, filtering

44. **events-load-check.spec.ts**
    - Purpose: Event loading verification
    - Tests: Event data loading

### Vetting Tests (10+ files)

45. **vetting-admin-comprehensive.spec.ts**
    - Purpose: Admin vetting comprehensive tests
    - Tests: 6 tests for admin vetting workflow
    - Features: Dashboard, detail view, actions

46. **vetting-admin-detail-enhanced.spec.ts**
    - Purpose: Enhanced admin vetting detail view
    - Tests: 6 tests for vetting detail operations

47. **vetting-admin-workflow-complete.spec.ts**
    - Purpose: Complete admin vetting workflow
    - Tests: 7 tests for full vetting process

48. **vetting-complete-flow.spec.ts**
    - Purpose: Complete vetting flow testing
    - Tests: End-to-end vetting process

49. **vetting-notes-direct.spec.ts**
    - Purpose: Vetting notes direct testing
    - Status: ✅ Using AuthHelpers
    - Tests: Admin notes functionality

50. **vetting-notes-display.spec.ts**
    - Purpose: Vetting notes display
    - Status: ✅ Using AuthHelpers
    - Tests: Notes rendering, formatting

51. **vetting-status-flow.spec.ts**
    - Purpose: Vetting status workflow
    - Tests: Status transitions, validation

52. **vetting-test-simple.spec.ts**
    - Purpose: Simple vetting tests
    - Tests: Basic vetting functionality

53. **vetting-workflow-test.spec.ts**
    - Purpose: Vetting workflow testing
    - Tests: Workflow steps, transitions

54. **simple-vetting-test.spec.ts**
    - Purpose: Simplified vetting tests
    - Tests: Core vetting features

### Diagnostic/Verification Tests (15+ files)

55. **simple-rebuild-verification.spec.ts**
    - Purpose: Verify admin dashboard loads after rebuild
    - Status: ✅ New (2025-10-10)
    - Tests: Post-rebuild functionality

56. **verify-recent-changes.spec.ts**
    - Purpose: Verify vetting workflow changes
    - Status: ✅ New (2025-10-10)
    - Tests: Notes, badges, counts

57. **verify-vetting-status-fix.spec.ts**
    - Purpose: Verify VettingStatus enum alignment
    - Status: ✅ New (2025-10-10)
    - Tests: Enum consistency with shared-types

58. **admin-test-minimal.spec.ts**
    - Purpose: Minimal admin testing
    - Tests: Basic admin functionality

59. **basic-test.spec.ts**
    - Purpose: Basic functionality checks
    - Tests: Core app features

60. **dashboard-smoke-test.spec.ts**
    - Purpose: Dashboard smoke testing
    - Tests: Quick dashboard verification

61. **debug-comprehensive-checks.spec.ts**
    - Purpose: Comprehensive debugging checks
    - Tests: Debug scenarios

62. **debug-simple-api-test.spec.ts**
    - Purpose: Simple API debugging
    - Tests: API endpoint verification

63. **debug-test.spec.ts**
    - Purpose: Debug test utilities
    - Tests: Debugging helpers

64. **form-interaction-test.spec.ts**
    - Purpose: Form interaction testing
    - Tests: Form fields, submission

65. **page-load-test.spec.ts**
    - Purpose: Page load verification
    - Tests: Page loading, rendering

66. **quick-check.spec.ts**
    - Purpose: Quick verification checks
    - Tests: Rapid functionality checks

67. **simple-test.spec.ts**
    - Purpose: Simple test patterns
    - Tests: Basic test cases

68. **smoke-test.spec.ts**
    - Purpose: Smoke testing
    - Tests: Critical path verification

69. **timeout-test.spec.ts**
    - Purpose: Timeout behavior testing
    - Tests: Timeout scenarios, handling

### Navigation/Routing Tests (5+ files)

70. **dashboard-navigation.spec.ts**
    - Purpose: Dashboard navigation testing
    - Tests: Dashboard routing

71. **events-navigation.spec.ts**
    - Purpose: Event navigation testing
    - Tests: Event page routing

72. **navigation-test.spec.ts**
    - Purpose: General navigation testing
    - Tests: App navigation, routing

73. **redirect-test.spec.ts**
    - Purpose: Redirect behavior testing
    - Tests: Redirect scenarios

74. **route-test.spec.ts**
    - Purpose: Route verification
    - Tests: Route definitions, navigation

### UI Component Tests (5+ files)

75. **button-test.spec.ts**
    - Purpose: Button component testing
    - Tests: Button interactions, states

76. **element-visibility-test.spec.ts**
    - Purpose: Element visibility checks
    - Tests: Visibility detection

77. **form-test.spec.ts**
    - Purpose: Form component testing
    - Tests: Form functionality

78. **ui-test.spec.ts**
    - Purpose: General UI testing
    - Tests: UI components, rendering

79. **wait-strategies-test.spec.ts**
    - Purpose: Wait strategy testing
    - Tests: Wait conditions, timing

### E2E Subdirectory Tests (6 files)

**Location**: `/apps/web/tests/playwright/e2e/`

80. **e2e/admin-vetting-workflow.spec.ts**
    - Purpose: Admin vetting workflow (E2E)
    - Tests: Complete admin vetting process

81. **e2e/dashboard-integration.spec.ts**
    - Purpose: Dashboard integration testing
    - Tests: Dashboard component integration

82. **e2e/tiptap-editors.spec.ts**
    - Purpose: TipTap editor testing
    - Status: ✅ Using AuthHelpers - 7/7 tests passing
    - Tests: Rich text editor functionality

83. **e2e/vetting-admin-only.spec.ts**
    - Purpose: Admin-only vetting tests
    - Tests: Admin vetting permissions

84. **e2e/vetting-full-workflow.spec.ts**
    - Purpose: Full vetting workflow (E2E)
    - Tests: Complete vetting lifecycle

85. **e2e/vetting-user-journey.spec.ts**
    - Purpose: User vetting journey
    - Tests: User perspective vetting flow

### Miscellaneous E2E Tests (4 files)

86. **api-test.spec.ts**
    - Purpose: General API testing
    - Tests: API endpoints, responses

87. **registration-test.spec.ts**
    - Purpose: User registration testing
    - Tests: Registration flow, validation

88. **user-flow-test.spec.ts**
    - Purpose: User flow testing
    - Tests: User journeys, workflows

89. **validation-test.spec.ts**
    - Purpose: Validation testing
    - Tests: Form validation, error messages

---

## ⚛️ React Unit Tests (20 files)

**Location**: `/apps/web/src/**/*.test.tsx` and `*.test.ts`
**Framework**: Vitest + React Testing Library + MSW
**Status**: Active - Component and integration testing

### Vetting Feature Tests (6 files)

1. **features/vetting/types/vettingStatus.test.ts**
   - Purpose: VettingStatus type utilities testing
   - Tests: Status conversion, validation
   - Location: `/apps/web/src/features/vetting/types/`

2. **features/vetting/hooks/useMenuVisibility.test.tsx**
   - Purpose: Menu visibility hook testing
   - Tests: Conditional menu rendering logic
   - Location: `/apps/web/src/features/vetting/hooks/`

3. **features/vetting/hooks/useVettingStatus.test.tsx**
   - Purpose: Vetting status hook testing
   - Tests: Status management, transitions
   - Location: `/apps/web/src/features/vetting/hooks/`

4. **features/vetting/components/VettingStatusBox.test.tsx**
   - Purpose: VettingStatusBox component testing
   - Tests: Status display, styling
   - Location: `/apps/web/src/features/vetting/components/`

5. **features/vetting/pages/VettingApplicationPage.test.tsx**
   - Purpose: Vetting application page testing
   - Tests: Page rendering, form submission
   - Location: `/apps/web/src/features/vetting/pages/`

6. **features/admin/vetting/components/__tests__/VettingApplicationsList.test.tsx**
   - Purpose: Admin vetting applications list testing
   - Tests: List rendering, filtering, sorting
   - Location: `/apps/web/src/features/admin/vetting/components/__tests__/`

### Dashboard Page Tests (5 files)

7. **pages/dashboard/__tests__/DashboardPage.test.tsx**
   - Purpose: Main dashboard page testing
   - Tests: Dashboard layout, widgets
   - Location: `/apps/web/src/pages/dashboard/__tests__/`

8. **pages/dashboard/__tests__/EventsPage.test.tsx**
   - Purpose: Events dashboard page testing
   - Tests: Event list, filtering
   - Location: `/apps/web/src/pages/dashboard/__tests__/`

9. **pages/dashboard/__tests__/MembershipPage.test.tsx**
   - Purpose: Membership dashboard page testing
   - Tests: Membership info, upgrade options
   - Location: `/apps/web/src/pages/dashboard/__tests__/`

10. **pages/dashboard/__tests__/ProfilePage.test.tsx**
    - Purpose: Profile dashboard page testing
    - Tests: Profile editing, validation
    - Location: `/apps/web/src/pages/dashboard/__tests__/`

11. **pages/dashboard/__tests__/SecurityPage.test.tsx**
    - Purpose: Security dashboard page testing
    - Tests: Password change, security settings
    - Location: `/apps/web/src/pages/dashboard/__tests__/`

### Event Component Tests (2 files)

12. **components/events/__tests__/EventSessionForm.test.tsx**
    - Purpose: Event session form testing
    - Tests: Session creation, editing
    - Location: `/apps/web/src/components/events/__tests__/`

13. **components/__tests__/EventsList.test.tsx**
    - Purpose: Events list component testing
    - Tests: Event rendering, filtering
    - Location: `/apps/web/src/components/__tests__/`

### Auth Tests (2 files)

14. **features/auth/api/__tests__/mutations.test.tsx**
    - Purpose: Auth mutation testing (TanStack Query)
    - Tests: Login, logout, register mutations
    - Location: `/apps/web/src/features/auth/api/__tests__/`

15. **stores/__tests__/authStore.test.ts**
    - Purpose: Auth store testing (Zustand)
    - Tests: Auth state management
    - Location: `/apps/web/src/stores/__tests__/`

### Integration Tests (3 files)

16. **test/integration/auth-flow-simplified.test.tsx**
    - Purpose: Simplified auth flow integration testing
    - Tests: Complete auth workflow with MSW
    - Location: `/apps/web/src/test/integration/`

17. **test/integration/msw-verification.test.ts**
    - Purpose: MSW mock server verification
    - Tests: MSW setup, handler functionality
    - Location: `/apps/web/src/test/integration/`

18. **test/integration/dashboard-integration.test.tsx**
    - Purpose: Dashboard integration testing
    - Tests: Dashboard with mocked API calls
    - Location: `/apps/web/src/test/integration/`

### Layout/Navigation Tests (1 file)

19. **components/layout/Navigation.test.tsx**
    - Purpose: Navigation component testing
    - Tests: Menu rendering, routing
    - Location: `/apps/web/src/components/layout/`

### Admin Tests (1 file)

20. **pages/admin/EventsManagementApiDemo.test.tsx**
    - Purpose: Admin events management API demo testing
    - Tests: Admin event operations
    - Location: `/apps/web/src/pages/admin/`

---

## 🔧 C# Backend Tests - Active (56 files)

**Framework**: xUnit + Moq + FluentAssertions
**Status**: Active test projects

### WitchCityRope.Core.Tests (5 active files)

**Purpose**: Domain logic, business rules, entities
**Location**: `/tests/WitchCityRope.Core.Tests/`

1. **Features/Authentication/AuthenticationServiceTests.cs**
   - Purpose: Authentication service testing
   - Tests: Login, logout, token validation

2. **Features/Events/EventServiceTests.cs**
   - Purpose: Event service testing
   - Tests: Event creation, editing, deletion

3. **Features/Events/EventServiceSessionManagementTests.cs**
   - Purpose: Event session management testing
   - Tests: Multi-session events, scheduling

4. **Features/Events/EventServiceOrganizerManagementTests.cs**
   - Purpose: Event organizer management testing
   - Tests: Organizer assignment, permissions

5. **Features/Health/HealthServiceTests.cs**
   - Purpose: Health check service testing
   - Tests: System health monitoring

### WitchCityRope.Infrastructure.Tests (15 files)

**Purpose**: Data access, external services, infrastructure
**Location**: `/tests/WitchCityRope.Infrastructure.Tests/`

#### Data Layer Tests (5 files)

6. **Data/WitchCityRopeDbContextTests.cs**
   - Purpose: DbContext functionality testing
   - Tests: Database operations, migrations

7. **Data/EntityConfigurationTests.cs**
   - Purpose: Entity configuration testing
   - Tests: EF Core configurations

8. **Data/MigrationTests.cs**
   - Purpose: Database migration testing
   - Tests: Migration scripts, schema changes

9. **Data/ComplexQueryTests.cs**
   - Purpose: Complex query testing
   - Tests: Advanced LINQ queries

10. **Data/ConcurrencyTests.cs**
    - Purpose: Concurrency handling testing
    - Tests: Optimistic concurrency, conflicts

#### PayPal Integration Tests (6 files)

11. **PayPal/PayPalServiceTests.cs**
    - Purpose: PayPal service testing
    - Tests: Payment processing, refunds

12. **PayPal/PayPalConfigurationTests.cs**
    - Purpose: PayPal configuration testing
    - Tests: Configuration validation

13. **PayPal/RealPayPalSandboxTests.cs**
    - Purpose: Real PayPal sandbox testing
    - Tests: Live sandbox integration

14. **PayPal/PayPalCiCdIntegrationTests.cs**
    - Purpose: PayPal CI/CD integration testing
    - Tests: Automated pipeline testing

15. **PayPal/MockPayPalServiceIntegrationTests.cs**
    - Purpose: Mocked PayPal service testing
    - Tests: PayPal mock integration

16. **PayPal/WebhookEndpointTests.cs**
    - Purpose: PayPal webhook testing
    - Tests: Webhook handling, validation

#### Security Tests (2 files)

17. **Security/EncryptionServiceTests.cs**
    - Purpose: Encryption service testing
    - Tests: Data encryption, decryption

18. **Security/JwtTokenServiceTests.cs**
    - Purpose: JWT token service testing
    - Tests: Token generation, validation

#### Email Tests (1 file)

19. **Email/EmailServiceTests.cs**
    - Purpose: Email service testing
    - Tests: Email sending, templates

#### Health Check Tests (1 file)

20. **HealthChecks/ServiceHealthCheckTests.cs**
    - Purpose: Service health check testing
    - Tests: Health check endpoints

### WitchCityRope.Api.Tests (4 files)

**Purpose**: API service layer testing
**Location**: `/tests/WitchCityRope.Api.Tests/`

21. **Integration/PaymentWorkflowIntegrationTests.cs**
    - Purpose: Payment workflow integration testing
    - Tests: Complete payment flows

22. **Services/MockPayPalServiceTests.cs**
    - Purpose: Mock PayPal service testing
    - Tests: PayPal service mocking

23. **Services/PaymentServiceTests.cs**
    - Purpose: Payment service testing
    - Tests: Payment processing logic

24. **Services/RefundServiceTests.cs**
    - Purpose: Refund service testing
    - Tests: Refund processing, validation

### WitchCityRope.E2E.Tests (6 files)

**Purpose**: C#-based E2E tests (may be legacy - Playwright is primary)
**Location**: `/tests/WitchCityRope.E2E.Tests/`

25. **Tests/Authentication/AuthenticationFlowTests.cs**
    - Purpose: C# authentication flow testing
    - Tests: Login/logout workflows

26. **Tests/Authentication/TwoFactorAuthTests.cs**
    - Purpose: Two-factor authentication testing
    - Tests: 2FA setup, validation

27. **Tests/UserJourneys/EventRegistrationFlowTests.cs**
    - Purpose: Event registration journey testing
    - Tests: User event registration flow

28. **Tests/UserJourneys/UserRegistrationFlowTests.cs**
    - Purpose: User registration journey testing
    - Tests: New user registration flow

29. **Tests/UserJourneys/VettingApplicationFlowTests.cs**
    - Purpose: Vetting application journey testing
    - Tests: Vetting application submission flow

30. **Tests/Visual/VisualRegressionTests.cs**
    - Purpose: Visual regression testing
    - Tests: Screenshot comparison

### New Integration Tests (5 files)

**Purpose**: Integration tests with new structure
**Location**: `/tests/integration/`

31. **Phase2ValidationIntegrationTests.cs**
    - Purpose: Phase 2 validation testing
    - Tests: Integration validation

32. **DtoValidation/AllDtosMappingTests.cs**
    - Purpose: DTO mapping validation
    - Tests: All DTO/Entity mappings

33. **Dashboard/ProfileUpdateDtoMappingTests.cs**
    - Purpose: Profile update DTO testing
    - Tests: Profile DTO mapping

34. **api/Features/Participation/ParticipationEndpointsAccessControlTests.cs**
    - Purpose: Participation endpoint access control
    - Tests: Authorization rules

35. **api/Features/Vetting/VettingEndpointsIntegrationTests.cs**
    - Purpose: Vetting endpoint integration testing
    - Tests: Vetting API integration

### New Unit Tests (17 files)

**Purpose**: Unit tests with new flat structure
**Location**: `/tests/unit/api/`

#### Service Tests (6 files)

36. **Services/DatabaseInitializationHealthCheckTests.cs**
    - Purpose: Database initialization health check testing
    - Tests: Database startup verification

37. **Services/DatabaseInitializationServiceTests.cs**
    - Purpose: Database initialization service testing
    - Tests: Database setup, seeding

38. **Services/MockPayPalServiceTests.cs**
    - Purpose: Mock PayPal service testing
    - Tests: PayPal mocking logic

39. **Services/PaymentServiceTests.cs**
    - Purpose: Payment service unit testing
    - Tests: Payment logic

40. **Services/RefundServiceTests.cs**
    - Purpose: Refund service unit testing
    - Tests: Refund logic
    - Status: ✅ Fixed (Oct 2025) - Entity persistence

41. **Services/SeedDataServiceTests.cs**
    - Purpose: Seed data service testing
    - Tests: Database seeding

#### Vetting Feature Tests (6 files)

42. **Features/Vetting/VettingEndpointsTests.cs**
    - Purpose: Vetting endpoint testing
    - Tests: Vetting API endpoints

43. **Features/Vetting/VettingServiceTests.cs**
    - Purpose: Vetting service testing
    - Tests: Vetting business logic

44. **Features/Vetting/Services/VettingAccessControlServiceTests.cs**
    - Purpose: Vetting access control testing
    - Tests: Vetting permissions

45. **Features/Vetting/Services/VettingEmailServiceTests.cs**
    - Purpose: Vetting email service testing
    - Tests: Vetting notification emails

46. **Features/Vetting/Services/VettingPublicServiceTests.cs**
    - Purpose: Vetting public service testing
    - Status: ✅ New (2025-10-10) - 15 tests
    - Tests: Public vetting operations

47. **Features/Vetting/Services/VettingServiceStatusChangeTests.cs**
    - Purpose: Vetting status change testing
    - Tests: Status transition logic

#### Other Feature Tests (5 files)

48. **Features/Dashboard/UserDashboardProfileServiceTests.cs**
    - Purpose: Dashboard profile service testing
    - Tests: Profile management

49. **Features/Participation/ParticipationServiceTests.cs**
    - Purpose: Participation service testing
    - Tests: Event participation logic

50. **Features/Safety/SafetyServiceTests.cs**
    - Purpose: Safety service testing
    - Tests: Safety monitoring

51. **Features/Users/UserManagementServiceTests.cs**
    - Purpose: User management service testing
    - Tests: User CRUD operations

52. **Integration/PaymentWorkflowIntegrationTests.cs**
    - Purpose: Payment workflow integration testing
    - Tests: Complete payment flows

### System Tests (1 file)

**Location**: `/tests/WitchCityRope.SystemTests/`

53. **SystemHealthCheckTests.cs**
    - Purpose: System-level health check testing
    - Tests: Overall system health

### Performance Tests (3 files)

**Location**: `/tests/WitchCityRope.PerformanceTests/`

54. **LoadTests/AuthenticationLoadTests.cs**
    - Purpose: Authentication load testing
    - Tests: Auth under load

55. **LoadTests/EventsLoadTests.cs**
    - Purpose: Events load testing
    - Tests: Event operations under load

56. **StressTests/SystemStressTests.cs**
    - Purpose: System stress testing
    - Tests: System limits, breaking points

---

## 🗄️ Legacy/Obsolete Tests (29+ files)

**Status**: Legacy - marked as obsolete or disabled
**Purpose**: Historical reference only - NOT ACTIVE

### WitchCityRope.Api.Tests.legacy-obsolete (13 files)

**Location**: `/tests/WitchCityRope.Api.Tests.legacy-obsolete/`
**Status**: Legacy - marked as obsolete

1. **Models/RequestModelValidationTests.cs**
2. **Validators/SubmitApplicationCommandValidatorTests.cs**
3. **Services/AuthServiceTests.cs**
4. **Services/EventServiceTests.cs**
5. **Services/PaymentServiceTests.cs**
6. **Services/UserServiceTests.cs**
7. **Services/VettingServiceTests.cs**
8. **Services/ConcurrencyAndEdgeCaseTests.cs**
9. **Features/Auth/LoginCommandTests.cs**
10. **Features/Events/CreateEventCommandTests.cs**
11. **Features/Events/EventSessionTests.cs**
12. **Features/Events/EventsManagementServiceTests.cs**
13. **Features/Events/UpdateEventValidationTests.cs**

### WitchCityRope.IntegrationTests.blazor-obsolete (8 files)

**Location**: `/tests/WitchCityRope.IntegrationTests.blazor-obsolete/`
**Status**: Legacy - Blazor migration to React

14. **AuthenticationTests.cs**
15. **BasicSetupTests.cs**
16. **BlazorNavigationTests.cs**
17. **DeepLinkValidationTests.cs**
18. **HtmlNavigationTests.cs**
19. **LoginNavigationTests.cs**
20. **NavigationLinksTests.cs**
21. **SimpleNavigationTests.cs**

### WitchCityRope.IntegrationTests.disabled (8 files)

**Location**: `/tests/WitchCityRope.IntegrationTests.disabled/`
**Status**: Disabled

22-29. Same files as blazor-obsolete (duplicate/backup?)

### WitchCityRope.Core.Tests - Disabled Tests (6 files)

**Location**: `/tests/WitchCityRope.Core.Tests/Entities.disabled/` and `/ValueObjects.disabled/`
**Status**: Disabled

30. **Entities.disabled/EventTests.cs**
31. **Entities.disabled/RegistrationTests.cs**
32. **Entities.disabled/UserTests.cs**
33. **ValueObjects.disabled/EmailAddressTests.cs**
34. **ValueObjects.disabled/MoneyTests.cs**
35. **ValueObjects.disabled/SceneNameTests.cs**

---

## 📚 Test Infrastructure (71+ files)

**Purpose**: Shared test utilities, builders, fixtures
**Location**: Various (primarily `/tests/WitchCityRope.Tests.Common/`)
**Status**: Supporting infrastructure - NO test files (helper classes only)

### Categories

1. **Test Builders** (~50 files)
   - User builders, Event builders, Registration builders
   - Purpose: Fluent test data creation

2. **Test Fixtures** (~30 files)
   - Database fixtures, API fixtures
   - Purpose: Test environment setup

3. **Helper Classes** (~20 files)
   - Authentication helpers, Database helpers
   - Purpose: Common test operations

4. **Base Classes** (~20 files)
   - Base test classes, abstract fixtures
   - Purpose: Test inheritance structure

5. **Mock Implementations** (~30 files)
   - Service mocks, Repository mocks
   - Purpose: Test doubles

6. **Extensions** (~20 files)
   - Test extensions, assertion helpers
   - Purpose: Enhanced test functionality

**Note**: Exact file counts vary as infrastructure is shared across projects

---

## 📋 Test Execution Summary

### E2E Tests (Playwright)
```bash
cd /apps/web/tests/playwright
npm test                          # Run all 89 spec files
npm test [filename]               # Run specific test
npm test -- --ui                  # Run with UI mode
```

### React Unit Tests (Vitest)
```bash
cd /apps/web
npm test                          # Run all 20 test files
npm run test:coverage             # With coverage report
npm test -- [filename]            # Run specific test
```

### C# Backend Tests (xUnit)
```bash
# Core domain tests (5 files)
dotnet test tests/WitchCityRope.Core.Tests/

# Infrastructure tests (15 files)
dotnet test tests/WitchCityRope.Infrastructure.Tests/

# API tests (4 files)
dotnet test tests/WitchCityRope.Api.Tests/

# Unit tests (17 files)
dotnet test tests/unit/api/

# Integration tests (5 files)
dotnet test tests/integration/

# All active tests (56 files)
dotnet test --filter "FullyQualifiedName!~legacy-obsolete&FullyQualifiedName!~blazor-obsolete&FullyQualifiedName!~disabled"
```

---

## 🎯 Quick Reference

### Finding Tests by Feature

**Vetting Tests**:
- E2E: 10+ files in `/apps/web/tests/playwright/vetting-*.spec.ts`
- React: 6 files in `/apps/web/src/features/vetting/**/*.test.tsx`
- C#: 6 files in `/tests/unit/api/Features/Vetting/`

**Event Tests**:
- E2E: 15+ files in `/apps/web/tests/playwright/events-*.spec.ts`
- React: 2 files in `/apps/web/src/components/events/__tests__/`
- C#: 3 files in `/tests/WitchCityRope.Core.Tests/Features/Events/`

**Authentication Tests**:
- E2E: 20+ files in `/apps/web/tests/playwright/login-*.spec.ts`, `auth-*.spec.ts`
- React: 2 files in `/apps/web/src/features/auth/` and `/stores/`
- C#: 1 file in `/tests/WitchCityRope.Core.Tests/Features/Authentication/`

**Dashboard Tests**:
- E2E: 5+ files in `/apps/web/tests/playwright/dashboard-*.spec.ts`
- React: 5 files in `/apps/web/src/pages/dashboard/__tests__/`
- C#: 1 file in `/tests/unit/api/Features/Dashboard/`

**Payment Tests**:
- E2E: Not explicitly covered (may be in general workflows)
- React: Not explicitly covered
- C#: 6+ files in `/tests/WitchCityRope.Infrastructure.Tests/PayPal/`

---

## 📊 Test Coverage by Type

| Feature Area | E2E Tests | React Tests | C# Tests | Total |
|--------------|-----------|-------------|----------|-------|
| Vetting | 10+ | 6 | 6 | 22+ |
| Events | 15+ | 2 | 3 | 20+ |
| Authentication | 20+ | 2 | 1 | 23+ |
| Dashboard | 5+ | 5 | 1 | 11+ |
| Payments | 0 | 0 | 12+ | 12+ |
| Admin | 4 | 1 | 0 | 5 |
| Navigation | 5+ | 1 | 0 | 6+ |
| Infrastructure | 0 | 0 | 20+ | 20+ |
| Diagnostic | 15+ | 0 | 4 | 19+ |
| **TOTAL** | **89** | **20** | **56** | **165** |

*Note: Counts are approximate for feature areas with many tests*

---

## 🔄 Maintenance Guidelines

### When Adding New Tests

1. **Add to appropriate section** in this document
2. **Update test counts** in summary tables
3. **Document purpose** and what the test covers
4. **Update Part 1** navigation if significant
5. **Cross-reference** with related tests

### When Removing/Archiving Tests

1. **Move to Legacy section** if obsolete
2. **Update counts** in all summary tables
3. **Document reason** for archival
4. **Remove from active test runs**
5. **Keep for historical reference**

### Regular Reviews

- **Monthly**: Verify test counts are accurate
- **Quarterly**: Review test organization, consolidate duplicates
- **After migrations**: Update test categorizations

---

## 📝 Version History

**Version 1.0** (2025-10-10):
- Initial creation with all 271 test files documented
- Organized by test type and feature area
- Comprehensive descriptions and locations
- Cross-referenced with Part 1 navigation index

---

*This catalog documents all test files as of 2025-10-10. For current test execution status, see CURRENT_TEST_STATUS.md*
*For testing standards and patterns, see TESTING_GUIDE.md and related documentation in Part 1*
