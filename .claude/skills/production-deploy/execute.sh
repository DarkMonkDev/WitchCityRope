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
# VAULT INTEGRATION
# ============================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../_shared/vault-helpers.sh"

echo "🔐 Initializing Vault..."
vault_init

# Pull production .env from vault to temp file
LOCAL_ENV_FILE=$(mktemp /tmp/witchcityrope-production-env.XXXXXX)
trap "rm -f $LOCAL_ENV_FILE" EXIT

vault_pull_env "secret/projects/witchcityrope/production" "$LOCAL_ENV_FILE"

# Get SSH key path from shared secrets
SSH_KEY_FILENAME=$(vault_get_field "secret/shared/digitalocean" "SSH_KEY_FILENAME")
SSH_KEY_PATH="$HOME/.ssh/$SSH_KEY_FILENAME"
echo "   ✅ SSH key path: $SSH_KEY_PATH"
echo ""

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

# Check 4: SSH key (path from vault)
echo ""
echo "4️⃣  Checking SSH access..."
SSH_KEY="$SSH_KEY_PATH"
if [ ! -f "$SSH_KEY" ]; then
    echo "   ❌ FAIL: SSH key not found: $SSH_KEY"
    echo ""
    echo "💡 Key path from vault: secret/shared/digitalocean -> SSH_KEY_FILENAME"
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

# Build Web (pull VITE_ build-time vars from vault-generated env file)
echo ""
echo "   Building Web image..."
VITE_PAYPAL_CLIENT_ID=$(grep '^VITE_PAYPAL_CLIENT_ID=' "$LOCAL_ENV_FILE" | cut -d'=' -f2-)
VITE_PAYPAL_MODE=$(grep '^VITE_PAYPAL_MODE=' "$LOCAL_ENV_FILE" | cut -d'=' -f2-)
VITE_AUTHORIZENET_LOGIN_ID=$(grep '^VITE_AUTHORIZENET_LOGIN_ID=' "$LOCAL_ENV_FILE" | cut -d'=' -f2-)
VITE_AUTHORIZENET_CLIENT_KEY=$(grep '^VITE_AUTHORIZENET_CLIENT_KEY=' "$LOCAL_ENV_FILE" | cut -d'=' -f2-)
VITE_AUTHORIZENET_ENVIRONMENT=$(grep '^VITE_AUTHORIZENET_ENVIRONMENT=' "$LOCAL_ENV_FILE" | cut -d'=' -f2-)

docker build \
  -f apps/web/Dockerfile \
  -t $REGISTRY/production-web-witchcityrope:latest \
  -t $REGISTRY/production-web-witchcityrope:$GIT_SHA \
  --target production \
  --build-arg BUILD_MODE=production \
  --build-arg VITE_API_BASE_URL="" \
  --build-arg VITE_APP_TITLE="WitchCityRope" \
  --build-arg VITE_APP_VERSION="$GIT_SHA" \
  --build-arg VITE_PAYPAL_CLIENT_ID="$VITE_PAYPAL_CLIENT_ID" \
  --build-arg VITE_PAYPAL_MODE="${VITE_PAYPAL_MODE:-sandbox}" \
  --build-arg VITE_AUTHORIZENET_LOGIN_ID="$VITE_AUTHORIZENET_LOGIN_ID" \
  --build-arg VITE_AUTHORIZENET_CLIENT_KEY="$VITE_AUTHORIZENET_CLIENT_KEY" \
  --build-arg VITE_AUTHORIZENET_ENVIRONMENT="${VITE_AUTHORIZENET_ENVIRONMENT:-SANDBOX}" \
  --build-arg VITE_ACCEPTJS_URL="https://js.authorize.net/v1/Accept.js" \
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

# Step 4: Update compose file and .env on server
echo "4️⃣  Updating docker-compose and .env files on server..."
scp -i $SSH_KEY deployment/docker-compose.production.yml $USER@$SERVER:$DEPLOY_PATH/docker-compose.production.yml

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not copy docker-compose file to server"
    exit 1
fi
echo "   ✅ Compose file updated"

# SCP vault-generated .env to server
scp -i $SSH_KEY "$LOCAL_ENV_FILE" $USER@$SERVER:$DEPLOY_PATH/.env.production

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not copy .env.production to server"
    exit 1
fi
echo "   ✅ .env.production updated from vault"
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

# Step 5b: Enable maintenance mode before container restart
# This shows a branded maintenance page to users while containers are restarting,
# instead of showing ugly 502/504 errors. Disabled automatically after health checks pass.
# Backdoor URL (notfai.com) remains accessible during maintenance.
echo "5️⃣b Enabling maintenance mode..."
MAINTENANCE_SCRIPT="$SCRIPT_DIR/../maintenance-mode/execute.sh"
if [ -f "$MAINTENANCE_SCRIPT" ]; then
    SKIP_CONFIRMATION=true bash "$MAINTENANCE_SCRIPT" --on --env production || {
        echo "   WARNING: Could not enable maintenance mode (continuing anyway)"
        echo "   Users may see brief errors during container restart"
    }
    MAINTENANCE_ENABLED=true
    echo "   Maintenance mode enabled - users see maintenance page"
    echo "   Backdoor: https://notfai.com (still works)"
else
    echo "   WARNING: Maintenance mode skill not found at $MAINTENANCE_SCRIPT"
    echo "   Skipping maintenance mode (users may see errors during restart)"
    MAINTENANCE_ENABLED=false
fi
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

# Step 9: Internal health checks (via SSH, bypasses nginx/maintenance mode)
echo "9️⃣  Running internal health checks (localhost via SSH)..."
echo ""

# API health (localhost:5001)
echo "   Checking API service (localhost:5001/health)..."
if ssh -i $SSH_KEY $USER@$SERVER "curl -f -s http://localhost:5001/health" > /dev/null 2>&1; then
    echo "   ✅ API container healthy"
else
    echo "   ❌ API health check failed"
    echo ""
    echo "💡 Containers are unhealthy - maintenance mode stays ON to protect users"
    echo "   ROLLBACK RECOMMENDED - use production-rollback skill"
    echo "   Check logs: ssh $USER@$SERVER 'docker logs witchcity-api-prod --tail 50'"
    exit 1
fi

# Web health (localhost:3001)
echo "   Checking web service (localhost:3001)..."
if ssh -i $SSH_KEY $USER@$SERVER "curl -f -s http://localhost:3001/" > /dev/null 2>&1; then
    echo "   ✅ Web container healthy"
else
    echo "   ❌ Web health check failed"
    echo ""
    echo "💡 Containers are unhealthy - maintenance mode stays ON to protect users"
    echo "   ROLLBACK RECOMMENDED - use production-rollback skill"
    exit 1
fi

# Database health (via API localhost)
echo "   Checking database connectivity (localhost:5001/health)..."
if ssh -i $SSH_KEY $USER@$SERVER "curl -s http://localhost:5001/health" 2>/dev/null | grep -q '"databaseConnected":true'; then
    echo "   ✅ Database connected"
else
    echo "   ⚠️  Database connectivity check inconclusive"
    echo "   Continuing - verify in API logs above"
fi

echo ""

# Step 9b: Disable maintenance mode now that internal health checks passed
if [ "$MAINTENANCE_ENABLED" = "true" ]; then
    echo "9️⃣b Disabling maintenance mode..."
    SKIP_CONFIRMATION=true bash "$MAINTENANCE_SCRIPT" --off --env production || {
        echo "   WARNING: Could not disable maintenance mode automatically"
        echo "   MANUAL ACTION REQUIRED: bash .claude/skills/maintenance-mode/execute.sh --off --env production"
    }
    echo "   Maintenance mode disabled - site is live"
    echo ""
fi

# Step 10: Smoke tests
echo "🔟  Running smoke tests..."
echo ""

SMOKE_PASS=0
SMOKE_FAIL=0

echo "   Testing homepage..."
if curl -f -s https://prod.notfai.com/ | grep -q "Witch City Rope"; then
    echo "   ✅ Homepage"
    SMOKE_PASS=$((SMOKE_PASS + 1))
else
    echo "   ❌ Homepage failed"
    SMOKE_FAIL=$((SMOKE_FAIL + 1))
fi

echo "   Testing API events endpoint..."
if curl -f -s https://prod.notfai.com/api/events > /dev/null; then
    echo "   ✅ Events API"
    SMOKE_PASS=$((SMOKE_PASS + 1))
else
    echo "   ❌ Events API failed"
    SMOKE_FAIL=$((SMOKE_FAIL + 1))
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
