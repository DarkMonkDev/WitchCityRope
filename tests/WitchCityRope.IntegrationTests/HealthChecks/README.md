# Integration Test Health Checks

This directory contains comprehensive health checks for integration tests to ensure the database is ready before running tests.

## Overview

The health check system validates three critical aspects before integration tests run:

1. **Database Connection** - Verifies PostgreSQL container is running and accepting connections
2. **Database Schema** - Ensures all required tables and migrations are applied
3. **Seed Data** - Validates that test users and roles exist for authentication tests

## Architecture

### Health Check Classes

- **`DatabaseHealthCheck`** - Tests basic database connectivity
- **`DatabaseSchemaHealthCheck`** - Verifies all required tables exist
- **`SeedDataHealthCheck`** - Ensures test data is present
- **`IntegrationTestHealthChecker`** - Orchestrates all health checks with retry logic

### Integration

The health checks are automatically run by the `PostgreSqlFixture` before any integration tests execute.

## Usage

### Automatic Health Checks

Health checks run automatically when the PostgreSQL fixture initializes:

```csharp
[Collection("PostgreSQL")]
public class MyIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;
    
    public MyIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture; // Health checks already passed at this point
    }
    
    [Fact]
    public async Task MyTest_ShouldWork()
    {
        // Test code here - database is guaranteed to be ready
    }
}
```

### Manual Health Check Validation

You can run health checks manually to validate database state:

```bash
# Run only health check tests
dotnet test --filter "Category=HealthCheck"

# Run a specific health check test
dotnet test --filter "FullyQualifiedName~DatabaseConnection_ShouldBeHealthy"
```

### Debugging Health Check Failures

If health checks fail, check the following:

1. **Docker Containers**: Ensure PostgreSQL container is running
   ```bash
   docker ps | grep postgres
   ```

2. **Database Connection**: Test direct connection to PostgreSQL
   ```bash
   docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT 1;"
   ```

3. **Schema Status**: Check if migrations are applied
   ```bash
   docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' OR table_schema = 'auth';"
   ```

## Configuration

### Required Tables

The health checks validate these tables exist:

**Public Schema:**
- Events, Tickets, Rsvps, Payments, UserNotes
- VettingApplications, VolunteerTasks, RefreshTokens
- IncidentReports, EventEmailTemplates

**Auth Schema:**
- Users, Roles, UserRoles, UserClaims, RoleClaims
- UserLogins, UserTokens

### Test Users

The seed data health check looks for these test users:
- `admin@witchcityrope.com`
- `teacher@witchcityrope.com`
- `member@witchcityrope.com`
- `vetted@witchcityrope.com`

## Best Practices

1. **Always use the PostgreSQL collection** for integration tests
2. **Don't skip health checks** - they prevent hard-to-debug test failures
3. **Check health check results** if integration tests fail unexpectedly
4. **Update health checks** when adding new required tables or seed data

## Troubleshooting

### Common Issues

1. **Container not ready**: Health checks include retry logic, but very slow systems may need longer delays
2. **Missing migrations**: Ensure EF Core migrations are applied to the test database
3. **Missing seed data**: Verify the database seeding process includes test users

### Health Check Status Meanings

- **Healthy**: All checks passed, tests can proceed
- **Degraded**: Minor issues detected, tests may work but could be unreliable
- **Unhealthy**: Critical issues detected, tests will likely fail

### Extending Health Checks

To add new health checks:

1. Create a new class implementing `IHealthCheck`
2. Add it to the `IntegrationTestHealthChecker` constructor
3. Update the `README.md` with new requirements
4. Add corresponding tests in `DatabaseHealthTests.cs`

Example:
```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Your health check logic here
        return HealthCheckResult.Healthy("Custom check passed");
    }
}
```

## Performance

- Health checks run once per test session during fixture initialization
- Individual health checks have 5-second timeouts
- Retry logic provides resilience against transient failures
- Total health check time: typically 5-15 seconds

This system ensures reliable integration tests by validating database readiness before any tests execute.