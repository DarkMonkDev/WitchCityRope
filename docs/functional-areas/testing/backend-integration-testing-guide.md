# Backend Integration Testing Guide

## Overview

This guide explains how to write and run integration tests for backend API endpoints, with a focus on DTO/Entity mapping validation. These tests prevent bugs like the profile update issue where DTO fields don't match entity properties, causing data loss.

**Created**: 2025-10-09
**Purpose**: Catch DTO/Entity mismatches that cause data loss bugs
**Test Infrastructure**: TestContainers + PostgreSQL + WebApplicationFactory

## Why DTO Mapping Tests Are Critical

### The Problem They Solve

The profile update bug happened because:
1. `UpdateProfileDto` had a `PhoneNumber` property
2. Developer assumed it would map to `ApplicationUser` entity
3. BUT `PhoneNumber` property didn't exist on `ApplicationUser` (it's on base `IdentityUser` class)
4. Data was silently lost when saving to database
5. Bug wasn't caught until production

### How These Tests Catch The Bug

DTO mapping tests use reflection to verify:
- **All DTO properties exist on entity** - Catches missing fields
- **Property types are compatible** - Catches type mismatches (string vs int)
- **Data saves correctly to database** - End-to-end verification with real PostgreSQL
- **API returns all DTO fields** - Catches missing field mappings in responses

## Test Project Structure

```
/tests/integration/
├── DtoMappingTestBase.cs              # Base class with validation helpers
├── IntegrationTestBase.cs             # Database + WebApplicationFactory setup
├── Dashboard/
│   └── ProfileUpdateDtoMappingTests.cs   # Profile-specific DTO tests
├── DtoValidation/
│   └── AllDtosMappingTests.cs         # Automated DTO discovery and validation
└── [Feature]/
    └── [Feature]DtoMappingTests.cs    # Feature-specific DTO tests
```

## Running The Tests

### Prerequisites

1. **Docker must be running** - Tests use TestContainers to spin up PostgreSQL
2. **API must build** - Integration tests reference API project

### Run All DTO Mapping Tests

```bash
cd /home/chad/repos/witchcityrope

# Run all integration tests
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj

# Run only DTO mapping tests
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj --filter "FullyQualifiedName~DtoMapping"

# Run automated DTO validation
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj --filter "FullyQualifiedName~AllDtosMappingTests"

# Run specific feature tests
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj --filter "FullyQualifiedName~ProfileUpdateDtoMappingTests"
```

### Test Output

Tests provide detailed output showing:
- ✅ Which DTOs passed validation
- ❌ Which DTOs have missing properties
- ⚠️  Which DTOs have type mismatches
- 📊 Complete list of all DTOs discovered in project

## Writing DTO Mapping Tests

### Pattern 1: Property Existence Validation

Verifies all DTO properties exist on entity:

```csharp
[Fact]
public void UpdateProfileDto_AllFields_ExistOnApplicationUser()
{
    // Exclude properties that are intentionally different
    var excludedProperties = new[]
    {
        "PhoneNumber" // On IdentityUser base class
    };

    // This will FAIL if DTO has properties that don't exist on entity
    AssertDtoPropertiesExistOnEntity<UpdateProfileDto, ApplicationUser>(excludedProperties);
}
```

### Pattern 2: Property Type Compatibility

Verifies DTO property types match entity property types:

```csharp
[Fact]
public void UpdateProfileDto_PropertyTypes_CompatibleWithApplicationUser()
{
    var excludedProperties = new[] { "PhoneNumber" };

    // Catches: DTO has int but entity has string, etc.
    AssertDtoPropertyTypesCompatible<UpdateProfileDto, ApplicationUser>(excludedProperties);
}
```

### Pattern 3: End-to-End Database Save

Verifies data saves correctly to real database:

```csharp
[Fact]
public async Task UpdateProfileDto_AllFields_SaveToDatabase()
{
    // Arrange: Create test DTO
    var updateDto = new UpdateProfileDto
    {
        SceneName = $"TestUser_{Guid.NewGuid():N}",
        FirstName = "John",
        LastName = "Doe",
        // ... all fields
    };

    // Act: Map DTO to entity and save
    var savedUser = await AssertDtoSavesToDatabase(
        updateDto,
        dto => new ApplicationUser
        {
            SceneName = dto.SceneName,
            FirstName = dto.FirstName,
            // ... map all fields
        },
        ctx => ctx.Users
    );

    // Assert: All fields persisted correctly
    savedUser.SceneName.Should().Be(updateDto.SceneName);
    savedUser.FirstName.Should().Be(updateDto.FirstName);
    // ... assert all fields
}
```

### Pattern 4: API Endpoint Response Validation

Verifies API returns all DTO fields after update:

```csharp
[Fact]
public async Task UpdateProfileEndpoint_ReturnsAllDtoFields_AfterUpdate()
{
    // Arrange: Create test user in database
    var testUser = new ApplicationUser { /* ... */ };
    // ... save to database

    // Act: Call API endpoint
    var client = CreateAuthenticatedClient(testUser.Id.ToString(), testUser.Email);
    var response = await client.PutAsJsonAsync("/api/dashboard/profile", updateDto);

    // Assert: All fields returned
    response.IsSuccessStatusCode.Should().BeTrue();
    var returnedProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
    returnedProfile!.FirstName.Should().Be(updateDto.FirstName);
    // ... assert all fields
}
```

## Automated DTO Discovery

The `AllDtosMappingTests` class uses reflection to find **ALL** DTOs in the project automatically:

```csharp
[Fact]
public void AllDtos_PropertiesMatchEntities()
{
    // Map DTOs to entities with excluded properties
    var dtoToEntityMappings = new Dictionary<Type, (Type, string[])>
    {
        [typeof(UpdateProfileDto)] = (typeof(ApplicationUser), new[] { "PhoneNumber" }),
        [typeof(EventDto)] = (typeof(Event), new[] { "CurrentRsvps", "CurrentTickets" }),
        // ... add new DTOs here
    };

    // Validates ALL DTOs automatically
    // Test FAILS if any DTO has properties not on entity
}
```

**When adding a new DTO:**
1. Add mapping to `dtoToEntityMappings` dictionary
2. Specify excluded properties (computed fields, related data)
3. Run tests to verify

## Common Patterns

### Excluding Properties

Some DTO properties are intentionally different from entities:

```csharp
// Computed fields (not in database)
new[] { "CurrentRsvps", "CurrentTickets", "AvailableCapacity" }

// Related entity data (joined in query)
new[] { "UserName", "UserEmail", "EventTitle" }

// Base class properties (on IdentityUser not ApplicationUser)
new[] { "PhoneNumber", "EmailConfirmed" }

// Foreign keys handled differently in DTOs
new[] { "EventId", "SessionId" }
```

### Handling DateTime Properties

All DateTime properties must be UTC for PostgreSQL:

```csharp
// In entity constructor or initialization
CreatedAt = DateTime.UtcNow

// In DTO mapping
StartDate = new DateTime(2025, 10, 9, 14, 30, 0, DateTimeKind.Utc)

// Tests verify UTC handling
[Fact]
public void AllDtos_WithDateTimeProperties_UseUtc()
{
    // Documents which DTOs have DateTime fields
    // Integration tests verify UTC in database saves
}
```

### Handling Nullable Properties

DTO properties should match entity nullability:

```csharp
// DTO
public string? FirstName { get; set; }  // Nullable

// Entity
public string? FirstName { get; set; }  // Nullable

// If mismatch, test will fail
```

## Test Utilities

### CreateAuthenticatedClient

Creates HTTP client with JWT authentication:

```csharp
var client = CreateAuthenticatedClient(
    userId: testUser.Id.ToString(),
    email: testUser.Email,
    role: "Administrator"
);
```

### AssertDtoSavesToDatabase

Maps DTO to entity, saves to database, verifies persistence:

```csharp
var savedEntity = await AssertDtoSavesToDatabase(
    dto,
    mapFunction,
    dbSetSelector
);
```

### GetAllProperties

Gets properties including from base classes (for ApplicationUser : IdentityUser):

```csharp
var properties = GetAllProperties(typeof(ApplicationUser));
// Returns properties from ApplicationUser AND IdentityUser
```

## Integration with CI/CD

These tests run in CI pipeline with TestContainers:

1. CI spins up PostgreSQL container
2. Tests run against real database
3. Container destroyed after tests
4. **Tests MUST pass before merge**

## Troubleshooting

### "DTO has properties not on entity"

**Problem**: DTO has field that doesn't exist on entity

**Solution**:
1. Add property to entity
2. Create migration: `dotnet ef migrations add Add[PropertyName]`
3. OR exclude property if it's computed/related data

### "Property types incompatible"

**Problem**: DTO property type doesn't match entity

**Solution**:
1. Change DTO type to match entity
2. OR add type conversion in mapping logic
3. OR exclude if intentional (e.g., string DTO for enum entity)

### "Docker not available"

**Problem**: TestContainers can't start PostgreSQL

**Solution**:
1. Ensure Docker is running: `docker ps`
2. Check Docker permissions
3. Restart Docker daemon

### "Test database connection failed"

**Problem**: Can't connect to test database

**Solution**:
1. Check `DatabaseTestFixture` is configured correctly
2. Verify connection string uses TestContainers port
3. Check database logs in test output

## Best Practices

### 1. Test DTOs When Created

**Add DTO mapping test immediately when creating new DTO**

Don't wait until bugs appear - write validation tests upfront.

### 2. Use Automated Discovery

Add new DTOs to `AllDtosMappingTests.dtoToEntityMappings` dictionary.

This ensures they're validated automatically.

### 3. Document Excluded Properties

When excluding properties, document WHY:

```csharp
new[] {
    "CurrentRsvps",  // Computed from EventParticipations count
    "TeacherNames"   // Joined from related Teachers table
}
```

### 4. Test Database Operations

Don't just test property existence - verify end-to-end save/load:

```csharp
// Not just: "Does property exist?"
// But also: "Does data save and load correctly?"
await AssertDtoSavesToDatabase(...);
```

### 5. Run Tests Frequently

Run DTO tests after:
- Adding new DTO
- Modifying existing DTO
- Changing entity schema
- Refactoring mapping logic

## Examples

### Profile Update Tests

Location: `/tests/integration/Dashboard/ProfileUpdateDtoMappingTests.cs`

These tests would have caught the profile bug:
- `UpdateProfileDto_AllFields_ExistOnApplicationUser` - FAILS if property missing
- `UpdateProfileDto_AllFields_SaveToDatabase` - FAILS if save doesn't work
- `UpdateProfileEndpoint_ReturnsAllDtoFields_AfterUpdate` - FAILS if API doesn't return field

### Automated DTO Validation

Location: `/tests/integration/DtoValidation/AllDtosMappingTests.cs`

Tests that discover and validate ALL DTOs automatically:
- `AllDtos_HaveMatchingEntities` - Discovers all DTOs, finds entities
- `AllDtos_PropertiesMatchEntities` - Validates all mappings
- `AllRequestDtos_OnlyHaveWritableProperties` - Ensures model binding works
- `AllDtos_WithDateTimeProperties_UseUtc` - Documents DateTime handling

## Related Documentation

- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md)
- [Backend Developer Lessons](/docs/lessons-learned/backend-developer-lessons-learned.md)
- [DTO Alignment Strategy](/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md)

## Maintenance

### When to Update This Guide

- New DTO testing pattern discovered
- New helper method added to `DtoMappingTestBase`
- TestContainers configuration changes
- New common testing scenarios

### File Locations

- **Test Base Classes**: `/tests/integration/DtoMappingTestBase.cs`, `IntegrationTestBase.cs`
- **Profile Tests**: `/tests/integration/Dashboard/ProfileUpdateDtoMappingTests.cs`
- **Automated Validation**: `/tests/integration/DtoValidation/AllDtosMappingTests.cs`
- **This Guide**: `/docs/functional-areas/testing/backend-integration-testing-guide.md`

---

**Created by**: backend-developer agent
**Purpose**: Document DTO mapping validation testing patterns
**Status**: ACTIVE - Critical for preventing data loss bugs
