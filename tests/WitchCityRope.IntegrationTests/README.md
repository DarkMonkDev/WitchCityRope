# WitchCityRope Integration Tests

This project contains integration tests for the WitchCityRope application, testing real database interactions, API endpoints, and authentication flows.

## 🚨 IMPORTANT: PostgreSQL Test Infrastructure

**As of January 15, 2025, all integration tests use real PostgreSQL containers via Testcontainers.**

This change was made because the in-memory database was hiding critical bugs. Using real PostgreSQL ensures our tests catch actual database behavior and constraints.

## Architecture

### PostgreSQL Testcontainers
Instead of using an in-memory database, we use Testcontainers to spin up a real PostgreSQL instance for each test run. This provides:

- **Real database behavior**: Tests actual PostgreSQL features, constraints, and SQL syntax
- **Migration testing**: Ensures EF Core migrations work correctly
- **Seed data validation**: Tests the actual data initialization process
- **Production parity**: Same database engine as production

### Test Infrastructure

### PostgreSQL Testcontainers

All tests use a shared PostgreSQL container managed by `TestWebApplicationFactory`:
- PostgreSQL 16-alpine image
- Automatic container lifecycle management
- Real migrations and seed data
- Shared across all tests in a collection for performance

### Test Collection Pattern

All test classes must use the PostgreSQL test collection:

```csharp
[Collection("PostgreSQL Integration Tests")]
public class YourTestClass
{
    private readonly TestWebApplicationFactory _factory;
    
    public YourTestClass(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }
}
```

## Common Issues and Solutions

### 1. DateTime UTC Errors

PostgreSQL requires all timestamps to be UTC:

```csharp
// Wrong
new DateTime(2025, 1, 15)

// Correct
new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
```

### 2. Duplicate Key Violations

Always use unique test data:

```csharp
// Wrong - causes conflicts
var email = "test@example.com";

// Correct - unique every time
var email = $"test_{Guid.NewGuid():N}@example.com";
```

### 3. Container Cleanup

If tests fail to clean up containers:

```bash
# View running containers
docker ps -a | grep testcontainers

# Clean up orphaned containers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f
```

### 4. Entity Tracking Conflicts

Ensure entities have initialized IDs:

```csharp
public MyEntity()
{
    Id = Guid.NewGuid(); // Don't forget this!
}
```

## Running Tests

```bash
# Ensure Docker is running
sudo systemctl start docker

# Run all integration tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthenticationTests"

# Run with detailed output
dotnet test --logger "console;verbosity=normal"
```

## Prerequisites

- Docker must be running (required for PostgreSQL containers)
- .NET 9.0 SDK
- Sufficient disk space for PostgreSQL images

## Test Categories

### Authentication Tests
- User registration and login
- JWT token generation
- Password validation
- Two-factor authentication

### Event Tests
- Event creation and management
- Registration and capacity handling
- Ticket generation
- RSVP functionality

### Payment Tests
- PayPal integration
- Refund processing
- Transaction recording

### User Management Tests
- Profile creation and updates
- Scene name validation
- Vetting status tracking

## Writing New Tests

1. **Always use the test collection**:
   ```csharp
   [Collection("PostgreSQL Integration Tests")]
   ```

2. **Use unique test data**:
   ```csharp
   var testId = Guid.NewGuid().ToString("N");
   var sceneName = $"TestUser_{testId}";
   ```

3. **Create service scopes**:
   ```csharp
   await using var scope = _factory.Services.CreateAsyncScope();
   var service = scope.ServiceProvider.GetRequiredService<IYourService>();
   ```

4. **Use UTC DateTimes**:
   ```csharp
   var createdAt = DateTime.UtcNow;
   ```

## Migration from In-Memory Database

For detailed information about the migration from in-memory to PostgreSQL, see:
- [MIGRATION_TO_POSTGRESQL.md](./MIGRATION_TO_POSTGRESQL.md)

Key points:
- In-memory was hiding nullable configuration issues
- Real PostgreSQL enforces constraints properly
- DateTime values must be UTC
- Entity IDs must be initialized

## Performance Considerations

- First test run takes ~5-10 seconds to start container
- Subsequent runs reuse the container
- Tests in the same collection run sequentially
- Container is disposed after all tests complete

## Debugging

To enable detailed EF Core logging:

```csharp
services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine);
});
```

To view PostgreSQL logs:

```bash
# Find container ID
docker ps | grep postgres

# View logs
docker logs <container-id>
```

## CI/CD Integration

For GitHub Actions or other CI environments:

```yaml
- name: Run Integration Tests
  run: dotnet test tests/WitchCityRope.IntegrationTests
  env:
    TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE: /var/run/docker.sock
    DOCKER_HOST: unix:///var/run/docker.sock
```

## Test Results

As of January 15, 2025:
- **Total Tests**: 133
- **Passing**: 115 (86%)
- **Failing**: 18 (UI/navigation tests)
- **Success Rate**: 86%

The migration to PostgreSQL revealed and fixed 4 critical bugs that were hidden by the in-memory provider.