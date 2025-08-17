# Developer Workflow Design - Docker Authentication Development Experience
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Overview

This document outlines the developer experience for working with the containerized WitchCityRope authentication system, including workflow diagrams for starting containers, debugging, hot reload, and testing scenarios.

## Developer Workflow Visual Overview

### Daily Development Workflow
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        DEVELOPER DAILY WORKFLOW                                 │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ 1. Project  │    │ 2. Start    │    │ 3. Develop  │    │ 4. Test &   │
│    Setup    │───►│ Containers  │───►│ & Debug     │───►│   Commit    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ • git pull  │    │ • ./dev.sh  │    │ • Code edit │    │ • Run tests │
│ • env check │    │ • Check     │    │ • Hot reload│    │ • Git add   │
│ • deps sync │    │   health    │    │ • Debug API │    │ • Commit    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

## Container Startup Workflow

### 1. Initial Project Setup
```
Developer Machine                                Docker Environment
┌─────────────────┐                             ┌─────────────────┐
│                 │                             │                 │
│ git clone repo  │                             │                 │
│ cd project      │                             │                 │
│ cp .env.example │                             │                 │
│    .env.dev     │                             │                 │
│                 │                             │                 │
└─────────────────┘                             └─────────────────┘

Prerequisites Checklist:
┌─────────────────────────────────────────────────────────────────┐
│ ☐ Docker Engine 24.0+ installed                               │
│ ☐ Docker Compose v2.20+ installed                             │
│ ☐ Git repository cloned                                       │
│ ☐ Environment file configured (.env.development)              │
│ ☐ Port conflicts checked (5173, 5655, 5433, 8080)           │
│ ☐ Sufficient disk space (2GB+ for images)                    │
│ ☐ Network connectivity for image pulls                        │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Container Startup Sequence
```
Command: ./dev.sh (or docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d)

Step 1: Network Creation
┌─────────────────────────────────────────────────────────────────┐
│ Creating network "witchcity-net" with driver "bridge"         │
│ Network: 172.20.0.0/16                                        │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~2 seconds

Step 2: Volume Creation  
┌─────────────────────────────────────────────────────────────────┐
│ Creating volume "postgres_data"                               │
│ Creating volume "node_modules_cache"                          │
│ Creating volume "nuget_cache"                                 │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~3 seconds

Step 3: Database Container Startup
┌─────────────────────────────────────────────────────────────────┐
│ postgres-db: Pulling image postgres:16-alpine                 │
│ postgres-db: Container starting on 172.20.0.30:5432          │
│ postgres-db: Initializing database...                         │
│ postgres-db: Running init scripts from /docker-entrypoint-...│
│ postgres-db: Health check: pg_isready ✓                      │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~15-30 seconds (first run)

Step 4: API Container Startup (waits for DB health)
┌─────────────────────────────────────────────────────────────────┐
│ api-service: Building from Dockerfile.dev                     │
│ api-service: Installing .NET dependencies                     │
│ api-service: Starting dotnet watch on 172.20.0.20:8080      │
│ api-service: Entity Framework migrations running...           │
│ api-service: Health check: /health endpoint ✓                │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~45-60 seconds (first run)

Step 5: React Container Startup (waits for API)
┌─────────────────────────────────────────────────────────────────┐
│ react-web: Building from Dockerfile.dev                       │
│ react-web: Installing npm dependencies                        │
│ react-web: Starting Vite dev server on 172.20.0.10:5173     │
│ react-web: Hot Module Replacement enabled                     │
│ react-web: Health check: Vite server ready ✓                 │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~30-45 seconds (first run)

Step 6: Test Container Startup (optional for dev)
┌─────────────────────────────────────────────────────────────────┐
│ test-runner: Building test environment                        │
│ test-runner: Installing test dependencies                     │
│ test-runner: Test server ready on 172.20.0.40:3000          │
└─────────────────────────────────────────────────────────────────┘
                           ⏰ ~20-30 seconds

TOTAL STARTUP TIME:
• First Run: ~2-3 minutes (includes image pulls and builds)
• Subsequent Runs: ~30-45 seconds (cached images and volumes)
```

### 3. Health Check Verification
```bash
# Automated health check script
#!/bin/bash
echo "🔍 Checking container health..."

# Check all services are running
docker-compose -f docker-compose.yml -f docker-compose.dev.yml ps

# Verify health endpoints
echo "📊 Health Check Results:"
echo "Database: $(curl -s http://localhost:5433 && echo "✓ Connected" || echo "✗ Failed")"
echo "API: $(curl -s http://localhost:5655/health && echo "✓ Healthy" || echo "✗ Failed")"  
echo "React: $(curl -s http://localhost:5173 && echo "✓ Ready" || echo "✗ Failed")"

# Verify authentication endpoints
echo "🔐 Authentication Endpoints:"
echo "Login: $(curl -s -o /dev/null -w "%{http_code}" http://localhost:5655/api/auth/login)"
echo "Register: $(curl -s -o /dev/null -w "%{http_code}" http://localhost:5655/api/auth/register)"

echo "🚀 Development environment ready!"
echo "   React: http://localhost:5173"
echo "   API: http://localhost:5655"
echo "   Database: localhost:5433"
```

## Hot Reload Development Workflow

### React Hot Reload Flow
```
Developer                 File System               react-web Container               Browser
    │                         │                           │                         │
    │ 1. Edit React file      │                           │                         │
    │────────────────────────►│                           │                         │
    │                         │ 2. File change detected  │                         │
    │                         │──────────────────────────►│                         │
    │                         │                           │ 3. Vite HMR rebuild    │
    │                         │                           │    (~100-300ms)        │
    │                         │                           │ 4. WebSocket update    │
    │                         │                           │────────────────────────►│
    │                         │                           │                         │ 5. Hot update
    │                         │                           │                         │    (no refresh)
    │ 6. See changes instantly│                           │                         │
    │◄────────────────────────┴───────────────────────────┴─────────────────────────┘
```

**Hot Reload Configuration**:
```yaml
# react-web container hot reload setup
react-web:
  volumes:
    - ./apps/web:/app:cached         # Bind mount source code
    - node_modules_cache:/app/node_modules  # Preserve dependencies
    - /app/.vite                     # Build cache
  environment:
    - CHOKIDAR_USEPOLLING=true       # File watching in Docker
    - VITE_HMR_PORT=24678           # Hot module replacement port
  ports:
    - "5173:5173"                   # Dev server
    - "24678:24678"                 # HMR WebSocket
```

### .NET API Hot Reload Flow
```
Developer               File System            api-service Container         Database
    │                       │                        │                        │
    │ 1. Edit C# file        │                        │                        │
    │───────────────────────►│                        │                        │
    │                       │ 2. File change         │                        │
    │                       │───────────────────────►│                        │
    │                       │                        │ 3. dotnet watch        │
    │                       │                        │    detects change      │
    │                       │                        │ 4. Recompile & restart │
    │                       │                        │    (~2-5 seconds)      │
    │                       │                        │ 5. Reconnect to DB     │
    │                       │                        │───────────────────────►│
    │ 6. API ready with     │                        │                        │
    │    new changes        │                        │                        │
    │◄──────────────────────┴────────────────────────┘                        │
```

**API Hot Reload Configuration**:
```yaml
# api-service container hot reload setup
api-service:
  volumes:
    - ./apps/api:/app:cached              # Bind mount source code
    - nuget_cache:/root/.nuget/packages   # Package cache
    - /app/bin                            # Exclude build artifacts
    - /app/obj                            # Exclude build artifacts
  command: ["dotnet", "watch", "run", "--project", "/app/WitchCityRope.Api.csproj"]
  environment:
    - DOTNET_USE_POLLING_FILE_WATCHER=true  # File watching in Docker
```

## Debugging Workflow

### React Debugging
```
Developer IDE               Browser DevTools          react-web Container
      │                          │                          │
      │ 1. Set breakpoint        │                          │
      │     in VS Code           │                          │
      │ 2. Attach debugger       │                          │
      │     to browser           │                          │
      │                          │ 3. Trigger action       │
      │                          │     (login, etc.)       │
      │                          │                          │ 4. Execute code
      │                          │                          │    with source maps
      │ 5. Breakpoint hit        │                          │
      │◄─────────────────────────┴──────────────────────────┘
      │ 6. Inspect variables,    │
      │    step through code     │
```

**React Debug Configuration**:
```json
// .vscode/launch.json
{
  "name": "Debug React in Container",
  "type": "node",
  "request": "attach", 
  "port": 9229,
  "address": "localhost",
  "localRoot": "${workspaceFolder}/apps/web",
  "remoteRoot": "/app",
  "sourceMaps": true
}
```

### API Debugging
```
Developer IDE               api-service Container      Database
      │                          │                       │
      │ 1. Set breakpoint        │                       │
      │     in C# code           │                       │
      │ 2. Attach debugger       │                       │
      │     to container         │                       │
      │                          │ 3. API request        │
      │                          │     triggers method   │
      │ 4. Breakpoint hit        │                       │
      │◄─────────────────────────┤                       │
      │ 5. Step through,         │                       │
      │    inspect objects       │                       │
      │ 6. Check DB queries      │ 7. Execute SQL        │
      │                          │──────────────────────►│
```

**API Debug Configuration**:
```yaml
# api-service debug setup
api-service:
  ports:
    - "5655:8080"    # API endpoints
    - "40000:40000"  # Debug port
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - DOTNET_RUNNING_IN_CONTAINER=true
```

### Database Debugging
```bash
# Connect to PostgreSQL container for debugging
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec postgres-db psql -U postgres -d witchcityrope

# View authentication tables
\dt auth.*

# Check user data
SELECT * FROM auth."AspNetUsers";

# Check role assignments  
SELECT u."Email", r."Name" 
FROM auth."AspNetUsers" u
JOIN auth."AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN auth."AspNetRoles" r ON ur."RoleId" = r."Id";

# Monitor authentication logs in real-time
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f api-service | grep -i auth
```

## Testing Workflow

### Test Execution Flow
```
Developer                 test-runner Container           Services Under Test
    │                           │                         │
    │ 1. npm run test           │                         │
    │──────────────────────────►│                         │
    │                           │ 2. Start Playwright    │
    │                           │ 3. Navigate to app     │
    │                           │────────────────────────►│ react-web:5173
    │                           │                         │
    │                           │ 4. Test authentication │
    │                           │────────────────────────►│ api-service:8080
    │                           │                         │
    │                           │ 5. Verify database     │
    │                           │────────────────────────►│ postgres-db:5432
    │                           │                         │
    │                           │ 6. Generate reports    │
    │ 7. View test results      │                         │
    │◄──────────────────────────┤                         │
    │ 8. Screenshots/videos     │                         │
    │    if tests fail          │                         │
```

### Test Commands and Workflow
```bash
# Run all tests
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --build --abort-on-container-exit

# Run specific test suites
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:auth
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:e2e
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:api

# Interactive test debugging
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner npm run test:debug

# View test results
docker-compose -f docker-compose.yml -f docker-compose.test.yml run test-runner cat /app/reports/test-results.html
```

### Test Output Locations
```
project-root/
├── test-reports/           # Mounted from container
│   ├── junit.xml          # CI/CD integration
│   ├── coverage/          # Code coverage reports
│   ├── screenshots/       # Test failure screenshots
│   └── videos/           # Test execution recordings
```

## Log Management and Monitoring

### Log Viewing Commands
```bash
# View all container logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# View specific service logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f react-web
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f api-service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f postgres-db

# Filter authentication-related logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs api-service | grep -i "auth\|login\|jwt"

# Real-time error monitoring
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f | grep -i "error\|exception\|fail"
```

### Log Structure and Locations
```
Container Logs Location:
┌─────────────────────────────────────────────────────────────┐
│ react-web:                                                  │
│   • Vite dev server logs                                   │
│   • Browser console errors (via source maps)              │
│   • Hot reload notifications                               │
│                                                             │
│ api-service:                                                │
│   • ASP.NET Core request logs                             │
│   • Authentication success/failure                         │
│   • Database connection status                             │
│   • JWT generation/validation                              │
│                                                             │
│ postgres-db:                                                │
│   • SQL query logs (if enabled)                           │
│   • Connection attempts                                    │
│   • Performance warnings                                   │
└─────────────────────────────────────────────────────────────┘
```

## Performance Monitoring

### Development Performance Metrics
```bash
# Container resource usage
docker stats

# Service response times
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:5655/health

# Database performance
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec postgres-db \
  psql -U postgres -d witchcityrope -c "SELECT * FROM pg_stat_activity WHERE state = 'active';"

# React build performance
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec react-web npm run build:analyze
```

## Troubleshooting Common Issues

### Container Communication Issues
```bash
# Test network connectivity between containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec react-web ping api-service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec api-service ping postgres-db

# Check container IP assignments
docker network inspect witchcity-net

# Verify service discovery
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec react-web nslookup api-service
```

### Authentication Issues Debug Flow
```
Issue: Login not working
┌─────────────────────────────────────────────────────────────┐
│ 1. Check React container can reach API:                    │
│    curl http://localhost:5655/health                       │
│                                                             │
│ 2. Verify API can reach database:                         │
│    docker exec api-service curl localhost:8080/health     │
│                                                             │
│ 3. Check authentication endpoint:                          │
│    curl -X POST http://localhost:5655/api/auth/login      │
│                                                             │
│ 4. Verify database has user data:                         │
│    docker exec postgres-db psql -U postgres -d witchcity... │
│                                                             │
│ 5. Check JWT secret configuration:                        │
│    docker exec api-service env | grep JWT                 │
└─────────────────────────────────────────────────────────────┘
```

### Container Restart Procedures
```bash
# Restart individual services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart react-web
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart api-service

# Restart with fresh build
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build react-web

# Complete environment reset
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

This developer workflow design ensures that working with the containerized authentication system is as smooth and productive as the current development environment, while providing additional benefits like environment consistency and easier debugging.