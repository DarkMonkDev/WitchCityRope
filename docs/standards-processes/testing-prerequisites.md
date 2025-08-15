# E2E Testing Prerequisites - Standard Process
<!-- Last Updated: 2025-08-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Team -->
<!-- Status: Active -->

## Overview
This document establishes the standard process for E2E testing environment validation. **ALL E2E testing MUST begin with environment validation** to prevent the #1 cause of test failures: unhealthy Docker containers.

## The Problem

E2E test failures are most commonly caused by:
1. **Docker containers appearing "running" but having compilation errors**
2. **Services not actually responding despite "Up" status**
3. **Database missing seed data**
4. **Port conflicts from previous sessions**
5. **Stale container state serving old code**

## Standard E2E Testing Process

### Phase 1: MANDATORY Environment Validation

**BEFORE running ANY E2E tests, validate the environment:**

#### 1. Container Status Check
```bash
# Verify all containers are running
docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity

# Expected: All show "Up X minutes (healthy)"
# If any missing or unhealthy, proceed to fix
```

#### 2. Health Status Validation
```bash
# Check health status for each service
docker inspect witchcity-web --format='{{.State.Health.Status}}'
docker inspect witchcity-api --format='{{.State.Health.Status}}'
docker inspect witchcity-db --format='{{.State.Health.Status}}'

# Expected: "healthy" for all
```

#### 3. Compilation Error Check
```bash
# Check for compilation errors in logs
docker logs witchcity-web --tail 50 | grep -i error
docker logs witchcity-api --tail 50 | grep -i error

# If ANY compilation errors found, restart containers
```

#### 4. Service Endpoint Verification
```bash
# Test all health endpoints
curl -f http://localhost:5651/health || echo "Web service unhealthy"
curl -f http://localhost:5653/health || echo "API service unhealthy"  
curl -f http://localhost:5653/health/database || echo "Database unhealthy"

# All must return 200 OK
```

#### 5. Database Seed Data Check
```bash
# Verify test accounts exist
PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_dev \
  -c "SELECT COUNT(*) FROM auth.\"Users\" WHERE \"Email\" LIKE '%@witchcityrope.com';"

# Should return count > 0 (typically 5 test accounts)
```

### Phase 2: Environment Fixes (If Needed)

**If ANY validation step fails:**

#### Standard Fix: Full Restart
```bash
# Preferred method - handles most issues
./dev.sh

# Alternative method
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

#### Nuclear Option: Complete Reset
```bash
# For persistent issues
docker-compose down --volumes --remove-orphans
docker system prune -f
./dev.sh

# Wait 2-3 minutes for all services to be healthy
```

#### Post-Fix Validation
**After any fix, re-run ALL validation steps. Do not proceed until 100% healthy.**

### Phase 3: E2E Test Execution

**Only after environment is validated healthy:**

```bash
# Navigate to test directory
cd tests/playwright

# Install dependencies if needed
npm ci

# Run E2E tests
npm test
```

## Environment Health Criteria

**All criteria MUST be met before E2E testing:**

- ✅ All Docker containers show "Up" and "healthy" status
- ✅ No compilation errors in container logs
- ✅ All health endpoints return 200 OK within 2 seconds
- ✅ Database contains seed data (5+ test accounts)
- ✅ Services respond to basic HTTP requests

## Common Failure Patterns

| Symptom | Likely Cause | Standard Fix |
|---------|--------------|---------------|
| "Element not found" in E2E tests | UI not loading due to compilation errors | Check logs, restart containers |
| "Connection refused" | Services not running | Restart containers |
| "Database connection failed" | Database unhealthy | Restart database, check logs |
| "404 Not Found" for API calls | API service not responding | Restart containers |
| Tests timeout on page load | Web service compilation errors | Check logs, restart |
| Intermittent test failures | Stale container state | Full restart with `./dev.sh` |

## Diagnostic Commands

**Use these commands to diagnose environment issues:**

```bash
# Container overview
docker ps -a
docker-compose ps

# Service logs
docker logs witchcity-web --tail 100
docker logs witchcity-api --tail 100  
docker logs witchcity-db --tail 50

# Health endpoints
curl -v http://localhost:5651/health
curl -v http://localhost:5653/health
curl -v http://localhost:5653/health/database

# Port usage
lsof -i :5651
lsof -i :5653
lsof -i :5433

# Database connectivity
PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_dev -c "SELECT version();"
```

## Integration with CI/CD

**For automated testing environments:**

1. Run environment validation as a separate CI step
2. Fail the build if environment validation fails
3. Only proceed to E2E tests after environment is healthy
4. Capture environment diagnostics in CI artifacts

## Agent Responsibilities

### Test-Executor Agent
- **MUST** run complete environment validation before ANY E2E tests
- **MUST** fix environment issues using standard procedures
- **MUST** re-validate after fixes
- **MUST NOT** proceed with E2E tests until environment is 100% healthy

### Orchestrator Agent
- **SHOULD** delegate environment validation to test-executor
- **SHOULD** not override test-executor's environment assessment
- **SHOULD** wait for environment health confirmation before proceeding

### Development Agents
- **SHOULD** not modify test environment setup
- **SHOULD** report environment issues to test-executor
- **SHOULD** not attempt E2E testing directly

## Success Metrics

**Track these metrics to ensure process effectiveness:**

- Environment validation success rate (target: >95%)
- E2E test failure rate due to environment issues (target: <5%)
- Time to fix environment issues (target: <2 minutes)
- False positive E2E failures (target: <1%)

## Documentation References

- **Test-Executor Agent**: `/.claude/agents/testing/test-executor.md`
- **Test-Executor Lessons**: `/docs/lessons-learned/test-executor.md`
- **Docker Development**: `/docs/standards-processes/development-standards/docker-development.md`
- **Testing Guide**: `/docs/standards-processes/testing/TESTING_GUIDE.md`

## Maintenance

**Review and update this process:**
- After any Docker configuration changes
- When new services are added
- After persistent environment issues
- Quarterly review of failure patterns

---

**Remember**: Healthy environment = reliable E2E tests. Always validate first!