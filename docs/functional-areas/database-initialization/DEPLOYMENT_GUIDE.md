# Database Auto-Initialization Deployment Guide

## Overview
This guide covers deployment of the database auto-initialization system to production environments.

## Production Configuration

### 1. Environment Variables

```bash
# Production appsettings.Production.json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,      # Enable for automatic schema updates
    "EnableSeedData": false,           # DISABLE for production
    "TimeoutSeconds": 60,              # Increase for larger migrations
    "FailOnSeedDataError": false,      # Don't fail in production
    "ExcludedEnvironments": [],        # Empty to allow in production
    "MaxRetryAttempts": 5,             # More retries for production
    "RetryDelaySeconds": 5.0           # Longer delays for stability
  }
}
```

### 2. Connection String Configuration

```bash
# Use environment variable for production connection string
export ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Database=witchcityrope;Username=produser;Password=SECURE_PASSWORD;Include Error Detail=false"
```

### 3. Health Check Configuration

The database initialization health check is available at:
- Development: `http://localhost:5653/api/health/database`
- Production: `https://api.witchcityrope.com/health/database`

## Deployment Steps

### Step 1: Pre-Deployment Validation

```bash
# 1. Test migrations locally with production-like data
dotnet ef migrations script --idempotent > migration_script.sql

# 2. Review the migration script for breaking changes
cat migration_script.sql | grep -E "DROP|ALTER|DELETE"

# 3. Backup production database
pg_dump -h prod-db -U produser -d witchcityrope > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Step 2: Deploy API with Auto-Initialization

```bash
# 1. Build production image
docker build -t witchcityrope-api:latest -f Dockerfile --target final .

# 2. Deploy with proper environment configuration
docker run -d \
  --name witchcityrope-api \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="$PROD_CONNECTION_STRING" \
  -e DatabaseInitialization__EnableAutoMigration=true \
  -e DatabaseInitialization__EnableSeedData=false \
  -p 8080:8080 \
  witchcityrope-api:latest
```

### Step 3: Monitor Initialization

```bash
# 1. Watch container logs during startup
docker logs -f witchcityrope-api

# 2. Check health endpoint
curl https://api.witchcityrope.com/health/database

# Expected response:
{
  "status": "Healthy",
  "data": {
    "initializationCompleted": true,
    "status": "Ready",
    "userCount": 1500,  # Your production user count
    "eventCount": 250   # Your production event count
  }
}
```

### Step 4: Rollback Procedure (if needed)

```bash
# 1. Stop the container
docker stop witchcityrope-api

# 2. Restore database backup
psql -h prod-db -U produser -d witchcityrope < backup_20250822_120000.sql

# 3. Deploy previous version
docker run -d \
  --name witchcityrope-api \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DatabaseInitialization__EnableAutoMigration=false \
  witchcityrope-api:previous
```

## Production Best Practices

### 1. Blue-Green Deployment

```yaml
# docker-compose.prod.yml
services:
  api-blue:
    image: witchcityrope-api:latest
    environment:
      - DatabaseInitialization__EnableAutoMigration=true
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/database"]
      
  api-green:
    image: witchcityrope-api:previous
    environment:
      - DatabaseInitialization__EnableAutoMigration=false
```

### 2. Migration Safety

**NEVER run seed data in production:**
```json
{
  "DatabaseInitialization": {
    "EnableSeedData": false  // CRITICAL: Always false in production
  }
}
```

**Test migrations before deployment:**
```bash
# Generate idempotent script
dotnet ef migrations script --idempotent

# Test on staging database first
psql -h staging-db -U stageuser -d witchcityrope_staging -f migration_script.sql
```

### 3. Monitoring and Alerts

Configure alerts for:
- Initialization timeout (>60 seconds)
- Health check failures
- Database connection failures
- Migration rollback events

Example alert configuration:
```yaml
alerts:
  - name: database_initialization_failed
    condition: health_check_status{endpoint="/health/database"} != "Healthy"
    duration: 5m
    action: page_oncall
```

## Troubleshooting

### Issue: Initialization Timeout

**Symptom**: Container exits with "Database initialization timeout"

**Solution**:
```bash
# Increase timeout for large migrations
-e DatabaseInitialization__TimeoutSeconds=120
```

### Issue: Migration Failure

**Symptom**: "Migration failed: relation already exists"

**Solution**:
```bash
# Check migration history
SELECT * FROM "__EFMigrationsHistory";

# Manually mark migration as applied if needed
INSERT INTO "__EFMigrationsHistory" (MigrationId, ProductVersion) 
VALUES ('20250822_InitialCreate', '9.0.0');
```

### Issue: Health Check Unhealthy

**Symptom**: Health endpoint returns "Unhealthy" after deployment

**Solution**:
```bash
# Check container logs
docker logs witchcityrope-api | grep -E "error|fail|exception"

# Verify database connectivity
docker exec witchcityrope-api psql $ConnectionStrings__DefaultConnection -c "SELECT 1"

# Check initialization status
curl https://api.witchcityrope.com/health/database | jq .
```

## Security Considerations

1. **Never expose sensitive connection strings in logs**
   - Use `Include Error Detail=false` in production
   - Sanitize logs before sharing

2. **Restrict health endpoint access**
   ```nginx
   location /health/database {
       allow 10.0.0.0/8;  # Internal network only
       deny all;
   }
   ```

3. **Use managed database credentials**
   - Azure: Managed Identity
   - AWS: IAM database authentication
   - On-premise: Vault or similar

## Performance Optimization

### For Large Databases

```json
{
  "DatabaseInitialization": {
    "EnableAutoMigration": true,
    "MigrationBatchSize": 1000,        # Process in batches
    "CommandTimeout": 300,              # 5 minutes for large operations
    "UseTransaction": false             # For very large migrations
  }
}
```

### Connection Pool Settings

```
Host=prod-db;...;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;Connection Idle Lifetime=300
```

## Validation Checklist

Before deploying to production:

- [ ] Migrations tested on staging environment
- [ ] Database backup completed
- [ ] Health check endpoint accessible
- [ ] Monitoring alerts configured
- [ ] Rollback procedure documented
- [ ] Connection strings secured
- [ ] Timeout values appropriate for database size
- [ ] Seed data disabled for production
- [ ] Migration script reviewed for breaking changes
- [ ] Team notified of deployment window

## Support

For issues with database initialization in production:
1. Check container logs for detailed error messages
2. Verify database connectivity and permissions
3. Review migration history table
4. Contact DevOps team with health check response

---

**Remember**: The auto-initialization system is designed to fail fast. If initialization fails, the container will exit to prevent running with an inconsistent database state. This is intentional and ensures data integrity.