# Authentication Testing Guide

## Overview

This document describes the testing approach for the WitchCityRope authentication system, which has been modernized to use ASP.NET Core Identity UI scaffolding instead of custom authentication implementation.

## Background

The original authentication system had a logout bug where clicking logout didn't actually log users out - the navigation menu would continue to show "my dashboard" and the user dropdown menu remained visible. This was due to complex JavaScript manipulation of Blazor authentication state.

The authentication system has been modernized to use Microsoft's standard Identity UI patterns, which are more reliable and follow established best practices.

## Test Files

### 1. Comprehensive Login Test
**File**: `test-identity-login-comprehensive.js`
- **Purpose**: Tests the complete login flow using Identity UI
- **Scope**: Login page navigation, form filling, submission, and post-login state verification
- **URL**: `http://localhost:5651/Identity/Account/Login`
- **Features**:
  - Network request monitoring
  - Console message capture
  - Screenshot generation
  - Detailed reporting

### 2. Logout Functionality Test
**File**: `test-identity-logout.js`
- **Purpose**: Specifically tests the logout functionality fix
- **Scope**: Login → Logout → Verification of logout success
- **Features**:
  - Tests new Identity UI logout forms
  - Verifies user menu disappearance
  - Confirms redirect to public pages
  - Screenshot documentation

### 3. Integration Test Suite
**File**: `test-auth-integration.js`
- **Purpose**: Runs multiple authentication tests in sequence
- **Scope**: Complete authentication flow validation
- **Features**:
  - Automated test execution
  - Summary reporting
  - Overall system health assessment

## Test Credentials

All tests use the seeded admin account:
- **Email**: `admin@witchcityrope.com`
- **Password**: `Test123!`

Additional test accounts available:
- **Teacher**: `teacher@witchcityrope.com` / `Test123!`
- **Vetted Member**: `vetted@witchcityrope.com` / `Test123!`
- **General Member**: `member@witchcityrope.com` / `Test123!`

## Running Tests

### Prerequisites
1. Ensure the application is running:
   ```bash
   cd /home/chad/repos/witchcityrope/WitchCityRope
   dotnet run --project src/WitchCityRope.Web
   ```

2. Install Puppeteer (if not already installed):
   ```bash
   npm install puppeteer
   ```

### Individual Tests

```bash
# Run comprehensive login test
node test-identity-login-comprehensive.js

# Run logout functionality test  
node test-identity-logout.js

# Run full integration test suite
node test-auth-integration.js
```

### Expected Behavior

#### Successful Login Test
- ✅ Navigation to Identity login page succeeds
- ✅ Email and password fields found and filled
- ✅ Form submission successful
- ✅ Redirect away from login page
- ✅ No error messages displayed
- ✅ User dropdown menu appears

#### Successful Logout Test
- ✅ Login successful (prerequisite)
- ✅ User dropdown menu found and opened
- ✅ Logout form found with correct action (`/Identity/Account/Logout`)
- ✅ Logout form submission successful
- ✅ User dropdown menu disappears
- ✅ Login link appears or redirect to public page
- ✅ **Original logout bug is FIXED**

## Changes from Previous Implementation

### Before (Custom Authentication)
- **Login URL**: `/identity/account/login`
- **Logout Method**: Complex JavaScript manipulation of Blazor circuits
- **Authentication State**: Custom AuthenticationStateProvider with manual cookie checks
- **Issues**: Logout didn't properly clear authentication state

### After (Identity UI)
- **Login URL**: `/Identity/Account/Login`
- **Logout Method**: Standard HTML form POST to `/Identity/Account/Logout`
- **Authentication State**: Standard `RevalidatingServerAuthenticationStateProvider`
- **Benefits**: Reliable, follows Microsoft patterns, fixes logout bug

## Technical Details

### Authentication Flow
1. User navigates to `/Identity/Account/Login`
2. Fills `#Input_Email` and `#Input_Password` fields
3. Submits form via `button[type="submit"]`
4. Identity UI handles authentication
5. Redirects to dashboard or configured return URL

### Logout Flow
1. User clicks dropdown trigger to open user menu
2. Clicks logout button in form: `form[action*="/Identity/Account/Logout"]`
3. Form submits POST request to Identity UI endpoint
4. Identity UI clears authentication cookies
5. Redirects to homepage or login page
6. UI updates to show logged-out state

### Key Elements
- **Email Field**: `#Input_Email` (Identity UI standard)
- **Password Field**: `#Input_Password` (Identity UI standard)
- **Login Form**: Standard HTML form with method="post"
- **Logout Form**: `<form method="post" action="/Identity/Account/Logout">`

## Troubleshooting

### Common Issues

1. **Test fails to find email/password fields**
   - Verify application is running on correct port
   - Check that Identity UI is properly scaffolded
   - Ensure viewport is set to desktop size (1280x800)

2. **Logout test fails**
   - Verify user dropdown menu can be opened
   - Check that logout form has correct action attribute
   - Ensure test account has proper permissions

3. **Network timeouts**
   - Increase timeout values in test scripts
   - Check server responsiveness
   - Verify database connectivity

### Debug Mode
Run tests with headed browser for visual debugging:
```javascript
// In test file, change:
headless: false,
devtools: true
```

## Reporting

Tests generate detailed reports including:
- **Screenshots**: Before/after each major step
- **Network Activity**: All authentication-related requests
- **Console Messages**: Browser console output
- **Final Assessment**: Overall test success/failure

Reports are saved to:
- `identity-login-test-results/`
- `auth-integration-results/`

## Validation

The authentication tests validate:
- ✅ Login functionality works correctly
- ✅ Logout functionality works correctly  
- ✅ Original logout bug is fixed
- ✅ Navigation menu state updates properly
- ✅ Authentication cookies are managed correctly
- ✅ User experience is smooth and reliable

## Next Steps

1. Run tests after any authentication-related changes
2. Add tests for additional scenarios (registration, password reset)
3. Integrate tests into CI/CD pipeline
4. Monitor authentication metrics in production