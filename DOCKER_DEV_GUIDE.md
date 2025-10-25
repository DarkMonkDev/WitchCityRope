# WitchCityRope Docker Development Guide

## Overview

WitchCityRope uses Docker containers for all development work to ensure consistency across development environments and match production deployment as closely as possible.

**IMPORTANT**: Local development servers (npm run dev without Docker) are DISABLED to prevent confusion and environment inconsistencies.

## Quick Start

```bash
# Start all services (PostgreSQL, API, React)
./dev.sh

# View logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Stop all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Stop and remove volumes (clean slate)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
```

## Service Access

Once containers are running:

| Service | URL | Purpose |
|---------|-----|---------|
| React App | http://localhost:5173 | Frontend development with HMR |
| API | http://localhost:5655 | Backend API endpoints |
| PostgreSQL | localhost:5434 | Database access (postgres/devpass123) |
| Test Server | http://localhost:8080 | Test execution environment |

**Port 5434 for PostgreSQL**: This dedicated port avoids conflicts with other local PostgreSQL containers (e.g., accounting-automation-db uses 5433).

## Architecture

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   React Web     │──────▶│   .NET API      │──────▶│   PostgreSQL    │
│  localhost:5173 │ HTTP  │  localhost:5655 │      │  localhost:5434 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
         │                                                   │
         │                                                   │
    HMR WebSocket                                    Automatic Migrations
    localhost:24678                                  (on API startup)
```

## Database Migrations

### Understanding EF Core Migrations

WitchCityRope uses Entity Framework Core migrations to manage database schema. All migrations are stored in `/apps/api/Migrations/`.

**Current State**: One initial migration (`InitialMigration`) containing the complete schema (2674 lines, ~132KB).

### Automatic Migration Application

**Migrations are applied automatically when the API container starts** via `DatabaseInitializationService`. You don't need to run manual migration commands for normal development.

#### Verify Automatic Migrations

```bash
# Check API startup logs
docker logs witchcity-api --tail 100 | grep "Database initialization"

# Expected output:
# - "Starting database initialization for environment: Development"
# - "Phase 1: Applying pending migrations"
# - "Successfully applied {Count} migrations" OR "Database is up to date"
# - "Phase 2: Populating seed data"
# - "Database initialization completed successfully"
```

#### Check Migration Status

```bash
# See which migrations are applied
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT * FROM \"__EFMigrationsHistory\";"

# List all database tables
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "\dt"

# Count users (should be 19 if seed data loaded)
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT COUNT(*) FROM \"Users\";"
```

### Creating New Migrations

When you make changes to entity models, create a new migration:

```bash
# 1. Make your changes to entity classes in /apps/api/Models/ or /apps/api/Features/

# 2. Navigate to API project directory
cd apps/api

# 3. Create migration (NEVER use --output-dir flag!)
dotnet ef migrations add DescriptiveNameForYourChanges

# 4. Migration files created in /apps/api/Migrations/:
#    - YYYYMMDDHHMMSS_DescriptiveNameForYourChanges.cs
#    - ApplicationDbContextModelSnapshot.cs (updated)

# 5. Review generated migration
cat Migrations/*_DescriptiveNameForYourChanges.cs
# Verify Up() method contains your expected schema changes

# 6. Test migration with fresh database
cd ../..
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# 7. Verify migration applied
docker logs witchcity-api --tail 100 | grep "migration"
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

### CRITICAL: Migration Best Practices

**✅ DO:**
- Always run `dotnet ef migrations add` from `/apps/api` directory
- Use descriptive migration names: `AddUserProfilePicture`, `UpdateEventCapacityField`
- Review generated migration files before committing
- Test migrations with fresh database (`down -v` then `up -d`)

**❌ DON'T:**
- NEVER use `--output-dir` flag (creates multiple migration directories causing confusion)
- Don't create migrations manually (always use `dotnet ef migrations add`)
- Don't modify existing migrations after they've been applied
- Don't bypass migrations by manually modifying the database schema

### Resetting Database (Fresh Start)

If you need to start with a completely clean database:

```bash
# Option 1: Remove volumes (destroys all data)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Option 2: Drop and recreate database manually
docker exec -it witchcity-postgres psql -U postgres -c "DROP DATABASE IF EXISTS witchcityrope_dev;"
docker exec -it witchcity-postgres psql -U postgres -c "CREATE DATABASE witchcityrope_dev;"
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart api

# Check logs to verify migrations applied
docker logs witchcity-api --tail 100
```

### Troubleshooting Migrations

#### Problem: Empty migration file created

**Symptom**: Migration file has no Up() or Down() content
**Cause**: EF detected no model changes
**Solution**:
```bash
# Check if you actually made model changes
# Delete the empty migration
dotnet ef migrations remove

# Make your entity changes, then try again
dotnet ef migrations add YourMigrationName
```

#### Problem: Migrations in wrong directory

**Symptom**: Migrations appear in `/apps/api/Infrastructure/Data/Migrations/` or `/apps/api/Data/Migrations/`
**Cause**: Used `--output-dir` flag (DON'T DO THIS!)
**Solution**:
```bash
# Delete the incorrect migration directories
rm -rf apps/api/Infrastructure/Data/Migrations
rm -rf apps/api/Data/Migrations

# Keep only /apps/api/Migrations/
# Create migrations correctly without --output-dir flag
cd apps/api
dotnet ef migrations add YourMigrationName
```

#### Problem: "Migration already applied" error

**Symptom**: Migration exists in `__EFMigrationsHistory` but not in code
**Solution**:
```bash
# Check migration history
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "SELECT * FROM \"__EFMigrationsHistory\";"

# Remove orphaned migration record
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev -c "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = 'YourMigrationId';"
```

#### Problem: Schema and migrations out of sync

**Symptom**: Database schema doesn't match migration files
**Solution**:
```bash
# Generate SQL script to see what EF thinks needs to change
cd apps/api
dotnet ef migrations script --output current-state.sql
cat current-state.sql

# If needed, create sync migration
dotnet ef migrations add SyncSchemaChanges
```

## Development Workflow

### 1. Starting Development

```bash
# Fresh start with latest code
git pull origin main
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Wait for services to be ready (~30 seconds)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml ps

# Check logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f
```

### 2. Making Code Changes

**React Changes (Hot Reload)**:
- Edit files in `/apps/web/src/`
- Changes reflected automatically via HMR
- Browser refreshes instantly

**API Changes (Auto Restart)**:
- Edit files in `/apps/api/`
- `dotnet watch` restarts API within 3-5 seconds
- Check logs: `docker logs witchcity-api -f`

**Database Schema Changes**:
1. Edit entity models in `/apps/api/Models/` or `/apps/api/Features/`
2. Create migration: `cd apps/api && dotnet ef migrations add YourChange`
3. Restart API to apply: `docker-compose restart api`
4. Verify: `docker logs witchcity-api | grep migration`

### 3. Viewing Logs

```bash
# All services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Specific service
docker logs witchcity-api -f
docker logs witchcity-web -f
docker logs witchcity-postgres -f

# Filter logs
docker logs witchcity-api --tail 100 | grep error
docker logs witchcity-api --since 5m
```

### 4. Database Access

```bash
# Connect to PostgreSQL
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev

# Common queries
\dt                                  # List all tables
\d "Users"                          # Describe Users table
SELECT * FROM "Users" LIMIT 5;      # Query users
SELECT COUNT(*) FROM "Events";      # Count events
\q                                  # Quit
```

### 5. Running Tests

```bash
# E2E tests (Playwright)
npm run test:e2e:playwright

# React unit tests
npm run test

# API tests
dotnet test apps/api/WitchCityRope.Api.Tests/
```

## Environment Variables

Development environment variables are configured in `docker-compose.dev.yml`:

### Key Configuration

```yaml
# API Service
ASPNETCORE_ENVIRONMENT: Development
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=devpass123"
Jwt__SecretKey: dev-jwt-secret-for-local-testing
Jwt__ExpirationMinutes: 480  # 8 hours for development

# React Service
NODE_ENV: development
VITE_API_BASE_URL: http://localhost:5655
CHOKIDAR_USEPOLLING: "true"  # Required for Docker HMR
VITE_HMR_PORT: 24678
```

## Docker Commands Reference

### Service Management

```bash
# Start all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Start specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d api

# Stop all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Restart specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart api

# View service status
docker-compose -f docker-compose.yml -f docker-compose.dev.yml ps

# View resource usage
docker stats
```

### Building and Rebuilding

```bash
# Rebuild all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build

# Rebuild specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build api

# Rebuild without cache (clean build)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache

# Build and start
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

### Cleanup

```bash
# Remove stopped containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml rm

# Remove volumes (destroys data!)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v

# Remove images
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down --rmi all

# Full cleanup (nuclear option)
docker system prune -a --volumes
```

### Debugging

```bash
# Execute command in running container
docker exec -it witchcity-api bash
docker exec -it witchcity-postgres psql -U postgres

# View container details
docker inspect witchcity-api

# View container logs
docker logs witchcity-api --tail 100 -f

# View port mappings
docker port witchcity-api
docker port witchcity-web
```

## Test Accounts

Once seed data is loaded, these test accounts are available:

| Email | Password | Role |
|-------|----------|------|
| admin@witchcityrope.com | Test123! | Admin |
| teacher@witchcityrope.com | Test123! | Teacher |
| vetted@witchcityrope.com | Test123! | Vetted Member |
| member@witchcityrope.com | Test123! | General Member |
| guest@witchcityrope.com | Test123! | Guest/Attendee |

## Common Issues

### Port Conflicts

**Symptom**: `Error: Port 5434 already in use`
**Solution**:
```bash
# Find process using the port
sudo netstat -tlnp | grep 5434

# Kill the process or change port in docker-compose.dev.yml
```

### Database Connection Errors

**Symptom**: API can't connect to PostgreSQL
**Solution**:
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Check database exists
docker exec -it witchcity-postgres psql -U postgres -l

# Verify connection string in docker-compose.dev.yml
# Should be: Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=devpass123
```

### Hot Reload Not Working

**Symptom**: React changes don't reflect in browser
**Solution**:
```bash
# Verify HMR WebSocket is accessible
# Browser DevTools → Network → WS tab
# Should see connection to ws://localhost:24678

# Check CHOKIDAR_USEPOLLING is set to "true"
docker-compose -f docker-compose.yml -f docker-compose.dev.yml config | grep CHOKIDAR

# Restart web service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
```

### API Changes Not Reflecting

**Symptom**: API code changes don't take effect
**Solution**:
```bash
# Check dotnet watch is running
docker logs witchcity-api --tail 50 | grep watch

# Verify source code is mounted
docker inspect witchcity-api | grep Mounts

# Force restart
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart api
```

## Performance Tips

1. **Use BuildKit**: Set environment variables for faster builds
   ```bash
   export DOCKER_BUILDKIT=1
   export COMPOSE_DOCKER_CLI_BUILD=1
   ```

2. **Volume Caching**: Docker Compose is configured to use `:cached` for better performance

3. **Resource Limits**: Adjust Docker Desktop memory allocation (8GB+ recommended)

4. **Prune Regularly**: Clean up unused images/containers weekly
   ```bash
   docker system prune -a
   ```

## See Also

- [CLAUDE.md](./CLAUDE.md) - Project configuration and standards
- [Staging Deployment Guide](./docs/functional-areas/deployment/staging-deployment-guide.md) - Deploying to staging
- [ApplicationDbContext.cs](./apps/api/Data/ApplicationDbContext.cs) - Database configuration and migration documentation
- [docker-compose.yml](./docker-compose.yml) - Base service configuration
- [docker-compose.dev.yml](./docker-compose.dev.yml) - Development overrides
