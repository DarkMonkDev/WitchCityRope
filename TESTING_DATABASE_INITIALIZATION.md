# Database Auto-Initialization Test Suite

## Overview

This document provides a comprehensive guide to running and understanding the test suite created for the database auto-initialization feature. The tests cover `DatabaseInitializationService`, `SeedDataService`, and `DatabaseInitializationHealthCheck` with both unit and integration testing approaches.

## Test Files Created

### Unit Tests - `/tests/unit/api/Services/`

#### 1. DatabaseInitializationServiceTests.cs
**Tests**: Background service behavior, error handling, timeout management, and environment-specific initialization logic.

**Key Test Coverage**:
- ✅ BackgroundService lifecycle (startup, execution, shutdown)
- ✅ Environment detection (Production vs Development seed data behavior) 
- ✅ Idempotent operations (safe to run multiple times with static state management)
- ✅ Timeout handling (30-second timeout with cancellation token support)
- ✅ Retry policies (exponential backoff for migration failures: 2s, 4s, 8s)
- ✅ Error classification (Connection, migration, seed data, timeout, configuration errors)
- ✅ Fail-fast pattern (Milan Jovanovic's error handling with structured logging)
- ✅ Configuration binding (DbInitializationOptions with defaults)
- ✅ Cancellation support (graceful shutdown during long operations)

#### 2. SeedDataServiceTests.cs
**Tests**: Seed data population, transaction management, idempotent operations, and error handling scenarios.

**Key Test Coverage**:
- ✅ Idempotent seed operations (skip if data already exists)
- ✅ Transaction management (rollback on errors, commit on success)
- ✅ User creation (ASP.NET Core Identity integration with 5 test accounts)
- ✅ Event creation (12 test events: 10 upcoming, 2 past with proper UTC dates)
- ✅ UTC DateTime handling (all dates created with DateTimeKind.Utc)
- ✅ Unique test data (GUIDs for scenario names to prevent conflicts)
- ✅ Result pattern (InitializationResult with success/failure details)
- ✅ Error handling (Identity errors, constraint violations, transaction failures)

#### 3. DatabaseInitializationHealthCheckTests.cs
**Tests**: Health check behavior, database connectivity verification, initialization status monitoring, and error handling.

**Key Test Coverage**:
- ✅ Initialization status (integration with static DatabaseInitializationService state)
- ✅ Database connectivity (CanConnectAsync verification)
- ✅ Structured data (timestamp, user/event counts, error details for monitoring)
- ✅ Health status logic (Healthy/Unhealthy based on initialization and connectivity)
- ✅ Error handling (connection failures, service provider errors, cancellation)
- ✅ Service scope management (proper disposal and resource cleanup)
- ✅ Concurrent access (multiple health checks executing simultaneously)

### Integration Tests - `/tests/integration/`

#### DatabaseInitializationIntegrationTests.cs
**Tests**: Complete initialization with real PostgreSQL via TestContainers.

**Key Test Coverage**:
- ✅ End-to-End flow (complete initialization with real PostgreSQL via TestContainers)
- ✅ Environment behavior (Development vs Production seed data differences)
- ✅ Idempotent integration (multiple runs don't create duplicate data)
- ✅ Real migrations (actual EF Core migration application and verification)
- ✅ Seed data integrity (verify test users and events created with correct properties)
- ✅ Health check integration (real database connectivity and status reporting)
- ✅ Timeout scenarios (short timeout configuration handling)
- ✅ Performance metrics (timing and record count validation)

## Prerequisites

### Required Software
- Docker (for PostgreSQL TestContainers in integration tests)
- .NET 9.0 SDK
- PostgreSQL (for integration tests)

### Docker Setup
```bash
# Ensure Docker is running
sudo systemctl start docker

# Verify Docker is accessible
docker ps
```

## Running the Tests

### 1. Unit Tests Only (Fast, No Dependencies)
```bash
# Run all unit tests for database initialization
cd /home/chad/repos/witchcityrope-react/tests/unit/api
dotnet test

# Run specific service tests
dotnet test --filter "DatabaseInitializationServiceTests"
dotnet test --filter "SeedDataServiceTests" 
dotnet test --filter "DatabaseInitializationHealthCheckTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### 2. Integration Tests (Requires Docker)
```bash
# Run integration tests (will start PostgreSQL container automatically)
cd /home/chad/repos/witchcityrope-react/tests/integration
dotnet test

# Run with detailed output and container logs
dotnet test --logger "console;verbosity=detailed"

# Run specific integration test
dotnet test --filter "DatabaseInitialization_WithDevelopmentEnvironment_CompletesSuccessfully"
```

### 3. All Database Initialization Tests
```bash
# From project root - run both unit and integration tests
dotnet test tests/unit/api/Services/DatabaseInitializationServiceTests.cs
dotnet test tests/unit/api/Services/SeedDataServiceTests.cs
dotnet test tests/unit/api/Services/DatabaseInitializationHealthCheckTests.cs
dotnet test tests/integration/DatabaseInitializationIntegrationTests.cs
```

## Test Patterns and Examples

### Background Service Testing
```csharp
// Test background service execution with proper timing
await service.StartAsync(cancellationToken);
await Task.Delay(100); // Allow background execution
await service.StopAsync(cancellationToken);

// Verify static state
DatabaseInitializationService.IsInitializationCompleted.Should().BeTrue();
```

### PostgreSQL Integration with TestContainers
```csharp
// Shared fixture pattern for efficient container reuse
[Collection("PostgreSQL Integration Tests")]
public class Tests : IClassFixture<PostgreSqlIntegrationFixture>

// UTC DateTime verification for PostgreSQL compatibility
createdEvents.Should().OnlyContain(e => e.StartDate.Kind == DateTimeKind.Utc);
```

### Error Classification Testing
```csharp
// Test private methods with reflection when needed
var method = typeof(DatabaseInitializationService)
    .GetMethod("ClassifyError", BindingFlags.NonPublic | BindingFlags.Static);
var result = method!.Invoke(null, new object[] { exception });
result.Should().Be(InitializationErrorType.TimeoutExceeded);
```

### Health Check Testing
```csharp
// Verify health check data structure
healthResult.Status.Should().Be(HealthStatus.Healthy);
healthResult.Data.Should().ContainKey("timestamp");
healthResult.Data.Should().ContainKey("userCount");
healthResult.Data.Should().ContainKey("eventCount");
```

## Troubleshooting

### Common Issues

#### 1. "Cannot connect to Docker daemon"
```bash
# Start Docker service
sudo systemctl start docker

# Add user to docker group (logout/login required)
sudo usermod -aG docker $USER
```

#### 2. "TestContainers timeout"
```bash
# Clean up stale containers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f

# Free up disk space
docker system prune -f
```

#### 3. "Database initialization tests fail"
- Check that PostgreSQL TestContainers can start
- Verify Docker has enough memory (>2GB recommended)
- Ensure no conflicting PostgreSQL instances running

#### 4. "Static state conflicts between tests"
- Tests properly reset static state in `Dispose()` methods
- Use `[Collection]` attributes to control test execution order if needed

### Performance Considerations

#### Test Execution Times
- **Unit Tests**: < 50ms per test (mocked dependencies)
- **Integration Tests**: 2-10 seconds per test (real PostgreSQL container)
- **Full Suite**: < 2 minutes total

#### TestContainers Optimization
- Container reuse via shared fixtures (PostgreSqlIntegrationFixture)
- Tests in same collection share container instance
- Automatic cleanup after test collection completes

## Test Data and Scenarios

### Test User Accounts Created
```
admin@witchcityrope.com / Test123!     - Administrator, Vetted
teacher@witchcityrope.com / Test123!   - Teacher, Vetted
vetted@witchcityrope.com / Test123!    - Member, Vetted  
member@witchcityrope.com / Test123!    - Member, Not Vetted
guest@witchcityrope.com / Test123!     - Attendee, Not Vetted
```

### Test Events Created
- **10 Upcoming Events**: Workshops, classes, meetups with realistic scheduling
- **2 Past Events**: For testing historical data scenarios
- **UTC DateTime Handling**: All dates created with proper UTC specification
- **Realistic Data**: Proper capacity, pricing, and location information

## Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Run Database Initialization Unit Tests
  run: dotnet test tests/unit/api/Services/ --filter "*DatabaseInitialization*" --logger "trx"

- name: Run Database Initialization Integration Tests  
  run: |
    # Ensure Docker is available
    docker --version
    # Run integration tests with real PostgreSQL
    dotnet test tests/integration/ --logger "trx"
```

## Benefits of This Test Suite

### For Development
- ✅ Validates Milan Jovanovic's fail-fast patterns work correctly
- ✅ Ensures database initialization is reliable across environments
- ✅ Tests both happy path and error scenarios comprehensively
- ✅ Provides confidence in PostgreSQL compatibility (UTC dates, constraints)
- ✅ Validates health check integration for monitoring systems

### For Operations
- ✅ Tests concurrent initialization attempts and static state management
- ✅ Verifies proper error classification and structured logging
- ✅ Validates timeout and retry policies work as expected
- ✅ Ensures idempotent operations safe for production deployment

### For Quality Assurance
- ✅ Comprehensive test coverage (unit + integration)
- ✅ Real database testing eliminates bugs hidden by mocking
- ✅ Cross-environment behavior verification (Dev vs Production)
- ✅ Performance benchmarking and timing validation

## Documentation Updates

The following documentation has been updated with these tests:

1. **Test Catalog**: `/docs/standards-processes/testing/TEST_CATALOG.md`
   - Added comprehensive entry for database initialization test suite
   - Updated test counts and coverage information

2. **Lessons Learned**: `/docs/lessons-learned/test-developer-lessons-learned.md`
   - Added new patterns for background service testing
   - Added PostgreSQL TestContainers integration patterns
   - Added ASP.NET Core Identity testing patterns
   - Added health check testing with service scopes

## Next Steps

1. **Run Tests Regularly**: Incorporate into development workflow
2. **Monitor Performance**: Track test execution times and optimize as needed
3. **Expand Coverage**: Add more edge cases as they're discovered in production
4. **Integrate Monitoring**: Use health check patterns for production monitoring

---

*These tests provide confidence that the database auto-initialization feature works reliably across all environments and scenarios.*