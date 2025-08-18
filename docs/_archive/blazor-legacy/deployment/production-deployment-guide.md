# Production Deployment Guide - BLAZOR LEGACY
<!-- ARCHIVED: 2025-08-17 - This document is for Blazor Server deployment -->
<!-- STATUS: OBSOLETE - Use React deployment guides for current project -->
<!-- REPLACEMENT: /docs/guides-setup/docker-production-deployment.md -->

> **⚠️ ARCHIVED CONTENT**  
> This document contains Blazor Server deployment procedures.  
> For current React + Docker deployment, see: `/docs/guides-setup/docker-production-deployment.md`

# Original WitchCityRope Production Deployment Guide

## Table of Contents
1. [Production Server Requirements](#production-server-requirements)
2. [Pre-Production Checklist](#pre-production-checklist)
3. [Step-by-Step Deployment Procedures](#step-by-step-deployment-procedures)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Monitoring and Alerting Setup](#monitoring-and-alerting-setup)
6. [Backup and Disaster Recovery Procedures](#backup-and-disaster-recovery-procedures)
7. [Scaling Strategies](#scaling-strategies)
8. [Maintenance Procedures](#maintenance-procedures)

## Production Server Requirements

### Minimum Hardware Requirements
- **CPU**: 4 vCPUs (Intel Xeon or AMD EPYC recommended)
- **RAM**: 8GB minimum, 16GB recommended
- **Storage**: 100GB SSD minimum (NVMe preferred)
- **Network**: 1Gbps connection with dedicated bandwidth
- **Redundancy**: Hot-swappable components where possible

### Software Requirements
- **Operating System**: Ubuntu 22.04 LTS or RHEL 8+
- **Container Runtime**: Docker 24.0+ and Docker Compose 2.20+
- **Web Server**: Nginx 1.24+ (for reverse proxy)
- **Database**: PostgreSQL 15+ (primary database)
- **Cache**: Redis 7.0+ (for session and caching)
- **SSL/TLS**: Let's Encrypt or commercial SSL certificate

### Network Requirements
- **Ports Required**:
  - 80/443 (HTTP/HTTPS)
  - 5432 (PostgreSQL - internal only)
  - 6379 (Redis - internal only)
  - 5000 (API - internal only)
  - 3000 (Web UI - internal only)
- **DNS**: A records for primary domain and subdomain (api.domain.com)
- **Load Balancer**: AWS ALB, Azure Load Balancer, or HAProxy
- **CDN**: CloudFlare, AWS CloudFront, or similar for static assets

### Security Requirements
- **Firewall**: UFW or iptables configured
- **SSL/TLS**: A+ rating on SSL Labs
- **Security Headers**: HSTS, CSP, X-Frame-Options configured
- **WAF**: Web Application Firewall (ModSecurity or cloud-based)
- **DDoS Protection**: CloudFlare or similar service

## Pre-Production Checklist

### Code Preparation
- [ ] All tests passing (unit, integration, E2E)
- [ ] Code coverage meets minimum threshold (80%+)
- [ ] No critical security vulnerabilities (OWASP scan clean)
- [ ] Performance tests meet SLA requirements
- [ ] All features tagged for release
- [ ] Production build created and tested
- [ ] Database migrations tested on staging

### Infrastructure Verification
- [ ] Production servers provisioned and hardened
- [ ] SSL certificates installed and verified
- [ ] DNS records configured and propagated
- [ ] Firewall rules configured and tested
- [ ] Backup systems configured and tested
- [ ] Monitoring agents installed
- [ ] Log aggregation configured

### Configuration Management
- [ ] Production environment variables set
- [ ] Database connection strings secured
- [ ] API keys and secrets stored in vault
- [ ] PayPal production credentials configured
- [ ] Email service production settings verified
- [ ] Redis connection configured
- [ ] JWT signing keys generated

### Documentation Review
- [ ] Deployment runbook reviewed and updated
- [ ] Rollback procedures documented
- [ ] Support contacts list updated
- [ ] Known issues documented
- [ ] API documentation published
- [ ] User guides updated

### Team Readiness
- [ ] Deployment team identified and available
- [ ] On-call schedule confirmed
- [ ] Communication channels established
- [ ] Escalation procedures defined
- [ ] Maintenance window scheduled
- [ ] Stakeholders notified

## Step-by-Step Deployment Procedures

### 1. Pre-Deployment Setup

```bash
# SSH into production server
ssh deploy@production-server.com

# Create deployment directory structure
sudo mkdir -p /opt/witchcityrope/{app,logs,data,backups,configs}
sudo chown -R deploy:deploy /opt/witchcityrope

# Clone production configuration
cd /opt/witchcityrope
git clone https://github.com/yourepo/witchcityrope-configs.git configs
```

### 2. Database Setup

```bash
# Install PostgreSQL
sudo apt update
sudo apt install postgresql-15 postgresql-contrib-15

# Create production database and user
sudo -u postgres psql <<EOF
CREATE USER witchcity_prod WITH PASSWORD 'strong_password_here';
CREATE DATABASE witchcityrope_prod OWNER witchcity_prod;
GRANT ALL PRIVILEGES ON DATABASE witchcityrope_prod TO witchcity_prod;
EOF

# Configure PostgreSQL for production
sudo vim /etc/postgresql/15/main/postgresql.conf
# Set: max_connections = 200
# Set: shared_buffers = 2GB
# Set: effective_cache_size = 6GB

# Enable SSL for PostgreSQL
sudo vim /etc/postgresql/15/main/pg_hba.conf
# Add: hostssl all all 0.0.0.0/0 md5

sudo systemctl restart postgresql
```

### 3. Redis Setup

```bash
# Install Redis
sudo apt install redis-server

# Configure Redis for production
sudo vim /etc/redis/redis.conf
# Set: maxmemory 2gb
# Set: maxmemory-policy allkeys-lru
# Set: requirepass strong_redis_password

# Enable Redis persistence
# Set: save 900 1
# Set: save 300 10
# Set: save 60 10000

sudo systemctl restart redis-server
```

### 4. Docker Setup

```bash
# Install Docker and Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### 5. Application Deployment

```bash
# Copy production environment file
cp configs/production/.env /opt/witchcityrope/app/.env

# Create production docker-compose file
cat > /opt/witchcityrope/app/docker-compose.prod.yml <<EOF
version: '3.8'

services:
  api:
    image: witchcityrope/api:${VERSION}
    container_name: witchcityrope-api
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=witchcityrope_prod;Username=witchcity_prod;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - JWT__Secret=${JWT_SECRET}
      - PayPal__ClientId=${PAYPAL_CLIENT_ID}
      - PayPal__ClientSecret=${PAYPAL_CLIENT_SECRET}
    depends_on:
      - db
      - redis
    networks:
      - witchcity-network
    volumes:
      - ./logs/api:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    image: witchcityrope/web:${VERSION}
    container_name: witchcityrope-web
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ApiUrl=http://api:5000
    depends_on:
      - api
    networks:
      - witchcity-network
    volumes:
      - ./logs/web:/app/logs

  nginx:
    image: nginx:alpine
    container_name: witchcityrope-nginx
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./configs/nginx/production.conf:/etc/nginx/nginx.conf:ro
      - ./configs/ssl:/etc/nginx/ssl:ro
      - ./logs/nginx:/var/log/nginx
    depends_on:
      - api
      - web
    networks:
      - witchcity-network

  db:
    image: postgres:15-alpine
    container_name: witchcityrope-db
    restart: always
    environment:
      - POSTGRES_USER=witchcity_prod
      - POSTGRES_PASSWORD=${DB_PASSWORD}
      - POSTGRES_DB=witchcityrope_prod
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./backups/postgres:/backups
    networks:
      - witchcity-network

  redis:
    image: redis:7-alpine
    container_name: witchcityrope-redis
    restart: always
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    networks:
      - witchcity-network

volumes:
  postgres-data:
  redis-data:

networks:
  witchcity-network:
    driver: bridge
EOF

# Deploy application
cd /opt/witchcityrope/app
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

# Run database migrations
docker exec witchcityrope-api dotnet ef database update
```

### 6. SSL/TLS Configuration

```bash
# Install Certbot
sudo snap install --classic certbot
sudo ln -s /snap/bin/certbot /usr/bin/certbot

# Obtain SSL certificate
sudo certbot certonly --standalone -d witchcityrope.com -d www.witchcityrope.com -d api.witchcityrope.com

# Configure auto-renewal
sudo systemctl enable snap.certbot.renew.timer
sudo systemctl start snap.certbot.renew.timer
```

### 7. Nginx Configuration

```nginx
# /opt/witchcityrope/configs/nginx/production.conf
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self' https:; script-src 'self' 'unsafe-inline' 'unsafe-eval' https:; style-src 'self' 'unsafe-inline' https:;" always;

    # SSL Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=web_limit:10m rate=20r/s;

    # API server
    upstream api_backend {
        server api:5000;
        keepalive 32;
    }

    # Web server
    upstream web_backend {
        server web:3000;
        keepalive 32;
    }

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name witchcityrope.com www.witchcityrope.com api.witchcityrope.com;
        return 301 https://$host$request_uri;
    }

    # API server
    server {
        listen 443 ssl http2;
        server_name api.witchcityrope.com;

        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;

        location / {
            limit_req zone=api_limit burst=20 nodelay;
            
            proxy_pass http://api_backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        location /health {
            access_log off;
            proxy_pass http://api_backend/health;
        }
    }

    # Web application
    server {
        listen 443 ssl http2;
        server_name witchcityrope.com www.witchcityrope.com;

        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;

        # HSTS
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

        location / {
            limit_req zone=web_limit burst=50 nodelay;
            
            proxy_pass http://web_backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Static assets with caching
        location ~* \.(jpg|jpeg|png|gif|ico|css|js|pdf|woff|woff2|ttf|svg)$ {
            proxy_pass http://web_backend;
            expires 30d;
            add_header Cache-Control "public, immutable";
        }
    }
}
```

## Post-Deployment Verification

### 1. Health Checks

```bash
# Check container status
docker ps -a

# Check API health endpoint
curl -f https://api.witchcityrope.com/health

# Check web application
curl -f https://witchcityrope.com

# Check database connectivity
docker exec witchcityrope-api dotnet exec /app/healthcheck.dll

# Check Redis connectivity
docker exec witchcityrope-redis redis-cli ping
```

### 2. Functional Testing

```bash
# Run smoke tests
cd /opt/witchcityrope/tests
./run-smoke-tests.sh production

# Test critical user flows
- User registration
- User login
- Event creation
- Event registration
- Payment processing
- Email notifications
```

### 3. Performance Verification

```bash
# Run load test
k6 run --vus 50 --duration 5m loadtest.js

# Check response times
curl -w "@curl-format.txt" -o /dev/null -s https://api.witchcityrope.com/api/events

# Monitor resource usage
docker stats --no-stream
htop
```

### 4. Security Verification

```bash
# SSL/TLS verification
curl -vI https://witchcityrope.com

# Check security headers
curl -I https://witchcityrope.com | grep -E "(X-Frame-Options|X-Content-Type-Options|Strict-Transport-Security)"

# Run security scan
docker run --rm owasp/zap2docker-stable zap-baseline.py -t https://witchcityrope.com
```

## Monitoring and Alerting Setup

### 1. Application Monitoring

```bash
# Install Prometheus
docker run -d \
  --name prometheus \
  -p 9090:9090 \
  -v /opt/witchcityrope/configs/prometheus:/etc/prometheus \
  prom/prometheus

# Install Grafana
docker run -d \
  --name grafana \
  -p 3001:3000 \
  -e "GF_SECURITY_ADMIN_PASSWORD=admin_password" \
  -v grafana-storage:/var/lib/grafana \
  grafana/grafana
```

### 2. Log Management

```bash
# Install ELK Stack
docker-compose -f elk-stack.yml up -d

# Configure Filebeat
cat > /opt/witchcityrope/configs/filebeat/filebeat.yml <<EOF
filebeat.inputs:
- type: log
  enabled: true
  paths:
    - /opt/witchcityrope/logs/api/*.log
    - /opt/witchcityrope/logs/web/*.log
    - /opt/witchcityrope/logs/nginx/*.log

output.elasticsearch:
  hosts: ["localhost:9200"]
  username: "elastic"
  password: "changeme"
EOF
```

### 3. Alerting Configuration

```yaml
# /opt/witchcityrope/configs/prometheus/alerts.yml
groups:
  - name: witchcityrope_alerts
    rules:
      - alert: HighCPUUsage
        expr: rate(process_cpu_seconds_total[5m]) * 100 > 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High CPU usage detected"
          description: "CPU usage is above 80% for 5 minutes"

      - alert: HighMemoryUsage
        expr: (1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100 > 85
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High memory usage detected"
          description: "Memory usage is above 85% for 5 minutes"

      - alert: APIDown
        expr: up{job="api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "API is down"
          description: "API service has been down for 1 minute"

      - alert: DatabaseConnectionFailure
        expr: postgres_up == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Database connection failure"
          description: "Cannot connect to PostgreSQL database"

      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          description: "Error rate is above 5% for 5 minutes"
```

### 4. Uptime Monitoring

```bash
# Install Uptime Kuma
docker run -d \
  --name uptime-kuma \
  -p 3002:3001 \
  -v uptime-kuma:/app/data \
  louislam/uptime-kuma:1

# Configure monitors for:
# - https://witchcityrope.com
# - https://api.witchcityrope.com/health
# - Database port 5432
# - Redis port 6379
```

## Backup and Disaster Recovery Procedures

### 1. Automated Backup Setup

```bash
# Create backup script
cat > /opt/witchcityrope/scripts/backup.sh <<'EOF'
#!/bin/bash
set -e

BACKUP_DIR="/opt/witchcityrope/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Backup database
docker exec witchcityrope-db pg_dump -U witchcity_prod witchcityrope_prod | gzip > "$BACKUP_DIR/db_$DATE.sql.gz"

# Backup Redis
docker exec witchcityrope-redis redis-cli --rdb /data/dump.rdb
cp /var/lib/docker/volumes/witchcityrope_redis-data/_data/dump.rdb "$BACKUP_DIR/redis_$DATE.rdb"

# Backup application files
tar -czf "$BACKUP_DIR/app_$DATE.tar.gz" /opt/witchcityrope/app

# Backup configurations
tar -czf "$BACKUP_DIR/configs_$DATE.tar.gz" /opt/witchcityrope/configs

# Upload to S3
aws s3 sync "$BACKUP_DIR" s3://witchcityrope-backups/production/

# Clean up old local backups (keep 7 days)
find "$BACKUP_DIR" -name "*.gz" -mtime +7 -delete
find "$BACKUP_DIR" -name "*.rdb" -mtime +7 -delete
EOF

chmod +x /opt/witchcityrope/scripts/backup.sh

# Schedule backups
crontab -e
# Add: 0 2 * * * /opt/witchcityrope/scripts/backup.sh >> /opt/witchcityrope/logs/backup.log 2>&1
```

### 2. Disaster Recovery Plan

```bash
# Recovery script
cat > /opt/witchcityrope/scripts/recover.sh <<'EOF'
#!/bin/bash
set -e

if [ $# -eq 0 ]; then
    echo "Usage: $0 <backup_date>"
    exit 1
fi

BACKUP_DATE=$1
BACKUP_DIR="/opt/witchcityrope/backups"

# Stop services
docker-compose -f /opt/witchcityrope/app/docker-compose.prod.yml down

# Restore database
gunzip < "$BACKUP_DIR/db_$BACKUP_DATE.sql.gz" | docker exec -i witchcityrope-db psql -U witchcity_prod witchcityrope_prod

# Restore Redis
docker cp "$BACKUP_DIR/redis_$BACKUP_DATE.rdb" witchcityrope-redis:/data/dump.rdb
docker restart witchcityrope-redis

# Restore application files
tar -xzf "$BACKUP_DIR/app_$BACKUP_DATE.tar.gz" -C /

# Restore configurations
tar -xzf "$BACKUP_DIR/configs_$BACKUP_DATE.tar.gz" -C /

# Start services
docker-compose -f /opt/witchcityrope/app/docker-compose.prod.yml up -d

echo "Recovery completed for backup date: $BACKUP_DATE"
EOF

chmod +x /opt/witchcityrope/scripts/recover.sh
```

### 3. Backup Testing

```bash
# Monthly backup restore test
# 1. Spin up test environment
# 2. Restore latest backup
# 3. Run verification tests
# 4. Document results

cat > /opt/witchcityrope/scripts/test-backup.sh <<'EOF'
#!/bin/bash
set -e

# Create test environment
docker-compose -f docker-compose.test.yml up -d

# Get latest backup
LATEST_BACKUP=$(ls -t /opt/witchcityrope/backups/db_*.sql.gz | head -1)

# Restore to test environment
gunzip < "$LATEST_BACKUP" | docker exec -i witchcityrope-test-db psql -U test_user test_db

# Run verification
docker exec witchcityrope-test-api dotnet test --filter "Category=BackupVerification"

# Clean up
docker-compose -f docker-compose.test.yml down -v

echo "Backup test completed successfully"
EOF
```

## Scaling Strategies

### 1. Horizontal Scaling

```yaml
# docker-compose.scale.yml
version: '3.8'

services:
  api:
    image: witchcityrope/api:${VERSION}
    deploy:
      replicas: 3
      update_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    networks:
      - witchcity-network

  web:
    image: witchcityrope/web:${VERSION}
    deploy:
      replicas: 2
      update_config:
        parallelism: 1
        delay: 10s
    networks:
      - witchcity-network
```

### 2. Database Scaling

```bash
# Set up read replicas
docker run -d \
  --name postgres-replica \
  -e POSTGRES_REPLICATION_MODE=slave \
  -e POSTGRES_MASTER_HOST=postgres-primary \
  -e POSTGRES_REPLICATION_USER=replicator \
  -e POSTGRES_REPLICATION_PASSWORD=rep_password \
  postgres:15-alpine

# Configure connection pooling
cat > /opt/witchcityrope/configs/pgbouncer.ini <<EOF
[databases]
witchcityrope_prod = host=localhost port=5432 dbname=witchcityrope_prod

[pgbouncer]
listen_port = 6432
listen_addr = *
auth_type = md5
auth_file = /etc/pgbouncer/userlist.txt
pool_mode = transaction
max_client_conn = 1000
default_pool_size = 25
EOF
```

### 3. Caching Strategy

```yaml
# Redis cluster configuration
redis-master:
  image: redis:7-alpine
  command: redis-server --appendonly yes
  volumes:
    - redis-master-data:/data

redis-slave:
  image: redis:7-alpine
  command: redis-server --slaveof redis-master 6379
  depends_on:
    - redis-master
  volumes:
    - redis-slave-data:/data

redis-sentinel:
  image: redis:7-alpine
  command: redis-sentinel /etc/redis/sentinel.conf
  volumes:
    - ./configs/redis/sentinel.conf:/etc/redis/sentinel.conf
```

### 4. Auto-Scaling Configuration

```bash
# Kubernetes HPA configuration
cat > hpa.yaml <<EOF
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: witchcityrope-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: witchcityrope-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
EOF
```

## Maintenance Procedures

### 1. Regular Maintenance Tasks

```bash
# Weekly maintenance script
cat > /opt/witchcityrope/scripts/weekly-maintenance.sh <<'EOF'
#!/bin/bash
set -e

echo "Starting weekly maintenance - $(date)"

# 1. Database maintenance
docker exec witchcityrope-db psql -U witchcity_prod -d witchcityrope_prod -c "VACUUM ANALYZE;"
docker exec witchcityrope-db psql -U witchcity_prod -d witchcityrope_prod -c "REINDEX DATABASE witchcityrope_prod;"

# 2. Clear old logs
find /opt/witchcityrope/logs -name "*.log" -mtime +30 -delete

# 3. Update container images
docker-compose -f /opt/witchcityrope/app/docker-compose.prod.yml pull

# 4. Clean up Docker resources
docker system prune -af --volumes

# 5. Check disk space
df -h | grep -E "(^/dev/|^Filesystem)"

# 6. Check for security updates
apt update && apt list --upgradable

echo "Weekly maintenance completed - $(date)"
EOF

chmod +x /opt/witchcityrope/scripts/weekly-maintenance.sh

# Schedule weekly maintenance
crontab -e
# Add: 0 3 * * 0 /opt/witchcityrope/scripts/weekly-maintenance.sh >> /opt/witchcityrope/logs/maintenance.log 2>&1
```

### 2. Zero-Downtime Deployment

```bash
# Blue-green deployment script
cat > /opt/witchcityrope/scripts/deploy-blue-green.sh <<'EOF'
#!/bin/bash
set -e

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

# Determine current environment
CURRENT=$(docker ps --format "table {{.Names}}" | grep -E "(blue|green)" | head -1 | grep -o -E "(blue|green)")
if [ "$CURRENT" = "blue" ]; then
    NEW="green"
else
    NEW="blue"
fi

echo "Deploying $VERSION to $NEW environment (current: $CURRENT)"

# Deploy to new environment
export DEPLOYMENT_COLOR=$NEW
docker-compose -f docker-compose.$NEW.yml pull
docker-compose -f docker-compose.$NEW.yml up -d

# Wait for health checks
echo "Waiting for health checks..."
sleep 30

# Check health
if ! curl -f http://localhost:5001/health; then
    echo "Health check failed, rolling back"
    docker-compose -f docker-compose.$NEW.yml down
    exit 1
fi

# Switch traffic
echo "Switching traffic to $NEW environment"
sed -i "s/upstream_$CURRENT/upstream_$NEW/g" /opt/witchcityrope/configs/nginx/production.conf
docker exec witchcityrope-nginx nginx -s reload

# Stop old environment after 5 minutes
echo "Scheduling old environment shutdown"
at now + 5 minutes <<< "docker-compose -f docker-compose.$CURRENT.yml down"

echo "Deployment completed successfully"
EOF
```

### 3. Database Maintenance

```sql
-- Monthly database optimization
-- /opt/witchcityrope/scripts/db-optimization.sql

-- Update statistics
ANALYZE;

-- Rebuild indexes
REINDEX DATABASE witchcityrope_prod;

-- Find and fix bloated tables
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS external_size
FROM pg_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 20;

-- Vacuum full on large tables (during maintenance window)
VACUUM FULL ANALYZE events;
VACUUM FULL ANALYZE registrations;
VACUUM FULL ANALYZE users;

-- Check for unused indexes
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE idx_scan = 0
AND schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_relation_size(indexrelid) DESC;
```

### 4. Security Updates

```bash
# Security update procedure
cat > /opt/witchcityrope/scripts/security-updates.sh <<'EOF'
#!/bin/bash
set -e

echo "Starting security update check - $(date)"

# Update package lists
apt update

# Check for security updates
SECURITY_UPDATES=$(apt list --upgradable 2>/dev/null | grep -i security | wc -l)

if [ $SECURITY_UPDATES -gt 0 ]; then
    echo "Found $SECURITY_UPDATES security updates"
    
    # Send alert
    curl -X POST https://alerts.witchcityrope.com/webhook \
        -H "Content-Type: application/json" \
        -d "{\"text\": \"Security updates available on production server: $SECURITY_UPDATES packages\"}"
    
    # Log updates
    apt list --upgradable 2>/dev/null | grep -i security >> /opt/witchcityrope/logs/security-updates.log
fi

# Check Docker images for vulnerabilities
docker images --format "table {{.Repository}}:{{.Tag}}" | tail -n +2 | while read image; do
    echo "Scanning $image for vulnerabilities..."
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
        aquasec/trivy image --severity HIGH,CRITICAL "$image"
done

echo "Security update check completed - $(date)"
EOF
```

### 5. Performance Tuning

```bash
# Performance monitoring and tuning
cat > /opt/witchcityrope/scripts/performance-tune.sh <<'EOF'
#!/bin/bash

# Monitor slow queries
docker exec witchcityrope-db psql -U witchcity_prod -d witchcityrope_prod -c "
SELECT 
    query,
    calls,
    total_time,
    mean,
    max_time,
    stddev_time
FROM pg_stat_statements
WHERE mean > 100
ORDER BY mean DESC
LIMIT 20;"

# Check connection pool efficiency
docker exec witchcityrope-api cat /app/logs/connection-pool.log | \
    grep -E "(pool exhausted|timeout)" | wc -l

# Analyze cache hit rates
docker exec witchcityrope-redis redis-cli info stats | grep -E "(keyspace_hits|keyspace_misses)"

# Generate performance report
echo "Performance Report - $(date)" > /opt/witchcityrope/logs/performance-report.txt
echo "======================" >> /opt/witchcityrope/logs/performance-report.txt
docker stats --no-stream >> /opt/witchcityrope/logs/performance-report.txt
EOF
```

## Troubleshooting Guide

### Common Issues and Solutions

1. **High Memory Usage**
   ```bash
   # Check memory consumers
   docker stats --no-stream
   ps aux --sort=-%mem | head
   
   # Clear caches if needed
   docker exec witchcityrope-redis redis-cli FLUSHDB
   ```

2. **Database Connection Issues**
   ```bash
   # Check connection count
   docker exec witchcityrope-db psql -U witchcity_prod -c "SELECT count(*) FROM pg_stat_activity;"
   
   # Kill idle connections
   docker exec witchcityrope-db psql -U witchcity_prod -c "
   SELECT pg_terminate_backend(pid) 
   FROM pg_stat_activity 
   WHERE state = 'idle' AND state_change < now() - interval '5 minutes';"
   ```

3. **API Performance Issues**
   ```bash
   # Enable detailed logging
   docker exec witchcityrope-api sed -i 's/LogLevel:Information/LogLevel:Debug/' appsettings.json
   docker restart witchcityrope-api
   
   # Check response times
   curl -w "@curl-format.txt" -o /dev/null -s https://api.witchcityrope.com/api/events
   ```

## Emergency Contacts

- **Primary On-Call**: +1-XXX-XXX-XXXX
- **Secondary On-Call**: +1-XXX-XXX-XXXX
- **Database Admin**: dba@witchcityrope.com
- **Security Team**: security@witchcityrope.com
- **Infrastructure Team**: infrastructure@witchcityrope.com

## Conclusion

This production deployment guide provides comprehensive procedures for deploying, maintaining, and scaling the WitchCityRope application in a production environment. Regular review and updates of these procedures ensure optimal performance, security, and reliability of the platform.

Remember to:
- Always test changes in staging before production
- Maintain regular backups and test recovery procedures
- Monitor system health and performance continuously
- Keep security patches and updates current
- Document any deviations from standard procedures

For additional support or questions, contact the infrastructure team.