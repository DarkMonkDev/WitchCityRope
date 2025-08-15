# Integration Test Patterns with PostgreSQL

> 🚨 **CRITICAL: Integration tests use real PostgreSQL via Testcontainers** 🚨
> 
> **The in-memory database was hiding critical bugs. Always use these patterns.**

## Overview

As of January 2025, all integration tests have been migrated from in-memory database to real PostgreSQL using Testcontainers. This migration exposed and fixed several critical bugs that were hidden by the in-memory provider.

## Key Changes from In-Memory to PostgreSQL

### Migration Summary
- **Before**: 93 tests couldn't run due to architectural issues
- **After migration**: 37 failures (database issues exposed)
- **Final**: 115+ passing, stable infrastructure

### Critical Bugs Found and Fixed
1. **RefundAmount nullable owned entity bug** - EF Core can't configure nullable owned entities
2. **RSVP duplicate key violations** - Missing Id initialization in constructor
3. **DateTime UTC requirements** - PostgreSQL timestamp with time zone only accepts UTC
4. **Duplicate test data** - Tests failing due to unique constraint violations

## Health Check System

### Always Run Health Checks First

```bash
# 1. FIRST: Run health checks to verify containers are ready
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. ONLY IF health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

### What Health Checks Validate
- ✅ Database connection (PostgreSQL container is running)
- ✅ Database schema (all required tables and migrations applied)
- ✅ Seed data (test users and roles exist)

### Troubleshooting Failed Health Checks

```bash
# Check Docker containers
docker ps

# Check database connection
docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT 1;"

# Check applied migrations
docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' OR table_schema = 'auth';"

# Clean up stale containers if needed
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f
```

## Critical PostgreSQL Patterns

### 1. DateTime Must Be UTC

```csharp
// ❌ WRONG - Kind is Unspecified
new DateTime(1990, 1, 1)

// ✅ CORRECT - Always specify UTC
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)

// ✅ BEST - Use UTC helpers
DateTime.UtcNow
dateTime.ToUniversalTime()
```

**DbContext Auto-Conversion**: The DbContext now automatically converts all DateTime values to UTC in `UpdateAuditFields()`.

### 2. Test Data Must Be Unique

```csharp
// ❌ WRONG - Same name used by multiple tests
SceneName.Create("TestUser")

// ✅ CORRECT - Always use GUIDs for uniqueness
SceneName.Create($"TestUser_{Guid.NewGuid():N}")

// ✅ Also apply to:
// - Emails: $"test_{Guid.NewGuid():N}@example.com"
// - Event names: $"Test Event {Guid.NewGuid():N}"
// - Any unique field
```

### 3. Nullable Owned Entities Pattern

```csharp
// ❌ WRONG - EF Core can't configure nullable owned entities
public Money? RefundAmount { get; set; }
// With: OwnsOne(p => p.RefundAmount)

// ✅ CORRECT - Use separate nullable properties
public decimal? RefundAmountValue { get; private set; }
public string? RefundCurrency { get; private set; }

public Money? RefundAmount => RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency)
    ? Money.Create(RefundAmountValue.Value, RefundCurrency) 
    : null;
```

### 4. Entity Constructor Initialization

```csharp
// ❌ WRONG - Default Guid.Empty causes duplicate key violations
public Rsvp(Guid userId, Event @event)
{
    UserId = userId;
    Event = @event;
    // Id not set - will be Guid.Empty!
}

// ✅ CORRECT - Always initialize required fields
public Rsvp(Guid userId, Event @event)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this!
    UserId = userId;
    Event = @event;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

## Test Setup Patterns

### Using TestWebApplicationFactory

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgresContainer;

    public TestWebApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WitchCityRopeIdentityDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add PostgreSQL DbContext
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
            dbContext.Database.Migrate();
        });
    }
}
```

### Shared PostgreSQL Fixture Pattern

```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public MyIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Create_User()
    {
        // Arrange
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
        
        // Act - always use unique data
        var user = new WitchCityRopeUser
        {
            Email = $"test_{Guid.NewGuid():N}@example.com",
            UserName = $"testuser_{Guid.NewGuid():N}"
        };
        
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        // Assert
        var saved = await dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(saved);
    }
}
```

## Common Integration Test Patterns

### Testing with Transactions

```csharp
[Fact]
public async Task Should_Rollback_On_Error()
{
    using var scope = _fixture.Factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
    
    using var transaction = await dbContext.Database.BeginTransactionAsync();
    try
    {
        // Perform operations
        var user = new WitchCityRopeUser { /* ... */ };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        // Simulate error
        throw new Exception("Test error");
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        // Verify rollback worked
    }
}
```

### Testing Migrations

```csharp
[Fact]
public async Task Should_Apply_All_Migrations()
{
    using var scope = _fixture.Factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
    
    // Get pending migrations
    var pending = await dbContext.Database.GetPendingMigrationsAsync();
    Assert.Empty(pending);
    
    // Verify specific tables exist
    var tableExists = await dbContext.Database
        .ExecuteSqlRawAsync("SELECT 1 FROM information_schema.tables WHERE table_name = 'Users'");
    Assert.Equal(1, tableExists);
}
```

### Testing Seed Data

```csharp
[Fact]
public async Task Should_Have_Seeded_Admin_User()
{
    using var scope = _fixture.Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
    
    var adminUser = await userManager.FindByEmailAsync("admin@witchcityrope.com");
    Assert.NotNull(adminUser);
    
    var isAdmin = await userManager.IsInRoleAsync(adminUser, "Admin");
    Assert.True(isAdmin);
}
```

## Performance Considerations

### Container Reuse
Tests in the same collection share the PostgreSQL container:
```csharp
[Collection("PostgreSQL Integration Tests")] // Shares container
public class EventTests { }

[Collection("PostgreSQL Integration Tests")] // Same container
public class UserTests { }
```

### Parallel Execution
- Tests within a collection run sequentially
- Different collections run in parallel
- Use unique data to avoid conflicts

### Cleanup Patterns
```csharp
public async Task Cleanup()
{
    using var scope = _fixture.Factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
    
    // Clean test data (be specific to avoid clearing seed data)
    var testUsers = dbContext.Users.Where(u => u.Email.Contains("test_"));
    dbContext.Users.RemoveRange(testUsers);
    await dbContext.SaveChangesAsync();
}
```

## Troubleshooting

### Common Issues

1. **"Cannot write DateTime with Kind=Unspecified"**
   - Always use UTC DateTimes
   - Check entity constructors
   - Verify seed data

2. **"Duplicate key value violates unique constraint"**
   - Use GUIDs in test data
   - Check for hardcoded values
   - Verify cleanup between tests

3. **"The entity type 'X' requires a primary key"**
   - Check for unwanted entity discovery
   - Verify owned entity configuration
   - Review navigation properties

4. **Container startup failures**
   - Ensure Docker is running: `sudo systemctl start docker`
   - Check disk space: `df -h`
   - Clean old containers: `docker system prune`

## Best Practices

1. **Always use real PostgreSQL** - Never go back to in-memory
2. **Run health checks first** - Ensures environment is ready
3. **Use unique test data** - Prevents conflicts
4. **Clean up after tests** - But preserve seed data
5. **Test in isolation** - Each test should be independent
6. **Use transactions for complex scenarios** - Enables rollback
7. **Monitor container resources** - PostgreSQL needs memory

## Key Files

- `/tests/WitchCityRope.IntegrationTests/TestWebApplicationFactory.cs` - Container setup
- `/tests/WitchCityRope.IntegrationTests/PostgreSqlFixture.cs` - Shared fixture
- `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs` - UTC conversion