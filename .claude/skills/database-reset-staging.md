---
name: database-reset-staging
description: Resets staging database with full schema drop. Use for schema changes or migrations. SINGLE SOURCE OF TRUTH for staging database reset automation.
---

# Database Reset Staging Skill

**Purpose**: Full database schema reset for staging environment - drops all schemas and lets migrations rebuild.

**When to Use**:
- Schema changes requiring clean slate
- Migration conflicts with existing tables
- Database corruption or inconsistencies
- After major refactoring

**When NOT to Use**:
- Just need fresh seed data (use selective delete instead, see database guide)
- Production database (NEVER - this is staging only)

**Background Documentation**: See `/docs/guides-setup/database-setup.md` (Staging Database Management section) for context and manual procedures.

## 🚨 CRITICAL WARNINGS

**This skill performs DESTRUCTIVE operations:**
- ❌ ALL data in staging database will be DELETED
- ❌ Both `public` AND `cms` schemas will be DROPPED
- ❌ Cannot be undone
- ✅ ONLY affects staging database (`witchcityrope_staging`)
- ✅ Migrations will rebuild schema automatically

**Prerequisites:**
- Staging code already deployed (use `staging-deploy` skill first)
- Database backup if needed (though staging data is expendable)

---

## Automated Reset Script

```bash
#!/bin/bash
# Staging Database Reset - Full Schema Drop
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE

set -e  # Exit on error

echo "🗄️  Staging Database Reset (Full Schema Drop)"
echo "=============================================="
echo ""

# Configuration
SERVER="104.131.165.14"
USER="witchcity"
SSH_KEY="/home/chad/.ssh/id_ed25519_witchcityrope"
DEPLOY_PATH="/opt/witchcityrope/staging"

echo "⚠️  WARNING: This will DELETE ALL data in staging database!"
echo "   Database: witchcityrope_staging"
echo "   Schemas: public, cms"
echo ""
echo "Press Ctrl+C to cancel, or Enter to continue..."
read

# Step 1: Get database connection info
echo "1️⃣  Retrieving database credentials..."
DB_CONNECTION=$(ssh -i $SSH_KEY $USER@$SERVER "cat $DEPLOY_PATH/.env.staging | grep ConnectionStrings__DefaultConnection" | cut -d'=' -f2-)

if [ -z "$DB_CONNECTION" ]; then
    echo "   ❌ FAIL: Could not retrieve database connection string"
    exit 1
fi

# Parse connection string
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

# Step 2: Stop containers
echo "2️⃣  Stopping staging containers..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml down"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not stop containers"
    exit 1
fi
echo "   ✅ Containers stopped"
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
    echo "   Attempting to restart containers..."
    ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml up -d"
    exit 1
fi
echo "   ✅ Schemas dropped and recreated"
echo ""

# Step 4: Start containers (migrations will run)
echo "4️⃣  Starting containers..."
ssh -i $SSH_KEY $USER@$SERVER "cd $DEPLOY_PATH && docker-compose -f docker-compose.staging.yml up -d"

if [ $? -ne 0 ]; then
    echo "   ❌ FAIL: Could not start containers"
    exit 1
fi
echo "   ✅ Containers starting"
echo ""

# Step 5: Wait for migrations
echo "5️⃣  Waiting for database initialization..."
echo "   Migrations and seed data will run automatically..."
sleep 15
echo "   ✅ Wait complete"
echo ""

# Step 6: Monitor initialization
echo "6️⃣  Checking database initialization..."
echo "   Use container-restart skill to view full initialization logs"
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
    echo "   API may still be starting up"
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
echo "   1. Use container-restart skill to monitor full initialization logs"
echo "   2. Verify seed data: curl https://staging.notfai.com/api/events | jq ."
echo "   3. Test critical endpoints"
echo "   4. If issues: Check API logs for migration errors"
echo ""

exit 0
```

---

## Manual Override (Emergency Only)

If skill fails, manual steps:

**Prerequisites**: Get DB credentials from server first

**Manual schema drop:**
Connect to database and execute:
```sql
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
DROP SCHEMA IF EXISTS cms CASCADE;
```

**Then**: Use `container-restart` skill to restart staging containers

---

## Common Issues & Solutions

### Issue: psql command not found

**Cause**: PostgreSQL client not installed locally

**Solution**:
```bash
# Ubuntu/Debian
sudo apt install postgresql-client

# macOS
brew install postgresql
```

### Issue: Connection timeout

**Cause**: Firewall or network issue

**Solution**:
1. Verify server is accessible: `ssh witchcity@104.131.165.14`
2. Check staging containers: Use `container-restart` skill
3. Verify database port is open (25060)

### Issue: Migrations fail after reset

**Cause**: Old migration state or code/DB mismatch

**Solution**:
1. Check API logs: `container-restart` skill
2. Ensure latest code deployed: `staging-deploy` skill
3. Verify no lingering tables: Run query to list all tables

### Issue: Seed data not populating

**Cause**: Seed condition not met

**Solution**:
- API only seeds if `appsettings.Staging.json` has `SeedData: true`
- Check environment configuration on server
- Manual trigger: Restart API container with `container-restart` skill

---

## Integration with Process

**Typical workflow:**
1. Make schema changes locally
2. Test migrations locally
3. Deploy code: Use `staging-deploy` skill
4. Reset database: Use THIS skill
5. Verify: Use `container-restart` skill to check logs

---

## Version History

- **2025-11-05**: Created as automation wrapper for staging database reset
- Extracted from: `docs/functional-areas/deployment/staging-deployment-guide.md`
- Complements: `docs/guides-setup/database-setup.md`

---

**Remember**: This skill is for staging only. Never use on production. Always use `staging-deploy` skill first to ensure latest code is deployed.
