# Test Migration Analysis - System Level
<!-- Created: 2025-09-18 -->
<!-- Agent: test-developer -->
<!-- Task: Analyze test migration requirements from DDD to Vertical Slice -->

## Executive Summary

**CRITICAL STATUS**: All .NET test suites are currently non-functional due to architectural migration from Domain-Driven Design to Vertical Slice Architecture. This represents a complete loss of backend test coverage until migration is completed.

### Migration Scale
- **Broken Tests**: ~300+ test files referencing archived DDD architecture
- **Working Tests**: Playwright E2E tests only (UI layer tests)
- **Test Coverage Loss**: 100% of unit, integration, and API tests
- **Business Logic Risk**: Critical domain logic tests completely unavailable

### Key Finding
The old DDD architecture in `/src/WitchCityRope.Core`, `/src/WitchCityRope.Infrastructure` has been moved to `/src/_archive/` but ALL test projects still reference the old namespace structure, causing complete compilation failure.

## Current Test Inventory

### 1. BROKEN - Core Domain Tests
**Location**: `/tests/WitchCityRope.Core.Tests/`
**Status**: ❌ 100% Compilation Failure

#### Test Files (All Broken)
- `Entities/EventTests.cs` - 44 tests for Event domain entity
- `Entities/UserTests.cs` - User domain entity tests
- `Entities/RegistrationTests.cs` - Registration domain entity tests
- `ValueObjects/MoneyTests.cs` - Money value object tests
- `ValueObjects/SceneNameTests.cs` - SceneName value object tests
- `ValueObjects/EmailAddressTests.cs` - EmailAddress value object tests

#### Business Logic Coverage (Currently Lost)
- **Event Management**: Creation, validation, capacity management, pricing tiers
- **User Management**: Authentication, roles, vetting status
- **Registration Logic**: RSVP workflows, capacity constraints
- **Value Object Validation**: Money, email, scene name validation
- **Domain Rules**: Complex business rule enforcement

#### Sample Test Pattern (Currently Broken)
```csharp
// This fails because WitchCityRope.Core.Entities.Event no longer exists
using WitchCityRope.Core.Entities; // ❌ BROKEN REFERENCE
using WitchCityRope.Core.ValueObjects; // ❌ BROKEN REFERENCE

public class EventTests
{
    [Fact]
    public void Constructor_ValidData_CreatesEvent()
    {
        var @event = new Event(title, description, startDate, endDate, capacity, eventType, location, organizer, pricingTiers);
        // This test validates critical event creation business logic
    }
}
```

### 2. BROKEN - Infrastructure Tests
**Location**: `/tests/WitchCityRope.Infrastructure.Tests/`
**Status**: ❌ 100% Compilation Failure

#### Test Categories (All Broken)
- **Database Tests**: Entity configurations, migrations, complex queries
- **Integration Tests**: TestContainers with PostgreSQL
- **Email Service Tests**: SendGrid integration
- **PayPal Integration Tests**: Payment webhook handling
- **Security Tests**: Authentication and authorization

#### Key Infrastructure Lost
- Real PostgreSQL testing with TestContainers
- Migration validation and rollback testing
- Payment processing validation
- Email delivery verification

### 3. BROKEN - Common Test Infrastructure
**Location**: `/tests/WitchCityRope.Tests.Common/`
**Status**: ❌ 100% Compilation Failure

#### Critical Test Utilities (All Broken)
- **Test Builders**: EventBuilder, UserBuilder, RegistrationBuilder
- **Test Doubles**: In-memory repositories, mock services
- **Database Fixtures**: TestContainers setup, database seeding
- **Performance Testing**: Load testing infrastructure

#### Impact
- Cannot create test data for any .NET tests
- Lost all shared test infrastructure
- No consistent test patterns across projects

### 4. BROKEN - API Integration Tests
**Location**: `/tests/integration/`, `/tests/WitchCityRope.Api.Tests/`
**Status**: ❌ Partial - Depends on broken common infrastructure

#### Missing API Test Coverage
- Feature endpoint testing
- DTO validation
- Service layer integration
- Authentication flows
- Error handling

### 5. WORKING - E2E Tests (Playwright)
**Location**: `/tests/playwright/`
**Status**: ✅ Functional - Tests UI layer

#### Working Test Categories
- **Authentication Flows**: Login, logout, user management
- **Event Management UI**: Admin event creation, editing
- **Dashboard Tests**: User dashboard functionality
- **Navigation Tests**: Page routing and user journeys
- **Visual Regression**: Screenshot comparison tests

#### E2E Test Coverage
- ~25 Playwright test files
- Cross-browser testing (Chrome, Firefox, Safari)
- Mobile viewport testing
- API integration through UI workflows

## Architecture Mapping - Old to New

### Domain-Driven Design → Vertical Slice Architecture

#### 1. Domain Entities → Feature DTOs/Models
```csharp
// OLD (Archived)
namespace WitchCityRope.Core.Entities
{
    public class Event // Rich domain entity with business logic
    public class User  // Domain entity with value objects
    public class Registration // Domain entity with complex rules
}

// NEW (Vertical Slice)
namespace WitchCityRope.Api.Features.Events.Models
{
    public class EventDto // Data transfer object
    public class CreateEventRequest // Feature-specific request
    public class EventResponse // Feature-specific response
}
```

#### 2. Domain Services → Feature Services
```csharp
// OLD (Archived)
namespace WitchCityRope.Core.Services
{
    public interface IEventService // Domain service interface
    public class EventDomainService // Rich domain logic
}

// NEW (Vertical Slice)
namespace WitchCityRope.Api.Features.Events.Services
{
    public class EventService // Feature-specific service
    // Logic moved to endpoints and services per feature
}
```

#### 3. Value Objects → Shared Models or Feature-Specific
```csharp
// OLD (Archived)
namespace WitchCityRope.Core.ValueObjects
{
    public class Money // Rich value object with validation
    public class SceneName // Domain-specific value object
    public class EmailAddress // Email validation logic
}

// NEW (Mixed Approach)
namespace WitchCityRope.Api.Models // Some moved to shared
namespace WitchCityRope.Api.Features.Shared // Some shared across features
// Or embedded directly in feature DTOs
```

#### 4. Repository Pattern → Direct Data Access
```csharp
// OLD (Archived)
namespace WitchCityRope.Infrastructure.Repositories
{
    public interface IEventRepository
    public class EventRepository // Repository pattern
}

// NEW (Vertical Slice)
namespace WitchCityRope.Api.Features.Events.Services
{
    public class EventService // Direct EF Core usage
    // No repository abstraction layer
}
```

### Test Pattern Migrations Required

#### Unit Tests: Entity → DTO/Service Testing
```csharp
// OLD Pattern (Broken)
[Fact]
public void Event_Create_ValidatesBusinessRules()
{
    var @event = new Event(...); // Test rich domain entity
    @event.Publish(); // Test domain behavior
}

// NEW Pattern (Needed)
[Fact]
public void EventService_CreateEvent_ValidatesRequest()
{
    var service = new EventService(context); // Test service layer
    var result = await service.CreateEventAsync(request); // Test service method
}
```

#### Integration Tests: Repository → Service Integration
```csharp
// OLD Pattern (Broken)
[Fact]
public async Task EventRepository_SaveEvent_PersistsToDatabase()
{
    var repository = new EventRepository(context);
    await repository.AddAsync(@event); // Test repository
}

// NEW Pattern (Needed)
[Fact]
public async Task EventEndpoint_CreateEvent_ReturnsCreated()
{
    var response = await client.PostAsync("/api/events", content); // Test endpoint
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

## Migration Categories

### MIGRATE NOW - Implemented Features

#### 1. Health Feature
**Status**: ✅ Implemented
**Location**: `/apps/api/Features/Health/`
**Tests Needed**:
- HealthService unit tests
- Health endpoint integration tests
- Health check monitoring tests

#### 2. Authentication Feature
**Status**: ✅ Partially Implemented
**Location**: `/apps/api/Features/Authentication/`
**Tests Needed**:
- Login/logout endpoint tests
- JWT token validation tests
- User session management tests

#### 3. Basic Events Functionality
**Status**: ⚠️ Partially Implemented
**Location**: `/apps/api/Features/Events/`
**Tests Needed**:
- Event CRUD operation tests
- Event endpoint integration tests
- Event DTO validation tests

### MIGRATE AS PENDING - Unimplemented Features

#### 1. Complex Event Business Logic
**Status**: ❌ Not Implemented
**Original Tests**: EventTests.cs domain logic
**Strategy**: Mark tests with `[Skip]` attribute with implementation plan

```csharp
[Fact(Skip = "Event publishing business logic not yet implemented in vertical slice architecture")]
public void Event_Publish_ValidatesPublishingRules() { }
```

#### 2. User Management Features
**Status**: ❌ Not Implemented
**Original Tests**: UserTests.cs, user role management
**Strategy**: Convert to pending integration tests

#### 3. Registration/RSVP Workflows
**Status**: ❌ Not Implemented
**Original Tests**: RegistrationTests.cs complex business rules
**Strategy**: Create pending E2E tests for complete workflows

### ARCHIVE - Obsolete Functionality

#### 1. Blazor-Specific Tests
**Location**: `/tests/WitchCityRope.Web.Tests/` (archived)
**Status**: ✅ Already Archived
**Reason**: Migrated to React frontend

#### 2. Domain Entity Constructor Tests
**Reason**: Rich domain entities replaced with simple DTOs
**Strategy**: Replace with DTO validation tests

#### 3. Repository Pattern Tests
**Reason**: Direct service access replaces repository pattern
**Strategy**: Replace with service integration tests

### CREATE NEW - Missing Test Patterns

#### 1. Vertical Slice Integration Tests
**Needed**: Feature-to-feature integration testing
**Pattern**: Test complete workflows across multiple features

#### 2. DTO Validation Tests
**Needed**: Input validation for all feature requests/responses
**Pattern**: Validate all DTO properties and business rules

#### 3. API Contract Tests
**Needed**: Ensure API contracts match frontend expectations
**Pattern**: Test NSwag-generated interfaces against actual API responses

#### 4. Feature Endpoint Tests
**Needed**: Complete HTTP endpoint testing per feature
**Pattern**: Test all CRUD operations, error handling, status codes

## Risk Assessment

### Critical Business Logic at Risk

#### High Risk - Complete Loss of Coverage
1. **Event Capacity Management**: No tests for overbooking prevention
2. **Payment Processing**: PayPal webhook validation completely untested
3. **User Authentication**: Security-critical login flows untested
4. **Data Validation**: Input validation completely uncovered

#### Medium Risk - Partial Coverage via E2E
1. **UI Workflows**: Covered by Playwright but not business logic
2. **Navigation**: Frontend routing tested but not API routing
3. **Form Validation**: UI validation tested but not server validation

#### Low Risk - Maintained Coverage
1. **Visual Consistency**: Playwright screenshot testing working
2. **User Journeys**: Complete workflows tested end-to-end
3. **Cross-Browser Compatibility**: Maintained via E2E tests

### Test Debt Accumulation

#### Technical Debt
- **No Unit Test Coverage**: Business logic changes untested
- **No Integration Testing**: Database operations unverified
- **No API Contract Testing**: Frontend-backend contract failures possible
- **No Performance Testing**: Response time regressions undetected

#### Business Impact
- **Release Confidence**: Cannot verify functionality before deployment
- **Regression Detection**: Breaking changes may go unnoticed
- **Code Quality**: No test-driven development possible for backend
- **Development Speed**: Manual testing slows development cycles

## Success Criteria for Migration

### Phase 1: Critical Infrastructure Restoration
- [ ] Health service tests functional
- [ ] Authentication endpoint tests functional
- [ ] Basic test infrastructure rebuilt (builders, fixtures)
- [ ] TestContainers integration working

### Phase 2: Core Feature Coverage
- [ ] Event CRUD operations fully tested
- [ ] User management endpoints tested
- [ ] Payment processing tests restored
- [ ] Database integration tests functional

### Phase 3: Business Logic Preservation
- [ ] All critical business rules have tests
- [ ] Domain validation logic verified
- [ ] Error handling comprehensively tested
- [ ] Performance baseline tests established

### Phase 4: Complete Coverage Restoration
- [ ] Test coverage >= 80% for all features
- [ ] Integration tests for all feature interactions
- [ ] E2E tests for all user workflows
- [ ] CI/CD pipeline fully functional

## Immediate Next Steps

1. **Create Migration Strategy Document** - Detailed step-by-step migration plan
2. **Restore Test Infrastructure** - Fix common test utilities first
3. **Implement Feature Test Templates** - Standard patterns for vertical slice testing
4. **Begin Health Feature Migration** - Prove pattern with simplest feature
5. **Document Business Logic Preservation** - Ensure no critical rules are lost

---

**CRITICAL**: This analysis reveals complete backend test infrastructure failure. Immediate action required to restore test coverage and prevent further accumulation of technical debt.