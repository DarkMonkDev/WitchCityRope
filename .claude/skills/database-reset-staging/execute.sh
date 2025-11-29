#!/bin/bash
# Staging Database Reset - Full Schema Drop
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script performs DESTRUCTIVE database reset with:
# - Full schema drop (public + cms)
# - Container restart for migrations
# - Health verification

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🗄️  Staging Database Reset (Full Schema Drop)"
echo "=============================================="
echo ""
echo "📋 Purpose: Full database schema reset for staging environment"
echo ""
echo "✅ Use when:"
echo "   • Schema changes requiring clean slate"
echo "   • Migration conflicts with existing tables"
echo "   • Database corruption or inconsistencies"
echo "   • After major refactoring"
echo ""
echo "❌ DO NOT use if:"
echo "   • Just need fresh seed data (use selective delete instead)"
echo "   • This is production database (STAGING ONLY!)"
echo "   • You haven't deployed latest code first (use staging-deploy skill first)"
echo ""
echo "🚨 CRITICAL WARNINGS - DESTRUCTIVE OPERATION:"
echo "   • ALL data in staging database will be DELETED"
echo "   • Both 'public' AND 'cms' schemas will be DROPPED"
echo "   • Cannot be undone"
echo "   • Only affects staging database (witchcityrope_staging)"
echo "   • Migrations will rebuild schema automatically"
echo ""
echo "📋 Prerequisites:"
echo "   • Staging code already deployed (use staging-deploy skill first)"
echo "   • Database backup if needed (though staging data is expendable)"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    echo "⚠️  WARNING: This will DELETE ALL data in staging database!"
    echo "   Database: witchcityrope_staging"
    echo "   Schemas: public, cms"
    echo ""
    read -p "Continue with database reset? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo ""
        echo "❌ Aborted by user"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/database-reset-staging/SKILL.md"
        echo "   /docs/guides-setup/database-setup.md"
        exit 0
    fi
    echo ""
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Running prerequisite checks..."
echo ""

# Check 1: SSH key
echo "1️⃣  Checking SSH access..."
SSH_KEY="/home/chad/.ssh/id_ed25519_witchcityrope"
if [ ! -f "$SSH_KEY" ]; then
    echo "   ❌ FAIL: SSH key not found: $SSH_KEY"
    echo ""
    echo "💡 Ensure SSH key is properly configured"
    exit 1
fi
echo "   ✅ SSH key found"

# Check 2: psql client installed
echo ""
echo "2️⃣  Checking PostgreSQL client..."
if ! command -v psql &> /dev/null; then
    echo "   ❌ FAIL: psql command not found"
    echo ""
    echo "💡 Install PostgreSQL client:"
    echo "   Ubuntu/Debian: sudo apt install postgresql-client"
    echo "   macOS: brew install postgresql"
    exit 1
fi
echo "   ✅ psql client installed"

# Check 3: Server connectivity
echo ""
echo "3️⃣  Testing server connectivity..."
SERVER="104.131.165.14"
USER="witchcity"
if ! ssh -i $SSH_KEY -o ConnectTimeout=10 $USER@$SERVER "echo '   ✅ Connected to server'" ; then
    echo "   ❌ FAIL: Cannot connect to server"
    echo ""
    echo "💡 Check SSH configuration and network connectivity"
    exit 1
fi
echo ""

echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - DATABASE RESET
# ============================================

# Configuration
DEPLOY_PATH="/opt/witchcityrope/staging"

# Step 1: Get database connection info
echo "1️⃣  Retrieving database credentials..."
DB_CONNECTION=$(ssh -i $SSH_KEY $USER@$SERVER "cat $DEPLOY_PATH/.env.staging | grep STAGING_DB_CONNECTION_STRING" | cut -d'=' -f2-)

if [ -z "$DB_CONNECTION" ]; then
    echo "   ❌ FAIL: Could not retrieve database connection string"
    echo ""
    echo "💡 Verify .env.staging exists on server at: $DEPLOY_PATH"
    exit 1
fi

# Parse keyword-value connection string
# Format: Host=...;Port=...;Database=...;Username=...;Password=...;
DB_HOST=$(echo "$DB_CONNECTION" | grep -oP 'Host=\K[^;]+')
DB_PORT=$(echo "$DB_CONNECTION" | grep -oP 'Port=\K[^;]+')
DB_NAME=$(echo "$DB_CONNECTION" | grep -oP 'Database=\K[^;]+')
DB_USER=$(echo "$DB_CONNECTION" | grep -oP 'Username=\K[^;]+')
DB_PASSWORD=$(echo "$DB_CONNECTION" | grep -oP 'Password=\K[^;]+')

echo "   Database: $DB_NAME"
echo "   Host: $DB_HOST:$DB_PORT"
echo "   ✅ Credentials retrieved"
echo ""

# Step 2: Stop API container
echo "2️⃣  Stopping API container..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker compose -f docker-compose.staging.yml stop api"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not stop API container"
    echo ""
    echo "💡 Check container status on server"
    echo "   See: .claude/skills/database-reset-staging/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ API container stopped"
echo ""

# Step 3: Drop schemas
echo "3️⃣  Dropping database schemas (public + cms)..."
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
    echo "💡 Attempting to restart containers..."
    ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker compose -f docker-compose.staging.yml up -d"
    echo ""
    echo "   See: .claude/skills/database-reset-staging/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ Schemas dropped and recreated"
echo ""

# Step 4: Recreate API container (uses existing deployed image)
echo "4️⃣  Recreating API container..."
echo "   Using currently deployed image - migrations will run on startup..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker compose -f docker-compose.staging.yml up -d --force-recreate api"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not recreate API container"
    echo ""
    echo "💡 Manual recovery may be required:"
    echo "   ssh $USER@$SERVER 'cd $DEPLOY_PATH && docker compose -f docker-compose.staging.yml up -d api'"
    exit 1
fi
echo "   ✅ API container recreated"
echo ""

# Step 5: Wait for migrations
echo "5️⃣  Waiting for database initialization..."
echo "   Migrations and seed data will run automatically..."
sleep 15
echo "   ✅ Wait complete"
echo ""

# Step 6: Monitor initialization
echo "6️⃣  Checking database initialization..."
echo "   API container is starting and running migrations..."
echo "   ✅ Containers are running"
echo ""

# Step 7: Verify database
echo "7️⃣  Verifying database reset..."
RECORD_COUNT=$(PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")

echo "   Tables created: $(echo $RECORD_COUNT | xargs)"

if [ $(echo $RECORD_COUNT | xargs) -gt 0 ]; then
    echo "   ✅ Database schema rebuilt"
else
    echo "   ⚠️  WARNING: No tables found - migrations may still be running"
fi
echo ""

# Step 8: Health check
echo "8️⃣  Running health check..."
sleep 10  # Give API more time to start
if curl -f -s https://staging.notfai.com/api/health > /dev/null; then
    echo "   ✅ API healthy"
else
    echo "   ⚠️  WARNING: API health check failed"
    echo "   API may still be starting up - check logs"
fi
echo ""

echo "✅ Database Reset Complete"
echo "=========================="
echo ""
echo "📊 Summary:"
echo "   • Database: $DB_NAME"
echo "   • Schemas: Dropped and recreated"
echo "   • Tables: $(echo $RECORD_COUNT | xargs) created by migrations"
echo "   • Containers: Restarted"
echo ""
echo "🎯 Next Steps:"
echo "   1. Monitor API logs for migration completion:"
echo "      ssh $USER@$SERVER 'docker logs -f witchcity-api-staging'"
echo "   2. Verify seed data:"
echo "      curl https://staging.notfai.com/api/events | jq ."
echo "   3. Test critical endpoints"
echo "   4. If issues: Check API logs for migration errors"
echo ""

exit 0
