# Docker Architecture Diagram - WitchCityRope Authentication Containerization
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Architecture Overview

This document outlines the Docker containerization architecture for the WitchCityRope authentication system, building on the proven hybrid JWT + HttpOnly Cookies pattern that's currently working in development.

## Container Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               HOST MACHINE                                      │
│                          (Ubuntu 24.04 - Docker Host)                         │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │   :5173     │    │   :5655     │    │   :5433     │    │   :8080     │     │
│  │ Port Map    │    │ Port Map    │    │ Port Map    │    │ Port Map    │     │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘     │
│         │                  │                  │                  │             │
│  ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐     │
│  │    REACT    │    │  API (.NET) │    │ POSTGRESQL  │    │    TEST     │     │
│  │ CONTAINER   │    │ CONTAINER   │    │ CONTAINER   │    │ CONTAINER   │     │
│  │             │    │             │    │             │    │             │     │
│  │ Port: 5173  │    │ Port: 8080  │    │ Port: 5432  │    │ Port: 3000  │     │
│  │ Vite Server │    │ Minimal API │    │ Database    │    │ Test Server │     │
│  │             │    │             │    │             │    │             │     │
│  │ • React UI  │    │ • Auth API  │    │ • Auth DB   │    │ • E2E Tests │     │
│  │ • Auth UI   │    │ • JWT Gen   │    │ • Users     │    │ • Test Data │     │
│  │ • Cookies   │    │ • Identity  │    │ • Roles     │    │ • Fixtures  │     │
│  │ • API Calls │    │ • CORS      │    │ • Sessions  │    │ • Reports   │     │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘     │
│         │                  │                  │                  │             │
│         └────────┬─────────┴─────────┬────────┴─────────┬────────┘             │
│                  │                   │                  │                      │
│                  ▼                   ▼                  ▼                      │
│         ┌─────────────────────────────────────────────────────────┐            │
│         │           DOCKER NETWORK: witchcity-net                 │            │
│         │              Internal Network: 172.20.0.0/16            │            │
│         └─────────────────────────────────────────────────────────┘            │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐       │
│  │                        VOLUME MOUNTS                                │       │
│  │                                                                     │       │
│  │  React Container:                                                   │       │
│  │  • ./apps/web:/app          (Source code hot reload)               │       │
│  │  • node_modules:/app/node_modules  (Dependencies cache)            │       │
│  │                                                                     │       │
│  │  API Container:                                                     │       │
│  │  • ./apps/api:/app          (Source code hot reload)               │       │
│  │  • nuget-cache:/root/.nuget (NuGet package cache)                  │       │
│  │                                                                     │       │
│  │  PostgreSQL Container:                                              │       │
│  │  • postgres-data:/var/lib/postgresql/data  (Persistent storage)    │       │
│  │  • ./apps/api/Data/Init:/docker-entrypoint-initdb.d  (Init SQL)    │       │
│  │                                                                     │       │
│  │  Test Container:                                                    │       │
│  │  • ./tests:/app/tests       (Test files)                           │       │
│  │  • test-reports:/app/reports (Test output)                         │       │
│  └─────────────────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## Service Communication Flow

### 1. User Authentication Flow
```
┌──────────────┐  HTTP Cookies   ┌──────────────┐   JWT Bearer    ┌──────────────┐
│              │ ─────────────► │              │ ─────────────► │              │
│ React SPA    │                │ Web Service  │                │ API Service  │
│ (Port 5173)  │ ◄───────────── │ (Port 5655)  │ ◄───────────── │ (Port 8080)  │
│              │  Auth Response │              │  Auth Response │              │
└──────────────┘                └──────────────┘                └──────────────┘
        │                               │                               │
        │ React Router                  │ ASP.NET Identity              │ JWT Validation
        │ Auth Context                  │ Cookie Management             │ Role Authorization
        │ API Client                    │ JWT Generation                │ Business Logic
        │                               │                               │
        └─────────────────┬─────────────┴─────────────┬─────────────────┘
                          │                           │
                          ▼                           ▼
                ┌──────────────────────────────────────────┐
                │         PostgreSQL Database              │
                │            (Port 5433)                   │
                │                                          │
                │  • AspNetUsers (Identity)                │
                │  • AspNetRoles (Roles)                   │
                │  • AspNetUserRoles (Assignments)         │
                │  • Application Tables (Events, etc.)     │
                └──────────────────────────────────────────┘
```

### 2. Service-to-Service Authentication Pattern
```
Web Service Container → API Service Container:
1. User authenticates with cookie-based login
2. Web service generates JWT for API calls
3. All React → API calls include JWT Bearer token
4. API validates JWT and processes request
```

## Network Configuration

### Docker Network Details
- **Network Name**: `witchcity-net`
- **Network Type**: Bridge network
- **IP Range**: `172.20.0.0/16`
- **Isolation**: Containers can communicate via service names

### Container Network Assignments
```yaml
services:
  react-web:
    networks:
      witchcity-net:
        ipv4_address: 172.20.0.10
        
  api-service:
    networks:
      witchcity-net:
        ipv4_address: 172.20.0.20
        
  postgres-db:
    networks:
      witchcity-net:
        ipv4_address: 172.20.0.30
        
  test-runner:
    networks:
      witchcity-net:
        ipv4_address: 172.20.0.40
```

## Port Mapping Strategy

### External to Internal Port Mapping
| Service | External Port | Internal Port | Protocol | Purpose |
|---------|--------------|---------------|----------|---------|
| React Web | 5173 | 5173 | HTTP | Development server with hot reload |
| API Service | 5655 | 8080 | HTTP | .NET Minimal API endpoints |
| PostgreSQL | 5433 | 5432 | TCP | Database connections |
| Test Server | 8080 | 3000 | HTTP | Test runner and reports |

### Development vs Production Ports
```yaml
# Development (docker-compose.dev.yml)
ports:
  - "5173:5173"  # React - matches existing dev setup
  - "5655:8080"  # API - matches existing dev setup  
  - "5433:5432"  # PostgreSQL - non-standard to avoid conflicts
  - "8080:3000"  # Tests - accessible for debugging

# Production (docker-compose.prod.yml)
ports:
  - "80:5173"    # React - standard HTTP
  - "8080:8080"  # API - standard app port
  - "5432:5432"  # PostgreSQL - standard port (internal only)
  # No test container in production
```

## Volume Mount Configuration

### Development Volume Strategy
```yaml
# React Container - Hot Reload
volumes:
  - ./apps/web:/app:cached              # Source code
  - /app/node_modules                   # Prevent overwrite
  - node_modules_cache:/app/.cache      # Build cache

# API Container - Hot Reload  
volumes:
  - ./apps/api:/app:cached              # Source code
  - /app/bin                            # Prevent overwrite
  - /app/obj                            # Prevent overwrite
  - nuget_cache:/root/.nuget/packages   # Package cache

# PostgreSQL Container - Data Persistence
volumes:
  - postgres_data:/var/lib/postgresql/data       # Database files
  - ./apps/api/Data/Init:/docker-entrypoint-initdb.d  # Init scripts

# Test Container - Test Assets
volumes:
  - ./tests:/app/tests:ro               # Test files (read-only)
  - test_reports:/app/reports           # Test output
  - test_screenshots:/app/screenshots   # Playwright screenshots
```

### Volume Types and Purposes
| Volume Type | Purpose | Persistence | Performance |
|------------|---------|-------------|-------------|
| **Bind Mounts** | Source code hot reload | Host filesystem | High I/O for dev |
| **Named Volumes** | Database/cache storage | Docker managed | Optimized |
| **Anonymous Volumes** | Temp/build artifacts | Container lifetime | Fast |

## Environment Variable Flow

### Configuration Strategy
```
Host Environment → .env files → Docker Compose → Container Environment
```

### Environment Configuration per Container
```yaml
# React Container Environment
environment:
  - NODE_ENV=development
  - VITE_API_URL=http://api-service:8080
  - VITE_APP_NAME=WitchCityRope
  - CHOKIDAR_USEPOLLING=true  # Hot reload in Docker

# API Container Environment  
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Host=postgres-db;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
  - Authentication__JwtSecret=${JWT_SECRET}
  - Authentication__Issuer=http://api-service:8080
  - Authentication__Audience=witchcity-react

# PostgreSQL Container Environment
environment:
  - POSTGRES_DB=witchcityrope
  - POSTGRES_USER=postgres  
  - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
  - POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C

# Test Container Environment
environment:
  - NODE_ENV=test
  - API_BASE_URL=http://api-service:8080
  - WEB_BASE_URL=http://react-web:5173
  - DB_CONNECTION_STRING=Host=postgres-db;Database=witchcityrope_test;Username=postgres;Password=${POSTGRES_PASSWORD}
```

## Container Dependencies and Startup Order

### Service Dependency Chain
```yaml
# Startup Order (managed by depends_on and healthchecks)
1. postgres-db     # Database must be ready first
2. api-service     # API needs database connection
3. react-web       # React needs API for authentication
4. test-runner     # Tests need all services running
```

### Health Check Configuration
```yaml
# PostgreSQL Health Check
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope"]
  interval: 5s
  timeout: 5s
  retries: 5

# API Health Check  
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
  interval: 10s
  timeout: 5s
  retries: 3

# React Health Check
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:5173 || exit 1"]
  interval: 10s
  timeout: 5s
  retries: 3
```

## Security Considerations in Containers

### Container Security Patterns
1. **Network Isolation**: Services communicate only through defined network
2. **Minimal Attack Surface**: Only necessary ports exposed externally
3. **Secret Management**: Environment variables for sensitive data
4. **Non-Root Execution**: All services run as non-root users
5. **Resource Limits**: CPU/memory limits prevent resource exhaustion

### Authentication Security in Containers
```yaml
# Security-first container configuration
security_opt:
  - no-new-privileges:true
user: "1001:1001"  # Non-root user
read_only: true    # Read-only filesystem where possible
tmpfs:
  - /tmp:noexec,nosuid,size=128m
```

## Development Experience Optimizations

### Hot Reload Configuration
```yaml
# React Hot Reload (Vite)
- Bind mount source code
- Enable polling for file changes
- Preserve node_modules in container
- Fast refresh for development

# .NET Hot Reload (dotnet watch)  
- Bind mount source code
- Exclude bin/obj directories
- Watch for C# file changes
- Automatic rebuild and restart
```

### Debug Access Points
```yaml
# Development Debug Ports (docker-compose.dev.yml only)
react-web:
  ports:
    - "5173:5173"  # Vite dev server
    - "24678:24678"  # Vite HMR
    
api-service:
  ports:
    - "5655:8080"  # API endpoints
    - "40000:40000"  # .NET debugger port
```

## Resource Allocation

### Container Resource Limits
```yaml
# Production Resource Limits
react-web:
  deploy:
    resources:
      limits:
        cpus: '0.5'
        memory: 512M
      reservations:
        cpus: '0.25'
        memory: 256M

api-service:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 1G
      reservations:
        cpus: '0.5'
        memory: 512M

postgres-db:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 2G
      reservations:
        cpus: '0.5'
        memory: 1G
```

## Container Image Strategy

### Multi-Stage Dockerfile Pattern
```dockerfile
# React Container - Multi-stage for production optimization
FROM node:20-alpine AS development
# Development dependencies and hot reload setup

FROM node:20-alpine AS build  
# Production build stage

FROM nginx:alpine AS production
# Serve static files in production

# API Container - Multi-stage for optimization
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
# Development with hot reload

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Build and publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
# Runtime-only production image
```

This architecture ensures that the proven authentication pattern (hybrid JWT + HttpOnly Cookies) works seamlessly in a containerized environment while maintaining development productivity through hot reload and debugging capabilities.