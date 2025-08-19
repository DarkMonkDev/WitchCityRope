# API Authentication Patterns - Extracted from Vertical Slice
<!-- Extracted: 2025-08-19 -->
<!-- Source: Vertical Slice Authentication Test (Complete) -->
<!-- Status: Production-Ready Patterns -->

## Overview

This document extracts the validated API authentication patterns from the successful vertical slice implementation. These patterns achieved 100% success rate with exceptional performance (94-98% faster than targets) and are ready for production implementation.

## Validated Architecture Pattern

### Hybrid JWT + HttpOnly Cookies Architecture
```
┌─────────────┐    HTTP/Cookies    ┌─────────────┐    JWT Bearer    ┌─────────────┐
│             │◄─────────────────► │             │◄──────────────► │             │
│   React     │                    │ Web Service │                 │ API Service │
│ Frontend    │                    │ (Auth+Proxy)│                 │ (Business)  │
│             │                    │             │                 │             │
└─────────────┘                    └─────────────┘                 └─────────────┘
     │                                   │                              │
     │ localhost:5173                    │ localhost:5651               │ localhost:5655
     │                                   │                              │
     └─── Cookie Authentication ─────────┘                              │
                                         └─── JWT Authentication ───────┘
```

### Service-to-Service Authentication Discovery
**CRITICAL FINDING**: During implementation, we discovered service-to-service authentication requirements between Docker containers that were missed in initial research. This discovery fundamentally changed our approach and resulted in significant cost savings.

- **Before**: NextAuth.js recommendation ($550+/month)
- **After**: Hybrid JWT + HttpOnly Cookies ($0/month)
- **Annual Savings**: $6,600+

## Working API Endpoints

### Authentication Endpoints (Web Service)

#### POST /api/auth/register
```json
// Request
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "sceneName": "TestUser"
}

// Response (201 Created)
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com", 
    "sceneName": "TestUser",
    "createdAt": "2025-08-16T10:30:00Z",
    "lastLoginAt": "2025-08-16T10:30:00Z"
  },
  "message": "Account created successfully"
}
```
**Side Effects**: Sets HttpOnly authentication cookie (30-day expiration), requests JWT token from API service

#### POST /api/auth/login
```json
// Request
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}

// Response (200 OK)
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "sceneName": "TestUser", 
    "createdAt": "2025-08-10T15:20:00Z",
    "lastLoginAt": "2025-08-16T10:30:00Z"
  },
  "message": "Login successful"
}
```
**Performance Achieved**: 56ms (94.4% faster than 1000ms target)

#### POST /api/auth/logout
```json
// Response (200 OK)
{
  "success": true,
  "message": "Logged out successfully"
}
```
**Performance Achieved**: 1ms (99.8% faster than 500ms target)

#### GET /api/auth/user
```json
// Response (200 OK)
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "sceneName": "TestUser",
    "createdAt": "2025-08-10T15:20:00Z",
    "lastLoginAt": "2025-08-16T10:30:00Z"
  }
}
```

### Protected API Endpoints (API Service)

#### GET /api/protected/welcome
```json
// Headers Required
{
  "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}

// Response (200 OK)
{
  "success": true,
  "data": {
    "message": "Welcome back, TestUser! You're successfully authenticated.",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "sceneName": "TestUser",
      "createdAt": "2025-08-10T15:20:00Z",
      "lastLoginAt": "2025-08-16T10:30:00Z"
    },
    "serverTime": "2025-08-16T10:30:00Z"
  }
}
```
**Performance Achieved**: 3ms (98.5% faster than 200ms target)

## JWT + HttpOnly Cookie Pattern

### HttpOnly Cookie Configuration
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "WitchCityRope.Auth";
    options.Cookie.HttpOnly = true;              // Prevents XSS attacks
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;          // CSRF protection
    options.ExpireTimeSpan = TimeSpan.FromDays(30);         // 30-day expiration
    options.SlidingExpiration = true;                       // Refresh on activity
    options.LoginPath = "/login";                           // Redirect path for React
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});
```

### JWT Claims Structure
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "scene_name": "TestUser",
  "jti": "4f8b2e1c-9a3d-4c7b-8e2f-1a5c9d7e3b6f",
  "iat": 1692180600,
  "exp": 1692184200,
  "iss": "WitchCityRope-API",
  "aud": "WitchCityRope-Services"
}
```

### JWT Service Implementation
```csharp
public class JwtService : IJwtService
{
    public JwtToken GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("scene_name", user.SceneName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "WitchCityRope-API",
            audience: "WitchCityRope-Services",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtToken
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = token.ValidTo
        };
    }
}
```

## ASP.NET Core Identity Configuration

### Identity Services Setup
```csharp
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
});
```

### User Entity Configuration
```csharp
public class User : IdentityUser<Guid>
{
    public string SceneName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public class AuthDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.SceneName)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => e.SceneName)
                .IsUnique();
        });
    }
}
```

## Service-to-Service Authentication Details

### Internal Service Token Endpoint
```csharp
// POST /api/auth/service-token (Internal Service-to-Service)
[HttpPost("service-token")]
public async Task<IActionResult> GenerateServiceToken([FromBody] ServiceTokenRequest request)
{
    var serviceSecret = Request.Headers["X-Service-Secret"].FirstOrDefault();
    if (serviceSecret != _configuration["ServiceAuth:Secret"])
    {
        return Unauthorized(new { error = "Invalid service credentials" });
    }

    var user = await _userManager.FindByIdAsync(request.UserId.ToString());
    if (user == null || user.Email != request.Email)
    {
        return NotFound(new { error = "User not found" });
    }

    var token = _jwtService.GenerateToken(user);
    return Ok(new
    {
        token = token.Token,
        expiresAt = token.ExpiresAt,
        user = new UserDto(user)
    });
}
```

### Service Secret Configuration
```json
{
  "ServiceAuth": {
    "Secret": "DevSecret-WitchCityRope-ServiceToService-Auth-2024!"
  }
}
```

## Performance Metrics Achieved

| Operation | Target | Actual | Improvement | Status |
|-----------|--------|--------|-------------|--------|
| Registration | 2000ms | 105ms | 94.75% faster | ✅ EXCEEDED |
| Login | 1000ms | 56ms | 94.4% faster | ✅ EXCEEDED |
| Protected API | 200ms | 3ms | 98.5% faster | ✅ EXCEEDED |
| Logout | 500ms | 1ms | 99.8% faster | ✅ EXCEEDED |
| Auth State Changes | 100ms | <50ms | >50% faster | ✅ EXCEEDED |

### Performance Optimization Factors
- ASP.NET Core Identity optimized queries
- Connection pooling in Entity Framework
- Minimal JWT payload size
- Efficient claim extraction
- Memory-only token storage (no localStorage)

## Security Validations Passed

### XSS Protection
- ✅ HttpOnly cookies prevent JavaScript access
- ✅ JWT tokens stored in memory only
- ✅ No localStorage/sessionStorage usage
- ✅ Verified through security validation suite

### CSRF Protection
- ✅ SameSite=Strict cookie policy
- ✅ CORS configured with credentials support
- ✅ Origin validation for development

### Authentication Security
- ✅ Password complexity enforced
- ✅ Account lockout after failed attempts
- ✅ Secure password hashing (PBKDF2)
- ✅ JWT token expiration (1 hour)
- ✅ Session management working correctly

## CORS Configuration for React

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment", builder =>
    {
        builder
            .WithOrigins("http://localhost:5173") // React dev server
            .AllowCredentials()                   // Required for cookies
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});
```

## Critical Implementation Lessons

### 1. JWT Claim Mapping Issues
**Problem**: JWT tokens created with `JwtRegisteredClaimNames.Sub` weren't being read by controllers expecting `ClaimTypes.NameIdentifier`.

**Solution**: Modified controllers to check both claim types:
```csharp
var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

### 2. API Response Structure
**Problem**: React expected flat response structure, API returned nested structure.

**Solution**: Modified React service to handle both:
```typescript
const authData = data.data || data;
```

### 3. Memory-Only Authentication State
**Pattern**: Store authentication state in React Context/Zustand, never in localStorage
```typescript
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
```

## Cost Analysis

### Chosen Solution: $0/month
- ASP.NET Core Identity: Free
- PostgreSQL: Self-hosted
- JWT libraries: Open source
- No third-party services

### Alternatives Considered:
- Auth0: $550+/month for our requirements
- Firebase Auth: $300+/month with our user count
- Clerk: $250+/month for features needed
- NextAuth.js: Would require additional infrastructure

**Annual Savings**: $6,600+ compared to commercial alternatives

## Production Deployment Checklist

### Security Configuration
- [ ] Replace development JWT secret with production secret (256-bit minimum)
- [ ] Configure production CORS origins (remove localhost)
- [ ] Set up HTTPS enforcement and security headers
- [ ] Implement rate limiting on authentication endpoints
- [ ] Configure production database connection with SSL

### Monitoring Setup
- [ ] Track authentication success/failure rates
- [ ] Monitor JWT token expiration patterns
- [ ] Alert on unusual login patterns
- [ ] Log all authentication events
- [ ] Track API response times

### Scaling Preparation
- [ ] Redis for distributed session management
- [ ] JWT token refresh strategy
- [ ] Database connection pooling optimization
- [ ] Load balancer session affinity

## Ready for Production

✅ **Authentication pattern validated**  
✅ **Security measures tested and passed**  
✅ **Performance benchmarks exceeded by 94-98%**  
✅ **Complete documentation available**  
✅ **Service-to-service authentication proven**  
✅ **Cost-effective solution ($0/month vs $550+/month)**  
✅ **Deployment checklist ready**  

## Source Documentation

- **Completion Summary**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/AUTHENTICATION-VERTICAL-SLICE-COMPLETE.md`
- **Functional Specification**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/requirements/functional-specification.md`
- **API Design**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/design/api-design.md`
- **Implementation Lessons**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/lessons-learned/authentication-implementation-lessons.md`
- **Production Checklist**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/production-deployment-checklist.md`

---
*Extracted from successful vertical slice implementation (2025-08-16) with 100% success rate and exceptional performance results.*