# Database Setup Guide

This guide covers PostgreSQL database configuration and setup for the WitchCityRope project.

## Prerequisites

- Docker and Docker Compose installed
- PostgreSQL 16 (if running locally without Docker)
- Basic understanding of connection strings

## Database Configuration

### PostgreSQL Authentication Details

- **Username**: `postgres`
- **Password**: `WitchCity2024!`
- **Database Name**: `witchcityrope_db`

### Docker Compose PostgreSQL Configuration

When using Docker Compose (RECOMMENDED):

- **Container Name**: `witchcityrope-db`
- **External Port**: `5433` (mapped from internal 5432)
- **Internal Port**: `5432`
- **Volume**: `witchcityrope_postgres_dev_data`

### Connection Strings

Choose the appropriate connection string based on your setup:

#### Docker Compose (Internal - for container-to-container communication)
```
Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

#### Docker Compose (External - for host machine access)
```
Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

#### Local PostgreSQL (without Docker)
```
Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

## Database Setup Steps

### 1. Using Docker Compose (Recommended)

The database is automatically set up when you start the development environment:

```bash
# Start all services including database
./dev.sh
```

**For container management**: Use `restart-dev-containers` skill for starting, stopping, and inspecting containers.

### 2. Verify Database Connection

```bash
# Test database connection
docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT 1;"

# Check applied migrations
docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' OR table_schema = 'auth';"
```

### 3. Run Database Migrations

```bash
# Run migrations from web container
docker-compose exec web dotnet ef database update
```

### 4. Access PostgreSQL Shell

```bash
# Access database shell
docker exec -it witchcityrope-db psql -U postgres

# Connect to specific database
\c witchcityrope_db

# List all tables
\dt

# Exit
\q
```

## Database Operations

### Backup Database

```bash
# Create backup
docker exec witchcityrope-db pg_dump -U postgres witchcityrope > backup.sql

# Create timestamped backup
docker exec witchcityrope-db pg_dump -U postgres witchcityrope > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore Database

```bash
# Restore from backup
docker exec -i witchcityrope-db psql -U postgres witchcityrope < backup.sql
```

### Reset Database

```bash
# Stop containers
docker-compose down

# Remove volume (WARNING: This deletes all data!)
docker volume rm witchcityrope_postgres_dev_data

# Restart containers (database will be recreated)
./dev.sh
```

## Integration Testing with PostgreSQL

Integration tests use PostgreSQL Testcontainers for isolation:

### Key Points

1. **Real PostgreSQL via Testcontainers** - No more in-memory database
2. **All DateTime must be UTC** - PostgreSQL enforces this strictly
3. **Test data must be unique** - Use GUIDs for all names/emails
4. **Health Check System** - Validates database readiness before tests

### Running Integration Tests

```bash
# FIRST: Run health checks to verify containers are ready
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# ONLY IF health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

### Common PostgreSQL Issues

#### DateTime UTC Requirements
```csharp
// ❌ WRONG - Kind is Unspecified
new DateTime(1990, 1, 1)

// ✅ CORRECT - Kind is UTC
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

#### Unique Constraint Violations
```csharp
// ❌ WRONG - Same name used by multiple tests
SceneName.Create("TestUser")

// ✅ CORRECT - Unique name per test
SceneName.Create($"TestUser_{Guid.NewGuid():N}")
```

## Troubleshooting

### Port Already in Use

If you get "port 5433 already in use" error:

```bash
# Find what's using the port
sudo lsof -i :5433

# Stop local PostgreSQL if running
sudo systemctl stop postgresql
```

### Container Won't Start

Use `restart-dev-containers` skill to:
- Check container logs
- Restart containers
- Rebuild if needed

Or manually restart with `./dev.sh`

### Connection Refused

1. Ensure Docker is running: `docker ps`
2. Check container status: `docker ps -a | grep postgres`
3. Verify port mapping: `docker port witchcityrope-db`
4. Check firewall isn't blocking port 5433

### Migration Failures

```bash
# View detailed migration errors
docker-compose exec web dotnet ef migrations list

# Remove last migration if needed
docker-compose exec web dotnet ef migrations remove

# Generate new migration
./scripts/generate-migration.sh MigrationName
```

## Security Considerations

1. **Never commit passwords** - Use environment variables in production
2. **Change default password** - The provided password is for development only
3. **Restrict network access** - In production, database should not be externally accessible
4. **Use SSL/TLS** - Enable encrypted connections in production
5. **Regular backups** - Implement automated backup strategy

## Related Documentation

- [Docker Development Guide](./docker-development.md) - Complete Docker setup
- [Development Standards](../standards-processes/development-standards.md) - Coding standards
- [Architecture Guide](../../ARCHITECTURE.md) - System architecture
---

# Staging Database Management

This section covers database operations for the staging environment on DigitalOcean.

## Staging Database Configuration

- **Environment**: DigitalOcean Managed PostgreSQL
- **Database Name**: `witchcityrope_staging`
- **Schemas**: `public` (application tables), `cms` (CMS tables)
- **Credentials Location**: `/opt/witchcityrope/staging/.env.staging` on server
- **Connection Variable**: `ConnectionStrings__DefaultConnection`

### Getting Staging Credentials

Credentials are stored on the staging server in environment variables.

**To retrieve connection string:**
```bash
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
cat /opt/witchcityrope/staging/.env.staging | grep ConnectionStrings__DefaultConnection
```

**Connection string format:**
```
Host=HOST;Port=PORT;Database=witchcityrope_staging;Username=USERNAME;Password=PASSWORD;SSL Mode=Require
```

## When to Reset Staging Database

### Option 1: Full Schema Reset (DROP SCHEMA)

**Use when:**
- Schema changes or migrations need clean slate
- Migration conflicts with existing tables
- "Relation already exists" errors
- After major refactoring

**DO NOT use when:**
- Just need fresh seed data (use Option 2)

**How:**
Use the `database-reset-staging` skill - automates full process:
```bash
# Run from project root
# Skill located at: /.claude/skills/database-reset-staging.md
```

**What it does:**
1. Stops staging containers
2. Drops BOTH `public` AND `cms` schemas (CASCADE)
3. Recreates empty schemas
4. Restarts containers
5. Migrations run automatically
6. Seed data populates automatically

**⚠️ CRITICAL**: Both schemas must be dropped together. If you only drop `public`, leftover CMS tables will cause migration failures with "relation already exists" errors.

### Option 2: Selective Data Reseed (DELETE FROM)

**Use when:**
- Schema is fine, just need fresh data
- Testing specific seed scenarios
- Updating seed data content

**Manual procedure:**

1. **Connect to database:**
```bash
# Get connection string first (see above)
# Then connect with psql:
PGPASSWORD='PASSWORD' psql -h HOST -p PORT -U USERNAME -d witchcityrope_staging
```

2. **Delete data (preserves schema):**
```sql
-- Delete in correct order to respect foreign keys
DELETE FROM "EventParticipations";
DELETE FROM "Sessions";
DELETE FROM "TicketTypes";
DELETE FROM "VolunteerPositions";
DELETE FROM "Events";
-- Add other tables as needed

-- Verify deletion
SELECT COUNT(*) FROM "Events";  -- Should return 0
```

3. **Restart API to trigger reseeding:**
```bash
# Use restart-dev-containers skill or manually:
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
cd /opt/witchcityrope/staging
docker-compose -f docker-compose.staging.yml restart api
```

4. **Monitor seed population:**
Use `restart-dev-containers` skill to watch logs and verify seed data population.

## Decision Tree

```
Need to reset staging database?
│
├─ Schema changes or migration issues?
│  └─ YES → Use database-reset-staging skill (Full DROP SCHEMA)
│
└─ Just need fresh data?
   └─ YES → Use selective DELETE FROM procedure
```

## Database Verification

After any reset operation:

**Check schema exists:**
```sql
SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';
-- Should return number > 0 after migrations
```

**Check seed data:**
```sql
SELECT COUNT(*) FROM "Users";
SELECT COUNT(*) FROM "Events";
-- Should match expected seed counts
```

**Test API health:**
```bash
curl https://staging.notfai.com/api/health | jq .
# Should return: {"status": "Healthy", "databaseConnected": true}
```

## Troubleshooting

### Issue: "Relation already exists" error during migrations

**Cause**: CMS schema tables left behind after partial schema drop

**Solution**: 
- Use `database-reset-staging` skill (drops BOTH schemas)
- Never manually drop only `public` schema

### Issue: Seed data not populating after reset

**Cause**: API seed conditions not met

**Solution**:
1. Verify API has `SeedData: true` in staging configuration
2. Check API logs for seed execution
3. Use `restart-dev-containers` skill to view detailed logs

### Issue: Cannot connect to database

**Cause**: Firewall, credentials, or SSL issue

**Solution**:
1. Verify credentials from `.env.staging` are correct
2. Ensure SSL mode is set: `SSL Mode=Require`
3. Check database is accessible from your network

### Issue: Migrations fail after schema drop

**Cause**: Code/database version mismatch

**Solution**:
1. Ensure latest code deployed: use `staging-deploy` skill first
2. Check API logs for specific migration error
3. Verify migration files are in correct order

## Related Skills

- **staging-deploy** - Deploy latest code to staging
- **database-reset-staging** - Full schema reset automation
- **restart-dev-containers** - Inspect logs and restart containers

## Security Notes

**⚠️ NEVER commit database credentials to git**

- Credentials live only in server `.env.staging` file
- Use SSH to retrieve when needed
- Connection examples in this guide use placeholders only

**⚠️ Staging only - never use these procedures on production**

## Related Documentation

- [Staging Deployment Guide](../functional-areas/deployment/staging-deployment-guide.md) - Deployment process
- [Secrets Management Guide](./secrets-management-guide-2025-10-24.md) - Credential management
