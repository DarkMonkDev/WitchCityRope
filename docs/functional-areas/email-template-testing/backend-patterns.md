# Backend Implementation Patterns - MUST FOLLOW

**Created**: 2026-03-08
**Purpose**: Exact patterns the backend-developer agent MUST follow when implementing email template test send endpoints.

## CRITICAL: Read design-spec.md First

Read `/home/chad/repos/witchcityrope/docs/functional-areas/email-template-testing/design-spec.md` BEFORE implementing anything. That document defines WHAT to build. This document defines HOW to build it.

---

## Pattern 1: Private Static Handler Methods (NOT Inline Lambdas)

The endpoint file (`EmailTemplateEndpoints.cs`) uses private static handler methods, NOT inline lambdas.

**WRONG** (do NOT do this):
```csharp
group.MapGet("/test-data", async (ISettingsService settingsService, CancellationToken ct) =>
{
    // inline lambda - WRONG PATTERN
});
```

**CORRECT** (follow this pattern):
```csharp
// In MapEmailTemplateEndpoints method - just the route mapping:
group.MapGet("/test-data", GetEmailTestData)
    .WithName("GetEmailTestData")
    .WithSummary("Get all email template test data")
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

// Separate private static handler method at the bottom of the class:
private static async Task<IResult> GetEmailTestData(
    ISettingsService settingsService,
    CancellationToken cancellationToken)
{
    // implementation here
}
```

## Pattern 2: CSRF Validation for Mutating Endpoints (PUT, POST, DELETE)

ALL PUT, POST, DELETE endpoints MUST include CSRF validation. Copy the exact pattern from existing handlers:

```csharp
private static async Task<IResult> SaveEmailTestData(
    HttpContext context,
    IAntiforgery antiforgery,
    [FromBody] Dictionary<string, string> testData,
    ISettingsService settingsService,
    CancellationToken cancellationToken)
{
    // CSRF validation - MUST be first
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.Problem(
            title: "CSRF Validation Failed",
            detail: "Antiforgery token validation failed. Please refresh the page and try again.",
            statusCode: 400);
    }

    // ... rest of handler
}
```

## Pattern 3: Route Mapping Conventions

Use the same pattern as existing routes in the file:

```csharp
group.MapGet("/test-data", GetEmailTestData)
    .WithName("GetEmailTestData")
    .WithSummary("Get all email template test data variable values")
    .WithDescription("Returns saved default values for all email template variables, used for test sends")
    .Produces<Dictionary<string, string>>(200)
    .ProducesProblem(401)
    .ProducesProblem(403)
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });
```

Note: Use `new AuthorizeAttribute { Roles = "Administrator" }` for admin-only (NOT the policy lambda pattern used in SettingsEndpoints.cs).

## Pattern 4: Using Statements Required

The endpoints file already has most needed usings. You may need to add:
```csharp
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Shared.Services;
```

Do NOT add unnecessary usings. Check what's already imported.

## Pattern 5: Where to Place New Code

1. **Route mappings**: Add at the end of `MapEmailTemplateEndpoints()`, before the closing brace of the method. Add a clear section header comment like existing sections:
   ```csharp
   // ========================================
   // Email Template Testing (Admin-only)
   // ========================================
   ```

2. **Handler methods**: Add at the very bottom of the class, after `ScheduleAdHocEmail` handler.

## Pattern 6: SendTestEmail Handler - Complete Implementation

The send-test handler needs:
1. CSRF validation (HttpContext + IAntiforgery)
2. Guid parsing of the template ID
3. Template lookup from ApplicationDbContext
4. Test data loading from ISettingsService
5. Merge with overrides
6. Auto-save overrides via ISettingsService.UpsertMultipleSettingsAsync
7. Variable substitution via EmailService.SubstituteVariables (internal static)
8. Email sending via IEmailService.SendEmailAsync
9. Logging via ILogger

The handler parameter list:
```csharp
private static async Task<IResult> SendTestEmail(
    HttpContext context,
    IAntiforgery antiforgery,
    Guid id,
    [FromBody] SendTestEmailRequest request,
    ApplicationDbContext dbContext,
    ISettingsService settingsService,
    IEmailService emailService,
    ILogger<EmailTemplateEndpoints> logger,
    CancellationToken cancellationToken)
```

Note: Use `ILogger<EmailTemplateEndpoints>` (not `ILogger<Program>`) — the class itself is static so we parameterize it.

## Pattern 7: SettingsSeeder Integration

The `SettingsSeeder` currently has `SeedSettingsAsync()` which has an early-return if any settings exist. The email test data seeding should be a SEPARATE method (`SeedEmailTestDataAsync`) that checks specifically for `EmailTestData:` prefixed settings.

This new method should be called from `SeedCoordinator.SeedAllDataAsync()` AFTER the `_settingsSeeder.SeedSettingsAsync()` call:

```csharp
_logger.LogDebug("Seeding settings...");
await _settingsSeeder.SeedSettingsAsync(cancellationToken);

_logger.LogDebug("Seeding email test data defaults...");
await _settingsSeeder.SeedEmailTestDataAsync(cancellationToken);
```

## Pattern 8: Result Type

The `IEmailService.SendEmailAsync()` returns `Result` (from `WitchCityRope.Api.Features.Shared.Models`). Check its properties:
- `result.IsSuccess` - boolean
- `result.Error` - string error message when not successful
