---
name: container-restart
description: Restarts WitchCityRope Docker development containers using the CORRECT procedure. Handles shutdown, rebuild with dev compose overlay, health checks, and compilation verification. Ensures environment is ready for development or testing. SINGLE SOURCE OF TRUTH for container restart process.
---

# Container Restart Skill

**Purpose**: Restart Docker containers the RIGHT way - this is the ONLY correct procedure.

**When to Use**:
- Before running E2E tests (MANDATORY)
- After code changes that need container rebuild
- When containers are unhealthy
- When "Element not found" errors appear in E2E tests (usually means compilation errors)
- When database seed data is missing

## 🚨 SINGLE SOURCE OF TRUTH

**This skill is the ONLY place where container restart procedure is documented.**

**If you find container restart instructions elsewhere:**
1. They are outdated or wrong
2. Report to librarian for cleanup
3. Use THIS skill instead

**DO NOT duplicate this procedure in:**
- ❌ Agent definitions
- ❌ Lessons learned (reference this skill instead)
- ❌ Process documentation (reference this skill instead)

---

## The Correct Process

### ❌ DON'T Do This:
```bash
# WRONG - Missing dev overlay
docker-compose up -d

# WRONG - Doesn't check compilation
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# WRONG - Doesn't check health
./dev.sh
# ... immediately runs tests
```

### ✅ DO This:

**Use this skill** - it handles everything correctly.

---

## How to Use This Skill

**Executable Script**: `execute.sh`

```bash
# From project root
bash .claude/skills/container-restart/execute.sh

# Skip confirmation prompt (for automation)
SKIP_CONFIRMATION=true bash .claude/skills/container-restart/execute.sh
```

**What the script does**:
1. Shows pre-flight information (purpose, when/when NOT to use)
2. Requires confirmation before proceeding (skippable with env var)
3. Validates prerequisites (Docker running, project root, dev overlay exists)
4. Stops existing containers
5. Starts containers with dev overlay (`docker-compose.yml + docker-compose.dev.yml`)
6. Checks for compilation errors in Web and API containers
7. Verifies health endpoints (Web, API, Database)
8. Checks database seed data
9. Reports status summary

**Script includes safety checks** - it will not run blindly without showing you what it's about to do.

---

## Quick Reference Commands

### Using the Skill (Recommended)
```bash
# From project root - with confirmation prompt
bash .claude/skills/container-restart/execute.sh

# For automation - skip confirmation
SKIP_CONFIRMATION=true bash .claude/skills/container-restart/execute.sh
```

### Manual Steps (If execute.sh unavailable)
```bash
# Stop containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Start with dev overlay
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

# Or use helper
./dev.sh

# Wait and check health
sleep 15
curl http://localhost:5173
curl http://localhost:5653/health
curl http://localhost:5653/health/database
```

---

## Common Issues & Solutions

### Issue: Containers start but tests fail

**Cause**: Compilation errors in container

**Solution**: Skill automatically checks compilation logs

**Manual check**:
```bash
docker logs witchcity-web --tail 50 | grep -i error
docker logs witchcity-api --tail 50 | grep -i error
```

### Issue: Port already in use

**Cause**: Old containers still running or other process using ports

**Solution**:
```bash
# Kill all witchcity containers
docker ps -a | grep witchcity | awk '{print $1}' | xargs docker rm -f

# Check what's using port 5173
lsof -i :5173
```

### Issue: Database not seeded

**Cause**: Fresh database or seed script not run

**Solution**:
```bash
./scripts/seed-database.sh
```

### Issue: Health checks fail after compilation succeeds

**Cause**: Services need more time to initialize

**Solution**: Wait longer (skill waits 25 seconds total)

---

## Integration with Agents

### test-executor

**BEFORE E2E tests:**
```
I'll restart containers to ensure environment is healthy.
```
*Skill is invoked automatically*

**Result**: Environment verified before tests run

### react-developer / backend-developer

**After code changes:**
```
I'll restart containers to apply my changes.
```
*Skill is invoked automatically*

**Result**: Code changes applied, compilation verified

---

## Integration with Lessons Learned

**Lessons learned should NOT duplicate this procedure.**

**Correct format in lessons learned:**

```markdown
## Problem: E2E Tests Fail with "Element Not Found"

**Problem**: Container shows "Up" but tests fail mysteriously.

Root cause: Container built but code has compilation errors.

**Solution**: Use `container-restart` skill before E2E tests.

The skill automatically checks for compilation errors.

**See**: `/.claude/skills/container-restart.md`
```

**❌ WRONG - Don't duplicate the procedure:**
```markdown
## Problem: E2E Tests Fail

**Solution**:
1. Run docker-compose down
2. Run docker-compose up with dev overlay
3. Check logs for errors
4. Verify health endpoints
... [detailed steps duplicated from skill]
```

---

## Output Format

When run via Claude Code, skill returns:

```json
{
  "containerRestart": {
    "status": "success",
    "timestamp": "2025-11-04T15:30:00Z",
    "containers": {
      "running": 3,
      "expected": 3
    },
    "compilation": {
      "web": "clean",
      "api": "clean"
    },
    "healthChecks": {
      "web": "healthy",
      "api": "healthy",
      "database": "healthy"
    },
    "seedData": {
      "users": 7,
      "status": "adequate"
    },
    "readyForTesting": true
  }
}
```

On failure:
```json
{
  "containerRestart": {
    "status": "failure",
    "error": "Compilation errors in web container",
    "details": "TypeError: Cannot read property 'foo' of undefined at line 42",
    "action": "Fix source code and restart again"
  }
}
```

---

## Maintenance

**This skill is the single source of truth.**

**To update the restart procedure:**
1. Update THIS file only
2. Test the new procedure
3. DO NOT update process docs, lessons learned, or agent definitions
4. They should reference this skill, not duplicate it

**To verify no duplication:**
```bash
# Run single-source-validator skill
bash .claude/skills/single-source-validator.md container-restart
```

---

## Version History

- **2025-11-04**: Created as single source of truth for container restart
- Consolidates instructions from:
  - test-executor.md (lines 150-160) ❌ Removed
  - docker-operations-guide.md (lines 80-120) ❌ Removed
  - test-executor-lessons-learned.md (lines 450-470) → Now references this skill

---

**Remember**: This skill is executable automation. Run it, don't copy it.
