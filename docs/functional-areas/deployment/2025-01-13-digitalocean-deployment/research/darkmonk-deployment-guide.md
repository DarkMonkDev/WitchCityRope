# DarkMonk Deployment Guide Reference
<!-- Last Updated: 2025-01-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Deployment Team -->
<!-- Status: Research -->

> **Source**: Copied from `/home/chad/repos/darkmonk/docs/04-deployment/DEPLOYMENT_GUIDE.md`
> **Date Copied**: 2025-01-13
> **Purpose**: Reference material for WitchCityRope deployment procedures and best practices

# DarkMonk Production Deployment Guide

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Deployment Strategies](#deployment-strategies)
4. [Step-by-Step Deployment](#step-by-step-deployment)
5. [Rollback Procedures](#rollback-procedures)
6. [Monitoring and Health Checks](#monitoring-and-health-checks)
7. [Troubleshooting](#troubleshooting)
8. [Security Checklist](#security-checklist)

## Prerequisites

### Required Tools
- Docker & Docker Compose
- .NET 9 SDK
- PostgreSQL client tools
- Git
- SSL certificates for production domain

### Access Requirements
- SSH access to production servers
- Database administrator credentials
- Container registry access
- Monitoring system access

## Environment Setup

### 1. Environment Variables

Create `.env.production` file:
```bash
# Database
POSTGRES_HOST=prod-db.darkmonk.com
POSTGRES_PORT=5432
POSTGRES_DB=darkmonk_prod
POSTGRES_USER=darkmonk_app
POSTGRES_PASSWORD=<secure-password>

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
DARKMONK_BASE_URL=https://darkmonk.com

# Security
JWT_SECRET_KEY=<secure-jwt-key>
DATA_PROTECTION_KEY_PATH=/app/keys

# Storage
STORAGE_PROVIDER=FileSystem
STORAGE_BASE_PATH=/app/storage

# Email
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USERNAME=apikey
SMTP_PASSWORD=<sendgrid-api-key>

# Payment Processing
STRIPE_SECRET_KEY=<stripe-secret-key>
STRIPE_WEBHOOK_SECRET=<stripe-webhook-secret>

# Redis Cache
REDIS_CONNECTION=prod-redis.darkmonk.com:6379,password=<redis-password>
```

### 2. Production Secrets

Store sensitive configuration in HashiCorp Vault or Azure Key Vault:
```bash
# Vault setup
vault kv put secret/darkmonk/production \
  db_password="<secure-password>" \
  jwt_secret="<secure-jwt-key>" \
  stripe_secret="<stripe-secret-key>" \
  sendgrid_api_key="<api-key>"
```

## Deployment Strategies

### 1. Blue-Green Deployment (Recommended for Zero Downtime)

```bash
# Deploy to green environment
./Scripts/deploy-production.sh deploy --strategy blue-green

# Switch traffic to green
./Scripts/deploy-production.sh switch-traffic green

# Verify and remove blue
./Scripts/deploy-production.sh cleanup blue
```

### 2. Rolling Update (With Brief Downtime)

```bash
# Standard deployment
./Scripts/deploy-production.sh deploy --strategy standard
```

### 3. Canary Deployment

```bash
# Deploy canary (10% traffic)
./Scripts/deploy-production.sh deploy --strategy canary --percentage 10

# Increase traffic gradually
./Scripts/deploy-production.sh canary --percentage 50
./Scripts/deploy-production.sh canary --percentage 100
```

## Step-by-Step Deployment

### 1. Pre-Deployment Checklist

```bash
# Run pre-deployment checks
./Scripts/deploy-production.sh pre-check

□ All tests passing
□ No security vulnerabilities
□ Database migrations reviewed
□ Configuration validated
□ Backup completed
□ Maintenance window scheduled
□ Team notified
```

### 2. Backup Current State

```bash
# Automated backup
./Scripts/deploy-production.sh backup

# Manual verification
docker exec darkmonk-db pg_dump -U postgres darkmonk_prod > backup_$(date +%Y%m%d_%H%M%S).sql
docker cp darkmonk-web:/app/storage ./storage_backup_$(date +%Y%m%d_%H%M%S)
```

### 3. Deploy Application

```bash
# Dry run first
./Scripts/deploy-production.sh dry-run

# Actual deployment
./Scripts/deploy-production.sh deploy

# Monitor deployment
./Scripts/deploy-production.sh monitor
```

### 4. Database Migrations

```bash
# Review migrations
./Scripts/migrate-database.sh dry-run production

# Apply migrations
./Scripts/migrate-database.sh apply production

# Verify
./Scripts/migrate-database.sh verify production
```

### 5. Post-Deployment Verification

```bash
# Run health checks
./Scripts/health-check.sh full production

# Check specific endpoints
curl -k https://darkmonk.com/health
curl -k https://darkmonk.com/api/products

# Monitor logs
docker logs -f darkmonk-web --tail 100
```

## Rollback Procedures

### Immediate Rollback (< 5 minutes)

```bash
# Quick rollback to previous version
./Scripts/deploy-production.sh rollback

# Verify rollback
./Scripts/health-check.sh full production
```

### Database Rollback

```bash
# Restore database backup
./Scripts/migrate-database.sh rollback production --to-version <version>

# Or full restore
docker exec -i darkmonk-db psql -U postgres darkmonk_prod < backup_20250628_120000.sql
```

### Full System Restore

```bash
# Stop current deployment
docker-compose -f docker-compose.prod.yml down

# Restore from backup
./Scripts/deploy-production.sh restore --backup-id <backup-id>

# Restart services
docker-compose -f docker-compose.prod.yml up -d
```

## Monitoring and Health Checks

### Continuous Monitoring

```bash
# Start monitoring daemon
./Scripts/health-check.sh monitor production --interval 60

# Check specific services
./Scripts/health-check.sh check-service database production
./Scripts/health-check.sh check-service redis production
./Scripts/health-check.sh check-service storage production
```

### Key Metrics to Monitor

1. **Application Health**
   - Response times < 200ms
   - Error rate < 1%
   - CPU usage < 70%
   - Memory usage < 80%

2. **Database Performance**
   - Query execution time < 100ms
   - Connection pool usage < 80%
   - Lock wait time < 50ms

3. **Business Metrics**
   - Order completion rate
   - Payment success rate
   - Cart abandonment rate

## Troubleshooting

### Common Issues

1. **Application Won't Start**
```bash
# Check logs
docker logs darkmonk-web --tail 200

# Verify configuration
docker exec darkmonk-web printenv | grep ASPNETCORE

# Test database connection
docker exec darkmonk-web /app/Scripts/test-db-connection.sh
```

2. **Database Connection Issues**
```bash
# Test connectivity
docker exec darkmonk-web pg_isready -h $POSTGRES_HOST -p $POSTGRES_PORT

# Check connection string
docker exec darkmonk-web dotnet user-secrets list
```

3. **SSL Certificate Issues**
```bash
# Verify certificates
openssl s_client -connect darkmonk.com:443 -servername darkmonk.com

# Check certificate expiry
echo | openssl s_client -connect darkmonk.com:443 2>/dev/null | openssl x509 -noout -dates
```

### Emergency Procedures

1. **Enable Maintenance Mode**
```bash
./Scripts/deploy-production.sh maintenance enable
```

2. **Scale Down**
```bash
docker-compose -f docker-compose.prod.yml scale web=1
```

3. **Emergency Rollback**
```bash
./Scripts/deploy-production.sh emergency-rollback
```

## Security Checklist

### Pre-Deployment Security
- [ ] Run security scan: `dotnet list package --vulnerable`
- [ ] Check for secrets in code: `git secrets --scan`
- [ ] Verify SSL certificates are valid
- [ ] Ensure all endpoints require HTTPS
- [ ] Validate CORS configuration
- [ ] Check rate limiting is enabled

### Post-Deployment Security
- [ ] Verify security headers are present
- [ ] Test authentication on all endpoints
- [ ] Check for information disclosure
- [ ] Verify logging doesn't contain sensitive data
- [ ] Run penetration tests on staging

### Production Security Configuration
```bash
# Security headers (should be present)
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: <policy>
```

## Deployment Schedule

### Recommended Deployment Windows
- **Tuesday-Thursday**: 2:00 AM - 4:00 AM PST (lowest traffic)
- **Avoid**: Weekends, Mondays, Fridays, holidays

### Deployment Frequency
- **Staging**: Daily or on-demand
- **Production**: Weekly (Tuesday night) or emergency fixes

## Post-Deployment Tasks

1. **Update Documentation**
   - Update version in README
   - Document any configuration changes
   - Update API documentation

2. **Notify Stakeholders**
   - Send deployment summary
   - Update status page
   - Notify customer support of changes

3. **Monitor for 24 Hours**
   - Watch error rates
   - Monitor performance metrics
   - Check user feedback channels

## Automated Deployment Pipeline

### GitHub Actions Workflow
```yaml
name: Production Deployment
on:
  push:
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy to Production
        run: |
          ./Scripts/deploy-production.sh deploy \
            --version ${{ github.ref_name }} \
            --strategy blue-green
```

## Support Contacts

- **DevOps Team**: devops@darkmonk.com
- **On-Call Engineer**: +1-555-DARKMONK
- **Emergency Escalation**: emergency@darkmonk.com

## Additional Resources

- [Architecture Documentation](./ARCHITECTURE.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [Monitoring Dashboard](https://monitoring.darkmonk.com)
- [Status Page](https://status.darkmonk.com)