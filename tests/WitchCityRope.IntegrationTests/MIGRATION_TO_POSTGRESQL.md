# PostgreSQL Integration Test Migration Guide

## Overview

This document details the migration from in-memory database to PostgreSQL for integration tests in the WitchCityRope project. This migration was completed on January 15, 2025, and revealed several critical bugs that were hidden by the in-memory provider.

## Why We Migrated

The in-memory database provider was hiding several critical issues:
1. **Database provider conflicts** - "Services for database providers 'Npgsql.EntityFrameworkCore.PostgreSQL', 'Microsoft.EntityFrameworkCore.InMemory' have been registered"
2. **Nullable owned entity configuration errors** - EF Core's limitations weren't apparent with in-memory
3. **Missing entity initialization** - In-memory didn't enforce unique constraints properly
4. **DateTime timezone issues** - PostgreSQL's strict UTC requirements weren't tested
5. **Constraint violations** - Real foreign key and unique constraints weren't validated

## Critical Bugs Discovered and Fixed

### 1. RefundAmount Nullable Owned Entity Issue

**Problem**: EF Core cannot configure nullable owned entities with required properties. This was causing:
```
The property 'Payment.RefundAmount#Money.Amount' cannot be marked as nullable/optional because the type of the property is 'decimal'
```

**Solution**: Restructured the Payment entity to use separate nullable properties:

```csharp
// Before (caused configuration errors)
public Money? RefundAmount { get; private set; }

// After - in Payment.cs
public decimal? RefundAmountValue { get; private set; }
public string? RefundCurrency { get; private set; }

public Money? RefundAmount => RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency)
    ? Money.Create(RefundAmountValue.Value, RefundCurrency)
    : null;
```

**Configuration Update**:
```csharp
// In PaymentConfiguration.cs
builder.Property(p => p.RefundAmountValue)
    .HasColumnName("RefundAmount")
    .HasPrecision(18, 2);

builder.Property(p => p.RefundCurrency)
    .HasMaxLength(3);

builder.Ignore(p => p.RefundAmount);
```

### 2. RSVP Duplicate Key Violations

**Problem**: RSVP entities were created without initializing the Id, causing:
```
The instance of entity type 'Rsvp' cannot be tracked because another instance with the key value '{Id: 00000000-0000-0000-0000-000000000000}' is already being tracked
```

**Solution**: Added Id initialization in the RSVP constructor:

```csharp
public Rsvp(WitchCityRopeUser user, Event @event)
{
    Id = Guid.NewGuid(); // This was missing!
    User = user ?? throw new ArgumentNullException(nameof(user));
    Event = @event ?? throw new ArgumentNullException(nameof(@event));
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

### 3. DateTime UTC Requirements

**Problem**: PostgreSQL's `timestamp with time zone` only accepts UTC DateTimes:
```
Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported
```

**Solution**: Added automatic UTC conversion in DbContext:

```csharp
// In WitchCityRopeIdentityDbContext.cs
private void UpdateAuditFields(ChangeTracker changeTracker)
{
    foreach (var entry in changeTracker.Entries())
    {
        // Convert all DateTime values to UTC
        foreach (var property in entry.Properties)
        {
            if (property.CurrentValue is DateTime dateTime)
            {
                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
                else if (dateTime.Kind == DateTimeKind.Local)
                {
                    property.CurrentValue = dateTime.ToUniversalTime();
                }
            }
        }
    }
}
```

### 4. Duplicate Test Data Issues

**Problem**: Tests were failing with unique constraint violations:
```
duplicate key value violates unique constraint "IX_WitchCityRopeUsers_SceneName"
```

**Solution**: All test data now uses GUIDs to ensure uniqueness:

```csharp
// Before (caused unique constraint violations)
var sceneName = SceneName.Create("TestUser");
var email = "test@example.com";

// After
var sceneName = SceneName.Create($"TestUser_{Guid.NewGuid():N}");
var email = $"test_{Guid.NewGuid():N}@example.com";
```

## Migration Steps

### 1. Update Test Class to Use PostgreSQL Collection

**Before:**
```csharp
public class YourTests : IClassFixture<TestWebApplicationFactory>
{
    // ...
}
```

**After:**
```csharp
[Collection("PostgreSQL Integration Tests")]
public class YourTests
{
    private readonly TestWebApplicationFactory _factory;
    
    public YourTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }
}
```

### 2. Use Service Scope for Database Access

```csharp
[Fact]
public async Task Should_Save_And_Retrieve_Event()
{
    // Create a service scope for this test
    await using var scope = _factory.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    
    // Your test logic here
}
```

### 3. Ensure DateTime Values are UTC

```csharp
// Wrong - will fail with PostgreSQL
var dateOfBirth = new DateTime(1990, 1, 1);

// Correct
var dateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
// OR
var createdAt = DateTime.UtcNow;
```

### 4. Use Unique Test Data

```csharp
// Create unique test data for every test
var testId = Guid.NewGuid().ToString("N");
var sceneName = SceneName.Create($"TestUser_{testId}");
var email = EmailAddress.Create($"test_{testId}@example.com");
var phoneNumber = PhoneNumber.Create($"555-{testId.Substring(0, 4)}");
```

### 5. Handle Password Validation in Test Environment

```csharp
// In test environment, password validation is relaxed
if (_factory.Services.GetRequiredService<IWebHostEnvironment>().IsEnvironment("Testing"))
{
    // Simple passwords like "Test123!" are allowed
}
```

## Key Infrastructure Components

### TestWebApplicationFactory

The factory now manages a PostgreSQL container lifecycle:

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<WitchCityRope.Web.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    
    public TestWebApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(d => 
                d.ServiceType == typeof(DbContextOptions<WitchCityRopeIdentityDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
            
            // Add PostgreSQL DbContext
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });
        });
    }
}
```

### PostgreSqlFixture

Shared fixture ensures all tests use the same container:

```csharp
[CollectionDefinition("PostgreSQL Integration Tests")]
public class PostgreSqlTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
```

## Common Pitfalls to Avoid

### 1. DateTime Without UTC

```csharp
// BAD - Will throw: Cannot write DateTime with Kind=Unspecified
new DateTime(2025, 1, 15)

// GOOD
new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
DateTime.UtcNow
```

### 2. Hardcoded Test Data

```csharp
// BAD - Will throw: duplicate key value violates unique constraint
email: "test@example.com"
sceneName: "TestUser"

// GOOD
email: $"test_{Guid.NewGuid():N}@example.com"
sceneName: $"TestUser_{Guid.NewGuid():N}"
```

### 3. Missing Entity Initialization

```csharp
// BAD - Missing required field initialization
public MyEntity(string name)
{
    Name = name;
    // Id not set - will cause duplicate key errors!
}

// GOOD
public MyEntity(string name)
{
    Id = Guid.NewGuid();
    Name = name;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

### 4. Nullable Owned Entities

```csharp
// BAD - EF Core cannot configure nullable owned entities with required properties
public Money? RefundAmount { get; set; } // Money has required Amount property

// GOOD - Use separate nullable properties
public decimal? RefundAmountValue { get; set; }
public string? RefundCurrency { get; set; }
public Money? RefundAmount => RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency)
    ? Money.Create(RefundAmountValue.Value, RefundCurrency)
    : null;
```

## Troubleshooting

### Container Issues

If tests fail with container errors:

```bash
# Check if Docker is running
sudo systemctl status docker

# Check if containers are running
docker ps -a | grep testcontainers

# Clean up orphaned containers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f

# Check disk space
df -h
```

### DateTime Errors

If you see "Cannot write DateTime with Kind=Unspecified":
1. Check all DateTime values are created with DateTimeKind.Utc
2. Review the DbContext UpdateAuditFields method is working
3. Use DateTime.UtcNow instead of DateTime.Now
4. Check test helper methods for DateTime creation

### Duplicate Key Violations

If you see duplicate key constraint violations:
1. Ensure all test data uses unique values (GUIDs)
2. Check entity constructors initialize Id field
3. Verify test cleanup between test runs
4. Look for hardcoded emails or scene names

### Performance Issues

1. **Container Startup**: First test run takes ~5-10 seconds to start container
2. **Shared Container**: All tests in collection share one container instance
3. **Parallel Execution**: Tests within a collection run sequentially
4. **Cleanup**: Container is automatically disposed after all tests complete

## Test Results Summary

### Before Migration
- 93 tests failing due to database provider conflicts
- Unable to run integration tests at all
- In-memory database hiding real issues

### After Migration (January 15, 2025)
- **Total Tests**: 133
- **Passing**: 115 (86%)
- **Failing**: 18 (UI/navigation tests only)
- **Key Issues Fixed**: 4 critical bugs discovered and resolved

## Benefits of This Approach

1. **Real Database Behavior**: Tests catch actual PostgreSQL constraints and behaviors
2. **Migration Testing**: Ensures migrations work correctly on PostgreSQL
3. **Seed Data Validation**: Tests the same DbInitializer used in production
4. **No False Positives**: In-memory provider quirks don't affect tests
5. **Better Confidence**: Tests reflect production behavior accurately
6. **Bug Discovery**: Found critical issues that were hidden by in-memory database

## Conclusion

While the migration required significant effort and revealed several bugs, the result is a much more reliable test suite that catches real issues before they reach production. The bugs discovered (nullable owned entities, missing Id initialization, DateTime UTC issues, duplicate test data) would have caused serious production issues if not caught.

**Key Takeaway**: Always prefer real database testing over in-memory providers for integration tests. The initial setup cost is worth the confidence gained from testing against real database behavior.