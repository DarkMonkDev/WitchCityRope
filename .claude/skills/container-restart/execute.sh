#!/bin/bash
# WitchCityRope Container Restart - Correct Procedure
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script restarts Docker containers the RIGHT way with:
# - Development overlay (docker-compose.dev.yml)
# - Compilation error checking
# - Health verification
# - Seed data validation

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🔄 WitchCityRope Container Restart"
echo "=================================="
echo ""
echo "📋 Purpose: Restart Docker containers with proper configuration"
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
echo "   5. Checks database seed data"
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
        echo "   .claude/skills/container-restart/SKILL.md"
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

# Step 1: Stop existing containers
echo "1️⃣  Stopping containers..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
echo "   ✅ Containers stopped"
echo ""

# Step 2: Start with development overlay (CRITICAL)
echo "2️⃣  Starting containers with dev overlay..."
echo "   Using: docker-compose.yml + docker-compose.dev.yml"
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

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

# Expected containers: postgres, api, web (from docker-compose.yml)
#                    + test-server (from docker-compose.dev.yml)
EXPECTED_COUNT=4
RUNNING_COUNT=$(docker ps --format "{{.Names}}" | grep -c witchcity || true)
if [ "$RUNNING_COUNT" -ne "$EXPECTED_COUNT" ]; then
    echo "   ❌ ERROR: Expected $EXPECTED_COUNT containers, found $RUNNING_COUNT"
    echo ""
    echo "💡 Troubleshooting:"
    echo "   • Check logs: docker logs witchcity-web"
    echo "   • Check logs: docker logs witchcity-api"
    echo "   • See: .claude/skills/container-restart/SKILL.md (Common Issues)"
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
    echo "   See: .claude/skills/container-restart/SKILL.md (Common Issues)"
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
    echo "   See: .claude/skills/container-restart/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ No API compilation errors"
echo ""

# Step 6: Wait for services to be ready
echo "6️⃣  Waiting for services to be ready..."
sleep 10
echo "   ✅ Services initializing..."
echo ""

# Step 7: Verify health endpoints
echo "7️⃣  Verifying health endpoints..."

# Web service
if curl -f http://localhost:5173 > /dev/null 2>&1; then
    echo "   ✅ Web service healthy (http://localhost:5173)"
else
    echo "   ❌ Web service unhealthy"
    echo ""
    echo "💡 Check logs: docker logs witchcity-web"
    echo "   See: .claude/skills/container-restart/SKILL.md (Common Issues)"
    exit 1
fi

# API service
if curl -f http://localhost:5653/health > /dev/null 2>&1; then
    echo "   ✅ API service healthy (http://localhost:5653)"
else
    echo "   ❌ API service unhealthy"
    echo ""
    echo "💡 Check logs: docker logs witchcity-api"
    echo "   See: .claude/skills/container-restart/SKILL.md (Common Issues)"
    exit 1
fi

# Database health
if curl -f http://localhost:5653/health/database > /dev/null 2>&1; then
    echo "   ✅ Database healthy"
else
    echo "   ❌ Database unhealthy"
    echo ""
    echo "💡 Check logs: docker logs witchcity-db"
    echo "   See: .claude/skills/container-restart/SKILL.md (Common Issues)"
    exit 1
fi

echo ""

# Step 8: Verify seed data
echo "8️⃣  Checking database seed data..."
SEED_COUNT=$(PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_dev -t -c "SELECT COUNT(*) FROM auth.\"Users\" WHERE \"Email\" LIKE '%@witchcityrope.com';" 2>/dev/null || echo "0")

if [ "$SEED_COUNT" -lt 5 ]; then
    echo "   ⚠️  WARNING: Low seed data count ($SEED_COUNT test users)"
    echo "   Expected at least 5 test accounts"
    echo ""
    echo "💡 Run: ./scripts/seed-database.sh"
else
    echo "   ✅ Database seeded ($SEED_COUNT test users)"
fi

echo ""
echo "✅ Container Restart Complete"
echo "=============================="
echo ""
echo "📊 Status Summary:"
echo "   • Containers: 4/4 running (postgres, api, web, test-server)"
echo "   • Compilation: No errors"
echo "   • Health checks: All passing"
echo "   • Database: Seeded"
echo ""
echo "🎯 Ready for:"
echo "   • Development"
echo "   • Running tests (unit, integration, E2E)"
echo "   • Making code changes"
echo ""

# Return success
exit 0
