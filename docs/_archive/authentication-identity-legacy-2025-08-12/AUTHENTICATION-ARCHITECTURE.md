# Authentication Architecture

**Last Updated**: July 5, 2025  
**Status**: Fully Implemented and Working

## Overview

WitchCityRope uses a hybrid authentication approach that combines JWT tokens for API authentication with cookie-based authentication for Blazor Server. This architecture provides secure API access while maintaining SignalR connection state.

## Architecture Components

### 1. API Layer Authentication
- **Technology**: JWT Bearer Tokens
- **Token Lifetime**: 24 hours
- **Endpoint**: `/api/identity/account/login`
- **Claims**: UserId, Email, DisplayName, SceneName, Roles
- **Secret Key**: Configured in appsettings.json

### 2. Blazor Server Authentication
- **Technology**: Cookie Authentication
- **Cookie Name**: `auth-token`
- **Cookie Options**: HttpOnly, Secure (in production), SameSite.Lax
- **State Provider**: `ServerAuthenticationStateProvider`
- **Session Persistence**: Survives page refreshes and navigation

### 3. Key Services

#### ApiClient (`/src/WitchCityRope.Web/Services/ApiClient.cs`)
- Handles all HTTP communication with API
- Automatically includes JWT token in Authorization header
- Fixed URL construction to support all endpoints
- Manages token from auth cookie

#### AuthenticationService (`/src/WitchCityRope.Web/Services/AuthenticationService.cs`)
- Orchestrates login/logout flows
- Manages auth cookie lifecycle
- Notifies AuthenticationStateProvider of changes
- Provides user claims access

#### ServerAuthenticationStateProvider (`/src/WitchCityRope.Web/Services/ServerAuthenticationStateProvider.cs`)
- Custom Blazor AuthenticationStateProvider
- Handles prerendering scenarios
- Synchronizes with AuthenticationService
- Provides auth state to Blazor components

#### JwtTokenService (`/src/WitchCityRope.Infrastructure/Security/JwtTokenService.cs`)
- Generates JWT tokens with proper claims
- Validates token signatures
- Configurable expiration and issuer

## Authentication Flow

### Login Process
```
1. User enters credentials on Login.razor page
2. Login.razor.cs calls AuthenticationService.LoginAsync()
3. AuthenticationService calls API via ApiClient
4. API validates credentials against database (BCrypt)
5. API generates JWT token via JwtTokenService
6. API returns token in LoginResponse
7. AuthenticationService stores token in auth cookie
8. AuthenticationService notifies ServerAuthenticationStateProvider
9. Blazor UI updates automatically (navigation menu, etc.)
10. User is redirected to returnUrl or dashboard
```

### Authentication State Management
```
1. On page load, ServerAuthenticationStateProvider checks for auth cookie
2. If cookie exists, JWT is parsed for claims
3. ClaimsPrincipal is created with user identity
4. AuthenticationState is provided to Blazor components
5. Components use <AuthorizeView> for conditional rendering
6. Navigation menu updates based on auth state
```

## Recent Fixes (July 5, 2025)

### 1. API Communication Issues
**Problem**: ApiClient was prepending "api/v1/" to all endpoints, preventing auth endpoints from being reached.
```csharp
// Before (broken):
var fullUrl = $"api/v1/{endpoint}";

// After (fixed):
endpoint = endpoint.TrimStart('/');
var fullUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
```

### 2. Duplicate Health Check Endpoints
**Problem**: Multiple health check registrations caused routing conflicts.
**Fix**: Removed duplicate `MapHealthChecks` call in Program.cs.

### 3. Login Redirect Issues
**Problem**: Using `forceLoad: true` was causing authentication state loss.
```csharp
// Before (broken):
Navigation.NavigateTo(navigateTo, forceLoad: true);

// After (fixed):
Navigation.NavigateTo(navigateTo, forceLoad: false);
```

### 4. Navigation Menu Not Updating
**Problem**: MainNav wasn't subscribing to authentication state changes.
**Fix**: Made MainNav inherit from AuthenticationStateAwareComponentBase and subscribe to state changes.

### 5. Dashboard API Errors
**Problem**: Entity Framework couldn't translate instance methods in LINQ queries.
```csharp
// Before (broken):
private string MapRegistrationStatus(RegistrationStatus? status)

// After (fixed):
private static string MapRegistrationStatus(RegistrationStatus? status)
```

## Security Considerations

### Token Security
- JWT secret key minimum 32 characters
- Tokens signed with HMAC SHA256
- Short expiration (24 hours)
- No sensitive data in token payload

### Cookie Security
- HttpOnly flag prevents JavaScript access
- Secure flag ensures HTTPS only (production)
- SameSite.Lax prevents CSRF attacks
- Path scoped to application root

### API Security
- All endpoints require valid JWT (except auth endpoints)
- Role-based authorization via [Authorize] attributes
- Claims-based permissions
- BCrypt password hashing

## Configuration

### API Configuration (`appsettings.json`)
```json
{
  "JwtSettings": {
    "Secret": "your-secret-key-minimum-32-characters",
    "Issuer": "WitchCityRope",
    "Audience": "WitchCityRopeUsers",
    "ExpirationInHours": 24
  }
}
```

### Web Configuration
```json
{
  "ApiUrl": "http://api:8080"  // Docker internal URL
}
```

## Testing Authentication

### Test Accounts
- admin@witchcityrope.com / Test123! (Administrator role)
- member@witchcityrope.com / Test123! (Member role)
- teacher@witchcityrope.com / Test123! (Teacher role)

### Manual Testing
1. Navigate to `/login`
2. Enter credentials
3. Verify redirect to dashboard
4. Check navigation menu updates
5. Verify API calls include auth header
6. Test logout functionality

### Automated Testing
```javascript
// test-login.js
const response = await fetch('http://localhost:5653/api/identity/account/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'admin@witchcityrope.com',
    password: 'Test123!'
  })
});
const data = await response.json();
console.log('Token:', data.token);
```

## Troubleshooting

### Common Issues

1. **"Invalid email or password"**
   - Check database seeding ran successfully
   - Verify password is "Test123!" for test accounts
   - Ensure API is accessible from Web container

2. **Login succeeds but redirects back to login**
   - Check auth cookie is being set
   - Verify ServerAuthenticationStateProvider is registered
   - Ensure navigation isn't using forceLoad

3. **Navigation menu doesn't update**
   - Verify MainNav is using AuthenticationStateAwareComponentBase
   - Check AuthenticationStateChanged events are firing
   - Ensure cookie contains valid JWT

4. **API calls return 401 Unauthorized**
   - Check auth cookie exists
   - Verify ApiClient is reading cookie correctly
   - Ensure JWT hasn't expired (24 hour lifetime)

### Debug Logging
Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "WitchCityRope.Web.Services": "Debug",
      "WitchCityRope.Api.Features.Auth": "Debug"
    }
  }
}
```

## Future Enhancements

1. **Refresh Token Implementation**
   - Add refresh token support
   - Automatic token renewal
   - Sliding expiration

2. **Two-Factor Authentication**
   - TOTP-based 2FA
   - Backup codes
   - Remember device option

3. **OAuth Integration**
   - Google OAuth (already configured)
   - Additional providers

4. **Session Management**
   - Active sessions display
   - Remote session termination
   - Device tracking