# Service Communication Flow Design - Containerized Authentication Architecture
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Overview

This document visualizes how the proven WitchCityRope authentication flow (hybrid JWT + HttpOnly Cookies) operates within Docker containers, detailing service-to-service communication patterns and container networking.

## Container-Based Authentication Flow

### High-Level Service Communication
```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  User Browser   │         │ Docker Network  │         │ Database Layer  │
│                 │         │  (witchcity-net) │         │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
         │                           │                           │
         │ HTTP/Cookie Auth          │ Container Communication   │ Database Queries
         ▼                           ▼                           ▼
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  react-web      │  JWT    │  api-service    │  SQL    │  postgres-db    │
│  Container      │ Bearer  │  Container      │ Query   │  Container      │
│  Port: 5173     │ ◄──────►│  Port: 8080     │ ◄──────►│  Port: 5432     │
└─────────────────┘         └─────────────────┘         └─────────────────┘
```

## Detailed Authentication Flow Sequence

### 1. User Registration Flow
```
User Browser                react-web           api-service          postgres-db
     │                      (5173)              (8080)               (5432)
     │                         │                   │                    │
     │ 1. POST /register       │                   │                    │
     │────────────────────────►│                   │                    │
     │   (credentials)         │                   │                    │
     │                         │ 2. POST /api/auth/register             │
     │                         │──────────────────►│                    │
     │                         │   (user data)     │                    │
     │                         │                   │ 3. INSERT user     │
     │                         │                   │───────────────────►│
     │                         │                   │                    │
     │                         │                   │ 4. User created    │
     │                         │                   │◄───────────────────│
     │                         │ 5. 201 Created    │                    │
     │                         │◄──────────────────│                    │
     │ 6. Registration success │                   │                    │
     │◄────────────────────────│                   │                    │
```

**Container Communication Details**:
- **Step 1**: Browser → `react-web:5173` (external port mapping)
- **Step 2**: `react-web` → `api-service:8080` (internal container network)
- **Step 3**: `api-service` → `postgres-db:5432` (internal container network)

### 2. User Login Flow with Cookie + JWT Pattern
```
User Browser                react-web           api-service          postgres-db
     │                      (5173)              (8080)               (5432)
     │                         │                   │                    │
     │ 1. POST /login          │                   │                    │
     │────────────────────────►│                   │                    │
     │   (email/password)      │                   │                    │
     │                         │ 2. POST /api/auth/login                │
     │                         │──────────────────►│                    │
     │                         │   (credentials)   │                    │
     │                         │                   │ 3. Validate user   │
     │                         │                   │───────────────────►│
     │                         │                   │                    │
     │                         │                   │ 4. User + roles    │
     │                         │                   │◄───────────────────│
     │                         │                   │ 5. Generate JWT    │
     │                         │                   │    for services    │
     │                         │ 6. Set-Cookie +   │                    │
     │                         │    JWT token      │                    │
     │                         │◄──────────────────│                    │
     │ 7. Cookie stored +      │                   │                    │
     │    Auth context updated │                   │                    │
     │◄────────────────────────│                   │                    │
```

**Authentication Tokens**:
- **Cookie**: HttpOnly, Secure, SameSite for browser authentication
- **JWT**: Bearer token for service-to-service API calls

### 3. Protected Resource Access Flow
```
User Browser                react-web           api-service          postgres-db
     │                      (5173)              (8080)               (5432)
     │                         │                   │                    │
     │ 1. GET /events          │                   │                    │
     │────────────────────────►│                   │                    │
     │   (cookies included)    │                   │                    │
     │                         │ 2. Verify cookie  │                    │
     │                         │    auth status    │                    │
     │                         │ 3. GET /api/events                     │
     │                         │──────────────────►│                    │
     │                         │   Authorization:  │                    │
     │                         │   Bearer <JWT>    │                    │
     │                         │                   │ 4. Validate JWT    │
     │                         │                   │    + check roles   │
     │                         │                   │ 5. Query events    │
     │                         │                   │───────────────────►│
     │                         │                   │                    │
     │                         │                   │ 6. Event data      │
     │                         │                   │◄───────────────────│
     │                         │ 7. JSON response  │                    │
     │                         │◄──────────────────│                    │
     │ 8. Render events page   │                   │                    │
     │◄────────────────────────│                   │                    │
```

## Container Networking Architecture

### Docker Network Configuration
```yaml
# Network: witchcity-net (172.20.0.0/16)
networks:
  witchcity-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

### Container IP Assignments
```
Container Name    │ Internal IP   │ Service Name     │ Accessible Via
─────────────────┼──────────────┼─────────────────┼─────────────────
react-web        │ 172.20.0.10  │ react-web       │ react-web:5173
api-service      │ 172.20.0.20  │ api-service     │ api-service:8080  
postgres-db      │ 172.20.0.30  │ postgres-db     │ postgres-db:5432
test-runner      │ 172.20.0.40  │ test-runner     │ test-runner:3000
```

### Service Discovery Pattern
```javascript
// React container configuration
const API_BASE_URL = process.env.NODE_ENV === 'development' 
  ? 'http://localhost:5655'        // External access for browser
  : 'http://api-service:8080';     // Container-to-container

// API container database connection
const connectionString = `Host=postgres-db;Database=witchcityrope;Username=postgres;Password=${process.env.POSTGRES_PASSWORD}`;
```

## Authentication Service Communication Details

### 1. React Container (`react-web`) Responsibilities
```typescript
// Authentication Context in React Container
interface AuthContext {
  // Cookie-based authentication state
  isAuthenticated: boolean;
  user: User | null;
  roles: string[];
  
  // Authentication methods
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  checkAuthStatus: () => Promise<void>;
}

// API Client with JWT handling
class ApiClient {
  private baseURL = process.env.VITE_API_URL; // Points to api-service
  
  async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${this.baseURL}${endpoint}`, {
      ...options,
      credentials: 'include',  // Include cookies for auth
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });
    
    if (response.status === 401) {
      // Handle unauthorized - redirect to login
      this.authContext.logout();
    }
    
    return response.json();
  }
}
```

### 2. API Container (`api-service`) Responsibilities
```csharp
// Service-to-Service Authentication Handler
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // For internal service calls within containers
        var token = await _jwtTokenService.GetServiceTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return await base.SendAsync(request, cancellationToken);
    }
}

// JWT Service for container communication
public class JwtTokenService
{
    public async Task<string> GenerateServiceTokenAsync(string userId, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new("iss", "http://api-service:8080"),      // Container issuer
            new("aud", "witchcity-react"),              // Audience
        };
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        // Token valid for container-to-container calls
        var token = new JwtSecurityToken(
            issuer: "http://api-service:8080",
            audience: "witchcity-react", 
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),    // Short-lived for security
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 3. Database Container (`postgres-db`) Communication
```sql
-- Authentication schema in PostgreSQL container
-- Tables for ASP.NET Core Identity
CREATE SCHEMA auth;

-- User authentication
CREATE TABLE auth.AspNetUsers (
    Id varchar(450) PRIMARY KEY,
    UserName varchar(256),
    NormalizedUserName varchar(256),
    Email varchar(256),
    NormalizedEmail varchar(256),
    EmailConfirmed boolean,
    PasswordHash text,
    -- ... other Identity fields
);

-- Role management
CREATE TABLE auth.AspNetRoles (
    Id varchar(450) PRIMARY KEY,
    Name varchar(256),
    NormalizedName varchar(256)
);

-- User-Role assignments
CREATE TABLE auth.AspNetUserRoles (
    UserId varchar(450) REFERENCES auth.AspNetUsers(Id),
    RoleId varchar(450) REFERENCES auth.AspNetRoles(Id),
    PRIMARY KEY (UserId, RoleId)
);
```

## Container Environment Configuration

### Environment Variables for Service Communication
```yaml
# React Container Environment
react-web:
  environment:
    - NODE_ENV=development
    - VITE_API_URL=http://api-service:8080    # Internal container URL
    - VITE_APP_NAME=WitchCityRope

# API Container Environment  
api-service:
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:8080
    - ConnectionStrings__DefaultConnection=Host=postgres-db;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
    - Authentication__JwtSecret=${JWT_SECRET}
    - Authentication__Issuer=http://api-service:8080
    - Authentication__Audience=witchcity-react
    - CORS__AllowedOrigins=http://react-web:5173

# PostgreSQL Container Environment
postgres-db:
  environment:
    - POSTGRES_DB=witchcityrope
    - POSTGRES_USER=postgres
    - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
```

## Health Check and Service Dependencies

### Service Startup Sequence
```yaml
# Dependency chain managed by Docker Compose
depends_on:
  postgres-db:
    condition: service_healthy    # Database must be ready
  api-service:
    condition: service_healthy    # API needs database
  react-web:
    condition: service_started    # React can start when API is healthy
```

### Health Check Endpoints
```yaml
# PostgreSQL Health Check
postgres-db:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope"]
    interval: 5s
    timeout: 5s
    retries: 5

# API Health Check
api-service:
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
    interval: 10s
    timeout: 5s
    retries: 3
    
# React Health Check
react-web:
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:5173 || exit 1"]
    interval: 10s
    timeout: 5s
    retries: 3
```

## Error Handling and Resilience

### Authentication Failure Scenarios
```typescript
// React Container Error Handling
class AuthenticationError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

// Handle container communication failures
async function handleApiCall<T>(apiCall: () => Promise<T>): Promise<T> {
  try {
    return await apiCall();
  } catch (error) {
    if (error.status === 401) {
      // Authentication failure - clear local state
      authContext.logout();
      router.navigate('/login');
    } else if (error.status === 503) {
      // Service unavailable - show maintenance message
      toast.error('Service temporarily unavailable');
    } else {
      // Network error - could be container connectivity issue
      toast.error('Unable to connect to service');
    }
    throw error;
  }
}
```

### Service Communication Resilience
```csharp
// API Container Resilience Patterns
public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientHttpClient> _logger;
    
    public async Task<T> GetAsync<T>(string endpoint)
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for {Endpoint} in {Delay}ms", 
                        retryCount, endpoint, timespan.TotalMilliseconds);
                });
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        });
    }
}
```

## Security Considerations in Container Communication

### Network Security
```yaml
# Secure container networking
networks:
  witchcity-net:
    driver: bridge
    internal: false              # Allow external access for web services
    ipam:
      config:
        - subnet: 172.20.0.0/16  # Private subnet for container communication
```

### Secret Management
```yaml
# Environment variables for sensitive data
services:
  api-service:
    environment:
      - JWT_SECRET=${JWT_SECRET}                    # From .env file
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}     # From .env file
    secrets:
      - jwt_signing_key
      - postgres_password

secrets:
  jwt_signing_key:
    external: true
  postgres_password:
    external: true
```

### Inter-Service Authentication
```csharp
// Service-to-service authentication with shared secret
public class ServiceAuthenticationMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Verify internal service calls
        if (context.Request.Path.StartsWithSegments("/api/internal"))
        {
            var serviceSecret = context.Request.Headers["X-Service-Secret"];
            if (serviceSecret != _configuration["ServiceSecret"])
            {
                context.Response.StatusCode = 401;
                return;
            }
        }
        
        await next(context);
    }
}
```

This containerized service communication design ensures that the proven authentication pattern continues to work seamlessly within Docker containers while maintaining security, performance, and reliability standards. The architecture supports both development and production scenarios with appropriate configuration for each environment.