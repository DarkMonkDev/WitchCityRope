# Testing Guide

## Overview

WitchCityRope uses a comprehensive testing strategy to ensure code quality and reliability:

## 🚨 MANDATORY: Pre-Flight Health Checks

**Before running ANY tests, you MUST run health checks:**

```bash
dotnet test tests/WitchCityRope.Core.Tests --filter "Category=HealthCheck"
```

These health checks verify:
- ✅ React dev server (port 5173)
- ✅ API service (port 5655)
- ✅ PostgreSQL database (port 5433)
- ✅ Docker containers healthy

**Why**: Port misconfigurations are the #1 cause of false test failures. Health checks prevent hours of debugging by validating infrastructure first.

See `/docs/standards-processes/testing-prerequisites.md` for full details.

## Testing Strategy

- **Unit Tests**: Test business logic in isolation
- **Integration Tests**: Test database interactions and API endpoints with real PostgreSQL
- **E2E Tests**: Test user workflows through the browser

## Test Projects

### 1. WitchCityRope.Core.Tests
- **Purpose**: Unit tests for domain entities, value objects, and business logic
- **Framework**: xUnit + FluentAssertions
- **Database**: None (pure unit tests)
- **Status**: ✅ 202 passing, 1 skipped

### 2. WitchCityRope.IntegrationTests  
- **Purpose**: Integration tests for database operations, API endpoints, and authentication
- **Framework**: xUnit + WebApplicationFactory + Testcontainers
- **Database**: PostgreSQL 16-alpine (via Testcontainers)
- **Status**: 🔧 Being migrated to PostgreSQL containers

### 3. E2E Tests (/tests/playwright) 🆕
- **Purpose**: End-to-end browser automation tests  
- **Framework**: Playwright + TypeScript
- **Status**: ✅ 180 tests migrated from Puppeteer (January 2025)
- **Requirements**: Application must be running
- **Features**: Cross-browser, Page Object Models, 40% faster than Puppeteer

> 🚨 **CRITICAL**: Old Puppeteer tests in `/tests/e2e/` are DEPRECATED. Use only Playwright tests.

## Integration Test Infrastructure

### PostgreSQL Testcontainers Setup

Integration tests use real PostgreSQL databases via Testcontainers, providing:
- Exact same database engine as production
- Real migration testing
- Accurate constraint and transaction behavior
- No false positives from in-memory database differences

### Key Components

1. **TestWebApplicationFactory.cs**
   ```csharp
   public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
   {
       private readonly PostgreSqlContainer _postgresContainer;
       
       // Creates and manages PostgreSQL container lifecycle
   }
   ```

2. **PostgreSqlTestCollection.cs**
   ```csharp
   [CollectionDefinition("PostgreSQL Integration Tests")]
   public class PostgreSqlTestCollection : ICollectionFixture<TestWebApplicationFactory>
   {
       // Ensures tests share the same database container
   }
   ```

3. **Test Classes**
   ```csharp
   [Collection("PostgreSQL Integration Tests")]
   public class AuthenticationTests
   {
       // Tests run against real PostgreSQL
   }
   ```

## Running Tests

### Prerequisites
- Docker must be running (for integration tests)
- .NET 9.0 SDK installed
- Node.js 16+ (for E2E tests)

### Commands

```bash
# Run all tests
dotnet test

# Run only unit tests (fast)
dotnet test tests/WitchCityRope.Core.Tests

# Run integration tests (slower, requires Docker)
dotnet test tests/WitchCityRope.IntegrationTests

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"

# Run with detailed logging
dotnet test -v normal

# Watch mode for TDD
dotnet watch test --project tests/WitchCityRope.Core.Tests

# E2E tests (requires app running)
cd tests/e2e
npm test
```

### Cleanup

```bash
# Remove test containers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f

# Remove test volumes
docker volume prune -f
```

## Writing Tests

### Unit Test Example
```csharp
[Fact]
public void Event_Should_Not_Allow_Registration_After_Capacity_Reached()
{
    // Arrange
    var @event = new Event(...);
    
    // Act & Assert
    @event.Invoking(e => e.RegisterAttendee(user))
        .Should().Throw<DomainException>()
        .WithMessage("Event is at capacity");
}
```

### Integration Test Example
```csharp
[Collection("PostgreSQL Integration Tests")]
public class EventRepositoryTests
{
    [Fact]
    public async Task Should_Save_And_Retrieve_Event()
    {
        // Uses real PostgreSQL database
        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        
        // Test with real database constraints and behaviors
    }
}
```

## Test Data

### Seeded Test Users
The DbInitializer creates these test accounts:
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **Member**: member@witchcityrope.com / Test123!
- **Attendee**: guest@witchcityrope.com / Test123!

### Test Helpers
- `AuthenticationTestHelper`: Creates test users and handles authentication
- `TestEmailService`: Mock email service for testing
- `TestPaymentService`: Mock payment service for testing

## Troubleshooting

### Common Issues

1. **"Services for database providers have been registered" error**
   - Solution: Ensure Program.cs skips database registration for Testing environment
   - Fixed by conditional registration based on environment

2. **Container startup failures**
   - Check Docker is running: `docker info`
   - Check disk space: `df -h`
   - Check container logs: `docker logs <container-id>`

3. **Test database not cleaned up**
   - Each test run creates a new database
   - Container is disposed after test run
   - Manual cleanup: `docker ps -a | grep testcontainers`

4. **Slow test startup**
   - First run downloads PostgreSQL image
   - Subsequent runs use cached image
   - Consider using `[Collection]` to share container

## Best Practices

1. **Use appropriate test level**
   - Unit tests for business logic
   - Integration tests for database/API
   - E2E tests for critical user paths

2. **Keep tests isolated**
   - Each test should be independent
   - Use transactions for database tests
   - Clean up test data after each test

3. **Use realistic test data**
   - Test edge cases and boundaries
   - Use the same validation as production
   - Test error scenarios

4. **Optimize test performance**
   - Share expensive resources (database containers)
   - Run unit tests first (fail fast)
   - Parallelize where possible

## CI/CD Integration

For CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Tests
  run: |
    dotnet test tests/WitchCityRope.Core.Tests
    dotnet test tests/WitchCityRope.IntegrationTests
  env:
    TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE: /var/run/docker.sock
    DOCKER_HOST: unix:///var/run/docker.sock
```

## Future Improvements

1. **Add mutation testing** to ensure test quality
2. **Implement contract testing** for API compatibility
3. **Add performance benchmarks** for critical paths
4. **Create snapshot tests** for UI components
5. **Add chaos engineering** tests for resilience