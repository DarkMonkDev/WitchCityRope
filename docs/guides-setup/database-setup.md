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

# Or manually:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 2. Verify Database Connection

```bash
# Check if PostgreSQL container is running
docker ps | grep witchcity-postgres

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

```bash
# Check container logs
docker logs witchcityrope-db

# Remove old containers and volumes
docker-compose down -v

# Restart fresh
./dev.sh
```

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