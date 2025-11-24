# CSRF Integration Testing Guide

## Overview
This guide explains how to update integration tests to support CSRF protection that has been implemented across ~38 POST/PUT/DELETE/PATCH endpoints in the API.

## CSRF Protection Implementation

### Backend (API)
- **Token Generation Endpoint**: `/api/antiforgery/token` (requires authentication)
  - Returns two cookies: `.AspNetCore.Antiforgery` (httpOnly) + `XSRF-TOKEN` (readable)
- **Protected Endpoints**: All POST/PUT/DELETE/PATCH endpoints validate CSRF tokens
  - Validation code: `await antiforgery.ValidateRequestAsync(context);`
  - Returns 400 "CSRF Validation Failed" if token missing or invalid

### Frontend (React)
- Fetches token after login via `/api/antiforgery/token`
- Includes token in `X-CSRF-TOKEN` header for all state-changing requests

## Integration Test Infrastructure Updates

### Base Class: IntegrationTestBase.cs
Location: `/tests/integration/IntegrationTestBase.cs`

Three new helper methods have been added:

#### 1. FetchCsrfTokenAsync
```csharp
protected async Task<string> FetchCsrfTokenAsync(HttpClient client)
```
- Fetches CSRF token from `/api/antiforgery/token`
- Extracts `XSRF-TOKEN` cookie value
- **Must be called after setting Bearer token** (endpoint requires authentication)
- Returns token string for use in requests

#### 2. AddCsrfTokenHeader
```csharp
protected void AddCsrfTokenHeader(HttpClient client, string csrfToken)
```
- Adds `X-CSRF-TOKEN` header to HttpClient
- Call after fetching token, before making state-changing requests

#### 3. CreateAuthenticatedClientWithCsrfAsync (Convenience Method)
```csharp
protected async Task<HttpClient> CreateAuthenticatedClientWithCsrfAsync(
    WebApplicationFactory<Program> factory,
    string bearerToken)
```
- One-stop method that:
  1. Creates HttpClient from factory
  2. Sets Bearer token for authentication
  3. Fetches CSRF token
  4. Adds CSRF token to headers
- Returns ready-to-use HttpClient

## How to Update Existing Tests

### Pattern 1: Manual Token Management (Existing Tests)

**BEFORE (without CSRF):**
```csharp
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

[Fact]
public async Task CreateVenue_AsAdmin_Succeeds()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
    var client = CreateHttpClient(token);

    var newVenue = new CreateVenueRequest { Name = "Test Venue" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

**AFTER (with CSRF):**
```csharp
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

[Fact]
public async Task CreateVenue_AsAdmin_Succeeds()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
    var client = await CreateHttpClientAsync(token); // Now async

    var newVenue = new CreateVenueRequest { Name = "Test Venue" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Pattern 2: Using Convenience Method (Recommended for New Tests)

```csharp
[Fact]
public async Task CreateVenue_AsAdmin_Succeeds()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

    // One-line client creation with CSRF
    var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

    var newVenue = new CreateVenueRequest { Name = "Test Venue" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Pattern 3: Tests with Helper Methods Returning (HttpClient, Guid)

**BEFORE:**
```csharp
private async Task<(HttpClient client, Guid userId)> CreateAdminUserAsync(string email)
{
    var userId = Guid.NewGuid();
    // ... create user in database ...

    var client = _factory.CreateClient();
    var token = GenerateJwtToken(userId, email, "Administrator");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    return (client, userId);
}
```

**AFTER:**
```csharp
private async Task<(HttpClient client, Guid userId)> CreateAdminUserAsync(string email)
{
    var userId = Guid.NewGuid();
    // ... create user in database ...

    var token = GenerateJwtToken(userId, email, "Administrator");
    var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

    return (client, userId);
}
```

## Important Notes

### 1. CSRF Token Requires Authentication
The `/api/antiforgery/token` endpoint requires authentication. You MUST:
1. Set Bearer token first
2. Then fetch CSRF token
3. Then make state-changing requests

### 2. Cookie Handling
WebApplicationFactory's default HttpClient handles cookies automatically across requests within the same client instance. The `.AspNetCore.Antiforgery` cookie is preserved automatically.

### 3. Only State-Changing Requests Need CSRF
- **GET requests**: Do NOT need CSRF tokens
- **POST/PUT/DELETE/PATCH requests**: DO need CSRF tokens

### 4. Tests Without Authentication
Tests that verify 401 Unauthorized responses (no Bearer token) should expect CSRF validation to fail as well, since the token endpoint requires authentication.

Example:
```csharp
[Fact]
public async Task CreateVenue_WithoutAuthentication_Returns401()
{
    // Arrange
    var client = _factory.CreateClient(); // No token, no CSRF
    var newVenue = new CreateVenueRequest { Name = "Test" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

    // Assert - Will likely be 401 from auth check, not 400 from CSRF
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Checklist for Updating Tests

For each test file with POST/PUT/DELETE/PATCH requests:

- [ ] Identify all test methods making state-changing requests
- [ ] Update `CreateHttpClient` helper to be async and fetch CSRF token
- [ ] OR use `CreateAuthenticatedClientWithCsrfAsync` convenience method
- [ ] Update test methods to `await` the client creation (now async)
- [ ] Run tests to verify they pass with CSRF protection
- [ ] Check for any 400 "CSRF Validation Failed" errors

## Files to Update

Priority order (files with most state-changing requests):

1. `/tests/integration/api/Endpoints/VenueEndpointsIntegrationTests.cs`
2. `/tests/integration/api/Features/EmailTemplates/EmailTemplateEndpointsIntegrationTests.cs`
3. `/tests/integration/api/Features/Participation/*.cs`
4. `/tests/integration/api/Features/Vetting/*.cs`
5. `/tests/integration/Features/Payments/ProcessVariableRefundIntegrationTests.cs`
6. `/tests/integration/Features/Volunteers/VolunteerTimingTests.cs`
7. `/tests/integration/Features/Attendance/TicketTimingTests.cs`
8. `/tests/integration/Features/Attendance/RsvpTimingTests.cs`

## Testing the CSRF Infrastructure

To verify CSRF token handling works:

```csharp
[Fact]
public async Task CsrfTokenFetch_WithAuthentication_ReturnsValidToken()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var csrfToken = await FetchCsrfTokenAsync(client);

    // Assert
    csrfToken.Should().NotBeNullOrEmpty();
    csrfToken.Length.Should().BeGreaterThan(10); // CSRF tokens are reasonably long
}

[Fact]
public async Task PostRequest_WithoutCsrfToken_Returns400()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    // Intentionally NOT fetching CSRF token

    var newVenue = new CreateVenueRequest { Name = "Test" };

    // Act
    var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("CSRF");
}
```

## Troubleshooting

### Error: "No Set-Cookie headers found in CSRF token response"
- **Cause**: Authentication failed or token endpoint not working
- **Fix**: Verify Bearer token is set before calling `FetchCsrfTokenAsync`

### Error: "XSRF-TOKEN cookie not found"
- **Cause**: Antiforgery middleware not configured or endpoint implementation issue
- **Fix**: Check API Program.cs for antiforgery services registration

### Tests pass without CSRF token
- **Cause**: CSRF validation not yet added to that specific endpoint
- **Fix**: This is expected for endpoints not yet protected. Tests are future-proof.

### 400 BadRequest with "CSRF Validation Failed"
- **Success!**: This means CSRF is working. Ensure test is calling `FetchCsrfTokenAsync`

## Next Steps

1. Update VenueEndpointsIntegrationTests.cs as a reference implementation
2. Use that pattern to update other test files
3. Run full integration test suite to verify
4. Document any endpoint-specific CSRF edge cases discovered
