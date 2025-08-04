# Compilation Fixes TODO

## Quick Fixes (Can be done in batch)

### 1. Infrastructure.Tests - PayPal Service (3 errors)
```csharp
// PayPalServiceTests.cs - Lines 75, 105
// CHANGE: CreatePaymentAsync(ticket) 
// TO: CreatePaymentAsync(registration)

// PayPalServiceTests.cs - Line 211
// REMOVE: TicketId = ticketId
// OR CHANGE TO: RegistrationId = registrationId
```

### 2. Api.Tests - Auth Types (4 errors)
```csharp
// Add missing using or create type:
using WitchCityRope.Core.DTOs.Auth; // or wherever UserWithAuth is

// Create JwtToken type if missing:
public record JwtToken(string Token, DateTime Expiry);

// Fix MockTokenService to implement:
public Task<string> GenerateToken(UserWithAuth user)
```

### 3. Web.Tests - EventDto Properties (7 errors)
```csharp
// TestDataBuilder.cs - Update EventDto creation:
// REMOVE these properties or add them to EventDto:
- Title
- StartDate  
- EndDate
- InstructorName
- RegistrationStatus
- TicketId

// Fix EventType conversion:
EventType = eventType.ToString() // not just eventType
```

### 4. E2E.Tests - Playwright Method Calls (14 errors)
```csharp
// All Playwright tests - Fix string comparison calls:
// CHANGE: await page.WaitForURLAsync(url, StringComparison.OrdinalIgnoreCase)
// TO: await page.WaitForURLAsync(url)

// CHANGE: await Expect(Locator).ToHaveTextAsync(new[] { "text" }, StringComparison.OrdinalIgnoreCase)
// TO: await Expect(Locator).ToHaveTextAsync("text")
```

### 5. E2E.Tests - WitchCityRopeUser Properties (10 errors)
```csharp
// Option A: Use constructor or factory method
var user = new WitchCityRopeUser(
    sceneNameValue: "TestScene",
    encryptedLegalName: "encrypted",
    emailAddress: "test@example.com",
    // ... other properties
);

// Option B: Make properties settable in WitchCityRopeUser class
public string SceneNameValue { get; set; } // Add set accessor
```

### 6. Web.Tests - Missing Services (4 errors)
```csharp
// Add interfaces or mock them:
services.AddScoped<INotificationService, MockNotificationService>();
services.AddScoped<IUserService, MockUserService>();

// Or create mocks:
var notificationService = new Mock<INotificationService>();
var userService = new Mock<IUserService>();
```

### 7. Other Quick Fixes
```csharp
// DatabaseFixture.cs - Fix Respawn tables:
TablesToIgnore = new Table[] 
{
    new Table("__EFMigrationsHistory"),
    new Table("AspNetRoles"),
    // ...
}

// EventCardComponentTests.cs - Add using:
using Microsoft.AspNetCore.Components;

// MainLayoutTests.cs - Fix timer call:
await InvokeAsync(() => { /* code */ });

// TestDataManager.cs - Fix Price:
// Use ticket.Price instead of event.Price
```

## Batch Fix Commands

### Fix all StringComparison issues:
```bash
# Find and remove StringComparison arguments from Playwright tests
find tests/WitchCityRope.E2E.Tests -name "*.cs" -exec sed -i 's/, StringComparison\.[A-Za-z]*//g' {} \;
```

### Fix all read-only property assignments:
```bash
# This needs manual review - properties need to be made settable or use constructors
```

## Priority Order
1. Infrastructure.Tests (3 errors) - 5 minutes
2. Api.Tests (4 errors) - 10 minutes  
3. EventDto properties (7 errors) - 15 minutes
4. Playwright StringComparison (14 errors) - 10 minutes with script
5. WitchCityRopeUser properties (10 errors) - 20 minutes
6. Remaining issues - 20 minutes

**Total estimated time: 1-2 hours**