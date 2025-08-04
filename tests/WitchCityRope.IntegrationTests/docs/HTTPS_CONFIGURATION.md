# HTTPS Configuration for Integration Tests

## Overview

Integration tests in this project are configured to run without HTTPS redirection to avoid common test failures related to SSL certificates and port configuration.

## Configuration Details

### 1. Test Environments
The following environment names are recognized as test environments:
- `Testing` - Used by TestWebApplicationFactory
- `Test` - Used by BlazorWebApplicationFactory  
- `Development` - Standard development environment

### 2. HTTPS Redirection Disabled
HTTPS redirection middleware (`UseHttpsRedirection()`) is disabled in:
- `/src/WitchCityRope.Web/Program.cs`
- `/src/WitchCityRope.Api/Infrastructure/ApiConfiguration.cs`

When the environment is one of the test environments, HTTPS redirection is skipped.

### 3. Kestrel Configuration
Test factories are configured to use HTTP only:
```csharp
builder.UseKestrel(options =>
{
    options.ListenLocalhost(8080); // HTTP only for tests
});
```

## Common Issues and Solutions

### Issue: "Failed to determine the https port for redirect"
**Cause**: The application is trying to redirect HTTP requests to HTTPS but cannot determine the HTTPS port.

**Solution**: This has been fixed by disabling HTTPS redirection in test environments.

### Issue: SSL Certificate Errors
**Cause**: Test clients trying to connect via HTTPS to self-signed certificates.

**Solution**: Tests now use HTTP exclusively, avoiding certificate validation issues.

## Testing HTTPS Configuration

A dedicated test class `HttpsRedirectionTests.cs` verifies that:
1. HTTP requests are not redirected to HTTPS
2. Health endpoints are accessible via HTTP
3. Static files can be served over HTTP

## Best Practices

1. Always use the provided test factories (`TestWebApplicationFactory` or `BlazorWebApplicationFactory`)
2. Don't manually configure HTTPS in test environments
3. Use `AllowAutoRedirect = false` when testing redirect behavior
4. For production-like testing with HTTPS, use separate E2E tests with proper certificates