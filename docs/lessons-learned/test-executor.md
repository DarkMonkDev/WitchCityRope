# Test Executor Lessons Learned
<!-- Last Updated: 2025-08-13 -->
<!-- Version: 1.0 -->
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
docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity

# Expected output: All containers should show "Up" status
# witchcity-web    Up X minutes (healthy)
# witchcity-api    Up X minutes (healthy) 
# witchcity-db     Up X minutes (healthy)
```

**2. Container Health Status**
```bash
# Check specific health status for each service
docker inspect witchcity-web --format='{{.State.Health.Status}}'
docker inspect witchcity-api --format='{{.State.Health.Status}}'
docker inspect witchcity-db --format='{{.State.Health.Status}}'

# Expected output: "healthy" for all containers
```

**3. Check for Compilation Errors**
```bash
# Check web service logs for compilation errors
docker logs witchcity-web --tail 50 | grep -i error

# Check API service logs for compilation errors  
docker logs witchcity-api --tail 50 | grep -i error

# If ANY compilation errors found, STOP and restart containers
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