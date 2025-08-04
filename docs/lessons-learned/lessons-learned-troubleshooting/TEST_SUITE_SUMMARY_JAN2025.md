# API Test Suite Summary - January 2025

## Test Results Overview
- **Total Tests**: 123
- **Passing**: 117 (95%)
- **Failing**: 6 (5%)
- **Status**: Major improvements achieved, remaining failures are architectural issues

## Categories Fixed

### 1. Model Validation Tests (27 tests - ALL FIXED ✅)
**Problem**: C# positional record parameters don't support validation attributes
```csharp
// This doesn't work:
public record CreateEventRequest([Required] string Title);

// Fixed by converting to:
public record CreateEventRequest
{
    [Required] public string Title { get; init; } = null!;
}
```

### 2. Validator Tests (3 tests - ALL FIXED ✅)
**Problems Fixed**:
- Phone validation was stripping formatting then checking length
- Error message case sensitivity ("at least" vs "At least")
- Invalid test phone numbers (only 7 digits when cleaned)

### 3. Concurrency Tests (8 tests - 2 FIXED, 6 REMAINING ❌)
**Fixed**:
- Payment processing concurrency
- Duplicate slug generation

**Still Failing**:
1. **Capacity enforcement** - Multiple threads exceed event capacity
2. **Optimistic concurrency** - Both updates fail, leaving original data
3. **Entity ownership** - EF Core can't save Money value objects
4. **Event filtering** - Complex queries return empty results
5. **Duplicate keys** - Scene name constraint violations
6. **Registration at capacity** - First user incorrectly waitlisted

## Key Technical Discoveries

### 1. Record Type Limitations
C# records with positional parameters don't work with DataAnnotations validation:
- Validation attributes on constructor parameters are ignored
- Must use traditional properties with init accessors
- This is a known C# limitation, not a bug

### 2. PostgreSQL Requirements
- All DateTime values must be UTC (`DateTimeKind.Utc`)
- Unique constraints are strictly enforced (unlike in-memory DB)
- LINQ queries have different translation limitations
- Owned entities have stricter tracking requirements

### 3. Concurrency Architecture Issues
The failing tests reveal real architectural problems:
- **Capacity checks aren't atomic** - Done in app code, not database
- **No transaction boundaries** - Operations can interleave
- **Optimistic concurrency works too well** - Both updates can fail
- **Entity tracking complexity** - Owned entities cause issues

## Recommended Solutions

### 1. Database-Level Constraints (High Priority)
```sql
-- Example: Enforce capacity at database level
CREATE OR REPLACE FUNCTION check_event_capacity()
RETURNS TRIGGER AS $$
BEGIN
    IF (SELECT COUNT(*) FROM Tickets 
        WHERE EventId = NEW.EventId 
        AND Status = 'Confirmed') >= 
       (SELECT Capacity FROM Events WHERE Id = NEW.EventId) THEN
        RAISE EXCEPTION 'Event is at full capacity';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

### 2. Retry Pattern for Concurrency
```csharp
public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateConcurrencyException) when (i < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100));
        }
    }
    throw new Exception("Max retries exceeded");
}
```

### 3. Proper Entity Tracking
```csharp
// Instead of creating detached entities:
var payment = new Payment(...);
ticket.Confirm(payment);

// Use proper tracking:
_context.Payments.Add(payment);
payment.Ticket = ticket;
ticket.Payment = payment;
await _context.SaveChangesAsync();
```

## Test Patterns to Remember

### 1. Always Use Unique Test Data
```csharp
// Bad:
var user = CreateUser(sceneName: "TestUser");

// Good:
var user = CreateUser(sceneName: $"TestUser_{Guid.NewGuid():N}");
```

### 2. Use UTC for All DateTimes
```csharp
// Bad:
var date = new DateTime(2025, 1, 1);

// Good:
var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
```

### 3. Handle Expected Concurrency
```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // This is expected in concurrent scenarios
    // Not necessarily a failure
}
```

## Running Tests

```bash
# All API tests
dotnet test tests/WitchCityRope.Api.Tests/

# Just concurrency tests
dotnet test tests/WitchCityRope.Api.Tests/ --filter "FullyQualifiedName~ConcurrencyAndEdgeCaseTests"

# With detailed output
dotnet test tests/WitchCityRope.Api.Tests/ -v detailed
```

## Files Modified
- `/src/WitchCityRope.Api/Features/Events/Models/CreateEventRequest.cs`
- `/src/WitchCityRope.Api/Features/Events/Services/EventService.cs`
- `/src/WitchCityRope.Core/Entities/Ticket.cs`
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventConfiguration.cs`
- `/tests/WitchCityRope.Api.Tests/Services/ConcurrencyAndEdgeCaseTests.cs`
- Plus many test files for unique data generation

## Next Engineer Action Items
1. Read `/docs/TEST_FIXES_HANDOFF.md` for detailed guidance
2. Focus on database-level capacity constraints first
3. Consider if 6 failing tests represent acceptable edge cases
4. Evaluate if architectural changes are worth the effort