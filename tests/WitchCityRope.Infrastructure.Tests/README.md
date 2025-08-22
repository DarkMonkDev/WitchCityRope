# WitchCityRope Infrastructure Tests

This project contains integration and unit tests for the WitchCityRope infrastructure layer.

## Test Categories

### Data Tests
- **WitchCityRopeDbContextTests**: Tests for DbContext configuration, audit fields, and basic CRUD operations
- **EntityConfigurationTests**: Tests for entity relationships, constraints, and value object storage
- **ComplexQueryTests**: Tests for complex queries, joins, pagination, and performance
- **MigrationTests**: Tests for database migrations, schema validation, and rollback scenarios
- **ConcurrencyTests**: Tests for handling concurrent updates, deadlocks, and optimistic concurrency

### Security Tests
- **EncryptionServiceTests**: Tests for encryption/decryption, hashing, and key management
- **JwtTokenServiceTests**: Tests for JWT token generation, validation, and claims

### Email Tests
- **EmailServiceTests**: Tests for SendGrid integration, email templates, and bulk sending

### PayPal Tests
- **PayPalServiceTests**: Tests for PayPal payment processing, refunds, and payment intents

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Category
```bash
# Run only unit tests (fast)
dotnet test --filter "Category=Unit"

# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Infrastructure

### Fixtures
- **DatabaseFixture**: Provides PostgreSQL test containers for integration tests
- **TestDbContextFactory**: Creates in-memory database contexts for unit tests
- **IntegrationTestBase**: Base class for all integration tests with common setup

### Test Databases
- **In-Memory SQLite**: Used for fast unit tests that need a real database provider
- **EF Core InMemory**: Used for simple unit tests (doesn't enforce constraints)
- **PostgreSQL Container**: Used for integration tests that need real database behavior

## Best Practices

1. **Isolation**: Each test creates its own database context and data
2. **Cleanup**: Tests clean up after themselves using IAsyncLifetime
3. **Parallel Execution**: Tests can run in parallel using xUnit collections
4. **Real Database**: Integration tests use real PostgreSQL via TestContainers

## Dependencies

- xUnit: Test framework
- FluentAssertions: Assertion library
- Moq: Mocking framework
- TestContainers: Docker container management for tests
- Microsoft.EntityFrameworkCore.InMemory: In-memory database provider
- Microsoft.EntityFrameworkCore.Sqlite: SQLite provider for tests

## Troubleshooting

### Docker Not Running
If you see errors about Docker not being available:
1. Ensure Docker Desktop is installed and running
2. Run only unit tests: `dotnet test --filter "Category!=Integration"`

### Database Connection Errors
1. Check that no other tests are using the same database
2. Ensure TestContainers can pull the PostgreSQL image
3. Check firewall settings for Docker

### Slow Tests
1. Use in-memory databases for unit tests
2. Run integration tests separately
3. Use test collections to control parallelization