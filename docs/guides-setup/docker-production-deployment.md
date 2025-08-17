# Docker Production Deployment Guide - WitchCityRope
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This guide provides step-by-step instructions for deploying WitchCityRope's React + .NET API authentication system to production using Docker containers. The deployment includes the proven Hybrid JWT + HttpOnly Cookies authentication pattern validated in the vertical slice implementation.

## Prerequisites

### Server Requirements

#### Minimum Hardware Specifications
- **CPU**: 4 cores (2.4 GHz)
- **RAM**: 8 GB
- **Storage**: 100 GB SSD (with expansion for user data growth)
- **Network**: 100 Mbps dedicated bandwidth

#### Recommended Hardware Specifications
- **CPU**: 8 cores (3.0 GHz)
- **RAM**: 16 GB
- **Storage**: 500 GB SSD with backup storage
- **Network**: 1 Gbps dedicated bandwidth

#### Operating System Support
- **Primary**: Ubuntu 22.04 LTS or Ubuntu 24.04 LTS
- **Alternative**: RHEL 9, CentOS Stream 9, Debian 12
- **Container Runtime**: Docker CE 24.0+ and Docker Compose v2.20+

### Software Prerequisites

#### Docker Installation
```bash
# Ubuntu/Debian installation
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Install Docker Compose v2
sudo apt-get update
sudo apt-get install docker-compose-plugin

# Verify installation
docker --version
docker compose version
```

#### SSL Certificate Requirements
- **Domain**: Valid SSL certificate for your domain (Let's Encrypt or commercial)
- **Files Required**: 
  - `ssl_certificate.pem` (full certificate chain)
  - `ssl_private_key.pem` (private key)
- **Permissions**: Certificate files must be readable by nginx user (101:101)

#### Network Configuration
- **Firewall Ports**: 80 (HTTP), 443 (HTTPS), 22 (SSH)
- **Domain DNS**: A records pointing to server IP
- **Load Balancer**: Optional but recommended for high availability

## Deployment Steps

### Step 1: Server Preparation

#### Clone Repository
```bash
# Create deployment directory
sudo mkdir -p /opt/witchcityrope
sudo chown $USER:$USER /opt/witchcityrope
cd /opt/witchcityrope

# Clone production branch
git clone https://github.com/DarkMonkDev/WitchCityRope.git .
git checkout main  # or specific release tag
```

#### Create Directory Structure
```bash
# Create required directories
mkdir -p {config,ssl,backups,logs}
mkdir -p config/{nginx,postgres,api}
mkdir -p ssl/letsencrypt
mkdir -p logs/{nginx,api,postgres}

# Set appropriate permissions
chmod 755 config ssl backups logs
chmod 700 ssl/letsencrypt
```

#### System User Setup
```bash
# Create service user for enhanced security
sudo useradd -r -s /bin/false witchcity
sudo usermod -aG docker witchcity

# Create data directories with proper ownership
sudo mkdir -p /var/lib/witchcityrope/{postgres,uploads,cache}
sudo chown -R witchcity:witchcity /var/lib/witchcityrope
```

### Step 2: Configuration Setup

#### Environment Configuration
```bash
# Copy example environment file
cp .env.example .env.production

# Edit production environment variables
nano .env.production
```

#### Production Environment Variables (.env.production)
```bash
# Production Environment Configuration
NODE_ENV=production
ASPNETCORE_ENVIRONMENT=Production

# Database Configuration
POSTGRES_DB=witchcityrope
POSTGRES_USER=witchcity_prod
POSTGRES_PASSWORD=CHANGE_THIS_SECURE_PASSWORD_123!

# JWT Authentication (generate strong secrets)
JWT_SECRET=CHANGE_THIS_TO_VERY_LONG_RANDOM_STRING_256_BITS
JWT_ISSUER=https://api.yourdomain.com
JWT_AUDIENCE=witchcityrope-app

# Domain Configuration
DOMAIN_NAME=yourdomain.com
API_DOMAIN=api.yourdomain.com
ALLOWED_ORIGINS=https://yourdomain.com

# API Configuration
VITE_API_BASE_URL=https://api.yourdomain.com

# Security Configuration
AUTHENTICATION__REQUIREHTTPS=true
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Resource Configuration
POSTGRES_MAX_CONNECTIONS=100
POSTGRES_SHARED_BUFFERS=256MB
POSTGRES_EFFECTIVE_CACHE_SIZE=1GB
```

#### Generate Secure Secrets
```bash
# Generate strong PostgreSQL password
openssl rand -base64 32

# Generate JWT secret (256-bit)
openssl rand -base64 64

# Store secrets securely
echo "postgres_password_here" | sudo tee /run/secrets/postgres_password
echo "jwt_secret_here" | sudo tee /run/secrets/jwt_secret
sudo chmod 600 /run/secrets/*
```

#### SSL Certificate Setup

##### Option A: Let's Encrypt (Recommended)
```bash
# Install Certbot
sudo apt-get install certbot

# Obtain certificates
sudo certbot certonly --standalone \
  -d yourdomain.com \
  -d api.yourdomain.com \
  --email admin@yourdomain.com \
  --agree-tos --no-eff-email

# Copy certificates to project
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem ssl/ssl_certificate.pem
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem ssl/ssl_private_key.pem
sudo chown 101:101 ssl/ssl_*.pem
```

##### Option B: Commercial Certificate
```bash
# Copy your certificate files
cp your_certificate.pem ssl/ssl_certificate.pem
cp your_private_key.pem ssl/ssl_private_key.pem
sudo chown 101:101 ssl/ssl_*.pem
chmod 644 ssl/ssl_certificate.pem
chmod 600 ssl/ssl_private_key.pem
```

#### Nginx Configuration
```bash
# Create production nginx configuration
cat > config/nginx/nginx.conf << 'EOF'
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
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    
    # Compression
    gzip on;
    gzip_vary on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml;
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=auth:10m rate=5r/s;
    
    include /etc/nginx/conf.d/*.conf;
}
EOF

# Create site configuration
cat > config/nginx/conf.d/witchcityrope.conf << 'EOF'
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name yourdomain.com api.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

# Main application (React)
server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /etc/nginx/ssl/ssl_certificate.pem;
    ssl_certificate_key /etc/nginx/ssl/ssl_private_key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_prefer_server_ciphers off;
    
    location / {
        proxy_pass http://react-web:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

# API service
server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;
    
    ssl_certificate /etc/nginx/ssl/ssl_certificate.pem;
    ssl_certificate_key /etc/nginx/ssl/ssl_private_key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_prefer_server_ciphers off;
    
    # API rate limiting
    location /api/auth/ {
        limit_req zone=auth burst=10 nodelay;
        proxy_pass http://api-service:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location / {
        limit_req zone=api burst=20 nodelay;
        proxy_pass http://api-service:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF
```

### Step 3: Production Deployment

#### Pre-deployment Validation
```bash
# Validate configuration files
docker compose -f docker-compose.yml -f docker-compose.prod.yml config

# Check system resources
free -h
df -h
docker system df

# Verify SSL certificates
openssl x509 -in ssl/ssl_certificate.pem -text -noout
```

#### Build and Deploy
```bash
# Pull latest images
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull

# Build production images
docker compose -f docker-compose.yml -f docker-compose.prod.yml build --no-cache

# Deploy to production
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Monitor startup
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f
```

#### Initial Database Setup
```bash
# Wait for database to be ready
sleep 30

# Run initial migrations
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service dotnet ef database update

# Create initial admin user (optional)
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service dotnet run --seed-admin-user
```

#### Deployment Verification
```bash
# Check service health
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# Test endpoints
curl -f https://yourdomain.com/health
curl -f https://api.yourdomain.com/health

# Verify authentication endpoints
curl -f https://api.yourdomain.com/api/auth/health

# Check SSL certificate
curl -I https://yourdomain.com
```

## Production Configuration

### Environment Variables Security

#### Secret Management with Docker Secrets
```bash
# Create Docker secrets
echo "your_postgres_password" | docker secret create postgres_password_v1 -
echo "your_jwt_secret" | docker secret create jwt_secret_v1 -
echo "$(cat ssl/ssl_certificate.pem)" | docker secret create ssl_cert_v1 -
echo "$(cat ssl/ssl_private_key.pem)" | docker secret create ssl_key_v1 -

# List created secrets
docker secret ls
```

#### Environment Variable Validation
```bash
# Check environment variables are loaded correctly
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service env | grep -E "(ASPNETCORE|Authentication|ConnectionStrings)"

# Verify database connection
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db pg_isready -U witchcity_prod -d witchcityrope
```

### SSL/TLS Configuration

#### Certificate Management
```bash
# Set up automatic certificate renewal
sudo crontab -e
# Add line: 0 2 * * * certbot renew --quiet && docker compose -f /opt/witchcityrope/docker-compose.yml -f /opt/witchcityrope/docker-compose.prod.yml restart nginx-proxy
```

#### SSL Security Validation
```bash
# Test SSL configuration with SSL Labs
# Visit: https://www.ssllabs.com/ssltest/

# Test SSL locally
openssl s_client -connect yourdomain.com:443 -servername yourdomain.com

# Verify HSTS headers
curl -I https://yourdomain.com | grep -i strict-transport-security
```

### Security Hardening

#### Container Security
```bash
# Audit container security
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image witchcityrope/api:latest

# Check container privileges
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service whoami
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db whoami
```

#### Network Security
```bash
# Verify network isolation
docker network inspect witchcity-net

# Check open ports
sudo netstat -tulpn | grep -E "(80|443|5432)"

# Verify firewall rules
sudo ufw status
```

#### File System Security
```bash
# Check file permissions
ls -la ssl/
ls -la config/
ls -la /var/lib/witchcityrope/

# Verify read-only mounts
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service mount | grep -E "(ro|read-only)"
```

## Database Setup

### Database Initialization

#### Initial Migration and Seeding
```bash
# Apply all migrations
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service \
  dotnet ef database update --connection "Host=postgres-db;Database=witchcityrope;Username=witchcity_prod;Password=$POSTGRES_PASSWORD"

# Verify tables created
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  psql -U witchcity_prod -d witchcityrope -c "\dt"

# Check initial data
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  psql -U witchcity_prod -d witchcityrope -c "SELECT COUNT(*) FROM AspNetRoles;"
```

### Database Backup Strategy

#### Automated Backup Setup
```bash
# Create backup script
cat > scripts/backup-database.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="/opt/witchcityrope/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="witchcityrope_backup_${TIMESTAMP}.sql"

# Create backup
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec -T postgres-db \
  pg_dump -U witchcity_prod -d witchcityrope > "${BACKUP_DIR}/${BACKUP_FILE}"

# Compress backup
gzip "${BACKUP_DIR}/${BACKUP_FILE}"

# Remove backups older than 30 days
find "${BACKUP_DIR}" -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: ${BACKUP_FILE}.gz"
EOF

chmod +x scripts/backup-database.sh

# Schedule daily backups
sudo crontab -e
# Add line: 0 2 * * * /opt/witchcityrope/scripts/backup-database.sh
```

#### Backup Restoration
```bash
# Restore from backup
gunzip backups/witchcityrope_backup_YYYYMMDD_HHMMSS.sql.gz
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec -T postgres-db \
  psql -U witchcity_prod -d witchcityrope < backups/witchcityrope_backup_YYYYMMDD_HHMMSS.sql
```

### Database Performance Tuning

#### PostgreSQL Configuration
```bash
# Create optimized PostgreSQL configuration
cat > config/postgres/postgresql.conf << 'EOF'
# Production PostgreSQL Configuration
max_connections = 100
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
random_page_cost = 1.1
effective_io_concurrency = 200
work_mem = 4MB
min_wal_size = 1GB
max_wal_size = 4GB

# Logging
log_destination = 'stderr'
logging_collector = on
log_directory = 'log'
log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log'
log_min_duration_statement = 1000
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
EOF
```

## Monitoring and Health Checks

### System Monitoring

#### Health Check Endpoints
```bash
# Application health checks
curl -f https://yourdomain.com/health
curl -f https://api.yourdomain.com/health
curl -f https://api.yourdomain.com/api/auth/health

# Database health check
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  pg_isready -U witchcity_prod -d witchcityrope
```

#### Performance Monitoring Script
```bash
# Create monitoring script
cat > scripts/system-monitor.sh << 'EOF'
#!/bin/bash
echo "=== WitchCityRope System Status ==="
echo "Date: $(date)"
echo

echo "=== Container Status ==="
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

echo "=== Resource Usage ==="
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"

echo "=== Health Checks ==="
curl -f -s https://yourdomain.com/health > /dev/null && echo "✅ Web: Healthy" || echo "❌ Web: Failed"
curl -f -s https://api.yourdomain.com/health > /dev/null && echo "✅ API: Healthy" || echo "❌ API: Failed"

echo "=== Database Status ==="
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db pg_isready -U witchcity_prod -d witchcityrope

echo "=== Disk Usage ==="
df -h /var/lib/witchcityrope
du -sh /opt/witchcityrope/logs/*

echo "=== Recent Errors ==="
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs --tail=20 | grep -i error
EOF

chmod +x scripts/system-monitor.sh
```

### Log Management

#### Centralized Logging Setup
```bash
# Configure log rotation
sudo tee /etc/logrotate.d/witchcityrope << 'EOF'
/opt/witchcityrope/logs/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    sharedscripts
    postrotate
        docker compose -f /opt/witchcityrope/docker-compose.yml -f /opt/witchcityrope/docker-compose.prod.yml restart nginx-proxy
    endscript
}
EOF
```

#### Log Analysis Commands
```bash
# View real-time logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f

# View authentication logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs api-service | grep -i auth

# View error logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs | grep -i error

# Monitor access patterns
tail -f logs/nginx/access.log | grep -E "(POST|PUT|DELETE)"
```

## Security Checklist

### Pre-Deployment Security Review

#### ✅ Environment Security
- [ ] Strong passwords generated for all services
- [ ] JWT secrets are 256-bit or longer
- [ ] SSL certificates are valid and properly configured
- [ ] Environment files contain no hardcoded secrets
- [ ] Docker secrets are properly configured

#### ✅ Network Security
- [ ] Firewall configured to allow only necessary ports (80, 443, 22)
- [ ] Database not accessible externally
- [ ] HTTPS enforced for all connections
- [ ] CORS properly configured for production domain only
- [ ] Rate limiting configured for API endpoints

#### ✅ Container Security
- [ ] Containers run as non-root users
- [ ] Read-only file systems where possible
- [ ] Security options configured (no-new-privileges)
- [ ] Resource limits applied
- [ ] Container images scanned for vulnerabilities

#### ✅ Authentication Security
- [ ] HTTPS required for all authentication endpoints
- [ ] JWT tokens have appropriate expiration times
- [ ] HttpOnly cookies configured correctly
- [ ] CSRF protection enabled
- [ ] Password policies enforced

#### ✅ Data Security
- [ ] Database encrypted at rest
- [ ] Backup encryption enabled
- [ ] Log files do not contain sensitive data
- [ ] File permissions properly set
- [ ] Audit logging enabled

### Post-Deployment Security Validation

#### Security Scanning
```bash
# Container vulnerability scan
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image witchcityrope/api:latest

# SSL/TLS assessment
testssl.sh https://yourdomain.com

# HTTP security headers check
curl -I https://yourdomain.com | grep -E "(X-Frame-Options|X-Content-Type-Options|Strict-Transport-Security)"
```

#### Penetration Testing
```bash
# Basic security assessment
nmap -sS -O yourdomain.com

# Web application testing (use carefully)
nikto -h https://yourdomain.com

# Authentication endpoint testing
curl -X POST https://api.yourdomain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"invalid","password":"invalid"}' \
  --fail --show-error
```

## Operational Procedures

### Updates and Maintenance

#### Application Updates
```bash
# Pull latest code
git fetch origin
git checkout v1.x.x  # specific version tag

# Build new images
docker compose -f docker-compose.yml -f docker-compose.prod.yml build --no-cache

# Rolling update (zero-downtime)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps api-service
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps react-web

# Verify update
curl -f https://api.yourdomain.com/health
```

#### Database Migration Updates
```bash
# Backup before migration
./scripts/backup-database.sh

# Run new migrations
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service \
  dotnet ef database update

# Verify migration success
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  psql -U witchcity_prod -d witchcityrope -c "\dt"
```

#### System Updates
```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Update Docker
sudo apt update && sudo apt install docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Restart containers after system update
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart
```

### Troubleshooting Guide

#### Common Issues and Solutions

##### Issue: Container Won't Start
```bash
# Check container logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs <service-name>

# Check resource constraints
docker system df
free -h

# Restart specific service
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart <service-name>
```

##### Issue: Database Connection Failed
```bash
# Check database health
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  pg_isready -U witchcity_prod -d witchcityrope

# Verify connection string
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec api-service \
  env | grep ConnectionStrings

# Check database logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs postgres-db
```

##### Issue: SSL Certificate Problems
```bash
# Check certificate validity
openssl x509 -in ssl/ssl_certificate.pem -noout -dates

# Renew Let's Encrypt certificate
sudo certbot renew --force-renewal

# Restart nginx after certificate update
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart nginx-proxy
```

##### Issue: High Memory Usage
```bash
# Check container resource usage
docker stats --no-stream

# Increase resource limits in docker-compose.prod.yml
# Add swap if needed
sudo swapon --show
```

#### Performance Troubleshooting
```bash
# Monitor API response times
while true; do
  curl -w "API Response: %{time_total}s\n" -o /dev/null -s https://api.yourdomain.com/health
  sleep 5
done

# Check database performance
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec postgres-db \
  psql -U witchcity_prod -d witchcityrope -c "SELECT * FROM pg_stat_activity;"

# Monitor container resources
watch -n 1 'docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"'
```

## Scaling Considerations

### Horizontal Scaling Preparation

#### Load Balancer Configuration
```bash
# Example HAProxy configuration for multiple API instances
cat > config/haproxy/haproxy.cfg << 'EOF'
global
    daemon

defaults
    mode http
    timeout connect 5s
    timeout client 30s
    timeout server 30s

frontend api_frontend
    bind *:8080
    default_backend api_servers

backend api_servers
    balance roundrobin
    option httpchk GET /health
    server api1 api-service-1:8080 check
    server api2 api-service-2:8080 check
EOF
```

#### Database Scaling Options
```bash
# PostgreSQL read replica setup
# This requires additional configuration in docker-compose.yml
# Consider managed database services for production scaling

# Connection pooling with PgBouncer
cat > config/pgbouncer/pgbouncer.ini << 'EOF'
[databases]
witchcityrope = host=postgres-db port=5432 dbname=witchcityrope

[pgbouncer]
listen_addr = 0.0.0.0
listen_port = 6432
auth_type = trust
pool_mode = transaction
max_client_conn = 100
default_pool_size = 20
EOF
```

### Vertical Scaling

#### Resource Optimization
```bash
# Monitor resource usage patterns
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" > resource_usage.log

# Adjust resource limits based on usage
# Edit docker-compose.prod.yml deploy.resources sections

# Database tuning for more resources
# Update config/postgres/postgresql.conf with higher limits
```

### Auto-scaling with Docker Swarm

#### Swarm Mode Setup
```bash
# Initialize Docker Swarm
docker swarm init

# Deploy as stack
docker stack deploy -c docker-compose.yml -c docker-compose.prod.yml witchcityrope

# Scale services
docker service scale witchcityrope_api-service=3
docker service scale witchcityrope_react-web=2
```

This production deployment guide provides a comprehensive foundation for deploying WitchCityRope with the proven authentication architecture. The deployment leverages the validated Hybrid JWT + HttpOnly Cookies pattern and includes all necessary security, monitoring, and operational procedures for a production environment.

**Key Success Factors:**
- Follow the security checklist completely
- Test all health endpoints after deployment
- Monitor logs during initial deployment period
- Implement automated backups immediately
- Schedule regular security updates

For ongoing operational support, refer to the [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) for day-to-day container management procedures.