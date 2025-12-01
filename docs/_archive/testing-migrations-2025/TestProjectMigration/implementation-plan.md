# Test Project Migration to ASP.NET Core Identity - Implementation Plan

## Executive Summary
This document outlines the plan to migrate the API and Web test projects from the custom authentication system to ASP.NET Core Identity. The migration affects 115 API tests and multiple Web tests, with a goal of achieving 80%+ test coverage.

## Current State Analysis

### API Tests Status
- **Total Tests**: 115 tests across 10 test files
- **Compilation Errors**: 22 errors
- **Main Issues**:
  - References to `WitchCityRopeDbContext` (replaced with `WitchCityRopeIdentityDbContext`)
  - Namespace changes for auth-related types
  - Some interfaces have moved or changed signatures

### Web Tests Status
- **Main Issues**:
  - Tests reference removed Blazor auth components (Login.razor, MainNav.razor)
  - Auth is now handled by Identity Razor Pages instead of Blazor components
  - Service references need updating (`AuthenticationService` → `SimplifiedIdentityAuthService`)

### New Identity Implementation
- **User**: `WitchCityRopeUser` extends `IdentityUser<Guid>`
- **SignIn**: Custom `WitchCityRopeSignInManager` supports email/scene name login
- **Auth**: Dual authentication (Cookie for Web, JWT for API)
- **Roles**: 5 role levels with custom authorization policies

## Implementation Phases

### Phase 1: Foundation Setup (Day 1-2)
1. **Create test infrastructure**
   - Base test classes for Identity testing
   - Mock factories for UserManager and SignInManager
   - Test data builders for Identity entities
   - Shared test context for database setup

2. **Fix compilation errors**
   - Update all DbContext references
   - Fix namespace imports
   - Update type references

### Phase 2: API Test Migration (Day 3-5)
1. **AuthService Tests**
   - Rewrite 17 authentication tests
   - Test JWT token generation
   - Test refresh token flow
   - Test lockout behavior

2. **User Management Tests**
   - Update UserService tests
   - Test role assignment
   - Test claims management
   - Test vetting status updates

3. **Integration Tests**
   - API endpoint authentication tests
   - Authorization policy tests
   - Cookie/JWT dual auth tests

### Phase 3: Web Test Migration (Day 6-7)
1. **Remove Obsolete Tests**
   - Delete tests for removed Blazor auth components
   - Archive old test code for reference

2. **Create New Identity Tests**
   - Integration tests for Identity pages
   - Test login/logout flows
   - Test registration process
   - Test password reset

3. **Service Tests**
   - Update SimplifiedIdentityAuthService tests
   - Test authentication state provider
   - Test authorization in Blazor components

### Phase 4: Coverage & Quality (Day 8-9)
1. **Coverage Analysis**
   - Run coverage tools
   - Identify gaps
   - Write additional tests

2. **Quality Assurance**
   - Performance tests for auth operations
   - Security tests (lockout, password policies)
   - Edge case testing

## Success Criteria
- ✅ All test projects compile without errors
- ✅ All existing tests pass or are properly migrated
- ✅ 80%+ code coverage achieved
- ✅ New tests cover Identity-specific features
- ✅ Integration tests verify end-to-end auth flows
- ✅ Performance benchmarks meet requirements

## Risk Mitigation
1. **Technical Risks**
   - Risk: Complex mocking of Identity services
   - Mitigation: Create comprehensive mock helpers and use in-memory database

2. **Timeline Risks**
   - Risk: Unexpected complexity in migration
   - Mitigation: Prioritize critical auth tests first

3. **Quality Risks**
   - Risk: Missing edge cases
   - Mitigation: Review Identity documentation for all features

## Deliverables
1. Migrated API test project with all tests passing
2. Migrated Web test project with appropriate Identity tests
3. Test coverage report showing 80%+ coverage
4. Documentation of test patterns for Identity
5. Performance test results

## Timeline
- **Day 1-2**: Foundation and compilation fixes
- **Day 3-5**: API test migration
- **Day 6-7**: Web test migration
- **Day 8-9**: Coverage analysis and additional tests
- **Total**: 9 days

## Dependencies
- ASP.NET Core Identity documentation
- Current production code stability
- Test framework compatibility (xUnit, bunit, Moq)

## Next Steps
1. Create technical design document
2. Set up test infrastructure
3. Begin Phase 1 implementation