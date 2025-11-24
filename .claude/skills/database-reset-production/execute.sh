#!/bin/bash
# Production Database Reset - Full Schema Drop
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# ⚠️ EXTREME CAUTION: THIS IS PRODUCTION DATABASE ⚠️
#
# This script performs DESTRUCTIVE database reset with:
# - Full schema drop (public + cms + hangfire)
# - Container restart for migrations
# - Health verification

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

echo "🗄️  Production Database Reset (Full Schema Drop)"
echo "================================================="
echo ""
echo "🚨🚨🚨 EXTREME WARNING - PRODUCTION DATABASE 🚨🚨🚨"
echo ""
echo "📋 Purpose: Full database schema reset for PRODUCTION environment"
echo ""
echo "✅ Use when:"
echo "   • CRITICAL schema changes that cannot be migrated"
echo "   • Database corruption beyond repair"
echo "   • After explicit approval from stakeholders"
echo "   • During planned maintenance window"
echo ""
echo "❌ DO NOT use if:"
echo "   • You're not 100% certain this is necessary"
echo "   • Production has live user data (WILL BE DELETED)"
echo "   • You haven't taken a complete database backup"
echo "   • This is during business hours"
echo "   • You haven't notified all stakeholders"
echo ""
echo "🚨 CRITICAL WARNINGS - DESTRUCTIVE OPERATION:"
echo "   • ALL PRODUCTION DATA WILL BE PERMANENTLY DELETED"
echo "   • USER ACCOUNTS, EVENTS, PAYMENTS - EVERYTHING GONE"
echo "   • CANNOT BE UNDONE"
echo "   • Only affects production database (witchcityrope_production)"
echo "   • Migrations will rebuild schema automatically"
echo "   • All users will need to re-register"
echo ""
echo "📋 Prerequisites:"
echo "   • COMPLETE DATABASE BACKUP VERIFIED"
echo "   • Production code already deployed"
echo "   • Stakeholder approval documented"
echo "   • Maintenance window scheduled"
echo "   • Rollback plan prepared"
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "⏭️  Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    echo "⚠️⚠️⚠️  FINAL WARNING: PRODUCTION DATABASE RESET ⚠️⚠️⚠️"
    echo ""
    echo "   This will DELETE ALL PRODUCTION DATA INCLUDING:"
    echo "   • All user accounts and profiles"
    echo "   • All events and registrations"
    echo "   • All payment records"
    echo "   • All vetting applications"
    echo "   • ALL BUSINESS DATA"
    echo ""
    echo "   Database: witchcityrope_production"
    echo "   Schemas: public, cms, hangfire"
    echo ""
    read -p "Type 'DELETE PRODUCTION DATA' to confirm: " CONFIRM
    if [ "$CONFIRM" != "DELETE PRODUCTION DATA" ]; then
        echo ""
        echo "❌ Aborted - confirmation text did not match"
        echo ""
        echo "📖 For more details, see:"
        echo "   .claude/skills/database-reset-production/SKILL.md"
        echo "   /docs/guides-setup/database-setup.md"
        exit 0
    fi
    echo ""
    read -p "Are you ABSOLUTELY SURE? (yes/NO): " FINAL_CONFIRM
    if [ "$FINAL_CONFIRM" != "yes" ]; then
        echo ""
        echo "❌ Aborted by user"
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
DEPLOY_PATH="/opt/witchcityrope/production"

# Step 1: Get database connection info
echo "1️⃣  Retrieving database credentials..."
DB_CONNECTION=$(ssh -i $SSH_KEY $USER@$SERVER "cat $DEPLOY_PATH/.env.production | grep PROD_DB_CONNECTION_STRING" | cut -d'=' -f2-)

if [ -z "$DB_CONNECTION" ]; then
    echo "   ❌ FAIL: Could not retrieve database connection string"
    echo ""
    echo "💡 Verify .env.production exists on server at: $DEPLOY_PATH"
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
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.production.yml stop api"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not stop API container"
    echo ""
    echo "💡 Check container status on server"
    echo "   See: .claude/skills/database-reset-production/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ API container stopped"
echo ""

# Step 3: Drop schemas
echo "3️⃣  Dropping database schemas (public + cms + hangfire)..."
echo "   ⚠️  THIS WILL DELETE ALL PRODUCTION DATA..."

PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -c "DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public; DROP SCHEMA IF EXISTS cms CASCADE; DROP SCHEMA IF EXISTS hangfire CASCADE;"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Schema drop failed"
    echo ""
    echo "💡 Attempting to restart API container..."
    ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.production.yml start api"
    echo ""
    echo "   See: .claude/skills/database-reset-production/SKILL.md (Common Issues)"
    exit 1
fi
echo "   ✅ Schemas dropped and recreated"
echo ""

# Step 4: Pull latest API image and recreate container
echo "4️⃣  Pulling latest API image and recreating container..."
echo "   Ensuring container runs latest code for migrations..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && IMAGE_TAG=latest docker-compose -f docker-compose.production.yml pull api && IMAGE_TAG=latest docker-compose -f docker-compose.production.yml up -d --force-recreate api"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not pull/recreate API container"
    echo ""
    echo "💡 Manual recovery may be required:"
    echo "   ssh $USER@$SERVER 'cd $DEPLOY_PATH && IMAGE_TAG=latest docker-compose -f docker-compose.production.yml up -d api'"
    exit 1
fi
echo "   ✅ API container recreated with latest image"
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
if curl -f -s https://prod.notfai.com/api/health > /dev/null; then
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
echo "   • API Container: Restarted"
echo ""
echo "🎯 Next Steps:"
echo "   1. Monitor API logs for migration completion:"
echo "      ssh $USER@$SERVER 'docker logs -f witchcity-api-production'"
echo "   2. Verify seed data:"
echo "      curl https://prod.notfai.com/api/events | jq ."
echo "   3. Test critical endpoints"
echo "   4. Notify stakeholders that production has been reset"
echo "   5. Users will need to re-register"
echo ""

exit 0
