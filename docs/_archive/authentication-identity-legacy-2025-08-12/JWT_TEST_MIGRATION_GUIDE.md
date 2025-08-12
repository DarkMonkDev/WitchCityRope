# JWT Test Migration Guide

## Overview

The WitchCityRope project uses different authentication mechanisms:
- **Web Project**: Cookie-based authentication with ASP.NET Core Identity
- **API Project**: JWT tokens for API authentication

## The Problem

The test projects have their own `IJwtService` interface that doesn't match the actual API interfaces, causing test failures and confusion.

## Current Architecture

### 1. Web Project (Cookie Authentication)
- Uses ASP.NET Core Identity
- Cookie-based authentication
- NO JWT tokens for user authentication
- Located in: `/src/WitchCityRope.Web/`

### 2. API Project (JWT Authentication)
- Uses JWT for API authentication
- Has two JWT interfaces:
  - `WitchCityRope.Api.Interfaces.IJwtService`
  - `WitchCityRope.Api.Features.Auth.Services.IJwtService`
- Located in: `/src/WitchCityRope.Api/`

### 3. Test Project Issues
- Has its own `IJwtService` in `WitchCityRope.Tests.Common.Interfaces`
- Interface methods don't match (async vs sync)
- Causes confusion and test failures

## Migration Steps

### 1. Update Test References

Replace test project IJwtService with the correct API interface:

```csharp
// ❌ WRONG - Using test project interface
using WitchCityRope.Tests.Common.Interfaces;
private readonly Mock<IJwtService> _jwtServiceMock;

// ✅ CORRECT - Using API project interface
using WitchCityRope.Api.Features.Auth.Services;
private readonly Mock<IJwtService> _jwtServiceMock;
```

### 2. Update Mock Setup

The API IJwtService has different methods:

```csharp
// ❌ WRONG - Test interface (async methods)
_jwtServiceMock.Setup(x => x.GenerateTokenAsync(It.IsAny<IUser>()))
    .ReturnsAsync(new JwtToken { Token = "test" });

// ✅ CORRECT - API interface (sync methods)
_jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<UserWithAuth>()))
    .Returns(new JwtToken { Token = "test", ExpiresIn = 3600 });
```

### 3. Use the JwtServiceMock Helper

Use the new mock helper for consistency:

```csharp
// Create a default mock
var jwtServiceMock = JwtServiceMock.Create();

// Create a mock with specific token
var jwtServiceMock = JwtServiceMock.CreateWithToken("my-token", "my-refresh-token");
```

### 4. Remove Test Project Interfaces

Consider removing the duplicate interfaces from the test project to avoid confusion:
- `/tests/WitchCityRope.Tests.Common/Interfaces/TestInterfaces.cs` - IJwtService
- `/tests/WitchCityRope.Tests.Common/TestDoubles/TestJwtService.cs`

## Common Test Scenarios

### Testing API Authentication

```csharp
[Fact]
public void Login_Should_Return_JWT_Token()
{
    // Arrange
    var jwtServiceMock = JwtServiceMock.Create();
    var authService = new AuthService(/*...*/, jwtServiceMock.Object, /*...*/);
    
    // Act
    var result = await authService.LoginAsync("user@example.com", "password");
    
    // Assert
    result.Should().NotBeNull();
    result.Token.Should().StartWith("test-token-for-");
}
```

### Testing Web Authentication (No JWT)

```csharp
[Fact]
public void Web_Login_Should_Use_Cookies_Not_JWT()
{
    // Web authentication doesn't use JWT
    // Test cookie authentication instead
    var signInManager = new Mock<SignInManager<WitchCityRopeUser>>();
    // ... test cookie-based login
}
```

## Key Points to Remember

1. **Web = Cookies, API = JWT** - Don't mix them up
2. **Use correct interfaces** - API project interfaces, not test project
3. **Sync vs Async** - API uses sync methods (GenerateToken, not GenerateTokenAsync)
4. **UserWithAuth not IUser** - API uses concrete types, not interfaces

## Affected Test Files

These files need to be updated:
- `/tests/WitchCityRope.Api.Tests/Services/AuthServiceTests.cs`
- `/tests/WitchCityRope.Api.Tests/Features/Auth/LoginCommandTests.cs`
- Any other test using IJwtService

## Questions?

If you're unsure whether a test should use JWT or cookies:
- Is it testing Web UI? → Use cookies
- Is it testing API endpoints? → Use JWT
- Is it testing internal services? → Depends on which project they're in