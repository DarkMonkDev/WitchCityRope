# WitchCityRope Login Issue Analysis

## Problem Summary
The login functionality appears to work but doesn't actually authenticate users. When clicking "Sign In", no API calls are made to the authentication endpoint, preventing actual authentication.

## Current State
- ✅ API is working correctly (`http://localhost:5653/api/identity/account/login`)
- ✅ Database has test users with correct passwords
- ✅ Docker containers are all running
- ✅ Internal Docker networking is working (web can reach api)
- ❌ Blazor app is NOT making HTTP calls when login button is clicked
- ❌ Authentication state is not persisting after "login"

## Root Cause Analysis

### 1. HttpClient Configuration
The HttpClient is properly configured in Program.cs:
```csharp
builder.Services.AddHttpClient<AuthenticationService>(client =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### 2. Environment Configuration
The Docker environment correctly sets:
- `ApiUrl=http://api:8080` (for internal Docker communication)
- `ASPNETCORE_ENVIRONMENT=Development`

### 3. The Issue
The Blazor component appears to have a try-catch block that's catching errors and displaying a generic "An error occurred during login" message without making the actual HTTP call. This could be due to:
- JavaScript interop issues during server-side rendering
- HttpClient not being properly injected
- A silent error preventing the HTTP call

## Immediate Workarounds

### Option 1: Direct Browser Testing
1. Open browser to: `http://localhost:5651/identity/account/login`
2. Open Developer Tools (F12) > Console
3. Try logging in and check for JavaScript errors

### Option 2: Check Container Health
```bash
# Check if web container is healthy
docker-compose ps

# If unhealthy, check detailed logs
docker-compose logs web | tail -50
```

### Option 3: Test from Inside Container
```bash
# Run the test script
./test-internal-api.sh
```

## Recommended Fixes

### 1. Add More Detailed Logging
The AuthenticationService already has logging, but it's not reaching the HTTP call. Add logging at the very start of LoginAsync to see if the method is being called.

### 2. Check Blazor Component Error Handling
The Login.razor.cs file has a try-catch that might be catching errors too broadly:
```csharp
catch (Exception)
{
    _errorMessage = "An error occurred during login";
}
```

### 3. Verify HttpClient Injection
Ensure the AuthenticationService is getting a properly configured HttpClient instance.

## Quick Test Commands

### Test API Directly
```bash
# Test from outside Docker
curl -X POST http://localhost:5653/api/identity/account/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@witchcityrope.com", "password": "Test123!"}'

# Test from inside web container
docker-compose exec web curl -X POST http://api:8080/api/identity/account/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@witchcityrope.com", "password": "Test123!"}'
```

### Monitor Logs
```bash
# Watch web logs for errors
docker-compose logs -f web

# Or use the monitoring script
chmod +x monitor-login.sh
./monitor-login.sh
```

## Next Steps

1. **Check Browser Console**: The most immediate step is to check the browser console for JavaScript errors when attempting to login.

2. **Enable Detailed Errors**: The application is configured with `DetailedErrors=true` but errors might be suppressed in the UI.

3. **Test Different Scenarios**:
   - Try with incorrect credentials to see if behavior changes
   - Check if registration works (different code path)
   - Test with browser DevTools Network tab open

4. **Review Recent Changes**: The authentication was working in previous sessions, so something in the recent changes or Docker configuration might have broken it.

## Test Credentials
- Email: `admin@witchcityrope.com`
- Password: `Test123!`
- Expected: Should redirect to `/member/dashboard` after successful login

## Support Files Created
- `test-login.sh` - Tests API authentication
- `test-internal-api.sh` - Tests API from inside web container
- `monitor-login.sh` - Monitors logs during login attempts
- Multiple Puppeteer test scripts in the project directory