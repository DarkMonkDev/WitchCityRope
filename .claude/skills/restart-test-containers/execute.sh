#!/bin/bash
# WitchCityRope Test Container Restart - Correct Procedure
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script restarts Docker TEST containers the RIGHT way with:
# - Test overlay (docker-compose.test.yml)
# - --no-cache build by default (clean test images)
# - Project isolation (-p witchcityrope-test)
# - Health verification
#
# Supports --skip-rebuild flag to skip container rebuild when code hasn't changed

set -e  # Exit on error

# ============================================
# PARSE ARGUMENTS
# ============================================

SKIP_REBUILD=false
USE_CACHE=false

for arg in "$@"; do
    case $arg in
        --skip-rebuild)
            SKIP_REBUILD=true
            shift
            ;;
        --use-cache)
            USE_CACHE=true
            shift
            ;;
        *)
            # Unknown argument
            ;;
    esac
done

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🧪 WitchCityRope Test Container Restart"
echo "========================================"
echo ""
echo "📋 Purpose: Restart Docker TEST containers with proper configuration"
echo ""
echo "✅ Use when:"
echo "   • Before running E2E tests"
echo "   • When test containers are unhealthy or stopped"
echo "   • After code changes that need fresh test container build"
echo ""
echo "❌ DO NOT use if:"
echo "   • You need dev containers - use restart-dev-containers instead"
echo ""
echo "⚙️  What this script does:"
echo "   1. Stops existing test containers"
if [ "$SKIP_REBUILD" = true ]; then
    echo "   2. Starts containers WITHOUT rebuild (--skip-rebuild mode)"
elif [ "$USE_CACHE" = true ]; then
    echo "   2. Rebuilds containers with Docker cache (--use-cache mode, faster)"
else
    echo "   2. Rebuilds with --no-cache for clean test images (default)"
fi
echo "   3. Waits for containers to initialize"
echo "   4. Verifies health endpoints"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    read -p "Continue with test container restart? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/restart-test-containers/SKILL.md"
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

# Check test overlay exists
if [ ! -f "docker-compose.test.yml" ]; then
    echo "❌ FAIL: docker-compose.test.yml not found"
    echo ""
    echo "💡 This is required for test configuration"
    exit 1
fi
echo "✅ Test overlay found"

echo ""

# ============================================
# MAIN SCRIPT - CONTAINER RESTART
# ============================================

# Step 1: Stop existing test containers
echo "1️⃣  Stopping test containers..."
# CRITICAL: Use -p witchcityrope-test to isolate from dev containers
# Without this flag, restarting test containers will DELETE dev containers!
docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml down -v 2>/dev/null || true
echo "   ✅ Test containers stopped"
echo ""

# Step 2: Start with test overlay
if [ "$SKIP_REBUILD" = true ]; then
    echo "2️⃣  Starting test containers (skip-rebuild mode)..."
    echo "   Using: docker-compose.yml + docker-compose.test.yml"
    echo "   Project: witchcityrope-test (isolated from dev containers)"
    echo "   Build: Skipped (using existing images)"
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml up -d
    BUILD_MODE="skip"
    NO_CACHE="false"
elif [ "$USE_CACHE" = true ]; then
    echo "2️⃣  Building and starting test containers (with cache)..."
    echo "   Using: docker-compose.yml + docker-compose.test.yml"
    echo "   Project: witchcityrope-test (isolated from dev containers)"
    echo "   Build: Incremental rebuild (uses Docker cache for speed)"
    # Build with Docker cache (much faster), then start containers
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml build
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml up -d
    BUILD_MODE="incremental"
    NO_CACHE="false"
else
    echo "2️⃣  Building and starting test containers..."
    echo "   Using: docker-compose.yml + docker-compose.test.yml"
    echo "   Project: witchcityrope-test (isolated from dev containers)"
    echo "   Build: Clean rebuild with --no-cache (ensures fresh test files)"
    # Build first with --no-cache, then start containers
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml build --no-cache
    docker-compose -p witchcityrope-test -f docker-compose.yml -f docker-compose.test.yml up -d
    BUILD_MODE="full"
    NO_CACHE="true"
fi

echo "   ✅ Test containers starting..."
echo ""

# Step 3: Wait for containers to initialize
echo "3️⃣  Waiting for initialization..."
sleep 20
echo "   ✅ Initial wait complete"
echo ""

# Step 4: Check container status
echo "4️⃣  Checking container status..."
docker ps --format "table {{.Names}}\t{{.Status}}" | grep -E "witchcity.*test" || true

# Expected containers: postgres-test, api-test, web-test, test-runner, db-test-helper
EXPECTED_COUNT=5
RUNNING_COUNT=$(docker ps --format "{{.Names}}" | grep -cE "witchcity.*test" || echo "0")
if [ "$RUNNING_COUNT" -lt "$EXPECTED_COUNT" ]; then
    echo "   ⚠️  WARNING: Expected $EXPECTED_COUNT containers, found $RUNNING_COUNT"
    echo ""
    echo "💡 Troubleshooting:"
    echo "   • Check logs: docker logs witchcity-web-test"
    echo "   • Check logs: docker logs witchcity-api-test"
    echo "   • See: .claude/skills/restart-test-containers/SKILL.md (Common Issues)"
    # Don't exit - some containers might still be starting
fi
echo "   ✅ $RUNNING_COUNT containers running"
echo ""

# Step 5: Wait for services to be ready
echo "5️⃣  Waiting for services to be ready..."
sleep 10
echo "   ✅ Services initializing..."
echo ""

# Step 6: Verify health endpoints (with retry)
echo "6️⃣  Verifying health endpoints..."

# Helper function for retrying health checks
check_container_health() {
    local container=$1
    local url=$2
    local name=$3
    local max_attempts=15  # 15 attempts * 5 seconds = 75 seconds max
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        # Check if container exists and is running
        if ! docker ps --format "{{.Names}}" | grep -q "$container"; then
            echo "   ❌ Container $container not running"
            return 1
        fi

        # Check health inside container
        if docker exec "$container" curl -sf "$url" > /dev/null 2>&1; then
            echo "   ✅ $name healthy"
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

# Check test-runner container (just needs to be running)
if docker ps --format "{{.Names}}" | grep -q "witchcity-test-runner"; then
    echo "   ✅ Test runner container running"
    TEST_RUNNER_HEALTHY="healthy"
else
    echo "   ❌ Test runner container not running"
    TEST_RUNNER_HEALTHY="unhealthy"
fi

# API service health
if ! check_container_health "witchcity-api-test" "http://localhost:8080/health" "API service"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-api-test"
    echo "   See: .claude/skills/restart-test-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-test-containers\",\"status\":\"failure\",\"error\":\"API service health check failed\",\"details\":\"API service did not respond after 75 seconds\",\"action\":\"Check container logs: docker logs witchcity-api-test\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

# Web service health
if ! check_container_health "witchcity-web-test" "http://localhost:5173" "Web service"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-web-test"
    echo "   See: .claude/skills/restart-test-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-test-containers\",\"status\":\"failure\",\"error\":\"Web service health check failed\",\"details\":\"Web service did not respond after 75 seconds\",\"action\":\"Check container logs: docker logs witchcity-web-test\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

# Database health (via API detailed health)
if ! check_container_health "witchcity-api-test" "http://localhost:8080/api/health/detailed" "Database (via API)"; then
    echo ""
    echo "💡 Check logs: docker logs witchcity-postgres-test"
    echo "💡 Check logs: docker logs witchcity-api-test"
    echo "   See: .claude/skills/restart-test-containers/SKILL.md (Common Issues)"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"restart-test-containers\",\"status\":\"failure\",\"error\":\"Database health check failed\",\"details\":\"Database connection check did not respond after 75 seconds\",\"action\":\"Check container logs: docker logs witchcity-postgres-test and docker logs witchcity-api-test\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi

echo ""
echo "✅ Test Container Restart Complete"
echo "==================================="
echo ""
echo "📊 Status Summary:"
echo "   • Containers: $RUNNING_COUNT/$EXPECTED_COUNT running"
if [ "$SKIP_REBUILD" = true ]; then
    echo "   • Build: Skipped (--skip-rebuild mode)"
elif [ "$USE_CACHE" = true ]; then
    echo "   • Build: Incremental rebuild with Docker cache (--use-cache mode)"
else
    echo "   • Build: Clean rebuild with --no-cache (default)"
fi
echo "   • Health checks: All passing"
echo ""
echo "🎯 Ready for:"
echo "   • Running E2E tests"
echo "   • Running integration tests"
echo ""

# Output structured data for agents
echo "=== SKILL_RESULT ==="
cat <<EOF
{
  "skill": "restart-test-containers",
  "status": "success",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "containers": {
    "running": $RUNNING_COUNT,
    "expected": $EXPECTED_COUNT,
    "healthy": true
  },
  "build": {
    "mode": "$BUILD_MODE",
    "no_cache": $NO_CACHE
  },
  "healthChecks": {
    "web": "healthy",
    "api": "healthy",
    "database": "healthy",
    "test_runner": "$TEST_RUNNER_HEALTHY"
  },
  "readyForTesting": true,
  "message": "Test environment ready"
}
EOF
echo "=== END_SKILL_RESULT ==="

# Return success
exit 0
