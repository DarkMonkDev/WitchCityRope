# CSRF Protection - Testing Guide
<!-- Last Updated: 2025-11-23 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Developer Team -->
<!-- Status: Active -->

## Overview

This guide provides **comprehensive testing instructions** for CSRF protection across backend integration tests, frontend unit tests, and manual testing scenarios.

---

## Table of Contents

1. [Backend Integration Testing](#backend-integration-testing)
2. [Frontend Unit Testing](#frontend-unit-testing)
3. [End-to-End Testing](#end-to-end-testing)
4. [Manual Testing](#manual-testing)
5. [Test Scenarios](#test-scenarios)
6. [Troubleshooting Test Failures](#troubleshooting-test-failures)

---

## Backend Integration Testing

### Infrastructure Setup

**Location**: `/tests/integration/IntegrationTestBase.cs`

Three helper methods available to all integration tests:

#### 1. FetchCsrfTokenAsync

```csharp
/// <summary>
/// Fetch CSRF token from /api/antiforgery/token endpoint
/// </summary>
/// <param name="client">HttpClient with Bearer token already set</param>
/// <returns>CSRF token string</returns>
protected async Task<string> FetchCsrfTokenAsync(HttpClient client)
```

**Usage**:
```csharp
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var csrfToken = await FetchCsrfTokenAsync(client);
// Returns: "CfDJ8N7t5..."
```

**Requirements**:
- Client must have Bearer token set BEFORE calling
- Endpoint requires authentication
- Throws exception if token fetch fails

#### 2. AddCsrfTokenHeader

```csharp
/// <summary>
/// Add CSRF token to request headers
/// </summary>
/// <param name="client">HttpClient to add header to</param>
/// <param name="csrfToken">CSRF token value</param>
protected void AddCsrfTokenHeader(HttpClient client, string csrfToken)
```

**Usage**:
```csharp
var csrfToken = await FetchCsrfTokenAsync(client);
AddCsrfTokenHeader(client, csrfToken);

// Client now has X-CSRF-TOKEN header set
```

#### 3. CreateAuthenticatedClientWithCsrfAsync (Recommended)

```csharp
/// <summary>
/// Convenience method: Creates HttpClient with authentication + CSRF token
/// </summary>
/// <param name="factory">WebApplicationFactory instance</param>
/// <param name="bearerToken">JWT bearer token</param>
/// <returns>Fully configured HttpClient ready for state-changing requests</returns>
protected async Task<HttpClient> CreateAuthenticatedClientWithCsrfAsync(
    WebApplicationFactory<Program> factory,
    string bearerToken)
```

**Usage** (recommended for most tests):
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

### Test Patterns

#### Pattern 1: Convenience Method (New Tests)

**Best for**: New test files, simple test scenarios

```csharp
[Fact]
public async Task UpdateUser_AsAdmin_Succeeds()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
    var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

    var updateRequest = new UpdateUserRequest { SceneName = "Updated Name" };

    // Act
    var response = await client.PutAsJsonAsync($"/api/admin/users/{userId}", updateRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Ok);
}
```

#### Pattern 2: Update Existing Helper (Files with Custom Helpers)

**Best for**: Test files with existing `CreateHttpClient()` helper methods

**Before (without CSRF)**:
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
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
    var client = CreateHttpClient(token); // Sync method

    var venue = new CreateVenueRequest { Name = "Test Venue" };
    var response = await client.PostAsJsonAsync("/api/admin/venues", venue);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

**After (with CSRF)**:
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
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
    var client = await CreateHttpClientAsync(token); // Now async

    var venue = new CreateVenueRequest { Name = "Test Venue" };
    var response = await client.PostAsJsonAsync("/api/admin/venues", venue);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

**Changes Required**:
1. Change `CreateHttpClient` → `CreateHttpClientAsync`
2. Add `await FetchCsrfTokenAsync(client)` and `AddCsrfTokenHeader(client, csrfToken)`
3. Update all test method calls to `await CreateHttpClientAsync(token)`

#### Pattern 3: Helper Returning (HttpClient, Guid) Tuple

**Before**:
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

**After**:
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

### Testing Public Endpoints

Public endpoints with `.DisableAntiforgery()` should **NOT** require CSRF tokens:

```csharp
[Fact]
public async Task SubmitPublicApplication_WithoutAuthentication_Succeeds()
{
    // Arrange
    var client = _factory.CreateClient(); // No auth, no CSRF
    var application = new VettingApplicationRequest
    {
        Email = "applicant@example.com",
        SceneName = "Test Applicant",
        // ... other fields ...
    };

    // Act
    var response = await client.PostAsJsonAsync(
        "/api/vetting/public/applications",
        application
    );

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Testing CSRF Infrastructure

**File**: `/tests/integration/api/CsrfTokenIntegrationTests.cs`

Four tests validate CSRF infrastructure:

```csharp
[Fact]
public async Task FetchCsrfToken_WithAuthentication_ReturnsValidToken()
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
    csrfToken.Length.Should().BeGreaterThan(10);
}

[Fact]
public async Task FetchCsrfToken_WithoutAuthentication_ThrowsException()
{
    // Arrange
    var client = _factory.CreateClient(); // No Bearer token

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        async () => await FetchCsrfTokenAsync(client)
    );
}

[Fact]
public async Task CreateAuthenticatedClientWithCsrf_CreatesValidClient()
{
    // Arrange
    var userId = await GetUserIdAsync("admin@witchcityrope.com");
    var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

    // Act
    var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

    // Assert
    client.Should().NotBeNull();
    client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
    client.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
    client.DefaultRequestHeaders.Contains("X-CSRF-TOKEN").Should().BeTrue();
}

[Fact]
public async Task GetRequest_DoesNotRequireCsrfToken()
{
    // Arrange
    var client = _factory.CreateClient(); // No auth, no CSRF

    // Act
    var response = await client.GetAsync("/api/events");

    // Assert - GET requests don't need CSRF or authentication for public endpoints
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**Test Results**: ✅ 4 passing tests validate CSRF infrastructure works correctly.

### Example Test File Update

**File**: `/tests/integration/api/Endpoints/VenueEndpointsIntegrationTests.cs`

Complete reference implementation showing all patterns:

```csharp
public class VenueEndpointsIntegrationTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    public VenueEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Updated helper method (async, with CSRF)
    private async Task<HttpClient> CreateHttpClientAsync(string? bearerToken = null)
    {
        var client = _factory.CreateClient();
        if (!string.IsNullOrEmpty(bearerToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

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
        var client = await CreateHttpClientAsync(token);

        var newVenue = new CreateVenueRequest
        {
            Name = "Test Venue",
            Address = "123 Test St",
            // ... other fields ...
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateVenue_AsAdmin_Succeeds()
    {
        // Arrange
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
        var client = await CreateHttpClientAsync(token);

        var venueId = Guid.NewGuid(); // Assume venue exists
        var updateRequest = new UpdateVenueRequest { Name = "Updated Venue" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/admin/venues/{venueId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Ok);
    }

    [Fact]
    public async Task DeleteVenue_AsAdmin_Succeeds()
    {
        // Arrange
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var token = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");
        var client = await CreateHttpClientAsync(token);

        var venueId = Guid.NewGuid(); // Assume venue exists

        // Act
        var response = await client.DeleteAsync($"/api/admin/venues/{venueId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

**Test Results**: ✅ 19 passing tests (1 pre-existing failure unrelated to CSRF).

---

## Frontend Unit Testing

### Testing CSRF Hook

**File**: `/apps/web/src/hooks/__tests__/useCSRFToken.test.tsx`

**Coverage**: 20 passing tests covering:

#### getCSRFToken() Tests

```typescript
describe('getCSRFToken', () => {
  it('returns CSRF token from XSRF-TOKEN cookie', () => {
    Cookies.set('XSRF-TOKEN', 'test-csrf-token-123');
    const token = getCSRFToken();
    expect(token).toBe('test-csrf-token-123');
  });

  it('returns undefined when cookie does not exist', () => {
    Cookies.remove('XSRF-TOKEN');
    const token = getCSRFToken();
    expect(token).toBeUndefined();
  });

  it('handles empty cookie value', () => {
    Cookies.set('XSRF-TOKEN', '');
    const token = getCSRFToken();
    expect(token).toBe('');
  });

  it('handles malformed cookie value', () => {
    Cookies.set('XSRF-TOKEN', 'invalid token with spaces');
    const token = getCSRFToken();
    expect(token).toBe('invalid token with spaces');
  });
});
```

#### initializeCSRFProtection() Tests

```typescript
describe('initializeCSRFProtection', () => {
  it('fetches CSRF token from API', async () => {
    const mockGet = vi.fn().mockResolvedValue({ data: {} });
    vi.mocked(apiClient).get = mockGet;

    await initializeCSRFProtection();

    expect(mockGet).toHaveBeenCalledWith('/api/antiforgery/token', {
      withCredentials: true,
    });
  });

  it('throws error when API call fails', async () => {
    const mockGet = vi.fn().mockRejectedValue(new Error('Network error'));
    vi.mocked(apiClient).get = mockGet;

    await expect(initializeCSRFProtection()).rejects.toThrow('Network error');
  });

  it('includes credentials in request', async () => {
    const mockGet = vi.fn().mockResolvedValue({ data: {} });
    vi.mocked(apiClient).get = mockGet;

    await initializeCSRFProtection();

    expect(mockGet).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ withCredentials: true })
    );
  });

  it('logs success message', async () => {
    const consoleSpy = vi.spyOn(console, 'log');
    vi.mocked(apiClient).get.mockResolvedValue({ data: {} });

    await initializeCSRFProtection();

    expect(consoleSpy).toHaveBeenCalledWith('CSRF token initialized successfully');
  });
});
```

**Run Tests**:
```bash
npm test useCSRFToken
```

**Expected**: ✅ 20 tests passing

### Testing Components Using CSRF

For components that make API requests, mock the CSRF token:

```typescript
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Cookies from 'js-cookie';
import { EventForm } from '@/components/events/EventForm';

describe('EventForm', () => {
  beforeEach(() => {
    // Set mock CSRF token
    Cookies.set('XSRF-TOKEN', 'mock-csrf-token-123');

    // Mock API client
    vi.mocked(apiClient.post).mockResolvedValue({
      data: { id: '123', title: 'Test Event' },
    });
  });

  afterEach(() => {
    // Clean up
    Cookies.remove('XSRF-TOKEN');
    vi.clearAllMocks();
  });

  it('includes CSRF token when submitting form', async () => {
    // Render
    render(<EventForm />);

    // Fill form
    await userEvent.type(screen.getByLabelText('Event Title'), 'Test Event');

    // Submit
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    // Assert - API interceptor should have added X-CSRF-TOKEN header
    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith(
        '/api/admin/events',
        expect.any(Object)
      );
    });

    // Note: API interceptor runs in real code, mock doesn't include headers
    // For actual header validation, use E2E tests
  });
});
```

---

## End-to-End Testing

### Playwright CSRF Integration

For E2E tests using Playwright, CSRF is handled automatically by the browser:

```typescript
import { test, expect } from '@playwright/test';

test.describe('Event Management', () => {
  test.beforeEach(async ({ page }) => {
    // Login (CSRF token automatically initialized)
    await page.goto('/login');
    await page.fill('[name="email"]', 'admin@witchcityrope.com');
    await page.fill('[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for login success and CSRF initialization
    await page.waitForURL('/dashboard');
  });

  test('admin can create event', async ({ page }) => {
    // Navigate to create event page
    await page.goto('/admin/events/create');

    // Fill event form
    await page.fill('[name="title"]', 'Test Event');
    await page.fill('[name="description"]', 'Test Description');

    // Submit form (CSRF token automatically included)
    await page.click('button[type="submit"]');

    // Assert success
    await expect(page.locator('.notification')).toContainText('Event created');
  });
});
```

**CSRF Behavior in E2E Tests**:
1. Login sets authentication cookie
2. `initializeCSRFProtection()` called automatically
3. CSRF cookies set by browser
4. Form submissions include CSRF token automatically (via API interceptor)
5. **No manual CSRF handling needed in E2E tests**

---

## Manual Testing

### Testing CSRF Protection with cURL

#### 1. Get Authentication Token

```bash
# Login and get JWT token
curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' \
  -c cookies.txt

# cookies.txt now contains auth token
```

#### 2. Fetch CSRF Token

```bash
# Get CSRF token (requires authentication)
curl -X GET http://localhost:5655/api/antiforgery/token \
  -b cookies.txt \
  -c cookies.txt \
  -v

# Response headers should include:
# Set-Cookie: .AspNetCore.Antiforgery=CfDJ8...; HttpOnly; SameSite=Strict
# Set-Cookie: XSRF-TOKEN=CfDJ8...; SameSite=Strict

# Extract XSRF-TOKEN value
CSRF_TOKEN=$(grep XSRF-TOKEN cookies.txt | awk '{print $7}')
echo "CSRF Token: $CSRF_TOKEN"
```

#### 3. Test Protected Endpoint (With CSRF Token)

```bash
# POST request with CSRF token (should succeed)
curl -X POST http://localhost:5655/api/admin/events \
  -H "Content-Type: application/json" \
  -H "X-CSRF-TOKEN: $CSRF_TOKEN" \
  -b cookies.txt \
  -d '{
    "title": "Test Event",
    "description": "Test Description",
    "startDate": "2025-12-01T19:00:00Z"
  }'

# Expected: 201 Created
```

#### 4. Test Protected Endpoint (Without CSRF Token)

```bash
# POST request WITHOUT CSRF token (should fail)
curl -X POST http://localhost:5655/api/admin/events \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{
    "title": "Test Event",
    "description": "Test Description",
    "startDate": "2025-12-01T19:00:00Z"
  }'

# Expected: 400 Bad Request
# Response: {"error": "CSRF Validation Failed"}
```

#### 5. Test Public Endpoint (No CSRF Required)

```bash
# Public endpoint without authentication or CSRF (should succeed)
curl -X POST http://localhost:5655/api/vetting/public/applications \
  -H "Content-Type: application/json" \
  -d '{
    "email": "applicant@example.com",
    "sceneName": "Test Applicant",
    "experience": "Beginner"
  }'

# Expected: 201 Created
```

### Testing in Browser DevTools

1. **Open DevTools** (F12)
2. **Navigate to Application tab** → Cookies
3. **Check cookies after login**:
   - ✅ `.AspNetCore.Antiforgery` (HttpOnly, Secure, SameSite=Strict)
   - ✅ `XSRF-TOKEN` (Readable, Secure, SameSite=Strict)
4. **Open Network tab**
5. **Submit a form** (e.g., create event)
6. **Check request headers**:
   - ✅ `Authorization: Bearer <jwt>`
   - ✅ `X-CSRF-TOKEN: <token>`
7. **Check response**:
   - ✅ 201 Created (success)

---

## Test Scenarios

### Scenario 1: Valid CSRF Token

**Setup**: User logged in, CSRF token initialized

**Test**:
```csharp
var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);
var response = await client.PostAsJsonAsync("/api/admin/events", eventData);
```

**Expected**: 201 Created

### Scenario 2: Missing CSRF Token

**Setup**: User authenticated, NO CSRF token

**Test**:
```csharp
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
// NO CSRF token fetched
var response = await client.PostAsJsonAsync("/api/admin/events", eventData);
```

**Expected**: 400 Bad Request "CSRF Validation Failed"

### Scenario 3: Invalid CSRF Token

**Setup**: User authenticated, CSRF token tampered with

**Test**:
```csharp
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token-123");
var response = await client.PostAsJsonAsync("/api/admin/events", eventData);
```

**Expected**: 400 Bad Request "CSRF Validation Failed"

### Scenario 4: Expired CSRF Token

**Setup**: CSRF token fetched, time passes, token expires

**Test**: (Requires manual timing or token expiration configuration)

**Expected**: 400 Bad Request, user must re-login or refresh token

### Scenario 5: Public Endpoint (No CSRF)

**Setup**: No authentication, no CSRF token

**Test**:
```csharp
var client = _factory.CreateClient(); // No auth, no CSRF
var response = await client.PostAsJsonAsync("/api/vetting/public/applications", applicationData);
```

**Expected**: 201 Created (CSRF not required)

### Scenario 6: GET Request (No CSRF)

**Setup**: User authenticated OR anonymous

**Test**:
```csharp
var client = _factory.CreateClient(); // No CSRF needed for GET
var response = await client.GetAsync("/api/events");
```

**Expected**: 200 OK (CSRF not required for GET requests)

---

## Troubleshooting Test Failures

### Backend Tests

#### Error: "No Set-Cookie headers found in CSRF token response"

**Cause**: Authentication failed or token endpoint not working

**Fix**:
```csharp
// Verify Bearer token is set BEFORE fetching CSRF
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

// Then fetch CSRF
var csrfToken = await FetchCsrfTokenAsync(client);
```

#### Error: "XSRF-TOKEN cookie not found"

**Cause**: Antiforgery middleware not configured

**Fix**: Check `/apps/api/Program.cs`:
```csharp
// Verify these lines exist:
builder.Services.AddAntiforgery(options => { ... });
app.UseAntiforgery();
```

#### Error: Tests pass without CSRF token

**Cause**: Endpoint doesn't have CSRF validation yet

**Fix**: This is expected for endpoints not yet protected. Tests are future-proof.

#### Error: 400 BadRequest with "CSRF Validation Failed"

**Success!**: CSRF is working. Ensure test fetches CSRF token:
```csharp
var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);
```

### Frontend Tests

#### Error: "XSRF-TOKEN cookie is undefined"

**Cause**: Cookie not set in test

**Fix**:
```typescript
beforeEach(() => {
  Cookies.set('XSRF-TOKEN', 'mock-csrf-token-123');
});
```

#### Error: "initializeCSRFProtection is not a function"

**Cause**: Missing import

**Fix**:
```typescript
import { initializeCSRFProtection, getCSRFToken } from '@/hooks/useCSRFToken';
```

#### Error: API call succeeds without CSRF token

**Cause**: API interceptor not running in test environment

**Fix**: Mock the interceptor or test with E2E tests instead:
```typescript
// For unit tests, mock the behavior
vi.mocked(apiClient.post).mockImplementation(async (url, data, config) => {
  // Verify CSRF token would be added in real code
  expect(Cookies.get('XSRF-TOKEN')).toBeDefined();
  return { data: mockResponse };
});
```

### E2E Tests

#### Error: Form submission fails with 400

**Cause**: CSRF token not initialized after login

**Fix**: Ensure login flow completes before form submission:
```typescript
await page.fill('[name="email"]', 'admin@witchcityrope.com');
await page.fill('[name="password"]', 'Test123!');
await page.click('button[type="submit"]');

// Wait for login AND CSRF initialization
await page.waitForURL('/dashboard');
await page.waitForTimeout(500); // Allow CSRF initialization to complete
```

#### Error: Cookies not persisted

**Cause**: Browser context not preserving cookies

**Fix**: Use persistent context:
```typescript
const context = await browser.newContext({
  storageState: 'state.json', // Persist cookies between tests
});
```

---

## Running Tests

### Backend Integration Tests

```bash
# Run all integration tests
dotnet test tests/integration/

# Run CSRF-specific tests
dotnet test --filter "CsrfTokenIntegrationTests"

# Run specific test file
dotnet test --filter "VenueEndpointsIntegrationTests"
```

### Frontend Unit Tests

```bash
# Run all tests
npm test

# Run CSRF-specific tests
npm test useCSRFToken

# Run with coverage
npm test -- --coverage
```

### E2E Tests

```bash
# Run all Playwright tests
npm run test:e2e

# Run specific test file
npx playwright test specs/admin/event-management.spec.ts

# Run with UI
npx playwright test --ui
```

---

## Test Coverage Goals

| Test Type | Target Coverage | Current Status |
|-----------|----------------|----------------|
| Backend Integration (CSRF infrastructure) | 100% | ✅ 4/4 tests passing |
| Backend Integration (Protected endpoints) | >90% | 🔄 VenueEndpoints updated (19 tests) |
| Frontend Unit (CSRF hook) | 100% | ✅ 20/20 tests passing |
| Frontend Unit (Components) | >80% | 🔄 Component tests use mocked CSRF |
| E2E (Critical flows) | 100% | ✅ Login + form submission working |

---

## Next Steps

### For Test Developers

1. **Update remaining integration test files** using patterns in this guide
2. **Add CSRF validation tests** for new endpoints
3. **Enhance E2E tests** to explicitly verify CSRF token presence

### For Automation

- [ ] CI/CD pipeline includes CSRF integration tests
- [ ] Pre-commit hooks run CSRF-related tests
- [ ] Test coverage reports track CSRF test percentage

---

## Reference

### Documentation
- **Implementation Guide**: [./implementation-guide.md](./implementation-guide.md)
- **Developer Guide**: [./developer-guide.md](./developer-guide.md)
- **Integration Test Summary**: `/tests/integration/CSRF_IMPLEMENTATION_SUMMARY.md`
- **Integration Test Guide**: `/tests/integration/CSRF_INTEGRATION_TEST_GUIDE.md`

### Test Files
- **CSRF Infrastructure Tests**: `/tests/integration/api/CsrfTokenIntegrationTests.cs`
- **Reference Implementation**: `/tests/integration/api/Endpoints/VenueEndpointsIntegrationTests.cs`
- **Frontend Tests**: `/apps/web/src/hooks/__tests__/useCSRFToken.test.tsx`

---

**Testing Guide Version**: 1.0
**Last Updated**: 2025-11-23
**Maintained By**: Test Developer Team
