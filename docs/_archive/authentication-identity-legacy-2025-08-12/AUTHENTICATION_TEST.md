# Authentication Testing Guide

This guide explains how to test the authentication system for the WitchCityRope application.

## Setup

1. **Start the API** (runs on https://localhost:7001):
   ```bash
   cd src/WitchCityRope.Api
   dotnet run
   ```

2. **Start the Web Application** (runs on https://localhost:5001):
   ```bash
   cd src/WitchCityRope.Web
   dotnet run
   ```

## Testing Authentication Flow

### 1. Public Test Page
Navigate to: https://localhost:5001/test-auth

This page shows:
- Current authentication status
- Links to test various authentication scenarios
- Authentication state details

### 2. Protected Test Page
Navigate to: https://localhost:5001/test-protected

Expected behavior:
- If not authenticated: Redirects to login page
- If authenticated: Shows user information, claims, and roles

### 3. Login Flow
1. Navigate to: https://localhost:5001/login
2. Enter credentials (you may need to register first)
3. After successful login, you should be redirected to the originally requested page

### 4. Registration Flow
1. Navigate to: https://localhost:5001/register
2. Fill out the registration form
3. After successful registration, you can log in

## Authentication Implementation Details

### Web Project (Blazor Server)
- **Authentication Type**: Cookie-based authentication
- **Configuration**: See `Program.cs` for cookie authentication setup
- **State Provider**: `AuthenticationService` extends `AuthenticationStateProvider`
- **Protected Routes**: Use `[Authorize]` attribute on pages/components

### API Project
- **Authentication Type**: JWT Bearer tokens
- **Configuration**: See `Infrastructure/ApiConfiguration.cs`
- **Protected Endpoints**: Use `.RequireAuthorization()` on endpoints

### Key Components

1. **AuthenticationService** (`/src/WitchCityRope.Web/Services/AuthenticationService.cs`)
   - Handles login/logout
   - Manages authentication state
   - Creates cookie authentication for Blazor Server
   - Stores JWT tokens for API calls

2. **App.razor** (`/src/WitchCityRope.Web/App.razor`)
   - Wraps routes with `CascadingAuthenticationState`
   - Uses `AuthorizeRouteView` for protected routes
   - Handles unauthorized access with redirect to login

3. **API Authentication** (`/src/WitchCityRope.Api/Infrastructure/ApiConfiguration.cs`)
   - Configures JWT bearer authentication
   - Defines authorization policies
   - Handles CORS for Web-to-API communication

## Troubleshooting

### Issue: Not redirecting to login
- Check that `App.razor` has `AuthorizeRouteView` configured
- Verify authentication middleware is in correct order in `Program.cs`

### Issue: API calls failing with 401
- Ensure JWT token is being sent in Authorization header
- Check CORS configuration allows your Web app origin
- Verify JWT settings match between Web and API projects

### Issue: Losing authentication state
- Check cookie settings in Web `Program.cs`
- Ensure `IHttpContextAccessor` is registered
- Verify session configuration

## Testing Checklist

- [ ] Public pages are accessible without authentication
- [ ] Protected pages redirect to login when not authenticated
- [ ] Login creates authentication session
- [ ] User information is displayed correctly after login
- [ ] Logout clears the session
- [ ] API calls include JWT token in Authorization header
- [ ] CORS allows Web-to-API communication
- [ ] Authentication persists across page refreshes (cookie-based)