# Regression Fixes Required - PostgreSQL Migration

## Priority 1: Compilation Errors (Blocking All API Tests)

### Issue: UserWithAuth Namespace Mismatch
**Files affected:**
- `/tests/WitchCityRope.Api.Tests/Features/Auth/LoginCommandTests.cs`
- `/tests/WitchCityRope.Api.Tests/Services/AuthServiceTests.cs`

**Problem:** Tests are looking for `UserWithAuth` in namespace `WitchCityRope.Api.Features.Auth.Services` but it's actually in `WitchCityRope.Api.Features.Auth.Models`

**Fix Required:**
```csharp
// Change from:
WitchCityRope.Api.Features.Auth.Services.UserWithAuth
// To:
WitchCityRope.Api.Features.Auth.Models.UserWithAuth
```

## Priority 2: Database Migration Conflicts

### Issue: "relation 'Events' already exists"
**Root Cause:** Test database not being properly cleaned between test runs

**Fixes Required:**
1. Update test database initialization to drop existing database before migrations
2. Add proper cleanup in test fixtures
3. Consider using unique database names per test run

**Code Location:** 
- `/tests/WitchCityRope.IntegrationTests/PostgreSqlFixture.cs`
- `/tests/WitchCityRope.Infrastructure.Tests/Fixtures/DatabaseFixture.cs`

## Priority 3: Authentication System Changes

### Issue: Login Page Redirects
**Problem:** Login page returns 302 (redirect) instead of 200 (OK)

**Possible Causes:**
- HTTPS redirection in test environment
- Authentication middleware ordering
- Test configuration issues

**Investigation Required:**
- Check `TestWebApplicationFactory` configuration
- Verify authentication setup in test environment

## Priority 4: Blazor Component Initialization

### Issue: Blazor Not Loading in Tests
**Symptoms:**
- `window.Blazor` undefined after 10 seconds
- Validation components not initializing

**Fixes Required:**
1. Ensure Blazor scripts are properly loaded in test environment
2. Add proper wait conditions for Blazor initialization
3. Consider increasing timeouts for CI environments

## Priority 5: Email Service Test Failures

### Issue: SendGrid Mock Verification Failures
**Problem:** Bulk email tests failing due to mock verification

**Fix Required:**
- Update mock setup to match actual SendGrid message structure
- Verify personalization structure in tests

## Quick Fixes Script

```bash
#!/bin/bash
# Fix namespace issues in tests
find tests -name "*.cs" -type f -exec sed -i 's/WitchCityRope.Api.Features.Auth.Services.UserWithAuth/WitchCityRope.Api.Features.Auth.Models.UserWithAuth/g' {} +

# Clean test databases
docker ps -a | grep postgres | awk '{print $1}' | xargs -r docker rm -f

# Clear EF migration cache
find . -name "*.Designer.cs" -path "*/Migrations/*" -delete
```

## Test Execution Order

After fixes, run tests in this order:
1. Core Tests (should pass) ✅
2. API Tests (after namespace fix)
3. Infrastructure Tests (after DB cleanup)
4. Integration Tests (after all fixes)
5. E2E/Playwright Tests (may need additional fixes)

## Validation Checklist

- [ ] All projects compile without errors
- [ ] No "table already exists" errors
- [ ] Login page accessible without redirects
- [ ] Blazor components initialize properly
- [ ] All email service tests pass
- [ ] No namespace resolution errors

## Next Steps

1. Apply namespace fixes immediately
2. Implement database cleanup strategy
3. Fix authentication configuration
4. Update Blazor initialization logic
5. Run full test suite to identify remaining issues