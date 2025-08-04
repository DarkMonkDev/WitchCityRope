# Authentication System - Test Coverage
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Overview
This document lists all tests covering the authentication system functionality. Tests are organized by type and location.

## Unit Tests

### Location: `/tests/WitchCityRope.Core.Tests/`

#### User Entity Tests
- `Entities/UserTests.cs`
  - ✅ User creation with valid data
  - ✅ Scene name validation
  - ✅ Age calculation from date of birth
  - ✅ IsVetted flag behavior
  - ✅ Full name encryption/decryption

### Location: `/tests/WitchCityRope.Api.Tests/`

#### Authentication Service Tests
- `Services/AuthServiceTests.cs`
  - ✅ Valid login returns JWT token
  - ✅ Invalid credentials return null
  - ✅ Locked account prevents login
  - ✅ Inactive account prevents login
  - ✅ Token generation includes correct claims
  - ✅ Refresh token generation and validation

#### User Service Tests  
- `Services/UserServiceTests.cs`
  - ✅ Create user with unique email
  - ✅ Duplicate email throws exception
  - ✅ Scene name uniqueness validation
  - ✅ Password hashing verification
  - ✅ User role assignment
  - ✅ Update last login timestamp

### Location: `/tests/WitchCityRope.Web.Tests/`

#### Login Component Tests
- `Features/Auth/LoginComponentTests.cs`
  - ✅ Login form renders correctly
  - ✅ Validation messages display
  - ✅ Redirect to Razor Pages on submit
  - ✅ Remember me checkbox functionality

#### Auth State Tests
- `Helpers/AuthenticationTestContext.cs`
  - ✅ Mock authenticated user context
  - ✅ Role-based authorization testing
  - ✅ Claims principal generation

## Integration Tests

### Location: `/tests/WitchCityRope.IntegrationTests/`

#### Authentication Flow Tests
- `AuthenticationTests.cs`
  - ✅ Login page accessible anonymously
  - ✅ Valid login creates auth cookie
  - ✅ Invalid login returns to form with error
  - ✅ Logout clears authentication
  - ✅ Protected pages require authentication
  - ✅ Remember me extends cookie expiration

#### Authorization Tests
- `AuthorizationTests.cs`
  - ✅ Admin pages require admin role
  - ✅ Vetted member content requires vetting
  - ✅ 403 returned for insufficient permissions
  - ✅ Role-based menu visibility

#### API Authentication Tests
- `ApiAuthenticationTests.cs`
  - ✅ Login endpoint returns JWT
  - ✅ Protected endpoints require valid JWT
  - ✅ Expired token returns 401
  - ✅ Refresh token extends session
  - ✅ Invalid refresh token requires re-login

## E2E Tests (Playwright)

### Location: `/tests/playwright/tests/`

#### Login Flow Tests
- `auth/login.spec.ts`
  - ✅ Complete login flow with valid credentials
  - ✅ Failed login shows error message
  - ✅ Account lockout after 5 attempts
  - ✅ Remember me persists across browser restart
  - ✅ Login with email or username
  - ✅ Redirect to original page after login

#### Registration Flow Tests
- `auth/registration.spec.ts`
  - ✅ Successful registration with valid data
  - ✅ Age verification prevents under-21 registration
  - ✅ Duplicate email shows error
  - ✅ Duplicate scene name shows error
  - ✅ Password complexity requirements enforced
  - ✅ Auto-login after registration

#### Profile Management Tests
- `auth/profile.spec.ts`
  - ✅ Update scene name (if unique)
  - ✅ Change password with current password
  - ✅ Update profile information
  - ❌ Email change verification (not implemented)

#### Authorization Tests
- `auth/authorization.spec.ts`
  - ✅ Anonymous users redirected to login
  - ✅ Admin menu only visible to admins
  - ✅ Vetting required message for social events
  - ✅ Teacher dashboard tab visibility

## Performance Tests

### Location: `/tests/WitchCityRope.PerformanceTests/`

#### Load Tests
- `LoadTests/AuthenticationLoadTests.cs`
  - ✅ Concurrent login requests
  - ✅ JWT generation performance
  - ✅ Session creation overhead
  - ✅ Database connection pooling

## Security Tests

### Manual Security Testing
- ✅ SQL injection in login form
- ✅ XSS in scene name display
- ✅ CSRF token validation
- ✅ Secure cookie flags in production
- ✅ Password in logs/errors (should not appear)
- ✅ Timing attacks on login

## Test Data

### Test Users (Seeded in Test Database)
```
admin@witchcityrope.com / Test123! (Admin, Vetted)
member@witchcityrope.com / Test123! (Member, Vetted)
user@witchcityrope.com / Test123! (Member, Not Vetted)
teacher@witchcityrope.com / Test123! (Teacher, Vetted)
```

## Coverage Summary

### Well Tested ✅
- Basic authentication flows
- Role-based authorization
- Password validation
- Account lockout
- JWT token generation
- Cookie authentication

### Needs More Testing ⚠️
- Email verification flow (not implemented)
- Password reset flow (not implemented)
- Two-factor authentication (not implemented)
- OAuth login (not implemented)
- Concurrent session handling
- Token refresh edge cases

### Not Tested ❌
- 2FA flows (feature not implemented)
- Social login (feature not implemented)
- Email confirmation (feature not enforced)
- Account deletion (GDPR compliance)

## Running Tests

### Unit Tests
```bash
dotnet test tests/WitchCityRope.Core.Tests/
dotnet test tests/WitchCityRope.Api.Tests/
dotnet test tests/WitchCityRope.Web.Tests/
```

### Integration Tests
```bash
# Start Docker first
docker-compose up -d

# Run tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

### E2E Tests
```bash
# Start application
docker-compose up -d

# Run Playwright tests
cd tests/playwright
npm test -- tests/auth/
```

### Security Tests
```bash
# Manual testing required
# Use OWASP ZAP or similar tools
```

---

*For test writing guidelines, see [lessons-learned/test-writers.md](/docs/lessons-learned/test-writers.md)*