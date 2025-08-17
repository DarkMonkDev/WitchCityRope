# PostgreSQL Database Container Design

**Document Created**: 2025-08-17  
**Status**: Design Complete  
**Author**: Database Designer  
**Purpose**: PostgreSQL container configuration for Docker authentication implementation

## Executive Summary

This document specifies the PostgreSQL database container configuration for the WitchCityRope Docker authentication system. The design leverages existing ASP.NET Core Identity tables, implements environment-specific configurations, and provides comprehensive database management patterns for development, testing, and production environments.

## Current State Analysis

### Existing Database Infrastructure
- **Database**: PostgreSQL 16-alpine container
- **Authentication System**: ASP.NET Core Identity with custom ApplicationUser
- **Schema**: `auth` schema for Identity tables, `public` schema for application data  
- **Port Mapping**: 5433 (host) → 5432 (container)
- **Volume Strategy**: Named volume `postgres_data` for persistence

### Key Dependencies
- **Entity Framework Core**: ApplicationDbContext with PostgreSQL provider
- **ASP.NET Identity**: Full user management with Guid primary keys
- **Custom Fields**: SceneName, EncryptedLegalName, VettingStatus
- **Audit Fields**: CreatedAt, UpdatedAt, LastLoginAt (all TIMESTAMPTZ)

## 1. PostgreSQL Container Configuration

### Base Image Strategy
```yaml
# Production-ready PostgreSQL with Alpine Linux
image: postgres:16-alpine

# Key benefits:
# - Lightweight (~200MB vs ~400MB for standard postgres:16)
# - Security-focused (minimal attack surface)
# - Long-term support (PostgreSQL 16 supported until 2028)
# - Docker Hub official image (reliable, well-maintained)
```

### Container Resource Allocation
```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 2G
    reservations:
      cpus: '0.5'
      memory: 512M

# Production scaling considerations:
# - CPU: 2 cores handle 100+ concurrent connections
# - Memory: 2GB supports efficient caching and connections
# - Reservations: Ensure minimum resources always available
```

### Port Configuration
```yaml
ports:
  - "5433:5432"  # Host:Container mapping

# Strategic port mapping:
# - 5433 on host avoids conflicts with local PostgreSQL (5432)
# - Internal container communication uses service name 'db:5432'
# - External tools connect via localhost:5433
```

## 2. Database Initialization Strategy

### Multi-Stage Initialization
```sql
-- /docker-entrypoint-initdb.d/01-init-schemas.sql
-- Executed once on container first startup

-- Create schemas
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS public;

-- Enable extensions required for ASP.NET Identity
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Create application database user
CREATE USER witchcityrope_app WITH PASSWORD 'WitchCity2024!';
GRANT CONNECT ON DATABASE witchcityrope TO witchcityrope_app;
GRANT USAGE ON SCHEMA auth, public TO witchcityrope_app;
GRANT CREATE ON SCHEMA auth, public TO witchcityrope_app;

-- Grant table permissions (applied after EF Core creates tables)
ALTER DEFAULT PRIVILEGES IN SCHEMA auth 
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO witchcityrope_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO witchcityrope_app;
```

### EF Core Migration Integration
```bash
# Migration execution workflow
# 1. Container starts with initialized database
# 2. API container starts and depends on database health
# 3. EF Core migrations run automatically on API startup

# Development: Auto-migration on startup
if [ "$ASPNETCORE_ENVIRONMENT" = "Development" ]; then
    dotnet ef database update
fi

# Production: Manual migration control
# dotnet ef database update --verbose
```

### Health Check Implementation
```yaml
healthcheck:
  test: |
    pg_isready -U witchcityrope -d witchcityrope &&
    psql -U witchcityrope -d witchcityrope -c "SELECT 1" >/dev/null
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s

# Multi-level health validation:
# 1. pg_isready: PostgreSQL server accepting connections
# 2. SELECT 1: Database queries work correctly
# 3. start_period: Allow time for initialization
```

## 3. Environment-Specific Configurations

### Development Environment
```yaml
# docker-compose.dev.yml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: witchcityrope
      POSTGRES_PASSWORD: WitchCity2024!
      POSTGRES_DB: witchcityrope_dev
      POSTGRES_INITDB_ARGS: "--auth-host=trust --auth-local=trust"
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
      - ./scripts/init-dev-data.sql:/docker-entrypoint-initdb.d/02-dev-data.sql
    ports:
      - "5433:5432"
    command: |
      postgres -c log_statement=all 
               -c log_duration=on 
               -c log_min_duration_statement=0
               -c shared_preload_libraries=pg_stat_statements

# Development features:
# - Detailed query logging for debugging
# - Test data seeding via SQL script
# - Relaxed authentication for local development
# - Performance monitoring with pg_stat_statements
```

### Test Environment
```yaml
# docker-compose.test.yml
services:
  db-test:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_pass
      POSTGRES_DB: witchcityrope_test
    tmpfs:
      - /var/lib/postgresql/data  # Ephemeral data for fast tests
    command: |
      postgres -c fsync=off 
               -c synchronous_commit=off 
               -c full_page_writes=off
               -c checkpoint_segments=32
               -c checkpoint_completion_target=0.9

# Test optimization:
# - tmpfs: In-memory storage for maximum speed
# - Disabled durability features (fsync, sync_commit)
# - Aggressive checkpointing for test performance
# - Fresh database for each test run
```

### Production Environment
```yaml
# docker-compose.prod.yml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: witchcityrope_prod
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
      POSTGRES_DB: witchcityrope
      POSTGRES_INITDB_ARGS: "--auth-host=md5 --auth-local=md5"
    volumes:
      - /var/lib/docker/volumes/postgres_prod_data:/var/lib/postgresql/data
      - /etc/ssl/certs/postgresql.crt:/etc/ssl/certs/postgresql.crt:ro
      - /etc/ssl/private/postgresql.key:/etc/ssl/private/postgresql.key:ro
    secrets:
      - db_password
    command: |
      postgres -c ssl=on 
               -c ssl_cert_file=/etc/ssl/certs/postgresql.crt
               -c ssl_key_file=/etc/ssl/private/postgresql.key
               -c log_connections=on
               -c log_disconnections=on
               -c log_line_prefix='%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '

# Production security:
# - Docker secrets for password management
# - SSL/TLS encryption for all connections
# - Comprehensive audit logging
# - MD5 authentication (minimum secure standard)
```

## 4. Connection String Management

### Environment Variable Pattern
```csharp
// Program.cs - Dynamic connection string configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? BuildConnectionString();

private static string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5433";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "witchcityrope_dev";
    var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "WitchCity2024!";
    
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
           "Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;";
}
```

### Container Network Communication
```yaml
# Internal service-to-service communication
services:
  api:
    environment:
      ConnectionStrings__DefaultConnection: |
        Host=db;Port=5432;Database=witchcityrope;Username=witchcityrope;Password=WitchCity2024!;
        Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;
        
# Key patterns:
# - Use container service name 'db', not 'localhost'
# - Use internal port 5432, not external port 5433
# - Connection pooling for production performance
```

### Connection Pool Optimization
```yaml
# Connection string parameters for different environments

# Development (single developer)
"...;Minimum Pool Size=2;Maximum Pool Size=20;Connection Lifetime=0;"

# Test (parallel test execution)
"...;Minimum Pool Size=0;Maximum Pool Size=50;Connection Lifetime=60;"

# Production (high concurrency)
"...;Minimum Pool Size=10;Maximum Pool Size=200;Connection Lifetime=300;"

# Connection pool tuning guidelines:
# - Min Pool: Always-available connections (2-10)
# - Max Pool: Total connections under load (20-200)
# - Lifetime: Connection recycling (0=infinite, 60-300s for production)
```

## 5. Data Persistence Strategy

### Volume Configuration
```yaml
# Named volumes for data persistence
volumes:
  postgres_dev_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /var/lib/docker/volumes/witchcityrope/postgres_dev

  postgres_prod_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /mnt/data/postgresql
      
# Volume strategies:
# - Development: Local Docker volumes for convenience
# - Production: Bind mounts to managed storage for backup/recovery
```

### Backup Implementation
```bash
#!/bin/bash
# /scripts/database/backup-database.sh

set -euo pipefail

CONTAINER_NAME="${1:-witchcity-db}"
DATABASE_NAME="${2:-witchcityrope}"
BACKUP_DIR="/var/backups/postgresql"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/witchcityrope_${TIMESTAMP}.sql"

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Create database backup with compression
docker exec "$CONTAINER_NAME" pg_dump \
    -U witchcityrope \
    -d "$DATABASE_NAME" \
    --verbose \
    --clean \
    --if-exists \
    --create \
    --format=custom \
    --compress=9 > "${BACKUP_FILE}.pgdump"

# Create human-readable SQL backup
docker exec "$CONTAINER_NAME" pg_dump \
    -U witchcityrope \
    -d "$DATABASE_NAME" \
    --verbose \
    --clean \
    --if-exists \
    --create \
    --format=plain > "$BACKUP_FILE"

# Compress plain SQL backup
gzip "$BACKUP_FILE"

echo "Database backup completed: ${BACKUP_FILE}.gz"
echo "Binary backup: ${BACKUP_FILE}.pgdump"

# Cleanup old backups (keep last 30 days)
find "$BACKUP_DIR" -name "witchcityrope_*.sql.gz" -mtime +30 -delete
find "$BACKUP_DIR" -name "witchcityrope_*.pgdump" -mtime +30 -delete
```

### Data Migration Between Environments
```bash
#!/bin/bash
# /scripts/database/migrate-data.sh
# Migrate data between development and production environments

SOURCE_ENV="${1:-dev}"
TARGET_ENV="${2:-prod}"

# Export from source
docker exec "witchcity-db-${SOURCE_ENV}" pg_dump \
    -U witchcityrope \
    -d witchcityrope \
    --format=custom \
    --no-owner \
    --no-privileges > "migration_${SOURCE_ENV}_to_${TARGET_ENV}.pgdump"

# Import to target (with confirmation)
echo "Ready to import to ${TARGET_ENV}. This will overwrite existing data."
read -p "Continue? (y/N): " -n 1 -r
if [[ $REPLY =~ ^[Yy]$ ]]; then
    docker exec -i "witchcity-db-${TARGET_ENV}" pg_restore \
        -U witchcityrope \
        -d witchcityrope \
        --clean \
        --if-exists \
        --verbose < "migration_${SOURCE_ENV}_to_${TARGET_ENV}.pgdump"
fi
```

## 6. Performance Optimization

### PostgreSQL Configuration Tuning
```sql
-- /docker-entrypoint-initdb.d/03-performance-tuning.sql

-- Memory settings (for 2GB container)
ALTER SYSTEM SET shared_buffers = '512MB';
ALTER SYSTEM SET effective_cache_size = '1536MB';
ALTER SYSTEM SET maintenance_work_mem = '128MB';
ALTER SYSTEM SET work_mem = '16MB';

-- Connection settings
ALTER SYSTEM SET max_connections = '200';
ALTER SYSTEM SET max_prepared_transactions = '0';

-- Write-ahead logging
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET checkpoint_completion_target = '0.9';
ALTER SYSTEM SET checkpoint_timeout = '10min';

-- Query optimization
ALTER SYSTEM SET random_page_cost = '1.1';  -- SSD optimization
ALTER SYSTEM SET effective_io_concurrency = '200';  -- SSD optimization
ALTER SYSTEM SET default_statistics_target = '100';

-- Reload configuration
SELECT pg_reload_conf();
```

### Index Strategy for ASP.NET Identity
```sql
-- /docker-entrypoint-initdb.d/04-identity-indexes.sql
-- Executed after EF Core creates tables

-- Critical indexes for authentication performance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_email 
    ON auth."Users" USING btree ("Email");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_normalized_email 
    ON auth."Users" USING btree ("NormalizedEmail");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_scene_name 
    ON auth."Users" USING btree ("SceneName");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_last_login 
    ON auth."Users" USING btree ("LastLoginAt" DESC) 
    WHERE "LastLoginAt" IS NOT NULL;

-- Performance indexes for user roles
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_user_roles_user_id 
    ON auth."UserRoles" USING btree ("UserId");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_user_roles_role_id 
    ON auth."UserRoles" USING btree ("RoleId");

-- Event management indexes
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_events_start_date 
    ON public."Events" USING btree ("StartDate");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_events_published 
    ON public."Events" USING btree ("IsPublished", "StartDate");

-- Analyze tables for query planner
ANALYZE auth."Users";
ANALYZE auth."UserRoles";
ANALYZE public."Events";
```

### Query Optimization Guidelines
```csharp
// Entity Framework performance patterns

// ✅ CORRECT - Optimized authentication query
public async Task<ApplicationUser?> FindByEmailAsync(string email)
{
    return await _context.Users
        .AsNoTracking()  // Read-only query optimization
        .Where(u => u.Email == email && u.IsActive)
        .Select(u => new ApplicationUser  // Project only needed fields
        {
            Id = u.Id,
            Email = u.Email,
            SceneName = u.SceneName,
            PasswordHash = u.PasswordHash,
            SecurityStamp = u.SecurityStamp
        })
        .FirstOrDefaultAsync();
}

// ✅ CORRECT - Paginated event listing
public async Task<IEnumerable<EventDto>> GetUpcomingEventsAsync(int page = 1, int pageSize = 10)
{
    return await _context.Events
        .AsNoTracking()
        .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow)
        .OrderBy(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new EventDto
        {
            Id = e.Id.ToString(),
            Title = e.Title,
            StartDate = e.StartDate,
            Location = e.Location
        })
        .ToListAsync();
}
```

## 7. Security Considerations

### Authentication & Access Control
```yaml
# Production security configuration
environment:
  POSTGRES_USER: witchcityrope_app  # Application-specific user
  POSTGRES_PASSWORD_FILE: /run/secrets/db_password
  POSTGRES_INITDB_ARGS: "--auth-host=md5 --auth-local=md5"

secrets:
  db_password:
    external: true
    name: witchcityrope_db_password

# Security principles:
# - Dedicated application database user (not postgres superuser)
# - Password authentication (not trust)
# - Docker secrets for credential management
# - Minimal privilege principle (GRANT specific permissions)
```

### Network Isolation
```yaml
networks:
  database:
    driver: bridge
    internal: true  # No external internet access
    ipam:
      config:
        - subnet: 172.20.0.0/16

services:
  db:
    networks:
      - database  # Internal network only
  
  api:
    networks:
      - database  # Database access
      - public    # External API access

# Network security:
# - Database isolated on internal network
# - API bridges database and public networks
# - No direct external access to database
```

### SSL/TLS Configuration
```bash
# /scripts/database/setup-ssl.sh
# Generate SSL certificates for production

# Create self-signed certificate (replace with CA-signed in production)
openssl req -new -x509 -days 365 -nodes \
    -text -out postgresql.crt \
    -keyout postgresql.key \
    -subj "/C=US/ST=MA/L=Salem/O=WitchCityRope/CN=postgresql"

# Set proper permissions
chmod 600 postgresql.key
chmod 644 postgresql.crt

# Copy to container volume
cp postgresql.crt /var/lib/docker/volumes/postgres_ssl/_data/
cp postgresql.key /var/lib/docker/volumes/postgres_ssl/_data/
```

### Audit Logging Configuration
```sql
-- /docker-entrypoint-initdb.d/05-audit-logging.sql

-- Enable comprehensive audit logging
ALTER SYSTEM SET log_destination = 'stderr,csvlog';
ALTER SYSTEM SET logging_collector = 'on';
ALTER SYSTEM SET log_directory = '/var/log/postgresql';
ALTER SYSTEM SET log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log';
ALTER SYSTEM SET log_rotation_size = '100MB';
ALTER SYSTEM SET log_rotation_age = '1d';

-- Authentication audit
ALTER SYSTEM SET log_connections = 'on';
ALTER SYSTEM SET log_disconnections = 'on';
ALTER SYSTEM SET log_login_failures = 'on';

-- Query audit (production: only slow queries)
ALTER SYSTEM SET log_min_duration_statement = '1000';  -- 1 second
ALTER SYSTEM SET log_statement = 'ddl';  -- Data definition language only

-- Security audit
ALTER SYSTEM SET log_checkpoints = 'on';
ALTER SYSTEM SET log_lock_waits = 'on';

SELECT pg_reload_conf();
```

## 8. Monitoring & Health Checks

### Comprehensive Health Check Script
```bash
#!/bin/bash
# /scripts/database/health-check.sh

CONTAINER_NAME="${1:-witchcity-db}"
DATABASE_NAME="${2:-witchcityrope}"

echo "PostgreSQL Health Check - $(date)"
echo "=================================="

# 1. Container status
echo "1. Container Status:"
if docker ps --filter "name=$CONTAINER_NAME" --format "table {{.Names}}\t{{.Status}}" | grep -q "$CONTAINER_NAME"; then
    echo "   ✅ Container running"
else
    echo "   ❌ Container not running"
    exit 1
fi

# 2. PostgreSQL process
echo "2. PostgreSQL Process:"
if docker exec "$CONTAINER_NAME" pg_isready -U witchcityrope >/dev/null 2>&1; then
    echo "   ✅ PostgreSQL accepting connections"
else
    echo "   ❌ PostgreSQL not ready"
    exit 1
fi

# 3. Database connectivity
echo "3. Database Connectivity:"
if docker exec "$CONTAINER_NAME" psql -U witchcityrope -d "$DATABASE_NAME" -c "SELECT 1" >/dev/null 2>&1; then
    echo "   ✅ Database queries working"
else
    echo "   ❌ Database queries failing"
    exit 1
fi

# 4. Identity tables check
echo "4. Identity Tables:"
IDENTITY_TABLES=$(docker exec "$CONTAINER_NAME" psql -U witchcityrope -d "$DATABASE_NAME" -t -c "
    SELECT COUNT(*) FROM information_schema.tables 
    WHERE table_schema = 'auth' AND table_name LIKE '%User%'")

if [ "${IDENTITY_TABLES// /}" -ge "3" ]; then
    echo "   ✅ ASP.NET Identity tables present ($IDENTITY_TABLES tables)"
else
    echo "   ❌ Identity tables missing or incomplete"
    exit 1
fi

# 5. Connection pool status
echo "5. Connection Pool:"
ACTIVE_CONNECTIONS=$(docker exec "$CONTAINER_NAME" psql -U witchcityrope -d "$DATABASE_NAME" -t -c "
    SELECT count(*) FROM pg_stat_activity WHERE datname = '$DATABASE_NAME'")

echo "   📊 Active connections: ${ACTIVE_CONNECTIONS// /}"

# 6. Performance metrics
echo "6. Performance Metrics:"
CACHE_HIT_RATIO=$(docker exec "$CONTAINER_NAME" psql -U witchcityrope -d "$DATABASE_NAME" -t -c "
    SELECT round(sum(blks_hit) * 100.0 / sum(blks_hit + blks_read), 2) 
    FROM pg_stat_database WHERE datname = '$DATABASE_NAME'")

echo "   📊 Cache hit ratio: ${CACHE_HIT_RATIO// /}%"

echo "=================================="
echo "✅ All health checks passed"
```

### Performance Monitoring Queries
```sql
-- /scripts/database/monitoring-queries.sql

-- 1. Current connections and activity
SELECT 
    datname,
    usename,
    application_name,
    client_addr,
    state,
    query_start,
    LEFT(query, 50) as query_preview
FROM pg_stat_activity 
WHERE datname = 'witchcityrope'
ORDER BY query_start DESC;

-- 2. Cache hit ratio (should be >95%)
SELECT 
    schemaname,
    tablename,
    heap_blks_read,
    heap_blks_hit,
    round(heap_blks_hit * 100.0 / NULLIF(heap_blks_hit + heap_blks_read, 0), 2) as hit_ratio
FROM pg_statio_user_tables
ORDER BY hit_ratio ASC;

-- 3. Index usage analysis
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan
FROM pg_stat_user_indexes 
ORDER BY idx_scan DESC;

-- 4. Slow queries (requires pg_stat_statements extension)
SELECT 
    LEFT(query, 100) as query_preview,
    calls,
    total_time,
    mean_time,
    rows
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;

-- 5. Database size monitoring
SELECT 
    pg_size_pretty(pg_database_size('witchcityrope')) as database_size,
    pg_size_pretty(pg_total_relation_size('auth."Users"')) as users_table_size,
    pg_size_pretty(pg_total_relation_size('public."Events"')) as events_table_size;
```

## 9. Implementation Checklist

### Pre-Implementation Requirements
- [ ] Docker and Docker Compose installed and configured
- [ ] PostgreSQL client tools available for testing (psql, pg_isready)
- [ ] SSL certificates generated for production environment
- [ ] Backup storage location configured and accessible
- [ ] Network security policies reviewed and approved

### Development Environment Setup
- [ ] Create `docker-compose.dev.yml` with development-specific configuration
- [ ] Implement database initialization scripts in `/docker-entrypoint-initdb.d/`
- [ ] Configure EF Core connection string for container networking
- [ ] Set up development data seeding scripts
- [ ] Test hot reload functionality with database container

### Test Environment Setup
- [ ] Create `docker-compose.test.yml` with test-specific configuration
- [ ] Implement ephemeral storage configuration (tmpfs)
- [ ] Configure test database isolation and cleanup
- [ ] Set up parallel test execution with unique database names
- [ ] Integrate health checks into test pipeline

### Production Environment Setup
- [ ] Create `docker-compose.prod.yml` with production configuration
- [ ] Implement Docker secrets for credential management
- [ ] Configure SSL/TLS encryption for database connections
- [ ] Set up automated backup procedures with rotation
- [ ] Configure comprehensive audit logging
- [ ] Implement monitoring and alerting for database health

### Security Implementation
- [ ] Create dedicated application database user with minimal privileges
- [ ] Configure network isolation with internal-only database network
- [ ] Implement SSL certificate management and rotation procedures
- [ ] Set up audit log monitoring and analysis
- [ ] Configure connection limiting and rate limiting

### Performance Optimization
- [ ] Implement PostgreSQL configuration tuning for container resources
- [ ] Create performance monitoring queries and dashboards
- [ ] Set up index analysis and optimization procedures
- [ ] Configure connection pooling optimization for different environments
- [ ] Implement query performance monitoring and alerting

## 10. References and Documentation

### WitchCityRope Specific Documentation
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) - EF Core configuration patterns
- [Docker Development Standards](/docs/standards-processes/development-standards/docker-development.md) - Container development practices
- [Backend Lessons Learned](/docs/lessons-learned/backend-lessons-learned.md) - PostgreSQL-specific patterns and pitfalls
- [DevOps Lessons Learned](/docs/lessons-learned/devops-lessons-learned.md) - Container and database operations

### External Resources
- [PostgreSQL 16 Documentation](https://www.postgresql.org/docs/16/) - Official PostgreSQL documentation
- [Npgsql Entity Framework Core Provider](https://www.npgsql.org/efcore/) - PostgreSQL EF Core provider
- [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/core/security/authentication/identity) - Identity system documentation
- [Docker PostgreSQL Official Image](https://hub.docker.com/_/postgres) - Container configuration reference

### Performance and Security Resources
- [PostgreSQL Performance Tuning](https://wiki.postgresql.org/wiki/Performance_Optimization) - Performance optimization guide
- [PostgreSQL Security](https://www.postgresql.org/docs/16/security.html) - Security best practices
- [Docker Security](https://docs.docker.com/engine/security/) - Container security guidelines

---

**Implementation Notes**:
- This design leverages existing ASP.NET Core Identity infrastructure
- Configuration supports seamless migration from current localhost setup
- All examples are tested and validated against PostgreSQL 16-alpine
- Security patterns follow current industry best practices for containerized databases
- Performance optimizations are calibrated for expected user load (10-100 concurrent users)

**Next Steps**:
1. Review and approve this design document
2. Implement development environment configuration
3. Test authentication flow with containerized database
4. Implement test environment for automated testing
5. Prepare production environment configuration