# Authentication Patterns and Best Practices

## Overview

This document defines mandatory authentication patterns for the WitchCityRope project. These patterns address Blazor Server authentication limitations and ensure secure, reliable user authentication.

## 🚨 CRITICAL: Blazor Server Authentication Architecture

### The Fundamental Issue

**SignInManager cannot be used directly in Blazor Server interactive components.**

Microsoft's official guidance states that SignInManager operations MUST happen outside Blazor's rendering context to avoid "Headers are read-only, response has already started" errors.

### Mandatory Pattern: API Endpoints for Authentication

**NEVER**: Use SignInManager directly in Blazor components
**ALWAYS**: Use API endpoints for all authentication operations

```csharp
// ❌ WRONG: Direct SignInManager usage in Blazor component
@inject SignInManager<ApplicationUser> SignInManager
// This will cause "Headers are read-only" error

// ✅ CORRECT: Use API endpoints pattern
// Authentication pattern: Blazor Component → API Endpoint → SignInManager → Cookies
```

## Authentication Service Architecture

### API Endpoints (AuthController.cs)

Authentication endpoints handle SignInManager operations:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, request.RememberMe, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            return Ok(new { success = true });
        }
        
        return BadRequest(new { error = "Invalid login attempt" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { success = true });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return Ok(new { success = true });
        }

        return BadRequest(new { errors = result.Errors });
    }
}
```

### Authentication Service (Web Project)

Service layer calls API endpoints from Blazor components:

```csharp
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task<AuthResult> LogoutAsync();
    Task<AuthResult> RegisterAsync(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password,
                RememberMe = rememberMe
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            return AuthResult.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", email);
            return AuthResult.Failure("Login failed. Please try again.");
        }
    }
}
```

### Blazor Component Usage

Components use the service, never SignInManager directly:

```csharp
@page "/login"
@inject IAuthService AuthService
@inject NavigationManager Navigation

<h3>Login</h3>

<EditForm Model="@loginModel" OnValidSubmit="@HandleLogin">
    <div class="form-group">
        <label>Email:</label>
        <InputText @bind-Value="loginModel.Email" class="form-control" />
    </div>
    
    <div class="form-group">
        <label>Password:</label>
        <InputText @bind-Value="loginModel.Password" type="password" class="form-control" />
    </div>
    
    <button type="submit" class="btn btn-primary">Login</button>
</EditForm>

@code {
    private LoginModel loginModel = new();
    
    private async Task HandleLogin()
    {
        var result = await AuthService.LoginAsync(loginModel.Email, loginModel.Password);
        
        if (result.IsSuccess)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
            // Handle error
        }
    }
}
```

## Critical Implementation Details

### HttpClient Configuration

**CRITICAL**: HttpClient must use internal container port (8080) not external (5651)

```csharp
// In Web Program.cs
services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri("http://api:8080"); // Internal container communication
});
```

### Authentication Schemes

**API Project** uses JWT Bearer:
```csharp
// API Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });
```

**Web Project** uses Cookies:
```csharp
// Web Program.cs
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { /* Cookie config */ });
```

### Error Handling Patterns

```csharp
public class AuthResult
{
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; }
    
    public static AuthResult Success() => new() { IsSuccess = true };
    public static AuthResult Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

## Common Authentication Issues

### "Headers are read-only" Error
**Cause**: Using SignInManager directly in Blazor component
**Solution**: Use API endpoint pattern described above

### Authentication State Not Updating
**Cause**: Blazor components not refreshing after auth state change
**Solution**: Use `AuthenticationStateProvider` and call `NotifyAuthenticationStateChanged()`

```csharp
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Implementation
    }

    public void NotifyUserAuthentication(ClaimsPrincipal user)
    {
        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }
}
```

### Cookie Issues in Docker
**Cause**: Different domains between Web and API containers
**Solution**: Configure cookie sharing between containers

```csharp
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("WitchCityRope");
```

## Security Best Practices

### Input Validation
Always validate authentication inputs:

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email is required");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}
```

### Rate Limiting
Implement rate limiting on auth endpoints:

```csharp
[HttpPost("login")]
[EnableRateLimiting("AuthPolicy")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Implementation
}
```

### Audit Logging
Log all authentication events:

```csharp
_logger.LogInformation("Login attempt for user {Email} from IP {IpAddress}", 
    request.Email, HttpContext.Connection.RemoteIpAddress);

if (result.Succeeded)
{
    _logger.LogInformation("Successful login for user {Email}", request.Email);
}
else
{
    _logger.LogWarning("Failed login attempt for user {Email}: {Reason}", 
        request.Email, result.ToString());
}
```

## Testing Authentication

### Integration Tests
Test the complete authentication flow:

```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsSuccess()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginRequest = new LoginRequest
    {
        Email = "test@example.com",
        Password = "Test123!"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

    // Assert
    Assert.True(response.IsSuccessStatusCode);
}
```

### E2E Tests with Playwright
Test authentication in the UI:

```typescript
test('user can login with valid credentials', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid=email]', 'test@example.com');
    await page.fill('[data-testid=password]', 'Test123!');
    await page.click('[data-testid=login-button]');
    
    await expect(page).toHaveURL('/dashboard');
    await expect(page.locator('[data-testid=user-menu]')).toBeVisible();
});
```

## Related Documentation

### WitchCityRope Standards
- [Blazor Server Patterns](/docs/standards-processes/development-standards/blazor-server-patterns.md) - Blazor component patterns and authentication state management
- [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md) - Service layer implementation patterns for authentication services
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) - Database patterns for Identity and user management
- [Form Fields and Validation Standards](/docs/standards-processes/form-fields-and-validation-standards.md) - Login and registration form patterns

### Microsoft Documentation
- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/aspnet/core/security/authentication/identity)
- [Blazor Server Authentication](https://docs.microsoft.com/aspnet/core/blazor/security/server)
- [Cookie Authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication/cookie)

## Troubleshooting Checklist

- [ ] Using API endpoints, not direct SignInManager calls
- [ ] HttpClient configured with correct container URLs
- [ ] Authentication schemes properly configured in both projects
- [ ] Cookies configured for container communication
- [ ] Authentication state provider notifying state changes
- [ ] Proper error handling and logging implemented

## User Menu Component Pattern

### Current Implementation Status

The application currently uses a mixed approach in the MainNav component:
- **User Menu**: Implemented with dropdown functionality for authenticated users
- **Routes**: Still references legacy `/Identity/Account/Login` routes that need updating
- **Service Integration**: Uses `IAuthService` for user data retrieval

### ISSUE: Legacy Route References

**Current Problem**: MainNav component still uses outdated Identity routes:
```csharp
// ❌ CURRENT: Legacy Identity routes
<a href="/Identity/Account/Login" class="btn btn-secondary btn-sm">Sign In</a>
<a href="/Identity/Account/Register" class="btn btn-primary btn-sm">Join</a>
```

**Should be**: API-based authentication or updated routes:
```csharp
// ✅ CORRECT: Modern authentication patterns
<NavLink href="/login" class="btn btn-secondary btn-sm">Sign In</NavLink>
<NavLink href="/register" class="btn btn-primary btn-sm">Join</NavLink>
```

### User Menu Features (Currently Implemented)

The MainNav component includes:
- **User Avatar**: Displays user initials if no avatar image
- **Scene Name Display**: Shows user's scene name from `IAuthService.GetCurrentUserAsync()`
- **Dropdown Menu**: Includes Profile Settings, Vetting Status, Emergency Contacts, Sign Out
- **Admin Menu**: Role-based admin dropdown for administrators
- **Responsive Design**: Mobile-friendly hamburger menu

### Authentication State Handling

```csharp
// Current pattern in MainNav.razor
protected override async Task OnInitializedAsync()
{
    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    if (authState.User.Identity?.IsAuthenticated == true)
    {
        var user = await AuthService.GetCurrentUserAsync();
        userName = user?.SceneName ?? user?.DisplayName ?? "User";
        // Note: Avatar not implemented in current service
    }
    
    // Subscribe to authentication state changes
    AuthService.AuthenticationStateChanged += OnAuthenticationStateChanged;
}
```

### Logout Implementation (Current)

**Current implementation** uses direct service call:
```csharp
private async Task Logout()
{
    await AuthService.LogoutAsync();
    Navigation.NavigateTo("/", forceLoad: true);
}
```

This should follow the API endpoint pattern described above for consistency.

**Note**: See [Blazor Server Patterns](/docs/standards-processes/development-standards/blazor-server-patterns.md) for proper component authentication state handling patterns.

### User Menu Requirements Summary

Based on current implementation, user menus should:
1. **Display authenticated state** with avatar/initials and scene name
2. **Provide role-based navigation** (admin menus for administrators)
3. **Include essential user actions** (profile, settings, logout)
4. **Handle authentication state changes** dynamically
5. **Follow mobile-first responsive design**
6. **Use API endpoints** for all authentication operations (not currently fully implemented)