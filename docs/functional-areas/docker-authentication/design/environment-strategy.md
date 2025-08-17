# Environment Strategy - Docker Multi-Environment Configuration
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Overview

This document outlines the multi-environment Docker strategy for WitchCityRope, showing how different docker-compose files layer to support development, testing, and production environments while maintaining the proven authentication architecture.

## Environment Layering Strategy

### Base Configuration Layer
**File**: `docker-compose.yml`
**Purpose**: Shared configuration across all environments

```yaml
# docker-compose.yml - Base Configuration
version: '3.8'

# Shared network configuration
networks:
  witchcity-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16

# Shared volume definitions
volumes:
  postgres_data:
    driver: local
  node_modules_cache:
    driver: local
  nuget_cache:
    driver: local

# Base service definitions
services:
  postgres-db:
    image: postgres:16-alpine
    networks:
      - witchcity-net
    environment:
      - POSTGRES_DB=witchcityrope
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope"]
      interval: 5s
      timeout: 5s
      retries: 5

  api-service:
    build:
      context: ./apps/api
      dockerfile: Dockerfile
    networks:
      - witchcity-net
    depends_on:
      postgres-db:
        condition: service_healthy
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=postgres-db;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
      - Authentication__JwtSecret=${JWT_SECRET}
      - Authentication__Issuer=http://api-service:8080
      - Authentication__Audience=witchcity-react

  react-web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile
    networks:
      - witchcity-net
    depends_on:
      - api-service
    environment:
      - VITE_API_URL=http://api-service:8080
      - VITE_APP_NAME=WitchCityRope
```

## Development Environment Layer

### Development Override
**File**: `docker-compose.dev.yml`
**Purpose**: Development-specific configurations with hot reload, debugging, and external port access

```yaml
# docker-compose.dev.yml - Development Overrides
version: '3.8'

services:
  postgres-db:
    ports:
      - "5433:5432"  # External access for database tools
    environment:
      - POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C
    volumes:
      - ./apps/api/Data/Init:/docker-entrypoint-initdb.d  # Development seed data

  api-service:
    build:
      context: ./apps/api
      dockerfile: Dockerfile.dev  # Development-specific Dockerfile
      target: development         # Multi-stage development target
    ports:
      - "5655:8080"              # External API access
      - "40000:40000"            # .NET debugger port
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug
      - Authentication__RequireHttps=false
      - CORS__AllowedOrigins=http://localhost:5173
    volumes:
      - ./apps/api:/app:cached                    # Hot reload source code
      - nuget_cache:/root/.nuget/packages         # Package cache
      - /app/bin                                  # Exclude build artifacts
      - /app/obj                                  # Exclude build artifacts
    command: ["dotnet", "watch", "run", "--project", "/app/WitchCityRope.Api.csproj"]

  react-web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile.dev  # Development-specific Dockerfile
      target: development         # Multi-stage development target
    ports:
      - "5173:5173"              # Vite dev server
      - "24678:24678"            # Vite HMR WebSocket
    environment:
      - NODE_ENV=development
      - VITE_API_URL=http://localhost:5655  # External API access for browser
      - CHOKIDAR_USEPOLLING=true            # File watching in Docker
      - VITE_HMR_PORT=24678                 # Hot module replacement
    volumes:
      - ./apps/web:/app:cached              # Hot reload source code
      - node_modules_cache:/app/node_modules # Preserve node_modules
      - /app/.cache                         # Build cache
    command: ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

  # Development-only services
  test-runner:
    build:
      context: ./tests
      dockerfile: Dockerfile.dev
    networks:
      - witchcity-net
    ports:
      - "8080:3000"              # Test runner UI
    depends_on:
      - react-web
      - api-service
    environment:
      - NODE_ENV=test
      - API_BASE_URL=http://api-service:8080
      - WEB_BASE_URL=http://react-web:5173
    volumes:
      - ./tests:/app/tests:ro             # Test files (read-only)
      - test_reports:/app/reports         # Test output
      - test_screenshots:/app/screenshots # Playwright screenshots
    command: ["npm", "run", "test:watch"]  # Watch mode for development

# Development-specific volumes
volumes:
  test_reports:
    driver: local
  test_screenshots:
    driver: local
```

### Development Usage
```bash
# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Watch logs for all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Rebuild specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build react-web
```

## Test Environment Layer

### Test Override
**File**: `docker-compose.test.yml`
**Purpose**: Isolated testing with clean database and CI/CD optimization

```yaml
# docker-compose.test.yml - Test Environment Overrides
version: '3.8'

services:
  postgres-db:
    environment:
      - POSTGRES_DB=witchcityrope_test  # Separate test database
      - POSTGRES_PASSWORD=${POSTGRES_TEST_PASSWORD}
    volumes:
      - postgres_test_data:/var/lib/postgresql/data  # Separate test data
      - ./tests/fixtures/sql:/docker-entrypoint-initdb.d  # Test fixtures
    # No external ports - test-only database

  api-service:
    build:
      context: ./apps/api
      dockerfile: Dockerfile.test  # Test-specific Dockerfile
      target: test                 # Multi-stage test target
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
      - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Warning
      - ConnectionStrings__DefaultConnection=Host=postgres-db;Database=witchcityrope_test;Username=postgres;Password=${POSTGRES_TEST_PASSWORD}
      - Authentication__JwtSecret=${JWT_TEST_SECRET}
      - Authentication__RequireHttps=false
    # No volume mounts - use built code for consistency
    # No external ports - internal testing only

  react-web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile.test  # Test-specific Dockerfile
      target: test                 # Multi-stage test target
    environment:
      - NODE_ENV=test
      - VITE_API_URL=http://api-service:8080
      - CI=true                    # Optimize for CI environment
    # No volume mounts - use built code for consistency
    # No external ports - internal testing only

  test-runner:
    build:
      context: ./tests
      dockerfile: Dockerfile.test
    networks:
      - witchcity-net
    depends_on:
      postgres-db:
        condition: service_healthy
      api-service:
        condition: service_healthy
      react-web:
        condition: service_healthy
    environment:
      - NODE_ENV=test
      - API_BASE_URL=http://api-service:8080
      - WEB_BASE_URL=http://react-web:5173
      - DB_CONNECTION_STRING=Host=postgres-db;Database=witchcityrope_test;Username=postgres;Password=${POSTGRES_TEST_PASSWORD}
      - PLAYWRIGHT_WORKERS=2       # Parallel test execution
      - PLAYWRIGHT_HEADED=false    # Headless for CI
    volumes:
      - test_reports:/app/reports  # Test output for CI
    command: ["npm", "run", "test:ci"]  # CI-optimized test run

# Test-specific volumes
volumes:
  postgres_test_data:
    driver: local
```

### Test Usage
```bash
# Run full test suite
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --build --abort-on-container-exit

# Run specific test type
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:e2e

# Clean test environment
docker-compose -f docker-compose.yml -f docker-compose.test.yml down -v
```

## Production Environment Layer

### Production Override
**File**: `docker-compose.prod.yml`
**Purpose**: Production-optimized configuration with security, performance, and monitoring

```yaml
# docker-compose.prod.yml - Production Overrides
version: '3.8'

services:
  postgres-db:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      - POSTGRES_DB=witchcityrope
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_INITDB_ARGS=--encoding=UTF-8
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./backups:/backups:ro      # Backup mount point
    # No external ports - internal only
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 1G
    security_opt:
      - no-new-privileges:true
    user: "999:999"  # postgres user

  api-service:
    build:
      context: ./apps/api
      dockerfile: Dockerfile.prod  # Production Dockerfile
      target: production          # Optimized production build
    restart: unless-stopped
    ports:
      - "8080:8080"              # Standard application port
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Warning
      - ConnectionStrings__DefaultConnection=Host=postgres-db;Database=witchcityrope;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      - Authentication__JwtSecret=${JWT_SECRET}
      - Authentication__Issuer=${JWT_ISSUER}
      - Authentication__Audience=${JWT_AUDIENCE}
      - Authentication__RequireHttps=true
      - CORS__AllowedOrigins=${ALLOWED_ORIGINS}
    # No volume mounts - immutable container
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
    security_opt:
      - no-new-privileges:true
    user: "1001:1001"  # Non-root user
    read_only: true
    tmpfs:
      - /tmp:noexec,nosuid,size=128m

  react-web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile.prod  # Production Dockerfile with Nginx
      target: production          # Optimized static build
    restart: unless-stopped
    ports:
      - "80:80"                  # Standard HTTP port
      - "443:443"                # HTTPS port
    environment:
      - NODE_ENV=production
    volumes:
      - ./ssl:/etc/nginx/ssl:ro  # SSL certificates
      - ./nginx/conf.d:/etc/nginx/conf.d:ro  # Nginx configuration
    # No source code mounts - immutable container
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    security_opt:
      - no-new-privileges:true
    user: "101:101"  # nginx user

  # Production monitoring services
  nginx-proxy:
    image: nginx:alpine
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
      - nginx_logs:/var/log/nginx
    depends_on:
      - react-web
      - api-service
    deploy:
      resources:
        limits:
          cpus: '0.25'
          memory: 256M

# Production-specific volumes
volumes:
  nginx_logs:
    driver: local
```

### Production Usage
```bash
# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Update specific service
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build api-service
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d api-service

# Production health check
docker-compose -f docker-compose.yml -f docker-compose.prod.yml ps
```

## Environment Configuration Matrix

### Configuration Comparison Table

| Feature | Development | Test | Production |
|---------|-------------|------|------------|
| **Hot Reload** | ✅ Full hot reload | ❌ Built code only | ❌ Immutable containers |
| **External Ports** | ✅ All services | ❌ None (internal only) | ✅ Web/API only |
| **Debug Access** | ✅ Debug ports open | ❌ No debugging | ❌ No debugging |
| **Volume Mounts** | ✅ Source code mounted | ❌ Built containers | ❌ Config only |
| **Log Level** | Debug | Warning | Warning |
| **HTTPS Required** | ❌ HTTP only | ❌ HTTP only | ✅ HTTPS enforced |
| **Resource Limits** | ❌ No limits | ✅ CI-optimized | ✅ Production limits |
| **Security Hardening** | ❌ Developer-friendly | ✅ Test isolation | ✅ Full hardening |
| **Database** | Dev data + seed | Clean test fixtures | Production data |
| **Monitoring** | ❌ Basic logging | ✅ Test reporting | ✅ Full monitoring |

## Environment Variable Strategy

### Environment File Structure
```
project-root/
├── .env                     # Default values (checked into git)
├── .env.development         # Development overrides (gitignored)
├── .env.test               # Test configuration (gitignored)
├── .env.production         # Production secrets (gitignored)
└── .env.example            # Template file (checked into git)
```

### Environment Variable Categories

#### Shared Configuration (.env)
```bash
# Application Configuration
VITE_APP_NAME=WitchCityRope
VITE_APP_VERSION=1.0.0

# Network Configuration
DOCKER_NETWORK=witchcity-net
POSTGRES_DB=witchcityrope

# Development Defaults (overridden per environment)
POSTGRES_PASSWORD=dev_password_change_in_production
JWT_SECRET=dev_jwt_secret_change_in_production
```

#### Development Environment (.env.development)
```bash
# Development-specific settings
NODE_ENV=development
ASPNETCORE_ENVIRONMENT=Development

# Development passwords (simple for local work)
POSTGRES_PASSWORD=devpass123
JWT_SECRET=dev-jwt-secret-for-local-testing

# Development URLs
VITE_API_URL=http://localhost:5655
ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000

# Debug settings
ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug
CHOKIDAR_USEPOLLING=true
```

#### Test Environment (.env.test)
```bash
# Test-specific settings
NODE_ENV=test
ASPNETCORE_ENVIRONMENT=Test

# Test database
POSTGRES_PASSWORD=testpass123
POSTGRES_TEST_PASSWORD=testpass123
JWT_SECRET=test-jwt-secret-for-ci
JWT_TEST_SECRET=test-jwt-secret-for-ci

# Test URLs (internal container communication)
VITE_API_URL=http://api-service:8080
API_BASE_URL=http://api-service:8080
WEB_BASE_URL=http://react-web:5173

# CI optimization
CI=true
PLAYWRIGHT_WORKERS=2
PLAYWRIGHT_HEADED=false
```

#### Production Environment (.env.production)
```bash
# Production settings
NODE_ENV=production
ASPNETCORE_ENVIRONMENT=Production

# Production secrets (managed by deployment system)
POSTGRES_USER=witchcity_prod
POSTGRES_PASSWORD=${SECRET_POSTGRES_PASSWORD}
JWT_SECRET=${SECRET_JWT_KEY}
JWT_ISSUER=https://api.witchcityrope.com
JWT_AUDIENCE=witchcityrope-app

# Production URLs
ALLOWED_ORIGINS=https://witchcityrope.com
VITE_API_URL=https://api.witchcityrope.com

# Security settings
AUTHENTICATION__REQUIREHTTPS=true
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

## Usage Patterns

### Development Workflow
```bash
# Start development environment
./dev.sh                    # Wrapper script
# OR
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f react-web

# Access services
# React: http://localhost:5173
# API: http://localhost:5655
# Database: localhost:5433
# Tests: http://localhost:8080
```

### Testing Workflow
```bash
# Run full test suite
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --build --abort-on-container-exit

# Run specific tests
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:e2e

# Clean test environment
docker-compose -f docker-compose.yml -f docker-compose.test.yml down -v
```

### Production Deployment
```bash
# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml pull
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Rolling update
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build api-service
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps api-service

# Backup database
docker-compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db pg_dump -U postgres witchcityrope > backup.sql
```

## Authentication Considerations per Environment

### Development Authentication
- **HTTP Only**: No HTTPS requirement for local development
- **Relaxed CORS**: Allow localhost origins for testing
- **Debug Tokens**: Longer-lived JWT tokens for easier debugging
- **Test Users**: Pre-seeded test accounts in database

### Test Authentication  
- **Isolated Data**: Separate test database with clean fixtures
- **Fast Authentication**: Simplified token validation for speed
- **Automated Testing**: API endpoints for test user creation/cleanup
- **Security Testing**: Include authentication security tests

### Production Authentication
- **HTTPS Enforced**: All authentication requires secure transport
- **Strict CORS**: Only production domains allowed
- **Short Token Lifetime**: Production-appropriate token expiration
- **Audit Logging**: Full authentication event logging
- **Secret Management**: External secret management integration

This multi-environment strategy ensures that the proven authentication architecture works consistently across all environments while optimizing each environment for its specific purpose and requirements.