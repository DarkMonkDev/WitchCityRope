# Admin User Management Testing Guide

This guide provides instructions for running the focused tests for admin user management functionality in WitchCityRope.

## Overview

We've created a comprehensive test suite that covers:

1. **Unit Tests** - ApiClient methods for user management with proper mocking
2. **Integration Tests** - Real HTTP calls to admin/users API endpoints with authentication
3. **E2E Tests** - Complete browser automation testing with real API integration

## Prerequisites

### 1. Development Environment Setup
```bash
# Ensure Docker is running (required for integration tests)
sudo systemctl start docker

# Start the development environment
./dev.sh
# OR manually:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 2. Install Test Dependencies
```bash
# Install Playwright dependencies
npm ci
npx playwright install --with-deps
```

### 3. Verify Application is Running
- **Web Application**: http://localhost:5651
- **API Application**: http://localhost:5653
- **Database**: PostgreSQL at localhost:5433

## Test Files Created

### Unit Tests
- **File**: `/tests/WitchCityRope.Web.Tests/Services/ApiClientTests.cs`
- **Extended with**: 11 new test methods for user management API client functions

### Integration Tests  
- **File**: `/tests/WitchCityRope.IntegrationTests/Admin/AdminUsersControllerTests.cs`
- **New file with**: 14 comprehensive integration tests

### E2E Tests
- **File**: `/tests/playwright/admin-user-management-api-integration.spec.ts`
- **New file with**: 7 end-to-end test scenarios

## Running the Tests

### 1. Unit Tests Only
```bash
# Run all unit tests
dotnet test tests/WitchCityRope.Web.Tests/

# Run only ApiClient tests
dotnet test tests/WitchCityRope.Web.Tests/ --filter "FullyQualifiedName~ApiClientTests"

# Run only the new user management unit tests
dotnet test tests/WitchCityRope.Web.Tests/ --filter "FullyQualifiedName~ApiClientTests" --filter "TestCategory=Unit"

# Run with detailed output
dotnet test tests/WitchCityRope.Web.Tests/Services/ApiClientTests.cs --logger "console;verbosity=detailed"
```

### 2. Integration Tests

**CRITICAL**: Always run health checks first before integration tests:

```bash
# Step 1: Run health checks to verify containers are ready
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# Step 2: ONLY if health checks pass - Run admin user integration tests
dotnet test tests/WitchCityRope.IntegrationTests/Admin/AdminUsersControllerTests.cs

# Step 3: Run all integration tests (if desired)
dotnet test tests/WitchCityRope.IntegrationTests/

# Run with detailed output for debugging
dotnet test tests/WitchCityRope.IntegrationTests/Admin/AdminUsersControllerTests.cs --logger "console;verbosity=detailed"
```

### 3. E2E Tests (Playwright)

**Ensure the application is running first** (http://localhost:5651):

```bash
# Run only the new admin user management E2E test
npx playwright test admin-user-management-api-integration.spec.ts

# Run with UI mode for debugging
npx playwright test admin-user-management-api-integration.spec.ts --ui

# Run in headed mode to see browser
npx playwright test admin-user-management-api-integration.spec.ts --headed

# Run specific test case
npx playwright test admin-user-management-api-integration.spec.ts -g "should login as admin and verify real users load"

# Run all admin tests
npm run test:admin
# OR
npx playwright test tests/playwright/admin/

# Generate test report
npx playwright show-report
```

### 4. Run All Admin User Management Tests

```bash
# Complete test sequence for admin user management
echo "=== Running Unit Tests ==="
dotnet test tests/WitchCityRope.Web.Tests/ --filter "FullyQualifiedName~ApiClientTests"

echo "=== Running Health Checks ==="
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

echo "=== Running Integration Tests ==="
dotnet test tests/WitchCityRope.IntegrationTests/Admin/AdminUsersControllerTests.cs

echo "=== Running E2E Tests ==="
npx playwright test admin-user-management-api-integration.spec.ts
```

## Test Scenarios Covered

### Unit Tests (ApiClient)
✅ **GetUsersAsync** with search parameters and pagination  
✅ **GetUsersAsync** with role filtering  
✅ **GetUsersAsync** error handling  
✅ **GetUserByIdAsync** with valid and invalid IDs  
✅ **UpdateUserAsync** success and error cases  
✅ **ResetUserPasswordAsync** functionality  
✅ **ManageUserLockoutAsync** lock and unlock operations  
✅ **GetUserStatsAsync** statistics retrieval  
✅ **GetAvailableRolesAsync** role dropdown data  

### Integration Tests (Real API Calls)
✅ **Authentication/Authorization** - Admin access required  
✅ **User Listing** - Paginated results with real data  
✅ **Search Functionality** - Filter users by search term  
✅ **Role Filtering** - Filter users by specific roles  
✅ **User Details** - Get individual user information  
✅ **User Updates** - Modify user properties (role, status, pronouns)  
✅ **Password Reset** - Admin password reset functionality  
✅ **User Lockout** - Lock and unlock user accounts  
✅ **Statistics** - User management dashboard stats  
✅ **Roles** - Available roles for dropdowns  
✅ **End-to-End Workflow** - Complete user management flow  

### E2E Tests (Browser Automation)
✅ **Admin Login** - Using correct ASP.NET Core Identity selectors  
✅ **Real API Data Loading** - Verify users load from API (not mock data)  
✅ **Search Functionality** - Test search input and results  
✅ **User Details** - Click user row to view details  
✅ **Role Filtering** - Test role filter dropdown  
✅ **Pagination** - Test page navigation if applicable  
✅ **Statistics Verification** - Verify stats cards show real numbers  
✅ **Responsive Design** - Test mobile, tablet, and desktop viewports  

## Test Accounts

These accounts are seeded and available for testing:

- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Guest/Attendee**: guest@witchcityrope.com / Test123!

## Troubleshooting

### Integration Tests Failing
1. **Check Docker**: Ensure Docker is running and containers are up
2. **Run Health Checks**: Always run health checks first
3. **Clean Containers**: `docker system prune` to clean up old containers
4. **Check Database**: Verify PostgreSQL container is healthy

### E2E Tests Failing  
1. **Application Running**: Ensure web app is accessible at http://localhost:5651
2. **Login Selectors**: Tests use correct Identity form selectors (fixed 2025-08-13)
3. **Screenshots**: Check `/tests/playwright/test-results/` for debugging screenshots
4. **Timing Issues**: Tests use simple waits, may need adjustment for slower environments

### Unit Tests Failing
1. **Dependencies**: Check NuGet package references
2. **Build Issues**: Ensure solution builds successfully
3. **Missing DTOs**: Verify all DTO classes are available

## Expected Test Results

### Success Criteria

**Unit Tests**: All 11+ user management ApiClient tests should pass
**Integration Tests**: All 14 admin users controller tests should pass  
**E2E Tests**: All 7 browser automation scenarios should pass

### Key Validations

1. **API Integration**: Verify real data loads from AdminUsersController
2. **Authentication**: Confirm admin-only access is enforced
3. **Search/Filter**: Validate search and role filtering work with API
4. **User Operations**: Confirm CRUD operations work end-to-end
5. **Error Handling**: Verify appropriate error responses
6. **Authorization**: Confirm non-admin users can't access endpoints

## Debugging Tips

### Enable Verbose Logging
```bash
# Integration tests with detailed logging
dotnet test --logger "console;verbosity=detailed"

# E2E tests with debug output
DEBUG=pw:api npx playwright test admin-user-management-api-integration.spec.ts
```

### View Test Artifacts
- **Integration Test Logs**: Console output shows detailed API calls
- **E2E Screenshots**: `/tests/playwright/test-results/` directory
- **Playwright Report**: `npx playwright show-report`

### Common Issues
- **Database Connection**: Check PostgreSQL container health
- **Authentication Failures**: Verify seeded admin user exists
- **API Timeouts**: Increase test timeouts if running in slow environment
- **Element Not Found**: E2E tests may need selector adjustments

## Next Steps

After running these tests successfully:

1. **Verify Coverage**: Ensure all user management features are tested
2. **Add More Scenarios**: Consider edge cases specific to your use case
3. **Performance Testing**: Add load tests for user management endpoints
4. **Security Testing**: Verify authorization edge cases
5. **Accessibility Testing**: Add accessibility checks to E2E tests

## Documentation Updated

The following documentation has been updated to reflect these new tests:

- **Test Catalog**: `/docs/standards-processes/testing/TEST_CATALOG.md`
- **Lessons Learned**: Contributing to test writer knowledge base

---

*These tests focus specifically on admin user management functionality and complement the existing test suite without running unrelated tests.*