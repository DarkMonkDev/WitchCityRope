# Integration Test Patterns with PostgreSQL

> **CRITICAL: Integration tests use real PostgreSQL via TestContainers**
>
> **NEVER use InMemoryDatabase. Always use these patterns.**

## Overview

All integration tests use real PostgreSQL via TestContainers with Respawn for fast database cleanup. The central base class is `IntegrationTestBase` in `tests/integration/IntegrationTestBase.cs`.

### Current State (March 2026)
- **200 passing, 0 failures, 11 skipped**
- TestContainers PostgreSQL 16-alpine
- Respawn for database reset between tests
- WebApplicationFactory for HTTP endpoint testing

## Key Architecture

### DatabaseTestFixture (Shared Container)

Located at `tests/WitchCityRope.Tests.Common/Fixtures/DatabaseTestFixture.cs`.

Each test collection shares ONE PostgreSQL container via `ICollectionFixture<DatabaseTestFixture>`:

```csharp
[Collection("Database")]  // Parallel execution OK
public class MyTests : IntegrationTestBase
{
    public MyTests(DatabaseTestFixture fixture) : base(fixture) { }
}

[Collection("Sequential")]  // Sequential execution, no parallelism
public class MySequentialTests : IntegrationTestBase
{
    public MySequentialTests(DatabaseTestFixture fixture) : base(fixture) { }
}
```

**When to use which collection:**
- `"Database"` — Default. Tests run in parallel. Use unique data (GUIDs) to avoid conflicts.
- `"Sequential"` — For tests that modify shared state or have ordering dependencies. Runs one at a time.

### IntegrationTestBase

Located at `tests/integration/IntegrationTestBase.cs`. Provides:

- `CreateDbContext()` — Fresh DbContext for direct database queries
- `CreateTestWebApplicationFactory()` — WebApplicationFactory with test overrides
- `GenerateJwtToken(userId, email, role)` — Valid JWT tokens for authenticated requests
- `JsonOptions` — JsonSerializerOptions with `JsonStringEnumConverter` for DTO deserialization
- Database reset via Respawn in `InitializeAsync()`

## Critical Patterns

### 1. WebApplicationFactory Setup

`CreateTestWebApplicationFactory()` configures these test overrides:

```csharp
// Connection string points to TestContainers PostgreSQL
builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

// Disable seed data — tests create their own
builder.UseSetting("DatabaseInitialization:EnableSeedData", "false");

// Enable mock PayPal for payment tests
builder.UseSetting("USE_MOCK_PAYMENT_SERVICE", "true");

// Service replacements in ConfigureServices:
// - DbContext → TestContainers PostgreSQL with EnableDynamicJson()
// - IAntiforgery → NoOpAntiforgery (CSRF bypassed in tests)
// - IEncryptionService → MockEncryptionService (passthrough, no real encryption)
// - Hangfire → In-memory storage
```

**Why MockEncryptionService?** The real `EncryptionService` requires `Safety:EncryptionKey` config and AES-256 encryption. Tests use plaintext values like `"encrypted-capture-id"` that can't be decrypted by the real service.

**Why NoOpAntiforgery?** Tests focus on business logic, not CSRF. This means CSRF rejection tests must be skipped.

### 2. JWT Authentication in Tests

```csharp
// Generate a valid JWT token
var token = GenerateJwtToken(userId, email, "Administrator");

// Create authenticated HTTP client
var client = factory.CreateClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

The `GenerateJwtToken()` method creates real signed JWTs matching the API's configuration (same secret, issuer, audience). **NEVER** use fake token strings like `"test-token-123"` — they will fail authentication.

### 3. Enum Deserialization with JsonOptions

The API uses `JsonStringEnumConverter` for enum serialization. Tests must use the same:

```csharp
// WRONG — enums deserialize as 0 (default)
var dto = await response.Content.ReadFromJsonAsync<EventDto>();

// CORRECT — use JsonOptions from IntegrationTestBase
var dto = await response.Content.ReadFromJsonAsync<EventDto>(JsonOptions);
```

This affects any DTO with enum properties (PricingType, TemplateTriggerType, AttendanceStatus, etc.).

### 4. Event Entities Require VenueId

Every `Event` entity requires a valid `VenueId` FK. Tests must create a Venue first:

```csharp
var venue = new Venue
{
    Name = $"Test Venue {Guid.NewGuid():N}",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
context.Venues.Add(venue);
await context.SaveChangesAsync();

var eventEntity = new Event
{
    Id = Guid.NewGuid(),
    Title = "Test Event",
    VenueId = venue.Id,  // REQUIRED
    // ... other properties
};
```

### 5. DateTime Must Be UTC

PostgreSQL `timestamp with time zone` only accepts UTC:

```csharp
// WRONG
DateOfBirth = new DateTime(1990, 1, 1)

// CORRECT
DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
// or
DateOfBirth = DateTime.UtcNow.AddYears(-30)
```

### 6. Test Data Must Be Unique

Use GUIDs to prevent unique constraint violations:

```csharp
var email = $"test-{Guid.NewGuid():N}@example.com";
var sceneName = $"TestUser-{Guid.NewGuid():N}"[..15];
```

### 7. ApplicationUser Required Fields

When creating test users directly in the database:

```csharp
var user = new ApplicationUser
{
    Id = Guid.NewGuid(),
    Email = email,
    SceneName = $"User-{Guid.NewGuid():N}"[..15],
    DateOfBirth = DateTime.UtcNow.AddYears(-30),  // REQUIRED
    Role = "Member",
    EmailVerificationToken = Guid.NewGuid().ToString(),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

### 8. EventAttendees Check Constraint

The `RegistrationStatus` column has a check constraint. Valid values only:

```
'confirmed', 'waitlist', 'checked-in', 'no-show', 'cancelled'
```

Using `"active"` or any other value will throw a constraint violation.

### 9. Many-to-Many Navigation Loading

For join tables like TicketTypeSessions, use `.ThenInclude()`:

```csharp
// WRONG — TicketType.Sessions will be empty
var evt = await context.Events
    .Include(e => e.TicketTypes)
    .FirstAsync();

// CORRECT — loads the Sessions navigation on each TicketType
var evt = await context.Events
    .Include(e => e.TicketTypes)
        .ThenInclude(t => t.Sessions)
    .FirstAsync();
```

### 10. Shared Factory for High-Test-Count Classes

If a test class has 15+ tests, creating a new `WebApplicationFactory` per test causes resource exhaustion (ports, connections). Use a shared static factory:

```csharp
[Collection("Sequential")]
public class ManyTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public ManyTests(DatabaseTestFixture fixture) : base(fixture)
    {
        if (_sharedFactory == null || _sharedConnectionString != ConnectionString)
        {
            _sharedFactory?.Dispose();
            _sharedFactory = CreateTestWebApplicationFactory();
            _sharedConnectionString = ConnectionString;
        }
    }

    private WebApplicationFactory<Program> _factory => _sharedFactory!;

    public void Dispose() { /* Don't dispose shared factory */ }
}
```

This pattern is safe because Sequential collection ensures no parallel execution.

### 11. RefundService Delegates PaymentStatus Updates to Callers

`RefundService.ProcessRefundAsync()` does NOT update `TicketPurchase.PaymentStatus`. The calling code must do it:

```csharp
var refundResult = await refundService.ProcessRefundAsync(request, ct);
if (refundResult.IsSuccess)
{
    ticketPurchase.PaymentStatus = TicketPurchasePaymentStatus.Refunded;
    await context.SaveChangesAsync(ct);
}
```

Tests should verify `TicketPurchase.PaymentStatus`, not `EventAttendance.Status`, for refund assertions.

## Collection Definitions

Defined in `tests/integration/`:

| Collection | File | Behavior |
|-----------|------|----------|
| `"Database"` | `DatabaseTestFixture.cs` | Parallel, shared PostgreSQL container |
| `"Sequential"` | `SequentialTestCollectionDefinition.cs` | Sequential, shared PostgreSQL container |

## Troubleshooting

### "The entry point exited without ever building an IHost"
- **Cause**: Too many WebApplicationFactory instances created (resource exhaustion)
- **Fix**: Use shared static factory pattern (see pattern #10 above)
- **Note**: Passes individually but fails in full run = this issue

### "Cannot write DateTime with Kind=Unspecified"
- Always use UTC DateTimes (see pattern #5)

### "Duplicate key value violates unique constraint"
- Use GUIDs in all test data (see pattern #6)

### "FK constraint violation on VenueId"
- All Event entities need a valid Venue (see pattern #4)

### Enum properties deserialize as 0/default
- Use `JsonOptions` with `JsonStringEnumConverter` (see pattern #3)

### CSRF test expects 400 but gets 200
- `NoOpAntiforgery` always validates. Skip CSRF rejection tests with reason.

## Key Files

- `tests/integration/IntegrationTestBase.cs` — Central base class
- `tests/WitchCityRope.Tests.Common/Fixtures/DatabaseTestFixture.cs` — Container + Respawn
- `tests/integration/SequentialTestCollectionDefinition.cs` — Sequential collection
- `apps/api/Features/Shared/Services/MockEncryptionService.cs` — Passthrough encryption for tests
- `apps/api/Features/Payments/Services/MockPayPalService.cs` — Mock PayPal for tests
