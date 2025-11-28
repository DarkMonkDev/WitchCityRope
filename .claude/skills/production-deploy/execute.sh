#!/bin/bash
# WitchCityRope Production Deployment
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script deploys to production environment on DigitalOcean with:
# - Build and registry push
# - Server deployment
# - Health verification
# - Smoke tests

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🚀 WitchCityRope Production Deployment"
echo "======================================"
echo ""
echo "📋 Purpose: Deploy to PRODUCTION environment safely and correctly"
echo ""
echo "✅ Use when:"
echo "   • After staging validation"
echo "   • When promoting features to production"
echo "   • After critical hotfixes"
echo "   • When requested by user/orchestrator"
echo ""
echo "❌ DO NOT use if:"
echo "   • Git has uncommitted changes"
echo "   • Staging has not been validated"
echo ""
echo "⚠️  CRITICAL WARNING - PRODUCTION ENVIRONMENT:"
echo "   • This deploys to LIVE production (prod.notfai.com)"
echo "   • Real users will be affected"
echo "   • Have rollback plan ready"
echo ""
echo "⚙️  What this script does:"
echo "   1. Checks prerequisites (git clean, SSH access)"
echo "   2. Builds production images (API and Web)"
echo "   3. Pushes to DigitalOcean Container Registry"
echo "   4. Connects to production server"
echo "   5. Pulls and deploys new images"
echo "   6. Runs health checks"
echo "   7. Runs smoke tests"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    read -p "Continue with PRODUCTION deployment? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/production-deploy/SKILL.md"
        echo "   /docs/functional-areas/deployment/production-deployment-guide.md"
        exit 0
    fi
    echo ""
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Running prerequisite checks..."
echo ""

# Check 1: Test results (DISABLED per user request 2025-11-24)
# User requested removal of 100% pass rate requirement for deployments
echo "1️⃣  Checking test status..."
echo "   ⚠️  Test pass rate check DISABLED - deploying without test validation"
echo "   (Test report checks were removed per user request)"

# NOTE: Original test validation code commented out below
# if [ ! -f "test-results/test-execution-report.md" ]; then
#     echo "   ❌ FAIL: No test execution report found"
#     exit 1
# fi
# if ! grep -q "^status: PASS" test-results/test-execution-report.md; then
#     echo "   ❌ FAIL: Tests not passing"
#     exit 1
# fi
# PASS_RATE=$(grep "^pass_rate:" test-results/test-execution-report.md | cut -d':' -f2 | tr -d ' %' || echo "0")
# PASS_RATE_INT=$(echo "$PASS_RATE" | cut -d'.' -f1)
# if [ "$PASS_RATE_INT" != "100" ]; then
#     echo "   ❌ FAIL: Production requires 100% test pass rate"
#     exit 1
# fi

# Check 2: Git status
echo ""
echo "2️⃣  Checking git status..."
if [ "$SKIP_GIT_CHECK" = "true" ]; then
    echo "   ⚠️  Git check SKIPPED (SKIP_GIT_CHECK=true)"
    echo "   Uncommitted changes:"
    git status --short
elif [ -n "$(git status --short)" ]; then
    echo "   ❌ FAIL: Uncommitted changes detected"
    echo ""
    echo "💡 Commit all changes before deploying to production"
    git status --short
    exit 1
else
    echo "   ✅ Git clean"
fi

# Check 3: Branch verification
echo ""
echo "3️⃣  Checking branch..."
BRANCH=$(git branch --show-current)
echo "   Current branch: $BRANCH"
if [ "$BRANCH" != "main" ]; then
    echo "   ⚠️  WARNING: Not on main branch (recommended for production)"
    if [ "$SKIP_CONFIRMATION" != "true" ]; then
        read -p "   Continue anyway? (y/N): " BRANCH_CONFIRM
        if [[ ! "$BRANCH_CONFIRM" =~ ^[Yy]$ ]]; then
            echo "   ❌ Aborted - switch to main branch for production"
            exit 1
        fi
    fi
fi
echo "   ✅ Branch verified"

# Check 4: SSH key
echo ""
echo "4️⃣  Checking SSH access..."
SSH_KEY="/home/chad/.ssh/id_ed25519_witchcityrope"
if [ ! -f "$SSH_KEY" ]; then
    echo "   ❌ FAIL: SSH key not found: $SSH_KEY"
    exit 1
fi
echo "   ✅ SSH key found"

# Check 5: Docker running
echo ""
echo "5️⃣  Checking Docker..."
if ! docker ps > /dev/null 2>&1; then
    echo "   ❌ FAIL: Docker is not running"
    exit 1
fi
echo "   ✅ Docker running"

# Check 6: DigitalOcean registry login
echo ""
echo "6️⃣  Checking registry access..."
if ! docker info 2>/dev/null | grep -q "registry.digitalocean.com"; then
    echo "   ⚠️  WARNING: May not be logged into DigitalOcean registry"
    echo "   If push fails, run: doctl registry login"
fi
echo "   ✅ Registry check complete"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - PRODUCTION DEPLOYMENT
# ============================================

# Configuration
REGISTRY="registry.digitalocean.com/witchcityrope"
SERVER="104.131.165.14"
USER="witchcity"
DEPLOY_PATH="/opt/witchcityrope/production"
GIT_SHA=$(git rev-parse --short HEAD)

echo "📦 Deployment Details:"
echo "   Registry: $REGISTRY"
echo "   Server: $SERVER"
echo "   Deploy Path: $DEPLOY_PATH"
echo "   Git SHA: $GIT_SHA"
echo "   URLs: https://prod.notfai.com, https://prod.witchcityrope.com"
echo ""

# Step 1: Build production images
echo "1️⃣  Building production images..."
echo ""

# Build API
echo "   Building API image..."
docker build \
  -f apps/api/Dockerfile \
  -t $REGISTRY/production-api-witchcityrope:latest \
  -t $REGISTRY/production-api-witchcityrope:$GIT_SHA \
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
  -t $REGISTRY/production-web-witchcityrope:latest \
  -t $REGISTRY/production-web-witchcityrope:$GIT_SHA \
  --target production \
  --build-arg BUILD_MODE=production \
  --build-arg VITE_API_BASE_URL="" \
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
docker push $REGISTRY/production-api-witchcityrope:latest
docker push $REGISTRY/production-api-witchcityrope:$GIT_SHA
echo "   ✅ API image pushed"

echo ""
echo "   Pushing Web image..."
docker push $REGISTRY/production-web-witchcityrope:latest
docker push $REGISTRY/production-web-witchcityrope:$GIT_SHA
echo "   ✅ Web image pushed"
echo ""

# Step 3: Test server connectivity
echo "3️⃣  Testing server connectivity..."
if ! ssh -i $SSH_KEY -o ConnectTimeout=10 $USER@$SERVER "echo '   ✅ Connected to server'" ; then
    echo "   ❌ FAIL: Cannot connect to server"
    exit 1
fi
echo ""

# Step 4: Update compose file on server
echo "4️⃣  Updating docker-compose file on server..."
scp -i $SSH_KEY deployment/docker-compose.production.yml $USER@$SERVER:$DEPLOY_PATH/docker-compose.production.yml

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not copy docker-compose file to server"
    exit 1
fi
echo "   ✅ Compose file updated"
echo ""

# Step 5: Pull images on server
echo "5️⃣  Pulling images on server..."
echo "   Pulling latest images..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && IMAGE_TAG=latest docker-compose -f docker-compose.production.yml pull"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Image pull failed"
    exit 1
fi
echo "   ✅ Images pulled"
echo ""

# Step 6: Deploy (restart containers)
echo "6️⃣  Deploying containers..."
echo "   Forcing container recreation with latest images..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && IMAGE_TAG=latest docker-compose -f docker-compose.production.yml up -d --force-recreate"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Container restart failed"
    exit 1
fi
echo "   ✅ Containers recreated successfully"
echo ""

# Step 6b: Verify containers were actually recreated
echo "6️⃣b Verifying containers were recreated..."
CONTAINER_AGE=$(ssh -i $SSH_KEY $USER@$SERVER "docker inspect witchcity-api-prod --format='{{.State.StartedAt}}'" 2>/dev/null)
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
    exit 1
fi
echo "   ✅ Containers verified as newly created ($AGE_SECONDS seconds old)"
echo ""

# Step 7: Wait for containers to stabilize
echo "7️⃣  Waiting for services to stabilize..."
sleep 30
echo "   ✅ Wait complete"
echo ""

# Step 8: Check container status
echo "8️⃣  Checking container status..."
echo "   Container status:"
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.production.yml ps"
echo ""
echo "   Recent API logs:"
ssh -i $SSH_KEY $USER@$SERVER "docker logs witchcity-api-prod --tail 20"
echo ""

# Step 9: Health checks
echo "9️⃣  Running health checks..."
echo ""

# Web health
echo "   Checking web service..."
if curl -f -s https://prod.notfai.com/ > /dev/null; then
    echo "   ✅ Web service healthy (https://prod.notfai.com/)"
else
    echo "   ❌ Web health check failed"
    echo ""
    echo "💡 ROLLBACK RECOMMENDED - use production-rollback skill"
    exit 1
fi

# API health
echo ""
echo "   Checking API service..."
if curl -f -s https://prod.notfai.com/api/health > /dev/null; then
    echo "   ✅ API service healthy (https://prod.notfai.com/api/health)"
else
    echo "   ❌ API health check failed"
    echo ""
    echo "💡 ROLLBACK IMMEDIATELY - use production-rollback skill"
    exit 1
fi

# Database health
echo ""
echo "   Checking database..."
if curl -f -s https://prod.notfai.com/api/health/database > /dev/null; then
    echo "   ✅ Database healthy"
else
    echo "   ⚠️  Database health check failed"
    echo "   Note: Non-critical if API logs show successful DB connectivity"
fi

echo ""

# Step 10: Smoke tests
echo "🔟  Running smoke tests..."
echo ""

SMOKE_PASS=0
SMOKE_FAIL=0

echo "   Testing homepage..."
if curl -f -s https://prod.notfai.com/ | grep -q "Witch City Rope"; then
    echo "   ✅ Homepage"
    ((SMOKE_PASS++))
else
    echo "   ❌ Homepage failed"
    ((SMOKE_FAIL++))
fi

echo "   Testing API events endpoint..."
if curl -f -s https://prod.notfai.com/api/events > /dev/null; then
    echo "   ✅ Events API"
    ((SMOKE_PASS++))
else
    echo "   ❌ Events API failed"
    ((SMOKE_FAIL++))
fi

echo ""
echo "   Smoke tests: $SMOKE_PASS passed, $SMOKE_FAIL failed"

if [ $SMOKE_FAIL -gt 0 ]; then
    echo "   ⚠️  WARNING: Some smoke tests failed - CONSIDER ROLLBACK"
fi

echo ""
echo "✅ Production Deployment Complete"
echo "=================================="
echo ""
echo "📊 Deployment Summary:"
echo "   • Server: $SERVER"
echo "   • URLs: https://prod.notfai.com, https://prod.witchcityrope.com"
echo "   • Git SHA: $GIT_SHA"
echo "   • Images: $REGISTRY/*-production:latest, :$GIT_SHA"
echo "   • Smoke tests: $SMOKE_PASS/$((SMOKE_PASS + SMOKE_FAIL))"
echo ""
echo "🎯 Next Steps:"
echo "   1. Manually test critical user flows"
echo "   2. Monitor logs: ssh $USER@$SERVER 'docker logs -f witchcity-api-prod'"
echo "   3. Monitor production: https://prod.notfai.com"
echo "   4. If issues: Use production-rollback skill IMMEDIATELY"
echo ""

exit 0
