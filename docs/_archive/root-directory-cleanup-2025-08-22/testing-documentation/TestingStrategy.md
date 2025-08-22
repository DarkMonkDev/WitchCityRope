# WitchCityRope Testing Strategy & Implementation Plan

## Overview
This document outlines a comprehensive testing strategy for the WitchCityRope application, covering unit tests, integration tests, and end-to-end tests.

## Testing Philosophy
- **Test Pyramid Approach**: Many unit tests, fewer integration tests, minimal E2E tests
- **Behavior-Driven**: Test behavior, not implementation details
- **Isolated**: Each test should be independent
- **Fast**: Unit tests should run in milliseconds
- **Reliable**: No flaky tests

## Technology Stack
- **Unit Testing**: xUnit, FluentAssertions, Moq
- **Integration Testing**: xUnit, TestContainers (for database), WebApplicationFactory
- **Blazor Component Testing**: bUnit
- **Code Coverage**: Coverlet
- **Test Data**: Bogus (fake data generator)

## Test Structure

### 1. Unit Tests

#### Core Project Tests
- **Domain Entities**
  - User entity validation and business rules
  - Event entity capacity management
  - Registration status transitions
  - Payment processing rules
  - VettingApplication workflow

- **Value Objects**
  - EmailAddress validation
  - Money calculations
  - SceneName validation

#### Infrastructure Project Tests
- **Services**
  - EncryptionService
  - JwtTokenService
  - SlugGenerator
  - EmailService (mock SendGrid)
  - PayPalService (mock PayPal API)

#### API Project Tests
- **Service Layer**
  - AuthService authentication logic
  - EventService business rules
  - UserService profile management
  - VettingService application workflow
  - PaymentService processing logic

- **Validators**
  - Request model validation
  - Business rule validation

### 2. Integration Tests

#### Database Integration Tests
- **Repository Tests**
  - CRUD operations
  - Complex queries
  - Transactions
  - Concurrency handling

- **DbContext Tests**
  - Entity configurations
  - Migrations
  - Seed data

#### API Integration Tests
- **Authentication Endpoints**
  - Login flow
  - Registration with validation
  - Token refresh
  - Two-factor authentication

- **Event Management**
  - Create/Update/Delete events
  - Event registration flow
  - Capacity management
  - Waitlist functionality

- **Payment Processing**
  - Payment intent creation
  - Webhook handling
  - Refund processing

#### External Service Integration Tests
- **Email Service**
  - SendGrid integration
  - Email template rendering

- **Payment Services**
  - PayPal integration
  - Stripe integration

### 3. Blazor Component Tests

#### Component Unit Tests
- **Authentication Components**
  - Login form validation
  - Registration form behavior
  - Two-factor input

- **Event Components**
  - Event card rendering
  - Event list filtering
  - Registration button states

- **Layout Components**
  - Navigation menu behavior
  - User menu dropdown
  - Mobile menu toggle

#### Component Integration Tests
- **Page Tests**
  - Dashboard data loading
  - Profile edit flow
  - Event registration flow

### 4. End-to-End Tests (Minimal)
- **Critical User Journeys**
  - New user registration → email verification → login
  - Browse events → register → payment → confirmation
  - Submit vetting application → review → approval

## Implementation Plan

### Phase 1: Test Infrastructure Setup
1. Create test projects structure
2. Add testing NuGet packages
3. Set up test utilities and helpers
4. Configure test databases
5. Create test data builders

### Phase 2: Core Unit Tests
1. Domain entity tests
2. Value object tests
3. Core service tests
4. Validation tests

### Phase 3: Infrastructure Tests
1. Database integration tests
2. External service mock tests
3. Repository pattern tests

### Phase 4: API Tests
1. Controller endpoint tests
2. Authentication flow tests
3. Authorization tests
4. API integration tests

### Phase 5: Blazor Component Tests
1. Component rendering tests
2. User interaction tests
3. Form validation tests
4. Navigation tests

### Phase 6: E2E Tests
1. Critical path tests
2. Cross-browser tests
3. Performance tests

## Test Project Structure

```
tests/
├── WitchCityRope.Core.Tests/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Services/
├── WitchCityRope.Infrastructure.Tests/
│   ├── Data/
│   ├── Services/
│   └── Integration/
├── WitchCityRope.Api.Tests/
│   ├── Unit/
│   │   ├── Services/
│   │   └── Validators/
│   └── Integration/
│       ├── Controllers/
│       └── Endpoints/
├── WitchCityRope.Web.Tests/
│   ├── Components/
│   ├── Pages/
│   └── Services/
├── WitchCityRope.IntegrationTests/
│   ├── Database/
│   ├── Api/
│   └── ExternalServices/
└── WitchCityRope.E2E.Tests/
    └── Scenarios/
```

## Testing Best Practices

### Naming Conventions
- Test classes: `[ClassUnderTest]Tests`
- Test methods: `[MethodName]_[Scenario]_[ExpectedBehavior]`
- Example: `LoginAsync_WithValidCredentials_ReturnsSuccessResult`

### Arrange-Act-Assert Pattern
```csharp
// Arrange
var service = new AuthService(...);
var request = new LoginRequest { ... };

// Act
var result = await service.LoginAsync(request);

// Assert
result.Should().BeSuccessful();
```

### Test Data Builders
```csharp
var user = new UserBuilder()
    .WithEmail("test@example.com")
    .WithSceneName("TestUser")
    .Build();
```

### Integration Test Base Classes
```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    protected TestDatabase Database { get; }
    protected HttpClient Client { get; }
    
    public async Task InitializeAsync() { ... }
    public async Task DisposeAsync() { ... }
}
```

## Coverage Goals
- **Unit Tests**: 80% code coverage
- **Integration Tests**: All critical paths covered
- **E2E Tests**: Happy path for main user journeys

## CI/CD Integration
- Run unit tests on every commit
- Run integration tests on PR
- Run E2E tests before deployment
- Generate coverage reports
- Fail build if coverage drops below threshold

## Parallel Execution Strategy
Tests will be implemented in parallel across these areas:
1. **Core & Domain Tests** (Agent 1)
2. **API Service Tests** (Agent 2)
3. **Infrastructure & Database Tests** (Agent 3)
4. **Blazor Component Tests** (Agent 4)

This allows for efficient test development while maintaining consistency.