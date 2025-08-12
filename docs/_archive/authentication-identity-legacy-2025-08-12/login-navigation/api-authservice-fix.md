# API AuthService Dependency Injection Fix

## Issue
Login was failing with dependency injection error:
```
Unable to resolve service for type 'WitchCityRope.Api.Features.Auth.Services.AuthService' while attempting to activate 'WitchCityRope.Api.Features.Auth.AuthController'.
```

## Root Cause
The AuthController was injecting the concrete `AuthService` class directly instead of using the `IAuthService` interface.

## Fixes Applied

### 1. Updated AuthController to Use Interface
Changed from:
```csharp
private readonly AuthService _authService;
public AuthController(AuthService authService)
```

To:
```csharp
private readonly IAuthService _authService;
public AuthController(IAuthService authService)
```

### 2. Added Missing Interface Method
Added `VerifyEmailAsync` to the IAuthService interface:
```csharp
/// <summary>
/// Verifies a user's email address with the provided token
/// </summary>
Task VerifyEmailAsync(string token);
```

### 3. Fixed Method Return Type
Changed AuthService.VerifyEmailAsync from returning `Task<bool>` to just `Task` to match the interface.

## Result
The API project now builds successfully and the AuthController properly uses dependency injection with the IAuthService interface.

## Testing
To test the login:
1. Ensure the API project is running (https://localhost:8181)
2. Ensure the Web project is running
3. Navigate to `/auth/login` in the Web application
4. Login with test credentials (e.g., admin@witchcityrope.com / Test123!)

The login should now work without dependency injection errors.