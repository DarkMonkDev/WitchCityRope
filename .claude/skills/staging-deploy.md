---
name: staging-deploy
description: Deploys WitchCityRope to staging environment on DigitalOcean. Automates build, registry push, server deployment, database migrations (if needed), health verification, and rollback capability. SINGLE SOURCE OF TRUTH for staging deployment automation.
---

# Staging Deployment Skill

**Purpose**: Deploy to staging environment safely and correctly - this is the automation wrapper.

**When to Use**:
- After Phase 5 validation passes
- When deploying new features for testing
- After hotfixes that need staging verification
- When requested by user/orchestrator

**Background Documentation**: See `/docs/functional-areas/deployment/staging-deployment-guide.md` for context and manual procedures.

## 🚨 SINGLE SOURCE OF TRUTH

**This skill is the ONLY automated deployment procedure.**

**Division of responsibility:**
- **This skill**: Executable automation (bash script)
- **Deployment guide** (`/docs/functional-areas/deployment/staging-deployment-guide.md`): Context, manual procedures, troubleshooting

**DO NOT duplicate deployment automation in:**
- ❌ Agent definitions
- ❌ Lessons learned (reference this skill instead)
- ❌ Process documentation (already has guide, references this skill)

---

## Prerequisites Check

```bash
#!/bin/bash
# Staging Deployment Prerequisites Checker

echo "📋 Staging Deployment Prerequisites"
echo "===================================="
echo ""

# 1. All tests must be passing
echo "1️⃣  Checking test status..."
if [ ! -f "test-results/test-execution-report.md" ]; then
    echo "   ❌ FAIL: No test execution report found"
    echo "   Run tests first: bash .claude/skills/container-restart.md && npm test"
    exit 1
fi

if ! grep -q "Status: PASS" test-results/test-execution-report.md; then
    echo "   ❌ FAIL: Tests not passing"
    echo "   Achieve 100% pass rate before deploying"
    exit 1
fi
echo "   ✅ All tests passing"

# 2. Git must be clean
echo ""
echo "2️⃣  Checking git status..."
if [ -n "$(git status --short)" ]; then
    echo "   ❌ FAIL: Uncommitted changes"
    echo "   Commit all changes before deploying"
    git status --short
    exit 1
fi
echo "   ✅ Git clean"

# 3. Check branch
echo ""
echo "3️⃣  Checking branch..."
BRANCH=$(git branch --show-current)
echo "   Current branch: $BRANCH"
if [ "$BRANCH" != "main" ] && [ "$BRANCH" != "staging" ]; then
    echo "   ⚠️  WARNING: Not on main or staging branch"
    read -p "   Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi
echo "   ✅ Branch verified"

# 4. SSH key accessible
echo ""
echo "4️⃣  Checking SSH access..."
SSH_KEY="/home/chad/.ssh/id_ed25519_witchcityrope"
if [ ! -f "$SSH_KEY" ]; then
    echo "   ❌ FAIL: SSH key not found: $SSH_KEY"
    exit 1
fi
echo "   ✅ SSH key found"

# 5. Docker running locally
echo ""
echo "5️⃣  Checking Docker..."
if ! docker ps > /dev/null 2>&1; then
    echo "   ❌ FAIL: Docker not running"
    exit 1
fi
echo "   ✅ Docker running"

echo ""
echo "✅ Prerequisites complete"
echo ""
```

---

## Automated Deployment Script

```bash
#!/bin/bash
# WitchCityRope Staging Deployment
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE

set -e  # Exit on error

echo "🚀 WitchCityRope Staging Deployment"
echo "===================================="
echo ""

# Configuration
REGISTRY="registry.digitalocean.com/witchcityrope"
SERVER="104.131.165.14"
USER="witchcity"
SSH_KEY="/home/chad/.ssh/id_ed25519_witchcityrope"
DEPLOY_PATH="/opt/witchcityrope/staging"
GIT_SHA=$(git rev-parse --short HEAD)

echo "📦 Deployment Details:"
echo "   Registry: $REGISTRY"
echo "   Server: $SERVER"
echo "   Git SHA: $GIT_SHA"
echo ""

# Step 1: Build production images
echo "1️⃣  Building production images..."
echo ""

# Build API
echo "   Building API image..."
docker build \
  -f apps/api/Dockerfile \
  -t $REGISTRY/witchcityrope-api:latest \
  -t $REGISTRY/witchcityrope-api:$GIT_SHA \
  --target production \
  .

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: API build failed"
    exit 1
fi
echo "   ✅ API image built"

# Build Web
echo ""
echo "   Building Web image..."
docker build \
  -f apps/web/Dockerfile \
  -t $REGISTRY/witchcityrope-web:latest \
  -t $REGISTRY/witchcityrope-web:$GIT_SHA \
  --target production \
  --build-arg BUILD_MODE=staging \
  --build-arg VITE_API_BASE_URL= \
  --build-arg VITE_APP_TITLE="WitchCityRope" \
  --build-arg VITE_APP_VERSION="$GIT_SHA" \
  .

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Web build failed"
    exit 1
fi
echo "   ✅ Web image built"
echo ""

# Step 2: Push to registry
echo "2️⃣  Pushing to DigitalOcean Container Registry..."
echo ""

echo "   Pushing API image..."
docker push $REGISTRY/witchcityrope-api:latest
docker push $REGISTRY/witchcityrope-api:$GIT_SHA
echo "   ✅ API image pushed"

echo ""
echo "   Pushing Web image..."
docker push $REGISTRY/witchcityrope-web:latest
docker push $REGISTRY/witchcityrope-web:$GIT_SHA
echo "   ✅ Web image pushed"
echo ""

# Step 3: Test server connectivity
echo "3️⃣  Testing server connectivity..."
if ! ssh -i $SSH_KEY -o ConnectTimeout=10 $USER@$SERVER "echo '✅ Connected'" ; then
    echo "   ❌ FAIL: Cannot connect to server"
    exit 1
fi
echo ""

# Step 4: Pull images on server
echo "4️⃣  Pulling images on server..."
echo "   Pulling latest images..."
ssh -i $SSH_KEY $USER@$SERVER "cd /opt/witchcityrope/staging && docker-compose -f docker-compose.staging.yml pull"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Image pull failed"
    exit 1
fi
echo "   ✅ Images pulled"
echo ""

# Step 5: Deploy (restart containers)
echo "5️⃣  Deploying containers..."
echo "   Restarting containers..."
ssh -i $SSH_KEY $USER@$SERVER "cd /opt/witchcityrope/staging && docker-compose -f docker-compose.staging.yml up -d"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Container restart failed"
    exit 1
fi
echo "   ✅ Containers restart command completed"
echo ""

# Step 5b: Verify containers were actually recreated (NEW)
echo "5️⃣b Verifying containers were recreated..."
CONTAINER_AGE=$(ssh -i $SSH_KEY $USER@$SERVER "docker inspect witchcity-api-staging --format='{{.State.StartedAt}}'" 2>/dev/null)
if [ -z "$CONTAINER_AGE" ]; then
    echo "   ❌ FAIL: Could not verify container restart"
    exit 1
fi

# Calculate age in seconds
CURRENT_TIME=$(date +%s)
CONTAINER_TIME=$(date -d "$CONTAINER_AGE" +%s 2>/dev/null || echo "0")
AGE_SECONDS=$((CURRENT_TIME - CONTAINER_TIME))

if [ $AGE_SECONDS -gt 120 ]; then
    echo "   ❌ FAIL: Container is too old ($AGE_SECONDS seconds) - restart may have failed"
    echo "   Container started at: $CONTAINER_AGE"
    echo "   Manual restart required:"
    echo "   ssh $USER@$SERVER 'cd /opt/witchcityrope/staging && docker-compose -f docker-compose.staging.yml up -d'"
    exit 1
fi
echo "   ✅ Containers verified as newly created ($AGE_SECONDS seconds old)"
echo ""

# Step 6: Wait for containers to stabilize
echo "6️⃣  Waiting for services to stabilize..."
sleep 30
echo "   ✅ Wait complete"
echo ""

# Step 7: Check container status
echo "7️⃣  Checking container status..."
echo "   Container status:"
ssh -i $SSH_KEY $USER@$SERVER "cd /opt/witchcityrope/staging && docker-compose -f docker-compose.staging.yml ps"
echo ""
echo "   Recent API logs:"
ssh -i $SSH_KEY $USER@$SERVER "docker logs witchcity-api-staging --tail 20"
echo ""

# Step 8: Health checks
echo "8️⃣  Running health checks..."
echo ""

# Web health
echo "   Checking web service..."
if curl -f -s https://staging.notfai.com/ > /dev/null; then
    echo "   ✅ Web service healthy (https://staging.notfai.com/)"
else
    echo "   ❌ Web health check failed"
    echo "   Consider rollback"
    exit 1
fi

# API health
echo ""
echo "   Checking API service..."
if curl -f -s https://staging.notfai.com/api/health > /dev/null; then
    echo "   ✅ API service healthy (https://staging.notfai.com/api/health)"
else
    echo "   ❌ API health check failed"
    echo "   Consider rollback"
    exit 1
fi

# Database health (via API)
echo ""
echo "   Checking database..."
if curl -f -s https://staging.notfai.com/api/health/database > /dev/null; then
    echo "   ✅ Database healthy"
else
    echo "   ❌ Database health check failed"
    echo "   Consider rollback"
    exit 1
fi

echo ""

# Step 9: Smoke tests
echo "9️⃣  Running smoke tests..."
echo ""

# Test critical endpoints
SMOKE_PASS=0
SMOKE_FAIL=0

echo "   Testing homepage..."
if curl -f -s https://staging.notfai.com/ | grep -q "WitchCityRope"; then
    echo "   ✅ Homepage"
    ((SMOKE_PASS++))
else
    echo "   ❌ Homepage failed"
    ((SMOKE_FAIL++))
fi

echo "   Testing API events endpoint..."
if curl -f -s https://staging.notfai.com/api/events > /dev/null; then
    echo "   ✅ Events API"
    ((SMOKE_PASS++))
else
    echo "   ❌ Events API failed"
    ((SMOKE_FAIL++))
fi

echo ""
echo "   Smoke tests: $SMOKE_PASS passed, $SMOKE_FAIL failed"

if [ $SMOKE_FAIL -gt 0 ]; then
    echo "   ⚠️  WARNING: Some smoke tests failed"
    echo "   Review logs and consider rollback if critical"
fi

echo ""
echo "✅ Staging Deployment Complete"
echo "==============================="
echo ""
echo "📊 Deployment Summary:"
echo "   • Server: $SERVER"
echo "   • URL: https://staging.notfai.com"
echo "   • Git SHA: $GIT_SHA"
echo "   • Images: $REGISTRY/*:latest, :$GIT_SHA"
echo "   • Smoke tests: $SMOKE_PASS/$((SMOKE_PASS + SMOKE_FAIL))"
echo ""
echo "🎯 Next Steps:"
echo "   1. Manually test critical user flows"
echo "   2. Monitor logs: ssh $USER@$SERVER 'docker logs -f witchcity-api-staging'"
echo "   3. Review staging: https://staging.notfai.com"
echo "   4. If issues: Use staging-rollback skill"
echo ""

# Return success
exit 0
```

---

## Rollback Procedure

**Separate skill**: `staging-rollback.md`

Quick rollback:
```bash
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
cd /opt/witchcityrope/staging

# Pull and restart previous :latest images
docker-compose -f docker-compose.staging.yml pull
docker-compose -f docker-compose.staging.yml up -d

# Or specify previous SHA
# docker-compose -f docker-compose.staging.yml pull witchcityrope-api:<previous-sha>
# docker-compose -f docker-compose.staging.yml up -d
```

---

## Critical Deployment Notes

### ⚠️ Shared Server Warning

**IMPORTANT**: Server hosts multiple applications.

**NEVER DO**:
```bash
# ❌ WRONG - Stops ALL containers on server
docker stop $(docker ps -q)
docker-compose down
```

**ALWAYS DO**:
```bash
# ✅ RIGHT - Only affects WitchCityRope containers
docker-compose -f docker-compose.staging.yml down
docker-compose -f docker-compose.staging.yml up -d
```

### 🚨 Image Tagging Convention

**ALWAYS USE** `:latest` tag for staging:

```bash
# ✅ CORRECT
docker build ... -t registry.digitalocean.com/witchcityrope/witchcityrope-api:latest

# ❌ WRONG - Compose file expects :latest
docker build ... -t registry.digitalocean.com/witchcityrope/witchcityrope-api:staging
```

**Why**: `docker-compose.staging.yml` uses `image: ${REGISTRY}/witchcityrope-api:${IMAGE_TAG:-latest}` which defaults to `latest`.

---

## Common Issues & Solutions

### Issue: Build fails locally

**Cause**: Missing dependencies, compilation errors

**Solution**:
1. Fix build errors
2. Test locally first: `docker build -f apps/api/Dockerfile .`
3. Ensure all tests passing before deploying

### Issue: Push to registry fails

**Cause**: Docker not logged into DigitalOcean registry

**Solution**:
```bash
# Login to DO registry (one-time)
doctl registry login
```

### Issue: Health checks fail after deployment

**Cause**: Services need more time, or deployment has errors

**Solution**:
1. Wait longer (skill waits 30 seconds)
2. Check logs via SSH to server and inspect container logs
3. If serious: Rollback

### Issue: Smoke tests fail

**Cause**: Specific endpoints broken

**Solution**:
1. Review which endpoint failed
2. Check API logs for errors
3. Consider rollback if critical functionality broken

---

## Integration with Agents

### git-manager / librarian

**After Phase 5 validation:**
```
I'll deploy to staging for testing.
```
*Skill is invoked automatically*

**Result**: Code deployed to staging safely

### test-executor

**After successful test run:**
```
All tests passed. Staging deployment can proceed.
```

---

## Integration with Process Documentation

**Deployment guide should reference this skill:**

```markdown
# Staging Deployment Process

After Phase 5 validation passes:

1. Use `staging-deploy` skill (automated)
2. Skill handles:
   - Build verification
   - Image push
   - Server deployment
   - Health checks
   - Smoke tests
3. Manual testing of critical flows
4. If issues: Use `staging-rollback` skill

**Automation**: `/.claude/skills/staging-deploy.md`
**Manual procedures**: This guide (for troubleshooting)
```

---

## Output Format

```json
{
  "stagingDeploy": {
    "status": "success",
    "timestamp": "2025-11-04T15:30:00Z",
    "gitSha": "abc123f",
    "build": {
      "api": "success",
      "web": "success"
    },
    "registry": {
      "api": "pushed",
      "web": "pushed",
      "tags": ["latest", "abc123f"]
    },
    "deployment": {
      "server": "104.131.165.14",
      "containersRestarted": true,
      "startTime": "2025-11-04T15:28:00Z",
      "endTime": "2025-11-04T15:30:00Z",
      "duration": "2m"
    },
    "healthChecks": {
      "web": "healthy",
      "api": "healthy",
      "database": "healthy"
    },
    "smokeTests": {
      "homepage": "pass",
      "eventsApi": "pass",
      "passed": 2,
      "failed": 0
    },
    "url": "https://staging.notfai.com",
    "rollbackAvailable": true,
    "previousSha": "def456g"
  }
}
```

On failure:
```json
{
  "stagingDeploy": {
    "status": "failure",
    "phase": "health_checks",
    "error": "API health check failed",
    "action": "Review logs and consider rollback",
    "rollbackCommand": "bash .claude/skills/staging-rollback.md"
  }
}
```

---

## Maintenance

**This skill is the automation wrapper.**

**Division of labor:**
- **This skill**: Executable bash script (automation)
- **Deployment guide**: Context, manual steps, troubleshooting

**To update deployment procedure:**
1. If automation changes: Update THIS skill
2. If context/background changes: Update deployment guide
3. Test changes before committing

**To verify no duplication:**
- Use `single-source-validator` skill to check for duplicated commands

---

## Version History

- **2025-11-05**: Fixed reliability - replaced SSH heredocs with direct commands, added container age verification
- **2025-11-04**: Created as automation wrapper for staging deployment

---

**Remember**: This skill automates deployment. The deployment guide provides context and manual procedures for troubleshooting.
