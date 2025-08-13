# JWT Service-to-Service Authentication

## Overview

WitchCityRope uses a hybrid authentication approach where the Blazor Server Web application authenticates users via cookies, but needs JWT tokens to make authenticated API calls on behalf of those users. This document explains the service-to-service authentication mechanism that bridges these two authentication methods.

## Architecture

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│                 │         │                 │         │                 │
│      User       │────────▶│   Web Service   │────────▶│   API Service   │
│    (Browser)    │ Cookie  │  (Blazor Server)│  JWT    │  (Minimal API)  │
│                 │  Auth   │                 │  Auth   │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
                                     │                           │
                                     │    Service-to-Service    │
                                     │◀──────────────────────────▶
                                     │   Authentication Flow    │
```

## Authentication Flow

### 1. User Login (Cookie Authentication)
1. User logs in via `/Identity/Account/Login` page
2. ASP.NET Core Identity validates credentials
3. Authentication cookie is created for the user session
4. User is now authenticated in the Web service

### 2. JWT Token Acquisition (Service-to-Service)
When the Web service needs to call the API on behalf of the user:

1. **AuthenticationEventHandler** (`/src/WitchCityRope.Web/Services/AuthenticationEventHandler.cs`) is triggered during sign-in
2. **ApiAuthenticationService** (`/src/WitchCityRope.Web/Services/ApiAuthenticationService.cs`) is called to get a JWT token
3. Web service sends a request to API's `/api/auth/service-token` endpoint with:
   - User ID and Email in the request body
   - Shared secret in `X-Service-Secret` header
4. API validates the service secret and generates a JWT token for that user
5. JWT token is stored in both server-side memory cache and browser session storage

### 3. API Calls with JWT
1. **AuthenticationDelegatingHandler** (`/src/WitchCityRope.Web/Services/AuthenticationDelegatingHandler.cs`) intercepts all HTTP requests from ApiClient
2. If user is authenticated, it retrieves the JWT token from storage
3. JWT token is added to the `Authorization: Bearer` header
4. API validates the JWT token and processes the request

## Implementation Details

### API Side Components

#### 1. Service Token Endpoint
**File**: `/src/WitchCityRope.Api/Features/Auth/AuthController.cs`

```csharp
[HttpPost("service-token")]
[AllowAnonymous] // Auth is handled via shared secret
public async Task<ActionResult<LoginResponse>> GetServiceToken([FromBody] ServiceTokenRequest request)
{
    // Validate service secret from header
    var serviceSecret = Request.Headers["X-Service-Secret"].FirstOrDefault();
    var expectedSecret = _configuration["ServiceAuth:Secret"];
    
    if (string.IsNullOrEmpty(serviceSecret) || serviceSecret != expectedSecret)
    {
        return Unauthorized(new { message = "Invalid service credentials" });
    }

    // Generate JWT token for the specified user
    var response = await _authService.GetServiceTokenAsync(request.UserId, request.Email);
    return Ok(response);
}
```

#### 2. Service Token Generation
**File**: `/src/WitchCityRope.Api/Features/Auth/Services/IdentityAuthService.cs`

```csharp
public async Task<LoginResponse> GetServiceTokenAsync(string userId, string email)
{
    // Find and validate user
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null || user.Email != email)
    {
        throw new UnauthorizedException("User not found or email mismatch");
    }

    // Validate user status
    if (!user.IsActive || !user.EmailConfirmed)
    {
        throw new UnauthorizedException("Account is not active or email not verified");
    }

    // Generate JWT token
    var token = _jwtService.GenerateToken(userWithAuth);
    var refreshToken = _jwtService.GenerateRefreshToken();

    return new LoginResponse
    {
        Token = token.Token,
        RefreshToken = refreshToken,
        ExpiresAt = token.ExpiresAt,
        User = MapToUserDto(user)
    };
}
```

### Web Side Components

#### 1. API Authentication Service
**File**: `/src/WitchCityRope.Web/Services/ApiAuthenticationService.cs`

```csharp
public async Task<string?> GetJwtTokenForUserAsync(WitchCityRopeUser user)
{
    // Check for existing valid token first
    var existingToken = await _jwtTokenService.GetTokenAsync();
    if (!string.IsNullOrEmpty(existingToken))
    {
        return existingToken;
    }

    // Prepare service token request
    var tokenRequest = new
    {
        UserId = user.Id.ToString(),
        Email = user.Email
    };

    // Add service secret header
    var serviceSecret = _configuration["ServiceAuth:Secret"];
    _httpClient.DefaultRequestHeaders.Add("X-Service-Secret", serviceSecret);

    // Call API to get JWT token
    var response = await _httpClient.PostAsync("/api/auth/service-token", content);
    
    if (response.IsSuccessStatusCode)
    {
        var authResponse = await response.Content.ReadFromJsonAsync<ApiAuthResponse>();
        await _jwtTokenService.SetTokenAsync(authResponse.Token);
        return authResponse.Token;
    }
}
```

#### 2. JWT Token Storage Service
**File**: `/src/WitchCityRope.Web/Services/JwtTokenService.cs`

Manages JWT token storage with dual-storage strategy:
- **Server-side memory cache**: Primary storage, works during server-side rendering
- **Browser session storage**: Fallback for interactive scenarios
- **User-specific cache keys**: Prevents token leakage between users

```csharp
public async Task<string?> GetTokenAsync()
{
    var userId = GetCurrentUserId();
    var cacheKey = $"jwt_token_{userId}";

    // Try server-side cache first (works during SSR)
    if (_memoryCache.TryGetValue(cacheKey, out string? cachedToken))
    {
        return cachedToken;
    }

    // Fallback to browser storage if JS interop is available
    var result = await _sessionStorage.GetAsync<string>(TokenKey);
    if (result.Success)
    {
        // Cache it server-side for future SSR requests
        _memoryCache.Set(cacheKey, result.Value, _cacheExpiry);
        return result.Value;
    }

    return null;
}
```

#### 3. Authentication Delegating Handler
**File**: `/src/WitchCityRope.Web/Services/AuthenticationDelegatingHandler.cs`

Automatically attaches JWT tokens to outgoing API requests:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
{
    if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
    {
        // Get JWT token from storage
        var token = await _jwtTokenService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    return await base.SendAsync(request, cancellationToken);
}
```

### Configuration

#### Important Database Schema Note
**CRITICAL**: The user data is stored in the `auth` schema, not the default public schema. The `UserManager` in ASP.NET Core Identity is configured to use:
- Table: `auth."Users"`
- Database: `witchcityrope_db`
- User ID format: GUID (e.g., `34b43f88-3f71-41c0-b84d-aaf577637b5c`)

#### Web Service Configuration
**File**: `/src/WitchCityRope.Web/Program.cs`

```csharp
// Register JWT token services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
builder.Services.AddScoped<AuthenticationEventHandler>();

// Register DelegatingHandler as Transient (required for IHttpClientFactory)
builder.Services.AddTransient<AuthenticationDelegatingHandler>();

// Configure HttpClient with authentication handler
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();
```

#### Application Settings
**Files**: `appsettings.json` and `appsettings.Development.json`

```json
{
  "ServiceAuth": {
    "Secret": "YOUR-SHARED-SECRET-FOR-SERVICE-TO-SERVICE-AUTH-MINIMUM-32-CHARS"
  },
  "JwtSettings": {
    "SecretKey": "YOUR-JWT-SECRET-KEY",
    "Issuer": "WitchCityRope",
    "Audience": "WitchCityRopeUsers",
    "ExpirationMinutes": 60
  }
}
```

## Security Considerations

### 1. Shared Secret Protection
- The service-to-service secret must be:
  - At least 32 characters long
  - Different from the JWT signing key
  - Stored securely (use Azure Key Vault or similar in production)
  - Rotated periodically

### 2. Token Expiration
- JWT tokens expire after 60 minutes by default
- Consider implementing token refresh logic for long-running sessions
- Tokens are automatically cleared on user logout

### 3. Validation Checks
The API performs several validation checks before issuing tokens:
- Service secret validation
- User existence verification
- Email match verification
- Account active status check
- Email confirmation status check

### 4. Storage Security
- Tokens are stored per-user to prevent cross-user token leakage
- Server-side cache is preferred over browser storage
- Tokens are cleared on logout via `AuthenticationEventHandler.SigningOut`

## Testing the Implementation

### 1. Manual Testing
```bash
# 1. Start both services (or use Docker)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
# OR
dotnet run --project src/WitchCityRope.Web
dotnet run --project src/WitchCityRope.Api

# 2. Test service-token endpoint directly
curl -X POST http://localhost:5653/api/auth/service-token \
  -H "Content-Type: application/json" \
  -H "X-Service-Secret: DevSecret-WitchCityRope-ServiceToService-Auth-2024!" \
  -d '{"userId": "34b43f88-3f71-41c0-b84d-aaf577637b5c", "email": "admin@witchcityrope.com"}'

# 3. Login as admin via Web UI
# Navigate to http://localhost:5651
# Login with: admin@witchcityrope.com / Test123!

# 4. Access admin area
# Navigate to /admin/users
# Check browser dev tools Network tab for API calls with JWT tokens
```

### 2. Verify JWT Token Flow
Check the following in browser dev tools:
1. After login, check Application > Session Storage for stored JWT token
2. On API calls, verify `Authorization: Bearer` header is present
3. Check console logs for JWT token acquisition messages

### 3. Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized on API calls | Missing or expired JWT token | Check if token acquisition succeeded during login |
| "Invalid service credentials" | Mismatched service secrets | Ensure both Web and API have same `ServiceAuth:Secret` |
| "User not found" error | Wrong database schema | Ensure UserManager is configured to use `auth` schema |
| Token not persisting | JS interop not available during SSR | Normal behavior - server cache handles SSR requests |
| User mismatch errors | Stale tokens after user switch | Clear browser session storage and re-login |
| Service registration error | Wrong IAuthService implementation | Must use `IdentityAuthService` not `AuthService` stub |

## Extending the System

### Adding Token Refresh
To implement automatic token refresh:

1. Check token expiration before each API call in `AuthenticationDelegatingHandler`
2. If token is near expiration, call refresh endpoint
3. Update stored token with new one

### Adding Token Validation
To add token validation in the Web service:

```csharp
private async Task<bool> IsTokenValidAsync(string token)
{
    var handler = new JsonWebTokenHandler();
    var result = handler.ReadJsonWebToken(token);
    return result.ValidTo > DateTime.UtcNow.AddMinutes(5); // 5 min buffer
}
```

### Implementing Client Credentials Flow
For true service-to-service calls (no user context):

1. Create dedicated service credentials
2. Implement OAuth 2.0 client credentials flow
3. Use separate HttpClient configuration for service calls

## Related Documentation

- [Authentication Overview](./README.md) - General authentication architecture
- [ADR-002: Authentication API Pattern](../../architecture/decisions/adr-002-authentication-api-pattern.md) - Architectural decision record
- [Blazor Server Patterns](../../standards-processes/development-standards/blazor-server-patterns.md) - Blazor-specific patterns

## Troubleshooting Checklist

- [ ] Both services have matching `ServiceAuth:Secret` values
- [ ] JWT settings are configured in both services
- [ ] User is properly authenticated with cookies before API calls
- [ ] `AuthenticationDelegatingHandler` is registered as Transient
- [ ] `IHttpContextAccessor` is registered in DI container
- [ ] API endpoints have proper `[Authorize]` attributes
- [ ] CORS is configured to allow Web service origin
- [ ] Database connection is working for user validation

## Contact

For questions or issues with the authentication system, check:
- `/docs/lessons-learned/` for known issues and solutions
- `/docs/architecture/decisions/` for architectural context
- GitHub issues for bug reports and feature requests