# Staging Deployment Guide - BLAZOR LEGACY
<!-- ARCHIVED: 2025-08-17 - This document is for Blazor Server deployment -->
<!-- STATUS: OBSOLETE - Use React deployment guides for current project -->
<!-- REPLACEMENT: /docs/guides-setup/docker-production-deployment.md -->

> **⚠️ ARCHIVED CONTENT**  
> This document contains Blazor Server deployment procedures.  
> For current React + Docker deployment, see: `/docs/guides-setup/docker-production-deployment.md`

# Original Staging Deployment Guide

## Overview

This comprehensive guide covers deploying WitchCityRope to a staging environment for final testing before production release. The staging environment mirrors production as closely as possible while providing additional debugging capabilities.

## Prerequisites

- Linux server (Ubuntu 20.04+ or similar)
- Docker and Docker Compose installed
- .NET 8.0 SDK (for migrations)
- Domain configured: `staging.witchcityrope.com`
- SSL certificates (Let's Encrypt or self-signed)
- Access to staging secrets/credentials

## Quick Start

```bash
# Clone repository
git clone https://github.com/witchcityrope/witchcityrope.git
cd witchcityrope

# Switch to staging branch
git checkout staging

# Copy staging environment file
cp .env.staging.example .env.staging

# Edit environment variables
nano .env.staging

# Deploy staging
./deploy-staging.sh
```

## Detailed Deployment Steps

### 1. Server Preparation

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install required packages
sudo apt install -y \
    docker.io \
    docker-compose \
    nginx \
    certbot \
    python3-certbot-nginx \
    sqlite3 \
    git \
    ufw

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Configure firewall
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### 2. Application Setup

```bash
# Create application directory
sudo mkdir -p /opt/witchcityrope/staging
sudo chown -R $USER:$USER /opt/witchcityrope
cd /opt/witchcityrope/staging

# Clone repository
git clone https://github.com/witchcityrope/witchcityrope.git .
git checkout staging

# Create required directories
mkdir -p data/staging logs certs/staging backups uploads
```

### 3. Environment Configuration

Create `.env.staging` file:

```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Staging
BUILD_TARGET=final
COMPOSE_PROJECT_NAME=witchcityrope-staging

# URLs
PUBLIC_URL=https://staging.witchcityrope.com
API_URL=https://api.staging.witchcityrope.com

# Security
JWT_SECRET=your-staging-jwt-secret-minimum-32-characters
ENCRYPTION_KEY=your-staging-encryption-key-32ch
STAGING_CERT_PASSWORD=your-cert-password

# Database
CONNECTION_STRING=Data Source=/app/data/witchcityrope_staging.db
ENABLE_REDIS=true
REDIS_CONNECTION_STRING=redis:6379,abortConnect=false

# Email (SendGrid)
SENDGRID_API_KEY=SG.your-sendgrid-api-key
SENDGRID_SANDBOX_MODE=true

# PayPal (Sandbox)
PAYPAL_CLIENT_ID=your-sandbox-client-id
PAYPAL_CLIENT_SECRET=your-sandbox-client-secret
PAYPAL_MODE=sandbox

# Monitoring
SEQ_API_KEY=your-seq-api-key
APPLICATION_INSIGHTS_CONNECTION_STRING=your-app-insights-connection

# Feature Flags
ENABLE_DEBUG_ENDPOINTS=true
ENABLE_SWAGGER=true
DETAILED_ERRORS=true
```

### 4. SSL Certificate Setup

```bash
# Option 1: Let's Encrypt
sudo certbot certonly --standalone \
  -d staging.witchcityrope.com \
  -d www.staging.witchcityrope.com \
  -d api.staging.witchcityrope.com

# Option 2: Self-signed (for internal testing)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout certs/staging/privkey.pem \
  -out certs/staging/fullchain.pem \
  -subj "/CN=*.staging.witchcityrope.com"

# Convert to PFX for .NET
openssl pkcs12 -export \
  -out certs/staging/staging.pfx \
  -inkey certs/staging/privkey.pem \
  -in certs/staging/fullchain.pem \
  -password pass:${STAGING_CERT_PASSWORD}
```

### 5. Database Initialization

```bash
# Make script executable
chmod +x scripts/init-staging-db.sh

# Run database initialization
./scripts/init-staging-db.sh

# Verify database
sqlite3 data/staging/witchcityrope_staging.db "SELECT COUNT(*) FROM Users;"
```

### 6. Docker Deployment

Create `docker-compose.staging.yml`:

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/WitchCityRope.Api/Dockerfile
      target: final
    container_name: wcr-staging-api
    restart: unless-stopped
    ports:
      - "5653:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_URLS=http://+:8080
    env_file:
      - .env.staging
    volumes:
      - ./data/staging:/app/data
      - ./logs/api:/app/logs
      - ./uploads:/app/uploads
      - ./certs/staging:/app/certs:ro
    networks:
      - staging-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build:
      context: .
      dockerfile: src/WitchCityRope.Web/Dockerfile
      target: final
    container_name: wcr-staging-web
    restart: unless-stopped
    ports:
      - "5651:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_URLS=http://+:8080
      - ApiBaseUrl=http://api:8080/
      - ApiBaseUrlExternal=https://api.staging.witchcityrope.com/
    env_file:
      - .env.staging
    volumes:
      - ./data/staging:/app/data:ro
      - ./logs/web:/app/logs
      - ./uploads:/app/wwwroot/uploads:ro
      - ./certs/staging:/app/certs:ro
    depends_on:
      - api
    networks:
      - staging-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    container_name: wcr-staging-redis
    restart: unless-stopped
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    networks:
      - staging-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  seq:
    image: datalust/seq:latest
    container_name: wcr-staging-seq
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - SEQ_API_KEY=${SEQ_API_KEY}
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
    networks:
      - staging-network

networks:
  staging-network:
    driver: bridge

volumes:
  redis-data:
  seq-data:
```

Deploy with Docker Compose:

```bash
# Build and start services
docker-compose -f docker-compose.staging.yml up -d --build

# View logs
docker-compose -f docker-compose.staging.yml logs -f

# Check service health
docker-compose -f docker-compose.staging.yml ps
```

### 7. Nginx Configuration

```bash
# Create Nginx configuration
sudo nano /etc/nginx/sites-available/staging.witchcityrope.com
```

Add the configuration from the SSL guide, then:

```bash
# Enable site
sudo ln -s /etc/nginx/sites-available/staging.witchcityrope.com /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

### 8. Post-Deployment Verification

```bash
# Check API health
curl https://api.staging.witchcityrope.com/health

# Check web application
curl -I https://staging.witchcityrope.com

# Test database connectivity
docker exec wcr-staging-api sqlite3 /app/data/witchcityrope_staging.db "SELECT COUNT(*) FROM Users;"

# Check logs for errors
docker logs wcr-staging-api --tail 50
docker logs wcr-staging-web --tail 50

# Monitor resource usage
docker stats
```

## Deployment Automation Script

Create `deploy-staging.sh`:

```bash
#!/bin/bash
set -e

echo "Starting WitchCityRope Staging Deployment..."

# Configuration
DEPLOY_DIR="/opt/witchcityrope/staging"
COMPOSE_FILE="docker-compose.staging.yml"
ENV_FILE=".env.staging"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Functions
log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Pre-deployment checks
if [ ! -f "$ENV_FILE" ]; then
    log_error "Environment file $ENV_FILE not found!"
    exit 1
fi

# Pull latest code
log_info "Pulling latest code..."
git pull origin staging

# Backup database
if [ -f "data/staging/witchcityrope_staging.db" ]; then
    log_info "Backing up database..."
    cp data/staging/witchcityrope_staging.db backups/staging_$(date +%Y%m%d_%H%M%S).db
fi

# Build and deploy
log_info "Building Docker images..."
docker-compose -f $COMPOSE_FILE build

log_info "Stopping existing services..."
docker-compose -f $COMPOSE_FILE down

log_info "Starting services..."
docker-compose -f $COMPOSE_FILE up -d

# Wait for services to be healthy
log_info "Waiting for services to be healthy..."
sleep 10

# Run database migrations
log_info "Running database migrations..."
docker exec wcr-staging-api dotnet ef database update

# Verify deployment
log_info "Verifying deployment..."
if curl -f https://api.staging.witchcityrope.com/health > /dev/null 2>&1; then
    log_info "API is healthy"
else
    log_error "API health check failed"
    exit 1
fi

if curl -f https://staging.witchcityrope.com > /dev/null 2>&1; then
    log_info "Web application is healthy"
else
    log_error "Web application health check failed"
    exit 1
fi

log_info "Deployment completed successfully!"
log_info "Staging URL: https://staging.witchcityrope.com"
log_info "API URL: https://api.staging.witchcityrope.com"
log_info "Seq Logs: http://$(hostname -I | awk '{print $1}'):5341"
```

Make executable:

```bash
chmod +x deploy-staging.sh
```

## Monitoring and Maintenance

### 1. Log Monitoring

```bash
# View real-time logs
docker-compose -f docker-compose.staging.yml logs -f

# Check specific service
docker logs wcr-staging-api --tail 100 -f

# Access Seq UI
# http://your-server-ip:5341
```

### 2. Database Maintenance

```bash
# Backup database
./scripts/backup-staging-db.sh

# Vacuum database
docker exec wcr-staging-api sqlite3 /app/data/witchcityrope_staging.db "VACUUM;"

# Check database size
du -h data/staging/witchcityrope_staging.db
```

### 3. Certificate Renewal

```bash
# Add to crontab
0 0 * * 0 /opt/witchcityrope/staging/scripts/renew-staging-cert.sh
```

### 4. Resource Monitoring

```bash
# Monitor Docker resources
docker system df
docker stats

# Clean up unused resources
docker system prune -a
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Check port usage
   sudo netstat -tlnp | grep -E '5651|5653|5341'
   ```

2. **Database Locked**
   ```bash
   # Restart API service
   docker-compose -f docker-compose.staging.yml restart api
   ```

3. **SSL Certificate Issues**
   ```bash
   # Check certificate
   openssl s_client -connect staging.witchcityrope.com:443
   ```

4. **Memory Issues**
   ```bash
   # Check memory usage
   free -h
   docker stats --no-stream
   ```

### Debug Mode

Enable debug logging:

```bash
# Set in .env.staging
SERILOG__MINIMUMLEVEL__DEFAULT=Debug
DETAILED_ERRORS=true

# Restart services
docker-compose -f docker-compose.staging.yml restart
```

## Rollback Procedure

If deployment fails:

```bash
# Stop services
docker-compose -f docker-compose.staging.yml down

# Restore database backup
cp backups/staging_YYYYMMDD_HHMMSS.db data/staging/witchcityrope_staging.db

# Checkout previous version
git checkout HEAD~1

# Redeploy
./deploy-staging.sh
```

## Security Checklist

- [ ] Environment variables secured
- [ ] SSL certificates valid
- [ ] Firewall configured
- [ ] Default passwords changed
- [ ] Debug endpoints protected
- [ ] Backup procedures tested
- [ ] Monitoring alerts configured
- [ ] Access logs reviewed

## Next Steps

After successful staging deployment:

1. Run full test suite
2. Perform user acceptance testing
3. Load test the application
4. Review security scan results
5. Document any issues found
6. Prepare production deployment plan