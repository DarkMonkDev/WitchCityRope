# Authentication System - Functional Design
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Overview
The WitchCityRope authentication system uses a hybrid approach with cookie-based authentication for the Blazor Server web application and JWT tokens for the REST API. This document describes the technical implementation details.

## Architecture Overview

### Authentication Flow Diagram
```
┌─────────────┐     ┌─────────────────┐     ┌──────────────┐
│   Browser   │────▶│  Blazor Server  │────▶│ Razor Pages  │
│             │◀────│  (Web Project)  │◀────│  (Identity)  │
└─────────────┘     └─────────────────┘     └──────────────┘
                            │                        │
                            ▼                        ▼
                    ┌─────────────────┐     ┌──────────────┐
                    │   API Project   │────▶│  PostgreSQL  │
                    │  (JWT Auth)     │◀────│   Database   │
                    └─────────────────┘     └──────────────┘
```

## Critical Design Decision: Blazor Server Authentication Pattern

### ⚠️ The SignInManager Problem
**Issue**: Using SignInManager directly in Blazor Server components causes "Headers are read-only, response has already started" errors.

**Root Cause**: Blazor Server components run in a different context than traditional HTTP request/response. SignInManager tries to modify HTTP headers (to set cookies) after the response has started streaming to the client.

**Solution**: Redirect to Razor Pages for all authentication operations:
```csharp
// In Blazor component
NavigationManager.NavigateTo("/Identity/Account/Login", true);

// Razor Page handles actual authentication
public async Task<IActionResult> OnPostAsync()
{
    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
    // This works because we're in traditional HTTP context
}
```

## Component Architecture

### Web Project (Blazor Server)

#### Authentication Components
- `/Features/Auth/Pages/Login.razor` - Redirects to Razor Page
- `/Features/Auth/Pages/Register.razor` - Redirects to Razor Page  
- `/Areas/Identity/Pages/Account/Login.cshtml` - Actual login handling
- `/Areas/Identity/Pages/Account/Register.cshtml` - Actual registration

#### Services
```csharp
// IAuthService - Interface for auth operations
public interface IAuthService
{
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsInRoleAsync(string role);
    Task LogoutAsync();
    // 2FA methods (not implemented)
}

// IdentityAuthService - Implementation
public class IdentityAuthService : IAuthService
{
    // Uses IHttpContextAccessor to check authentication state
    // Does NOT use SignInManager directly
}
```

#### Configuration (Program.cs)
```csharp
// Identity configuration
builder.Services.AddIdentityCore<WitchCityRopeUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedEmail = false; // Email verification not enforced
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});
```

### API Project (REST API)

#### JWT Authentication
```csharp
// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });
```

#### Auth Endpoints
- `POST /api/auth/login` - Returns JWT and refresh token
- `POST /api/auth/register` - Creates new user
- `POST /api/auth/refresh` - Refreshes expired token
- `POST /api/auth/verify-email` - Email verification
- `POST /api/auth/logout` - Client-side token removal

#### JWT Service
```csharp
public interface IJwtService
{
    string GenerateToken(WitchCityRopeUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
}
```

## Database Schema

### Identity Tables (auth schema)
- `AspNetUsers` - User accounts
- `AspNetRoles` - System roles
- `AspNetUserRoles` - User-role mappings
- `AspNetUserClaims` - Additional claims
- `AspNetUserLogins` - External login providers (not used)
- `AspNetUserTokens` - Token storage

### Custom User Properties
```csharp
public class WitchCityRopeUser : IdentityUser<Guid>
{
    public string SceneName { get; set; }  // Unique display name
    public string? LegalName { get; set; } // Encrypted
    public DateTime DateOfBirth { get; set; }
    public bool IsVetted { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Refresh Tokens (for API)
```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

## Authorization Policies

### Role-Based Policies
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", 
        policy => policy.RequireRole("Administrator"));
    
    options.AddPolicy("RequireEventOrganizerRole", 
        policy => policy.RequireRole("Administrator", "EventOrganizer"));
    
    options.AddPolicy("RequireVettedMember", 
        policy => policy.RequireAssertion(context =>
            context.User.Identity.IsAuthenticated &&
            context.User.HasClaim("IsVetted", "true")));
});
```

### Usage in Components
```razor
<AuthorizeView Policy="RequireAdminRole">
    <Authorized>
        <!-- Admin-only content -->
    </Authorized>
    <NotAuthorized>
        <p>You need admin access.</p>
    </NotAuthorized>
</AuthorizeView>
```

## Security Implementation

### Password Hashing
- Uses ASP.NET Core Identity's default hasher (PBKDF2)
- Automatic salt generation
- Work factor adjusts with hardware improvements

### Account Protection
- Lockout after 5 failed attempts
- 15-minute lockout duration
- Tracking of access failed count

### Session Security
- Secure, HttpOnly cookies in production
- CSRF protection via antiforgery tokens
- SameSite cookie policy

### API Security
- JWT tokens expire after configured duration
- Refresh tokens for seamless re-authentication
- Token revocation capability

## Integration Points

### Email Service
```csharp
public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
```
- Used for email verification (when enabled)
- Password reset functionality (not fully implemented)

### User Context Service
```csharp
public interface IUserContext
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
}
```
- Provides current user info to services
- Different implementations for Web and API

## Error Handling

### Login Failures
- Generic "Invalid login attempt" for security
- Specific messages only for lockout
- Logging of failed attempts for monitoring

### Registration Errors
- Clear messages for validation failures
- Duplicate email/username detection
- Age verification with specific error

## Performance Considerations

### Caching
- User claims cached for request duration
- Role checks cached per request
- No long-term auth caching (security)

### Database Queries
- Eager loading of roles when needed
- Indexed on Email and UserName
- Async operations throughout

## Future Enhancements (Not Implemented)

### Two-Factor Authentication
- Database fields exist
- UI components created
- Service methods return "not implemented"
- Requires SMS/Email provider setup

### OAuth Integration
- Google login button exists
- No backend implementation
- Requires OAuth app configuration

### Password Reset
- Partial implementation exists
- Email sending not configured
- Token generation ready

---

*This document describes the current technical implementation. For business requirements, see [business-requirements.md](business-requirements.md)*