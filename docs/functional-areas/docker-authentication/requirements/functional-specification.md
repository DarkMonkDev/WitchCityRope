# Functional Specification: Docker Implementation of Proven Authentication System
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

Containerize the EXISTING WORKING authentication system (React + .NET API + PostgreSQL) to validate it operates identically in Docker environment. This specification preserves the proven Hybrid JWT + HttpOnly Cookie authentication architecture that currently works perfectly at localhost:5173 (React) and localhost:5655 (API).

**Core Principle**: Zero authentication code changes - only configuration and containerization.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173 → Container port 3000
- **API Service** (.NET 9 Minimal API): Business logic at http://localhost:5655 → Container port 8080
- **Database** (PostgreSQL): localhost:5433 → Container port 5432
- **Test Infrastructure**: localhost:8080 → Container port 80
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Container Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Compose Network                   │
│                         (bridge)                            │
├─────────────────┬─────────────────┬─────────────────────────┤
│   React App     │    .NET API     │      PostgreSQL         │
│   (Vite Dev)    │   (Minimal API) │     (postgres:16)       │
│                 │                 │                         │
│ Port: 5173:3000 │ Port: 5655:8080 │   Port: 5433:5432      │
│ Service: web    │ Service: api    │   Service: postgres     │
│                 │                 │                         │
│ - Hot Reload    │ - dotnet watch  │ - Data Persistence      │
│ - Volume Mount  │ - Volume Mount  │ - Health Checks         │
│ - Vite HMR      │ - JWT/Identity  │ - Auto Restart          │
└─────────────────┴─────────────────┴─────────────────────────┘
```

### Component Structure
```
/docker/
├── docker-compose.yml          # Main orchestration
├── docker-compose.dev.yml      # Development overrides
├── Dockerfile.web              # React + Vite container
├── Dockerfile.api              # .NET API container
├── .env.development            # Development environment
├── .dockerignore               # Build optimization
└── scripts/
    ├── dev.sh                  # Development startup
    ├── health-check.sh         # Container health validation
    └── cleanup.sh              # Container cleanup
```

### Service Communication Architecture
- **Internal Network**: Custom bridge network `witchcityrope-dev`
- **DNS Resolution**: Container service names (web, api, postgres)
- **API Base URL**: Configurable via environment variable for container vs host
- **Authentication Flow**: React → api:8080/api/auth/* → postgres:5432

## Data Models

### Container Environment Configuration

#### Environment Variables - API Service
```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Information

# Database Connection (Container Network)
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!

# JWT Configuration
JWT_SECRET=DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!
JWT_ISSUER=WitchCityRope-API
JWT_AUDIENCE=WitchCityRope-Services

# CORS Configuration
CORS_ORIGINS=http://localhost:5173,http://web:3000,http://localhost:3000
```

#### Environment Variables - React Service
```bash
# Vite Configuration
NODE_ENV=development
VITE_API_BASE_URL=http://localhost:5655

# Development Server
VITE_HOST=0.0.0.0
VITE_PORT=3000
```

#### Environment Variables - PostgreSQL Service
```bash
POSTGRES_DB=witchcityrope_dev
POSTGRES_USER=postgres
POSTGRES_PASSWORD=WitchCity2024!
POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256
```

### Volume Mapping Strategy
```yaml
# React Development Volumes
- ./apps/web:/app                    # Source code hot reload
- /app/node_modules                  # Prevent host conflict
- web_dist:/app/dist                 # Build output

# API Development Volumes  
- ./apps/api:/app                    # Source code hot reload
- /app/bin                           # Prevent host conflict
- /app/obj                           # Prevent host conflict

# Database Volumes
- postgres_data:/var/lib/postgresql/data  # Data persistence
```

## API Specifications

### Container Health Check Endpoints
| Method | Path | Service | Purpose | Expected Response |
|--------|------|---------|---------|-------------------|
| GET | /health | API | Container health | 200 OK |
| GET | /api/auth/health | API | Auth system health | 200 OK |
| GET | / | React | Vite dev server | 200 OK |

### Service-to-Service Authentication Flow
1. **React Login** → `POST http://api:8080/api/auth/login`
2. **API Response** → JWT token + HttpOnly cookie set
3. **React State** → Store user data in AuthContext
4. **Protected Calls** → `Authorization: Bearer <token>` + cookies
5. **Logout** → `POST http://api:8080/api/auth/logout`

### Container Network Communication
- **React to API**: Service name `api` resolves to API container IP
- **API to Database**: Service name `postgres` resolves to DB container IP
- **External Access**: Host ports mapped to container ports
- **Cookie Domain**: Configured for localhost and container network

## Docker Operations Guide Specifications

### Comprehensive Operations Documentation Requirements
The implementation must include a complete Docker operations guide covering all development and troubleshooting scenarios:

#### Container Management Operations
- **Starting containers**: Multiple startup scenarios (fresh start, restart, specific services)
- **Stopping containers**: Graceful shutdown, force stop, selective service stopping  
- **Restarting containers**: Individual service restart, full environment restart, restart policies
- **Health checking**: Service health validation, dependency checking, connectivity testing
- **Viewing logs**: Service-specific logs, aggregated logs, real-time monitoring, debugging with container logs
- **Troubleshooting**: Common issues, debugging steps, error resolution patterns
- **Database operations**: Container-based migrations, PostgreSQL management, data persistence validation

#### Hot Reload Testing and Validation Procedures
- **.NET API Hot Reload**: Comprehensive validation procedures for dotnet watch functionality in containers
- **React Hot Reload**: Vite HMR testing and validation in containerized environment  
- **File watching**: Volume mount validation, file system event propagation testing
- **Performance validation**: Hot reload timing benchmarks, responsiveness testing
- **Test procedures**: Step-by-step validation processes for hot reload functionality
- **Troubleshooting hot reload**: Common hot reload issues and resolution in Docker containers

#### Agent Accessibility and Awareness Requirements
- **test-executor access**: Direct links to Docker testing procedures and health checks in their lessons learned file
- **backend-developer access**: API-specific Docker operations and troubleshooting guidance in their lessons learned file
- **react-developer access**: Frontend containerization and hot reload documentation in their lessons learned file
- **orchestrator integration**: Automated direction to appropriate Docker guides based on task type
- **lessons learned integration**: All agents have Docker operations guide references in their role-specific lessons learned files
- **central documentation awareness**: Agents know about /docs/architecture/docker-architecture.md and /docs/guides-setup/docker-operations-guide.md locations

#### Central Documentation Architecture Requirements
- **Docker Architecture Overview**: High-level Docker strategy document at /docs/architecture/docker-architecture.md
- **Operations Guide Location**: Centralized guide at /docs/guides-setup/docker-operations-guide.md accessible to all development agents
- **Agent Direction System**: Mechanism for orchestrator to direct agents to appropriate documentation
- **Cross-referencing**: Links between central architecture and specific operational guides
- **Public accessibility**: Documentation structure supports future agent discovery of Docker guides
- **Strategic vs operational separation**: Clear distinction between architecture overview and operational procedures

### Docker Operations Guide Structure
```
/docs/guides-setup/docker-operations-guide.md
├── Quick Start Commands
├── Container Management
│   ├── Starting Services
│   ├── Stopping Services  
│   ├── Restarting Services
│   └── Health Checking
├── Development Workflows
│   ├── Hot Reload Testing
│   ├── Log Monitoring
│   └── Performance Validation
├── Troubleshooting
│   ├── Common Issues
│   ├── Service Communication
│   ├── Volume Mount Problems
│   └── Network Connectivity
└── Agent-Specific Sections
    ├── For Test Executor
    ├── For Backend Developer
    └── For React Developer
```

### Central Docker Architecture Document
```
/docs/architecture/docker-architecture.md
├── Docker Strategy Overview
├── Service Architecture
├── Local Development Setup vs Test/Production Provisions
├── Future Scaling Provisions
├── Links to All Docker-Related Guides
├── Agent Instruction Matrix
└── Which Agents Need This Information
```

## Component Specifications

### React Container (Vite Development)

#### Dockerfile.web
```dockerfile
# Multi-stage build for development and production
FROM node:18-alpine AS base
WORKDIR /app

# Development stage
FROM base AS development
COPY apps/web/package*.json ./
RUN npm install
COPY apps/web/ .
EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]

# Production stage
FROM base AS production
COPY apps/web/package*.json ./
RUN npm ci --only=production
COPY apps/web/ .
RUN npm run build
EXPOSE 80
CMD ["npm", "run", "preview", "--", "--host", "0.0.0.0", "--port", "80"]
```

#### Vite Configuration Updates
```typescript
// vite.config.ts - Container-aware configuration
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    watch: {
      usePolling: true, // For file system compatibility
    },
    proxy: {
      '/api': {
        target: process.env.VITE_API_BASE_URL || 'http://api:8080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

### API Container (.NET 9 Development)

#### Dockerfile.api
```dockerfile
# Multi-stage build for development and production
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

# Development stage
FROM base AS development
COPY apps/api/*.csproj ./
RUN dotnet restore
COPY apps/api/ .
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--urls", "http://+:8080", "--no-hot-reload"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=base /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]
```

#### Program.cs Configuration Updates
```csharp
// Connection string - container-aware
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!";

// CORS - container and host origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment", builder => builder
        .WithOrigins(
            "http://localhost:5173", 
            "http://localhost:3000", 
            "http://web:3000",
            "http://127.0.0.1:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});
```

### PostgreSQL Container

#### Service Configuration
```yaml
postgres:
  image: postgres:16-alpine
  container_name: witchcityrope-postgres
  environment:
    POSTGRES_DB: witchcityrope_dev
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: WitchCity2024!
    POSTGRES_INITDB_ARGS: --auth-host=scram-sha-256
  ports:
    - "5433:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
    - ./docker/init:/docker-entrypoint-initdb.d
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope_dev"]
    interval: 30s
    timeout: 10s
    retries: 5
    start_period: 30s
  restart: unless-stopped
```

## Docker Compose Specifications

### Main Configuration (docker-compose.yml)
```yaml
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
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope_dev"]
      interval: 30s
      timeout: 10s
      retries: 5
    networks:
      - witchcityrope-dev

  api:
    build:
      context: .
      dockerfile: docker/Dockerfile.api
      target: development
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!
    ports:
      - "5655:8080"
    volumes:
      - ./apps/api:/app
      - /app/bin
      - /app/obj
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - witchcityrope-dev

  web:
    build:
      context: .
      dockerfile: docker/Dockerfile.web
      target: development
    environment:
      - NODE_ENV=development
      - VITE_API_BASE_URL=http://localhost:5655
    ports:
      - "5173:3000"
    volumes:
      - ./apps/web:/app
      - /app/node_modules
    depends_on:
      - api
    networks:
      - witchcityrope-dev

volumes:
  postgres_data:

networks:
  witchcityrope-dev:
    driver: bridge
```

### Development Overrides (docker-compose.dev.yml)
```yaml
version: '3.8'

services:
  api:
    environment:
      - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug
      - ASPNETCORE_LOGGING__LOGLEVEL__MICROSOFT=Information
    volumes:
      - ./apps/api:/app:delegated
    command: ["dotnet", "watch", "run", "--urls", "http://+:8080"]

  web:
    environment:
      - VITE_PORT=3000
      - VITE_HOST=0.0.0.0
    volumes:
      - ./apps/web:/app:delegated
    command: ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]
```

## Service-to-Service Communication

### Container Network Configuration
```yaml
networks:
  witchcityrope-dev:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

### Authentication Flow Preservation
1. **React Component** makes login request
2. **Container DNS** resolves `api` to API container IP
3. **HTTP Request** → `http://api:8080/api/auth/login`
4. **API Controller** processes with existing Identity logic
5. **Response** includes JWT token + sets HttpOnly cookie
6. **React State** updates with user data
7. **Subsequent requests** include both token and cookies

### CORS Configuration for Containers
```csharp
// Updated CORS policy for container environment
.WithOrigins(
    "http://localhost:5173",    // Host access
    "http://localhost:3000",    // Alternative Vite port
    "http://web:3000",          // Container-to-container
    "http://127.0.0.1:5173"     // Local alternative
)
.AllowCredentials()             // Critical for HttpOnly cookies
```

### Environment-Aware API Base URL
```typescript
// authService.ts - Container-aware configuration
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'

// Development: http://localhost:5655 (host port mapping)
// Container: http://api:8080 (internal service communication)
```

## Development Workflow

### Hot Reload Configuration

#### React (Vite) Hot Reload
```dockerfile
# Volume mounting for file watching
volumes:
  - ./apps/web:/app:delegated       # Source code
  - /app/node_modules               # Isolated dependencies
  
# Vite configuration
server: {
  host: '0.0.0.0',               # Allow external connections
  port: 3000,                    # Internal container port
  watch: {
    usePolling: true,            # Cross-platform file watching
    interval: 1000               # Polling interval
  }
}
```

#### .NET API Hot Reload
```dockerfile
# Volume mounting for source watching
volumes:
  - ./apps/api:/app:delegated       # Source code
  - /app/bin                        # Isolated build artifacts
  - /app/obj                        # Isolated object files

# dotnet watch command
CMD ["dotnet", "watch", "run", "--urls", "http://+:8080", "--no-hot-reload"]
```

### Development Helper Scripts

#### dev.sh - Main Development Script
```bash
#!/bin/bash
# Complete development environment startup

echo "🐋 Starting WitchCityRope Docker Development Environment"

# Check for required files
if [[ ! -f "docker-compose.yml" ]]; then
    echo "❌ docker-compose.yml not found"
    exit 1
fi

# Build and start services
echo "📦 Building containers..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build

echo "🚀 Starting services..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 15

# Health check
echo "🏥 Checking service health..."
./docker/scripts/health-check.sh

echo "✅ Development environment ready!"
echo "🌐 React App: http://localhost:5173"
echo "🔌 API: http://localhost:5655"
echo "🗄️ PostgreSQL: localhost:5433"
echo ""
echo "📊 View logs: docker-compose logs -f [service]"
echo "🛑 Stop: docker-compose down"
```

#### health-check.sh - Service Validation
```bash
#!/bin/bash
# Validate all services are healthy and authentication works

echo "🔍 Checking PostgreSQL..."
docker-compose exec postgres pg_isready -U postgres -d witchcityrope_dev

echo "🔍 Checking API health..."
curl -f http://localhost:5655/health || echo "❌ API health check failed"

echo "🔍 Checking React app..."
curl -f http://localhost:5173 || echo "❌ React app not responding"

echo "🔍 Testing authentication endpoint..."
curl -f http://localhost:5655/api/auth/health || echo "❌ Auth endpoint not available"

echo "✅ All health checks complete"
```

### Database Initialization and Seeding
```yaml
# PostgreSQL initialization
postgres:
  volumes:
    - ./docker/init/01-init.sql:/docker-entrypoint-initdb.d/01-init.sql
    - ./docker/init/02-seed.sql:/docker-entrypoint-initdb.d/02-seed.sql

# EF Core migrations in API startup
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync(); // Apply pending migrations
}
```

### Log Aggregation and Monitoring
```bash
# View all service logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f web
docker-compose logs -f api  
docker-compose logs -f postgres

# Monitor resource usage
docker stats
```

## Security Configuration

### Development Secrets Management
```yaml
# .env.development (not committed)
POSTGRES_PASSWORD=WitchCity2024!
JWT_SECRET=DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!

# docker-compose.yml
env_file:
  - .env.development
```

### JWT Secret Handling in Containers
```csharp
// Program.cs - Environment-aware JWT configuration
var jwtSecretKey = builder.Configuration["JWT_SECRET"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!";
```

### Cookie Security Settings
```csharp
// Configure cookies for container environment
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,  // Allow cross-container requests
    HttpOnly = HttpOnlyPolicy.Always,          // XSS protection
    Secure = CookieSecurePolicy.SameAsRequest  // HTTPS when available
});
```

### Network Isolation
```yaml
# Custom bridge network for service isolation
networks:
  witchcityrope-dev:
    driver: bridge
    internal: false  # Allow external access for development
```

### PostgreSQL Authentication
```bash
# Secure password authentication
POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256

# Connection string with proper authentication
Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!;Include Error Detail=true
```

## Validation Approach

### Container Startup Sequence
1. **PostgreSQL** starts first and becomes healthy
2. **API** starts after PostgreSQL health check passes
3. **React** starts after API is available
4. **Dependencies** enforced via `depends_on` with health conditions

### Health Check Endpoints
```csharp
// API health endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});
```

### Authentication Flow Testing
```bash
# Test complete authentication flow
curl -X POST http://localhost:5655/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234","sceneName":"TestUser"}' \
  -c cookies.txt

curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234"}' \
  -b cookies.txt -c cookies.txt

curl http://localhost:5655/api/protected/welcome \
  -H "Authorization: Bearer <token>" \
  -b cookies.txt
```

### Performance Monitoring
```bash
# Container resource monitoring
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"

# Response time testing
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:5655/api/auth/health
```

### Troubleshooting Procedures
```bash
# Check service connectivity
docker-compose exec web ping api
docker-compose exec api ping postgres

# Verify environment variables
docker-compose exec api env | grep -E "(ASPNETCORE|ConnectionStrings|JWT)"
docker-compose exec web env | grep VITE

# Check port bindings
docker-compose ps
netstat -tulpn | grep -E "(5173|5655|5433)"
```

## Integration Points

### Authentication System (Preserved)
- **ASP.NET Core Identity**: User management and authentication
- **JWT Service**: Token generation and validation
- **HttpOnly Cookies**: Session management
- **Authorization**: Role-based access control

### Email Notifications (Container-Ready)
- **SMTP Configuration**: Environment-based configuration
- **Service Communication**: Email service calls API endpoints
- **Template Rendering**: Existing Razor templates work unchanged

### Event Management (Unchanged)
- **Event CRUD**: All existing event management functionality
- **Database Access**: EF Core patterns remain identical
- **API Endpoints**: Event controllers work without modification

### Testing Infrastructure
- **Playwright E2E**: Tests run against containerized services
- **Test Server**: Python HTTP server container for test fixtures
- **Database**: Isolated test database for integration tests

## Performance Requirements

### Response Time Targets
- **React App Load**: <2 seconds initial load in container
- **API Authentication**: <50ms response time (matches native)
- **Database Queries**: <100ms for user operations
- **Hot Reload**: React changes <1s, API restarts <5s

### Resource Allocation
```yaml
# Container resource limits
api:
  deploy:
    resources:
      limits:
        memory: 512M
        cpus: "0.5"
      reservations:
        memory: 256M
        cpus: "0.25"

web:
  deploy:
    resources:
      limits:
        memory: 256M
        cpus: "0.25"
```

### Reliability Requirements
- **Container Health**: Automatic restart on failure
- **Data Persistence**: Database survives container restarts
- **Network Resilience**: Graceful handling of temporary network issues
- **Session Management**: User authentication survives API container restarts

## Testing Requirements

### Unit Test Coverage
- **No Changes Required**: Existing 80% coverage maintained
- **Container Tests**: Docker health check validations
- **Service Tests**: Authentication flow integration tests

### Integration Tests for APIs
```csharp
// Integration test configuration for containers
public class AuthenticationIntegrationTests : IClassFixture<DockerWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public AuthenticationIntegrationTests(DockerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Test against containerized API
        var response = await _client.PostAsync("/api/auth/login", content);
        response.EnsureSuccessStatusCode();
    }
}
```

### E2E Tests for Workflows
```typescript
// Playwright test configuration for containers
import { test, expect } from '@playwright/test'

test.beforeEach(async ({ page }) => {
  // Test against containerized React app
  await page.goto('http://localhost:5173')
})

test('authentication flow works in containers', async ({ page }) => {
  // Complete authentication workflow test
  await page.click('text=Register')
  await page.fill('[data-testid=email]', 'test@example.com')
  await page.fill('[data-testid=password]', 'Test1234')
  await page.fill('[data-testid=sceneName]', 'TestUser')
  await page.click('[data-testid=submit]')
  
  await expect(page.locator('text=Welcome, TestUser')).toBeVisible()
})
```

### Performance Benchmarks
```bash
# Load testing against containers
ab -n 100 -c 10 http://localhost:5655/api/auth/health
wrk -t4 -c100 -d30s http://localhost:5173/

# Authentication performance test
artillery run auth-load-test.yml
```

## Migration Requirements

### Database Migrations
```csharp
// Automatic migration in containerized environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Apply pending migrations on startup
    await context.Database.MigrateAsync();
    
    // Seed test data if needed
    await SeedTestDataAsync(context);
}
```

### Data Transformation
- **No Changes Required**: Existing database schema preserved
- **Connection Strings**: Updated for container service names
- **Migration Path**: Existing migrations work unchanged

### Backward Compatibility
- **Development**: Both native and containerized development supported
- **API Contracts**: All existing endpoints unchanged
- **Database Schema**: Backward compatible with existing data

## Dependencies

### NuGet Packages Required
```xml
<!-- No additional packages required -->
<!-- Existing packages work in containers -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
```

### External Services
- **Docker Desktop**: Latest stable version
- **PostgreSQL**: postgres:16-alpine image
- **Node.js**: node:18-alpine image
- **.NET**: mcr.microsoft.com/dotnet/sdk:9.0 image

### Configuration Needs
```bash
# Development environment file
POSTGRES_PASSWORD=WitchCity2024!
JWT_SECRET=DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!
ASPNETCORE_ENVIRONMENT=Development
VITE_API_BASE_URL=http://localhost:5655
```

## Acceptance Criteria

Technical criteria for completion:

### Functional Requirements
- [ ] All authentication endpoints functional in containers
- [ ] React login/register forms work with containerized API
- [ ] JWT token generation and validation works between containers
- [ ] HttpOnly cookies function across container network
- [ ] Protected routes accessible after authentication
- [ ] Logout clears authentication state properly
- [ ] User sessions persist through container restarts

### Performance Requirements
- [ ] React app loads in <2 seconds in container environment
- [ ] API responses <50ms (same as native development)
- [ ] Database queries <100ms for authentication operations
- [ ] Hot reload: React <1s, API restart <5s

### Development Experience
- [ ] `./dev.sh` script starts complete environment
- [ ] Hot reload works for both React and .NET API
- [ ] File changes trigger appropriate container updates
- [ ] Debugging works with containerized services
- [ ] Log monitoring accessible for all services

### Testing Integration
- [ ] Playwright E2E tests pass against containers
- [ ] Integration tests work with containerized API
- [ ] Performance benchmarks meet native environment levels
- [ ] Health checks validate all service availability

### Security Validation
- [ ] JWT authentication works identically in containers
- [ ] HttpOnly cookies secure against XSS in container network
- [ ] CORS configuration allows proper service communication
- [ ] Database connections use secure authentication
- [ ] Environment secrets properly managed

### Operational Requirements
- [ ] Services start in correct dependency order
- [ ] Failed containers restart automatically
- [ ] Database data persists through container lifecycle
- [ ] Resource usage within acceptable limits
- [ ] Container logs provide adequate debugging information

### Documentation Requirements
- [ ] Comprehensive Docker operations guide created
- [ ] Central Docker architecture document established
- [ ] Hot reload testing procedures documented for .NET API
- [ ] Agent-specific Docker documentation sections complete
- [ ] Orchestrator can direct agents to appropriate Docker guides
- [ ] Troubleshooting procedures cover all common development scenarios
- [ ] Cross-references between central architecture and operational guides functional

## Quality Checklist

- [ ] Preserves exact authentication functionality
- [ ] Maintains development hot reload experience  
- [ ] Service-to-service communication works flawlessly
- [ ] Performance meets or exceeds native development
- [ ] Security measures preserved in container environment
- [ ] Database persistence and connectivity validated
- [ ] E2E test suite passes against containers
- [ ] Documentation complete for setup and troubleshooting
- [ ] Container health monitoring functional
- [ ] Zero authentication code changes achieved
- [ ] Docker operations guide comprehensive and agent-accessible
- [ ] Central Docker architecture documentation complete
- [ ] Hot reload testing procedures validated for .NET API
- [ ] Agent direction system functional for Docker guidance
- [ ] Troubleshooting documentation covers development workflows

This functional specification ensures the Docker implementation preserves all existing authentication functionality while providing the benefits of containerized development environments, following the established microservices architecture where React handles UI and the .NET API handles all business logic and authentication.