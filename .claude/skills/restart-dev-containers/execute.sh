#!/bin/bash
# WitchCityRope Dev Container Restart - Correct Procedure
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script restarts Docker DEVELOPMENT containers the RIGHT way with:
# - Development overlay (docker-compose.dev.yml)
# - Compilation error checking
# - Health verification
# - Seed data validation

set -e  # Exit on error

# ============================================
# OPTIONAL VAULT INTEGRATION
# ============================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../_shared/vault-helpers.sh"

VAULT_AVAILABLE=false
echo "🔐 Checking Vault availability..."
if vault_try_init; then
    VAULT_AVAILABLE=true
fi
echo ""

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🔄 WitchCityRope Dev Container Restart"
echo "======================================="
echo ""
echo "📋 Purpose: Restart Docker DEVELOPMENT containers with proper configuration"
echo ""
echo "✅ Use when:"
echo "   • Before running E2E tests (MANDATORY)"
echo "   • After code changes that need container rebuild"
echo "   • When containers are unhealthy"
echo "   • When 'Element not found' errors appear in E2E tests"
echo "   • When database seed data is missing"
echo ""
echo "❌ DO NOT use if:"
echo "   • Containers are already healthy and tests are passing"
echo "   • You just need to view logs (use: docker logs <container-name>)"
echo ""
echo "⚙️  What this script does:"
echo "   1. Stops existing containers"
echo "   2. Rebuilds with dev overlay (docker-compose.yml + docker-compose.dev.yml)"
echo "   3. Checks for compilation errors"
echo "   4. Verifies health endpoints"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    read -p "Continue with container restart? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/restart-dev-containers/SKILL.md"
        exit 0
    fi
    echo ""
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Running prerequisite checks..."
echo ""

# Check Docker is running
if ! docker ps > /dev/null 2>&1; then
    echo "❌ FAIL: Docker is not running"
    echo ""
    echo "💡 Start Docker and try again"
    exit 1
fi
echo "✅ Docker is running"

# Check we're in project root
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ FAIL: Must run from project root (witchcityrope/)"
    echo ""
    echo "💡 Current directory: $(pwd)"
    echo "   Expected: /home/chad/repos/witchcityrope"
    exit 1
fi
echo "✅ In project root directory"

# Check dev overlay exists
if [ ! -f "docker-compose.dev.yml" ]; then
    echo "❌ FAIL: docker-compose.dev.yml not found"
    echo ""
    echo "💡 This is required for development configuration"
    exit 1
fi
echo "✅ Dev overlay found"

echo ""

# ============================================
# MAIN SCRIPT - CONTAINER RESTART
# ============================================

# Step 0: Pull dev secrets from vault (optional)
if [ "$VAULT_AVAILABLE" = true ]; then
    echo "0️⃣  Pulling dev secrets from vault..."
    if vault_pull_env "secret/projects/witchcityrope/staging" ".env.development"; then
        echo "   ✅ .env.development updated from vault (staging secrets for local dev)"
    else
        echo "   ⚠️  Could not pull secrets - using existing .env.development"
    fi
    echo ""
fi

# Step 1: Stop existing containers
echo "1️⃣  Stopping containers..."
# CRITICAL: Use -p witchcityrope-dev to isolate from test containers
# Without this flag, restarting dev containers will DELETE test containers!
# See: docs/test-baselines/test-parity-investigation-2025-12-01.md (Session 10)
docker-compose -p witchcityrope-dev -f docker-compose.yml -f docker-compose.dev.yml down
echo "   ✅ Containers stopped"
echo ""

# Step 2: Start with development overlay (CRITICAL)
echo "2️⃣  Starting containers with dev overlay..."
echo "   Using: docker-compose.yml + docker-compose.dev.yml"
echo "   Project: witchcityrope-dev (isolated from test containers)"
# CRITICAL: Use -p witchcityrope-dev to isolate from test containers
docker-compose -p witchcityrope-dev -f docker-compose.yml -f docker-compose.dev.yml up -d --build

echo "   ✅ Containers starting..."
echo ""

# Step 3: Wait for containers to initialize
echo "3️⃣  Waiting for initialization..."
sleep 15
echo "   ✅ Initial wait complete"
echo ""

# Step 4: Check container status
echo "4️⃣  Checking container status..."
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep witchcity

# Expected containers: postgres, api, web (3 core dev containers)
# Test infrastructure is handled separately by restart-test-containers skill
EXPECTED_COUNT=3
RUNNING_COUNT=$(docker ps --format "{{.Names}}" | grep -E "^witchcity-(postgres|api|web)$" | wc -l)
if [ "$RUNNING_COUNT" -ne "$EXPECTED_COUNT" ]; then
    echo "   ❌ ERROR: Expected $EXPECTED_COUNT containers, found $RUNNING_COUNT"
    echo ""
    echo "💡 Troubleshooting:"
    echo "   • Check logs: docker logs witchcity-web"
    echo "   • Check logs: docker logs witchcity-api"
    echo "   • See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"Container count mismatch\",\"details\":\"Expected $EXPECTED_COUNT containers, found $RUNNING_COUNT\",\"action\":\"Check container logs and restart\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi
echo "   ✅ All $EXPECTED_COUNT containers running"
echo ""

# Step 5: Check for compilation errors (CRITICAL)
echo "5️⃣  Checking for compilation errors..."
echo ""

# Check Web container
echo "   Web container logs:"
WEB_ERRORS=$(docker logs witchcity-web --tail 50 2>&1 | grep -i "error" | grep -v "0 error" || true)
if [ -n "$WEB_ERRORS" ]; then
    echo "   ❌ COMPILATION ERRORS IN WEB:"
    echo "$WEB_ERRORS"
    echo ""
    echo "💡 FIX SOURCE CODE AND RESTART"
    echo "   See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"Web compilation errors\",\"details\":\"$(echo "$WEB_ERRORS" | head -n 1 | sed 's/"/\\"/g')\",\"action\":\"Fix source code and restart\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi
echo "   ✅ No web compilation errors"

# Check API container
echo ""
echo "   API container logs:"
API_ERRORS=$(docker logs witchcity-api --tail 50 2>&1 | grep -i "error" | grep -v "0 error" || true)
if [ -n "$API_ERRORS" ]; then
    echo "   ❌ COMPILATION ERRORS IN API:"
    echo "$API_ERRORS"
    echo ""
    echo "💡 FIX SOURCE CODE AND RESTART"
    echo "   See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"API compilation errors\",\"details\":\"$(echo "$API_ERRORS" | head -n 1 | sed 's/"/\\"/g')\",\"action\":\"Fix source code and restart\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi
echo "   ✅ No API compilation errors"
echo ""

# Step 6: Wait for services to be ready
echo "6️⃣  Waiting for services to be ready..."
sleep 10
echo "   ✅ Services initializing..."
echo ""

# Step 7: Verify health endpoints (with retry)
echo "7️⃣  Verifying health endpoints..."

# Helper function for retrying health checks
check_health() {
    local url=$1
    local name=$2
    local max_attempts=12  # 12 attempts * 5 seconds = 60 seconds max
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        if curl -f "$url" > /dev/null 2>&1; then
            echo "   ✅ $name healthy ($url)"
            return 0
        fi

        if [ $attempt -lt $max_attempts ]; then
            echo "   ⏳ Waiting for $name (attempt $attempt/$max_attempts)..."
            sleep 5
        fi
        attempt=$((attempt + 1))
    done

    echo "   ❌ $name unhealthy after $max_attempts attempts"
    return 1
}

# Web service
if ! check_health "http://localhost:5173" "Web service"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-web"
    echo "   See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"Web service health check failed\",\"details\":\"Web service did not respond after 60 seconds\",\"action\":\"Check container logs: docker logs witchcity-web\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

# API service
if ! check_health "http://localhost:5655/health" "API service"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-api"
    echo "   See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"API service health check failed\",\"details\":\"API service did not respond after 60 seconds\",\"action\":\"Check container logs: docker logs witchcity-api\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

# Database health (via detailed health endpoint)
if ! check_health "http://localhost:5655/api/health/detailed" "Database (detailed health)"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-db"
    echo "💡 Check logs: docker logs witchcity-api"
    echo "   See: .claude/skills/restart-dev-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-dev-containers\",\"status\":\"failure\",\"error\":\"Database health check failed\",\"details\":\"Database connection check did not respond after 60 seconds\",\"action\":\"Check container logs: docker logs witchcity-db and docker logs witchcity-api\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

echo ""
echo "✅ Container Restart Complete"
echo "=============================="
echo ""
echo "📊 Status Summary:"
echo "   • Containers: 3/3 running (postgres, api, web)"
echo "   • Compilation: No errors"
echo "   • Health checks: All passing"
echo ""
echo "🎯 Ready for:"
echo "   • Development"
echo "   • Running tests (unit, integration, E2E)"
echo "   • Making code changes"
echo ""

# Output structured data for agents
echo "=== SKILL_RESULT ==="
cat <<EOF
{
  "skill": "restart-dev-containers",
  "status": "success",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "containers": {
    "running": 3,
    "expected": 3,
    "healthy": true
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
  "readyForTesting": true,
  "message": "Environment ready for development and testing"
}
EOF
echo "=== END_SKILL_RESULT ==="

# Return success
exit 0
