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

## How to Use This Skill

**Executable Script**: `execute.sh`

```bash
# From project root - with confirmation prompt
bash .claude/skills/database-reset-staging/execute.sh

# Skip confirmation prompt (for automation)
SKIP_CONFIRMATION=true bash .claude/skills/database-reset-staging/execute.sh
```

**What the script does**:
1. Shows pre-flight information (purpose, when/when NOT to use, destructive operation warnings)
2. Requires confirmation before proceeding (skippable with env var)
3. Validates prerequisites:
   - SSH key accessible
   - PostgreSQL client installed (psql)
   - Server connectivity
4. Retrieves database credentials from staging server
5. Stops staging containers
6. Drops all database schemas (public + cms)
7. Recreates public schema
8. Starts containers (migrations run automatically)
9. Waits for database initialization
10. Verifies schema rebuild
11. Runs health check
12. Reports summary

**Script includes CRITICAL safety warnings** - this is a DESTRUCTIVE operation that cannot be undone.

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

**Then**: Use `restart-dev-containers` skill to restart staging containers

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
2. Check staging containers: Use `restart-dev-containers` skill
3. Verify database port is open (25060)

### Issue: Migrations fail after reset

**Cause**: Old migration state or code/DB mismatch

**Solution**:
1. Check API logs: `restart-dev-containers` skill
2. Ensure latest code deployed: `staging-deploy` skill
3. Verify no lingering tables: Run query to list all tables

### Issue: Seed data not populating

**Cause**: Seed condition not met

**Solution**:
- API only seeds if `appsettings.Staging.json` has `SeedData: true`
- Check environment configuration on server
- Manual trigger: Restart API container with `restart-dev-containers` skill

---

## Integration with Process

**Typical workflow:**
1. Make schema changes locally
2. Test migrations locally
3. Deploy code: Use `staging-deploy` skill
4. Reset database: Use THIS skill
5. Verify: Use `restart-dev-containers` skill to check logs

---

## Version History

- **2025-11-05**: Created as automation wrapper for staging database reset
- Extracted from: `docs/functional-areas/deployment/staging-deployment-guide.md`
- Complements: `docs/guides-setup/database-setup.md`

---

**Remember**: This skill is for staging only. Never use on production. Always use `staging-deploy` skill first to ensure latest code is deployed.
