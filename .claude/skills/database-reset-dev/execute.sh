#!/bin/bash
# WitchCityRope Dev Database Reset - Delete All Data and Reseed
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script performs DESTRUCTIVE database reset with:
# - Full schema drop (public + cms)
# - API container restart only (triggers auto-seeding)
# - Seed data verification

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🗄️  Dev Database Reset (Full Data Delete + Auto-Reseed)"
echo "======================================================="
echo ""
echo "📋 Purpose: Delete all data in dev database and restart API to trigger auto-seeding"
echo ""
echo "✅ Use when:"
echo "   • Need fresh seed data for testing"
echo "   • Database data is corrupted or inconsistent"
echo "   • After manual data changes that broke the database state"
echo "   • Testing with clean slate required"
echo "   • Seed data accidentally modified or deleted"
echo ""
echo "❌ DO NOT use if:"
echo "   • This is staging database (use database-reset-staging skill)"
echo "   • This is production database (NEVER!)"
echo "   • Just need container restart without data reset (use container-restart skill)"
echo ""
echo "🚨 CRITICAL WARNINGS - DESTRUCTIVE OPERATION:"
echo "   • ALL data in development database will be DELETED"
echo "   • Both 'public' AND 'cms' schemas will be DROPPED"
echo "   • Cannot be undone"
echo "   • Only affects local dev database (witchcityrope_dev)"
echo "   • API container will automatically re-seed on startup"
echo ""
echo "🔧 What happens:"
echo "   1. Drop all database schemas (deletes all data)"
echo "   2. Recreate empty public schema"
echo "   3. Restart ONLY the API container"
echo "   4. API auto-seeds database on startup (10-15 seconds)"
echo "   5. Verify seed data populated"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    echo "⚠️  WARNING: This will DELETE ALL data in dev database!"
    echo "   Database: witchcityrope_dev"
    echo "   Schemas: public, cms"
    echo ""
    read -p "Continue with database reset? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/database-reset-dev/SKILL.md"
        exit 0
    fi
    echo ""
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Running prerequisite checks..."
echo ""

# Check 1: Docker is running
echo "1️⃣  Checking Docker..."
if ! docker ps > /dev/null 2>&1; then
    echo "   ❌ FAIL: Docker is not running"
    echo ""
    echo "💡 Start Docker and try again"
    exit 1
fi
echo "   ✅ Docker is running"

# Check 2: Project root
echo ""
echo "2️⃣  Checking project root..."
if [ ! -f "docker-compose.yml" ]; then
    echo "   ❌ FAIL: Must run from project root (witchcityrope/)"
    echo ""
    echo "💡 Current directory: $(pwd)"
    echo "   Expected: /home/chad/repos/witchcityrope"
    exit 1
fi
echo "   ✅ In project root directory"

# Check 3: psql client installed
echo ""
echo "3️⃣  Checking PostgreSQL client..."
if ! command -v psql &> /dev/null; then
    echo "   ❌ FAIL: psql command not found"
    echo ""
    echo "💡 Install PostgreSQL client:"
    echo "   Ubuntu/Debian: sudo apt install postgresql-client"
    echo "   macOS: brew install postgresql"
    exit 1
fi
echo "   ✅ psql client installed"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - DATABASE RESET
# ============================================

# Configuration
DB_HOST="localhost"
DB_PORT="5434"  # Docker-exposed port (NOT standard 5432)
DB_NAME="witchcityrope_dev"
DB_USER="postgres"
DB_PASSWORD="devpass123"

# Step 1: Drop schemas (this deletes all data)
echo "1️⃣  Dropping database schemas (public + cms)..."
echo "   This will delete ALL tables and data..."

PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -c "DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public; DROP SCHEMA IF EXISTS cms CASCADE;"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Schema drop failed"
    echo ""
    echo "💡 Check database connection:"
    echo "   • Is PostgreSQL container running?"
    echo "   • Port 5434 accessible?"
    echo "   See: .claude/skills/database-reset-dev/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ Schemas dropped and recreated (all data deleted)"
echo ""

# Step 2: Restart API container only (triggers auto-seeding)
echo "2️⃣  Restarting API container..."
echo "   Only restarting api service (not web or database containers)"

# Use compose restart to handle any container name variations
# CRITICAL: Must use -p witchcityrope-dev to match how containers were started
docker-compose -p witchcityrope-dev -f docker-compose.yml -f docker-compose.dev.yml restart api

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not restart API container"
    echo ""
    echo "💡 Try manual restart:"
    echo "   bash .claude/skills/container-restart/execute.sh"
    exit 1
fi
echo "   ✅ API container restarted"
echo ""

# Step 3: Wait for API initialization (initial wait)
echo "3️⃣  Waiting for API initialization..."
echo "   API will run migrations and populate seed data automatically"
echo "   Initial wait for container to start..."

sleep 10

echo "   ✅ Initial wait complete"
echo ""

# Step 4: Check container status
echo "4️⃣  Checking API container status..."
API_STATUS=$(docker ps --format "{{.Status}}" --filter "name=witchcity-api" | head -n 1)
if [ -z "$API_STATUS" ]; then
    echo "   ❌ ERROR: API container not running"
    echo ""
    echo "💡 Check logs: docker logs witchcity-api"
    echo "   See: .claude/skills/database-reset-dev/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ API container running: $API_STATUS"
echo ""

# Step 5: Check for compilation errors (same check as container-restart)
echo "5️⃣  Checking API compilation..."

# Get actual API container name (handles any prefix/suffix variations)
API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -i "api" | head -n 1)

if [ -z "$API_CONTAINER" ]; then
    echo "   ❌ ERROR: Could not find API container"
    exit 1
fi

API_ERRORS=$(docker logs "$API_CONTAINER" --tail 50 2>&1 | grep -E "(Build FAILED|Unhandled exception|System\.Exception|Failed to|Connection refused)" || true)
if [ -n "$API_ERRORS" ]; then
    echo "   ❌ COMPILATION ERRORS IN API:"
    echo "$API_ERRORS"
    echo ""
    echo "💡 FIX SOURCE CODE AND RESTART"
    echo "   See: .claude/skills/database-reset-dev/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ No API compilation errors (container: $API_CONTAINER)"
echo ""

# Step 6: Wait for services to be ready (migrations + seeding)
echo "6️⃣  Waiting for services to be ready (migrations + seeding)..."
echo "   This takes about 10-15 seconds..."
sleep 15
echo "   ✅ Services initializing..."
echo ""

# Step 7: Verify API health endpoint
echo "7️⃣  Verifying API health endpoint..."

# Retry logic for health check (up to 3 attempts)
MAX_RETRIES=3
RETRY_COUNT=0
HEALTH_OK=false

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if curl -f http://localhost:5655/health > /dev/null 2>&1; then
        HEALTH_OK=true
        break
    else
        RETRY_COUNT=$((RETRY_COUNT + 1))
        if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
            echo "   ⏳ Health check attempt $RETRY_COUNT failed, retrying in 5 seconds..."
            sleep 5
        fi
    fi
done

if [ "$HEALTH_OK" = true ]; then
    echo "   ✅ API service healthy (http://localhost:5655)"
else
    echo "   ❌ API health check failed after $MAX_RETRIES attempts"
    echo ""
    echo "💡 Troubleshooting:"
    echo "   • Check logs: docker logs $API_CONTAINER --tail 100"
    echo "   • Check migrations: docker logs $API_CONTAINER | grep -i migration"
    echo "   • See: .claude/skills/database-reset-dev/SKILL.md (Common Issues)"
    exit 1
fi
echo ""

# Step 8: Verify database health
echo "8️⃣  Verifying database health..."
if curl -f http://localhost:5655/health/database > /dev/null 2>&1; then
    echo "   ✅ Database healthy"
else
    echo "   ⚠️  WARNING: Database health check failed"
    echo "   Database may still be initializing - check logs"
fi
echo ""

# Step 9: Verify seed data
echo "9️⃣  Verifying seed data population..."
SEED_COUNT=$(PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -t -c "SELECT COUNT(*) FROM \"Users\" WHERE \"Email\" LIKE '%@witchcityrope.com';" 2>/dev/null || echo "0")

SEED_COUNT_CLEAN=$(echo $SEED_COUNT | xargs)

if [ "$SEED_COUNT_CLEAN" -lt 5 ]; then
    echo "   ⚠️  WARNING: Low seed data count ($SEED_COUNT_CLEAN test users)"
    echo "   Expected at least 5-7 test accounts"
    echo ""
    echo "💡 Check API logs for seeding errors:"
    echo "   docker logs witchcity-api | grep -i seed"
else
    echo "   ✅ Seed data populated ($SEED_COUNT_CLEAN test users)"
fi

echo ""
echo "✅ Database Reset Complete"
echo "=========================="
echo ""
echo "📊 Summary:"
echo "   • Database: $DB_NAME"
echo "   • Schemas: Dropped and recreated"
echo "   • API container: Restarted"
echo "   • Seed data: Populated ($SEED_COUNT_CLEAN test users)"
echo ""
echo "🎯 Ready for:"
echo "   • Development"
echo "   • Testing with fresh data"
echo "   • Clean slate testing"
echo ""
echo "📋 Test Accounts Available:"
echo "   • admin@witchcityrope.com / Test123!"
echo "   • teacher@witchcityrope.com / Test123!"
echo "   • vetted@witchcityrope.com / Test123!"
echo "   • member@witchcityrope.com / Test123!"
echo "   • guest@witchcityrope.com / Test123!"
echo ""

exit 0
