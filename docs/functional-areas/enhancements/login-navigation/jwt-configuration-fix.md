# JWT Configuration Fix

## Issue
Login was failing with error:
```
System.InvalidOperationException: 'JWT secret key not configured'
```

## Root Cause
There was a configuration mismatch between:
- **appsettings.json**: Uses `JwtSettings:` prefix
- **JwtTokenService.cs**: Was looking for `Jwt:` prefix (without "Settings")

This caused the JWT service to not find the secret key configuration.

## Fixes Applied

### 1. Updated JwtTokenService.cs
Changed configuration paths from:
```csharp
_issuer = configuration["Jwt:Issuer"];
_audience = configuration["Jwt:Audience"];
_secretKey = configuration["Jwt:SecretKey"];
_expirationMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);
```

To:
```csharp
_issuer = configuration["JwtSettings:Issuer"];
_audience = configuration["JwtSettings:Audience"];
_secretKey = configuration["JwtSettings:SecretKey"];
_expirationMinutes = configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60);
```

### 2. Updated Secret Key in appsettings.json
Changed from placeholder:
```json
"SecretKey": "YOUR-SECRET-KEY-HERE-MINIMUM-32-CHARACTERS-LONG"
```

To a proper development key:
```json
"SecretKey": "WitchCityRopeSecretKey2024$Development#Secure!"
```

## Important Security Note
The secret key in appsettings.json is for development only. For production:
1. Use a different, more secure key
2. Store it in environment variables or secure configuration
3. Never commit production keys to source control

## Testing Instructions

1. **Start the API project**:
   ```bash
   cd src/WitchCityRope.Api
   dotnet run
   ```
   The API should start on https://localhost:8181

2. **Start the Web project**:
   ```bash
   cd src/WitchCityRope.Web
   dotnet run
   ```
   The Web app should start on https://localhost:8281

3. **Test Login**:
   - Navigate to https://localhost:8281/auth/login
   - Login with: admin@witchcityrope.com / Test123!
   - Verify navigation menu updates with user options

## Result
The JWT configuration is now properly aligned between the configuration files and the JWT service, allowing successful token generation during login.