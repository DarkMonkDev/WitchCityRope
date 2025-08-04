# WitchCityRope QA Bug Report
**Date:** July 7, 2025  
**QA Engineer:** Claude Code  
**Environment:** Docker Development Environment (Ubuntu 24.04)

## Executive Summary
Comprehensive testing revealed **5 critical bugs** and **3 medium-priority issues** that prevent the test suite from running successfully. The main issues are architectural inconsistencies, outdated URL paths, and compilation errors in the test infrastructure.

## Critical Bugs (🔴 High Priority)

### **BUG-001: Test Suite Compilation Failure**
**Status:** 🔴 CRITICAL  
**Component:** Test Infrastructure  
**Description:** All .NET test projects fail to compile due to missing `User` entity in Core module.

**Root Cause:** The `User` entity is located in `WitchCityRope.Api.Features.Auth.Models` but test code expects it in `WitchCityRope.Core.Entities`. This violates Clean Architecture principles.

**Affected Files:**
- `tests/WitchCityRope.Tests.Common/Builders/UserBuilder.cs`
- `tests/WitchCityRope.Tests.Common/Builders/EventBuilder.cs`  
- `tests/WitchCityRope.Tests.Common/Builders/RegistrationBuilder.cs`
- `tests/WitchCityRope.Tests.Common/TestDoubles/InMemoryUserRepository.cs`
- `tests/WitchCityRope.Tests.Common/Interfaces/TestInterfaces.cs`
- `tests/WitchCityRope.Tests.Common/TestDoubles/TestJwtService.cs`

**Error Messages:**
```
CS0246: The type or namespace name 'User' could not be found (are you missing a using directive or an assembly reference?)
```

**Impact:** ALL .NET tests are blocked from running.

### **BUG-002: Outdated Authentication URLs in Tests**
**Status:** 🔴 CRITICAL  
**Component:** UI Tests (Puppeteer)  
**Description:** 161 test files still use `/identity/account/login` instead of `/identity/account/login`.

**Root Cause:** Authentication URL structure was changed but tests weren't updated.

**Affected Test Results:**
- ❌ `test-login-comprehensive.js` - 404 errors on /identity/account/login
- ❌ `test-auth-simple.js` - "No element found for selector: input[type='password']"
- ❌ `tests/ui/test-admin-quick.js` - Login failures
- ❌ All Puppeteer tests that require authentication

**Evidence:**
```
📡 GET http://localhost:5651/identity/account/login
📨 Response: 200 OK - http://localhost:5651/identity/account/login
🖥️ Console [error]: Failed to load resource: the server responded with a status of 404 (Not Found)
```

**Impact:** ALL UI/E2E tests fail at login step.

### **BUG-003: Integration Test Compilation Errors**
**Status:** 🔴 CRITICAL  
**Component:** Integration Tests  
**Description:** Multiple CS1503 errors in integration test methods.

**Root Cause:** Incorrect parameter types being passed to assertion methods.

**Affected Files:**
- `tests/WitchCityRope.IntegrationTests/ProtectedRouteNavigationTests.cs:314,343,370`
- `tests/WitchCityRope.IntegrationTests/UserMenuIntegrationTests.cs:400,438,468`

**Error Messages:**
```
CS1503: Argument 3: cannot convert from 'string' to 'System.Net.HttpStatusCode'
CS1503: Argument 4: cannot convert from 'string' to 'System.Net.HttpStatusCode'
```

**Impact:** Integration tests cannot compile or run.

### **BUG-004: Missing Password Input Field**
**Status:** 🔴 CRITICAL  
**Component:** Authentication UI  
**Description:** Login page at `/identity/account/login` returns "Not found" page instead of login form.

**Root Cause:** The page shows a "Not found" title but still renders some form elements. However, the password input field is missing.

**Evidence:**
```javascript
Page info: {
  "title": "Not found",
  "url": "http://localhost:5651/identity/account/login",
  "forms": 1,
  "inputs": [
    {
      "type": "hidden",
      "name": "__RequestVerificationToken"
    },
    {
      "type": "email",
      "name": "",
      "id": "",
      "placeholder": "Your email"
    }
  ]
  // No password input found
}
```

**Impact:** Manual and automated login attempts fail.

### **BUG-005: Authentication Page Routing Issues**
**Status:** 🔴 CRITICAL  
**Component:** Web Application Routing  
**Description:** `/identity/account/login` endpoint returns 404 resources but still serves HTML content.

**Root Cause:** Routing configuration inconsistency between old and new authentication paths.

**Evidence:**
```
🖥️ Console [error]: Failed to load resource: the server responded with a status of 404 (Not Found)
```

**Impact:** Authentication flow is broken, causing cascade failures in all dependent features.

## Medium Priority Issues (🟡 Medium)

### **BUG-006: XUnit Test Runner Version Mismatch**
**Status:** 🟡 MEDIUM  
**Component:** Test Infrastructure  
**Description:** Test projects expect xunit.runner.visualstudio 2.9.3 but 3.0.0 is resolved.

**Error Messages:**
```
NU1603: WitchCityRope.Api.Tests depends on xunit.runner.visualstudio (>= 2.9.3) but xunit.runner.visualstudio 2.9.3 was not found. xunit.runner.visualstudio 3.0.0 was resolved instead.
```

**Impact:** Test runner compatibility issues (minor).

### **BUG-007: Nullable Reference Warnings**
**Status:** 🟡 MEDIUM  
**Component:** Integration Tests  
**Description:** Potential null reference warnings in authentication tests.

**Affected Files:**
- `tests/WitchCityRope.IntegrationTests/AuthenticationTests.cs:155,327,330`

**Error Messages:**
```
CS8602: Dereference of a possibly null reference
CS8618: Non-nullable property 'Token' must contain a non-null value when exiting constructor
CS8618: Non-nullable property 'ErrorMessage' must contain a non-null value when exiting constructor
```

**Impact:** Code quality and potential runtime issues.

### **BUG-008: Blazor SignalR Connection Errors**
**Status:** 🟡 MEDIUM  
**Component:** Web Application  
**Description:** Console errors about failed resource loading and Blazor connection issues.

**Evidence:**
```
🖥️ Console [error]: Failed to load resource: the server responded with a status of 404 (Not Found)
🖥️ Console [info]: [2025-07-07T23:18:24.264Z] Information: WebSocket connected to ws://localhost:5651/_blazor?id=...
```

**Impact:** Potential UI responsiveness issues.

## Test Coverage Analysis

### **Current Test Status:**
- **Core Tests:** ❌ BLOCKED (compilation errors)
- **Infrastructure Tests:** ❌ BLOCKED (compilation errors)  
- **API Tests:** ❌ BLOCKED (compilation errors)
- **Integration Tests:** ❌ BLOCKED (compilation errors)
- **UI Tests (Puppeteer):** ❌ BLOCKED (auth path issues)
- **Performance Tests:** ❌ BLOCKED (auth path issues)

### **Test Coverage Impact:**
- **Current Coverage:** 0% (all tests blocked)
- **Expected Coverage:** 80%+ (per project standards)
- **Regression Risk:** HIGH (no working tests to validate changes)

## Recommendations

### **Immediate Actions Required:**
1. **Fix User Entity Architecture** (BUG-001)
2. **Update Authentication URLs** (BUG-002)  
3. **Fix Integration Test Parameters** (BUG-003)
4. **Resolve Authentication Routing** (BUG-004, BUG-005)

### **Secondary Actions:**
1. **Update XUnit Dependencies** (BUG-006)
2. **Add Null Reference Checks** (BUG-007)
3. **Investigate Blazor Resource Loading** (BUG-008)

## Next Steps
1. **Phase 1:** Fix architectural issues (User entity placement)
2. **Phase 2:** Update all test URLs from `/identity/account/login` to `/identity/account/login`
3. **Phase 3:** Fix compilation errors in integration tests
4. **Phase 4:** Validate authentication routing works end-to-end
5. **Phase 5:** Run full test suite to verify all fixes

## Test Environment Status
- **Docker Containers:** ✅ All healthy
- **Web Application:** ✅ Running (http://localhost:5651)
- **API:** ✅ Running (http://localhost:5653)
- **Database:** ✅ Running (PostgreSQL on 5433)
- **Test Infrastructure:** ❌ BLOCKED by above bugs

---
**QA Sign-off:** This report documents all identified bugs before any fixes are attempted. Solutions will be analyzed and implemented in subsequent phases.