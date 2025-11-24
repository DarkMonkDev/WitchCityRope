# CSRF Protection - Developer Quick Reference
<!-- Last Updated: 2025-11-23 -->
<!-- Version: 1.0 -->
<!-- Owner: Development Teams -->
<!-- Status: Active -->

## Quick Start

This guide provides **quick reference patterns** for developers working with CSRF protection. For comprehensive details, see [Implementation Guide](./implementation-guide.md).

---

## Table of Contents

1. [For Backend Developers](#for-backend-developers)
2. [For Frontend Developers](#for-frontend-developers)
3. [For Test Developers](#for-test-developers)
4. [Common Mistakes](#common-mistakes)
5. [Quick Troubleshooting](#quick-troubleshooting)

---

## For Backend Developers

### Adding CSRF to New Endpoints

#### Protected Endpoint Pattern (Default)

**Use for**: Any POST/PUT/DELETE/PATCH endpoint requiring authentication

```csharp
app.MapPost("/api/admin/events", async (
    CreateEventRequest request,
    EventService eventService,
    IAntiforgery antiforgery,  // ← Inject IAntiforgery
    HttpContext context) =>
{
    // CSRF validation FIRST (throws on failure)
    await antiforgery.ValidateRequestAsync(context);

    // Business logic executes ONLY if CSRF valid
    var eventId = await eventService.CreateEventAsync(request);
    return Results.Created($"/api/events/{eventId}", eventId);
})
.RequireAuthorization(policy => policy.RequireRole("Administrator"));
```

**Key Points**:
- Inject `IAntiforgery antiforgery` and `HttpContext context`
- Call `await antiforgery.ValidateRequestAsync(context)` **FIRST**
- Method throws `AntiforgeryValidationException` on failure (handled by middleware)
- Returns 400 "CSRF Validation Failed" automatically

#### Public Endpoint Pattern (Disabled CSRF)

**Use for**: Anonymous public forms, external webhooks

```csharp
app.MapPost("/api/vetting/public/applications", async (
    VettingApplicationRequest request,
    VettingService vettingService) =>
{
    // No CSRF validation needed for public endpoint
    var applicationId = await vettingService.SubmitPublicApplicationAsync(request);
    return Results.Created($"/api/vetting/applications/{applicationId}", applicationId);
})
.AllowAnonymous()
.DisableAntiforgery(); // ← Explicitly disable CSRF
```

**When to Disable**:
- ✅ Public forms (vetting applications, incident reports)
- ✅ Anonymous endpoints (no authentication available)
- ✅ External webhooks (PayPal, SendGrid cannot send CSRF tokens)
- ❌ **NEVER disable for authenticated endpoints** (security risk)

### Endpoint Checklist

When creating a new endpoint, check:

- [ ] Is this POST/PUT/DELETE/PATCH? (state-changing)
- [ ] Does it require authentication? (Bearer token)
- [ ] If YES to both → Add CSRF validation (inject IAntiforgery + validate)
- [ ] If anonymous/public → Use `.DisableAntiforgery()`
- [ ] If GET request → No CSRF needed (read-only)

### Common Mistakes

#### ❌ WRONG: No CSRF Validation

```csharp
// DANGER: Vulnerable to CSRF attacks!
app.MapPost("/api/admin/users/{userId}/roles", async (
    string userId,
    UpdateRolesRequest request,
    UserService userService) =>
{
    // No CSRF validation - CSRF attack possible!
    await userService.UpdateUserRolesAsync(userId, request);
    return Results.Ok();
})
.RequireAuthorization();
```

**Why Wrong**: Authenticated endpoint without CSRF allows privilege escalation attacks.

#### ❌ WRONG: Using Non-Existent Method

```csharp
// COMPILATION ERROR: .RequireAntiforgery() does NOT exist in .NET 9!
app.MapPost("/api/events", async (...) => { ... })
    .RequireAuthorization()
    .RequireAntiforgery(); // ← Method doesn't exist!
```

**Why Wrong**: `.RequireAntiforgery()` is not a valid method in .NET 9 Minimal APIs.

#### ✅ CORRECT: Manual Validation

```csharp
app.MapPost("/api/admin/users/{userId}/roles", async (
    string userId,
    UpdateRolesRequest request,
    UserService userService,
    IAntiforgery antiforgery,
    HttpContext context) =>
{
    // CSRF validation protects against privilege escalation
    await antiforgery.ValidateRequestAsync(context);

    await userService.UpdateUserRolesAsync(userId, request);
    return Results.Ok();
})
.RequireAuthorization(policy => policy.RequireRole("Administrator"));
```

---

## For Frontend Developers

### CSRF is Automatic!

**Good news**: Frontend developers **don't manually handle CSRF tokens**. Everything is automatic.

### What Happens Automatically

1. **After login** → `initializeCSRFProtection()` called automatically
2. **CSRF cookies set** → `.AspNetCore.Antiforgery` + `XSRF-TOKEN`
3. **API requests** → Axios interceptor adds `X-CSRF-TOKEN` header automatically
4. **You do nothing** → Just make normal API calls!

### Example: Normal Form Submission

```typescript
// You write this (no CSRF handling needed):
const handleSubmit = async (values: EventFormValues) => {
  try {
    const response = await apiClient.post('/api/admin/events', values);
    notifications.show({ message: 'Event created!', color: 'green' });
  } catch (error) {
    notifications.show({ message: 'Failed to create event', color: 'red' });
  }
};
```

**What happens behind the scenes**:
1. Axios interceptor reads `XSRF-TOKEN` cookie
2. Adds `X-CSRF-TOKEN: <token>` header automatically
3. Request sent with both Bearer token AND CSRF token
4. Backend validates both tokens
5. Request succeeds or fails based on validation

### When You Might See CSRF Errors

**Symptom**: API returns 400 "CSRF Validation Failed"

**Likely Causes**:
1. User not logged in (CSRF requires authentication)
2. CSRF token expired (user must re-login)
3. Cookie blocked by browser (check cookie settings)

**Solution**: Usually resolved by:
- Redirect to login page (AuthContext handles this)
- Let user re-authenticate (gets new CSRF token automatically)

### Manual CSRF Initialization (Rare)

**Only needed if**: You're implementing custom authentication flow

```typescript
import { initializeCSRFProtection } from '@/hooks/useCSRFToken';

// After custom login success:
const handleCustomLogin = async () => {
  // Your custom login logic
  await customLoginMethod();

  // Initialize CSRF protection
  await initializeCSRFProtection();
};
```

**Normal case**: `useLogin` hook already calls `initializeCSRFProtection()`, so you don't need this.

---

## For Test Developers

### Integration Tests (Backend)

#### Pattern 1: Use Convenience Method (Recommended)

```csharp
[Fact]
public async Task CreateEvent_AsAdmin_Succeeds()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

    // One-line client creation with CSRF
    var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

    var newEvent = new CreateEventRequest { Title = "Test Event" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/events", newEvent);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

#### Pattern 2: Update Existing Helper (Custom Test Helpers)

```csharp
// Before (without CSRF):
private HttpClient CreateHttpClient(string? bearerToken = null)
{
    var client = _factory.CreateClient();
    if (!string.IsNullOrEmpty(bearerToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);
    }
    return client;
}

// After (with CSRF):
private async Task<HttpClient> CreateHttpClientAsync(string? bearerToken = null)
{
    var client = _factory.CreateClient();
    if (!string.IsNullOrEmpty(bearerToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);

        // Fetch and add CSRF token
        var csrfToken = await FetchCsrfTokenAsync(client);
        AddCsrfTokenHeader(client, csrfToken);
    }
    return client;
}

// Update all test method calls:
var client = await CreateHttpClientAsync(token); // Now async
```

**Important**: Helper methods in `IntegrationTestBase` already exist:
- `FetchCsrfTokenAsync(client)` - Gets CSRF token from API
- `AddCsrfTokenHeader(client, token)` - Adds token to headers
- `CreateAuthenticatedClientWithCsrfAsync(factory, token)` - One-stop method

### Frontend Tests (React)

CSRF hooks are already tested (20 passing tests). For component tests:

```typescript
// Mock the CSRF token cookie
import Cookies from 'js-cookie';

beforeEach(() => {
  // Set mock CSRF token
  Cookies.set('XSRF-TOKEN', 'mock-csrf-token-123');
});

afterEach(() => {
  // Clean up
  Cookies.remove('XSRF-TOKEN');
});

test('form submission includes CSRF token', async () => {
  // Your component test
  // API interceptor will automatically add X-CSRF-TOKEN header
});
```

### Testing Public Endpoints

Public endpoints (with `.DisableAntiforgery()`) should **NOT** require CSRF tokens:

```csharp
[Fact]
public async Task SubmitPublicApplication_WithoutCsrf_Succeeds()
{
    // Arrange
    var client = _factory.CreateClient(); // No auth, no CSRF
    var application = new VettingApplicationRequest { /* ... */ };

    // Act
    var response = await client.PostAsJsonAsync(
        "/api/vetting/public/applications",
        application
    );

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

---

## Common Mistakes

### Backend

#### Mistake 1: Forgetting CSRF Validation

```csharp
// ❌ WRONG: No CSRF validation
app.MapPost("/api/admin/settings", async (SettingsRequest request) =>
{
    // Vulnerable to CSRF attacks!
    await settingsService.UpdateAsync(request);
    return Results.Ok();
});
```

**Fix**: Add CSRF validation:
```csharp
// ✅ CORRECT
app.MapPost("/api/admin/settings", async (
    SettingsRequest request,
    IAntiforgery antiforgery,
    HttpContext context) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await settingsService.UpdateAsync(request);
    return Results.Ok();
});
```

#### Mistake 2: Using `.DisableAntiforgery()` on Authenticated Endpoints

```csharp
// ❌ WRONG: Disabling CSRF on authenticated endpoint
app.MapPost("/api/admin/users/{id}/roles", async (...) => { ... })
    .RequireAuthorization()
    .DisableAntiforgery(); // ← SECURITY RISK!
```

**Why Wrong**: Allows CSRF attacks on privileged operations.

**Fix**: Remove `.DisableAntiforgery()` and add validation:
```csharp
// ✅ CORRECT
app.MapPost("/api/admin/users/{id}/roles", async (
    string id,
    UpdateRolesRequest request,
    IAntiforgery antiforgery,
    HttpContext context) =>
{
    await antiforgery.ValidateRequestAsync(context);
    // ...
})
.RequireAuthorization();
```

### Frontend

#### Mistake 1: Manually Adding CSRF Header

```typescript
// ❌ WRONG: Manually adding CSRF token (unnecessary)
const response = await apiClient.post('/api/events', eventData, {
  headers: {
    'X-CSRF-TOKEN': Cookies.get('XSRF-TOKEN'), // ← Don't do this!
  },
});
```

**Why Wrong**: Axios interceptor already adds this header automatically.

**Fix**: Just make normal API call:
```typescript
// ✅ CORRECT: Let interceptor handle CSRF
const response = await apiClient.post('/api/events', eventData);
```

#### Mistake 2: Not Handling CSRF Initialization Errors

```typescript
// ❌ WRONG: Ignoring CSRF initialization failure
onSuccess: async (data) => {
  useAuthStore.getState().login(data.user);
  await initializeCSRFProtection(); // Error silently ignored
}
```

**Fix**: Handle errors properly:
```typescript
// ✅ CORRECT: Log errors but don't block login
onSuccess: async (data) => {
  useAuthStore.getState().login(data.user);
  try {
    await initializeCSRFProtection();
  } catch (error) {
    console.error('CSRF initialization failed:', error);
    // Login still succeeds, user can try operations
    // (may fail with CSRF error, but won't block login)
  }
}
```

### Testing

#### Mistake 1: Not Awaiting Async Helper

```csharp
// ❌ WRONG: Forgetting to await async helper
var client = CreateHttpClientAsync(token); // Missing await!
```

**Fix**:
```csharp
// ✅ CORRECT: Await async helper
var client = await CreateHttpClientAsync(token);
```

#### Mistake 2: Expecting CSRF on GET Requests

```csharp
// ❌ WRONG: Adding CSRF to GET request (unnecessary)
var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);
var response = await client.GetAsync("/api/events"); // GET doesn't need CSRF
```

**Why Wrong**: CSRF only needed for POST/PUT/DELETE/PATCH (state-changing requests).

**Fix**: Use simple authenticated client for GET:
```csharp
// ✅ CORRECT: GET requests don't need CSRF
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
var response = await client.GetAsync("/api/events");
```

---

## Quick Troubleshooting

### Backend Issues

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| 400 "CSRF Validation Failed" | Token missing or invalid | Check frontend sending `X-CSRF-TOKEN` header |
| 500 Internal Server Error | Missing `IAntiforgery` injection | Add `IAntiforgery antiforgery, HttpContext context` parameters |
| Compilation error: `.RequireAntiforgery()` not found | Using non-existent method | Use manual validation: `await antiforgery.ValidateRequestAsync(context)` |
| Public endpoint returns 400 | CSRF enabled on anonymous endpoint | Add `.DisableAntiforgery()` to endpoint |

### Frontend Issues

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| All requests return 400 | CSRF token not initialized | Check `initializeCSRFProtection()` called after login |
| CSRF token undefined | Not logged in | Verify user authentication before state-changing requests |
| Token exists but validation fails | Cookie/header mismatch | Check browser cookies (DevTools → Application → Cookies) |
| Public form returns 400 | Backend missing `.DisableAntiforgery()` | Ask backend to disable CSRF for that endpoint |

### Test Issues

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Tests fail with 400 | Not fetching CSRF token | Use `CreateAuthenticatedClientWithCsrfAsync()` helper |
| "XSRF-TOKEN not found" | Token endpoint not working | Verify `/api/antiforgery/token` returns 200 OK |
| Integration tests pass, real app fails | Missing frontend integration | Check `initializeCSRFProtection()` in login flow |
| Public endpoint test fails | Test trying to add CSRF | Don't fetch/add CSRF for public endpoints |

---

## Reference

### Key Files

**Backend**:
- Configuration: `/apps/api/Program.cs` (lines ~40-50, ~120-140)
- Token endpoint: `/apps/api/Program.cs` (lines ~180-200)
- Protected endpoints: `/apps/api/Features/*/Endpoints/*.cs` (38 files)

**Frontend**:
- CSRF hook: `/apps/web/src/hooks/useCSRFToken.ts`
- Login integration: `/apps/web/src/features/auth/api/mutations.ts`
- API interceptor: `/apps/web/src/api/client.ts`

**Tests**:
- Integration test base: `/tests/integration/IntegrationTestBase.cs`
- Frontend tests: `/apps/web/src/hooks/__tests__/useCSRFToken.test.tsx`

### Documentation

- **Implementation Guide**: [./implementation-guide.md](./implementation-guide.md) - Complete architecture and details
- **Testing Guide**: [./testing-guide.md](./testing-guide.md) - Comprehensive testing instructions
- **Integration Test Guide**: `/tests/integration/CSRF_INTEGRATION_TEST_GUIDE.md` - Backend test patterns
- **Technology Research**: `/docs/functional-areas/security/research/2025-11-23-dotnet9-antiforgery-json-api-research.md`

### Quick Commands

```bash
# Check if middleware enabled
grep -n "UseAntiforgery" apps/api/Program.cs

# Find all protected endpoints
grep -rn "ValidateRequestAsync" apps/api/Features/

# Find public endpoints (CSRF disabled)
grep -rn "DisableAntiforgery" apps/api/Features/

# Run CSRF integration tests
dotnet test --filter "CsrfTokenIntegrationTests"

# Run frontend CSRF tests
npm test useCSRFToken
```

---

## Need Help?

1. **Check Implementation Guide**: [./implementation-guide.md](./implementation-guide.md)
2. **Check Testing Guide**: [./testing-guide.md](./testing-guide.md)
3. **Review Lessons Learned**: `/docs/lessons-learned/backend-developer-lessons-learned-4.md` (lines 1068-1213)
4. **Check Technology Research**: `/docs/functional-areas/security/research/2025-11-23-dotnet9-antiforgery-json-api-research.md`

---

**Quick Reference Version**: 1.0
**Last Updated**: 2025-11-23
**For Questions**: Review implementation guide or consult backend-developer lessons learned
