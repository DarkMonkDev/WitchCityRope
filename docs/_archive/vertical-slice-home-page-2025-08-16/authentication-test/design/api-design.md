# API Design Specification: Authentication Vertical Slice Test
<!-- Last Updated: 2025-08-16 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Design Phase -->

## Overview

This document defines the API design specification for the authentication vertical slice test that validates the Hybrid JWT + HttpOnly Cookies pattern for WitchCityRope React migration. This throwaway implementation proves the authentication flow works correctly before committing to full production implementation.

## Architecture Overview

### Service Communication Pattern
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

### Authentication Flow Design
1. **React → Web Service**: HttpOnly cookie authentication
2. **Web Service → API Service**: JWT Bearer token authentication
3. **Web Service Internal**: ASP.NET Core Identity user management
4. **API Service Internal**: JWT token validation and business logic

## 1. Authentication Flow Design

### 1.1 User Registration Flow
```mermaid
sequenceDiagram
    participant R as React App
    participant W as Web Service
    participant A as API Service
    participant DB as PostgreSQL

    R->>W: POST /api/auth/register
    W->>W: Validate input (ASP.NET Identity)
    W->>DB: Create user account
    W->>W: Generate HttpOnly cookie
    W->>A: Request JWT token (service-to-service)
    A->>A: Generate JWT for user
    A-->>W: Return JWT token
    W->>R: Set cookie + return user data
    Note over R,W: User now authenticated with cookie
```

### 1.2 User Login Flow
```mermaid
sequenceDiagram
    participant R as React App
    participant W as Web Service
    participant A as API Service

    R->>W: POST /api/auth/login
    W->>W: Validate credentials (ASP.NET Identity)
    W->>W: Generate HttpOnly cookie
    W->>A: Request JWT token (service-to-service)
    A->>A: Generate JWT for user
    A-->>W: Return JWT token
    W->>R: Set cookie + return user data
    Note over R,W: User authenticated for 30 days
```

### 1.3 Protected API Access Flow
```mermaid
sequenceDiagram
    participant R as React App
    participant W as Web Service
    participant A as API Service

    R->>W: GET /api/protected/welcome (with cookie)
    W->>W: Validate cookie authentication
    W->>A: GET /api/protected/welcome (with JWT)
    A->>A: Validate JWT token
    A-->>W: Return protected data
    W-->>R: Proxy response
    Note over R,A: Service-to-service JWT authentication
```

### 1.4 Logout Flow
```mermaid
sequenceDiagram
    participant R as React App
    participant W as Web Service

    R->>W: POST /api/auth/logout
    W->>W: Clear HttpOnly cookie
    W->>W: Clear server-side JWT cache
    W-->>R: Success response
    Note over R,W: User logged out completely
```

## 2. API Endpoints Specification

### 2.1 Web Service Endpoints (Authentication Layer)

#### POST /api/auth/register
**Purpose**: Create new user account with ASP.NET Core Identity

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "sceneName": "TestUser"
}
```

**Response (201 Created)**:
```json
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

**Side Effects**:
- Sets HttpOnly authentication cookie (30-day expiration)
- Requests JWT token from API service for immediate use
- Stores JWT in server-side memory cache

**Error Responses**:
```json
// 400 Bad Request - Validation failure
{
  "success": false,
  "error": "Email already exists",
  "details": "An account with this email address already exists"
}

// 400 Bad Request - Password requirements
{
  "success": false,
  "error": "Password requirements not met",
  "details": "Password must be at least 8 characters with uppercase, lowercase, and number"
}
```

#### POST /api/auth/login
**Purpose**: Authenticate user and establish session

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK)**:
```json
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

**Side Effects**:
- Sets HttpOnly authentication cookie
- Updates last login timestamp
- Requests and caches JWT token for API service calls

**Error Responses**:
```json
// 401 Unauthorized
{
  "success": false,
  "error": "Invalid credentials",
  "details": "Email or password is incorrect"
}

// 403 Forbidden - Account locked
{
  "success": false,
  "error": "Account locked",
  "details": "Too many failed login attempts. Try again in 15 minutes."
}
```

#### POST /api/auth/logout
**Purpose**: Clear authentication session

**Request**: None (authentication via cookie)

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

**Side Effects**:
- Clears HttpOnly authentication cookie
- Removes JWT token from server cache
- Invalidates session data

#### GET /api/auth/user
**Purpose**: Get current authenticated user information

**Request**: None (authentication via cookie)

**Response (200 OK)**:
```json
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

**Error Responses**:
```json
// 401 Unauthorized
{
  "success": false,
  "error": "Not authenticated",
  "details": "Please log in to access this resource"
}
```

### 2.2 API Service Endpoints (Business Logic Layer)

#### GET /api/protected/welcome
**Purpose**: Return personalized welcome message for authenticated users

**Request**: None (authentication via JWT Bearer token)

**Headers Required**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK)**:
```json
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

**Error Responses**:
```json
// 401 Unauthorized - Missing token
{
  "success": false,
  "error": "Authorization token required",
  "details": "JWT Bearer token must be provided in Authorization header"
}

// 401 Unauthorized - Invalid token
{
  "success": false,
  "error": "Invalid token",
  "details": "JWT token is expired or malformed"
}

// 403 Forbidden - Valid token but insufficient permissions
{
  "success": false,
  "error": "Insufficient permissions",
  "details": "User does not have access to this resource"
}
```

#### POST /api/auth/service-token (Internal Service-to-Service)
**Purpose**: Generate JWT token for authenticated user (Web Service → API Service communication)

**Headers Required**:
```
X-Service-Secret: DevSecret-WitchCityRope-ServiceToService-Auth-2024!
Content-Type: application/json
```

**Request**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com"
}
```

**Response (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-08-16T11:30:00Z",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "sceneName": "TestUser"
  }
}
```

**Error Responses**:
```json
// 401 Unauthorized - Invalid service secret
{
  "success": false,
  "error": "Invalid service credentials",
  "details": "Service secret is missing or incorrect"
}

// 404 Not Found - User doesn't exist
{
  "success": false,
  "error": "User not found",
  "details": "No user found with the provided ID and email"
}
```

## 3. Cookie Configuration

### 3.1 HttpOnly Cookie Settings
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

### 3.2 CORS Configuration for React Development
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

### 3.3 Cookie Security Headers
```csharp
// Additional security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

## 4. JWT Token Design

### 4.1 JWT Claims Structure
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

### 4.2 JWT Configuration
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "WitchCityRope-API",
            ValidAudience = "WitchCityRope-Services",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
```

### 4.3 JWT Token Generation Service
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

## 5. Database Design

### 5.1 Users Table Schema
```sql
-- Users table for authentication test
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email" VARCHAR(254) NOT NULL UNIQUE,
    "NormalizedEmail" VARCHAR(254) NOT NULL UNIQUE,
    "UserName" VARCHAR(254) NOT NULL,
    "NormalizedUserName" VARCHAR(254) NOT NULL,
    "SceneName" VARCHAR(50) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "SecurityStamp" TEXT NOT NULL,
    "ConcurrencyStamp" TEXT NOT NULL,
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT TRUE,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ NULL,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastLoginAt" TIMESTAMPTZ NULL,
    
    -- Constraints
    CONSTRAINT "CK_Users_Email_Format" CHECK ("Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'),
    CONSTRAINT "CK_Users_SceneName_Length" CHECK (LENGTH("SceneName") >= 2)
);

-- Indexes for performance
CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");
CREATE INDEX "IX_Users_SceneName" ON "Users" ("SceneName");
CREATE INDEX "IX_Users_UserName" ON "Users" ("UserName");
CREATE INDEX "IX_Users_NormalizedUserName" ON "Users" ("NormalizedUserName");
```

### 5.2 EF Core Entity Configuration
```csharp
public class User : IdentityUser<Guid>
{
    public string SceneName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public class AuthDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.SceneName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.LastLoginAt)
                .HasColumnType("timestamptz");

            entity.HasIndex(e => e.SceneName)
                .IsUnique();
        });
    }
}
```

### 5.3 Database Connection Configuration
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
    
    // Enable sensitive data logging in development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

## 6. Security Measures

### 6.1 Password Security
```csharp
// ASP.NET Core Identity password requirements
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
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});
```

### 6.2 CSRF Protection
```csharp
// CSRF protection for cookie-based authentication
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// Add CSRF token to responses for AJAX calls
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth"))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Headers.Add("X-CSRF-TOKEN", tokens.RequestToken);
    }
    await next();
});
```

### 6.3 Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthApi", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;          // 10 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1); // Per minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});
```

### 6.4 Input Validation
```csharp
// FluentValidation for authentication requests
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email address is required")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage(
                "Password must contain uppercase, lowercase, and number");

        RuleFor(x => x.SceneName)
            .NotEmpty().WithMessage("Scene name is required")
            .Length(2, 50).WithMessage("Scene name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage(
                "Scene name can only contain letters, numbers, underscores, and hyphens");
    }
}
```

## 7. Error Handling

### 7.1 Global Error Response Format
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = Activity.Current?.Id ?? Guid.NewGuid().ToString();
}
```

### 7.2 Exception Handling Middleware
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Error = "An error occurred while processing your request",
            Details = "Please try again later or contact support if the problem persists"
        };

        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            _ => 500
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 7.3 Authentication-Specific Error Responses
```csharp
// Custom exception types for authentication
public class AuthenticationException : Exception
{
    public string ErrorCode { get; }
    
    public AuthenticationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

// Service layer error handling
public async Task<Result<UserDto>> LoginAsync(LoginRequest request)
{
    try
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email, 
            request.Password, 
            isPersistent: true, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            return Result<UserDto>.Success(new UserDto(user));
        }

        if (result.IsLockedOut)
        {
            return Result<UserDto>.Failure("Account is temporarily locked due to failed login attempts");
        }

        return Result<UserDto>.Failure("Invalid email or password");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed for {Email}", request.Email);
        return Result<UserDto>.Failure("Login could not be completed at this time");
    }
}
```

## 8. Implementation Checklist

### 8.1 Backend Implementation Requirements
- [ ] **ASP.NET Core Identity Setup**
  - [ ] Configure Identity services with PostgreSQL
  - [ ] Configure cookie authentication settings
  - [ ] Set up password requirements and lockout policies
  - [ ] Create User entity with SceneName property
  - [ ] Configure EF Core DbContext with Identity

- [ ] **JWT Service Implementation**
  - [ ] Create IJwtService interface and JwtService implementation
  - [ ] Configure JWT authentication in API service
  - [ ] Implement token generation with proper claims
  - [ ] Set up token validation parameters
  - [ ] Create service-to-service token endpoint

- [ ] **Authentication API Endpoints**
  - [ ] POST /api/auth/register - User registration
  - [ ] POST /api/auth/login - User login with cookie generation
  - [ ] POST /api/auth/logout - Clear authentication state
  - [ ] GET /api/auth/user - Get current user information

- [ ] **Protected API Endpoints**
  - [ ] GET /api/protected/welcome - JWT-protected welcome endpoint
  - [ ] JWT Bearer token authentication middleware
  - [ ] Service-to-service authentication bridge

- [ ] **Security Configuration**
  - [ ] HTTPS enforcement and security headers
  - [ ] CORS configuration for React development server
  - [ ] Rate limiting for authentication endpoints
  - [ ] Input validation with FluentValidation
  - [ ] Global exception handling middleware

### 8.2 Database Implementation Requirements
- [ ] **PostgreSQL Schema**
  - [ ] Create Users table with ASP.NET Identity columns
  - [ ] Add SceneName unique constraint and indexes
  - [ ] Set up proper column types (timestamptz for dates)
  - [ ] Create EF Core migration for table creation

- [ ] **Connection Configuration**
  - [ ] Configure connection string with pooling
  - [ ] Set up retry policies for connection resilience
  - [ ] Enable sensitive data logging for development only

### 8.3 Testing Requirements
- [ ] **API Testing**
  - [ ] Test registration endpoint with valid/invalid data
  - [ ] Test login endpoint with correct/incorrect credentials
  - [ ] Test logout endpoint clears authentication state
  - [ ] Test protected endpoint requires valid JWT token
  - [ ] Test service-to-service token generation

- [ ] **Security Testing**
  - [ ] Verify HttpOnly cookies prevent XSS access
  - [ ] Test CSRF protection for state-changing operations
  - [ ] Validate JWT token expiration and refresh
  - [ ] Test rate limiting prevents brute force attacks
  - [ ] Verify input validation prevents injection attacks

- [ ] **Integration Testing**
  - [ ] Test complete registration → login → protected access flow
  - [ ] Verify cookie persistence across browser sessions
  - [ ] Test JWT token automatic refresh for long sessions
  - [ ] Validate service-to-service authentication bridge

## 9. Configuration Examples

### 9.1 Development Environment Configuration
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=witchcityrope_auth_test;Username=postgres;Password=WitchCity2024!"
  },
  "Jwt": {
    "SecretKey": "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!",
    "Issuer": "WitchCityRope-API",
    "Audience": "WitchCityRope-Services",
    "ExpirationMinutes": 60
  },
  "ServiceAuth": {
    "Secret": "DevSecret-WitchCityRope-ServiceToService-Auth-2024!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 9.2 React Environment Configuration
```typescript
// Environment configuration for React client
export const authConfig = {
  baseUrl: 'http://localhost:5651', // Web Service URL
  endpoints: {
    register: '/api/auth/register',
    login: '/api/auth/login',
    logout: '/api/auth/logout',
    user: '/api/auth/user',
    protectedWelcome: '/api/protected/welcome'
  },
  fetchOptions: {
    credentials: 'include' as RequestCredentials, // Include cookies
    headers: {
      'Content-Type': 'application/json'
    }
  }
};
```

## 10. Success Criteria

### 10.1 Functional Success Criteria
- [ ] User can register with email/password/scene name
- [ ] User can login and receive HttpOnly authentication cookie
- [ ] Protected welcome page accessible only when authenticated  
- [ ] Authentication state persists across browser refreshes
- [ ] User can logout and authentication state is cleared
- [ ] Web Service → API Service JWT authentication works
- [ ] Error handling provides clear, actionable feedback

### 10.2 Security Success Criteria
- [ ] HttpOnly cookies prevent XSS access to tokens
- [ ] CORS configuration allows React dev server with credentials
- [ ] JWT tokens have proper expiration and validation
- [ ] Service-to-service authentication requires shared secret
- [ ] Rate limiting prevents authentication brute force
- [ ] Input validation prevents injection attacks
- [ ] All authentication endpoints use HTTPS in production

### 10.3 Performance Success Criteria
- [ ] Registration completes in < 2000ms
- [ ] Login completes in < 1000ms
- [ ] Protected API calls complete in < 200ms
- [ ] Authentication state changes complete in < 100ms
- [ ] JWT token generation completes in < 50ms

## 11. Cleanup and Next Steps

### 11.1 Post-POC Cleanup
After successful validation of the authentication pattern:
1. Document lessons learned in backend-lessons-learned.md
2. Archive throwaway components not suitable for production
3. Update authentication patterns documentation
4. Plan production-quality implementation based on POC results

### 11.2 Production Implementation Considerations
- Replace development secrets with secure secret management
- Add comprehensive audit logging for security monitoring
- Implement token refresh mechanisms for long-running sessions
- Add support for OAuth providers (Google, Facebook)
- Scale JWT token caching with distributed cache (Redis)
- Add comprehensive monitoring and alerting for authentication failures

## 12. Related Documentation

- **Functional Specification**: `/docs/functional-areas/vertical-slice-home-page/authentication-test/requirements/functional-specification.md`
- **Authentication Decision**: `/docs/architecture/react-migration/AUTHENTICATION-DECISION-FINAL.md`
- **JWT Service Documentation**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`
- **Backend Lessons Learned**: `/docs/lessons-learned/backend-lessons-learned.md`
- **Coding Standards**: `/docs/standards-processes/CODING_STANDARDS.md`

---

**⚠️ Important**: This is throwaway code designed to validate the authentication pattern. Focus on proving the concept works correctly rather than production-quality implementation. The goal is to validate the Hybrid JWT + HttpOnly Cookies approach before committing to full implementation.