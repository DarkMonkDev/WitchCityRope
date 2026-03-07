# Docker-Only Testing Standard

<!-- Last Updated: 2025-09-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Team -->
<!-- Status: MANDATORY -->

## 🚨 CRITICAL: MANDATORY Docker-Only Development and Testing Environment 🚨

**ALL TESTING ACTIVITIES MUST USE DOCKER CONTAINERS EXCLUSIVELY**

This document establishes the **SINGLE SOURCE OF TRUTH** for testing environment requirements. Compliance is **MANDATORY** for all developers and test agents.

## 🛑 FUNDAMENTAL RULE

**NEVER run `npm run dev` - it is DISABLED and will ERROR**

**ONLY use Docker containers via `./dev.sh` for ALL development and testing**

## 🎯 Approved Environment

### ✅ ONLY APPROVED TESTING ENVIRONMENT:
- **React App**: Docker container on **port 5173**
- **API Service**: Docker container on **port 5655**
- **PostgreSQL Database**: Docker container on **port 5434**

### ❌ FORBIDDEN ENVIRONMENTS:
- Local dev servers on ports 5174, 5175, 3000, or any non-Docker ports
- Mixed Docker + local dev server environments
- Testing against localhost development servers
- Any `npm run dev` or `npm start` commands

## 🚨 MANDATORY PRE-FLIGHT CHECKLIST

**EVERY developer and test agent MUST verify this checklist BEFORE any testing work:**

Use the `restart-dev-containers` skill for dev environment or `restart-test-containers` skill for test environment. Both skills include health checks and verification automatically.

For manual verification: check that containers are running on correct ports (web: 5173, API: 5655, DB: 5434) and no local dev servers conflict on ports 5174/5175/3000.

## 📋 Agent-Specific Requirements

### Test-Developer Agent
- **NEVER create tests targeting local dev servers**
- **ALWAYS verify Docker environment before test creation**
- **MUST use port 5173 in ALL test configurations**
- **COORDINATE with test-executor** - they expect Docker environment

### Test-Executor Agent
- **NEVER execute tests without Docker verification**
- **ALWAYS check container health before test execution**
- **IMMEDIATELY fail tests if local dev servers detected**
- **RESTART Docker containers if unhealthy**: `./dev.sh`

### React Developer
- **NEVER start local dev servers for testing**
- **ALWAYS use Docker for component testing**
- **COORDINATE with test agents** - they expect port 5173 Docker
- **VERIFY environment before any React testing work**

### Backend Developer
- **NEVER run local API servers for testing**
- **ALWAYS use Docker API on port 5655**
- **VERIFY API health through Docker before testing**
- **COORDINATE with frontend** - they expect Docker API endpoints

## 🚨 EMERGENCY PROTOCOLS

### If Tests Fail - MANDATORY Response Order:

1. **FIRST**: Use `restart-dev-containers` skill (for dev) or `restart-test-containers` skill (for test) — both include health checks and conflict detection
2. **SECOND**: Verify no local dev server conflicts on ports 5174/5175/3000
3. **ONLY THEN**: Re-run tests after Docker environment verified

### If Local Dev Servers Detected:

**IMMEDIATE ACTION REQUIRED:**
1. ❌ **STOP ALL TESTING** - Results will be invalid
2. 🔥 **KILL ROGUE PROCESSES**: `./scripts/kill-local-dev-servers.sh`
3. 🐳 **VERIFY DOCKER**: Ensure containers healthy on correct ports
4. ✅ **RE-VALIDATE**: Run pre-flight checklist again
5. 🔄 **RESTART TESTS**: Only after Docker-only environment confirmed

## 💥 Consequences of Non-Compliance

### Test Result Invalidation:
- ❌ **False positives**: Tests pass against wrong environment
- ❌ **False negatives**: Tests fail due to port conflicts
- ❌ **Environment drift**: Different test results per developer
- ❌ **Debug time waste**: Hours spent on non-existent issues

### Development Impact:
- ❌ **Integration failures**: Code works locally but fails in Docker
- ❌ **Production issues**: Environment differences cause bugs
- ❌ **Team confusion**: Mixed results across different environments
- ❌ **CI/CD breakage**: Pipeline expects Docker, gets local dev

## 🎯 Success Verification

### All Tests Must Pass These Criteria:
1. ✅ **Environment Check**: Pre-flight checklist completed
2. ✅ **Port Verification**: Only port 5173 (Docker) used for React
3. ✅ **Process Check**: No local dev servers running
4. ✅ **Container Health**: All Docker containers healthy
5. ✅ **Endpoint Validation**: Services respond on correct Docker ports

### Quality Gates:
- **Zero tolerance** for testing against local dev servers
- **Mandatory Docker verification** before test execution
- **Immediate failure** if port conflicts detected
- **Automatic rejection** of results from mixed environments

## 📚 Related Documentation

**Agents MUST read these documents:**
- `/docs/lessons-learned/test-developer-lessons-learned.md` - Test creation patterns
- `/docs/lessons-learned/test-executor-lessons-learned.md` - Test execution requirements
- `/docs/lessons-learned/react-developer-lessons-learned.md` - React testing requirements
- `/docs/lessons-learned/backend-developer-lessons-learned.md` - API testing requirements
- `/docs/standards-processes/testing/TESTING.md` - Comprehensive testing guide

## 🔧 Troubleshooting

### Common Issues and Solutions:

**"React app not loading"** — Use `restart-dev-containers` skill to rebuild and verify web container health.

**"API endpoints returning 404"** — Use `restart-dev-containers` skill to restart API with health checks.

**"Tests timing out"** — Check for port conflicts, then use `restart-dev-containers` or `restart-test-containers` skill.

**"Connection refused errors"** — Use `restart-dev-containers` skill (automatically waits for health checks).

## 📞 Escalation

If Docker environment cannot be established:
1. **Document issue**: Container logs, error messages
2. **Report to orchestrator**: Include pre-flight checklist results
3. **DO NOT PROCEED** with testing until environment verified
4. **Update this document** if new solutions discovered

## 🎉 Success Metrics

**100% Compliance Target:**
- All tests run against Docker containers exclusively
- Zero false failures due to environment issues
- Zero port conflicts during testing
- Consistent results across all developers and CI/CD

**This standard is MANDATORY and NON-NEGOTIABLE for reliable testing.**