# Production Environment Configuration

## Overview

This document details the production environment setup and configuration for WitchCityRope.

## Server Requirements

### Minimum Requirements
- **OS**: Ubuntu 22.04 LTS (or compatible Linux distribution)
- **CPU**: 2 vCPUs
- **RAM**: 2GB
- **Storage**: 20GB SSD
- **Network**: 100Mbps connection
- **Ports**: 80, 443, 22 (SSH)

### Recommended Requirements
- **CPU**: 4 vCPUs
- **RAM**: 4GB
- **Storage**: 50GB SSD
- **Network**: 1Gbps connection

## Environment Variables

### Application Settings

```bash
# Core Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
DOTNET_RUNNING_IN_CONTAINER=true

# Application URLs
APP_URL=https://yourdomain.com
APP_NAME="Witch City Rope"

# Feature Flags
FEATURE_REGISTRATION_ENABLED=true
FEATURE_EVENTS_ENABLED=true
FEATURE_MARKETPLACE_ENABLED=true
FEATURE_MAINTENANCE_MODE=false
```

### Database Configuration

```bash
# PostgreSQL Connection
ConnectionStrings__DefaultConnection="Server=db;Port=5432;Database=WitchCityRope;User Id=witchcity;Password=your-secure-password;Include Error Detail=true"

# Database Pool Settings
Database__MaxPoolSize=100
Database__CommandTimeout=30
Database__EnableRetryOnFailure=true
Database__MaxRetryCount=3
```

### Security Configuration

```bash
# JWT Authentication
Authentication__JwtKey="your-very-long-secure-jwt-key-at-least-32-characters"
Authentication__JwtIssuer="https://yourdomain.com"
Authentication__JwtAudience="https://yourdomain.com"
Authentication__JwtExpirationDays=7
Authentication__RefreshTokenExpirationDays=30

# Password Policy
Authentication__Password__RequireDigit=true
Authentication__Password__RequiredLength=8
Authentication__Password__RequireNonAlphanumeric=true
Authentication__Password__RequireUppercase=true
Authentication__Password__RequireLowercase=true

# Security Headers
Security__EnableHsts=true
Security__EnableXssProtection=true
Security__EnableFrameOptions=true
Security__ContentSecurityPolicy="default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline';"
```

### Email Configuration

```bash
# SMTP Settings
Email__SmtpServer=smtp.sendgrid.net
Email__SmtpPort=587
Email__SmtpUsername=apikey
Email__SmtpPassword=your-sendgrid-api-key
Email__FromEmail=noreply@witchcityrope.com
Email__FromName="Witch City Rope"
Email__EnableSsl=true

# Email Templates
Email__Templates__WelcomeEmail=true
Email__Templates__PasswordReset=true
Email__Templates__EventReminder=true
Email__Templates__PaymentConfirmation=true
```

### Storage Configuration

```bash
# S3-Compatible Storage
Storage__Provider=S3
Storage__S3__ServiceUrl=https://s3.amazonaws.com
Storage__S3__BucketName=witchcityrope-assets
Storage__S3__AccessKey=your-access-key
Storage__S3__SecretKey=your-secret-key
Storage__S3__Region=us-east-1
Storage__S3__UseHttp=false

# Upload Limits
Storage__MaxFileSize=10485760  # 10MB
Storage__AllowedExtensions=.jpg,.jpeg,.png,.gif,.webp
Storage__ImageProcessing__MaxWidth=2048
Storage__ImageProcessing__MaxHeight=2048
Storage__ImageProcessing__ThumbnailSize=300
```

### Caching Configuration

```bash
# Redis Cache
Cache__Provider=Redis
Cache__Redis__ConnectionString=redis:6379
Cache__Redis__InstanceName=WitchCityRope
Cache__DefaultExpirationMinutes=60

# Cache Settings
Cache__EnableResponseCaching=true
Cache__EnableOutputCaching=true
Cache__CacheDuration__Short=300     # 5 minutes
Cache__CacheDuration__Medium=1800   # 30 minutes
Cache__CacheDuration__Long=3600     # 1 hour
```

### Logging Configuration

```bash
# Serilog Settings
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft=Warning
Serilog__MinimumLevel__Override__System=Warning

# Log Outputs
Serilog__WriteTo__Console=true
Serilog__WriteTo__File=true
Serilog__WriteTo__FilePath=/logs/witchcityrope-.log
Serilog__WriteTo__RollingInterval=Day
Serilog__WriteTo__RetainedFileCountLimit=30

# Structured Logging
Serilog__Enrich__FromLogContext=true
Serilog__Enrich__WithMachineName=true
Serilog__Enrich__WithEnvironmentName=true
```

### Performance Settings

```bash
# Application Performance
Performance__EnableResponseCompression=true
Performance__EnableRequestDecompression=true
Performance__MaxConcurrentRequests=100
Performance__RequestTimeout=30

# Database Performance
Performance__EnableQueryLogging=false
Performance__SlowQueryThreshold=1000  # milliseconds
Performance__EnableConnectionPooling=true
```

### Monitoring Configuration

```bash
# Application Insights
ApplicationInsights__InstrumentationKey=your-instrumentation-key
ApplicationInsights__EnableAdaptiveSampling=true
ApplicationInsights__EnableDependencyTracking=true

# Health Checks
HealthChecks__Database__Enabled=true
HealthChecks__Database__Timeout=10
HealthChecks__Redis__Enabled=true
HealthChecks__Storage__Enabled=true
HealthChecks__Email__Enabled=true
```

### Rate Limiting

```bash
# API Rate Limiting
RateLimiting__EnableRateLimiting=true
RateLimiting__PermitLimit=100
RateLimiting__Window=60  # seconds
RateLimiting__QueueProcessingOrder=OldestFirst
RateLimiting__QueueLimit=50

# Specific Endpoints
RateLimiting__Endpoints__Login__PermitLimit=5
RateLimiting__Endpoints__Login__Window=300
RateLimiting__Endpoints__Register__PermitLimit=3
RateLimiting__Endpoints__Register__Window=3600
```

### Payment Processing

```bash
# Stripe Configuration
Stripe__PublishableKey=pk_live_your-key
Stripe__SecretKey=sk_live_your-key
Stripe__WebhookSecret=whsec_your-webhook-secret
Stripe__Currency=usd
Stripe__PaymentMethods=card

# Payment Settings
Payment__EnableTestMode=false
Payment__RequireEmailReceipt=true
Payment__AutomaticTax=true
```

## System Configuration

### Firewall Rules

```bash
# UFW Configuration
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
```

### Kernel Parameters

```bash
# /etc/sysctl.conf
net.core.somaxconn = 65535
net.ipv4.tcp_max_syn_backlog = 65535
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_fin_timeout = 30
vm.swappiness = 10
```

### Service Limits

```bash
# /etc/security/limits.conf
* soft nofile 65535
* hard nofile 65535
* soft nproc 32768
* hard nproc 32768
```

## Docker Configuration

### Docker Daemon Settings

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "default-ulimits": {
    "nofile": {
      "Name": "nofile",
      "Hard": 65535,
      "Soft": 65535
    }
  }
}
```

### Docker Compose Override

```yaml
# docker-compose.override.yml
version: '3.8'

services:
  web:
    restart: always
    mem_limit: 1g
    memswap_limit: 2g
    cpus: '2.0'
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  db:
    restart: always
    mem_limit: 512m
    command: >
      postgres
      -c max_connections=200
      -c shared_buffers=256MB
      -c effective_cache_size=1GB
      -c maintenance_work_mem=64MB
```

## Monitoring Setup

### Health Check Endpoints

```bash
# Application Health
/health          # Basic health check
/health/ready    # Readiness probe
/health/live     # Liveness probe

# Detailed Health
/health/db       # Database connectivity
/health/redis    # Cache connectivity
/health/storage  # Storage accessibility
```

### Log Aggregation

```bash
# Filebeat Configuration
filebeat.inputs:
- type: container
  paths:
    - '/var/lib/docker/containers/*/*.log'
  processors:
    - add_docker_metadata:
        host: "unix:///var/run/docker.sock"

output.elasticsearch:
  hosts: ["localhost:9200"]
  index: "witchcityrope-%{+yyyy.MM.dd}"
```

## Security Hardening

### SSL/TLS Configuration

```nginx
# Strong SSL Configuration
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
ssl_prefer_server_ciphers off;
ssl_session_cache shared:SSL:10m;
ssl_session_timeout 10m;
ssl_stapling on;
ssl_stapling_verify on;
```

### Security Headers

```nginx
# Nginx Security Headers
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';" always;
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
```

## Backup Configuration

### Automated Backups

```bash
# Backup Environment Variables
BACKUP_RETENTION_DAYS=30
BACKUP_S3_BUCKET=witchcityrope-backups
BACKUP_ENCRYPTION_KEY=your-backup-encryption-key
BACKUP_NOTIFICATION_EMAIL=admin@witchcityrope.com
```

### Backup Schedule

```cron
# Database Backups
0 2 * * * /opt/witchcityrope/scripts/backup-database.sh
0 14 * * * /opt/witchcityrope/scripts/backup-database.sh

# File Backups
0 3 * * * /opt/witchcityrope/scripts/backup-uploads.sh

# Configuration Backups
0 4 * * 0 /opt/witchcityrope/scripts/backup-configs.sh
```

## Maintenance Scripts

### Health Check Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/health-check.sh

HEALTH_URL="http://localhost:5000/health"
TIMEOUT=10

if curl -f -s --max-time $TIMEOUT "$HEALTH_URL" > /dev/null; then
    echo "Application is healthy"
    exit 0
else
    echo "Application health check failed"
    # Send alert
    curl -X POST https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
         -H 'Content-type: application/json' \
         -d '{"text":"WitchCityRope health check failed!"}'
    exit 1
fi
```

### Log Rotation

```bash
# /etc/logrotate.d/witchcityrope
/opt/witchcityrope/logs/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 0644 www-data www-data
    sharedscripts
    postrotate
        docker-compose -f /opt/witchcityrope/docker-compose.prod.yml kill -s USR1 web
    endscript
}
```