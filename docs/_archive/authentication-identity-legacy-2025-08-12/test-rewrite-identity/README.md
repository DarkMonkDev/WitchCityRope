# Web Tests Rewrite for ASP.NET Core Identity

## Overview
This enhancement involves a complete rewrite of the Web test project to align with the ASP.NET Core Identity migration and current architecture.

## Status
**Started**: January 11, 2025  
**Current Phase**: PAUSED - Waiting for Form Standardization  
**Blocker**: Web project validation system migration to Blazor/.NET 9

## Background
The existing Web test project has 318+ compilation errors due to:
- Interface method mismatches after Identity migration
- Obsolete test patterns from the previous authentication system
- Changed DTOs and service contracts
- Missing or changed extension methods

## Goals
1. Achieve 80%+ code coverage for Web project
2. Test ASP.NET Core Identity integration thoroughly
3. Follow .NET 9 and Blazor testing best practices
4. Create maintainable, well-documented tests
5. Ensure all tests pass in CI/CD pipeline

## Architecture Decisions
- Use bUnit for Blazor component testing
- Mock services at interface level
- Test components in isolation
- Use AAA (Arrange-Act-Assert) pattern
- Group tests by feature/module

## Directory Structure
```
tests/WitchCityRope.Web.Tests/
├── Auth/                      # Authentication & authorization tests
├── Components/               # Reusable component tests
├── Features/                # Feature-specific tests
│   ├── Admin/              # Admin portal tests
│   ├── Events/             # Event management tests
│   ├── Members/            # Member area tests
│   └── Public/             # Public pages tests
├── Helpers/                 # Test utilities and base classes
├── Services/               # Service integration tests
└── TestData/               # Test data builders

docs/enhancements/test-rewrite-identity/
├── README.md               # This file
├── test-plan.md           # Detailed test plan
├── coverage-report.md     # Coverage tracking
├── best-practices.md      # Testing guidelines
└── progress/              # Daily progress logs
```

## Progress Tracking
See [progress/](./progress/) directory for daily updates.
- **Latest**: [2025-01-11-paused.md](progress/2025-01-11-paused.md) - Paused for form standardization

## Work Completed Before Pause
✅ Test infrastructure created (helpers, base classes, data builders)  
✅ Example component tests (LoginTests, MainLayoutTests)  
✅ Service tests implemented (AuthService, ToastService, ApiClient)  
✅ All MudBlazor references removed (project uses Syncfusion only)  
✅ Documentation and best practices guide created

## Next Steps (When Unblocked)
1. ✅ ~~Create test infrastructure and base classes~~ (DONE)
2. ✅ ~~Implement authentication test helpers~~ (DONE)
3. Update tests for new Blazor validation methodology
4. Write remaining component tests by priority
5. Add integration tests
6. Achieve 80% coverage target