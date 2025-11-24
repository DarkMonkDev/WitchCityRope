#!/bin/bash
# WitchCityRope Staging Deployment
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script deploys to staging environment on DigitalOcean with:
# - Build and registry push
# - Server deployment
# - Health verification
# - Smoke tests

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🚀 WitchCityRope Staging Deployment"
echo "===================================="
echo ""
echo "📋 Purpose: Deploy to staging environment safely and correctly"
echo ""
echo "✅ Use when:"
echo "   • After Phase 5 validation passes"
echo "   • When deploying new features for testing"
echo "   • After hotfixes that need staging verification"
echo "   • When requested by user/orchestrator"
echo ""
echo "❌ DO NOT use if:"
echo "   • Git has uncommitted changes"
echo "   • You haven't tested locally first"
echo ""
echo "⚠️  CRITICAL WARNING - SHARED SERVER:"
echo "   • Server hosts MULTIPLE applications"
echo "   • NEVER use: docker stop \$(docker ps -q)"
echo "   • ALWAYS use compose files to target specific containers"
echo ""
echo "⚙️  What this script does:"
echo "   1. Checks prerequisites (git clean, SSH access)"
echo "   2. Builds production images (API and Web)"
echo "   3. Pushes to DigitalOcean Container Registry"
echo "   4. Connects to staging server"
echo "   5. Pulls and deploys new images"
echo "   6. Runs health checks"
echo "   7. Runs smoke tests"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    read -p "Continue with staging deployment? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/staging-deploy/SKILL.md"
        echo "   /docs/functional-areas/deployment/staging-deployment-guide.md"
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
#     echo "   ❌ FAIL: Tests not passing or below 90% threshold"
#     exit 1
# fi

# Check 2: Git status
echo ""
echo "2️⃣  Checking git status..."
if [ -n "$(git status --short)" ]; then
    echo "   ❌ FAIL: Uncommitted changes detected"
    echo ""
    echo "💡 Commit all changes before deploying:"
    git status --short
    exit 1
fi
echo "   ✅ Git clean"

# Check 3: Branch verification
echo ""
echo "3️⃣  Checking branch..."
BRANCH=$(git branch --show-current)
echo "   Current branch: $BRANCH"
if [ "$BRANCH" != "main" ] && [ "$BRANCH" != "staging" ]; then
    echo "   ⚠️  WARNING: Not on main or staging branch"
    if [ "$SKIP_CONFIRMATION" != "true" ]; then
        read -p "   Continue anyway? (y/N): " BRANCH_CONFIRM
        if [[ ! "$BRANCH_CONFIRM" =~ ^[Yy]$ ]]; then
            echo "   ❌ Aborted - switch to main or staging branch"
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
    echo ""
    echo "💡 Ensure SSH key is properly configured"
    exit 1
fi
echo "   ✅ SSH key found"

# Check 5: Docker running
echo ""
echo "5️⃣  Checking Docker..."
if ! docker ps > /dev/null 2>&1; then
    echo "   ❌ FAIL: Docker is not running"
    echo ""
    echo "💡 Start Docker and try again"
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
# MAIN SCRIPT - STAGING DEPLOYMENT
# ============================================

# Configuration
REGISTRY="registry.digitalocean.com/witchcityrope"
SERVER="104.131.165.14"
USER="witchcity"
DEPLOY_PATH="/opt/witchcityrope/staging"
GIT_SHA=$(git rev-parse --short HEAD)

echo "📦 Deployment Details:"
echo "   Registry: $REGISTRY"
echo "   Server: $SERVER"
echo "   Deploy Path: $DEPLOY_PATH"
echo "   Git SHA: $GIT_SHA"
echo "   URL: https://staging.notfai.com"
echo ""

# Step 1: Build production images
echo "1️⃣  Building production images..."
echo ""

# Build API
echo "   Building API image..."
docker build \
  -f apps/api/Dockerfile \
  -t $REGISTRY/staging-api-witchcityrope:latest \
  -t $REGISTRY/staging-api-witchcityrope:$GIT_SHA \
  --target production \
  .

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: API build failed"
    echo ""
    echo "💡 Fix build errors and try again"
    echo "   See: .claude/skills/staging-deploy/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ API image built"

# Build Web
echo ""
echo "   Building Web image..."
docker build \
  -f apps/web/Dockerfile \
  -t $REGISTRY/staging-web-witchcityrope:latest \
  -t $REGISTRY/staging-web-witchcityrope:$GIT_SHA \
  --target production \
  --build-arg BUILD_MODE=staging \
  --build-arg VITE_API_BASE_URL= \
  --build-arg VITE_APP_TITLE="WitchCityRope" \
  --build-arg VITE_APP_VERSION="$GIT_SHA" \
  .

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Web build failed"
    echo ""
    echo "💡 Fix build errors and try again"
    echo "   See: .claude/skills/staging-deploy/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ Web image built"
echo ""

# Step 2: Push to registry
echo "2️⃣  Pushing to DigitalOcean Container Registry..."
echo ""

echo "   Pushing API image..."
docker push $REGISTRY/staging-api-witchcityrope:latest
docker push $REGISTRY/staging-api-witchcityrope:$GIT_SHA
echo "   ✅ API image pushed"

echo ""
echo "   Pushing Web image..."
docker push $REGISTRY/staging-web-witchcityrope:latest
docker push $REGISTRY/staging-web-witchcityrope:$GIT_SHA
echo "   ✅ Web image pushed"
echo ""

# Step 3: Test server connectivity
echo "3️⃣  Testing server connectivity..."
if ! ssh -i $SSH_KEY -o ConnectTimeout=10 $USER@$SERVER "echo '   ✅ Connected to server'" ; then
    echo "   ❌ FAIL: Cannot connect to server"
    echo ""
    echo "💡 Check SSH configuration and network connectivity"
    exit 1
fi
echo ""

# Step 4: Pull images on server
echo "4️⃣  Pulling images on server..."
echo "   Pulling latest images..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml pull"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Image pull failed"
    echo ""
    echo "💡 Check registry access on server"
    echo "   See: .claude/skills/staging-deploy/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ Images pulled"
echo ""

# Step 5: Deploy (restart containers)
echo "5️⃣  Deploying containers..."
echo "   Restarting containers..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml up -d"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Container restart failed"
    echo ""
    echo "💡 Check container logs on server"
    echo "   ssh $USER@$SERVER 'docker logs witchcity-api-staging'"
    exit 1
fi
echo "   ✅ Containers restart command completed"
echo ""

# Step 5b: Verify containers were actually recreated
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
    echo ""
    echo "💡 Manual restart required:"
    echo "   ssh $USER@$SERVER 'cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml up -d'"
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
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml ps"
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
    echo ""
    echo "💡 Consider rollback - use staging-rollback skill"
    echo "   See: .claude/skills/staging-deploy/SKILL.md (Common Issues)"
    exit 1
fi

# API health
echo ""
echo "   Checking API service..."
if curl -f -s https://staging.notfai.com/api/health > /dev/null; then
    echo "   ✅ API service healthy (https://staging.notfai.com/api/health)"
else
    echo "   ❌ API health check failed"
    echo ""
    echo "💡 Consider rollback - use staging-rollback skill"
    echo "   See: .claude/skills/staging-deploy/SKILL.md (Common Issues)"
    exit 1
fi

# Database health (via API) - Non-blocking check
echo ""
echo "   Checking database..."
if curl -f -s https://staging.notfai.com/api/health/database > /dev/null; then
    echo "   ✅ Database healthy"
else
    echo "   ⚠️  Database health check failed"
    echo "   Note: This is non-critical if API logs show successful DB connectivity"
    echo "   Continuing deployment - verify database connectivity in API logs above"
fi

echo ""

# Step 9: Smoke tests
echo "9️⃣  Running smoke tests..."
echo ""

# Test critical endpoints
SMOKE_PASS=0
SMOKE_FAIL=0

echo "   Testing homepage..."
if curl -f -s https://staging.notfai.com/ | grep -q "Witch City Rope"; then
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
echo "   • Images: $REGISTRY/*-staging:latest, :$GIT_SHA"
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
