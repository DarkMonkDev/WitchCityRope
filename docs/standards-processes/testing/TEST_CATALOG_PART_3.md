# WitchCityRope Test Catalog - PART 3 (Archived Test Information)
<!-- Last Updated: 2025-09-22 -->
<!-- Version: 2.0 -->
<!-- Owner: Testing Team -->
<!-- Status: ARCHIVED TEST MIGRATION AND LEGACY INFORMATION -->

## 🚨 NAVIGATION TO CURRENT/RECENT TESTS 🚨

**FOR CURRENT TESTS**: See `/docs/standards-processes/testing/TEST_CATALOG.md` (Part 1)
**FOR RECENT PATTERNS**: See `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md` (Part 2)

### 📚 WHAT'S IN THIS PART 3:
- Test migration analysis and history
- Legacy test architecture documentation
- Archived test patterns and cleanup notes
- Historical infrastructure transformation details
- Obsolete test system information

### 🎯 WHEN TO READ THIS PART:
- Need historical context on test migrations?
- Looking for legacy pattern explanations?
- Want to understand past test architecture decisions?
- Need context on why certain approaches were abandoned?

---

## Archived Test Migration Information

**Added**: `/apps/web/src/test/integration/msw-verification.test.ts`
**Updated**: `/apps/web/src/stores/__tests__/authStore.test.ts`, `/apps/web/src/features/auth/api/__tests__/mutations.test.tsx`, `/apps/web/src/test/integration/auth-flow-simplified.test.tsx`
**Purpose**: Fixed Mock Service Worker (MSW) configuration for proper API request interception and improved test pass rate from 33% to 75%+ 
**Context**: MSW was not intercepting requests properly due to port mismatches, response structure misalignment, and component tests conflicting with global MSW setup

**Test Coverage & Pass Rate Improvements**:
- ✅ **EventsList Component**: 8/8 tests passing (was 2/8)
- ✅ **AuthStore**: All state management tests passing
- ✅ **MSW Integration**: Request interception working correctly
- ✅ **Auth Flow**: Integration tests aligned with NSwag UserDto structure

**Critical Fixes Applied**:
1. **MSW Global vs Component Testing Pattern**:
   - Component tests now use `server.use()` to override MSW handlers per test
   - Removed conflicting `mockFetch` direct mocking when MSW is active globally
   - Fixed: EventsList error handling tests now work with MSW handlers

2. **Port Standardization**: 
   - Primary auth endpoints: localhost:5651 (Web Service)
   - Legacy support: localhost:5655 (Main API Service) 
   - Removed incorrect port 5653 references

3. **NSwag UserDto Structure Alignment**:
   - Added missing properties: `firstName`, `lastName`, `roles`, `isActive`, `updatedAt`
   - All UserDto mock data now matches generated schema exactly
   - Properties: `id`, `email`, `sceneName`, `firstName`, `lastName`, `roles`, `isActive`, `createdAt`, `updatedAt`, `lastLoginAt`

4. **API Response Structure Fixes**:
   - Login: `{ success: true, user: UserDto, message: string }` (matches NSwag LoginResponse)
   - Auth check: `{ success: true, data: UserDto }` 
   - Protected endpoints return complete UserDto structure
   - Fixed refresh endpoint handlers for axios interceptor

5. **Test Data Alignment**:
   - EventsList tests now expect "Test Event 1" and "Test Event 2" (matching MSW handler)
   - Integration tests expect complete UserDto properties
   - Error handling tests properly use MSW error responses

4. **Test Data Consistency**:
   - Updated all test files to use consistent NSwag UserDto structure
   - Fixed authStore.test.ts logout behavior (store doesn't make API calls)
   - Updated mutations.test.tsx to use correct ports and response structure
   - Fixed auth-flow.test.tsx integration test expectations

5. **Architecture Clarification**:
   - Auth store: State management only (no direct API calls)
   - Auth mutations: API calls via TanStack Query hooks (useLogin, useLogout)
   - MSW handlers: Support both current and legacy endpoint patterns

**Benefits**:
- ✅ MSW now properly intercepts React test API calls
- ✅ Test data matches production API responses exactly
- ✅ NSwag generated types work correctly in tests
- ✅ Consistent UserDto structure prevents type errors
- ✅ Proper separation between auth store (state) and auth mutations (API)
- ✅ Support for both current (5651) and legacy (5655) API ports

### Improved Authentication E2E Test - 2025-08-19
**Added**: `/apps/web/tests/playwright/auth-flow-improved.spec.ts`
**Purpose**: Enhanced E2E authentication test following best practices from lessons learned
**Context**: Created comprehensive, reliable authentication flow test for React architecture validation

**Test Coverage**:
- Successful login flow with admin user and navigation verification
- Protected route access control (redirects to login when unauthenticated)
- Invalid credentials handling with proper error display
- Complete logout flow with auth state clearing
- Authentication state persistence across page refresh
- Protected route returnTo parameter functionality
- HttpOnly cookie security verification (anti-XSS protection)
- Performance testing (login completes within 3 seconds)
- Cross-browser authentication consistency testing

**Best Practices Applied**:
- ✅ Uses existing test accounts (admin@witchcityrope.com) for reliability
- ✅ Proper wait strategies (no fixed timeouts) - `page.waitForURL()` with timeouts
- ✅ Data-testid attributes where possible, proper form selectors (`input[name="email"]`)
- ✅ Tests real API interactions (not mocked)
- ✅ Clear Arrange-Act-Assert pattern throughout
- ✅ Test isolation with `page.context().clearCookies()` in beforeEach
- ✅ Comprehensive error scenarios and edge cases
- ✅ Security testing (httpOnly cookie verification)
- ✅ Performance benchmarking with timing assertions

**Testing Architecture Validated**:
- ✅ React Router v7 navigation patterns with protected routes
- ✅ Authentication state management with proper persistence
- ✅ Form interaction patterns with Mantine components
- ✅ API integration for login/logout endpoints
- ✅ Cross-browser compatibility (Chrome, Firefox, Safari)
- ✅ Security best practices (httpOnly cookies, XSS protection)

**Key Patterns Established**:
```typescript
// Proper wait strategy for authentication flow
await page.waitForURL('/dashboard', { timeout: 10000 })
await expect(page.locator('text=Welcome')).toBeVisible({ timeout: 10000 })

// Security testing pattern
const accessibleCookies = await page.evaluate(() => document.cookie)
expect(accessibleCookies).not.toContain('auth-token') // HttpOnly protection

// Performance testing pattern
const startTime = Date.now()
// ... authentication flow ...
const loginDuration = endTime - startTime
expect(loginDuration).toBeLessThan(3000)
```

**Benefits for Development**:
- ✅ Validates complete React authentication architecture works in browser
- ✅ Provides reliable test patterns for future E2E authentication testing
- ✅ Establishes security verification patterns for authentication flows
- ✅ Creates performance benchmarks for authentication responsiveness
- ✅ Validates cross-browser authentication consistency

### Authentication Integration Test - 2025-08-19
**Added**: `/apps/web/src/test/integration/auth-flow.test.tsx` and `/apps/web/src/test/integration/auth-flow-simplified.test.tsx`
**Purpose**: Integration tests for complete authentication flow testing multiple components working together
**Context**: Created to validate React authentication architecture with TanStack Query, Zustand, and React Router v7

**Integration Points Tested**:
- Mantine form validation → TanStack Query mutation → Zustand store → React Router navigation
- MSW API mocking for authentication endpoints with full URLs
- Auth store permission calculation from user roles
- Query invalidation and cache management on auth state changes
- Error handling across the complete auth flow
- Session state persistence and cleanup

**Testing Architecture Validated**:
- ✅ Multiple React hooks working together (useLogin + useAuth + useNavigate)
- ✅ Zustand store integration with TanStack Query mutations
- ✅ MSW v2 API mocking with full URL patterns (not relative paths)
- ✅ React Testing Library with complex component interactions
- ✅ Mantine component testing with proper mocks (matchMedia, ResizeObserver)

**Key Patterns Established**:
```typescript
// Integration test wrapper with all providers
const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      <MantineProvider>
        {component}
      </MantineProvider>
    </QueryClientProvider>
  )
}

// MSW handlers must use full URLs for axios baseURL compatibility
http.post('http://localhost:5653/api/auth/login', handler)  // ✅ CORRECT
http.post('/api/auth/login', handler)  // ❌ WRONG with baseURL
```

**Challenges Identified**:
- React Router context issues in isolated component testing - requires proper provider setup
- Zustand store snapshot warnings in complex component trees - solved by mocking Router hooks
- MSW response.clone issues with mock Response objects - requires proper Response mocking

### React Authentication Hook Unit Tests - 2025-08-19
**Added**: `/apps/web/src/features/auth/api/__tests__/mutations.test.tsx`
**Purpose**: Unit tests for TanStack Query useLogin and useLogout hooks to validate React testing patterns
**Context**: Created as part of React migration from Blazor Server to validate testing infrastructure

**Test Coverage**:
- `useLogin` hook with success/failure scenarios
- `useLogout` hook with success/failure scenarios  
- TanStack Query integration with React Testing Library
- Zustand auth store integration testing
- MSW API mocking for authentication endpoints
- React Router navigation testing
- Query invalidation testing
- Retry behavior testing (disabled for auth mutations)

**Testing Infrastructure Validated**:
- ✅ Vitest test runner working with React hooks
- ✅ React Testing Library renderHook for custom hooks
- ✅ TanStack Query integration with test wrappers
- ✅ MSW v2 API mocking for authentication endpoints
- ✅ Zustand store state management in tests
- ✅ React Router navigation mocking and verification

**Test Patterns Established**:
```typescript
// Query client wrapper for hook testing
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

// Hook testing with async operations
const { result } = renderHook(() => useLogin(), { wrapper: createWrapper() })
act(() => { result.current.mutate(credentials) })
await waitFor(() => { expect(result.current.isSuccess).toBe(true) })
```

**Issues Discovered and Resolved**:
- MSW handler path mismatches (needed full URLs for API client)
- Auth store User interface mismatch (roles vs role property)
- Global fetch mocking conflicts between MSW and auth store
- Async state timing issues in hook testing

**Files Created**:
- `/apps/web/src/features/auth/api/__tests__/mutations.test.tsx` - Main test file
- `/apps/web/src/features/auth/api/__tests__/mutations-simple.test.tsx` - Simplified test with direct mocks

**Files Updated**:
- `/apps/web/src/test/mocks/handlers.ts` - Added proper auth endpoints with correct paths
- Handlers updated to use auth store User interface (roles array)

**Test Execution**:
```bash
# Run React hook tests
npm test -- src/features/auth/api/__tests__/mutations.test.tsx

# Run simple version
npm test -- src/features/auth/api/__tests__/mutations-simple.test.tsx
```

**Value for Development**:
- ✅ Validates React testing patterns work with new architecture
- ✅ Establishes testing infrastructure for TanStack Query hooks
- ✅ Confirms MSW integration for API mocking
- ✅ Creates reusable patterns for future React testing
- ✅ Validates Zustand store testing approaches
- ✅ Provides template for auth-related testing

## Recent Additions (August 2025)

### Form Design Showcase Content Verification Test - 2025-08-18
**Added**: `/tests/e2e/form-designs-check.spec.ts`
**Purpose**: Verify form design showcase pages actually display content beyond HTTP 200 responses
**Context**: User reported form design pages at /form-designs/* return 200 but don't actually load content

**Test Results - CRITICAL FINDINGS**:
- ✅ All pages return HTTP 200 OK (routes configured correctly)
- ❌ **Body content is completely empty on all pages**
- ❌ **No form elements, buttons, or interactive content rendered**
- ❌ **React components are not being rendered despite React root existing**
- ✅ No console errors or network failures detected

**Root Cause Identified**: Form design showcase components are **NOT IMPLEMENTED** in React
- Routes exist but point to empty/missing components
- Requires React developer to implement actual form design showcase components
- This is a **development issue, not a testing issue**

**Test Cases**:
1. `Form Design A (Floating Labels) page loads and displays content` - **FAILED: No content**
2. `Form Design showcase main page loads and displays content` - **FAILED: No navigation links**
3. `All form design pages return successful HTTP responses` - **PASSED: All return 200**
4. `Form Design A - Detailed content inspection` - **FAILED: No form elements**

**Diagnostic Features**:
- Comprehensive content inspection with element counting
- Cross-browser testing (Chrome, Firefox, Safari, Mobile)
- Screenshots captured for visual verification
- Console and network error monitoring
- HTML structure analysis to confirm React mounting

**Value for Development Team**:
- ✅ Proves issue is React component implementation, not routing
- ✅ Confirms HTTP layer works correctly
- ✅ Provides visual evidence via screenshots
- ✅ Eliminates network/server issues as root cause
- 🎯 **Action Required**: React developer needs to implement form design showcase components

### Form Components Test Page E2E Test - 2025-08-18
**Added**: `/tests/e2e/form-components.spec.ts`
**Purpose**: Comprehensive E2E test for Mantine v7 form components validation page
**Context**: Created to verify Form Components Test Page (http://localhost:5173/form-test) loads correctly and all components are functional

**Test Cases**:
1. `should load the form test page successfully` - Basic page loading and title verification
2. `should display all test control buttons` - Verify test control buttons are visible and labeled correctly
3. `should display all form input components` - Check all form fields and components are visible
4. `should fill test data when Fill Test Data button is clicked` - Test data population functionality
5. `should show validation errors when Toggle Errors button is clicked` - Validation error display
6. `should disable all fields when Disable All button is clicked` - Field disable/enable functionality
7. `should test conflict data and validation responses` - Conflict data and async validation testing
8. `should display form state information` - Form state badges and accordions
9. `should handle form submission` - Form submission and notification testing
10. `should verify responsive layout on mobile viewport` - Mobile responsiveness testing
11. `should capture network requests and errors` - Network monitoring and debugging
12. `should verify all component sections are present` - Complete page structure verification

**Components Tested**:
- BaseInput, BaseSelect, BaseTextarea (basic components)
- EmailInput, SceneNameInput, PasswordInput, PhoneInput (specialized components)
- EmergencyContactGroup (composite component)
- Form state display, validation errors, submission handling
- Test control buttons and interactions

**Testing Features**:
- Comprehensive console and network error capture
- Screenshots at each test step for debugging
- Cross-browser testing (Chrome, Firefox, Safari, Mobile)
- Proper Playwright waits and timeouts
- Data-testid attribute targeting for stable selectors
- Responsive design verification

**Quality Assurance**:
- ✅ Tests all interactive elements with data-testid attributes
- ✅ Captures screenshots for debugging failed tests
- ✅ Monitors console errors and network failures
- ✅ Tests mobile viewport responsiveness
- ✅ Verifies async validation and form submission flows
- ✅ Comprehensive error handling and logging

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

## Recent Additions (September 2025)

### 🚨 CRITICAL: Mantine UI Login Solution for Playwright E2E Tests - 2025-09-12 🚨
**Added**: Comprehensive login method testing and working solution for Mantine UI components in Playwright
**Purpose**: Solve the critical issue where E2E tests can't login properly due to Mantine component CSS console errors blocking form interaction
**Context**: Created and tested multiple approaches to find a reliable login method that works with Mantine UI components

**PROBLEM IDENTIFIED**:
- **Issue**: Current E2E tests can't login properly due to Mantine component CSS console errors
- **Root Cause**: Tests were failing due to incorrect selectors and misunderstanding of Mantine CSS warnings
- **Impact**: All authentication-dependent E2E tests were unreliable or failing
- **Previous Attempts**: Various workarounds attempted but none provided a systematic solution

**SOLUTION IMPLEMENTED**:

#### Files Created:
- ✅ `/tests/e2e/login-methods-test.spec.ts` - Comprehensive testing of 5 different login approaches
- ✅ `/tests/e2e/helpers/auth.helper.ts` - Reusable authentication helper with multiple fallback strategies
- ✅ `/tests/e2e/working-login-solution.spec.ts` - Demonstrates the working approach with examples

#### Key Findings from Testing:
1. **Method 1 (WORKS)**: `page.fill()` with `data-testid` selectors is reliable
2. **Method 2 (FAILS)**: `input[name="email"]` selectors don't work - elements don't exist
3. **Method 3 (PARTIAL)**: DOM manipulation works but is not needed
4. **Method 4 (WORKS)**: Console errors from Mantine CSS warnings are NOT blocking
5. **Method 5 (WORKS)**: Comprehensive helper with fallbacks provides reliability

**CRITICAL DISCOVERY**: 
```typescript
// ❌ WRONG - These selectors don't work because LoginPage uses data-testid
await page.locator('input[name="email"]').fill('admin@witchcityrope.com')

// ✅ CORRECT - Use data-testid selectors from LoginPage.tsx
await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com')
await page.locator('[data-testid="password-input"]').fill('Test123!')
await page.locator('[data-testid="login-button"]').click()
```

**MANTINE UI INSIGHTS**:
- **CSS Warnings Are NOT Blocking**: Console warnings like "Unsupported style property &:focus-visible" don't prevent form interaction
- **Data-TestId Strategy**: LoginPage.tsx uses `data-testid` attributes, not `name` attributes
- **page.fill() Method**: Works reliably with Mantine TextInput and PasswordInput components
- **Performance**: Login completes in ~1.8 seconds average

**WORKING AUTHENTICATION HELPER**:
```typescript
// Simple usage
import { AuthHelper, quickLogin } from './helpers/auth.helper'

// Method 1: Using helper class
const success = await AuthHelper.loginAs(page, 'admin')

// Method 2: Quick utility function
await quickLogin(page, 'admin') // Throws on failure

// Method 3: Custom credentials
await AuthHelper.loginWithCredentials(page, {
  email: 'admin@witchcityrope.com',
  password: 'Test123!'
})
```

**HELPER FEATURES**:
- ✅ Multiple fallback strategies (fill() → force → DOM manipulation)
- ✅ Console error handling with filtering of CSS warnings
- ✅ Authentication state management (clear, verify, logout)
- ✅ Cross-browser compatibility
- ✅ Performance timing and monitoring
- ✅ Test account management for all user types

**ERROR HANDLING STRATEGY**:
```typescript
// Differentiates between blocking errors and CSS warnings
page.on('console', msg => {
  if (msg.type() === 'error') {
    const errorText = msg.text()
    // Only treat as critical if it's not a Mantine CSS warning
    if (!errorText.includes('style property') && !errorText.includes('maxWidth')) {
      criticalErrors.push(errorText)
    }
  }
})
```

**BEST PRACTICES ESTABLISHED**:
1. **Selector Priority**: data-testid > semantic selectors > text selectors > CSS selectors
2. **Error Classification**: Separate CSS warnings from JavaScript errors
3. **Timeout Management**: Use appropriate timeouts for authentication flows (15s)
4. **State Management**: Clear authentication state between tests for isolation
5. **Monitoring**: Track API responses and navigation events for debugging

**TEST EXECUTION COMMANDS**:
```bash
# Test all login methods
cd tests/e2e && npm test login-methods-test.spec.ts

# Test working solution
cd tests/e2e && npm test working-login-solution.spec.ts -- --project=chromium

# Focus on single browser for debugging
cd tests/e2e && npm test working-login-solution.spec.ts -- --project=chromium --headed
```

**INTEGRATION WITH EXISTING PATTERNS**:
- ✅ Follows lessons learned from authentication timeout enhancements (2025-09-08)
- ✅ Uses established console error monitoring patterns (2025-09-10)
- ✅ Implements proper wait strategies and navigation verification
- ✅ Compatible with existing Playwright configuration and test structure
- ✅ Supports all test accounts: admin, teacher, member, vetted, guest

**IMMEDIATE IMPACT**:
- **Reliable Login**: E2E tests can now consistently login with Mantine UI components
- **Developer Experience**: Clear, reusable helper reduces test development time
- **Debugging Tools**: Comprehensive error analysis and performance monitoring
- **Future-Proof**: Multiple fallback strategies ensure robustness against UI changes

**NEXT STEPS**:
1. Update existing E2E tests to use the new AuthHelper
2. Replace failing login attempts with the working data-testid approach  
3. Apply console error filtering to other tests experiencing Mantine CSS warnings
4. Document these patterns in the Playwright testing standards

**TAGS**: #mantine-ui #authentication #playwright #data-testid #css-warnings #e2e-testing #login-solution

---

## 📋 Vetting System - Comprehensive E2E Workflow Test

**Location**: `/tests/e2e/vetting-system.spec.ts`
**Purpose**: Complete end-to-end testing of the vetting system happy path workflow from discovery to approval
**Status**: ✅ Active | **Added**: 2025-09-22 | **Updated**: 2025-09-22 - Fixed form validation testing

### Test Description
This comprehensive test covers the complete vetting system workflow:
1. Guest user discovers vetting requirement while logged out
2. Logs in and navigates to "How to Join" page
3. Fills out and submits complete vetting application with all fields
4. Admin reviews application, adds notes, and approves application
5. Guest user sees updated status and next steps

### Test Cases

#### 1. **Complete vetting workflow from discovery to approval**
**Purpose**: Tests the full happy path from initial discovery through admin approval
**Test Steps**:
- ✅ Navigate to "How to Join" page while logged out - verifies login requirement message
- ✅ Click login button and authenticate as guest user (`guest@witchcityrope.com`)
- ✅ Navigate back to "How to Join" via navigation link
- ✅ Fill out complete vetting application including all required and optional fields:
  - **Required**: Real name, "Why would you like to join" essay, experience with rope essay, community standards agreement checkbox
  - **Optional**: Pronouns, FetLife handle, other names/handles
  - **Form Validation**: Tests that submit button is disabled until ALL required fields are filled
  - Email, experience with rope, safety training, reason for joining
  - Agreement to community standards
- ✅ Submit application and verify success
- ✅ Check dashboard for application confirmation
- ✅ Verify "How to Join" page shows submitted status and next steps
- ✅ Switch to admin user (`admin@witchcityrope.com`)
- ✅ Navigate to vetting admin area
- ✅ Find and view guest's application
- ✅ Verify application data integrity
- ✅ Add admin note and save
- ✅ Refresh page to confirm note persistence
- ✅ Approve application to "Approved to Schedule Interview" status
- ✅ Refresh page to confirm status change
- ✅ Switch back to guest user
- ✅ Verify updated status shown in dashboard and "How to Join" page

#### 2. **Vetting system accessibility and error handling**
**Purpose**: Tests form accessibility attributes and error scenarios
**Test Steps**:
- ✅ Check for proper form labels and aria-labels
- ✅ Test form validation on empty submission
- ✅ Verify error messages are properly displayed
- ✅ Test error page navigation (Return Home, Reload buttons)

### Key Features Tested
- **Authentication Integration**: Seamless login/logout between guest and admin users
- **Form Functionality**: All vetting form fields including new optional fields (pronouns, other names)
- **Data Persistence**: Application data integrity from submission through admin review
- **Admin Workflow**: Complete admin review process including notes and status changes
- **User Experience**: Status updates and next steps clearly communicated to users
- **Error Handling**: Graceful handling of API errors and user guidance
- **Navigation**: Consistent navigation between pages and user roles

### Error Scenarios Handled
- ✅ API 500 errors during application check - test continues with admin workflow
- ✅ Existing applications - test verifies status display instead of form
- ✅ Missing form elements - fallback selectors and graceful degradation
- ✅ Authentication failures - clear error reporting and recovery

### Technical Notes
- Uses AuthHelper for reliable authentication across user roles
- Includes comprehensive screenshot capture for debugging
- Handles multiple UI states (logged out, error pages, existing applications)
- Tests both required and optional form fields
- Verifies data flow from frontend through API to admin interface

**DEPENDENCIES**:
- Docker containers running (web:5173, api:5655, postgres:5433)
- Test accounts: guest@witchcityrope.com, admin@witchcityrope.com
- AuthHelper from `/tests/e2e/helpers/auth.helper.ts`

**TAGS**: #vetting #e2e-testing #user-journey #admin-workflow #form-testing #authentication #happy-path

---

*For test writing best practices, see [lessons-learned/test-writers.md](/docs/lessons-learned/test-writers.md)*