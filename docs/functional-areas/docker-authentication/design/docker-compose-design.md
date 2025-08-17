# Docker Compose Design - Multi-Environment Configuration
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Implementation Phase -->

## Overview

This document provides the complete docker-compose configuration design for WitchCityRope across all environments (development, test, production). The design implements a layered approach where a base configuration is extended by environment-specific overrides, maintaining the proven authentication architecture while optimizing for each deployment scenario.

## Architecture Design Principles

### Layered Configuration Strategy
- **Base Layer** (`docker-compose.yml`): Shared service definitions, networks, and volumes
- **Environment Layers** (`docker-compose.{env}.yml`): Environment-specific overrides
- **Composition Pattern**: Multiple files combined via `-f` flags for flexible deployment

### Service Architecture
- **react-web**: React + Vite frontend with hot reload (dev) / Nginx static serving (prod)
- **api-service**: .NET 9 Minimal API with JWT authentication
- **postgres-db**: PostgreSQL 16 with proper connection pooling
- **test-server**: Python HTTP server for development testing (dev only)

### Network Design
- **Custom Bridge Network**: `witchcity-net` with defined subnet (172.20.0.0/16)
- **Service Discovery**: Container-to-container communication via service names
- **Port Strategy**: External ports only in dev/prod, internal-only for test isolation

### Volume Strategy
- **Named Volumes**: Persistent data (postgres_data, nuget_cache, node_modules_cache)
- **Bind Mounts**: Development source code mounting for hot reload
- **Anonymous Volumes**: Build artifacts exclusion in development

## File Structure and Usage

### Base Configuration
```
docker-compose.yml                 # Shared service definitions
├── Services: postgres-db, api-service, react-web
├── Networks: witchcity-net
├── Volumes: postgres_data, nuget_cache, node_modules_cache
└── Health Checks: Database connectivity, API endpoints
```

### Environment-Specific Overrides
```
docker-compose.dev.yml            # Development with hot reload
├── Volume mounts for source code
├── External ports for all services
├── Debug configuration
└── Additional test-server service

docker-compose.test.yml           # CI/CD testing
├── Ephemeral volumes
├── Test-specific database
├── No external ports (isolation)
└── Optimized for automated testing

docker-compose.prod.yml           # Production deployment
├── Resource limits and security
├── Immutable containers
├── HTTPS enforcement
└── Monitoring integration
```

## Service Configuration Design

### PostgreSQL Database Service
- **Base**: PostgreSQL 16 Alpine with health checks
- **Development**: External port 5433, development seed data
- **Test**: Separate test database with fixtures, no external access
- **Production**: Resource limits, security hardening, backup mounts

### API Service (.NET 9)
- **Base**: Minimal API with JWT authentication and CORS
- **Development**: Hot reload with `dotnet watch`, debug ports, relaxed security
- **Test**: Built containers for consistency, test-specific configuration
- **Production**: Optimized runtime, security constraints, monitoring

### React Web Service
- **Base**: Vite-based React application with environment variables
- **Development**: HMR with file watching, development proxy configuration
- **Test**: Built static assets for consistent testing
- **Production**: Nginx-served static files with SSL termination

### Test Server (Development Only)
- **Purpose**: Python HTTP server for development testing scenarios
- **Configuration**: Simple HTTP server on port 8080
- **Usage**: Integration testing and API endpoint validation

## Authentication Architecture Integration

### JWT Service-to-Service Pattern
- **Token Issuer**: API service generates tokens for authenticated users
- **Token Audience**: React application and internal services
- **Secret Management**: Environment-specific JWT secrets
- **CORS Configuration**: Environment-appropriate allowed origins

### Cookie-Based User Authentication
- **HttpOnly Cookies**: Secure cookie configuration across environments
- **SameSite Policy**: Environment-specific security settings
- **Domain Configuration**: Development vs production domain handling
- **Session Management**: Persistent sessions with appropriate timeouts

### Environment-Specific Security
- **Development**: HTTP allowed, relaxed CORS, longer token lifetime
- **Test**: Secure testing with isolated authentication data
- **Production**: HTTPS enforced, strict CORS, short token lifetime, audit logging

## Environment Variable Strategy

### Base Configuration Variables
```bash
# Shared across all environments
POSTGRES_DB=witchcityrope
POSTGRES_USER=postgres
VITE_APP_NAME=WitchCityRope
ASPNETCORE_URLS=http://+:8080
```

### Development Environment Variables
```bash
# Development-specific
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=devpass123
JWT_SECRET=dev-jwt-secret-for-local-testing
VITE_API_URL=http://localhost:5655
CORS__AllowedOrigins=http://localhost:5173
```

### Test Environment Variables
```bash
# Test-specific
ASPNETCORE_ENVIRONMENT=Test
POSTGRES_PASSWORD=testpass123
JWT_SECRET=test-jwt-secret-for-ci
VITE_API_URL=http://api-service:8080
CI=true
```

### Production Environment Variables
```bash
# Production-specific (secrets managed externally)
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_PASSWORD=${SECRET_POSTGRES_PASSWORD}
JWT_SECRET=${SECRET_JWT_KEY}
VITE_API_URL=https://api.witchcityrope.com
AUTHENTICATION__REQUIREHTTPS=true
```

## Volume and Data Management

### Development Volumes
- **Source Code Mounts**: Live editing with hot reload
- **Package Caches**: NuGet and npm package persistence
- **Build Exclusions**: Prevent host/container build conflicts

### Test Volumes
- **Ephemeral Data**: Clean state for each test run
- **Test Reports**: Output collection for CI/CD
- **Fixture Data**: Consistent test data setup

### Production Volumes
- **Persistent Data**: Database and application data
- **Configuration**: Read-only configuration mounts
- **Logs**: Centralized logging output

## Security Considerations

### Development Security
- **Relaxed Policies**: HTTP allowed, permissive CORS
- **Debug Access**: Open debug ports and detailed logging
- **Simple Secrets**: Easy-to-use development credentials

### Test Security
- **Isolation**: No external network access
- **Clean State**: Fresh authentication data per test run
- **Fast Validation**: Optimized for CI/CD performance

### Production Security
- **Hardened Containers**: Non-root users, read-only filesystems
- **Secret Management**: External secret injection
- **Network Security**: Internal-only database access
- **Resource Limits**: DoS protection via resource constraints

## Health Check and Dependency Management

### Health Check Strategy
- **Database**: `pg_isready` with proper credentials
- **API**: `/health` endpoint with dependency validation
- **React**: HTTP response check on main port
- **Dependency Order**: Services start only after dependencies are healthy

### Service Dependencies
```
react-web → api-service → postgres-db
test-server → api-service, react-web
```

## Usage Commands and Workflows

### Development Workflow
```bash
# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Monitor logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Rebuild specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build api-service
```

### Test Workflow
```bash
# Run test suite
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --build --abort-on-container-exit

# Clean test environment
docker-compose -f docker-compose.yml -f docker-compose.test.yml down -v
```

### Production Deployment
```bash
# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Rolling update
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps api-service
```

## Implementation Benefits

### Development Benefits
- **Hot Reload**: Immediate feedback for code changes
- **Debug Access**: Full debugging capabilities
- **Easy Setup**: Single command development environment
- **Service Integration**: Complete authentication flow testing

### Test Benefits
- **Isolation**: Clean test environment per run
- **Consistency**: Identical containers across CI/CD
- **Performance**: Optimized for automated testing
- **Coverage**: Full authentication flow validation

### Production Benefits
- **Security**: Hardened containers with minimal attack surface
- **Performance**: Optimized runtime configuration
- **Scalability**: Resource-managed services
- **Reliability**: Health checks and dependency management

This design provides a robust, secure, and maintainable Docker configuration that supports the complete development lifecycle while preserving the proven authentication architecture across all environments.