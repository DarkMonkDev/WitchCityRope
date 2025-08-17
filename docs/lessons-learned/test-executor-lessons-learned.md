# Test Executor Lessons Learned
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.1 -->
<!-- Owner: Test Team -->
<!-- Status: Active -->

## Overview
Critical lessons learned for the test-executor agent, including mandatory E2E testing prerequisites and common failure patterns.

## E2E Testing Prerequisites - MANDATORY CHECKS

### 🚨 CRITICAL: ALWAYS CHECK DOCKER CONTAINER HEALTH FIRST 🚨

**THIS IS SUPER COMMON AND MUST BE DONE EVERY TIME BEFORE E2E TESTS**

The #1 cause of E2E test failures is unhealthy Docker containers. The test-executor agent MUST verify the environment before attempting any E2E tests.

### Pre-Test Environment Validation Checklist

**1. Docker Container Status Check**
```bash
# Check all WitchCity containers are running
docker-compose ps

# Expected output: All containers should show "Up" status
# Name                State          Ports
# witchcityrope-web  Up            0.0.0.0:5173->3000/tcp
# witchcityrope-api  Up (healthy)  0.0.0.0:5655->8080/tcp
# witchcityrope-postgres Up (healthy) 0.0.0.0:5433->5432/tcp

# Quick health check
curl -f http://localhost:5173 && echo "✅ React app ready"
curl -f http://localhost:5655/health && echo "✅ API healthy"
docker-compose exec postgres pg_isready -U postgres && echo "✅ Database ready"
```

**2. Container Health Status**
```bash
# Check specific health status for each service
docker-compose exec postgres pg_isready -U postgres -d witchcityrope_dev
curl -f http://localhost:5655/health
curl -f http://localhost:5173

# Check authentication system health
curl -f http://localhost:5655/api/auth/health

# Expected output: All commands should succeed without errors
```

**3. Check for Compilation Errors**
```bash
# Check web service logs for compilation errors
docker-compose logs --tail 50 web | grep -i error

# Check API service logs for compilation errors  
docker-compose logs --tail 50 api | grep -i error

# Check for database connection issues
docker-compose logs --tail 50 api | grep -i "database\|postgres\|connection"

# If ANY compilation errors found, STOP and restart containers
docker-compose restart
```

**4. Service Health Endpoints**
```bash
# Verify web service responds
curl -f http://localhost:5651/health || echo "Web service unhealthy"

# Verify API service responds
curl -f http://localhost:5653/health || echo "API service unhealthy"

# Verify database connectivity
curl -f http://localhost:5653/health/database || echo "Database unhealthy"
```

### Common Issues and Solutions

#### 1. Containers Not Running At All
**Symptom**: `docker ps` shows no WitchCity containers
**Solution**: 
```bash
./dev.sh
# OR
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

#### 2. Compilation Errors Preventing Startup
**Symptom**: Containers exist but health checks fail, logs show CS#### errors
**Example Log**: `CS0246: The type or namespace name 'LoginRequest' could not be found`
**Solution**: 
```bash
# Stop and restart containers to pick up latest code
docker-compose down
./dev.sh
```

#### 3. Port Conflicts
**Symptom**: Container fails to start with "port already in use" error
**Solution**:
```bash
# Find what's using the ports
lsof -i :5651
lsof -i :5653
lsof -i :5433

# Kill conflicting processes or restart containers
docker-compose down
./dev.sh
```

#### 4. Database Connection Issues
**Symptom**: Web/API containers running but database health check fails
**Solution**:
```bash
# Check database container logs
docker logs witchcity-db --tail 100

# Restart database container
docker restart witchcity-db

# If still failing, full restart
./dev.sh
```

#### 5. Stale Container State
**Symptom**: Containers appear healthy but serve old/cached content
**Solution**:
```bash
# Force rebuild and restart
docker-compose down --volumes
./dev.sh
```

### Diagnostic Commands for Troubleshooting

```bash
# Get overview of all containers
docker ps -a

# Check web service logs for errors
docker logs witchcity-web --tail 100 | grep -i error

# Check API service logs for errors
docker logs witchcity-api --tail 100 | grep -i error

# Check database logs
docker logs witchcity-db --tail 50

# Test all health endpoints
curl http://localhost:5651/health
curl http://localhost:5653/health
curl http://localhost:5653/health/database

# Check if database has seed data
PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_dev \
  -c "SELECT COUNT(*) FROM auth.\"Users\" WHERE \"Email\" LIKE '%@witchcityrope.com';"
```

### MANDATORY E2E Test Execution Flow

**The test-executor agent MUST follow this exact sequence:**

1. **Environment Health Check** (MANDATORY)
   - Run all diagnostic commands above
   - Verify all containers healthy
   - Check for compilation errors
   - Test service endpoints

2. **Environment Fix If Needed**
   - If ANY issues found, restart with `./dev.sh`
   - Re-verify health after restart
   - Do NOT proceed until environment is 100% healthy

3. **Database Seed Verification**
   - Verify test accounts exist
   - Confirm seed data loaded

4. **ONLY THEN Proceed with E2E Tests**
   - Navigate to `/tests/playwright`
   - Run `npm test`
   - Monitor for environment-related failures

### Critical Success Metrics

- ✅ All containers show "healthy" status
- ✅ No compilation errors in logs
- ✅ All health endpoints return 200 OK
- ✅ Database seed data present
- ✅ Services respond within 2 seconds

### Failure Patterns to Watch For

| Pattern | Cause | Solution |
|---------|-------|----------|
| `Element not found` in E2E tests | UI not loading due to compilation errors | Check logs, restart containers |
| `Connection refused` errors | Services not running | Restart containers |
| `Database connection failed` | Database container unhealthy | Restart database, check logs |
| `404 Not Found` for API calls | API service not responding | Check API logs, restart |
| Tests timeout waiting for page load | Web service compilation errors | Check web logs, restart |

### Emergency Restart Procedure

If environment is completely broken:

```bash
# Nuclear option - full reset
docker-compose down --volumes --remove-orphans
docker system prune -f
./dev.sh

# Wait for all services to be healthy (2-3 minutes)
# Then re-run diagnostics
```

## Historical Context

This checklist was created because E2E test failures were consistently caused by:
1. Containers appearing to run but having compilation errors
2. Services not actually responding despite container "Up" status
3. Database missing seed data
4. Port conflicts from previous sessions
5. Stale container state serving old code

**The test-executor agent has been specifically designed to handle these environment issues BEFORE attempting any test execution.**

## Integration with Test-Executor Agent

The test-executor agent's Phase 1 (Environment Pre-Flight Checks) implements all these requirements. This document serves as the comprehensive reference for why these checks are mandatory and how to troubleshoot common issues.

---

**Remember**: E2E tests are only as reliable as the environment they run in. Always verify environment health first!

## Docker Operations Knowledge for Test Execution

### Essential Docker Operations Reference
**Primary Documentation**: [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md)
**Architecture Overview**: [Docker Architecture](/docs/architecture/docker-architecture.md)

### Container Management for Testing

#### Starting Test Environment
```bash
# Start complete environment for testing
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Wait for services to be ready
sleep 15

# Verify health before testing
./docker/scripts/health-check.sh
```

#### Restarting Containers During Testing
```bash
# Restart all services if tests fail
docker-compose restart

# Restart specific service if needed
docker-compose restart api    # For API issues
docker-compose restart web    # For React issues
docker-compose restart postgres  # For database issues
```

#### Container Health Checking for Tests
```bash
# Comprehensive health check script for testing
#!/bin/bash
echo "🔍 Testing container health for QA..."

# Test each service individually
echo "Testing PostgreSQL..."
docker-compose exec postgres pg_isready -U postgres -d witchcityrope_dev || exit 1

echo "Testing API..."
curl -f http://localhost:5655/health || exit 1

echo "Testing React app..."
curl -f http://localhost:5173 || exit 1

echo "Testing authentication endpoints..."
curl -f http://localhost:5655/api/auth/health || exit 1

echo "✅ All services healthy for testing"
```

#### Viewing Container Logs for Debugging
```bash
# Monitor all services during test execution
docker-compose logs -f &

# View specific service logs
docker-compose logs -f web    # React development server
docker-compose logs -f api    # .NET API logs
docker-compose logs -f postgres  # Database logs

# Search for errors in logs
docker-compose logs api | grep -i error
docker-compose logs api | grep -i "auth\|login\|jwt"
```

#### Test Environment Cleanup
```bash
# Clean test data between test runs
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c "TRUNCATE TABLE AspNetUsers CASCADE;"

# Reset containers to clean state
docker-compose down
docker-compose up -d
./docker/scripts/health-check.sh
```

### Container Testing Workflow

#### Pre-Test Container Validation
1. **Start Environment**: Use docker-compose with development overrides
2. **Health Check**: Verify all services are healthy and responding
3. **Authentication Test**: Verify auth endpoints are accessible
4. **Log Monitoring**: Start log monitoring for test debugging

#### During Test Execution
1. **Monitor Logs**: Watch for container errors during test runs
2. **Resource Monitoring**: Check container resource usage if tests are slow
3. **Network Validation**: Verify service-to-service communication is working

#### Post-Test Cleanup
1. **Log Review**: Check container logs for any errors during test execution
2. **Data Cleanup**: Reset test data if needed for next test run
3. **Container Status**: Verify containers are still healthy after tests

### Troubleshooting Container Issues During Testing

#### Container Communication Problems
```bash
# Test React to API communication
docker-compose exec web curl -f http://api:8080/health

# Test API to database communication
docker-compose exec api pg_isready -h postgres -p 5432 -U postgres -d witchcityrope_dev

# Check container network
docker-compose exec web ping api
docker-compose exec api ping postgres
```

#### Authentication Testing in Containers
```bash
# Test complete authentication flow in containers
# 1. Register
curl -X POST http://localhost:5655/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234","sceneName":"TestUser"}' \
  -c cookies.txt

# 2. Login
curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234"}' \
  -b cookies.txt -c cookies.txt

# 3. Access protected endpoint
curl http://localhost:5655/api/protected/welcome \
  -b cookies.txt
```

#### Container Performance Monitoring
```bash
# Monitor container resource usage during tests
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"

# Monitor API response times
while true; do
  curl -w "API Health: %{time_total}s\n" -o /dev/null -s http://localhost:5655/health
  sleep 5
done
```

### Critical Docker Knowledge for Test-Executor

#### Container Restart Scenarios
- **API Code Changes**: API container restarts automatically with dotnet watch
- **React Code Changes**: React updates via Vite HMR without container restart
- **Database Issues**: PostgreSQL container has health checks and auto-restart
- **Full Environment Reset**: Use docker-compose down/up for complete refresh

#### Container-Specific Test Considerations
- **Authentication State**: User sessions survive API container restarts
- **Database Persistence**: User data persists through container lifecycle
- **Hot Reload Impact**: Code changes during tests may cause temporary failures
- **Network Timing**: Container-to-container communication may have slight delays

#### Emergency Docker Recovery
```bash
# If containers are completely broken
docker-compose down -v  # WARNING: Deletes database data
docker system prune -f
docker-compose build --no-cache
docker-compose up -d

# Wait for health and re-run tests
./docker/scripts/health-check.sh
```

**Critical**: Always use the [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) for detailed procedures and troubleshooting steps specific to containerized testing scenarios.

## NEW: Docker Authentication Testing Lessons (2025-08-17)

### Database Schema Initialization Issues

**CRITICAL LESSON**: Always verify database schema exists before testing authentication endpoints.

**Common Issue Pattern**:
1. Containers start successfully
2. API health endpoint works  
3. Authentication endpoints return 400/401 errors
4. Root cause: Database tables don't exist

**Mandatory Database Checks Before Auth Testing**:
```bash
# 1. Check database exists
PGPASSWORD=devpass123 psql -h localhost -p 5433 -U postgres -c "\l" | grep witchcityrope

# 2. Check tables exist  
PGPASSWORD=devpass123 psql -h localhost -p 5433 -U postgres -d witchcityrope_dev -c "\dt"

# 3. If no tables found, check for migration issues
docker logs witchcity-api | grep -i -E "(migrat|schema|ef)"

# 4. Create development database if missing
PGPASSWORD=devpass123 psql -h localhost -p 5433 -U postgres -c "CREATE DATABASE witchcityrope_dev;"
```

**Resolution Strategies**:
```bash
# Option 1: Manual schema creation (immediate fix)
# Run SQL scripts manually or use pgAdmin

# Option 2: Add migration to container startup (permanent fix)
# Modify Dockerfile: RUN dotnet ef database update

# Option 3: Add init scripts to postgres container
# Add SQL files to docker-entrypoint-initdb.d/
```

### Health Check Endpoint Misconfigurations

**LESSON**: Health checks in docker-compose.yml must match actual API endpoints.

**Issue Found**: 
- Health check tried `/health` but API serves `/api/Health`
- Health check used wrong connection string format
- Containers showed "unhealthy" despite working correctly

**Correct Health Check Configuration**:
```yaml
# In docker-compose.yml
api:
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:8080/api/Health || exit 1"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

### Network Subnet Conflicts

**LESSON**: Always check for Docker network conflicts before starting containers.

**Issue Found**: 
- Default subnet `172.20.0.0/16` conflicted with existing networks
- Container startup failed with "Pool overlaps" error

**Resolution Process**:
```bash
# 1. Clean up conflicting networks
docker network prune -f

# 2. Check existing subnets
docker network ls -q | xargs docker network inspect | grep -A 3 "Config"

# 3. Change subnet in docker-compose.yml
networks:
  witchcity-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.25.0.0/16  # Use non-conflicting subnet
```

### Docker Compose Environment Variable Issues

**LESSON**: Boolean values in docker-compose environment must be strings.

**Issue Found**:
- `CORS__AllowCredentials: true` caused validation error
- `VITE_ENABLE_DEBUG: true` caused validation error

**Correct Format**:
```yaml
environment:
  CORS__AllowCredentials: "true"      # String, not boolean
  VITE_ENABLE_DEBUG: "true"          # String, not boolean
  Authentication__RequireHttps: "false"  # String, not boolean
```

### Container Communication Testing

**LESSON**: Test both external and internal container communication.

**Testing Pattern**:
```bash
# External (host to container)
curl http://localhost:5655/api/Health

# Internal (container to container)  
docker exec witchcity-web wget -qO- http://api:8080/api/Health

# Network connectivity
docker exec witchcity-web ping api
docker exec witchcity-api ping postgres
```

### Authentication Endpoint Discovery

**LESSON**: Use Swagger/OpenAPI to discover available endpoints instead of guessing.

**Discovery Process**:
```bash
# 1. Check if Swagger is available
curl -s http://localhost:5655/swagger/index.html

# 2. Get all available endpoints
curl -s http://localhost:5655/swagger/v1/swagger.json | jq '.paths | keys'

# 3. Test specific endpoint patterns
curl -s http://localhost:5655/swagger/v1/swagger.json | jq '.paths | keys | map(select(contains("Auth")))'
```

### Performance Testing in Containers

**LESSON**: Container performance can be excellent when properly configured.

**Measured Performance**:
- React app load time: <8ms
- API response time: <7ms  
- Memory usage: React 94MB, API 336MB, DB 46MB
- CPU usage: All containers <2% CPU

**Performance Testing Commands**:
```bash
# Response time testing
time curl -s http://localhost:5173 > /dev/null
time curl -s http://localhost:5655/api/Health > /dev/null

# Resource monitoring
docker stats --no-stream | grep witchcity
```

### Test Results Documentation Standards

**LESSON**: Comprehensive test documentation must include both successes and failures.

**Required Test Documentation**:
1. **Executive Summary**: Overall pass/fail with percentages
2. **Detailed Results**: Individual test outcomes with evidence
3. **Issue Analysis**: Root cause analysis for failures
4. **Resolution Steps**: Specific commands to fix issues
5. **Performance Metrics**: Actual measurements vs targets
6. **Recommendations**: Next steps for production readiness

### Container Debugging Best Practices

**LESSON**: Use systematic debugging approach for container issues.

**Debugging Sequence**:
1. **Container Status**: `docker ps -a`
2. **Service Logs**: `docker logs <container> --tail 50`
3. **Network Connectivity**: `docker exec <container> ping <target>`
4. **Internal Service Tests**: `docker exec <container> curl <endpoint>`
5. **Configuration Validation**: `docker exec <container> env | grep <pattern>`

### Test-Executor Agent Scope Boundaries

**LESSON**: Stay within scope - fix environment issues but report code issues.

**Within Scope (Can Fix)**:
- Container startup issues
- Network configuration problems
- Database connectivity issues
- Health check configurations
- Port conflicts

**Outside Scope (Report Only)**:
- Source code compilation errors
- Business logic failures
- Missing feature implementations
- Authentication algorithm issues

**Action Pattern**:
- Environment issues → Fix immediately and continue testing
- Code issues → Document thoroughly and report to orchestrator

### Comprehensive Testing Approach

**LESSON**: Test infrastructure first, then business logic.

**Testing Sequence**:
1. **Infrastructure**: Containers, networking, ports
2. **Connectivity**: Service-to-service communication  
3. **API Health**: Basic endpoint responsiveness
4. **Configuration**: CORS, environment variables
5. **Business Logic**: Authentication endpoints
6. **Performance**: Response times and resource usage
7. **Development Tools**: Hot reload, logging

This ensures environment issues are resolved before wasting time on business logic testing.

### Integration with Project Architecture

**LESSON**: Understand the complete authentication architecture before testing.

**Key Architecture Elements**:
- **React Frontend**: Cookie-based authentication
- **API Backend**: JWT token generation
- **Database**: User storage and session management
- **Container Network**: Service discovery and communication
- **Development Tools**: Hot reload, debugging, monitoring

**Testing Must Validate**:
- All architectural components work together
- Security patterns are implemented correctly
- Development workflow is functional
- Production deployment patterns are viable

---

**Updated**: 2025-08-17 - Added comprehensive Docker authentication testing lessons learned from successful test execution of containerized authentication system.