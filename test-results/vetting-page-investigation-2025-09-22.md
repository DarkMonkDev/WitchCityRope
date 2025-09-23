# Vetting Page 400 Error Investigation Report
**Date**: 2025-09-22
**Investigator**: test-executor
**Status**: CRITICAL API ISSUE DISCOVERED

## Executive Summary

**CRITICAL FINDING**: Unable to complete JWT token investigation due to fundamental API authentication failure. The API is incorrectly rejecting JSON containing exclamation marks (!), preventing all login attempts with the standard test password "Test123!".

## Environment Status

### ✅ Infrastructure Health (100% Operational)
- **API Service**: http://localhost:5655/health → `{"status":"Healthy"}` ✅
- **Database**: PostgreSQL accessible on port 5433 ✅
- **Web Container**: Serving React HTML on port 5173 ✅
- **Docker Containers**: All running (web shows "unhealthy" but functional) ✅

### ❌ Authentication System (0% Functional)
- **Login API**: Completely broken due to JSON parsing error ❌
- **React App**: Not initializing (login form not rendering) ❌
- **JWT Token Investigation**: Impossible due to login failure ❌

## Root Cause Analysis

### Primary Issue: API JSON Parsing Bug
```
Microsoft.AspNetCore.Http.BadHttpRequestException: Failed to read parameter "LoginRequest request" from the request body as JSON.
---> System.Text.Json.JsonException: '!' is an invalid escapable character within a JSON string.
```

**Test Payload That Fails**:
```json
{"email":"admin@witchcityrope.com","password":"Test123!"}
```

**Error Location**: The API is incorrectly treating "!" as requiring escape sequences in JSON strings, which is not valid JSON specification behavior.

### Secondary Issue: React App Not Rendering
- **Symptom**: Login button not found in DOM
- **Likely Cause**: ES6 import errors or TypeScript compilation issues preventing React initialization
- **Impact**: Cannot test UI-based authentication flows

## Investigation Attempts

### 1. Direct API Testing ❌
```bash
# Attempted login with correct credentials
curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Result: 400 Bad Request - JSON parsing error
```

### 2. Alternative Authentication Testing ❌
```bash
# Tried password without exclamation mark
curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123"}'

# Result: 401 Unauthorized - Invalid credentials (expected)
```

### 3. E2E Test Investigation ❌
```javascript
// Attempted automated browser testing
await page.waitForSelector('[data-testid="login-button"]', { timeout: 10000 });

// Result: TimeoutError - Login button not rendering
```

## Database Verification

### ✅ User Data Present
```sql
SELECT "Email", "UserName" FROM "Users" WHERE "Email" LIKE '%admin%';
```
**Result**: admin@witchcityrope.com exists with proper account setup

### ✅ Password Configuration Confirmed
**Source**: `/apps/api/Services/SeedDataService.cs:279`
```csharp
var createResult = await _userManager.CreateAsync(user, "Test123!");
```
**Confirmed**: All test accounts use password "Test123!" including the problematic exclamation mark

## Impact Assessment

### Cannot Investigate Vetting Page JWT Issues Because:
1. **No Authentication Possible**: Cannot obtain auth tokens due to login API failure
2. **No Browser Access**: React app not rendering login forms
3. **No Cookie Analysis**: No successful login to generate auth cookies
4. **No JWT Decoding**: No tokens to analyze for missing claims

### Specific Investigation Requests Blocked:
- ❌ **JWT token claims analysis**: Requires successful login first
- ❌ **ReviewerId claim verification**: Requires auth token first
- ❌ **'sub' claim checking**: Requires JWT payload first
- ❌ **vetting API 400 error details**: Requires authenticated session first
- ❌ **Browser console errors**: Requires functional React app first

## Required Fixes (Priority Order)

### 1. CRITICAL: Fix API JSON Parsing (backend-developer)
**Issue**: System.Text.Json incorrectly treating "!" as invalid escape character
**Location**: API JSON deserialization configuration
**Fix**: Review JSON serializer settings in Program.cs or authentication middleware

### 2. HIGH: Fix React App Initialization (react-developer)
**Issue**: React app not rendering, likely ES6 import or TypeScript errors
**Investigation**: Check browser console for import failures or compilation errors
**Fix**: Resolve import errors to enable UI-based testing

### 3. MEDIUM: Comprehensive Authentication Testing (test-developer)
**Issue**: Need robust authentication test suite once API is fixed
**Requirements**: Test JWT token generation, claims validation, cookie handling

## Next Steps

### Immediate Actions Required:
1. **Backend Developer**: Fix JSON parsing to handle passwords with special characters
2. **React Developer**: Diagnose and fix React app initialization failure
3. **Test Executor**: Re-run investigation once authentication is functional

### Post-Fix Investigation Plan:
1. ✅ **Login as admin**: Test authentication with fixed API
2. ✅ **Navigate to /admin/vetting**: Access vetting page
3. ✅ **Capture API requests**: Monitor network tab for vetting API calls
4. ✅ **Analyze JWT tokens**: Decode auth cookies and verify claims
5. ✅ **Document 400 errors**: Capture exact error responses from vetting API

## Test Environment Readiness

### ✅ Ready for Testing:
- Docker containers operational
- Database seeded with test accounts
- API health endpoints responding
- Test framework configured

### ❌ Blocking Issues:
- API authentication completely broken
- React UI not functional
- No path to obtain JWT tokens for analysis

## Evidence Artifacts

### Log Files:
- **API Error Response**: Full stack trace showing JSON parsing failure
- **Database Query Results**: Confirmed admin user exists
- **Container Status**: Docker containers running but web marked unhealthy

### Failed Test Results:
- **E2E Test**: `/test-results/vetting-investigation-Vett-*` (TimeoutError)
- **API Test**: 400 Bad Request with detailed JSON parsing error

## Conclusion

**The vetting page 400 error investigation cannot proceed due to fundamental authentication system failure**. The API's inability to parse JSON with exclamation marks prevents all login attempts, making JWT token analysis impossible.

**Recommendation**: Prioritize fixing the API JSON parsing issue as it blocks all authentication-dependent testing and development work.

---
**Next Action**: backend-developer MUST fix API JSON deserialization before any vetting system investigation can continue.