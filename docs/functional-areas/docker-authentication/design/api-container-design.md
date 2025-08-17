# .NET API Container Design Specification

## Overview

This document provides comprehensive design specifications for containerizing the WitchCityRope .NET 9 Minimal API with multi-stage Docker builds, hot reload development support, and production optimization. The design preserves existing JWT + HttpOnly cookie authentication while enabling efficient containerized development and secure production deployment.

## Current State Analysis

**Working API Configuration:**
- .NET 9 Minimal API running on localhost:5655
- JWT authentication with service-to-service patterns
- HttpOnly cookies for user authentication bridge
- ASP.NET Core Identity with PostgreSQL
- Entity Framework Core 9 database access
- Health checks and CORS configuration

**Container Requirements:**
- Preserve hot reload for development efficiency
- Multi-stage builds for production optimization
- Database connectivity with proper connection strings
- Authentication functionality across container boundaries
- Service dependency management and health checking

## 1. Multi-Stage Dockerfile Design

### Development Stage Configuration

```dockerfile
# Development stage - Full SDK with hot reload support
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development

# Create app directory
WORKDIR /app

# Install additional tools for debugging
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Copy project file first for dependency caching
COPY *.csproj ./

# Restore dependencies with retries for network issues
RUN dotnet restore --verbosity normal

# Copy source code
COPY . ./

# Expose ports for API and debugging
EXPOSE 5655
EXPOSE 5656

# Configure hot reload environment
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENV ASPNETCORE_ENVIRONMENT=Development

# Run with hot reload and specific binding
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5655", "--no-restore"]
```

### Production Stage Configuration

```dockerfile
# Production build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source and build
COPY . ./
RUN dotnet build -c Release --no-restore

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Production runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory
WORKDIR /app

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean

# Copy published application
COPY --from=build /app/publish .

# Change ownership to app user
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 5655

# Configure production environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5655

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5655/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]
```

### Build Optimization Strategies

```dockerfile
# Layer caching optimization
# 1. Copy package references first (rarely change)
COPY *.csproj nuget.config* ./
RUN dotnet restore

# 2. Copy source code last (frequently changes)
COPY . ./

# Multi-platform build support
# Use BuildKit for better caching and parallel builds
# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# .dockerignore optimization
# bin/
# obj/
# .git/
# .vs/
# *.user
# *.suo
```

## 2. Hot Reload Configuration

### Volume Mount Strategy

```yaml
# docker-compose.dev.yml
services:
  api:
    build:
      context: ./apps/api
      dockerfile: Dockerfile
      target: development
    volumes:
      # Source code hot reload
      - ./apps/api:/app:delegated
      # Preserve node_modules and build artifacts
      - /app/bin
      - /app/obj
      # NuGet package cache
      - ~/.nuget/packages:/root/.nuget/packages:ro
    environment:
      # Hot reload configuration
      - DOTNET_USE_POLLING_FILE_WATCHER=true
      - DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5655
```

### File Watcher Configuration

```csharp
// Program.cs - Development configuration
if (builder.Environment.IsDevelopment())
{
    // Configure file watching for hot reload
    builder.Configuration.AddJsonFile("appsettings.Development.json", 
        optional: false, reloadOnChange: true);
    
    // Enhanced logging for development
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    
    // Enable detailed entity framework logging
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors();
    });
}
```

### Hot Reload Performance Validation

```bash
# Test hot reload functionality
echo "// Hot reload test - $(date)" >> apps/api/Program.cs

# Expected behavior:
# - File change detected within 1-2 seconds
# - Application restart within 3-5 seconds
# - Health endpoint available immediately after restart
curl -f http://localhost:5655/health
```

## 3. Environment Configurations

### Development Environment

```yaml
# docker-compose.dev.yml
services:
  api:
    build:
      target: development
    ports:
      - "5655:5655"  # API port
      - "5656:5656"  # Debug port
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:5655
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!"
      Jwt__SecretKey: "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!"
      Jwt__Issuer: "WitchCityRope-API"
      Jwt__Audience: "WitchCityRope-Services"
      ServiceAuth__Secret: "DevSecret-WitchCityRope-ServiceToService-Auth-2024"
      DOTNET_USE_POLLING_FILE_WATCHER: "true"
      DOTNET_WATCH_RESTART_ON_RUDE_EDIT: "true"
    volumes:
      - ./apps/api:/app:delegated
      - /app/bin
      - /app/obj
      - ~/.nuget/packages:/root/.nuget/packages:ro
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - witchcityrope-internal
      - witchcityrope-public
```

### Test Environment

```yaml
# docker-compose.test.yml
services:
  api-test:
    build:
      target: production
    environment:
      ASPNETCORE_ENVIRONMENT: Testing
      ConnectionStrings__DefaultConnection: "Host=postgres-test;Port=5432;Database=witchcityrope_test;Username=postgres;Password=TestPassword2024!"
      Jwt__SecretKey: "TestSecret-JWT-WitchCityRope-TestEnv-2024-32CharMinimum!"
      Logging__LogLevel__Default: "Information"
      Logging__LogLevel__Microsoft: "Warning"
    networks:
      - test-network
    depends_on:
      postgres-test:
        condition: service_healthy
    # No volume mounts for test isolation
```

### Production Environment

```yaml
# docker-compose.prod.yml
services:
  api:
    build:
      target: production
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5655
      # Connection string from secrets
      ConnectionStrings__DefaultConnection_FILE: /run/secrets/db_connection
      # JWT secrets from external secret management
      Jwt__SecretKey_FILE: /run/secrets/jwt_secret
      ServiceAuth__Secret_FILE: /run/secrets/service_secret
    secrets:
      - db_connection
      - jwt_secret
      - service_secret
    networks:
      - internal-network  # No external access
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
      restart_policy:
        condition: unless-stopped
        delay: 10s
        max_attempts: 3
```

## 4. Service Dependencies

### PostgreSQL Dependency Management

```yaml
# Proper service dependency configuration
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: witchcityrope_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: WitchCity2024!
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope_dev"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - witchcityrope-internal

  api:
    depends_on:
      postgres:
        condition: service_healthy
    # Wait script for additional safety
    command: ["./wait-for-db.sh", "postgres:5432", "--", "dotnet", "watch", "run"]
```

### Wait Script Implementation

```bash
#!/bin/bash
# wait-for-db.sh
set -e

host="$1"
shift
cmd="$@"

until pg_isready -h "$host" -p 5432 -U postgres -d witchcityrope_dev; do
  >&2 echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

>&2 echo "PostgreSQL is up - executing command"
exec $cmd
```

### Health Check Implementation

```csharp
// Enhanced health check configuration
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "database", "sql" })
    .AddCheck<AuthenticationHealthCheck>("authentication")
    .AddCheck<JwtServiceHealthCheck>("jwt-service");

// Custom health checks
public class AuthenticationHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            
            // Test authentication service functionality
            await authService.ValidateServiceAsync();
            
            return HealthCheckResult.Healthy("Authentication service is responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Authentication service failed", ex);
        }
    }
}

// Health check endpoint configuration
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

### Graceful Shutdown Handling

```csharp
// Graceful shutdown configuration
var app = builder.Build();

// Configure graceful shutdown
var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

applicationLifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application is shutting down gracefully...");
});

applicationLifetime.ApplicationStopped.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application has shut down");
});

// Configure shutdown timeout
app.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

## 5. Authentication Configuration

### JWT Secret Management

```yaml
# Development (docker-compose.dev.yml)
services:
  api:
    environment:
      Jwt__SecretKey: "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!"
      ServiceAuth__Secret: "DevSecret-WitchCityRope-ServiceToService-Auth-2024"

# Production (docker-compose.prod.yml)
services:
  api:
    secrets:
      - jwt_secret
      - service_secret
    environment:
      Jwt__SecretKey_FILE: /run/secrets/jwt_secret
      ServiceAuth__Secret_FILE: /run/secrets/service_secret

secrets:
  jwt_secret:
    external: true
    name: witchcityrope_jwt_secret_v1
  service_secret:
    external: true
    name: witchcityrope_service_secret_v1
```

### Container-Aware CORS Configuration

```csharp
// Container-aware CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment", corsBuilder =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:5173",    // Host React dev server
            "http://localhost:3000",    // Alternative Vite port
            "http://127.0.0.1:5173",   // Local IP access
        };

        // Add container origins in development
        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.AddRange(new[]
            {
                "http://web:3000",         // Container-to-container
                "http://web:5173",         // Container-to-container Vite
                "http://localhost:8080",   // Docker port mapping
            });
        }

        corsBuilder
            .WithOrigins(allowedOrigins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});
```

### Cookie Configuration for Containers

```csharp
// Container-aware cookie configuration
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax; // More permissive for containers
    
    // Development vs production cookie settings
    if (builder.Environment.IsDevelopment())
    {
        options.Secure = CookieSecurePolicy.SameAsRequest; // Allow HTTP in dev
    }
    else
    {
        options.Secure = CookieSecurePolicy.Always; // HTTPS only in production
    }
});

// Identity cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "WitchCityRope.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    
    // Container-aware secure policy
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
});
```

### Service-to-Service Authentication

```csharp
// Enhanced service authentication for containers
public class ServiceAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceAuthenticationMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate service-to-service authentication
        if (context.Request.Path.StartsWithSegments("/api/auth/service-token"))
        {
            var serviceSecret = context.Request.Headers["X-Service-Secret"].FirstOrDefault();
            var expectedSecret = GetServiceSecret();
            
            if (string.IsNullOrEmpty(serviceSecret) || serviceSecret != expectedSecret)
            {
                _logger.LogWarning("Invalid service authentication attempt from {RemoteIpAddress}", 
                    context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await _next(context);
    }

    private string GetServiceSecret()
    {
        // Support both direct config and file-based secrets
        var secretFile = _configuration["ServiceAuth:Secret_FILE"];
        if (!string.IsNullOrEmpty(secretFile) && File.Exists(secretFile))
        {
            return File.ReadAllText(secretFile).Trim();
        }
        
        return _configuration["ServiceAuth:Secret"] ?? throw new InvalidOperationException(
            "Service authentication secret not configured");
    }
}
```

## 6. API Configuration

### Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "WitchCityRope": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!"
  },
  "Jwt": {
    "SecretKey": "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!",
    "Issuer": "WitchCityRope-API",
    "Audience": "WitchCityRope-Services",
    "ExpirationMinutes": 60
  },
  "ServiceAuth": {
    "Secret": "DevSecret-WitchCityRope-ServiceToService-Auth-2024"
  }
}
```

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "WitchCityRope": "Information"
    }
  },
  "AllowedHosts": "*",
  "ForwardedHeaders": {
    "ForwardedProto": "X-Forwarded-Proto",
    "ForwardedFor": "X-Forwarded-For"
  }
}
```

### Connection String Injection

```csharp
// Flexible connection string configuration
public static class DatabaseConfiguration
{
    public static void ConfigureDatabase(this IServiceCollection services, 
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = GetConnectionString(configuration, environment);
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                
                // Command timeout for long operations
                npgsqlOptions.CommandTimeout(30);
            });
            
            // Development vs production configuration
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }
    
    private static string GetConnectionString(IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
        // Try file-based connection string first (production)
        var connectionFile = configuration["ConnectionStrings:DefaultConnection_FILE"];
        if (!string.IsNullOrEmpty(connectionFile) && File.Exists(connectionFile))
        {
            return File.ReadAllText(connectionFile).Trim();
        }
        
        // Fall back to direct configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not configured");
        }
        
        return connectionString;
    }
}
```

### Swagger Configuration Per Environment

```csharp
// Environment-specific Swagger configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WitchCityRope API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(DocExpansion.None);
        c.DefaultModelsExpandDepth(-1); // Hide models section
    });
}
else if (app.Environment.IsStaging())
{
    // Limited Swagger for staging
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WitchCityRope API v1 (Staging)");
        c.RoutePrefix = "api-docs";
    });
}
// No Swagger in production for security
```

## 7. Performance Optimization

### Multi-Stage Build Optimization

```dockerfile
# Optimized multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore --verbosity minimal

FROM restore AS build
COPY . ./
RUN dotnet build -c Release --no-restore --verbosity minimal

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --verbosity minimal

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
# Alpine image for smaller size
RUN apk add --no-cache curl postgresql-client
WORKDIR /app
COPY --from=publish /app/publish .
USER 1001
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]
```

### Image Size Reduction

```dockerfile
# .dockerignore for build optimization
bin/
obj/
.git/
.vs/
.vscode/
*.user
*.suo
*.sln.docstates
.dockerignore
Dockerfile*
README.md
.env
.gitignore
```

### Runtime Performance Settings

```csharp
// Runtime performance configuration
public static void ConfigurePerformance(this IServiceCollection services, IConfiguration configuration)
{
    // Configure GC settings for containerized environments
    services.Configure<GCSettings>(options =>
    {
        // Server GC for better throughput in containers
        GCSettings.IsServerGC = true;
    });
    
    // Connection pooling configuration
    services.AddDbContextPool<ApplicationDbContext>(options =>
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        options.UseNpgsql(connectionString);
    }, poolSize: 128); // Optimize for container resource limits
    
    // HTTP client configuration
    services.AddHttpClient<ApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        MaxConnectionsPerServer = 50 // Limit connections per container
    });
}
```

### Memory and CPU Limits

```yaml
# Container resource optimization
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '1.0'         # 1 CPU core maximum
          memory: 512M        # 512MB memory maximum
        reservations:
          cpus: '0.25'        # Reserve 25% CPU
          memory: 128M        # Reserve 128MB memory
      restart_policy:
        condition: unless-stopped
        delay: 10s
        max_attempts: 3
        window: 120s
```

## 8. Security Hardening

### Non-Root User Execution

```dockerfile
# Security-hardened Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Create non-root user
RUN adduser -D -u 1001 -g 1001 -s /bin/sh appuser

# Install minimal required packages
RUN apk add --no-cache \
    curl \
    postgresql-client \
    && rm -rf /var/cache/apk/*

WORKDIR /app

# Copy application
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R appuser:appuser /app && \
    chmod -R 755 /app

# Switch to non-root user
USER appuser

# Health check as non-root user
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5655/health || exit 1

EXPOSE 5655
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]
```

### Secret Management

```yaml
# Production secret management
services:
  api:
    secrets:
      - source: jwt_secret_v2
        target: /run/secrets/jwt_secret
        mode: 0400  # Read-only for owner
      - source: db_connection_v1
        target: /run/secrets/db_connection
        mode: 0400
    environment:
      # Reference secrets by file path
      Jwt__SecretKey_FILE: /run/secrets/jwt_secret
      ConnectionStrings__DefaultConnection_FILE: /run/secrets/db_connection

secrets:
  jwt_secret_v2:
    external: true
    name: witchcityrope_jwt_secret_v2
  db_connection_v1:
    external: true
    name: witchcityrope_db_connection_v1
```

### Network Policies

```yaml
# Secure network configuration
networks:
  public:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
  internal:
    driver: bridge
    internal: true  # No external internet access
    ipam:
      config:
        - subnet: 172.21.0.0/16

services:
  web:
    networks:
      - public      # External access for user traffic
      - internal    # Internal service communication
  
  api:
    networks:
      - internal    # Service-to-service only
    
  postgres:
    networks:
      - internal    # Database access only
```

### Container Scanning

```bash
# Container security scanning
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image witchcityrope/api:latest

# Vulnerability scanning with Snyk
snyk container test witchcityrope/api:latest

# CIS Docker Benchmark
docker run --rm --net host --pid host --userns host --cap-add audit_control \
  -e DOCKER_CONTENT_TRUST=$DOCKER_CONTENT_TRUST \
  -v /etc:/etc:ro \
  -v /usr/bin/containerd:/usr/bin/containerd:ro \
  -v /usr/bin/runc:/usr/bin/runc:ro \
  -v /usr/lib/systemd:/usr/lib/systemd:ro \
  -v /var/lib:/var/lib:ro \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  --label docker_bench_security \
  docker/docker-bench-security
```

## Complete Docker Compose Configuration

### Development Configuration

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: witchcityrope_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: WitchCity2024!
    ports:
      - "5433:5432"
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
      - ./docker/init-scripts:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope_dev"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    networks:
      - witchcityrope-internal

  api:
    build:
      context: ./apps/api
      dockerfile: Dockerfile
      target: development
    ports:
      - "5655:5655"
      - "5656:5656"  # Debug port
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:5655
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!"
      Jwt__SecretKey: "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!"
      Jwt__Issuer: "WitchCityRope-API"
      Jwt__Audience: "WitchCityRope-Services"
      ServiceAuth__Secret: "DevSecret-WitchCityRope-ServiceToService-Auth-2024"
      DOTNET_USE_POLLING_FILE_WATCHER: "true"
      DOTNET_WATCH_RESTART_ON_RUDE_EDIT: "true"
    volumes:
      - ./apps/api:/app:delegated
      - /app/bin
      - /app/obj
      - ~/.nuget/packages:/root/.nuget/packages:ro
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - witchcityrope-internal
      - witchcityrope-public
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5655/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile
      target: development
    ports:
      - "5173:5173"
    environment:
      NODE_ENV: development
      VITE_API_BASE_URL: http://localhost:5655
    volumes:
      - ./apps/web:/app:delegated
      - /app/node_modules
    networks:
      - witchcityrope-public
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5173"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_dev_data:
    driver: local

networks:
  witchcityrope-internal:
    driver: bridge
    internal: false  # Allow external in development
  witchcityrope-public:
    driver: bridge
```

### Production Configuration

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB_FILE: /run/secrets/db_name
      POSTGRES_USER_FILE: /run/secrets/db_user
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    volumes:
      - postgres_prod_data:/var/lib/postgresql/data
    secrets:
      - db_name
      - db_user
      - db_password
    networks:
      - internal-network
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G

  api:
    build:
      context: ./apps/api
      dockerfile: Dockerfile
      target: production
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5655
      ConnectionStrings__DefaultConnection_FILE: /run/secrets/db_connection
      Jwt__SecretKey_FILE: /run/secrets/jwt_secret
      ServiceAuth__Secret_FILE: /run/secrets/service_secret
    secrets:
      - db_connection
      - jwt_secret
      - service_secret
    depends_on:
      - postgres
    networks:
      - internal-network
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
      restart_policy:
        condition: unless-stopped
        delay: 10s
        max_attempts: 3

  nginx:
    image: nginx:alpine
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    networks:
      - internal-network
      - public-network

volumes:
  postgres_prod_data:
    driver: local

networks:
  internal-network:
    driver: bridge
    internal: true
  public-network:
    driver: bridge

secrets:
  db_connection:
    external: true
    name: witchcityrope_db_connection_v1
  jwt_secret:
    external: true
    name: witchcityrope_jwt_secret_v2
  service_secret:
    external: true
    name: witchcityrope_service_secret_v1
  db_name:
    external: true
    name: witchcityrope_db_name_v1
  db_user:
    external: true
    name: witchcityrope_db_user_v1
  db_password:
    external: true
    name: witchcityrope_db_password_v1
```

## Health Check Endpoints

### Database Health Check

```csharp
// Enhanced health check implementation
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic connectivity
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Test query execution
            var userCount = await _context.Users.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                { "database", "PostgreSQL" },
                { "users", userCount },
                { "timestamp", DateTime.UtcNow }
            };
            
            return HealthCheckResult.Healthy("Database is accessible", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

### Authentication Health Check

```csharp
public class AuthenticationHealthCheck : IHealthCheck
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test JWT service
            var testToken = _jwtService.GenerateToken(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "healthcheck@test.com",
                UserName = "healthcheck"
            });
            
            if (string.IsNullOrEmpty(testToken.Token))
            {
                return HealthCheckResult.Unhealthy("JWT service not functioning");
            }
            
            return HealthCheckResult.Healthy("Authentication services operational");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Authentication check failed", ex);
        }
    }
}
```

## Summary

This comprehensive API container design provides:

**✅ Multi-Stage Build Architecture**
- Development stage with hot reload support
- Production stage with optimized runtime
- Build caching and layer optimization

**✅ Hot Reload Development**
- File watcher configuration for rapid development
- Volume mounting strategies for code changes
- Performance validation procedures

**✅ Environment-Specific Configuration**
- Development: Full debugging and logging
- Test: Isolated environment with test data
- Production: Security hardened with secrets management

**✅ Service Dependencies**
- PostgreSQL dependency management
- Health checks for all components
- Graceful shutdown handling

**✅ Authentication Preservation**
- JWT + HttpOnly cookie patterns maintained
- Service-to-service authentication across containers
- CORS configuration for container networking

**✅ Performance Optimization**
- Multi-stage builds for minimal production images
- Resource limits and monitoring
- Connection pooling and runtime optimization

**✅ Security Hardening**
- Non-root user execution
- Secret management with Docker secrets
- Network isolation and security scanning

The design maintains full compatibility with existing authentication patterns while providing a robust foundation for containerized development and production deployment.