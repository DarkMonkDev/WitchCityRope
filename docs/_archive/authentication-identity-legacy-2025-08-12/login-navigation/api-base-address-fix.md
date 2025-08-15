# API Base Address Configuration Fix

## Issue
Login was failing with error:
```
System.InvalidOperationException: An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.
```

## Root Cause
The `AuthenticationService` was using an HttpClient without a configured BaseAddress, attempting to make calls to relative URLs like `"api/auth/login"`.

## Fix Applied

Updated `Program.cs` to configure HttpClient specifically for AuthenticationService:

```csharp
// Configure HttpClient for AuthenticationService
builder.Services.AddHttpClient<AuthenticationService>(client =>
{
    // Use configuration to get the API URL
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

Also removed the duplicate registration of AuthenticationService since `AddHttpClient<T>` already registers the service.

## Configuration

The API URL is configured in `appsettings.json`:
```json
"ApiUrl": "https://localhost:8181"
```

This matches the API's default HTTPS port as configured in the API project.

## Testing

After this fix:
1. Start the API project (it should run on https://localhost:8181)
2. Start the Web project
3. Navigate to `/auth/login`
4. Login with test credentials:
   - admin@witchcityrope.com / Test123!
   - member@witchcityrope.com / Test123!

The login should now work correctly without the BaseAddress error.