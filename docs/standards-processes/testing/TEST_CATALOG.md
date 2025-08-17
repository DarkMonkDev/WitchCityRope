# WitchCityRope Test Catalog
<!-- Last Updated: 2025-08-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Testing Team -->
<!-- Status: Active -->

## Overview
This catalog provides a comprehensive inventory of all tests in the WitchCityRope project, organized by type and location. This is the single source of truth for understanding our test coverage.

## Quick Reference
- **Unit Tests**: ~75 tests files across Core/API/Web projects
- **Integration Tests**: 133 tests (PostgreSQL with TestContainers)
- **E2E Tests**: 45 Playwright test spec files (Migrated from Puppeteer)
- **Performance Tests**: Basic load testing infrastructure

**Status**: Major migration completed January 2025 - All Puppeteer tests migrated to Playwright

## Recent Additions (August 2025)

### Vertical Slice Home Page Tests - 2025-08-16
**Added**: Comprehensive test suite for React + API + PostgreSQL stack proof-of-concept
**Purpose**: Validate the complete vertical slice implementation works end-to-end
**Location**: Multiple test directories

#### API Unit Tests
**File**: `/tests/unit/api/EventsController.test.cs`
**Purpose**: Unit tests for EventsController with mocked IEventService
**Test Methods**:
- `GetEvents_WithDatabaseEvents_ReturnsOkWithEventList()` - Tests successful database response
- `GetEvents_WithEmptyDatabase_ReturnsFallbackEvents()` - Tests fallback mechanism
- `GetEvents_WithDatabaseException_ReturnsFallbackEvents()` - Tests error handling
- `GetEvents_ReturnsEventDtoArray_ProveStackWorks()` - Proves API contract works
- `GetEvents_AllDateTimesAreUtc_ProvePostgreSqlCompatibility()` - Tests UTC datetime handling

**Features**:
- Mock IEventService for isolated testing
- FluentAssertions for readable test assertions
- Tests both success and error scenarios
- Validates EventDto structure matches React expectations
- Proves API layer of the stack works

#### React Component Tests
**File**: `/tests/unit/web/EventsList.test.tsx`  
**Purpose**: Unit tests for EventsList component with mocked fetch
**Test Methods**:
- `displays loading spinner while fetching events` - Tests loading state
- `displays events when data loads successfully` - Tests successful data display
- `displays error message when API call fails` - Tests error handling
- `displays error message when API returns non-200 status` - Tests HTTP error handling
- `displays empty state when no events are returned` - Tests empty state
- `calls correct API endpoint for events` - Tests API endpoint correctness
- `proves React + API stack integration works` - End-to-end component behavior
- `handles network timeout gracefully` - Tests timeout scenarios

**Features**:
- Uses Vitest testing framework
- Mocks fetch() for isolated testing
- Tests all component states (loading, success, error, empty)
- Validates component structure and CSS classes
- Proves React layer of the stack works

#### E2E Tests
**File**: `/tests/e2e/home-page.spec.ts`
**Purpose**: End-to-end browser tests for complete React + API + PostgreSQL stack
**Test Cases**:
- `page loads successfully` - Basic page loading
- `events display from API` - API integration verification
- `loading state displays correctly` - Loading state testing
- `responsive layout works on different screen sizes` - Responsive design
- `API integration works end-to-end` - Network monitoring
- `error handling works when API is unavailable` - Error scenarios
- `proves complete React + API + PostgreSQL stack works` - **Main proof-of-concept test**

**Features**:
- Playwright browser automation
- Tests complete stack integration
- Cross-browser testing (Chrome, Firefox, Safari)
- Mobile and desktop responsive testing
- Network request monitoring
- Screenshot capture for debugging
- Automatic server startup/shutdown

#### Test Infrastructure
**Files Created**:
- `/tests/unit/api/WitchCityRope.Api.Tests.csproj` - C# test project
- `/apps/web/vitest.config.ts` - Vitest configuration
- `/apps/web/src/test/setup.ts` - Test setup file
- `/tests/e2e/playwright.config.ts` - Playwright configuration
- `/tests/e2e/package.json` - E2E test dependencies
- `/tests/run-vertical-slice-tests.sh` - Test runner script

**Test Execution**:
```bash
# Run all vertical slice tests
./tests/run-vertical-slice-tests.sh

# Individual test types
cd tests/unit/api && dotnet test                    # API unit tests
cd apps/web && npm test                             # React component tests  
cd tests/e2e && npm test                            # E2E tests
```

**Quality Assurance**:
- ✅ API Unit Tests prove controller returns correct EventDto structure
- ✅ React Component Tests prove components handle API responses correctly
- ✅ E2E Tests prove complete browser → React → API → PostgreSQL flow works
- ✅ All tests include error handling and edge cases
- ✅ Tests are minimal and focused (throwaway code for proof-of-concept)
- ✅ Comprehensive documentation for maintenance

## Recent Fixes (August 2025)

### Login Selector Fixes - 2025-08-13
**Issue**: Admin user management E2E tests failing due to incorrect login selectors  
**Root Cause**: Tests using `input[type="email"]` instead of correct `input[name="Input.EmailOrUsername"]`  
**Solution**: Updated all admin login tests to use ASP.NET Core Identity form selectors  

**Fixed Files**:
- ✅ `/tests/playwright/admin-user-management.spec.ts`
- ✅ `/tests/playwright/admin-user-details.spec.ts`  
- ✅ `/tests/playwright/admin/admin-user-management-focused.spec.ts`
- ✅ `/tests/playwright/admin/admin-user-management-updated.spec.ts`

**Correct Selectors**:
- Email: `input[name="Input.EmailOrUsername"]` (supports both email and username)
- Password: `input[name="Input.Password"]`
- Alternative: `input[placeholder*="email"]` (matches "your@email.com or username")

**Note**: Login page object (`/tests/playwright/pages/login.page.ts`) and auth helpers (`/tests/playwright/helpers/auth.helpers.ts`) already use correct selectors.

### Simple Admin User Management Test - 2025-08-13
**Added**: `/tests/playwright/admin/admin-user-management-simple.spec.ts`  
**Purpose**: Simplified admin user management test that avoids Blazor E2E helper timeouts  
**Features**:
- Basic admin login with known working selectors
- Navigation to `/admin/users` page
- Verification of key page elements (title, stats cards, user grid)
- Simple interaction testing (click user row, check for details panel)
- Responsive design testing
- Standard Playwright waits only (no complex Blazor circuit waiting)

**Test Cases**:
1. `should login as admin and verify user management page loads` - Core functionality
2. `should verify admin can access user details` - Basic interaction testing  
3. `should verify page elements are responsive` - Cross-device compatibility

### Admin User Management API Integration Tests - 2025-08-13
**Added**: Complete test suite for admin user management functionality
**Location**: Multiple test files

#### Unit Tests
**File**: `/tests/WitchCityRope.Web.Tests/Services/ApiClientTests.cs` (extended)
**Purpose**: Unit tests for ApiClient user management methods with proper mocking
**New Test Methods**:
- `GetUsersAsync_WithSearchParameters_ReturnsPagedResult()` - Tests search functionality
- `GetUsersAsync_WithRoleFilter_BuildsCorrectQuery()` - Tests role filtering
- `GetUsersAsync_WithHttpError_ThrowsException()` - Tests error handling
- `GetUserByIdAsync_WithValidId_ReturnsUser()` - Tests user detail retrieval
- `GetUserByIdAsync_WithNotFoundId_ReturnsNull()` - Tests not found scenarios
- `UpdateUserAsync_WithValidData_ReturnsTrue()` - Tests user updates
- `UpdateUserAsync_WithServerError_ReturnsFalse()` - Tests update error handling
- `ResetUserPasswordAsync_WithValidData_ReturnsTrue()` - Tests password reset
- `ManageUserLockoutAsync_LockUser_ReturnsTrue()` - Tests user lockout
- `GetUserStatsAsync_WithValidResponse_ReturnsStats()` - Tests statistics
- `GetAvailableRolesAsync_WithValidResponse_ReturnsRoles()` - Tests role dropdown

#### Integration Tests
**File**: `/tests/WitchCityRope.IntegrationTests/Admin/AdminUsersControllerTests.cs` (new)
**Purpose**: Tests real HTTP calls to admin/users API endpoints with authentication
**Test Methods**:
- `GetUsers_WithAdminAuth_ReturnsPagedUserList()` - Tests user listing
- `GetUsers_WithSearchFilter_ReturnsFilteredResults()` - Tests search functionality
- `GetUsers_WithRoleFilter_ReturnsUsersOfSpecificRole()` - Tests role filtering
- `GetUsers_WithoutAdminAuth_ReturnsUnauthorized()` - Tests authorization
- `GetUserById_WithValidId_ReturnsUserDetails()` - Tests user details
- `GetUserById_WithInvalidId_ReturnsNotFound()` - Tests not found handling
- `UpdateUser_WithValidData_UpdatesUserSuccessfully()` - Tests user updates
- `UpdateUser_WithInvalidData_ReturnsBadRequest()` - Tests validation
- `ResetUserPassword_WithValidData_ResetsPasswordSuccessfully()` - Tests password reset
- `ManageUserLockout_LockUser_LocksUserSuccessfully()` - Tests user locking
- `ManageUserLockout_UnlockUser_UnlocksUserSuccessfully()` - Tests user unlocking
- `GetUserStats_WithAdminAuth_ReturnsStatistics()` - Tests statistics endpoint
- `GetRoles_WithAdminAuth_ReturnsAvailableRoles()` - Tests role endpoint
- `CompleteUserManagementWorkflow_CreateSearchUpdateDelete_WorksEndToEnd()` - End-to-end workflow

#### E2E Tests
**File**: `/tests/playwright/admin-user-management-api-integration.spec.ts` (new)
**Purpose**: E2E tests for admin user management with real API integration
**Test Cases**:
1. `should login as admin and verify real users load from API` - Core API integration
2. `should test search functionality with real API calls` - Search testing
3. `should test user details functionality` - User details modal/page
4. `should test role filter functionality` - Role filtering
5. `should verify pagination works with real data` - Pagination testing
6. `should verify stats cards display real data from API` - Statistics verification
7. `should handle responsive design for user management` - Cross-device testing

**Features**:
- Uses correct ASP.NET Core Identity login selectors (fixed 2025-08-13)
- Tests real API data loading (not mock data)
- Verifies search, filter, and pagination functionality
- Tests user details modal/panel interaction
- Validates statistics cards show real numbers
- Includes responsive design testing
- Simple Playwright waits without complex Blazor E2E helper
- Comprehensive screenshots for debugging

### Blazor Server Migration Test - 2025-08-13
**Added**: `/tests/playwright/admin/admin-users-blazor.spec.ts`  
**Purpose**: New E2E test specifically for Blazor Server architecture (migrated from Razor Pages)  
**Context**: Website converted from Razor Pages to Blazor Server, old tests failing  
**Features**:
- Works with Blazor Server components (not old Razor Pages architecture)
- Avoids broken Blazor E2E helper (timing out)
- Uses simple Playwright waits like `page.waitForLoadState('networkidle')`
- Direct navigation and login (bypasses global setup)
- Comprehensive debugging with screenshots
- Graceful handling of loading states

**Test Cases**:
1. `should login as admin and access user management with Blazor Server` - Main functionality test
2. `should handle page loading states gracefully` - Loading state verification
3. `should capture page structure for debugging` - Debugging aid

## Important Migration Notes

### Blazor Server Architecture Change
**CRITICAL**: The website has been converted from Razor Pages to Blazor Server. This affects E2E testing:

**Issues**:
- Old E2E tests expecting Razor Pages behavior are failing
- Blazor E2E helper timing out (probably outdated for new architecture)
- Need new test patterns for Blazor Server components

**Solutions**:
- ✅ New test: `/tests/playwright/admin/admin-users-blazor.spec.ts` - Works with current Blazor Server architecture
- ✅ Use simple Playwright waits instead of complex Blazor E2E helper
- ✅ Direct navigation and element verification patterns
- ✅ Known working login selectors from recent fixes

**Test Update Status**:
- ❌ **NEEDS UPDATE**: Most existing E2E tests need updating for Blazor Server
- ✅ **WORKING**: New Blazor Server-specific tests created
- ✅ **WORKING**: Admin login selectors fixed (2025-08-13)
- 🔄 **ONGOING**: Migrate remaining tests to work with Blazor Server architecture

## Test Organization

### 1. Unit Tests

#### Core Domain Tests
**Location**: `/tests/WitchCityRope.Core.Tests/`  
**Status**: Domain logic and entity validation tests

Key test files:
- `Entities/EventTests.cs` - Event entity validation and business rules
- `Entities/UserTests.cs` - User entity and scene name validation
- `Entities/RegistrationTests.cs` - Ticket and registration logic
- `Entities/PaymentTests.cs` - Payment processing logic
- `Entities/VettingApplicationTests.cs` - Vetting workflow
- `ValueObjects/MoneyTests.cs` - Money value object
- `Services/EventValidationServiceTests.cs` - Event validation rules

#### API Unit Tests
**Location**: `/tests/WitchCityRope.Api.Tests/`  
**Count**: Multiple test files for API services and features
**Status**: Service layer and feature command testing

Key test files:
- `Services/AuthServiceTests.cs` - Authentication logic
- `Services/EventServiceTests.cs` - Event management
- `Services/UserServiceTests.cs` - User management
- `Services/PaymentServiceTests.cs` - Payment processing
- `Services/VettingServiceTests.cs` - Vetting workflow
- `Features/Auth/LoginCommandTests.cs` - Login command handler
- `Features/Events/CreateEventCommandTests.cs` - Event creation

#### Web Component Tests
**Location**: Multiple web test projects (consolidation needed)
**Status**: Blazor component testing with bUnit

Test projects found:
- `WitchCityRope.Web.Tests/` - Main web tests
- `WitchCityRope.Web.Tests.Login/` - Login-specific tests  
- `WitchCityRope.Web.Tests.New/` - New test structure
- `WitchCityRope.Tests.Common/` - Shared test utilities

### 2. Integration Tests

#### Database Integration Tests
**Location**: `/tests/WitchCityRope.IntegrationTests/`  
**Count**: Multiple integration test files
**Status**: Uses PostgreSQL with TestContainers (real database testing)

Key features:
- Uses PostgreSQL with TestContainers
- Real database testing (no in-memory)
- Health check system for test readiness

Key test files:
- `AuthenticationTests.cs` - Auth flow integration
- `DatabaseHealthTests.cs` - Database connectivity
- `NavigationLinksTests.cs` - Route testing
- `UserMenuIntegrationTests.cs` - UI component integration

#### Infrastructure Tests
**Location**: `/tests/WitchCityRope.Infrastructure.Tests/`  
**Status**: Data access layer and repository testing

Test categories:
- Database configuration tests
- Migration tests
- Repository pattern tests
- Service integration tests

### 3. E2E Tests (Playwright)

#### Main E2E Test Suite
**Location**: `/tests/playwright/`  
**Count**: 44 Playwright test spec files (migrated from Puppeteer in January 2025)  
**Status**: Comprehensive browser automation with Page Object Models

Test categories:
```
├── admin/ (9 spec files) - Login selectors fixed 2025-08-13
│   ├── admin-user-management-updated.spec.ts ✅ Fixed
│   ├── admin-user-management-focused.spec.ts ✅ Fixed
│   ├── admin-user-management-simple.spec.ts ✅ Simple test without Blazor E2E helper
│   ├── admin-users-blazor.spec.ts ✅ NEW - Blazor Server architecture test
│   ├── admin-events-management.spec.ts
│   ├── admin-event-creation.spec.ts
│   ├── admin-login-success.spec.ts
│   ├── admin-dashboard.spec.ts
│   └── admin-access.spec.ts
├── auth/ (Authentication flows)
├── events/ (3 spec files)
│   ├── event-display.spec.ts
│   ├── event-edit.spec.ts  
│   └── event-creation.spec.ts
├── infrastructure/ (5 spec files)
│   ├── layout-system.spec.ts
│   ├── error-timing.spec.ts
│   ├── css-loading.spec.ts
│   ├── page-status.spec.ts
│   └── styling.spec.ts
├── ui/ (3 spec files)
│   ├── user-dropdown.spec.ts
│   ├── button-interactivity.spec.ts
│   └── navigation.spec.ts
├── rsvp/ (RSVP functionality)
├── validation/ (Form validation)
└── specs/visual/ (Visual regression)
```

#### Page Objects
**Location**: `/tests/playwright/pages/`
- Implements Page Object Model pattern
- Reusable components for test maintainability

#### Test Helpers
**Location**: `/tests/playwright/helpers/`
- Authentication helpers
- Data generation utilities
- Common assertions

#### Utility Scripts
**Location**: `/tests/playwright/utilities/`
- `analyze-login-logs.js` - Analyzes login test results from monitoring runs
- `test-homepage.js` - Quick smoke test for homepage functionality
- Scripts for debugging and analysis (not part of main test suite)

### 4. Performance Tests

**Location**: `/tests/WitchCityRope.PerformanceTests/`  
**Status**: Basic load testing infrastructure

Subdirectories:
- `LoadTests/` - Load testing scenarios
- `StressTests/` - System stress testing

### 5. Deprecated Test Locations

#### Migrated E2E Tests (DO NOT USE)
- `/tests/WitchCityRope.E2E.Tests/` - Old Puppeteer tests (migrated)
- `/tests/e2e/` - Old Puppeteer tests (if exists)
- `/ToBeDeleted/` - Deprecated tests
- **CRITICAL**: All Puppeteer tests migrated to Playwright in January 2025

## Running Tests

### Prerequisites
```bash
# Ensure Docker is running
sudo systemctl start docker

# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Unit Tests
```bash
# Run all unit tests
dotnet test tests/WitchCityRope.Core.Tests/
dotnet test tests/WitchCityRope.Api.Tests/

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

### E2E Tests
```bash
cd tests/playwright

# Install dependencies (first time only)
npm install

# Run all E2E tests
npm test

# Run specific category
npm test tests/auth/

# Run with UI mode (debugging)
npm test -- --ui

# Update visual baselines
npm test -- --update-snapshots
```

### Utility Scripts
```bash
# Located in tests/playwright/utilities/

# Analyze login test results
node tests/playwright/utilities/analyze-login-logs.js

# Quick homepage smoke test
node tests/playwright/utilities/test-homepage.js
```

### Performance Tests
```bash
dotnet test tests/WitchCityRope.PerformanceTests/
```

## Test Data

### Seeded Test Users
```
admin@witchcityrope.com / Test123! - Administrator, Vetted
member@witchcityrope.com / Test123! - Member, Vetted
user@witchcityrope.com / Test123! - Member, Not Vetted
teacher@witchcityrope.com / Test123! - Teacher, Vetted
organizer@witchcityrope.com / Test123! - Event Organizer
```

### Test Database
- Container: `witchcity-postgres-test`
- Auto-created by TestContainers
- Cleaned between test runs

## Coverage Goals

### Current Coverage
- **Unit Tests**: Business logic and API service layer coverage
- **Integration Tests**: Real PostgreSQL database testing
- **E2E Tests**: 44 Playwright spec files covering critical user workflows

### Target Coverage
- Unit Tests: Core domain logic and API services
- Integration Tests: Full database integration patterns
- E2E Tests: Critical user journeys with cross-browser support

## Known Issues & Recent Fixes

### Global Setup Issue Fixed (August 2025)
- **FIXED**: Global setup timeout for vetted user authentication
- **FIXED**: Blazor E2E helper timeout increased from 30s to 60s  
- **FIXED**: Function call error in global-setup.ts

### Integration Tests
- Uses real PostgreSQL with TestContainers (migrated from in-memory)
- **CRITICAL**: Always run health checks first before integration tests

### E2E Tests  
- Successfully migrated 180 Puppeteer tests to 44 Playwright spec files
- Improved stability with better timeout configurations
- Cross-browser testing support (Chrome, Firefox, Safari)

### Performance Tests
- Basic infrastructure in place
- Expansion needed for comprehensive load testing

## Maintenance

### Adding New Tests
1. **Playwright E2E**: Add to appropriate category folder in `/tests/playwright/`
2. **Unit Tests**: Follow patterns in Core.Tests, Api.Tests, Web.Tests projects  
3. **Integration Tests**: Use PostgreSQL TestContainers pattern
4. **Update this catalog** when adding new test categories or major changes

### Test Data Management
- Seeded test users in DbInitializer.cs
- Use unique identifiers (GUIDs) to avoid conflicts
- **CRITICAL**: All DateTime values must be UTC for PostgreSQL

### Recent Major Changes
- **January 2025**: All Puppeteer tests migrated to Playwright
- **July 2025**: Integration tests migrated to real PostgreSQL
- **August 2025**: Fixed Blazor E2E timeout issues

---

*For test writing best practices, see [lessons-learned/test-writers.md](/docs/lessons-learned/test-writers.md)*