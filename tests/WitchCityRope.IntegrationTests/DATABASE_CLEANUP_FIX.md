# Database Cleanup Fix for Integration Tests

## Problem
Integration tests were failing with the error:
```
Npgsql.PostgresException (0x80004005): 42P07: relation "Events" already exists
```

This occurred because the test setup was trying to create database tables that already existed from previous test runs.

## Solution Implemented

### 1. Updated TestWebApplicationFactory
Modified the database initialization logic to:
- Check if tables already exist before trying to create them
- Clean up existing data using TRUNCATE/DELETE instead of dropping the entire database
- Handle errors gracefully when tables don't exist

### 2. Created DatabaseCleaner
Added a new `DatabaseCleaner` class that uses Respawn for proper database cleanup between tests:
- Respawn intelligently handles foreign key relationships
- Preserves system tables and migrations history
- Provides fast, reliable cleanup

### 3. Created IntegrationTestBaseWithCleanup
A new base class that:
- Automatically cleans the database before each test (configurable)
- Provides helper methods for database assertions
- Ensures each test starts with a known, clean state

## Usage

### For New Tests
Inherit from `IntegrationTestBaseWithCleanup` instead of `IntegrationTestBase`:

```csharp
public class MyIntegrationTest : IntegrationTestBaseWithCleanup
{
    public MyIntegrationTest(PostgreSqlFixture postgresFixture) 
        : base(postgresFixture)
    {
    }

    [Fact]
    public async Task MyTest()
    {
        // Database is automatically cleaned before this test runs
        // Your test code here
    }
}
```

### For Existing Tests
To fix existing tests without changing their base class:

1. Update the test to handle existing database state
2. Or migrate to use `IntegrationTestBaseWithCleanup`

### Disabling Automatic Cleanup
If a test doesn't need database cleanup (for performance):

```csharp
protected override bool ShouldResetDatabaseBeforeTest => false;
```

## What Was Changed

1. **TestWebApplicationFactory.cs**
   - Added logic to check if tables exist before creating
   - Implemented data cleanup instead of schema recreation
   - Added better error handling

2. **DatabaseCleaner.cs** (new file)
   - Implements Respawn-based database cleanup
   - Handles database initialization

3. **IntegrationTestBaseWithCleanup.cs** (new file)
   - Enhanced base class with automatic cleanup
   - Provides database assertion helpers

4. **WitchCityRope.IntegrationTests.csproj**
   - Added Respawn package reference

## Running Tests

The tests should now run successfully without the "table already exists" error:

```bash
dotnet test WitchCityRope.IntegrationTests.csproj
```

## Troubleshooting

If you still encounter issues:

1. **Ensure Docker is running** - Tests use Testcontainers for PostgreSQL
2. **Check for stale containers** - Remove any orphaned test containers
3. **Verify connection string** - Ensure the test is using the correct database
4. **Check logs** - The updated code provides detailed logging of database operations

## Benefits

- ✅ No more "table already exists" errors
- ✅ Tests are isolated from each other
- ✅ Faster than dropping/recreating the entire database
- ✅ Maintains referential integrity
- ✅ Easy to use and extend