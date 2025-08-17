# Existing Docker Knowledge for React + .NET Implementation

<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian -->
<!-- Status: Active -->
<!-- Source: Consolidated from Blazor Docker implementation -->

## Overview

This document consolidates relevant Docker knowledge from the previous Blazor implementation that applies to containerizing the current React + .NET API + PostgreSQL authentication system.

**Source Files**: Blazor Docker guides archived to `/docs/archives/docker-blazor-setup/`
**Target Architecture**: React (Vite) + .NET Minimal API + PostgreSQL + Authentication

## Service Architecture (Adapted from Blazor)

### Current Working System (Non-Docker)
- **React (Vite)**: http://localhost:5173
- **API (.NET Minimal)**: http://localhost:5655  
- **PostgreSQL**: localhost:5432 (native)
- **Authentication**: Hybrid JWT + HttpOnly Cookies (WORKING PERFECTLY)

### Target Docker Architecture
- **React Container**: Port 5173 → Internal 3000 (Vite standard)
- **API Container**: Port 5655 → Internal 8080 (.NET standard)
- **PostgreSQL Container**: Port 5433 → Internal 5432
- **Networking**: Bridge network for service-to-service communication

## Port Configuration Lessons

### Successful Port Patterns (from Blazor implementation)
- **External ports**: 5651-5654 range was used successfully
- **Internal container ports**: 8080/8081 for .NET services worked well
- **Database port**: 5433 external → 5432 internal is proven pattern
- **Avoid conflicts**: Check for port usage with `lsof -i :PORT`

### React + Vite Adaptations
- **Vite dev server**: Default internal port 3000 or 5173
- **Hot reload**: Requires proper volume mounting for file watching
- **API calls**: Must use container network names, not localhost

## Database Configuration

### PostgreSQL Container Setup (Proven)
```yaml
postgres:
  image: postgres:16-alpine
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: WitchCity2024!
    POSTGRES_DB: witchcityrope_db
  ports:
    - "5433:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
```

### Connection String Patterns
- **From containers**: `Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **From host**: `Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **Service discovery**: Use container service names in connection strings

## Service-to-Service Communication

### Critical for Authentication
The biggest challenge will be preserving the JWT service-to-service authentication between React and API containers.

### Network Configuration Requirements
- **Bridge network**: Allow containers to communicate by service name
- **API calls from React**: Must target `http://api:8080` not `localhost:5655`
- **Environment variables**: API base URL must be configurable for container vs host

### Authentication Flow Preservation
1. React login form → API `/auth/login` endpoint
2. API sets HttpOnly cookie + returns JWT
3. React stores auth state in context
4. Protected API calls include JWT in service-to-service communication
5. All flows must work identically in containers

## Development Workflow Patterns

### Hot Reload Requirements

#### React (Vite) Hot Reload
- **Volume mounting**: `/app/src:/app/src` for source file watching
- **Node modules**: Separate volume to avoid conflicts
- **Environment**: `NODE_ENV=development`
- **Vite config**: Must allow connections from container network

#### .NET API Hot Reload
- **Volume mounting**: `/app:/app` for source watching
- **dotnet watch**: Command `dotnet watch run --urls http://+:8080`
- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`
- **File watching**: Ensure file system notifications work in container

### Development Helper Scripts (Adapted)
The Blazor implementation had successful patterns:
- **dev.sh**: Interactive menu for container management
- **restart-web.sh**: Quick restart for hot reload failures
- **Health checks**: Container health monitoring

## Docker Compose Patterns

### Multi-Stage Builds (for Production)
```dockerfile
# Development stage
FROM node:18-alpine as development
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 3000
CMD ["npm", "run", "dev"]

# Production stage
FROM nginx:alpine as production
COPY --from=build /app/dist /usr/share/nginx/html
```

### Development vs Production Targets
- **Development**: Use `target: development` with hot reload
- **Production**: Use `target: production` with optimized builds
- **Environment variables**: Control build target with `BUILD_TARGET`

## Environment Variable Management

### API Configuration
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
JWT_SECRET=YourSuperSecretKeyForDevelopmentPurposesOnly!123
```

### React Configuration
```env
VITE_API_BASE_URL=http://localhost:5655
NODE_ENV=development
```

**Critical**: API base URL must be configurable for container vs host environments

## Health Checks and Monitoring

### Container Health Checks
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Monitoring Commands
- `docker-compose ps` - Service status
- `docker-compose logs -f [service]` - Real-time logs
- `docker stats` - Resource usage
- Container health status inspection

## Troubleshooting Patterns

### Hot Reload Failures
**Problem**: File changes not triggering reload in containers
**Solutions**:
1. Check volume mount paths
2. Verify file watching permissions
3. Restart container: `docker-compose restart [service]`
4. Full rebuild: `docker-compose up -d --build`

### Service Communication Issues
**Problem**: React can't reach API container
**Solutions**:
1. Verify network configuration
2. Check service names in API calls
3. Confirm ports are properly exposed
4. Test with `docker-compose exec react ping api`

### Database Connection Problems
**Problem**: API can't connect to PostgreSQL
**Solutions**:
1. Verify connection string uses service name `postgres`
2. Check database container is healthy
3. Confirm port 5432 is exposed internally
4. Test connection: `docker-compose exec api ping postgres`

## Security Considerations

### Development Environment
- **JWT secrets**: Use development-only values
- **CORS settings**: Relaxed for development
- **HTTPS**: Can be disabled for development containers
- **Environment files**: Never commit `.env` with real secrets

### Volume Mounting Security
- **Source code**: Mount with appropriate permissions
- **Node modules**: Use named volumes to avoid conflicts
- **Database data**: Persistent volumes for data retention

## Performance Optimization

### Container Resource Allocation
- **Memory limits**: Set appropriate limits for development
- **CPU limits**: Avoid over-allocation
- **Build cache**: Use multi-stage builds with caching
- **Volume optimization**: Separate volumes for dependencies

### Build Optimization
- **Layer caching**: Order Dockerfile instructions for best caching
- **Dependency installation**: Copy package files first
- **Source code**: Copy source after dependencies
- **BuildKit**: Enable for faster builds

## Migration from Blazor Patterns

### What Works Directly
- PostgreSQL container configuration
- Port mapping strategies
- Environment variable patterns
- Docker compose structure
- Health check implementations
- Volume mounting concepts

### What Needs Adaptation
- **Web technology**: Blazor Server → React + Vite
- **Build process**: .NET publish → npm build
- **Hot reload**: ASP.NET Core → Vite HMR
- **Static files**: Blazor assets → React build output
- **Development server**: Kestrel → Vite dev server

### Critical Differences
- **Authentication handling**: Must preserve exact JWT + HttpOnly cookie flow
- **Service calls**: React axios/fetch → API endpoints instead of Blazor Server
- **Session management**: Client-side state instead of server-side Blazor circuits

## Implementation Priority

### Phase 1: Basic Containerization
1. **PostgreSQL**: Use proven container config
2. **.NET API**: Adapt from Blazor patterns with minimal changes
3. **React**: New Vite-based container with volume mounting

### Phase 2: Service Integration
1. **Networking**: Configure bridge network for service communication
2. **Environment**: Set up configurable API URLs
3. **Authentication**: Preserve exact JWT flow in container environment

### Phase 3: Development Experience
1. **Hot reload**: Ensure both React and .NET hot reload work
2. **Helper scripts**: Adapt dev.sh and other utilities
3. **Testing**: Validate E2E tests work with containers

## Success Validation

### Authentication Flow Tests
1. **Registration**: New user signup works in containers
2. **Login**: User login sets cookies and JWT correctly
3. **Protected access**: Authenticated API calls work
4. **Logout**: Session cleanup works properly
5. **Service-to-service**: JWT validation between React and API

### Development Experience Tests
1. **React hot reload**: Component changes update immediately
2. **.NET hot reload**: API changes restart quickly
3. **Database persistence**: Data survives container restarts
4. **E2E tests**: Playwright tests pass against containers

## Lessons Learned from Blazor Implementation

### What Worked Well
- **Port standardization**: Consistent port ranges
- **Environment configuration**: Clear .env file patterns
- **Helper scripts**: Interactive development menus
- **Health checks**: Reliable container monitoring
- **Volume management**: Proper data persistence

### Common Pitfalls to Avoid
- **Port conflicts**: Always check for existing services
- **File permissions**: Ensure proper volume mount permissions
- **Service naming**: Use consistent container names
- **Connection strings**: Don't hardcode localhost in containers
- **Certificate issues**: Handle HTTPS properly or disable for dev

### Docker-Specific Authentication Challenges
1. **Cookie domain**: Ensure cookies work across container network
2. **CORS configuration**: Proper origins for container setup
3. **JWT validation**: Service-to-service auth in container network
4. **Session storage**: Ensure session state works in containers

## Next Steps for Implementation

1. **Analyze current auth**: Document exact authentication flow requirements
2. **Create Dockerfiles**: Separate files for React and .NET API
3. **Design docker-compose**: Multi-service configuration with networking
4. **Test service communication**: Verify React → API calls work in containers
5. **Validate authentication**: Ensure complete auth flow works
6. **Optimize development**: Hot reload and debugging experience
7. **Document deployment**: Complete setup and troubleshooting guide

## Files and References

### Source Documentation (Archived)
- `/docs/archives/docker-blazor-setup/DOCKER_DEV_GUIDE.md`
- `/docs/archives/docker-blazor-setup/DOCKER_QUICK_REFERENCE.md`
- `/docs/archives/docker-blazor-setup/DOCKER_SETUP.md`
- `/docs/archives/docker-blazer-setup/PORT_UPDATE_SUMMARY.md`
- `/docs/archives/docker-blazor-setup/docker-development.md`

### Current Authentication Implementation
- React app at localhost:5173 with working auth flows
- .NET API at localhost:5655 with JWT + HttpOnly cookies
- PostgreSQL database with user management
- Playwright E2E tests validating complete authentication

This consolidated knowledge provides the foundation for containerizing the proven authentication system without breaking any existing functionality.