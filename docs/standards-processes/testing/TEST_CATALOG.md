# WitchCityRope Test Catalog
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Testing Team -->
<!-- Status: Active -->

## Overview
This catalog provides a comprehensive inventory of all tests in the WitchCityRope project, organized by type and location. This is the single source of truth for understanding our test coverage.

## Quick Reference
- **Unit Tests**: 203 tests (99.5% passing)
- **API Tests**: 123 tests (95% passing)
- **Integration Tests**: 133 tests (86% passing)
- **E2E Tests**: 180 tests (Migrated to Playwright, 83% passing)

## Test Organization

### 1. Unit Tests

#### Core Domain Tests
**Location**: `/tests/WitchCityRope.Core.Tests/`  
**Count**: 203 tests  
**Status**: 202 passing, 1 skipped

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
**Count**: 123 tests  
**Status**: 117 passing, 6 failing

Key test files:
- `Services/AuthServiceTests.cs` - Authentication logic
- `Services/EventServiceTests.cs` - Event management
- `Services/UserServiceTests.cs` - User management
- `Services/PaymentServiceTests.cs` - Payment processing
- `Services/VettingServiceTests.cs` - Vetting workflow
- `Features/Auth/LoginCommandTests.cs` - Login command handler
- `Features/Events/CreateEventCommandTests.cs` - Event creation

#### Web Component Tests
**Location**: `/tests/WitchCityRope.Web.Tests/`  
**Status**: Multiple test projects need consolidation

Test projects found:
- `WitchCityRope.Web.Tests/` - Main web tests
- `WitchCityRope.Web.Tests.Login/` - Login-specific tests
- `WitchCityRope.Web.Tests.New/` - New test structure

### 2. Integration Tests

#### Database Integration Tests
**Location**: `/tests/WitchCityRope.IntegrationTests/`  
**Count**: 133 tests  
**Status**: 115 passing, 18 failing (navigation issues)

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
**Status**: Compilation issues due to architectural changes

Test categories:
- Database configuration tests
- Migration tests
- Repository pattern tests
- Service integration tests

### 3. E2E Tests (Playwright)

#### Main E2E Test Suite
**Location**: `/tests/playwright/tests/`  
**Count**: 180 tests (migrated from Puppeteer)  
**Status**: 83% passing

Test categories:
```
├── auth/
│   ├── login.spec.ts - Login flows
│   ├── registration.spec.ts - Registration flows
│   ├── profile.spec.ts - Profile management
│   └── authorization.spec.ts - Role-based access
├── events/
│   ├── event-list.spec.ts - Event browsing
│   ├── event-detail.spec.ts - Event details
│   ├── event-rsvp.spec.ts - RSVP functionality
│   └── event-admin.spec.ts - Admin management
├── admin/
│   ├── dashboard.spec.ts - Admin dashboard
│   ├── user-management.spec.ts - User admin
│   └── vetting.spec.ts - Vetting workflow
└── user/
    ├── dashboard.spec.ts - Member dashboard
    └── tickets.spec.ts - Ticket management
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
**Status**: Basic load testing implemented

Key tests:
- `AuthenticationLoadTests.cs` - Login performance
- `EventsLoadTests.cs` - Event listing performance
- `SystemStressTests.cs` - Overall system stress

### 5. Deprecated Test Locations

#### Old Puppeteer Tests (DO NOT USE)
- `/tests/e2e/` - Old Puppeteer tests
- `/ToBeDeleted/` - Deprecated tests
- All Puppeteer tests migrated to Playwright

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
- **Unit Tests**: ~95% of business logic
- **Integration Tests**: ~80% of data access
- **E2E Tests**: ~70% of user workflows

### Target Coverage
- Unit Tests: 95%+ 
- Integration Tests: 85%+
- E2E Tests: 80%+

## Known Issues

### Integration Tests
- 18 tests failing due to missing navigation routes
- Some concurrency issues with parallel execution

### E2E Tests  
- Flaky tests in event RSVP flow
- Visual regression tests need baseline updates

### Performance Tests
- Need more realistic load scenarios
- Missing API performance tests

## Maintenance

### Adding New Tests
1. Follow existing patterns in respective test folders
2. Use descriptive test names: `Given_When_Then` format
3. Keep tests isolated and independent
4. Update this catalog when adding new test categories

### Updating Test Data
- Modify seed data in `DbInitializer.cs`
- Use unique identifiers to avoid conflicts
- Document any new test users here

---

*For test writing best practices, see [lessons-learned/test-writers.md](/docs/lessons-learned/test-writers.md)*